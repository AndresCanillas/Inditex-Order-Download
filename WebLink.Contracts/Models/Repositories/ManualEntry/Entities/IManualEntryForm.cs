using Service.Contracts.Database;

namespace WebLink.Contracts.Models
{
    public interface IManualEntryForm : IEntity, IBasicTracing
    {
        int? ProjectID { get; set; }
        string Url { get; set; }
        string Name { get; set; }
        string Description { get; set; }
    }
}
