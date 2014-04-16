using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace WPPMM.RemoteApi
{
    /// <summary>
    /// This class provides function to parse response JSON string and invoke proper callback.
    /// </summary>
    /// <remarks>
    /// Error callback include error code as an argument.
    /// <see cref="WPPMM.RemoteApi.StatusCode"/>
    /// </remarks>
    ///
    internal class ResultHandler
    {
        internal static void HandleGetSupportedExposureCompensation(string jString, Action<int> error, Action<EvRange[]> result)
        {
            var json = JObject.Parse(jString);
            if (BasicResultHandler.HandleError(json, error))
            {
                return;
            }

            var maxlist = new List<int>();
            foreach (int max in json["result"][0].Values<int>())
            {
                maxlist.Add(max);
            }

            var minlist = new List<int>();
            foreach (int min in json["result"][1].Values<int>())
            {
                minlist.Add(min);
            }

            var deflist = new List<int>();
            foreach (int def in json["result"][2].Values<int>())
            {
                deflist.Add(def);
            }

            if (maxlist.Count != minlist.Count || minlist.Count != deflist.Count)
            {
                error.Invoke(StatusCode.IllegalResponse);
            }
            var tmp = new List<EvRange>();
            for (int i = 0; i < maxlist.Count; i++)
            {
                tmp.Add(new EvRange { IndexStep = EvConverter.GetDefinition(deflist[i]), MaxIndex = maxlist[i], MinIndex = minlist[i] });
            }
            result.Invoke(tmp.ToArray());
        }

        internal static void HandleSetTouchAFPosition(string jString, Action<int> error, Action<SetAFResult> result)
        {
            var json = JObject.Parse(jString);
            if (BasicResultHandler.HandleError(json, error))
            {
                return;
            }

            var res = json["result"][1];
            result.Invoke(new SetAFResult
            {
                Focused = res.Value<bool>("AFResult"),
                Mode = res.Value<string>("AFType")
            });
        }

        internal static void HandleGetApplicationInfo(string jString, Action<int> error, Action<ApplicationInfo> result)
        {
            BasicResultHandler.HandleParallelValues<string>(jString, 2, error,
                (array) => { result.Invoke(new ApplicationInfo { name = array[0], version = array[1] }); });
        }

        internal static void HandleGetMethodTypes(string jString, Action<int> error, Action<MethodType[]> result)
        {
            var json = JObject.Parse(jString);
            if (BasicResultHandler.HandleError(json, error))
            {
                return;
            }

            var method_types = new List<MethodType>();
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
                method_types.Add(new MethodType()
                {
                    name = token.Value<string>(0),
                    reqtypes = req.ToArray(),
                    restypes = res.ToArray(),
                    version = token.Value<string>(3)
                });
            }
            result.Invoke(method_types.ToArray());
        }

        internal static void HandleGetEvent(string jString, Action<int> error, Action<Event> result)
        {
            var json = JObject.Parse(jString);
            if (BasicResultHandler.HandleError(json, error))
            {
                return;
            }

            var jResult = json["result"];

            var jApi = jResult[0];
            string[] apis = null;
            if (jApi.HasValues)
            {
                var apilist = new List<string>();
                foreach (var str in jApi["names"].Values<string>())
                {
                    apilist.Add(str);
                }
                apis = apilist.ToArray();
            }

            var jStatus = jResult[1];
            string status = null;
            if (jStatus.HasValues)
            {
                status = jStatus.Value<string>("cameraStatus");
            }

            var jZoom = jResult[2];
            ZoomInfo zoom = null;
            if (jZoom.HasValues)
            {
                zoom = new ZoomInfo
                {
                    position = jZoom.Value<int>("zoomPosition"),
                    number_of_boxes = jZoom.Value<int>("zoomNumberBox"),
                    current_box_index = jZoom.Value<int>("zoomIndexCurrentBox"),
                    position_in_current_box = jZoom.Value<int>("zoomPositionCurrentBox")
                };
            }

            var jLiveview = jResult[3];
            bool liveview_status = false;
            if (jLiveview.HasValues)
            {
                jLiveview.Value<bool>("liveviewStatus");
            }

            var jExposureMode = jResult[18];
            BasicInfo<string> exposure = null;
            if (jExposureMode.HasValues)
            {
                var modecandidates = new List<string>();
                foreach (var str in jExposureMode["exposureModeCandidates"].Values<string>())
                {
                    modecandidates.Add(str);
                }
                exposure = new BasicInfo<string>
                {
                    current = jExposureMode.Value<string>("currentExposureMode"),
                    candidates = modecandidates.ToArray()
                };
            }

            var jPostView = jResult[19];
            BasicInfo<string> postview = null;
            if (jPostView.HasValues)
            {
                var pvcandidates = new List<string>();
                foreach (var str in jPostView["postviewImageSizeCandidates"].Values<string>())
                {
                    pvcandidates.Add(str);
                }
                postview = new BasicInfo<string>
                {
                    current = jPostView.Value<string>("currentPostviewImageSize"),
                    candidates = pvcandidates.ToArray()
                };
            }

            var jSelfTimer = jResult[20];
            BasicInfo<int> selftimer = null;
            if (jSelfTimer.HasValues)
            {
                var stcandidates = new List<int>();
                foreach (var str in jSelfTimer["selfTimerCandidates"].Values<int>())
                {
                    stcandidates.Add(str);
                }
                selftimer = new BasicInfo<int>
                {
                    current = jSelfTimer.Value<int>("currentSelfTimer"),
                    candidates = stcandidates.ToArray()
                };
            }

            var jShootMode = jResult[21];
            BasicInfo<string> shootmode = null;
            if (jShootMode.HasValues)
            {
                var smcandidates = new List<string>();
                foreach (var str in jShootMode["shootModeCandidates"].Values<string>())
                {
                    smcandidates.Add(str);
                }
                shootmode = new BasicInfo<string>
                {
                    current = jShootMode.Value<string>("currentShootMode"),
                    candidates = smcandidates.ToArray()
                };
            }

            var jEV = jResult[25];
            EvInfo ev = null;
            if (jEV.HasValues)
            {
                ev = new EvInfo
                {
                    CurrentIndex = jEV.Value<int>("currentExposureCompensation"),
                    Range = new EvRange
                    {
                        MaxIndex = jEV.Value<int>("maxExposureCompensation"),
                        MinIndex = jEV.Value<int>("minExposureCompensation"),
                        IndexStep = EvConverter.GetDefinition(jEV.Value<int>("stepIndexOfExposureCompensation"))
                    }
                };
            }

            var jFN = jResult[27];
            BasicInfo<string> fn = null;
            if (jFN.HasValues)
            {
                var fncandidates = new List<string>();
                foreach (var str in jFN["fNumberCandidates"].Values<string>())
                {
                    fncandidates.Add(str);
                }
                fn = new BasicInfo<string>
                {
                    current = jFN.Value<string>("currentFNumber"),
                    candidates = fncandidates.ToArray()
                };
            }

            var jIso = jResult[29];
            BasicInfo<string> iso = null;
            if (jIso.HasValues)
            {
                var isocandidates = new List<string>();
                foreach (var str in jIso["isoSpeedRateCandidates"].Values<string>())
                {
                    isocandidates.Add(str);
                }
                iso = new BasicInfo<string>
                {
                    current = jIso.Value<string>("currentIsoSpeedRate"),
                    candidates = isocandidates.ToArray()
                };
            }

            var jPS = jResult[31];
            bool? ps = null;
            if (jPS.HasValues)
            {
                ps = jPS.Value<bool>("isShifted");
            }

            var jSS = jResult[32];
            BasicInfo<string> ss = null;
            if (jSS.HasValues)
            {
                var sscandidates = new List<string>();
                foreach (var str in jSS["shutterSpeedCandidates"].Values<string>())
                {
                    sscandidates.Add(str);
                }
                ss = new BasicInfo<string>
                {
                    current = jSS.Value<string>("currentShutterSpeed"),
                    candidates = sscandidates.ToArray()
                };
            }

            result.Invoke(new Event()
            {
                AvailableApis = apis,
                CameraStatus = status,
                ZoomInfo = zoom,
                LiveviewAvailable = liveview_status,
                PostviewSizeInfo = postview,
                SelfTimerInfo = selftimer,
                ShootModeInfo = shootmode,
                FNumber = fn,
                ISOSpeedRate = iso,
                ShutterSpeed = ss,
                EvInfo = ev,
                ExposureMode = exposure,
                ProgramShiftActivated = ps
            });
        }
    }
}
