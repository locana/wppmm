
namespace WPPMM.RemoteApi
{
    /// <summary>
    /// Status code definition.
    /// </summary>
    public class StatusCode
    {
        public const int OK = 0;

        public const int Any = 1;
        public const int Timeout = 2;
        public const int IllegalArgument = 3;
        public const int IllegalDataFormat = 4;
        public const int IllegalRequest = 5;
        public const int IllegalResponse = 6;
        public const int IllegalState = 7;
        public const int IllegalType = 8;
        public const int IndexOutOfBounds = 9;
        public const int NoSuchElement = 10;
        public const int NoSuchField = 11;
        public const int NoSuchMethod = 12;
        public const int NullPointer = 13;
        public const int UnsupportedVersion = 14;
        public const int UnsupportedOperation = 15;

        public const int Unauthrorized = 401;
        public const int Forbidden = 403;
        public const int NotFound = 404;
        public const int NotAcceptable = 406;
        public const int RequestEntityTooLarge = 413;
        public const int RequestUriTooLong = 414;
        public const int NotImplemented = 501;
        public const int ServiceUnavailable = 503;

        public const int ShootingFailure = 40400;
        public const int CameraNotReady = 40401;
        public const int DuplicatePolling = 40402;
        public const int StillCapturingNotFinished = 40403;
    }
}
