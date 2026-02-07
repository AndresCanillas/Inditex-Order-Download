using Service.Contracts;
using Service.Contracts.Database;
using Services.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts.Migrations;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Services
{
    public class ProviderGetLocationService : IProviderGetLocationService
    {
        private readonly IUserData userData;
        private readonly IUserRepository repo;
        private readonly ILogService log;
        private readonly ILocalizationService g;
        private readonly ICompanyRepository companyRepository;
        private readonly IOrderEmailService orderEmailService;
        private readonly IOrderNotificationManager notificationManager;
        private readonly IProjectRepository projectRepository;

        private readonly ICatalogRepository catalogRepo;
        private IConnectionManager connManager;
        private IFactory factory;

        public ProviderGetLocationService(ICatalogRepository catalogRepo, IConnectionManager connManager, IFactory factory, IOrderEmailService orderEmailService, ICompanyRepository companyRepository, IUserRepository repo)
        {
            this.catalogRepo = catalogRepo;
            this.connManager = connManager;
            this.factory = factory;
            this.orderEmailService = orderEmailService;
            this.companyRepository = companyRepository;
            this.repo = repo;
        }

        public class CountryFactory
        {
            public string Factory { get; set; }  
        }


        public int GetLocation(int companyId, int projectid, string countryCode, string catalogName, string filterField, string selectField)
        {
            var catalogs = catalogRepo.GetByProjectID(projectid, true);
            if (catalogs == null || catalogs.Count == 0)
            {
                SendNotificationFactoryNotFound(companyId, projectid, countryCode);
                return 0;
            }

            var catalog = catalogs.Find(c => string.Equals(c.Name, catalogName, StringComparison.OrdinalIgnoreCase));
            if (catalog == null)
            {
                SendNotificationFactoryNotFound(companyId, projectid, countryCode);
                return 0;
            }

            using (var dynamicDb = connManager.OpenDB("CatalogDB"))
            {
                // Use parameterized query to prevent SQL injection
                var countryLocation = dynamicDb.SelectOne<CountryFactory>(
                    $@"SELECT a.{selectField} FROM {catalog.TableName} a WHERE a.[{filterField}] = '{countryCode}'"
                );

                // Fixed logic: check if countryLocation is NOT null
                if (countryLocation != null && !string.IsNullOrEmpty(countryLocation.Factory))
                {
                    using (var ctx = factory.GetInstance<PrintDB>())
                    {
                        var printLocation = ctx.Locations.FirstOrDefault(
                            f => string.Equals(f.FactoryCode, countryLocation.Factory, StringComparison.OrdinalIgnoreCase)
                        );
                        
                        if (printLocation != null)
                        {
                            return printLocation.ID;
                        }
                    }
                }
                SendNotificationFactoryNotFound(companyId, projectid, countryCode);
                return 0;
            }
        }

        private async Task SendNotificationFactoryNotFound(int companyId, int projectid, string countryCode)
        {
            var company = companyRepository.GetByID(companyId);
            if(company == null) return; 
            var companyName = company.Name ?? "Unknown Company";

            var emailSubject = $"No record found in CountryFactory for {companyName} and country code {countryCode}";
            var emailBody = $"Please, check Country factory catalog for company {companyName}: {countryCode} record not found";

            var companyContactUser = repo.GetByID(company.CustomerSupport1);
            if(companyContactUser == null) return;
            await orderEmailService.SendMessage(companyContactUser.Email, emailSubject, emailBody, null);

        }
    }
}
