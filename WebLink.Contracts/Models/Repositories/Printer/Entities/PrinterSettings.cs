using System;
using System.ComponentModel.DataAnnotations;

namespace WebLink.Contracts.Models
{
    public class PrinterSettings : IPrinterSettings
    {
        public int ID { get; set; }
        public int PrinterID { get; set; }
        public Printer Printer { get; set; }
        public int ArticleID { get; set; }
        public Article Article { get; set; }
        public double XOffset { get; set; }
        public double YOffset { get; set; }
        public string Speed { get; set; }
        public string Darkness { get; set; }
        public bool Rotated { get; set; }
        public bool ChangeOrientation { get; set; }
        public bool PauseOnError { get; set; }
        public bool EnableCut { get; set; }
        public CutBehavior CutBehavior { get; set; }
        public bool ResumeAfterCut { get; set; }
        [MaxLength(50)]
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        [MaxLength(50)]
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}

