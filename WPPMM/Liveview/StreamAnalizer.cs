using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace WPPMM.Liveview
{
    internal class StreamAnalizer
    {
        private bool _IsOpen = true;
        public bool IsOpen
        {
            private set { _IsOpen = value; }
            get { return _IsOpen; }
        }

        private const int FPS_INTERVAL = 5000;
        private int packet_counter = 0;

        private const int CHeaderLength = 8;
        private const int PHeaderLength = 128;

        private Stream str;

        internal StreamAnalizer(Stream str)
        {
            if (str == null)
            {
                Log("Stream MUST NOT be null");
                throw new ArgumentNullException("Stream MUST NOT be null.");
            }
            this.str = str;
        }

        internal void Dispose()
        {
            IsOpen = false;
        }

        private void DisposeResouces()
        {
            if (str != null)
            {
                str.Dispose();
            }
            str = null;
            Dispose();
        }

        internal async void RunFpsDetector()
        {
            if (!IsOpen)
            {
                Log("StreamAnalizer is already disposed.");
                throw new ObjectDisposedException("StreamAnalizer is already disposed.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(FPS_INTERVAL));
            var fps = packet_counter * 1000 / FPS_INTERVAL;
            packet_counter = 0;
            Log("- - - - " + fps + " FPS - - - -");
            if (IsOpen)
            {
                RunFpsDetector();
            }
        }

        internal byte[] Next()
        {
            var CHeader = BlockingRead(CHeaderLength);
            if (CHeader[0] != (byte)0xFF || CHeader[1] != (byte)0x01) // Check fixed data
            {
                DisposeResouces();
                Log("Unexpected common header");
                throw new IOException("Unexpected common header");
            }

            var PHeader = BlockingRead(PHeaderLength);
            if (PHeader[0] != (byte)0x24 || PHeader[1] != (byte)0x35 || PHeader[2] != (byte)0x68 || PHeader[3] != (byte)0x79) // Check fixed data
            {
                DisposeResouces();
                Log("Unexpected payload header");
                throw new IOException("Unexpected payload header");
            }
            int data_size = ReadIntFromByteArray(PHeader, 4, 3);
            int padding_size = ReadIntFromByteArray(PHeader, 7, 1);

            var data = BlockingRead(data_size);
            BlockingRead(padding_size); // discard padding from stream

            packet_counter++;

            return data;
        }

        private byte[] ReadBuffer = new byte[8192];

        private byte[] BlockingRead(int numBytes)
        {
            var remainBytes = numBytes;
            int read;
            using (var output = new MemoryStream())
            {
                while (remainBytes > 0)
                {
                    if (!IsOpen)
                    {
                        Log("IsOpen false: Finish while loop in BlockingRead");
                        throw new IOException("Force finish reading");
                    }
                    var source = str;
                    if (source == null)
                    {
                        DisposeResouces();
                        Log("Cannot access Stream. Finish reading.");
                        throw new IOException("Cannot access Stream. Finish reading.");
                    }
                    try
                    {
                        read = source.Read(ReadBuffer, 0, Math.Min(ReadBuffer.Length, remainBytes));
                    }
                    catch (ObjectDisposedException)
                    {
                        DisposeResouces();
                        Log("Caught ObjectDisposedException while reading bytes: forcefully disposed.");
                        throw new IOException("Stream forcefully disposed");
                    }
                    if (read < 0)
                    {
                        DisposeResouces();
                        Log("Detected end of stream.");
                        throw new IOException("End of stream");
                    }
                    remainBytes -= read;
                    output.Write(ReadBuffer, 0, read);
                }
                return output.ToArray();
            }
        }

        private static int ReadIntFromByteArray(byte[] bytearray, int index, int length)
        {
            int int_data = 0;
            for (int i = 0; i < length; i++)
            {
                int_data = (int_data << 8) | (bytearray[index + i] & 0xff);
            }
            return int_data;
        }

        private static void Log(string message)
        {
            Debug.WriteLine("[StreamAnalizer] " + message);
        }
    }
}
