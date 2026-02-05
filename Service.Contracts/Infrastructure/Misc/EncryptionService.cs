using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;

/*
How to create and register a self signed X509 certificate
----------------------------------------------------------

	New-SelfSignedCertificate -Subject "CN=Test Code Signing" -Type CodeSigningCert -KeySpec "Signature" -KeyUsage "DigitalSignature" -FriendlyName "Test Code Signing" -NotAfter (get-date).AddYears(5)

	New-SelfSignedCertificate -Subject "CN=SmartdotsClientCert" -FriendlyName "SmartdotsClientCert" -NotAfter (get-date).AddYears(15) -CertStoreLocation "Cert:LocalMachine\MY" -KeyUsage KeyEncipherment,DataEncipherment,KeyAgreement -Type DocumentEncryptionCert
	
Execute CMD as Admin, then run the command below:

Makecert –r –pe –n CN="[SubjectName]" –b 01/01/2017 –e 01/01/2030 –eku 1.3.6.1.5.5.7.3.1 -sky exchange -a sha512 -ss my -sr localmachine

NOTE: Makecert located at: C:\Program Files (x86)\Windows Kits\8.1\bin\x64

From Control Panel go to Administrative Tools / Manager Computer Certificates
Then export the certificate including private key. Note: While exporting you can optionally set a password (remember password).
Then add the resulting .pfx in the application as an embedded resource (prefereably change the extension to .dat).
NOTE: When registering the certificate during installation, remember that if you exported using a password, then you will have to provide the same password when trying to import the certificate.
More details in: http://msdn.microsoft.com/en-us/library/bfsktky3.aspx  
Article titled: Makecert.exe (Certificate Creation Tool)

*/

namespace Service.Contracts
{
	public interface IEncryptionService
	{
		string EncryptString(string str);
		string DecryptString(string cipherText);
	}


	public static class HashFunctions
	{
		public static string SHA512(string input)
		{
			var sha = System.Security.Cryptography.SHA512.Create();//new SHA512Managed();

            var buffer = Encoding.Unicode.GetBytes(input);
			var hash = sha.ComputeHash(buffer);
			return Convert.ToBase64String(hash);
		}
	}


	class EncryptionService: IEncryptionService
	{
		private static string sKey = "asrg2Dgf3SdbdWegjsATVz==";
		private static string sIV = "C5dgJlKowMwQslcIdh46f6==";
		private static string initVector = "$6dgkAfbpqsauJds";
		private static int keySize = 256;

		public EncryptionService()
		{
			try
			{
				X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
				store.Open(OpenFlags.ReadOnly);
				foreach (X509Certificate cert in store.Certificates)
				{
					if (cert.Subject.Contains("SMD"))
					{
						RSA rsa = (cert as X509Certificate2).PrivateKey as RSA;
						var json = Encoding.UTF8.GetString(rsa.Decrypt(Properties.Resources.rijndael, RSAEncryptionPadding.OaepSHA1));
						var keyData = JsonConvert.DeserializeObject<EncryptionKey>(json);
						sKey = keyData.Key;
						sIV = keyData.IV;
						initVector = keyData.Other;
						return;
					}
				}
			}
			catch { }
		}




		/// <summary>
		/// Encrypts the given string
		/// </summary>
		/// <param name="str">String to encrypt.</param>
		public string EncryptString(string str)
		{
			if (str == null || str == "") return "";
			byte[] initVectorBytes = Encoding.ASCII.GetBytes(initVector);
			byte[] saltValueBytes = Encoding.ASCII.GetBytes(sIV);
			byte[] plainTextBytes = Encoding.UTF8.GetBytes(str);
			PasswordDeriveBytes password = new PasswordDeriveBytes(sKey, saltValueBytes);
			byte[] keyBytes = password.GetBytes(keySize / 8);
			RijndaelManaged symmetricKey = new RijndaelManaged();
			symmetricKey.Mode = CipherMode.CBC;
			ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes);
			MemoryStream memoryStream = new MemoryStream();
			CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
			cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
			cryptoStream.FlushFinalBlock();
			byte[] cipherTextBytes = memoryStream.ToArray();
			memoryStream.Close();
			cryptoStream.Close();
			string cipherText = Convert.ToBase64String(cipherTextBytes);
			return cipherText;
		}

		/// <summary>
		/// Decrypts the given text.
		/// </summary>
		/// <param name="cipherText">a string previously encrypted using the EncryptString method.</param>
		public string DecryptString(string cipherText)
		{
			if (cipherText == null || cipherText == "") return "";
			byte[] initVectorBytes = Encoding.ASCII.GetBytes(initVector);
			byte[] saltValueBytes = Encoding.ASCII.GetBytes(sIV);
			byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
			PasswordDeriveBytes password = new PasswordDeriveBytes(sKey, saltValueBytes);
			byte[] keyBytes = password.GetBytes(keySize / 8);
			RijndaelManaged symmetricKey = new RijndaelManaged(); 
			symmetricKey.Mode = CipherMode.CBC;
			ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes);
            using(var memoryStream = new MemoryStream(cipherTextBytes))
            {
                CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                byte[] plainTextBytes = new byte[cipherTextBytes.Length];

                int decryptedByteCount = 0;
                int readbytes = 0;
                while((readbytes = cryptoStream.Read(plainTextBytes, readbytes, plainTextBytes.Length - readbytes)) > 0)
                {
                    decryptedByteCount += readbytes;
                }

                memoryStream.Close();
                cryptoStream.Close();
                string plainText = Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                return plainText;
            }
        }
	}

#pragma warning disable CS0649
	class EncryptionKey
	{
		public string Key;
		public string IV;
		public string Other;
	}
#pragma warning restore CS0649
}
