using System;
using System.Collections.Generic;
using Zebra.Sdk.Printer;

namespace Zebra.Sdk.Device
{
	/// <summary>
	///       This is an utility class for performing file operations on a device.
	///       </summary>
	public interface FileUtil
	{
		/// <summary>
		///       Retrieves the names of the files which are stored on the device.
		///       </summary>
		/// <returns>List of file names.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If there is an error parsing the directory data returned by the device.</exception>
		string[] RetrieveFileNames();

		/// <summary>
		///       Retrieves the names of the files which are stored on the device.
		///       </summary>
		/// <param name="extensions">The extensions to filter on.</param>
		/// <returns>List of file names.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If there is an error parsing the directory data returned by the device.</exception>
		string[] RetrieveFileNames(string[] extensions);

		/// <summary>
		///       Retrieves the properties of the objects which are stored on the device.
		///       </summary>
		/// <returns>The list of objects with their properties.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If there is an error parsing the directory data returned by the device.</exception>
		List<PrinterObjectProperties> RetrieveObjectsProperties();

		/// <summary>
		///       Sends the contents of a file to the device.
		///       </summary>
		/// <param name="filePath">The full file path (e.g. "C:\\Users\\%USERNAME%\\Documents\\sample.lbl").</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an issue communicating with the device (e.g. the connection is not 
		///       open).</exception>
		void SendFileContents(string filePath);

		/// <summary>
		///       Sends the contents of a file to the device.
		///       </summary>
		/// <param name="filePath">The full file path (e.g. "C:\\Users\\%USERNAME%\\Documents\\sample.lbl").</param>
		/// <param name="handler">Callback to update on progress</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an issue communicating with the device (e.g. the connection is not 
		///       open).</exception>
		void SendFileContents(string filePath, ProgressMonitor handler);
	}
}