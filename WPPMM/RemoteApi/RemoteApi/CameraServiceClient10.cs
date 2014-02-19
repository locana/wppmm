using System;

namespace WPPMM.RemoteApi
{
    /// <summary>
    /// Client of camera service for API version 1.0
    /// </summary>
    public class CameraServiceClient10
    {
        private readonly string endpoint;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endpoint"> Endpoint URL of camera service.</param>
        public CameraServiceClient10(string endpoint)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException();
            }
            this.endpoint = endpoint;
        }

        public void SetShootMode(string mode, Action<int> error, Action result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.setShootMode(mode),
                (res) => { BasicResultHandler.HandleNoValue(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void GetShootMode(Action<int> error, Action<string> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getShootMode(),
                (res) => { BasicResultHandler.HandleSingleValue<string>(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void GetAvailableShootMode(Action<int> error, Action<BasicInfo<string>> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getAvailableShootMode(),
                (res) => { BasicResultHandler.HandleBasicInfo<string>(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void GetSupportedShootMode(Action<int> error, Action<string[]> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getSupportedShootMode(),
                (res) => { BasicResultHandler.HandleArray<string>(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void ActTakePicture(Action<int> error, Action<string[]> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.actTakePicture(),
                (res) => { BasicResultHandler.HandleArray<string>(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void AwaitTakePicture(Action<int> error, Action<string[]> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.awaitTakePicture(),
                (res) => { BasicResultHandler.HandleArray<string>(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void StartMovieRec(Action<int> error, Action result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.startMovieRec(),
                (res) => { BasicResultHandler.HandleNoValue(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void StopMovieRec(Action<int> error, Action<string> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.stopMovieRec(),
                (res) => { BasicResultHandler.HandleSingleValue<string>(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void StartAudioRec(Action<int> error, Action result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.startAudioRec(),
                (res) => { BasicResultHandler.HandleNoValue(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void StopAudioRec(Action<int> error, Action result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.stopAudioRec(),
                (res) => { BasicResultHandler.HandleNoValue(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void StartLiveview(Action<int> error, Action<string> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.startLiveview(),
                (res) => { BasicResultHandler.HandleSingleValue<string>(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void StopLiveview(Action<int> error, Action result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.stopLiveview(),
                (res) => { BasicResultHandler.HandleNoValue(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void ActZoom(string direction, string movement, Action<int> error, Action result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.actZoom(direction, movement),
                (res) => { BasicResultHandler.HandleNoValue(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void SetSelfTimer(int second, Action<int> error, Action result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.setSelfTimer(second),
                (res) => { BasicResultHandler.HandleNoValue(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void GetSelfTimer(Action<int> error, Action<int> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getSelfTimer(),
                (res) => { BasicResultHandler.HandleSingleValue<int>(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void GetSupportedSelfTimer(Action<int> error, Action<int[]> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getSupportedSelfTimer(),
                (res) => { BasicResultHandler.HandleArray<int>(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void GetAvailableSelfTimer(Action<int> error, Action<BasicInfo<int>> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getAvailableSelfTimer(),
                (res) => { BasicResultHandler.HandleBasicInfo<int>(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void SetPostviewImageSize(string size, Action<int> error, Action result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.setPostviewImageSize(size),
                (res) => { BasicResultHandler.HandleNoValue(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void GetPostviewImageSize(Action<int> error, Action<string> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getPostviewImageSize(),
                (res) => { BasicResultHandler.HandleSingleValue<string>(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void GetSupportedPostviewImageSize(Action<int> error, Action<string[]> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getSupportedPostviewImageSize(),
                (res) => { BasicResultHandler.HandleArray<string>(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void GetAvailablePostviewImageSize(Action<int> error, Action<BasicInfo<string>> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getAvailablePostviewImageSize(),
                (res) => { BasicResultHandler.HandleBasicInfo<string>(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void StartRecMode(Action<int> error, Action result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.startRecMode(),
                (res) => { BasicResultHandler.HandleNoValue(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void StopRecMode(Action<int> error, Action result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.stopRecMode(),
                (res) => { BasicResultHandler.HandleNoValue(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void GetAvailableApiList(Action<int> error, Action<string[]> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getAvailableApiList(),
                (res) => { BasicResultHandler.HandleArray<string>(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void GetApplicationInfo(Action<int> error, Action<ApplicationInfo> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getApplicationInfo(),
                (res) => { ResultHandler.HandleGetApplicationInfo(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void GetVersions(Action<int> error, Action<string[]> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getVersions(),
                (res) => { BasicResultHandler.HandleArray<string>(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        public void GetMethodTypes(string version, Action<int> error, Action<MethodType[]> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getMethodTypes(version),
                (res) => { ResultHandler.HandleGetMethodTypes(res, error, result); },
                (code) => { error.Invoke(code); });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="longpolling">Set true for event notification, false for immediate response.</param>
        /// <param name="error"></param>
        /// <param name="result"></param>
        public void GetEvent(bool longpolling, Action<int> error, Action<Event> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getEvent(longpolling),
                (res) => { ResultHandler.HandleGetEvent(res, error, result); },
                (code) => { error.Invoke(code); });
        }
    }
}
