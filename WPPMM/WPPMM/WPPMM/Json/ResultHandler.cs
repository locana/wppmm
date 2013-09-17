using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace WPPMM.Json
{
    /// <summary>
    /// This class provides function to parse response JSON string and invoke proper callback.
    /// </summary>
    /// <remarks>
    /// Error callback include error code as an argument.
    /// <see cref="WPPMM.Json.StatusCode"/>
    /// </remarks>
    ///
    public class ResultHandler
    {
        public static void SetShootMode(string jString, Action<int> error, Action result)
        {
            BasicResultHandler.NoValueAction(jString, error, result);
        }

        public static void GetShootMode(string jString, Action<int> error, Action<string> result)
        {
            BasicResultHandler.StringAction(jString, error, result);
        }

        public static void GetSupportedShootMode(string jString, Action<int> error, Action<string[]> result)
        {
            BasicResultHandler.StringsArrayAction(jString, error, result);
        }

        public static void GetAvailableShootMode(string jString, Action<int> error, Action<string, string[]> result)
        {
            BasicResultHandler.String_StringArrayAction(jString, error, result);
        }

        public static void ActTakePicture(string jString, Action<int> error, Action<string[]> result)
        {
            BasicResultHandler.StringsArrayAction(jString, error, result);
        }

        public static void AwaitTakePicture(string jString, Action<int> error, Action<string[]> result)
        {
            BasicResultHandler.StringsArrayAction(jString, error, result);
        }

        public static void StartMovieRec(string jString, Action<int> error, Action result)
        {
            BasicResultHandler.NoValueAction(jString, error, result);
        }

        public static void StopMovieRec(string jString, Action<int> error, Action<string[]> result)
        {
            BasicResultHandler.StringsArrayAction(jString, error, result);
        }

        public static void StartLiveview(string jString, Action<int> error, Action<string> result)
        {
            BasicResultHandler.StringAction(jString, error, result);
        }

        public static void StopLiveview(string jString, Action<int> error, Action result)
        {
            BasicResultHandler.NoValueAction(jString, error, result);
        }

        public static void ActZoom(string jString, Action<int> error, Action result)
        {
            BasicResultHandler.NoValueAction(jString, error, result);
        }

        public static void SetSelfTimer(string jString, Action<int> error, Action result)
        {
            BasicResultHandler.NoValueAction(jString, error, result);
        }

        public static void GetSelfTimer(string jString, Action<int> error, Action<int> result)
        {
            BasicResultHandler.IntegerAction(jString, error, result);
        }

        public static void GetSupportedSelfTimer(string jString, Action<int> error, Action<int[]> result)
        {
            BasicResultHandler.IntegerArrayAction(jString, error, result);
        }

        public static void GetAvailableSelfTimer(string jString, Action<int> error, Action<int, int[]> result)
        {
            BasicResultHandler.Int_IntArrayAction(jString, error, result);
        }

        public static void SetPostviewImageSize(string jString, Action<int> error, Action result)
        {
            BasicResultHandler.NoValueAction(jString, error, result);
        }

        public static void GetPostviewImageSize(string jString, Action<int> error, Action<string> result)
        {
            BasicResultHandler.StringAction(jString, error, result);
        }

        public static void GetSupportedPostviewImageSize(string jString, Action<int> error, Action<string[]> result)
        {
            BasicResultHandler.StringsArrayAction(jString, error, result);
        }

        public static void GetAvailablePostviewImageSize(string jString, Action<int> error, Action<string, string[]> result)
        {
            BasicResultHandler.String_StringArrayAction(jString, error, result);
        }

        public static void StartRecMode(string jString, Action<int> error, Action result)
        {
            BasicResultHandler.NoValueAction(jString, error, result);
        }

        public static void StopRecMode(string jString, Action<int> error, Action result)
        {
            BasicResultHandler.NoValueAction(jString, error, result);
        }

        public static void GetAvailableApiList(string jString, Action<int> error, Action<string[]> result)
        {
            BasicResultHandler.StringsArrayAction(jString, error, result);
        }

        public static void GetApplicationInfo(string jString, Action<int> error, Action<string, string> result)
        {
            BasicResultHandler.String_StringAction(jString, error, result);
        }

        public static void GetVersions(string jString, Action<int> error, Action<string[]> result)
        {
            BasicResultHandler.StringsArrayAction(jString, error, result);
        }

        public static void GetMethodTypes(string jString, Action<int> error, MethodTypesHandler result)
        {
            var json = JObject.Parse(jString);
            if (BasicResultHandler.HandleError(json, error))
            {
                return;
            }

            foreach (var token in json["results"])
            {
                var req = new List<string>();
                foreach (var type in token[1].Values<string>())
                {
                    req.Add(type);
                }
                var res = new List<string>();
                foreach (var type in token[2].Values<string>())
                {
                    res.Add(type);
                }
                result.Invoke(token.Value<string>(0), req.ToArray(), res.ToArray(), token.Value<string>(3));
            }
        }

        public static void GetEvent(string jString, Action<int> error, GetEventHandler result)
        {
            var json = JObject.Parse(jString);
            if (BasicResultHandler.HandleError(json, error))
            {
                return;
            }

            var apilist = new List<string>();
            foreach (var str in json["result"][0]["names"].Values<string>())
            {
                apilist.Add(str);
            }

            var status = json["result"].Value<string>(1);

            var jZoom = json["result"][2];
            var zoom = new ZoomInfo
            {
                position = jZoom.Value<int>("zoomPosition"),
                number_of_boxes = jZoom.Value<int>("zoomNumberBox"),
                current_box_index = jZoom.Value<int>("zoomIndexCurrentBox"),
                position_in_current_box = jZoom.Value<int>("zoomPositionCurrentBox")
            };

            var liveview_status = json["result"].Value<bool>(3);

            var jPostView = json["result"][19];
            var pvcandidates = new List<string>();
            foreach (var str in jPostView["postviewImageSizeCandidates"].Values<string>())
            {
                pvcandidates.Add(str);
            }
            var postview = new StrStrArray
            {
                current = jPostView.Value<string>("currentPostviewImageSize"),
                candidates = pvcandidates.ToArray()
            };

            var jSelfTimer = json["result"][20];
            var stcandidates = new List<int>();
            foreach (var str in jSelfTimer["selfTimerCandidates"].Values<int>())
            {
                stcandidates.Add(str);
            }
            var selftimer = new IntIntArray
            {
                current = jSelfTimer.Value<int>("currentSelfTimer"),
                candidates = stcandidates.ToArray()
            };

            var jShootMode = json["result"][21];
            var smcandidates = new List<string>();
            foreach (var str in jShootMode["shootModeCandidates"].Values<string>())
            {
                smcandidates.Add(str);
            }
            var shootmode = new StrStrArray
            {
                current = jShootMode.Value<string>("currentShootMode"),
                candidates = pvcandidates.ToArray()
            };

            result.Invoke(apilist.ToArray(), status, zoom, liveview_status, postview, selftimer, shootmode);
        }
    }
}
