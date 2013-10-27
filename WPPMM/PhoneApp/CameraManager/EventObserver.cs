using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using WPPMM.RemoteApi;

namespace WPPMM.CameraManager
{
    public class EventObserver
    {
        private readonly CameraServiceClient10 client;

        private bool ongoing = false;

        private int failure_count = 0;

        private const int RETRY_LIMIT = 3;

        private const int RETRY_INTERVAL_SEC = 3;

        private Status status;

        private Action<EventMember> OnDetectDifference = null;

        private Action OnStop = null;

        private BackgroundWorker worker = new BackgroundWorker()
        {
            WorkerReportsProgress = false,
            WorkerSupportsCancellation = true
        };

        public EventObserver(CameraServiceClient10 client)
        {
            this.client = client;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="status">Status object to update</param>
        /// <param name="OnDetectDifference">Called when the parameter has been changed</param>
        /// <param name="OnStop">Called when event observation is finished with error</param>
        ///
        public void Start(Status status, Action<EventMember> OnDetectDifference, Action OnStop)
        {
            if (status == null | OnDetectDifference == null || OnStop == null)
            {
                throw new ArgumentNullException();
            }
            this.status = status;
            this.OnDetectDifference = OnDetectDifference;
            this.OnStop = OnStop;
            ongoing = true;
            failure_count = RETRY_LIMIT;
            worker.DoWork += AnalyzeEventData;
            client.GetEvent(false, OnError, OnSuccess);
        }

        /// <summary>
        /// Force finish event observation. Any callbacks will not be invoked after this.
        /// </summary>
        public void Stop()
        {
            Deactivate();
        }

        private void OnError(int code)
        {
            switch (code)
            {
                case StatusCode.NotAcceptable:
                case StatusCode.CameraNotReady:
                case StatusCode.IllegalState:
                case StatusCode.ServiceUnavailable:
                case StatusCode.Timeout:
                case StatusCode.Any:
                    if (++failure_count < RETRY_LIMIT)
                    {
                        Timer timer = new Timer((state) =>
                        {
                            Call();
                        }, null, TimeSpan.FromSeconds(RETRY_INTERVAL_SEC), new TimeSpan(-1));
                        return;
                    }
                    break;
                default:
                    break;
            }

            if (OnStop != null)
            {
                OnStop.Invoke();
            }
            Deactivate();
        }

        private void OnSuccess(Event data)
        {
            failure_count = 0;
            worker.RunWorkerAsync(data);
        }

        private void AnalyzeEventData(object sender, DoWorkEventArgs e)
        {
            var data = e.Argument as Event;
            if (StatusComparator.IsAvailableApisModified(status, data.AvailableApis))
                NotifyChangeDetected(EventMember.AvailableApis);

            if (StatusComparator.IsCameraStatusModified(status, data.CameraStatus))
                NotifyChangeDetected(EventMember.CameraStatus);

            if (StatusComparator.IsLiveviewAvailableModified(status, data.LiveviewAvailable))
                NotifyChangeDetected(EventMember.LiveviewAvailable);

            if (StatusComparator.IsPostviewSizeInfoModified(status, data.PostviewSizeInfo))
                NotifyChangeDetected(EventMember.PostviewSizeInfo);

            if (StatusComparator.IsSelftimerInfoModified(status, data.SelfTimerInfo))
                NotifyChangeDetected(EventMember.SelfTimerInfo);

            if (StatusComparator.IsShootModeInfoModified(status, data.ShootModeInfo))
                NotifyChangeDetected(EventMember.ShootModeInfo);

            if (StatusComparator.IsZoomInfoModified(status, data.ZoomInfo))
                NotifyChangeDetected(EventMember.ZoomInfo);

            if (StatusComparator.IsExposureModeInfoModified(status, data.ExposureMode))
                NotifyChangeDetected(EventMember.ExposureMode);

            if (StatusComparator.IsFNumberModified(status, data.FNumber))
                NotifyChangeDetected(EventMember.FNumber);

            if (StatusComparator.IsShutterSpeedModified(status, data.ShutterSpeed))
                NotifyChangeDetected(EventMember.ShutterSpeed);

            if (StatusComparator.IsISOModified(status, data.ISOSpeedRate))
                NotifyChangeDetected(EventMember.ISOSpeedRate);

            if (StatusComparator.IsEvInfoModified(status, data.EvInfo))
                NotifyChangeDetected(EventMember.EVInfo);

            if (StatusComparator.IsProgramShiftModified(status, data.ProgramShiftActivated))
                NotifyChangeDetected(EventMember.ProgramShift);

            Call();
        }

        private void Call()
        {
            if (ongoing)
            {
                client.GetEvent(true, OnError, OnSuccess);
            }
        }

        private void NotifyChangeDetected(EventMember target)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (OnDetectDifference != null)
                { OnDetectDifference.Invoke(target); }
            });
        }

        private void Deactivate()
        {
            Debug.WriteLine("EventObserver deactivated");
            ongoing = false;
            status = null;
            OnStop = null;
            OnDetectDifference = null;
            worker.DoWork -= AnalyzeEventData;
        }
    }

    public enum EventMember
    {
        AvailableApis,
        CameraStatus,
        ZoomInfo,
        LiveviewAvailable,
        PostviewSizeInfo,
        SelfTimerInfo,
        ShootModeInfo,
        ExposureMode,
        ShutterSpeed,
        FNumber,
        ISOSpeedRate,
        EVInfo,
        ProgramShift
    }
}
