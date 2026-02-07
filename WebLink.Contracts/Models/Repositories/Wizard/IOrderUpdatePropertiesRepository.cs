namespace WebLink.Contracts.Models
{
    public interface IOrderUpdatePropertiesRepository : IGenericRepository<IOrderUpdateProperties>
    {
		IOrderUpdateProperties GetByOrderID(int orderID);
		IOrderUpdateProperties GetByOrderID(PrintDB ctx, int orderID);
	}
}