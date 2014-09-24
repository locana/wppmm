using Microsoft.Phone.Tasks;
using System.Diagnostics;
using System.Text;

namespace Kazyx.WPPMM.Utils
{
    public class DebugUtil
    {
        private static DebugUtil sDebugUtil = new DebugUtil();
        private StringBuilder LogBuilder = new StringBuilder();

        private DebugUtil() { }

        public static DebugUtil GetInstance()
        {
            return sDebugUtil;
        }

        /// <summary>
        /// Show given string on Debug log and keep to local instance.
        /// </summary>
        /// <param name="s">Log mesage</param>
        public static void Log(string s)
        {
#if DEBUG
            DebugUtil.GetInstance().AppendLog(s);
#endif
        }

        private void AppendLog(string s)
        {
#if DEBUG
            Debug.WriteLine(s);
            LogBuilder.Append(s);
            LogBuilder.Append("\n");
#endif
        }

        /// <summary>
        /// Get entire log.
        /// </summary>
        /// <returns>Log message</returns>
        public string GetLog()
        {
#if DEBUG
            return LogBuilder.ToString();
#else
            return "";
#endif
        }

        /// <summary>
        /// Export all logs to email application.
        /// </summary>
        public void ComposeDebugMail()
        {
#if DEBUG
            EmailComposeTask emailComposeTask = new EmailComposeTask();
            emailComposeTask.Subject = "debug messages.";
            emailComposeTask.Body = "Debug logs are here:\n\n" + this.GetLog();
            emailComposeTask.To = "";
            emailComposeTask.Show();
#endif
        }
    }
}
