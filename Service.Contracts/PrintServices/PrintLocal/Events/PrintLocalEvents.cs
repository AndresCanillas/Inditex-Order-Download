using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts.PrintLocal
{
	public class PLOrderStatusChangeEvent : EQEventInfo
	{
		public int OrderID;
		public PLOrderStatus Status;
		public string OrderNumber;

		public PLOrderStatusChangeEvent(int orderid, PLOrderStatus status, string orderNumber)
		{
			OrderID = orderid;
			Status = status;
			OrderNumber = orderNumber;
		}
	}

	public enum PLOrderStatus
	{
		Unknown,
		Pending = 30,
		Printing = 40,
		Completed = 101,
		Cancelled = 102
	}

	public class PLArticleStatusChangeEvent : EQEventInfo
	{
		public int PrintJobID;
		public string ArticleCode;
		public PLJobStatus Status;

		public PLArticleStatusChangeEvent(int printjobid, string articleCode, PLJobStatus status)
		{
			PrintJobID = printjobid;
			ArticleCode = articleCode;
			Status = status;
		}
	}

	public enum PLJobStatus
	{
		Unknown,
		Printing,
		Completed,
		Cancelled
	}


	public class PLUnitProgressChangeEvent : EQEventInfo
	{
		public int PrintJobDetailID;
		public int Progress;
		public int Extras;

        public int EncodeProgress;
        public int TransferProgress;
        public int ExportProgress;
        public int VerifyProgress;
        public DateTime? LastEncodeDate;
        public DateTime? LastPrintDate;
        public DateTime? LastVerifyDate;

        public PLUnitProgressChangeEvent(int printjobid, int progress, int extras,int encodeProgress,int transferProgress,int exportProgress, int verifyProgress, DateTime? lastEncodeUpdateDate,DateTime? lastPrintUpdateDate,DateTime? lastVerifyUpdateDate)
		{
			PrintJobDetailID = printjobid;
			Progress = progress;
			Extras = extras;
            EncodeProgress = encodeProgress;
            TransferProgress = transferProgress;
            ExportProgress = exportProgress;
            VerifyProgress = verifyProgress;
            LastEncodeDate = lastEncodeUpdateDate;
            LastPrintDate = lastPrintUpdateDate;
            LastVerifyDate = lastVerifyUpdateDate;
		}
	}

    public class DuplicatedEPCEvent : EQEventInfo
    {
        public List<string> EPCList;
        public string FactyoryName;
        public int OrderId;

        public DuplicatedEPCEvent(string factyoryName, int orderId, List<string> epcList)
        {
            FactyoryName = factyoryName;
            OrderId = orderId;
            EPCList = epcList;
        }
    }
}
