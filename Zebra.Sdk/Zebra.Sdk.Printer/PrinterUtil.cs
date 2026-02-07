using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Device;
using Zebra.Sdk.Graphics;
using Zebra.Sdk.Graphics.Internal;
using Zebra.Sdk.Printer.Internal;
using Zebra.Sdk.Printer.Operations.Internal;
using Zebra.Sdk.Settings;
using Zebra.Sdk.Settings.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       Numerous utilities to simplify printer operations.
	///       </summary>
	public class PrinterUtil
	{
		private PrinterUtil()
		{
		}

		/// <summary>
		///       Encodes supplied image in either ZPL or CPCL after dithering and resizing.
		///       </summary>
		/// <param name="filePathOnPrinter">The printer file path you wish to store the image to.</param>
		/// <param name="image">ZebraImage to be dithered and resized.</param>
		/// <param name="convertedGraphicOutputStream">Stream to store encoded image data.</param>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If the file type is not supported or an invalid image is supplied.</exception>
		/// <exception cref="T:System.IO.IOException">Could not read/write to file.</exception>
		public static void ConvertGraphic(string filePathOnPrinter, ZebraImageI image, Stream convertedGraphicOutputStream)
		{
			PrinterUtil.ConvertGraphic(filePathOnPrinter, image, 0, 0, convertedGraphicOutputStream);
		}

		/// <summary>
		///       Encodes supplied image in either ZPL or CPCL after dithering and resizing.
		///       </summary>
		/// <param name="filePathOnPrinter">The printer file path you wish to store the image to.</param>
		/// <param name="image">ZebraImage to be dithered and resized.</param>
		/// <param name="width">Width of the resulting image. If 0 the image is not resized.</param>
		/// <param name="height">Height of the resulting image. If 0 the image is not resized.</param>
		/// <param name="convertedGraphicOutputStream">Stream to store converted image data encoded with the printers native
		///       language.</param>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If the file type is not supported or an invalid image is supplied.</exception>
		/// <exception cref="T:System.IO.IOException">Could not read/write to file.</exception>
		public static void ConvertGraphic(string filePathOnPrinter, ZebraImageI image, int width, int height, Stream convertedGraphicOutputStream)
		{
			GraphicsConversionUtil graphicsConversionUtilZpl;
			if (filePathOnPrinter == null || image == null || convertedGraphicOutputStream == null)
			{
				throw new ZebraIllegalArgumentException("Parameter cannot be null");
			}
			if (PrinterUtil.GetPrinterLanguageFromFileExtension(filePathOnPrinter) != PrinterLanguage.CPCL)
			{
				graphicsConversionUtilZpl = new GraphicsConversionUtilZpl();
			}
			else
			{
				graphicsConversionUtilZpl = new GraphicsConversionUtilCpcl();
			}
			graphicsConversionUtilZpl.SendImageToStream(filePathOnPrinter, (ZebraImageInternal)image, width, height, convertedGraphicOutputStream);
		}

		/// <summary>
		///       Create a backup of your printer's settings, alerts, and files.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="profilePath">The location of where to store the profile. The extension must be .zprofile; if it is not, the
		///       method will change it to.zprofile for you.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:System.IO.IOException">If there is an issue creating the profile.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">Could not interpret the response from the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		public static void CreateBackup(string connectionString, string profilePath)
		{
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				linkOsPrinter.CreateBackup(profilePath);
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
		}

		/// <summary>
		///       Create a profile of your printer's settings, alerts, and files for cloning to other printers.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="profilePath">The location of where to store the profile.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:System.IO.IOException">If there is an issue creating the profile.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">Could not interpret the response from the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		public static void CreateProfile(string connectionString, string profilePath)
		{
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				linkOsPrinter.CreateProfile(profilePath);
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
		}

		/// <summary>
		///       Deletes file(s) from the printer and reports what files were actually removed.
		///       </summary>
		/// <param name="connectionString">The connection string. (May be null)</param>
		/// <param name="filePath">The location of the file on the printer. Wildcards are also accepted. (e.g. "E:FORMAT.ZPL", "E:*.*")</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		public static void DeleteFile(string connectionString, string filePath)
		{
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				linkOsPrinter.DeleteFile(filePath);
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
		}

		/// <summary>
		///       Deletes file(s) from the printer and reports what files were actually removed.
		///       </summary>
		/// <param name="connectionString">The connection string. (May be null)</param>
		/// <param name="filePath">The location of the file on the printer. Wildcards are also accepted. (e.g. "E:FORMAT.ZPL", "E:*.*")</param>
		/// <returns>An array of the files which were deleted.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If the format of <c>dateTime</c> is invalid.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		public static string[] DeleteFileReportDeleted(string connectionString, string filePath)
		{
			List<string> strs = new List<string>();
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				strs.AddRange(new List<string>(linkOsPrinter.RetrieveFileNames()));
				linkOsPrinter.DeleteFile(filePath);
				string[] strArrays = linkOsPrinter.RetrieveFileNames();
				for (int i = 0; i < (int)strArrays.Length; i++)
				{
					strs.Remove(strArrays[i]);
				}
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
			return strs.ToArray();
		}

		/// <summary>
		///       Returns a new instance of <c>PrinterStatus</c> that can be used to determine the status of a printer.
		///       </summary>
		/// <param name="printerConnection">Connection to the printer.</param>
		/// <param name="language">Printer control language to be used.</param>
		/// <returns>A new instance of <c>PrinterStatus</c>.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an issue communicating with the printer (e.g.\u00a0the connection is not
		///       open.)</exception>
		public static PrinterStatus GetCurrentStatus(Connection printerConnection, PrinterLanguage language)
		{
			return (new HostStatusOperation(printerConnection, language)).Execute();
		}

		private static ZebraPrinterLinkOs GetLinkOsPrinter(string connectionString)
		{
			Connection connection = ConnectionBuilderInternal.Build(connectionString);
			connection.Open();
			ZebraPrinterLinkOs zebraPrinterLinkO = ZebraPrinterFactory.CreateLinkOsPrinter(ZebraPrinterFactory.GetInstance(connection));
			if (zebraPrinterLinkO == null)
			{
				connection.Close();
				throw new NotALinkOsPrinterException();
			}
			return zebraPrinterLinkO;
		}

		/// <summary>
		///       Retrieves a file from the printer's file system and returns the contents of that file as a byte[].
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="filePathOnPrinter">File to retrieve. (e.g. "R:MYFILE.PNG")</param>
		/// <returns>File contents.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If the filename is invalid or does not exist.</exception>
		public static byte[] GetObjectFromPrinter(string connectionString, string filePathOnPrinter)
		{
			byte[] objectFromPrinter = null;
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				objectFromPrinter = linkOsPrinter.GetObjectFromPrinter(filePathOnPrinter);
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
			return objectFromPrinter;
		}

		/// <summary>
		///       Retrieves a file from the printer's file system and returns the contents of that file as a byte[].
		///       </summary>
		/// <param name="destinationStream">Stream to receive file contents.</param>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="filePathOnPrinter">The file to retrieve. (e.g. "R:MYFILE.PNG")</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If the filename is invalid or does not exist.</exception>
		public static void GetObjectFromPrinter(Stream destinationStream, string connectionString, string filePathOnPrinter)
		{
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				linkOsPrinter.GetObjectFromPrinter(destinationStream, filePathOnPrinter);
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
		}

		/// <summary>
		///       Retrieves a file from the printer's file system and returns the contents of that file as a byte[].
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="filePathOnPrinter">File to retrieve. (e.g. "R:MYFILE.PNG")</param>
		/// <param name="ftpPassword">Password to use for ftp, if null a default password will be used.</param>
		/// <returns>File contents.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device, or the ftp password is not correct.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If the filename is invalid or does not exist.</exception>
		public static byte[] GetObjectFromPrinterViaFtp(string connectionString, string filePathOnPrinter, string ftpPassword)
		{
			byte[] objectFromPrinterViaFtp = null;
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				objectFromPrinterViaFtp = linkOsPrinter.GetObjectFromPrinterViaFtp(filePathOnPrinter, ftpPassword);
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
			return objectFromPrinterViaFtp;
		}

		/// <summary>
		///       Retrieves a file from the printer's file system and returns the contents of that file as a byte[].
		///       </summary>
		/// <param name="destination">Stream to receive the file contents.</param>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="filePathOnPrinter">File to retrieve. (e.g. "R:MYFILE.PNG")</param>
		/// <param name="ftpPassword">Password to use for ftp, if null a default password will be used.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device, or the ftp password is not correct.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If the filename is invalid or does not exist.</exception>
		public static void GetObjectFromPrinterViaFtp(Stream destination, string connectionString, string filePathOnPrinter, string ftpPassword)
		{
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				linkOsPrinter.GetObjectFromPrinterViaFtp(destination, filePathOnPrinter, ftpPassword);
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
		}

		/// <summary>
		///       Retrieves status of the printer odometer which includes the total print length, head clean counter, label dot
		///       length, head new, latch open counter, and both user resettable counters.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <returns>The odometer status.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		public static List<string> GetOdometerStatus(string connectionString)
		{
			List<string> strs = new List<string>();
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				Connection connection = linkOsPrinter.Connection;
				strs.Add(string.Concat("Total Print Length: ", SGD.GET("odometer.total_print_length", connection)));
				strs.Add(string.Concat("Head Clean Count: ", SGD.GET("odometer.headclean", connection)));
				strs.Add(string.Concat("Label Dot Length: ", SGD.GET("odometer.label_dot_length", connection)));
				strs.Add(string.Concat("Head New: ", SGD.GET("odometer.headnew", connection)));
				strs.Add(string.Concat("Latch Open Count: ", SGD.GET("odometer.latch_open_count", connection)));
				strs.Add(string.Concat("User Resettable Counter: ", SGD.GET("odometer.media_marker_count", connection)));
				strs.Add(string.Concat("User Resettable Counter 1: ", SGD.GET("odometer.media_marker_count1", connection)));
				strs.Add(string.Concat("User Resettable Counter 2: ", SGD.GET("odometer.media_marker_count2", connection)));
				strs.Add(string.Concat("User Label Resettable Counter: ", SGD.GET("odometer.user_label_count", connection)));
				strs.Add(string.Concat("User Label Resettable Counter 1: ", SGD.GET("odometer.user_label_count1", connection)));
				strs.Add(string.Concat("User Label Resettable Counter 2: ", SGD.GET("odometer.user_label_count2", connection)));
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
			return strs;
		}

		/// <summary>
		///       Retrieves a list of currently open tcp ports on the printer.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <returns>The port status.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		public static List<TcpPortStatus> GetPortStatus(string connectionString)
		{
			List<TcpPortStatus> portStatus = null;
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				portStatus = linkOsPrinter.GetPortStatus();
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
			return portStatus;
		}

		private static PrinterLanguage GetPrinterLanguageFromFileExtension(string filePathOnPrinter)
		{
			int num = filePathOnPrinter.LastIndexOf('.');
			if (num > 0)
			{
				string upper = filePathOnPrinter.Substring(num + 1).Trim().ToUpper();
				if (upper.Equals("GRF") || upper.Equals("PNG"))
				{
					return PrinterLanguage.ZPL;
				}
				if (upper.Equals("PCX"))
				{
					return PrinterLanguage.CPCL;
				}
			}
			throw new ZebraIllegalArgumentException("Unsupported file type for graphics conversion");
		}

		/// <summary>
		///       Retrieves status of the printer which includes any error messages currently set along with the number of labels
		///       remaining in queue, number of labels remaining in batch, and whether or not a label is currently being processed.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <returns>The printer status.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		public static List<string> GetPrinterStatus(string connectionString)
		{
			List<string> strs = new List<string>();
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				PrinterStatus currentStatus = linkOsPrinter.GetCurrentStatus();
				string[] statusMessage = (new PrinterStatusMessages(currentStatus)).GetStatusMessage();
				if (!currentStatus.isReadyToPrint)
				{
					string[] strArrays = statusMessage;
					for (int i = 0; i < (int)strArrays.Length; i++)
					{
						strs.Add(strArrays[i]);
					}
				}
				else
				{
					strs.Add("Ready To Print");
				}
				strs.Add(string.Concat("Partial format is in progress: ", Convert.ToString(currentStatus.isPartialFormatInProgress)));
				strs.Add(string.Concat("Labels remaining in queue: ", currentStatus.numberOfFormatsInReceiveBuffer));
				strs.Add(string.Concat("Printing ", currentStatus.labelsRemainingInBatch, " labels remaining in current batch"));
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
			return strs;
		}

		/// <summary>
		///       Retrieve all settings and their attributes from the specified printer.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <returns>A map of setting names versus Setting objects from the printer specified in the connection string.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the settings could not be retrieved.</exception>
		public static Dictionary<string, Setting> GetSettingsFromPrinter(string connectionString)
		{
			ZebraPrinterLinkOs linkOsPrinter = null;
			Dictionary<string, Setting> allSettings = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				allSettings = linkOsPrinter.GetAllSettings();
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
			return allSettings;
		}

		/// <summary>
		///       Retrieves the names of the files which are stored on the device.
		///       </summary>
		/// <param name="connectionString">The connection string. (May be null)</param>
		/// <param name="filter">Filter for returned files. (e.g. "E:*.ZPL", "*:*.*", "R:MYFILE.*")</param>
		/// <returns>List of file names on the printer.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If there is an error parsing the directory data returned by the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		public static string[] ListFiles(string connectionString, string filter)
		{
			string[] strArrays = null;
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				strArrays = ZPLUtilities.FilterFileList(linkOsPrinter.RetrieveFileNames(), filter);
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
			return strArrays;
		}

		/// <summary>
		///       Takes settings, alerts, and files from a backup, and applies them to a printer.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="backupPath">Path to the profile to load. (e.g. /home/user/profile.zprofile)</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:System.IO.IOException">If there is an issue creating the profile.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		public static void LoadBackup(string connectionString, string backupPath)
		{
			PrinterUtil.LoadBackup(connectionString, backupPath, false);
		}

		/// <summary>
		///       Takes settings, alerts, and files from a backup, and applies them to a printer.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="backupPath">Path to the profile to load. (e.g. /home/user/profile.zprofile)</param>
		/// <param name="isVerbose">Increases the amount of detail presented to the user.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:System.IO.IOException">If there is an issue creating the profile.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		public static void LoadBackup(string connectionString, string backupPath, bool isVerbose)
		{
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				linkOsPrinter.LoadBackup(backupPath, isVerbose);
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
		}

		/// <summary>
		///       Takes settings, alerts, and files from a profile, and applies them to a printer.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="profilePath">Path to the profile to load. (e.g. /home/user/profile.zprofile)</param>
		/// <param name="filesToDelete">An enum describing which files to delete.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:System.IO.IOException">If there is an issue creating the profile.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		public static void LoadProfile(string connectionString, string profilePath, FileDeletionOption filesToDelete)
		{
			PrinterUtil.LoadProfile(connectionString, profilePath, filesToDelete, false);
		}

		/// <summary>
		///       Takes settings, alerts, and files from a profile, and applies them to a printer.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="profilePath">Path to the profile to load. (e.g. /home/user/profile.zprofile)</param>
		/// <param name="filesToDelete">An enum describing which files to delete.</param>
		/// <param name="isVerbose">Increases the amount of detail presented to the user.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:System.IO.IOException">If there is an issue creating the profile.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		public static void LoadProfile(string connectionString, string profilePath, FileDeletionOption filesToDelete, bool isVerbose)
		{
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				linkOsPrinter.LoadProfile(profilePath, filesToDelete, isVerbose);
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
		}

		/// <summary>
		///       Causes the specified printer to print a configuration label.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		public static void PrintConfigLabel(string connectionString)
		{
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				linkOsPrinter.PrintConfigurationLabel();
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
		}

		/// <summary>
		///       Causes the specified printer to print a directory listing of all the files saved on the printer.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		public static void PrintDirectoryLabel(string connectionString)
		{
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				linkOsPrinter.PrintDirectoryLabel();
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
		}

		/// <summary>
		///       Causes the specified printer to print a network configuration label.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		public static void PrintNetworkConfigLabel(string connectionString)
		{
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				linkOsPrinter.PrintNetworkConfigurationLabel();
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
		}

		/// <summary>
		///       Retrieves the quick status of the printer.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <returns>Highest level error or "Ready to Print".</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		public static string QuickStatus(string connectionString)
		{
			string str = "";
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				PrinterStatus currentStatus = linkOsPrinter.GetCurrentStatus();
				string[] statusMessage = (new PrinterStatusMessages(currentStatus)).GetStatusMessage();
				str = (!currentStatus.isReadyToPrint ? statusMessage[0] : "Ready To Print");
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
			return str;
		}

		/// <summary>
		///       Resets the network of the specified printer.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		public static void ResetNetwork(string connectionString)
		{
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				linkOsPrinter.ResetNetwork();
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
		}

		/// <summary>
		///       Resets the specified printer.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		public static void ResetPrinter(string connectionString)
		{
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				linkOsPrinter.Reset();
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
		}

		/// <summary>
		///       Restores the printer's network settings to their factory default configuration.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		public static void RestoreNetworkDefaults(string connectionString)
		{
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				linkOsPrinter.RestoreNetworkDefaults();
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
		}

		/// <summary>
		///       Restores the printer's settings to their factory default configuration.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		public static void RestorePrinterDefaults(string connectionString)
		{
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				linkOsPrinter.RestoreDefaults();
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
		}

		/// <summary>
		///       Send contents of <c>data</c> directly to the device specified via <c>connectionString</c> using UTF-8 encoding.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="data">Data to send to the printer.</param>
		/// <exception cref="T:System.IO.IOException">If there is an error encoding <c>data</c>.</exception>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		public static void SendContents(string connectionString, string data)
		{
			PrinterUtil.SendContents(connectionString, data, "utf-8");
		}

		/// <summary>
		///       Send contents of <c>data</c> directly to the device specified via <c>connectionString</c> using <c>encoding</c>.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="data">Data to send to the printer.</param>
		/// <param name="encoding">A character-encoding name (eg. UTF-8).</param>
		/// <exception cref="T:System.IO.IOException">If there is an error encoding <c>data</c>.</exception>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		public static void SendContents(string connectionString, string data, string encoding)
		{
			PrinterUtil.SendContents(connectionString, new MemoryStream(Encoding.GetEncoding(encoding).GetBytes(data)));
		}

		/// <summary>
		///       Send contents of <c>data</c> directly to the device specified via <c>connectionString</c> using UTF-8 encoding.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="data">Data to send to the printer.</param>
		/// <exception cref="T:System.IO.IOException">If there is an error encoding <c>data</c>.</exception>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		public static void SendContents(string connectionString, Stream data)
		{
			PrinterUtil.SendContents(connectionString, data, "utf-8");
		}

		/// <summary>
		///       Send contents of <c>data</c> directly to the device specified via <c>connectionString</c> using <c>encoding</c>.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="data">Data to send to the printer.</param>
		/// <param name="encoding">A character-encoding name (eg. UTF-8).</param>
		/// <exception cref="T:System.IO.IOException">If there is an error encoding <c>data</c>.</exception>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		public static void SendContents(string connectionString, Stream data, string encoding)
		{
			Connection connection = null;
			try
			{
				connection = ConnectionBuilderInternal.Build(connectionString);
				connection.Open();
				byte[] numArray = new byte[data.Length];
				data.Read(numArray, 0, (int)numArray.Length);
				connection.Write(numArray);
			}
			finally
			{
				if (connection != null)
				{
					connection.Close();
				}
			}
		}

		/// <summary>
		///       Send contents of <c>data</c> directly to the device specified via <c>connectionString</c> using <c>encoding</c>.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="data">Data to send to the printer.</param>
		/// <param name="encoding">A character-encoding name (eg. UTF-8).</param>
		/// <exception cref="T:System.IO.IOException">If there is an error encoding <c>data</c>.</exception>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		public static void SendJSON(string connectionString, Stream data, string encoding)
		{
			Connection connection = null;
			try
			{
				connection = ConnectionBuilderInternal.Build(connectionString);
				connection.Open();
				connection = ConnectionUtil.SelectConnection(connection);
				byte[] numArray = new byte[data.Length];
				data.Read(numArray, 0, (int)numArray.Length);
				connection.Write(numArray);
			}
			finally
			{
				if (connection != null)
				{
					connection.Close();
				}
			}
		}

		/// <summary>
		///       Set the RTC time and date on the printer.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="dateTime">Format MM-dd-yyyy HH:mm:ss.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If the format of <c>dateTime</c> is invalid.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		public static void SetClock(string connectionString, string dateTime)
		{
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				linkOsPrinter.SetClock(dateTime);
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
		}

		/// <summary>
		///       Stores the file on the printer at the specified location and name using any required file wrappers.
		///       </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="filePath">The path of the file to store.</param>
		/// <param name="remoteName">The path on the printer.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:System.IO.IOException">If there is an issue storing the file.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.NotALinkOsPrinterException">This feature is only available on Link-OS printers.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If there is an issue storing the file.</exception>
		public static void StoreFile(string connectionString, string filePath, string remoteName)
		{
			ZebraPrinterLinkOs linkOsPrinter = null;
			try
			{
				linkOsPrinter = PrinterUtil.GetLinkOsPrinter(connectionString);
				if (remoteName == null)
				{
					linkOsPrinter.StoreFileOnPrinter(filePath);
				}
				else
				{
					linkOsPrinter.StoreFileOnPrinter(filePath, remoteName);
				}
			}
			finally
			{
				if (linkOsPrinter != null)
				{
					linkOsPrinter.Connection.Close();
				}
			}
		}

		/// <summary>
		///       Update the printer firmware.
		///       </summary>
		/// <param name="connection">The connection string.</param>
		/// <param name="firmwareFilePath">File path of firmware file.</param>
		/// <param name="timeout">Timeout in milliseconds. The minimum allowed timeout is 10 minutes (600000ms) due to the need to
		///       reset the printer after flashing the firmware. If a timeout value less than the minimum is provided, the minimum
		///       will be used instead.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If the connection can not be opened or is closed prematurely.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language could not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If an invalid firmware file is specified for the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryException">If an error occurs while waiting for the printer to come back online.</exception>
		/// <exception cref="T:System.TimeoutException">If the maximum timeout is reached prior to the printer coming back online with the new
		///       firmware.</exception>
		/// <exception cref="T:System.IO.FileNotFoundException">If the firmware file cannot be found or cannot be opened.</exception>
		public static void UpdateFirmware(string connection, string firmwareFilePath, long timeout)
		{
			Connection connection1 = ConnectionBuilderInternal.Build(connection);
			try
			{
				connection1.Open();
				ZebraPrinterLinkOs zebraPrinterLinkO = ZebraPrinterFactory.CreateLinkOsPrinter(ZebraPrinterFactory.GetInstance(connection1));
				if (zebraPrinterLinkO != null)
				{
					zebraPrinterLinkO.UpdateFirmware(firmwareFilePath, timeout, new PrinterUtil.FirmwareHandler());
				}
			}
			finally
			{
				connection1.Close();
			}
		}

		private class FirmwareHandler : FirmwareUpdateHandler
		{
			public FirmwareHandler()
			{
			}

			public override void FirmwareDownloadComplete()
			{
			}

			public override void PrinterOnline(ZebraPrinterLinkOs printer, string firmwareVersion)
			{
			}

			public override void ProgressUpdate(int bytesWritten, int totalBytes)
			{
			}
		}
	}
}