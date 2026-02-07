using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Service.Contracts.Authentication;
using Services.Core;
using System;
using WebLink.Contracts.Models;

namespace WebLink.Controllers
{
    [Authorize(Roles = Roles.SysAdmin)]
    public class OrderWorkflowConfigController : Controller
    {
        private readonly IOrderWorkflowConfigRepository repo;
        private readonly ILogService log;
		private readonly ILocalizationService g;

        public OrderWorkflowConfigController(IOrderWorkflowConfigRepository repo, ILocalizationService g, ILogService log)
        {
            this.repo = repo;
			this.g = g;
			this.log = log;
		}


		[HttpGet, Route("/orderworkflowconfig/getbyprojectid/{id}")]
		public IOrderWorkflowConfig GetByProjectID(int id)
		{
			try
			{
				return repo.GetByProjectID(id);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}


		[HttpPost, Route("/orderworkflowconfig/update")]
        public OperationResult UpdateParams([FromBody]OrderWorkflowConfig data)
        {
            try
            {
				if(data.ID == 0)
					return new OperationResult(true, g["Order Workflow Configuration Created!"], repo.Insert(data));
				else
					return new OperationResult(true, g["Order Workflow Configuration Updated!"], repo.Update(data));
            }
            catch (Exception ex)
            {
                log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
        }
	}
}