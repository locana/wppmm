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
            ResultHandler.GetEvent(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getEvent(longpolling)),
                code => { throw new RemoteApiException(code); },
                @event => result = @event);
            return result;
        }

        public async Task<MethodType[]> GetMethodTypesAsync(string version)
        {
            MethodType[] result = null;
            ResultHandler.GetMethodTypes(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getMethodTypes(version)),
                code => { throw new RemoteApiException(code); },
                types => result = types);
            return result;
        }

        public async Task<string[]> GetVersionsAsync()
        {
            string[] result = null;
            ResultHandler.GetVersions(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getVersions()),
                code => { throw new RemoteApiException(code); },
                versions => result = versions);
            return result;
        }

        public async Task<ApplicationInfo> GetApplicationInfoAsync()
        {
            ApplicationInfo result = null;
            ResultHandler.GetVersions(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getApplicationInfo()),
                code => { throw new RemoteApiException(code); },
                values => result = new ApplicationInfo { name = values[0], version = values[1] });
            return result;
        }

        public async Task<string[]> GetAvailableApiListAsync()
        {
            string[] result = null;
            ResultHandler.GetAvailableApiList(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getAvailableApiList()),
                code => { throw new RemoteApiException(code); },
                list => result = list);
            return result;
        }

        public async Task StartRecModeAsync()
        {
            ResultHandler.StartRecMode(
                await AsyncPostClient.Post(endpoint, RequestGenerator.startRecMode()),
                code => { throw new RemoteApiException(code); },
                () => { });
        }

        public async Task StopRecModeAsync()
        {
            ResultHandler.StopRecMode(
                await AsyncPostClient.Post(endpoint, RequestGenerator.stopRecMode()),
                code => { throw new RemoteApiException(code); },
                () => { });
        }

        public async Task ActZoomAsync(string direction, string movement)
        {
            ResultHandler.ActZoom(
                await AsyncPostClient.Post(endpoint, RequestGenerator.actZoom(direction, movement)),
                code => { throw new RemoteApiException(code); },
                () => { });
        }

        public async Task<string> StartLiveviewAsync()
        {
            string result = null;
            ResultHandler.StartLiveview(
                await AsyncPostClient.Post(endpoint, RequestGenerator.startLiveview()),
                code => { throw new RemoteApiException(code); },
                url => result = url);
            return result;
        }

        public async Task StopLiveviewAsync()
        {
            ResultHandler.StopLiveview(
                await AsyncPostClient.Post(endpoint, RequestGenerator.stopLiveview()),
                code => { throw new RemoteApiException(code); },
                () => { });
        }

        public async Task StartAudioRecAsync()
        {
            ResultHandler.StartAudioRec(
                await AsyncPostClient.Post(endpoint, RequestGenerator.startAudioRec()),
                code => { throw new RemoteApiException(code); },
                () => { });
        }

        public async Task StopAudioRecAsync()
        {
            ResultHandler.StopAudioRec(
                await AsyncPostClient.Post(endpoint, RequestGenerator.stopAudioRec()),
                code => { throw new RemoteApiException(code); },
                () => { });
        }

        public async Task StartMovieRecAsync()
        {
            ResultHandler.StartMovieRec(
                await AsyncPostClient.Post(endpoint, RequestGenerator.startMovieRec()),
                code => { throw new RemoteApiException(code); },
                () => { });
        }

        public async Task<string> StopMovieRecAsync()
        {
            string result = null;
            ResultHandler.StopMovieRec(
                await AsyncPostClient.Post(endpoint, RequestGenerator.stopMovieRec()),
                code => { throw new RemoteApiException(code); },
                url => result = url);
            return result;
        }

        public async Task<string[]> ActTakePictureAsync()
        {
            string[] result = null;
            ResultHandler.ActTakePicture(
                await AsyncPostClient.Post(endpoint, RequestGenerator.actTakePicture()),
                code => { throw new RemoteApiException(code); },
                url => result = url);
            return result;
        }

        public async Task<string[]> AwaitTakePictureAsync()
        {
            string[] result = null;
            ResultHandler.AwaitTakePicture(
                await AsyncPostClient.Post(endpoint, RequestGenerator.awaitTakePicture()),
                code => { throw new RemoteApiException(code); },
                url => result = url);
            return result;
        }

        public async Task SetSelfTimerAsync(int timer)
        {
            ResultHandler.SetSelfTimer(
                await AsyncPostClient.Post(endpoint, RequestGenerator.setSelfTimer(timer)),
                code => { throw new RemoteApiException(code); },
                () => { });
        }

        public async Task<int> GetSelfTimerAsync()
        {
            int result = 0;
            ResultHandler.GetSelfTimer(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getSelfTimer()),
                code => { throw new RemoteApiException(code); },
                timer => result = timer);
            return result;
        }

        public async Task<int[]> GetSupportedSelfTimerAsync()
        {
            int[] result = null;
            ResultHandler.GetSupportedSelfTimer(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getSupportedSelfTimer()),
                code => { throw new RemoteApiException(code); },
                timer => result = timer);
            return result;
        }

        public async Task<BasicInfo<int>> GetAvailableSelfTimerAsync()
        {
            BasicInfo<int> result = null;
            ResultHandler.GetAvailableSelfTimer(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getAvailableSelfTimer()),
                code => { throw new RemoteApiException(code); },
                info => result = info);
            return result;
        }

        public async Task SetPostviewImageSizeAsync(string size)
        {
            ResultHandler.SetPostviewImageSize(
                await AsyncPostClient.Post(endpoint, RequestGenerator.setPostviewImageSize(size)),
                code => { throw new RemoteApiException(code); },
                () => { });
        }

        public async Task<string> GetPostviewImageSizeAsync()
        {
            string result = null;
            ResultHandler.GetPostviewImageSize(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getPostviewImageSize()),
                code => { throw new RemoteApiException(code); },
                size => result = size);
            return result;
        }

        public async Task<string[]> GetSupportedPostviewImageSizeAsync()
        {
            string[] result = null;
            ResultHandler.GetSupportedPostviewImageSize(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getSupportedPostviewImageSize()),
                code => { throw new RemoteApiException(code); },
                size => result = size);
            return result;
        }

        public async Task<BasicInfo<string>> GetAvailablePostviewImageSizeAsync()
        {
            BasicInfo<string> result = null;
            ResultHandler.GetAvailablePostviewImageSize(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getAvailablePostviewImageSize()),
                code => { throw new RemoteApiException(code); },
                info => result = info);
            return result;
        }

        public async Task SetShootModeAsync(string mode)
        {
            ResultHandler.SetShootMode(
                await AsyncPostClient.Post(endpoint, RequestGenerator.setShootMode(mode)),
                code => { throw new RemoteApiException(code); },
                () => { });
        }

        public async Task<string> GetShootModeAsync()
        {
            string result = null;
            ResultHandler.GetShootMode(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getShootMode()),
                code => { throw new RemoteApiException(code); },
                mode => result = mode);
            return result;
        }

        public async Task<string[]> GetSupportedShootModeAsync()
        {
            string[] result = null;
            ResultHandler.GetSupportedShootMode(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getSupportedShootMode()),
                code => { throw new RemoteApiException(code); },
                mode => result = mode);
            return result;
        }

        public async Task<BasicInfo<string>> GetAvailableShootModeAsync()
        {
            BasicInfo<string> result = null;
            ResultHandler.GetAvailableShootMode(
                await AsyncPostClient.Post(endpoint, RequestGenerator.getAvailableShootMode()),
                code => { throw new RemoteApiException(code); },
                info => result = info);
            return result;
        }
    }
}
