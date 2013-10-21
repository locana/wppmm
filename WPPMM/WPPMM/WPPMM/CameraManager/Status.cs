using System;
using System.Collections.Generic;
using WPPMM.RemoteApi;

namespace WPPMM.CameraManager
{

    public class Status
    {

        /// <summary>
        /// returnes true if it's possible to connect.
        /// (device info has got correctly)
        /// </summary>
        public bool isAvailableConnecting
        {
            get;
            set;
        }

        /// <summary>
        /// Is this phone connected to target device (after getting URL of liveview)
        /// </summary>
        public bool isConnected
        {
            get;
            set;
        }

        /// <summary>
        /// Is available shooting (liveview running)
        /// </summary>
        public bool isAvailableShooting
        {
            get;
            set;
        }

        /// <summary>
        /// true during taking picture
        /// </summary>
        public bool isTakingPicture
        {
            get;
            set;
        }

        /// <summary>
        /// returnes true during rendering jpeg image on live view screen
        /// </summary>
        public bool isRendering
        {
            get;
            set;
        }

        public List<String> MethodTypes
        {
            get;
            set;
        }

        public List<String> SupportedFNumbers
        {
            get;
            set;
        }

        public void Init()
        {
            _init();
        }

        public Status()
        {
            _init();
        }

        private void _init()
        {
            isAvailableConnecting = false;
            isAvailableShooting = false;
            isConnected = false;
            isTakingPicture = false;
            isRendering = false;
            MethodTypes = new List<string>();
            SupportedFNumbers = new List<string>();
        }

        public string[] AvailableApis { internal set; get; }
        public string CameraStatus { internal set; get; }
        public ZoomInfo ZoomInfo { internal set; get; }
        public bool LiveviewAvailable { internal set; get; }
        public StrStrArray PostviewSizeInfo { internal set; get; }
        public IntIntArray SelfTimerInfo { internal set; get; }
        public StrStrArray ShootModeInfo { internal set; get; }
    }
}
