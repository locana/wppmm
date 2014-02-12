using System;
using System.Diagnostics;

namespace WPPMM.SonyNdefUtils
{
    public class SonyNdefRecord
    {

        public byte ndefHeader
        {
            get;
            set;
        }

        public int typeLength
        {
            get;
            set;
        }

        public int payloadLength
        {
            get;
            set;
        }

        public int idLength
        {
            get;
            set;
        }

        public String type
        {
            get;
            set;
        }

        public String id
        {
            get;
            set;
        }

        public String payload
        {
            get;
            set;
        }

        public String SSID
        {
            get;
            set;
        }

        public String Password
        {
            get;
            set;
        }

        public bool IsIdExist
        {
            get;
            set;
        }

        public SonyNdefRecord()
        {
            ndefHeader = (byte)0;
            typeLength = 0;
            payloadLength = 0;
            idLength = 0;
            type = "";
            id = "";
            payload = "";
            SSID = "";
            Password = "";
            IsIdExist = false;
        }

        public void dump()
        {
            Debug.WriteLine("NDEF header: " + Convert.ToString(ndefHeader, 2));
            Debug.WriteLine("Type length: " + typeLength);
            Debug.WriteLine("payload length: " + payloadLength);
            Debug.WriteLine("id length: " + idLength);
            Debug.WriteLine("type: " + type);
            Debug.WriteLine("id: " + id);
            Debug.WriteLine("payload: " + payload);
            Debug.WriteLine("SSID: " + SSID);
            Debug.WriteLine("Password: " + Password);
        }


    }
}
