using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebLink.Contracts.Models
{


    public class OrderPoolRepository : GenericRepository<IOrderPool, OrderPool>, IOrderPoolRepository
    {
        private const string ORDER_SECTIONS = "OrderSections";
        private const int ID_NOTFOUND = -1;
        private const string MADE_IN = "MadeIn";
        private readonly IAppConfig config;
        private readonly IDBConnectionManager connectionManager;
        private readonly ILogService log;
        private readonly PrintDB ctx;
        private readonly ICatalogDataRepository catalogDataRepository;

        public OrderPoolRepository(IFactory factory,
                                   IEventQueue events,
                                   IAppConfig config,
                                   IDBConnectionManager connectionManager,
                                   ILogService log,
                                   ICatalogDataRepository catalogDataRepository) : base(factory, (ctx) => ctx.OrderPools)
        {
            this.factory = factory;
            this.events = events;
            this.config = config;
            this.connectionManager = connectionManager;
            this.log = log;
            this.ctx = factory.GetInstance<PrintDB>();
            this.catalogDataRepository = catalogDataRepository;
        }
        protected override string TableName => "OrderPools";

        protected override void UpdateEntity(PrintDB ctx, IUserData userData, OrderPool actual, IOrderPool data)
        {
            actual.ProjectID = data.ProjectID;
            actual.OrderNumber = data.OrderNumber;
            actual.Seasson = data.Seasson;
            actual.Year = data.Year;
            actual.ProviderCode1 = data.ProviderCode1;
            actual.ProviderName1 = data.ProviderName1;
            actual.ProviderCode2 = data.ProviderCode2;
            actual.ProviderName2 = data.ProviderName2;
            actual.Size = data.Size;
            actual.ArticleCode = data.ArticleCode;
            actual.CategoryCode1 = data.CategoryCode1;
            actual.CategoryCode2 = data.CategoryCode2;
            actual.CategoryCode3 = data.CategoryCode3;
            actual.CategoryCode4 = data.CategoryCode4;
            actual.CategoryCode5 = data.CategoryCode5;
            actual.CategoryCode6 = data.CategoryCode6;
            actual.CategoryText1 = data.CategoryText1;
            actual.CategoryText2 = data.CategoryText2;
            actual.CategoryText3 = data.CategoryText3;
            actual.CategoryText4 = data.CategoryText4;
            actual.CategoryText5 = data.CategoryText5;
            actual.CategoryText6 = data.CategoryText6;
            actual.ColorCode = data.ColorCode;
            actual.ColorName = data.ColorName;
            actual.CreationDate = data.CreationDate;
            actual.DeletedBy = data.DeletedBy;
            actual.DeletedDate = data.DeletedDate;
            actual.ExpectedProductionDate = data.ExpectedProductionDate;
            actual.LastUpdatedDate = data.LastUpdatedDate;
            actual.Price1 = data.Price1;
            actual.Price2 = data.Price2;
            actual.ProcessedBy = data.ProcessedBy;
            actual.ProcessedDate = data.ProcessedDate;
            actual.Quantity = data.Quantity;
        }

        public OrderPoolDTO GetOrderByOrderNumber(string ordernumber, int projectid)
        {
            var orders = ctx
                    .OrderPools
                    .Where(op => op.OrderNumber == ordernumber && op.ProjectID == projectid).ToList();

            if(orders == null || !orders.Any())
                return null;
            else
            {
                var listOfSections = GetListOfSections(orders.FirstOrDefault());
                var listOfMadeIn = GetListOfMadeIn(orders.FirstOrDefault());
                var orderHeader = Map(orders.FirstOrDefault(), listOfSections, listOfMadeIn);
                if(orderHeader == null)
                {
                    return null;
                }
                return GetOrderSizeInfo(orderHeader, orders);
            }

        }

        private OrderPoolDTO GetOrderSizeInfo(OrderPoolDTO orderHeader, IEnumerable<OrderPool> orders)
        {
            foreach(var order in orders)
            {

                var sizeInfo = new OrderPoolSizeInfoDTO()
                {
                    Color = order.ColorCode,
                    Size = order.Size,
                    Quantity = order.Quantity,
                };
                orderHeader.Sizes.Add(sizeInfo);
            }
            return orderHeader;
        }

        private OrderPoolDTO GetBarcaOrderSizeInfo(OrderPoolDTO orderHeader, IEnumerable<OrderPool> orders)
        {
            foreach(var order in orders)
            {

                var sizeInfo = new OrderPoolSizeInfoDTO()
                {
                    Color = order.ColorCode,
                    Size = order.Size,
                    Quantity = order.Quantity,
                    Description = order.CategoryText2,
                    EAN = order.CategoryText1,
                    Price1 = order.Price1.ToString(),
                    Price2 = order.Price2.ToString(),
                    Currency1 = order.CategoryCode4,
                    Currency2 = order.CategoryCode5,
                    UN = order.CategoryText3

                };
                orderHeader.Sizes.Add(sizeInfo);
            }
            return orderHeader;
        }


        private OrderPoolDTO MapBarca(OrderPool orderPool, List<OrderSectionCatalog> listOfSections, List<MadeInCatalog> listOfMadeIn)
        {
            if(orderPool == null) return null;

            var comapanyData = GetCompanyData(orderPool.ProjectID);

            if(comapanyData == null) return null;
            return new OrderPoolDTO()
            {
                CreatedBy = "user",
                CompanyID = comapanyData.CompanyId,
                OrderNumber = orderPool.OrderNumber,
                BrandID = comapanyData.BrandId,
                Price1 = orderPool.Price1.ToString(),
                Price2 = orderPool.Price2.ToString(),
                EAN = orderPool.CategoryText1,
                IsNew = true,
                SeasonID = orderPool.ProjectID,
                DeliveryDate = orderPool.ExpectedProductionDate,
                ID = orderPool.ID,
                ProviderReference = orderPool.ProviderCode2,
                Description = orderPool.CategoryText2,
                UN = orderPool.CategoryText3,
                Currency1 = orderPool.CategoryCode4,
                Currency2 = orderPool.CategoryCode5,
                ArticleCode = orderPool.ArticleCode,
                CreatedDate = orderPool.CreationDate,
                ExpectedProductionDate = orderPool.ExpectedProductionDate,
                ProcessedBy = orderPool.ProcessedBy,
                ProcessedDate = orderPool.ProcessedDate

            };
        }

        private OrderPoolDTO Map(OrderPool orderPool, List<OrderSectionCatalog> listOfSections, List<MadeInCatalog> listOfMadeIn)
        {
            if(orderPool == null) return null;

            var comapanyData = GetCompanyData(orderPool.ProjectID);

            if(comapanyData == null) return null;
            return new OrderPoolDTO()
            {
                CreatedBy = "user",
                CompanyID = comapanyData.CompanyId,
                OrderNumber = orderPool.OrderNumber,
                BrandID = comapanyData.BrandId,
                Price1 = orderPool.Price1?.ToString(),
                Price2 = orderPool.Price2?.ToString(),
                IsNew = true,
                SeasonID = orderPool.ProjectID,
                SectionID = GetSectionId(orderPool, listOfSections),
                ArticleQuality1 = orderPool.CategoryCode1,
                ArticleQuality2 = orderPool.CategoryCode2,
                MarketOriginID = GetMarketOfOrigin(orderPool, listOfMadeIn),
                DeliveryDate = orderPool.ExpectedProductionDate,
                ID = orderPool.ID,
                //ProviderCompanyID = orderPool.ProviderCode1, 
                ProviderReference = orderPool.ProviderCode2,
                SizeSetName = orderPool.CategoryText2,
                SectionName = orderPool.CategoryText1 == null ? "" : orderPool.CategoryText1,
                SizeCategory = $"{orderPool.CategoryText2} / {orderPool.CategoryText3} / {orderPool.CategoryText4}",
                ArticleCode = orderPool.ArticleCode,
                CreatedDate = orderPool.CreationDate,
                ProviderLocationName = GetProviderLocationName(orderPool.ProviderCode2, comapanyData.CompanyId)
            };
        }

        private string GetProviderLocationName(string providerCode2, int companyId)
        {
            if(string.IsNullOrEmpty(providerCode2.Trim()))
                return string.Empty;

            var countryName = (from cp in ctx.CompanyProviders
                               join l in ctx.Locations on cp.DefaultProductionLocation equals l.ID
                               join co in ctx.Countries on l.CountryID equals co.ID
                               where cp.ClientReference == providerCode2 && cp.CompanyID == companyId
                               select co.Name).FirstOrDefault();
            return countryName == null ? string.Empty : countryName;
        }

        private int GetMarketOfOrigin(OrderPool orderPool)
        {
            var catalog = ctx.Catalogs.FirstOrDefault(c => c.ProjectID == orderPool.ProjectID && c.Name == MADE_IN);
            if(catalog == null) return ID_NOTFOUND;
            var catalogData = catalogDataRepository.GetList(catalog.CatalogID);
            if(string.IsNullOrEmpty(catalogData)) return ID_NOTFOUND;

            var listOfMadeIn = JsonConvert.DeserializeObject<List<MadeInCatalog>>(catalogData);

            if(listOfMadeIn == null) return ID_NOTFOUND;

            int madeInId = listOfMadeIn.Any(s => s.Spanish.ToUpper().Trim().Contains(orderPool.CategoryText5.ToUpper().Trim()))
                 ? listOfMadeIn.First(s => s.Spanish.ToUpper().Trim().Contains(orderPool.CategoryText5.ToUpper().Trim())).ID
                 : ID_NOTFOUND;

            return madeInId;
        }

        private int GetMarketOfOrigin(OrderPool orderPool, List<MadeInCatalog> listOfMadeIn)
        {
            if(listOfMadeIn == null) return ID_NOTFOUND;

            int madeInId = listOfMadeIn.Any(s => s.Spanish.ToUpper().Trim().Contains(orderPool.CategoryText5.ToUpper().Trim()))
                 ? listOfMadeIn.First(s => s.Spanish.ToUpper().Trim().Contains(orderPool.CategoryText5.ToUpper().Trim())).ID
                 : ID_NOTFOUND;

            return madeInId;
        }

        private int GetSectionId(OrderPool orderPool, List<OrderSectionCatalog> listOfSections)
        {
            if(string.IsNullOrEmpty(orderPool.CategoryText1))
                return -1;
            if(listOfSections == null) return ID_NOTFOUND;

            int sectionId = listOfSections.Any(s => s.Name.ToUpper().Trim() == orderPool.CategoryText1.ToUpper().Trim())
                     ? listOfSections.First(s => s.Name.ToUpper().Trim() == orderPool.CategoryText1.ToUpper().Trim()).ID
                     : ID_NOTFOUND;
            return sectionId;
        }

        private List<MadeInCatalog> GetListOfMadeIn(OrderPool orderPool)
        {
            var catalog = ctx.Catalogs.FirstOrDefault(c => c.ProjectID == orderPool.ProjectID && c.Name == MADE_IN);
            if(catalog == null) return null;
            var catalogData = catalogDataRepository.GetList(catalog.CatalogID);
            if(string.IsNullOrEmpty(catalogData)) return null;

            return JsonConvert.DeserializeObject<List<MadeInCatalog>>(catalogData);
        }
        private List<OrderSectionCatalog> GetListOfSections(OrderPool orderPool)
        {
            var catalog = ctx.Catalogs.FirstOrDefault(c => c.ProjectID == orderPool.ProjectID && c.Name == ORDER_SECTIONS);
            if(catalog == null) return null;
            var catalogData = catalogDataRepository.GetList(catalog.CatalogID);
            if(string.IsNullOrEmpty(catalogData)) return null;

            return JsonConvert.DeserializeObject<List<OrderSectionCatalog>>(catalogData);
        }

        private int GetSectionId(OrderPool orderPool)
        {
            if(string.IsNullOrEmpty(orderPool.CategoryText1))
                return -1;

            var catalog = ctx.Catalogs.FirstOrDefault(c => c.ProjectID == orderPool.ProjectID && c.Name == ORDER_SECTIONS);
            if(catalog == null) return ID_NOTFOUND;
            var catalogData = catalogDataRepository.GetList(catalog.CatalogID);
            if(string.IsNullOrEmpty(catalogData)) return ID_NOTFOUND;

            var listOfSections = JsonConvert.DeserializeObject<List<OrderSectionCatalog>>(catalogData);
            if(listOfSections == null) return ID_NOTFOUND;

            int sectionId = listOfSections.Any(s => s.Name.ToUpper().Trim() == orderPool.CategoryText1.ToUpper().Trim())
                 ? listOfSections.First(s => s.Name.ToUpper().Trim() == orderPool.CategoryText1.ToUpper().Trim()).ID
                 : ID_NOTFOUND;

            return sectionId;
        }

        private CompanyData GetCompanyData(int projectId)
        {

            var query = from p in ctx.Projects
                        join b in ctx.Brands on p.BrandID equals b.ID
                        join c in ctx.Companies on b.CompanyID equals c.ID
                        where p.ID == projectId
                        select new CompanyData()
                        {
                            CompanyId = c.ID,
                            CompanyName = c.Name,
                            BrandId = b.ID,
                            BrandName = b.Name,
                            ProjectId = p.ID,
                            ProjectName = p.Name,
                        };
            return query.FirstOrDefault();
        }



        public List<OrderPoolDTO> GetOrdersByCompanyId(int companyid, int projectid)
        {
            var dictionary = new Dictionary<string, OrderPool>();
            var orders = ctx
            .OrderPools
            .Join(ctx.CompanyProviders, op => op.ProviderCode2, pv => pv.ClientReference, (op, pv) => new { OrderPool = op, Provider = pv })
            .Join(ctx.Projects, j1 => j1.OrderPool.ProjectID, pj => pj.ID, (j1, pj) => new { j1.OrderPool, j1.Provider, Project = pj })
            .Join(ctx.Brands, j2 => j2.Project.BrandID, br => br.ID, (j2, br) => new { j2.OrderPool, j2.Provider, j2.Project, Brand = br })

            //.Where(op => op.ProviderCode2 == companyid.ToString() && op.ProjectID == projectid)
            .Where(w => w.Provider.ProviderCompanyID == companyid && w.Provider.CompanyID == w.Brand.CompanyID)
            .Select(s => s.OrderPool)
            .ToList();
            foreach(var order in orders)
            {
                if(order.DeletedDate == null)
                    _ = dictionary.TryAdd($"{order.ArticleCode}--{order.OrderNumber}", order);
            }



            if(orders == null || !orders.Any())
                return null;
            else
            {
                var listOfSections = GetListOfSections(orders.FirstOrDefault());
                var listOfMadeIn = GetListOfMadeIn(orders.FirstOrDefault());
                var ordersHeader = dictionary.Select(x => MapBarca(x.Value, listOfSections, listOfMadeIn)).ToList();
                if(ordersHeader == null)
                {
                    return null;
                }
                return ordersHeader.Select(x => GetBarcaOrderSizeInfo(x, orders.Where(o => o.OrderNumber == x.OrderNumber && o.ArticleCode == x.ArticleCode && o.DeletedDate is null))).ToList();

            }
        }

        public List<OrderPoolDTO> GetOrdersByProject(int projectid)
        {
            var dictionary = new Dictionary<string, OrderPool>();
            var orders = ctx
            .OrderPools
            .Where(op => op.ProjectID == projectid)
            .ToList();
            foreach(var order in orders)
            {
                if(order.DeletedDate == null)
                    _ = dictionary.TryAdd($"{order.ArticleCode}--{order.OrderNumber}-{order.CreationDate.ToString()}", order);
            }
            if(orders == null || !orders.Any())
                return null;
            else
            {
                var listOfSections = GetListOfSections(orders.FirstOrDefault());
                var listOfMadeIn = GetListOfMadeIn(orders.FirstOrDefault());
                var ordersHeader = dictionary.Select(x => MapBarca(x.Value, listOfSections, listOfMadeIn)).ToList();
                if(ordersHeader == null)
                {
                    return null;
                }
                return ordersHeader.Select(x => GetBarcaOrderSizeInfo(x, orders.Where(o => o.OrderNumber == x.OrderNumber
                                                                                        && o.ArticleCode == x.ArticleCode
                                                                                        && o.CreationDate == x.CreatedDate
                                                                                        && o.DeletedDate == null))).ToList();
            }
        }

        public IOrderPool CheckIfExist(IOrderPool order)
        {
            // for barça
            //var found = (from pool in ctx.OrderPools
            //            where pool.ProjectID == order.ProjectID
            //            && pool.DeletedDate == null
            //            && pool.OrderNumber == order.OrderNumber
            //            && pool.ProviderCode2 == order.ProviderCode2
            //            && pool.CategoryText1 == order.CategoryText1
            //            && pool.CreationDate == order.CreationDate
            //            select pool).FirstOrDefault();
            //return found;
            var found = ctx.OrderPools
                .AsNoTracking()
                .Where(w => order.ProjectID > 0 || w.ProjectID == order.ProjectID)
                .Where(w => order.DeletedDate == null)
                .Where(w => string.IsNullOrEmpty(order.OrderNumber) || w.OrderNumber == order.OrderNumber)
                .Where(w => string.IsNullOrEmpty(order.ProviderCode2) || w.ProviderCode2 == order.ProviderCode2)
                // only for Barça
                .Where(w => string.IsNullOrEmpty(order.CategoryText1) || w.CategoryText1 == order.CategoryText1)
                .Where(w => w.CreationDate == order.CreationDate)
                .FirstOrDefault();

            return found;
        }


        private class CompanyData
        {
            internal int CompanyId { get; set; }
            internal string CompanyName { get; set; }
            internal int BrandId { get; set; }
            internal string BrandName { get; set; }
            internal int ProjectId { get; set; }
            internal string ProjectName { get; set; }
        }
    }




    public class OrderSectionCatalog
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public string Code { get; set; }
    }

    public class OrderComparer : IEqualityComparer<OrderPool>
    {
        public bool Equals(OrderPool x, OrderPool y)
        {
            if(x == null || y == null)
                return false;

            return x.OrderNumber == y.OrderNumber && x.ArticleCode == y.ArticleCode;
        }

        public int GetHashCode(OrderPool obj)
        {
            return HashCode.Combine(obj.OrderNumber, obj.ArticleCode);
        }
    }

    public class MadeInCatalog
    {
        private string spanish;
        public int ID { get; set; }
        public string Spanish
        {
            get
            {
                return spanish;
            }
            set
            {
                spanish = value;

            }
        }
        public string ES
        {
            get
            {
                return spanish;
            }
            set
            {
                spanish = value;
            }
        }
    }

}

