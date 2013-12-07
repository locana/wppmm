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
                (res) => { ResultHandler.SetShootMode(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void GetShootMode(Action<int> error, Action<string> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getShootMode(),
                (res) => { ResultHandler.GetShootMode(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void GetAvailableShootMode(Action<int> error, Action<string, string[]> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getAvailableShootMode(),
                (res) => { ResultHandler.GetAvailableShootMode(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void GetSupportedShootMode(Action<int> error, Action<string[]> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getSupportedShootMode(),
                (res) => { ResultHandler.GetSupportedShootMode(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void ActTakePicture(Action<int> error, Action<string[]> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.actTakePicture(),
                (res) => { ResultHandler.ActTakePicture(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void AwaitTakePicture(Action<int> error, Action<string[]> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.awaitTakePicture(),
                (res) => { ResultHandler.AwaitTakePicture(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void StartMovieRec(Action<int> error, Action result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.startMovieRec(),
                (res) => { ResultHandler.StartMovieRec(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void StopMovieRec(Action<int> error, Action<string> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.stopMovieRec(),
                (res) => { ResultHandler.StopMovieRec(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void StartAudioRec(Action<int> error, Action result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.startAudioRec(),
                (res) => { ResultHandler.StartAudioRec(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void StopAudioRec(Action<int> error, Action result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.stopAudioRec(),
                (res) => { ResultHandler.StopAudioRec(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void StartLiveview(Action<int> error, Action<string> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.startLiveview(),
                (res) => { ResultHandler.StartLiveview(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void StopLiveview(Action<int> error, Action result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.stopLiveview(),
                (res) => { ResultHandler.StopLiveview(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void ActZoom(string direction, string movement, Action<int> error, Action result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.actZoom(direction, movement),
                (res) => { ResultHandler.ActZoom(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void SetSelfTimer(int second, Action<int> error, Action result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.setSelfTimer(second),
                (res) => { ResultHandler.SetSelfTimer(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void GetSelfTimer(Action<int> error, Action<int> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getSelfTimer(),
                (res) => { ResultHandler.GetSelfTimer(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void GetSupportedSelfTimer(Action<int> error, Action<int[]> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getSupportedSelfTimer(),
                (res) => { ResultHandler.GetSupportedSelfTimer(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void GetAvailableSelfTimer(Action<int> error, Action<int, int[]> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getAvailableSelfTimer(),
                (res) => { ResultHandler.GetAvailableSelfTimer(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void SetPostviewImageSize(string size, Action<int> error, Action result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.setPostviewImageSize(size),
                (res) => { ResultHandler.SetPostviewImageSize(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void GetPostviewImageSize(Action<int> error, Action<string> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getPostviewImageSize(),
                (res) => { ResultHandler.GetPostviewImageSize(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void GetSupportedPostviewImageSize(Action<int> error, Action<string[]> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getSupportedPostviewImageSize(),
                (res) => { ResultHandler.GetSupportedPostviewImageSize(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void GetAvailablePostviewImageSize(Action<int> error, Action<string, string[]> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getAvailablePostviewImageSize(),
                (res) => { ResultHandler.GetAvailablePostviewImageSize(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void StartRecMode(Action<int> error, Action result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.startRecMode(),
                (res) => { ResultHandler.StartRecMode(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void StopRecMode(Action<int> error, Action result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.stopRecMode(),
                (res) => { ResultHandler.StopRecMode(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void GetAvailableApiList(Action<int> error, Action<string[]> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getAvailableApiList(),
                (res) => { ResultHandler.GetAvailableApiList(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void GetApplicationInfo(Action<int> error, Action<string, string> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getApplicationInfo(),
                (res) => { ResultHandler.GetApplicationInfo(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void GetVersions(Action<int> error, Action<string[]> result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getVersions(),
                (res) => { ResultHandler.GetVersions(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void GetMethodTypes(string version, Action<int> error, MethodTypesHandler result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getMethodTypes(version),
                (res) => { ResultHandler.GetMethodTypes(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="longpolling">Set true for event notification, false for immediate response.</param>
        /// <param name="error"></param>
        /// <param name="result"></param>
        public void GetEvent(bool longpolling, Action<int> error, GetEventHandler result)
        {
            AsyncPostClient.Post(endpoint, RequestGenerator.getEvent(longpolling),
                (res) => { ResultHandler.GetEvent(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }
    }
}
