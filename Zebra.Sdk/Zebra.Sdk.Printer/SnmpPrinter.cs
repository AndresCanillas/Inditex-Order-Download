using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Comm.Snmp.Internal;
using Zebra.Sdk.Device;
using Zebra.Sdk.Settings.Internal;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       An instance of an SNMP only Zebra printer.
	///       </summary>
	public class SnmpPrinter
	{
		private Zebra.Sdk.Comm.Connection connection;

		private string getCommunityName;

		private string setCommunityName;

		/// <summary>
		///       Gets the SNMP get community name.
		///       </summary>
		public string CommunityNameGet
		{
			get
			{
				return this.getCommunityName;
			}
		}

		/// <summary>
		///       Gets the SNMP set community name.
		///       </summary>
		public string CommunityNameSet
		{
			get
			{
				return this.setCommunityName;
			}
		}

		protected virtual int DiscoTimeoutInMSec
		{
			get
			{
				return 10000;
			}
		}

		/// <summary>
		///       Creates an instance of a Zebra printer which is limited to only SNMP operations.
		///       </summary>
		/// <param name="address">The IP Address or DNS Hostname.</param>
		/// <exception cref="T:Zebra.Sdk.Printer.SnmpException">If there was an exception communicating over SNMP.</exception>
		public SnmpPrinter(string address) : this(address, "public", "public")
		{
		}

		/// <summary>
		///       Creates an instance of a Zebra printer, with the given community names, which is limited to only SNMP operations.
		///       </summary>
		/// <param name="address">The IP Address or DNS Hostname.</param>
		/// <param name="getCommunityName">SNMP get community name.</param>
		/// <param name="setCommunityName">SNMP set community name.</param>
		/// <exception cref="T:Zebra.Sdk.Printer.SnmpException">If there was an exception communicating over SNMP.</exception>
		public SnmpPrinter(string address, string getCommunityName, string setCommunityName)
		{
			this.getCommunityName = getCommunityName;
			this.setCommunityName = setCommunityName;
			try
			{
				this.Init(address);
			}
			catch (ArgumentException)
			{
				throw new SnmpException("Could not resolve DNS name to IP Address.");
			}
			catch (SocketException)
			{
				throw new SnmpException("Could not resolve DNS name to IP Address.");
			}
			catch (TimeoutException timeoutException)
			{
				throw new SnmpException(timeoutException.Message);
			}
		}

		protected Zebra.Sdk.Comm.Connection GetConnection(string address)
		{
			int num = 15000;
			Task<IPAddress[]> hostAddressesAsync = Dns.GetHostAddressesAsync(address);
			if (!hostAddressesAsync.Wait(num))
			{
				throw new TimeoutException(string.Concat("Operation timed out retrieving ", address));
			}
			return new TcpConnection(hostAddressesAsync.Result[0].ToString(), 0);
		}

		/// <summary>
		///       Gets the value of the specified <c>oid</c>.
		///       </summary>
		/// <param name="oid">Object identifier.</param>
		/// <returns>The value of the OID.</returns>
		/// <exception cref="T:Zebra.Sdk.Printer.SnmpException">If there was an exception communicating over SNMP.</exception>
		public string GetOidValue(string oid)
		{
			string str;
			try
			{
				str = this.GetSnmpImpl(this.CommunityNameGet, this.CommunityNameSet, SettingType.STRING).Get(((IpAddressable)this.connection).Address, oid);
			}
			catch (ArgumentException)
			{
				throw new SnmpException(string.Concat("oid ", oid, " not found."));
			}
			catch (SnmpTimeoutException)
			{
				throw new SnmpException(string.Concat("Timed out retrieving ", oid));
			}
			return str;
		}

		internal virtual Snmp GetSnmpImpl(string getCommunityName, string setCommunityName, SettingType type)
		{
			return new Snmp(this.CommunityNameGet, this.CommunityNameSet, type);
		}

		private void Init(string address)
		{
			this.connection = this.GetConnection(address);
			this.InitConnectionAttributes();
		}

		private void InitConnectionAttributes()
		{
			ConnectionAttributes attributes = (new ConnectionAttributeProvider()).GetAttributes(this.connection);
			attributes.snmpGetCommunityName = this.CommunityNameGet;
			attributes.snmpSetCommunityName = this.CommunityNameSet;
		}

		/// <summary>
		///       Sets the value of the specified <c>oid</c> to <c>valueToSet</c>.
		///       </summary>
		/// <param name="oid">Object identifier.</param>
		/// <param name="valueToSet">The value to set the OID to.</param>
		/// <exception cref="T:Zebra.Sdk.Printer.SnmpException">If there was an exception communicating over SNMP</exception>
		public void SetOidValue(string oid, string valueToSet)
		{
			try
			{
				this.GetSnmpImpl(this.CommunityNameGet, this.CommunityNameSet, SettingType.STRING).Set(((IpAddressable)this.connection).Address, oid, valueToSet);
			}
			catch (ZebraIllegalArgumentException zebraIllegalArgumentException)
			{
				throw new SnmpException(zebraIllegalArgumentException.Message);
			}
			catch (ArgumentException)
			{
				throw new SnmpException(string.Concat("oid ", oid, " not found."));
			}
			catch (SnmpTimeoutException)
			{
				throw new SnmpException(string.Concat("Timed out setting ", oid));
			}
		}

		/// <summary>
		///       Sets the value of the specified <c>oid</c> to <c>valueToSet</c>.
		///       </summary>
		/// <param name="oid">Object identifier.</param>
		/// <param name="valueToSet">The value to set the OID to.</param>
		/// <exception cref="T:Zebra.Sdk.Printer.SnmpException">If there was an exception communicating over SNMP</exception>
		public void SetOidValue(string oid, int valueToSet)
		{
			try
			{
				string str = Convert.ToString(valueToSet);
				this.GetSnmpImpl(this.CommunityNameGet, this.CommunityNameSet, SettingType.INTEGER).Set(((IpAddressable)this.connection).Address, oid, str);
			}
			catch (ZebraIllegalArgumentException zebraIllegalArgumentException)
			{
				throw new SnmpException(zebraIllegalArgumentException.Message);
			}
			catch (ArgumentException)
			{
				throw new SnmpException(string.Concat("oid ", oid, " not found."));
			}
			catch (SnmpTimeoutException)
			{
				throw new SnmpException(string.Concat("Timed out setting ", oid));
			}
		}
	}
}