using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Service.Contracts.WF;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebLink.Contracts;

namespace WebLink.Controllers
{
	[Authorize]
	public class TasksController : Controller
	{
		private IAutomatedProcessManager apm;
		private ILogService log;

		public TasksController(
			IAutomatedProcessManager apm,
			ILogService log)
		{
			this.log = log;
			this.apm = apm;
		}

		[HttpGet, Route("/wf/tasks/{workflowid}")]
		public async Task<OperationResult> GetTasks(int workflowid)
		{
			try
			{
				var wf = await apm.GetWorkflowAsync(workflowid);
				var result = new List<WFTaskDTO>();

				var tasks = wf.GetTasks();
				var counters = await wf.GetItemCountersByTaskAsync();
				foreach(var t in tasks)
				{
					if (!counters.TryGetValue(t.TaskID, out var counterData))
						counterData = new TaskCounterData();

					result.Add(new WFTaskDTO()
					{
						TaskID = t.TaskID,
						TaskName = t.TaskName,
						CanRunOutOfFlow = t.CanRunOutOfFlow,
						IsDetached = t.IsDetached,
						Active = counterData.Counters.Active,
						Waiting = counterData.Counters.Waiting,
						Delayed = counterData.Counters.Delayed,
						Rejected = counterData.Counters.Rejected,
						Completed = counterData.Counters.Completed,
						Cancelled = counterData.Counters.Cancelled
					});
				}

				var detachedTasks = wf.GetDetachedTasks();
				foreach (var t in detachedTasks)
				{
					if (!counters.TryGetValue(t.TaskID, out var counterData))
						counterData = new TaskCounterData();

					result.Add(new WFTaskDTO()
					{
						TaskID = t.TaskID,
						TaskName = t.TaskName,
						CanRunOutOfFlow = t.CanRunOutOfFlow,
						IsDetached = t.IsDetached,
						Active = counterData.Counters.Active,
						Waiting = counterData.Counters.Waiting,
						Delayed = counterData.Counters.Delayed,
						Rejected = counterData.Counters.Rejected,
						Completed = counterData.Counters.Completed,
						Cancelled = counterData.Counters.Cancelled
					});
				}

				return new OperationResult(true, null, result);
			}
			catch(Exception ex)
			{
				log.LogException(ex);
				return OperationResult.InternalError;
			}
		}


		[HttpGet, Route("/wf/tasks/{workflowid}/counters")]
		public async Task<OperationResult> GetCounters(int workflowid)
		{
			try
			{
				var wf = await apm.GetWorkflowAsync(workflowid);
				var result = await wf.GetItemCountersAsync();
				return new OperationResult(true, null, result);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return OperationResult.InternalError;
			}
		}
	}

	public class WFTaskDTO
	{
		public int TaskID;
		public string TaskName;
		public bool CanRunOutOfFlow;
		public bool IsDetached;
		public int Active;
		public int Waiting;
		public int Delayed;
		public int Rejected;
		public int Completed;
		public int Cancelled;
	}
}