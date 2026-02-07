using System;
using System.Collections.Generic;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Device;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Settings.Internal;

namespace Zebra.Sdk.Printer.Operations.Internal
{
	internal class ConfiguredAlertsGrabberOperation : PrinterOperationBase<List<PrinterAlert>>
	{
		public ConfiguredAlertsGrabberOperation(Connection connection, PrinterLanguage language) : base(connection, language)
		{
		}

		public override List<PrinterAlert> Execute()
		{
			List<PrinterAlert> alerts;
			this.SelectStatusChannelIfOpen();
			this.IsOkToProceed();
			try
			{
				alerts = (new AlertsUtilLinkOs(this.connection)).GetAlerts();
			}
			catch (ZebraIllegalArgumentException zebraIllegalArgumentException)
			{
				throw new ConnectionException(zebraIllegalArgumentException.Message);
			}
			return alerts;
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