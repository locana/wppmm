using System;
using System.Threading.Tasks;

namespace WPPMM.RemoteApi
{
    public class CameraApiClient
    {
        private readonly string endpoint;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endpoint"> Endpoint URL of camera service.</param>
        public CameraApiClient(string endpoint)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException();
            }
            this.endpoint = endpoint;
        }

        public Task<Event> GetEvent(Boolean longpolling)
        {
            Task<string> result = AsyncPostClient.Post(endpoint, RequestGenerator.getEvent(longpolling));

            var tcs = new TaskCompletionSource<Event>();
            ResultHandler.GetEvent(result.Result,
                (code) => { tcs.TrySetException(new RemoteApiException(code)); },
                (@event) => { tcs.TrySetResult(@event); });
            return tcs.Task;
        }
    }
}
