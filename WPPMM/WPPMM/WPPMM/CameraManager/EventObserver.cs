using System;
using System.Threading;
using WPPMM.RemoteApi;

namespace WPPMM.CameraManager
{
    public class EventObserver
    {
        private readonly CameraServiceClient10 client;

        private bool ongoing = false;

        private int failure_count = 0;

        private const int RETRY_LIMIT = 3;

        private const int RETRY_INTERVAL_SEC = 2;

        private Action OnDetectDifference = null;

        private Action OnStop = null;

        public EventObserver(CameraServiceClient10 client)
        {
            this.client = client;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="OnDetectDifference">Called when any of parameter has been changed.</param>
        /// <param name="OnStop">Called when event observation is finished with error.</param>
        public void Start(Action OnDetectDifference, Action OnStop)
        {
            this.OnDetectDifference = OnDetectDifference;
            this.OnStop = OnStop;
            ongoing = true;
            failure_count = RETRY_LIMIT;
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
                    break;
                default:
                    return;
            }

            if (++failure_count < RETRY_LIMIT)
            {
                Timer timer = new Timer((state) =>
                {
                    Call();
                }, null, TimeSpan.FromSeconds(RETRY_INTERVAL_SEC), new TimeSpan(-1));
            }
            else
            {
                if (OnStop != null)
                {
                    OnStop.Invoke();
                }
                Deactivate();
            }
        }

        private void OnSuccess(Event @event)
        {
            failure_count = 0;
            //TODO go to the background thread and start analization.
            //TODO check differences between the latest and the previous.
            //TODO update to the latest information.
            //TODO notify the differences
            Call();
        }

        private void Call()
        {
            if (ongoing)
            {
                client.GetEvent(true, OnError, OnSuccess);
            }
        }

        private void Deactivate()
        {
            ongoing = false;
            OnStop = null;
            OnDetectDifference = null;
        }
    }
}
