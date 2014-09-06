using Kazyx.RemoteApi;
using Kazyx.RemoteApi.AvContent;
using Kazyx.RemoteApi.Camera;
using Kazyx.WPPMM.CameraManager;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Kazyx.WPMMM.CameraManager
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

        public static async Task<List<DateInfo>> GetDateListAsync(AvContentApiClient av, string uri)
        {
            var contents = await av.GetContentListAsync(new ContentListTarget
            {
                Sorting = SortMode.Ascending,
                Grouping = ContentGroupingMode.Date,
                Uri = uri,
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

        public static async Task GetContentsOfDay(AvContentApiClient av, string uri, bool includeMovies)
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
            });

            var list = new List<ThumbnailInfo>();
            foreach (var content in contents)
            {
                if (content.IsContent == TextBoolean.True
                    && content.ImageContent != null
                    && content.ImageContent.OriginalImages != null
                    && content.ImageContent.OriginalImages.Count > 0)
                {
                    list.Add(new ThumbnailInfo
                    {
                        Name = content.ImageContent.OriginalImages[0].FileName,
                        LargeUrl = content.ImageContent.LargeImageUrl,
                        ThumbnailUrl = content.ImageContent.ThumbnailUrl,
                        ContentType = content.ContentKind,
                        Uri = content.Uri,
                    });
                }
            }
        }

        public static async Task<string> GetMovieStreamUri(AvContentApiClient av, string contentUri)
        {
            var uri = await av.SetStreamingContentAsync(new PlaybackContent { Uri = contentUri, RemotePlayType = RemotePlayMode.SimpleStreaming });
            await av.StartStreamingAsync();
            return uri.Url;
        }
    }

    public class DateInfo
    {
        public string Title { set; get; }
        public string Uri { set; get; }
    }

    public class ThumbnailInfo
    {
        public string Name { set; get; }
        public string LargeUrl { set; get; }
        public string ThumbnailUrl { set; get; }
        public string Uri { set; get; }
        public string ContentType { set; get; }
    }
}
