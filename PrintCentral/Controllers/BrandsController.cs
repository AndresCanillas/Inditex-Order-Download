using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Controllers
{
    [Authorize]
    public class BrandsController : Controller
    {
        private readonly IBrandRepository repo;
        private IUserData userData;
        private readonly ILogService log;
        private readonly ILocalizationService g;

        public BrandsController(
            IBrandRepository repo,
            IUserData userData,
            ILogService log, 
            ILocalizationService g)
        {
            this.repo = repo;
            this.userData = userData;
            this.log = log;
            this.g = g;
        }

        [HttpGet, Route("/brands/getbyid/{brandid}")]
        public IBrand GetByID(int brandid)
        {
            try
            {
                return repo.GetByID(brandid);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpGet, Route("/brands/getbycompanyid/{companyid}")]
        public List<IBrand> GetByCompanyID(int companyid)
        {
            try
            {
                return repo.GetByCompanyID(companyid);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpGet, Route("/brands/getbycompanyidme/{companyid}")]
        public List<IBrand> GetByCompanyIDME(int companyid)
        {
            try
            {
                return repo.GetByCompanyIDME(companyid);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }


        [HttpPost, Route("/brands/insert")]
        public OperationResult Insert([FromBody]Brand data)
        {
            try
            {
                if (!userData.Admin_Brands_CanAdd)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Brand Created!"], repo.Insert(data));
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                if (ex.IsNameIndexException())
                    return new OperationResult(false, g["There is already an item with that name."]);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/brands/update")]
        public OperationResult Update([FromBody]Brand data)
        {
            try
            {
                if (!userData.Admin_Brands_CanEdit)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Brand saved!"], repo.Update(data));
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                if (ex.IsNameIndexException())
                    return new OperationResult(false, g["There is already an item with that name."]);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/brands/delete/{id}")]
        public OperationResult Delete(int id)
        {
            try
            {
                if (!userData.Admin_Brands_CanDelete)
                    return OperationResult.Forbid;
                repo.Delete(id);
                return new OperationResult(true, g["Brand Deleted!"]);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

		[HttpPost, Route("/brands/rename/{id}/{name}")]
		public OperationResult Rename(int id, string name)
		{
			try
			{
                if (!userData.Admin_Brands_CanRename)
                    return OperationResult.Forbid;
                repo.Rename(id, name);
				return new OperationResult(true, g["Brand Renamed!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Unexpected error while renaming brand."]);
			}
		}

		[Route("/brands/uploadicon/{brandid}")]
        public IActionResult UpdateIcon(int brandid)
        {
			try
			{
                if (!userData.Admin_Brands_CanEditLogo)
                    return Forbid();
                if (Request.Form.Files != null && Request.Form.Files.Count == 1)
				{
					var file = Request.Form.Files[0];
					if (".png,.jpg,.jpeg,.gif".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0)
						return Content($"{{\"success\":false, \"message\":\"{g["Can only accept .png, .jpg, .jpeg and .gif files"]}\"}}");
					using (MemoryStream ms = new MemoryStream())
					{
						using (Stream src = file.OpenReadStream())
						{
							src.CopyTo(ms, 4096);
						}
						repo.UpdateIcon(brandid, ms.ToArray());
						return Content($"{{\"success\":true, \"message\":\"\", \"FileID\":{brandid}}}");
					}
				}
				else return Content($"{{\"success\":false, \"message\":\"{g["Invalid Request. Was expecting a single file."]}\"}}");
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return Content($"{{\"success\":false, \"message\":\"{g["Unexpected error while uploading image file."]}\"}}");
			}
        }

        [Route("/brands/geticon/{brandid}")]
        public IActionResult GetIcon(int brandid)
        {
            try
            {
                Response.Headers[HeaderNames.CacheControl] = "no-cache";
                var icon = repo.GetIcon(brandid);
                if (icon != null)
                    return File(icon, "image/png");
                else
                    return File("/images/no_logo.png", "image/png");
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return File("/images/no_logo.png", "image/png");
            }
        }

		[HttpPost, Route("/brands/assignrfidconfig/{brandid}/{configid}")]
		public OperationResult AssignRFIDConfig(int brandid, int configid)
		{
			try
			{
				if (!userData.Admin_Brands_CanEditRFIDSettings)
					return OperationResult.Forbid;
				repo.AssignRFIDConfig(brandid, configid);
				return new OperationResult(true, g["RFID configuration updated!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["RFID configuration could not be updated."]);
			}
		}
	}
}