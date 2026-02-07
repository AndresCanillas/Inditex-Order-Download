using System;
using System.IO;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       A class used to convert TrueType® fonts for use on ZPL printers.
	///       </summary>
	public class FontConverterZpl
	{
		private FontConverterZpl()
		{
		}

		/// <summary>
		///       Returns a <c>Stream</c> which provides the TTE header information as determined from the source stream.
		///       </summary>
		/// <param name="sourceStream">Stream containing TrueType® font data</param>
		/// <param name="pathOnPrinter">Location of the font file on the printer.</param>
		/// <returns>Header information for the provided stream</returns>
		public static Stream GetTteFontHeader(Stream sourceStream, string pathOnPrinter)
		{
			return FontConverterHelper.GetFontHeader(sourceStream, pathOnPrinter, 'E');
		}

		/// <summary>
		///       Returns a <c>Stream</c> which provides the TTF header information as determined from the source stream.
		///       </summary>
		/// <param name="sourceStream">Stream containing TrueType® font data</param>
		/// <param name="pathOnPrinter">Location of the font file on the printer.</param>
		/// <returns>Header information for the provided stream</returns>
		public static Stream GetTtfFontHeader(Stream sourceStream, string pathOnPrinter)
		{
			return FontConverterHelper.GetFontHeader(sourceStream, pathOnPrinter, 'T');
		}

		/// <summary>
		///       Converts a native TrueType® font to a ZPL TTE format.
		///       </summary>
		/// <param name="sourceFilePath">Path to a TrueType® font.</param>
		/// <param name="destinationStream">Destination stream for converted ZPL.</param>
		/// <param name="pathOnPrinter">Location to save the font file on the printer.</param>
		/// <exception cref="T:System.Exception">If the source file is not found</exception>
		public static void SaveAsTtePrinterFont(string sourceFilePath, Stream destinationStream, string pathOnPrinter)
		{
			try
			{
				FontConverterHelper.SaveFontAsPrinterFont(new FileStream(sourceFilePath, FileMode.Open), destinationStream, pathOnPrinter, ".TTE");
			}
			catch (FileNotFoundException fileNotFoundException1)
			{
				FileNotFoundException fileNotFoundException = fileNotFoundException1;
				throw new Exception(fileNotFoundException.Message, fileNotFoundException);
			}
		}

		/// <summary>
		///       Converts a native TrueType® font to a ZPL TTE format.
		///       </summary>
		/// <param name="sourceInputStream">Stream containing the TrueType® font data.</param>
		/// <param name="destinationStream">Destination stream for converted ZPL.</param>
		/// <param name="pathOnPrinter">Location to save the font file on the printer.</param>
		public static void SaveAsTtePrinterFont(Stream sourceInputStream, Stream destinationStream, string pathOnPrinter)
		{
			FontConverterHelper.SaveFontAsPrinterFont(sourceInputStream, destinationStream, pathOnPrinter, ".TTE");
		}

		/// <summary>
		///       Converts a native TrueType® font to a ZPL TTF format.
		///       </summary>
		/// <param name="sourceFilePath">Path to a TrueType® font.</param>
		/// <param name="destinationStream">Destination stream for converted ZPL.</param>
		/// <param name="pathOnPrinter">Location to save the font file on the printer.</param>
		/// <exception cref="T:System.Exception">If the source file is not found</exception>
		public static void SaveAsTtfPrinterFont(string sourceFilePath, Stream destinationStream, string pathOnPrinter)
		{
			try
			{
				using (FileStream fileStream = new FileStream(sourceFilePath, FileMode.Open))
				{
					FontConverterHelper.SaveFontAsPrinterFont(fileStream, destinationStream, pathOnPrinter, ".TTF");
				}
			}
			catch (FileNotFoundException fileNotFoundException1)
			{
				FileNotFoundException fileNotFoundException = fileNotFoundException1;
				throw new Exception(fileNotFoundException.Message, fileNotFoundException);
			}
		}

		/// <summary>
		///       Converts a native TrueType® font to a ZPL TTF format.
		///       </summary>
		/// <param name="sourceInputStream">Stream containing the TrueType® font data.</param>
		/// <param name="destinationStream">Destination stream for converted ZPL.</param>
		/// <param name="pathOnPrinter">Location to save the font file on the printer.</param>
		public static void SaveAsTtfPrinterFont(Stream sourceInputStream, Stream destinationStream, string pathOnPrinter)
		{
			FontConverterHelper.SaveFontAsPrinterFont(sourceInputStream, destinationStream, pathOnPrinter, ".TTF");
		}
	}
}