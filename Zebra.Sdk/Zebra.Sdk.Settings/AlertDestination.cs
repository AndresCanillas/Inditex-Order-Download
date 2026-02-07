using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Zebra.Sdk.Device;

namespace Zebra.Sdk.Settings
{
	/// <summary>
	///       Enumeration of the various alert destinations which can be set on Zebra Printers.
	///       </summary>
	[JsonObject]
	public class AlertDestination
	{
		/// <summary>
		///       Alert Destination 'Serial'
		///       </summary>
		public static AlertDestination SERIAL;

		/// <summary>
		///       Alert Destination 'Parallel'
		///       </summary>
		public static AlertDestination PARALLEL;

		/// <summary>
		///       Alert Destination 'E-Mail'
		///       </summary>
		public static AlertDestination EMAIL;

		/// <summary>
		///       Alert Destination 'TCP'
		///       </summary>
		public static AlertDestination TCP;

		/// <summary>
		///       Alert Destination 'UDP'
		///       </summary>
		public static AlertDestination UDP;

		/// <summary>
		///       Alert Destination 'SNMP'
		///       </summary>
		public static AlertDestination SNMP;

		/// <summary>
		///       Alert Destination 'USB'
		///       </summary>
		public static AlertDestination USB;

		/// <summary>
		///       Alert Destination 'HTTP-POST'
		///       </summary>
		public static AlertDestination HTTP;

		/// <summary>
		///       Alert Destination 'Bluetooth'
		///       </summary>
		public static AlertDestination BLUETOOTH;

		/// <summary>
		///       Alert Destination 'SDK'
		///       </summary>
		public static AlertDestination SDK;

		private string name;

		private string destination;

		private static List<AlertDestination> allVals;

		/// <summary>
		///       Returns the alert destination as a string.
		///       </summary>
		[JsonIgnore]
		public string DestinationAsSGDString
		{
			get
			{
				return this.destination.ToUpper();
			}
		}

		/// <summary>
		///       Gets/Sets the alert destination name.
		///       </summary>
		[JsonProperty("destination")]
		public string DestinationName
		{
			get
			{
				return this.name;
			}
			set
			{
				this.name = value;
				this.destination = AlertDestination.CreateAlertDestinationFromName(this.name).destination;
			}
		}

		static AlertDestination()
		{
			AlertDestination.SERIAL = new AlertDestination("SERIAL");
			AlertDestination.PARALLEL = new AlertDestination("PARALLEL");
			AlertDestination.EMAIL = new AlertDestination("EMAIL", "E-MAIL");
			AlertDestination.TCP = new AlertDestination("TCP");
			AlertDestination.UDP = new AlertDestination("UDP");
			AlertDestination.SNMP = new AlertDestination("SNMP");
			AlertDestination.USB = new AlertDestination("USB");
			AlertDestination.HTTP = new AlertDestination("HTTP", "HTTP-POST");
			AlertDestination.BLUETOOTH = new AlertDestination("BLUETOOTH");
			AlertDestination.SDK = new AlertDestination("SDK");
			AlertDestination.allVals = new List<AlertDestination>()
			{
				AlertDestination.SERIAL,
				AlertDestination.PARALLEL,
				AlertDestination.EMAIL,
				AlertDestination.TCP,
				AlertDestination.UDP,
				AlertDestination.SNMP,
				AlertDestination.USB,
				AlertDestination.HTTP,
				AlertDestination.BLUETOOTH,
				AlertDestination.SDK
			};
		}

		private AlertDestination()
		{
		}

		private AlertDestination(string destination) : this(destination, destination)
		{
		}

		private AlertDestination(string name, string destination)
		{
			this.name = name;
			this.destination = destination;
		}

		/// <summary>
		///       Creates an AlertDestination based on the <c>destination</c>.
		///       </summary>
		/// <param name="destination">Name of one of the values of AlertDestination.</param>
		/// <returns>Based on the <c>destination</c></returns>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If <c>destination</c> is not a valid alert destination.</exception>
		public static AlertDestination CreateAlertDestination(string destination)
		{
			AlertDestination alertDestination;
			List<AlertDestination>.Enumerator enumerator = AlertDestination.allVals.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					AlertDestination current = enumerator.Current;
					if (!current.ToString().Equals(destination))
					{
						continue;
					}
					alertDestination = current;
					return alertDestination;
				}
				throw new ZebraIllegalArgumentException("Invalid alert destination.");
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
		}

		/// <summary>
		///       Creates an AlertDestination based on the <c>destinationName</c>.
		///       <see cref="P:Zebra.Sdk.Settings.AlertDestination.DestinationName" /></summary>
		/// <param name="destinationName">Name of one of the destinations in AlertDestination.</param>
		/// <returns>Based on the <c>destinationName</c></returns>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If <c>destinationName</c> is not a valid alert destination.</exception>
		public static AlertDestination CreateAlertDestinationFromName(string destinationName)
		{
			AlertDestination alertDestination;
			List<AlertDestination>.Enumerator enumerator = AlertDestination.allVals.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					AlertDestination current = enumerator.Current;
					if (!current.DestinationName.Equals(destinationName))
					{
						continue;
					}
					alertDestination = current;
					return alertDestination;
				}
				throw new ZebraIllegalArgumentException("Invalid alert destination.");
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
		}

		/// <summary>Determines whether the specified object is equal to the current object.</summary>
		/// <param name="obj">The object to compare with the current object. </param>
		/// <returns>
		///   <see langword="true" /> if the specified object  is equal to the current object; otherwise, <see langword="false" />.</returns>
		public override bool Equals(object obj)
		{
			if (obj == null || obj.GetType() != this.GetType())
			{
				return false;
			}
			AlertDestination alertDestination = (AlertDestination)obj;
			if (!this.name.Equals(alertDestination.name))
			{
				return false;
			}
			return this.destination.Equals(alertDestination.destination);
		}

		/// <summary>Serves as the default hash function. </summary>
		/// <returns>A hash code for the current object.</returns>
		public override int GetHashCode()
		{
			return this.name.GetHashCode() ^ this.destination.GetHashCode();
		}

		/// <summary>
		///       Returns the alert destination.
		///       </summary>
		/// <returns>String representation of the alert destination (e.g. "TCP").</returns>
		public override string ToString()
		{
			return this.destination;
		}
	}
}