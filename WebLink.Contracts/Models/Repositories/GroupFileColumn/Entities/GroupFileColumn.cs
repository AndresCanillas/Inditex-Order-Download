using System;

namespace WebLink.Contracts.Models
{
    public class GroupFileColumn : IGroupFileColumn
    {
        public int ProjectId { get; set; }

        public Project Project { get; set; }

        public string TableName { get; set; }
        public string Key { get; set; }
        public int ID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}

