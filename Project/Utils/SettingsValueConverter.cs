using Kazyx.RemoteApi;
using Kazyx.RemoteApi.Camera;
using Kazyx.WPMMM.Resources;
using Kazyx.WPMMM.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Kazyx.WPPMM.Utils
{
    public class SettingsValueConverter
    {
        public static int GetSelectedIndex<T>(Capability<T> info)
        {
            if (info == null || info.Candidates == null || info.Candidates.Count == 0)
            {
                return 0;
            }
            if (typeof(T) == typeof(string) || typeof(T) == typeof(int))
            {
                for (int i = 0; i < info.Candidates.Count; i++)
                {
                    if (info.Candidates[i].Equals(info.Current))
                    {
                        return i;
                    }
                }
            }
            else if (typeof(T) == typeof(StillImageSize))
            {
                var size = info as Capability<StillImageSize>;
                for (int i = 0; i < info.Candidates.Count; i++)
                {
                    if (size.Candidates[i].AspectRatio == size.Current.AspectRatio
                        && size.Candidates[i].SizeDefinition == size.Current.SizeDefinition)
                    {
                        return i;
                    }
                }
            }
            return 0;
        }

        public static int GetSelectedIndex(EvCapability info)
        {
            if (info == null || info.Candidate == null)
            {
                return 0;
            }
            return info.CurrentIndex;
        }

        public static Capability<string> FromSelfTimer(Capability<int> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromSelfTimer(val));
            }
            return new Capability<string>
            {
                Current = FromSelfTimer(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromSelfTimer(int val)
        {
            if (val == 0) { return AppResources.Off; }
            else { return val + AppResources.Seconds; }
        }

        public static Capability<string> FromPostViewSize(Capability<string> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromPostViewSize(val));
            }
            return new Capability<string>
            {
                Current = FromPostViewSize(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromPostViewSize(string val)
        {
            switch (val)
            {
                case PostviewSizeParam.Px2M:
                    return AppResources.Size2M;
                case PostviewSizeParam.Original:
                    return AppResources.SizeOriginal;
                default:
                    return val;
            }
        }

        public static Capability<string> FromShootMode(Capability<string> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromShootMode(val));
            }
            return new Capability<string>
            {
                Current = FromShootMode(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromShootMode(string val)
        {
            switch (val)
            {
                case ShootModeParam.Movie:
                    return AppResources.ShootModeMovie;
                case ShootModeParam.Still:
                    return AppResources.ShootModeStill;
                case ShootModeParam.Audio:
                    return AppResources.ShootModeAudio;
                case ShootModeParam.Interval:
                    return AppResources.ShootModeIntervalStill;
                default:
                    return val;
            }
        }

        public static Capability<string> FromExposureMode(Capability<string> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromExposureMode(val));
            }
            return new Capability<string>
            {
                Current = FromExposureMode(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromExposureMode(string val)
        {
            switch (val)
            {
                case ExposureMode.Aperture:
                    return AppResources.ExposureMode_A;
                case ExposureMode.SS:
                    return AppResources.ExposureMode_S;
                case ExposureMode.Program:
                    return AppResources.ExposureMode_P;
                case ExposureMode.Superior:
                    return AppResources.ExposureMode_sA;
                case ExposureMode.Manual:
                    return AppResources.ExposureMode_M;
                case ExposureMode.Intelligent:
                    return AppResources.ExposureMode_iA;
                default:
                    return val;
            }
        }

        public static Capability<string> FromSteadyMode(Capability<string> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromSteadyMode(val));
            }
            return new Capability<string>
            {
                Current = FromSteadyMode(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromSteadyMode(string val)
        {
            switch (val)
            {
                case SteadyMode.On:
                    return AppResources.On;
                case SteadyMode.Off:
                    return AppResources.Off;
                default:
                    return val;
            }
        }

        public static Capability<string> FromBeepMode(Capability<string> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromBeepMode(val));
            }
            return new Capability<string>
            {
                Current = FromBeepMode(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromBeepMode(string val)
        {
            switch (val)
            {
                case BeepMode.On:
                    return AppResources.On;
                case BeepMode.Silent:
                    return AppResources.Off;
                case BeepMode.Shutter:
                    return AppResources.BeepModeShutterOnly;
                default:
                    return val;
            }
        }

        public static Capability<string> FromViewAngle(Capability<int> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromViewAngle(val));
            }
            return new Capability<string>
            {
                Current = FromViewAngle(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromViewAngle(int val)
        {
            return val + AppResources.ViewAngleUnit;
        }

        public static Capability<string> FromMovieQuality(Capability<string> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var cand in info.Candidates)
            {
                mCandidates.Add(FromMovieQuality(cand));
            }

            return new Capability<string>
            {
                Current = FromMovieQuality(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromMovieQuality(string p)
        {
            return p;
        }

        public static Capability<string> FromStillImageSize(Capability<StillImageSize> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromStillImageSize(val));
            }
            return new Capability<string>
            {
                Current = FromStillImageSize(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromStillImageSize(StillImageSize val)
        {
            return val.SizeDefinition + " (" + val.AspectRatio + ")";
        }

        private static readonly char[] StillImageSizeIndicators = { '(', ')' };

        public static StillImageSize ToStillImageSize(string val)
        {
            var array = val.Split(StillImageSizeIndicators);
            if (array == null || array.Length != 2)
            {
                throw new ArgumentException("Failed to convert " + val + " to StillImageSize");
            }
            return new StillImageSize
            {
                AspectRatio = array[1].Trim(),
                SizeDefinition = array[2].Trim()
            };
        }

        public static Capability<string> FromWhiteBalance(Capability<string> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromWhiteBalance(val));
            }
            return new Capability<string>
            {
                Current = FromWhiteBalance(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromWhiteBalance(string val)
        {
            switch (val)
            {
                case WhiteBalanceMode.Fluorescent_WarmWhite:
                    return AppResources.WB_Fluorescent_WarmWhite;
                case WhiteBalanceMode.Fluorescent_CoolWhite:
                    return AppResources.WB_Fluorescent_CoolWhite;
                case WhiteBalanceMode.Fluorescent_DayLight:
                    return AppResources.WB_Fluorescent_DayLight;
                case WhiteBalanceMode.Fluorescent_DayWhite:
                    return AppResources.WB_Fluorescent_DayWhite;
                case WhiteBalanceMode.Incandescent:
                    return AppResources.WB_Incandescent;
                case WhiteBalanceMode.Shade:
                    return AppResources.WB_Shade;
                case WhiteBalanceMode.Auto:
                    return AppResources.WB_Auto;
                case WhiteBalanceMode.Cloudy:
                    return AppResources.WB_Cloudy;
                case WhiteBalanceMode.DayLight:
                    return AppResources.WB_DayLight;
                case WhiteBalanceMode.Manual:
                    return AppResources.WB_ColorTemperture;
                case WhiteBalanceMode.Flash:
                    return AppResources.WB_Flash;
                case WhiteBalanceMode.Custom:
                    return AppResources.WB_Custom;
                case WhiteBalanceMode.Custom_1:
                    return AppResources.WB_Custom1;
                case WhiteBalanceMode.Custom_2:
                    return AppResources.WB_Custom2;
                case WhiteBalanceMode.Custom_3:
                    return AppResources.WB_Custom3;
            }
            return val;
        }

        private static Capability<string> AsDisabledCapability<T>(Capability<T> info)
        {
            if (info == null || info.Candidates == null || info.Candidates.Count == 0)
            {
                var list = new List<string>();
                list.Add(AppResources.Disabled);

                return new Capability<string>
                {
                    Candidates = list,
                    Current = AppResources.Disabled
                };
            }
            return null;
        }

        public static string[] FromExposureCompensation(EvCapability info)
        {
            if (info == null)
            {
                DebugUtil.Log("Return null.");
                return new string[] { AppResources.Disabled };
            }

            int num = info.Candidate.MaxIndex + Math.Abs(info.Candidate.MinIndex) + 1;
            var mCandidates = new string[num];
            for (int i = 0; i < num; i++)
            {
                DebugUtil.Log("ev: " + i);
                mCandidates[i] = FromExposureCompensation(i + info.Candidate.MinIndex, info.Candidate.IndexStep);
            }

            return mCandidates;
        }

        private static string FromExposureCompensation(int index, EvStepDefinition def)
        {
            var value = EvConverter.GetEv(index, def);
            var strValue = Math.Round(value, 1, MidpointRounding.AwayFromZero).ToString("0.0");

            if (value <= 0)
            {
                return "EV " + strValue;
            }
            else
            {
                return "EV +" + strValue;
            }
        }

        public static Capability<string> FromFlashMode(Capability<string> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromFlashMode(val));
            }
            return new Capability<string>
            {
                Current = FromFlashMode(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromFlashMode(string val)
        {
            switch (val)
            {
                case FlashMode.Auto:
                    return AppResources.FlashMode_Auto;
                case FlashMode.On:
                    return AppResources.On;
                case FlashMode.Off:
                    return AppResources.Off;
                case FlashMode.RearSync:
                    return AppResources.FlashMode_RearSync;
                case FlashMode.SlowSync:
                    return AppResources.FlashMode_SlowSync;
                case FlashMode.Wireless:
                    return AppResources.FlashMode_Wireless;
            }
            return val;
        }

        public static Capability<string> FromFocusMode(Capability<string> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromFocusMode(val));
            }
            return new Capability<string>
            {
                Current = FromFocusMode(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromFocusMode(string val)
        {
            switch (val)
            {
                case FocusMode.Continuous:
                    return AppResources.FocusMode_AFC;
                case FocusMode.Single:
                    return AppResources.FocusMode_AFS;
                case FocusMode.Manual:
                    return AppResources.FocusMode_Manual;
            }
            return val;
        }


        internal static Capability<string> FromZoomSetting(Capability<string> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
            {
                DebugUtil.Log("[FromZoomSetting] returns null");
                return res;
            }

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromZoomSetting(val));
            }
            return new Capability<string>
            {
                Current = FromZoomSetting(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromZoomSetting(string val)
        {
            switch (val)
            {
                case ZoomMode.ClearImageDigital:
                    return AppResources.ZoomMode_ClearImageDigital;
                case ZoomMode.Optical:
                    return AppResources.ZoomMode_Optical;
            }
            return val;
        }

        internal static Capability<string> FromImageQuality(Capability<string> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromImageQuality(val));
            }
            return new Capability<string>
            {
                Current = FromImageQuality(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromImageQuality(string val)
        {
            switch (val)
            {
                case ImageQuality.RawAndJpeg:
                    return AppResources.ImageQuality_RawAndJpeg;
                case ImageQuality.Fine:
                    return AppResources.ImageQuality_Fine;
                case ImageQuality.Standard:
                    return AppResources.ImageQuality_Standard;
            }
            return val;
        }

        internal static Capability<string> FromContShootingMode(Capability<string> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromContShootingMode(val));
            }
            return new Capability<string>
            {
                Current = FromContShootingMode(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromContShootingMode(string val)
        {
            switch (val)
            {
                case ContinuousShootMode.Single:
                    return AppResources.ContinuousShootMode_Single;
                case ContinuousShootMode.Cont:
                    return AppResources.ContinuousShootMode_Cont;
                case ContinuousShootMode.SpeedPriority:
                    return AppResources.ContinuousShootMode_SpeedPriority;
                case ContinuousShootMode.Burst:
                    return AppResources.ContinuousShootMode_Burst;
                case ContinuousShootMode.MotionShot:
                    return AppResources.ContinuousShootMode_MotionShot;
            }
            return val;
        }

        internal static Capability<string> FromContShootingSpeed(Capability<string> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromContShootingSpeed(val));
            }
            return new Capability<string>
            {
                Current = FromContShootingSpeed(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromContShootingSpeed(string val)
        {
            switch(val){
                case ContinuousShootSpeed.FixedFrames_10_In_1_25Sec:
                    return AppResources.ContinuousShootSpeed_FixedFrames_10_In_1_25Sec;
                case ContinuousShootSpeed.FixedFrames_10_In_2Sec:
                    return AppResources.ContinuousShootSpeed_FixedFrames_10_In_2Sec;
                case ContinuousShootSpeed.FixedFrames_10_In_5Sec:
                    return AppResources.ContinuousShootSpeed_FixedFrames_10_In_5Sec;
                case ContinuousShootSpeed.High:
                    return AppResources.ContinuousShootSpeed_High;
                case ContinuousShootSpeed.Low:
                    return AppResources.ContinuousShootSpeed_Low;
            }
            return val;
        }

        internal static Capability<string> FromFlipMode(Capability<string> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromFlipMode(val));
            }
            return new Capability<string>
            {
                Current = FromFlipMode(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromFlipMode(string val)
        {
            switch (val)
            {
                case FlipMode.On:
                    return AppResources.On;
                case FlipMode.Off:
                    return AppResources.Off;
            }
            return val;
        }

        internal static Capability<string> FromSceneSelection(Capability<string> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromSceneSelection(val));
            }
            return new Capability<string>
            {
                Current = FromSceneSelection(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromSceneSelection(string val)
        {
            switch (val)
            {
                case Scene.Normal:
                    return AppResources.Scene_Normal;
                case Scene.UnderWater:
                    return AppResources.Scene_UnderWater;
            }
            return val;
        }

        internal static Capability<string> FromIntervalTime(Capability<string> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromIntervalTime(val));
            }
            return new Capability<string>
            {
                Current = FromIntervalTime(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromIntervalTime(string val)
        {
            return val + " " + AppResources.Seconds;
        }

        internal static Capability<string> FromColorSetting(Capability<string> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromColorSetting(val));
            }
            return new Capability<string>
            {
                Current = FromColorSetting(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromColorSetting(string val)
        {
            switch (val)
            {
                case ColorMode.Neutral:
                    return AppResources.ColorMode_Neutral;
                case ColorMode.Vivid:
                    return AppResources.ColorMode_Vivid;
            }
            return val;
        }

        internal static Capability<string> FromMovieFormat(Capability<string> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromMovieFormat(val));
            }
            return new Capability<string>
            {
                Current = FromMovieFormat(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromMovieFormat(string val)
        {
            switch (val)
            {
                case MovieFormatMode.MP4:
                    return AppResources.MovieFormatMode_MP4;
                case MovieFormatMode.XAVCS:
                    return AppResources.MovieFormatMode_XAVCS;
            }
            return val;
        }

        internal static Capability<string> FromIrRemoteControl(Capability<string> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromIrRemoteControl(val));
            }
            return new Capability<string>
            {
                Current = FromIrRemoteControl(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromIrRemoteControl(string val)
        {
            switch (val)
            {
                case IrRemoteSetting.On:
                    return AppResources.On;
                case IrRemoteSetting.Off:
                    return AppResources.Off;
            }
            return val;
        }

        internal static Capability<string> FromTvColorSystem(Capability<string> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromTvColorSystem(val));
            }
            return new Capability<string>
            {
                Current = FromTvColorSystem(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromTvColorSystem(string val)
        {
            switch (val)
            {
                case TvColorSystemMode.NTSC:
                    return AppResources.TvColorSystemMode_NTSC;
                case TvColorSystemMode.PAL:
                    return AppResources.TvColorSystemMode_PAL;
            }
            return val;
        }

        internal static Capability<string> FromTrackingFocusMode(Capability<string> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromTrackingFocusMode(val));
            }
            return new Capability<string>
            {
                Current = FromTrackingFocusMode(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromTrackingFocusMode(string val)
        {
            switch (val)
            {
                case TrackingFocusMode.On:
                    return AppResources.On;
                case TrackingFocusMode.Off:
                    return AppResources.Off;
            }
            return val;
        }

        internal static Capability<string> FromAutoPowerOff(Capability<int> info)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (var val in info.Candidates)
            {
                mCandidates.Add(FromAutoPowerOff(val));
            }
            return new Capability<string>
            {
                Current = FromAutoPowerOff(info.Current),
                Candidates = mCandidates
            };
        }

        private static string FromAutoPowerOff(int val)
        {
            return val + " " + AppResources.Seconds;
        }

        internal static string[] FromFramingGrid(string[] keys)
        {
            string[] names = new string[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                switch (keys[i])
                {
                    case FramingGridTypes.Off:
                        names[i] = AppResources.Off;
                        break;
                    case FramingGridTypes.RuleOfThirds:
                        names[i] = AppResources.Grid_RuleOfThirds;
                        break;
                    case FramingGridTypes.Diagonal:
                        names[i] = AppResources.Grid_Diagonal;
                        break;
                    case FramingGridTypes.Square:
                        names[i] = AppResources.Grid_Square;
                        break;
                    case FramingGridTypes.Crosshairs:
                        names[i] = AppResources.Grid_Crosshairs;
                        break;
                    case FramingGridTypes.Fibonacci:
                        names[i] = AppResources.Grid_Fibonacci;
                        break;
                    case FramingGridTypes.GoldenRatio:
                        names[i] = AppResources.Grid_GoldenRatio;
                        break;
                    default:
                        names[i] = keys[i];
                        break;
                }
            }
            return names;
        }

        internal static string[] FromFramingGridColor(string[] keys)
        {
            string[] names = new string[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                switch (keys[i])
                {
                    case FramingGridColor.White:
                        names[i] = AppResources.White;
                        break;
                    case FramingGridColor.Black:
                        names[i] = AppResources.Black;
                        break;
                    case FramingGridColor.Red:
                        names[i] = AppResources.Red;
                        break;
                    case FramingGridColor.Green:
                        names[i] = AppResources.Green;
                        break;
                    case FramingGridColor.Blue:
                        names[i] = AppResources.Blue;
                        break;
                    default:
                        names[i] = keys[i];
                        break;
                }
            }
            return names;
        }

        internal static string[] FromFibonacciLineOrigin(string[] keys)
        {
            string[] names = new string[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                switch (keys[i])
                {
                    case FibonacciLineOrigins.UpperLeft:
                        names[i] = AppResources.UpperLeft;
                        break;
                    case FibonacciLineOrigins.UpperRight:
                        names[i] = AppResources.UpperRight;
                        break;
                    case FibonacciLineOrigins.BottomLeft:
                        names[i] = AppResources.BottomLeft;
                        break;
                    case FibonacciLineOrigins.BottomRight:
                        names[i] = AppResources.BottomRight;
                        break;
                    default:
                        names[i] = keys[i];
                        break;
                }
            }
            return names;
        }
    }
}
