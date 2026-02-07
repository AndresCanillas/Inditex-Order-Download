//using Lextm.SharpSnmpLib;
using System;
using System.Collections.Generic;
using Zebra.Sdk.Comm.Snmp.Internal;
using Zebra.Sdk.Printer.Discovery;

namespace Zebra.Sdk.Util.Internal
{
	internal class NetworkUtil
	{
		private readonly static List<string> cardCommandIdValues;

		static NetworkUtil()
		{
			NetworkUtil.cardCommandIdValues = new List<string>()
			{
				"zmotif"
			};
		}

		public NetworkUtil()
		{
		}

		private static Dictionary<string, string> AddDeviceIdArtributesToMap(string deviceId)
		{
			Dictionary<string, string> strs = new Dictionary<string, string>();
			string[] strArrays = deviceId.Split(new char[] { ';' });
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				string[] strArrays1 = strArrays[i].Split(new char[] { ':' });
				if ((int)strArrays1.Length > 1)
				{
					string str = strArrays1[0];
					string str1 = strArrays1[1].Trim();
					switch (str)
					{
						case "MANUFACTURER":
						case "MFG":
						{
							strs.Add("MFG", str1);
							break;
						}
						case "MODEL":
						case "MDL":
						{
							strs.Add("MODEL", str1);
							break;
						}
						case "COMMAND SET":
						case "CMD":
						{
							strs.Add("CMD", str1);
							break;
						}
						case "SERIAL NUMBER":
						case "SN":
						{
							strs.Add("SERIAL_NUMBER", str1);
							break;
						}
					}
				}
			}
			return strs;
		}

		internal static Dictionary<string, string> GetIEEE1284DeviceId(string address)
		{
			throw new NotImplementedException();
			//Dictionary<string, string> strs = new Dictionary<string, string>();
			//try
			//{
			//	SnmpGet snmpGet = new SnmpGet(address, new SnmpPreferences());
			//	snmpGet.Init("1.3.6.1.4.1.2699.1.2.1.2.1.1.3.1");
			//	snmpGet.SendRequest();
			//	string str = snmpGet.GetPdu().Variables[0].Data.ToString();
			//	if (!string.IsNullOrEmpty(str))
			//	{
			//		strs = NetworkUtil.AddDeviceIdArtributesToMap(str);
			//	}
			//}
			//catch (Exception exception)
			//{
			//}
			//return strs;
		}

		internal static bool IsCardPrinter(string address)
		{
			bool flag = false;
			try
			{
				if (!string.IsNullOrEmpty(address))
				{
					Dictionary<string, string> eEE1284DeviceId = NetworkUtil.GetIEEE1284DeviceId(address);
					if (eEE1284DeviceId.Count > 0)
					{
						string item = "";
						if (eEE1284DeviceId.ContainsKey("CMD"))
						{
							item = eEE1284DeviceId["CMD"];
						}
						if (eEE1284DeviceId.ContainsKey("MFG") && (eEE1284DeviceId["MFG"].ToLower().Contains("card") || NetworkUtil.IsCardPrinterCmdId(item)))
						{
							flag = true;
						}
					}
				}
			}
			catch
			{
			}
			return flag;
		}

		private static bool IsCardPrinterCmdId(string cmd)
		{
			bool flag = false;
			if (!string.IsNullOrEmpty(cmd) && NetworkUtil.cardCommandIdValues.Contains(cmd.ToLower()))
			{
				flag = true;
			}
			return flag;
		}

		internal static void StartSinglePrinterDiscovery(string tcpAddress, DiscoveryHandler singlePrinterDiscoveryHandler)
		{
			LinkedList<string> strs = new LinkedList<string>();
			strs.AddLast(tcpAddress);
			NetworkDiscoverer.FindPrinters(singlePrinterDiscoveryHandler, new List<string>(strs));
		}
	}
}