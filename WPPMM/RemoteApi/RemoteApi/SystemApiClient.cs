using System;
using System.Threading.Tasks;

namespace WPPMM.RemoteApi
{
    public class SystemApiClient
    {
        private readonly string endpoint;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endpoint">Endpoint URL of system service.</param>
        public SystemApiClient(string endpoint)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException();
            }
            this.endpoint = endpoint;
        }

        public async Task<DateTimeOffset> GetCurrentTime()
        {
            DateTimeOffset result;
            ResultHandler.HandleGetCurrentTime(
                await AsyncPostClient.Post(endpoint, RequestGenerator.Jsonize("getCurrentTime")),
                code => { throw new RemoteApiException(code); },
                info => result = info);
            return result;
        }

        public async Task<MethodType[]> GetMethodTypesAsync(string version)
        {
            MethodType[] result = null;
            ResultHandler.HandleGetMethodTypes(
                await AsyncPostClient.Post(endpoint, RequestGenerator.Jsonize("getMethodTypes", version)),
                code => { throw new RemoteApiException(code); },
                types => result = types);
            return result;
        }

        public async Task<string[]> GetVersionsAsync()
        {
            string[] result = null;
            BasicResultHandler.HandleArray<string>(
                await AsyncPostClient.Post(endpoint, RequestGenerator.Jsonize("getVersions")),
                code => { throw new RemoteApiException(code); },
                versions => result = versions);
            return result;
        }
    }
}
