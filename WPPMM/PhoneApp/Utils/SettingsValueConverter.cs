using WPPMM.RemoteApi;

namespace WPPMM.Utils
{
    public class SettingsValueConverter
    {
        public static int GetSelectedIndex(BasicInfo<int> info)
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

        public static int GetSelectedIndex(BasicInfo<string> info)
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

        public static BasicInfo<string> FromSelfTimer(BasicInfo<int> info)
        {
            if (info == null || info.candidates == null || info.candidates.Length == 0)
            {
                return new BasicInfo<string>
                {
                    candidates = new string[] { Resources.AppResources.Disabled },
                    current = Resources.AppResources.Disabled
                };
            }
            var mCandidates = new string[info.candidates.Length];
            for (int i = 0; i < info.candidates.Length; i++)
            {
                mCandidates[i] = FromSelfTimer(info.candidates[i]);
            }
            return new BasicInfo<string>
            {
                current = FromSelfTimer(info.current),
                candidates = mCandidates
            };
        }

        private static string FromSelfTimer(int val)
        {
            if (val == 0) { return Resources.AppResources.Off; }
            else { return val + Resources.AppResources.Seconds; }
        }

        public static BasicInfo<string> FromPostViewSize(BasicInfo<string> info)
        {
            if (info == null || info.candidates == null || info.candidates.Length == 0)
            {
                return new BasicInfo<string>
                {
                    candidates = new string[] { Resources.AppResources.Disabled },
                    current = Resources.AppResources.Disabled
                };
            }
            var mCandidates = new string[info.candidates.Length];
            for (int i = 0; i < info.candidates.Length; i++)
            {
                mCandidates[i] = FromPostViewSize(info.candidates[i]);
            }
            return new BasicInfo<string>
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
                    return Resources.AppResources.Size2M;
                case PostviewSizeParam.Original:
                    return Resources.AppResources.SizeOriginal;
                default:
                    return val;
            }
        }

        public static BasicInfo<string> FromShootMode(BasicInfo<string> info)
        {
            if (info == null || info.candidates == null || info.candidates.Length == 0)
            {
                return new BasicInfo<string>
                {
                    candidates = new string[] { Resources.AppResources.Disabled },
                    current = Resources.AppResources.Disabled
                };
            }
            var mCandidates = new string[info.candidates.Length];
            for (int i = 0; i < info.candidates.Length; i++)
            {
                mCandidates[i] = FromShootMode(info.candidates[i]);
            }
            return new BasicInfo<string>
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
                    return Resources.AppResources.ShootModeMovie;
                case ShootModeParam.Still:
                    return Resources.AppResources.ShootModeStill;
                case ShootModeParam.Audio:
                    return Resources.AppResources.ShootModeAudio;
                default:
                    return val;
            }
        }
    }
}
