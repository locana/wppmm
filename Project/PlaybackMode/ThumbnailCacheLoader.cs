using Kazyx.WPPMM.DataModel;
using Kazyx.WPPMM.Utils;
using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Kazyx.WPPMM.PlaybackMode
{
    public class ThumbnailCacheLoader
    {
        private const string CACHE_ROOT = "/thumb_cache/";
        private const string CACHE_ROOT_TMP = "/tmp/thumb_cache/";

        private ThumbnailCacheLoader()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                StorageUtil.ConfirmDirectoryCreated(store, CACHE_ROOT);
                StorageUtil.ConfirmDirectoryCreated(store, CACHE_ROOT_TMP);
            }
        }

        private static readonly ThumbnailCacheLoader instance = new ThumbnailCacheLoader();

        public static ThumbnailCacheLoader INSTANCE
        {
            get { return instance; }
        }

        private const int THUMBNAIL_SIZE = 240;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uuid">Specify uuid to delete directory for the device, otherwise delete all of stored cache.</param>
        public void DeleteCache(string uuid = null)
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (uuid == null)
                {
                    DebugUtil.Log("Delete all of thumbnail cache.");
                    StorageUtil.DeleteDirectoryReqursive(store, CACHE_ROOT, false);
                    StorageUtil.DeleteDirectoryReqursive(store, CACHE_ROOT_TMP, false);
                }
                else
                {
                    DebugUtil.Log("Delete thumbnail cache of " + uuid);
                    var directory = CACHE_ROOT + uuid.Replace(":", "-") + "/";
                    var directory_tmp = CACHE_ROOT_TMP + uuid.Replace(":", "-") + "/";

                    StorageUtil.DeleteDirectoryReqursive(store, directory);
                    StorageUtil.DeleteDirectoryReqursive(store, directory_tmp);
                }
            }
        }

        /// <summary>
        /// Asynchronously download thumbnail image and return local cache path.
        /// </summary>
        /// <param name="uuid">UUID of the target device.</param>
        /// <param name="content">Source of thumbnail image.</param>
        /// <returns>Path to local thumbnail cache.</returns>
        public async Task<string> GetCachePathAsync(string uuid, ContentInfo content)
        {
            var uri = new Uri(content.ThumbnailUrl);
            var directory = CACHE_ROOT + uuid.Replace(":", "-") + "/";
            var directory_tmp = CACHE_ROOT_TMP + uuid.Replace(":", "-") + "/";
            var filename = content.CreatedTime.Replace(":", "-").Replace("/", "-") + "--" + Path.GetFileName(uri.LocalPath);
            var filepath = directory + filename;
            var filepath_tmp = directory_tmp + filename;

            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                StorageUtil.ConfirmDirectoryCreated(store, directory);
                StorageUtil.ConfirmDirectoryCreated(store, directory_tmp);

                if (store.FileExists(filepath))
                {
                    // DebugUtil.Log("Existing thumbnail cache: " + filepath);
                    return filepath;
                }

                if (store.FileExists(filepath_tmp))
                {
                    return await GetResizedCachePathAsync(filepath_tmp, filepath);
                }
            }

            using (var stream = await Downloader.GetResponseStreamAsync(uri))
            {
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    lock (this)
                    {
                        if (!store.FileExists(filepath_tmp))
                        {
                            using (var dst = store.CreateFile(filepath_tmp))
                            {
                                stream.CopyTo(dst);
                            }
                        }
                    }
                    return await GetResizedCachePathAsync(filepath_tmp, filepath);
                }
            }
        }

        private Task<string> GetResizedCachePathAsync(string path, string resizedPath)
        {
            var tcs = new TaskCompletionSource<string>();
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (store.FileExists(resizedPath))
                        {
                            tcs.TrySetResult(resizedPath);
                            return;
                        }
                    }

                    using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        using (var stream = store.OpenFile(path, FileMode.Open))
                        {
                            var original = new BitmapImage();
                            original.CreateOptions = BitmapCreateOptions.None;
                            original.SetSource(stream);
                            var max = Math.Max(original.PixelHeight, original.PixelWidth);
                            var scale = (float)THUMBNAIL_SIZE / (float)max;
                            var wbmp = new WriteableBitmap(original);

                            using (var writeTo = store.CreateFile(resizedPath))
                            {
                                wbmp.SaveJpeg(writeTo, (int)(original.PixelWidth * scale), (int)(original.PixelHeight * scale), 0, 96);
                                // DebugUtil.Log("Saved resized image: " + resizedPath + " - " + (int)(tmp.PixelWidth * scale) + "x" + (int)(tmp.PixelHeight * scale));
                                DebugUtil.Log("New thumbnail cache: " + resizedPath);
                                tcs.TrySetResult(resizedPath);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    DebugUtil.Log(e.StackTrace);
                    tcs.TrySetException(e);
                }
            });
            return tcs.Task;
        }
    }
}
