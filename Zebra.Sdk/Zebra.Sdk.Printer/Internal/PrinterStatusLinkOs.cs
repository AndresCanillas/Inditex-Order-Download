using System;
using System.Collections.Generic;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Settings.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class PrinterStatusLinkOs : PrinterStatusZpl
	{
		protected override byte LineSeparatorChar
		{
			get
			{
				return (byte)10;
			}
		}

		public PrinterStatusLinkOs(Connection connection) : base(connection)
		{
		}

		public static PrinterStatus Create(string hostStatusResponse)
		{
			string str = string.Format("{{\"device.host_status\":\"{0}\"}}", hostStatusResponse);
			return new PrinterStatusLinkOs(new PrinterlessConnection(Encoding.UTF8.GetBytes(str)));
		}

		protected override int FindStartOfHsResponse(byte[] printerStatusAsByteArray)
		{
			return 0;
		}

		protected override byte[] GetStatusInfoFromPrinter()
		{
			byte[] numArray = this.printerConnection.SendAndWaitForValidResponse(Encoding.UTF8.GetBytes("{}{\"device.host_status\":null}"), this.printerConnection.MaxTimeoutForRead, this.printerConnection.TimeToWaitForMoreData, new JsonValidator());
			if (numArray.Length == 0)
			{
				throw new ConnectionException("Malformed status response - unable to determine printer status");
			}
			return this.ParseJsonStatusResponse(Encoding.UTF8.GetString(numArray));
		}

		private byte[] ParseJsonStatusResponse(string responseAsString)
		{
			byte[] bytes;
			try
			{
				string str = StringUtilities.ConvertKeyValueJsonToMap(responseAsString)["device.host_status"].Replace("\\\\r\\\\n", "\n");
				bytes = Encoding.UTF8.GetBytes(str);
			}
			catch (Exception)
			{
				throw new ConnectionException("Malformed status response - unable to determine printer status");
			}
			return bytes;
		}
	}
}