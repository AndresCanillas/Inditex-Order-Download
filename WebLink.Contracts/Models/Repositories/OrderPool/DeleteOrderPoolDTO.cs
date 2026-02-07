namespace WebLink.Contracts.Models.Repositories.OrderPool
{
    public class DeleteOrderPoolDTO
    {
        public int ID { get; set; }  
        public string OrderNumber { get; set; } 
        public string ArticleCode { get; set; } 
        public int ProjectID { get; set; }   
    }
}
