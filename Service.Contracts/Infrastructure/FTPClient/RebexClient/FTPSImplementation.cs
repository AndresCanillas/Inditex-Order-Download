using System;
using Rebex.Net;

namespace RebexFtpLib.Client
{
	class FTPSImplementation: FTPImplementation
	{
        /// <summary>
        /// Initializes a new instance of this class with the provided data.
        /// </summary>
        /// <param name="server">The target server name or IP address</param>
        /// <param name="user">User name</param>
        /// <param name="password">Password</param>
        /// <param name="useFTPS">Indicates if connection should be secured.</param>
		/// <param name="allowInvalidCerts"></param>
		/// <param name="keyFile">Allows to provide a path to a keyfile (And automatically changes the component to work in SFTP mode). If left null, no key file will be used an we will be working on regular FTP/FTPS mode.</param>
		public FTPSImplementation(string server, int port, string user, string password, bool allowInvalidCerts)
			: base(server, port, user, password)
		{
			mode = FTPMode.FTPS;
			try
			{
				AllowInvalidCertificates = allowInvalidCerts;
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Attempts to connect to the ftp server. This method will throw an exception if the connection cannot be established.
		/// </summary>
		public override void Connect()
		{
			try
			{
				// Connect securely using explicit SSL. 
				// Use the third argument to specify additional SSL parameters. 
				// For Testing can use: CertificateVerifier.AcceptAll 
				// For Prod must use CertificateVerifier.Default
				TlsParameters parameters = new TlsParameters();


				if (AllowInvalidCertificates)
				{
					parameters.CertificateVerifier = CertificateVerifier.AcceptAll;
				}
				else
				{
					parameters.CertificateVerifier = CertificateVerifier.Default;
				}

				//ftp.Options = FtpOptions.ReuseControlConnectionSession;
				ftp.Settings.ReuseControlConnectionSession = true;
				ftp.Settings.SslAllowedVersions = TlsVersion.Any;
				ftp.Settings.SslServerCertificateVerifier = parameters.CertificateVerifier;
				ftp.Timeout = 600000;

				//ftp.Connect(server, port, parameters, FtpSecurity.Explicit);
				ftp.Connect(server, port, SslMode.Explicit);



				ftp.Login(user, password);
				ftp.SecureTransfers = true;
				ftp.TransferType = FtpTransferType.Binary;
				RaiseOnConnect();
			}
			catch (FtpException ftpe)
			{
                throw ftpe;
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}
	}
}
