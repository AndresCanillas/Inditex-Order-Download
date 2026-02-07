using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Device;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Settings.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class FileUtilZpl : FileUtilA
	{
		private const string READ_ONLY = "READ ONLY";

		private const string ONBOARD_FLASH = "ONBOARD FLASH";

		private const string RAM = "RAM";

		public FileUtilZpl(Connection printerConnection) : base(printerConnection)
		{
		}

		public override PrinterFilePropertiesList ExtractFilePropertiesFromDirResult(string dirResult)
		{
			PrinterFilePropertiesList printerFilePropertiesList = null;
			if (!dirResult.Trim().StartsWith("<?xml version='1.0'?>\r\n<ZEBRA-ELTRON-PERSONALITY>"))
			{
				printerFilePropertiesList = (!JsonHelper.IsValidJson(Encoding.UTF8.GetBytes(dirResult)) ? base.ExtractFilePropertiesFromDirResult(dirResult) : this.ExtractFilePropertiesFromJsonFileDriveListingResponse(dirResult));
			}
			else
			{
				printerFilePropertiesList = this.ExtractFilePropertiesFromHZLResponse(dirResult);
			}
			return printerFilePropertiesList;
		}

		private PrinterFilePropertiesList ExtractFilePropertiesFromHZLResponse(string dirResult)
		{
			PrinterFilePropertiesList printerFilePropertiesList = new PrinterFilePropertiesList();
			try
			{
				using (MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(dirResult.Trim())))
				{
					XmlDocument xmlDocument = new XmlDocument();
					xmlDocument.Load(memoryStream);
					foreach (XmlNode elementsByTagName in xmlDocument.GetElementsByTagName("OBJECT"))
					{
						if (elementsByTagName.NodeType != XmlNodeType.Element)
						{
							continue;
						}
						XmlAttributeCollection attributes = elementsByTagName.Attributes;
						string value = elementsByTagName.FirstChild.Value;
						int num = 0;
						try
						{
							num = int.Parse(attributes.GetNamedItem("SIZE").Value);
						}
						catch (Exception)
						{
						}
						printerFilePropertiesList.Add(new PrinterFilePropertiesZpl(attributes.GetNamedItem("MEMORY-LOCATION").Value, value, attributes.GetNamedItem("TYPE").Value, (long)num));
					}
				}
			}
			catch (Exception exception1)
			{
				throw new ZebraIllegalArgumentException(exception1.Message);
			}
			return printerFilePropertiesList;
		}

		private PrinterFilePropertiesList ExtractFilePropertiesFromJsonFileDriveListingResponse(string dirResult)
		{
			PrinterFilePropertiesList printerFilePropertiesList = new PrinterFilePropertiesList();
			try
			{
				foreach (KeyValuePair<string, Dictionary<string, FileUtilZpl.FileObjectWithCrc32Data>> item in JObject.Parse(dirResult).ToObject<Dictionary<string, Dictionary<string, Dictionary<string, FileUtilZpl.FileObjectWithCrc32Data>>>>()[ZPLUtilities.FILE_DRIVE_LISTING_SETTING_NAME])
				{
					string key = item.Key;
					foreach (KeyValuePair<string, FileUtilZpl.FileObjectWithCrc32Data> value in item.Value)
					{
						long size = value.Value.Size;
						long crc32 = value.Value.Crc32;
						string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(value.Key);
						string str = Path.GetExtension(value.Key).TrimStart(new char[] { '.' });
						PrinterFilePropertiesZpl printerFilePropertiesZpl = new PrinterFilePropertiesZpl(string.Concat(key, ":"), fileNameWithoutExtension, str, size, crc32);
						printerFilePropertiesList.Add(printerFilePropertiesZpl);
					}
				}
			}
			catch (Exception exception)
			{
				throw new ZebraIllegalArgumentException(exception.Message);
			}
			return printerFilePropertiesList;
		}

		private PrinterFilePropertiesList GetObjectsListForDriveTypes(List<StorageInfo> storageInfoList, HashSet<Zebra.Sdk.Printer.DriveType> driveTypes, PrinterLanguage printerLanguage)
		{
			PrinterFilePropertiesList printerFilePropertiesList = new PrinterFilePropertiesList();
			foreach (StorageInfo storageInfo in storageInfoList)
			{
				if (!driveTypes.Contains(storageInfo.driveType))
				{
					continue;
				}
				printerFilePropertiesList.AddAll(this.GetObjectsListFromDrive(Convert.ToString(storageInfo.driveLetter), printerLanguage).GetObjectsProperties());
			}
			return printerFilePropertiesList;
		}

		private PrinterFilePropertiesList GetObjectsListFromAllDrives(PrinterLanguage printerLanguage)
		{
			return this.GetObjectsListFromDrive("", printerLanguage);
		}

		private PrinterFilePropertiesList GetObjectsListFromDrive(string driveLetter, PrinterLanguage printerLanguage)
		{
			PrinterCommandImpl printerCommandImpl;
			if (printerLanguage != PrinterLanguage.LINE_PRINT || this.printerConnection is StatusConnection)
			{
				printerCommandImpl = new PrinterCommandImpl(string.Concat(new string[] { "{}{\"", ZPLUtilities.FILE_DRIVE_LISTING_SETTING_NAME, "\":\"", driveLetter, "\"}" }));
			}
			else
			{
				this.printerConnection.Write(Encoding.UTF8.GetBytes(string.Concat(new string[] { "! U1 setvar \"", ZPLUtilities.FILE_DRIVE_LISTING_SETTING_NAME, "\" \"", driveLetter, "\"", StringUtilities.CRLF })));
				printerCommandImpl = new PrinterCommandImpl(string.Concat("! U1 getvar \"", ZPLUtilities.FILE_DRIVE_LISTING_SETTING_NAME, "\"", StringUtilities.CRLF));
			}
			string str = Encoding.UTF8.GetString(printerCommandImpl.SendAndWaitForValidJsonResponse(this.printerConnection));
			if (printerLanguage == PrinterLanguage.LINE_PRINT && !(this.printerConnection is StatusConnection))
			{
				str = str.Replace("^\"|\"$", "");
				str = str.TrimStart(new char[] { '\"' }).TrimEnd(new char[] { '\"' });
				str = string.Concat("{\"file.drive_listing\":", str, "}");
			}
			return this.ExtractFilePropertiesFromJsonFileDriveListingResponse(str);
		}

		public List<StorageInfo> GetStorageInfo()
		{
			PrinterCommand printerCommandImpl = new PrinterCommandImpl(ZPLUtilities.PRINTER_GET_STORAGE_INFO_COMMAND);
			return ZPLUtilities.ParseHWCommand(Encoding.UTF8.GetString(printerCommandImpl.SendAndWaitForResponse(this.printerConnection, this.printerConnection.MaxTimeoutForRead, this.printerConnection.TimeToWaitForMoreData)));
		}

		public List<StorageInfo> GetStorageInfoViaJsonChannel()
		{
			List<StorageInfo> storageInfos;
			try
			{
				PrinterCommandImpl printerCommandImpl = new PrinterCommandImpl(string.Concat("{}{\"", ZPLUtilities.FILE_DRIVE_INFO_SETTING_NAME, "\":null}"));
				storageInfos = ZPLUtilities.ParseFileDriveInfoJson(Encoding.UTF8.GetString(printerCommandImpl.SendAndWaitForValidJsonResponse(this.printerConnection)));
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw new ConnectionException(oException.Message, oException);
			}
			return storageInfos;
		}

		public List<StorageInfo> GetStorageInfoViaSgd()
		{
			List<StorageInfo> storageInfos;
			try
			{
				PrinterCommandImpl printerCommandImpl = new PrinterCommandImpl(string.Concat("! U1 getvar \"", ZPLUtilities.FILE_DRIVE_INFO_SETTING_NAME, "\"\r\n"));
				string str = Regex.Replace(Encoding.UTF8.GetString(printerCommandImpl.SendAndWaitForValidJsonResponse(this.printerConnection)), "^\"|\"$", "");
				storageInfos = ZPLUtilities.ParseFileDriveInfoJson(string.Format("{{ \"file.drive_info\" : {0} }}", str));
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw new ConnectionException(oException.Message, oException);
			}
			return storageInfos;
		}

		public override string[] RetrieveFileNames()
		{
			return this.RetrieveFilePropertiesFromPrinter().GetFileNamesFromProperties();
		}

		public override string[] RetrieveFileNames(string[] extensions)
		{
			return this.RetrieveFilePropertiesFromPrinter().FilterByExtension(extensions).GetFileNamesFromProperties();
		}

		public override List<PrinterObjectProperties> RetrieveObjectsProperties()
		{
			return this.RetrieveFilePropertiesFromPrinter().GetObjectsProperties();
		}

		public List<PrinterObjectProperties> RetrieveObjectsProperties(List<StorageInfo> storageInfoList, HashSet<Zebra.Sdk.Printer.DriveType> driveTypes)
		{
			List<PrinterObjectProperties> printerObjectProperties = null;
			PrinterFilePropertiesList printerFilePropertiesList = this.RetrieveFilePropertiesFromPrinter();
			if (driveTypes != null && driveTypes.Count != 0)
			{
				Dictionary<string, Zebra.Sdk.Printer.DriveType> strs = new Dictionary<string, Zebra.Sdk.Printer.DriveType>();
				foreach (StorageInfo storageInfo in storageInfoList)
				{
					strs.Add(Convert.ToString(storageInfo.driveLetter), storageInfo.driveType);
				}
				printerObjectProperties = new List<PrinterObjectProperties>(printerFilePropertiesList.GetObjectsProperties());
				List<PrinterObjectProperties>.Enumerator enumerator = printerFilePropertiesList.GetObjectsProperties().GetEnumerator();
				while (enumerator.MoveNext())
				{
					PrinterObjectProperties current = enumerator.Current;
					string str = current.DrivePrefix.Replace(":", "");
					Zebra.Sdk.Printer.DriveType? nullable = null;
					nullable = (!Regex.IsMatch(str, "Z", RegexOptions.IgnoreCase) ? new Zebra.Sdk.Printer.DriveType?(strs[str]) : new Zebra.Sdk.Printer.DriveType?(Zebra.Sdk.Printer.DriveType.READ_ONLY));
					HashSet<Zebra.Sdk.Printer.DriveType> driveTypes1 = driveTypes;
					Zebra.Sdk.Printer.DriveType? nullable1 = nullable;
					if (driveTypes1.Contains((nullable1.HasValue ? nullable1.GetValueOrDefault() : Zebra.Sdk.Printer.DriveType.UNKNOWN)))
					{
						continue;
					}
					printerObjectProperties.Remove(current);
				}
			}
			return printerObjectProperties ?? printerFilePropertiesList.GetObjectsProperties();
		}

		public List<PrinterObjectProperties> RetrieveObjectsPropertiesWithCrc32(List<StorageInfo> storageInfoList, HashSet<Zebra.Sdk.Printer.DriveType> driveTypes, PrinterLanguage printerLanguage)
		{
			PrinterFilePropertiesList printerFilePropertiesList;
			printerFilePropertiesList = (driveTypes == null || driveTypes.Count == 0 ? this.GetObjectsListFromAllDrives(printerLanguage) : this.GetObjectsListForDriveTypes(storageInfoList, driveTypes, printerLanguage));
			return printerFilePropertiesList.GetObjectsProperties();
		}

		[JsonObject]
		private class FileObjectWithCrc32Data
		{
			private string access;

			private long size;

			private long crc32;

			private string date;

			private string flags;

			[JsonProperty(PropertyName="access")]
			public string Access
			{
				get
				{
					return this.access;
				}
				set
				{
					this.access = value;
				}
			}

			[JsonProperty(PropertyName="crc32")]
			public long Crc32
			{
				get
				{
					return this.crc32;
				}
				set
				{
					this.crc32 = value;
				}
			}

			[JsonProperty(PropertyName="date")]
			public string Date
			{
				get
				{
					return this.date;
				}
				set
				{
					this.date = value;
				}
			}

			[JsonProperty(PropertyName="flags")]
			public string Flags
			{
				get
				{
					return this.flags;
				}
				set
				{
					this.flags = value;
				}
			}

			[JsonProperty(PropertyName="size")]
			public long Size
			{
				get
				{
					return this.size;
				}
				set
				{
					this.size = value;
				}
			}

			public FileObjectWithCrc32Data()
			{
			}
		}
	}
}