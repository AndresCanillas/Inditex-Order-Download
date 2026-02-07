using System;
using System.IO;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Comm.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class PrinterConnectionOutputStream : MemoryStream
	{
		private Connection printerConnection;

		public PrinterConnectionOutputStream(string connectionString)
		{
			this.printerConnection = ConnectionBuilderInternal.Build(connectionString);
		}

		public PrinterConnectionOutputStream(Connection printerConnection)
		{
			this.printerConnection = printerConnection;
		}

		public void ClosePrinterConnection()
		{
			this.printerConnection.Close();
		}

		public void OpenPrinterConnection()
		{
			this.printerConnection.Open();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			try
			{
				this.printerConnection.Write(buffer, offset, count);
			}
			catch (ConnectionException connectionException)
			{
				throw new IOException(connectionException.Message);
			}
		}

		public override void WriteByte(byte value)
		{
			throw new IOException("This method is not implemented.");
		}
	}
}