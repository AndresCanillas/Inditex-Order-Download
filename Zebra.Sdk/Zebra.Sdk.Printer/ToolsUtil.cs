using System;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       This is a utility class for performing printer actions. (Restore defaults, calibrate, etc.)
	///       </summary>
	public interface ToolsUtil
	{
		/// <summary>
		///       Sends the appropriate calibrate command to the printer.
		///       </summary>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		void Calibrate();

		/// <summary>
		///       Sends the appropriate print configuration command to the printer.
		///       </summary>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		void PrintConfigurationLabel();

		/// <summary>
		///       Sends the appropriate reset command to the printer.
		///       </summary>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		void Reset();

		/// <summary>
		///       Sends the appropriate restore defaults command to the printer.
		///       </summary>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		void RestoreDefaults();

		/// <summary>
		///       Converts the specified command to bytes using the default charset and sends the bytes to the printer.
		///       </summary>
		/// <param name="command">The command to send to the printer.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		void SendCommand(string command);

		/// <summary>
		///       Converts the specified command to bytes using the specified charset "encoding" and sends the bytes to the
		///       printer.
		///       </summary>
		/// <param name="command">The command to send to the printer.</param>
		/// <param name="encoding">A character-encoding name (eg. UTF-8).</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		void SendCommand(string command, string encoding);
	}
}