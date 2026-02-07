using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Zebra.Sdk.Device;

namespace Zebra.Sdk.Settings
{
	/// <summary>
	///       Enumeration of the various printer alert conditions which can be set on Zebra Printers.
	///       </summary>
	[JsonObject]
	public class AlertCondition
	{
		/// <summary>
		///       Alert condition 'None'
		///       </summary>
		public static AlertCondition NONE;

		/// <summary>
		///       Alert condition 'Paper Out'
		///       </summary>
		public static AlertCondition PAPER_OUT;

		/// <summary>
		///       Alert condition 'Ribbon Out'
		///       </summary>
		public static AlertCondition RIBBON_OUT;

		/// <summary>
		///       Alert condition 'Head Too Hot'
		///       </summary>
		public static AlertCondition HEAD_TOO_HOT;

		/// <summary>
		///       Alert condition 'Head Cold'
		///       </summary>
		public static AlertCondition HEAD_COLD;

		/// <summary>
		///       Alert condition 'Head Open'
		///       </summary>
		public static AlertCondition HEAD_OPEN;

		/// <summary>
		///       Alert condition 'Power Supply Too Hot'
		///       </summary>
		public static AlertCondition POWER_SUPPLY_OVER_TEMP;

		/// <summary>
		///       Alert condition 'Ribbon In'
		///       </summary>
		public static AlertCondition RIBBON_IN_WARNING;

		/// <summary>
		///       Alert condition 'Rewind'
		///       </summary>
		public static AlertCondition REWIND_FULL;

		/// <summary>
		///       Alert condition 'Cutter Jammed'
		///       </summary>
		public static AlertCondition CUTTER_JAMMED;

		/// <summary>
		///       Alert condition 'Printer Paused'
		///       </summary>
		public static AlertCondition PRINTER_PAUSED;

		/// <summary>
		///       Alert condition 'PQ Job Completed'
		///       </summary>
		public static AlertCondition PQ_JOB_COMPLETED;

		/// <summary>
		///       Alert condition 'Label Ready'
		///       </summary>
		public static AlertCondition LABEL_READY;

		/// <summary>
		///       Alert condition 'Head Element Bad'
		///       </summary>
		public static AlertCondition HEAD_ELEMENT_BAD;

		/// <summary>
		///       Alert condition 'Basic Runtime'
		///       </summary>
		public static AlertCondition ZBI_BASIC_RUNTIME_ERROR;

		/// <summary>
		///       Alert condition 'Basic Forced'
		///       </summary>
		public static AlertCondition ZBI_BASIC_FORCED_ERROR;

		/// <summary>
		///       Alert condition 'Power On'
		///       </summary>
		public static AlertCondition POWER_ON;

		/// <summary>
		///       Alert condition 'Clean Printhead'
		///       </summary>
		public static AlertCondition CLEAN_PRINTHEAD;

		/// <summary>
		///       Alert condition 'Media Low'
		///       </summary>
		public static AlertCondition MEDIA_LOW;

		/// <summary>
		///       Alert condition 'Ribbon Low'
		///       </summary>
		public static AlertCondition RIBBON_LOW;

		/// <summary>
		///       Alert condition 'Replace Head'
		///       </summary>
		public static AlertCondition REPLACE_HEAD;

		/// <summary>
		///       Alert condition 'Battery Low'
		///       </summary>
		public static AlertCondition BATTERY_LOW;

		/// <summary>
		///       Alert condition 'RFID Error'
		///       </summary>
		public static AlertCondition RFID_ERROR;

		/// <summary>
		///       Alert condition 'All Messages'
		///       </summary>
		public static AlertCondition ALL_MESSAGES;

		/// <summary>
		///       Alert condition 'Cold Start'
		///       </summary>
		public static AlertCondition COLD_START;

		/// <summary>
		///       Alert condition 'SGD Set'
		///       </summary>
		public static AlertCondition SGD_SET;

		/// <summary>
		///       Alert condition 'Motor Overtemp'
		///       </summary>
		public static AlertCondition MOTOR_OVERTEMP;

		/// <summary>
		///       Alert condition 'Printhead Shutdown'
		///       </summary>
		public static AlertCondition PRINTHEAD_SHUTDOWN;

		/// <summary>
		///       Alert condition 'Shutting Down'
		///       </summary>
		public static AlertCondition SHUTTING_DOWN;

		/// <summary>
		///       Alert condition 'Restarting'
		///       </summary>
		public static AlertCondition RESTARTING;

		/// <summary>
		///       Alert condition 'No Reader Present'
		///       </summary>
		public static AlertCondition NO_READER_PRESENT;

		/// <summary>
		///       Alert condition 'Thermistor Fault'
		///       </summary>
		public static AlertCondition THERMISTOR_FAULT;

		/// <summary>
		///       Alert condition 'Invalid Head'
		///       </summary>
		public static AlertCondition INVALID_HEAD;

		/// <summary>
		///       Alert condition 'Country Code Error'
		///       </summary>
		public static AlertCondition COUNTRY_CODE_ERROR;

		/// <summary>
		///       Alert condition 'MCR Result Ready'
		///       </summary>
		public static AlertCondition MCR_RESULT_READY;

		/// <summary>
		///       Alert condition 'PMCU Download'
		///       </summary>
		public static AlertCondition PMCU_DOWNLOAD;

		/// <summary>
		///       Alert condition 'Media Cartridge'
		///       </summary>
		public static AlertCondition MEDIA_CARTRIDGE;

		/// <summary>
		///       Alert condition 'Media Cartridge Load Failure'
		///       </summary>
		public static AlertCondition MEDIA_CARTRIDGE_LOAD_FAILURE;

		/// <summary>
		///       Alert condition 'Media Cartridge Eject Failure'
		///       </summary>
		public static AlertCondition MEDIA_CARTRIDGE_EJECT_FAILURE;

		/// <summary>
		///       Alert condition 'Media Cartridge Forced Eject'
		///       </summary>
		public static AlertCondition MEDIA_CARTRIDGE_FORCED_EJECT;

		/// <summary>
		///       Alert condition 'Cleaning Mode'
		///       </summary>
		public static AlertCondition CLEANING_MODE;

		private static List<AlertCondition> allVals;

		private string conditionType;

		private string name;

		/// <summary>
		///       Gets/sets the alert condition name
		///       </summary>
		[JsonProperty("condition")]
		public string ConditionName
		{
			get
			{
				return this.name;
			}
			set
			{
				this.name = value;
				this.conditionType = AlertCondition.CreateAlertConditionFromName(this.name).conditionType;
			}
		}

		static AlertCondition()
		{
			AlertCondition.NONE = new AlertCondition("NONE", "NONE");
			AlertCondition.PAPER_OUT = new AlertCondition("PAPER_OUT", "PAPER OUT");
			AlertCondition.RIBBON_OUT = new AlertCondition("RIBBON_OUT", "RIBBON OUT");
			AlertCondition.HEAD_TOO_HOT = new AlertCondition("HEAD_TOO_HOT", "HEAD TOO HOT");
			AlertCondition.HEAD_COLD = new AlertCondition("HEAD_COLD", "HEAD COLD");
			AlertCondition.HEAD_OPEN = new AlertCondition("HEAD_OPEN", "HEAD OPEN");
			AlertCondition.POWER_SUPPLY_OVER_TEMP = new AlertCondition("POWER_SUPPLY_OVER_TEMP", "SUPPLY TOO HOT");
			AlertCondition.RIBBON_IN_WARNING = new AlertCondition("RIBBON_IN_WARNING", "RIBBON IN");
			AlertCondition.REWIND_FULL = new AlertCondition("REWIND_FULL", "REWIND");
			AlertCondition.CUTTER_JAMMED = new AlertCondition("CUTTER_JAMMED", "CUTTER JAMMED");
			AlertCondition.PRINTER_PAUSED = new AlertCondition("PRINTER_PAUSED", "PRINTER PAUSED");
			AlertCondition.PQ_JOB_COMPLETED = new AlertCondition("PQ_JOB_COMPLETED", "PQ JOB COMPLETED");
			AlertCondition.LABEL_READY = new AlertCondition("LABEL_READY", "LABEL READY");
			AlertCondition.HEAD_ELEMENT_BAD = new AlertCondition("HEAD_ELEMENT_BAD", "HEAD ELEMENT BAD");
			AlertCondition.ZBI_BASIC_RUNTIME_ERROR = new AlertCondition("ZBI_BASIC_RUNTIME_ERROR", "BASIC RUNTIME");
			AlertCondition.ZBI_BASIC_FORCED_ERROR = new AlertCondition("ZBI_BASIC_FORCED_ERROR", "BASIC FORCED");
			AlertCondition.POWER_ON = new AlertCondition("POWER_ON", "POWER ON");
			AlertCondition.CLEAN_PRINTHEAD = new AlertCondition("CLEAN_PRINTHEAD", "CLEAN PRINTHEAD");
			AlertCondition.MEDIA_LOW = new AlertCondition("MEDIA_LOW", "MEDIA LOW");
			AlertCondition.RIBBON_LOW = new AlertCondition("RIBBON_LOW", "RIBBON LOW");
			AlertCondition.REPLACE_HEAD = new AlertCondition("REPLACE_HEAD", "REPLACE HEAD");
			AlertCondition.BATTERY_LOW = new AlertCondition("BATTERY_LOW", "BATTERY LOW");
			AlertCondition.RFID_ERROR = new AlertCondition("RFID_ERROR", "RFID ERROR");
			AlertCondition.ALL_MESSAGES = new AlertCondition("ALL_MESSAGES", "ALL MESSAGES");
			AlertCondition.COLD_START = new AlertCondition("COLD_START", "COLD START");
			AlertCondition.SGD_SET = new AlertCondition("SGD_SET", "SGD SET");
			AlertCondition.MOTOR_OVERTEMP = new AlertCondition("MOTOR_OVERTEMP", "MOTOR OVERTEMP");
			AlertCondition.PRINTHEAD_SHUTDOWN = new AlertCondition("PRINTHEAD_SHUTDOWN", "PRINTHEAD SHUTDOWN");
			AlertCondition.SHUTTING_DOWN = new AlertCondition("SHUTTING_DOWN", "SHUTTING DOWN");
			AlertCondition.RESTARTING = new AlertCondition("RESTARTING", "RESTARTING");
			AlertCondition.NO_READER_PRESENT = new AlertCondition("NO_READER_PRESENT", "NO READER PRESENT");
			AlertCondition.THERMISTOR_FAULT = new AlertCondition("THERMISTOR_FAULT", "THERMISTOR FAULT");
			AlertCondition.INVALID_HEAD = new AlertCondition("INVALID_HEAD", "INVALID HEAD");
			AlertCondition.COUNTRY_CODE_ERROR = new AlertCondition("COUNTRY_CODE_ERROR", "COUNTRY CODE ERROR");
			AlertCondition.MCR_RESULT_READY = new AlertCondition("MCR_RESULT_READY", "MCR RESULT READY");
			AlertCondition.PMCU_DOWNLOAD = new AlertCondition("PMCU_DOWNLOAD", "PMCU DOWNLOAD");
			AlertCondition.MEDIA_CARTRIDGE = new AlertCondition("MEDIA_CARTRIDGE", "MEDIA CARTRIDGE");
			AlertCondition.MEDIA_CARTRIDGE_LOAD_FAILURE = new AlertCondition("MEDIA_CARTRIDGE_LOAD_FAILURE", "MEDIA CARTRIDGE LOAD FAILURE");
			AlertCondition.MEDIA_CARTRIDGE_EJECT_FAILURE = new AlertCondition("MEDIA_CARTRIDGE_EJECT_FAILURE", "MEDIA CARTRIDGE EJECT FAILURE");
			AlertCondition.MEDIA_CARTRIDGE_FORCED_EJECT = new AlertCondition("MEDIA_CARTRIDGE_FORCED_EJECT", "MEDIA CARTRIDGE FORCED EJECT");
			AlertCondition.CLEANING_MODE = new AlertCondition("CLEANING_MODE", "CLEANING MODE");
			AlertCondition.allVals = new List<AlertCondition>()
			{
				AlertCondition.NONE,
				AlertCondition.PAPER_OUT,
				AlertCondition.RIBBON_OUT,
				AlertCondition.HEAD_TOO_HOT,
				AlertCondition.HEAD_COLD,
				AlertCondition.HEAD_OPEN,
				AlertCondition.POWER_SUPPLY_OVER_TEMP,
				AlertCondition.RIBBON_IN_WARNING,
				AlertCondition.REWIND_FULL,
				AlertCondition.CUTTER_JAMMED,
				AlertCondition.PRINTER_PAUSED,
				AlertCondition.PQ_JOB_COMPLETED,
				AlertCondition.LABEL_READY,
				AlertCondition.HEAD_ELEMENT_BAD,
				AlertCondition.ZBI_BASIC_RUNTIME_ERROR,
				AlertCondition.ZBI_BASIC_FORCED_ERROR,
				AlertCondition.POWER_ON,
				AlertCondition.CLEAN_PRINTHEAD,
				AlertCondition.MEDIA_LOW,
				AlertCondition.RIBBON_LOW,
				AlertCondition.REPLACE_HEAD,
				AlertCondition.BATTERY_LOW,
				AlertCondition.RFID_ERROR,
				AlertCondition.ALL_MESSAGES,
				AlertCondition.COLD_START,
				AlertCondition.SGD_SET,
				AlertCondition.MOTOR_OVERTEMP,
				AlertCondition.PRINTHEAD_SHUTDOWN,
				AlertCondition.SHUTTING_DOWN,
				AlertCondition.RESTARTING,
				AlertCondition.NO_READER_PRESENT,
				AlertCondition.THERMISTOR_FAULT,
				AlertCondition.INVALID_HEAD,
				AlertCondition.COUNTRY_CODE_ERROR,
				AlertCondition.MCR_RESULT_READY,
				AlertCondition.PMCU_DOWNLOAD
			};
		}

		private AlertCondition()
		{
		}

		private AlertCondition(string hzaConditionName, string hzaConditionType)
		{
			this.name = hzaConditionName;
			this.conditionType = hzaConditionType;
		}

		/// <summary>
		///       Creates an AlertCondition based on the <c>condition</c>.
		///       </summary>
		/// <param name="condition">Name of one of the values of AlertCondition.</param>
		/// <returns>Based on the string <c>condition</c></returns>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If <c>condition</c> is not a valid alert condition.</exception>
		public static AlertCondition CreateAlertCondition(string condition)
		{
			AlertCondition alertCondition;
			List<AlertCondition>.Enumerator enumerator = AlertCondition.allVals.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					AlertCondition current = enumerator.Current;
					if (!current.ToString().Equals(condition))
					{
						continue;
					}
					alertCondition = current;
					return alertCondition;
				}
				throw new ZebraIllegalArgumentException("Invalid alert condition.");
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
		}

		/// <summary>
		///       Creates an AlertCondition based on the <c>conditionName</c>.
		///       </summary>
		/// <param name="conditionName">Name of one of the conditions in AlertCondition.</param>
		/// <returns>Based on the string <c>conditionName</c></returns>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If <c>conditionName</c> is not a valid alert condition.</exception>
		public static AlertCondition CreateAlertConditionFromName(string conditionName)
		{
			AlertCondition alertCondition;
			List<AlertCondition>.Enumerator enumerator = AlertCondition.allVals.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					AlertCondition current = enumerator.Current;
					if (!current.ConditionName.Equals(conditionName))
					{
						continue;
					}
					alertCondition = current;
					return alertCondition;
				}
				throw new ZebraIllegalArgumentException("Invalid alert condition.");
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
			AlertCondition alertCondition = (AlertCondition)obj;
			if (!this.name.Equals(alertCondition.name))
			{
				return false;
			}
			return this.conditionType.Equals(alertCondition.conditionType);
		}

		/// <summary>Serves as the default hash function. </summary>
		/// <returns>A hash code for the current object.</returns>
		public override int GetHashCode()
		{
			return this.name.GetHashCode() ^ this.conditionType.GetHashCode();
		}

		/// <summary>
		///       Returns the alert condition.
		///       </summary>
		/// <returns>String representation of the alert condition (e.g. "PAPER OUT").</returns>
		public override string ToString()
		{
			return this.conditionType;
		}
	}
}