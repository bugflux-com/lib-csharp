using System;
using System.IO;
using System.Net;

namespace Bugflux
{
    /// <summary>
    /// Class representing result after sending report to bugflux sender. Result can be casted to bool so you can write for eample if(report.send()) { ... } 
    /// </summary>
    public class Result
    {
        /// <summary>
        /// Exception which was thrown when trying to send data to server.
        /// </summary>
        public Exception ExceptionThrown { get; set; }

        /// <summary>
        /// Json which was tried to be send to server. This field is always set. If json was not the reason of fail, you can try to save it somewhere and try to send it later.
        /// </summary>
        public string JsonTriedToBeSent { get; set; }

        /// <summary>
        /// Response of the server in case of success.
        /// </summary>
        public HttpWebResponse ServerResponseIfOK { get; set; }

        /// <summary>
        /// Body of the response of the server in case of success.
        /// </summary>
        public string ServerResponseBodyIfOK { get; set; }

        /// <summary>
        /// Result constructor.
        /// </summary>
        public Result()
        {
            JsonTriedToBeSent = null;
            ExceptionThrown = null;
        }

        /// <summary>
        /// Converts Result to Bool, based on ExceptionThrown.
        /// </summary>
        /// <param name="result">Result to be converted</param>
        /// <returns>Bool telling whether sending succeeded</returns>
        public static implicit operator bool(Result result)
        {
            return result.ExceptionThrown == null ? true : false;
        }

        /// <summary>
        /// Reads server answer body from ExceptionThrown if exception is WebException.
        /// </summary>
        /// <returns>Null if ExceptionThrown is not WebException and server response body otherwise.</returns>
        public string GetServerResponseBody()
        {
            System.Net.WebResponse resp = GetServerResponse();
            if (resp == null)
                return null;

            return resp == null ? null : new StreamReader(resp.GetResponseStream()).ReadToEnd();          
        }

        /// <summary>
        /// Gets server response from ExceptionThrown if exception is WebException.
        /// </summary>
        /// <returns>Null if ExceptionThrown is not WebException and server response otherwise.</returns>
        public System.Net.WebResponse GetServerResponse()
        {
            if (ExceptionThrown is System.Net.WebException)
            {
                System.Net.WebException ex = (System.Net.WebException)ExceptionThrown;
                System.Net.WebResponse resp = ex.Response;                
                return resp;
            }
            else return null;
        }
    }
}
