using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using Service.Contracts.Authentication;
using Service.Contracts.Database;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
	public class DBInitialization
	{
		private IFactory factory;
		private IDBConnectionManager connManager;

		public DBInitialization(IFactory factory)
		{
			this.factory = factory;
			this.connManager = factory.GetInstance<IDBConnectionManager>();
		}


		public async Task<bool> EnsureDBInitialized(PrintDB printerCtx, IdentityDB identityCtx)
		{
			try
			{
				using (var conn = connManager.OpenWebLinkDB()) { FixDBMigrationHistory(conn, "WebLink.Migrations.Initial"); }
				using (var conn = connManager.OpenUsersDB()) { FixDBMigrationHistory(conn, "WebLink.Migrations.IdentityDBMigrations.Initial"); }
			}
			catch (SqlException) { }
			printerCtx.Database.Migrate();
			identityCtx.Database.Migrate();
			return true;
		}

        public async Task<bool> SeedDB(PrintDB printerCtx)
        {
            await CreateRole(Roles.SysAdmin);
            await CreateRole(Roles.IDTCostumerService);
            await CreateRole(Roles.IDTProdManager);
            await CreateRole(Roles.IDTLabelDesign);
            await CreateRole(Roles.CompanyAdmin);
            await CreateRole(Roles.ProdManager);
            await CreateRole(Roles.DataUpload);
            await CreateRole(Roles.PrinterOperator);
            await CreateRole(Roles.IDTExtProdManager);
            await CreateUser("SysConfig", "defaultSysConfigPWD128$", "SysAdmin");
            await CreateUser("qa.zip.dw", "zipDW_$386", "SysAdmin");
            
            await CreateDefaultCompany(printerCtx);
            return true;
        }

        private void FixDBMigrationHistory(IDBX conn, string initialMigrationTypeName)
		{
			var initial = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.FullName.Contains(initialMigrationTypeName)).FirstOrDefault();
			if(initial != null)
			{
				var attr = initial.GetCustomAttribute(typeof(MigrationAttribute));
				if(attr != null)
				{
					var migrationAttr = attr as MigrationAttribute;
					var migrations = conn.Select<__EFMigrationsHistory>("select * from __EFMigrationsHistory");
					if(migrations.Count > 0 && migrations[0].MigrationId != migrationAttr.Id)
					{
						conn.ExecuteNonQuery("delete from __EFMigrationsHistory");
						conn.ExecuteNonQuery("insert into __EFMigrationsHistory values (@initialMigration, '2.1.4-rtm-31024')", migrationAttr.Id);
					}
				}
			}
		}


		private async Task CreateRole(string roleName)
		{
			var roleManager = factory.GetInstance<IRoleManager>();
			AppRole role = await roleManager.FindByNameAsync(roleName);
			if (role == null)
			{
				role = new AppRole() { Name = roleName };
				await roleManager.CreateAsync(role);
			}
		}

		private async Task CreateUser(string userName, string password, params string[] roles)
		{
			var userManager = factory.GetInstance<IUserManager>();
			var user = await userManager.FindByNameAsync(userName);
			if (user == null)
			{
				user = new AppUser(userName);
				user.CompanyID = 1;
				user.LocationID = 1;
				await userManager.CreateAsync(user, password);
				if (roles != null)
				{
					foreach (var role in roles)
						await userManager.AddToRoleAsync(user, role);
				}
			}
		}

		private async Task CreateDefaultCompany(PrintDB ctx)
		{
			ICompany company = await ctx.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Name == "Smartdots");
			if (company == null)
			{
				company = new Company()
				{
					Name = "Smartdots",
					ShowAsCompany = true,
					CompanyCode = "SMD",
					ClientReference = "SMD",
					CreatedBy = "system",
					CreatedDate = DateTime.Now,
					UpdatedBy = "system",
					UpdatedDate = DateTime.Now
				};
				var companyRepo = factory.GetInstance<ICompanyRepository>();
				company = await companyRepo.InsertAsync(ctx, company);
			}

			ICountry country = await ctx.Countries.AsNoTracking().FirstOrDefaultAsync(c=>c.Name == "Spain");
			if(country == null)
			{
				country = new Country()
				{
					Name = "Spain",
					Alpha2 = "ES",
					Alpha3 = "ESP",
					NumericCode = "724"
				};
				var countryRepo = factory.GetInstance<ICountryRepository>();
				country = await countryRepo.InsertAsync(ctx, country);
			}

			ILocation location = await ctx.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.CompanyID == company.ID && l.Name == "Castellar");
			if(location == null)
			{
				location = new Location()
				{
					Name = "Castellar",
					CompanyID = company.ID,
					FactoryCode = "SDS01",
					DeliverTo = "Daniel Roca",
					AddressLine1 = "Carrer del Solsonès, 61",
					AddressLine2 = "Polígono Ind. El Pla de la Bruguera",
					CityOrTown = "Castellar del Vallés",
					ZipCode = "08211",
					StateOrProvince = "Cataluña",
					CountryID = country.ID,
					CutoffTime = "12:00",
					EnableERP = true,
					WorkingDays = 127,
					CreatedBy = "system",
					CreatedDate = DateTime.Now,
					UpdatedBy = "system",
					UpdatedDate = DateTime.Now
				};
				var locationRepo = factory.GetInstance<ILocationRepository>();
				location = await locationRepo.InsertAsync(ctx, location);
			}

			IBrand brand = await ctx.Brands.AsNoTracking().FirstOrDefaultAsync(b => b.CompanyID == company.ID && b.Name == "Smartdots");
			if(brand == null)
			{
				brand = new Brand()
				{
					CompanyID = company.ID,
					Name = "Smartdots",
					EnableFTPFolder = false,
					CreatedBy = "system",
					CreatedDate = DateTime.Now,
					UpdatedBy = "system",
					UpdatedDate = DateTime.Now,
				};
				var brandRepo = factory.GetInstance<IBrandRepository>();
				brand = await brandRepo.InsertAsync(ctx, brand);
			}

			IProject project = ctx.Projects.AsNoTracking().FirstOrDefault(p => p.BrandID == brand.ID && p.Name == "Smartdots");
			if(project == null)
			{
				project = new Project()
				{
					BrandID = brand.ID,
					Name = "Smartdots",
					Hidden = false,
					CreatedBy = "system",
					CreatedDate = DateTime.Now,
					UpdatedBy = "system",
					UpdatedDate = DateTime.Now
				};
				var projectRepo = factory.GetInstance<IProjectRepository>();
				project = await projectRepo.InsertAsync(ctx, project);
			}
		}
	}

#pragma warning disable CS0649
	class __EFMigrationsHistory
	{
		public string MigrationId;
		public string ProductVersion;
	}
	#pragma warning restore CS0649
}
