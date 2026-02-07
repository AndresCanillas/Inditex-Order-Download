using System;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class ToolsUtilZpl : ToolsUtil
	{
		protected Connection connection;

		public ToolsUtilZpl(Connection connection)
		{
			this.connection = connection;
		}

		public void Calibrate()
		{
			this.connection.Write(Encoding.UTF8.GetBytes(ZPLUtilities.PRINTER_CALIBRATE));
		}

		public void PrintConfigurationLabel()
		{
			this.connection.Write(Encoding.UTF8.GetBytes(ZPLUtilities.PRINTER_CONFIG_LABEL));
		}

		public void Reset()
		{
			this.connection.Write(Encoding.UTF8.GetBytes(ZPLUtilities.PRINTER_RESET));
		}

		public void RestoreDefaults()
		{
			this.connection.Write(Encoding.UTF8.GetBytes(ZPLUtilities.PRINTER_RESTORE_DEFAULTS));
		}

		public void SendCommand(string command)
		{
			if (command != null)
			{
				this.connection.Write(Encoding.UTF8.GetBytes(command));
			}
		}

		public void SendCommand(string command, string encoding)
		{
			if (command != null)
			{
				this.connection.Write(Encoding.GetEncoding(encoding).GetBytes(command));
			}
		}
	}
}