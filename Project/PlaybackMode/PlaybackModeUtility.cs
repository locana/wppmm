using Kazyx.RemoteApi;
using Kazyx.RemoteApi.AvContent;
using Kazyx.RemoteApi.Camera;
using Kazyx.WPMMM.DataModel;
using Kazyx.WPPMM.CameraManager;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Kazyx.WPMMM.PlaybackMode
{
    public class PlaybackModeUtility
    {
        public static async Task<bool> MoveToShootingModeAsync(CameraApiClient camera, CameraStatus status)
        {
            return await MoveToSpecifiedModeAsync(camera, status, CameraFunction.RemoteShooting, EventParam.Idle);
        }

        public static async Task<bool> MoveToContentTransferModeAsync(CameraApiClient camera, CameraStatus status)
        {
            return await MoveToSpecifiedModeAsync(camera, status, CameraFunction.ContentTransfer, EventParam.ContentsTransfer);
        }

        private static async Task<bool> MoveToSpecifiedModeAsync(CameraApiClient camera, CameraStatus status, string nextFunction, string nextState)
        {
            var tcs = new TaskCompletionSource<bool>();
            var ct = new CancellationTokenSource(10000); // State change timeout 10 sec.
            ct.Token.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false);

            PropertyChangedEventHandler status_observer = (sender, e) =>
            {
                switch (e.PropertyName)
                {
                    case "Status":
                        var current = (sender as CameraStatus).Status;
                        if (nextState == current)
                        {
                            Debug.WriteLine("Camera state changed to " + nextState + " successfully.");
                            tcs.TrySetResult(true);
                        }
                        else if (EventParam.NotReady != current)
                        {
                            Debug.WriteLine("Unfortunately camera state changed to " + current);
                            tcs.TrySetResult(false);
                        }
                        Debug.WriteLine("It might be in transitioning state...");
                        break;
                    default:
                        break;
                }
            };

            try
            {
                status.PropertyChanged += status_observer;
                await camera.SetCameraFunctionAsync(nextFunction);
                return await tcs.Task;
            }
            catch (RemoteApiException e)
            {
                if (e.code != StatusCode.IllegalState)
                {
                    Debug.WriteLine("Failed to change camera state.");
                    return false;
                }
            }
            finally
            {
                status.PropertyChanged -= status_observer;
            }

            try
            {
                Debug.WriteLine("Failed to change camera state. Check current state...");
                return nextState == await camera.GetCameraFunctionAsync();
            }
            catch (RemoteApiException)
            {
                return false;
            }
        }

        /// <summary>
        /// Camera devices should support "storage" scheme.
        /// </summary>
        /// <param name="av"></param>
        /// <returns></returns>
        public static async Task<bool> IsStorageSupportedAsync(AvContentApiClient av)
        {
            var schemes = await av.GetSchemeListAsync();
            foreach (var scheme in schemes)
            {
                if (scheme.Scheme == Scheme.Storage)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get uri of the storages.
        /// </summary>
        /// <param name="av"></param>
        /// <returns></returns>
        public static async Task<List<string>> GetStoragesUriAsync(AvContentApiClient av)
        {
            var sources = await av.GetSourceListAsync(new UriScheme { Scheme = Scheme.Storage });
            var list = new List<string>(sources.Count);
            foreach (var source in sources)
            {
                list.Add(source.Source);
            }
            return list;
        }

        private const int CONTENT_LOOP_STEP = 50;

        public static async Task GetDateListAsEventsAsync(AvContentApiClient av, string uri, Action<DateListEventArgs> handler, CancellationTokenSource cancel = null)
        {
            var count = await av.GetContentCountAsync(new CountingTarget
            {
                Grouping = ContentGroupingMode.Date,
                Uri = uri,
            });

            var loops = count.NumOfContents / CONTENT_LOOP_STEP + (count.NumOfContents % CONTENT_LOOP_STEP == 0 ? 0 : 1);

            for (var i = 0; i < loops; i++)
            {
                var dates = await GetDateListAsync(av, uri, i * CONTENT_LOOP_STEP, CONTENT_LOOP_STEP);
                if (cancel != null && cancel.IsCancellationRequested)
                {
                    break;
                }
                handler.Invoke(new DateListEventArgs(dates));
            }
        }

        private static async Task<List<DateInfo>> GetDateListAsync(AvContentApiClient av, string uri, int startFrom, int count)
        {
            var contents = await av.GetContentListAsync(new ContentListTarget
            {
                Sorting = SortMode.Ascending,
                Grouping = ContentGroupingMode.Date,
                Uri = uri,
                StartIndex = startFrom,
                MaxContents = count
            });

            var list = new List<DateInfo>();
            foreach (var content in contents)
            {
                if (content.IsFolder == TextBoolean.True)
                {
                    list.Add(new DateInfo { Title = content.Title, Uri = content.Uri });
                }
            }
            return list;
        }

        public static async Task GetContentsOfDayAsEventsAsync(AvContentApiClient av, DateInfo date, bool includeMovies, Action<ContentListEventArgs> handler, CancellationTokenSource cancel = null)
        {
            var count = await av.GetContentCountAsync(new CountingTarget
            {
                Grouping = ContentGroupingMode.Date,
                Uri = date.Uri,
            });

            var loops = count.NumOfContents / CONTENT_LOOP_STEP + (count.NumOfContents % CONTENT_LOOP_STEP == 0 ? 0 : 1);

            for (var i = 0; i < loops; i++)
            {
                var contents = await GetContentsOfDayAsync(av, date.Uri, i * CONTENT_LOOP_STEP, CONTENT_LOOP_STEP, includeMovies);
                if (cancel != null && cancel.IsCancellationRequested)
                {
                    break;
                }
                handler.Invoke(new ContentListEventArgs(date, contents));
            }
        }

        private static async Task<List<ContentInfo>> GetContentsOfDayAsync(AvContentApiClient av, string uri, int startFrom, int count, bool includeMovies)
        {
            var types = new List<string>();
            types.Add(ContentKind.StillImage);
            if (includeMovies)
            {
                types.Add(ContentKind.MovieMp4);
                types.Add(ContentKind.MovieXavcS);
            }

            var contents = await av.GetContentListAsync(new ContentListTarget
            {
                Sorting = SortMode.Ascending,
                Grouping = ContentGroupingMode.Date,
                Uri = uri,
                Types = types,
                StartIndex = startFrom,
                MaxContents = count
            });

            var list = new List<ContentInfo>();
            foreach (var content in contents)
            {
                if (content.ImageContent != null
                    && content.ImageContent.OriginalContents != null
                    && content.ImageContent.OriginalContents.Count > 0)
                {
                    var contentInfo = new ContentInfo
                    {
                        Name = RemoveExtension(content.ImageContent.OriginalContents[0].FileName),
                        LargeUrl = content.ImageContent.LargeImageUrl,
                        ThumbnailUrl = content.ImageContent.ThumbnailUrl,
                        ContentType = content.ContentKind,
                        Uri = content.Uri,
                        CreatedTime = content.CreatedTime,
                        Protected = content.IsProtected == TextBoolean.True,
                        RemotePlaybackAvailable = (content.RemotePlayTypes != null && content.RemotePlayTypes.Contains(RemotePlayMode.SimpleStreaming)),
                    };

                    if (content.ContentKind == ContentKind.StillImage)
                    {
                        foreach (var original in content.ImageContent.OriginalContents)
                        {
                            if (original.Type == ImageType.Jpeg)
                            {
                                contentInfo.OriginalUrl = original.Url;
                            }
                        }
                    }

                    list.Add(contentInfo);
                }
            }

            return list;
        }

        public static async Task<string> PrepareMovieStreamingAsync(AvContentApiClient av, string contentUri)
        {
            var uri = await av.SetStreamingContentAsync(new PlaybackContent { Uri = contentUri, RemotePlayType = RemotePlayMode.SimpleStreaming });
            await av.StartStreamingAsync();
            return uri.Url;
        }

        private static string RemoveExtension(string name)
        {
            if (name == null)
            {
                return "";
            }
            if (!name.Contains("."))
            {
                return name;
            }
            else
            {
                var index = name.LastIndexOf(".");
                if (index == 0)
                {
                    return "";
                }
                else
                {
                    return name.Substring(0, index);
                }
            }
        }
    }

    public class DateListEventArgs : EventArgs
    {
        private readonly List<DateInfo> dates;

        public DateListEventArgs(List<DateInfo> dates)
        {
            this.dates = dates;
        }

        public List<DateInfo> DateList
        {
            get { return dates; }
        }
    }

    public class ContentListEventArgs : EventArgs
    {
        private readonly DateInfo date;
        private readonly List<ContentInfo> contents;

        public ContentListEventArgs(DateInfo date, List<ContentInfo> contents)
        {
            this.date = date;
            this.contents = contents;
        }

        public DateInfo DateInfo
        {
            get { return date; }
        }

        public List<ContentInfo> ContentList
        {
            get { return contents; }
        }
    }
}
