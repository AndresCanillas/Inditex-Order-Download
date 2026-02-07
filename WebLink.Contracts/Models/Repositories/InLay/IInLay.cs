using Service.Contracts.Database;

namespace WebLink.Contracts.Models
{
    public interface IInLay : IEntity, IBasicTracing
    {
        string ChipName { get; set; }
        string Description { get; set; }
        string Image { get; set; }
        string ProviderName { get; set; }
        string Model { get; set; }        
    }
}
