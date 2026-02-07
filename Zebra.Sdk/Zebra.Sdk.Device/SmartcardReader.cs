using System;

namespace Zebra.Sdk.Device
{
	/// <summary>
	///       Provides access to the smartcard reader, for printers equipped with one.
	///       </summary>
	public interface SmartcardReader
	{
		/// <summary>
		///       Turns the printer's smartcard reader off, if present.
		///       </summary>
		void Close();

		/// <summary>
		///       Sends a CT_DATA command to the printer's smartcard reader, if present.
		///       </summary>
		/// <param name="asciiHexData">Data to be sent to the smartcard using the CT_DATA card command.</param>
		/// <returns>A byte array containing the response from the smartcard reader.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		byte[] DoCommand(string asciiHexData);

		/// <summary>
		///       Sends a CT_ATR command to the printer's smartcard reader, if present.
		///       </summary>
		/// <returns>A byte array containing the response from the smartcard reader.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		byte[] GetATR();
	}
}