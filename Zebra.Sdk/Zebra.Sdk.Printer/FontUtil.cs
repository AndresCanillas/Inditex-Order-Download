using System;
using System.IO;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       Defines functions used for downloading fonts to Zebra printers.
	///       </summary>
	public interface FontUtil
	{
		/// <summary>
		///       Adds a TrueType® font file to a profile and stores it at the specified path as a TrueType® extension (TTE).
		///       </summary>
		/// <param name="sourceFilePath">Path to a TrueType® font to be added to the profile.</param>
		/// <param name="pathOnPrinter">Location to save the font file in the profile.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		void DownloadTteFont(string sourceFilePath, string pathOnPrinter);

		/// <summary>
		///       Adds a TrueType® font to a profile and stores it at the specified path as a TrueType® extension (TTE).
		///       </summary>
		/// <param name="sourceInputStream">Input Stream containing the font data.</param>
		/// <param name="pathOnPrinter">Location to save the font file in the profile.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		void DownloadTteFont(Stream sourceInputStream, string pathOnPrinter);

		/// <summary>
		///       Adds a TrueType® font file to a profile and stores it at the specified path as a TTF.
		///       </summary>
		/// <param name="sourceFilePath">Path to a TrueType® font to be added to the profile.</param>
		/// <param name="pathOnPrinter">Location to save the font file in the profile.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		void DownloadTtfFont(string sourceFilePath, string pathOnPrinter);

		/// <summary>
		///       Adds a TrueType® font file to a profile and stores it at the specified path as a TTF.
		///       </summary>
		/// <param name="sourceInputStream">Input Stream containing the font data.</param>
		/// <param name="pathOnPrinter">Location to save the font file in the profile.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		void DownloadTtfFont(Stream sourceInputStream, string pathOnPrinter);
	}
}