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

        internal static bool IsPostviewSizeInfoModified(CameraStatus status, Capability<string> latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.PostviewSizeInfo;
            status.PostviewSizeInfo = latest;
            return IsModified(previous, latest);
        }

        internal static bool IsSelftimerInfoModified(CameraStatus status, Capability<int> latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.SelfTimerInfo;
            status.SelfTimerInfo = latest;
            return IsModified(previous, latest);
        }

        internal static bool IsShootModeInfoModified(CameraStatus status, Capability<string> latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.ShootModeInfo;
            status.ShootModeInfo = new ExtendedInfo<string>(latest);
            return IsModified(previous, latest);
        }

        internal static bool IsExposureModeInfoModified(CameraStatus status, Capability<string> latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.ExposureMode;
            status.ExposureMode = latest;
            return IsModified(previous, latest);
        }

        internal static bool IsShutterSpeedModified(CameraStatus status, Capability<string> latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.ShutterSpeed;
            status.ShutterSpeed = latest;
            return IsModified(previous, latest);
        }

        internal static bool IsISOModified(CameraStatus status, Capability<string> latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.ISOSpeedRate;
            status.ISOSpeedRate = latest;
            return IsModified(previous, latest);
        }

        internal static bool IsFNumberModified(CameraStatus status, Capability<string> latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.FNumber;
            status.FNumber = latest;
            return IsModified(previous, latest);
        }

        internal static bool IsEvInfoModified(CameraStatus status, EvCapability latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.EvInfo;
            status.EvInfo = latest;
            return previous == null ||
                previous.CurrentIndex != latest.CurrentIndex ||
                previous.Candidate.MaxIndex != latest.Candidate.MaxIndex ||
                previous.Candidate.MinIndex != latest.Candidate.MinIndex ||
                previous.Candidate.IndexStep != latest.Candidate.IndexStep;
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

        private static bool IsModified(Capability<string> previous, Capability<string> latest)
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

        private static bool IsModified(Capability<int> previous, Capability<int> latest)
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
