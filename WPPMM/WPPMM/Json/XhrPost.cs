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
        public static async void Post(string endpoint, string json, Action<string> OnResponse, Action OnError)
        {
            if (endpoint == null || json == null || OnResponse == null || OnError == null)
            {
                throw new ArgumentNullException();
            }

            var request = HttpWebRequest.Create(new Uri(endpoint)) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/json";
            request.AllowReadStreamBuffering = true;
            request.AllowReadStreamBuffering = true;
            request.AllowAutoRedirect = false;

            var data = Encoding.UTF8.GetBytes(json);
            request.ContentLength = data.Length;

            var PostRequestHandler = new AsyncCallback((ar) =>
            {
                var req = ar.AsyncState as HttpWebRequest;
                try
                {
                    var res = req.EndGetResponse(ar) as HttpWebResponse;

                    if (res.StatusCode == HttpStatusCode.OK)
                    {
                        using (var reader = new StreamReader(res.GetResponseStream()))
                        {
                            var response = reader.ReadToEnd();
                            if (string.IsNullOrEmpty(response))
                            {
                                Debug.WriteLine("Result json is null or empty");
                                OnError.Invoke();
                            }
                            else
                            {
                                OnResponse.Invoke(response);
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("HTTP status code is not OK");
                        OnError.Invoke();
                    }
                }
                catch (WebException e)
                {
                    var res = e.Response as HttpWebResponse;
                    if (res != null)
                    {
                        Debug.WriteLine("Http Status Error: " + res.StatusCode);
                    }
                    else
                    {
                        Debug.WriteLine("WebException: " + e.Status);
                    }
                    OnError.Invoke();
                }
            });

            using (var stream = await Task.Factory.FromAsync<Stream>(request.BeginGetRequestStream, request.EndGetRequestStream, null))
            {
                await stream.WriteAsync(data, 0, data.Length);
            }

            request.BeginGetResponse(PostRequestHandler, request);
        }
    }
}
