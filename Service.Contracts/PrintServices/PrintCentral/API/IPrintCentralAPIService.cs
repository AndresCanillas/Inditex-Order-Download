using Service.Contracts;
using Service.Contracts.PrintServices.PrintCentral;
using Service.Contracts.PrintServices.PrintCentral.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Service.Contracts.PrintCentral
{

    public interface IPrintCentralAPIService
    {
        bool UploadProjectImage(string filePath, int projectID);
        void UploadOrderFile(string filePath, OrderData dto);
        Task UploadProjectImageAsync(string filePath, int projectID);
        Task UploadOrderFileAsync(string filePath, OrderData dto);
        Task InsertProviderAsync(int companyID, Provider provider);
        Task<int> InsertCompanyAsync(Suplier suplier);
        Task<IEnumerable<Provider>> GetProviders(int companyID);
        Task<IEnumerable<Suplier>> FilterCompaniesByName(string filterbyname);
        Task CheckUser(List<SupplierUserChecks> supplierUserChecks, int projectid); 

        void Connect();
        void Disconnect();
        Task InsertAddressAsync(int company, Address address);
        Task<OperationResult> AttachOrderGroupDocument(OrderAttachDocumentRequest attachRequest, Stream fileStream,string filename);

        Task UploadOrderManagamentFileAsync(string filePath, int projectID);
        Task<int> GetProviderLocation(int companyID, int projectid, string sendToCountry, string catalogName, string filterField, string selectField);
        Task<int> GetSupplierAddress(Address address, int supplierID);
        //IPrintCentralClient GetClient();
        //IAppConfig GetConfig();
        Task SendEmailIfErrorSupplier(List<SupplierErrors> errorsSuppliers, int projectID);
        Task UpdateProviderAsync(Provider provider);
        Task SendCustomerOrderNotificationAsync(CustomersOrderRecievedMailNotification customerOrderRecievedNotification, int projectID);
        Task InsertOrdersInOrderPool (List<OrderPool> orderPools, int projectID);
    }
    // Duplicated field from UploadOrderDTO
    public class OrderData
    {
        public int ProductionType { get; set; }
        public int PrinterID { get; set; }
        public int FactoryID { get; set; }
        public int ProjectID { get; set; }
        public int CompanyID { get; set; }
        public int BrandID { get; set; }
        public bool IsStopped { get; set; }
        public bool IsBillable { get; set; }
        public string MDOrderNumber { get; set; }
        public string OrderCategoryClient { get; set; }

    }

    public class Suplier
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int? MainLocationID { get; set; }
        public string MainContact { get; set; }
        public string MainContactEmail { get; set; }
        public string Culture { get; set; }
        public string Instructions { get; set; }
        public byte[] Logo { get; set; }
        public string CompanyCode { get; set; }
        public string IDTZone { get; set; }
        public string GSTCode { get; set; }
        public int? GSTID { get; set; }
        public string ClientReference { get; set; }
        public int? SLADays { get; set; }
        public int? DefaultProductionLocation { get; set; }
        public int? DefaultDeliveryLocation { get; set; }
        public bool ShowAsCompany { get; set; }
        public string FtpUser { get; set; }
        public string FtpPassword { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }

        public int? RFIDConfigID { get; set; }
        //public RFIDConfig RFIDConfig { get; set; }

        public string OrderSort { get; set; }
        public string HeaderFields { get; set; }
        public string StopFields { get; set; }

        public string CustomerSupport1 { get; set; }
        public string CustomerSupport2 { get; set; }
        public string ProductionManager1 { get; set; }
        public string ProductionManager2 { get; set; }
        public string ClientContact1 { get; set; }
        public string ClientContact2 { get; set; }
        public bool SyncWithSage { get; set; }
        public string SageRef { get; set; }
        public bool IsBroker { get; set; }
    }

    public class Provider
    {
        public bool IsVerifyfied { get; set; }
        public int ID { get; set; }
        public int ProviderCompanyID { get; set; }
        public string ClientReference { get; set; }
        public int? DefaultProductionLocation { get; set; }
        public int? SLADays { get; set; }
        public string CompanyName { get; set; }



    }
    public class Address
    {
        public string Name { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string CityOrTown { get; set; }
        public string StateOrProvince { get; set; }
        public string Country { get; set; }
        public int CountryID { get; set; }
        public string ZipCode { get; set; }
        public string Notes { get; set; }
        public bool Default { get; set; }
        public bool SyncWithSage { get; set; }
        public string SageRef { get; set; }
        public string AddressLine3 { get; set; }
        public string SageProvinceCode { get; set; }
        public string Telephone1 { get; set; }
        public string Telephone2 { get; set; }
        public string Email1 { get; set; }
        public string Email2 { get; set; }
        public string BusinessName1 { get; set; }
        public string BusinessName2 { get; set; }
    }


    public class CompanyInfo
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string CompanyCode { get; set; }
    }

    public class ProjectInfo
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string ProjectCode { get; set; }
        public string BrandFtpFolder { get; set; }
        public string ProjectFtpFolder { get; set; }
        public int BrandID { get; set; }
    }

}
