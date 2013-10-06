using Newtonsoft.Json.Linq;
using System.Reflection;
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

        internal static string setShootMode(string mode)
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name, mode);
        }

        internal static string getShootMode()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        internal static string getSupportedShootMode()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        internal static string getAvailableShootMode()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        internal static string actTakePicture()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        internal static string awaitTakePicture()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        internal static string startMovieRec()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        internal static string stopMovieRec()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        internal static string startLiveview()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        internal static string stopLiveview()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        internal static string actZoom(string direction, string movement)
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name, direction, movement);
        }

        internal static string setSelfTimer(int second)
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name, second);
        }

        internal static string getSelfTimer()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        internal static string getSupportedSelfTimer()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        internal static string getAvailableSelfTimer()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        internal static string setPostviewImageSize(string size)
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name, size);
        }

        internal static string getPostviewImageSize()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        internal static string getSupportedPostviewImageSize()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        internal static string getAvailablePostviewImageSize()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        internal static string getEvent(bool longpolling)
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name, longpolling);
        }

        internal static string startRecMode()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        internal static string stopRecMode()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        internal static string getAvailableApiList()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        internal static string getApplicationInfo()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        internal static string getVersions()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        internal static string getMethodTypes(string version)
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name, version);
        }
    }
}
