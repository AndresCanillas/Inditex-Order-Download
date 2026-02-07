using System;
using System.Collections.Generic;
using Zebra.Sdk.Comm;

namespace Zebra.Sdk.Printer.Discovery
{
	/// <summary>
	///       Container holding information about a discovered printer.
	///       </summary>
	public abstract class DiscoveredPrinter
	{
		protected readonly string address;

		protected Dictionary<string, string> discoSettings;

		/// <summary>
		///       MAC address, IP Address, or local name of printer.
		///       </summary>
		public string Address
		{
			get
			{
				return this.address;
			}
		}

		/// <summary>
		///       Returna a <c>Dictionary</c> of all settings obtained via the chosen discovery method.
		///       </summary>
		/// <returns>
		///   <see cref="T:System.Collections.Generic.Dictionary`2" /> containing available attributes of the discovered printer.</returns>
		public Dictionary<string, string> DiscoveryDataMap
		{
			get
			{
				return this.discoSettings;
			}
		}

		/// <summary>
		///       Creates an object holding information about a discovered printer.
		///       </summary>
		/// <param name="address">MAC address, IP Address, or local name of printer.</param>
		public DiscoveredPrinter(string address)
		{
			this.address = address;
			this.discoSettings = new Dictionary<string, string>();
		}

		/// <summary>
		///       Returns true if two discovered printer objects have the same address, otherwise it returns false.
		///       </summary>
		/// <param name="o">DiscoveredPrinter object to compare against.</param>
		/// <returns>true if equal</returns>
		public override bool Equals(object o)
		{
			DiscoveredPrinter discoveredPrinter = o as DiscoveredPrinter;
			DiscoveredPrinter discoveredPrinter1 = discoveredPrinter;
			if (discoveredPrinter == null)
			{
				return false;
			}
			return discoveredPrinter1.address.Equals(this.address);
		}

		/// <summary>
		///       Creates a connection based on the information in the DiscoveredPrinter response.
		///       </summary>
		/// <returns>a <c>Connection</c> to the discovered printer</returns>
		public abstract Connection GetConnection();

		/// <summary>
		///       Returns a hash code for this DiscoveredPrinter.
		///       </summary>
		/// <returns>The hash code</returns>
		public override int GetHashCode()
		{
			return string.Concat("DiscoveredPrinter", this.address).GetHashCode();
		}

		/// <summary>
		///       For TCP, this returns the IP Address. For driver, this returns the local printer name.
		///       </summary>
		public override string ToString()
		{
			return this.address;
		}
	}
}