﻿using Microsoft.Phone.Reactive;
using Microsoft.Xna.Framework.Media;
using System;
using System.Diagnostics;
using System.Net;

namespace WPPMM.CameraManager
{
    class Downloader
    {

        public Downloader()
        {
        }

        internal void DownloadImageFile(Uri uri, Action<Picture> OnCompleted, Action OnError)
        {
            var request = HttpWebRequest.Create(uri);
            Observable.FromAsyncPattern<WebResponse>(request.BeginGetResponse, request.EndGetResponse)()
            .Select(res => res.GetResponseStream())
            .Select(strm => new MediaLibrary().SavePictureToCameraRoll(string.Format("SavedPicture{0}.jpg", DateTime.Now), strm))
            .ObserveOnDispatcher()
            .Subscribe(pic =>
            {
                if (pic == null)
                {
                    Debug.WriteLine("Saved Picture is null");
                    OnError();
                }
                else
                {
                    OnCompleted(pic);
                }
            }
            , e =>
            {
                OnError.Invoke();
            });
        }
    }
}
