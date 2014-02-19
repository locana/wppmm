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
        /// <param name="endpoint">Endpoint URL of camera service.</param>
        public CameraApiClient(string endpoint)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException();
            }
            this.endpoint = endpoint;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="longpolling">Set true for event notification, false for immediate response.</param>
        /// <returns></returns>
        public async Task<Event> GetEventAsync(bool longpolling)
        {
            Event result = null;
            ResultHandler.HandleGetEvent(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getEvent(longpolling)),
                code => { throw new RemoteApiException(code); },
                @event => result = @event);
            return result;
        }

        public async Task<MethodType[]> GetMethodTypesAsync(string version)
        {
            MethodType[] result = null;
            ResultHandler.HandleGetMethodTypes(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getMethodTypes(version)),
                code => { throw new RemoteApiException(code); },
                types => result = types);
            return result;
        }

        public async Task<string[]> GetVersionsAsync()
        {
            string[] result = null;
            BasicResultHandler.HandleArray<string>(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getVersions()),
                code => { throw new RemoteApiException(code); },
                versions => result = versions);
            return result;
        }

        public async Task<ApplicationInfo> GetApplicationInfoAsync()
        {
            ApplicationInfo result = null;
            ResultHandler.HandleGetApplicationInfo(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getApplicationInfo()),
                code => { throw new RemoteApiException(code); },
                info => result = info);
            return result;
        }

        public async Task<string[]> GetAvailableApiListAsync()
        {
            string[] result = null;
            BasicResultHandler.HandleArray<string>(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getAvailableApiList()),
                code => { throw new RemoteApiException(code); },
                list => result = list);
            return result;
        }

        public async Task StartRecModeAsync()
        {
            BasicResultHandler.HandleNoValue(
                await AsyncPostClient.Post(endpoint, RequestGenerator.startRecMode()),
                code => { throw new RemoteApiException(code); },
                () => { });
        }

        public async Task StopRecModeAsync()
        {
            BasicResultHandler.HandleNoValue(
                await AsyncPostClient.Post(endpoint, RequestGenerator.stopRecMode()),
                code => { throw new RemoteApiException(code); },
                () => { });
        }

        public async Task ActZoomAsync(string direction, string movement)
        {
            BasicResultHandler.HandleNoValue(
                await AsyncPostClient.Post(endpoint, RequestGenerator.actZoom(direction, movement)),
                code => { throw new RemoteApiException(code); },
                () => { });
        }

        public async Task<string> StartLiveviewAsync()
        {
            string result = null;
            BasicResultHandler.HandleSingleValue<string>(
                await AsyncPostClient.Post(endpoint, RequestGenerator.startLiveview()),
                code => { throw new RemoteApiException(code); },
                url => result = url);
            return result;
        }

        public async Task StopLiveviewAsync()
        {
            BasicResultHandler.HandleNoValue(
                await AsyncPostClient.Post(endpoint, RequestGenerator.stopLiveview()),
                code => { throw new RemoteApiException(code); },
                () => { });
        }

        public async Task StartAudioRecAsync()
        {
            BasicResultHandler.HandleNoValue(
                await AsyncPostClient.Post(endpoint, RequestGenerator.startAudioRec()),
                code => { throw new RemoteApiException(code); },
                () => { });
        }

        public async Task StopAudioRecAsync()
        {
            BasicResultHandler.HandleNoValue(
                await AsyncPostClient.Post(endpoint, RequestGenerator.stopAudioRec()),
                code => { throw new RemoteApiException(code); },
                () => { });
        }

        public async Task StartMovieRecAsync()
        {
            BasicResultHandler.HandleNoValue(
                await AsyncPostClient.Post(endpoint, RequestGenerator.startMovieRec()),
                code => { throw new RemoteApiException(code); },
                () => { });
        }

        public async Task<string> StopMovieRecAsync()
        {
            string result = null;
            BasicResultHandler.HandleSingleValue<string>(
                await AsyncPostClient.Post(endpoint, RequestGenerator.stopMovieRec()),
                code => { throw new RemoteApiException(code); },
                url => result = url);
            return result;
        }

        public async Task<string[]> ActTakePictureAsync()
        {
            string[] result = null;
            BasicResultHandler.HandleArray<string>(
                await AsyncPostClient.Post(endpoint, RequestGenerator.actTakePicture()),
                code => { throw new RemoteApiException(code); },
                url => result = url);
            return result;
        }

        public async Task<string[]> AwaitTakePictureAsync()
        {
            string[] result = null;
            BasicResultHandler.HandleArray<string>(
                await AsyncPostClient.Post(endpoint, RequestGenerator.awaitTakePicture()),
                code => { throw new RemoteApiException(code); },
                url => result = url);
            return result;
        }

        public async Task SetSelfTimerAsync(int timer)
        {
            BasicResultHandler.HandleNoValue(
                await AsyncPostClient.Post(endpoint, RequestGenerator.setSelfTimer(timer)),
                code => { throw new RemoteApiException(code); },
                () => { });
        }

        public async Task<int> GetSelfTimerAsync()
        {
            int result = 0;
            BasicResultHandler.HandleSingleValue<int>(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getSelfTimer()),
                code => { throw new RemoteApiException(code); },
                timer => result = timer);
            return result;
        }

        public async Task<int[]> GetSupportedSelfTimerAsync()
        {
            int[] result = null;
            BasicResultHandler.HandleArray<int>(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getSupportedSelfTimer()),
                code => { throw new RemoteApiException(code); },
                timer => result = timer);
            return result;
        }

        public async Task<BasicInfo<int>> GetAvailableSelfTimerAsync()
        {
            BasicInfo<int> result = null;
            BasicResultHandler.HandleBasicInfo<int>(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getAvailableSelfTimer()),
                code => { throw new RemoteApiException(code); },
                info => result = info);
            return result;
        }

        public async Task SetPostviewImageSizeAsync(string size)
        {
            BasicResultHandler.HandleNoValue(
                await AsyncPostClient.Post(endpoint, RequestGenerator.setPostviewImageSize(size)),
                code => { throw new RemoteApiException(code); },
                () => { });
        }

        public async Task<string> GetPostviewImageSizeAsync()
        {
            string result = null;
            BasicResultHandler.HandleSingleValue<string>(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getPostviewImageSize()),
                code => { throw new RemoteApiException(code); },
                size => result = size);
            return result;
        }

        public async Task<string[]> GetSupportedPostviewImageSizeAsync()
        {
            string[] result = null;
            BasicResultHandler.HandleArray<string>(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getSupportedPostviewImageSize()),
                code => { throw new RemoteApiException(code); },
                size => result = size);
            return result;
        }

        public async Task<BasicInfo<string>> GetAvailablePostviewImageSizeAsync()
        {
            BasicInfo<string> result = null;
            BasicResultHandler.HandleBasicInfo<string>(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getAvailablePostviewImageSize()),
                code => { throw new RemoteApiException(code); },
                info => result = info);
            return result;
        }

        public async Task SetShootModeAsync(string mode)
        {
            BasicResultHandler.HandleNoValue(
                await AsyncPostClient.Post(endpoint, RequestGenerator.setShootMode(mode)),
                code => { throw new RemoteApiException(code); },
                () => { });
        }

        public async Task<string> GetShootModeAsync()
        {
            string result = null;
            BasicResultHandler.HandleSingleValue<string>(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getShootMode()),
                code => { throw new RemoteApiException(code); },
                mode => result = mode);
            return result;
        }

        public async Task<string[]> GetSupportedShootModeAsync()
        {
            string[] result = null;
            BasicResultHandler.HandleArray<string>(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getSupportedShootMode()),
                code => { throw new RemoteApiException(code); },
                mode => result = mode);
            return result;
        }

        public async Task<BasicInfo<string>> GetAvailableShootModeAsync()
        {
            BasicInfo<string> result = null;
            BasicResultHandler.HandleBasicInfo<string>(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getAvailableShootMode()),
                code => { throw new RemoteApiException(code); },
                info => result = info);
            return result;
        }
    }
}
