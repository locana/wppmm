using System;
using System.Diagnostics;
using Kazyx.RemoteApi;
using Kazyx.WPMMM.Resources;

namespace Kazyx.WPPMM.Utils
{
    public class SettingsValueConverter
    {
        public static int GetSelectedIndex(Capability<int> info)
        {
            if (info == null || info.candidates == null || info.candidates.Length == 0)
            {
                return 0;
            }
            for (int i = 0; i < info.candidates.Length; i++)
            {
                if (info.candidates[i] == info.current)
                {
                    return i;
                }
            }
            return 0;
        }

        public static int GetSelectedIndex(Capability<string> info)
        {
            if (info == null || info.candidates == null || info.candidates.Length == 0)
            {
                return 0;
            }
            for (int i = 0; i < info.candidates.Length; i++)
            {
                if (info.candidates[i] == info.current)
                {
                    return i;
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
            if (info == null || info.candidates == null || info.candidates.Length == 0)
            {
                return new Capability<string>
                {
                    candidates = new string[] { AppResources.Disabled },
                    current = AppResources.Disabled
                };
            }
            var mCandidates = new string[info.candidates.Length];
            for (int i = 0; i < info.candidates.Length; i++)
            {
                mCandidates[i] = FromSelfTimer(info.candidates[i]);
            }
            return new Capability<string>
            {
                current = FromSelfTimer(info.current),
                candidates = mCandidates
            };
        }

        private static string FromSelfTimer(int val)
        {
            if (val == 0) { return AppResources.Off; }
            else { return val + AppResources.Seconds; }
        }

        public static Capability<string> FromPostViewSize(Capability<string> info)
        {
            if (info == null || info.candidates == null || info.candidates.Length == 0)
            {
                return new Capability<string>
                {
                    candidates = new string[] { AppResources.Disabled },
                    current = AppResources.Disabled
                };
            }
            var mCandidates = new string[info.candidates.Length];
            for (int i = 0; i < info.candidates.Length; i++)
            {
                mCandidates[i] = FromPostViewSize(info.candidates[i]);
            }
            return new Capability<string>
            {
                current = FromPostViewSize(info.current),
                candidates = mCandidates
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
            if (info == null || info.candidates == null || info.candidates.Length == 0)
            {
                return new Capability<string>
                {
                    candidates = new string[] { AppResources.Disabled },
                    current = AppResources.Disabled
                };
            }
            var mCandidates = new string[info.candidates.Length];
            for (int i = 0; i < info.candidates.Length; i++)
            {
                mCandidates[i] = FromShootMode(info.candidates[i]);
            }
            return new Capability<string>
            {
                current = FromShootMode(info.current),
                candidates = mCandidates
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
                default:
                    return val;
            }
        }

        public static Capability<string> FromExposureMode(Capability<string> info)
        {
            if (info == null || info.candidates == null || info.candidates.Length == 0)
            {
                return new Capability<string>
                {
                    candidates = new string[] { AppResources.Disabled },
                    current = AppResources.Disabled
                };
            }

            var mCandidates = new string[info.candidates.Length];
            for (int i = 0; i < info.candidates.Length; i++)
            {
                mCandidates[i] = FromExposureMode(info.candidates[i]);
            }
            return new Capability<string>
            {
                current = FromExposureMode(info.current),
                candidates = mCandidates
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
    }
}
