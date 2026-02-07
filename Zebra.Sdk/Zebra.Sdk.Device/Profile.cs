using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Graphics;
using Zebra.Sdk.Graphics.Internal;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Internal;
using Zebra.Sdk.Settings;
using Zebra.Sdk.Settings.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Device
{
	/// <summary>
	///       Interface to access the contents of a .zprofile file.
	///       </summary>
	public class Profile : Zebra.Sdk.Device.Device, SettingsProvider, FileUtil, FontUtil, AlertProvider, FileUtilLinkOs, GraphicsUtil
	{
		private readonly string pathToProfile;

		private ZebraSettingsListI profileBasedSettingsList;

		/// <summary>
		///       Create a <c>Profile</c> object backed by an existing .zprofile file.
		///       </summary>
		/// <param name="pathToProfile">Path to the profile file.</param>
		/// <exception cref="T:System.IO.FileNotFoundException">If the file <c>pathToProfile</c> does not exist.</exception>
		public Profile(string pathToProfile)
		{
			this.pathToProfile = pathToProfile;
			this.profileBasedSettingsList = new ZebraSettingsListFromProfile(this.pathToProfile);
		}

		/// <summary>
		///       Adds a firmware file to an existing printer profile.
		///       </summary>
		/// <param name="pathToFirmwareFile">Full path to the firmware file to be added to the profile.</param>
		/// <exception cref="T:System.IO.IOException">If there is an error reading <c>pathToFirmwareFile</c> or if there 
		///       is an error writing  to the.zprofile file.</exception>
		public void AddFirmware(string pathToFirmwareFile)
		{
			FileInfo fileInfo = new FileInfo(pathToFirmwareFile);
			byte[] bytes = Encoding.UTF8.GetBytes(fileInfo.Name);
			(new ZipUtil(this.pathToProfile)).AddEntry("firmwareFile.txt", File.OpenRead(pathToFirmwareFile), bytes);
		}

		/// <summary>
		///       Adds a firmware file to an existing printer profile.
		///       </summary>
		/// <param name="firmwareFileName">The name of the firmware file</param>
		/// <param name="firmwareFileContents">The firmware file contents</param>
		/// <exception cref="T:System.IO.IOException">If there is an error reading <c>pathToFirmwareFile</c> or if there 
		///       is an error writing  to the.zprofile file.</exception>
		public void AddFirmware(string firmwareFileName, byte[] firmwareFileContents)
		{
			(new ZipUtil(this.pathToProfile)).AddEntry("firmwareFile.txt", firmwareFileContents, Encoding.UTF8.GetBytes(firmwareFileName));
		}

		/// <summary>
		///       Adds data to supplement an existing printer profile.
		///       </summary>
		/// <param name="supplementData">Byte array containing the data to be used to supplement the printer profile.</param>
		/// <exception cref="T:System.IO.IOException">If there is an error writing to the .zprofile file.</exception>
		public void AddSupplement(byte[] supplementData)
		{
			(new ZipUtil(this.pathToProfile)).AddEntry("profileSupplement.txt", supplementData);
		}

		/// <summary>
		///       Configures an alert to be triggered when the alert's condition occurs or becomes resolved.
		///       </summary>
		/// <param name="alert">The alert to trigger when it's condition occurs or becomes resolved.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException"></exception>
		public void ConfigureAlert(PrinterAlert alert)
		{
			try
			{
				List<PrinterAlert> alertsFromJson = ProfileHelper.GetAlertsFromJson(this.pathToProfile);
				PrinterAlert printerAlert = alert;
				int num = 0;
				while (num < alertsFromJson.Count)
				{
					if (!this.IsMatchingAlert(alertsFromJson[num], printerAlert))
					{
						num++;
					}
					else
					{
						alertsFromJson.RemoveAt(num);
						break;
					}
				}
				if ((printerAlert.OnSet ? true : printerAlert.OnClear))
				{
					alertsFromJson.Add(alert);
				}
				ProfileHelper.CommitAlertsToProfile(this.pathToProfile, alertsFromJson);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				throw new ConnectionException(exception.Message, exception);
			}
		}

		/// <summary>
		///       Configures a list of alerts to be triggered when their conditions occur or become resolved.
		///       </summary>
		/// <param name="alerts">The list of alerts to trigger when their conditions occur or become resolved.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException"></exception>
		public void ConfigureAlerts(List<PrinterAlert> alerts)
		{
			foreach (PrinterAlert alert in alerts)
			{
				this.ConfigureAlert(alert);
			}
		}

		/// <summary>
		///       Deletes the file from the profile. The <c>filePath</c> may also contain wildcards.
		///       </summary>
		/// <param name="filePath">The location of the file on the printer. Wildcards are also 
		///       accepted (e.g. "E:FORMAT.ZPL", "E:*.*")</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error accessing the profile.</exception>
		public void DeleteFile(string filePath)
		{
			try
			{
				ProfileHelper.DeleteFileFromProfile(this.pathToProfile, filePath);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				throw new ConnectionException(exception.Message, exception);
			}
		}

		/// <summary>
		///       Adds a TrueType® font file to a profile and stores it at the specified path as a TrueType® extension (TTE).
		///       </summary>
		/// <param name="sourceFilePath">Path to a TrueType® font to be added to the profile.</param>
		/// <param name="pathOnPrinter">Location to save the font file in the profile.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public void DownloadTteFont(string sourceFilePath, string pathOnPrinter)
		{
			using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
			{
				FontConverterZpl.SaveAsTtePrinterFont(sourceFilePath, binaryWriter.BaseStream, pathOnPrinter);
				try
				{
					(new ZipUtil(this.pathToProfile)).AddEntry(pathOnPrinter, ((MemoryStream)binaryWriter.BaseStream).ToArray());
				}
				catch (IOException oException1)
				{
					IOException oException = oException1;
					throw new ConnectionException(oException.Message, oException);
				}
			}
		}

		/// <summary>
		///       Adds a TrueType® font to a profile and stores it at the specified path as a TrueType® extension (TTE).
		///       </summary>
		/// <param name="sourceInputStream">Input Stream containing the font data.</param>
		/// <param name="pathOnPrinter">Location to save the font file in the profile.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public void DownloadTteFont(Stream sourceInputStream, string pathOnPrinter)
		{
			using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
			{
				FontConverterZpl.SaveAsTtePrinterFont(sourceInputStream, binaryWriter.BaseStream, pathOnPrinter);
				try
				{
					(new ZipUtil(this.pathToProfile)).AddEntry(pathOnPrinter, ((MemoryStream)binaryWriter.BaseStream).ToArray());
				}
				catch (IOException oException1)
				{
					IOException oException = oException1;
					throw new ConnectionException(oException.Message, oException);
				}
			}
		}

		/// <summary>
		///       Adds a TrueType® font file to a profile and stores it at the specified path as a TTF.
		///       </summary>
		/// <param name="sourceFilePath">Path to a TrueType® font to be added to the profile.</param>
		/// <param name="pathOnPrinter">Location to save the font file in the profile.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public void DownloadTtfFont(string sourceFilePath, string pathOnPrinter)
		{
			using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
			{
				FontConverterZpl.SaveAsTtfPrinterFont(sourceFilePath, binaryWriter.BaseStream, pathOnPrinter);
				try
				{
					(new ZipUtil(this.pathToProfile)).AddEntry(pathOnPrinter, ((MemoryStream)binaryWriter.BaseStream).ToArray());
				}
				catch (IOException oException1)
				{
					IOException oException = oException1;
					throw new ConnectionException(oException.Message, oException);
				}
			}
		}

		/// <summary>
		///       Adds a TrueType® font file to a profile and stores it at the specified path as a TTF.
		///       </summary>
		/// <param name="sourceInputStream">Input Stream containing the font data.</param>
		/// <param name="pathOnPrinter">Location to save the font file in the profile.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public void DownloadTtfFont(Stream sourceInputStream, string pathOnPrinter)
		{
			using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
			{
				FontConverterZpl.SaveAsTtfPrinterFont(sourceInputStream, binaryWriter.BaseStream, pathOnPrinter);
				try
				{
					(new ZipUtil(this.pathToProfile)).AddEntry(pathOnPrinter, ((MemoryStream)binaryWriter.BaseStream).ToArray());
				}
				catch (IOException oException1)
				{
					IOException oException = oException1;
					throw new ConnectionException(oException.Message, oException);
				}
			}
		}

		/// <summary>
		///       Retrieve all settings and their attributes.
		///       </summary>
		/// <returns>Map of setting IDs and setting attributes contained in the profile</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the settings could not be retrieved</exception>
		public Dictionary<string, Setting> GetAllSettings()
		{
			return this.profileBasedSettingsList.GetAllSettings().Cast<DictionaryEntry>().ToDictionary<DictionaryEntry, string, Setting>((DictionaryEntry k) => (string)k.Key, (DictionaryEntry v) => (Setting)v.Value);
		}

		/// <summary>
		///       Retrieves all of the profile's setting values.
		///       </summary>
		/// <returns>Values of all the settings provided by the profile.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the settings could not be loaded.</exception>
		public Dictionary<string, string> GetAllSettingValues()
		{
			return this.profileBasedSettingsList.GetAllSettingValues();
		}

		/// <summary>
		///       Retrieve the values of all the settings that are archivable.
		///       </summary>
		/// <returns>Values of all the settings with the archivable attribute that are in the profile.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the settings could not be loaded.</exception>
		public Dictionary<string, string> GetArchivableSettingValues()
		{
			Dictionary<string, string> strs = new Dictionary<string, string>();
			foreach (string availableSetting in this.GetAvailableSettings())
			{
				try
				{
					if (this.ShouldArchive(availableSetting))
					{
						strs.Add(availableSetting, this.profileBasedSettingsList.GetValue(availableSetting));
					}
				}
				catch (SettingsException)
				{
				}
				catch (ZebraIllegalArgumentException)
				{
				}
				catch (ConnectionException)
				{
				}
			}
			return strs;
		}

		/// <summary>
		///       Retrieve all of the setting identifiers for a profile.
		///       </summary>
		/// <returns>Set of identifiers available for a profile.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the settings could not be loaded.</exception>
		public HashSet<string> GetAvailableSettings()
		{
			return new HashSet<string>(this.GetAllSettingValues().Keys);
		}

		/// <summary>
		///       Retrieve the values of all the settings that are clonable.
		///       </summary>
		/// <returns>Values of all the settings with the clonable attribute that are in the profile.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the settings could not be loaded.</exception>
		public Dictionary<string, string> GetClonableSettingValues()
		{
			Dictionary<string, string> strs = new Dictionary<string, string>();
			foreach (string availableSetting in this.GetAvailableSettings())
			{
				try
				{
					if (this.ShouldClone(availableSetting))
					{
						strs.Add(availableSetting, this.profileBasedSettingsList.GetValue(availableSetting));
					}
				}
				catch (SettingsException)
				{
				}
				catch (ZebraIllegalArgumentException)
				{
				}
				catch (ConnectionException)
				{
				}
			}
			return strs;
		}

		/// <summary>
		///       A list of objects detailing the alert configurations in a profile.
		///       </summary>
		/// <returns>A list of alert objects currently in a profile.</returns>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If the alerts could not be extracted from the profile.</exception>
		public List<PrinterAlert> GetConfiguredAlerts()
		{
			List<PrinterAlert> alertsFromJson;
			try
			{
				alertsFromJson = ProfileHelper.GetAlertsFromJson(this.pathToProfile);
			}
			catch (Exception exception)
			{
				throw new ZebraIllegalArgumentException(exception.Message);
			}
			return alertsFromJson;
		}

		/// <summary>
		///       Returns the file name of the firmware file within the profile.
		///       </summary>
		/// <returns>The file name of the firmware file within the profile.</returns>
		/// <exception cref="T:System.IO.IOException">If there is an error removing the firmware file from the .zprofile file.</exception>
		/// <exception cref="T:System.IO.FileNotFoundException">If the firmware file cannot be found in the .zprofile file.</exception>
		public string GetFirmwareFilename()
		{
			string entryExtraContent = (new ZipUtil(this.pathToProfile)).GetEntryExtraContent();
			if (string.IsNullOrEmpty(entryExtraContent))
			{
				entryExtraContent = (new ZipUtil(this.pathToProfile)).GetEntryContents("firmwareFileUserSpecifiedName.txt");
			}
			return entryExtraContent;
		}

		/// <summary>
		///       Retrieves a file from the profile and returns the contents of that file as a byte array.
		///       </summary>
		/// <param name="filePath">The absolute file path on the printer ("E:SAMPLE.TXT").</param>
		/// <returns>The file contents</returns>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If the filePath is invalid, or if the file does not exist on the printer.</exception>
		public byte[] GetObjectFromPrinter(string filePath)
		{
			BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream());
			this.GetObjectFromPrinter(binaryWriter.BaseStream, filePath);
			return ((MemoryStream)binaryWriter.BaseStream).ToArray();
		}

		/// <summary>
		///       Retrieves a file from the printer's file system and writes the contents of that file to destinationStream.
		///       </summary>
		/// <param name="destinationStream">Output stream to receive the file contents</param>
		/// <param name="filePath">The absolute file path on the printer ("E:SAMPLE.TXT").</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an issue communicating with the device 
		///       (e.g. the connection is not open).</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If the filePath is invalid, or if the file does not exist on the printer.</exception>
		public void GetObjectFromPrinter(Stream destinationStream, string filePath)
		{
			try
			{
				byte[] numArray = null;
				byte[] numArray1 = null;
				try
				{
					numArray = (new ZipUtil(this.pathToProfile)).ExtractEntry(filePath);
				}
				catch (FileNotFoundException fileNotFoundException)
				{
					throw new ZebraIllegalArgumentException(fileNotFoundException.Message);
				}
				catch (IOException oException)
				{
					throw new ZebraIllegalArgumentException(oException.Message);
				}
				if (!FileWrapper.IsHzoExtension(filePath.Substring(filePath.LastIndexOf('.') + 1)))
				{
					numArray1 = FileWrapper.StripOffCISDFWrapper(numArray);
					destinationStream.Write(numArray1, 0, (int)numArray1.Length);
				}
				else
				{
					FileWrapper.UnwrapHZOResult(destinationStream, filePath, Encoding.UTF8.GetString(numArray));
				}
			}
			catch (IOException oException1)
			{
				throw new ZebraIllegalArgumentException(oException1.Message);
			}
		}

		/// <summary>
		///       This method is not valid for a profile.
		///       </summary>
		/// <param name="filePath">NA</param>
		/// <param name="ftpPassword">NA</param>
		/// <returns>NA</returns>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If this method is called.</exception>
		public byte[] GetObjectFromPrinterViaFtp(string filePath, string ftpPassword)
		{
			throw new ZebraIllegalArgumentException("Cannot access a profile over FTP");
		}

		/// <summary>
		///       This method is not valid for a profile.
		///       </summary>
		/// <param name="destinationStream">NA</param>
		/// <param name="filePath">NA</param>
		/// <param name="ftpPassword">NA</param>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If this method is called.</exception>
		public void GetObjectFromPrinterViaFtp(Stream destinationStream, string filePath, string ftpPassword)
		{
			throw new ZebraIllegalArgumentException("Cannot access a profile over FTP");
		}

		/// <summary>
		///       Retrieves a file from the profile and returns the contents of that file as a byte array including all necessary
		///       file wrappers for re-downloading to a Zebra printer.
		///       </summary>
		/// <param name="filePath">The absolute file path on the printer ("E:SAMPLE.TXT").</param>
		/// <returns>A Zebra printer downloadable file content.</returns>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If the filePath is invalid, or if the file does not exist on the printer.</exception>
		public byte[] GetPrinterDownloadableObjectFromPrinter(string filePath)
		{
			byte[] numArray;
			try
			{
				numArray = (new ZipUtil(this.pathToProfile)).ExtractEntry(filePath);
			}
			catch (FileNotFoundException fileNotFoundException)
			{
				throw new ZebraIllegalArgumentException(fileNotFoundException.Message);
			}
			catch (IOException oException)
			{
				throw new ZebraIllegalArgumentException(oException.Message);
			}
			return numArray;
		}

		/// <summary>
		///       Retrieves the profile's <see cref="T:Zebra.Sdk.Settings.Setting" /> for a setting id.
		///       </summary>
		/// <param name="settingId">The setting id.</param>
		/// <returns>The <see cref="T:Zebra.Sdk.Settings.Setting" /></returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the setting could not be retrieved.</exception>
		public Setting GetSetting(string settingId)
		{
			return this.profileBasedSettingsList.GetSetting(settingId);
		}

		/// <summary>
		///       Retrieves the allowable range for a setting.
		///       </summary>
		/// <param name="settingId">The setting id.</param>
		/// <returns>The setting's range as a string</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the setting does not exist</exception>
		public string GetSettingRange(string settingId)
		{
			string settingRange;
			try
			{
				settingRange = this.profileBasedSettingsList.GetSettingRange(settingId);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				throw new SettingsException(exception.Message, exception);
			}
			return settingRange;
		}

		/// <summary>
		///       Retrieves the profile's setting values for a list of setting ids.
		///       </summary>
		/// <param name="listOfSettings">List of setting ids.</param>
		/// <returns>The settings' values.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the settings could not be retrieved.</exception>
		public Dictionary<string, string> GetSettingsValues(List<string> listOfSettings)
		{
			Dictionary<string, string> strs;
			try
			{
				Dictionary<string, string> strs1 = new Dictionary<string, string>();
				Dictionary<string, string> allSettingValues = this.GetAllSettingValues();
				foreach (string listOfSetting in listOfSettings)
				{
					if (!allSettingValues.ContainsKey(listOfSetting))
					{
						continue;
					}
					strs1.Add(listOfSetting, allSettingValues[listOfSetting]);
				}
				strs = strs1;
			}
			catch (ArgumentException argumentException1)
			{
				ArgumentException argumentException = argumentException1;
				throw new SettingsException(argumentException.Message, argumentException);
			}
			return strs;
		}

		/// <summary>
		///       Returns the data type of the setting.
		///       </summary>
		/// <param name="settingId">The setting id</param>
		/// <returns>The data type of the setting (e.g. string, bool, enum, etc.)</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the setting does not exist</exception>
		public string GetSettingType(string settingId)
		{
			string settingType;
			try
			{
				settingType = this.profileBasedSettingsList.GetSettingType(settingId);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				throw new SettingsException(exception.Message, exception);
			}
			return settingType;
		}

		/// <summary>
		///       Retrieves the profile's setting value for a setting id.
		///       </summary>
		/// <param name="settingId">The setting id.</param>
		/// <returns>The setting's value.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the settings could not be retrieved.</exception>
		public string GetSettingValue(string settingId)
		{
			string item;
			try
			{
				Dictionary<string, string> allSettingValues = this.GetAllSettingValues();
				if (!allSettingValues.ContainsKey(settingId))
				{
					throw new SettingsException();
				}
				item = allSettingValues[settingId];
			}
			catch (ArgumentException argumentException1)
			{
				ArgumentException argumentException = argumentException1;
				throw new SettingsException(argumentException.Message, argumentException);
			}
			return item;
		}

		/// <summary>
		///       This method is not valid for a profile.
		///       </summary>
		/// <returns></returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If this method is called.</exception>
		public List<StorageInfo> GetStorageInfo()
		{
			throw new ConnectionException("Storage info is not available for a profile");
		}

		/// <summary>
		///       Returns the supplement data within the profile.
		///       </summary>
		/// <returns>The supplement data within the profile.</returns>
		/// <exception cref="T:System.IO.IOException">If there is an error writing to the .zprofile file.</exception>
		/// <exception cref="T:System.IO.FileNotFoundException">If the .zprofile file cannot be found.</exception>
		public string GetSupplement()
		{
			return (new ZipUtil(this.pathToProfile)).GetEntryContents("profileSupplement.txt");
		}

		private bool IsMatchingAlert(PrinterAlert alert, PrinterAlert printerAlert)
		{
			bool flag;
			flag = (printerAlert.Condition != alert.Condition ? false : printerAlert.Destination == alert.Destination);
			bool flag1 = (!flag || printerAlert.Condition != AlertCondition.SGD_SET ? false : !alert.SgdName.Equals(printerAlert.SgdName));
			if (!flag)
			{
				return false;
			}
			return !flag1;
		}

		/// <summary>
		///       Returns true if the setting is read only.
		///       </summary>
		/// <param name="settingId">The setting id</param>
		/// <returns>True if the setting is read only</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the setting does not exist</exception>
		public bool IsSettingReadOnly(string settingId)
		{
			bool flag;
			try
			{
				flag = this.profileBasedSettingsList.IsSettingReadOnly(settingId);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				throw new SettingsException(exception.Message, exception);
			}
			return flag;
		}

		/// <summary>
		///       Returns true if value is valid for the given setting.
		///       </summary>
		/// <param name="settingId">The setting id.</param>
		/// <param name="value">The setting's value</param>
		/// <returns>True if value is valid for the given setting.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the setting does not exist</exception>
		public bool IsSettingValid(string settingId, string value)
		{
			bool flag;
			try
			{
				flag = this.profileBasedSettingsList.IsSettingValid(settingId, value);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				throw new SettingsException(exception.Message, exception);
			}
			return flag;
		}

		/// <summary>
		///       Returns true if the setting is write only.
		///       </summary>
		/// <param name="settingId">The setting id</param>
		/// <returns>True if the setting is write only</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the setting does not exist</exception>
		public bool IsSettingWriteOnly(string settingId)
		{
			bool flag;
			try
			{
				flag = this.profileBasedSettingsList.IsSettingWriteOnly(settingId);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				throw new SettingsException(exception.Message, exception);
			}
			return flag;
		}

		/// <summary>
		///       Prints an image from the connecting device file system to the connected device as a monochrome image.
		///       </summary>
		/// <param name="imageFilePath">Image file to be printed.</param>
		/// <param name="x">Horizontal starting position in dots.</param>
		/// <param name="y">Vertical starting position in dots.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		/// <exception cref="T:System.IO.IOException">When the file could not be found, opened, or is an unsupported graphic.</exception>
		public void PrintImage(string imageFilePath, int x, int y)
		{
			throw new ConnectionException("Printing an image is not applicable to a profile.");
		}

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
		public void PrintImage(string imageFilePath, int x, int y, int width, int height, bool insideFormat)
		{
			throw new ConnectionException("Printing an image is not applicable to a profile.");
		}

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
		public void PrintImage(ZebraImageI image, int x, int y, int width, int height, bool insideFormat)
		{
			throw new ConnectionException("Printing an image is not applicable to a profile.");
		}

		/// <summary>
		///       Change or retrieve settings in the profile.
		///       </summary>
		/// <param name="settingValuePairs">The settings to change.</param>
		/// <returns>
		///   <see cref="T:System.Collections.Generic.Dictionary`2" /> results of the setting commands</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the setting is malformed, or if the setting could not be set.</exception>
		public Dictionary<string, string> ProcessSettingsViaMap(Dictionary<string, string> settingValuePairs)
		{
			return this.profileBasedSettingsList.ProcessSettingsViaMap(settingValuePairs);
		}

		/// <summary>
		///       Removes a configured alert from a profile.
		///       </summary>
		/// <param name="alert">The alert to be removed from the configuration.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public void RemoveAlert(PrinterAlert alert)
		{
			if (alert != null)
			{
				this.ConfigureAlert(new PrinterAlert(alert.Condition, alert.Destination, false, false, "", 0, false));
			}
		}

		/// <summary>
		///       Removes all alerts currently in a profile.
		///       </summary>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public void RemoveAllAlerts()
		{
			List<PrinterAlert> printerAlerts = new List<PrinterAlert>();
			try
			{
				ProfileHelper.CommitAlertsToProfile(this.pathToProfile, printerAlerts);
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw new ConnectionException(oException.Message, oException);
			}
		}

		/// <summary>
		///       Removes the firmware file from the profile.
		///       </summary>
		/// <exception cref="T:System.IO.IOException">If there is an error removing the firmware file from the .zprofile file.</exception>
		public void RemoveFirmware()
		{
			(new ZipUtil(this.pathToProfile)).RemoveEntry("firmwareFileUserSpecifiedName.txt");
			(new ZipUtil(this.pathToProfile)).RemoveEntry("firmwareFile.txt");
			(new ZipUtil(this.pathToProfile)).RemoveEntry("firmwareFileUserSpecifiedName.txt");
		}

		/// <summary>
		///       Retrieves the names of the files which are in the profile.
		///       </summary>
		/// <returns>List of file names.</returns>
		public string[] RetrieveFileNames()
		{
			List<string> entryNames = (new ZipUtil(this.pathToProfile)).GetEntryNames();
			List<string> strs = new List<string>();
			foreach (string entryName in entryNames)
			{
				if (ProfileHelper.IsSpecialProfileFile(entryName))
				{
					continue;
				}
				strs.Add(entryName);
			}
			return strs.ToArray();
		}

		/// <summary>
		///       Retrieves the names of the files which are stored on the device.
		///       </summary>
		/// <param name="extensions">The extensions to filter on.</param>
		/// <returns>List of file names.</returns>
		public string[] RetrieveFileNames(string[] extensions)
		{
			List<string> strs = new List<string>();
			if (extensions != null)
			{
				string[] strArrays = this.RetrieveFileNames();
				for (int i = 0; i < (int)strArrays.Length; i++)
				{
					string str = strArrays[i];
					string[] strArrays1 = extensions;
					for (int j = 0; j < (int)strArrays1.Length; j++)
					{
						string str1 = strArrays1[j];
						if (str.ToLower().EndsWith(string.Concat(".", str1.ToLower())))
						{
							strs.Add(str);
						}
					}
				}
			}
			return strs.ToArray();
		}

		/// <summary>
		///       Retrieves the properties of the objects which are stored on the device.
		///       </summary>
		/// <returns>The list of objects with their properties.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If there is an error parsing the directory data returned by the device.</exception>
		public List<PrinterObjectProperties> RetrieveObjectsProperties()
		{
			List<PrinterObjectProperties> printerObjectProperties = new List<PrinterObjectProperties>();
			string[] strArrays = this.RetrieveFileNames();
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				string str = strArrays[i];
				PrinterFilePath printerFilePath = FileUtilities.ParseDriveAndExtension(str);
				string drive = printerFilePath.Drive;
				if (drive.Length > 0 && !drive.EndsWith(":"))
				{
					drive = string.Concat(drive, ":");
				}
				string extension = printerFilePath.Extension;
				if (extension.StartsWith("."))
				{
					extension = extension.Substring(1);
				}
				long length = (long)0;
				try
				{
					length = (long)(new ZipUtil(this.pathToProfile)).GetEntryContents(str).Length;
				}
				catch (FileNotFoundException)
				{
				}
				catch (IOException)
				{
				}
				printerObjectProperties.Add(new PrinterFilePropertiesZpl(drive, printerFilePath.FileName, extension, length));
			}
			return printerObjectProperties;
		}

		/// <summary>
		///       Adds a file to the profile named <c>fileNameOnPrinter</c> with the file contents from
		///       <c>fileContents</c>.
		///       </summary>
		/// <param name="fileNameOnPrinter">The full name of the file on the printer (e.g "R:SAMPLE.ZPL").</param>
		/// <param name="fileContents">The contents of the file to send.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public void SendContents(string fileNameOnPrinter, byte[] fileContents)
		{
			try
			{
				(new ZipUtil(this.pathToProfile)).AddEntry(fileNameOnPrinter, fileContents);
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw new ConnectionException(oException.Message, oException);
			}
		}

		/// <summary>
		///       This method is not valid for a profile.
		///       </summary>
		/// <param name="filePath">Path to the file containing the data to send.</param>
		/// <exception cref="T:System.InvalidOperationException">If this method is called.</exception>
		public void SendFileContents(string filePath)
		{
			throw new InvalidOperationException("sendFileContents is not valid for profiles");
		}

		/// <summary>
		///       This method is not valid for a profile.
		///       </summary>
		/// <param name="filePath">Path to the file containing the data to send.</param>
		/// <param name="handler">Progress monitor callback handler.</param>
		/// <exception cref="T:System.InvalidOperationException">If this method is called.</exception>
		public void SendFileContents(string filePath, ProgressMonitor handler)
		{
			throw new InvalidOperationException("sendFileContents is not valid for profiles");
		}

		/// <summary>
		///       Change settings in the profile.
		///       </summary>
		/// <param name="settings">The settings to change</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If a setting is malformed, or one or more settings could not be set.</exception>
		public void SetAllSettings(Dictionary<string, Setting> settings)
		{
			this.profileBasedSettingsList.SetAllSettings(settings);
		}

		/// <summary>
		///       Change the value of the setting in the profile to the given value.
		///       </summary>
		/// <param name="settingId">The setting id.</param>
		/// <param name="value">The setting's value.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the setting is read only, does not exist, or if the setting could not be set.</exception>
		public void SetSetting(string settingId, string value)
		{
			this.profileBasedSettingsList.SetSetting(settingId, value);
		}

		/// <summary>
		///       Change the setting in the profile.
		///       </summary>
		/// <param name="settingId">The setting id.</param>
		/// <param name="setting">The setting.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the setting is malformed, or if the setting could not be set.</exception>
		public void SetSetting(string settingId, Setting setting)
		{
			this.profileBasedSettingsList.SetSetting(settingId, setting);
		}

		/// <summary>
		///       Set more than one setting.
		///       </summary>
		/// <param name="settingValuePairs">Map a setting ID to the new value for the setting.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the settings cannot be saved in the profile.</exception>
		public void SetSettings(Dictionary<string, string> settingValuePairs)
		{
			this.profileBasedSettingsList.SetSettings(settingValuePairs);
		}

		private bool ShouldArchive(string id)
		{
			if (!this.profileBasedSettingsList.IsSettingArchivable(id) || this.profileBasedSettingsList.IsSettingWriteOnly(id))
			{
				return false;
			}
			return !this.profileBasedSettingsList.IsSettingReadOnly(id);
		}

		private bool ShouldClone(string id)
		{
			if (!this.profileBasedSettingsList.IsSettingClonable(id) || this.profileBasedSettingsList.IsSettingWriteOnly(id))
			{
				return false;
			}
			return !this.profileBasedSettingsList.IsSettingReadOnly(id);
		}

		/// <summary>
		///       Stores the file in the profile using any required file wrappers.
		///       </summary>
		/// <param name="filePath">The full file path (e.g. "C:\\Users\\%USERNAME%\\Documents\\sample.zpl").</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If filePath cannot be used to create a printer file name.</exception>
		public void StoreFileOnPrinter(string filePath)
		{
			this.StoreFileOnPrinter(filePath, FileUtilities.GetFileNameOnPrinter(filePath));
		}

		/// <summary>
		///       Stores the file in the profile at the specified location and name using any required file wrappers.
		///       </summary>
		/// <param name="filePath">The full file path (e.g. "C:\\Users\\%USERNAME%\\Documents\\sample.zpl").</param>
		/// <param name="fileNameOnPrinter">The full name of the file on the printer (e.g "R:SAMPLE.ZPL").</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		public void StoreFileOnPrinter(string filePath, string fileNameOnPrinter)
		{
			this.StoreFileOnPrinter(FileReader.ToByteArray(filePath), fileNameOnPrinter);
		}

		/// <summary>
		///       Stores a file in the profile named <c>fileNameOnPrinter</c> with the file contents from
		///       <c>fileContents</c> using any required file wrappers.
		///       </summary>
		/// <param name="fileContents">The contents of the file to store.</param>
		/// <param name="fileNameOnPrinter">The full name of the file on the printer (e.g "R:SAMPLE.ZPL").</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error connecting to the device.</exception>
		public void StoreFileOnPrinter(byte[] fileContents, string fileNameOnPrinter)
		{
			try
			{
				(new ZipUtil(this.pathToProfile)).AddEntry(fileNameOnPrinter, FileWrapper.WrapFileWithCisdfHeader(fileContents, fileNameOnPrinter));
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw new ConnectionException(oException.Message, oException);
			}
		}

		/// <summary>
		///       Stores the specified <c>image</c> to the connected printer as a monochrome image.
		///       </summary>
		/// <param name="deviceDriveAndFileName">Path on the printer where the image will be stored.</param>
		/// <param name="image">The image to be stored on the printer.</param>
		/// <param name="width">Desired width of the printed image, in dots. Passing -1 will preserve original width.</param>
		/// <param name="height">Desired height of the printed image, in dots. Passing -1 will preserve original height.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an issue communicating with the printer (e.g. the connection is not open).</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If <c>printerDriveAndFileName</c> has an incorrect format.</exception>
		public void StoreImage(string deviceDriveAndFileName, ZebraImageI image, int width, int height)
		{
			GraphicsConversionUtilZpl graphicsConversionUtilZpl = new GraphicsConversionUtilZpl();
			try
			{
				using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
				{
					graphicsConversionUtilZpl.SendImageToStream(deviceDriveAndFileName, (ZebraImageInternal)image, width, height, binaryWriter.BaseStream);
					(new ZipUtil(this.pathToProfile)).AddEntry(deviceDriveAndFileName, ((MemoryStream)binaryWriter.BaseStream).ToArray());
				}
			}
			catch (ArgumentException argumentException1)
			{
				ArgumentException argumentException = argumentException1;
				throw new ZebraIllegalArgumentException(argumentException.Message, argumentException);
			}
			catch (IOException oException)
			{
				throw new ConnectionException(oException.Message);
			}
		}

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
		public void StoreImage(string deviceDriveAndFileName, string imageFullPath, int width, int height)
		{
			using (ZebraImageI zebraImageI = ReflectionUtil.InvokeZebraImageFactory_GetImage(imageFullPath))
			{
				this.StoreImage(deviceDriveAndFileName, ReflectionUtil.InvokeZebraImageFactory_GetImage(imageFullPath), width, height);
			}
		}
	}
}