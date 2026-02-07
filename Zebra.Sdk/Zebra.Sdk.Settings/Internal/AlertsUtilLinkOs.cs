using System;
using System.Collections.Generic;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Device;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Settings;

namespace Zebra.Sdk.Settings.Internal
{
	internal class AlertsUtilLinkOs
	{
		private Connection connection;

		public AlertsUtilLinkOs(ZebraPrinterLinkOs printer)
		{
			this.connection = printer.Connection;
		}

		public AlertsUtilLinkOs(Connection connection)
		{
			this.connection = connection;
		}

		public List<PrinterAlert> GetAlerts()
		{
			return this.GetConfiguredAlerts();
		}

		private string GetAlertSgdValue(PrinterAlert alert)
		{
			string str = (alert.OnSet ? "Y" : "N");
			string str1 = (alert.OnClear ? "Y" : "N");
			return string.Concat(new object[] { alert.Condition.ToString(), ",", alert.DestinationAsSgdString, ",", str, ",", str1, ",", alert.DestinationAddress, ",", alert.Port, ",N,", alert.SgdName });
		}

		private List<PrinterAlert> GetConfiguredAlerts()
		{
			List<PrinterAlert> printerAlerts = new List<PrinterAlert>();
			Connection connection = ConnectionUtil.SelectConnection(this.connection);
			string item = JsonHelper.ParseGetResponse(connection.SendAndWaitForValidResponse(Encoding.UTF8.GetBytes("{}{\"alerts.configured\":null}"), connection.MaxTimeoutForRead, connection.TimeToWaitForMoreData, new JsonValidator()))["alerts.configured"];
			if (!item.Trim().Equals(""))
			{
				string[] strArrays = item.Split(new string[] { "|" }, StringSplitOptions.None);
				for (int i = 0; i < (int)strArrays.Length; i++)
				{
					string str = strArrays[i];
					string[] strArrays1 = str.Split(new char[] { ',' });
					if ((int)strArrays1.Length != 7 && (int)strArrays1.Length != 8)
					{
						throw new ZebraIllegalArgumentException(string.Concat("Invalid alert [", str, "] from printer."));
					}
					AlertCondition alertCondition = AlertCondition.CreateAlertCondition(strArrays1[0]);
					AlertDestination alertDestination = AlertDestination.CreateAlertDestination(strArrays1[1]);
					bool flag = strArrays1[2].Equals("Y");
					bool flag1 = strArrays1[3].Equals("Y");
					string str1 = strArrays1[4];
					string str2 = strArrays1[5];
					bool flag2 = strArrays1[6].Equals("Y");
					string str3 = "";
					if ((int)strArrays1.Length > 7)
					{
						str3 = strArrays1[7];
					}
					try
					{
						printerAlerts.Add(new PrinterAlert(alertCondition, alertDestination, str3, flag, flag1, str1, int.Parse(str2), flag2));
					}
					catch (FormatException)
					{
						printerAlerts.Add(new PrinterAlert(alertCondition, alertDestination, str3, flag, flag1, str1, 0, flag2));
					}
				}
			}
			return printerAlerts;
		}

		public void RemoveAllAlerts()
		{
			this.connection = ConnectionUtil.SelectConnection(this.connection);
			this.connection.SendAndWaitForValidResponse(Encoding.UTF8.GetBytes("{}{\"alerts.configured\":\"\"}"), this.connection.MaxTimeoutForRead, this.connection.TimeToWaitForMoreData, new JsonValidator());
		}

		private void SendAlertsViaSgd(List<PrinterAlert> alerts)
		{
			foreach (PrinterAlert alert in alerts)
			{
				byte[] numArray = JsonHelper.BuildSetCommand(new Dictionary<string, string>()
				{
					{ "alerts.add", this.GetAlertSgdValue(alert) }
				});
				this.connection = ConnectionUtil.SelectConnection(this.connection);
				this.connection.SendAndWaitForValidResponse(numArray, this.connection.MaxTimeoutForRead, this.connection.TimeToWaitForMoreData, new JsonValidator());
			}
		}

		public void SetAlerts(List<PrinterAlert> alerts)
		{
			this.SendAlertsViaSgd(alerts);
		}

		public static void SetAlerts(List<PrinterAlert> alerts, Connection c)
		{
			(new AlertsUtilLinkOs(c)).SendAlertsViaSgd(alerts);
		}
	}
}