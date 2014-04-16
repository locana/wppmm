using System.Linq;
using WPPMM.RemoteApi;

namespace WPPMM.CameraManager
{
    class StatusComparator
    {
        internal static bool IsAvailableApisModified(CameraStatus status, string[] latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.AvailableApis;
            status.AvailableApis = latest;
            if (previous == null ||
                previous.Length != latest.Length)
            {
                return true;
            }
            foreach (var api in latest)
            {
                if (!previous.Contains(api))
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool IsCameraStatusModified(CameraStatus status, string latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.Status;
            status.Status = latest;

            return previous != latest;
        }

        internal static bool IsZoomInfoModified(CameraStatus status, ZoomInfo latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.ZoomInfo;
            status.ZoomInfo = latest;

            return previous == null ||
                previous.current_box_index != latest.current_box_index ||
                previous.number_of_boxes != latest.number_of_boxes ||
                previous.position != latest.position ||
                previous.position_in_current_box != latest.position_in_current_box;
        }

        internal static bool IsLiveviewAvailableModified(CameraStatus status, bool latest)
        {
            var previous = status.IsLiveviewAvailable;
            status.IsLiveviewAvailable = latest;

            return previous != latest;
        }

        internal static bool IsPostviewSizeInfoModified(CameraStatus status, BasicInfo<string> latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.PostviewSizeInfo;
            status.PostviewSizeInfo = latest;
            return IsModified(previous, latest);
        }

        internal static bool IsSelftimerInfoModified(CameraStatus status, BasicInfo<int> latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.SelfTimerInfo;
            status.SelfTimerInfo = latest;
            return IsModified(previous, latest);
        }

        internal static bool IsShootModeInfoModified(CameraStatus status, BasicInfo<string> latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.ShootModeInfo;
            status.ShootModeInfo = new ExtendedInfo<string>(latest);
            return IsModified(previous, latest);
        }

        internal static bool IsExposureModeInfoModified(CameraStatus status, BasicInfo<string> latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.ExposureMode;
            status.ExposureMode = latest;
            return IsModified(previous, latest);
        }

        internal static bool IsShutterSpeedModified(CameraStatus status, BasicInfo<string> latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.ShutterSpeed;
            status.ShutterSpeed = latest;
            return IsModified(previous, latest);
        }

        internal static bool IsISOModified(CameraStatus status, BasicInfo<string> latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.ISOSpeedRate;
            status.ISOSpeedRate = latest;
            return IsModified(previous, latest);
        }

        internal static bool IsFNumberModified(CameraStatus status, BasicInfo<string> latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.FNumber;
            status.FNumber = latest;
            return IsModified(previous, latest);
        }

        internal static bool IsEvInfoModified(CameraStatus status, EvInfo latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.EvInfo;
            status.EvInfo = latest;
            return previous == null ||
                previous.CurrentIndex != latest.CurrentIndex ||
                previous.Range.MaxIndex != latest.Range.MaxIndex ||
                previous.Range.MinIndex != latest.Range.MinIndex ||
                previous.Range.IndexStep != latest.Range.IndexStep;
        }

        internal static bool IsProgramShiftModified(CameraStatus status, bool? latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.ProgramShiftActivated;
            status.ProgramShiftActivated = (bool)latest;
            return previous != latest;
        }

        private static bool IsModified(BasicInfo<string> previous, BasicInfo<string> latest)
        {
            if (previous == null ||
                previous.current != latest.current ||
                previous.candidates.Length != latest.candidates.Length)
            {
                return true;
            }
            foreach (var candidate in latest.candidates)
            {
                if (!previous.candidates.Contains(candidate))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsModified(BasicInfo<int> previous, BasicInfo<int> latest)
        {
            if (previous == null ||
                previous.current != latest.current ||
                previous.candidates.Length != latest.candidates.Length)
            {
                return true;
            }
            foreach (var candidate in latest.candidates)
            {
                if (!previous.candidates.Contains(candidate))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
