using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
	public interface IZPrinter : IDisposable
	{
		int ID { get; }
		int CompanyID { get; }
		int LocationID { get; }
		string DeviceID { get; }
		string ProductName { get; }
		string Name { get; set; }
		string Firmware { get; }
		int CurrentJobID { get; }
		IWSConnection MainChannel { get; set; }
		IWSConnection RawChannel { get; }
		bool Connected { get; }
		void RegisterChannel(IWSConnection conn);
		bool IsPrinting();
		void ResumeWork();
		Task<PrinterState> GetPrinterState();
		void StartJob(IPrinterJob job);
		void PauseJob(IPrinterJob job);
		Task PrintSample(int projectid, int articleid, int orderid, int detailid);
	}


	public class PrinterState: IEqualityComparer<PrinterState>, IEquatable<PrinterState>
	{
		public int ID;
		public bool Online;
		public bool Ready;
		public bool Paused;
		public bool PaperOut;
		public bool RibbonOut;
		public bool HeadOpen;
		public int FormatsInBuffer;

		public bool Equals(PrinterState x, PrinterState y)
		{
			if (x == null && y == null)
				return true;
			if (x == null || y == null)
				return false;
			return (x.Ready == y.Ready &&
				x.Paused == y.Paused &&
				x.PaperOut == y.PaperOut &&
				x.RibbonOut == y.RibbonOut &&
				x.HeadOpen == y.HeadOpen &&
				x.FormatsInBuffer == y.FormatsInBuffer);
		}

		public int GetHashCode(PrinterState obj)
		{
			StringBuilder sb = new StringBuilder(8);
			sb.Append(obj.Ready ? '1' : '0');
			sb.Append(obj.Paused ? '1' : '0');
			sb.Append(obj.PaperOut ? '1' : '0');
			sb.Append(obj.RibbonOut ? '1' : '0');
			sb.Append(obj.HeadOpen ? '1' : '0');
			sb.Append(obj.FormatsInBuffer);
			return sb.ToString().GetHashCode();
		}

		public bool Equals(PrinterState other)
		{
			if (other == null)
				return false;
			return (this.Ready == other.Ready &&
				this.HeadOpen == other.HeadOpen &&
				this.FormatsInBuffer == other.FormatsInBuffer &&
				this.PaperOut == other.PaperOut &&
				this.RibbonOut == other.RibbonOut &&
				this.Paused == other.Paused);
		}

		public bool CanPrint
		{
			get
			{
				return Online && !(Paused || PaperOut || RibbonOut || HeadOpen);
			}
		}
	}


	public class CompactPrinterState
	{
		public CompactPrinterState(PrinterState state)
		{
			ID = state.ID;
			var flags = PrinterStateFlags.None;
			if (state.Online) flags |= PrinterStateFlags.Online;
			if (state.Ready) flags |= PrinterStateFlags.Ready;
			if (state.Paused) flags |= PrinterStateFlags.Paused;
			if (state.PaperOut) flags |= PrinterStateFlags.PaperOut;
			if (state.RibbonOut) flags |= PrinterStateFlags.RibbonOut;
			if (state.HeadOpen) flags |= PrinterStateFlags.HeadOpen;
			FL = (int)flags;
		}

		public int ID { get; set; }
		public int FL { get; set; }
	}

	public enum PrinterStateFlags
	{
		None = 0,
		Online = 1,
		Ready = 2,
		Paused = 4,
		PaperOut = 8,
		RibbonOut = 16,
		HeadOpen = 32
	}
}
