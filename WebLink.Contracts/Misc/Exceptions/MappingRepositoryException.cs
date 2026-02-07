using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;

namespace WebLink.Contracts
{
	[Serializable]
	public class MappingRepositoryException : SystemException
	{
		public MappingRepositoryException() : base("") { }
		public MappingRepositoryException(string message) : base(message) { }
		public MappingRepositoryException(string message, Exception innerException) : base(message, innerException) { }
		public MappingRepositoryException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	[Serializable]
	public class MappingNotFoundException : MappingRepositoryException
	{
		public MappingNotFoundException():base("") { }
		public MappingNotFoundException(string message) : base(message) { }
		public MappingNotFoundException(string message, string filename) : base(message) { this.FileName = filename; }
		public MappingNotFoundException(string message, Exception innerException) : base(message, innerException) { }

		public string FileName { get; set; }

		public MappingNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			FileName = info.GetString("FileName");
		}

		[SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}

			info.AddValue("FileName", FileName);
			base.GetObjectData(info, context);
		}
	}
}
