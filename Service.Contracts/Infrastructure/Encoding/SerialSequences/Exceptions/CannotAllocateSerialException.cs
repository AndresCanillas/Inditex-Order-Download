using System;
using System.Runtime.Serialization;

namespace Service.Contracts
{
    [Serializable]
    public class CannotAllocateSerialException : MultiSerialSerialSequenceException
    {
        public CannotAllocateSerialException()
        {
        }

        public CannotAllocateSerialException(string message) : base(message)
        {
        }

        public CannotAllocateSerialException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected CannotAllocateSerialException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}