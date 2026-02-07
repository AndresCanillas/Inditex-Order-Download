using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Operations.Internal;

namespace Zebra.Sdk.Settings
{
	/// <summary>
	///       Methods to use the LinkOS 3.2 JSON syntax to get the ranges for a list of SDGs, without the need to use an allconfig.
	///       </summary>
	public class SettingsRanges
	{
		/// <summary>
		///   <markup>
		///     <include item="SMCAutoDocConstructor">
		///       <parameter>Zebra.Sdk.Settings.SettingsRanges</parameter>
		///     </include>
		///   </markup>
		/// </summary>
		public SettingsRanges()
		{
		}

		/// <summary>
		///       Use the LinkOS 3.2 JSON syntax to get the ranges for a list of SDGs, without the need to use an allconfig.
		///       </summary>
		/// <param name="settings">A list of SGD names.</param>
		/// <param name="printerConnection">A connection to a LinkOS printer.</param>
		/// <param name="printerLanguage">the current printer control language</param>
		/// <param name="version">LinkOS version</param>
		/// <returns>A map from setting name to a string representing the range.</returns>
		/// <exception cref="T:System.InvalidOperationException">If the printer is not LinkOS 3.2 or higher.</exception>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If the connection fails.</exception>
		/// <exception cref="T:System.IO.IOException">If there is an error parsing the JSON response from the printer.</exception>
		public static Dictionary<string, string> GetRanges(List<string> settings, Connection printerConnection, PrinterLanguage printerLanguage, LinkOsInformation version)
		{
			if (version.Major < 3 || version.Major == 3 && version.Minor < 2)
			{
				throw new InvalidOperationException("Not supported for LinkOS versions less than 3.2");
			}
			string str = "";
			foreach (string setting in settings)
			{
				str = string.Concat(str, "\"", setting, "\",");
			}
			if (str.Length > 0)
			{
				str = str.Substring(0, str.Length - 1);
			}
			string str1 = string.Format("{{}}{{'get':{{ 'field': ['range'], 'name': [{0}] }}}}".Replace("'", "\""), str);
			SendJsonOperation sendJsonOperation = new SendJsonOperation(printerConnection, printerLanguage, str1);
			Dictionary<string, string> strs = new Dictionary<string, string>();
			return SettingsRanges.ParseJsonForRanges(sendJsonOperation.Execute());
		}

		/// <summary>
		///       Parse the JSON response from the JSON get range command. Assumes that the only field requested in the JSON get 
		///       command was the "range" field.
		///       </summary>
		/// <param name="jsonResponse">Response from a LinkOS 3.2 or greater printer.</param>
		/// <returns>Map from setting names to range strings.</returns>
		public static Dictionary<string, string> ParseJsonForRanges(string jsonResponse)
		{
			string range;
			Dictionary<string, SettingsRanges.SettingRange> obj = JObject.Parse(jsonResponse).ToObject<Dictionary<string, SettingsRanges.SettingRange>>();
			Dictionary<string, string> strs = new Dictionary<string, string>();
			foreach (string key in obj.Keys)
			{
				SettingsRanges.SettingRange item = obj[key];
				Dictionary<string, string> strs1 = strs;
				string str = key;
				if (item != null)
				{
					range = item.Range;
				}
				else
				{
					range = null;
				}
				strs1.Add(str, range);
			}
			return strs;
		}

		[JsonObject]
		private class SettingRange
		{
			private string range;

			[JsonProperty(PropertyName="range")]
			public string Range
			{
				get
				{
					return this.range;
				}
				set
				{
					this.range = value;
				}
			}

			public SettingRange(string range)
			{
				this.range = range;
			}

			public SettingRange()
			{
				this.range = null;
			}
		}
	}
}