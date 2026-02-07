using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using Org.BouncyCastle.Crypto;
using Service.Contracts;
using Service.Contracts.PrintCentral;
using Services.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using Address = WebLink.Contracts.Models.Address;

namespace WebLink.Controllers
{
	[Authorize]
	public class AddressesController : Controller
	{
		private IAddressRepository repo;
        private IUserData userData;
        private ILocalizationService g;
		private ILogService log;
        private IFactory factory;


        public AddressesController(
            IAddressRepository repo,
            IUserData userData,
            ILocalizationService g,
            ILogService log,
            IFactory factory)
        {
            this.repo = repo;
            this.userData = userData;
            this.g = g;
            this.log = log;
            this.factory = factory;
        }

        [HttpPost, Route("/addresses/insert/{companyId}")]
		public OperationResult Insert(int companyId, [FromBody]Contracts.Models.Address data)
		{
			try
			{
                var objectData = repo.Insert(data);
                if (objectData != null)
                {
                    repo.AddToCompanyAddress(companyId, objectData.ID);
                }
                return new OperationResult(true, g["Address Created!"], objectData);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

        private int GetCompanyFromSupplierID(int supplierID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
               var cp =  ctx.CompanyProviders.FirstOrDefault(s => s.ID == supplierID);
               if(cp != null)
                {
                    return  ctx.Companies.FirstOrDefault(c => c.ID == cp.ProviderCompanyID).ID; 
                }
            }
            return 0; 
        }

        [HttpPost, Route("/addresses/GetSupplierAddress/{supplierID}")]
		public async Task<int> GetSupplierAddress([FromBody] Service.Contracts.PrintCentral.Address address, int supplierID)
		{
			try
			{

                var companyID = GetCompanyFromSupplierID(supplierID);
                if(companyID == 0) return -1; 
				var addresses = repo.GetByCompany(companyID);
				if (addresses == null || !addresses.Any())
				{
					(bool flowControl, int value) = SaveAddress(address, companyID);
					if (!flowControl)
					{
						return value;
					}
				}
				else
				{

                    //var adreesesCount = addresses.Count(a =>
                    //    a.ZipCode.ToUpper().Trim() == address.ZipCode.ToUpper().Trim() &&
                    //    a.CountryID == address.CountryID &&
                    //    a.CityOrTown.ToUpper().Trim() == address.CityOrTown.ToUpper().Trim() &&
                    //    //		a.Notes == address.Notes &&
                    //    a.AddressLine1.ToUpper().Trim() == address.AddressLine1.ToUpper().Trim() &&
                    //    a.AddressLine2.ToUpper().Trim() == address.AddressLine3.ToUpper().Trim() &&
                    //    a.AddressLine3.ToUpper().Trim() == address.AddressLine3.ToUpper().Trim() &&
                    //    a.Name.ToUpper().Trim() == address.Name.ToUpper().Trim() &&
                    //    //a.BusinessName2 == address.BusinessName2 &&
                    //    a.BusinessName1.ToUpper().Trim() == address.BusinessName1.ToUpper().Trim()); 


                    // Search for existing address matching the input address
                    var existingAddress = addresses.FirstOrDefault(a =>
                        (a.ZipCode?.ToUpper().Trim() ?? string.Empty) == (address.ZipCode?.ToUpper().Trim() ?? string.Empty) &&
                        a.CountryID == address.CountryID &&
                        (a.CityOrTown?.ToUpper().Trim() ?? string.Empty) == (address.CityOrTown?.ToUpper().Trim() ?? string.Empty) &&
                        (a.AddressLine1?.ToUpper().Trim() ?? string.Empty) == (address.AddressLine1?.ToUpper().Trim() ?? string.Empty) &&
                        (a.AddressLine2?.ToUpper().Trim() ?? string.Empty) == (address.AddressLine2?.ToUpper().Trim() ?? string.Empty) &&
                        (a.AddressLine3?.ToUpper().Trim() ?? string.Empty) == (address.AddressLine3?.ToUpper().Trim() ?? string.Empty) &&
                        (a.Name?.ToUpper().Trim() ?? string.Empty) == (address.Name?.ToUpper().Trim() ?? string.Empty) &&
                        (a.BusinessName1?.ToUpper().Trim() ?? string.Empty) == (address.BusinessName1?.ToUpper().Trim() ?? string.Empty));

                    if (existingAddress != null)
					{
						return existingAddress.ID;
					}
					else
					{
						(bool flowControl, int value) = SaveAddress(address, companyID);
						if (!flowControl)
						{
							return value;
						}
					}
				}

				return -2;
			}
			catch (Exception ex)
			{
				log.LogException(ex);
                return -3;
			}
		}

        private (bool flowControl, int value) SaveAddress(Service.Contracts.PrintCentral.Address address, int companyID)
        {
            var objectData = repo.Insert(new Address()
            {
                AddressLine1 = address.AddressLine1,
                AddressLine2 = address.AddressLine2,
                AddressLine3 = address.AddressLine3,
                BusinessName1 = address.BusinessName1,
                BusinessName2 = address.BusinessName2,
                CityOrTown = address.CityOrTown,
                Country = address.Country,
                CountryID = address.CountryID,
                Default = address.Default,
                Email1 = address.Email1,
                Email2 = address.Email2,
                Name = address.Name,
                Notes = address.Notes,
                SageProvinceCode = address.SageProvinceCode,
                SageRef = address.SageRef,
                StateOrProvince = address.StateOrProvince,
                SyncWithSage = address.SyncWithSage,
                Telephone1 = address.Telephone1,
                Telephone2 = address.Telephone2,
                ZipCode = address.ZipCode
            });

            if(objectData != null)
            {
                repo.AddToCompanyAddress(companyID, objectData.ID);
                return (flowControl: false, value: objectData.ID);
            }

            return (flowControl: true, value: default);
        }

        [HttpPost, Route("/addresses/update")]
		public OperationResult Update([FromBody]Contracts.Models.Address data)
		{
			try
			{
                return new OperationResult(true, g["Address saved!"], repo.Update(data));
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/addresses/delete/{id}")]
		public OperationResult Delete(int id)
		{
			try
			{
                repo.Delete(id);
				return new OperationResult(true, g["Address Deleted!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpGet, Route("/addresses/getbyid/{id}")]
		public IAddress GetByID(int id)
		{
			try
			{
				return repo.GetByID(id);
			}
			catch(Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[HttpGet, Route("/addresses/getlist")]
		public List<IAddress> GetList()
		{
			try
			{
				return repo.GetList();
			}
			catch(Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[HttpGet, Route("/addresses/getbycompany/{id}")]
		public List<IAddress> GetByCompany(int id)
		{
			try
			{
				return repo.GetByCompany(id);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

        [HttpGet, Route("/addresses/getdefaultbycompany/{id}")]
        public IAddress GetDefaultByCompany(int id)
        {
            try
            {

				IAddress def = repo.GetDefaultByCompany(id);

				return def;
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpPost, Route("/addresses/setdefaultaddress/{companyId}/{id}/{isDefault}")]
        public OperationResult SetDefaultAddress(int companyId, int id, bool isDefault)
        {
            try
            {
                repo.SetDefaultAddress(companyId, id, isDefault);
                return new OperationResult(true, g["Default Address Updated"]);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }
    }
}