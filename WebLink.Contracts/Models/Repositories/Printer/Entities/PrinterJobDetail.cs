using Service.Contracts;
using System;
using System.ComponentModel.DataAnnotations;

namespace WebLink.Contracts.Models
{
    [TargetTable("PrinterJobDetails")]
    public class PrinterJobDetail : IPrinterJobDetail
    {
        private object syncObj = new object();
        private volatile int printed;
        private volatile int errors;
        private volatile int extras;
        private volatile int encoded;
        private volatile int transferProgress;
        private volatile int exportProgress;
        private volatile int verifyProgress;

        [PK, Identity]
        public int ID { get; set; }
        public int PrinterJobID { get; set; }
        [IgnoreField]
        public PrinterJob PrinterJob { get; set; }
        public int ProductDataID { get; set; }      // ID of the Detail row from Dynamic Catalogs.
        public int Quantity { get; set; }
        public int QuantityRequested { get; set; }
        [MaxLength(25)]
        public string PackCode { get; set; }

        public int Printed
        {
            get { lock(syncObj) return printed; }
            set { lock(syncObj) printed = value; }
        }
        public int Errors
        {
            get { lock(syncObj) return errors; }
            set { lock(syncObj) errors = value; }
        }
        public int Extras
        {
            get { lock(syncObj) return extras; }
            set { lock(syncObj) extras = value; }
        }
        public int Encoded
        {
            get { lock(syncObj) return encoded; }
            set { lock(syncObj) encoded = value; }
        }
        
        public int TransferProgress
        {
            get { lock(syncObj) return transferProgress; }
            set { lock(syncObj) transferProgress = value; }
        }
        
        public int ExportProgress
        {
            get { lock(syncObj) return exportProgress; }
            set { lock(syncObj) exportProgress = value; }
        }
        
        public int VerifyProgress
        {
            get { lock(syncObj) return verifyProgress; }
            set { lock(syncObj) verifyProgress = value; }
        }
        
        public DateTime? LastEncodeDate { get; set; }
        public DateTime? LastPrintDate { get; set; }
        public DateTime? LastVerifyDate { get; set; }

        public DateTime UpdatedDate { get; set; }

    }
}

