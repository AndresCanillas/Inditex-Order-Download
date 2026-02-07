//using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Printer.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       A class used to print template formats using comma separated values as input data.
	///       </summary>
	public class CsvPrinter
	{
		private CsvPrinter()
		{
		}

		private static Dictionary<int, string> ConvertToKeyedByFieldNumber(string[] sourceData, TemplateInfo templateInfo)
		{
			Dictionary<string, string> strs = CsvPrinterHelper.ParseSingleLineFormat(sourceData);
			Dictionary<int, string> nums = new Dictionary<int, string>();
			foreach (string key in strs.Keys)
			{
				FieldDescriptionData[] fieldDescriptionDataArray = templateInfo.variableFields;
				for (int i = 0; i < (int)fieldDescriptionDataArray.Length; i++)
				{
					FieldDescriptionData fieldDescriptionDatum = fieldDescriptionDataArray[i];
					if (key.Equals(fieldDescriptionDatum.FieldName))
					{
						nums.Add(fieldDescriptionDatum.FieldNumber, strs[key]);
					}
				}
			}
			return nums;
		}

		private static bool CsvDataIsSingleLineWithVariables(List<string[]> csvData)
		{
			if (csvData.Count != 1)
			{
				return false;
			}
			return CsvPrinterHelper.ParseSingleLineFormat(csvData[0]).Count != 0;
		}

		private static bool DoesNotHavePrintChannel(Connection connection)
		{
			bool flag = false;
			MultichannelConnection multichannelConnection = connection as MultichannelConnection;
			MultichannelConnection multichannelConnection1 = multichannelConnection;
			flag = (multichannelConnection == null ? connection is StatusConnection : !multichannelConnection1.PrintingChannel.Connected);
			return flag;
		}

		private static void DoOutput(Connection connection, string templateFilename, string defaultQuantityString, Stream outputDataStream, TemplateInfo templateInfo, Dictionary<int, string> mapInPrintableForm)
		{
			string str = FormatUtilZpl.GenerateStoredFormat(templateInfo.pathOnPrinter, mapInPrintableForm, defaultQuantityString);
			if (outputDataStream != null)
			{
				byte[] bytes = Encoding.UTF8.GetBytes(str);
				outputDataStream.Write(bytes, 0, (int)bytes.Length);
			}
			if (connection != null)
			{
				connection.Write(Encoding.UTF8.GetBytes(str));
			}
		}

		private static bool HasPrintingChannel(Connection connection)
		{
			bool flag = false;
			MultichannelConnection multichannelConnection = connection as MultichannelConnection;
			MultichannelConnection multichannelConnection1 = multichannelConnection;
			flag = (multichannelConnection == null ? !(connection is StatusConnection) : multichannelConnection1.PrintingChannel.Connected);
			return flag;
		}

		private static bool IsChannelInvalidForZpl(Connection connection, PrinterLanguage printerLanguage)
		{
			bool flag = (!CsvPrinter.HasPrintingChannel(connection) ? false : printerLanguage == PrinterLanguage.LINE_PRINT);
			if (!flag)
			{
				flag = CsvPrinter.DoesNotHavePrintChannel(connection);
			}
			return flag;
		}

		private static Connection OpenConnection(string destinationDevice)
		{
			Connection connection = ConnectionBuilderInternal.Build(destinationDevice);
			connection.Open();
			PrinterLanguage zPL = PrinterLanguage.ZPL;
			ZebraPrinterLinkOs linkOsPrinter = ZebraPrinterFactory.GetLinkOsPrinter(connection);
			if (linkOsPrinter != null)
			{
				zPL = linkOsPrinter.PrinterControlLanguage;
			}
			if (CsvPrinter.IsChannelInvalidForZpl(connection, zPL))
			{
				throw new ConnectionException("Cannot send Zpl - printer is in line mode or port is status port.");
			}
			return connection;
		}

		/// <summary>
		///       Print template formats using comma separated values as input data.
		///       </summary>
		/// <param name="sourceDataStream">The source stream containing the CSV data.</param>
		/// <param name="templateFilename">The template to merge the CSV data to.</param>
		/// <param name="defaultQuantityString">The quantity, if not specified in the data.</param>
		/// <param name="outputDataStream">Optional stream to send data to.</param>
		/// <exception cref="T:System.IO.IOException">If an I/O error occurs.</exception>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If it was not possible to connect to the device.</exception>
		public static void Print(Stream sourceDataStream, string templateFilename, string defaultQuantityString, Stream outputDataStream)
		{
			CsvPrinter.Print(null, sourceDataStream, templateFilename, defaultQuantityString, outputDataStream, false);
		}

		/// <summary>
		///       Print template formats using comma separated values as input data.
		///       </summary>
		/// <param name="destinationDevice">The connection string.</param>
		/// <param name="sourceDataStream">The source stream containing the CSV data.</param>
		/// <param name="templateFilename">The template to merge the CSV data to.</param>
		/// <param name="defaultQuantityString">The quantity, if not specified in the data.</param>
		/// <param name="outputDataStream">Optional stream to send data to.</param>
		/// <exception cref="T:System.IO.IOException">If an I/O error occurs.</exception>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If it was not possible to connect to the device.</exception>
		public static void Print(string destinationDevice, Stream sourceDataStream, string templateFilename, string defaultQuantityString, Stream outputDataStream)
		{
			CsvPrinter.Print(destinationDevice, sourceDataStream, templateFilename, defaultQuantityString, outputDataStream, false);
		}

		/// <summary>
		///       Print template formats using comma separated values as input data.
		///       </summary>
		/// <param name="sourceDataStream">The source stream containing the CSV data.</param>
		/// <param name="templateFilename">The template to merge the CSV data to.</param>
		/// <param name="defaultQuantityString">The quantity, if not specified in the data.</param>
		/// <param name="outputDataStream">Optional stream to send data to.</param>
		/// <param name="verbose">If true, print a running commentary to standard out.</param>
		/// <exception cref="T:System.IO.IOException">If an I/O error occurs.</exception>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If it was not possible to connect to the device.</exception>
		public static void Print(Stream sourceDataStream, string templateFilename, string defaultQuantityString, Stream outputDataStream, bool verbose)
		{
			CsvPrinter.Print(null, sourceDataStream, templateFilename, defaultQuantityString, outputDataStream, verbose);
		}

		/// <summary>
		///       Print template formats using comma separated values as input data.
		///       </summary>
		/// <param name="destinationDevice">The connection string.</param>
		/// <param name="sourceDataStream">The source stream containing the CSV data.</param>
		/// <param name="templateFilename">The template to merge the CSV data to.</param>
		/// <param name="defaultQuantityString">The quantity, if not specified in the data.</param>
		/// <param name="outputDataStream">Optional stream to send data to.</param>
		/// <param name="verbose">If true, print a running commentary to standard out.</param>
		/// <exception cref="T:System.IO.IOException">If an I/O error occurs.</exception>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If it was not possible to connect to the device.</exception>
		/// <exception cref="T:System.ArgumentException">If there is an issue with the arguments.</exception>
		public static void Print(string destinationDevice, Stream sourceDataStream, string templateFilename, string defaultQuantityString, Stream outputDataStream, bool verbose)
		{
			throw new NotImplementedException();
			//Connection connection = null;
			//try
			//{
			//	VerbosePrinter verbosePrinter = new VerbosePrinter(verbose);
			//	verbosePrinter.WriteLine("Reading CSV data...");
			//	List<string[]> strArrays = new List<string[]>();
			//	using (StreamReader streamReader = new StreamReader(sourceDataStream, true))
			//	{
			//		using (CsvReader csvReader = new CsvReader(streamReader))
			//		{
			//			if (csvReader.ReadHeader())
			//			{
			//				strArrays.Add(csvReader.FieldHeaders);
			//			}
			//			while (csvReader.Read())
			//			{
			//				strArrays.Add(csvReader.CurrentRecord);
			//			}
			//		}
			//	}
			//	verbosePrinter.WriteLine(string.Format("CSV Data contains {0} lines...", strArrays.Count));
			//	foreach (string[] strArray in strArrays)
			//	{
			//		verbosePrinter.Write(string.Format("This line contains {0} items...", (int)strArray.Length));
			//		string[] strArrays1 = strArray;
			//		for (int i = 0; i < (int)strArrays1.Length; i++)
			//		{
			//			verbosePrinter.Write(string.Format("<{0}>", strArrays1[i]));
			//		}
			//		verbosePrinter.WriteLine("");
			//	}
			//	verbosePrinter.WriteLine("...end of CSV Data");
			//	int[] numArray = new int[] { -1 };
			//	TemplateInfo templateInfo = new TemplateInfo();
			//	templateInfo.Acquire(destinationDevice, templateFilename);
			//	verbosePrinter.WriteLine("Done acquiring template");
			//	if (!string.IsNullOrEmpty(destinationDevice))
			//	{
			//		connection = CsvPrinter.OpenConnection(destinationDevice);
			//	}
			//	int[] numArray1 = new int[(int)templateInfo.variableFields.Length];
			//	for (int j = 0; j < (int)numArray1.Length; j++)
			//	{
			//		numArray1[j] = j;
			//	}
			//	if (templateInfo.isLocalToComputer && connection != null)
			//	{
			//		connection.Write(FileReader.ToByteArray(templateFilename));
			//	}
			//	if (!CsvPrinter.CsvDataIsSingleLineWithVariables(strArrays))
			//	{
			//		verbosePrinter.WriteLine("Is not single line w/variables");
			//		try
			//		{
			//			verbosePrinter.WriteLine("Getting first line of data...");
			//			string[] item = strArrays[0];
			//			verbosePrinter.WriteLine("...extracted first line of data");
			//			numArray1 = CsvPrinterHelper.ExtractFdsByColumnHeading(templateInfo.variableFields, item, numArray);
			//			verbosePrinter.WriteLine("Done extractFdsByColumnHeading");
			//			strArrays.RemoveAt(0);
			//		}
			//		catch (UseDefaultMappingException useDefaultMappingException)
			//		{
			//		}
			//		verbosePrinter.WriteLine("Starting CSV processing...");
			//		foreach (string[] strArray1 in strArrays)
			//		{
			//			Dictionary<int, string> nums = new Dictionary<int, string>();
			//			if ((int)templateInfo.variableFields.Length > (int)strArray1.Length)
			//			{
			//				continue;
			//			}
			//			for (int k = 0; k < (int)templateInfo.variableFields.Length; k++)
			//			{
			//				nums.Add(templateInfo.variableFields[k].FieldNumber, strArray1[numArray1[k]]);
			//			}
			//			CsvPrinter.DoOutput(connection, templateFilename, (numArray[0] >= 0 ? strArray1[numArray[0]] : defaultQuantityString), outputDataStream, templateInfo, nums);
			//			verbosePrinter.WriteLine("...printed a line of CSV");
			//		}
			//		verbosePrinter.WriteLine("Done processing CSV");
			//	}
			//	else
			//	{
			//		verbosePrinter.WriteLine("Is single line w/variables");
			//		Dictionary<int, string> keyedByFieldNumber = CsvPrinter.ConvertToKeyedByFieldNumber(strArrays[0], templateInfo);
			//		verbosePrinter.WriteLine("Done ConvertToKeyedByFieldNumber");
			//		CsvPrinter.DoOutput(connection, templateFilename, defaultQuantityString, outputDataStream, templateInfo, keyedByFieldNumber);
			//		verbosePrinter.WriteLine("Printed the line of CSV");
			//	}
			//}
			//finally
			//{
			//	if (connection != null)
			//	{
			//		connection.Close();
			//	}
			//}
		}
	}
}