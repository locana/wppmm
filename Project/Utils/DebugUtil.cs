#if DEBUG
using Microsoft.Phone.Tasks;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
#endif

namespace Kazyx.WPPMM.Utils
{
    public class DebugUtil
    {
#if DEBUG
        private static StringBuilder LogBuilder = new StringBuilder();

        private const string LOG_ROOT = "/log_store/";

        private const string LOG_EXTENSION = ".txt";

        private const int MaxLength = 63 * 1024; // Byte

        private static readonly object Lock = new object();
#endif

        /// <summary>
        /// Show given string on Debug log and keep to local instance.
        /// </summary>
        /// <param name="s">Log mesage</param>
        public static void Log(string s)
        {
#if DEBUG
            lock (Lock)
            {
                Debug.WriteLine(s);
                LogBuilder.Append(s);
                LogBuilder.Append("\n");
                if (LogBuilder.Length > MaxLength)
                {
                    Flush();
                }
            }
#endif
        }

#if DEBUG
        public static void Flush(bool crash = false)
        {
            lock (Lock)
            {
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    StorageUtil.ConfirmDirectoryCreated(store, LOG_ROOT);
                    var time = DateTimeOffset.Now.ToLocalTime().ToString("yyyyMMdd-HHmmss");
                    var filename = LOG_ROOT + time + (crash ? "_crash" : "") + LOG_EXTENSION;
                    Debug.WriteLine("\n\nFlush log file: {0}\n\n", filename);

                    using (var str = store.CreateFile(filename))
                    {
                        using (var writer = new StreamWriter(str))
                        {
                            writer.Write(LogBuilder.ToString());
                        }
                    }
                }
                LogBuilder.Clear();
            }
        }

        public static string[] LogFiles()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                StorageUtil.ConfirmDirectoryCreated(store, LOG_ROOT);
                return store.GetFileNames(LOG_ROOT + "*");
            }
        }

        public static string GetFile(string filename)
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                StorageUtil.ConfirmDirectoryCreated(store, LOG_ROOT);
                using (var str = store.OpenFile(LOG_ROOT + filename, FileMode.Open))
                {
                    using (var reader = new StreamReader(str))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }
#endif

        /// <summary>
        /// Export all logs to email application.
        /// </summary>
        public static void ComposeDebugMail(string message)
        {
#if DEBUG
            lock (Lock)
            {
                EmailComposeTask emailComposeTask = new EmailComposeTask();
                emailComposeTask.Subject = "debug messages.";
                emailComposeTask.Body = "Debug logs are here:\n\n" + message;
                emailComposeTask.To = "";
                emailComposeTask.Show();
            }
#endif
        }
    }
}
