using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Service.Contracts.WF;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using WebLink.Contracts;

namespace WebLink.Controllers
{
	[Authorize]
	public class WorkflowsController : Controller
	{
		private IAutomatedProcessManager apm;
		private IPrincipal principal;
		private IUserData userData;
        private ILocalizationService g;
		private ILogService log;

		public WorkflowsController(
			IAutomatedProcessManager apm,
			IPrincipal principal,
			IUserData userData,
            ILocalizationService g,
			ILogService log)
		{
			this.apm = apm;
			this.principal = principal;
			this.userData = userData;
            this.g = g;
			this.log = log;
		}

		[HttpGet, Route("/wf/workflows")]
		public async Task<OperationResult> GetWorkflows()
		{
			try
			{
				var workflows = await apm.GetWorkflowsAsync();
				return new OperationResult(true, null, workflows);
			}
			catch(Exception ex)
			{
				log.LogException(ex);
				return OperationResult.InternalError;
			}
		}

        [HttpPost, Route("/wf/finditems")]
		public async Task<OperationResult> FindItems([FromBody]ItemFindFilter filter)
		{
			try
			{
				if (filter.TaskID == 0) filter.TaskID = null;
				if (filter.ItemID == 0) filter.ItemID = null;
                if (String.IsNullOrWhiteSpace(filter.ItemName)) filter.ItemName = null;
				if (string.IsNullOrWhiteSpace(filter.Keywords)) filter.Keywords = null;
				if (filter.ItemStatus.HasValue && filter.ItemStatus == 0) filter.ItemStatus = null;
              
                var wf = await apm.GetWorkflowAsync(filter.WorkflowID);
				var items = await wf.FindItemsAsync(filter.TaskID, filter.ItemStatus, filter.ItemID, filter.ItemName, filter.Keywords, filter.FromDate, filter.ToDate);
				return new OperationResult(true, null, items);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
                return new OperationResult(false, ex.Message);
            }
		}

		[HttpPost, Route("wf/{workflowid}/moveitems")]
		public async Task<OperationResult> MoveItems(int workflowid, [FromBody] ItemMoveDTO operationInfo)
		{
			try
			{
				if (!userData.IsIDTAdminRoles)
					return OperationResult.Forbid;

				TimeSpan? delayTime = null;
				if (operationInfo.Minutes > 0)
					delayTime = TimeSpan.FromMinutes(operationInfo.Minutes);

				var wf = await apm.GetWorkflowAsync(workflowid);
				foreach (var itemid in operationInfo.Items)
				{
					var item = await wf.FindItemAsync(itemid);
					await wf.MoveAsync(item, operationInfo.TaskID, operationInfo.ItemStatus, delayTime, operationInfo.Reason, principal.Identity);
				}
				return new OperationResult(true, g["Item(s) moved successfully!"], null);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return OperationResult.InternalError;
			}
		}


		[HttpPost, Route("wf/{workflowid}/retry")]
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
					await wf.MakeActiveAsync(item, principal.Identity);
				}
				return new OperationResult(true, g["Item(s) updated successfully!"], null);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return OperationResult.InternalError;
			}
		}


		[HttpPost, Route("wf/{workflowid}/delay")]
		public async Task<OperationResult> DelayItems(int workflowid, [FromBody] ItemDelayDTO operationInfo)
		{
			try
			{
				if (!userData.IsIDTAdminRoles)
					return OperationResult.Forbid;

				if (operationInfo.Minutes < 1)
					operationInfo.Minutes = 1;

				var delayTime = TimeSpan.FromMinutes(operationInfo.Minutes);

				var wf = await apm.GetWorkflowAsync(workflowid);
				foreach (var itemid in operationInfo.Items)
				{
					var item = await wf.FindItemAsync(itemid);
					await wf.DelayAsync(item, operationInfo.Reason, delayTime, principal.Identity);
				}
				return new OperationResult(true, g["Item(s) delayed successfully!"], null);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return OperationResult.InternalError;
			}
		}


		[HttpPost, Route("wf/{workflowid}/reject")]
		public async Task<OperationResult> RejectItems(int workflowid, [FromBody] ItemOperationDTO operationInfo)
		{
			try
			{
				if (!userData.IsIDTAdminRoles)
					return OperationResult.Forbid;

				var wf = await apm.GetWorkflowAsync(workflowid);
				foreach (var itemid in operationInfo.Items)
				{
					var item = await wf.FindItemAsync(itemid);
					await wf.RejectAsync(item, operationInfo.Reason, principal.Identity);
				}
				return new OperationResult(true, g["Item(s) rejected successfully!"], null);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return OperationResult.InternalError;
			}
		}


		[HttpPost, Route("wf/{workflowid}/changestatus")]
		public async Task<OperationResult> ChangeItemsStatus(int workflowid, [FromBody] ItemStatusUpdateDTO operationInfo)
		{
			try
			{
				if (!userData.IsIDTAdminRoles)
					return OperationResult.Forbid;

				var wf = await apm.GetWorkflowAsync(workflowid);
				foreach (var itemid in operationInfo.Items)
				{
					var item = await wf.FindItemAsync(itemid);
					switch (operationInfo.ItemStatus)
					{
						case ItemStatus.Completed:
							await wf.CompleteAsync(item, operationInfo.Reason, principal.Identity);
							break;
						case ItemStatus.Cancelled:
							await wf.CancelAsync(item, operationInfo.Reason, principal.Identity);
							break;
						case ItemStatus.Active:
							await wf.MakeActiveAsync(item, principal.Identity);
							break;
						case ItemStatus.Delayed:
							await wf.DelayAsync(item, operationInfo.Reason, TimeSpan.FromMinutes(60), principal.Identity);
							break;
						case ItemStatus.Rejected:
							await wf.RejectAsync(item, operationInfo.Reason, principal.Identity);
							break;
						case ItemStatus.Waiting:
							throw new InvalidOperationException("Cannot change an item status to Waiting");
					}
				}
				return new OperationResult(true, g["Item(s) updated successfully!"], null);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return OperationResult.InternalError;
			}
		}


		[HttpPost, Route("wf/{workflowid}/changepriority")]
		public async Task<OperationResult> ChangeItemsPriority(int workflowid, [FromBody] ItemPriorityUpdateDTO operationInfo)
		{
			try
			{
				if (!userData.IsIDTAdminRoles)
					return OperationResult.Forbid;

				var wf = await apm.GetWorkflowAsync(workflowid);
				foreach (var itemid in operationInfo.Items)
				{
					var item = await wf.FindItemAsync(itemid);
					await wf.ChangePriorityAsync(item, operationInfo.ItemPriority, principal.Identity);
				}
				return new OperationResult(true, g["Item(s) updated successfully!"], null);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return OperationResult.InternalError;
			}
		}

		[HttpPost, Route("wf/{workflowid}/complete")]
		public async Task<OperationResult> CompleteItems(int workflowid, [FromBody] ItemOperationDTO operationInfo)
		{
			try
			{
				if (!userData.IsIDTAdminRoles)
					return OperationResult.Forbid;

				var wf = await apm.GetWorkflowAsync(workflowid);
				foreach (var itemid in operationInfo.Items)
				{
					var item = await wf.FindItemAsync(itemid);
					await wf.CompleteAsync(item, operationInfo.Reason, principal.Identity);
				}
				return new OperationResult(true, g["Item(s) completed successfully!"], null);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return OperationResult.InternalError;
			}
		}

		[HttpPost, Route("wf/{workflowid}/cancel")]
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
					await wf.CancelAsync(item, operationInfo.Reason, principal.Identity);
				}
				return new OperationResult(true, g["Item(s) cancelled successfully!"], null);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return OperationResult.InternalError;
			}
		}

		[HttpPost, Route("wf/{workflowid}/reactivate")]
		public async Task<OperationResult> ReactivateItems(int workflowid, [FromBody] ItemMoveDTO operationInfo)
		{
			try
			{
				if (!userData.IsIDTAdminRoles)
					return OperationResult.Forbid;

				TimeSpan? delayTime = null;
				if (operationInfo.Minutes > 0)
					delayTime = TimeSpan.FromMinutes(operationInfo.Minutes);

				var wf = await apm.GetWorkflowAsync(workflowid);
				foreach (var itemid in operationInfo.Items)
				{
					var item = await wf.FindItemAsync(itemid);
					await wf.ReactivateAsync(item, operationInfo.TaskID, operationInfo.ItemStatus, delayTime, operationInfo.Reason, principal.Identity);
				}
				return new OperationResult(true, g["Item(s) reactivated successfully!"], null);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return OperationResult.InternalError;
			}
		}
	}

	public class ItemFindFilter
	{
		public int WorkflowID;
		public int? TaskID;
		public ItemStatus? ItemStatus;
		public long? ItemID;
		public string ItemName;
		public string Keywords;
        public DateTime? FromDate;
        public DateTime? ToDate;
	}
	public class ItemMoveDTO
	{
		public List<long> Items;
		public ItemStatus ItemStatus;
		public int Minutes;
		public int TaskID;
		public string Reason;
	}

	public class ItemOperationDTO
	{
		public List<long> Items;
		public string Reason;
	}

	public class ItemStatusUpdateDTO
	{
		public List<long> Items;
		public ItemStatus ItemStatus;
		public string Reason;
	}

	public class ItemDelayDTO
	{
		public List<long> Items;
		public int Minutes;
		public string Reason;
	}

	public class ItemPriorityUpdateDTO
	{
		public List<long> Items;
		public ItemPriority ItemPriority;
	}
}