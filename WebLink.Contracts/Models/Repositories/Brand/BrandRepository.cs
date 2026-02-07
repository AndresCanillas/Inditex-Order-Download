using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WebLink.Contracts.Models
{
    public class BrandRepository: GenericRepository<IBrand, Brand>, IBrandRepository
	{
		private IFtpAccountRepository ftpRepo;

		public BrandRepository(
			IFactory factory,
			IFtpAccountRepository ftpRepo
			)
            : base(factory, (ctx)=>ctx.Brands)
		{
			this.ftpRepo = ftpRepo;
        }


		protected override string TableName { get => "Brands"; }


        protected override void UpdateEntity(PrintDB ctx, IUserData userData, Brand actual, IBrand data)
        {
            actual.Name = data.Name;
			actual.EnableFTPFolder = data.EnableFTPFolder;
			if(data.EnableFTPFolder)
			{
				if (!ftpRepo.IsValidFtpDirectory(data.FTPFolder))
					throw new InvalidOperationException($"Specified FTP folder \"{data.FTPFolder}\" is not valid");

				var otherBrand = ctx.Brands.Where(p => p.ID != actual.ID && p.CompanyID == actual.CompanyID && p.FTPFolder == data.FTPFolder).FirstOrDefault();
				if (otherBrand != null)
					throw new Exception($"Cannot create ftp folder {data.FTPFolder} because it is being used by another brand {otherBrand.Name}");

				string homeDir = ftpRepo.GetCompanyHomeDirectory(actual.CompanyID);
				string brandDirectory = Path.Combine(homeDir, data.FTPFolder);
				if (!String.IsNullOrWhiteSpace(actual.FTPFolder) && actual.FTPFolder != data.FTPFolder)
				{
					string originalDirectory = Path.Combine(homeDir, actual.FTPFolder);
					if (Directory.Exists(originalDirectory))
					{
						Directory.Move(originalDirectory, brandDirectory);
					}
					else
					{
						if (!Directory.Exists(brandDirectory))
							Directory.CreateDirectory(brandDirectory);
					}
				}
				else
				{
					if (!Directory.Exists(brandDirectory))
						Directory.CreateDirectory(brandDirectory);
				}
			}

			actual.FTPFolder = data.FTPFolder;
			actual.RFIDConfigID = data.RFIDConfigID;
        }


		protected override void BeforeDelete(PrintDB ctx, IUserData userData, Brand actual, out bool cancelOperation)
		{
			cancelOperation = false;
			var projects = ctx.Projects.Where(p => p.BrandID == actual.ID).AsNoTracking().ToList();
			if (projects.Count > 0)
				throw new Exception("Cannot delete brand if it still has projects. Delete all projects first.");
		}


		public List<IBrand> GetByCompanyID(int companyid)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetByCompanyID(ctx, companyid);
			}
		}


		public List<IBrand> GetByCompanyID(PrintDB ctx, int companyid)
		{
			return new List<IBrand>(All(ctx).Where(p => p.CompanyID == companyid));
		}

        public List<IBrand> GetByCompanyIDME(int companyid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByCompanyIDME(ctx, companyid);
            }
        }
        private List<IBrand> GetByCompanyIDME(PrintDB ctx, int companyid)
        {
            return ctx.Brands.Where(p => p.CompanyID == companyid).ToList<IBrand>();
        }


        public void UpdateIcon(int brandid, byte[] content)
        {
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				UpdateIcon(ctx, brandid, content);
			}
		}


		public void UpdateIcon(PrintDB ctx, int brandid, byte[] content)
		{
			var brand = (Brand)GetByID(ctx, brandid);
			brand.Icon = ImageProcessing.CreateThumb(content);
			ctx.SaveChanges();
		}


		public byte[] GetIcon(int brandid)
        {
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetIcon(ctx, brandid);
			}
		}


		public byte[] GetIcon(PrintDB ctx, int brandid)
		{
			var brand = (Brand)GetByID(ctx, brandid);
			return brand.Icon;
		}


		public IBrand GetSelectedBrand()
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetSelectedBrand(ctx);
			}
		}


		public IBrand GetSelectedBrand(PrintDB ctx)
		{
			var userData = factory.GetInstance<IUserData>();
			IBrand brand;
			var brandid = userData.SelectedBrandID;
			if (brandid <= 0)
			{
				var companyid = userData.SelectedCompanyID;
				brand = ctx.Brands.Where(b => b.CompanyID == companyid).OrderByDescending(p => p.CreatedDate).Take(1).AsNoTracking().FirstOrDefault();
			}
			else
			{
				brand = ctx.Brands.Where(b => b.ID == brandid).AsNoTracking().FirstOrDefault();
			}

			if (brand != null)
				return brand;
			else
				return new Brand() { Name = "No Brand" };
		}


		public void AssignRFIDConfig(int brandid, int configid)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				AssignRFIDConfig(ctx, brandid, configid);
			}
		}


		public void AssignRFIDConfig(PrintDB ctx, int brandid, int configid)
		{
			var brand = ctx.Brands.Where(c => c.ID == brandid).Single();
			brand.RFIDConfigID = configid;
			ctx.SaveChanges();
		}

        public IEnumerable<Brand> GetAllByID(IEnumerable<int> brandIDs)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetAllByID(ctx, brandIDs);
            }
        }

        public IEnumerable<Brand> GetAllByID(PrintDB ctx, IEnumerable<int> brandIDs)
        {
            return ctx.Brands.Where(w => brandIDs.Contains(w.ID)).ToList();// use to list to force execute query, to avoid return IQueriable
        }
    }
}
