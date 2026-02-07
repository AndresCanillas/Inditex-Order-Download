using System;
using System.Collections.Generic;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Settings.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class ZebraPrinterZpl : ZebraPrinterA
	{
		private PrinterLanguage language;

		public override PrinterLanguage PrinterControlLanguage
		{
			get
			{
				try
				{
					this.language = this.ObtainLanguage(this.connection);
				}
				catch (Exception)
				{
					this.language = PrinterLanguage.ZPL;
				}
				return this.language;
			}
		}

		public ZebraPrinterZpl(Zebra.Sdk.Comm.Connection connection) : base(connection)
		{
			this.fileUtil = new FileUtilZpl(connection);
			this.formatUtil = new FormatUtilZpl(connection);
			this.graphicsUtil = new GraphicsUtilZpl(connection);
			this.toolsUtil = new ToolsUtilZpl(connection);
		}

		public override PrinterStatus GetCurrentStatus()
		{
			return new PrinterStatusZpl(this.connection);
		}

		private PrinterLanguage GetLanguageViaJson(Zebra.Sdk.Comm.Connection c)
		{
			PrinterLanguage language;
			byte[] numArray = c.SendAndWaitForValidResponse(Encoding.UTF8.GetBytes("{}{\"device.languages\":null}"), c.MaxTimeoutForRead, c.TimeToWaitForMoreData, new JsonValidator());
			try
			{
				language = PrinterLanguage.GetLanguage(StringUtilities.ConvertKeyValueJsonToMap(numArray)["device.languages"]);
			}
			catch (Exception)
			{
				throw new ZebraPrinterLanguageUnknownException(string.Concat("Zebra printer language could not be determined for ", c.ToString()));
			}
			return language;
		}

		private PrinterLanguage GetLanguageViaSgd(Zebra.Sdk.Comm.Connection c)
		{
			return PrinterLanguage.GetLanguage(SGD.GET("device.languages", c));
		}

		private PrinterLanguage ObtainLanguage(Zebra.Sdk.Comm.Connection c)
		{
			return this.language ?? this.QueryPrinterLanguage(c);
		}

		private PrinterLanguage QueryPrinterLanguage(Zebra.Sdk.Comm.Connection c)
		{
			MultichannelConnection multichannelConnection = c as MultichannelConnection;
			MultichannelConnection multichannelConnection1 = multichannelConnection;
			if (multichannelConnection != null)
			{
				if (multichannelConnection1.StatusChannel.Connected)
				{
					return this.GetLanguageViaJson(multichannelConnection1.StatusChannel);
				}
			}
			else if (c is StatusConnection)
			{
				return this.GetLanguageViaJson(c);
			}
			return this.GetLanguageViaSgd(c);
		}

		public override void SetConnection(Zebra.Sdk.Comm.Connection newConnection)
		{
			this.connection = newConnection;
			this.fileUtil = new FileUtilZpl(this.connection);
			this.formatUtil = new FormatUtilZpl(this.connection);
			this.graphicsUtil = new GraphicsUtilZpl(this.connection);
			this.toolsUtil = new ToolsUtilZpl(this.connection);
		}
	}
}