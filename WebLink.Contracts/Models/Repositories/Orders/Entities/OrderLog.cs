using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebLink.Contracts.Models
{
    public class OrderLog : IOrderLog
    {
        #region IEntity
        public int ID { get; set; }
        #endregion IEntity

        #region IOrderLog
        public int OrderID { get; set; }
        public OrderLogLevel Level { get; set; }

        [MaxLength(1024)]
        public string Message { get; set; }
        [MaxLength(1024)]
        public string Comments { get; set; }
        #endregion IOrderLog

        #region IBasicTracing

        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }

        #endregion IBasicTracing
    }
}
