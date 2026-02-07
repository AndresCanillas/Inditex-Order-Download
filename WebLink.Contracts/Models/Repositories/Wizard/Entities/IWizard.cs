using Service.Contracts.Database;

namespace WebLink.Contracts.Models
{
    public interface IWizard : IEntity, IBasicTracing
    {

        int OrderID { get; set; }

        bool IsCompleted { get; set; }

        float Progress { get; set; }

        float GetProgress();
    }
}