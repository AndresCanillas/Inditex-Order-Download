using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Service.Contracts.PrintCentral;
using Services.Core;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Controllers
{
	[Authorize]
	public class CategoryController : Controller
	{
		private ICategoryRepository repo;
        private IUserData userData;
        private ILocalizationService g;
		private ILogService log;

        public CategoryController(
			ICategoryRepository repo,
			IUserData userData,
            ILocalizationService g,
			ILogService log)
		{
			this.repo = repo;
            this.userData = userData;
            this.g = g;
			this.log = log;
		}

		[HttpPost, Route("/categories/insert")]
		public OperationResult Insert([FromBody]Category data)
		{
			try
			{
				if (!userData.Admin_Categories_CanAdd)
					return OperationResult.Forbid;
				return new OperationResult(true, g["Category Created!"], repo.Insert(data));
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/categories/update")]
		public OperationResult Update([FromBody]Category data)
		{
			try
			{
				if (!userData.Admin_Categories_CanEdit)
					return OperationResult.Forbid;
				return new OperationResult(true, g["Category Saved!"], repo.Update(data));
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/categories/delete/{id}")]
		public OperationResult Delete(int id)
		{
			try
			{
				if (!userData.Admin_Categories_CanDelete)
					return OperationResult.Forbid;
				repo.Delete(id);
				return new OperationResult(true, g["Category Deleted!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpGet, Route("/categories/getbyid/{id}")]
		public ICategory GetByID(int id)
		{
			try
			{
				return repo.GetByID(id);
			}
			catch(Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[HttpGet, Route("/categories/getlist")]
		public List<ICategory> GetList()
		{
			try
			{
				return repo.GetList();
			}
			catch(Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[HttpGet, Route("/categories/getbyproject/{id}")]
		public List<ICategory> GetByProject(int id)
		{
			try
			{
				return repo.GetByProject(id);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}
    }
}