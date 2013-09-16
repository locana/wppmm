using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPPMM.CameraManager
{
    class CameraManager
    {

        private static CameraManager cameraManager = new CameraManager();

        private CameraManager()
        {

        }


        public static CameraManager GetInstance()
        {
            return cameraManager;
        }

        public void RequestSearchDevices()
        {
            SearchDevices.RequestSearchDevices();
        }

    }
}
