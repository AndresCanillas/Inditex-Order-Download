using System;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class ToolsUtilCpcl : ToolsUtil
	{
		protected Connection connection;

		public ToolsUtilCpcl(Connection connection)
		{
			this.connection = connection;
		}

		public void Calibrate()
		{
			this.connection.Write(Encoding.UTF8.GetBytes(CPCLUtilities.PRINTER_FORM_FEED));
		}

		public void PrintConfigurationLabel()
		{
			this.connection.Write(Encoding.UTF8.GetBytes(CPCLUtilities.PRINTER_CONFIG_LABEL));
		}

		public void Reset()
		{
			SGD.SET("device.reset", "", this.connection);
		}

		public void RestoreDefaults()
		{
			SGD.SET("device.restore_defaults", "display", this.connection);
			this.Reset();
		}

		public void SendCommand(string command)
		{
			if (command != null)
			{
				string str = string.Concat(command, StringUtilities.CRLF);
				this.connection.Write(Encoding.UTF8.GetBytes(str));
			}
		}

		public void SendCommand(string command, string encoding)
		{
			if (command != null)
			{
				string str = string.Concat(command, StringUtilities.CRLF);
				this.connection.Write(Encoding.GetEncoding(encoding).GetBytes(str));
			}
		}
	}
}