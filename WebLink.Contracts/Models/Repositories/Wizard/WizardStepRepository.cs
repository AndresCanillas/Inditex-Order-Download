using Service.Contracts;
using Services.Core;
using System.Collections.Generic;
using System.Linq;

namespace WebLink.Contracts.Models
{
	public class WizardStepRepository : GenericRepository<IWizardStep, WizardStep>, IWizardStepRepository
	{
		private ILogService log;

		public WizardStepRepository(
			IFactory factory,
			ILogService log
			)
			: base(factory, (ctx) => ctx.WizardSteps)
		{
			this.log = log;
		}


		protected override string TableName { get => "WizardSteps"; }


		protected override void UpdateEntity(PrintDB ctx, IUserData userData, WizardStep actual, IWizardStep data)
		{
			actual.IsCompleted = data.IsCompleted;
			//actual.Url = data.Url;
			//actual.WizardID = data.WizardID;
		}


		public void MarkAsComplete(int wizardStepID)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				MarkAsComplete(ctx, wizardStepID);
			}
		}


		public void MarkAsComplete(PrintDB ctx, int wizardStepID)
		{
			var entity = GetByID(ctx, wizardStepID);
			entity.IsCompleted = true;
			Update(ctx, entity);
		}


		public void MarkAsCompleteByGroup(int position, IEnumerable<int> ordersIDs)
		{
			// this way, lose entity events notifications... 
			// Can send update notifications like this if necesary:
			//		events.Send(new EntityEvent(companyid, actual, DBOperation.Update));
			//		(just need to query the data of all orders that were updated and send one event for each)

			var connManager = factory.GetInstance<IDBConnectionManager>();

			var inOrders = string.Join(",", ordersIDs.ToArray());

			//int affected = -1;

            if(inOrders.Length > 1)
            {
                using(var db = connManager.OpenWebLinkDB())
                {
                    string query = $@"
					UPDATE s
					SET IsCompleted = 1
					FROM [dbo].[WizardSteps] s
					INNER JOIN [dbo].[Wizards] wz ON s.WizardID = wz.ID
					WHERE wz.OrderID IN ({inOrders})
					AND s.Position = {position}
				";

                    var affected = db.ExecuteNonQuery(query);
                }
            }
			
			
			//if (affected != ordersIDs.Count)
			//{
			//	// could be register app log
			//}
		}
	}
}
