using Kazyx.RemoteApi;
using Kazyx.WPMMM.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Kazyx.WPPMM.Utils
{
    public class SettingsValueConverter
    {
        public static int GetSelectedIndex<T>(Capability<T> info)
        {
            if (info == null || info.Candidates == null || info.Candidates.Length == 0)
            {
                return 0;
            }
            if (typeof(T) == typeof(string) || typeof(T) == typeof(int))
            {
                for (int i = 0; i < info.Candidates.Length; i++)
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
                for (int i = 0; i < info.Candidates.Length; i++)
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

            var mCandidates = new string[info.Candidates.Length];
            for (int i = 0; i < info.Candidates.Length; i++)
            {
                mCandidates[i] = FromSelfTimer(info.Candidates[i]);
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

            var mCandidates = new string[info.Candidates.Length];
            for (int i = 0; i < info.Candidates.Length; i++)
            {
                mCandidates[i] = FromPostViewSize(info.Candidates[i]);
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

            var mCandidates = new string[info.Candidates.Length];
            for (int i = 0; i < info.Candidates.Length; i++)
            {
                mCandidates[i] = FromShootMode(info.Candidates[i]);
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

            var mCandidates = new string[info.Candidates.Length];
            for (int i = 0; i < info.Candidates.Length; i++)
            {
                mCandidates[i] = FromExposureMode(info.Candidates[i]);
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

            var mCandidates = new string[info.Candidates.Length];
            for (int i = 0; i < info.Candidates.Length; i++)
            {
                mCandidates[i] = FromSteadyMode(info.Candidates[i]);
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

            var mCandidates = new string[info.Candidates.Length];
            for (int i = 0; i < info.Candidates.Length; i++)
            {
                mCandidates[i] = FromBeepMode(info.Candidates[i]);
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

            var mCandidates = new string[info.Candidates.Length];
            for (int i = 0; i < info.Candidates.Length; i++)
            {
                mCandidates[i] = FromViewAngle(info.Candidates[i]);
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

            var mCandidates = new string[info.Candidates.Length];
            for (int i = 0; i < info.Candidates.Length; i++)
            {
                mCandidates[i] = FromMovieQuality(info.Candidates[i]);
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
                Candidates = mCandidates.ToArray()
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

            var mCandidates = new string[info.Candidates.Length];
            for (int i = 0; i < info.Candidates.Length; i++)
            {
                mCandidates[i] = FromWhiteBalance(info.Candidates[i]);
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
            }
            return val;
        }

        private static Capability<string> AsDisabledCapability<T>(Capability<T> info)
        {
            if (info == null || info.Candidates == null || info.Candidates.Length == 0)
            {
                return new Capability<string>
                {
                    Candidates = new string[] { AppResources.Disabled },
                    Current = AppResources.Disabled
                };
            }
            return null;
        }

        public static string[] FromExposureCompensation(EvCapability info)
        {
            if (info == null)
            {
                Debug.WriteLine("Return null.");
                return new string[] { AppResources.Disabled };
            }

            int num = info.Candidate.MaxIndex + Math.Abs(info.Candidate.MinIndex) + 1;
            var mCandidates = new string[num];
            for (int i = 0; i < num; i++)
            {
                Debug.WriteLine("ev: " + i);
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

            var mCandidates = new string[info.Candidates.Length];
            for (int i = 0; i < info.Candidates.Length; i++)
            {
                mCandidates[i] = FromFlashMode(info.Candidates[i]);
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

    }
}
