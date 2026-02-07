using Service.Contracts.Database;

namespace WebLink.Contracts.Models
{
    public interface IOrderUpdateProperties : IOrderProcessProperties
    {

        int OrderID { get; set; }
        bool IsActive { get; set; }
        bool IsRejected { get; set; }
    }
}