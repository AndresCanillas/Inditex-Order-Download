using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Device;
using Zebra.Sdk.Graphics;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Operations.Internal;
using Zebra.Sdk.Settings;
using Zebra.Sdk.Settings.Internal;
using Zebra.Sdk.Util.FileConversion.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class ZebraPrinterLinkOsImpl : ZebraPrinterLinkOs, ZebraPrinter, FileUtil, GraphicsUtil, FormatUtil, ToolsUtil, Zebra.Sdk.Device.Device, SettingsProvider, ProfileUtil, FontUtil, AlertProvider, FileUtilLinkOs, FormatUtilLinkOs, ToolsUtilLinkOs, FirmwareUpdaterLinkOs
	{
		private ZebraPrinter genericPrinter;

		public ZebraSettingsListFromConnection settings;

		protected ProfileUtil profile;

		protected AlertsUtilLinkOs alerts;

		protected FileUtilLinkOs fileUtil;

		protected FormatUtilLinkOs formatUtil;

		protected ToolsUtilLinkOsHelper toolsUtilHelper;

		protected FirmwareUpdaterLinkOs fwDownloader;

		private string communityName;

		private Zebra.Sdk.Printer.LinkOsInformation linkOsInformation;

		private PrinterLanguage language;

		public string CommunityName
		{
			get
			{
				return this.communityName;
			}
			set
			{
				this.communityName = value;
			}
		}

		public Zebra.Sdk.Comm.Connection Connection
		{
			get
			{
				return this.genericPrinter.Connection;
			}
		}

		public Zebra.Sdk.Printer.LinkOsInformation LinkOsInformation
		{
			get
			{
				return this.linkOsInformation;
			}
		}

		public PrinterLanguage PrinterControlLanguage
		{
			get
			{
				return this.language;
			}
		}

		public ZebraPrinterLinkOsImpl(ZebraPrinter genericPrinter, Zebra.Sdk.Printer.LinkOsInformation linkOsVersion, PrinterLanguage language)
		{
			this.linkOsInformation = linkOsVersion;
			this.language = language;
			this.Init(genericPrinter);
		}

		public ZebraPrinterLinkOsImpl(Zebra.Sdk.Comm.Connection c, Zebra.Sdk.Printer.LinkOsInformation linkOsVersion, PrinterLanguage language)
		{
			this.linkOsInformation = linkOsVersion;
			this.language = language;
			this.Init(new ZebraPrinterZpl(c));
		}

		public void Calibrate()
		{
			(new PrinterCalibrator(this.Connection, this.PrinterControlLanguage)).Execute();
		}

		public void ConfigureAlert(PrinterAlert alert)
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			this.ConfigureAlerts(new List<PrinterAlert>(new PrinterAlert[] { alert }));
		}

		public void ConfigureAlerts(List<PrinterAlert> alerts)
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			this.alerts.SetAlerts(alerts);
		}

		public void CreateBackup(string pathToOutputFile)
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			this.profile.CreateBackup(pathToOutputFile);
		}

		public void CreateProfile(string pathToOutputFile)
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			this.profile.CreateProfile(pathToOutputFile);
		}

		public void CreateProfile(Stream profileDestinationStream)
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			this.profile.CreateProfile(profileDestinationStream);
		}

		public void DeleteFile(string filePath)
		{
			this.fileUtil.DeleteFile(filePath);
		}

		private void DownloadFont(string sourcePath, string pathOnPrinter)
		{
			try
			{
				using (FileStream fileStream = new FileStream(sourcePath, FileMode.Open))
				{
					this.DownloadFont(fileStream, pathOnPrinter);
				}
			}
			catch (FileNotFoundException fileNotFoundException)
			{
				throw new ConnectionException(fileNotFoundException.Message);
			}
		}

		private void DownloadFont(Stream fontInputStream, string pathOnPrinter)
		{
			try
			{
				List<PrinterFileDescriptor> printerFileDescriptors = new List<PrinterFileDescriptor>();
				if (fontInputStream is FileStream)
				{
					PrinterFileMetadata printerFileMetadatum = new PrinterFileMetadata(fontInputStream.Length, "0000", "0000");
					using (PrinterFileDescriptor printerFileDescriptor = new PrinterFileDescriptor(fontInputStream, pathOnPrinter, printerFileMetadatum))
					{
						printerFileDescriptors.Add(printerFileDescriptor);
						(new FileStorer(printerFileDescriptors, this.Connection, this.PrinterControlLanguage, this.linkOsInformation)).Execute();
					}
				}
				else if (!fontInputStream.CanSeek)
				{
					using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
					{
						for (int i = 0; (long)i < fontInputStream.Length; i++)
						{
							binaryWriter.Write((byte)fontInputStream.ReadByte());
						}
						binaryWriter.BaseStream.Position = (long)0;
						PrinterFileMetadata printerFileMetadatum1 = new PrinterFileMetadata(binaryWriter.BaseStream);
						using (PrinterFileDescriptor printerFileDescriptor1 = new PrinterFileDescriptor(binaryWriter.BaseStream, pathOnPrinter, printerFileMetadatum1))
						{
							printerFileDescriptors.Add(printerFileDescriptor1);
							(new FileStorer(printerFileDescriptors, this.Connection, this.PrinterControlLanguage, this.linkOsInformation)).Execute();
						}
					}
				}
				else
				{
					PrinterFileMetadata printerFileMetadatum2 = new PrinterFileMetadata(fontInputStream);
					fontInputStream.Position = (long)0;
					using (PrinterFileDescriptor printerFileDescriptor2 = new PrinterFileDescriptor(fontInputStream, pathOnPrinter, printerFileMetadatum2))
					{
						printerFileDescriptors.Add(printerFileDescriptor2);
						(new FileStorer(printerFileDescriptors, this.Connection, this.PrinterControlLanguage, this.linkOsInformation)).Execute();
					}
				}
			}
			catch (FileNotFoundException fileNotFoundException)
			{
				throw new ConnectionException(fileNotFoundException.Message);
			}
			catch (IOException oException)
			{
				throw new ConnectionException(oException.Message);
			}
		}

		public void DownloadTteFont(string sourceFilePath, string pathOnPrinter)
		{
			this.DownloadFont(sourceFilePath, FileUtilities.ChangeExtension(pathOnPrinter, ".TTE"));
		}

		public void DownloadTteFont(Stream sourceInputStream, string pathOnPrinter)
		{
			this.DownloadFont(sourceInputStream, FileUtilities.ChangeExtension(pathOnPrinter, ".TTE"));
		}

		public void DownloadTtfFont(string sourceFilePath, string pathOnPrinter)
		{
			this.DownloadFont(sourceFilePath, FileUtilities.ChangeExtension(pathOnPrinter, ".TTF"));
		}

		public void DownloadTtfFont(Stream sourceInputStream, string pathOnPrinter)
		{
			this.DownloadFont(sourceInputStream, FileUtilities.ChangeExtension(pathOnPrinter, ".TTF"));
		}

		public Dictionary<string, Setting> GetAllSettings()
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			return this.settings.GetAllSettings().Cast<DictionaryEntry>().ToDictionary<DictionaryEntry, string, Setting>((DictionaryEntry k) => (string)k.Key, (DictionaryEntry v) => (Setting)v.Value);
		}

		public Dictionary<string, string> GetAllSettingValues()
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			return this.settings.GetAllSettingValues();
		}

		public HashSet<string> GetAvailableSettings()
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			return this.settings.GetAllSettingIds();
		}

		public List<PrinterAlert> GetConfiguredAlerts()
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			return this.alerts.GetAlerts();
		}

		public PrinterStatus GetCurrentStatus()
		{
			return (new HostStatusOperation(this.Connection, this.PrinterControlLanguage)).Execute();
		}

		protected virtual FirmwareUpdaterLinkOs GetFirmwareDownloader(ZebraPrinterLinkOs zebraPrinterLinkOs)
		{
			return new FirmwareUpdaterLinkOsBase(zebraPrinterLinkOs);
		}

		public byte[] GetObjectFromPrinter(string filePath)
		{
			return this.fileUtil.GetObjectFromPrinter(filePath);
		}

		public void GetObjectFromPrinter(Stream destinationStream, string filePath)
		{
			this.fileUtil.GetObjectFromPrinter(destinationStream, filePath);
		}

		public byte[] GetObjectFromPrinterViaFtp(string filePath, string ftpPassword)
		{
			return this.fileUtil.GetObjectFromPrinterViaFtp(filePath, ftpPassword);
		}

		public void GetObjectFromPrinterViaFtp(Stream destinationStream, string filePath, string ftpPassword)
		{
			this.fileUtil.GetObjectFromPrinterViaFtp(destinationStream, filePath, ftpPassword);
		}

		public List<TcpPortStatus> GetPortStatus()
		{
			return PortStatus.GetPortStatus(this.genericPrinter.Connection, this.CommunityName);
		}

		public byte[] GetPrinterDownloadableObjectFromPrinter(string filePath)
		{
			return this.fileUtil.GetPrinterDownloadableObjectFromPrinter(filePath);
		}

		public string GetSettingRange(string settingId)
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			return this.settings.GetSettingRange(settingId);
		}

		public Dictionary<string, string> GetSettingsValues(List<string> listOfSettings)
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			return this.settings.GetValues(listOfSettings);
		}

		public string GetSettingType(string settingId)
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			return this.settings.GetSettingType(settingId);
		}

		public string GetSettingValue(string settingId)
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			return this.settings.GetValue(settingId);
		}

		public List<StorageInfo> GetStorageInfo()
		{
			return this.fileUtil.GetStorageInfo();
		}

		public FieldDescriptionData[] GetVariableFields(string formatString)
		{
			return this.genericPrinter.GetVariableFields(formatString);
		}

		private bool HasSupplementalData(string pathToOutputFile)
		{
			return (new ZipUtil(pathToOutputFile)).ExtractEntry("profileSupplement.txt").Length != 0;
		}

		private void Init(ZebraPrinter genericPrinter)
		{
			this.genericPrinter = genericPrinter;
			this.alerts = new AlertsUtilLinkOs(this);
			this.profile = new ProfileUtilLinkOsImpl(this);
			this.fileUtil = new FileUtilLinkOsImpl(this);
			this.formatUtil = new FormatUtilLinkOsImpl(this);
			this.toolsUtilHelper = new ToolsUtilLinkOsHelper(genericPrinter.Connection, this.language);
			this.fwDownloader = this.GetFirmwareDownloader(this);
			this.settings = new ZebraSettingsListFromConnection(genericPrinter.Connection);
			this.communityName = "public";
		}

		private bool IsOnlySettingsChannelOpen(MultichannelConnection multiChannelConnection)
		{
			if (!multiChannelConnection.StatusChannel.Connected)
			{
				return false;
			}
			return !multiChannelConnection.PrintingChannel.Connected;
		}

		public bool IsSettingReadOnly(string settingId)
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			return this.settings.IsSettingReadOnly(settingId);
		}

		public bool IsSettingValid(string settingId, string value)
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			return this.settings.IsSettingValid(settingId, value);
		}

		public bool IsSettingWriteOnly(string settingId)
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			return this.settings.IsSettingWriteOnly(settingId);
		}

		public void LoadBackup(string pathToBackup)
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			this.ThrowExceptionSupplementalDataStatusOnly(pathToBackup);
			this.profile.LoadBackup(pathToBackup);
		}

		public void LoadBackup(string pathToBackup, bool isVerbose)
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			this.ThrowExceptionSupplementalDataStatusOnly(pathToBackup);
			this.profile.LoadBackup(pathToBackup, isVerbose);
		}

		public void LoadProfile(string pathToProfile)
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			this.ThrowExceptionSupplementalDataStatusOnly(pathToProfile);
			this.profile.LoadProfile(pathToProfile);
		}

		public void LoadProfile(string pathToProfile, FileDeletionOption filesToDelete, bool isVerbose)
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			this.ThrowExceptionSupplementalDataStatusOnly(pathToProfile);
			this.profile.LoadProfile(pathToProfile, filesToDelete, isVerbose);
		}

		public void PrintConfigurationLabel()
		{
			(new ConfigurationLabelPrinter(this.Connection, this.PrinterControlLanguage, this.linkOsInformation)).Execute();
		}

		public void PrintDirectoryLabel()
		{
			this.ThrowExceptionInLinePrintMode();
			this.toolsUtilHelper.PrintDirectoryLabel();
		}

		public void PrintImage(string imageFilePath, int x, int y)
		{
			this.ThrowExceptionInLinePrintMode();
			this.ThrowExceptionStatusOnly();
			this.genericPrinter.PrintImage(imageFilePath, x, y);
		}

		public void PrintImage(string imageFilePath, int x, int y, int width, int height, bool insideFormat)
		{
			this.ThrowExceptionInLinePrintMode();
			this.ThrowExceptionStatusOnly();
			this.genericPrinter.PrintImage(imageFilePath, x, y, width, height, insideFormat);
		}

		public void PrintImage(ZebraImageI image, int x, int y, int width, int height, bool insideFormat)
		{
			this.ThrowExceptionInLinePrintMode();
			this.ThrowExceptionStatusOnly();
			this.genericPrinter.PrintImage(image, x, y, width, height, insideFormat);
		}

		public void PrintNetworkConfigurationLabel()
		{
			(new NetworkConfigurationLabelPrinter(this.Connection, this.PrinterControlLanguage, this.linkOsInformation)).Execute();
		}

		public void PrintStoredFormat(string formatPathOnPrinter, string[] vars)
		{
			this.ThrowExceptionInLinePrintMode();
			this.genericPrinter.PrintStoredFormat(formatPathOnPrinter, vars);
		}

		public void PrintStoredFormat(string formatPathOnPrinter, string[] vars, string encoding)
		{
			this.ThrowExceptionInLinePrintMode();
			this.genericPrinter.PrintStoredFormat(formatPathOnPrinter, vars, encoding);
		}

		public void PrintStoredFormat(string formatPathOnPrinter, Dictionary<int, string> vars)
		{
			this.ThrowExceptionInLinePrintMode();
			this.genericPrinter.PrintStoredFormat(formatPathOnPrinter, vars);
		}

		public void PrintStoredFormat(string formatPathOnPrinter, Dictionary<int, string> vars, string encoding)
		{
			this.ThrowExceptionInLinePrintMode();
			this.genericPrinter.PrintStoredFormat(formatPathOnPrinter, vars, encoding);
		}

		public void PrintStoredFormatWithVarGraphics(string formatPathOnPrinter, Dictionary<int, string> vars)
		{
			this.ThrowExceptionInLinePrintMode();
			this.ThrowExceptionStatusOnly();
			this.formatUtil.PrintStoredFormatWithVarGraphics(formatPathOnPrinter, vars);
		}

		public void PrintStoredFormatWithVarGraphics(string formatPathOnPrinter, Dictionary<int, string> vars, string encoding)
		{
			this.ThrowExceptionInLinePrintMode();
			this.ThrowExceptionStatusOnly();
			this.formatUtil.PrintStoredFormatWithVarGraphics(formatPathOnPrinter, vars, encoding);
		}

		public void PrintStoredFormatWithVarGraphics(string formatPathOnPrinter, Dictionary<int, ZebraImageI> imgVars, Dictionary<int, string> vars)
		{
			this.ThrowExceptionInLinePrintMode();
			this.ThrowExceptionStatusOnly();
			this.formatUtil.PrintStoredFormatWithVarGraphics(formatPathOnPrinter, imgVars, vars);
		}

		public void PrintStoredFormatWithVarGraphics(string formatPathOnPrinter, Dictionary<int, ZebraImageI> imgVars, Dictionary<int, string> vars, string encoding)
		{
			this.ThrowExceptionInLinePrintMode();
			this.ThrowExceptionStatusOnly();
			this.formatUtil.PrintStoredFormatWithVarGraphics(formatPathOnPrinter, imgVars, vars, encoding);
		}

		public Dictionary<string, string> ProcessSettingsViaMap(Dictionary<string, string> settingValuePairs)
		{
			return (new SettingsUpdaterOperation(this.Connection, settingValuePairs, this.PrinterControlLanguage)).Process();
		}

		public void RemoveAlert(PrinterAlert alert)
		{
			if (alert != null)
			{
				this.ConfigureAlert(new PrinterAlert(alert.Condition, alert.Destination, false, false, "", 0, false));
			}
		}

		public void RemoveAllAlerts()
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			this.alerts.RemoveAllAlerts();
		}

		public void Reset()
		{
			(new PrinterResetter(this.Connection, this.PrinterControlLanguage)).Execute();
		}

		public void ResetNetwork()
		{
			(new NetworkResetter(this.Connection, this.PrinterControlLanguage)).Execute();
		}

		public void RestoreDefaults()
		{
			(new PrinterDefaulter(this.Connection, this.PrinterControlLanguage)).Execute();
		}

		public void RestoreNetworkDefaults()
		{
			(new NetworkDefaulter(this.Connection, this.PrinterControlLanguage)).Execute();
		}

		public string[] RetrieveFileNames()
		{
			return this.genericPrinter.RetrieveFileNames();
		}

		public string[] RetrieveFileNames(string[] extensions)
		{
			return this.genericPrinter.RetrieveFileNames(extensions);
		}

		public byte[] RetrieveFormatFromPrinter(string formatPathOnPrinter)
		{
			byte[] array;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				this.RetrieveFormatFromPrinter(memoryStream, formatPathOnPrinter);
				array = memoryStream.ToArray();
			}
			return array;
		}

		public void RetrieveFormatFromPrinter(Stream formatData, string formatPathOnPrinter)
		{
			Stream stream = null;
			Stream dZUnwrapperStream = null;
			HzoToDzConverterStream hzoToDzConverterStream = null;
			try
			{
				try
				{
					stream = (new ObjectGrabberOperation(formatPathOnPrinter, this.Connection, this.PrinterControlLanguage, this.LinkOsInformation)).Execute();
					if (!(stream is MultipartFormReceiverStream))
					{
						hzoToDzConverterStream = new HzoToDzConverterStream(stream);
						dZUnwrapperStream = new DZ_UnwrapperStream(hzoToDzConverterStream);
					}
					else
					{
						dZUnwrapperStream = new MPF_UnwrapperStream(stream);
					}
					byte[] bytes = Encoding.UTF8.GetBytes(ZPLUtilities.ReplaceAllWithInternalCharacters("^XA"));
					formatData.Write(bytes, 0, (int)bytes.Length);
					for (int i = dZUnwrapperStream.ReadByte(); i != -1; i = dZUnwrapperStream.ReadByte())
					{
						formatData.WriteByte((byte)i);
					}
					bytes = Encoding.UTF8.GetBytes(ZPLUtilities.ReplaceAllWithInternalCharacters("^XZ"));
					formatData.Write(bytes, 0, (int)bytes.Length);
				}
				catch (IOException oException)
				{
					throw new ConnectionException(oException.Message);
				}
				catch (ZebraIllegalArgumentException zebraIllegalArgumentException)
				{
					throw new ConnectionException(zebraIllegalArgumentException.Message);
				}
			}
			finally
			{
				if (stream != null)
				{
					stream.Dispose();
				}
				if (hzoToDzConverterStream != null)
				{
					hzoToDzConverterStream.Dispose();
				}
				if (dZUnwrapperStream != null)
				{
					dZUnwrapperStream.Dispose();
				}
			}
		}

		public List<PrinterObjectProperties> RetrieveObjectsProperties()
		{
			return (new ObjectsListingOperation(this.genericPrinter.Connection, this.language, this.linkOsInformation)).Execute();
		}

		public void SendCommand(string command)
		{
			this.genericPrinter.SendCommand(command);
		}

		public void SendCommand(string command, string encoding)
		{
			this.genericPrinter.SendCommand(command, encoding);
		}

		public void SendFileContents(string filePath)
		{
			this.genericPrinter.SendFileContents(filePath);
		}

		public void SendFileContents(string filePath, ProgressMonitor handler)
		{
			this.genericPrinter.SendFileContents(filePath, handler);
		}

		public void SetClock(string dateTime)
		{
			this.toolsUtilHelper.SetClock(dateTime);
		}

		public void SetConnection(Zebra.Sdk.Comm.Connection newConnection)
		{
			this.genericPrinter.SetConnection(newConnection);
			this.settings.SetConnection(newConnection);
		}

		public void SetSetting(string settingId, string value)
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			this.settings.SetSetting(settingId, value);
		}

		public void SetSettings(Dictionary<string, string> settingValuePairs)
		{
			this.ThrowExceptionInLinePrintModeRawOnly();
			this.settings.SetSettings(settingValuePairs);
		}

		public void StoreFileOnPrinter(string filePath)
		{
			this.fileUtil.StoreFileOnPrinter(filePath);
		}

		public void StoreFileOnPrinter(string filePath, string fileNameOnPrinter)
		{
			this.fileUtil.StoreFileOnPrinter(filePath, fileNameOnPrinter);
		}

		public void StoreFileOnPrinter(byte[] fileContents, string fileNameOnPrinter)
		{
			this.fileUtil.StoreFileOnPrinter(fileContents, fileNameOnPrinter);
		}

		public void StoreImage(string deviceDriveAndFileName, ZebraImageI image, int width, int height)
		{
			(new ImageStorer(this.Connection, this.PrinterControlLanguage, this.LinkOsInformation)).Execute(deviceDriveAndFileName, image, width, height);
		}

		public void StoreImage(string deviceDriveAndFileName, string imageFullPath, int width, int height)
		{
			using (ZebraImageI zebraImageI = ReflectionUtil.InvokeZebraImageFactory_GetImage(imageFullPath))
			{
				(new ImageStorer(this.Connection, this.PrinterControlLanguage, this.LinkOsInformation)).Execute(deviceDriveAndFileName, zebraImageI, width, height);
			}
		}

		private void ThrowExceptionInLinePrintMode()
		{
			if (this.PrinterControlLanguage == PrinterLanguage.LINE_PRINT)
			{
				throw new ConnectionException("Operation cannot be performed with a printer set to line print mode");
			}
		}

		private void ThrowExceptionInLinePrintModeRawOnly()
		{
			Zebra.Sdk.Comm.Connection connection = this.Connection;
			if (this.PrinterControlLanguage == PrinterLanguage.LINE_PRINT)
			{
				MultichannelConnection multichannelConnection = connection as MultichannelConnection;
				MultichannelConnection multichannelConnection1 = multichannelConnection;
				if (multichannelConnection != null)
				{
					if (multichannelConnection1.PrintingChannel.Connected && !multichannelConnection1.StatusChannel.Connected)
					{
						throw new ConnectionException("Operation cannot be performed on raw channel with a printer set to line print mode");
					}
				}
				else if (!(connection is StatusConnection))
				{
					throw new ConnectionException("Operation cannot be performed on raw channel with a printer set to line print mode");
				}
			}
		}

		private void ThrowExceptionStatusOnly()
		{
			Zebra.Sdk.Comm.Connection connection = this.Connection;
			MultichannelConnection multichannelConnection = connection as MultichannelConnection;
			MultichannelConnection multichannelConnection1 = multichannelConnection;
			if (multichannelConnection != null)
			{
				if (this.IsOnlySettingsChannelOpen(multichannelConnection1))
				{
					throw new ConnectionException("Operation cannot be performed with only the status channel open");
				}
			}
			else if (connection is StatusConnection)
			{
				throw new ConnectionException("Operation cannot be performed over the status channel");
			}
		}

		private void ThrowExceptionSupplementalDataStatusOnly(string pathToOutputFile)
		{
			if (this.HasSupplementalData(pathToOutputFile))
			{
				Zebra.Sdk.Comm.Connection connection = this.Connection;
				MultichannelConnection multichannelConnection = connection as MultichannelConnection;
				MultichannelConnection multichannelConnection1 = multichannelConnection;
				if (multichannelConnection != null)
				{
					if (this.IsOnlySettingsChannelOpen(multichannelConnection1))
					{
						throw new ConnectionException("Supplemental data cannot be sent with only the status channel open");
					}
				}
				else if (connection is StatusConnection)
				{
					throw new ConnectionException("Supplemental data cannot be sent over the status channel");
				}
			}
		}

		public void UpdateFirmware(string firmwareFilePath, FirmwareUpdateHandler handler)
		{
			this.fwDownloader.UpdateFirmware(firmwareFilePath, handler);
		}

		public void UpdateFirmware(string firmwareFilePath, long timeout, FirmwareUpdateHandler handler)
		{
			this.fwDownloader.UpdateFirmware(firmwareFilePath, timeout, handler);
		}

		public void UpdateFirmwareUnconditionally(string firmwareFilePath, FirmwareUpdateHandler handler)
		{
			this.fwDownloader.UpdateFirmwareUnconditionally(firmwareFilePath, handler);
		}

		public void UpdateFirmwareUnconditionally(string firmwareFilePath, long timeout, FirmwareUpdateHandler handler)
		{
			this.fwDownloader.UpdateFirmwareUnconditionally(firmwareFilePath, timeout, handler);
		}
	}
}