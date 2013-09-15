using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Threading;

namespace WPPMM.Json
{
    public class Request
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

            return json.ToString();
        }

        private static string CreateJson10(string name, params object[] prms)
        {
            return CreateJson(name, "1.0", prms);
        }

        public static string setShootMode(string mode)
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name, mode);
        }

        public static string getShootMode()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        public static string getSupportedShootMode()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        public static string getAvailableShootMode()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        public static string actTakePicture()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        public static string awaitTakePicture()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        public static string startMovieRec()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        public static string stopMovieRec()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        public static string startLiveview()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        public static string stopLiveview()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        public static string actZoom(string direction, string movement)
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name, direction, movement);
        }

        public static string setSelfTimer(int second)
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name, second);
        }

        public static string getSelfTimer()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        public static string getSupportedSelfTimer()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        public static string getAvailableSelfTimer()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        public static string setPostviewImageSize(string size)
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name, size);
        }

        public static string getPostviewImageSize()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        public static string getSupportedPostviewImageSize()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        public static string getAvailablePostviewImageSize()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        public static string getEvent(bool longpolling)
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name, longpolling);
        }

        public static string startRecMode()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        public static string stopRecMode()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        public static string getAvailableApiList()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        public static string getApplicationInfo()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        public static string getVersions()
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name);
        }

        public static string getMethodTypes(string version)
        {
            return CreateJson10(MethodBase.GetCurrentMethod().Name, version);
        }
    }
}
