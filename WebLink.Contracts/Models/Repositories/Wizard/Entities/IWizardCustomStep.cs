using Service.Contracts.Database;

namespace WebLink.Contracts.Models
{
    public interface IWizardCustomStep : IEntity, IBasicTracing
    {

        int? CompanyID {get;set;}

        int? BrandID { get; set; }

        int? ProjectID { get; set; }

        WizardStepType Type { get; set; }
        
        string Url { get; set; }
        
        int Position { get; set; }
        string Name { get; set; }
        string Description { get; set; }
    }
}