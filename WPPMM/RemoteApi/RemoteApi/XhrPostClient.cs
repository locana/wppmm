using Microsoft.Phone.Reactive;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WPPMM.RemoteApi
{
    internal class XhrPostClient
    {
        /// <summary>
        /// Asynchronously POST a request to the endpoint.
        /// Callback delegates are invoked on the UI thread.
        /// </summary>
        /// <param name="endpoint">URL of the endpoint.</param>
        /// <param name="json">Reqeust body.</param>
        /// <param name="OnResponse">Result json string callback.</param>
        /// <param name="OnError">Connection error callback.</param>
        internal static async void Post(string endpoint, string json, Action<string> OnResponse, Action OnError)
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

            using (var stream = await Task.Factory.FromAsync<Stream>(request.BeginGetRequestStream, request.EndGetRequestStream, null))
            {
                await stream.WriteAsync(data, 0, data.Length);
            }

            Observable.FromAsyncPattern<WebResponse>(request.BeginGetResponse, request.EndGetResponse)()
                .Select(webres =>
                {
                    try
                    {
                        var res = webres as HttpWebResponse;
                        if (res.StatusCode == HttpStatusCode.OK)
                        {
                            using (var reader = new StreamReader(res.GetResponseStream()))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                        else
                        {
                            Debug.WriteLine("HTTP status code is not OK");
                        }
                    }
                    catch (WebException e)
                    {
                        var res = e.Response as HttpWebResponse;
                        if (res != null)
                            Debug.WriteLine("Http Status Error: " + res.StatusCode);
                        else
                            Debug.WriteLine("WebException: " + e.Status);
                    }
                    return null;
                })
                .ObserveOnDispatcher()
                .Subscribe(res =>
                {
                    if (string.IsNullOrEmpty(res))
                        OnError.Invoke();
                    else
                        OnResponse.Invoke(res);
                },
                err =>
                {
                    OnError.Invoke();
                });
        }
    }
}
