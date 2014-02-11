using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPPMM.SonyNdefUtils
{
    public class NdefParseException : Exception
    {

        public NdefParseException()
        {
        }

        public NdefParseException(String message)
            : base(message)
        {
        }
    }
}
