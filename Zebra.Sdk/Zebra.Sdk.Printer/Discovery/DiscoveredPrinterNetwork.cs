using System;
using System.Collections.Generic;
using Zebra.Sdk.Comm;

namespace Zebra.Sdk.Printer.Discovery
{
	/// <summary>
	///       Instance of <see cref="T:Zebra.Sdk.Printer.Discovery.DiscoveredPrinter" /> that is returned when performing a network discovery.
	///       </summary>
	public class DiscoveredPrinterNetwork : DiscoveredPrinter
	{
		/// <summary>
		///       Returns an instance of a <c>DiscoveredPrinterNetwork</c> with <c>address</c> and <c>port</c>.
		///       </summary>
		/// <param name="address">The address of the discovered network printer</param>
		/// <param name="port">The active raw port of the discovered network printer</param>
		public DiscoveredPrinterNetwork(string address, int port) : base(address)
		{
			this.discoSettings.Add("ADDRESS", address);
			this.discoSettings.Add("PORT_NUMBER", Convert.ToString(port));
		}

		/// <summary>
		///       Returns an instance of a <c>DiscoveredPrinterNetwork</c> built using the provided attributes.
		///       </summary>
		/// <param name="attributes">A map of attributes associated with the discovered network printer</param>
		public DiscoveredPrinterNetwork(Dictionary<string, string> attributes) : base((attributes.ContainsKey("ADDRESS") ? attributes["ADDRESS"] : ""))
		{
			this.discoSettings = attributes;
		}

		/// <summary>
		///       Returns true if two discovered printer objects have the same address, otherwise it returns false.
		///       </summary>
		/// <param name="o">DiscoveredPrinter object to compare against.</param>
		/// <returns>true if equal</returns>
		public override bool Equals(object o)
		{
			if (!(o is DiscoveredPrinterNetwork))
			{
				return false;
			}
			if (o == this)
			{
				return true;
			}
			DiscoveredPrinterNetwork discoveredPrinterNetwork = (DiscoveredPrinterNetwork)o;
			string empty = string.Empty;
			if (this.discoSettings.ContainsKey("SERIAL_NUMBER"))
			{
				empty = this.discoSettings["SERIAL_NUMBER"];
			}
			if (string.IsNullOrEmpty(empty))
			{
				empty = "UNKNOWN";
			}
			string item = string.Empty;
			if (discoveredPrinterNetwork.DiscoveryDataMap.ContainsKey("SERIAL_NUMBER"))
			{
				item = discoveredPrinterNetwork.DiscoveryDataMap["SERIAL_NUMBER"];
			}
			if (string.IsNullOrEmpty(item))
			{
				item = "UNKNOWN";
			}
			return (new DiscoveredPrinterNetwork.EqualsBuilder()).Append(empty.Trim(), item.Trim()).Append(base.Address, discoveredPrinterNetwork.Address).IsEqual();
		}

		/// <summary>
		///       Creates a connection based on the information in the DiscoveredPrinter response.
		///       </summary>
		/// <returns>a <c>Connection</c> to the discovered printer</returns>
		public override Connection GetConnection()
		{
			Connection tcpConnection;
			string item;
			string str = this.discoSettings["ADDRESS"];
			string item1 = this.discoSettings["PORT_NUMBER"];
			if (this.discoSettings.ContainsKey("JSON_PORT_NUMBER"))
			{
				item = this.discoSettings["JSON_PORT_NUMBER"];
			}
			else
			{
				item = null;
			}
			string str1 = item;
			if (str1 == null || str1.Length == 0 || int.Parse(str1) == 0)
			{
				tcpConnection = new TcpConnection(str, int.Parse(item1));
			}
			else
			{
				tcpConnection = new MultichannelTcpConnection(str, int.Parse(item1), int.Parse(str1));
			}
			return tcpConnection;
		}

		/// <summary>
		///       Returns a hash code for this DiscoveredPrinter.
		///       </summary>
		public override int GetHashCode()
		{
			string empty = string.Empty;
			if (this.discoSettings.ContainsKey("SERIAL_NUMBER"))
			{
				empty = this.discoSettings["SERIAL_NUMBER"];
			}
			if (string.IsNullOrEmpty(empty))
			{
				empty = "UNKNOWN";
			}
			return (new DiscoveredPrinterNetwork.HashCodeBuilder(47, 3)).Append(base.Address).Append(empty.Trim()).ToHashCode();
		}

		private class EqualsBuilder
		{
			private bool isEqual;

			public EqualsBuilder()
			{
			}

			public DiscoveredPrinterNetwork.EqualsBuilder Append(string lhs, string rhs)
			{
				if (!this.isEqual)
				{
					return this;
				}
				if (lhs == rhs)
				{
					return this;
				}
				if (lhs == null || rhs == null)
				{
					this.SetEquals(false);
					return this;
				}
				this.isEqual = lhs.Equals(rhs);
				return this;
			}

			public bool IsEqual()
			{
				return this.isEqual;
			}

			protected void SetEquals(bool isEqual)
			{
				this.isEqual = isEqual;
			}
		}

		private class HashCodeBuilder
		{
			private int constant;

			private int total;

			public HashCodeBuilder(int oddNumber, int multiplier)
			{
				if (oddNumber % 2 == 0)
				{
					throw new ArgumentException("Initial odd value required");
				}
				if (multiplier % 2 == 0)
				{
					throw new ArgumentException("Odd multiplier required");
				}
				this.total = oddNumber;
				this.constant = multiplier;
			}

			public DiscoveredPrinterNetwork.HashCodeBuilder Append(string value)
			{
				if (!string.IsNullOrEmpty(value))
				{
					this.total = this.total * this.constant + value.GetHashCode();
				}
				else
				{
					this.total *= this.constant;
				}
				return this;
			}

			public int ToHashCode()
			{
				return this.total;
			}
		}
	}
}