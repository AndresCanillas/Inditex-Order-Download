//using Org.BouncyCastle.Asn1;
//using Org.BouncyCastle.Asn1.Pkcs;
//using Org.BouncyCastle.Asn1.X509;
//using Org.BouncyCastle.Pkcs;
//using Org.BouncyCastle.Security;
//using Org.BouncyCastle.X509;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Zebra.Sdk.Certificate.Internal;

namespace Zebra.Sdk.Certificate
{
	/// <summary>
	///       A utility class used to extract info from certificate files and convert the contents into Zebra friendly formats.
	///       </summary>
	public class ZebraP12Info
	{
		//private Pkcs12Store keyStore;

		private string firstAlias = string.Empty;

		///// <summary>
		/////       Gets the keystore of the processed client certificate.
		/////       </summary>
		///// <returns>The Pkcs12Store containing information about the processed certificate file.</returns>
		//public Pkcs12Store KeyStore
		//{
		//	get
		//	{
		//		return this.keyStore;
		//	}
		//}

		/// <summary>
		///       Creates a wrapper that opens up the provided certificate keystore stream.
		///       </summary>
		/// <param name="pkcs12Stream">The stream containing certificate keystore file contents.</param>
		/// <param name="p12Password">The password used to access the certificate file.</param>
		/// <exception cref="T:Zebra.Sdk.Certificate.ZebraCertificateException">Thrown if the certificate stream contents cannot be accessed or if the
		///       certificate password was incorrect.</exception>
		public ZebraP12Info(Stream pkcs12Stream, string p12Password)
		{
			throw new NotImplementedException();
			//try
			//{
			//	this.keyStore = this.GetPkcs12KeyStore(pkcs12Stream, p12Password);
			//	IEnumerator enumerator = this.keyStore.Aliases.GetEnumerator();
			//	while (enumerator.MoveNext())
			//	{
			//		string str = enumerator.Current.ToString();
			//		if (!this.keyStore.IsKeyEntry(str))
			//		{
			//			continue;
			//		}
			//		this.firstAlias = str;
			//		break;
			//	}
			//}
			//catch (ArgumentException argumentException)
			//{
			//	throw new ZebraCertificateException("The provided password was incorrect.", argumentException);
			//}
			//catch (IOException oException1)
			//{
			//	IOException oException = oException1;
			//	string str1 = "The provided certificate file was invalid.";
			//	if ((new CultureInfo("en-US")).TextInfo.ToLower(oException.Message).Contains("password"))
			//	{
			//		str1 = "The provided password was incorrect.";
			//	}
			//	throw new ZebraCertificateException(str1, oException);
			//}
			//catch (Exception exception)
			//{
			//	throw new ZebraCertificateException("The provided stream does not appear to contain valid certificate content or the password is incorrect.", exception);
			//}
		}

		/// <summary>
		///       Get a list of aliases present in the certificate keystore.
		///       </summary>
		/// <returns>A list of the aliases present in the certificate keystore.</returns>
		/// <exception cref="T:Zebra.Sdk.Certificate.ZebraCertificateException">If the keystore has not been initialized (loaded).</exception>
		public List<string> GetAliases()
		{
			throw new NotImplementedException();
			//List<string> strs;
			//if (this.keyStore == null)
			//{
			//	throw new ZebraCertificateException("The certificate file was not valid.");
			//}
			//try
			//{
			//	List<string> strs1 = new List<string>();
			//	IEnumerator enumerator = this.keyStore.Aliases.GetEnumerator();
			//	if (enumerator != null)
			//	{
			//		while (enumerator.MoveNext())
			//		{
			//			strs1.Add(enumerator.Current.ToString());
			//		}
			//	}
			//	strs = strs1;
			//}
			//catch (Exception exception)
			//{
			//	throw new ZebraCertificateException("The certificate file was not valid.", exception);
			//}
			//return strs;
		}

		/// <summary>
		///       Get the common name of the CA associated with the certificate file.
		///       </summary>
		/// <returns>The CA common name.</returns>
		/// <exception cref="T:Zebra.Sdk.Certificate.ZebraCertificateException">If the provided alias does not exist or the underlying 
		///       certificate could not be parsed.</exception>
		public string GetCaCommonName()
		{
			return this.GetCaCommonName(this.firstAlias);
		}

		/// <summary>
		///       Get the common name of the CA associated with the certificate file.
		///       </summary>
		/// <param name="alias">The alias name of the specific entry to extract from the certificate file</param>
		/// <returns>The CA common name.</returns>
		/// <exception cref="T:Zebra.Sdk.Certificate.ZebraCertificateException">If the provided alias does not exist or the underlying 
		///       certificate could not be parsed.</exception>
		public string GetCaCommonName(string alias)
		{
			throw new NotImplementedException();
			//string commonNameHelper;
			//if (this.keyStore == null)
			//{
			//	throw new ZebraCertificateException("The certificate file was not valid.");
			//}
			//if (!this.GetAliases().Contains(alias))
			//{
			//	throw new ZebraCertificateException(string.Concat("The provided alias \"", alias, "\" was not found in the provided keystore."));
			//}
			//try
			//{
			//	X509CertificateEntry[] certificateChain = CertUtilitiesFactory.GetCertUtilities().GetCertificateChain(alias, this.keyStore);
			//	X509Certificate certificate = certificateChain[(int)certificateChain.Length - 1].Certificate;
			//	commonNameHelper = CertUtilitiesFactory.GetCertificateHelper().GetCommonNameHelper(certificate);
			//}
			//catch (EncryptionException encryptionException)
			//{
			//	throw new ZebraCertificateException("The certificate could not be parsed for a common name.", encryptionException);
			//}
			//return commonNameHelper;
		}

		/// <summary>
		///       Get the content of the ca, which is determined to be all entries in the certificate chain after the first entry.
		///       </summary>
		/// <returns>The Zebra-friendly ca content.</returns>
		/// <exception cref="T:Zebra.Sdk.Certificate.ZebraCertificateException">If the provided alias does not exist or the ca content is corrupt.</exception>
		public string GetCaContent()
		{
			return this.GetCaContent(this.firstAlias);
		}

		/// <summary>
		///       Get the content of the ca, which is determined to be all entries in the certificate chain after the first entry.
		///       </summary>
		/// <param name="alias">The alias name of the specific entry to extract from the certificate file.</param>
		/// <returns>The Zebra-friendly ca content.</returns>
		/// <exception cref="T:Zebra.Sdk.Certificate.ZebraCertificateException">If the provided alias does not exist or the ca content is corrupt.</exception>
		public string GetCaContent(string alias)
		{
			throw new NotImplementedException();
			//string str;
			//if (this.keyStore == null)
			//{
			//	throw new ZebraCertificateException("The certificate file was not valid.");
			//}
			//if (!this.GetAliases().Contains(alias))
			//{
			//	throw new ZebraCertificateException(string.Concat("The provided alias \"", alias, "\" was not found in the provided keystore."));
			//}
			//try
			//{
			//	string empty = string.Empty;
			//	using (MemoryStream memoryStream = new MemoryStream())
			//	{
			//		CertUtilitiesFactory.GetCertUtilities().GetCaFromPkcs12Keystore(alias, memoryStream, this.keyStore);
			//		empty = Encoding.UTF8.GetString(memoryStream.ToArray());
			//	}
			//	if (string.IsNullOrEmpty(empty))
			//	{
			//		throw new ZebraCertificateException("The provided certificate file does not contain a ca.");
			//	}
			//	str = empty;
			//}
			//catch (IOException oException)
			//{
			//	throw new ZebraCertificateException("Failed to retrieve the ca contents", oException);
			//}
			//return str;
		}

		/// <summary>
		///       Get the expiration data of the CA associated with the certificate file.
		///       </summary>
		/// <returns>The CA expiration date.</returns>
		/// <exception cref="T:Zebra.Sdk.Certificate.ZebraCertificateException">If the certificate keystore is invalid or the provided alias 
		///       does not exist in the certificate keystore.</exception>
		public DateTime GetCaExpirationDate()
		{
			return this.GetCertificateExpirationDate(this.firstAlias);
		}

		/// <summary>
		///       Get the expiration data of the CA associated with the certificate file.
		///       </summary>
		/// <param name="alias">The alias name of the specific entry to extract from the certificate file.</param>
		/// <returns>The CA expiration date.</returns>
		/// <exception cref="T:Zebra.Sdk.Certificate.ZebraCertificateException">If the certificate keystore is invalid or the provided alias 
		///       does not exist in the certificate keystore.</exception>
		public DateTime GetCaExpirationDate(string alias)
		{
			throw new NotImplementedException();
			//if (this.keyStore == null)
			//{
			//	throw new ZebraCertificateException("The certificate file was not valid.");
			//}
			//if (!this.GetAliases().Contains(alias))
			//{
			//	throw new ZebraCertificateException(string.Concat("The provided alias \"", alias, "\" was not found in the provided keystore."));
			//}
			//X509CertificateEntry[] certificateChain = CertUtilitiesFactory.GetCertUtilities().GetCertificateChain(alias, this.keyStore);
			//return certificateChain[(int)certificateChain.Length - 1].Certificate.NotAfter;
		}

		/// <summary>
		///       Get the common name of the client certificate associated with the certificate file.
		///       </summary>
		/// <returns>The certificate common name.</returns>
		/// <exception cref="T:Zebra.Sdk.Certificate.ZebraCertificateException">If the provided alias does not exist or the underlying 
		///       certificate could not be parsed.</exception>
		public string GetCertificateCommonName()
		{
			return this.GetCertificateCommonName(this.firstAlias);
		}

		/// <summary>
		///       Get the common name of the client certificate associated with the certificate file.
		///       </summary>
		/// <param name="alias">The alias name of the specific entry to extract from the certificate file.</param>
		/// <returns>The certificate common name.</returns>
		/// <exception cref="T:Zebra.Sdk.Certificate.ZebraCertificateException">If the provided alias does not exist or the underlying 
		///       certificate could not be parsed.</exception>
		public string GetCertificateCommonName(string alias)
		{
			throw new NotImplementedException();
			//if (this.keyStore == null)
			//{
			//	throw new ZebraCertificateException("The certificate file was not valid.");
			//}
			//if (!this.GetAliases().Contains(alias))
			//{
			//	throw new ZebraCertificateException(string.Concat("The provided alias \"", alias, "\" was not found in the provided keystore."));
			//}
			//X509Certificate certificate = CertUtilitiesFactory.GetCertUtilities().GetCertificateChain(alias, this.keyStore)[0].Certificate;
			//string commonNameHelper = "";
			//try
			//{
			//	commonNameHelper = CertUtilitiesFactory.GetCertificateHelper().GetCommonNameHelper(certificate);
			//}
			//catch (EncryptionException encryptionException)
			//{
			//	throw new ZebraCertificateException("The certificate could not be parsed for a common name.", encryptionException);
			//}
			//return commonNameHelper;
		}

		/// <summary>
		///       Get the content of the first entry in the certificate's certificate chain.
		///       </summary>
		/// <returns>The Zebra-friendly certificate content</returns>
		/// <exception cref="T:Zebra.Sdk.Certificate.ZebraCertificateException">If the provided alias does not exist or the certificate content is corrupt</exception>
		public string GetCertificateContent()
		{
			return this.GetCertificateContent(this.firstAlias);
		}

		/// <summary>
		///       Get the content of the first entry in the certificate's certificate chain.
		///       </summary>
		/// <param name="alias">The alias name of the specific entry to extract from the certificate file.</param>
		/// <returns>The Zebra-friendly certificate content.</returns>
		/// <exception cref="T:Zebra.Sdk.Certificate.ZebraCertificateException">If the provided alias does not exist or the certificate content is corrupt</exception>
		public string GetCertificateContent(string alias)
		{
			throw new NotImplementedException();
			//string str;
			//if (this.keyStore == null)
			//{
			//	throw new ZebraCertificateException("The certificate file was not valid.");
			//}
			//if (!this.GetAliases().Contains(alias))
			//{
			//	throw new ZebraCertificateException(string.Concat("The provided alias \"", alias, "\" was not found in the provided keystore."));
			//}
			//try
			//{
			//	using (MemoryStream memoryStream = new MemoryStream())
			//	{
			//		CertUtilitiesFactory.GetCertUtilities().GetCertificateFromPkcs12Keystore(alias, memoryStream, this.keyStore);
			//		str = Encoding.UTF8.GetString(memoryStream.ToArray());
			//	}
			//}
			//catch (IOException oException)
			//{
			//	throw new ZebraCertificateException("Failed to retrieve the certificate contents", oException);
			//}
			//return str;
		}

		/// <summary>
		///       Get the expiration data of the client certificate associated with the certificate file.
		///       </summary>
		/// <returns>The client certificate expiration date.</returns>
		/// <exception cref="T:Zebra.Sdk.Certificate.ZebraCertificateException">If the certificate keystore is invalid or the provided alias 
		///       does not exist in the certificate keystore.</exception>
		public DateTime GetCertificateExpirationDate()
		{
			return this.GetCertificateExpirationDate(this.firstAlias);
		}

		/// <summary>
		///       Get the expiration data of the client certificate associated with the certificate file.
		///       </summary>
		/// <param name="alias">The alias name of the specific entry to extract from the certificate file.</param>
		/// <returns>The client certificate expiration date.</returns>
		/// <exception cref="T:Zebra.Sdk.Certificate.ZebraCertificateException">If the certificate keystore is invalid or the provided alias 
		///       does not exist in the certificate keystore.</exception>
		public DateTime GetCertificateExpirationDate(string alias)
		{
			throw new NotImplementedException();
			//if (this.keyStore == null)
			//{
			//	throw new ZebraCertificateException("The certificate file was not valid.");
			//}
			//if (!this.GetAliases().Contains(alias))
			//{
			//	throw new ZebraCertificateException(string.Concat("The provided alias \"", alias, "\" was not found in the provided keystore."));
			//}
			//return CertUtilitiesFactory.GetCertUtilities().GetCertificateChain(alias, this.keyStore)[0].Certificate.NotAfter;
		}

		/// <summary>
		///       Get the issuer of the client certificate.
		///       </summary>
		/// <returns>The client certificate issuer.</returns>
		/// <exception cref="T:Zebra.Sdk.Certificate.ZebraCertificateException">If the provided certificate file is invalid, alias does not exist, 
		///       or contained private key is corrupt or unsupported.</exception>
		public string GetCertificateIssuer()
		{
			return this.GetCertificateIssuer(this.firstAlias);
		}

		/// <summary>
		///       Get the issuer of the client certificate.
		///       </summary>
		/// <param name="alias">The alias name of the specific entry to extract from the certificate file.</param>
		/// <returns>The client certificate issuer.</returns>
		/// <exception cref="T:Zebra.Sdk.Certificate.ZebraCertificateException">If the provided certificate file is invalid, alias does not exist, 
		///       or contained private key is corrupt or unsupported.</exception>
		public string GetCertificateIssuer(string alias)
		{
			throw new NotImplementedException();
			//if (this.keyStore == null)
			//{
			//	throw new ZebraCertificateException("The certificate file was not valid.");
			//}
			//if (!this.GetAliases().Contains(alias))
			//{
			//	throw new ZebraCertificateException(string.Concat("The provided alias \"", alias, "\" was not found in the provided keystore."));
			//}
			//return CertUtilitiesFactory.GetCertUtilities().GetCertificateChain(alias, this.keyStore)[0].Certificate.IssuerDN.ToString();
		}

		/// <summary>
		///       Get the encrypted private key content.
		///       </summary>
		/// <param name="passwordToEncryptKey">The password used to encrypt the resulting private key.</param>
		/// <returns>The encrypted private key in PEM format.</returns>
		/// <exception cref="T:Zebra.Sdk.Certificate.ZebraCertificateException">If the provided alias does not exist, certificate password is 
		///       incorrect, or private key content is corrupt.</exception>
		public string GetEncryptedPrivateKeyContent(string passwordToEncryptKey)
		{
			return this.GetEncryptedPrivateKeyContent(this.firstAlias, passwordToEncryptKey);
		}

		/// <summary>
		///       Get the encrypted private key content.
		///       </summary>
		/// <param name="alias">The alias name of the specific entry to extract from the certificate file.</param>
		/// <param name="passwordToEncryptKey">The password used to encrypt the resulting private key.</param>
		/// <returns>The encrypted private key in PEM format.</returns>
		/// <exception cref="T:Zebra.Sdk.Certificate.ZebraCertificateException">If the provided alias does not exist, certificate password is 
		///       incorrect, or private key content is corrupt.</exception>
		public string GetEncryptedPrivateKeyContent(string alias, string passwordToEncryptKey)
		{
			throw new NotImplementedException();
			//string str;
			//if (this.keyStore == null)
			//{
			//	throw new ZebraCertificateException("The certificate file was not valid.");
			//}
			//if (!this.GetAliases().Contains(alias))
			//{
			//	throw new ZebraCertificateException(string.Concat("The provided alias \"", alias, "\" was not found in the provided keystore."));
			//}
			//try
			//{
			//	using (MemoryStream memoryStream = new MemoryStream())
			//	{
			//		CertUtilitiesFactory.GetCertUtilities().GetPrivateKeyFromPkcs12Keystore(alias, passwordToEncryptKey, memoryStream, this.keyStore);
			//		str = Encoding.UTF8.GetString(memoryStream.ToArray());
			//	}
			//}
			//catch (KeyException keyException)
			//{
			//	throw new ZebraCertificateException("Could not recover the key from the provided certificate keystore.", keyException);
			//}
			//catch (Exception exception)
			//{
			//	throw new ZebraCertificateException("Failed to retrieve the private key contents", exception);
			//}
			//return str;
		}

		private Pkcs12Store GetPkcs12KeyStore(Stream pkcs12Stream, string storePassword)
		{
			throw new NotImplementedException();
			//Pkcs12Store pkcs12Store = new Pkcs12Store();
			//if (storePassword == null)
			//{
			//	storePassword = string.Empty;
			//}
			//try
			//{
			//	pkcs12Store.Load(pkcs12Stream, storePassword.ToCharArray());
			//}
			//catch (Exception exception1)
			//{
			//	Exception exception = exception1;
			//	if (exception is InvalidKeyException)
			//	{
			//		throw new ArgumentException("The provided password was incorrect.", exception);
			//	}
			//	if (!(exception is IOException))
			//	{
			//		if (!(exception is NullReferenceException))
			//		{
			//			throw new ArgumentException("Failed to read contents of the certificate file - make sure that the provided password is correct.", exception);
			//		}
			//		throw new IOException(exception.Message, exception);
			//	}
			//	throw;
			//}
			//return pkcs12Store;
		}

		/// <summary>
		///       Get the algorithm used by the private key.
		///       </summary>
		/// <returns>The private key algorithm.</returns>
		/// <exception cref="T:Zebra.Sdk.Certificate.ZebraCertificateException">If the provided certificate file is invalid, alias does not exist, 
		///       or contained private key is corrupt or unsupported.</exception>
		public string GetPrivateKeyAlgorithm()
		{
			return this.GetPrivateKeyAlgorithm(this.firstAlias);
		}

		/// <summary>
		///       Get the algorithm used by the private key.
		///       </summary>
		/// <param name="alias">The alias name of the specific entry to extract from the certificate file.</param>
		/// <returns>The private key algorithm.</returns>
		/// <exception cref="T:Zebra.Sdk.Certificate.ZebraCertificateException">If the provided certificate file is invalid, alias does not exist, 
		///       or contained private key is corrupt or unsupported.</exception>
		public string GetPrivateKeyAlgorithm(string alias)
		{
			throw new NotImplementedException();
			//string friendlyName;
			//if (this.keyStore == null)
			//{
			//	throw new ZebraCertificateException("The certificate file was not valid.");
			//}
			//if (!this.GetAliases().Contains(alias))
			//{
			//	throw new ZebraCertificateException(string.Concat("The provided alias \"", alias, "\" was not found in the provided keystore."));
			//}
			//try
			//{
			//	friendlyName = (new Oid(PrivateKeyInfoFactory.CreatePrivateKeyInfo(CertUtilitiesFactory.GetCertUtilities().GetPrivateKey(alias, this.keyStore)).PrivateKeyAlgorithm.Algorithm.Id)).FriendlyName;
			//}
			//catch (KeyException keyException)
			//{
			//	throw new ZebraCertificateException("Could not recover the key from the provided certificate keystore.", keyException);
			//}
			//return friendlyName;
		}
	}
}