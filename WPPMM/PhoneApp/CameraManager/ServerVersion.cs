using System;
using System.Diagnostics;

namespace WPPMM.CameraManager
{
    public class ServerVersion
    {
        readonly int major;
        readonly int minor;
        readonly int release;

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
                major = int.Parse(sepa[0]);
                minor = int.Parse(sepa[1]);
                release = int.Parse(sepa[2]);
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
                if (minor == 1)
                {
                    return release >= 1;
                }
                return minor > 1;
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
