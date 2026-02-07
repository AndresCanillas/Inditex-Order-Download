using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public class WizardCustomStep : IWizardCustomStep
    {
        #region IEntity
        [PK, Identity]
        public int ID { get; set; }
        #endregion IEntity

        #region IWizardCustomStep
        public int? CompanyID { get; set; }
        public int? BrandID { get; set; }
        public int? ProjectID { get; set; }
        public WizardStepType Type { get; set; }
        public string Url { get; set; }
        public int Position { get; set; }
        [MaxLen(64)]
        public string Name { get; set; }
        [MaxLen(255)]
        public string Description { get; set; }

        #endregion IWizardCustomStep

        #region IBasicTracing
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        #endregion IBasicTracing
    }
}
