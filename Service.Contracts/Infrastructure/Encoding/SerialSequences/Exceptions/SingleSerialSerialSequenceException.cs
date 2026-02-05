using System;
using System.Runtime.Serialization;

namespace Service.Contracts
{
    [Serializable]
    public class SingleSerialSerialSequenceException : Exception
    {
        public SingleSerialSerialSequenceException()
        {
        }

        public SingleSerialSerialSequenceException(string message) : base(message)
        {
        }

        public SingleSerialSerialSequenceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SingleSerialSerialSequenceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
