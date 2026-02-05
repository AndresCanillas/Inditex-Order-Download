using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
    public class Print
    {
        //public string OrderClient;
        //public string OrderMD;
        //public int JobId;
        //public string Company;
        //public string Article;


        public int IdPrint { get; set; } = 0;
        public int? IdSystemJob { get; set; } = null;
        public int OperationMode { get; set; } = 2;
        public string OrderClient { get; set; }
        public string OrderMD { get; set; }
        public string OrderSage { get; set; } = string.Empty;
        public string Client { get; set; }
        public string LabelCode { get; set; }
        public string Image { get; set; } = string.Empty;
        public bool Sync { get; set; } = true;
        public string SyncTS { get; set; } = null;
        public List<PrintHeader> PrintHeader;

        public Print()
        {
            PrintHeader = new List<PrintHeader>();
        }
    }

    public class PrintHeader
    {
        public int IdRfidHeader { get; set; } = 0;
        public int IdPrint { get; set; }
        public int? IdSystemJob { get; set; } = null;
        public int? HeaderLine { get; set; } = null;
        public string ClientRfid { get; set; }
        public string Barcode { get; set; } = null;
        public string BarcodeQr { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Colour { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int ExtraLabels { get; set; } = 5;
        public int TagsXPage { get; set; } = 1;
        public bool WrEpc { get; set; } = true;
        public bool WrAccesPwd { get; set; } = true;
        public bool WrKillPwd { get; set; } = true;
        public bool WrUserMem { get; set; } = true;
        public int LockEpc { get; set; } = (int)RFIDLockType.Lock;
        public int LockAccesPwd { get; set; } = (int)RFIDLockType.Lock;
        public int LockKillPwd { get; set; } = (int)RFIDLockType.Lock;
        public int LockUserMem { get; set; } = (int)RFIDLockType.Lock;
        public int TagsOk { get; set; } = 0;
        public int TagsNOk { get; set; } = 0;
        public string NodoPC { get; set; } = string.Empty;
        public bool RegLocked { get; set; } = false;
        public bool Sync { get; set; } = true;
        public string SyncTS { get; set; } = null;

        public List<PrintHeaderDetail> HeaderDetail;

        public PrintHeader()
        {
            HeaderDetail = new List<PrintHeaderDetail>();
        }
        //public int printId;
        //public int JobId;
        //public int LineId;
        //public string Company;
        //public string Barcode;
        //public string Size;
        //public int Quantity;
        //public int ExtraLabels;
        //public TagEncodingInfo encoding;
    }

    public class PrintHeaderDetail
    {
        public int IdRfidDetail { get; set; } = 0;
        public int IdRfidHeader { get; set; }
        public int? IdSystemJob { get; set; } = null;
        public int? HeaderLine { get; set; } = null;
        public int? DetailLine { get; set; } = null;
        public string ClientRfid { get; set; }
        public string Epc { get; set; }
        public string Tid { get; set; } = null;
        public string AccesPwd { get; set; } = "00000000";
        public string KillPwd { get; set; } = "00000000";
        public string UserMem { get; set; } = "00000000";
        public bool EncodedOk { get; set; } = false;
        public int Serial { get; set; }
        public bool CheckedEpc { get; set; } = false;
        public bool CheckedAccesPwd { get; set; } = false;
        public bool CheckedKillPwd { get; set; } = false;
        public bool CheckedUserMem { get; set; } = false;
        public bool CheckedLocks { get; set; } = false;
        public bool Sync { get; set; } = true;
        public string SyncTS { get; set; } = null;
        public string NodoPC { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;

        //public int JobId;
        //public int HeaderLineId;
        //public int DetailId;
        //public string ClientRfid;
        //public TagEncodingInfo encoding;
    }
}
