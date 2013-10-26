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
            var data = Encoding.UTF8.GetBytes(json);

            using (var stream = await Task.Factory.FromAsync<Stream>(request.BeginGetRequestStream, request.EndGetRequestStream, null))
            {
                await stream.WriteAsync(data, 0, data.Length);
            }

            var webres = await Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);
            try
            {
                var res = webres as HttpWebResponse;
                if (res.StatusCode == HttpStatusCode.OK)
                {
                    using (var reader = new StreamReader(res.GetResponseStream()))
                    {
                        var body = reader.ReadToEnd();
#if WINDOWS_PHONE
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
#else
                        await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
#endif
                            {
                                if (string.IsNullOrEmpty(body))
                                    OnError.Invoke();
                                else
                                    OnResponse.Invoke(body);
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
    }
}
