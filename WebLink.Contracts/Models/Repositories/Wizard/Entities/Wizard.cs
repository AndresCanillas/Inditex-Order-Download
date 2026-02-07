using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public class Wizard : IWizard
    {


        #region IEntity
        [PK, Identity]
        public int ID { get; set; }
        #endregion IEntity

        #region IWizard
        
        public int OrderID { get; set; }
        // calculate steps id
        public bool IsCompleted { get; set; }
        public float Progress { get; set; }

        //[LazyLoad]
        //public ICollection<WizardStep> Steps { get; set; }

        #endregion IWizard

        #region IBasicTracing
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        #endregion IBasicTracing


        public float GetProgress() { return 0; }
    }
}
