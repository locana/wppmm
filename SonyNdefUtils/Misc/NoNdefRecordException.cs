using System;

namespace WPPMM.SonyNdefUtils
{
    public class NoNdefRecordException : Exception
    {

        public NoNdefRecordException()
        {
        }

        public NoNdefRecordException(String message)
            : base(message)
        {
        }


    }
}
