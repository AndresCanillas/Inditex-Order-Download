using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Service.Contracts.Database;

namespace Service.Contracts.WF
{
	class TaskDataModel
	{
		private static int sortOrder = 0;
		private static int NextSortOrder { get => Interlocked.Increment(ref sortOrder); }

		private IConnectionManager connManager;

		public TaskDataModel(IConnectionManager connManager)
		{
			this.connManager = connManager;
		}


		public async Task<TaskData> BindTaskToDatabase(int workflowid, string taskName, TaskType tasktype)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var taskData = await conn.SelectOneAsync<TaskData>(@"
					select * from TaskData
					where 
						WorkflowID = @workflowid and
						TaskName = @name", 
					workflowid, taskName);

				if (taskData == null)
				{
					taskData = new TaskData()
					{
						WorkflowID = workflowid,
						TaskName = taskName,
						TaskType = tasktype,
						Detached = false,
						SortOrder = NextSortOrder
					};
					await conn.InsertAsync(taskData);
				}
				else
				{
					taskData.TaskType = tasktype;
					taskData.Detached = false;
					taskData.SortOrder = NextSortOrder;
					await conn.UpdateAsync(taskData);
				}
				return taskData;
			}
		}


		/// <summary>
		/// Gets a list of all the items that are ready to be processed on the specified workflow.
		/// Items are sorted by ItemPriority (higher priority first) and DelayedUntil ascending (oldest first).
		/// This process will also update the returned items to the Active state to reflect the fact that the items
		/// will be in active execution.
		/// NOTE: Most API operations should refuse to perform operation on Active items to prevent race conditions.
		/// </summary>
		/// <returns>A list of items that are ready for execution.</returns>
		public async Task<List<ItemData>> GetReadyItems(int workflowid)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var items = await conn.SelectAsync<ItemData>($@"
                    declare @tmp WFItem

                    update ItemData WITH (UPDLOCK, ROWLOCK)
	                    set ItemStatus = {(int)ItemStatus.Active}
                    output inserted.* into @tmp
                    where
						WorkflowID = @workflowid and
						TaskID is not null and
						ItemStatus = {(int)ItemStatus.Delayed} and
						DelayedUntil < @currentDate

                    select * from @tmp order by ItemPriority desc, DelayedUntil asc",
					workflowid, DateTime.Now);
				return items;
			}
		}

		/// <summary>
		/// Moves all active items in the specified task back to the delayed state (without modifying any other property).
		/// </summary>
		/// <param name="workflowid">ID of the workflow</param>
		/// <param name="taskid">ID of the task</param>
		/// <param name="token">Token used to cancel the operation</param>
		internal async Task ResetTaskItems(int workflowid, int taskid, CancellationToken token)
        {
            bool success;
            do
            {
                success = false;
                try
                {
                    using (var conn = await connManager.OpenDBAsync("APM"))
                    {
                        await conn.ExecuteNonQueryAsync(@"
					        update ItemData set ItemStatus = @delayedState 
					        where 
						        WorkflowID = @workflowid and
						        TaskID = @taskid and
						        ItemStatus = @activeState",
                            ItemStatus.Delayed, workflowid, taskid, ItemStatus.Active);
                        success = true;
                    }
                }
                catch
                {
                }
                if (!success)
                    await Task.Delay(1000);
            } while (!token.IsCancellationRequested && !success);
        }


		internal async Task<List<TaskData>> GetWorkflowTasks(int workflowID)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				return await conn.SelectAsync<TaskData>(@"
					select * from TaskData
					where 
						WorkflowID = @workflowid
					order by SortOrder",
					workflowID);
			}
		}


		internal async Task DeleteDetachedTask(int workflowid, int taskid)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				await conn.ExecuteNonQueryAsync(@"
					delete from TaskData where
						WorkflowID = @workflowid and
						TaskID = @taskid and
						Detached = 1",
					workflowid, taskid);
			}
		}


		internal async Task<TaskData> GetTaskByName(int workflowID, string taskName)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var taskData = await conn.SelectOneAsync<TaskData>(@"
					select * from TaskData
					where
						WorkflowID = @workflowid and
						[TaskName] = @taskname and
						Detached = 0",
					workflowID, taskName);
				return taskData;
			}
		}


		internal async Task<TaskData> GetTaskByTaskID(int workflowID, int taskid)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var taskData = await conn.SelectOneAsync<TaskData>(@"
					select * from TaskData
					where
						WorkflowID = @workflowid and
						[TaskID] = @taskid and
						Detached = 0",
					workflowID, taskid);
				return taskData;
			}
		}


		internal async Task<TaskData> GetInitialTask(int workflowid)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var taskData = await conn.SelectOneAsync<TaskData>(@"
					select top 1 * from TaskData
					where
						WorkflowID = @workflowid and
						Detached = 0
                    order by SortOrder",
					workflowid);
				return taskData;
			}
		}
	}
}
