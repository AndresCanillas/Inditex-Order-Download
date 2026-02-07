using System;
using System.Collections.Generic;
using System.IO;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       Defines functions used for interacting with printer formats.
	///       </summary>
	public interface FormatUtil
	{
		/// <summary>
		///       Returns a list of descriptors of the variable fields in this format.
		///       </summary>
		/// <param name="formatString">The contents of the recalled format.</param>
		/// <returns>A list of field data descriptors. For a CPCL printer, the nth element of the list will contain the
		///       integer n and no name. For a LinkOS/ZPL printer, each element will contain an ^FN number and a variable name if
		///       present. If the format contains multiple ^FNs with the same number, only the last one will be in the result.<br />
		///       See <see cref="T:Zebra.Sdk.Printer.FieldDescriptionData" /> for an example of how variable fields look.
		///       </returns>
		FieldDescriptionData[] GetVariableFields(string formatString);

		/// <summary>
		///       Prints a stored format on the printer, filling in the fields specified by the array.
		///       </summary>
		/// <param name="formatPathOnPrinter">The name of the format on the printer, including the extension (e.g. "E:FORMAT.ZPL").</param>
		/// <param name="vars">An array of strings representing the data to fill into the format. For LinkOS/ZPL printer formats,
		///       index 0 of the array corresponds to field number 2 (^FN2). For CPCL printer formats, the variables are passed in
		///       the order that they are found in the format.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		void PrintStoredFormat(string formatPathOnPrinter, string[] vars);

		/// <summary>
		///       Prints a stored format on the printer, filling in the fields specified by the array.
		///       </summary>
		/// <param name="formatPathOnPrinter">The location of the file on the printer (e.g. "E:FORMAT.ZPL").</param>
		/// <param name="vars">An array of strings representing the data to fill into the format. For LinkOS/ZPL printer formats, 
		///       index 0 of the array corresponds to field number 2 (^FN2). For CPCL printer formats, the variables are passed in
		///       the order that they are found in the format.</param>
		/// <param name="encoding">A character-encoding name (eg. UTF-8).</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		/// <exception cref="T:System.ArgumentException">If the encoding is not supported.</exception>
		void PrintStoredFormat(string formatPathOnPrinter, string[] vars, string encoding);

		/// <summary>
		///       Prints a stored format on the printer, filling in the fields specified by the Dictionary.
		///       </summary>
		/// <param name="formatPathOnPrinter">The location of the file on the printer (e.g. "E:FORMAT.ZPL").</param>
		/// <param name="vars">A Dictionary which contains the key/value pairs for the stored format. For LinkOS/ZPL printer formats, the
		///       key number should correspond directly to the number of the field in the format.For CPCL printer formats, the
		///       values will be passed in ascending numerical order.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		void PrintStoredFormat(string formatPathOnPrinter, Dictionary<int, string> vars);

		/// <summary>
		///       Prints a stored format on the printer, filling in the fields specified by the Dictionary.
		///       </summary>
		/// <param name="formatPathOnPrinter">The location of the file on the printer (e.g. "E:FORMAT.ZPL").</param>
		/// <param name="vars">A Dictionary which contains the key/value pairs for the stored format. For LinkOS/ZPL printer formats, the
		///       key number should correspond directly to the number of the field in the format.For CPCL printer formats, the
		///       values will be passed in ascending numerical order.</param>
		/// <param name="encoding">A character-encoding name (e.g. UTF-8).</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		/// <exception cref="T:System.ArgumentException">If the encoding is not supported.</exception>
		void PrintStoredFormat(string formatPathOnPrinter, Dictionary<int, string> vars, string encoding);

		/// <summary>
		///       Retrieves a format from the printer.
		///       </summary>
		/// <param name="formatPathOnPrinter">The location of the file on the printer (e.g. "E:FORMAT.ZPL").</param>
		/// <returns>The contents of the format file.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an issue communicating with the printer (e.g. the connection is not open).</exception>
		byte[] RetrieveFormatFromPrinter(string formatPathOnPrinter);

		/// <summary>
		///       Retrieves a format from the printer.
		///       </summary>
		/// <param name="formatData">The format.</param>
		/// <param name="formatPathOnPrinter">The location of the file on the printer (e.g. "E:FORMAT.ZPL").</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an issue communicating with the printer (e.g. the connection is not open).</exception>
		void RetrieveFormatFromPrinter(Stream formatData, string formatPathOnPrinter);
	}
}