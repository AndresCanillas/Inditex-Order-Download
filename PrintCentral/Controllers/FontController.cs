using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using WebLink.Contracts;
using WebLink.Contracts.Models;


namespace PrintCentral.Controllers
{
    public class FontController : Controller
    {

		private IFontRepository repo;
		private IUserData userData;
		private ILocalizationService g;
		private ILogService log;

		public FontController(
			IFontRepository repo,
			IUserData userData,
			ILocalizationService g,
			ILogService log)
		{
			this.repo = repo;
			this.userData = userData;
			this.g = g;
			this.log = log;
		}

		public IActionResult Index()
        {
            return View();
        }


		[HttpGet, Route("/font/getlist")]
		public IEnumerable<string> GetList()
		{
             return repo.GetList();
		}

        [HttpGet, Route("/font/getupdateddate")]
        public Dictionary<string, string> GetUpdatedDate()
        {
            return repo.GetUpdatedDate();
        }


		[Route("/font/uploadfont")]
		public IActionResult UploadFont()
		{
			try
			{
				if (!userData.Admin_Fonts_CanAdd)
					return Forbid();
				if (Request.Form.Files != null && Request.Form.Files.Count == 1)
				{
					var file = Request.Form.Files[0];
					if (".ttf,.woff,.otf,.fnt,.ttc".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0)
						return Content($"{{\"success\":false, \"message\":\"{g["Can only accept .ttf, .woff, .otf and .fnt files"]}\"}}");

					using (Stream fileContent = file.OpenReadStream())
					{
						var metadata = repo.UploadFont(file.FileName, fileContent);
						var json = $"{{\"success\":true, \"message\":\" {file.FileName} successfully added.\", \"Data\":{JsonConvert.SerializeObject(metadata)}}}";
						return Content(json);
					}
				}
				else return Content($"{{\"success\":false, \"message\":\"{g["Invalid Request. Was expecting a single file."]}\"}}");
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return Content($"{{\"success\":false, \"message\":\"{g["Unexpected error while uploading font file."]}\"}}");
			}
		}


		[HttpGet, Route("/font/get/{filename}")]
		public IActionResult GetFont(string filename)
		{
			if (!userData.Admin_Fonts_CanEdit)
				return Forbid();
			try
			{
				var font = repo.GetFont(filename);
				Response.Headers.Add("X-Content-Type-Options", "nosniff");
				Response.Headers[HeaderNames.CacheControl] = "no-cache";
				return File(font, MimeTypes.GetMimeType(Path.GetExtension(filename)), filename);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return NotFound();
			}
		}

        [HttpPost, Route("/font/download")]
        public IActionResult Download([FromBody] FontModel fontModel)
        {
            if (!userData.Admin_Fonts_CanEdit)
                return Forbid();
            try
            {
                var font = repo.GetFont(fontModel.FontName);
                Response.Headers.Add("X-Content-Type-Options", "nosniff");
                Response.Headers[HeaderNames.CacheControl] = "no-cache";
                return File(font, MimeTypes.GetMimeType(Path.GetExtension(fontModel.FontName)), fontModel.FontName);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return NotFound();
            }
        }


        [HttpPost, Route("/font/delete/{filename}")]
		public OperationResult Delete(string filename)
		{
			try
			{
				if (!userData.Admin_Fonts_CanDelete)
					return OperationResult.Forbid;
				repo.DeleteFont(filename);
				return new OperationResult(true, g["Font Deleted!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}
	}

    public class FontModel
    {
        public string FontName;
    }
}