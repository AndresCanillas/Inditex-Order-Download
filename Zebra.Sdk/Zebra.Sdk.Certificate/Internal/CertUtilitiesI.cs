//using Org.BouncyCastle.Crypto;
//using Org.BouncyCastle.Pkcs;
using System;
using System.IO;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Certificate.Internal
{
	internal interface CertUtilitiesI
	{
		string ConvertDerCertToPemCert(byte[] derContents);

		string ConvertDerKeyToPemKey(byte[] derContents);

		//X509CertificateEntry[] CreateCertChain(string certifcateContents);

		//void CreateP12File(AsymmetricKeyParameter privateKey, X509CertificateEntry[] chain, string p12FileName, string p12Password);

		//void GetCaFromPkcs12Keystore(string alias, Stream caStream, Pkcs12Store keyStore);

		//X509CertificateEntry GetCertificate(string pemFileContents);

		//X509CertificateEntry[] GetCertificateChain(string alias, Pkcs12Store keyStore);

		//void GetCertificateFromPkcs12Keystore(string alias, Stream certificateStream, Pkcs12Store keyStore);

		//AsymmetricCipherKeyPair GetKeyPair();

		//AsymmetricKeyParameter GetPrivateKey(string alias, Pkcs12Store keyStore);

		//void GetPrivateKeyFromPkcs12Keystore(string alias, string privateKeyPassphrase, Stream keyStream, Pkcs12Store keyStore);

		//void Save(CertificateInfo signingServerResponseData, AsymmetricCipherKeyPair keyPair, string tomcatConfigurationDirectory, string p12Passphrase, string privatekeyPassphrase, int serverPort, string dbUrl, string dbUsername, string dbPassword);
	}
}