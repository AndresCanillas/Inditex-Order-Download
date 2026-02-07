using System;
using System.Collections.Generic;
using Zebra.Sdk.Graphics;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       Defines functions used for interacting with Link-OS printer formats.
	///       </summary>
	public interface FormatUtilLinkOs
	{
		/// <summary>
		///       Prints a stored format on the printer, filling in the fields specified by the <c>Dictionary</c>.
		///       </summary>
		/// <param name="storedFormatPath">The location of the file on the printer (e.g. "E:FORMAT.ZPL").</param>
		/// <param name="vars">A Dictionary which contains the key/value pairs for the stored format. For ZPL formats, the key number
		///       should correspond directly to the number of the field in the format. For CPCL formats, the values will be passed
		///       in ascending numerical order.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		void PrintStoredFormatWithVarGraphics(string storedFormatPath, Dictionary<int, string> vars);

		/// <summary>
		///       Prints a stored format on the printer, filling in the fields specified by the <c>Dictionary</c>.
		///       </summary>
		/// <param name="storedFormatPath">The location of the file on the printer (e.g. "E:FORMAT.ZPL").</param>
		/// <param name="vars">A Dictionary which contains the key/value pairs for the stored format. For ZPL formats, the key number
		///       should correspond directly to the number of the field in the format. For CPCL formats, the values will be passed
		///       in ascending numerical order.</param>
		/// <param name="encoding">A character-encoding name (e.g. UTF-8)</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		void PrintStoredFormatWithVarGraphics(string storedFormatPath, Dictionary<int, string> vars, string encoding);

		/// <summary>
		///       Prints a stored format on the printer, filling in the fields specified by the Dictionaries.
		///       </summary>
		/// <param name="storedFormatPath">The location of the file on the printer (e.g. "E:FORMAT.ZPL").</param>
		/// <param name="imgVars">A Dictionary which contains the key/value pairs for the images to be used in the stored format. For ZPL
		///       formats, the key number should correspond directly to the number of the field in the format. For CPCL formats,
		///       the values will be passed in ascending numerical order.</param>
		/// <param name="vars">A Dictionary which contains the key/value pairs for the stored format. For ZPL formats, the key number
		///       should correspond directly to the number of the field in the format. For CPCL formats, the values will be passed
		///       in ascending numerical order.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		void PrintStoredFormatWithVarGraphics(string storedFormatPath, Dictionary<int, ZebraImageI> imgVars, Dictionary<int, string> vars);

		/// <summary>
		///       Prints a stored format on the printer, filling in the fields specified by the Dictionaries.
		///       </summary>
		/// <param name="storedFormatPath">The location of the file on the printer (e.g. "E:FORMAT.ZPL").</param>
		/// <param name="imgVars">A Dictionary which contains the key/value pairs for the images to be used in the stored format. For ZPL
		///       formats, the key number should correspond directly to the number of the field in the format. For CPCL formats,
		///       the values will be passed in ascending numerical order.</param>
		/// <param name="vars">A Dictionary which contains the key/value pairs for the stored format. For ZPL formats, the key number
		///       should correspond directly to the number of the field in the format. For CPCL formats, the values will be passed
		///       in ascending numerical order.</param>
		/// <param name="encoding">A character-encoding name (e.g. UTF-8)</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		void PrintStoredFormatWithVarGraphics(string storedFormatPath, Dictionary<int, ZebraImageI> imgVars, Dictionary<int, string> vars, string encoding);
	}
}