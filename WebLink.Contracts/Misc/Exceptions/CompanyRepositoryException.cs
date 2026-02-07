using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;

namespace WebLink.Contracts
{
	[Serializable]
	public class CompanyRepositoryException : SystemException
	{
		public CompanyRepositoryException(string message) : base(message) { }
		public CompanyRepositoryException(string message, Exception innerException) : base(message, innerException) { }
		public CompanyRepositoryException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	[Serializable]
	public class CompanyRepositoryNotFoundException : CompanyRepositoryException
	{
		public CompanyRepositoryNotFoundException(string message) : base(message) { }
		public CompanyRepositoryNotFoundException(string message, Exception innerException) : base(message, innerException) { }
		public CompanyRepositoryNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	[Serializable]
	public class CompanyCodeNotFoundException : CompanyRepositoryException
	{
		public string CompanyCode { get; set; }
		public CompanyCodeNotFoundException(string message) : base(message) { }
		public CompanyCodeNotFoundException(string message, string companyCode) : base(message) { CompanyCode = companyCode; }
		public CompanyCodeNotFoundException(string message, Exception innerException) : base(message, innerException) { }
		public CompanyCodeNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			CompanyCode = info.GetString("CompanyCode");
		}

		[SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}

			info.AddValue("CompanyCode", CompanyCode);
			base.GetObjectData(info, context);
		}
	}
}
