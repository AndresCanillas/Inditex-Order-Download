namespace WebLink.Services.Wizards.PuntoRoma
{
    public class OrderDataDTO
    {
        public int OrderGroupID;
        public int OrderID;
        public int ProjectID;

        public OrderDataDTO(int OrderGroupID, int OrderID, int ProjectID)
        {
            this.OrderGroupID = OrderGroupID;
            this.OrderID = OrderID; 
            this.ProjectID = ProjectID;
        }
    }
}
