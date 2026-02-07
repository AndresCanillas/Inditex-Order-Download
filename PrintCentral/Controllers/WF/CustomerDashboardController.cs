using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Service.Contracts.WF;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Contracts.Services;
using WebLink.Contracts.Workflows;

namespace WebLink.Controllers
{
	[Authorize]
	public class CustomerDashboardController : Controller
	{
		private readonly static object syncObj = new object();
		private static Dictionary<int, string> Tasks = null;

		private readonly IAppConfig config;
		private readonly IUserData userData;
		private readonly EntityCacheService cache;
		private readonly IWorkflowQueries workflowQueries;
		private readonly ILogService log;
		private readonly IProjectRepository projectRepository;
		private readonly IUserRepository userRepository;
		private readonly IEventQueue events;
		private readonly IAutomatedProcessManager apm;
		private readonly ILocalizationService g;

		public CustomerDashboardController(
			IAppConfig config,
			IUserData userData,
			EntityCacheService cache,
			IWorkflowQueries workflowQueries,
			ILogService log,
			IProjectRepository projectRepository,
			IUserRepository userRepository,
			IEventQueue events,
			IAutomatedProcessManager apm,
			ILocalizationService g
			)
		{
			this.config = config;
			this.userData = userData;
			this.cache = cache;
			this.workflowQueries = workflowQueries;
			this.log = log;
			this.projectRepository = projectRepository;
			this.userRepository = userRepository;
			this.events = events;	
			this.apm = apm;	
			this.g = g;
		}

		private async Task<Dictionary<int, string>> GetCustomerDashboardTasks()
		{
			lock (syncObj)
			{
				if (Tasks != null)
					return Tasks;
			}

			var dashboardTasks = config.Bind<List<DashboardTask>>("WFDashboards.CustomerDashboard.Tasks");
			if (dashboardTasks != null)
			{
				var allTasks = await workflowQueries.GetTasks();

				lock (syncObj)
				{
					// NOTE: Double lock check is necessary because another thread could have loaded the tasks by now
					if (Tasks != null)
						return Tasks;

					Tasks = new Dictionary<int, string>();
					foreach (var dasboardTask in dashboardTasks)
					{
						var wftask = allTasks.FirstOrDefault(t => String.Compare(t.TaskName, dasboardTask.Name.Trim(), true) == 0);
						if (wftask != null)
							Tasks.Add(wftask.TaskID, dasboardTask.Description);
					}
				}
			}

			return Tasks;
		}

		[HttpGet, Route("/wf/Dashboard/Customer/GetCountersByTask")]
		public async Task<OperationResult> GetCountersByTask()
		{
			List<TaskInfo> CountersByTaskResult = new List<TaskInfo>();

			var tasks = await GetCustomerDashboardTasks();

            if(tasks != null)
            {
                //initialize CountersByTaskResult
                foreach(var task in tasks)
                {
                    CountersByTaskResult.Add(new TaskInfo(task.Key, task.Value, 0, 0));
                }

                var filter = new ItemFilter();
                filter.Tasks = tasks.Keys;
                filter.Statuses = new ItemStatus[] { ItemStatus.Waiting, ItemStatus.Rejected };

                var counters = await workflowQueries.GetItemCountersGroupedByTaskAsync(filter);

                foreach(var counter in counters)
                {
                    var t = CountersByTaskResult.FirstOrDefault(o => o.TaskName == tasks[counter.Value.TaskID]);
                    if(t != null)
                    {
                        t.Waiting = counter.Value.Counters.Waiting;
                        t.Rejected = counter.Value.Counters.Rejected;
                    }
                }
            }

			return new OperationResult(true, null, CountersByTaskResult);
		}

		[HttpGet, Route("/wf/Dashboard/Customer/GetCustomersList")]
		public OperationResult GetCustomersList()
		{
			var customers = userRepository.GetCustomerServiceUsersWithProjects();

			// If user is not sysadmin only can see himself
			if (!userData.UserRoles.Contains<String>("SysAdmin"))
			{
				customers = customers.Where(o => o.Id == userData.Id).ToList();
			}

			return new OperationResult(true, null, customers);
		}

		[HttpGet, Route("/wf/Dashboard/Customer/GetCountersByTaskForUser/{userid}")]
		public async Task<OperationResult> GetCountersByTaskForUser(string userid)
		{
			if (!userData.IsSysAdmin && userid != userData.Id) return OperationResult.Forbid;

			List<TaskInfo> CountersByTaskResult = new List<TaskInfo>();
			List<int> projectsAssignedToUser = projectRepository.GetProjectsForCustomerService(userid).ToList();

			if (projectsAssignedToUser.Count > 0)
			{
				var tasks = await GetCustomerDashboardTasks();

                if(tasks != null)
                {
                    //initialize CountersByTaskResult
                    foreach(var task in tasks)
                    {
                        CountersByTaskResult.Add(new TaskInfo(task.Key, task.Value, 0, 0));
                    }

                    var filter = new ItemFilter();
                    filter.Tasks = tasks.Keys;
                    filter.Statuses = new ItemStatus[] { ItemStatus.Waiting, ItemStatus.Rejected };
                    var counters = await workflowQueries.GetItemCountersGroupedByTaskAsync<OrderFileItem>(filter, p => p.ProjectID.IN(projectsAssignedToUser));

                    foreach(var counter in counters)
                    {
                        var t = CountersByTaskResult.FirstOrDefault(o => o.TaskName == tasks[counter.Value.TaskID]);
                        if(t != null)
                        {
                            t.Waiting = counter.Value.Counters.Waiting;
                            t.Rejected = counter.Value.Counters.Rejected;
                        }
                    }
                }
			}
			return new OperationResult(true, null, CountersByTaskResult.OrderBy(o => o.TaskName));
		}

		[HttpGet, Route("/wf/Dashboard/Customer/GetItemsWithErrors")]
		public async Task<OperationResult> GetItemsWithErrors(
			[FromQuery] int taskID,
			[FromQuery] ItemStatus itemStatus,
			[FromQuery] string userid)
		{
			if (!userData.IsSysAdmin && userid != userData.Id) return OperationResult.Forbid;

			var filter = new PagedItemFilter()
			{
				Tasks = new int[] { taskID },
				Statuses = new ItemStatus[] { itemStatus },
				IncludedStateProperties = new string[] { "FileName", "WorkflowFileID", "OrderNumber","OrderID" },
			};

			IEnumerable<WorkItemError> ItemsWithErrors;

			if (userid != null)
			{
				List<int> projectsAssignedToUser = projectRepository.GetProjectsForCustomerService(userid).ToList();
				ItemsWithErrors = await workflowQueries.GetTaskErrorsByItemStateAsync<OrderFileItem>(filter, p => p.ProjectID.IN(projectsAssignedToUser));
			}
			else
			{
				ItemsWithErrors = await workflowQueries.GetTaskErrorsAsync(filter);
			}

			var result = new List<ItemErrorInfo>();

			foreach (var item in ItemsWithErrors)
			{
				var project = cache.ProjectCache.GetByID(item.ProjectID);
				var brand = cache.BrandCache.GetByID(project.BrandID);
				var company = cache.CompanyCache.GetByID(brand.CompanyID);

				var itemErrorInfo = new ItemErrorInfo();

				itemErrorInfo.WorkflowID = item.WorkflowID;
				itemErrorInfo.TaskID = item.TaskID;
				itemErrorInfo.ItemID = item.ItemID;
				itemErrorInfo.ItemName = item.ItemName;
				itemErrorInfo.ItemStatus = item.ItemStatus;
				itemErrorInfo.ItemState = item.ItemState;
				itemErrorInfo.CreatedDate = item.CreatedDate;
				itemErrorInfo.ProjectID = item.ProjectID;
				itemErrorInfo.LastErrorMessage = item.LastErrorMessage;
				itemErrorInfo.LastErrorDate = item.LastErrorDate;
				itemErrorInfo.LastErrorTaskID = item.LastErrorTaskID;
				itemErrorInfo.ProjectName = project.Name;
				itemErrorInfo.BrandName = brand.Name;
				itemErrorInfo.CompanyName = company.Name;
				itemErrorInfo.Reference = item.ExtraProperties.Where(o => o.Name == "FileName").FirstOrDefault().Value;
				if (itemErrorInfo.Reference == String.Empty) 
				{
					itemErrorInfo.Reference = item.ExtraProperties.Where(o => o.Name == "OrderNumber").FirstOrDefault().Value;
				}
				itemErrorInfo.WorkflowFileID = item.ExtraProperties.Where(o => o.Name == "WorkflowFileID").FirstOrDefault().Value; 
				itemErrorInfo.OrderID = item.ExtraProperties.Where(o => o.Name == "OrderID").FirstOrDefault().Value;

				result.Add(itemErrorInfo);
			}

			return new OperationResult(true, null, result);
		}

        [HttpGet, Route("/wf/Dashboard/Customer/GetUserTotalItemsAsync/{userid}")]
        public async Task<int> GetUserTotalItemsAsync(String userid)
		{

            List<TaskInfo> CountersByTaskResult = new List<TaskInfo>();
            List<int> projectsAssignedToUser = projectRepository.GetProjectsForCustomerService(userid).ToList();

            if (projectsAssignedToUser.Count > 0)
            {
                var tasks = await GetCustomerDashboardTasks();

                //initialize CountersByTaskResult
                foreach (var task in tasks)
                {
                    CountersByTaskResult.Add(new TaskInfo(task.Key, task.Value, 0, 0));
                }

                var filter = new ItemFilter();
                filter.Tasks = tasks.Keys;
                filter.Statuses = new ItemStatus[] { ItemStatus.Waiting, ItemStatus.Rejected };
                var counters = await workflowQueries.GetItemCountersGroupedByTaskAsync<OrderFileItem>(filter, p => p.ProjectID.IN(projectsAssignedToUser));

                foreach (var counter in counters)
                {
                    var t = CountersByTaskResult.FirstOrDefault(o => o.TaskName == tasks[counter.Value.TaskID]);
                    if (t != null)
                    {
                        t.Waiting = counter.Value.Counters.Waiting;
                        t.Rejected = counter.Value.Counters.Rejected;
                    }
                }
            }
            return CountersByTaskResult.Sum(p => p.Waiting + p.Rejected);

            
		}

		[HttpPost, Route("/wf/Dashboard/Customer/{workflowid}/cancel")]
		public async Task<OperationResult> CancelItems(int workflowid, [FromBody] ItemOperationDTO operationInfo)
		{
			try
			{
				if (!userData.IsIDTAdminRoles)
					return OperationResult.Forbid;

				var wf = await apm.GetWorkflowAsync(workflowid);
				foreach (var itemid in operationInfo.Items)
				{
					var item = await wf.FindItemAsync(itemid);
					await wf.CancelAsync(item, operationInfo.Reason, userData.Principal.Identity);
				}
				events.Send(new DashboardRefreshEvent());

				return new OperationResult(true, g["Item(s) cancelled successfully!"], null);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return OperationResult.InternalError;
			}
		}

		[HttpPost, Route("/wf/Dashboard/Customer/{workflowid}/retry")]
		public async Task<OperationResult> RetryItems(int workflowid, [FromBody] ItemOperationDTO operationInfo)
		{
			try
			{
				if (!userData.IsIDTAdminRoles)
					return OperationResult.Forbid;

				var wf = await apm.GetWorkflowAsync(workflowid);
				foreach (var itemid in operationInfo.Items)
				{
					var item = await wf.FindItemAsync(itemid);
					await wf.MakeActiveAsync(item, userData.Principal.Identity);
				}

				events.Send(new DashboardRefreshEvent());	

				return new OperationResult(true, g["Item(s) updated successfully!"], null);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return OperationResult.InternalError;
			}
		}

		private class DashboardTask
		{
			public string Name { get; set; }
			public string Description { get; set; }
		}

		private class ItemErrorInfo : WorkItemError
		{
			public string ProjectName { get; set; }
			public string BrandName { get; set; }
			public string CompanyName { get; set; }
			public string Reference { get; set; }
			public string WorkflowFileID { get; set; }
			public string  OrderID { get; set; }
		}

		private class TaskInfo
		{
			public int TaskID { get; set; }
			public string TaskName { get; }
			public int Waiting { get; set; }
			public int Rejected { get; set; }
			public TaskInfo(int taskID, string taskName, int waiting, int rejected)
			{
				TaskID = taskID;
				TaskName = taskName;
				Waiting = waiting;
				Rejected = rejected;
			}
		}
	}
}