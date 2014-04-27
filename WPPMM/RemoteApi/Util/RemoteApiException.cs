using System;

namespace WPPMM.RemoteApi
{
    /// <summary>
    /// Exception for Task style async request.
    /// </summary>
    public class RemoteApiException : Exception
    {
        /// <summary>
        /// Status code of this Remote API error.
        /// </summary>
        public int code { get; private set; }

        internal RemoteApiException(int code)
        {
            this.code = code;
        }
    }
}
