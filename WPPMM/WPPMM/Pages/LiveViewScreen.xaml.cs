using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WPPMM.CameraManager;

namespace WPPMM.Pages
{
    public partial class LiveViewScreen : PhoneApplicationPage
    {

        private CameraManager.CameraManager cameraManager = null;
        private bool isRequestingLiveview = false;

        public LiveViewScreen()
        {
            InitializeComponent();

            cameraManager = CameraManager.CameraManager.GetInstance();
            cameraManager.RegisterUpdateListener(UpdateListener);
            cameraManager.StartLiveView();

            isRequestingLiveview = true;
            // cameraManager.StartLiveView();
        }

        public void UpdateListener()
        {
            if (isRequestingLiveview && CameraManager.CameraManager.GetLiveviewUrl() != null)
            {
                // starting liveview
                cameraManager.ConnectLiveView();
            }
                
        }
    }
}