//using Org.BouncyCastle.Asn1.Pkcs;
//using Org.BouncyCastle.Pkcs;
//using Org.BouncyCastle.Security;
//using Org.BouncyCastle.Security.Certificates;
//using Org.BouncyCastle.X509;
using System;
using System.Runtime.Serialization;

namespace Zebra.Sdk.Certificate
{
	[Serializable]
	internal class CertificateException : Exception
	{
		public CertificateException()
		{
		}

		public CertificateException(string message) : base(message)
		{
		}

		public CertificateException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected CertificateException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}