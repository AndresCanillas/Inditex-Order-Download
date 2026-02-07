using Service.Contracts;
using Services.Core;
using System.Collections.Generic;
using System.Linq;

namespace WebLink.Contracts.Models
{
    public class WizardRepository : GenericRepository<IWizard, Wizard>, IWizardRepository
    {
        private ILogService log;

        public WizardRepository(
            IFactory factory,
            ILogService log
            )
            : base(factory, (ctx) => ctx.Wizards)
        {
            this.log = log;
        }

        protected override string TableName => "Wizards";

        protected override void UpdateEntity(PrintDB ctx, IUserData userData, Wizard actual, IWizard data)
        {
            actual.IsCompleted = data.IsCompleted;
            actual.Progress = data.Progress;
            //actual.OrderID = data.OrderID;
        }

        public IWizard GetByOrder(int orderID)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByOrder(ctx, orderID);
            }
        }

        public IWizard GetByOrder(PrintDB ctx, int orderID)
        {
            return ctx.Wizards
                .Where(w => w.OrderID.Equals(orderID))
                .FirstOrDefault();
        }

        public void Reset(int wizardID)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                Reset(ctx, wizardID);
            }
        }

        public void Reset(PrintDB ctx, int wizardID)
        {
            var wsRepo = factory.GetInstance<IWizardStepRepository>();

            var wizard = GetByID(ctx, wizardID);
            wizard.IsCompleted = false;
            wizard.Progress = 0;

            // update steps firts

            var steps = ctx.WizardSteps.Where(w => w.WizardID.Equals(wizardID)).ToList();
            foreach (var step in steps)
            {
                step.IsCompleted = false;
                wsRepo.Update(ctx, step);
            }

            Update(ctx, wizard);
        }

        public float UpdateProgress(int wizardID)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return UpdateProgress(ctx, wizardID);
            }
        }

        /// <summary>
        /// Calculate and update wizard entity
        /// </summary>
        /// <param name="wizardID"></param>
        /// <returns>Current progress</returns>
        public float UpdateProgress(PrintDB ctx, int wizardID)
        {
            var q = ctx.WizardSteps.Where(w => w.WizardID.Equals(wizardID)).ToList();
            var total = q.Count();
            var completed = q.Where(w => w.IsCompleted.Equals(true)).Count();
            var wzd = ctx.Wizards.First(w => w.ID.Equals(wizardID));

            wzd.Progress = (int)(((float)completed / total) * 100);
            Update(ctx, wzd);

            return wzd.Progress;
        }

        public void UpdateProgressByGroup(IEnumerable<int> ordersIDs)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                UpdateProgressByGroup(ctx, ordersIDs);
            }
        }

        public void UpdateProgressByGroup(PrintDB ctx, IEnumerable<int> ordersIDs)
        {
            foreach (var orderID in ordersIDs)
            {
                UpdateProgressByOrderID(ctx, orderID);
            }
        }

        private float UpdateProgressByOrderID(int orderID)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return UpdateProgressByOrderID(ctx, orderID);
            }
        }

        /// <summary>
        /// Calculate and update wizard entity
        /// </summary>
        /// <param name="wizardID"></param>
        /// <returns>Current progress</returns>
        private float UpdateProgressByOrderID(PrintDB ctx, int orderID)
        {
            var wzd = ctx.Wizards.First(w => w.OrderID.Equals(orderID));
            var q = ctx.WizardSteps.Where(w => w.WizardID.Equals(wzd.ID)).ToList();
            var total = q.Count();
            var completed = q.Where(w => w.IsCompleted.Equals(true)).Count();

            wzd.Progress = (int)(((float)completed / total) * 100);

            Update(ctx, wzd);

            return wzd.Progress;
        }

        public void SetAsComplete(int orderID)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                SetAsComplete(ctx, orderID);
            }
        }

        public void SetAsComplete(PrintDB ctx, int orderID)
        {
            var stpRepo = factory.GetInstance<IWizardStepRepository>();

            var wzd = ctx.Wizards.FirstOrDefault(w => w.OrderID.Equals(orderID));
            if (wzd == null)
            {
                return;
            }

            var q = ctx.WizardSteps.Where(w => w.WizardID.Equals(wzd.ID)).ToList();

            q.ForEach(stp =>
            {
                stp.IsCompleted = true;
                stpRepo.Update(ctx, stp);
            });

            wzd.Progress = 100;

            Update(ctx, wzd);
        }

        public IEnumerable<IWizardStep> GetSteps(int wizardID)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetSteps(ctx, wizardID);
            }
        }

        public IEnumerable<IWizardStep> GetSteps(PrintDB ctx, int wizardID)
        {
            //throw new NotImplementedException();
            var result = ctx.WizardSteps
                .Where(w => w.WizardID.Equals(wizardID))
                .OrderBy(w => w.Position)
                .Select(s => s)
                .ToList();

            return result;
        }
    }
}
