using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace WPPMM.RemoteApi
{
    internal class BasicResultHandler
    {
        protected static bool HandleError(JObject json, Action<int> error)
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

        protected static void NoValueAction(string jString, Action<int> error, Action result)
        {
            var json = JObject.Parse(jString);
            if (HandleError(json, error))
            {
                return;
            }

            result.Invoke();
        }

        protected static void StringAction(string jString, Action<int> error, Action<string> result)
        {
            var json = JObject.Parse(jString);
            if (HandleError(json, error))
            {
                return;
            }

            result.Invoke(json["result"][0].ToString());
        }

        protected static void StringsArrayAction(string jString, Action<int> error, Action<string[]> result)
        {
            var json = JObject.Parse(jString);
            if (HandleError(json, error))
            {
                return;
            }

            var strings = new List<string>();
            foreach (var token in json["result"][0].Values<string>())
            {
                strings.Add(token);
            }

            result.Invoke(strings.ToArray());
        }

        protected static void IntegerAction(string jString, Action<int> error, Action<int> result)
        {
            var json = JObject.Parse(jString);
            if (HandleError(json, error))
            {
                return;
            }

            result.Invoke(json["result"].Value<int>(0));
        }

        protected static void IntegerArrayAction(string jString, Action<int> error, Action<int[]> result)
        {
            var json = JObject.Parse(jString);
            if (HandleError(json, error))
            {
                return;
            }

            var integers = new List<int>();
            foreach (var token in json["result"][0].Values<int>())
            {
                integers.Add(token);
            }

            result.Invoke(integers.ToArray());
        }

        protected static void String_StringArrayAction(string jString, Action<int> error, Action<string, string[]> result)
        {
            var json = JObject.Parse(jString);
            if (HandleError(json, error))
            {
                return;
            }

            var strings = new List<string>();
            foreach (var token in json["result"][1].Values<string>())
            {
                strings.Add(token);
            }

            result.Invoke(json["result"].Value<string>(0), strings.ToArray());
        }

        protected static void Int_IntArrayAction(string jString, Action<int> error, Action<int, int[]> result)
        {
            var json = JObject.Parse(jString);
            if (HandleError(json, error))
            {
                return;
            }

            var integers = new List<int>();
            foreach (var token in json["result"][1].Values<int>())
            {
                integers.Add(token);
            }

            result.Invoke(json["result"].Value<int>(0), integers.ToArray());
        }

        protected static void String_StringAction(string jString, Action<int> error, Action<string, string> result)
        {
            var json = JObject.Parse(jString);
            if (HandleError(json, error))
            {
                return;
            }

            var results = json["result"];
            result.Invoke(results.Value<string>(0), results.Value<string>(1));
        }
    }
}
