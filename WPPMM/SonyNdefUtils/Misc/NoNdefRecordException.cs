using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPPMM.SonyNdefUtils
{
    public class NoNdefRecordException : Exception
    {

        public NoNdefRecordException()
        {
        }

        public NoNdefRecordException(String message): base(message)
        {
        }


    }
}
