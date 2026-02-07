using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace WebLink.Contracts
{
	[Serializable]
    public class OrderDocumentServiceException : SystemException
    {
        public OrderDocumentServiceException(string message) : base(message) { }
        public OrderDocumentServiceException(string message, Exception innerException) : base(message, innerException) { }
		public OrderDocumentServiceException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	[Serializable]
	public class OrderDocumentServiceCreatePreviewException : SystemException
    {
        public OrderDocumentServiceCreatePreviewException(string message) : base(message) { }
        public OrderDocumentServiceCreatePreviewException(string message, Exception innerException) : base(message, innerException) { }
		public OrderDocumentServiceCreatePreviewException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}
