using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;

namespace WebLink.Contracts
{
	[Serializable]
	public class OrderRepositoryException : SystemException
    {
        public OrderRepositoryException(string message) : base(message) { }
        public OrderRepositoryException(string message, Exception innerException) : base(message, innerException) { }
		public OrderRepositoryException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	[Serializable]
	public class ArticleCodeNotFoundException : OrderRepositoryException
    {
        public string ArticleCode { get; set; }
        public ArticleCodeNotFoundException(string message) : base(message) { }
        public ArticleCodeNotFoundException(string message, string articleCode) : base(message) { ArticleCode = articleCode; }
        public ArticleCodeNotFoundException(string message, Exception innerException) : base(message, innerException) { }
		public ArticleCodeNotFoundException(string message, Exception innerException, string articleCode) : base(message, innerException) { ArticleCode = articleCode; }
		public ArticleCodeNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			ArticleCode = info.GetString("ArticleCode");
		}


		[SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}

			info.AddValue("ArticleCode", ArticleCode);
			base.GetObjectData(info, context);
		}
	}
}
