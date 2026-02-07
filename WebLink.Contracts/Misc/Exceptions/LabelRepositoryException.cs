using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;

namespace WebLink.Contracts
{
	[Serializable]
	public class LabelRepositoryException : SystemException
	{
		public LabelRepositoryException(string message) : base(message) { }
		public LabelRepositoryException(string message, Exception innerException) : base(message, innerException) { }
		public LabelRepositoryException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	[Serializable]
	public class LabelRepositoryLabelPreviewException : LabelRepositoryException
	{
		public int LabelID { get; set; }
		public LabelRepositoryLabelPreviewException(string message) : base(message) { }
		public LabelRepositoryLabelPreviewException(string message, int labelID) : base(message) { LabelID = labelID; }
		public LabelRepositoryLabelPreviewException(string message, Exception innerException) : base(message, innerException) { }
		public LabelRepositoryLabelPreviewException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			LabelID = info.GetInt32("LabelID");
		}

		[SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}

			info.AddValue("LabelID", LabelID);
			base.GetObjectData(info, context);
		}
	}
}
