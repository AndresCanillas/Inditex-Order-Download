using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Settings;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class ProfileHelper
	{
		public const string ALERTS_JSON_NAME = "alerts.json";

		public const string SETTINGS_JSON_NAME = "settings.json";

		public const string PROFILE_SUPPLEMENT_NAME = "profileSupplement.txt";

		public const string FIRMWARE_FILE_NAME = "firmwareFile.txt";

		public const string FIRMWARE_FILE_USER_SPECIFIED_NAME = "firmwareFileUserSpecifiedName.txt";

		public ProfileHelper()
		{
		}

		public static void CommitAlertsToProfile(string pathToProfile, List<PrinterAlert> alerts)
		{
			ProfileHelper.ModifyProfile(pathToProfile, new ProfileHelper.OurAlertsWriter(alerts));
		}

		public static string CreateJson(List<PrinterAlert> alerts)
		{
			string str;
			try
			{
				str = JsonConvert.SerializeObject(alerts, Formatting.Indented).Replace("\r", "");
			}
			catch (Exception exception)
			{
				throw new SettingsException(exception.Message);
			}
			return str;
		}

		public static void DeleteFileFromProfile(string pathToProfile, string fileNameToDelete)
		{
			ProfileHelper.ModifyProfile(pathToProfile, new ProfileHelper.OurComponentDeleter(fileNameToDelete));
		}

		public static List<PrinterAlert> GetAlertsFromJson(string pathToOutputFile)
		{
			return ProfileHelper.GetAlertsFromJsonData((new ZipUtil(pathToOutputFile)).GetEntryContents("alerts.json"));
		}

		public static List<PrinterAlert> GetAlertsFromJsonData(string alertJsonData)
		{
			List<PrinterAlert> printerAlerts = JsonConvert.DeserializeObject<List<PrinterAlert>>(alertJsonData);
			if (printerAlerts == null)
			{
				return new List<PrinterAlert>();
			}
			return printerAlerts;
		}

		public static Dictionary<string, string> GetSettingsFromProfile(string pathToOutputFile)
		{
			return StringUtilities.ConvertKeyValueJsonToMap((new ZipUtil(pathToOutputFile)).GetEntryContents("settings.json"));
		}

		public static void HandleSpecialCases(Dictionary<string, string> settingsAsMap, RestoreType restoreType)
		{
			string item = null;
			if (settingsAsMap.ContainsKey("internal_wired.ip.protocol"))
			{
				item = settingsAsMap["internal_wired.ip.protocol"];
			}
			if ((restoreType != RestoreType.ARCHIVE || item == null ? true : !Regex.IsMatch(item, "permanent", RegexOptions.IgnoreCase)))
			{
				settingsAsMap.Remove("internal_wired.ip.addr");
			}
			string str = null;
			if (settingsAsMap.ContainsKey("wlan.ip.protocol"))
			{
				str = settingsAsMap["wlan.ip.protocol"];
			}
			if ((restoreType != RestoreType.ARCHIVE || str == null ? true : !Regex.IsMatch(str, "permanent", RegexOptions.IgnoreCase)))
			{
				settingsAsMap.Remove("ip.addr");
				settingsAsMap.Remove("wlan.ip.addr");
			}
			string item1 = null;
			if (settingsAsMap.ContainsKey("external_wired.ip.protocol"))
			{
				item1 = settingsAsMap["external_wired.ip.protocol"];
			}
			if ((restoreType != RestoreType.ARCHIVE || item1 == null ? true : !Regex.IsMatch(item1, "permanent", RegexOptions.IgnoreCase)))
			{
				settingsAsMap.Remove("external_wired.ip.addr");
			}
		}

		public static bool IsSpecialProfileFile(string fileName)
		{
			if (fileName.Equals("settings.json") || fileName.Equals("alerts.json") || fileName.Equals("profileSupplement.txt") || fileName.Equals("firmwareFile.txt"))
			{
				return true;
			}
			return fileName.Equals("firmwareFileUserSpecifiedName.txt");
		}

		public static void ModifyProfile(string pathToProfile, ProfileHelper.ProfileComponentTransformer ourThing)
		{
			byte[] byteArray = FileReader.ToByteArray(pathToProfile);
			File.Create("proftmp").Dispose();
			FileInfo fileInfo = new FileInfo("proftmp");
			using (FileStream fileStream = new FileStream(fileInfo.FullName, FileMode.OpenOrCreate))
			{
				fileStream.Write(byteArray, 0, (int)byteArray.Length);
			}
			File.Delete(pathToProfile);
			string fullName = fileInfo.FullName;
			try
			{
				using (FileStream fileStream1 = new FileStream(pathToProfile, FileMode.Create))
				{
					using (ZipArchive zipArchive = new ZipArchive(fileStream1, ZipArchiveMode.Create))
					{
						ZipUtil zipUtil = new ZipUtil(fullName);
						foreach (string entryName in zipUtil.GetEntryNames())
						{
							byte[] numArray = (ourThing.ShouldTransformComponent(entryName) ? ourThing.TransformComponent() : zipUtil.ExtractEntry(entryName));
							if (numArray == null || numArray.Length == 0)
							{
								continue;
							}
							using (Stream stream = zipArchive.CreateEntry(entryName).Open())
							{
								stream.Write(numArray, 0, (int)numArray.Length);
							}
						}
						fileInfo.Delete();
					}
				}
			}
			catch (Exception exception)
			{
				Console.WriteLine(exception.StackTrace);
			}
		}

		internal class OurAlertsWriter : ProfileHelper.ProfileComponentTransformer
		{
			private List<PrinterAlert> myAlerts;

			public OurAlertsWriter(List<PrinterAlert> alerts)
			{
				this.myAlerts = alerts;
			}

			public bool ShouldTransformComponent(string fileName)
			{
				return fileName.Equals("alerts.json");
			}

			public byte[] TransformComponent()
			{
				byte[] bytes;
				try
				{
					bytes = Encoding.UTF8.GetBytes(ProfileHelper.CreateJson(this.myAlerts));
				}
				catch
				{
					bytes = null;
				}
				return bytes;
			}
		}

		internal class OurComponentDeleter : ProfileHelper.ProfileComponentTransformer
		{
			private string myDeleteSpecification;

			public OurComponentDeleter(string deleteSpecification)
			{
				this.myDeleteSpecification = deleteSpecification;
			}

			public bool ShouldTransformComponent(string fileName)
			{
				return Regex.IsMatch(fileName, Regex.Escape(this.myDeleteSpecification).Replace("\\*", ".*"));
			}

			public byte[] TransformComponent()
			{
				return null;
			}
		}

		public interface ProfileComponentTransformer
		{
			bool ShouldTransformComponent(string fileName);

			byte[] TransformComponent();
		}
	}
}