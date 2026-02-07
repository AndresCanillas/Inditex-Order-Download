using Service.Contracts;
using System;
using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public class ComparerConfiguration : IComparerConfiguration
    {
        [PK, Identity]
        public int ID { get; set; }
        public int? CompanyID { get; set; }
        public int? BrandID { get; set; }
        public int? ProjectID { get; set; }
        public ConflictMethod Method { get; set; }        
        public ComparerType Type { get; set; }
        public string ColumnName { get; set; }
        public bool CategorizeArticle { get; set; }

        #region IBasicTracing
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        #endregion IBasicTracing
    }

    public class OrderComparerViewModel
    {
        public string NewData { get; set; }
        public string PrevData { get; set; }
        public List<List<string>> NewDataUpdates { get; set; } = new List<List<string>>();
        public List<List<string>> PrevDataUpdates { get; set; } = new List<List<string>>();
    }

    public class OrderData
    {
        public List<Dictionary<string, string>> NewData { get; set; }
        public List<Dictionary<string, string>> PrevData { get; set; }
    }
}
