using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Zebra.Sdk.Comm;

namespace Zebra.Sdk.Util.Internal
{
	internal class CisdfFileSender
	{
		private Connection connection;

		private List<PrinterFileDescriptor> fileDescriptors;

		public CisdfFileSender(Connection connection, List<PrinterFileDescriptor> fileDescriptors)
		{
			this.connection = connection;
			this.fileDescriptors = fileDescriptors;
		}

		public static void Send(Connection connection, PrinterFileDescriptor fileDescriptor)
		{
			CisdfFileSender.Send(connection, new List<PrinterFileDescriptor>()
			{
				fileDescriptor
			});
		}

		public static void Send(Connection connection, List<PrinterFileDescriptor> fileDescriptors)
		{
			(new CisdfFileSender(connection, fileDescriptors)).Send();
		}

		private void Send()
		{
			if (this.fileDescriptors == null || this.fileDescriptors.Count == 0)
			{
				throw new ArgumentException("No files to send");
			}
			foreach (PrinterFileDescriptor fileDescriptor in this.fileDescriptors)
			{
				this.SendToPrinter(fileDescriptor);
			}
		}

		private void SendToPrinter(PrinterFileDescriptor fileDescriptor)
		{
			this.connection.Write(Encoding.UTF8.GetBytes(FileWrapper.CreateCisdfHeader(fileDescriptor)));
			if (fileDescriptor.SourceStream.CanSeek)
			{
				fileDescriptor.SourceStream.Position = (long)0;
			}
			FileUtilities.SendFileContentsInChunks(this.connection, fileDescriptor.SourceStream);
			this.connection.Write(FileWrapper.CisdfTrailer);
		}
	}
}