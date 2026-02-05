using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Service.Contracts
{
	public static class TcpHelper
	{
		private static Regex IPv4 = new Regex(@"^\d{1,3}.\d{1,3}.\d{1,3}.\d{1,3}$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
		private static Regex IPv6 = new Regex(@"([0-9a-fA-F]{0,4}:){7}[0-9a-fA-F]{0,4}", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

		/// <summary>
		/// Gets the end point for the provided host name.
		/// </summary>
		public static IPEndPoint GetEndPoint(string host, int port)
		{
			if (IPv4.IsMatch(host) || IPv6.IsMatch(host))
			{
				return new IPEndPoint(IPAddress.Parse(host), port);
			}
			else if (String.Compare(host, "localhost", true) == 0)
			{
				return new IPEndPoint(IPAddress.Loopback, port);
			}
			else
			{
				IPHostEntry he = Dns.GetHostEntry(host);
				if (he.AddressList == null || he.AddressList.Length == 0)
				{
					throw new Exception("DNS server could not resolve host " + host);
				}
				else
				{
					foreach (IPAddress ip in he.AddressList)
					{
						if (ip.AddressFamily == AddressFamily.InterNetwork)
							return new IPEndPoint(ip, port);
					}
					return new IPEndPoint(he.AddressList[he.AddressList.Length - 1], port);
				}
			}
		}

		public static IPEndPoint GetEndPoint(string ep)
		{
			int port;
			string[] tokens = ep.Split(':');
			if (tokens.Length != 2 || !Int32.TryParse(tokens[1], out port))
				throw new Exception("The caller provided an invalid endpoint: " + ep);
			return GetEndPoint(tokens[0], port);
		}

		public static string LocalIPv4
		{
			get
			{
				try
				{
					string[] addresses = GetLocalIPv4Addresses();
					if (addresses.Length > 0)
						return addresses[0];
					else
						return "";
				}
				catch (Exception)
				{
					return "";
				}
			}
		}


		public static string LocalIPv6
		{
			get
			{
				try
				{
					string[] addresses = GetLocalIPv6Addresses();
					if (addresses.Length > 0)
						return addresses[0];
					else
						return "";
				}
				catch (Exception)
				{
					return "";
				}
			}
		}


		public static bool IsLocalAddress(IPAddress address)
		{
			string[] localips = GetLocalAddresses(true);
			string ip = address.ToString();
			string found = localips.FirstOrDefault(p => p == ip);
			return found != null;
		}


		public static bool IsLocalAddress(string address)
		{
			string[] localips = GetLocalAddresses(true);
			string found = localips.FirstOrDefault(p => p == address);
			return found != null;
		}


		public static string[] GetLocalAddresses(bool includeIPv6 = false)
		{
			List<string> ips = new List<string>();
			foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (ni.OperationalStatus == OperationalStatus.Up && ni.GetIPProperties().GatewayAddresses.Count > 0)
				{
					IPInterfaceProperties props = ni.GetIPProperties();
					foreach (UnicastIPAddressInformation addr in props.UnicastAddresses)
					{
						if (addr.Address.AddressFamily != AddressFamily.InterNetwork && addr.Address.AddressFamily != AddressFamily.InterNetworkV6)
							continue;
						if (addr.Address.AddressFamily == AddressFamily.InterNetworkV6 && !includeIPv6)
							continue;
						if (addr.Address.ToString() == "127.0.0.1")
							continue;
						ips.Add(addr.Address.ToString());
					}
				}
			}
			return ips.ToArray();
		}


		public static string[] GetLocalIPv4Addresses()
		{
			List<string> ips = new List<string>();
			foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (ni.OperationalStatus == OperationalStatus.Up && ni.GetIPProperties().GatewayAddresses.Count > 0)
				{
					IPInterfaceProperties props = ni.GetIPProperties();
					foreach (UnicastIPAddressInformation addr in props.UnicastAddresses)
					{
						if (addr.Address.AddressFamily != AddressFamily.InterNetwork && addr.Address.AddressFamily != AddressFamily.InterNetworkV6)
							continue;
						if (addr.Address.AddressFamily == AddressFamily.InterNetworkV6)
							continue;
						if (addr.Address.ToString() == "127.0.0.1")
							continue;
						ips.Add(addr.Address.ToString());
					}
				}
			}
			return ips.ToArray();
		}


		public static string[] GetLocalIPv6Addresses()
		{
			List<string> ips = new List<string>();
			foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (ni.OperationalStatus == OperationalStatus.Up && ni.GetIPProperties().GatewayAddresses.Count > 0)
				{
					IPInterfaceProperties props = ni.GetIPProperties();
					foreach (UnicastIPAddressInformation addr in props.UnicastAddresses)
					{
						if (addr.Address.AddressFamily != AddressFamily.InterNetwork && addr.Address.AddressFamily != AddressFamily.InterNetworkV6)
							continue;
						if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
							continue;
						if (addr.Address.ToString() == "::1" || addr.Address.ToString() == "0:0:0:0:0:0:0:1")
							continue;
						ips.Add(addr.Address.ToString());
					}
				}
			}
			return ips.ToArray();
		}


		public static string GetHostAddress(string address)
		{
			IPAddress IPV6Address = null;
			if (IPv4.IsMatch(address) || IPv6.IsMatch(address))
				return address;
			else
			{
				IPAddress[] arr = Dns.GetHostAddresses(address);
				if (arr.Length > 0)
				{
					for (int i = 0; i < arr.Length; i++)
					{
						IPAddress ip = arr[i];
						if (
							(ip.AddressFamily == AddressFamily.InterNetwork ||
							ip.AddressFamily == AddressFamily.InterNetworkV6) &&
							!ip.IsIPv6LinkLocal)
						{
							if (ip.AddressFamily == AddressFamily.InterNetworkV6)
								IPV6Address = ip;
							else
								return ip.ToString();
						}
					}
					if (IPV6Address != null)
						return IPV6Address.ToString();
				}
				throw new Exception("DNS could not resolve host " + address + ", try using an IP address instead.");
			}
		}


		public static string GetHostName(string address)
		{
			try
			{
				if (IPv4.IsMatch(address) || IPv6.IsMatch(address))
				{
					return GetHostName(IPAddress.Parse(address));
				}
				else
				{
					return address;
				}
			}
			catch
			{
				return address;
			}
		}


		public static string GetHostName(IPAddress address)
		{
			IPHostEntry h = Dns.GetHostEntry(address);
			return h.HostName;
		}


		public static bool IsLocalEndPoint(string endpoint)
		{
			IPEndPoint ep = ParseIPEndPoint(endpoint);
			return IsLocalEndPoint(ep);
		}


		public static bool IsLocalEndPoint(IPEndPoint ep)
		{
			if (ep.Address.AddressFamily == AddressFamily.InterNetworkV6)
			{
				if (ep.Address == IPAddress.IPv6Loopback || ep.Address.ToString() == "::1" || ep.Address.ToString() == "0:0:0:0:0:0:0:1")
					return true;
				string[] addresses = GetLocalIPv6Addresses();
				string address = ep.Address.ToString();
				string found = addresses.FirstOrDefault(p => p == ep.Address.ToString());
				return found != null;
			}
			else
			{
				if (ep.Address == IPAddress.Loopback || ep.Address.ToString() == "127.0.0.1")
					return true;
				string[] addresses = GetLocalIPv4Addresses();
				string address = ep.Address.ToString();
				string found = addresses.FirstOrDefault(p => p == ep.Address.ToString());
				return found != null;
			}
		}


		public static IPEndPoint ParseIPEndPoint(string endpoint)
		{
			IPAddress ip;
			int port = 5000;
			string[] tokens = endpoint.Split(':');
			if (tokens.Length > 2)
			{
				throw new Exception("IPV6 Addresses not supported");
			}
			if (!IPAddress.TryParse(tokens[0], out ip))
			{
				throw new Exception("Invalid IPV4 Address");
			}
			if (tokens.Length > 1 && !int.TryParse(tokens[tokens.Length - 1], out port))
			{
				throw new Exception("Invalid port");
			}
			return new IPEndPoint(ip, port);
		}

		public static int CharCount(this string str, char c)
		{
			int count = 0;
			int idx = str.IndexOf(c);
			while (idx >= 0)
			{
				count++;
				idx = str.IndexOf(c, idx + 1);
			}
			return count;
		}
	}
}
