
namespace WPPMM.RemoteApi
{
    public class ApiParams
    {
        public const string ShootModeStill = "still";
        public const string ShootModeMovie = "movie";
        public const string ShootModeAudio = "audio";

        public const int SelfTimerOff = 0;
        public const int SelfTimer2 = 2;
        public const int SelfTimer10 = 10;

        public const string ZoomDirIn = "in";
        public const string ZoomDirOut = "out";

        public const string ZoomActStart = "start";
        public const string ZoomActStop = "stop";
        public const string ZoomAct1Shot = "1shot";

        public const string PostImgOriginal = "Original";
        public const string PostImg2M = "2M";

        public const string EventError = "Error";
        public const string EventNotReady = "NotReady";
        public const string EventIdle = "IDLE";
        public const string EventStCapturing = "StillCapturing";
        public const string EventStSaving = "StillSaving";
        public const string EventMvWaitRecStart = "MovieWaitRecStart";
        public const string EventMvRecording = "MovieRecording";
        public const string EventMvWaitRecStop = "MovieWaitRecStop";
        public const string EventMvSaving = "MovieSaving";
    }
}
