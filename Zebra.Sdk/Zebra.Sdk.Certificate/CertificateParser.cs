//using Org.BouncyCastle.Asn1.Pkcs;
//using Org.BouncyCastle.Pkcs;
//using Org.BouncyCastle.Security;
//using Org.BouncyCastle.Security.Certificates;
//using Org.BouncyCastle.X509;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Zebra.Sdk.Certificate.Internal;

namespace Zebra.Sdk.Certificate
{
	/// <summary>
	///       Takes in a certificate file (P12, DER, PEM, etc) and processes it into a ZebraCertificateInfo object which contains
	///       the selected certificate, Certificate Authority certificate chain, and private key (if applicable).
	///       </summary>
	public class CertificateParser
	{
		private static string PRIVATE_KEY_PATTERN;

		private static string CERTIFICATE_PATTERN;

		private static string PEM_PATTERN;

		static CertificateParser()
		{
			CertificateParser.PRIVATE_KEY_PATTERN = "(-----BEGIN( RSA)? PRIVATE KEY-----.*?-----END( RSA)? PRIVATE KEY-----)";
			CertificateParser.CERTIFICATE_PATTERN = "(-----BEGIN CERTIFICATE-----.*?-----END CERTIFICATE-----)";
			CertificateParser.PEM_PATTERN = "(-----BEGIN .*?-----.*?-----END .*?-----)";
		}

		/// <summary>
		///   <markup>
		///     <include item="SMCAutoDocConstructor">
		///       <parameter>Zebra.Sdk.Certificate.CertificateParser</parameter>
		///     </include>
		///   </markup>
		/// </summary>
		public CertificateParser()
		{
		}

		private static void AddCertificate(ZebraCertificateInfo pemCertificateData, X509CertificateParser certFactory, string individualCert)
		{
			throw new NotImplementedException();
			//using (Stream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(individualCert)))
			//{
			//	if (certFactory.ReadCertificate(memoryStream).GetBasicConstraints() != -1)
			//	{
			//		pemCertificateData.CaCertificates.Add(individualCert);
			//	}
			//	else
			//	{
			//		pemCertificateData.ClientCertificate = individualCert;
			//	}
			//}
		}

		private static IEnumerator GetMatcher(string pattern, string inputString)
		{
			return (new Regex(pattern, RegexOptions.Multiline | RegexOptions.Singleline)).Matches(inputString).GetEnumerator();
		}

		private static string GetPrivateKeyFromPem(string certData)
		{
			IEnumerator matcher = CertificateParser.GetMatcher(CertificateParser.PRIVATE_KEY_PATTERN, certData);
			if (!matcher.MoveNext())
			{
				return null;
			}
			return ((Match)matcher.Current).Groups[0].Value.Replace("\r\n", "\n").Replace("\n", "\r\n");
		}

		private static bool ParseAsCertificate(byte[] buffer, ZebraCertificateInfo pemCertificateData)
		{
			//bool flag;
			//try
			//{
			//	X509CertificateParser x509CertificateParser = new X509CertificateParser();
			//	foreach (X509Certificate x509Certificate in x509CertificateParser.ReadCertificates(buffer))
			//	{
			//		StringWriter stringWriter = new StringWriter();
			//		CertUtilitiesFactory.GetCertificateHelper().PemWriterHelper(x509Certificate, stringWriter);
			//		CertificateParser.AddCertificate(pemCertificateData, x509CertificateParser, stringWriter.ToString().Replace("\r\n", "\n").Replace("\n", "\r\n"));
			//	}
			//	flag = true;
			//}
			//catch (CertificateException certificateException)
			//{
			//	return false;
			//}
			//return flag;
			throw new NotImplementedException();
		}

		private static bool ParseAsDerPrivateKey(byte[] buffer, ZebraCertificateInfo pemCertificateData)
		{
			throw new NotImplementedException();
			//bool flag;
			//try
			//{
			//	PrivateKeyInfo privateKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(PrivateKeyFactory.CreateKey(buffer));
			//	StringWriter stringWriter = new StringWriter();
			//	CertUtilitiesFactory.GetCertificateHelper().PemWriterHelper(privateKeyInfo, stringWriter);
			//	pemCertificateData.PrivateKey = stringWriter.ToString().Replace("\r\n", "\n").Replace("\n", "\r\n");
			//	flag = true;
			//}
			//catch (Exception exception)
			//{
			//	return false;
			//}
			//return flag;
		}

		private static void ParseAsP12(byte[] buffer, ZebraCertificateInfo pemCertificateData, string alias, string p12Password)
		{
			throw new NotImplementedException();
			//using (Stream memoryStream = new MemoryStream(buffer))
			//{
			//	ZebraP12Info zebraP12Info = new ZebraP12Info(memoryStream, p12Password);
			//	pemCertificateData.ClientCertificate = zebraP12Info.GetCertificateContent();
			//	pemCertificateData.PrivateKey = zebraP12Info.GetEncryptedPrivateKeyContent(p12Password);
			//	if (alias == null)
			//	{
			//		IEnumerator enumerator = zebraP12Info.KeyStore.Aliases.GetEnumerator();
			//		do
			//		{
			//			if (!enumerator.MoveNext())
			//			{
			//				break;
			//			}
			//			alias = enumerator.Current.ToString();
			//		}
			//		while (!zebraP12Info.KeyStore.IsKeyEntry(alias));
			//	}
			//	CertUtilitiesI certUtilities = CertUtilitiesFactory.GetCertUtilities();
			//	X509CertificateEntry[] certificateChain = certUtilities.GetCertificateChain(alias, zebraP12Info.KeyStore);
			//	for (int i = 1; i < (int)certificateChain.Length; i++)
			//	{
			//		byte[] encoded = certificateChain[i].Certificate.GetEncoded();
			//		pemCertificateData.CaCertificates.Add(certUtilities.ConvertDerCertToPemCert(encoded));
			//	}
			//}
		}

		private static bool ParseAsPem(ZebraCertificateInfo pemCertificateData, string certData)
		{
			if (!certData.Contains("-----BEGIN"))
			{
				return false;
			}
			certData = CertificateParser.StripExtraInfo(certData);
			pemCertificateData.PrivateKey = CertificateParser.GetPrivateKeyFromPem(certData);
			CertificateParser.ProcessPemCertificates(pemCertificateData, certData);
			return true;
		}

		/// <summary>
		///       Takes in a certificate file (P12, DER, PEM, etc) and processes it into a ZebraCertificateInfo object which contains
		///       the selected certificate, Certificate Authority certificate chain, and private key (if applicable).
		///       </summary>
		/// <param name="certificateStream">Data stream for the certificate file to be processed.</param>
		/// <param name="alias">The certificate to use within a multi-certificate (like PKCS12) file.</param>
		/// <param name="password">Used to unlock a protected certificate file.</param>
		/// <returns>Instance containing all certificate and key information for the processed file.</returns>
		/// <exception cref="T:System.IO.IOException">If there is an issue reading the certificate file.</exception>
		/// <exception cref="T:Zebra.Sdk.Certificate.ZebraCertificateException">If there is an issue processing the certificate file.</exception>
		public static ZebraCertificateInfo ParseCertificate(Stream certificateStream, string alias, string password)
		{
			ZebraCertificateInfo zebraCertificateInfo = null;
			try
			{
				byte[] numArray = new byte[0];
				using (BinaryReader binaryReader = new BinaryReader(certificateStream))
				{
					numArray = binaryReader.ReadBytes((int)certificateStream.Length);
				}
				numArray = CertificateParser.StripPkcs7OpenSSLHeader(numArray);
				string str = Encoding.UTF8.GetString(numArray);
				zebraCertificateInfo = new ZebraCertificateInfo();
				if (!CertificateParser.ParseAsPem(zebraCertificateInfo, str) && !CertificateParser.ParseAsCertificate(numArray, zebraCertificateInfo) && !CertificateParser.ParseAsDerPrivateKey(numArray, zebraCertificateInfo))
				{
					CertificateParser.ParseAsP12(numArray, zebraCertificateInfo, alias, password);
				}
			}
			catch (ArgumentException argumentException1)
			{
				ArgumentException argumentException = argumentException1;
				throw new IOException(argumentException.Message, argumentException);
			}
			catch (FormatException formatException1)
			{
				FormatException formatException = formatException1;
				throw new IOException(formatException.Message, formatException);
			}
			return zebraCertificateInfo;
		}

		private static void ProcessPemCertificates(ZebraCertificateInfo pemCertificateData, string certData)
		{
			try
			{
				IEnumerator matcher = CertificateParser.GetMatcher(CertificateParser.CERTIFICATE_PATTERN, certData);
				while (matcher.MoveNext())
				{
					string str = ((Match)matcher.Current).Groups[0].Value.Replace("\r\n", "\n").Replace("\n", "\r\n");
					CertificateParser.AddCertificate(pemCertificateData, new X509CertificateParser(), str);
				}
			}
			catch (CertificateException)
			{
			}
		}

		private static string StripExtraInfo(string certData)
		{
			StringBuilder stringBuilder = new StringBuilder();
			IEnumerator enumerator = (new Regex(CertificateParser.PEM_PATTERN, RegexOptions.Multiline | RegexOptions.Singleline)).Matches(certData).GetEnumerator();
			while (enumerator.MoveNext())
			{
				string value = ((Match)enumerator.Current).Groups[0].Value;
				stringBuilder.Append(value);
				stringBuilder.Append("\n");
			}
			return stringBuilder.ToString();
		}

		private static byte[] StripPkcs7OpenSSLHeader(byte[] buffer)
		{
			string str = "-----BEGIN PKCS7-----";
			string str1 = "-----END PKCS7-----";
			string str2 = Encoding.UTF8.GetString(buffer);
			if (!str2.Contains(str))
			{
				return buffer;
			}
			str2 = str2.Replace(str, "");
			str2 = str2.Replace(str1, "");
			return Convert.FromBase64String(str2.Trim());
		}
	}
}