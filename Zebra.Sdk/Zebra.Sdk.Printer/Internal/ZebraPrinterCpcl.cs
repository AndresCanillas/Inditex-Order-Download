using System;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;

namespace Zebra.Sdk.Printer.Internal
{
	internal class ZebraPrinterCpcl : ZebraPrinterA
	{
		private PrinterLanguage language;

		public override PrinterLanguage PrinterControlLanguage
		{
			get
			{
				return this.language;
			}
		}

		public ZebraPrinterCpcl(Zebra.Sdk.Comm.Connection connection, PrinterLanguage language) : base(connection)
		{
			this.language = language;
			this.fileUtil = new FileUtilCpcl(connection);
			this.formatUtil = new FormatUtilCpcl(connection);
			this.graphicsUtil = new GraphicsUtilCpcl(connection);
			this.toolsUtil = new ToolsUtilCpcl(connection);
		}

		public override PrinterStatus GetCurrentStatus()
		{
			return new PrinterStatusCpcl(this.connection);
		}

		public override void SetConnection(Zebra.Sdk.Comm.Connection newConnection)
		{
			this.connection = newConnection;
			this.fileUtil = new FileUtilCpcl(this.connection);
			this.formatUtil = new FormatUtilCpcl(this.connection);
			this.graphicsUtil = new GraphicsUtilCpcl(this.connection);
			this.toolsUtil = new ToolsUtilCpcl(this.connection);
		}
	}
}