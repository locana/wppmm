using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace WPPMM.RemoteApi
{
    internal class BasicResultHandler
    {
        /// <summary>
        /// Check whether the response is error or success.
        /// </summary>
        /// <param name="json"></param>
        /// <param name="error"></param>
        /// <returns>true if the json is the error response.</returns>
        internal static bool HandleError(JObject json, Action<int> error)
        {
            if (json == null)
            {
                error.Invoke(6); // Illegal Response
                return true;
            }
            if (json["error"] != null)
            {
                error.Invoke(json["error"].Value<int>(0));
                return true;
            }

            return false;
        }

        /// <summary>
        /// For response which has no value or no effective value.
        /// </summary>
        /// <param name="jString"></param>
        /// <param name="error"></param>
        /// <param name="result"></param>
        internal static void HandleNoValue(string jString, Action<int> error, Action result)
        {
            var json = JObject.Parse(jString);
            if (HandleError(json, error))
            {
                return;
            }

            result.Invoke();
        }

        /// <summary>
        /// For response which has a single value.
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="jString"></param>
        /// <param name="error"></param>
        /// <param name="result"></param>
        internal static void HandleSingleValue<T>(string jString, Action<int> error, Action<T> result)
        {
            var json = JObject.Parse(jString);
            if (HandleError(json, error))
            {
                return;
            }

            result.Invoke(json["result"].Value<T>(0));
        }

        /// <summary>
        /// For response which has a single Array consists of a single type.
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="jString"></param>
        /// <param name="error"></param>
        /// <param name="result"></param>
        internal static void HandleArray<T>(string jString, Action<int> error, Action<T[]> result)
        {
            var json = JObject.Parse(jString);
            if (HandleError(json, error))
            {
                return;
            }

            var array = new List<T>();
            foreach (var token in json["result"][0].Values<T>())
            {
                array.Add(token);
            }

            result.Invoke(array.ToArray());
        }

        /// <summary>
        /// For response which has multiple same type values in parallel.
        /// </summary>
        /// <typeparam name="T">Type of the values</typeparam>
        /// <param name="jString"></param>
        /// <param name="num">Number of the values contained</param>
        /// <param name="error"></param>
        /// <param name="result"></param>
        internal static void HandleParallelValues<T>(string jString, int num, Action<int> error, Action<T[]> result)
        {
            var json = JObject.Parse(jString);
            if (HandleError(json, error))
            {
                return;
            }

            var results = json["result"];
            var array = new T[num];
            for (int i = 0; i < num; i++)
            {
                array[i] = results.Value<T>(i);
            }

            result.Invoke(array);
        }

        /// <summary>
        /// For response which has a single value and a single array.
        /// </summary>
        /// <typeparam name="T">Type of the values</typeparam>
        /// <param name="jString"></param>
        /// <param name="error"></param>
        /// <param name="result"></param>
        internal static void HandleBasicInfo<T>(string jString, Action<int> error, Action<BasicInfo<T>> result)
        {
            var json = JObject.Parse(jString);
            if (HandleError(json, error))
            {
                return;
            }

            var _candidates = new List<T>();
            foreach (var token in json["result"][1].Values<T>())
            {
                _candidates.Add(token);
            }

            result.Invoke(new BasicInfo<T> { current = json["result"].Value<T>(0), candidates = _candidates.ToArray() });
        }
    }
}
