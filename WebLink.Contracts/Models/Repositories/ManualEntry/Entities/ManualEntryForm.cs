using Service.Contracts;
using System;

namespace WebLink.Contracts.Models.Repositories.ManualEntry.Entities
{

    public enum OrderPoolFormType
    {
        ManualEntryForm = 1,
        PoolManagerForm = 2,   
        PDFExtractor = 3
    }
    public class ManualEntryForm : IManualEntryForm
    {
        #region IEntity
        [PK, Identity]
        public int ID { get; set; }
        #endregion IEntity

        #region IManualEntryForm 
        public int? ProjectID { get; set; }
        public string Url { get; set; }
        [MaxLen(64)]
        public string Name { get; set; }
        [MaxLen(255)]
        public string Description { get; set; }

        public int? CompanyID { get; set; }  
        public OrderPoolFormType FormType { get; set; } 

        #endregion IManualEntryForm

        #region IBasicTracing
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        #endregion IBasicTracing

    }
}
