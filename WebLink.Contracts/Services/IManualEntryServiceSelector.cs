namespace WebLink.Contracts.Services
{
    public interface IManualEntryServiceSelector
    {
        IManualEntryFilterService GetFilterService(string manualEntryFilterService);
        IManualEntryService GetService(string serviceName); 
        IManualEntryGrouppingService GetGrouppingService (string serviceName);  
    }
}
