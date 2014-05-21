using Kazyx.RemoteApi;
using System.Diagnostics;
using System.Linq;

namespace Kazyx.WPPMM.CameraManager
{
    class StatusUpdater
    {
        internal static void AvailableApis(CameraStatus status, string[] latest)
        {
            if (latest == null)
            {
                return;
            }
            status.AvailableApis = latest;
        }

        internal static void CameraStatus(CameraStatus status, string latest)
        {
            if (latest == null)
            {
                return;
            }
            status.Status = latest;
        }

        internal static void ZoomInfo(CameraStatus status, ZoomInfo latest)
        {
            if (latest == null)
            {
                return;
            }
            status.ZoomInfo = latest;
        }

        internal static void LiveviewAvailability(CameraStatus status, bool latest)
        {
            status.IsLiveviewAvailable = latest;
        }

        internal static void PostviewSize(CameraStatus status, Capability<string> latest)
        {
            if (latest == null)
            {
                return;
            }
            status.PostviewSizeInfo = latest;
        }

        internal static void SelfTimer(CameraStatus status, Capability<int> latest)
        {
            if (latest == null)
            {
                return;
            }
            status.SelfTimerInfo = latest;
        }

        internal static void ShootMode(CameraStatus status, Capability<string> latest)
        {
            if (latest == null)
            {
                return;
            }
            status.ShootModeInfo = new ExtendedInfo<string>(latest);
        }

        internal static void ExposureMode(CameraStatus status, Capability<string> latest)
        {
            if (latest == null)
            {
                return;
            }
            status.ExposureMode = latest;
        }

        internal static void ShutterSpeed(CameraStatus status, Capability<string> latest)
        {
            if (latest == null)
            {
                return;
            }
            status.ShutterSpeed = latest;
        }

        internal static void ISO(CameraStatus status, Capability<string> latest)
        {
            if (latest == null)
            {
                return;
            }
            status.ISOSpeedRate = latest;
        }

        internal static void FNumber(CameraStatus status, Capability<string> latest)
        {
            if (latest == null)
            {
                return;
            }
            status.FNumber = latest;
        }

        internal static void EvInfo(CameraStatus status, EvCapability latest)
        {
            if (latest == null)
            {
                return;
            }
            status.EvInfo = latest;
        }

        internal static void ProgramShift(CameraStatus status, bool? latest)
        {
            if (latest == null)
            {
                return;
            }
            status.ProgramShiftActivated = (bool)latest;
        }

        internal static void FocusStatus(CameraStatus status, string latest)
        {
            if (latest == null)
            {
                return;
            }
            status.FocusStatus = latest;
        }

        internal static void BeepMode(CameraStatus status, Capability<string> latest)
        {
            if (latest == null)
            {
                return;
            }
            status.BeepMode = latest;
        }

        internal static void SteadyMode(CameraStatus status, Capability<string> latest)
        {
            if (latest == null)
            {
                return;
            }
            status.SteadyMode = latest;
        }

        internal static void ViewAngle(CameraStatus status, Capability<int> latest)
        {
            if (latest == null)
            {
                return;
            }
            status.ViewAngle = latest;
        }

        internal static void MovieQuality(CameraStatus status, Capability<string> latest)
        {
            if (latest == null)
            {
                return;
            }
            status.MovieQuality = latest;
        }
    }
}
