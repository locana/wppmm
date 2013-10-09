using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPPMM.CameraManager
{

    class Status
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
        }
    }
}
