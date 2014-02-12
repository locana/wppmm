using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Networking.Proximity;

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

            while (recordPointer < raw.Length)
            {
                bool isLastMessage = false;

                var record = new SonyNdefRecord();
                record.ndefHeader = raw[recordPointer];
                recordPointer++;
                // Debug.WriteLine("NDEF header: " + Convert.ToString(record.ndefHeader, 2));

                record.typeLength = raw[recordPointer];
                recordPointer++;

                // to check whether short record or not
                if ((0x10 & record.ndefHeader) == 0x10)
                {
                    // short length
                    record.payloadLength = raw[recordPointer];
                    recordPointer += 1;
                }
                else
                {
                    // not sony nfc format
                    record.payloadLength = raw[recordPointer + 3] << 24 | raw[recordPointer + 2] << 16 | raw[recordPointer + 1] << 8 | raw[recordPointer];
                    recordPointer += 4;
                }

                // to check last message
                if ((0x40 & record.ndefHeader) == 0x40)
                {
                    isLastMessage = true;
                }

                // to check id length (0 or 1)
                if ((0x08 & record.ndefHeader) == 0x08)
                {
                    record.IsIdExist = true;
                }
                else
                {
                    record.IsIdExist = false;
                }


                // get id length
                if (record.IsIdExist)
                {
                    record.idLength = raw[recordPointer];
                    recordPointer++;
                }

                // get type
                record.type = Encoding.UTF8.GetString(raw, recordPointer, record.typeLength);
                recordPointer += record.typeLength;

                StringBuilder sb = new StringBuilder();

                // get id (if exist)
                if (record.IsIdExist)
                {
                    record.id = Encoding.UTF8.GetString(raw, recordPointer, record.idLength);
                    recordPointer += record.idLength;
                }

                // something strange, 1 byte here.
                recordPointer++;

                // get payload
                Byte[] payload = new Byte[record.payloadLength];
                Array.Copy(raw, recordPointer, payload, 0, record.payloadLength);
                recordPointer += record.payloadLength;

                var parsedPayload = this.ParseSonyNdefPayload(payload);
                record.SSID = parsedPayload.SSID;
                record.Password = parsedPayload.Password;

                records.Add(record);

                record.dump();

                // currently, only first record is required to dispaly SSID/Password. 
                // and it looks that 2nd chunk difficult to parse.....
                break;
            }
        }

        private SonyNdefRecord ParseSonyNdefPayload(byte[] payload)
        {
            var ret = new SonyNdefRecord();
            StringBuilder sb = new StringBuilder();

            int pointer = 0;

            // remove header?
            long header = (payload[0] << 24) | (payload[1] << 16) | (payload[2] << 8) | (payload[3]);
            Debug.WriteLine("header?: " + header.ToString("x8"));

            pointer = 4;

            int contentCount = 0;

            while (pointer < payload.Length)
            {
                Debug.WriteLine("pointer: " + pointer + " length: " + payload.Length);

                Debug.WriteLine("-----Record[" + contentCount++ + "]-----");

                // find id?
                int id = payload[pointer] << 8 | payload[pointer + 1];
                pointer += 2;
                Debug.WriteLine("id: " + id.ToString("x4"));

                // find size?
                int size = payload[pointer] << 8 | payload[pointer + 1];
                pointer += 2;
                Debug.WriteLine("size: " + size.ToString("x4"));

                Byte[] value = new Byte[size];
                Array.Copy(payload, pointer, value, 0, size);

                String valueText = Encoding.UTF8.GetString(value, 0, value.Length);
                Debug.WriteLine("value: " + valueText);

                if (id == 0x1000)
                {
                    ret.SSID = valueText;
                }
                else if (id == 0x1001)
                {
                    ret.Password = valueText;
                }

                pointer += size;

                Debug.WriteLine("-----Record End----");

                if (ret.SSID.Length > 0 && ret.Password.Length > 0)
                {
                    break;
                }
            }
            return ret;
        }
    }
}
