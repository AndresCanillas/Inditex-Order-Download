using System;
using System.IO;

namespace Zebra.Sdk.Device.Internal
{
	internal abstract class ProfileComponentHandler
	{
		internal Action<Stream> settingsHandler;

		internal Action<Stream> alertsHandler;

		internal Action<Stream> firmwareHandler;

		internal Action<Stream> firmwareDisplayNameHandler;

		internal Action<Stream> supplementHandler;

		internal Action<string, Stream> fileHandler;

		public ProfileComponentHandler()
		{
			ProfileComponentHandler profileComponentHandler = this;
			this.settingsHandler = new Action<Stream>(profileComponentHandler.SettingsHandler);
			ProfileComponentHandler profileComponentHandler1 = this;
			this.alertsHandler = new Action<Stream>(profileComponentHandler1.AlertsHandler);
			ProfileComponentHandler profileComponentHandler2 = this;
			this.firmwareHandler = new Action<Stream>(profileComponentHandler2.FirmwareHandler);
			ProfileComponentHandler profileComponentHandler3 = this;
			this.firmwareDisplayNameHandler = new Action<Stream>(profileComponentHandler3.FirmwareDisplayNameHandler);
			ProfileComponentHandler profileComponentHandler4 = this;
			this.supplementHandler = new Action<Stream>(profileComponentHandler4.SupplementHandler);
			ProfileComponentHandler profileComponentHandler5 = this;
			this.fileHandler = new Action<string, Stream>(profileComponentHandler5.FileHandler);
		}

		public abstract void AlertsHandler(Stream sourceStream);

		public abstract void FileHandler(string fileName, Stream sourceStream);

		public abstract void FirmwareDisplayNameHandler(Stream sourceStream);

		public abstract void FirmwareHandler(Stream sourceStream);

		public abstract void SettingsHandler(Stream sourceStream);

		public abstract void SupplementHandler(Stream sourceStream);
	}
}