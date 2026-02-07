using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Device;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Discovery;
using Zebra.Sdk.Settings;
using Zebra.Sdk.Settings.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class ProfileUtilLinkOsImpl : ProfileUtil
	{
		protected readonly ZebraPrinterLinkOsImpl linkOsPrinter;

		protected string drivePrefix;

		public ProfileUtilLinkOsImpl(ZebraPrinterLinkOsImpl printer)
		{
			this.linkOsPrinter = printer;
			this.drivePrefix = "*:";
		}

		private bool ContainsCisdWrapper(string dataAsString)
		{
			dataAsString = dataAsString.Trim();
			if (dataAsString.StartsWith("! CISDFCRC16"))
			{
				return true;
			}
			return dataAsString.StartsWith("! CISDFRCRC16");
		}

		public void CreateBackup(string pathToOutputFile)
		{
			this.CreateProfile(pathToOutputFile);
		}

		private void CreateOutputZipFile(Stream profileDestinationStream, byte[] settingsAsJson)
		{
			bool flag;
			ZipArchive zipArchive = null;
			bool flag1 = false;
			try
			{
				zipArchive = new ZipArchive(profileDestinationStream, ZipArchiveMode.Create, true);
				using (BinaryWriter binaryWriter = new BinaryWriter(zipArchive.CreateEntry("settings.json").Open()))
				{
					binaryWriter.Write(settingsAsJson);
				}
				Connection connection = this.linkOsPrinter.Connection;
				if (this.linkOsPrinter.PrinterControlLanguage != PrinterLanguage.LINE_PRINT)
				{
					flag = false;
				}
				else
				{
					flag = (connection is StatusConnection ? false : !(connection is MultichannelConnection));
				}
				flag1 = flag;
				if (flag1)
				{
					this.EnableZplMode(this.linkOsPrinter);
				}
				try
				{
					string str = ProfileHelper.CreateJson(this.linkOsPrinter.GetConfiguredAlerts());
					using (BinaryWriter binaryWriter1 = new BinaryWriter(zipArchive.CreateEntry("alerts.json").Open()))
					{
						binaryWriter1.Write(Encoding.UTF8.GetBytes(str));
					}
				}
				catch (SettingsException settingsException1)
				{
					SettingsException settingsException = settingsException1;
					throw new IOException(settingsException.Message, settingsException);
				}
				this.SaveFilesToProfile(zipArchive, this.linkOsPrinter);
			}
			finally
			{
				if (flag1)
				{
					try
					{
						this.ReEnableLinePrintMode(this.linkOsPrinter);
					}
					catch (ConnectionException)
					{
					}
				}
				if (zipArchive != null)
				{
					zipArchive.Dispose();
				}
			}
		}

		public void CreateProfile(string pathToOutputFile)
		{
			using (FileStream fileStream = new FileStream(FileUtilities.ChangeExtension(pathToOutputFile, "zprofile"), FileMode.CreateNew))
			{
				this.CreateOutputZipFile(fileStream, this.GetAllConfig());
				fileStream.Flush();
			}
		}

		public void CreateProfile(Stream profileDestinationStream)
		{
			this.CreateOutputZipFile(profileDestinationStream, this.GetAllConfig());
		}

		private void DeleteAllCloneableFiles()
		{
			foreach (string str in new HashSet<string>(new string[] { "ZPL", "GRF", "DAT", "BAS", "STO", "PNG", "LBL", "PCX", "BMP", "IMG", "WML", "HTM" }))
			{
				try
				{
					this.linkOsPrinter.DeleteFile(string.Concat(this.drivePrefix, "*.", str));
				}
				catch (ConnectionException)
				{
				}
			}
		}

		private void DeleteAllFiles()
		{
			try
			{
				this.linkOsPrinter.DeleteFile(string.Concat(this.drivePrefix, "*.*"));
			}
			catch (ConnectionException)
			{
			}
		}

		private void DeleteFilesBeforeLoadingProfile(FileDeletionOption filesToDelete)
		{
			switch (filesToDelete)
			{
				case FileDeletionOption.ALL:
				{
					this.DeleteAllFiles();
					return;
				}
				case FileDeletionOption.CLONEABLE:
				{
					this.DeleteAllCloneableFiles();
					return;
				}
				case FileDeletionOption.NONE:
				{
					return;
				}
				default:
				{
					return;
				}
			}
		}

		private void EnableZplMode(ZebraPrinterLinkOsImpl linkOsPrinter)
		{
			SGD.SET("device.languages", "zpl", linkOsPrinter.Connection);
		}

		private byte[] GetAllConfig()
		{
			return ConnectionUtil.SelectConnection(this.linkOsPrinter.Connection).SendAndWaitForValidResponse(Encoding.UTF8.GetBytes("{}{\"allconfig\":null}"), 10000, 2000, new JsonValidator());
		}

		private PrinterFileDescriptor GetFileDescriptors(byte[] data, string fileName)
		{
			if (this.ContainsCisdWrapper(Encoding.UTF8.GetString(data)))
			{
				data = FileWrapper.StripOffCISDFWrapper(data);
			}
			else if (FileWrapper.IsHzoExtension(Path.GetExtension(fileName).Replace(".", "")))
			{
				using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
				{
					try
					{
						FileWrapper.UnwrapHZOResult(binaryWriter.BaseStream, fileName, Encoding.UTF8.GetString(data));
					}
					catch (ZebraIllegalArgumentException zebraIllegalArgumentException1)
					{
						ZebraIllegalArgumentException zebraIllegalArgumentException = zebraIllegalArgumentException1;
						throw new IOException(zebraIllegalArgumentException.Message, zebraIllegalArgumentException);
					}
					data = ((MemoryStream)binaryWriter.BaseStream).ToArray();
				}
			}
			BinaryReader binaryReader = new BinaryReader(new MemoryStream(data));
			PrinterFileMetadata printerFileMetadatum = new PrinterFileMetadata((long)((int)data.Length), "0000", "0000");
			return new PrinterFileDescriptor(binaryReader.BaseStream, fileName, printerFileMetadatum);
		}

		private bool IsCloneable(string fileName)
		{
			if (fileName.ToUpper().StartsWith("Z:"))
			{
				return false;
			}
			return this.IsValidExtension(fileName);
		}

		private bool IsLinkOs2_5_OrHigher(LinkOsInformation linkOsInformation)
		{
			if (linkOsInformation.Major == 2)
			{
				return linkOsInformation.Minor >= 5;
			}
			if (linkOsInformation.Major >= 3)
			{
				return true;
			}
			return false;
		}

		private bool IsValidExtension(string filePath)
		{
			return !(new HashSet<string>(new string[] { "PAC", "NRD", "BAZ", "BAE", "TTF", "TTE", "TXT", "CSV" })).Contains(filePath.Substring(filePath.LastIndexOf('.') + 1));
		}

		private void LoadAlertsFromProfile(string pathToOutputFile)
		{
			List<PrinterAlert> alertsFromJson = ProfileHelper.GetAlertsFromJson(pathToOutputFile);
			try
			{
				this.linkOsPrinter.RemoveAllAlerts();
				this.linkOsPrinter.ConfigureAlerts(alertsFromJson);
			}
			catch (ConnectionException)
			{
			}
		}

		public void LoadBackup(string pathToOutputFile, bool isVerbose)
		{
			this.DeleteFilesBeforeLoadingProfile(FileDeletionOption.ALL);
			this.LoadCloneOrArchiveImage(pathToOutputFile, RestoreType.ARCHIVE, isVerbose);
		}

		public void LoadBackup(string pathToOutputFile)
		{
			this.LoadBackup(pathToOutputFile, false);
		}

		private void LoadCloneOrArchiveImage(string pathToOutputFile, RestoreType restoreType, bool isVerbose)
		{
			this.LoadFirmwareFromProfile(pathToOutputFile, isVerbose);
			this.RestoreSettings(pathToOutputFile, restoreType);
			this.LoadAlertsFromProfile(pathToOutputFile);
			this.LoadFilesFromProfile(pathToOutputFile);
			this.LoadSupplementFromProfile(pathToOutputFile);
		}

		private void LoadFilesFromProfile(string pathToOutputFile)
		{
			ZipUtil zipUtil = new ZipUtil(pathToOutputFile);
			List<string> entryNames = zipUtil.GetEntryNames();
			List<PrinterFileDescriptor> printerFileDescriptors = new List<PrinterFileDescriptor>();
			Connection connection = ConnectionUtil.SelectConnection(this.linkOsPrinter.Connection);
			foreach (string entryName in entryNames)
			{
				if (ProfileHelper.IsSpecialProfileFile(entryName))
				{
					continue;
				}
				byte[] numArray = zipUtil.ExtractEntry(entryName);
				if (!this.ShouldSendMultipartForm(connection, this.linkOsPrinter))
				{
					connection.Write(numArray);
				}
				else
				{
					printerFileDescriptors.Add(this.GetFileDescriptors(numArray, entryName));
				}
			}
			if (printerFileDescriptors.Any<PrinterFileDescriptor>())
			{
				MultipartFileSender.Send(connection, printerFileDescriptors);
			}
		}

		private void LoadFirmwareFromProfile(string pathToOutputFile, bool isVerbose)
		{
			ZipUtil zipUtil = new ZipUtil(pathToOutputFile);
			if (zipUtil.ContainsEntry("firmwareFile.txt"))
			{
				FileInfo fileInfo = null;
				string str = "sdkTmpFwFile.txt";
				try
				{
					try
					{
						using (FileStream fileStream = File.Create(str))
						{
							fileInfo = new FileInfo(str);
						}
						zipUtil.ExtractEntry(fileInfo.FullName, "firmwareFile.txt");
						this.linkOsPrinter.UpdateFirmware(fileInfo.FullName, new FirmwareUpdateHandlerVerboseDecorator(isVerbose, this.linkOsPrinter.Connection.ToString(), pathToOutputFile, new ProfileUtilLinkOsImpl.ProfileFirmwareHandler()
						{
							progressUpdate = (int bytesWritten, int totalBytes) => {
							},
							firmwareDownloadComplete = () => {
							},
							printerOnline = (ZebraPrinterLinkOs printer, string firmwareVersion) => {
								Connection connection = printer.Connection;
								try
								{
									connection.Open();
									this.linkOsPrinter.SetConnection(connection);
								}
								catch (ConnectionException)
								{
								}
							}
						}));
					}
					catch (ZebraPrinterLanguageUnknownException zebraPrinterLanguageUnknownException)
					{
						throw new ConnectionException(zebraPrinterLanguageUnknownException.Message);
					}
					catch (ZebraIllegalArgumentException zebraIllegalArgumentException)
					{
						throw new ConnectionException(zebraIllegalArgumentException.Message);
					}
					catch (DiscoveryException discoveryException)
					{
						throw new ConnectionException(discoveryException.Message);
					}
					catch (TimeoutException timeoutException)
					{
						throw new ConnectionException(timeoutException.Message);
					}
				}
				finally
				{
					if (File.Exists(str))
					{
						File.Delete(str);
					}
				}
			}
		}

		public void LoadProfile(string pathToOutputFile, FileDeletionOption filesToDelete, bool isVerbose)
		{
			this.DeleteFilesBeforeLoadingProfile(filesToDelete);
			this.LoadCloneOrArchiveImage(pathToOutputFile, RestoreType.CLONE, isVerbose);
		}

		public void LoadProfile(string pathToOutputFile)
		{
			this.LoadProfile(pathToOutputFile, FileDeletionOption.NONE, false);
		}

		private void LoadSupplementFromProfile(string pathToOutputFile)
		{
			byte[] numArray = (new ZipUtil(pathToOutputFile)).ExtractEntry("profileSupplement.txt");
			if (numArray.Length != 0)
			{
				this.linkOsPrinter.Connection.Write(numArray);
			}
		}

		private void ReEnableLinePrintMode(ZebraPrinterLinkOsImpl linkOsPrinter)
		{
			SGD.SET("device.languages", "line_print", linkOsPrinter.Connection);
		}

		private void RestoreSettings(string pathToOutputFile, RestoreType restoreType)
		{
			Dictionary<string, string> strs = new Dictionary<string, string>();
			Dictionary<string, string> strs1 = new Dictionary<string, string>();
			Profile profile = new Profile(pathToOutputFile);
			try
			{
				strs = (restoreType == RestoreType.ARCHIVE ? profile.GetArchivableSettingValues() : profile.GetClonableSettingValues());
			}
			catch (SettingsException settingsException)
			{
				throw new IOException(settingsException.Message);
			}
			ProfileHelper.HandleSpecialCases(strs, restoreType);
			try
			{
				int num = 0;
				foreach (string key in strs.Keys)
				{
					strs1.Add(key, strs[key]);
					if (num >= 100)
					{
						num = 0;
						this.linkOsPrinter.SetSettings(strs1);
						strs1 = new Dictionary<string, string>();
					}
					num++;
				}
				this.linkOsPrinter.SetSettings(strs1);
			}
			catch (SettingsException settingsException1)
			{
				throw new ConnectionException(settingsException1.Message);
			}
		}

		private void SaveFilesToProfile(ZipArchive zos, ZebraPrinterLinkOs printer)
		{
			string[] strArrays = printer.RetrieveFileNames();
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				if (this.IsCloneable(strArrays[i]))
				{
					try
					{
						byte[] printerDownloadableObjectFromPrinter = printer.GetPrinterDownloadableObjectFromPrinter(strArrays[i]);
						if (printerDownloadableObjectFromPrinter.Length != 0)
						{
							using (BinaryWriter binaryWriter = new BinaryWriter(zos.CreateEntry(strArrays[i]).Open()))
							{
								binaryWriter.Write(printerDownloadableObjectFromPrinter);
							}
						}
					}
					catch (ZebraIllegalArgumentException)
					{
					}
				}
			}
		}

		private bool ShouldSendMultipartForm(Connection connection, ZebraPrinterLinkOs linkOsPrinter)
		{
			if (!this.IsLinkOs2_5_OrHigher(linkOsPrinter.LinkOsInformation))
			{
				return false;
			}
			if (linkOsPrinter.PrinterControlLanguage != PrinterLanguage.LINE_PRINT)
			{
				return true;
			}
			return connection is StatusConnection;
		}

		private class ProfileFirmwareHandler : FirmwareUpdateHandler
		{
			public ProfileFirmwareHandler()
			{
			}

			public override void FirmwareDownloadComplete()
			{
				throw new NotImplementedException();
			}

			public override void PrinterOnline(ZebraPrinterLinkOs printer, string firmwareVersion)
			{
				throw new NotImplementedException();
			}

			public override void ProgressUpdate(int bytesWritten, int totalBytes)
			{
				throw new NotImplementedException();
			}
		}
	}
}