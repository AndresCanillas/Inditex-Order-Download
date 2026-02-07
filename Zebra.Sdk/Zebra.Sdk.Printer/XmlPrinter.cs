using System;
using System.IO;
using System.Xml;
using Zebra.Sdk.Printer.Internal;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       A class used to print template formats using XML as input.
	///       </summary>
	public class XmlPrinter
	{
		private XmlPrinter()
		{
		}

		/// <summary>
		///       Print template formats using XML as input data.
		///       </summary>
		/// <param name="sourceDataStream">The source stream containing the XML.</param>
		/// <param name="templateFilename">The template to merge the XML to.</param>
		/// <param name="defaultQuantityString">The quantity, if not specified in the data.</param>
		/// <param name="outputDataStream">Optional stream to send data to.</param>
		/// <exception cref="T:System.IO.IOException">If an I/O error occurs.</exception>
		/// <exception cref="T:System.ArgumentException">If there is an issue with the arguments.</exception>
		public static void Print(Stream sourceDataStream, string templateFilename, string defaultQuantityString, Stream outputDataStream)
		{
			XmlPrinter.Print(null, sourceDataStream, templateFilename, defaultQuantityString, outputDataStream, false);
		}

		/// <summary>
		///       Print template formats using XML as input data to <c>destinationDevice</c>.
		///       </summary>
		/// <param name="destinationDevice">The connection string.</param>
		/// <param name="sourceDataStream">The source stream containing the XML.</param>
		/// <param name="templateFilename">The template to merge the XML to.</param>
		/// <param name="defaultQuantityString">The quantity, if not specified in the data.</param>
		/// <param name="outputDataStream">Optional stream to send data to.<br />
		///       See <a href="../../../../Zebra/Sdk/Comm/ConnectionBuilder.html">ConnectionBuilder</a> for the format of
		///       <c>destinationDevice</c></param>
		/// <exception cref="T:System.IO.IOException">If an I/O error occurs.</exception>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If it was not possible to connect to the device.</exception>
		/// <exception cref="T:System.ArgumentException">If there is an issue with the arguments.</exception>
		public static void Print(string destinationDevice, Stream sourceDataStream, string templateFilename, string defaultQuantityString, Stream outputDataStream)
		{
			XmlPrinter.Print(destinationDevice, sourceDataStream, templateFilename, defaultQuantityString, outputDataStream, false);
		}

		/// <summary>
		///       Print template formats using XML as input data with optional running commentary to standard out.
		///       </summary>
		/// <param name="sourceDataStream">The source stream containing the XML.</param>
		/// <param name="templateFilename">The template to merge the XML to.</param>
		/// <param name="defaultQuantityString">The quantity, if not specified in the data.</param>
		/// <param name="outputDataStream">Optional stream to send data to.</param>
		/// <param name="verbose">If true, print a running commentary to standard out.</param>
		/// <exception cref="T:System.IO.IOException">If an I/O error occurs.</exception>
		/// <exception cref="T:System.ArgumentException">If there is an issue with the arguments.</exception>
		public static void Print(Stream sourceDataStream, string templateFilename, string defaultQuantityString, Stream outputDataStream, bool verbose)
		{
			XmlPrinter.Print(null, sourceDataStream, templateFilename, defaultQuantityString, outputDataStream, verbose);
		}

		/// <summary>
		///       Print template formats using XML as input data to a device with connection string <c>destinationDevice</c>.
		///       </summary>
		/// <param name="destinationDevice">The connection string.</param>
		/// <param name="sourceDataStream">The source stream containing the XML.</param>
		/// <param name="templateFilename">The template to merge the XML to.</param>
		/// <param name="defaultQuantityString">The quantity, if not specified in the data.</param>
		/// <param name="outputDataStream">Optional stream to send data to.</param>
		/// <param name="verbose">If true, print a running commentary to standard out.<br />
		///       See <a href="../../../../Zebra/Sdk/Comm/ConnectionBuilder.html">ConnectionBuilder</a> for the format of
		///       <c>destinationDevice</c>.</param>
		/// <exception cref="T:System.IO.IOException">If an I/O error occurs.</exception>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If it was not possible to connect to the device.</exception>
		/// <exception cref="T:System.ArgumentException">If there is an issue with the arguments.</exception>
		public static void Print(string destinationDevice, Stream sourceDataStream, string templateFilename, string defaultQuantityString, Stream outputDataStream, bool verbose)
		{
			try
			{
				VerbosePrinter verbosePrinter = new VerbosePrinter(verbose);
				verbosePrinter.WriteLine("Starting XML print...");
				verbosePrinter.Write("Converting XML data to CSV...");
				using (Stream stream = XmlToCsvConverter.Convert(sourceDataStream))
				{
					verbosePrinter.WriteLine("done.");
					stream.Position = (long)0;
					CsvPrinter.Print(destinationDevice, stream, templateFilename, defaultQuantityString, outputDataStream, verbose);
				}
			}
			catch (XmlException xmlException)
			{
				throw new ArgumentException(xmlException.Message);
			}
			catch (InvalidOperationException invalidOperationException)
			{
				throw new ArgumentException(invalidOperationException.Message);
			}
		}
	}
}