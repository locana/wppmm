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

        public List<String> MethodTypes
        {
            get;
            set;
        }

        public List<String> AvailablePostViewSize
        {
            get;
            set;
        }

        public String PostViewImageSize
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
            MethodTypes = new List<string>();
            AvailablePostViewSize = new List<String>();
            PostViewImageSize = "";
        }

        public string[] AvailableApis { set; get; }
        public string CameraStatus { set; get; }
        public ZoomInfo ZoomInfo { set; get; }
        public bool LiveviewAvailable { set; get; }
        public BasicInfo<string> PostviewSizeInfo { set; get; }
        public BasicInfo<int> SelfTimerInfo { set; get; }
        public BasicInfo<string> ShootModeInfo { set; get; }
        public BasicInfo<string> ExposureMode { set; get; }
        public BasicInfo<string> ShutterSpeed { set; get; }
        public BasicInfo<string> ISOSpeedRate { set; get; }
        public BasicInfo<string> FNumber { set; get; }
        public EvInfo EvInfo { set; get; }
        public bool ProgramShiftActivated { set; get; }
    }
}
