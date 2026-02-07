using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Contracts.Platform.PoolFiles;

namespace WebLink.Controllers
{
  //  [Authorize]
    public class OrderPoolController : Controller
    {
		private readonly IFactory factory;
		private readonly IProjectRepository projectRepository;
		private readonly IUserData userData;
		private readonly ILogService log;
        private readonly ILocalizationService g;
        private readonly IOrderUtilService orderUtilService;

        public OrderPoolController(
			IFactory factory,
			IProjectRepository projectRepository,
            IUserData userData,
            ILogService log, 
            ILocalizationService g,
            IOrderUtilService orderUtilService)
        {
			this.factory = factory;
			this.projectRepository = projectRepository;
            this.userData = userData;
            this.log = log;
            this.g = g;
            this.orderUtilService = orderUtilService;
        }

        [HttpPost, Route("/orderpool/upload")]
        public async Task<OperationResult> UploadPoolFile()
        {
			Type handlerType;
			IPoolFileHandler handler;

			// Only smartdots users can upload a pool file
			if (userData.CompanyID != 1)
				return new OperationResult(false, g["User does not have the requred permissions."]);

			// A file is required
			if (Request.Form.Files == null || Request.Form.Files.Count != 1)
				return new OperationResult(false, g["Endpoint expects a single file."]);

			try
			{
				var file = Request.Form.Files[0];
				using(var stream = file.OpenReadStream())
				{
                    //await handler.UploadAsync(project, stream);
                    await orderUtilService.SendToPool(userData.SelectedProjectID, file.Name, stream);
                }

				return new OperationResult(true, "");
            }
            catch (Exception ex)
            {
                log.LogException(ex);
				return new OperationResult(false, ex.Message);
			}
		}

        [HttpPost, Route("/orderpool/Insert/{projectid}")]
        public async Task<OperationResult> InsertAsync([FromBody] List<OrderPool> orderPools, int projectid)
        {
            try
            {
                await orderUtilService.SaveOrderPoolList(projectid, orderPools);
                return new OperationResult(true, "");

            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, "");
            }
        }
	}
}