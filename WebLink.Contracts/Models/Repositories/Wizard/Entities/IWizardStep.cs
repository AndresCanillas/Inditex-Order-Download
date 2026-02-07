using Service.Contracts.Database;

namespace WebLink.Contracts.Models
{
    // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum
    public enum WizardStepType
    {
        NotDefine = 0,
        SetOrderData = 5,
        ItemAssignment = 7,
        Quantity = 10,
        Labelling = 15,
        //Composition = 20,
        Extras = 30,
        SupportFiles = 50,
        ShippingAddress = 80,
        Review = 90
    }

    public interface IWizardStep: IEntity, IBasicTracing
    {
        int WizardID { get; set; }
        bool IsCompleted { get; set; }
        // Class Name To Handle Step actions
        WizardStepType Type { get; set; }
        string Url { get; set; }
        //string PageRoute { get; set; }
        int Position { get; set; }
        string Name { get; set; }
        string Description { get; set; }
    }
}