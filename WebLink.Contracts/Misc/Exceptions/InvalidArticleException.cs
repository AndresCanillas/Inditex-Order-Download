using System;
using System.Runtime.Serialization;

namespace WebLink.Contracts
{
    [Serializable]
    internal class InvalidArticleException : Exception
    {
        public InvalidArticleException()
        {
        }

        public InvalidArticleException(string message) : base(message)
        {
        }

        public InvalidArticleException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidArticleException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}