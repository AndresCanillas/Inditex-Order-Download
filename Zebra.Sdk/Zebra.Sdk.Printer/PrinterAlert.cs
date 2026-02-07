using Newtonsoft.Json;
using System;
using Zebra.Sdk.Settings;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       Defines a printer alert.
	///       </summary>
	[JsonObject]
	public class PrinterAlert
	{
		private AlertCondition condition;

		private bool onSet;

		private bool onClear;

		private string sgdName;

		protected AlertDestination destination;

		protected string destinationAddress;

		protected int port;

		protected bool quelling;

		protected string alertText;

		/// <summary>
		///       Return the text received from the printer.
		///       </summary>
		[JsonProperty("alertText")]
		public string AlertText
		{
			get
			{
				return this.alertText;
			}
			set
			{
				this.alertText = value;
			}
		}

		/// <summary>
		///       Gets the current alert condition.
		///       </summary>
		[JsonIgnore]
		public AlertCondition Condition
		{
			get
			{
				return this.condition;
			}
		}

		/// <summary>
		///       Return the <c>AlertCondition</c> of the alert.
		///       </summary>
		[JsonProperty("condition")]
		public string ConditionName
		{
			get
			{
				return this.condition.ConditionName;
			}
			set
			{
				this.condition = AlertCondition.CreateAlertConditionFromName(value);
			}
		}

		/// <summary>
		///       Return the <c>AlertDestination</c> used by the alert.
		///       </summary>
		[JsonIgnore]
		public AlertDestination Destination
		{
			get
			{
				return this.destination;
			}
		}

		/// <summary>
		///       Return the destination where the alert should be sent, for example, an IP Address or an email address, depending 
		///       on the value of getDestinationAsSGDString.
		///       </summary>
		[JsonProperty("destinationAddress")]
		public string DestinationAddress
		{
			get
			{
				return this.destinationAddress;
			}
			set
			{
				this.destinationAddress = value;
			}
		}

		/// <summary>
		///       Return the <c>string</c> representation of the alert destination for Set-Get-Do.
		///       </summary>
		[JsonIgnore]
		public string DestinationAsSgdString
		{
			get
			{
				return this.destination.DestinationAsSGDString;
			}
		}

		[JsonProperty("destination")]
		private string DestinationName
		{
			get
			{
				return this.destination.DestinationName;
			}
			set
			{
				this.destination = AlertDestination.CreateAlertDestinationFromName(value);
			}
		}

		/// <summary>
		///       Return alert will be triggered on 'clear'.
		///       </summary>
		[JsonProperty("onClear")]
		public bool OnClear
		{
			get
			{
				return this.onClear;
			}
			set
			{
				this.onClear = value;
			}
		}

		/// <summary>
		///       Return true if the alert is fired when 'set'.
		///       </summary>
		[JsonProperty("onSet")]
		public bool OnSet
		{
			get
			{
				return this.onSet;
			}
			set
			{
				this.onSet = value;
			}
		}

		/// <summary>
		///       Return the destination port number where the alert should be sent.
		///       </summary>
		[JsonProperty("port")]
		public int Port
		{
			get
			{
				return this.port;
			}
			set
			{
				this.port = value;
			}
		}

		/// <summary>
		///       Return the quelling state of the alert.
		///       </summary>
		[JsonProperty("quelling")]
		public bool Quelling
		{
			get
			{
				return this.quelling;
			}
			set
			{
				this.quelling = value;
			}
		}

		/// <summary>
		///       Return the <c>string</c> representation of the Set-Get-Do Name.
		///       </summary>
		[JsonProperty("sgdName")]
		public string SgdName
		{
			get
			{
				return this.sgdName;
			}
			set
			{
				this.sgdName = value;
			}
		}

		private PrinterAlert()
		{
		}

		/// <summary>
		///       Creates an instance of a PrinterAlert object.
		///       </summary>
		/// <param name="condition">The printer condition that will trigger the alert.</param>
		/// <param name="destination">The destination that the alert will be sent to.</param>
		/// <param name="onSet">If true, the alert will be triggered when the condition occurs.</param>
		/// <param name="onClear">If true, the alert will be triggered when the condition is cleared.</param>
		/// <param name="destinationAddress">The destination address, if the destination requires one.</param>
		/// <param name="port">The destination port, if the destination requires one.</param>
		/// <param name="quelling">If true, the alert is quelled.</param>
		public PrinterAlert(AlertCondition condition, AlertDestination destination, bool onSet, bool onClear, string destinationAddress, int port, bool quelling) : this(condition, destination, "", onSet, onClear, destinationAddress, port, quelling, "")
		{
		}

		/// <summary>
		///       Creates an instance of a PrinterAlert object, including a Set-Get-Do name.
		///       </summary>
		/// <param name="condition">The printer condition that will trigger the alert.</param>
		/// <param name="destination">The destination that the alert will be sent to.</param>
		/// <param name="sgdName">If <c>condition</c> is <c>SGD_SET</c>, the name of the Set-Get-Do to be monitored.</param>
		/// <param name="onSet">If true, the alert will be triggered when the condition occurs.</param>
		/// <param name="onClear">If true, the alert will be triggered when the condition is cleared.</param>
		/// <param name="destinationAddress">The destination address, if the destination requires one.</param>
		/// <param name="port">The destination port, if the destination requires one.</param>
		/// <param name="quelling">If true, the alert is quelled.</param>
		public PrinterAlert(AlertCondition condition, AlertDestination destination, string sgdName, bool onSet, bool onClear, string destinationAddress, int port, bool quelling) : this(condition, destination, sgdName, onSet, onClear, destinationAddress, port, quelling, "")
		{
		}

		/// <summary>
		///       Creates an instance of a PrinterAlert object, including the printer alert text.
		///       </summary>
		/// <param name="condition">The printer condition that will trigger the alert.</param>
		/// <param name="destination">The destination that the alert will be sent to.</param>
		/// <param name="onSet">If true, the alert will be triggered when the condition occurs.</param>
		/// <param name="onClear">If true, the alert will be triggered when the condition is cleared.</param>
		/// <param name="destinationAddress">The destination address, if the destination requires one.</param>
		/// <param name="port">The destination port, if the destination requires one.</param>
		/// <param name="quelling">If true, the alert is quelled.</param>
		/// <param name="alertText">The text received from the printer.</param>
		public PrinterAlert(AlertCondition condition, AlertDestination destination, bool onSet, bool onClear, string destinationAddress, int port, bool quelling, string alertText) : this(condition, destination, "", onSet, onClear, destinationAddress, port, quelling, alertText)
		{
		}

		/// <summary>
		///       Creates an instance of a PrinterAlert object, including a Set-Get-Do name and the printer alert text.
		///       </summary>
		/// <param name="condition">The printer condition that will trigger the alert.</param>
		/// <param name="destination">The destination that the alert will be sent to.</param>
		/// <param name="sgdName">If <c>condition</c> is <c>SGD_SET</c>, the name of the Set-Get-Do to be monitored.</param>
		/// <param name="onSet">If true, the alert will be triggered when the condition occurs.</param>
		/// <param name="onClear">If true, the alert will be triggered when the condition is cleared.</param>
		/// <param name="destinationAddress">The destination address, if the destination requires one.</param>
		/// <param name="port">The destination port, if the destination requires one.</param>
		/// <param name="quelling">If true, the alert is quelled.</param>
		/// <param name="alertText">The text received from the printer.</param>
		public PrinterAlert(AlertCondition condition, AlertDestination destination, string sgdName, bool onSet, bool onClear, string destinationAddress, int port, bool quelling, string alertText)
		{
			this.condition = condition;
			this.destination = destination;
			this.sgdName = sgdName;
			this.onSet = onSet;
			this.onClear = onClear;
			this.destinationAddress = destinationAddress;
			this.port = port;
			this.quelling = quelling;
			this.alertText = alertText;
		}
	}
}