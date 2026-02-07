using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Settings;
using Zebra.Sdk.Settings.Internal;

namespace Zebra.Sdk.Printer.Operations.Internal
{
	internal class SettingsGrabberOperation : PrinterOperationBase<Dictionary<string, Setting>>
	{
		public SettingsGrabberOperation(Connection connection, PrinterLanguage language) : base(connection, language)
		{
		}

		public override Dictionary<string, Setting> Execute()
		{
			Dictionary<string, Setting> dictionary;
			this.SelectStatusChannelIfOpen();
			this.IsOkToProceed();
			ZebraSettingsListFromConnection zebraSettingsListFromConnection = new ZebraSettingsListFromConnection(this.connection);
			try
			{
				OrderedDictionary allSettings = zebraSettingsListFromConnection.GetAllSettings();
				if (allSettings == null || allSettings.Count == 0)
				{
					throw new SettingsException("Error Retrieving Settings or No Printer Connection");
				}
				dictionary = allSettings.Cast<DictionaryEntry>().ToDictionary<DictionaryEntry, string, Setting>((DictionaryEntry k) => (string)k.Key, (DictionaryEntry v) => (Setting)v.Value);
			}
			catch (SettingsException settingsException)
			{
				throw new ConnectionException(settingsException.Message);
			}
			return dictionary;
		}

		private void IsOkToProceed()
		{
			if (base.IsPrintingChannelInLineMode())
			{
				throw new ConnectionException("Cannot retrieve settings from printer over printing channel when in line mode");
			}
		}
	}
}