using System;
using System.Collections.Generic;
using System.IO;
using Zebra.Sdk.Printer;

namespace Zebra.Sdk.Device
{
	/// <summary>
	///       Utility class for performing file operations on a Zebra Link-OS printer.
	///       </summary>
	public interface FileUtilLinkOs
	{
		/// <summary>
		///       Deletes the file from the printer. The <c>filePath</c> may also contain wildcards.
		///       </summary>
		/// <param name="filePath">The location of the file on the printer. Wildcards are also accepted (e.g. "E:FORMAT.ZPL", "E:*.*")</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		void DeleteFile(string filePath);

		/// <summary>
		///       Retrieves a file from the printer's file system and returns the contents of that file as a byte array.
		///       </summary>
		/// <param name="filePath">The absolute file path on the printer ("E:SAMPLE.TXT").</param>
		/// <returns>The file contents</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an issue communicating with the device 
		///       (e.g. the connection is not open).</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If the filePath is invalid, or if the file does not exist on the printer.</exception>
		byte[] GetObjectFromPrinter(string filePath);

		/// <summary>
		///       Retrieves a file from the printer's file system and writes the contents of that file to destinationStream.
		///       </summary>
		/// <param name="destinationStream">Output stream to receive the file contents</param>
		/// <param name="filePath">The absolute file path on the printer ("E:SAMPLE.TXT").</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an issue communicating with the device 
		///       (e.g. the connection is not open).</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If the filePath is invalid, or if the file does not exist on the printer.</exception>
		void GetObjectFromPrinter(Stream destinationStream, string filePath);

		/// <summary>
		///       Retrieves a file from the printer's file system via FTP and returns the contents of that file as a byte array.
		///       </summary>
		/// <param name="filePath">The absolute file path on the printer ("E:SAMPLE.TXT")</param>
		/// <param name="ftpPassword">The password used to login to FTP, if null, a default password ("1234") will be used.</param>
		/// <returns>The file contents.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer, or the ftpPassword is invalid</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If the filePath is invalid, or if the file does not exist on the printer.</exception>
		byte[] GetObjectFromPrinterViaFtp(string filePath, string ftpPassword);

		/// <summary>
		///       Retrieves a file from the printer's file system via FTP and writes the contents of that file to destinationStream.
		///       </summary>
		/// <param name="destinationStream">The output stream to receive the file contents</param>
		/// <param name="filePath">The absolute file path on the printer ("E:SAMPLE.TXT")</param>
		/// <param name="ftpPassword">The password used to login to FTP, if null, a default password ("1234") will be used.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer, or the ftpPassword is invalid</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If the filePath is invalid, or if the file does not exist on the printer.</exception>
		void GetObjectFromPrinterViaFtp(Stream destinationStream, string filePath, string ftpPassword);

		/// <summary>
		///       Retrieves a file from the printer's file system and returns the contents of that file as a byte array including
		///       all necessary file wrappers for redownloading to a Zebra printer.
		///       </summary>
		/// <param name="filePath">The absolute file path on the printer ("E:SAMPLE.TXT")</param>
		/// <returns>A Zebra printer downloadable file content.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an issue communicating with the device 
		///       (e.g. the connection is not open).</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If the filePath is invalid, or if the file does not exist on the printer.</exception>
		byte[] GetPrinterDownloadableObjectFromPrinter(string filePath);

		/// <summary>
		///       Retrieves storage information for all of the printer's available drives.
		///       </summary>
		/// <returns>A list of objects detailing information about the printer's available drives.</returns>
		List<StorageInfo> GetStorageInfo();

		/// <summary>
		///       Stores the file on the printer using any required file wrappers.
		///       </summary>
		/// <param name="filePath">The full file path (e.g. "C:\\Users\\%USERNAME%\\Documents\\sample.zpl").</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:System.IO.IOException">If there is an issue reading the file</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If filePath cannot be used to create a printer file name.</exception>
		void StoreFileOnPrinter(string filePath);

		/// <summary>
		///       Stores the file on the printer at the specified location and name using any required file wrappers.
		///       </summary>
		/// <param name="filePath">The full file path (e.g. "C:\\Users\\%USERNAME%\\Documents\\sample.zpl").</param>
		/// <param name="fileNameOnPrinter">The full name of the file on the printer (e.g "R:SAMPLE.ZPL").</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:System.IO.IOException">If there is an issue reading the file</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If fileNameOnPrinter is not a legal printer file name.</exception>
		void StoreFileOnPrinter(string filePath, string fileNameOnPrinter);

		/// <summary>
		///       Stores a file on the printer named <c>fileNameOnPrinter</c> with the file contents from 
		///       <c>fileContents</c> using any required file wrappers.
		///       </summary>
		/// <param name="fileContents">The contents of the file to store.</param>
		/// <param name="fileNameOnPrinter">The full name of the file on the printer (e.g "R:SAMPLE.ZPL").</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If fileNameOnPrinter is not a legal printer file name.</exception>
		void StoreFileOnPrinter(byte[] fileContents, string fileNameOnPrinter);
	}
}