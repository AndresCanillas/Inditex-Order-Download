using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Device;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Discovery;
using Zebra.Sdk.Settings;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Weblink
{
	/// <summary>
	///       Task to configure a printers Weblink setting.
	///       </summary>
	public class WeblinkConfigurator
	{
		private Connection connection;

		private WeblinkConfigurationState myState;

		private ConfigurationStatus myStatus;

		private ConnectionReestablisher reestablisher;

		private WeblinkConfigurator.WeblinkLocationToSet weblinkLocationToSet;

		private PrinterLanguage printerLanguage;

		private LinkOsInformation linkOsVersion;

		/// <summary>
		///       Returns the current <see cref="T:Zebra.Sdk.Weblink.WeblinkConfigurationState" />.
		///       </summary>
		public WeblinkConfigurationState CurrentState
		{
			get
			{
				return this.myState;
			}
		}

		/// <summary>
		///       Returns the <see cref="T:Zebra.Sdk.Weblink.ConfigurationStatus" /> of the weblink configuration task.
		///       </summary>
		public ConfigurationStatus Status
		{
			get
			{
				return this.myStatus;
			}
		}

		/// <summary>
		///       Initializes a new instance of the WeblinkConfiguratior class.
		///       </summary>
		/// <param name="printer">The <see cref="T:Zebra.Sdk.Printer.Discovery.DiscoveredPrinter" /> to configure.</param>
		public WeblinkConfigurator(DiscoveredPrinter printer) : this(printer.GetConnection())
		{
		}

		/// <summary>
		///       Initializes a new instance of the WeblinkConfiguratior class.
		///       </summary>
		/// <param name="connection">A <see cref="T:Zebra.Sdk.Comm.Connection" /> to a printer</param>
		public WeblinkConfigurator(Connection connection)
		{
			this.connection = connection;
			this.myState = WeblinkConfigurationState.ConnectToPrinter;
			this.myStatus = ConfigurationStatus.NOT_STARTED;
		}

		private bool CheckConnection(string setting, Dictionary<string, string> allSettings)
		{
			string item = null;
			if (allSettings.ContainsKey(setting))
			{
				item = allSettings[setting];
			}
			if (item == null)
			{
				return false;
			}
			return !item.Equals("0");
		}

		/// <summary>
		///       Configures a printer to connect to a Zebra Weblink server and attempts to validate a successful connection.
		///       </summary>
		/// <param name="webLinkAddress">Weblink address to set.</param>
		/// <param name="strategy">Determines which weblink setting will be configured.</param>
		/// <param name="webConfigStatusUpdater">Callback object to report task status.</param>
		/// <exception cref="T:Zebra.Sdk.Weblink.ZebraWeblinkException">Thrown when a configuration error occurs.</exception>
		public void Configure(string webLinkAddress, WeblinkAddressStrategy strategy, WeblinkConfigurationStateUpdater webConfigStatusUpdater)
		{
			this.myStatus = ConfigurationStatus.IN_PROCESS;
			this.UpdateState(webConfigStatusUpdater, WeblinkConfigurationState.ConnectToPrinter);
			if (string.IsNullOrEmpty(webLinkAddress) || !this.IsValidWeblinkUrl(webLinkAddress))
			{
				this.myStatus = ConfigurationStatus.CONFIGURATION_FAILED;
				this.UpdateState(webConfigStatusUpdater, this.myState);
				throw new ZebraWeblinkException("Invalid weblink address.");
			}
			ConfigurationStatus configurationStatu = this.myStatus;
			try
			{
				try
				{
					this.ConnectToPrinter();
					this.reestablisher = this.connection.GetConnectionReestablisher((long)60000);
					this.UpdateState(webConfigStatusUpdater, WeblinkConfigurationState.GetSettings);
					Dictionary<string, string> settings = this.GetSettings();
					Dictionary<string, string> settingsToConfigure = this.GetSettingsToConfigure(webLinkAddress, strategy, webConfigStatusUpdater, settings);
					if (settingsToConfigure.Count > 0)
					{
						(new SettingsValues()).SetValues(settingsToConfigure, this.connection, this.printerLanguage, this.linkOsVersion);
						Sleeper.Sleep((long)1000);
						this.RestartPrinter(webConfigStatusUpdater);
						this.ReconnectToPrinter(webConfigStatusUpdater);
					}
					this.VerifyPrinterNumConnections(settingsToConfigure, settings, webConfigStatusUpdater);
					configurationStatu = ConfigurationStatus.SUCCESSFULLY_COMPLETED;
				}
				catch (ZebraWeblinkException)
				{
					configurationStatu = ConfigurationStatus.CONFIGURATION_FAILED;
					throw;
				}
				catch (Exception exception)
				{
					configurationStatu = ConfigurationStatus.CONFIGURATION_FAILED;
					throw new ZebraWeblinkException(exception);
				}
			}
			finally
			{
				if (this.connection != null)
				{
					try
					{
						this.connection.Close();
					}
					catch (ConnectionException)
					{
					}
				}
				this.myStatus = configurationStatu;
				this.UpdateState(webConfigStatusUpdater, this.myState);
			}
		}

		private void ConnectToPrinter()
		{
			this.connection.Open();
		}

		private LinkOsInformation GetLinkOsVersionInfo()
		{
			if (this.linkOsVersion == null)
			{
				string str = SGD.GET("appl.link_os_version", this.connection);
				if (str.Equals("?"))
				{
					throw new NotALinkOsPrinterException();
				}
				this.linkOsVersion = new LinkOsInformation(str);
			}
			return this.linkOsVersion;
		}

		private PrinterLanguage GetPrinterLanguage()
		{
			if (this.printerLanguage == null)
			{
				string str = SGD.GET("device.languages", this.connection);
				if (str.Equals("?"))
				{
					throw new NotALinkOsPrinterException();
				}
				this.printerLanguage = PrinterLanguage.GetLanguage(str);
			}
			return this.printerLanguage;
		}

		private Dictionary<string, string> GetSettings()
		{
			List<string> strs = new List<string>()
			{
				"rtc.date",
				"weblink.ip.conn1.location",
				"weblink.ip.conn2.location",
				"weblink.ip.conn1.num_connections",
				"weblink.ip.conn2.num_connections",
				"appl.link_os_version"
			};
			this.printerLanguage = this.GetPrinterLanguage();
			this.linkOsVersion = this.GetLinkOsVersionInfo();
			return (new SettingsValues()).GetValues(strs, this.connection, this.printerLanguage, this.linkOsVersion);
		}

		private Dictionary<string, string> GetSettingsToConfigure(string webLinkAddress, WeblinkAddressStrategy strategy, WeblinkConfigurationStateUpdater webConfigStatusUpdater, Dictionary<string, string> allSettings)
		{
			this.UpdateState(webConfigStatusUpdater, WeblinkConfigurationState.ConfigureWeblink);
			Dictionary<string, string> strs = new Dictionary<string, string>();
			if (this.RtcTooOld(allSettings))
			{
				strs.Add("rtc.date", "01-01-2013");
			}
			string item = "";
			if (!allSettings.ContainsKey("weblink.ip.conn1.location"))
			{
				throw new ZebraWeblinkException("Weblink settings not retrieved");
			}
			item = allSettings["weblink.ip.conn1.location"];
			if (item == null)
			{
				try
				{
					SGD.SET("device.reset", "", this.connection);
				}
				catch (ConnectionException)
				{
				}
				throw new ZebraWeblinkException("Weblink settings unavailable. Rebooting printer. Please try again.");
			}
			bool flag = this.CheckConnection("weblink.ip.conn1.num_connections", allSettings);
			string str = "";
			if (!allSettings.ContainsKey("weblink.ip.conn2.location"))
			{
				throw new ZebraWeblinkException("Weblink settings not retrieved");
			}
			str = allSettings["weblink.ip.conn2.location"];
			bool flag1 = this.CheckConnection("weblink.ip.conn2.num_connections", allSettings);
			if (Regex.IsMatch(item, string.Format("^{0}$", webLinkAddress), RegexOptions.IgnoreCase) && Regex.IsMatch(str, string.Format("^{0}$", webLinkAddress), RegexOptions.IgnoreCase))
			{
				if (strategy != WeblinkAddressStrategy.FORCE_CONNECTION_1)
				{
					webConfigStatusUpdater.ProgressUpdate(string.Concat("Both weblink locations set to ", webLinkAddress, ", clearing location 1"));
					strs.Add("weblink.ip.conn1.location", "");
					this.weblinkLocationToSet = WeblinkConfigurator.WeblinkLocationToSet.SET_LOCATION_2;
				}
				else
				{
					webConfigStatusUpdater.ProgressUpdate(string.Concat("Both weblink locations set to ", webLinkAddress, ", clearing location 2"));
					strs.Add("weblink.ip.conn2.location", "");
					this.weblinkLocationToSet = WeblinkConfigurator.WeblinkLocationToSet.SET_LOCATION_1;
				}
			}
			else if (Regex.IsMatch(item, string.Format("^{0}$", webLinkAddress), RegexOptions.IgnoreCase))
			{
				if (strategy == WeblinkAddressStrategy.FORCE_CONNECTION_2)
				{
					webConfigStatusUpdater.ProgressUpdate(string.Concat("Weblink location 1 already set to ", webLinkAddress, ", configuring for location 2"));
					strs.Add("weblink.ip.conn2.location", webLinkAddress);
					strs.Add("weblink.ip.conn1.location", "");
					this.weblinkLocationToSet = WeblinkConfigurator.WeblinkLocationToSet.SET_LOCATION_2;
				}
				else if (!flag)
				{
					webConfigStatusUpdater.ProgressUpdate(string.Concat("Weblink location 1 already set to ", webLinkAddress));
					this.weblinkLocationToSet = WeblinkConfigurator.WeblinkLocationToSet.SET_LOCATION_1;
				}
				else
				{
					webConfigStatusUpdater.ProgressUpdate(string.Concat("Weblink location 1 already connected to ", webLinkAddress));
					this.UpdateState(webConfigStatusUpdater, WeblinkConfigurationState.RestartPrinter);
					this.UpdateState(webConfigStatusUpdater, WeblinkConfigurationState.ReconnectToPrinter);
					this.weblinkLocationToSet = WeblinkConfigurator.WeblinkLocationToSet.SET_LOCATION_1;
				}
			}
			else if (!Regex.IsMatch(str, string.Format("^{0}$", webLinkAddress), RegexOptions.IgnoreCase))
			{
				switch (strategy)
				{
					case WeblinkAddressStrategy.AUTO_SELECT:
					{
						if (str == string.Empty || !this.IsValidWeblinkUrl(str))
						{
							strs.Add("weblink.ip.conn2.location", webLinkAddress);
							this.weblinkLocationToSet = WeblinkConfigurator.WeblinkLocationToSet.SET_LOCATION_2;
							break;
						}
						else if (item == string.Empty || !this.IsValidWeblinkUrl(item))
						{
							strs.Add("weblink.ip.conn1.location", webLinkAddress);
							this.weblinkLocationToSet = WeblinkConfigurator.WeblinkLocationToSet.SET_LOCATION_1;
							break;
						}
						else if (!flag1)
						{
							strs.Add("weblink.ip.conn2.location", webLinkAddress);
							this.weblinkLocationToSet = WeblinkConfigurator.WeblinkLocationToSet.SET_LOCATION_2;
							break;
						}
						else if (flag)
						{
							strs.Add("weblink.ip.conn2.location", webLinkAddress);
							this.weblinkLocationToSet = WeblinkConfigurator.WeblinkLocationToSet.SET_LOCATION_2;
							break;
						}
						else
						{
							strs.Add("weblink.ip.conn1.location", webLinkAddress);
							this.weblinkLocationToSet = WeblinkConfigurator.WeblinkLocationToSet.SET_LOCATION_1;
							break;
						}
					}
					case WeblinkAddressStrategy.FORCE_CONNECTION_1:
					{
						strs.Add("weblink.ip.conn1.location", webLinkAddress);
						this.weblinkLocationToSet = WeblinkConfigurator.WeblinkLocationToSet.SET_LOCATION_1;
						break;
					}
					case WeblinkAddressStrategy.FORCE_CONNECTION_2:
					{
						strs.Add("weblink.ip.conn2.location", webLinkAddress);
						this.weblinkLocationToSet = WeblinkConfigurator.WeblinkLocationToSet.SET_LOCATION_2;
						break;
					}
					default:
					{
							//goto Label0;
							break;
					}
				}
				webConfigStatusUpdater.ProgressUpdate(string.Format("Setting weblink location {0} to {1}", (this.weblinkLocationToSet == WeblinkConfigurator.WeblinkLocationToSet.SET_LOCATION_1 ? "1" : "2"), webLinkAddress));
			}
			else if (strategy == WeblinkAddressStrategy.FORCE_CONNECTION_1)
			{
				webConfigStatusUpdater.ProgressUpdate(string.Concat("Weblink location 2 already set to ", webLinkAddress, ", configuring for location 1"));
				strs.Add("weblink.ip.conn1.location", webLinkAddress);
				strs.Add("weblink.ip.conn2.location", "");
				this.weblinkLocationToSet = WeblinkConfigurator.WeblinkLocationToSet.SET_LOCATION_1;
			}
			else if (!flag1)
			{
				webConfigStatusUpdater.ProgressUpdate(string.Concat("Weblink location 2 already set to ", webLinkAddress));
				this.weblinkLocationToSet = WeblinkConfigurator.WeblinkLocationToSet.SET_LOCATION_2;
			}
			else
			{
				webConfigStatusUpdater.ProgressUpdate(string.Concat("Weblink location 2 already connected to ", webLinkAddress));
				this.UpdateState(webConfigStatusUpdater, WeblinkConfigurationState.RestartPrinter);
				this.UpdateState(webConfigStatusUpdater, WeblinkConfigurationState.ReconnectToPrinter);
				this.weblinkLocationToSet = WeblinkConfigurator.WeblinkLocationToSet.SET_LOCATION_2;
			}
			return strs;
		}

		private bool IsConnected(Dictionary<string, string> newSettings)
		{
			int num = -1;
			WeblinkConfigurator.WeblinkLocationToSet weblinkLocationToSet = this.weblinkLocationToSet;
			if (weblinkLocationToSet == WeblinkConfigurator.WeblinkLocationToSet.SET_LOCATION_1)
			{
				num = int.Parse(newSettings["weblink.ip.conn1.num_connections"]);
			}
			else if (weblinkLocationToSet == WeblinkConfigurator.WeblinkLocationToSet.SET_LOCATION_2)
			{
				num = int.Parse(newSettings["weblink.ip.conn2.num_connections"]);
			}
			return num > 0;
		}

		/// <summary>
		///       Returns true if the supplied weblink <c>url</c> is valid for the Zebra weblink server.
		///       </summary>
		/// <param name="url">Potential URL to a Zebra Weblink server.</param>
		/// <returns>true if the URL is a properly formed URL for the Zebra Weblink server.</returns>
		public bool IsValidWeblinkUrl(string url)
		{
			bool flag = false;
			try
			{
				flag = (new Regex("^(https://)[a-zA-Z0-9]+([\\-\\.]{1}[a-zA-Z0-9]*)*(:[0-9]{1,5})?(/.*)?$")).IsMatch(url);
			}
			catch
			{
			}
			return flag;
		}

		private void ReconnectToPrinter(WeblinkConfigurationStateUpdater webConfigStatusUpdater)
		{
			this.UpdateState(webConfigStatusUpdater, WeblinkConfigurationState.ReconnectToPrinter);
			Sleeper.Sleep((long)30000);
			this.reestablisher.ReestablishConnection(new WeblinkConfigurator.ReconnectionHandler()
			{
				printerOnline = (ZebraPrinterLinkOs zebraPrinter, string firmwareVersion) => this.connection = zebraPrinter.Connection
			});
		}

		private void RestartPrinter(WeblinkConfigurationStateUpdater webConfigStatusUpdater)
		{
			this.UpdateState(webConfigStatusUpdater, WeblinkConfigurationState.RestartPrinter);
			SGD.SET("device.reset", "", this.connection);
			this.connection.Close();
		}

		private bool RtcTooOld(Dictionary<string, string> allSettings)
		{
			string item = allSettings["rtc.date"];
			try
			{
				if (DateTime.ParseExact(item, "MM-dd-yyyy", CultureInfo.InvariantCulture).CompareTo(DateTime.Parse("01-01-2013")) < 0)
				{
					return true;
				}
			}
			catch
			{
			}
			return false;
		}

		private void UpdateState(WeblinkConfigurationStateUpdater webConfigStateUpdater, WeblinkConfigurationState newState)
		{
			webConfigStateUpdater.UpdateState(newState);
			this.myState = newState;
		}

		private void VerifyPrinterNumConnections(Dictionary<string, string> settings, Dictionary<string, string> allSettings, WeblinkConfigurationStateUpdater webConfigStatusUpdater)
		{
			this.UpdateState(webConfigStatusUpdater, WeblinkConfigurationState.VerifyWeblinkConnection);
			bool i = false;
			if ((new LinkOsInformation(allSettings["appl.link_os_version"])).Major >= 2)
			{
				try
				{
					if (this.weblinkLocationToSet == WeblinkConfigurator.WeblinkLocationToSet.SET_LOCATION_1)
					{
						if (int.Parse(allSettings["weblink.ip.conn1.num_connections"]) > 0)
						{
							return;
						}
					}
					else if (this.weblinkLocationToSet == WeblinkConfigurator.WeblinkLocationToSet.SET_LOCATION_2 && int.Parse(allSettings["weblink.ip.conn2.num_connections"]) > 0)
					{
						return;
					}
					int num = 0;
					for (i = this.IsConnected(this.GetSettings()); !i && num < 5; i = this.IsConnected(this.GetSettings()))
					{
						Sleeper.Sleep((long)2000);
						num++;
					}
				}
				catch (ZebraIllegalArgumentException zebraIllegalArgumentException)
				{
					throw new ZebraWeblinkException(zebraIllegalArgumentException);
				}
				catch (FormatException)
				{
				}
			}
			if (!i)
			{
				throw new ZebraWeblinkException("Could not verify the connection to the Zebra Weblink server. You may need to review the printer weblink logs and/or the server logs if there are any connection issues.");
			}
		}

		internal class ReconnectionHandler : PrinterReconnectionHandler
		{
			internal Action<ZebraPrinterLinkOs, string> printerOnline;

			public ReconnectionHandler()
			{
				WeblinkConfigurator.ReconnectionHandler reconnectionHandler = this;
				this.printerOnline = new Action<ZebraPrinterLinkOs, string>(reconnectionHandler.PrinterOnline);
			}

			public void PrinterOnline(ZebraPrinterLinkOs printer, string firmwareVersion)
			{
				this.printerOnline(printer, firmwareVersion);
			}
		}

		private enum WeblinkLocationToSet
		{
			SET_LOCATION_1,
			SET_LOCATION_2
		}
	}
}