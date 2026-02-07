namespace WebLink.Contracts.Models.Delivery
{ 
    public class PackageDetail
    {
        public int ID { get; set; }
        public int PackageID { get; set; }          // ID of the parent record (Package)    
        public int? ArticleID { get; set; }          // ID of the parent record (OrderArticle)   
        public int? ArticleUnitsID { get; set; }
        public int? PrinterJobID { get; set; }
        public int? PrinterJobDetailID { get; set; }    
        public string ArticleCode { get; set; }
        public string Description { get; set; }
        public string Size { get; set; }
        public string Colour { get; set; }
        public int Quantity { get; set; }           // Quantity of labels in this package
        public decimal Price { get; set; }         // Price of the article
        public int? ArticleCentralID { get; set; }
        public PrinterJobDetail PrinterJobDetail { get; set; } // Navigation property for the printer job detail
    }

}
