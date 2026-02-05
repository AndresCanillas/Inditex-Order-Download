using System;
using System.Collections.Generic;

namespace Service.Contracts.PrintCentral
{
    // Data contract used to synchronize data about produced RFID labels
    public class EncodedLabelDTO
    {
        public int CompanyID { get; set; }
        public int ProjectID { get; set; }
        public int OrderID { get; set; }
        public int DeviceID { get; set; }
        public string ArticleCode { get; set; }
        public string Barcode { get; set; }
        public long Serial { get; set; }
        public string TID { get; set; }
        public string EPC { get; set; }
        public string AccessPassword { get; set; }
        public string KillPassword { get; set; }
        public float RSSI { get; set; }
        public DateTime Date { get; set; }
        public int? InlayConfigID { get; set; }
        public string InlayConfigDescription { get; set; }
    }

    public class PendingOrdersRq
    {
        public IEnumerable<int> Orders { get; set; }
        public int FactoryID { get; set; }
        public double DeltaTime { get; set; }
        public bool ExecuteSync { get; set; }
    }
}
