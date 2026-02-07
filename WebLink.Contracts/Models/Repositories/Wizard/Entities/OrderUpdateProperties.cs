using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public class OrderUpdateProperties : IOrderUpdateProperties
    {
        #region IEntity
        public int ID { get; set; }
        #endregion IEntity

        #region IOrderUpdateProperties

        public int OrderID { get; set; }

        public bool IsActive { get; set; }

        public bool IsRejected { get; set; }


        #endregion IOrderUpdateProperties

        #region IBasicTracing

        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }

        #endregion IBasicTracing

    }
}
