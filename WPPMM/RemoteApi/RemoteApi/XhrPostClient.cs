using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
#if NETFX_CORE
using Windows.UI.Core;
#elif WINDOWS_PHONE
using System.Windows;
#endif

namespace WPPMM.RemoteApi
{
    internal class XhrPostClient
    {
        /// <summary>
        /// Asynchronously POST a request to the endpoint.
        /// Callback delegates are invoked on the UI thread.
        /// </summary>
        /// <param name="endpoint">URL of the endpoint.</param>
        /// <param name="body">Reqeust body.</param>
        /// <param name="OnResponse">Result json string callback.</param>
        /// <param name="OnError">Connection error callback.</param>
        internal static async void Post(string endpoint, string body, Action<string> OnResponse, Action OnError)
        {
            if (endpoint == null || body == null || OnResponse == null || OnError == null)
            {
                throw new ArgumentNullException();
            }

            var request = HttpWebRequest.Create(new Uri(endpoint)) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/json";
            //request.AllowReadStreamBuffering = true;
            Debug.WriteLine(body);
            var data = Encoding.UTF8.GetBytes(body);

            try
            {
                using (var stream = await Task.Factory.FromAsync<Stream>(request.BeginGetRequestStream, request.EndGetRequestStream, null))
                {
                    await stream.WriteAsync(data, 0, data.Length);
                }
                var webres = await Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);
                var res = webres as HttpWebResponse;
                if (res.StatusCode == HttpStatusCode.OK)
                {
                    using (var reader = new StreamReader(res.GetResponseStream()))
                    {
                        var resbody = reader.ReadToEnd();
#if WINDOWS_PHONE
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
#else
                        await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
#endif
                            {
                                if (string.IsNullOrEmpty(resbody))
                                    OnError.Invoke();
                                else
                                    OnResponse.Invoke(resbody);
                            });
                        return;
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

#if WINDOWS_PHONE
            Deployment.Current.Dispatcher.BeginInvoke(() =>
#else
            await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
#endif
                {
                    OnError.Invoke();
                });
        }

        /// <summary>
        /// Asynchronously POST a request to the endpoint.
        /// Response will be returned asynchronously.
        /// </summary>
        /// <param name="endpoint">URL of the endpoint.</param>
        /// <param name="body">Reqeust body.</param>
        /// <returns></returns>
        internal static Task<string> Post(string endpoint, string body)
        {
            if (endpoint == null || body == null)
            {
                throw new ArgumentNullException();
            }

            var tcs = new TaskCompletionSource<string>();

            var request = HttpWebRequest.Create(new Uri(endpoint)) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/json";
            Debug.WriteLine(body);
            var data = Encoding.UTF8.GetBytes(body);

            var ResponseHandler = new AsyncCallback((res) =>
            {
                try
                {
                    var result = res as HttpWebResponse;
                    if (result.StatusCode == HttpStatusCode.OK)
                        using (var reader = new StreamReader(result.GetResponseStream()))
                        {
                            var resbody = reader.ReadToEnd();
                            if (string.IsNullOrEmpty(resbody))
                                tcs.TrySetException(new WebException("null or empty result"));
                            else
                                tcs.TrySetResult(body);
                        }
                    else
                        tcs.TrySetException(new WebException("HTTP Status code is not OK"));
                }
                catch (WebException e)
                {
                    var result = e.Response as HttpWebResponse;
                    if (result != null)
                        Debug.WriteLine("Http Status Error: " + result.StatusCode);
                    else
                        Debug.WriteLine("WebException: " + e.Status);
                    tcs.TrySetException(e);
                };
            });

            request.BeginGetRequestStream((res) =>
            {
                using (var stream = request.EndGetRequestStream(res))
                {
                    stream.Write(data, 0, data.Length);
                }
                request.BeginGetResponse(ResponseHandler, null);
            }, null);

            return tcs.Task;
        }
    }
}
