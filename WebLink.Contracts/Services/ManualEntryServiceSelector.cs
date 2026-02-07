using System;
using System.Collections.Generic;

namespace WebLink.Contracts.Services
{
    public class ManualEntryServiceSelector : IManualEntryServiceSelector
    {
        private const string TEMPE_SERVICE = "TEMPE";
        private const string MASSIMODUTTI_SERVICE = "MD";
        private const string ZARA_SERVICE = "ZARA"; 
        private const string BERSHKA_SERVICE = "BERSHKA";    
        private const string BARÇA_SERVICE = "BAR";   
        private const string TEMPE_XML_SERVICE = "TEMPE_XML";
        private const string TENDAM_SERVICE = "TENDAM"; 
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, IManualEntryService> _services; 
        public ManualEntryServiceSelector(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;

            
        }

        public IManualEntryFilterService GetFilterService(string manualEntryFilterService)
        {
            if(manualEntryFilterService == TENDAM_SERVICE)
            {
                return (GrupoTendamManualEntryFilterService)_serviceProvider.GetService(typeof(GrupoTendamManualEntryFilterService));
            }
            return null;
        }

        public IManualEntryGrouppingService GetGrouppingService(string serviceName)
        {
            if(serviceName == TENDAM_SERVICE)
            {
                return (GrupoTendamManualEntryGrouppingService)_serviceProvider.GetService(typeof(GrupoTendamManualEntryGrouppingService));
            }
            return null;
        }

        public IManualEntryService GetService(string serviceName)
        {
            if(serviceName == TEMPE_SERVICE)
            {
                
                return (TempeManualEntryService)_serviceProvider.GetService(typeof(TempeManualEntryService)); 
            }

            if(serviceName == MASSIMODUTTI_SERVICE)
            {
                return (ManualEntryService) _serviceProvider.GetService(typeof(ManualEntryService));    
            }

            if(serviceName == ZARA_SERVICE)
            {
                return (ZaraManualEntryService) _serviceProvider.GetService(typeof(ZaraManualEntryService));     
            }

            if (serviceName == BERSHKA_SERVICE)
            {
                return (BershkaManualEntryService)_serviceProvider.GetService(typeof(BershkaManualEntryService));
            }
            if(serviceName == BARÇA_SERVICE)
            {
                return (BarcaManualEntryService)_serviceProvider.GetService(typeof(BarcaManualEntryService));
            }

            if(serviceName == TEMPE_XML_SERVICE)
            {
                return (TempeManualEntryXmlService)_serviceProvider.GetService(typeof(TempeManualEntryXmlService));
            }
            return null; 
        }


    }
}
