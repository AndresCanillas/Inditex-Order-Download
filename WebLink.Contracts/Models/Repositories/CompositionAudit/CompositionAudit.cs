using Service.Contracts.Database;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebLink.Contracts.Models
{

    public interface ICompostionAudit : IEntity
    {
        int OrderID { get; set; }
        string AuditCompo { get; set; } 
        string CreatedBy { get; set; }     
        DateTime CreatedDate { get; set; } 
    }
    public enum AuditStatus
    {
        Pending, 
        Pass, 
        Error 
    }

    public class CompositionAudit : ICompostionAudit
    {
        public int ID { get; set; }
        public int OrderID { get; set; }
        [Column(TypeName = "nvarchar(max)")]
        public string AuditCompo { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }

        public DateTime AuditDate {  get; set; }    

        public AuditStatus Status { get; set; }
        [Column(TypeName = "nvarchar(max)")]
        public string AuditMessages { get; set; }           
    }
}
