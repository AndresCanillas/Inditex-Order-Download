using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace WebLink.Contracts
{
    [Serializable]
    public class PoolFileHandlerException : Exception
    {
        public PoolFileHandlerException()
        {
        }

        public PoolFileHandlerException(string message) : base(message)
        {
        }

        public PoolFileHandlerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PoolFileHandlerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
