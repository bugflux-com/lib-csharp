using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace Bugflux
{
    /// <summary>
    /// Class representing data which doesn't change along with every error.
    /// </summary>
    public class ProjectClientInfo
    {

        private string projectKey = "00000000";

        /// <summary>
        /// Project key. Default value is 00000000. It is important to set it, otherwise serwer will reject your reports.
        /// </summary>
        public string ProjectKey
        {
            get
            {
                return projectKey;
            }
            set
            {
                if (!String.IsNullOrEmpty(value))
                    projectKey = value.Substring(0, Math.Min(value.Length, Report.MAX_STR_LEN));
            }
        }

        private string version = "0.0.0.0";

        /// <summary>
        /// Version number of the project. Default value is taken from info about assembly.
        /// </summary>
        public string Version
        {
            get
            {
                return version;
            }
            set
            {
                if (!String.IsNullOrEmpty(value))
                    version = value.Substring(0, Math.Min(value.Length, Report.MAX_STR_LEN));
            }
        }

        private string operatingSystem = "Unknown";

        /// <summary>
        /// Operating system used by the user. If it is possible to get the info set to Unknown.
        /// </summary>
        public string OperatingSystem
        {
            get
            {
                return operatingSystem;
            }
            set
            {
                if (!String.IsNullOrEmpty(value))
                    operatingSystem = value.Substring(0, Math.Min(value.Length, Report.MAX_STR_LEN));
            }
        }

        private string language = "en_US";

        /// <summary>
        /// Language of the application, not OS (eq. pl_PL - language_REGION). Default value is en_US.
        /// </summary>
        public string Language
        {
            get
            {
                return language;
            }
            set
            {
                if (!String.IsNullOrEmpty(value))
                    language = value.Substring(0, Math.Min(value.Length, Report.MAX_STR_LEN));
            }
        }

        private string environment = "Development";

        /// <summary>
        /// Environment name (eq. Development, Testing, Production). If debbuger is attached, set to Development, otherwise Production.
        /// </summary>
        public string Environment
        {
            get
            {
                return environment;
            }
            set
            {
                if (!String.IsNullOrEmpty(value))
                    environment = value.Substring(0, Math.Min(value.Length, Report.MAX_STR_LEN));
            }
        }

        private string clientId;

        /// <summary>
        /// Client unique id (explicitly identifies the device or user). Max length is 64. By default it is hash of string combined from baseboard and bios serial numbers, processor id and uuid of the system. If none of these values is available it is set to Report.FAIL_HASH.
        /// </summary>
        public string ClientId
        {
            get
            {
                return clientId;
            }
            set
            {
                if (!String.IsNullOrEmpty(value))
                    clientId = value.Substring(0, Math.Min(value.Length, Report.MAX_ID_LEN));
            }
        }

        /// <summary>
        /// Enum with possible environments
        /// </summary>
        public enum Environments
        {
            /// <summary>
            /// Development, when creating project or aplication.
            /// </summary>
            DEVELOPMENT,
            /// <summary>
            /// When testing application
            /// </summary>
            TESTING,
            /// <summary>
            /// After deployment
            /// </summary>
            PRODUCTION
        }

        /// <summary>
        /// Constructor sets ClientId and OperatingSystem values. You can change them if you want to example set CliendId based on license numer.
        /// </summary>
        /// <param name="projectKey">Project key. You should take it from bugflux server, it is generated for every project.</param>
        /// <param name="version">Version number of the project. If null or empty, taken from assembly info.</param>
        /// <param name="language">Language of application, not OS. If null or empty, it is default value en_US.</param>
        /// <param name="environment">Environment name. If null or empty, Development in case of debbuger attached, Production otherwise.</param>
        public ProjectClientInfo(string projectKey = null, string version = null, string language = null, string environment = null)
        {
            setOperatingSystem();
            setClientId();

            if (!String.IsNullOrEmpty(projectKey))
                ProjectKey = projectKey;

            if (!String.IsNullOrEmpty(version))
                Version = version;
            else
                setVersion();

            Language = language;

            if (!String.IsNullOrEmpty(environment))
                Environment = environment;
            else
                setEnvironment();
        }

        #region Initial mathods

        private void setOperatingSystem()
        {
            try
            {

                switch (System.Environment.OSVersion.Platform)
                {
                    case System.PlatformID.Win32Windows:
                    case System.PlatformID.Win32NT:
                        setWindowsOperatingSystem();
                        break;
                    case System.PlatformID.Unix:
                    case System.PlatformID.MacOSX:
                        setUnixMacOperatingSystem();
                        break;
                    default:
                        break;
                }
                
            }
            catch (Exception) { }
        }

        private void setWindowsOperatingSystem()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
                using (var mngobj = searcher.Get())
                {
                    var name = (from x in mngobj.Cast<ManagementObject>()
                                select x.GetPropertyValue("Caption")).FirstOrDefault();

                    OperatingSystem = name != null && name.ToString() != "" ? name.ToString() : "Unknown";
                }
            }
            catch (Exception) { }
        }

        private void setUnixMacOperatingSystem()
        {
            try
            {
                OperatingSystem = "UNIX OR MAC";
                // TODO
            }
            catch (Exception) { }
        }

        private void setClientId()
        {
            // TODO fro UNIX and MAC

            string processorsIds = getProcessorsIds();
            string baseBoardId = getBaseBoardSerialNumber();
            string UUID = getUUID();
            string BIOSId = getBIOSSerialNumber();

            string concatenated = processorsIds + baseBoardId + UUID + BIOSId;
            if (concatenated.Length != 0)
            {
                string hex = Report.HexHashFromString(concatenated);
                ClientId = hex;
            }
            else
            {
                ClientId = Report.FAIL_HASH;
            }
        }

        private static string getProcessorsIds()
        {
            string processorsIds = "";

            try {

                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "Select * FROM WIN32_Processor"))
                using (ManagementObjectCollection mObject = searcher.Get())
                {
                    if (mObject.Count != 0)
                    {
                        foreach (ManagementObject obj in mObject)
                        {
                            string nextProcessorId = "";

                            try
                            {
                                nextProcessorId = obj["ProcessorId"].ToString();
                            }
                            catch (Exception)
                            {
                                nextProcessorId = "";
                            }

                            processorsIds += nextProcessorId;
                        }
                    }
                }
                return processorsIds;
            }
            catch (Exception)
            {
                return "";
            }
        }

        private static string getBaseBoardSerialNumber()
        {
            string baseBoardIds = "";

            try {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "Select * FROM WIN32_BaseBoard"))
                using (ManagementObjectCollection mObject = searcher.Get())
                {
                    if (mObject.Count != 0)
                    {
                        foreach (ManagementObject obj in mObject)
                        {
                            string nextBaseBoardId = "";

                            try
                            {
                                nextBaseBoardId = obj["SerialNumber"].ToString();
                            }
                            catch (Exception)
                            {
                                nextBaseBoardId = "";
                            }

                            baseBoardIds += nextBaseBoardId;
                        }
                    }
                }
                return baseBoardIds;
            }
            catch (Exception)
            {
                return "";
            }
        }

        private static string getUUID()
        {
            string UUIDs = "";

            try {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                   "Select * FROM WIN32_ComputerSystemProduct"))
                using (ManagementObjectCollection mObject = searcher.Get())
                {
                    if (mObject.Count != 0)
                    {
                        foreach (ManagementObject obj in mObject)
                        {
                            string nextUUID = "";

                            try
                            {
                                nextUUID = obj["UUID"].ToString();
                            }
                            catch (Exception)
                            {
                                nextUUID = "";
                            }

                            UUIDs += nextUUID;
                        }
                    }
                }
                return UUIDs;
            }
            catch(Exception)
            {
                return "";
            }
        }

        private static string getBIOSSerialNumber()
        {
            string BIOSSerialNumbers = "";

            try {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                  "Select * FROM WIN32_BIOS"))
                using (ManagementObjectCollection mObject = searcher.Get())
                {
                    if (mObject.Count != 0)
                    {
                        foreach (ManagementObject obj in mObject)
                        {
                            string nextBIOSId = "";

                            try
                            {
                                nextBIOSId = obj["SerialNumber"].ToString();
                            }
                            catch (Exception)
                            {
                                nextBIOSId = "";
                            }

                            BIOSSerialNumbers += nextBIOSId;
                        }
                    }
                }
                return BIOSSerialNumbers;
            }
            catch (Exception)
            {
                return "";
            }
        }

        private void setVersion()
        {
            try {
                if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
                {
                    string ver = System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
                    if (!String.IsNullOrEmpty(ver))
                        Version = ver;
                    else throw new Exception("Empty version!");
                }
                else
                {
                    string ver = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
                    if (!String.IsNullOrEmpty(ver))
                        Version = ver;
                    else throw new Exception("Empty version!");
                }
            }
            catch (Exception)
            {
                Version = "0.0.0.0";
            }
            
        }

        private void setEnvironment()
        {
            if (Debugger.IsAttached)
                Environment = "Development";
            else
                Environment = "Production";
        }

        #endregion // Initial mathods

        /// <summary>
        /// Sets environment using enum with base environments.
        /// </summary>
        /// <param name="environment"></param>
        public void SetEnvironment(Environments environment)
        {
            switch (environment)
            {

                case Environments.DEVELOPMENT:
                    Environment = "Development";
                    break;
                case Environments.TESTING:
                    Environment = "Testing";
                    break;
                case Environments.PRODUCTION:
                    Environment = "Production";
                    break;
                default:
                    Environment = "Unknown";
                    break;
            }
        }

    }

    /// <summary>
    /// Class representing error report which is sent to bugflux server.
    /// </summary>
    public class Report
    {
        /// <summary>
        /// Mas string length of some values to be send to bugflux
        /// </summary>
        public static int MAX_STR_LEN = 255;

        /// <summary>
        /// Max length of ClientId
        /// </summary>
        public static int MAX_ID_LEN = 64;

        /// <summary>
        /// Max length of error Hash
        /// </summary>
        public static int MAX_HASH_LEN = 64;

        /// <summary>
        /// Hash used when none of the values required to compute hash is available (when creating ClientId or error Hash)
        /// </summary>
        public static string FAIL_HASH = "BugfluxDefaultFailHash";

        private string hash = FAIL_HASH;

        /// <summary>
        /// Hash uniquely identifying an exception. Max length is 64. By default it is computed from file name and method where exception occured and exception type. If it is not possible to get this data it is set to Report.FAIL_HASH.
        /// </summary>
        public string Hash
        {
            get
            {
                return hash;
            }
            set
            {
                if (!String.IsNullOrEmpty(value))
                    hash = value.Substring(0, Math.Min(value.Length, MAX_HASH_LEN));
            }
        }

        private string name;

        /// <summary>
        /// Name (title) of the exception. By default it is Message property of Exception.
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                if (!String.IsNullOrEmpty(value))
                    name = value.Substring(0, Math.Min(value.Length, MAX_STR_LEN));
            }
        }

        private StackTrace stackTrace;

        /// <summary>
        /// Full stack trace dump. By default it is Exception.StackTrace property. If null, then StackTrace of place, where Report with this exception was created is set.
        /// </summary>
        public StackTrace StackTrace 
        {
            get
            {
                return stackTrace;
            }
            set
            {
                if (value != null)
                    stackTrace = value;
            }
        }

        /// <summary>
        /// Message filled by the user or by the programmer (log with user's actions). By default it is empty as it is not required.
        /// </summary>
        public string Message { get; set; }


        /// <summary>
        /// Report constructor.
        /// </summary>
        /// <param name="ex">Exception which occured.</param>
        /// <param name="message">>Message filled by the user or by the programmer (log with user's actions).</param>
        /// <param name="name">Name (title) of the exception.</param>
        /// <param name="hash">Hash uniquely identifying an exception.</param>        
        public Report(Exception ex, string message = "", string name = null, string hash = null)
        {
            if (name == null || name == "")
            {
                var nameForError = ex.Message;
                if (nameForError == null || nameForError == "")
                {
                    nameForError = ex.GetType().ToString();
                    if (nameForError == null || nameForError == "")
                        Name = "No error message, no error type.";
                    else
                        Name = nameForError;
                }
                else
                    Name = nameForError;
            }
            else
                Name = name;
            if (ex.StackTrace == null || ex.StackTrace == "")
                StackTrace = new StackTrace(1, true);
            else
                StackTrace = new StackTrace(ex, true);

            Message = message;

            if (String.IsNullOrEmpty(hash))
                setDefaultHash(ex);
            else
                Hash = hash;
        }


        private void setDefaultHash(Exception ex)
        {
            try
            {
                var query = StackTrace.GetFrames()         // get the frames
                              .Select(frame => new
                              {                   // get the info
                                  FileName = frame.GetFileName(),
                                  LineNumber = frame.GetFileLineNumber(),
                                  ColumnNumber = frame.GetFileColumnNumber(),
                                  Method = frame.GetMethod(),
                                  Class = frame.GetMethod().DeclaringType,
                              });

                var firstframe = query.ElementAt(0);
                string filename = Path.GetFileName(firstframe.FileName);
                string classofexception = ex.GetType().ToString();

                string concatenated = filename + classofexception + firstframe.Method;
                string hex = HexHashFromString(concatenated);
                Hash = hex;
            }
            catch (Exception)
            {
                Hash = Report.FAIL_HASH;
            }
        }

        /// <summary>
        /// Computes SHA256 hash
        /// </summary>
        /// <param name="s">String from which hash shoild be computed</param>
        /// <returns>String with hexadecimal representation of given string</returns>
        public static string HexHashFromString(string s)
        {
            byte[] byteContents = Encoding.Unicode.GetBytes(s);
            System.Security.Cryptography.SHA256 hash =
            new System.Security.Cryptography.SHA256CryptoServiceProvider();
            byte[] hashText = hash.ComputeHash(byteContents);
            string hex = BitConverter.ToString(hashText).Replace("-", "");
            return hex;
        }


        /// <summary>
        /// Sends report to bugflux server. Firstly creates json with CreateJSON() method and then sends it with SendJSON().
        /// </summary>
        /// <param name="config">Configuration which should be used to fill report data and send it to server.</param>
        /// <returns>Result with exception information in case of fail.</returns>
        public Result Send(Config config)
        {
            string json = CreateJSON(config);
            return SendJSON(json, config);
        }

        /// <summary>
        /// Sends report to bugflux server asynchronously. Firstly creates json with CreateJSON() method and then sends it with SendJSONAsync().
        /// </summary>
        /// <param name="config"></param>
        public void SendAsync(Config config)
        {
            string json = CreateJSON(config);
            SendJSONAsync(json, config);
        }

        /// <summary>
        /// Creates json string from all report fields and also client and project information from ProjectAndClientInfo field in config.
        /// </summary>
        /// <param name="config">Configuration to be used when creating json.</param>
        /// <returns>Json string created.</returns>
        public string CreateJSON(Config config)
        {
            String message = "";
            if (String.IsNullOrEmpty(Message))
            {
                if (config.IncludeTraceInMessage)
                    message = config.GetTraceInformation();
            }
            else
                message = Message;

            try
            {
                string json = new JavaScriptSerializer().Serialize(new
                {
                    project = config.ProjectAndClientInfo.ProjectKey,
                    version = config.ProjectAndClientInfo.Version,
                    system = config.ProjectAndClientInfo.OperatingSystem,
                    language = config.ProjectAndClientInfo.Language,
                    hash = Hash,
                    name = Name,
                    environment = config.ProjectAndClientInfo.Environment,
                    stack_trace = StackTrace.ToString(),
                    message = message,
                    client_id = config.ProjectAndClientInfo.ClientId
                });

                return json;
            }
            catch (Exception)
            {
                return "{}";
            }
        }


        /// <summary>
        /// Sends data to bugflux server.
        /// </summary>
        /// <param name="json">String supposed to be json (otherwise it will fail) with all required fields.</param>
        /// <param name="config">Configuration which should be used to send json to server.</param>
        /// <returns>Result with exception information in case of fail.</returns>
        public static Result SendJSON(string json, Config config)
        {
            Result ret = new Result();
            ret.JsonTriedToBeSent = json;

            try
            {
                string url = config.ServerAddress + "/api/" + config.ApiVersion + config.ErrorsPath;
                HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                httpWebRequest.ContentType = "application/json; charset=utf-8";
                httpWebRequest.Method = "POST";
                httpWebRequest.Accept = "application/json; charset=utf-8";

                if(!config.StrictSSL)
                    httpWebRequest.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => { return true; };

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(json);
                    streamWriter.Flush();
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    ret.ServerResponseIfOK = httpResponse;
                    ret.ServerResponseBodyIfOK = new StreamReader(httpResponse.GetResponseStream()).ReadToEnd();
                    return ret;
                }
                string info = new StreamReader(httpResponse.GetResponseStream()).ReadToEnd();
                ret.ExceptionThrown = new Exception("Wrong server answer: " + info);
                return ret;
            }
            catch (Exception ex)
            {
                ret.ExceptionThrown = ex;
                return ret;
            }
        }

        /// <summary>
        /// Send data to Bugflux server asynchronously
        /// </summary>
        /// <param name="json">String supposed to be json (otherwise it will fail) with all required fields</param>
        /// <param name="config">Configuration which should be used to send json to server.</param>
        public static void SendJSONAsync(string json, Config config)
        {
            try
            {
                string url = config.ServerAddress + "/api/" + config.ApiVersion + config.ErrorsPath;
                HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                httpWebRequest.ContentType = "application/json; charset=utf-8";
                httpWebRequest.Method = "POST";
                httpWebRequest.Accept = "application/json; charset=utf-8";

                if (!config.StrictSSL)
                    httpWebRequest.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => { return true; };

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(json);
                    streamWriter.Flush();
                }

                httpWebRequest.BeginGetResponse(null, null);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// This method is for lazy people, who do not want to create object and then invoke send() method on it, instead they can use this method, which does these two actions.
        /// </summary>
        /// <param name="ex">Exception which occured.</param>
        /// <param name="config">Configuration which should be used to fill report data and send it to server.</param>
        /// <param name="message">Message filled by the user or by the programmer (log with user's actions).</param>
        /// <param name="name">Name (title) of the exception.</param>
        /// <param name="hash">Hash uniquely identifying an exception.</param>  
        /// <returns>Result with exception information in case of fail</returns>
        public static Result Send(Exception ex, Config config = null, string message = "", string name = null, string hash = null)
        {
            Report rep = new Report(ex, message, name, hash);
            Config c;
            if (config == null)
                c = Config.DefaultConfig;
            else
                c = config;
            return rep.Send(c);
        }

    }
}
