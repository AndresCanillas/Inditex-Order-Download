using System;
using System.Collections.Generic;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Settings.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Settings
{
	/// <summary>
	///       Methods to use the LinkOS 3.2 JSON syntax to get the values for a list of SDGs, without the need to use an allconfig.
	///       </summary>
	public class SettingsValues
	{
		/// <summary>
		///   <markup>
		///     <include item="SMCAutoDocConstructor">
		///       <parameter>Zebra.Sdk.Settings.SettingsValues</parameter>
		///     </include>
		///   </markup>
		/// </summary>
		public SettingsValues()
		{
		}

		/// <summary>
		///       Get the values for a list of settings from a printer.
		///       </summary>
		/// <param name="settingNames">The settings to be retrieved.</param>
		/// <param name="printerConnection">A connection to the printer.</param>
		/// <param name="printerLanguage">The printer control language for the connection.</param>
		/// <param name="version">The LinkOS version information.</param>
		/// <returns>A map from setting name to value.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If it was not possible to communicate with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If it was not possible to parse the response from the printer.</exception>
		public Dictionary<string, string> GetValues(List<string> settingNames, Connection printerConnection, PrinterLanguage printerLanguage, LinkOsInformation version)
		{
			Connection connection = ConnectionUtil.SelectConnection(printerConnection);
			if (this.ShouldUseJson(printerConnection, printerLanguage, version))
			{
				return this.GetValuesUsingJson(settingNames, connection);
			}
			return this.GetValuesUsingSGD(settingNames, connection);
		}

		private Dictionary<string, string> GetValuesUsingJson(List<string> settingNames, Connection printerConnection)
		{
			byte[] numArray = JsonHelper.BuildQuery(settingNames);
			return JsonHelper.ParseGetResponse(printerConnection.SendAndWaitForValidResponse(numArray, printerConnection.MaxTimeoutForRead, printerConnection.TimeToWaitForMoreData, new JsonValidator()));
		}

		private Dictionary<string, string> GetValuesUsingSGD(List<string> settingNames, Connection printerConnection)
		{
			Dictionary<string, string> strs = new Dictionary<string, string>();
			foreach (string settingName in settingNames)
			{
				string str = string.Concat("! U1 getvar \"", settingName, "\"", StringUtilities.CRLF);
				byte[] numArray = printerConnection.SendAndWaitForValidResponse(Encoding.UTF8.GetBytes(str), printerConnection.MaxTimeoutForRead, printerConnection.TimeToWaitForMoreData, new SgdValidator());
				string str1 = StringUtilities.StripQuotes(Encoding.UTF8.GetString(numArray));
				if (str1.Equals("?"))
				{
					str1 = null;
				}
				strs.Add(settingName, str1);
			}
			return strs;
		}

		/// <summary>
		///       Set each of the settings in settingValues on a printer.
		///       </summary>
		/// <param name="settingValues">Map from setting names to desired values.</param>
		/// <param name="printerConnection">A connection to the printer.</param>
		/// <param name="printerLanguage">The printer control language for the connection.</param>
		/// <param name="version">The LinkOS version information.</param>
		/// <returns>A map from setting name to value.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If it was not possible to communicate with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException"></exception>
		public Dictionary<string, string> SetValues(Dictionary<string, string> settingValues, Connection printerConnection, PrinterLanguage printerLanguage, LinkOsInformation version)
		{
			Connection connection = ConnectionUtil.SelectConnection(printerConnection);
			if (this.ShouldUseJson(printerConnection, printerLanguage, version))
			{
				return this.SetValuesUsingJson(settingValues, connection);
			}
			return this.SetValuesUsingSGD(settingValues, connection);
		}

		private Dictionary<string, string> SetValuesUsingJson(Dictionary<string, string> settingValues, Connection printerConnection)
		{
			byte[] numArray = JsonHelper.BuildSetCommand(settingValues);
			return JsonHelper.ParseGetResponse(printerConnection.SendAndWaitForValidResponse(numArray, printerConnection.MaxTimeoutForRead, printerConnection.TimeToWaitForMoreData, new JsonValidator()));
		}

		private Dictionary<string, string> SetValuesUsingSGD(Dictionary<string, string> settingValues, Connection printerConnection)
		{
			foreach (string key in settingValues.Keys)
			{
				string str = string.Concat(new string[] { "! U1 setvar \"", key, "\" \"", settingValues[key], "\"", StringUtilities.CRLF });
				printerConnection.Write(Encoding.UTF8.GetBytes(str));
			}
			return this.GetValuesUsingSGD(new List<string>(settingValues.Keys), printerConnection);
		}

		private bool ShouldUseJson(Connection printerConnection, PrinterLanguage printerLanguage, LinkOsInformation version)
		{
			bool connected;
			bool flag;
			if (version == null || version.Major < 1)
			{
				return false;
			}
			bool flag1 = printerLanguage == PrinterLanguage.LINE_PRINT;
			if (!(printerConnection is MultichannelConnection))
			{
				flag = printerConnection is StatusConnection;
				connected = !(printerConnection is StatusConnection);
			}
			else
			{
				flag = ((MultichannelConnection)printerConnection).StatusChannel.Connected;
				connected = ((MultichannelConnection)printerConnection).PrintingChannel.Connected;
			}
			if (flag)
			{
				return true;
			}
			if (!connected)
			{
				return false;
			}
			return !flag1;
		}
	}
}