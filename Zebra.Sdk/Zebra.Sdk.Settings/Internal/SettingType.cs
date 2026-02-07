using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Settings.Internal
{
	internal class SettingType
	{
		public const string INTEGER_TYPE = "integer";

		public const string ENUM_TYPE = "enum";

		public const string STRING_TYPE = "string";

		public const string BOOL_TYPE = "bool";

		public const string DOUBLE_TYPE = "double";

		public const string IPV4ADDRESS_TYPE = "ipv4-address";

		public static SettingType INTEGER;

		public static SettingType ENUM;

		public static SettingType STRING;

		public static SettingType BOOL;

		public static SettingType DOUBLE;

		public static SettingType IPV4ADDRESS;

		private static List<SettingType> settingTypes;

		private string description;

		static SettingType()
		{
			SettingType.INTEGER = new SettingType("integer");
			SettingType.ENUM = new SettingType("enum");
			SettingType.STRING = new SettingType("string");
			SettingType.BOOL = new SettingType("bool");
			SettingType.DOUBLE = new SettingType("double");
			SettingType.IPV4ADDRESS = new SettingType("ipv4-address");
			SettingType.settingTypes = new List<SettingType>()
			{
				SettingType.INTEGER,
				SettingType.ENUM,
				SettingType.STRING,
				SettingType.BOOL,
				SettingType.DOUBLE,
				SettingType.IPV4ADDRESS
			};
		}

		private SettingType(string description)
		{
			this.description = description;
		}

		public static SettingType FromString(string name)
		{
			SettingType settingType;
			if (name != null)
			{
				List<SettingType>.Enumerator enumerator = SettingType.settingTypes.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						SettingType current = enumerator.Current;
						if (!string.Equals(name, current.ToString(), StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}
						settingType = current;
						return settingType;
					}
					return null;
				}
				finally
				{
					((IDisposable)enumerator).Dispose();
				}
			}
			return null;
		}

		public override string ToString()
		{
			return this.description;
		}
	}
}