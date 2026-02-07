using Service.Contracts;
using Services.Core;
using System.Collections.Generic;
using System.Linq;

namespace WebLink.Contracts.Models
{

    public interface IERPCompanyLocationRepository
    {
        
        IEnumerable<ERPCompanyConfigDTO> GetByCompany(int companyID);
        IEnumerable<ERPCompanyConfigDTO> GetByCompany(PrintDB ctx, int companyID);

        void SaveERPConfiguration(UpdateERPConfigRequest rq);
        void SaveERPConfiguration(PrintDB ctx, UpdateERPConfigRequest rq);

        IERPCompanyLocation GetByOrder(OrderInfoDTO orderInfo);
        IERPCompanyLocation GetByOrder(PrintDB ctx, OrderInfoDTO orderInfo);

        IERPCompanyLocation DeleteErpConfig(int erpConfigID);
        IERPCompanyLocation DeleteErpConfig(PrintDB ctx, int erpConfigID);

    }

    public class ERPCompanyLocationRepository : GenericRepository<IERPCompanyLocation, ERPCompanyLocation>, IERPCompanyLocationRepository
    {

        private ILogService log;


        public ERPCompanyLocationRepository(
            IFactory factory,
            ILogService log
            )
            : base(factory, (ctx) => ctx.ERPCompanyLocations)
        {
            this.log = log;
        }

        protected override string TableName { get => "ERPCompanyLocations"; }

        protected override void UpdateEntity(PrintDB ctx, IUserData userData, ERPCompanyLocation actual, IERPCompanyLocation data)
        {
            if (!userData.IsIDT)
            {
                AuthorizeOperation(ctx, userData, actual);
            }

            actual.BillingFactoryCode = data.BillingFactoryCode;
            actual.CompanyID = data.CompanyID;
            actual.Currency = data.Currency;
            actual.ExpeditionAddressCode = data.ExpeditionAddressCode;
            actual.ERPInstanceID = data.ERPInstanceID;
            actual.DeliveryAddressID = data.DeliveryAddressID;
            actual.ProductionFactoryCode = data.ProductionFactoryCode; // erp production location
            actual.ProductionLocationID = data.ProductionLocationID; // our factory location

        }

        public IEnumerable<ERPCompanyConfigDTO> GetByCompany(int companyID)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByCompany(ctx, companyID).ToList();
            }
        }

        public IEnumerable<ERPCompanyConfigDTO> GetByCompany(PrintDB ctx, int companyID)
        {
            var q = ctx.ERPCompanyLocations
                .Join(ctx.Locations,
                ec => ec.ProductionLocationID, l => l.ID,
                (erpConfig, loc) => new { Location = loc, ERPCompanyLocation = erpConfig })
                .Join(ctx.ERPConfigs,
                jj1 =>jj1.ERPCompanyLocation.ERPInstanceID, jerp => jerp.ID,
                (j1, erp) => new {j1.Location, j1.ERPCompanyLocation, ERPConfig = erp } 
                )
                .Where(w => w.ERPCompanyLocation.CompanyID.Equals(companyID))
                .Select(s => new ERPCompanyConfigDTO()
                {
                    ID = s.ERPCompanyLocation.ID,
                    BillingFactoryCode = s.ERPCompanyLocation.BillingFactoryCode,
                    CompanyID = s.ERPCompanyLocation.CompanyID,
                    Currency = s.ERPCompanyLocation.Currency,
                    DeliveryAddressID = s.ERPCompanyLocation.DeliveryAddressID,
                    ExpeditionAddressCode = s.ERPCompanyLocation.ExpeditionAddressCode,
                    ERPInstanceID = s.ERPCompanyLocation.ERPInstanceID,
                    ERPName = s.ERPConfig.Name,
                    LocationName = s.Location.Name,
                    ProductionFactoryCode = s.ERPCompanyLocation.ProductionFactoryCode,
                    ProductionLocationID = s.ERPCompanyLocation.ProductionLocationID
                });


            return q;
        }

        public void SaveERPConfiguration(UpdateERPConfigRequest rq)
        {
            using (PrintDB ctx = factory.GetInstance<PrintDB>())
            {
                SaveERPConfiguration(ctx, rq);
            }
        }

        public void SaveERPConfiguration(PrintDB ctx, UpdateERPConfigRequest rq)
        {
            rq.Config.ToList().ForEach(cfg =>
            {
                cfg.CompanyID = rq.CompanyID;
                if (cfg.ID < 1)
                {
                    var i = Insert(ctx, cfg);
                    cfg.ID = i.ID;
                }
                else
                {
                    var r = Update(ctx, cfg);
                }
            });
        }

        public IERPCompanyLocation GetByOrder(OrderInfoDTO orderInfo)
        {
            using (PrintDB ctx = factory.GetInstance<PrintDB>())
            {
                return GetByOrder(ctx, orderInfo);
            }
        }

        public IERPCompanyLocation GetByOrder(PrintDB ctx, OrderInfoDTO orderInfo)
        {
            IEnumerable<IERPCompanyLocation> q = ctx.ERPCompanyLocations
                .Where(w => w.CompanyID.Equals(orderInfo.BillToCompanyID))
                .Where(w => w.ProductionLocationID.Equals(orderInfo.LocationID))
                .Select(s => s).ToList();

            if (q.Count() < 1)
            {
                log.LogWarning($"OrderID [{orderInfo.OrderID}] - OrderNumber [{orderInfo.OrderNumber}] - Company [{orderInfo.BillToCompanyID}] no has ERP Configuration for LocationID [{orderInfo.LocationID}]");

                var cmp = ctx.Companies.Find(orderInfo.BillToCompanyID);

                if (cmp.SyncWithSage && cmp.SageRef.Length > 0)
                {
                    var loc = ctx.Locations.Find(orderInfo.LocationID);

                    // default option
                    q = new List<IERPCompanyLocation>() {
                        new ERPCompanyLocation() {
                            BillingFactoryCode = loc.FactoryCode,
                            DeliveryAddressID = null,
                            ExpeditionAddressCode = string.Empty,
                            ProductionFactoryCode = loc.FactoryCode,
                            ProductionLocationID = orderInfo.LocationID.Value,
                            CompanyID = orderInfo.BillToCompanyID,
                            Currency = "EUR",
                            ERPInstanceID = 0 // disable to register in erp
                        }
                    };
                }
            }

            return q.First();

        }

        public IERPCompanyLocation DeleteErpConfig(int erpConfigID)
        {
            using(PrintDB ctx = factory.GetInstance<PrintDB>())
            {
                return DeleteErpConfig(ctx, erpConfigID);
            }
        }

        public IERPCompanyLocation DeleteErpConfig(PrintDB ctx, int erpConfigID)
        {
            var config = GetByID(ctx, erpConfigID, true);

            Delete(ctx, erpConfigID);

            return config;
        }
    }


    public class UpdateERPConfigRequest
    {
        public int CompanyID { get; set; }
        public IEnumerable<ERPCompanyLocation> Config { get; set; }
    }

}
