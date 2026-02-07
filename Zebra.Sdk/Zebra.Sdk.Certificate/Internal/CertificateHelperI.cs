//using Org.BouncyCastle.Asn1.Pkcs;
//using Org.BouncyCastle.X509;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Zebra.Sdk.Certificate.Internal
{
	internal interface CertificateHelperI
	{
		string GetCommonNameHelper(X509Certificate x509Certificate);

		//void PemWriterHelper(PrivateKeyInfo privateKey, StringWriter strWriter);

		void PemWriterHelper(X509Certificate thisCert, StringWriter strWriter);
	}
}