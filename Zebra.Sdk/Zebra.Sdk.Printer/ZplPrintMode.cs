using System;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       Enumeration of the various print modes supported by Zebra Printers.
	///       </summary>
	public class ZplPrintMode
	{
		/// <summary>
		///       Rewind print mode
		///       </summary>
		public static ZplPrintMode REWIND;

		/// <summary>
		///       Peel-off print mode
		///       </summary>
		public static ZplPrintMode PEEL_OFF;

		/// <summary>
		///       Tear-off print mode (this also implies Linerless Tear print mode)
		///       </summary>
		public static ZplPrintMode TEAR_OFF;

		/// <summary>
		///       Cutter print mode
		///       </summary>
		public static ZplPrintMode CUTTER;

		/// <summary>
		///       Applicator print mode
		///       </summary>
		public static ZplPrintMode APPLICATOR;

		/// <summary>
		///       Delayed cut print mode
		///       </summary>
		public static ZplPrintMode DELAYED_CUT;

		/// <summary>
		///       Linerless peel print mode
		///       </summary>
		public static ZplPrintMode LINERLESS_PEEL;

		/// <summary>
		///       Linerless rewind print mode
		///       </summary>
		public static ZplPrintMode LINERLESS_REWIND;

		/// <summary>
		///       Partial cutter print mode
		///       </summary>
		public static ZplPrintMode PARTIAL_CUTTER;

		/// <summary>
		///       RFID print mode
		///       </summary>
		public static ZplPrintMode RFID;

		/// <summary>
		///       Kiosk print mode
		///       </summary>
		public static ZplPrintMode KIOSK;

		/// <summary>
		///       Unknown print mode
		///       </summary>
		public static ZplPrintMode UNKNOWN;

		private string description;

		static ZplPrintMode()
		{
			ZplPrintMode.REWIND = new ZplPrintMode("Rewind");
			ZplPrintMode.PEEL_OFF = new ZplPrintMode("Peel-Off");
			ZplPrintMode.TEAR_OFF = new ZplPrintMode("Tear-Off");
			ZplPrintMode.CUTTER = new ZplPrintMode("Cutter");
			ZplPrintMode.APPLICATOR = new ZplPrintMode("Applicator");
			ZplPrintMode.DELAYED_CUT = new ZplPrintMode("Delayed Cut");
			ZplPrintMode.LINERLESS_PEEL = new ZplPrintMode("Linerless Peel");
			ZplPrintMode.LINERLESS_REWIND = new ZplPrintMode("Linerless Rewind");
			ZplPrintMode.PARTIAL_CUTTER = new ZplPrintMode("Partial Cutter");
			ZplPrintMode.RFID = new ZplPrintMode("RFID");
			ZplPrintMode.KIOSK = new ZplPrintMode("Kiosk");
			ZplPrintMode.UNKNOWN = new ZplPrintMode("Unknown");
		}

		private ZplPrintMode(string description)
		{
			this.description = description;
		}

		/// <summary>
		///       Returns the print mode.
		///       </summary>
		/// <returns>String representation of the print mode (e.g. "Rewind").</returns>
		public override string ToString()
		{
			return this.description;
		}
	}
}