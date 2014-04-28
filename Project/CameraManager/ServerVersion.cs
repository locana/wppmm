using System;
using System.Diagnostics;

namespace Kazyx.WPPMM.CameraManager
{
    public class ServerVersion
    {
        readonly uint major;
        readonly uint minor;
        readonly uint release;

        public static ServerVersion CreateDefault()
        {
            return new ServerVersion();
        }

        private ServerVersion()
        {
            major = 2;
            minor = 0;
            release = 0;
        }

        public ServerVersion(string version)
        {
            var sepa = version.Split('.');
            if (sepa.Length != 3)
            {
                throw new ArgumentException(version + " is invalid version name.");
            }
            try
            {
                major = uint.Parse(sepa[0]);
                minor = uint.Parse(sepa[1]);
                release = uint.Parse(sepa[2]);
                Debug.WriteLine("ServerVersion: " + version);
                if (IsLiberated)
                {
                    Debug.WriteLine("This is liberated version!!");
                }
                else
                {
                    Debug.WriteLine("This is restricted version...");
                }
            }
            catch (Exception)
            {
                throw new ArgumentException(version + " is invalid version name.");
            }
        }

        private bool CheckLiberated()
        {
            if (major == 2)
            {
                if (minor == 0)
                {
                    return release >= 1;
                }
                return true;
            }
            return major > 2;
        }

        private bool? _IsLeberated;

        public bool IsLiberated
        {
            get
            {
                if (_IsLeberated == null)
                {
                    _IsLeberated = CheckLiberated();
                }
                return (bool)_IsLeberated;
            }
        }
    }
}
