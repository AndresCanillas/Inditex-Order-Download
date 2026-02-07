using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using WebLink.Contracts;

namespace WebLink.Contracts.Models
{
	public class AddressRepository : GenericRepository<IAddress, Address>, IAddressRepository
	{
		public AddressRepository(IFactory factory)
			: base(factory, (ctx)=>ctx.Addresses)
		{
		}


		protected override string TableName { get => "Addresses"; }


		protected override void UpdateEntity(PrintDB ctx, IUserData userData, Address entity, IAddress data)
		{
			entity.AddressLine1 = data.AddressLine1;
			entity.AddressLine2 = data.AddressLine2;
			entity.CityOrTown = data.CityOrTown;
			entity.StateOrProvince = data.StateOrProvince;
			entity.Country = data.Country;
            entity.CountryID = data.CountryID;
            entity.Default = data.Default;
			entity.ZipCode = data.ZipCode;
            entity.Notes = data.Notes;
            entity.Name = data.Name;
            entity.AddressLine3 = data.AddressLine3;
            entity.Telephone1 = data.Telephone1;
            entity.Telephone2 = data.Telephone2;
            entity.Email1 = data.Email1;
            entity.Email2 = data.Email2;
            entity.BusinessName1 = data.BusinessName1;
            entity.BusinessName2 = data.BusinessName2;
            // TODO: required especial rol to modifiy this properties 
            entity.SyncWithSage = data.SyncWithSage;
            entity.SageRef = data.SageRef;
            entity.SageProvinceCode = data.SageProvinceCode;

        }


        public void AddToCompanyAddress(int companyId, int id)
        {
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				AddToCompanyAddress(ctx, companyId, id);
			}
		}


		public void AddToCompanyAddress(PrintDB ctx, int companyId, int id)
		{
			var userData = factory.GetInstance<IUserData>();
			ctx.CompanyAddresses.Add(new CompanyAddress() { CompanyID = companyId, AddressID = id, CreatedBy = userData.UserName, CreatedDate = DateTime.Now });
			ctx.SaveChanges();
		}


		public List<IAddress> GetByCompany(int id)
        {
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetByCompany(ctx, id);
			}
        }


		public List<IAddress> GetByCompany(PrintDB ctx, int id)
		{
			return new List<IAddress>(
				from a in ctx.Addresses
				join c in ctx.CompanyAddresses on a.ID equals c.AddressID
				where c.CompanyID == id
				select a)
				.OrderByDescending(x => x.Default)
				.ThenBy(x => x.Name)
				.ToList();
		}


		public IAddress GetDefaultByCompany(int id)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetDefaultByCompany(ctx, id);
			}
		}


		public IAddress GetDefaultByCompany(PrintDB ctx, int id)
		{
			return (from a in ctx.Addresses
					join c in ctx.CompanyAddresses on a.ID equals c.AddressID
					where c.CompanyID == id
					orderby a.Default descending
					select a).AsNoTracking().FirstOrDefault();
			// use GetByCompany methos  is a better option, make test and update this method
			//return GetByCompany(id).FirstOrDefault();
		}


		public void SetDefaultAddress(int companyId, int id, bool isDefault)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				SetDefaultAddress(ctx, companyId, id, isDefault);
			}
		}


		public void SetDefaultAddress(PrintDB ctx, int companyId, int id, bool isDefault)
		{
			var currentAddress = (from a in ctx.Addresses
								  join c in ctx.CompanyAddresses on a.ID equals c.AddressID
								  where c.CompanyID.Equals(companyId) && a.Default.Equals(true)
								  select a
							  ).FirstOrDefault();

			if (currentAddress != null)
			{
				currentAddress.Default = false;
				Update(ctx, currentAddress);
			}

			if (isDefault)
			{
				var newDefault = GetByID(id);
				newDefault.Default = isDefault;
				Update(ctx, newDefault);
			}
		}
    }
}
