using System;
using Zebra.Sdk.Comm;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       A class used to obtain the status of a Zebra printer.
	///       </summary>
	public abstract class PrinterStatus
	{
		/// <summary>
		///       The print mode. For CPCL printers this is always <see cref="F:Zebra.Sdk.Printer.ZplPrintMode.UNKNOWN" /></summary>
		public ZplPrintMode printMode;

		/// <summary>
		///       The length of the label in dots. For CPCL printers this is always 0.
		///       </summary>
		public int labelLengthInDots;

		/// <summary>
		///       The number of formats currently in the receive buffer of the printer. For CPCL printers this is always 0.
		///       </summary>
		public int numberOfFormatsInReceiveBuffer;

		/// <summary>
		///       The number of labels remaining in the batch. For CPCL printers this is always 0.
		///       </summary>
		public int labelsRemainingInBatch;

		/// <summary>
		///   <c>true</c> if there is a partial format in progress. For CPCL printers this is always <c>false</c>.
		///       </summary>
		public bool isPartialFormatInProgress;

		/// <summary>
		///   <c>true</c> if the head is cold. For CPCL printers this is always <c>false</c></summary>
		public bool isHeadCold;

		/// <summary>
		///   <c>true</c> if the head is open.
		///       </summary>
		public bool isHeadOpen;

		/// <summary>
		///   <c>true</c> if the head is too hot. For CPCL printers this is always <c>false</c></summary>
		public bool isHeadTooHot;

		/// <summary>
		///   <c>true</c> if the paper is out.
		///       </summary>
		public bool isPaperOut;

		/// <summary>
		///   <c>true</c> if the ribbon is out.
		///       </summary>
		public bool isRibbonOut;

		/// <summary>
		///   <c>true</c> if the receive buffer is full. For CPCL printers this is always <c>false</c></summary>
		public bool isReceiveBufferFull;

		/// <summary>
		///   <c>true</c> if the printer is paused. For CPCL printers this is always <c>false</c></summary>
		public bool isPaused;

		/// <summary>
		///   <c>true</c> if the printer reports back that it is ready to print
		///       </summary>
		public bool isReadyToPrint;

		protected Connection printerConnection;

		private bool statusHasBeenRetrievedFromPrinter;

		/// <summary>
		///       Constructs a PrinterStatus instance that can be used to determine the status of a printer.
		///       </summary>
		/// <param name="printerConnection">Connection to the target printer</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs</exception>
		public PrinterStatus(Connection printerConnection)
		{
			this.printerConnection = printerConnection;
			this.numberOfFormatsInReceiveBuffer = 0;
			this.labelsRemainingInBatch = 0;
			this.isPartialFormatInProgress = false;
			this.isHeadCold = false;
			this.printMode = ZplPrintMode.UNKNOWN;
			this.labelLengthInDots = 0;
			this.GetStatusFromPrinter();
		}

		private void GetStatusFromPrinter()
		{
			if (!this.statusHasBeenRetrievedFromPrinter)
			{
				this.UpdateStatus();
				this.statusHasBeenRetrievedFromPrinter = true;
				this.isReadyToPrint = !this.isPaperOut;
				this.isReadyToPrint &= !this.isPaused;
				this.isReadyToPrint &= !this.isReceiveBufferFull;
				this.isReadyToPrint &= !this.isHeadTooHot;
				this.isReadyToPrint &= !this.isHeadOpen;
				this.isReadyToPrint &= !this.isRibbonOut;
			}
		}

		protected abstract void UpdateStatus();
	}
}