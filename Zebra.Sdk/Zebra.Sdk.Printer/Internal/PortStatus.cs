//using Lextm.SharpSnmpLib;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Comm.Snmp.Internal;
using Zebra.Sdk.Printer;

namespace Zebra.Sdk.Printer.Internal
{
	internal class PortStatus
	{
		//private readonly static string PORT_STATUS_OID;

		static PortStatus()
		{
			//PortStatus.PORT_STATUS_OID = "1.3.6.1.2.1.6.13.1.1";
		}

		private PortStatus()
		{
		}

		public static List<TcpPortStatus> GetPortStatus(Connection connection, string getCommunityName)
		{
			List<TcpPortStatus> portStatusViaSnmp;
			try
			{
				portStatusViaSnmp = PortStatus.GetPortStatusViaSnmp(connection, getCommunityName);
			}
			catch (ConnectionException)
			{
				portStatusViaSnmp = PortStatus.GetPortStatusViaSGD(connection);
			}
			return portStatusViaSnmp;
		}

		private static List<TcpPortStatus> GetPortStatusViaSGD(Connection connection)
		{
			List<TcpPortStatus> tcpPortStatuses = new List<TcpPortStatus>();
			string[] strArrays = SGD.GET("ip.netstat", connection).Split(new string[] { "\r\n" }, StringSplitOptions.None);
			if ((int)strArrays.Length <= 2)
			{
				throw new ConnectionException("Port status could not be obtained.");
			}
			for (int i = 2; i < (int)strArrays.Length; i++)
			{
				string[] strArrays1 = Regex.Split(strArrays[i], "\\s+", RegexOptions.None);
				if ((int)strArrays1.Length > 4 && strArrays1[0].Equals("tcp"))
				{
					string str = ((int)strArrays1.Length > 5 ? strArrays1[5] : "");
					string str1 = strArrays1[3].Substring(strArrays1[3].LastIndexOf('.') + 1);
					string str2 = strArrays1[4].Substring(0, strArrays1[4].LastIndexOf('.'));
					if (str2.Equals("*"))
					{
						str2 = "0.0.0.0";
					}
					string str3 = strArrays1[4].Substring(strArrays1[4].LastIndexOf('.') + 1);
					if (str3.Equals("*"))
					{
						str3 = "0";
					}
					tcpPortStatuses.Add(new TcpPortStatus(str1, PortStatus.LookupType(str1), str2, str3, str));
				}
			}
			tcpPortStatuses.Sort((TcpPortStatus tcpPortOne, TcpPortStatus tcpPortTwo) => {
				int num = int.Parse(tcpPortOne.PrinterPort);
				int num1 = int.Parse(tcpPortTwo.PrinterPort);
				if (num < num1)
				{
					return -1;
				}
				if (num > num1)
				{
					return 1;
				}
				int num2 = int.Parse(tcpPortOne.RemotePort);
				int num3 = int.Parse(tcpPortTwo.RemotePort);
				if (num2 < num3)
				{
					return -1;
				}
				if (num2 > num3)
				{
					return 1;
				}
				return 0;
			});
			return tcpPortStatuses;
		}

		private static List<TcpPortStatus> GetPortStatusViaSnmp(Connection connection, string communityName)
		{
			throw new NotImplementedException();
			//List<TcpPortStatus> tcpPortStatuses = new List<TcpPortStatus>();
			//if (!(connection is IpAddressable))
			//{
			//	throw new ConnectionException("Connection does not support SNMP");
			//}
			//Regex regex = new Regex("1.3.6.1.2.1.6.13.1.1.([0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}).([0-9]{1,5}).([0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}).([0-9]{1,5})");
			//string pORTSTATUSOID = PortStatus.PORT_STATUS_OID;
			//string str = "KNOWN";
			//ConnectionAttributes attributes = (new ConnectionAttributeProvider()).GetAttributes(connection);
			//attributes.snmpGetCommunityName = communityName;
			//string address = ((IpAddressable)connection).Address;
			//try
			//{
			//	SnmpGetNext snmpGetNext = new SnmpGetNext(address, new SnmpPreferences(attributes));
			//	while (!str.Equals("UNKNOWN"))
			//	{
			//		snmpGetNext.Init(pORTSTATUSOID);
			//		snmpGetNext.SendRequest();
			//		ISnmpPdu pdu = snmpGetNext.GetPdu();
			//		str = PortStatus.LookupStatus(pdu.Variables[0].Data.ToString());
			//		pORTSTATUSOID = pdu.Variables[0].Id.ToString();
			//		Match match = regex.Match(pORTSTATUSOID);
			//		if (!match.Success)
			//		{
			//			continue;
			//		}
			//		string value = match.Groups[2].Value;
			//		string value1 = match.Groups[3].Value;
			//		string str1 = match.Groups[4].Value;
			//		tcpPortStatuses.Add(new TcpPortStatus(value, PortStatus.LookupType(value), value1, str1, str));
			//	}
			//}
			//catch (Exception exception)
			//{
			//	throw new ConnectionException(exception.Message);
			//}
			//return tcpPortStatuses;
		}

		private static string LookupStatus(string status)
		{
			string str = "UNKNOWN";
			try
			{
				switch (int.Parse(status))
				{
					case 1:
					{
						str = "CLOSED";
						break;
					}
					case 2:
					{
						str = "LISTEN";
						break;
					}
					case 3:
					{
						str = "SYN_SENT";
						break;
					}
					case 4:
					{
						str = "SYN_RCVD";
						break;
					}
					case 5:
					{
						str = "ESTABLISHED";
						break;
					}
					case 6:
					{
						str = "FIN_WAIT_1";
						break;
					}
					case 7:
					{
						str = "FIN_WAIT_2";
						break;
					}
					case 8:
					{
						str = "CLOSED_WAIT";
						break;
					}
					case 9:
					{
						str = "CLOSING";
						break;
					}
					case 10:
					{
						str = "LAST_ACK";
						break;
					}
					case 11:
					{
						str = "TIME_WAIT";
						break;
					}
					default:
					{
						str = "UNKNOWN";
						break;
					}
				}
			}
			catch (Exception)
			{
			}
			return str;
		}

		private static string LookupType(string type)
		{
			int num;
			string str = "";
			try
			{
				num = int.Parse(type);
				if (num <= 110)
				{
					switch (num)
					{
						case 20:
						{
							str = string.Concat(str, "FTP");
							break;
						}
						case 21:
						{
							str = string.Concat(str, "FTP");
							break;
						}
						case 22:
						case 24:
						{
							goto Label0;
						}
						case 23:
						{
							str = string.Concat(str, "Telnet");
							break;
						}
						case 25:
						{
							str = string.Concat(str, "SMTP");
							break;
						}
						default:
						{
							if (num == 80)
							{
								str = string.Concat(str, "HTTP");
								break;
							}
							else if (num == 110)
							{
								str = string.Concat(str, "POP3");
								break;
							}
							else
							{
								goto Label0;
							}
						}
					}
				}
				else if (num == 515)
				{
					str = string.Concat(str, "LPD");
				}
				else if (num == 631)
				{
					str = string.Concat(str, "IPP");
				}
				else
				{
					if (num != 6101)
					{
						goto Label0;
					}
					str = string.Concat(str, "RAW");
				}
			}
			catch (Exception)
			{
			}
			return str;
		Label0:
			if (num >= 9100 && num <= 9112)
			{
				str = string.Concat(str, "RAW");
				return str;
			}
			else
			{
				return str;
			}
		}
	}
}