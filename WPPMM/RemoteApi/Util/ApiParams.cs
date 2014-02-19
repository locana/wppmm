
namespace WPPMM.RemoteApi
{
    public class ShootModeParam
    {
        public const string Still = "still";
        public const string Movie = "movie";
        public const string Audio = "audio";
    }

    public class ZoomParam
    {
        public const string DirectionIn = "in";
        public const string DirectionOut = "out";
        public const string ActionStart = "start";
        public const string ActionStop = "stop";
        public const string Action1Shot = "1shot";
    }

    public class PostviewSizeParam
    {
        public const string Original = "Original";
        public const string Px2M = "2M";
    }

    public class SelfTimerParam
    {
        public const int Off = 0;
        public const int TwoSec = 2;
        public const int TenSec = 10;
    }

    public class EventParam
    {
        public const string Error = "Error";
        public const string NotReady = "NotReady";
        public const string Idle = "IDLE";
        public const string StCapturing = "StillCapturing";
        public const string StSaving = "StillSaving";
        public const string MvWaitRecStart = "MovieWaitRecStart";
        public const string MvRecording = "MovieRecording";
        public const string MvWaitRecStop = "MovieWaitRecStop";
        public const string MvSaving = "MovieSaving";
        public const string AuWaitRecStart = "AudioWaitRecStart";
        public const string AuRecording = "AudioRecording";
        public const string AuWaitRecStop = "AudioWaitRecStop";
        public const string AuSaving = "AudioSaving";
    }
}
