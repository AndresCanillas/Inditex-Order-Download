using Newtonsoft.Json;
using System;
using Zebra.Sdk.Settings.Internal;

namespace Zebra.Sdk.Settings
{
	/// <summary>
	///       A class that represents an internal device setting.
	///       </summary>
	[JsonObject]
	public class Setting
	{
		private string @value;

		private string type;

		private string range;

		private bool clone;

		private bool archive;

		private string access;

		/// <summary>
		///       Gets or sets a string that describes the access permissions for the setting. RW is returned for settings that have 
		///       read and write permissions. R is returned for settings that are read-only W is returned for settings that are write-only
		///       </summary>
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

		/// <summary>
		///        Gets or sets if this setting can be applied when loading a backup
		///       </summary>
		[JsonProperty(PropertyName="archive")]
		public bool Archive
		{
			get
			{
				return this.archive;
			}
			set
			{
				this.archive = value;
			}
		}

		/// <summary>
		///       Gets or sets if this setting can be applied when loading a profile
		///       </summary>
		[JsonProperty(PropertyName="clone")]
		public bool Clone
		{
			get
			{
				return this.clone;
			}
			set
			{
				this.clone = value;
			}
		}

		/// <summary>
		///       Returns true if the setting does not have write access.
		///       </summary>
		[JsonIgnore]
		public bool IsReadOnly
		{
			get
			{
				return !this.access.Contains("W");
			}
		}

		/// <summary>
		///       Returns true if the setting does not have read access.
		///       </summary>
		[JsonIgnore]
		public bool IsWriteOnly
		{
			get
			{
				return !this.access.Contains("R");
			}
		}

		/// <summary>
		///       Gets or sets a string that describes the acceptable range of values for this setting.
		///       </summary>
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

		/// <summary>
		///       Gets or sets a string describing the data type of the setting.
		///       </summary>
		[JsonProperty(PropertyName="type")]
		public string Type
		{
			get
			{
				return this.type;
			}
			set
			{
				this.type = value;
			}
		}

		/// <summary>
		///       Gets or sets the setting's value.
		///       </summary>
		[JsonProperty(PropertyName="value")]
		public string Value
		{
			get
			{
				return this.@value;
			}
			set
			{
				this.@value = value;
			}
		}

		/// <summary>
		///   <markup>
		///     <include item="SMCAutoDocConstructor">
		///       <parameter>Zebra.Sdk.Settings.Setting</parameter>
		///     </include>
		///   </markup>
		/// </summary>
		public Setting()
		{
		}

		/// <summary>
		///       Returns true if <c>value</c> is valid for the given setting.
		///       </summary>
		/// <param name="value">Setting value.</param>
		/// <returns>true if value is within the setting's range</returns>
		/// <exception cref="T:System.FormatException"></exception>
		public bool IsValid(string value)
		{
			SettingRange settingRangeFloat = null;
			SettingType settingType = SettingType.FromString(this.type);
			if (settingType != null)
			{
				string str = settingType.ToString();
				if (str == "double")
				{
					settingRangeFloat = new SettingRangeFloat(this.range);
				}
				else if (str == "integer")
				{
					settingRangeFloat = new SettingRangeInteger(this.range);
				}
				else if (str == "enum" || str == "bool")
				{
					settingRangeFloat = new SettingRangeChoices(this.range);
				}
				else if (str == "string")
				{
					settingRangeFloat = new SettingRangeString(this.range);
				}
				else if (str == "ipv4-address")
				{
					settingRangeFloat = new SettingRangeIpV4Address(this.range);
				}
			}
			if (settingRangeFloat == null)
			{
				return true;
			}
			return settingRangeFloat.IsInRange(value);
		}

		/// <summary>
		///       Retruns a human readable string of the setting.
		///       </summary>
		/// <returns>Setting [settingData=value=Value type=Type range=Range].</returns>
		public override string ToString()
		{
			return string.Concat(new string[] { "Setting [settingData=value= ", this.@value, " type= ", this.type, " range= ", this.range, "]" });
		}
	}
}