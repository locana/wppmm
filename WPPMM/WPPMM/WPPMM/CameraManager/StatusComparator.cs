using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPPMM.RemoteApi;

namespace WPPMM.CameraManager
{
    class StatusComparator
    {
        internal static bool IsAvailableApisModified(Status status, string[] latest)
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

        internal static bool IsCameraStatusModified(Status status, string latest)
        {
            var previous = status.CameraStatus;
            status.CameraStatus = latest;

            return previous != latest;
        }

        internal static bool IsZoomInfoModified(Status status, ZoomInfo latest)
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

        internal static bool IsLiveviewAvailableModified(Status status, bool latest)
        {
            var previous = status.LiveviewAvailable;
            status.LiveviewAvailable = latest;

            return previous != latest;
        }

        internal static bool IsPostviewSizeInfoModified(Status status, StrStrArray latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.PostviewSizeInfo;
            status.PostviewSizeInfo = latest;
            return IsModified(previous, latest);
        }

        internal static bool IsSelftimerInfoModified(Status status, IntIntArray latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.SelfTimerInfo;
            status.SelfTimerInfo = latest;
            return IsModified(previous, latest);
        }

        internal static bool IsShootModeInfoModified(Status status, StrStrArray latest)
        {
            if (latest == null)
            {
                return false;
            }
            var previous = status.ShootModeInfo;
            status.ShootModeInfo = latest;
            return IsModified(previous, latest);
        }

        private static bool IsModified(StrStrArray previous, StrStrArray latest)
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

        private static bool IsModified(IntIntArray previous, IntIntArray latest)
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
