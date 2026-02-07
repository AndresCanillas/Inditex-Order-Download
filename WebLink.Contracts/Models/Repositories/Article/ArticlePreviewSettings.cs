namespace WebLink.Contracts.Models
{
    public class ArticlePreviewSettings
    {
        public int ID { get; set; }
        public int ArticleID { get; set; }
        public Article Company { get; set; }
        public int Rows { get; set; }
        public int Cols { get; set; }
    }
}

