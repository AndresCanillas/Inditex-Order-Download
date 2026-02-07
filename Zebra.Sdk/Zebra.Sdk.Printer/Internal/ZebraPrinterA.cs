using System;
using System.Collections.Generic;
using System.IO;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Device;
using Zebra.Sdk.Graphics;
using Zebra.Sdk.Printer;

namespace Zebra.Sdk.Printer.Internal
{
	internal abstract class ZebraPrinterA : ZebraPrinter, FileUtil, GraphicsUtil, FormatUtil, ToolsUtil
	{
		protected Zebra.Sdk.Comm.Connection connection;

		protected FileUtil fileUtil;

		protected FormatUtil formatUtil;

		protected GraphicsUtil graphicsUtil;

		protected ToolsUtil toolsUtil;

		public Zebra.Sdk.Comm.Connection Connection
		{
			get
			{
				return this.connection;
			}
		}

		public abstract PrinterLanguage PrinterControlLanguage
		{
			get;
		}

		protected ZebraPrinterA(Zebra.Sdk.Comm.Connection connection)
		{
			this.connection = connection;
		}

		public void Calibrate()
		{
			this.toolsUtil.Calibrate();
		}

		public abstract PrinterStatus GetCurrentStatus();

		public FieldDescriptionData[] GetVariableFields(string formatString)
		{
			return this.formatUtil.GetVariableFields(formatString);
		}

		public void PrintConfigurationLabel()
		{
			this.toolsUtil.PrintConfigurationLabel();
		}

		public void PrintImage(string imageFilePath, int x, int y)
		{
			this.graphicsUtil.PrintImage(imageFilePath, x, y);
		}

		public void PrintImage(string imageFilePath, int x, int y, int width, int height, bool insideFormat)
		{
			this.graphicsUtil.PrintImage(imageFilePath, x, y, width, height, insideFormat);
		}

		public void PrintImage(ZebraImageI image, int x, int y, int width, int height, bool insideFormat)
		{
			this.graphicsUtil.PrintImage(image, x, y, width, height, insideFormat);
		}

		public void PrintStoredFormat(string formatPathOnPrinter, string[] vars)
		{
			this.formatUtil.PrintStoredFormat(formatPathOnPrinter, vars);
		}

		public void PrintStoredFormat(string formatPathOnPrinter, string[] vars, string encoding)
		{
			this.formatUtil.PrintStoredFormat(formatPathOnPrinter, vars, encoding);
		}

		public void PrintStoredFormat(string formatPathOnPrinter, Dictionary<int, string> vars)
		{
			this.formatUtil.PrintStoredFormat(formatPathOnPrinter, vars);
		}

		public void PrintStoredFormat(string formatPathOnPrinter, Dictionary<int, string> vars, string encoding)
		{
			this.formatUtil.PrintStoredFormat(formatPathOnPrinter, vars, encoding);
		}

		public void Reset()
		{
			this.toolsUtil.Reset();
		}

		public void RestoreDefaults()
		{
			this.toolsUtil.RestoreDefaults();
		}

		public string[] RetrieveFileNames()
		{
			return this.fileUtil.RetrieveFileNames();
		}

		public string[] RetrieveFileNames(string[] extensions)
		{
			return this.fileUtil.RetrieveFileNames(extensions);
		}

		public byte[] RetrieveFormatFromPrinter(string formatPathOnPrinter)
		{
			return this.formatUtil.RetrieveFormatFromPrinter(formatPathOnPrinter);
		}

		public void RetrieveFormatFromPrinter(Stream formatData, string formatPathOnPrinter)
		{
			this.formatUtil.RetrieveFormatFromPrinter(formatData, formatPathOnPrinter);
		}

		public List<PrinterObjectProperties> RetrieveObjectsProperties()
		{
			return this.fileUtil.RetrieveObjectsProperties();
		}

		public void SendCommand(string command)
		{
			this.toolsUtil.SendCommand(command);
		}

		public void SendCommand(string command, string encoding)
		{
			this.toolsUtil.SendCommand(command, encoding);
		}

		public void SendFileContents(string filePath)
		{
			this.fileUtil.SendFileContents(filePath);
		}

		public void SendFileContents(string filePath, ProgressMonitor handler)
		{
			this.fileUtil.SendFileContents(filePath, handler);
		}

		public abstract void SetConnection(Zebra.Sdk.Comm.Connection newConnection);

		public void StoreImage(string deviceDriveAndFileName, ZebraImageI image, int width, int height)
		{
			this.graphicsUtil.StoreImage(deviceDriveAndFileName, image, width, height);
		}

		public void StoreImage(string deviceDriveAndFileName, string imageFullPath, int width, int height)
		{
			this.graphicsUtil.StoreImage(deviceDriveAndFileName, imageFullPath, width, height);
		}
	}
}