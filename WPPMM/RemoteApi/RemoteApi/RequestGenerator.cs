using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace WPPMM.RemoteApi
{
    /// <summary>
    /// This class provide function to generate JSON string to set as a body of API request.
    /// You just have to set required argument for the APIs.
    /// </summary>
    internal class RequestGenerator
    {
        private static int request_id = 0;

        private static int GetID()
        {
            var id = Interlocked.Increment(ref request_id);
            if (request_id > 1000000000)
            {
                request_id = 0;
            }
            return id;
        }

        private static string CreateJson(string name, string version, params object[] prms)
        {
            var param = new JArray();
            foreach (var p in prms)
            {
                param.Add(p);
            }

            var json = new JObject(
                new JProperty("method", name),
                new JProperty("version", version),
                new JProperty("id", GetID()),
                new JProperty("params", param));

            return json.ToString().Replace(" ", "").Replace("\n", "").Replace("\r", "");
        }

        private static string CreateJson10(string name, params object[] prms)
        {
            return CreateJson(name, "1.0", prms);
        }

        private static string GetCurrentMethodName([CallerMemberName]string name = "")
        {
            return name;
        }

        internal static string setShootMode(string mode)
        {
            return CreateJson10(GetCurrentMethodName(), mode);
        }

        internal static string getShootMode()
        {
            return CreateJson10(GetCurrentMethodName());
        }

        internal static string getSupportedShootMode()
        {
            return CreateJson10(GetCurrentMethodName());
        }

        internal static string getAvailableShootMode()
        {
            return CreateJson10(GetCurrentMethodName());
        }

        internal static string actTakePicture()
        {
            return CreateJson10(GetCurrentMethodName());
        }

        internal static string awaitTakePicture()
        {
            return CreateJson10(GetCurrentMethodName());
        }

        internal static string startMovieRec()
        {
            return CreateJson10(GetCurrentMethodName());
        }

        internal static string stopMovieRec()
        {
            return CreateJson10(GetCurrentMethodName());
        }

        internal static string startAudioRec()
        {
            return CreateJson10(GetCurrentMethodName());
        }

        internal static string stopAudioRec()
        {
            return CreateJson10(GetCurrentMethodName());
        }

        internal static string startLiveview()
        {
            return CreateJson10(GetCurrentMethodName());
        }

        internal static string stopLiveview()
        {
            return CreateJson10(GetCurrentMethodName());
        }

        internal static string actZoom(string direction, string movement)
        {
            return CreateJson10(GetCurrentMethodName(), direction, movement);
        }

        internal static string setSelfTimer(int second)
        {
            return CreateJson10(GetCurrentMethodName(), second);
        }

        internal static string getSelfTimer()
        {
            return CreateJson10(GetCurrentMethodName());
        }

        internal static string getSupportedSelfTimer()
        {
            return CreateJson10(GetCurrentMethodName());
        }

        internal static string getAvailableSelfTimer()
        {
            return CreateJson10(GetCurrentMethodName());
        }

        internal static string setPostviewImageSize(string size)
        {
            return CreateJson10(GetCurrentMethodName(), size);
        }

        internal static string getPostviewImageSize()
        {
            return CreateJson10(GetCurrentMethodName());
        }

        internal static string getSupportedPostviewImageSize()
        {
            return CreateJson10(GetCurrentMethodName());
        }

        internal static string getAvailablePostviewImageSize()
        {
            return CreateJson10(GetCurrentMethodName());
        }

        internal static string getEvent(bool longpolling)
        {
            return CreateJson10(GetCurrentMethodName(), longpolling);
        }

        internal static string startRecMode()
        {
            return CreateJson10(GetCurrentMethodName());
        }

        internal static string stopRecMode()
        {
            return CreateJson10(GetCurrentMethodName());
        }

        internal static string getAvailableApiList()
        {
            return CreateJson10(GetCurrentMethodName());
        }

        internal static string getApplicationInfo()
        {
            return CreateJson10(GetCurrentMethodName());
        }

        internal static string getVersions()
        {
            return CreateJson10(GetCurrentMethodName());
        }

        internal static string getMethodTypes(string version)
        {
            return CreateJson10(GetCurrentMethodName(), version);
        }
    }
}
