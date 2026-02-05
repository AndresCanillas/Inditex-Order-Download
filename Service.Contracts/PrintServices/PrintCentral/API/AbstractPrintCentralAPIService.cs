using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.PrintCentral {
    [Obsolete("Prueba para crear cliente generico")]
    public abstract class AbstractPrintCentralAPIServiceFAIL
    {
     
        public virtual void Connect()
        {         
            var user = GetConfig().GetValue<string>("PrintEntryWeb.Credentials.User");
            var password = GetConfig().GetValue<string>("PrintEntryWeb.Credentials.Password");
            ((BaseServiceClient)GetClient()).Login("/", user, password);
        }

        public virtual void Disconnect()
        {
            GetClient().Logout();
        }

        public abstract IPrintCentralClient GetClient();

        public abstract IAppConfig GetConfig(); 
 

        public virtual Task<IEnumerable<Provider>> GetProviders(int companyID)
        {
            throw new NotImplementedException();
        }

        public virtual Task InsertAddressAsync(int company, Address address)
        {
            throw new NotImplementedException();
        }

        public virtual Task<int> InsertCompanyAsync(Suplier suplier)
        {
            throw new NotImplementedException();
        }

        public virtual Task InsertProviderAsync(int companyID, Provider provider)
        {
            throw new NotImplementedException();
        }

        public virtual void UploadOrderFile(string filePath, OrderData dto)
        {
            throw new NotImplementedException();
        }

        public virtual Task UploadOrderFileAsync(string filePath, OrderData dto)
        {
            throw new NotImplementedException();
        }

        public virtual bool UploadProjectImage(string filePath, int projectID)
        {
            throw new NotImplementedException();
        }

        public virtual Task UploadProjectImageAsync(string filePath, int projectID)
        {
            throw new NotImplementedException();
        }
    }
}
