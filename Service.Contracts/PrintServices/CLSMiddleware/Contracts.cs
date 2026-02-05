using System;
using System.Text;
using System.Collections.Generic;
using Service.Contracts.Database;

namespace Service.Contracts.CLSMiddleware
{
	public class CLSResponse
	{
		public bool Success;
		public string ErrorMessage;
	}


	public class CLSEncodeJob
	{
		public int DeviceID;
		public int OrderID;
		public string MDOrderNumber;
		public int ArticleID;
		public string ArticleCode;
		public string GroupingColumn;
		public string DisplayColumn1;
		public string DisplayColumn2;
		public string DisplayColumn3;
		public List<TableData> Data;
	}


	public class CLSSettings
	{

	}


	public class PrintedTagData
	{
		public int DeviceID;
		public int OrderID;
		public string MDOrderNumber;
		public List<UnitData> Units;
	}


	public class UnitData
	{
		public int UnitID;
		public int ArticleID;
		public string ArticleCode;
		public string GroupingColumn;
		public string DisplayColumn1;
		public string DisplayColumn2;
		public string DisplayColumn3;
		public EncodingSettings EncodingSettings;
		public List<EncodingData> EPCs;
	}


	public class EncodingSettings
	{
		public bool WriteUserMemory { get; set; }
		public bool WriteAccessPassword { get; set; }
		public bool WriteKillPassword { get; set; }
		public bool WriteLocks { get; set; }
		public RFIDLockType EPCLock { get; set; }
		public RFIDLockType UserLock { get; set; }
		public RFIDLockType AccessLock { get; set; }
		public RFIDLockType KillLock { get; set; }
	}


	public class EncodingData
	{
		public string EPC { get; set; }
		public string Barcode { get; set; }
		public long SerialNumber { get; set; }
		public string UserMemory { get; set; }
		public string AccessPassword { get; set; }
		public string KillPassword { get; set; }
		public string TrackingCode { get; set; }
	}


	public class CLSRFIDResult
	{
		public int UnitID;
		public List<RFIDTagInfo> EncodedTags;
	}


	public class RFIDTagInfo
	{
		public int UnitID;
		public string Barcode;
		public string EPC;
		public string TID;
		public long Serial;
		public string AccessPassword;
		public string KillPassword;
		public int RSSI;
		public DateTime EncodingDate;
	}


	public class ClosedOrder
	{
		public int OrderID;
		public string MDOrderNumber;
		public CloseReason CloseReason;
	}


	public enum CloseReason
	{
		OrderCompleted,
		OrderCancelled
	}
}
