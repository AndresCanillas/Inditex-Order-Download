using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.Documents
{
	public interface IDynamicExportClient
	{
		string Url { get; set; }
		ExportResult ProcessOrder(ExportSettings settings);
		Task<ExportResult> ProcessOrderAsync(ExportSettings settings);
	}

	public class ExportSettings
	{
		public int PrinterJobId { get; set; }
		public string OutputPath { get; set; }
		public List<RFIDData> RFIDData { get; set; }
		public List<RFIDData> RFIDExtras { get; set; }
	}

	public class ExportResult
	{
		public bool Success { get; set; }
		public string Error { get; set; }
	}

	public class RFIDData
	{
		public string Barcode { get; set; }
		public long SerialNumber { get; set; }
		public string SerialNumberHEX { get; set; }
		public string EPC { get; set; }
		public string UserMemory { get; set; }
		public string AccessPassword { get; set; }
		public string KillPassword { get; set; }
		public bool ApplyLocks { get; set; }
		public int EPCLock { get; set; }
		public int UserLock { get; set; }
		public int AccessPasswordLock { get; set; }
		public int KillPasswordLock { get; set; }
	}
}
