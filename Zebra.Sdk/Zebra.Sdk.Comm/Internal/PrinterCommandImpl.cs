using System;
using System.IO;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Settings.Internal;

namespace Zebra.Sdk.Comm.Internal
{
	internal class PrinterCommandImpl : PrinterCommand
	{
		private string command = string.Empty;

		public PrinterCommandImpl(string command)
		{
			this.command = command;
		}

		public byte[] SendAndWaitForResponse(Connection printerConnection)
		{
			return this.SendAndWaitForResponse(printerConnection, printerConnection.MaxTimeoutForRead, printerConnection.TimeToWaitForMoreData);
		}

		public byte[] SendAndWaitForResponse(Connection printerConnection, int maxTimeoutForRead, int timeToWaitForMoreData)
		{
			return this.SendAndWaitForResponse(printerConnection, maxTimeoutForRead, timeToWaitForMoreData, null);
		}

		public byte[] SendAndWaitForResponse(Connection printerConnection, int maxTimeoutForRead, int timeToWaitForMoreData, string terminator)
		{
			return printerConnection.SendAndWaitForResponse(Encoding.UTF8.GetBytes(this.command), maxTimeoutForRead, timeToWaitForMoreData, terminator);
		}

		public void SendAndWaitForResponse(BinaryWriter response, Connection printerConnection)
		{
			printerConnection.SendAndWaitForValidResponse(response, new BinaryReader(new MemoryStream(Encoding.UTF8.GetBytes(this.command))), printerConnection.MaxTimeoutForRead, printerConnection.TimeToWaitForMoreData, null);
		}

		public void SendAndWaitForResponse(BinaryWriter response, Connection printerConnection, int maxTimeoutForRead, int timeToWaitForMoreData, string terminator)
		{
			printerConnection.SendAndWaitForResponse(response, new BinaryReader(new MemoryStream(Encoding.UTF8.GetBytes(this.command))), printerConnection.MaxTimeoutForRead, printerConnection.TimeToWaitForMoreData, terminator);
		}

		public byte[] SendAndWaitForValidJsonResponse(Connection printerConnection)
		{
			return printerConnection.SendAndWaitForValidResponse(Encoding.UTF8.GetBytes(this.command), printerConnection.MaxTimeoutForRead, printerConnection.TimeToWaitForMoreData, new JsonValidator());
		}
	}
}