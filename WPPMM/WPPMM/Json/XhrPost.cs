using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WPPMM.Json
{
    public class XhrPost
    {
        /// <summary>
        /// Asynchronously POST a request to the endpoint.
        /// </summary>
        /// <param name="endpoint">URL of the endpoint.</param>
        /// <param name="json">Reqeust body.</param>
        /// <param name="OnResponse">Result json string callback.</param>
        /// <param name="OnError">Connection error callback.</param>
        public static void Post(string endpoint, string json, Action<string> OnResponse, Action OnError)
        {
            if (endpoint == null || json == null || OnResponse == null || OnError == null)
            {
                throw new ArgumentNullException();
            }

            var request = HttpWebRequest.Create(new Uri(endpoint)) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/json";

            var data = Encoding.UTF8.GetBytes(json);
            request.ContentLength = data.Length;

            var PostRequestHandler = new AsyncCallback((ar) =>
            {
                try
                {
                    var req = ar.AsyncState as HttpWebRequest;
                    using (var res = req.EndGetResponse(ar) as HttpWebResponse)
                    {
                        if (res.StatusCode == HttpStatusCode.OK)
                        {
                            using (var reader = new StreamReader(res.GetResponseStream()))
                            {
                                var response = reader.ReadToEnd();
                                if (string.IsNullOrEmpty(response))
                                {
                                    OnError.Invoke();
                                }
                                else
                                {
                                    OnResponse.Invoke(response);
                                }
                            }
                        }
                    }
                }
                catch (WebException e)
                {
                    Debug.WriteLine("WebException: " + e.Status);
                    OnError.Invoke();
                }
            });

            var RequestStreamHandler = new AsyncCallback((ar) =>
            {
                var stream = request.EndGetRequestStream(ar) as Stream;
                stream.Write(data, 0, data.Length);
                stream.Close();
                request.BeginGetResponse(PostRequestHandler, request);
            });

            request.BeginGetRequestStream(RequestStreamHandler, request);
        }
    }
}
