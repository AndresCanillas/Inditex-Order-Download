using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Service.Contracts.Database;

namespace Service.Contracts.WF
{
	class WorkflowDataModel
	{
		private readonly IFactory factory;
		private readonly IConnectionManager connManager;

		public WorkflowDataModel(IFactory factory, IConnectionManager connManager)
		{
			this.factory = factory;
			this.connManager = connManager;
		}


		public async Task EnsureDBCreated()
		{
			IDBConfiguration db = factory.GetInstance<IDBConfiguration>();
			db.Configure("APM");
			await db.EnsureCreatedAsync();
			using(var conn = await db.CreateConnectionAsync())
			{
				await UpdateDBObjects(conn);
			}
		}


		public async Task<WorkflowData> BindWorkflowToDatabase(WorkflowDefinition definition)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				// Resets all active items
				await conn.ExecuteNonQueryAsync(@"
					update ItemData set ItemStatus = @delayedState 
					where 
						ItemStatus = @activeState",
					ItemStatus.Delayed, ItemStatus.Active);

				var wfData = await conn.SelectOneAsync<WorkflowData>("select * from WorkflowData where Name = @name", definition.Name);
				if (wfData == null)
				{
					wfData = new WorkflowData()
					{
						Name = definition.Name,
						Detached = false
					};
					await conn.InsertAsync(wfData);
				}
				else
				{
					wfData.Detached = false;
					await conn.UpdateAsync(wfData);
				}

				// Detach all workflow tasks, they will be reatached when the tasks of the owrkflow are initialized. Only those tasks that no longer exist in code will be left as detached.
				await conn.ExecuteNonQueryAsync(@"
					update
						TaskData set Detached = 1
					where
						WorkflowID = @wfid",
					wfData.WorkflowID);

				return wfData;
			}
		}

		internal async Task<List<WorkflowData>> GetWorkflows()
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				return conn.Select<WorkflowData>("select * from WorkflowData");
			}
		}

		internal async Task<WorkflowData> GetWorkflowByID(int workflowid)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				return conn.SelectOne<WorkflowData>(workflowid);
			}
		}


		internal async Task<WorkflowData> GetWorkflowByName(string name)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				return conn.SelectOne<WorkflowData>(@"select * from WorkflowData where Name = @name", name);
			}
		}


		private async Task UpdateDBObjects(IDBX conn)
		{
			await conn.ExecuteNonQueryAsync(Properties.Resources.WorkflowDataModelScript);
		}
	}
}
