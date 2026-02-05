using System;
using System.Runtime.Serialization;

namespace Service.Contracts
{
    [Serializable]
    public class NoMoreSerialAvailableMSQException : MultiSerialSerialSequenceException
    {
        public NoMoreSerialAvailableMSQException() 
        {
        }

        public NoMoreSerialAvailableMSQException(string message) : base(message)
        {
        }

        public NoMoreSerialAvailableMSQException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NoMoreSerialAvailableMSQException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}