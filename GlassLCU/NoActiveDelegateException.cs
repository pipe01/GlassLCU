using GlassLCU.API;
using System;

namespace GlassLCU
{
    public class NoActiveDelegateException : APIErrorException
    {
        public NoActiveDelegateException() : base()
        {
        }

        public NoActiveDelegateException(string message) : base(message)
        {
        }

        public NoActiveDelegateException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NoActiveDelegateException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }

        public NoActiveDelegateException(ErrorData error) : base(error)
        {
        }
    }
}
