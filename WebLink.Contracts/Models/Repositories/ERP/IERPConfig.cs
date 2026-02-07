using Service.Contracts.Database;

namespace WebLink.Contracts.Models
{
    public interface IERPConfig : IEntity, IBasicTracing
    {
        string Name { get; set; }
    }
}
