using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts.PrintCentral;
using Services.Core;
using System;
using WebLink.Contracts.Models;

namespace WebLink.Controllers
{
	[Authorize]
	public class VariableDataController : Controller
	{
		private IVariableDataRepository repo;
		private ILogService log;

		public VariableDataController(
			IVariableDataRepository repo,
			ILogService log)
		{
			this.log = log;
			this.repo = repo;
		}

		[HttpGet, Route("/variabledata/getbyid/{projectid}/{id}")]
		public IVariableData GetByID(int projectid, int id)
		{
			try
			{
				return repo.GetByID(projectid, id);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[HttpGet, Route("/variabledata/getbycode/{projectid}/{barcode}")]
		public IVariableData GetByCode(int projectid, string barcode)
		{
			try
			{
				return repo.GetByBarcode(projectid, barcode);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

        [HttpGet, Route("/variabledata/getbydetail/{projectid}/{detailID}")]
        public IVariableData GetByDetailID(int projectid, int detailID)
        {
            try
            {
                return repo.GetByDetailID(projectid, detailID);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }
    }
}