using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Bugflux
{
    /// <summary>
    /// Class which holds information about server configuration and helps in dealing with unhandledEnableDefaultBehaviour exceptions.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Address of bugflux server. Default value is https://bugflux so don't forget to change it!
        /// </summary>
        public string ServerAddress { get; set; }

        /// <summary>
        /// Api Version of bugflux server (this library is created for v1 and it is default value). If your bugflux server support newer api version download proper library!
        /// </summary> 
        public string ApiVersion { get; set; }

        /// <summary>
        /// Path where to post reports. In v1 of the bugflux api this path is /errors and this is default value.
        /// </summary> 
        public string ErrorsPath { get; set; }

        private ProjectClientInfo projectAndClientInfo;

        /// <summary>
        /// Information about project and client.
        /// </summary>
        public ProjectClientInfo ProjectAndClientInfo
        {
            get
            {
                return projectAndClientInfo;
            }
            set
            {
                if (value != null)
                    projectAndClientInfo = value;
            }
        }

        private static List<Config> defaultConfigs { get; set; }

        /// <summary>
        /// List of default configs. It always has at least one element - defaultConfig.
        /// </summary>
        public static List<Config> DefaultConfigs
        {
            get
            {
                return new List<Config>(defaultConfigs);
            }
        }

        /// <summary>
        /// Default configuration. It is used as default configuration for every raport sent with default unhandled exceptions handler if enableDefaultBehaviour is set to true. 
        /// </summary>
        public static Config DefaultConfig
        {
            get
            {
                return DefaultConfigs[0];
            }
            set
            {
                if(value != null)
                    defaultConfigs[0] = value;
            }
        }

        /// <summary>
        /// If you are using default behaviour for unhandled exceptions you can add more default configs - then when unhandled exception is thrown, information about it will be sent as many times as you have default configs, using each of these configs once.
        /// </summary>
        /// <param name="config">Config to be added.</param>
        public static void AddDefaultConfig(Config config) {
            defaultConfigs.Add(config);
        }

        /// <summary>
        /// Removes last added default config. It never removes first default config, only configs added by you. 
        /// </summary>
        public static void RemoveLastDefaultConfig() {
            if(defaultConfigs.Count > 1) {
                defaultConfigs.Remove(defaultConfigs.Last());
            }
        }

        private static bool enableDefaultBehaviour = false;

        /// <summary>
        /// Whether to handle unhandled exceptions. At default false. If true every unhandled exception is sent to buglux server.
        /// </summary>
        public static bool EnableDefaultBehaviour
        {
            get
            {
                return enableDefaultBehaviour;
            }
            set
            {
                if (value == true && enableDefaultBehaviour == false)
                {
                    turnOnDefaultExceptionHandling();
                    enableDefaultBehaviour = true;
                }
                else if (value == false && enableDefaultBehaviour == true)
                {
                    turnOffDefaultExceptionHandling();
                    enableDefaultBehaviour = false;
                }

            }
        }

        private static UnhandledExceptionEventHandler extraExceptionHandler = null;

        private bool includeTraceInMessage = false;
        private StringBuilder stringBuilderForTrace;

        /// <summary>
        /// Whether to include information from Trace class in Message field. At default false.
        /// </summary>
        public bool IncludeTraceInMessage
        {
            get
            {
                return includeTraceInMessage;
            }
            set
            {
                if (value == true && includeTraceInMessage == false)
                {
                    turnOnIncludingTraceInMessage();
                    includeTraceInMessage = true;
                }
                else if (value == false && enableDefaultBehaviour == true)
                {
                    turnOffIncludingTraceInMessage();
                    includeTraceInMessage = false;
                }

            }
        }

       

        /// <summary>
        /// Whether to write error message to console in default bugflux uncought exceptions handler.
        /// </summary>
        public static bool Silent { get; set; }

        /// <summary>
        /// Whether to reject unsafe connections (unknown certificate etc.), when using https
        /// </summary>
        public bool StrictSSL { get; set; }

        /// <summary>
        /// Config constructor.
        /// </summary>
        /// <param name="serverAddress">Address of bugflux server.</param>
        /// <param name="projectAndClientInfo">Information about project and client.</param>
        /// <param name="apiVersion">Api Version of bugflux server.</param>
        /// <param name="errorsPath">Path where to post reports.</param>
        public Config(string serverAddress = "https://bugflux",
            ProjectClientInfo projectAndClientInfo = null,
            string apiVersion = "v1",
            string errorsPath = "/errors")
        {
            ServerAddress = serverAddress;
            ApiVersion = apiVersion;
            ErrorsPath = errorsPath;
                        
            StrictSSL = true;

            if(projectAndClientInfo == null)
                ProjectAndClientInfo = new ProjectClientInfo();
            else            
                ProjectAndClientInfo = projectAndClientInfo;            
        }

        static Config()
        {
            Silent = false;
            defaultConfigs = new List<Config>();
            Config defaultConfig = new Config();
            defaultConfigs.Add(defaultConfig);            
        }


        #region Default exception handling

        private static void turnOnDefaultExceptionHandling()
        {
            addExceptionHandler(new UnhandledExceptionEventHandler(DefaulExceptionHandler));
        }

        private static void turnOffDefaultExceptionHandling()
        {
            removeExceptionHandler(new UnhandledExceptionEventHandler(DefaulExceptionHandler));
        }

        private static void DefaulExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Report rep = new Report(e);
            foreach (Config c in defaultConfigs)
            {
                rep.SendAsync(c);
            }
            
            if (!Config.Silent)
            {
                string times = defaultConfigs.Count == 1 ? "" : "\nSent using " + defaultConfigs.Count + " configurations.";
                Console.WriteLine("Default bugflux exception handler: " + rep.Name + times);
            }
        }

        /// <summary>
        /// Adds unhandled exceptions handler. Only one programmer's method can be set as handler, so if already another method has been set as handler, it is removed. This method doesn't remove default exception handler set with Config.EnableDefaultBehaviour = true, to do this, write Config.EnableDefaultBehaviour = false.
        /// </summary>
        /// <param name="method">Method taking object and UnhandledExceptionEventArgs params which should handle unhandled exceptions.</param>
        public static void SetUnhandledExceptionHandler(Action<object, UnhandledExceptionEventArgs> method)
        {
            if (extraExceptionHandler != null)
                removeExceptionHandler(extraExceptionHandler);
            var handler = new UnhandledExceptionEventHandler(method);
            addExceptionHandler(handler);
            extraExceptionHandler = handler;
        }

        /// <summary>
        /// Removes handler previously set with setExtraExceptionHandlerMethod method. If no handler is set, it does nothing.
        /// </summary>
        public static void RemoveUnhandledExceptionHandler()
        {
            if (extraExceptionHandler == null)
                return;
            removeExceptionHandler(extraExceptionHandler);
            extraExceptionHandler = null;
        }

        private static void addExceptionHandler(UnhandledExceptionEventHandler handler)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += handler;
        }

        private static void removeExceptionHandler(UnhandledExceptionEventHandler handler)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException -= handler;
        }

        #endregion // Default exception handling


        #region Including Trace in Message

        private void turnOnIncludingTraceInMessage()
        {
            stringBuilderForTrace = new StringBuilder();
            Trace.Listeners.Add(new TextWriterTraceListener(new System.IO.StringWriter(stringBuilderForTrace)));
        }

        private void turnOffIncludingTraceInMessage()
        {
            Trace.Listeners.Add(new TextWriterTraceListener(new System.IO.StringWriter(stringBuilderForTrace)));
            stringBuilderForTrace = null;
        }

        /// <summary>
        /// Gets information from Trace since including Trace in Message field was turned on.
        /// </summary>
        /// <returns>All text written by Trace or empty string if including Trace in Message is on.</returns>
        public string GetTraceInformation()
        {
            return includeTraceInMessage ? stringBuilderForTrace.ToString() : "";
        }

        /// <summary>
        /// Clears information received from Trace class. If including Trace in Message field is off, this method has no effect.
        /// </summary>
        public void ClearInformationFromTrace()
        {
            if (includeTraceInMessage)
                stringBuilderForTrace.Clear();
        }

        #endregion // Including Trace in Message
    }
}
