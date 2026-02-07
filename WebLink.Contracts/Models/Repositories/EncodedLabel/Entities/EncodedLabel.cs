using System;
using System.ComponentModel.DataAnnotations;

namespace WebLink.Contracts.Models
{
    // Stores a record of each rfid label printed through the system
    public class EncodedLabel : IEncodedLabel
    {
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public int ProjectID { get; set; }
        public int OrderID { get; set; }
        [MaxLength(25)]
        public string ArticleCode { get; set; }
        [MaxLength(25)]
        public string Barcode { get; set; }
        public int ProductionType { get; set; }
        public int ProductionLocationID { get; set; }
        public int DeviceID { get; set; }
        public long Serial { get; set; }
        [MaxLength(32)]
        public string TID { get; set; }
        [MaxLength(32)]
        public string EPC { get; set; }
        [MaxLength(8)]
        public string AccessPassword { get; set; }
        [MaxLength(8)]
        public string KillPassword { get; set; }
        public float RSSI { get; set; }
        public bool Success { get; set; }
        [MaxLength(200)]
        public string ErrorCode { get; set; }
        public DateTime Date { get; set; }
        public SyncState SyncState { get; set; }
        public int? InlayConfigID { get; set; }
        public string InlayConfigDescription { get; set; }

    }
}

