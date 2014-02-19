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

        /// <summary>
        /// Automatically insert "version":"1.0"
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static string Jsonize(string name)
        {
            return CreateJson(name, "1.0");
        }

        /// <summary>
        /// Automatically insert "version":"1.0"
        /// </summary>
        /// <param name="name"></param>
        /// <param name="prms"></param>
        /// <returns></returns>
        internal static string Jsonize(string name, params string[] prms)
        {
            return CreateJson(name, "1.0", prms);
        }

        /// <summary>
        /// Automatically insert "version":"1.0"
        /// </summary>
        /// <param name="name"></param>
        /// <param name="prms"></param>
        /// <returns></returns>
        internal static string Jsonize(string name, params int[] prms)
        {
            return CreateJson(name, "1.0", prms);
        }

        /// <summary>
        /// Automatically insert "version":"1.0"
        /// </summary>
        /// <param name="name"></param>
        /// <param name="prms"></param>
        /// <returns></returns>
        internal static string Jsonize(string name, params bool[] prms)
        {
            return CreateJson(name, "1.0", prms);
        }
    }
}
