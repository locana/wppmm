using System.IO.IsolatedStorage;

namespace Kazyx.WPPMM.Utils
{
    public class StorageUtil
    {
        private StorageUtil() { }

        public static void DeleteDirectoryReqursive(IsolatedStorageFile store, string directory, bool includeThis = true)
        {
            if (store.DirectoryExists(directory))
            {
                foreach (var file in store.GetFileNames(directory))
                {
                    store.DeleteFile(directory + file);
                }
                foreach (var dir in store.GetDirectoryNames(directory))
                {
                    DeleteDirectoryReqursive(store, directory + dir + "/");
                }

                if (includeThis)
                {
                    store.DeleteDirectory(directory);
                }
            }
        }

        public static void ConfirmDirectoryCreated(IsolatedStorageFile store, string directory)
        {
            if (!store.DirectoryExists(directory))
            {
                store.CreateDirectory(directory);
            }
        }
    }
}
