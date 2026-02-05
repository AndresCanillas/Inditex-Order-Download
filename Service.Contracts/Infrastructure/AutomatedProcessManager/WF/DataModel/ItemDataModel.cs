using Newtonsoft.Json;
using Service.Contracts.Database;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	class ItemDataModel
	{
		private IFactory factory;
		private IConnectionManager connManager;
		private IEventQueue events;
		private ILogService log;


		public ItemDataModel(IFactory factory, IConnectionManager connManager, IEventQueue events, ILogService log)
		{
			this.factory = factory;
			this.connManager = connManager;
			this.events = events;
			this.log = log;
		}


		internal async Task LogMessage(ItemData item, string taskName, string message, ItemLogVisibility visibility)
		{
			if (item.ItemID == 0)
				throw new InvalidOperationException("Item is not in a valid state.");

			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var attachedData = JsonConvert.SerializeObject(new BaseItemLogData()
				{
					TaskID = item.TaskID ?? 0,
					TaskName = taskName,
					Message = message
				});

				var entry = new ItemLog()
				{
					WorkflowID = item.WorkflowID,
					ItemID = item.ItemID,
					EntryType = ItemLogEntryType.Message,
					Visibility = visibility,
					AttachedData = attachedData,
					Date = DateTime.Now
				};

				await conn.InsertAsync(entry);
			}
		}


		internal async Task LogWarning(ItemData item, string taskName, string message, ItemLogVisibility visibility)
		{
			if (item.ItemID == 0)
				throw new InvalidOperationException("Item is not in a valid state.");

			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var attachedData = JsonConvert.SerializeObject(new BaseItemLogData()
				{
					TaskID = item.TaskID ?? 0,
					TaskName = taskName,
					Message = message
				});

				var entry = new ItemLog()
				{
					WorkflowID = item.WorkflowID,
					ItemID = item.ItemID,
					EntryType = ItemLogEntryType.Warning,
					Visibility = visibility,
					AttachedData = attachedData,
					Date = DateTime.Now
				};

				await conn.InsertAsync(entry);
			}
		}


		internal async Task LogException(ItemData item, string taskName, string message, Exception ex, ItemLogVisibility visibility)
		{
			if (item.ItemID == 0)
				throw new InvalidOperationException("Item is not in a valid state.");

			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var attachedData = JsonConvert.SerializeObject(new BaseItemLogData()
				{
					TaskID = item.TaskID ?? 0,
					TaskName = taskName,
					Message = message,
					ExceptionMessage = ex.Message,
					ExceptionType = ex.GetType().Name,
					ExceptionStackTrace = ex.MinimalStackTrace()
				});

				var entry = new ItemLog()
				{
					WorkflowID = item.WorkflowID,
					ItemID = item.ItemID,
					EntryType = ItemLogEntryType.Error,
					Visibility = visibility,
					AttachedData = attachedData,
					Date = DateTime.Now
				};

				await conn.InsertAsync(entry);
			}
		}


		internal async Task AddItemHistoryEntry(int taskid, string taskName, ItemData item, TaskResult taskResult, int elapsedMilliseconds)
		{
			if (item.ItemID == 0)
				throw new InvalidOperationException("Item is not in a valid state.");

			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var historyData = new TaskHistoryLogData();
				historyData.TaskID = taskid;
				historyData.TaskName = taskName;
				historyData.Message = $"Item executed by {taskName}";
				if (taskResult.Exception != null)
				{
					historyData.ExceptionMessage = taskResult.Exception.Message;
					historyData.ExceptionType = taskResult.Exception.GetType().Name;
					historyData.ExceptionStackTrace = taskResult.Exception.MinimalStackTrace();
				}
				historyData.Success = taskResult.Status == TaskStatus.OK;
				historyData.Status = taskResult.Status.ToString();
				historyData.StatusReason = taskResult.Reason;
				historyData.RouteCode = taskResult.RouteCode;
				historyData.DelayTime = (int)taskResult.DelayTime.TotalSeconds;
				historyData.RetryCount = item.RetryCount;
				historyData.ExecutionTime = elapsedMilliseconds;

				var isError = taskResult.Exception != null;

				var entry = new ItemLog()
				{
					WorkflowID = item.WorkflowID,
					ItemID = item.ItemID,
					EntryType = ItemLogEntryType.TaskHistory,
					EntrySubType = isError ? ItemLogEntrySubType.Failure : ItemLogEntrySubType.Success,
					Visibility = ItemLogVisibility.Public,
					AttachedData = JsonConvert.SerializeObject(historyData, Formatting.Indented),
					Date = DateTime.Now
				};

				await conn.InsertAsync(entry);
			}
		}


		internal async Task AddExceptionHistoryEntry(int taskid, string taskName, ItemData item, Exception ex, int elapsedMilliseconds)
		{
			if (item.ItemID == 0)
				throw new InvalidOperationException("Item is not in a valid state.");

			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var historyData = new TaskHistoryLogData();
				historyData.TaskID = taskid;
				historyData.TaskName = taskName;
				historyData.Message = $"Item executed by {taskName}";
				historyData.ExceptionMessage = ex.Message;
				historyData.ExceptionType = ex.GetType().Name;
				historyData.ExceptionStackTrace = ex.MinimalStackTrace();
				historyData.Success = false;
				historyData.Status = "Routing to exception handler";
				historyData.StatusReason = "Item will be moved to a different task due to an unhandled exception";
				historyData.RouteCode = "";
				historyData.DelayTime = 0;
				historyData.RetryCount = item.RetryCount;
				historyData.ExecutionTime = elapsedMilliseconds;

				var entry = new ItemLog()
				{
					WorkflowID = item.WorkflowID,
					ItemID = item.ItemID,
					EntryType = ItemLogEntryType.TaskHistory,
					EntrySubType = ItemLogEntrySubType.Failure,
					Visibility = ItemLogVisibility.Restricted,
					AttachedData = JsonConvert.SerializeObject(historyData, Formatting.Indented),
					Date = DateTime.Now
				};

				await conn.InsertAsync(entry);
			}
		}

        // Need to disable this warning using #pragma to allow Build in NET 8, as this serialization cannot be easily changed
        // without introducing incompatibilities with what is already stored in the DB. Technically speaking we could write a
        // migration tool to re-serialize all records in the DB, but managing such a migration would be too complicated at the
        // moment, and the benefit is minimal.
        #pragma warning disable SYSLIB0011
    
        internal byte[] SerializeException(Exception ex)
		{
			if (ex == null)
				return null;
			try
			{
				byte[] content;
				using (var stream = new MemoryStream())
				{
					new BinaryFormatter().Serialize(stream, ex);
					content = stream.GetBuffer();
				}
				return content;
			}
			catch (SerializationException)
			{
				// We got a Non-serializable exception type, this is sad, but we cannot save it in the DB.
				// Instead we will replace it with a generic one, the problem here is that any custom data
				// in the exception will be lost. If this is an exception defined by you, then ensure it is
				// serializable to avoid this problem.
				return SerializeException(new Exception($"Catched a non-serializable exception of type {ex.GetType().FullName}\r\nMessage: {ex.Message}\r\nStackTrace: {ex.MinimalStackTrace()}"));
			}
		}


		internal async Task<Exception> GetLastException(ItemData item)
		{
			if (item.ItemID == 0)
				throw new InvalidOperationException("Item is not in a valid state.");

			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var result = await conn.ExecuteScalarAsync(@"
					select LastException
                    from ItemData WITH (NOLOCK)
					where
						WorkflowID = @workflowid and
						ItemID = @itemid",
					item.WorkflowID, item.ItemID);

				if (result == null || result is DBNull)
					return null;

				byte[] content = (byte[])result;
				using (var stream = new MemoryStream(content))
				{
					var ex = (Exception)new BinaryFormatter().Deserialize(stream);
					return ex;
				}
			}
		}

        #pragma warning restore SYSLIB0011

        internal async Task<TimeSpan> GetWaitTaskTimeout(int workflowID, int taskID, TimeSpan defaultTimeout)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var result = await conn.ExecuteScalarAsync(@"
					select min(WakeTimeout) as WakeTimeout
                    from ItemData WITH (NOLOCK)
					where
						WorkflowID = @workflowid and
						TaskID = @taskid",
					workflowID, taskID);

				if (result != null && result != DBNull.Value)
				{
					var timeout = ((DateTime)result - DateTime.Now);
					if (timeout.Ticks < 0)
						return TimeSpan.Zero;
					else
						return timeout;
				}
				else
				{
                    // IMPORTANT: Default timeout cannot be less than 1 minute. Reason: Wait tasks involve manual steps or processes that can take long to complete, it should not be expected for them to complete quickly.
                    if(defaultTimeout.TotalMinutes < 1)
                        return TimeSpan.FromMinutes(1);
					else
                        return defaultTimeout;
				}
			}
		}

		internal async Task<int> WakeWaitingItemsByTimeout(int workflowID, int taskID)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				return await conn.ExecuteNonQueryAsync($@"
					update ItemData set
						WakeTimeout = null,
                        WakeEventState = null,
                        ItemStatus = {(int)ItemStatus.Delayed},
                        DelayedUntil = @delayedDate
					where
						WorkflowID = @workflowid and
						TaskID = @taskid and
						WakeTimeout <= @date",
                    DateTime.Now.AddSeconds(-1), workflowID, taskID, DateTime.Now);
			}
		}

		internal async Task<List<ItemLog>> GetItemLog(ItemData itemData, ItemLogEntryType? entryType)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var logEntries = await conn.SelectAsync<ItemLog>(@"
					select * 
                    from ItemLog WITH (NOLOCK)
					where
						WorkflowID = @workflowid and
						ItemID = @itemid and
						(@entryType is null or EntryType = @entryType)
					order by Date desc",
					itemData.WorkflowID, itemData.ItemID, entryType);
				return logEntries;
			}
		}


		internal async Task<ItemData> GetItemByID(int workflowid, long itemid)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var itemData = await conn.SelectOneAsync<ItemData>(@"
					select *
                    from ItemData
					where 
						WorkflowID = @workflowid and
						ItemID = @itemid",
					workflowid, itemid);

				return itemData;
			}
		}


		internal async Task<ItemData> GetItemByName(int workflowid, string itemName)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var itemData = await conn.SelectOneAsync<ItemData>(@"
					select *
                    from ItemData 
					where 
						WorkflowID = @workflowid and
						ItemName = @itemName",
					workflowid, itemName);

				return itemData;
			}
		}


		internal async Task<List<ItemData>> FindItems(int workflowid, string itemName, string keywords, DateTime? fromdate, DateTime? todate)
		{
            if(itemName == null && keywords == null)
                throw new InvalidOperationException("You need to specify at least one discriminant value in order to execute FindItems.");

            if (fromdate !=null && todate!=null && fromdate >= todate)
                throw new InvalidOperationException("The start date must be before the end date.");

			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var items = await conn.SelectAsync<ItemData>(@"
					select top 1000 
	                    WorkflowID, ItemID, ParentWorkflowID, ParentItemID, ItemName, Keywords,
                        TaskID, TaskDate, ItemPriority, ItemStatus, StatusReason, RouteCode,
                        DelayedUntil, RetryCount, WorkflowStatus, CompletedFrom, CompletedDate,
                        RejectOnFinally, OutstandingHandler, WakeTimeout, MaxTries, CreatedDate
                    from ItemData with (nolock)
					where 
						WorkflowID = @workflowid and
						(@itemName is null or ItemName like @itemName) and
						(@keywords is null or CHARINDEX(@keywords, [Keywords]) > 0) and
                        (@fromdate is null or TaskDate >= @fromdate) and
                        (@todate is null or TaskDate <= @todate)
					order by ItemID",
					workflowid, itemName, keywords, fromdate, todate);

				return items;
			}
		}

		internal async Task<List<ItemData>> FindItems(int workflowid, int? taskid, ItemStatus? itemstatus, long? itemid, 
            string itemName, string keywords, DateTime? fromdate, DateTime? todate)
		{
            if(taskid == null && itemstatus == null && itemid == null && itemName == null && keywords == null && todate == null && fromdate==null)
				throw new InvalidOperationException("You need to specify at least one discriminant value in order to execute FindItems.");

            if(fromdate != null && todate != null && fromdate >= todate)
                throw new InvalidOperationException("The start date must be before the end date.");

            using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var items = await conn.SelectAsync<ItemData>(@"
					select top 1000 
	                    WorkflowID, ItemID, ParentWorkflowID, ParentItemID, ItemName, Keywords,
                        TaskID, TaskDate, ItemPriority, ItemStatus, StatusReason, RouteCode,
                        DelayedUntil, RetryCount, WorkflowStatus, CompletedFrom, CompletedDate,
                        RejectOnFinally, OutstandingHandler, WakeTimeout, MaxTries, CreatedDate
                    from ItemData with (nolock)
					where 
						WorkflowID = @workflowid and
						(@taskid is null or TaskID = @taskid) and
						(@itemid is null or ItemID = @itemid) and
						(@itemstatus is null or ItemStatus = @itemstatus) and
						(@itemName is null or ItemName like @itemName) and
						(@keywords is null or exists (select * from STRING_SPLIT(@keywords, ',') where CHARINDEX(value, [Keywords]) > 0)) and
                        (@fromdate is null or TaskDate >= @fromdate) and
                        (@todate is null or TaskDate <= @todate)
					order by ItemID",
					workflowid, taskid, itemid, itemstatus, itemName, keywords,fromdate,todate);
				return items;
			}
		}


		internal async Task MakeDelayedInTask(ItemData itemData, string reason, TimeSpan delayTime, IIdentity identity)
		{
			if (String.IsNullOrWhiteSpace(reason))
				throw new InvalidOperationException($"You need to indicate a reason why this operation is being performed.");

			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var delayedUntil = DateTime.Now.Add(delayTime);

				string logMessage;
				logMessage = $"Item was delayed until {delayedUntil} by user {identity.Name}. Reason: {reason}";
				var logData = JsonConvert.SerializeObject(new BaseItemLogData() { Message = logMessage });


				var rowCount = await conn.ExecuteNonQueryAsync(@"
					begin transaction T1
					begin try
						update ItemData set 
							ItemStatus = @newStatus,
							StatusReason = @reason,
							DelayedUntil = @delayedUntil
						where
							WorkflowID = @workflowid and
							ItemID = @itemid and 
							TaskID = @expectedTaskID and
							ItemStatus = @expectedStatus

						if @@ROWCOUNT > 0
							insert into ItemLog(WorkflowID, ItemID, EntryType, EntrySubType, Visibility, AttachedData, Date)
							values(@workflowid, @itemid, 1, 0, 1, @logData, @date)

						commit transaction T1
					end try
					begin catch
						rollback transaction T1
					end catch",
					ItemStatus.Delayed, reason, delayedUntil,
					itemData.WorkflowID, itemData.ItemID, itemData.TaskID, itemData.ItemStatus,
					logData, DateTime.Now);

				if (rowCount == 2)
				{
					itemData.ItemStatus = ItemStatus.Delayed;
					itemData.StatusReason = reason;
					itemData.DelayedUntil = delayedUntil;
				}
				else
				{
					throw new InvalidOperationException("Item cannot be put in the Delayed state because the supplied information is not valid or the item state was changed in a different thread, prease retry.");
				}
			}
		}


		internal async Task MakeActiveInTask(int taskid, ItemData itemData, IIdentity identity)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var delayedUntil = DateTime.Now.AddSeconds(-1);

				string logMessage;
				if (identity != null)
					logMessage = $"Item was set to the active state by user {identity.Name}";
				else
					logMessage = $"Item was set to the active state by anonymous user";

				var logData = JsonConvert.SerializeObject(new BaseItemLogData() { Message = logMessage });

				if (itemData.ItemStatus == ItemStatus.Active ||
					itemData.ItemStatus == ItemStatus.Waiting ||
					itemData.ItemStatus == ItemStatus.Completed ||
					itemData.ItemStatus == ItemStatus.Cancelled)
					throw new InvalidOperationException($"Cannot update item status to Active because it is in a state that does not allow this operation. Current Stauts: {itemData.ItemStatus}");

				var rowCount = await conn.ExecuteNonQueryAsync(@"
					begin transaction T1
					begin try

						update ItemData set 
							ItemStatus = @newStatus,
							DelayedUntil = @delayedUntil
						where
							WorkflowID = @workflowid and
							ItemID = @itemid and 
							TaskID = @expectedTaskID and
							ItemStatus = @expectedStatus

						if @@ROWCOUNT > 0
							insert into ItemLog(WorkflowID, ItemID, EntryType, EntrySubType, Visibility, AttachedData, Date)
							values(@workflowid, @itemid, 1, 0, 1, @logData, @date)

						commit transaction T1
					end try
					begin catch
						rollback transaction T1
					end catch
					",
					ItemStatus.Delayed, delayedUntil,
					itemData.WorkflowID, itemData.ItemID, taskid, itemData.ItemStatus,
					logData, DateTime.Now);

				if (rowCount == 2)
				{
					itemData.ItemStatus = ItemStatus.Active;
					itemData.DelayedUntil = delayedUntil;
				}
				else
				{
					throw new InvalidOperationException("Item cannot be put in the Active state because the supplied information is not valid or the item state was changed in a different thread, prease retry.");
				}
			}
		}


		internal async Task MakeRejectedInTask(ItemData itemData, string reason, IIdentity identity)
		{
			if (String.IsNullOrWhiteSpace(reason))
				throw new InvalidOperationException($"You need to indicate a reason why this operation is being performed.");

			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				string logMessage;
				logMessage = $"Item was rejected by user {identity.Name}. Reason: {reason}";
				var logData = JsonConvert.SerializeObject(new BaseItemLogData() { Message = logMessage });

				var rowCount = await conn.ExecuteNonQueryAsync(@"
					begin transaction T1
					begin try

						update ItemData set 
							ItemStatus = @newStatus,
							StatusReason = @reason
						where
							WorkflowID = @workflowid and
							ItemID = @itemid and 
							TaskID = @expectedTaskID and
							ItemStatus = @expectedStatus

						if @@ROWCOUNT > 0
							insert into ItemLog(WorkflowID, ItemID, EntryType, EntrySubType, Visibility, AttachedData, Date)
							values(@workflowid, @itemid, 1, 0, 1, @logData, @date)

						commit transaction T1
					end try
					begin catch
						rollback transaction T1
					end catch

					",
					ItemStatus.Rejected, reason,
					itemData.WorkflowID, itemData.ItemID, itemData.TaskID, itemData.ItemStatus,
					logData, DateTime.Now);

				if (rowCount == 2)
				{
					itemData.ItemStatus = ItemStatus.Rejected;
					itemData.StatusReason = reason;
				}
				else
				{
					throw new InvalidOperationException("Item cannot be put in the Rejected state because the supplied information is not valid or the item state was changed in a different thread, prease retry.");
				}
			}
		}


		internal async Task<bool> CancelWaitingItem(int workflowid, int taskid, long itemid)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var rowCount = await conn.ExecuteNonQueryAsync($@"
                    update ItemData set
                        ItemStatus = {(int)ItemStatus.Cancelled},
                        StatusReason = 'Item was cancelled in subworkflow',
                        WorkflowStatus = {(int)WorkflowStatus.Cancelled},
                        CompletedFrom = {taskid},
                        CompletedDate = GETDATE(),
                        TaskID = null
                    where
                        WorkflowID = @workflowid and
                        ItemID = @itemid and
                        TaskID = @taskid",
					workflowid, itemid, taskid);

                if(rowCount > 0)
                {
                    events.Send(new ItemCompletedEvent()
                    {
                        WorkflowID = workflowid,
                        ItemID = itemid,
                        WorkflowStatus = WorkflowStatus.Cancelled,
                        Date = DateTime.Now
                    });
                }

                return rowCount > 0;
			}
		}


        internal async Task<int> WakeWaitingItemsByEvent(int workflowid, int taskid, Expression expression, EQEventInfo evt)
        {
            using(var conn = await connManager.OpenDBAsync("APM"))
            {
                var translator = new QueryTranslator();
                var qry = translator.Translate(expression, evt);

                var args = new List<object>()
                {
                    JsonConvert.SerializeObject(evt),       // @evtState
                    DateTime.Now.AddSeconds(-1),            // @delayDate
                    workflowid,
                    taskid
                };            
                args.AddRange(qry.Arguments);               // queryExpression Arguments

                return await conn.ExecuteNonQueryAsync($@"
                    update ItemData set
                        WakeTimeout = null,
                        WakeEventState = @evtState,
                        ItemStatus = {(int)ItemStatus.Delayed},
                        DelayedUntil = @delayDate
                    where
                        WorkflowID = @workflowid
                        and TaskID = @taskid
                        and ItemStatus = {(int)ItemStatus.Waiting}
                        and {qry.QueryExpression}",
                    args.ToArray());
            }
        }


		internal async Task UpdateEditableProperties(ItemData itemData, string itemName, string keywords, ItemPriority itemPriority)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				await conn.ExecuteNonQueryAsync(@"
					update ItemData set
						ItemName = @itemname,
						Keywords = @keywords,
						ItemPriority = @itemPriority
					where
						WorkflowID = @workflowid and
						ItemID = @itemid",
					itemName, keywords, itemPriority,
					itemData.WorkflowID, itemData.ItemID);
			}
		}

		// NOTES:
		//		This method will retry indefinitely until the operation succeeds or cancellation is requested.
		//		The intention here is to be resilient to transient errors in the system database, this is specially important when updating the item state.
		//		Basically what we are saying here is that the task execution process should not lose updates made to the item state just cause the 
		//      database went down for a few moments, at the same time, the entire activity of the task should freeze until the database comes back online.
		//		Still, in case of error we add some logging but not so frequently that it breaks the logging system.
		internal async Task Update(ItemData item, CancellationToken cancellationToken)
		{
			int retryCount = 0;
			bool success = false;
			do
			{
				try
				{
					using (var conn = await connManager.OpenDBAsync("APM"))
					{
						await conn.UpdateAsync(item);
						RaiseEvents(item);
						success = true;
					}
				}
				catch (Exception ex)
				{
					if (retryCount % 60 == 0)
						log.LogException(ex);
					retryCount++;
					await Task.Delay(1000);
				}
				if (cancellationToken.IsCancellationRequested)
					return;
			} while (!success);
		}

		// NOTES:
		//		> This method will retry indefinitely until the operation succeeds or cancellation is requested.
		//		  The intention here is to be resilient to transient errors in the system database, this is specially important when updating the item state.
		//		  Basically what we are saying here is that the task execution process should not lose updates made to the item state just because the 
		//        database went down for a few moments, at the same time, the entire activity of the task should freeze until the database comes back online.
		//		  Still, in case of error we add some logging but not so frequently that it breaks the logging system.
		//		> Additionally, this method also makes sure that the item is still sitting in the task it is believed to be on (expectedTaskID);
		//		  this check is important when attempting to move the item from one task to the next, because if we do not validate this,
		//		  the item would start executing in multiple places at once.
		internal async Task<bool> SafeUpdate(ItemData item, CancellationToken cancellationToken, int? expectedTaskID)
		{
			int retryCount = 0;
			do
			{
				try
				{
					using (var conn = await connManager.OpenDBAsync("APM"))
					{
						string sql;
						var updateStatement = conn.GetUpdateStatement(item);

						if (expectedTaskID != null)
							sql = $"{updateStatement.Item1} and TaskID = {expectedTaskID}";
						else
							sql = updateStatement.Item1;

						var rowsAffected = await conn.ExecuteNonQueryAsync(sql, updateStatement.Item2);
						if (rowsAffected > 0)
						{
							RaiseEvents(item);
						}
						return rowsAffected > 0;
					}
				}
				catch (Exception ex)
				{
					if (retryCount % 60 == 0)
						log.LogException(ex);
					retryCount++;
					await Task.Delay(1000);
				}
				if (cancellationToken.IsCancellationRequested)
					return false;
			} while (true);
		}


		private void RaiseEvents(ItemData itemData)
		{
			if (itemData.WorkflowStatus != WorkflowStatus.InFlow)
			{
				events.Send(new ItemCompletedEvent()
				{
					WorkflowID = itemData.WorkflowID,
					ItemID = itemData.ItemID,
					WorkflowStatus = itemData.WorkflowStatus,
					Date = itemData.CompletedDate.Value
				});
			}
			else
			{
				if (itemData.ItemStatus == ItemStatus.Rejected)
				{
					events.Send(new ItemRejectedEvent()
					{
						WorkflowID = itemData.WorkflowID,
						ItemID = itemData.ItemID,
						TaskID = itemData.TaskID.Value,
						RejectReason = itemData.StatusReason,
						Date = DateTime.Now
					});
				}
			}
		}


		internal async Task<string> GetItemState(int workflowid, long itemid)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var result = await conn.ExecuteScalarAsync(@"
					select ItemState from ItemData
					where 
						WorkflowID = @workflowid and 
						ItemID = @itemid",
						workflowid, itemid
					);

				if (result == null || result is DBNull)
					return null;

				return result.ToString();
			}
		}


		internal async Task UpdateItemState(int workflowid, long itemid, string state)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var rows = await conn.ExecuteNonQueryAsync(@"
					update ItemData set ItemState = @state 
					where 
						WorkflowID = @workflowid and 
						ItemID = @itemid and
						ItemStatus <> @activestatus",
						state, workflowid, itemid, ItemStatus.Active
					);
				if (rows == 0)
					throw new Exception("Operation did not update any item, this can happen if you try to update the state of an item while the item is Active.");
			}
		}


		internal async Task<ItemData> CreateItemForWorkflow<TItem>(int workflowid, TItem item, int? taskid, ItemStatus? itemStatus, string sourceEventState = null)
			where TItem : WorkItem, new()
		{
			ItemData itemData;
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				if (item.Detached)
				{
					itemData = await CreateNewItem(conn, workflowid, item, taskid, itemStatus);
					itemData.SourceEventState = sourceEventState;
					await conn.InsertAsync(itemData);
				}
				else
				{
					itemData = await conn.SelectOneAsync<ItemData>("select * from ItemData where WorkflowID = @workflow and ItemID = @itemid", workflowid, item.ItemID);
					if (itemData != null)
					{
						if (itemData.WorkflowStatus == WorkflowStatus.InFlow)
						{
							var currentTaskName = await conn.ExecuteScalarAsync("select [TaskName] from TaskData where TaskID = @id", itemData.TaskID);
							throw new InvalidOperationException($"Cannot reinsert item {workflowid}/{item.ItemID}/{item.ItemName} into the specified workflow because the item is already being processed in that workflow. CurrentTask: {currentTaskName}, CurrentStatus: {itemData.ItemStatus}.");
						}
						else
						{
							await InsertReactivateItem<TItem>(conn, itemData, taskid, sourceEventState);
						}
					}
					else
					{
						itemData = await CreateNewItem(conn, workflowid, item, taskid, itemStatus);
						itemData.SourceEventState = sourceEventState;
						await conn.InsertAsync(itemData);
					}
				}
			}
			return itemData;
		}

		// Not meant to be called from outside, to reactivate an item through the API call MoveItem instead
		private async Task InsertReactivateItem<TItem>(IDBX conn, ItemData itemData, int? taskid, string sourceEventState = null)
			where TItem : WorkItem, new()
		{
			itemData.TaskID = taskid;
			itemData.TaskDate = DateTime.Now;
			itemData.ItemStatus = ItemStatus.Delayed;
			itemData.DelayedUntil = DateTime.Now.AddSeconds(-1);
			itemData.WorkflowStatus = WorkflowStatus.InFlow;
			itemData.RetryCount = 0;
			itemData.CompletedFrom = null;
			itemData.CompletedDate = null;
			itemData.SourceEventState = sourceEventState;
			await conn.UpdateAsync(itemData);

            string taskName = string.Empty;
            if(taskid.HasValue)
                taskName = (await conn.ExecuteScalarAsync("select [TaskName] from TaskData where TaskID = @id", itemData.TaskID)).ToString();

            await LogMessage(itemData, taskName, $"Item {itemData.ItemName} was reactivated due to a reinsert", ItemLogVisibility.Public);
		}


		private async Task<ItemData> CreateNewItem<TItem>(IDBX conn, int workflowid, TItem item, int? taskid, ItemStatus? itemStatus)
			where TItem : WorkItem, new()
		{
			var itemid = item.ItemID;
			var itemName = item.ItemName;

			if (itemid == 0)
				itemid = await conn.GetNextValueAsync("WFManagerSQ");

			if (itemName == null)
				itemName = $"C{itemid:D7}";

            item.WorkflowID = workflowid;
            item.ItemID = itemid;
            item.TaskID = taskid ?? 0;
            item.ItemName = itemName;

			var itemData = new ItemData()
			{
				WorkflowID = workflowid,
				ItemID = itemid,
				ParentWorkflowID = null,
				ParentItemID = null,
				ItemName = itemName,
				Keywords = "",
				TaskID = taskid,
				TaskDate = DateTime.Now,
				ItemPriority = item.Priority, 
				ItemStatus = itemStatus ?? ItemStatus.Delayed,
				StatusReason = null,
				RouteCode = null,
				DelayedUntil = DateTime.Now.AddSeconds(-1),
				RetryCount = 0,
				WorkflowStatus = WorkflowStatus.InFlow,
				CompletedFrom = null,
				CompletedDate = null,
				ItemState = JsonConvert.SerializeObject(item),
				SourceEventState = null,
				MaxTries = item.MaxTries,
				CreatedDate = DateTime.Now,
			};
			return itemData;
		}


		internal async Task<CounterData> GetWorkflowCounters(int workflowid)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var counters = await conn.SelectAsync<ItemCounter>(@"
					select ItemStatus, count(*) [Counter]
                    from ItemData WITH (NOLOCK)
					where
						WorkflowID = @workflowid
					group by ItemStatus",
					workflowid);
				var result = new CounterData(counters);
				return result;
			}
		}


		internal async Task<List<TaskItemCounter>> GetWorkflowCountersByTaskAsync(int workflowid)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var counters = await conn.SelectAsync<TaskItemCounter>(@"
					select t.TaskID, i.ItemStatus, count(*) [Counter]
                    from ItemData i WITH (NOLOCK)
						join TaskData t on i.TaskID = t.TaskID
					where
						t.WorkflowID = @workflowid
					group by t.TaskID, i.ItemStatus",
					workflowid);
				return counters;
			}
		}


		public static string FormatJsonValue<T>(T v)
		{
			if (typeof(T) == typeof(string) || typeof(T) == typeof(DateTime))
				return $"'{v.ToString().Replace("'", "''")}'";
			return v.ToString();
		}

		internal async Task<CounterData> GetTaskCounters(int workflowid, int taskid)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var counters = await conn.SelectAsync<ItemCounter>(@"
					select ItemStatus, count(*) [Counter]
                    from ItemData WITH (NOLOCK)
					where
						WorkflowID = @workflowid and
						TaskID = @taskid
					group by ItemStatus",
					workflowid, taskid);
				var result = new CounterData(counters);
				return result;
			}
		}

		internal async Task<bool> CanMoveItem(int workflowid, long itemid)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var itemData = await conn.SelectOneAsync<ItemData>(@"
					select WorkflowID, ItemID, DelayedUntil, ItemStatus from ItemData
					where
						WorkflowID = @workflowid and
						ItemID = @itemid",
					workflowid, itemid);

				if (itemData == null)
					throw new InvalidOperationException($"Could not find ItemID {itemid} in workflow {workflowid}.");

				var delayedUntil = itemData.DelayedUntil;

				if (itemData.ItemStatus == ItemStatus.Active || (itemData.ItemStatus == ItemStatus.Delayed && delayedUntil < DateTime.Now.AddSeconds(10)))
					return false;

				return true;
			}
		}


		internal async Task MoveItem(int workflowid, long itemid, int? taskid, ItemStatus status, TimeSpan? delayTime, string reason, IIdentity identity, bool isReactivate)
		{
			if (String.IsNullOrWhiteSpace(reason))
				throw new InvalidOperationException($"You need to indicate a reason why this operation is being performed.");

			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var taskData = await conn.SelectOneAsync<TaskData>(@"
					select TaskID, TaskName from TaskData
					where
						TaskID = @taskid and
						WorkflowID = @workflowid and
						Detached = 0",
					taskid, workflowid);

				if (taskData == null)
					throw new InvalidOperationException($"Could not find TaskID {taskid} in workflow {workflowid}, or the task is detached.");

				var itemData = await conn.SelectOneAsync<ItemData>(@"
					select WorkflowID, ItemID, ItemName, WorkflowStatus,
                        CompletedFrom, DelayedUntil, ItemStatus
                    from ItemData
					where
						WorkflowID = @workflowid and
						ItemID = @itemid",
					workflowid, itemid);

				if (itemData == null)
					throw new InvalidOperationException($"Could not find ItemID {itemid} in workflow {workflowid}.");

				if (isReactivate)
				{
					if (itemData.WorkflowStatus == WorkflowStatus.InFlow)
						throw new InvalidOperationException($"The current Workflow Status of ItemID {itemid} {itemData.ItemName} is 'InFlow'. Item reactivation can only be executed on completed or cancelled items.");
					if (taskid == null)
					{
						if (itemData.CompletedFrom.HasValue)
							taskid = itemData.CompletedFrom.Value;
						else
							throw new InvalidOperationException($"ItemID {itemid} {itemData.ItemName} cannot be reactivated unless a specific taskid is specified.");
					}
				}

				var delayedUntil = itemData.DelayedUntil;

				// Must avoid moving an item that is executing (or within 10 seconds of starting execution).
				if (itemData.ItemStatus == ItemStatus.Active || (itemData.ItemStatus == ItemStatus.Delayed && delayedUntil < DateTime.Now.AddSeconds(10)))
				{
					throw new InvalidOperationException("Cannot move item unless it is in the Waiting, Rejected, Completed or Cancelled states");
				}

				if (status == ItemStatus.Active)
				{
					status = ItemStatus.Delayed;    // Active State should be replaced by delayed to a past date (so that the workflow activates it asap), we should never put an item in the Active state using any API calls.
					delayedUntil = DateTime.Now.AddSeconds(-1);
				}

				if (status == ItemStatus.Delayed)
				{
					if (delayTime.HasValue)
						delayedUntil = DateTime.Now + delayTime.Value;
					else
						delayedUntil = DateTime.Now.AddMinutes(1);
				}

				string logMessage;
				if (isReactivate)
					logMessage = $"Item was reactivated to task {taskData.TaskName} by user {identity.Name}. Reason: {reason}";
				else
					logMessage = $"Item was moved to task {taskData.TaskName} by user {identity.Name}. Reason: {reason}";

				var logData = JsonConvert.SerializeObject(new BaseItemLogData() { Message = logMessage });


				await conn.ExecuteNonQueryAsync(@"
					begin transaction T1
					begin try

						update ItemData set 
							TaskID = @taskid,
							TaskDate = @date,
							ItemStatus = @newItemStatus,
							DelayedUntil = @delayedDate,
							RetryCount = 0,
							WorkflowStatus = @wfStatus,
							CompletedFrom = null,
							CompletedDate = null,
							RejectOnFinally = 0,
							OutstandingHandler = null
						where
							WorkflowID = @workflowid and
							ItemID = @itemid and
							ItemStatus = @lastSeenStatus and
							WorkflowStatus = @oldWorkflowStatus

						if @@ROWCOUNT > 0
							insert into ItemLog(WorkflowID, ItemID, EntryType, EntrySubType, Visibility, AttachedData, Date)
							values(@workflowid, @itemid, 1, 0, 1, @logData, @date)

						commit transaction T1
					end try
					begin catch
						rollback transaction T1
					end catch
				",
				taskid, DateTime.Now, status, delayedUntil, WorkflowStatus.InFlow,
				workflowid, itemid, itemData.ItemStatus, itemData.WorkflowStatus, logData);
			}
		}

		
		internal async Task ChangeItemPriority(int workflowid, long itemid, ItemPriority priority, IIdentity identity)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				string logMessage;
				logMessage = $"Item Priority was updated to {priority}, by user {identity.Name}";
				var logData = JsonConvert.SerializeObject(new BaseItemLogData() { Message = logMessage });

				var rows = await conn.ExecuteNonQueryAsync(@"
					begin transaction T1
					begin try
						update ItemData set 
							ItemPriority = @newPriority
						where
							WorkflowID = @workflowid and
							ItemID = @itemid

						if @@ROWCOUNT > 0
							insert into ItemLog(WorkflowID, ItemID, EntryType, EntrySubType, Visibility, AttachedData, Date)
							values(@workflowid, @itemid, 1, 0, 1, @logData, @date)

						commit transaction T1
					end try
					begin catch
						rollback transaction T1
					end catch
				",
				priority, workflowid, itemid,
				logData, DateTime.Now);

				if (rows == 0)
					throw new Exception("Could not update the item priority, no rows were updated. Please try again.");
			}
		}


		internal async Task CompleteItem(int workflowid, long itemid, string reason, ItemStatus status, IIdentity identity)
		{
			if (String.IsNullOrWhiteSpace(reason))
				throw new InvalidOperationException($"You need to indicate a reason why this operation is being performed.");

			if (status != ItemStatus.Completed && status != ItemStatus.Cancelled)
				throw new InvalidOperationException($"This operations is meant only to cancel or complete items. Specified status {status} is not valid.");

			if (identity == null)
				throw new InvalidOperationException($"You need to indicate the identity of the user that is performing this operation.");

			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				var itemData = await conn.SelectOneAsync<ItemData>(@"
					select WorkflowID, ItemID, TaskID, ItemStatus from ItemData
					where
						WorkflowID = @workflowid and
						ItemID = @itemid",
					workflowid, itemid);

				if (itemData == null)
					throw new InvalidOperationException($"Could not find Item {itemid} in workflow {workflowid}.");

				if (itemData.ItemStatus == ItemStatus.Completed || itemData.ItemStatus == ItemStatus.Cancelled || itemData.TaskID == null)
					throw new InvalidOperationException("Item is already completed or cancelled.");

				if (itemData.ItemStatus == ItemStatus.Active)
					throw new InvalidOperationException("Cannot execute this operation on an active item, wait for the item to be delayed, waiting or rejected and try again.");

				WorkflowStatus wfstatus;
				if (status == ItemStatus.Completed)
					wfstatus = WorkflowStatus.ForcedCompleted;
				else
					wfstatus = WorkflowStatus.Cancelled;

				var currentTask = await conn.SelectOneAsync<TaskData>("select TaskID, TaskName from TaskData where TaskID = @taskid", itemData.TaskID.Value);

				var rows = await conn.ExecuteNonQueryAsync(@"
					update ItemData set
						ItemStatus = @status,
						StatusReason = @reason,
						WorkflowStatus = @wfstatus,
						CompletedFrom = @lastSeenTask,
						CompletedDate = @date,
						TaskID = null
					where
						WorkflowID = @workflowid and
						ItemID = @itemid and
						TaskID = @lastSeenTask and
						ItemStatus = @lastSeenStatus",
					status, reason, wfstatus, itemData.TaskID, DateTime.Now,
					workflowid, itemid, itemData.ItemStatus
				);

				if (rows == 0)
					throw new InvalidOperationException("The state of the item has been modified since last read. The item might be active or might have moved to a different task.");
				else
				{
					if (status == ItemStatus.Completed)
						await LogMessage(itemData, currentTask.TaskName, $"Item {itemData.ItemID} was forced to complete by user {identity.Name}", ItemLogVisibility.Public);
					else
						await LogMessage(itemData, currentTask.TaskName, $"Item {itemData.ItemID} was cancelled by user {identity.Name}", ItemLogVisibility.Public);
				}
			}
		}

		internal async Task CancelAllItemsAsync(int workflowid, string reason)
		{
			using (var conn = await connManager.OpenDBAsync("APM"))
			{
				await conn.ExecuteNonQueryAsync(@"
				update ItemData set
					ItemStatus = @status,
					StatusReason = @reason,
					WorkflowStatus = @wfstatus,
					CompletedFrom = TaskID,
					CompletedDate = GETDATE(),
					TaskID = null
				where WorkflowID = @workflowid",
					ItemStatus.Cancelled, reason, WorkflowStatus.Cancelled, workflowid
				);
			}
		}
	}
}
