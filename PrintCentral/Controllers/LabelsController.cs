using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Controllers
{
	[Authorize]
	public class LabelsController : Controller
    {
		private ILabelRepository repo;
		private IUserData userData;
		private IAppConfig config;
        private ILocalizationService g;
		private ILogService log;

		public LabelsController(
			ILabelRepository repo,
			IUserData userData,
			IAppConfig config,
			ILocalizationService g,
			ILogService log)
		{
			this.repo = repo;
            this.userData = userData;
			this.config = config;
            this.g = g;
			this.log = log;
		}

		[HttpPost, Route("/labels/insert")]
		public OperationResult Insert([FromBody]LabelData data)
		{
			try
			{
                if (!userData.Admin_Labels_CanAdd)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Label Created!"], repo.Insert(data));
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/labels/update")]
		public OperationResult Update([FromBody]LabelData data)
		{
			try
			{
                if (!userData.Admin_Labels_CanEdit)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Label saved!"], repo.Update(data));
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/labels/delete/{id}")]
		public OperationResult Delete(int id)
		{
			try
			{
                if (!userData.Admin_Labels_CanDelete)
                    return OperationResult.Forbid;
                repo.Delete(id);
				return new OperationResult(true, g["Label Deleted!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/labels/rename/{id}/{name}")]
		public OperationResult Rename(int id, string name)
		{
			try
			{
                if (!userData.Admin_Labels_CanRename)
                    return OperationResult.Forbid;
                repo.Rename(id, name);
				return new OperationResult(true, g["Label Renamed!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Unexpected error while renaming Label."]);
			}
		}

		[HttpGet, Route("/labels/getbyid/{id}")]
		public ILabelData GetByID(int id)
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

		[HttpGet, Route("/labels/getlist")]
		public List<ILabelData> GetList()
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

		[HttpGet, Route("/labels/getbyprojectid/{projectid}")]
		public List<ILabelData> GetByProjectID(int projectid)
		{
			try
			{
				return repo.GetByProjectID(projectid);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[Route("/labels/upload/{id}")]
		[RequestSizeLimit(10000000000)]
		public IActionResult UploadFile(int id)
		{
			try
			{
                if (!userData.Admin_Labels_CanEdit)
                    return Forbid();
                if (Request.Form.Files != null && Request.Form.Files.Count == 1)
				{
					var file = Request.Form.Files[0];
					if (".nlbl,.lbl".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0)
						return Content(Newtonsoft.Json.JsonConvert.SerializeObject(new { success = false, message = g["Can only accept .nlbl and .lbl files"] }));

					var niceLabelInfo = new NiceLabelInfo();
					using (var src = file.OpenReadStream())
					{
						niceLabelInfo = repo.UploadFile(id, file.FileName, src);
					}

					var ret = new { success = true, message = "", FileID = id, Data = niceLabelInfo };
					return Content(Newtonsoft.Json.JsonConvert.SerializeObject(ret));

				}
				else return Content($"{{\"success\":false, \"message\":\"{g["Invalid Request. Was expecting a single file."]}\"}}");
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return Content($"{{\"success\":false, \"message\":\"{g["Unexpected error while uploading label file."]}\"}}");
			}
		}

		[HttpGet, Route("/labels/download/{id}")]
		public ActionResult DownloadFile(int id)
		{
			try
			{
                if (!userData.Admin_Labels_CanEdit)
                    return Forbid();
                string fileName;
				var stream = repo.DownloadFile(id, out fileName);
				return File(stream, "application/nlbl", fileName);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return Content("Unexpected error while downloading label file.");
			}
		}

		[HttpGet, Route("/labels/getpreview/{id}")]
		public IActionResult GetLabelPreview(int id)
		{
			try
			{
				if (id != 0)
				{
					var image = repo.GetLabelPreview(id);
					if (image != null)
						return File(image, "image/png");
				}
				return File("~/images/no_preview.png", "image/png", "no_preview.png");
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return File("~/images/no_preview.png", "image/png", "no_preview.png");
			}
		}

        [AllowAnonymous]
		[HttpGet, Route("/labels/getarticlepreview/{labelid}/{orderid}/{variableDataDetailID}")]
		public async Task<IActionResult> GetArticlePreview(int labelid, int orderid, int variableDataDetailID)
		{
			try
			{
				var image = await repo.GetArticlePreviewAsync(labelid, orderid, variableDataDetailID);
				return File(image, "image/png");
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return File("~/images/no_preview.png", "image/png", "no_preview.png");
			}
		}

		[HttpPost, Route("/labels/setpreviewwithvariables/{labelid}")]
		public async Task<OperationResult> SetLabelPreviewWithVariables(int labelid, [FromBody]string previewData)
		{
			try
			{
				if (!userData.Admin_Labels_CanEdit)
					return OperationResult.Forbid;
				await repo.SetLabelPreviewWithVariablesAsync(labelid, previewData);
				return new OperationResult(true, "Preview updated");
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}


		[HttpPost, Route("/labels/setpreview/{labelid}/{orderid}/{variableDataDetailID}")]
		public async Task<OperationResult> SetLabelPreview(int labelid, int orderid, int variableDataDetailID)
		{
			try
			{
                if (!userData.Admin_Labels_CanEdit)
                    return OperationResult.Forbid;
                await repo.SetLabelPreviewAsync(labelid, orderid, variableDataDetailID);
				return new OperationResult(true, "Preview updated");
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/labels/info/{id}")]
		public async Task<OperationResult> GetLabelVariables(int id)
		{
			try
			{
                if (!userData.Admin_Labels_CanSee)
                    return OperationResult.Forbid;
                var info = await repo.GetLabelInfo(id);
				return new OperationResult(true, "", info);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

        [HttpPost, Route("/labels/updategroupingfields/{id}/{data}")]
        public OperationResult UpdateGroupingFields(int id, string data)
        {
            try
            {
                if (!userData.Admin_Projects_CanEdit)
                    return OperationResult.Forbid;
                repo.UpdateGroupingFields(id, data);
                return new OperationResult(true, g["Grouping fields Updated!"]);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/labels/updatecomparerfield/{id}/{data}")]
        public OperationResult UpdateComparerField(int id, string data)
        {
            try
            {
                if (!userData.Admin_Projects_CanEdit)
                    return OperationResult.Forbid;
                repo.UpdateComparerField(id, data);
                return new OperationResult(true, g["Comparer Field Updated!"]);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }
    }
}