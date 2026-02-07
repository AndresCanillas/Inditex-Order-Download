using Service.Contracts;
using System;

namespace WebLink.Contracts.Models
{
    public class ArticleTracking : IArticleTracking
    {
        [PK, Identity]
        public int ID { get; set; }
        public int ArticleID { get; set; }
        public DateTime InitialDate { get; set; }
        [Nullable]
        public string LastUpdateUserName { get; set; }
    }
}

