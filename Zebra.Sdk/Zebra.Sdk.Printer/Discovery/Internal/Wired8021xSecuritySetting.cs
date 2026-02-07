using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class Wired8021xSecuritySetting : EnumAttributes
	{
		public static Wired8021xSecuritySetting NONE;

		public static Wired8021xSecuritySetting PEAP;

		public static Wired8021xSecuritySetting EAP_TLS;

		public static Wired8021xSecuritySetting EAP_TTLS;

		private static List<EnumAttributes> possibleSettings;

		static Wired8021xSecuritySetting()
		{
			Wired8021xSecuritySetting.NONE = new Wired8021xSecuritySetting(0, "None");
			Wired8021xSecuritySetting.PEAP = new Wired8021xSecuritySetting(1, "PEAP");
			Wired8021xSecuritySetting.EAP_TLS = new Wired8021xSecuritySetting(2, "EAP-TLS");
			Wired8021xSecuritySetting.EAP_TTLS = new Wired8021xSecuritySetting(3, "EAP-TTLS");
			Wired8021xSecuritySetting.possibleSettings = new List<EnumAttributes>()
			{
				Wired8021xSecuritySetting.NONE,
				Wired8021xSecuritySetting.PEAP,
				Wired8021xSecuritySetting.EAP_TLS,
				Wired8021xSecuritySetting.EAP_TTLS
			};
		}

		private Wired8021xSecuritySetting(int value, string description) : base(value, description)
		{
		}

		public static Wired8021xSecuritySetting IntToEnum(int value)
		{
			Wired8021xSecuritySetting nONE = Wired8021xSecuritySetting.NONE;
			foreach (Wired8021xSecuritySetting possibleSetting in Wired8021xSecuritySetting.possibleSettings)
			{
				if (possibleSetting.Value != value)
				{
					continue;
				}
				nONE = possibleSetting;
				return nONE;
			}
			return nONE;
		}
	}
}