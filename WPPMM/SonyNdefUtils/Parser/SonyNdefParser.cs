using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Proximity;
using System.Runtime.InteropServices.WindowsRuntime;

namespace WPPMM.SonyNdefUtils
{
    public class SonyNdefParser
    {

        private Byte[] raw;
        private List<SonyNdefRecord> records;

        public SonyNdefParser(Byte[] input)
        {
            raw = input;
            records = new List<SonyNdefRecord>();

        }

        public SonyNdefParser(ProximityMessage message)
        {
            var rawMsg = message.Data.ToArray();
            raw = rawMsg;
            records = new List<SonyNdefRecord>();

        }

        public List<SonyNdefRecord> Parse()
        {
            try
            {
                this._parse();
            }
            catch
            {
                throw new NdefParseException("Failed to parse");
            }

            if (records.Count == 0)
            {
                throw new NoNdefRecordException("It seems that there's no ndef record.");
            }

            return records;
        }

        private void _parse()
        {
            int recordPointer = 0;

            var record = new SonyNdefRecord();
            record.ndefHeader = raw[recordPointer];

            recordPointer++;

            record.typeLength = raw[recordPointer];

            // to check whether short record or not
            if ((0x10 & record.ndefHeader) == 0x10)
            {
                // short length
                recordPointer += 1;
                record.payloadLength = raw[recordPointer];
            }
            else
            {
                // not sony nfc format
                recordPointer += 4;
            }

            // to check id length (0 or 1)
            if ((0x08 & record.ndefHeader) == 0x08)
            {
                record.idLength = 1;
                recordPointer++;
                record.idLength = (int)raw[recordPointer];
            }
            else
            {
                record.idLength = 0;
                
            }
            
            recordPointer++;

            // get type
            int typeEndPointer = recordPointer + record.typeLength;
            StringBuilder sb = new StringBuilder();
            for (; recordPointer < typeEndPointer; recordPointer++)
            {
                sb.Append((char)raw[recordPointer]);
            }
            record.type = sb.ToString();
            sb.Clear();

            recordPointer++;

            // get id (if exist)
            if (record.idLength > 0)
            {
                int idEndPointer = recordPointer + record.idLength;
                for (; recordPointer < idEndPointer; recordPointer++)
                {
                    sb.Append((char)raw[recordPointer]);
                }
                record.id = sb.ToString();
                sb.Clear();
                recordPointer++;
            }

            // get payload
            int payloadEndPointer = recordPointer + record.payloadLength;

            Byte[] payload = new Byte[record.payloadLength];
            Array.Copy(raw, recordPointer, payload, 0, record.payloadLength);

            var parsedPayload = this.ParsePayload(payload);
            record.SSID = parsedPayload.SSID;
            record.Password = parsedPayload.Password;

            for (; recordPointer < payloadEndPointer; recordPointer++)
            {
                char ch = (char)raw[recordPointer];
                if ((int)ch > 0x19)
                {
                    // Debug.WriteLine(ch);
                    sb.Append(ch);
                }
                else
                {
                    // Debug.WriteLine((int)ch);
                }
            }
            record.payload = sb.ToString();
            sb.Clear();

            records.Add(record);

            record.dump();
        }

        private SonyNdefRecord ParsePayload(byte[] payload)
        {
            var ret = new SonyNdefRecord();
            StringBuilder sb = new StringBuilder();

            int SSIDStartPointer = 0;
            int SSIDLength = 0;
            int PasswordStartPointer = 0;
            int PasswordLength = 0;

            // find SSID
            for (int i = 0; i < payload.Length; i++)
            {
                int val = (int)payload[i];
                Debug.WriteLine((char)val);

                if (val > 0x19 && i > 1 && (int)payload[i - 2] == 0)
                {
                    // ASCII
                    SSIDStartPointer = i;
                    SSIDLength = (int)payload[i - 1];
                    break;
                }
            }

            Debug.WriteLine("ssid starts from: " + SSIDStartPointer);
            Debug.WriteLine("ssid length: " + SSIDLength);

            // get ssid
            for (int i = 0; i < SSIDLength; i++)
            {
                sb.Append((char)payload[SSIDStartPointer + i]);
            }
            ret.SSID = sb.ToString();
            Debug.WriteLine("ssid: " + ret.SSID);

            sb.Clear();

            // find password
            for (int i = SSIDStartPointer + SSIDLength; i < payload.Length; i++)
            {

                int val = (int)payload[i];

                if (val > 0x19 && i > 1 && (int)payload[i - 2] == 0)
                {
                    // if ASCII found
                    PasswordStartPointer = i;
                    PasswordLength = (int)payload[i - 1];
                    break;
                }
            }

            Debug.WriteLine("password starts from: " + PasswordStartPointer);
            Debug.WriteLine("password length: " + PasswordLength);

            // get password
            for (int i = 0; i < PasswordLength; i++)
            {
                sb.Append((char)payload[PasswordStartPointer + i]);
            }
            ret.Password = sb.ToString();



            return ret;

        }

        

    }
}
