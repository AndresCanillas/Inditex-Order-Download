using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;

namespace WebLink.Contracts.Models
{
    public class ProviderRepository : IProviderRepository
    {
        private IFactory factory;
        private IDBConnectionManager connManager;
        private IEventQueue events;

        public ProviderRepository(
            IFactory factory,
            IDBConnectionManager connManager,
            IEventQueue events
            )
        {
            this.factory = factory;
            this.connManager = connManager;
            this.events = events;
        }


        public ICompanyProvider GetByID(int providerRecordID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByID(ctx, providerRecordID);
            }
        }


        public ICompanyProvider GetByID(PrintDB ctx, int providerRecordID)
        {
            return ctx.CompanyProviders.Find(providerRecordID);
        }

        public List<ProviderDTO> GetByCompanyIDWithArticleDetails(int companyid)
        {
            var providers = GetByCompanyID(companyid);
            var printDB = factory.GetInstance<PrintDB>();
            foreach(var provider in providers)
            {
                var articles = from ad in printDB.ArticleDetails
                               join ar in printDB.Articles on ad.ArticleID equals ar.ID
                               where ad.CompanyID == provider.ProviderCompanyID
                               select (new ArticleDetailDTO()
                               {
                                   ID = ad.ID,
                                   ArticleId = ad.ArticleID,
                                   Article = ar.Name,
                                   CompanyId = provider.ProviderCompanyID,
                               });
                provider.ArticleDetailDTO = new List<ArticleDetailDTO>();
                if(articles != null && articles.Any())
                {

                    provider.ArticleDetailDTO.AddRange(articles);
                }

            }
            return providers;

        }

        [Obsolete("Duplicated Method")]
        public List<ProviderDTO> GetByCompanyIDME(int companyid)
        {
            using(var conn = connManager.OpenWebLinkDB())
            {
                return conn.Select<ProviderDTO>(@"
                    SELECT 
                    p.ID
                    , p.CompanyID
                    , p.ProviderCompanyID
                    , c.Name as CompanyName
                    , c.CompanyCode
                    , p.ClientReference
                    , p.DefaultProductionLocation
                    , p.Instructions
                    , p.SLADays
                    , l.Name as LocationName
                    , b.Name BillingInfoName
                    , b.ID BillingInfoId
                    , CAST( (CASE WHEN p.CreatedDate != p.UpdatedDate THEN 1
                         ELSE 0 
                         END ) AS BIT) AS IsVeryfied
										
                    FROM CompanyProviders p
                    JOIN Companies c				ON p.ProviderCompanyID = c.ID
                    LEFT OUTER JOIN Locations l		ON p.DefaultProductionLocation = l.ID
                    LEFT OUTER JOIN BillingsInfo b	ON b.ID = p.BillingInfoID
                    WHERE p.CompanyID = @companyid
                    ORDER BY CompanyName
                ", companyid);
            }
        }


        public List<ProviderDTO> GetByCompanyID(int companyid)
        {
            var userData = factory.GetInstance<IUserData>();
            if(!userData.IsIDT) return new List<ProviderDTO>(); // check permissions here is unnecesary other programmer create a by pass GetByCompanyIDME
            using(var conn = connManager.OpenWebLinkDB())
            {
                return conn.Select<ProviderDTO>(@"
                SELECT 
                p.ID
                , p.CompanyID
                , p.ProviderCompanyID
                , c.Name as CompanyName
                , c.CompanyCode
                , p.ClientReference
                , p.DefaultProductionLocation
                , p.Instructions
                , p.SLADays
                , l.Name as LocationName
                , b.Name BillingInfoName
                , b.ID BillingInfoId
                , CAST( (CASE WHEN p.CreatedDate != p.UpdatedDate THEN 1
                    ELSE 0 
                    END ) AS BIT) AS IsVeryfied
										
                FROM CompanyProviders p
                JOIN Companies c				ON p.ProviderCompanyID = c.ID
                LEFT OUTER JOIN Locations l		ON p.DefaultProductionLocation = l.ID
                LEFT OUTER JOIN BillingsInfo b	ON b.ID = p.BillingInfoID
                WHERE p.CompanyID = @companyid
                    ORDER BY CompanyName
                ", companyid);
            }
        }

        [Obsolete("This Method Hide multiple reference for the same provider, return first found")]
        public CompanyProvider GetByProviderID(int companyid, int providerid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByProviderID(ctx, companyid, providerid);
            }
        }

        [Obsolete("This Method Hide multiple reference for the same provider, return first found")]
        public CompanyProvider GetByProviderID(PrintDB ctx, int companyid, int providerid)
        {
            return ctx.CompanyProviders.FirstOrDefault(p => p.CompanyID == companyid && p.ProviderCompanyID == providerid);
        }

        [Obsolete("This Method Hide multiple reference for the same provider, return first found")]
        public ProviderDTO GetProvider(int companyid, int companyProviderID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetProvider(ctx, companyid, companyProviderID);
            }
        }

        [Obsolete("This Method Hide multiple reference for the same provider, return first found")]
        public ProviderDTO GetProvider(PrintDB ctx, int companyid, int companyProviderID)
        {
            var result = ctx.ProviderTrewView
                .Join(ctx.Companies, p => p.CompanyID, c => c.ID, (prov, c) => new { Provider = prov, Company = c })
                .Join(ctx.Locations, j1 => j1.Provider.DefaultProductionLocationID, l => l.ID, (j1, loc) => new { j1.Provider, j1.Company, ProductionLocation = loc })
                .Join(ctx.Locations, j2 => j2.Provider.DefaultBillingLocationID, l => l.ID, (j2, loc) => new { j2.Provider, j2.Company, j2.ProductionLocation, BillingLocation = loc })
                //.Where(w => w.Provider.TopParentID.Equals(companyid))
                .Where(w => w.Provider.Parents.Contains($".{companyid}."))
                .Where(w => w.Provider.CompanyID.Equals(companyProviderID))
                .Select(s => new ProviderDTO()
                {
                    ProviderCompanyID = s.Provider.CompanyID,
                    CompanyName = s.Company.Name,
                    CompanyCode = s.Company.CompanyCode,
                    ClientReference = s.Provider.ClientReference,
                    DefaultProductionLocation = s.ProductionLocation.ID,
                    LocationName = s.ProductionLocation.Name
                }).FirstOrDefault();

            if(result == null)
                throw new InvalidOperationException($"Company {companyProviderID} is not a valid provider for company {companyid}");

            return result;
        }


        public ICompanyProvider GetProviderBy(int companyId, int providerCompanyID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetProviderBy(ctx, companyId, providerCompanyID);
            }
        }


        public ICompanyProvider GetProviderBy(PrintDB ctx, int companyId, int providerCompanyID)
        {
            var q = ctx.CompanyProviders
            .FirstOrDefault(w => w.CompanyID == companyId && w.ProviderCompanyID == providerCompanyID);

            //if (q == null)
            //{
            //    var providerIds = ctx.CompanyProviders.Where(x => x.CompanyID.Equals(companyId)).Select(y => y.ProviderCompanyID).ToList();
            //    q = ctx.CompanyProviders.FirstOrDefault(x => providerIds.Contains(x.CompanyID) && x.ProviderCompanyID == providerCompanyID);
            //}

            return q;
        }


        public ICompanyProvider GetProviderByClientReference(int companyId, string clientReference)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetProviderByClientReference(ctx, companyId, clientReference);
            }
        }

        public ICompanyProvider GetProviderByClientReference(PrintDB ctx, int companyId, string clientReference)
        {
            var range = new List<int>() { companyId };
            var resultFound = false;
            var currentLevel = 0;

            ICompanyProvider found = null;

            while(!resultFound && currentLevel < 3)
            {
                currentLevel++;

                var pv = ctx.CompanyProviders
                    .Where(w => range.Any(a => a == w.CompanyID))
                    .ToList();

                var multipleWithSameReference = pv
                    .Where(w => w.ClientReference == clientReference);

                if(multipleWithSameReference.Count() > 1)
                    throw new CompanyCodeNotFoundException($"Company with ClientReference [{clientReference}] found multiple times.", clientReference);

                found = multipleWithSameReference.FirstOrDefault();// Count == 1

                if(found != null) resultFound = true; // stop search

                range.AddRange(pv.Select(s => s.ProviderCompanyID));

            }

            if(found == null)
                throw new CompanyCodeNotFoundException($"Company with ClientReference [{clientReference}] could not be found.", clientReference);

            return found;
        }



        public void UpdateProvider(ProviderDTO data)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                UpdateProvider(ctx, data);
            }
        }


        public void UpdateProvider(PrintDB ctx, ProviderDTO data)
        {
            var provider = ctx.CompanyProviders.Where(p => p.ID == data.ID).SingleOrDefault();
            if(provider != null)
            {

                provider.ProviderCompanyID = data.ProviderCompanyID;
                provider.ClientReference = data.ClientReference;
                provider.DefaultProductionLocation = data.DefaultProductionLocation;
                provider.Instructions = data.Instructions;
                provider.SLADays = data.SLADays;
                //provider.BillingInfoID = data.BillingInfoId;
                provider.UpdatedDate = DateTime.Now;
                ctx.SaveChanges();
                events.Send(new EntityEvent(provider.CompanyID, provider, DBOperation.Update));
            }
            else throw new Exception($"Provider {data.ID} was not found.");
        }


        public int AddProviderToCompany(int companyid, ProviderDTO provider)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return AddProviderToCompany(ctx, companyid, provider);
            }
        }


        public int AddProviderToCompany(PrintDB ctx, int companyid, ProviderDTO provider)
        {
            CompanyProvider p = new CompanyProvider()
            {
                CompanyID = companyid,
                ProviderCompanyID = provider.ProviderCompanyID,
                ClientReference = provider.ClientReference,
                DefaultProductionLocation = provider.DefaultProductionLocation,
                Instructions = provider.Instructions,
                SLADays = provider.SLADays,
                CreatedDate = DateTime.Now
            };
            ctx.CompanyProviders.Add(p);

            // mark compnay as a broker
            var company = ctx.Companies.FirstOrDefault(f => f.ID == companyid);
            company.IsBroker = true;

            ctx.SaveChanges();
            events.Send(new EntityEvent(p.CompanyID, p, DBOperation.Update));

            return p.ID;
        }


        public void RemoveProviderFromCompany(int providerid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                RemoveProviderFromCompany(ctx, providerid);
            }
        }


        public void RemoveProviderFromCompany(PrintDB ctx, int providerid)
        {
            var provider = ctx.CompanyProviders.Where(p => p.ID == providerid).SingleOrDefault();


            if(provider != null)
            {
                using(var transaction = ctx.Database.BeginTransaction())
                {
                    ctx.Database.ExecuteSqlCommand($@"DELETE FROM ARTICLEDETAILS WHERE COMPANYID={provider.ProviderCompanyID}
                         AND ARTICLEID IN (SELECT A.ID FROM Articles A
                         INNER JOIN Projects P ON P.ID = A.ProjectID 
                         INNER JOIN Brands B ON B.ID = P.BrandID 
                         INNER JOIN Companies C ON C.ID = B.CompanyID
                         WHERE C.ID={provider.CompanyID})");
                    ctx.CompanyProviders.Remove(provider);
                    ctx.SaveChanges();
                    transaction.Commit();
                }




                events.Send(new EntityEvent(provider.CompanyID, provider, DBOperation.Delete));

                if(ctx.CompanyProviders.Count(w => w.CompanyID == provider.CompanyID) == 0)
                {
                    var company = ctx.Companies.FirstOrDefault(f => f.ID == provider.CompanyID);
                    company.IsBroker = false;
                    ctx.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Testing Method, only used for ERP
        /// </summary>
        /// <param name="companyId"></param>
        /// <param name="billingToId"></param>
        /// <returns></returns>
        public ProviderTreeView GetBillingProviderInfo(int companyId, int billingToId)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetBillingProviderInfo(ctx, companyId, billingToId);
            }
        }


        public ProviderTreeView GetBillingProviderInfo(PrintDB ctx, int companyId, int billingToId)
        {

            if(companyId.Equals(billingToId))
            {
                var q = ctx.Companies.Where(w => w.ID == billingToId).Select(f => new ProviderTreeView()
                {
                    ClientReference = f.ClientReference,
                    CompanyID = f.ID,
                    Currency = string.Empty,
                    DefaultBillingLocationID = f.DefaultProductionLocation,
                    DefaultProductionLocationID = f.DefaultProductionLocation,
                    Name = f.Name,
                    ParentCompanyID = null,
                    Parents = string.Empty,
                    ProviderRecordID = 0,
                    SLADays = 0,
                    TopParentID = null
                }).First();

                return q;

            }
            else
            {
                var r = ctx.ProviderTrewView
                //.Join(ctx.Companies, tv => tv.CompanyID, c => c.ID, (ptv, cmp) => new { ProviderTreeView = ptv, Company = cmp })
                .Where(w => (w.CompanyID.Equals(billingToId) && w.Parents.Contains($".{companyId}.")))
                .Select(s => s).FirstOrDefault();
                return r;
            }
        }

        public string GetProviderLocationName(string clientReference, int companyid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetProviderLocationName(ctx,clientReference,companyid);  
            }
        }

        public string GetProviderLocationName(PrintDB ctx, string clientReference, int companyid)
        {
            if(string.IsNullOrEmpty(clientReference.Trim()))
                return string.Empty;

            var countryName = (from cp in ctx.CompanyProviders
                               join l in ctx.Locations on cp.DefaultProductionLocation equals l.ID
                               join co in ctx.Countries on l.CountryID equals co.ID
                               where cp.ClientReference == clientReference && cp.CompanyID == companyid
                               select co.Name).FirstOrDefault();
            return countryName == null ? string.Empty : countryName;
        }
    }
}

