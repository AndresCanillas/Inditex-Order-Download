using System;
using System.Runtime.Serialization;

namespace Service.Contracts
{
    [Serializable]
    public class MultiSerialSerialSequenceException : Exception
    {
        public MultiSerialSerialSequenceException()
        {
        }

        public MultiSerialSerialSequenceException(string message) : base(message)
        {
        }

        public MultiSerialSerialSequenceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MultiSerialSerialSequenceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
