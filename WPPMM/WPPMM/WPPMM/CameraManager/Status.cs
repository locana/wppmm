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

        public Status()
        {
            _init();
        }

        private void _init()
        {
        }
    }
}
