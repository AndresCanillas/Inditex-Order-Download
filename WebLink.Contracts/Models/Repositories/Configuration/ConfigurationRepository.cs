using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
	public class ConfigurationRepository: IConfigurationRepository
	{
		private IFactory factory;
        private IProviderRepository providerRepo;

        public ConfigurationRepository(IFactory factory, IProviderRepository providerRepo)
		{
			this.factory = factory;
            this.providerRepo = providerRepo;
		}


		/* ===========================================================================================================================================
		 * Gets the LocationID that should be used to produce an order. In this call: 
		 *		- companyid is the id of the company that issues the order (order.CompanyID)
		 *		- providerid is the id of the company that will receive the order (order.SendToCompanyID)
		 *		- projectid is the id of the project (order.ProjectID)
		 * 
		 * The production location (and the SLA) can be configured in different places in the system, this method will try to get this
		 * configuration from one table first, and if no configuration is found there, it will try a different table and so on, until the
		 * required configuration is found or fall back to a system wide default.
		 * 
		 * The order in which tables are queried determines which records have priority over the others, therefore, these queries should
		 * not be changed lightly.
		 * 
		 * The ProductionLocation is determined in the following order:
		 *		> IF the companyid and providerid are the same (in this case the order will be sent to the issuer himself, not to a vendor/provider), then:
		 *			> Query the Projects table (using projectid)
		 *			> If no production location is found in Projects, then Query Companies table (using companyid)
		 *			> If no production location is found in Companies, then return the first Smartdots location available
		 *			
		 *		> IF the companyid is different from the providerid (in this case the order will be sent to a different company, not the issuer), then:
		 *			> Query the CompanyProviders table (using the composed key: companyid/providerid)
		 *			> If no production location is found in CompanyProviders, then Query the Projects table (using projectid)
		 *			> If no production location is found in Projects, then Query Companies table (using companyid)
		 *			> If no production location is found in Companies, then return the first Smartdots location available
		 * =========================================================================================================================================== */
		public int FindProductionLocationID(int companyid, string clientReference, int projectid)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return FindProductionLocationID(ctx, companyid, clientReference, projectid);
			}
		}


		public int FindProductionLocationID(PrintDB ctx, int companyid, string clientReference, int projectid)
		{
			var project = ctx.Projects.Where(p => p.ID == projectid).Single();
			var company = ctx.Companies.Where(c => c.ID == companyid).Single();
			if (company.ClientReference == clientReference)
			{
				if (project.DefaultFactory.HasValue)
				{
					return project.DefaultFactory.Value;
				}
				else if (company.DefaultProductionLocation.HasValue)
				{
					return company.DefaultProductionLocation.Value;
				}
			}
			else
			{
				var provider = providerRepo.GetProviderByClientReference(ctx, companyid, clientReference);
				if (provider.DefaultProductionLocation.HasValue)
				{
					return provider.DefaultProductionLocation.Value;
				}
				else if (project.DefaultFactory.HasValue)
				{
					return project.DefaultFactory.Value;
				}
				else if (company.DefaultProductionLocation.HasValue)
				{
					return company.DefaultProductionLocation.Value;
				}
			}

			// Fallback location: First one that belongs to smartdots
			var location = ctx.Locations.Where(l => l.CompanyID == 1).Take(1).Single();
			return location.ID;
		}



		/* ===========================================================================================================================================
		 * Gets the Location (complete data contract) and the SLADays that should be used to produce an order. In this call: 
		 *		- companyid is the id of the company that issues the order (order.CompanyID)
		 *		- providerid is the id of the company that will receive the order (order.SendToCompanyID)
		 *		- projectid is the id of the project (order.ProjectID)
		 *		- SLADays is an output argument (will be defaulted to 7 if no SLA has been setup)
		 * 
		 * This is almost identical to the previos method, the main difference is that it returns the entiry location object and
		 * also has to return the SLADays as an output argument.
		 * 
		 * The SLA used will be the one setup in the table from which we take the ProductionLocation, if no SLA has been configured
		 * in that specific table, then 7 days is assumed.
		 * =========================================================================================================================================== */
		public ILocation GetProductionLocationAndSLA(int companyid, string clientReference, int projectid, out int SLADays)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetProductionLocationAndSLA(ctx, companyid, clientReference, projectid, out SLADays);
			}
		}


		public ILocation GetProductionLocationAndSLA(PrintDB ctx, int companyid, string clientReference, int projectid, out int SLADays)
		{
			SLADays = 7;
			var project = ctx.Projects.Where(p => p.ID == projectid).Single();
			var company = ctx.Companies.Where(c => c.ID == companyid).Single();
			if (company.ClientReference == clientReference)
			{
				if (project.DefaultFactory.HasValue)
				{
					if (project.SLADays.HasValue)
						SLADays = project.SLADays.Value;
					return ctx.Locations.FirstOrDefault(l => l.ID == project.DefaultFactory.Value);
				}
				else if (company.DefaultProductionLocation.HasValue)
				{
					if (company.SLADays.HasValue)
						SLADays = company.SLADays.Value;
					return ctx.Locations.FirstOrDefault(l => l.ID == company.DefaultProductionLocation.Value);
				}
			}
			else
			{
				var provider = providerRepo.GetProviderByClientReference(ctx, companyid, clientReference);

				if (provider.DefaultProductionLocation.HasValue)
				{
					if (provider.SLADays.HasValue)
						SLADays = provider.SLADays.Value;
					return ctx.Locations.FirstOrDefault(l => l.ID == provider.DefaultProductionLocation.Value);
				}
				else if (project.DefaultFactory.HasValue)
				{
					if (project.SLADays.HasValue)
						SLADays = project.SLADays.Value;
					return ctx.Locations.FirstOrDefault(l => l.ID == project.DefaultFactory.Value);
				}
				else if (company.DefaultProductionLocation.HasValue)
				{
					if (company.SLADays.HasValue)
						SLADays = company.SLADays.Value;
					return ctx.Locations.FirstOrDefault(l => l.ID == company.DefaultProductionLocation.Value);
				}
			}

			// Fallback location: First one that belongs to smartdots
			var location = ctx.Locations.Where(l => l.CompanyID == 1).Take(1).Single();
			return location;
		}


		public DateTime GetOrderDueDate(int companyid, string clientReference, int projectid)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetOrderDueDate(ctx, companyid, clientReference, projectid);
			}
		}


		public DateTime GetOrderDueDate(PrintDB ctx, int companyid, string clientReference, int projectid)
		{
			var productionLocation = GetProductionLocationAndSLA(ctx, companyid, clientReference, projectid, out var SLADays);

			// SLA calculation should take into account WorkWeekDays and Holidays, which need to be configured per Factory (productionLocation here).
			var weekWorkDays = productionLocation.WorkingDays > 0 ? (WorkWeekDays)productionLocation.WorkingDays : WorkWeekDays.Monday | WorkWeekDays.Tuesday | WorkWeekDays.Wednesday | WorkWeekDays.Thursday | WorkWeekDays.Friday;
			var definition = new[] { new { Name = "", Month = "", Day = "" } };
			var thisYearHolidays = new int[0];
			if (productionLocation.Holidays != null)
			{
				var holidays = JsonConvert.DeserializeAnonymousType(productionLocation.Holidays, definition);
				thisYearHolidays = holidays.Select(x => new DateTime(DateTime.Now.Year, int.Parse(x.Month), int.Parse(x.Day)).DayOfYear).ToArray();
			}

			// We also have a cutoff time (which is time since midnight, Server Time): If an order arrives after this threshold, then it will be treated
			// as if it had arrived the next day. NOTE: If this behavior is not required, then simply pass TimeSpan.Zero in the call to CalculateSLA...
			TimeSpan cutOffTime = productionLocation.CutoffTime != null ? TimeSpan.FromHours(double.Parse(productionLocation.CutoffTime.Split(":")[0])) : TimeSpan.FromHours(12);

			return CalculateSLA(DateTime.Now, SLADays, weekWorkDays, thisYearHolidays, cutOffTime);
		}

        public async Task<OrderParameters> GetOrderParametersAsync(PrintDB ctx, int companyid, string clientReference, int projectid)
        {
            // TODO: change name to providerID Parameter, is a SentToCompanyId Value, that is requried value
            var loc = GetProductionLocationAndSLA(ctx, companyid, clientReference, projectid , out var SLADays);
            //var productionLocationID = FindProductionLocationID(ctx, companyid, providerCompanyId.Value, projectid);

            if(loc == null)
            {
                throw new InvalidOperationException($"Production location is not properly setup. Company: {companyid}, project: {projectid}, clientReferece: {clientReference}.");
            }

            // TODO: Compare loc.ID with productionLocationID, maybe are the same value
            await Task.FromResult(1);// TODO: convert in to awaitable

            return new OrderParameters()
            {
                ProductionLocationID = loc.ID,
                SLADays = SLADays,
                DueDate = GetOrderDueDate(ctx, companyid, clientReference, projectid)
            };
        }

        [Obsolete("use GetOrderParametersAsync(PrintDB ctx, int companyid, int projectid, int? providerCompanyId)")]
		public async Task<OrderParameters> GetOrderParametersAsync_INTAKEVERSION_NOTESTED(PrintDB ctx, int companyid, int projectid, int? providerCompanyId)
		{
			int SLADays = 7;
			Location location = null;

			var project = await ctx.Projects.Where(p => p.ID == projectid).SingleAsync();
			var company = await ctx.Companies.Where(c => c.ID == companyid).SingleAsync();

			if (providerCompanyId == null || companyid == providerCompanyId)
			{
				if (project.DefaultFactory.HasValue)
				{
					if (project.SLADays.HasValue)
						SLADays = project.SLADays.Value;
					location = await ctx.Locations.FirstOrDefaultAsync(l => l.ID == project.DefaultFactory.Value);
				}
				else if (company.DefaultProductionLocation.HasValue)
				{
					if (company.SLADays.HasValue)
						SLADays = company.SLADays.Value;
					location = await ctx.Locations.FirstOrDefaultAsync(l => l.ID == company.DefaultProductionLocation.Value);
				}
			}
			else
			{
				var provider = providerRepo.GetProviderBy(ctx, companyid, providerCompanyId.Value);

				if (provider.DefaultProductionLocation.HasValue)
				{
					if (provider.SLADays.HasValue)
						SLADays = provider.SLADays.Value;
					location = await ctx.Locations.FirstOrDefaultAsync(l => l.ID == provider.DefaultProductionLocation.Value);
				}
				else if (project.DefaultFactory.HasValue)
				{
					if (project.SLADays.HasValue)
						SLADays = project.SLADays.Value;
					location = await ctx.Locations.FirstOrDefaultAsync(l => l.ID == project.DefaultFactory.Value);
				}
				else if (company.DefaultProductionLocation.HasValue)
				{
					if (company.SLADays.HasValue)
						SLADays = company.SLADays.Value;
					location = await ctx.Locations.FirstOrDefaultAsync(l => l.ID == company.DefaultProductionLocation.Value);
				}
			}

			// Fallback location: First one that belongs to smartdots
			if (location == null)
			{
				location = await ctx.Locations.Where(l => l.ID == 1).SingleAsync();
			}

			var dueDate = CalculateSLAForLocation(location, SLADays);

			return new OrderParameters()
			{
				ProductionLocationID = location.ID,
				SLADays = SLADays,
				DueDate = dueDate
			};
		}


        public async Task<OrderParameters> GetOrderParametersAsync(PrintDB ctx, int companyid, string clientReference, int projectid, int locationid)
        {
            // TODO: Ignore LocationID paremeter received, ClientReference is required for better aproach

            var loc = GetProductionLocationAndSLA(ctx, companyid, clientReference, projectid, out var SLADays);
            //var productionLocationID = FindProductionLocationID(ctx, companyid, providerCompanyId.Value, projectid);

            // TODO: Compare loc.ID with productionLocationID, maybe are the same value
            var r = await Task.FromResult(1);

            return new OrderParameters()
            {
                ProductionLocationID = locationid,
                SLADays = SLADays,
                DueDate = GetOrderDueDate(ctx, companyid, clientReference, projectid)
            };


        }

        [Obsolete("use GetOrderParametersAsync(PrintDB ctx, int companyid, int projectid, int locationid, int? providerCompanyId)")]
        public async Task<OrderParameters> GetOrderParametersAsync_LOGIC_PROBLEM(PrintDB ctx, int companyid, int projectid, int locationid, int? providerid)
        {
            // WARN: providerId must bue a CompanyID
			int SLADays = 7;

			var project = await ctx.Projects.Where(p => p.ID == projectid).SingleAsync();
			var company = await ctx.Companies.Where(c => c.ID == companyid).SingleAsync();
			var location = await ctx.Locations.Where(l => l.ID == locationid).SingleAsync();
            var providerRecord = await ctx.CompanyProviders.Where(w => w.ID == providerid.Value).SingleAsync();

            if (providerid == null || companyid == providerid)
			{
				if (project.DefaultFactory.HasValue)
				{
					if (project.SLADays.HasValue)
						SLADays = project.SLADays.Value;
				}
				else if (company.DefaultProductionLocation.HasValue)
				{
					if (company.SLADays.HasValue)
						SLADays = company.SLADays.Value;
				}
			}
			else
			{

                var provider = providerRepo.GetProviderBy(ctx, companyid, providerRecord.ProviderCompanyID);

				if (provider.DefaultProductionLocation.HasValue)
				{
					if (provider.SLADays.HasValue)
						SLADays = provider.SLADays.Value;
				}
				else if (project.DefaultFactory.HasValue)
				{
					if (project.SLADays.HasValue)
						SLADays = project.SLADays.Value;
				}
				else if (company.DefaultProductionLocation.HasValue)
				{
					if (company.SLADays.HasValue)
						SLADays = company.SLADays.Value;
				}
			}

			var dueDate = CalculateSLAForLocation(location, SLADays);

			return new OrderParameters()
			{
				ProductionLocationID = location.ID,
				SLADays = SLADays,
				DueDate = dueDate
			};
            
        }


        private DateTime CalculateSLAForLocation(Location location, int SLADays)
		{
			// SLA calculation should take into account WorkWeekDays and Holidays, which need to be configured per Factory (productionLocation here).
			var weekWorkDays = location.WorkingDays > 0 ? (WorkWeekDays)location.WorkingDays : WorkWeekDays.Monday | WorkWeekDays.Tuesday | WorkWeekDays.Wednesday | WorkWeekDays.Thursday | WorkWeekDays.Friday;
			var definition = new[] { new { Name = "", Month = "", Day = "" } };
			var thisYearHolidays = new int[0];
			if (location.Holidays != null)
			{
				var holidays = JsonConvert.DeserializeAnonymousType(location.Holidays, definition);
				thisYearHolidays = holidays.Select(x => new DateTime(DateTime.Now.Year, int.Parse(x.Month), int.Parse(x.Day)).DayOfYear).ToArray();
			}

			// We also have a cutoff time (which is time since midnight, Server Time): If an order arrives after this threshold, then it will be treated
			// as if it had arrived the next day. NOTE: If this behavior is not required, then simply pass TimeSpan.Zero in the call to CalculateSLA...
			TimeSpan cutOffTime = location.CutoffTime != null ? TimeSpan.FromHours(double.Parse(location.CutoffTime.Split(":")[0])) : TimeSpan.FromHours(12);

			return CalculateSLA(DateTime.Now, SLADays, weekWorkDays, thisYearHolidays, cutOffTime);
		}


		/// <summary>
		/// Calculates SLA based of the specified days, but taking into account workDays and holidays.
		/// Workdays must be set to the days of the week that are laboral, while thisYearHolidays is
		/// an array containing the DayOfYear of each holiday.
		/// </summary>
		public static DateTime CalculateSLA(DateTime startDate, int slaDays, WorkWeekDays weekWorkDays, int[] thisYearHolidays, TimeSpan cutOffTime)
		{
			// Account for the cutOff Time
			var cutOffThreshold = startDate.Date.Add(cutOffTime);
			if (startDate > cutOffThreshold)
				startDate = startDate.AddDays(1);

			// Adjust the date (if necesary) so we start counting days on a working day. This is necesary when the startDate falls on a week end or a holiday.
			while (!weekWorkDays.HasFlag(GetWorkDay(startDate.DayOfWeek)) || thisYearHolidays.Contains(startDate.DayOfYear))
				startDate = startDate.AddDays(1);

			// Now startDate points to an actual working day, so we can start substracting SLA days (skipping week ends and holidays)
			while (--slaDays > 0)
			{
				startDate = startDate.AddDays(1);
				while (!weekWorkDays.HasFlag(GetWorkDay(startDate.DayOfWeek)) || thisYearHolidays.Contains(startDate.DayOfYear))
					startDate = startDate.AddDays(1);
			}
			return startDate;
		}


		public enum WorkWeekDays
		{
			Monday = 1,
			Tuesday = 2,
			Wednesday = 4,
			Thursday = 8,
			Friday = 16,
			Saturday = 32,
			Sunday = 64
		}


		private static WorkWeekDays GetWorkDay(DayOfWeek dayOfWeek)
		{
			switch (dayOfWeek)
			{
				case DayOfWeek.Monday: return WorkWeekDays.Monday;
				case DayOfWeek.Tuesday: return WorkWeekDays.Tuesday;
				case DayOfWeek.Wednesday: return WorkWeekDays.Wednesday;
				case DayOfWeek.Thursday: return WorkWeekDays.Thursday;
				case DayOfWeek.Friday: return WorkWeekDays.Friday;
				case DayOfWeek.Saturday: return WorkWeekDays.Saturday;
				default: return WorkWeekDays.Sunday;
			}
		}


		public int FindDefaultDeliveryAddress(int sendToCompanyID)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return FindDefaultDeliveryAddress(ctx, sendToCompanyID);
			}
		}


		public int FindDefaultDeliveryAddress(PrintDB ctx, int sendToCompanyID)
		{
			// TODO: se deberia utilizar el repository de direcciones para no repetir consultas y que estas sean siempre las mismas
			var addresses = (from ca in ctx.CompanyAddresses join addr in ctx.Addresses on ca.AddressID equals addr.ID where ca.CompanyID == sendToCompanyID orderby addr.Default descending select addr).ToList();
			if (addresses.Count == 0)
			{
				var company = (from c in ctx.Companies where c.ID == sendToCompanyID select c).Single();
				if (company.DefaultDeliveryLocation != null)
				{
					var location = ctx.Locations.Single(l => l.ID == company.DefaultDeliveryLocation);
					var country = ctx.Countries.FirstOrDefault(c => c.Name.ToLower().Contains(location.Country));
					if (country == null)
					{
						country = ctx.Countries.Single(c => c.Alpha3.Equals("ESP"));
					}

					// TODO: se modifico la estructura de address, ahora tiene mas campos
					Address address = new Address();
					address.Name = location.Name;
					address.AddressLine1 = location.AddressLine1;
					address.AddressLine2 = location.AddressLine2;
					address.CityOrTown = location.CityOrTown;
					address.StateOrProvince = location.StateOrProvince;
					address.ZipCode = location.ZipCode;
					address.Country = location.Country;
					address.CountryID = country.ID;
					address.Default = true;
					address.CreatedBy = "SYSTEM";
					address.CreatedDate = DateTime.Now;
					address.UpdatedBy = "SYSTEM";
					address.UpdatedDate = DateTime.Now;
					ctx.Addresses.Add(address);
					ctx.SaveChanges();

					CompanyAddress companyAddress = new CompanyAddress();
					companyAddress.CompanyID = company.ID;
					companyAddress.AddressID = address.ID;
					companyAddress.CreatedBy = "SYSTEM";
					companyAddress.CreatedDate = DateTime.Now;
					companyAddress.UpdatedBy = "SYSTEM";
					companyAddress.UpdatedDate = DateTime.Now;
					ctx.CompanyAddresses.Add(companyAddress);
					ctx.SaveChanges();

					return address.ID;
				}
				else throw new NotDefaultAddressFoundException($"Cannot determine a default delivery address for company {company.Name} (CompanyID {sendToCompanyID}). Either create a default address for this company, or define a default delivery location in the company settings.");

			}
			else
			{
				return addresses.First().ID;
				//var defaultAddress = addresses.FirstOrDefault(addr => addr.Default);
				//if (defaultAddress == null)
				//	return addresses[0].ID;
				//else
				//	return defaultAddress.ID;
			}
		}
	}


	public class ConfigurationException : SystemException
	{
		public ConfigurationException(string message) : base(message) { }
		public ConfigurationException(string message, Exception innerException) : base(message, innerException) { }
	}


	public class NotDefaultAddressFoundException : ConfigurationException
	{
		public NotDefaultAddressFoundException(string message) : base(message) { }
		public NotDefaultAddressFoundException(string message, Exception innerException) : base(message, innerException) { }
	}
}
