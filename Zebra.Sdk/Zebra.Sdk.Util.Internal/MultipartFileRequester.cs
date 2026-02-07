using System;
using System.Text;
using Zebra.Sdk.Comm;

namespace Zebra.Sdk.Util.Internal
{
	internal class MultipartFileRequester
	{
		private Connection connection;

		private string fullFilePath;

		public MultipartFileRequester(Connection connection, string fullFilePath)
		{
			this.connection = connection;
			this.fullFilePath = fullFilePath;
		}

		private string GenerateBoundary()
		{
			Guid guid = Guid.NewGuid();
			string str = guid.ToString().Replace("-", "Z");
			return string.Concat(str, str).Substring(0, 65);
		}

		public static void Send(Connection connection, string fullFilePath)
		{
			(new MultipartFileRequester(connection, fullFilePath)).Send();
		}

		private void Send()
		{
			if (this.fullFilePath == null || this.fullFilePath.Length == 0)
			{
				throw new ArgumentException("No file name specified");
			}
			this.SendToPrinter();
		}

		private void SendToPrinter()
		{
			string str = this.GenerateBoundary();
			string str1 = string.Format("{{}}--{0}\r\nContent-Disposition: filename=\"{1}\"; action=\"retrieve\"\r\nContent-Type: application/octet-stream\r\nContent-Transfer-Encoding: binary\r\n\r\n\r\n--{2}--\r\n\r\n", str, this.fullFilePath, str);
			this.connection.Write(Encoding.UTF8.GetBytes(str1));
		}
	}
}