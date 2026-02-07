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
	public class MaterialsController : Controller
	{
		private IMaterialRepository repo;
        private IUserData userData;
        private ILocalizationService g;
		private ILogService log;

		public MaterialsController(
			IMaterialRepository repo,
            IUserData userData,
            ILocalizationService g,
			ILogService log)
		{
			this.log = log;
			this.repo = repo;
            this.userData = userData;
            this.g = g;
		}

		[HttpPost, Route("/materials/insert")]
		public OperationResult Insert([FromBody]Material data)
		{
			try
			{
                if (!userData.Admin_Materials_CanAdd)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Material Created!"], repo.Insert(data));
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/materials/update")]
		public OperationResult Update([FromBody]Material data)
		{
			try
			{
                if (!userData.Admin_Materials_CanEdit)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Material saved!"], repo.Update(data));
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/materials/delete/{id}")]
		public OperationResult Delete(int id)
		{
			try
			{
                if (!userData.Admin_Materials_CanDelete)
                    return OperationResult.Forbid;
                repo.Delete(id);
				return new OperationResult(true, g["Material Deleted!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/materials/rename/{id}/{name}")]
		public OperationResult Rename(int id, string name)
		{
			try
			{
                if (!userData.Admin_Materials_CanRename)
                    return OperationResult.Forbid;
                repo.Rename(id, name);
				return new OperationResult(true, g["Material Renamed!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Unexpected error while renaming Material."]);
			}
		}

		[HttpGet, Route("/materials/getbyid/{id}")]
		public IMaterial GetByID(int id)
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

		[HttpGet, Route("/materials/getlist")]
		public List<IMaterial> GetList()
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
	}
}