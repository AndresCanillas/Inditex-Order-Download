using System;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Device;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       An interface used to obtain various properties of a Zebra printer.
	///       </summary>
	public interface ZebraPrinter : FileUtil, GraphicsUtil, FormatUtil, ToolsUtil
	{
		/// <summary>
		///       Returns the printer's connection.
		///       </summary>
		Zebra.Sdk.Comm.Connection Connection
		{
			get;
		}

		/// <summary>
		///       Returns the printer control language (e.g. ZPL or CPCL) of the printer.
		///       </summary>
		PrinterLanguage PrinterControlLanguage
		{
			get;
		}

		/// <summary>
		///       Returns a new instance of <c>PrinterStatus</c> that can be used to determine the status of a printer.
		///       </summary>
		/// <returns>A new instance of <c>PrinterStatus</c>.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an issue communicating with the printer (e.g. the connection is not
		///       open.)</exception>
		PrinterStatus GetCurrentStatus();

		/// <summary>
		///       Changes the printer's connection.
		///       </summary>
		/// <param name="newConnection">The new connection to be used for communication with the printer.</param>
		void SetConnection(Zebra.Sdk.Comm.Connection newConnection);
	}
}