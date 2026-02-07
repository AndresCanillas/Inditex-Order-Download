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
    public class RfidConfigController : Controller
    {
        private readonly IRFIDConfigRepository repo;
        private readonly ILogService log;
		private readonly ILocalizationService g;

        public RfidConfigController(IRFIDConfigRepository repo, ILocalizationService g, ILogService log)
        {
            this.repo = repo;
			this.g = g;
			this.log = log;
		}

		[HttpGet, Route("/rfidconfig/getbycompanyid/{id}")]
		public IRFIDConfig GetByCompanyID(int id)
		{
			try
			{
				return repo.GetByCompanyID(id);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[HttpGet, Route("/rfidconfig/getbybrandid/{id}")]
		public IRFIDConfig GetByBrandID(int id)
		{
			try
			{
				return repo.GetByBrandID(id);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[HttpGet, Route("/rfidconfig/getbyprojectid/{id}")]
		public IRFIDConfig GetByProjectID(int id)
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


		[HttpPost, Route("/rfidconfig/update")]
        public OperationResult UpdateParams([FromBody]RFIDConfig data)
        {
            try
            {
				if(data.ID == 0)
					return new OperationResult(true, g["RFID Configuration Created!"], repo.Insert(data));
				else
					return new OperationResult(true, g["RFID Configuration Updated!"], repo.Update(data));
            }
            catch (Exception ex)
            {
                log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
        }

		[HttpPost, Route("/rfidconfig/updatesequence/{id}/{serial}")]
		public OperationResult UpdateSequence(int id, int serial)
		{
			try
			{
				repo.UpdateSequence(id, serial);
				return new OperationResult(true, g["Serials sequence updated!"], null);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}
	}
}