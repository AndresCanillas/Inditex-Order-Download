using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Util.Internal
{
	internal class CertificateInfo
	{
		protected string certificate;

		protected List<string> resultMessages;

		protected CertificateSigningRequestInfo userRequestInfo;

		public string Certificate
		{
			get
			{
				return this.certificate;
			}
			set
			{
				this.certificate = value;
			}
		}

		public List<string> ResultMessages
		{
			get
			{
				return this.resultMessages;
			}
			set
			{
				this.resultMessages = value;
			}
		}

		public CertificateSigningRequestInfo UserRequestInfo
		{
			get
			{
				return this.userRequestInfo;
			}
			set
			{
				this.userRequestInfo = value;
			}
		}

		public CertificateInfo()
		{
		}

		public CertificateInfo(CertificateSigningRequestInfo userInfo)
		{
			this.UserRequestInfo = userInfo;
			this.resultMessages = new List<string>();
		}
	}
}