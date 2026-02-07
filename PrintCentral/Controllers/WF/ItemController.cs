using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Contracts;
using Services.Core;
using System;
using System.Threading.Tasks;
using WebLink.Contracts;

namespace WebLink.Controllers
{
	[Authorize]
	public class ItemController : Controller
	{
		private IAutomatedProcessManager apm;
		private IUserData userData;
		private ILogService log;

		public ItemController(
			IAutomatedProcessManager apm,
            IUserData userData,
			ILogService log)
		{
			this.apm = apm;
			this.userData = userData;
			this.log = log;
		}

		[HttpGet, Route("wf/{workflowid}/{itemid}/state")]
		public async Task<OperationResult> GetItemPersistedState(int workflowid, long itemid)
		{
			try
			{
				if (!userData.IsIDTAdminRoles)
					return OperationResult.Forbid;

				var wf = await apm.GetWorkflowAsync(workflowid);
				var item = await wf.FindItemAsync(itemid);
				var jo = JObject.Parse(await item.GetSavedStateAsync());
				return new OperationResult(true, null, jo.ToString(Formatting.Indented));
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return OperationResult.InternalError;
			}
		}


		[HttpPost, Route("wf/{workflowid}/{itemid}/state")]
		public async Task<OperationResult> SetItemPersistedState(int workflowid, long itemid, [FromBody]string state)
		{
			try
			{
				if (!userData.IsIDTAdminRoles)
					return OperationResult.Forbid;

				var wf = await apm.GetWorkflowAsync(workflowid);
				var item = await wf.FindItemAsync(itemid);
				await item.UpdateSavedStateAsync(state);
				return new OperationResult(true, "Item State was updated");
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return OperationResult.InternalError;
			}
		}


		[HttpGet, Route("wf/{workflowid}/{itemid}/history")]
		public async Task<OperationResult> GetItemHistory(int workflowid, long itemid)
		{
			try
			{
				if (!userData.IsIDTAdminRoles)
					return OperationResult.Forbid;

				var wf = await apm.GetWorkflowAsync(workflowid);
				var item = await wf.FindItemAsync(itemid);
				var history = await item.GetHistoryAsync();
				return new OperationResult(true, null, history);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return OperationResult.InternalError;
			}
		}


		[HttpGet, Route("wf/{workflowid}/{itemid}/log")]
		public async Task<OperationResult> GetItemLog(int workflowid, long itemid)
		{
			try
			{
				if (!userData.IsIDTAdminRoles)
					return OperationResult.Forbid;

				var wf = await apm.GetWorkflowAsync(workflowid);
				var item = await wf.FindItemAsync(itemid);
				var log = await item.GetLogAsync();
				return new OperationResult(true, null, log);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return OperationResult.InternalError;
			}
		}

        [HttpGet, Route("wf/{workflowid}/{itemid}")]
        public async Task<OperationResult> GetItem(int workflowid, long itemid)
        {
            try
            {
                if(!userData.IsIDTAdminRoles)
                    return OperationResult.Forbid;

                var wf = await apm.GetWorkflowAsync(workflowid);
                var item = await wf.FindItemAsync(itemid);
                return new OperationResult(true, null, item);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return OperationResult.InternalError;
            }
        }
    }
}