using Service.Contracts;
using Service.Contracts.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebLink.Contracts.Models
{
    public class LocationRepository : GenericRepository<ILocation, Location>, ILocationRepository
    {
        public LocationRepository(IFactory factory)
            : base(factory, (ctx) => ctx.Locations)
        {
        }


        protected override string TableName { get => "Locations"; }


        protected override void UpdateEntity(PrintDB ctx, IUserData userData, Location actual, ILocation data)
        {
            actual.Name = data.Name;
            actual.DeliverTo = String.IsNullOrWhiteSpace(data.DeliverTo) ? "" : data.DeliverTo;
            actual.AddressLine1 = String.IsNullOrWhiteSpace(data.AddressLine1) ? "" : data.AddressLine1;
            actual.AddressLine2 = String.IsNullOrWhiteSpace(data.AddressLine2) ? "" : data.AddressLine2;
            actual.CityOrTown = String.IsNullOrWhiteSpace(data.CityOrTown) ? "" : data.CityOrTown;
            actual.StateOrProvince = String.IsNullOrWhiteSpace(data.StateOrProvince) ? "" : data.StateOrProvince;
            actual.ZipCode = String.IsNullOrWhiteSpace(data.ZipCode) ? "" : data.ZipCode;
            actual.Country = String.IsNullOrWhiteSpace(data.Country) ? "" : data.Country;
            actual.CountryID = data.CountryID;
            actual.MaxNotEncodingQuantity = data.MaxNotEncodingQuantity;
            actual.FscCode = data.FscCode;
            if (userData.Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService))
            {
                actual.EnableERP = data.EnableERP;
                actual.ERPCurrency = data.ERPCurrency;
                actual.FactoryCode = data.FactoryCode;
                actual.Holidays = data.Holidays;
                actual.WorkingDays = data.WorkingDays;
                actual.CutoffTime = data.CutoffTime;
            }
        }


        public List<ILocation> GetByCompanyID(int companyid)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return new List<ILocation>(All(ctx).Where(p => p.CompanyID == companyid));
            }
        }


        public List<ILocation> GetByCompanyID(PrintDB ctx, int companyid)
        {
            return new List<ILocation>(
                All(ctx).Where(p => p.CompanyID == companyid)
            );
        }

        public List<ILocation> GetIDTFactories()
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetIDTFactories(ctx);
            }

        }

        public List<ILocation> GetIDTFactories(PrintDB ctx)
        {
            var q = ctx.Locations.Where(w => w.CompanyID == 1);

            return q.ToList<ILocation>();
        }

        public IEnumerable<ILocation> GetFactoriesInUseFor(IUserData userData)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetFactoriesInUseFor(ctx, userData).ToList();
            }
        }

        public IEnumerable<ILocation> GetFactoriesInUseFor(PrintDB ctx, IUserData userData)
        {
            var companyID = 0;

            if (userData.SelectedCompanyID != 1)
            {
                companyID = userData.SelectedCompanyID;
            }

            var q = ctx.CompanyOrders
                .Join(ctx.Locations,
                ord => ord.LocationID,
                loc => loc.ID,
                (o, l) => new { Order = o, Location = l })
                .Where(w => w.Order.SendToCompanyID == companyID || w.Order.CompanyID == companyID)
                .Where(w => w.Order.SendToCompanyID != 45);


            return q.Select(s => s.Location).Distinct().ToList();
        }

    }
}
