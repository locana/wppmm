using WPPMM.RemoteApi;

namespace WPPMM.Utils
{
    public class SettingsValueConverter
    {
        public static BasicInfo<string> FromSelfTimer(BasicInfo<int> info)
        {
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
                case ApiParams.PostImg2M:
                    return Resources.AppResources.Size2M;
                case ApiParams.PostImgOriginal:
                    return Resources.AppResources.SizeOriginal;
                default:
                    return val;
            }
        }

        public static BasicInfo<string> FromShootMode(BasicInfo<string> info)
        {
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
                case ApiParams.ShootModeMovie:
                    return Resources.AppResources.ShootModeMovie;
                case ApiParams.ShootModeStill:
                    return Resources.AppResources.ShootModeStill;
                default:
                    return val;
            }
        }
    }
}
