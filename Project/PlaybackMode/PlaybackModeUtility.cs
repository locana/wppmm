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
            return await MoveToSpecifiedModeAsync(camera, status, EventParam.Idle);
        }

        public static async Task<bool> MoveToContentTransferModeAsync(CameraApiClient camera, CameraStatus status)
        {
            return await MoveToSpecifiedModeAsync(camera, status, EventParam.ContentsTransfer);
        }

        private static async Task<bool> MoveToSpecifiedModeAsync(CameraApiClient camera, CameraStatus status, string nextState)
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
                await camera.SetCameraFunctionAsync(CameraFunction.ContentTransfer);
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

        public static async Task GetDateListAsEvents(AvContentApiClient av, string uri, Action<DateListEventArgs> handler)
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

        public static async Task GetContentsOfDayAsEvents(AvContentApiClient av, DateInfo date, bool includeMovies, Action<ContentListEventArgs> handler)
        {
            var count = await av.GetContentCountAsync(new CountingTarget
            {
                Grouping = ContentGroupingMode.Date,
                Uri = date.Uri,
            });

            var loops = count.NumOfContents / CONTENT_LOOP_STEP + (count.NumOfContents % CONTENT_LOOP_STEP == 0 ? 0 : 1);

            for (var i = 0; i < loops; i++)
            {
                var contents = await GetContentsOfDay(av, date.Uri, i * CONTENT_LOOP_STEP, CONTENT_LOOP_STEP, includeMovies);
                handler.Invoke(new ContentListEventArgs(date, contents));
            }
        }

        private static async Task<List<ContentInfo>> GetContentsOfDay(AvContentApiClient av, string uri, int startFrom, int count, bool includeMovies)
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
                if (content.IsContent == TextBoolean.True
                    && content.ImageContent != null
                    && content.ImageContent.OriginalImages != null
                    && content.ImageContent.OriginalImages.Count > 0)
                {
                    list.Add(new ContentInfo
                    {
                        Name = content.ImageContent.OriginalImages[0].FileName,
                        LargeUrl = content.ImageContent.LargeImageUrl,
                        ThumbnailUrl = content.ImageContent.ThumbnailUrl,
                        ContentType = content.ContentKind,
                        Uri = content.Uri,
                    });
                }
            }

            return list;
        }

        public static async Task<string> GetMovieStreamUri(AvContentApiClient av, string contentUri)
        {
            var uri = await av.SetStreamingContentAsync(new PlaybackContent { Uri = contentUri, RemotePlayType = RemotePlayMode.SimpleStreaming });
            await av.StartStreamingAsync();
            return uri.Url;
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
