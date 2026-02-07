using System;
using System.Text;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       This class is used to acquire a human readable string of the current errors/warnings stored in a
	///       <see cref="T:Zebra.Sdk.Printer.PrinterStatus" /> instance.
	///       </summary>
	public class PrinterStatusMessages
	{
		/// <summary>
		///       Message to indicate the head is open.
		///       </summary>
		public static string HEAD_OPEN_MSG;

		/// <summary>
		///       Message to indicate the head is too hot.
		///       </summary>
		public static string HEAD_TOO_HOT_MSG;

		/// <summary>
		///       Message to indicate the paper is out.
		///       </summary>
		public static string PAPER_OUT_MSG;

		/// <summary>
		///       Message to indicate the ribbon is out.
		///       </summary>
		public static string RIBBON_OUT_MSG;

		/// <summary>
		///       Message to indicate the receive buffer is full.
		///       </summary>
		public static string RECEIVE_BUFFER_FULL_MSG;

		/// <summary>
		///       Message to indicate printer is paused.
		///       </summary>
		public static string PAUSE_MSG;

		/// <summary>
		///       Message to indicate <c>printerStatus</c> is null.
		///       </summary>
		public static string NULL_MSG;

		private PrinterStatus printerStatus;

		static PrinterStatusMessages()
		{
			PrinterStatusMessages.HEAD_OPEN_MSG = "HEAD OPEN";
			PrinterStatusMessages.HEAD_TOO_HOT_MSG = "HEAD TOO HOT";
			PrinterStatusMessages.PAPER_OUT_MSG = "PAPER OUT";
			PrinterStatusMessages.RIBBON_OUT_MSG = "RIBBON OUT";
			PrinterStatusMessages.RECEIVE_BUFFER_FULL_MSG = "RECEIVE BUFFER FULL";
			PrinterStatusMessages.PAUSE_MSG = "PAUSE";
			PrinterStatusMessages.NULL_MSG = "INVALID STATUS";
		}

		/// <summary>
		///       Used to acquire a human readable string of the current errors/warnings stored in <c>printerStatus</c></summary>
		/// <param name="printerStatus">an instance of <see cref="T:Zebra.Sdk.Printer.PrinterStatus" /> that will be used to acquire the human readable string
		///       of warnings/errors stored in <c>printerStatus</c></param>
		public PrinterStatusMessages(PrinterStatus printerStatus)
		{
			this.printerStatus = printerStatus;
		}

		/// <summary>
		///       Used to acquire a human readable string of the current errors/warnings passed to this instance.
		///       </summary>
		/// <returns>A human readable string array of the current errors/warnings passed to this instance.</returns>
		public string[] GetStatusMessage()
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (this.printerStatus == null)
			{
				stringBuilder.Append(PrinterStatusMessages.NULL_MSG);
				stringBuilder.Append(";");
				return new string[] { stringBuilder.ToString() };
			}
			if (this.printerStatus.isHeadOpen)
			{
				stringBuilder.Append(PrinterStatusMessages.HEAD_OPEN_MSG);
				stringBuilder.Append(";");
			}
			if (this.printerStatus.isHeadTooHot)
			{
				stringBuilder.Append(PrinterStatusMessages.HEAD_TOO_HOT_MSG);
				stringBuilder.Append(";");
			}
			if (this.printerStatus.isPaperOut)
			{
				stringBuilder.Append(PrinterStatusMessages.PAPER_OUT_MSG);
				stringBuilder.Append(";");
			}
			if (this.printerStatus.isRibbonOut)
			{
				stringBuilder.Append(PrinterStatusMessages.RIBBON_OUT_MSG);
				stringBuilder.Append(";");
			}
			if (this.printerStatus.isReceiveBufferFull)
			{
				stringBuilder.Append(PrinterStatusMessages.RECEIVE_BUFFER_FULL_MSG);
				stringBuilder.Append(";");
			}
			if (this.printerStatus.isPaused)
			{
				stringBuilder.Append(PrinterStatusMessages.PAUSE_MSG);
				stringBuilder.Append(";");
			}
			if (stringBuilder.Length > 0 && stringBuilder[stringBuilder.Length - 1] == ';')
			{
				stringBuilder.Remove(stringBuilder.Length - 1, 1);
			}
			return StringUtilities.Split(stringBuilder.ToString(), ";");
		}
	}
}