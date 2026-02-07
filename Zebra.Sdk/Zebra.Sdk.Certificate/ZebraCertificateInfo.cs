using System;
using System.Collections.Generic;
using System.Text;

namespace Zebra.Sdk.Certificate
{
	/// <summary>
	///       Handler class for managing certificate and private key information.
	///       </summary>
	public class ZebraCertificateInfo
	{
		/// <summary>
		///       The file name on a printer to which a private key must be saved for wireless security.
		///       </summary>
		public static string CLIENT_PRIVATE_KEY_NRD_PRINTER_FILE_NAME;

		/// <summary>
		///       The file name on a printer to which a wireless ca must be saved for wireless security.
		///       </summary>
		public static string CA_CERT_NRD_PRINTER_FILE_NAME;

		/// <summary>
		///       The file name on a printer to which a client cert must be saved for wireless security.
		///       </summary>
		public static string CLIENT_CERT_NRD_PRINTER_FILE_NAME;

		private string clientCertificate;

		private List<string> caCertificates;

		private string privateKey;

		/// <summary>
		///       Contains the Certificate Authority certificate chain, if set.
		///       </summary>
		public List<string> CaCertificates
		{
			get
			{
				return this.caCertificates;
			}
		}

		/// <summary>
		///       Contains the client certificate, if set.
		///       </summary>
		public string ClientCertificate
		{
			get
			{
				return this.clientCertificate;
			}
			set
			{
				this.clientCertificate = value;
			}
		}

		/// <summary>
		///       Contains the private key, if set.
		///       </summary>
		public string PrivateKey
		{
			get
			{
				return this.privateKey;
			}
			set
			{
				this.privateKey = value;
			}
		}

		static ZebraCertificateInfo()
		{
			ZebraCertificateInfo.CLIENT_PRIVATE_KEY_NRD_PRINTER_FILE_NAME = "E:PRIVKEY.NRD";
			ZebraCertificateInfo.CA_CERT_NRD_PRINTER_FILE_NAME = "E:CACERTSV.NRD";
			ZebraCertificateInfo.CLIENT_CERT_NRD_PRINTER_FILE_NAME = "E:CERTCLN.NRD";
		}

		/// <summary>
		///       Initializes a new instance of the ZebraCertificateInfo class.
		///       </summary>
		public ZebraCertificateInfo()
		{
			this.caCertificates = new List<string>();
		}

		/// <summary>
		///       Returns the CA Certificate chain.
		///       </summary>
		/// <returns>CA Certificate chain in PEM format.</returns>
		public string GetCaChain()
		{
			if (this.caCertificates.Count == 0)
			{
				return null;
			}
			StringBuilder stringBuilder = new StringBuilder();
			foreach (string caCertificate in this.caCertificates)
			{
				stringBuilder.Append(string.Concat(caCertificate, "\r\n"));
			}
			return stringBuilder.ToString();
		}
	}
}