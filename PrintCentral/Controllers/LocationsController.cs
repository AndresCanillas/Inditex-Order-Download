using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Controllers
{
	[Authorize]
	public class LocationsController : Controller
	{
		private ILocationRepository repo;
        private IUserData userData;
        private ILocalizationService g;
		private ILogService log;

		public LocationsController(
			ILocationRepository repo,
            IUserData userData,
            ILocalizationService g,
			ILogService log)
		{
			this.repo = repo;
            this.userData = userData;
            this.g = g;
			this.log = log;
		}

		[HttpPost, Route("/locations/insert")]
		public OperationResult Insert([FromBody]Location data)
		{
			try
			{
                if (!userData.Admin_Locations_CanAdd)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Location Created!"], repo.Insert(data));
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}


		[HttpPost, Route("/locations/update")]
		public OperationResult Update([FromBody]Location data)
		{
			try
			{
                if (!userData.Admin_Locations_CanEdit)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Location Saved!"], repo.Update(data));
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}


		[HttpPost, Route("/locations/delete/{id}")]
		public OperationResult Delete(int id)
		{
			try
			{
                if (!userData.Admin_Locations_CanDelete)
                    return OperationResult.Forbid;
                repo.Delete(id);
				return new OperationResult(true, g["Location Deleted!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/locations/rename/{id}/{name}")]
		public OperationResult Rename(int id, string name)
		{
			try
			{
                if (!userData.Admin_Locations_CanRename)
                    return OperationResult.Forbid;
                repo.Rename(id, name);
				return new OperationResult(true, "Location Renamed!");
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpGet, Route("/locations/getbyid/{id}")]
		public ILocation GetByID(int id)
		{
			try
			{
				return repo.GetByID(id);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[HttpGet, Route("/locations/getlist")]
		public List<ILocation> GetList()
		{
			try
			{
				return repo.GetList();
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[HttpGet, Route("/locations/getbycompanyid/{id}")]
		public List<ILocation> GetByCompanyID(int id)
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
	}
}