using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPPMM.Json;

namespace WPPMM.CameraManager
{

    /// <summary>
    /// A client of camera service version 1.0
    /// </summary>
    public class CameraServiceClient10
    {
        private readonly string endpoint;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="endpoint"> Endpoint URL of camera service.</param>
        public CameraServiceClient10(string endpoint)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException();
            }
            this.endpoint = endpoint;
        }

        public void actTakePicture(Action<int> error, Action<string[]> result)
        {
            XhrPost.Post(endpoint, Request.actTakePicture(),
                (res) => { ResultHandler.ActTakePicture(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }

        public void startLiveview(Action<int> error, Action<string> result)
        {
            XhrPost.Post(endpoint, Request.startLiveview(),
                (res) => { ResultHandler.StartLiveview(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
        }


    }
}
