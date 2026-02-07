using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Internal;
using Zebra.Sdk.Settings;
using Zebra.Sdk.Settings.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Device
{
	/// <summary>
	///       Class which allows you to store a zprofile to a mirror server.
	///       </summary>
	public class ProfileToMirrorServer
	{
		private string server;

		private string user;

		private string password;

		private string pathToProfile;

		/// <summary>
		///       Creates an instance of a class which can be used to store a profile onto a mirror server.
		///       </summary>
		/// <param name="pathToProfile">Path to the profile to load. (e.g. /home/user/profile.zprofile).</param>
		/// <exception cref="T:System.IO.IOException">If an I/O error occurs.</exception>
		public ProfileToMirrorServer(string pathToProfile)
		{
			this.pathToProfile = pathToProfile;
		}

		protected virtual void DeleteAllFilesOnMirrorServer()
		{
			using (FTP fTP = new FTP(this.server, this.user, this.password))
			{
				fTP.DeleteAllFilesAndSubDirectories(new List<string>(new string[] { "appl", "commands", "files" }));
			}
		}

		private void LoadAlertsFromProfile(string pathToOutputFile)
		{
			List<PrinterAlert> alertsFromJson = ProfileHelper.GetAlertsFromJson(pathToOutputFile);
			PrinterlessConnection printerlessConnection = new PrinterlessConnection();
			AlertsUtilLinkOs.SetAlerts(alertsFromJson, printerlessConnection);
			string stuffWrittenOnConnection = printerlessConnection.GetStuffWrittenOnConnection();
			this.StoreFileViaFtp("commands", Encoding.UTF8.GetBytes(stuffWrittenOnConnection), "alerts.txt");
		}

		private void LoadFilesFromProfile(string pathToOutputFile)
		{
			List<FtpFileHolder> ftpFileHolders = new List<FtpFileHolder>();
			ZipUtil zipUtil = new ZipUtil(pathToOutputFile);
			foreach (string entryName in zipUtil.GetEntryNames())
			{
				if (ProfileHelper.IsSpecialProfileFile(entryName))
				{
					continue;
				}
				byte[] numArray = zipUtil.ExtractEntry(entryName);
				ftpFileHolders.Add(new FtpFileHolder("commands", entryName, new MemoryStream(numArray)));
			}
			this.StoreFilesViaFtp(ftpFileHolders);
		}

		private void LoadFirmwareFromProfile(Profile p)
		{
			string str = Encoding.UTF8.GetString((new ZipUtil(this.pathToProfile)).ExtractEntry("firmwareFileUserSpecifiedName.txt"));
			if (str != null && !string.IsNullOrEmpty(str))
			{
				ZipUtil zipUtil = new ZipUtil(this.pathToProfile);
				using (Stream inputStreamToEntry = zipUtil.GetInputStreamToEntry("firmwareFile.txt"))
				{
					if (inputStreamToEntry != null)
					{
						this.StoreFileViaFtp("appl", str, inputStreamToEntry);
						zipUtil.CloseStreams();
					}
				}
			}
		}

		/// <summary>
		///       Stores the profile to the mirror server.
		///       </summary>
		/// <param name="server">The FTP server path.</param>
		/// <param name="user">The FTP user name. (The user should have read/write/create/delete access.)</param>
		/// <param name="password">The FTP password.</param>
		/// <returns>A list of the errors that happened while uploading settings and files.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		/// <exception cref="T:System.IO.FileNotFoundException">If the profile is not found.</exception>
		/// <exception cref="T:System.IO.IOException">If an I/O error occurs.</exception>
		public List<string> SendToMirrorServer(string server, string user, string password)
		{
			this.server = server;
			this.user = user;
			this.password = password;
			List<string> strs = new List<string>();
			this.DeleteAllFilesOnMirrorServer();
			try
			{
				Profile profile = new Profile(this.pathToProfile);
				this.LoadFirmwareFromProfile(profile);
				byte[] numArray = JsonHelper.BuildSetCommand(profile.GetClonableSettingValues());
				this.StoreFileViaFtp("commands", numArray, "settings.txt");
			}
			catch (SettingsException)
			{
			}
			try
			{
				this.LoadAlertsFromProfile(this.pathToProfile);
			}
			catch (SettingsException)
			{
			}
			this.LoadFilesFromProfile(this.pathToProfile);
			return strs;
		}

		internal virtual void StoreFilesViaFtp(List<FtpFileHolder> files)
		{
			using (FTP fTP = new FTP(this.server, this.user, this.password))
			{
				fTP.PutFiles(files);
			}
		}

		protected virtual void StoreFileViaFtp(string filePath, byte[] fileContents, string fileName)
		{
			using (FTP fTP = new FTP(this.server, this.user, this.password))
			{
				fTP.PutFile(filePath, fileName, fileContents);
			}
		}

		protected virtual void StoreFileViaFtp(string filePath, string fileName, Stream stream)
		{
			using (FTP fTP = new FTP(this.server, this.user, this.password))
			{
				fTP.PutFile(filePath, fileName, stream);
			}
		}
	}
}