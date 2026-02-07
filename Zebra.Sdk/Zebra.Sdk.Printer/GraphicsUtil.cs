using System;
using Zebra.Sdk.Graphics;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       This is an utility class for printing images on a device.
	///       </summary>
	public interface GraphicsUtil
	{
		/// <summary>
		///       Prints an image from the connecting device file system to the connected device as a monochrome image.
		///       </summary>
		/// <param name="imageFilePath">Image file to be printed.</param>
		/// <param name="x">Horizontal starting position in dots.</param>
		/// <param name="y">Vertical starting position in dots.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		/// <exception cref="T:System.IO.IOException">When the file could not be found, opened, or is an unsupported graphic.</exception>
		void PrintImage(string imageFilePath, int x, int y);

		/// <summary>
		///       Prints an image from the connecting device file system to the connected device as a monochrome image.
		///       </summary>
		/// <param name="imageFilePath">Image file to be printed.</param>
		/// <param name="x">Horizontal starting position in dots.</param>
		/// <param name="y">Vertical starting position in dots.</param>
		/// <param name="width">Desired width of the printed image. Passing a value less than 1 will preserve original width.</param>
		/// <param name="height">Desired height of the printed image. Passing a value less than 1 will preserve original height.</param>
		/// <param name="insideFormat">Boolean value indicating whether this image should be printed by itself (false), or is part 
		///       of a format being written to the connection (true).</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		/// <exception cref="T:System.IO.IOException">When the file could not be found, opened, or is an unsupported graphic.</exception>
		void PrintImage(string imageFilePath, int x, int y, int width, int height, bool insideFormat);

		/// <summary>
		///       Prints an image to the connected device as a monochrome image.
		///       </summary>
		/// <param name="image">The image to be printed.</param>
		/// <param name="x">Horizontal starting position in dots.</param>
		/// <param name="y">Vertical starting position in dots.</param>
		/// <param name="width">Desired width of the printed image. Passing a value less than 1 will preserve original width.</param>
		/// <param name="height">Desired height of the printed image. Passing a value less than 1 will preserve original height.</param>
		/// <param name="insideFormat">Boolean value indicating whether this image should be printed by itself (false), or is part 
		///       of a format being written to the connection (true).</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		void PrintImage(ZebraImageI image, int x, int y, int width, int height, bool insideFormat);

		/// <summary>
		///       Stores the specified <c>image</c> to the connected printer as a monochrome image.
		///       </summary>
		/// <param name="deviceDriveAndFileName">Path on the printer where the image will be stored.</param>
		/// <param name="image">The image to be stored on the printer.</param>
		/// <param name="width">Desired width of the printed image, in dots. Passing -1 will preserve original width.</param>
		/// <param name="height">Desired height of the printed image, in dots. Passing -1 will preserve original height.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an issue communicating with the printer (e.g. the connection is not open).</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If <c>printerDriveAndFileName</c> has an incorrect format.</exception>
		void StoreImage(string deviceDriveAndFileName, ZebraImageI image, int width, int height);

		/// <summary>
		///       Stores the specified <c>image</c> to the connected printer as a monochrome image.
		///       </summary>
		/// <param name="deviceDriveAndFileName">Path on the printer where the image will be stored.</param>
		/// <param name="imageFullPath">The image file to be stored on the printer.</param>
		/// <param name="width">Desired width of the printed image, in dots. Passing -1 will preserve original width.</param>
		/// <param name="height">Desired height of the printed image, in dots. Passing -1 will preserve original height.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an issue communicating with the printer (e.g. the connection is not open).</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If <c>printerDriveAndFileName</c> has an incorrect format.</exception>
		/// <exception cref="T:System.IO.IOException">If the file could not be found, opened, or is an unsupported graphic.</exception>
		void StoreImage(string deviceDriveAndFileName, string imageFullPath, int width, int height);
	}
}