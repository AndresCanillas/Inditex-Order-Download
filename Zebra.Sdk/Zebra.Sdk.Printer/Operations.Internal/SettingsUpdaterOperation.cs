using System;
using System.Collections.Generic;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Settings;
using Zebra.Sdk.Settings.Internal;

namespace Zebra.Sdk.Printer.Operations.Internal
{
	internal class SettingsUpdaterOperation : PrinterOperationBase<object>
	{
		private Dictionary<string, string> settingsToSet;

		public SettingsUpdaterOperation(Connection connection, Dictionary<string, string> settingsToSet, PrinterLanguage language) : base(connection, language)
		{
			this.settingsToSet = settingsToSet;
		}

		public override object Execute()
		{
			this.SelectStatusChannelIfOpen();
			this.IsOkToProceed();
			try
			{
				(new ZebraSettingsListFromConnection(this.connection)).SetSettings(this.settingsToSet);
			}
			catch (SettingsException settingsException)
			{
				throw new ConnectionException(settingsException.Message);
			}
			return null;
		}

		private void IsOkToProceed()
		{
			if (base.IsPrintingChannelInLineMode())
			{
				throw new ConnectionException("Cannot update settings over printing channel when in line mode");
			}
		}

		public Dictionary<string, string> Process()
		{
			this.SelectStatusChannelIfOpen();
			this.IsOkToProceed();
			return (new ZebraSettingsListFromConnection(this.connection)).ProcessSettingsViaMap(this.settingsToSet);
		}
	}
}