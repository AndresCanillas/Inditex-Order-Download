using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebLink.Contracts.Models
{
	public class WizardCustomStepRepository : GenericRepository<IWizardCustomStep, WizardCustomStep>, IWizardCustomStepRepository
	{
		private ILogService log;

		public WizardCustomStepRepository(
			IFactory factory,
			ILogService log
			)
			: base(factory, (ctx) => ctx.WizardCustomSteps)
		{
			this.log = log;
		}


		protected override string TableName => "WizardStepsByCompany";


		protected override void UpdateEntity(PrintDB ctx, IUserData userData, WizardCustomStep actual, IWizardCustomStep data)
		{
			throw new NotImplementedException();
		}


		public IEnumerable<IWizardCustomStep> GetByProjectID(int id)
		{
			throw new NotImplementedException();
		}


		public IEnumerable<IWizardCustomStep> GetByProjectID(PrintDB ctx, int id)
		{
			throw new NotImplementedException();
		}


		public IEnumerable<IWizardCustomStep> GetGenericSteps()
		{
			throw new NotImplementedException();
		}


		public IEnumerable<IWizardCustomStep> GetGenericSteps(PrintDB ctx)
		{
			throw new NotImplementedException();
		}


		public IEnumerable<IWizardCustomStep> GetAvailablesStepsInProject(int orderID)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetAvailablesStepsInProject(ctx, orderID).ToList();
			}
		}


		public IEnumerable<IWizardCustomStep> GetAvailablesStepsInProject(PrintDB ctx, int orderID)
		{
			var orderInfo = (
				from o in ctx.CompanyOrders
				where o.ID.Equals(orderID)
				select new
				{
					OrderID = o.ID,
					ProjectID = o.ProjectID
				})
				.ToList()
				.First();

			// verify exist wizardcustomsteps configured
			var steps = ctx.WizardCustomSteps
				.Where(w => w.ProjectID.Equals(orderInfo.ProjectID));

			return steps;
		}



		public IEnumerable<IWizardCustomStep> GetAvailablesStepsInBrand(int orderID)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetAvailablesStepsInBrand(ctx, orderID).ToList();
			}
		}


		public IEnumerable<IWizardCustomStep> GetAvailablesStepsInBrand(PrintDB ctx, int orderID)
		{
			var orderInfo = (
				from o in ctx.CompanyOrders
				join p in ctx.Projects on o.ProjectID equals p.ID
				join b in ctx.Brands on p.BrandID equals b.ID
				where o.ID.Equals(orderID)

				select new
				{
					OrderID = o.ID,
					ProjectID = o.ProjectID,
					BrandID = b.ID,
					CompanyID = b.CompanyID
				})
				.ToList()
				.First();

			// verify exist wizardcustomsteps configured
			var steps = ctx.WizardCustomSteps
				.Where(w => w.BrandID.Equals(orderInfo.BrandID));

			return steps;
		}


		public IEnumerable<IWizardCustomStep> GetAvailablesStepsFor(int orderID)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetAvailablesStepsFor(ctx, orderID).ToList();
			}
		}


		public IEnumerable<IWizardCustomStep> GetAvailablesStepsFor(PrintDB ctx, int orderID)
		{
			var orderInfo = (
				from o in ctx.CompanyOrders
				join p in ctx.Projects on o.ProjectID equals p.ID
				join b in ctx.Brands on p.BrandID equals b.ID
				where o.ID.Equals(orderID)

				select new
				{
					OrderID = o.ID,
					ProjectID = o.ProjectID,
					BrandID = b.ID,
					CompanyID = b.CompanyID
				})
				.ToList()
				.First();

			// verify exist wizardcustomsteps configured
			var steps = ctx.WizardCustomSteps
				.Where(w => w.ProjectID.Equals(orderInfo.ProjectID)
				|| w.BrandID.Equals(orderInfo.BrandID)
				|| w.CompanyID.Equals(orderInfo.CompanyID)
				).ToList();

            
            var stepsByProject = steps.Where(w => w.ProjectID == orderInfo.ProjectID);

            var stepsByBrand = steps.Where(w => w.BrandID == orderInfo.BrandID && w.ProjectID == null);

            var stepsByCompany = steps.Where(w => w.CompanyID == orderInfo.CompanyID && w.BrandID == null && w.ProjectID == null);

            return stepsByProject.Concat(stepsByBrand).Concat(stepsByCompany);
		}
    }
}
