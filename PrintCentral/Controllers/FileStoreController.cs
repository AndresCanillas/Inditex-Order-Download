using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WebLink.Contracts;


namespace PrintCentral.Controllers
{
	[Authorize]
	public class FileStoreController : Controller
    {

		private IFileStoreManager storeManager;
		private IUserData userData;
		private ILocalizationService g;
		private ILogService log;

		public FileStoreController(
			IFileStoreManager storeManager,
			IUserData userData,
			ILocalizationService g,
			ILogService log)
		{
			this.storeManager = storeManager;
			this.userData = userData;
			this.g = g;
			this.log = log;
		}


		[HttpGet, Route("/fsm")]
		public async Task<OperationResult> GetStores()
		{
			try
			{
				var stores = await storeManager.GetAllStoresAsync();
				var result = new List<string>();
				foreach (var store in stores)
					result.Add(store.StoreName);

				return new OperationResult(true, null, result);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return OperationResult.InternalError;
			}
		}


		[HttpGet, Route("/fsm/{storeName}/categories")]
		public OperationResult GetCategories(string storeName)
		{
			try
			{
				var store = storeManager.OpenStore(storeName);
				return new OperationResult(true, null, store.Categories);
			}
			catch (Exception ex)
			{
				return new OperationResult(false, ex.Message);
			}
		}


		[HttpGet, Route("/fsm/{storeName}/files/{fileid}")]
		public async Task<OperationResult> GetFile(string storeName, int fileid)
		{
			try
			{
				var store = storeManager.OpenStore(storeName);
				var file = await store.TryGetFileAsync(fileid);
				if (file == null)
					return OperationResult.NotFound;

				return new OperationResult(true, null, new FSFileInfo()
				{
					StoreID = file.StoreID,
					FileID = file.FileID,
					FileName = file.FileName,
					FileSize = (int)file.FileSize,
					CreateDate = file.CreatedDate,
					UpdateDate = file.UpdatedDate
				});
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return OperationResult.InternalError;
			}
		}


		[HttpGet, Route("/fsm/{storeName}/files/{fileid}/content")]
		public async Task<IActionResult> GetFileContent(string storeName, int fileid)
		{
			try
			{
				var store = storeManager.OpenStore(storeName);
				var file = await store.TryGetFileAsync(fileid);
				if (file == null)
					return NotFound();

				Response.Headers.Add("X-Content-Type-Options", "nosniff");
				Response.Headers[HeaderNames.CacheControl] = "no-cache";
				return File(await file.GetContentAsStreamAsync(), MimeTypes.GetMimeType(Path.GetExtension(file.FileName)), file.FileName);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return BadRequest();
			}
		}


		[HttpPost, Route("/fsm/{storeName}/files/{fileid}/content")]
		public async Task<IActionResult> SetFileContent(string storeName, int fileid)
		{
			try
			{
				if (!userData.IsIDT)
					return Forbid();

				if (Request.Form.Files != null && Request.Form.Files.Count == 1)
				{
					var file = Request.Form.Files[0];
					using (Stream fileContent = file.OpenReadStream())
					{
						var store = storeManager.OpenStore(storeName);
						var storeFile = await store.TryGetFileAsync(fileid);
						if (storeFile == null)
							return NotFound();

						await storeFile.SetContentAsync(fileContent);
						var json = $"{{\"success\":true, \"message\":\"\", \"Data\":{JsonConvert.SerializeObject(storeFile)}}}";
						return Content(json);
					}
				}
				else return Content($"{{\"success\":false, \"message\":\"{g["Invalid Request. Was expecting a single file."]}\"}}");
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return Content($"{{\"success\":false, \"message\":\"{g["Unexpected error while uploading file."]}\"}}");
			}
		}


		[HttpPost, Route("/fsm/{storeName}/files/{fileid}/create/{filename}")]
		public async Task<OperationResult> CreateFile(string storeName, int fileid, string filename)
		{
			try
			{
				var store = storeManager.OpenStore(storeName);
				var file = await store.GetOrCreateFileAsync(fileid, filename);
				return new OperationResult(true, null, new FSFileInfo()
				{
					StoreID = file.StoreID,
					FileID = file.FileID,
					FileName = file.FileName,
					FileSize = (int)file.FileSize,
					CreateDate = file.CreatedDate,
					UpdateDate = file.UpdatedDate
				});
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, ex.Message);
			}
		}


		[HttpGet, Route("/fsm/{storeName}/files/{fileid}/{category}")]
		public async Task<OperationResult> GetAttachmentList(string storeName, int fileid, string category)
		{
			try
			{
				var store = storeManager.OpenStore(storeName);
				var file = await store.TryGetFileAsync(fileid);
				if (file == null)
					return OperationResult.NotFound;

				var result = new List<FSAttachmentInfo>();
				var attachmentCategory = await file.GetAttachmentCategoryAsync(category);
				foreach (var attachment in attachmentCategory)
				{
					var remoteAttachment = attachment as IRemoteAttachment;
					result.Add(new FSAttachmentInfo()
					{
						StoreID = store.StoreID,
						FileID = file.FileID,
						CategoryID = remoteAttachment.CategoryID,
						AttachmentID = remoteAttachment.AttachmentID,
						FileName = remoteAttachment.FileName,
						FileSize = (int)remoteAttachment.FileSize,
						CreateDate = remoteAttachment.CreatedDate,
						UpdateDate = remoteAttachment.UpdatedDate
					});
				}
				return new OperationResult(true, null, result);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, ex.Message);
			}
		}


		[HttpPost, Route("/fsm/{storeName}/files/{fileid}/{category}/create/{filename}")]
		public async Task<OperationResult> CreateAttachment(string storeName, int fileid, string category, string filename)
		{
			try
			{
				var store = storeManager.OpenStore(storeName);
				var file = await store.TryGetFileAsync(fileid);
				if (file == null)
					return OperationResult.NotFound;

				var fileCategory = await file.GetAttachmentCategoryAsync(category);
				var remoteAttachment = await fileCategory.GetOrCreateAttachmentAsync(filename);

				return new OperationResult(true, null, new FSAttachmentInfo()
				{
					StoreID = store.StoreID,
					FileID = file.FileID,
					CategoryID = remoteAttachment.CategoryID,
					AttachmentID = remoteAttachment.AttachmentID,
					FileName = remoteAttachment.FileName,
					FileSize = (int)remoteAttachment.FileSize,
					CreateDate = remoteAttachment.CreatedDate,
					UpdateDate = remoteAttachment.UpdatedDate
				});
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, ex.Message);
			}
		}


		[HttpGet, Route("/fsm/{storeName}/files/{fileid}/{category}/{attachmentid}")]
		public async Task<IActionResult> GetAttachmentContent(string storeName, int fileid, string category, int attachmentid)
		{
			try
			{
				var store = storeManager.OpenStore(storeName);
				var file = await store.TryGetFileAsync(fileid);
				if (file == null)
					return NotFound();

				var cat = await file.GetAttachmentCategoryAsync(category);
				var attachment = await cat.TryGetAttachmentAsync(attachmentid);
				if(attachment == null)
					return NotFound();

				Response.Headers.Add("X-Content-Type-Options", "nosniff");
				Response.Headers[HeaderNames.CacheControl] = "no-cache";
				return File(await attachment.GetContentAsStreamAsync(), MimeTypes.GetMimeType(Path.GetExtension(attachment.FileName)), attachment.FileName);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return BadRequest();
			}
		}

        [HttpGet, Route("/fsm/getimagepreview/{storeName}/files/{fileid}/{category}/{attachmentid}")]
        public async Task<IActionResult> GetImagePreview(string storeName, int fileid, string category, int attachmentid)
        {
            try
            {
                var store = storeManager.OpenStore(storeName);
                var file = await store.TryGetFileAsync(fileid);
                if (file == null)
                    return NotFound();

                var cat = await file.GetAttachmentCategoryAsync(category);
                var attachment = await cat.TryGetAttachmentAsync(attachmentid);
                if (attachment == null)
                    return NotFound();
                return File(await attachment.GetContentAsStreamAsync(), "image/png");
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return File("~/images/no_preview.png", "image/png", "no_preview.png");
            }
        }


        [HttpPost, Route("/fsm/{storeName}/files/{fileid}/{category}/{attachmentid}")]
		public async Task<IActionResult> SetAttachmentContent(string storeName, int fileid, string category, int attachmentid)
		{
			try
			{
				if (!userData.CanSeeVMenu_UploadOrder)
					return Forbid();

				if (Request.Form.Files != null && Request.Form.Files.Count == 1)
				{
					var file = Request.Form.Files[0];
					using (Stream fileContent = file.OpenReadStream())
					{
						var store = storeManager.OpenStore(storeName);
						var storeFile = await store.TryGetFileAsync(fileid);
						if (storeFile == null)
							return NotFound();

						var cat = await storeFile.GetAttachmentCategoryAsync(category);
						var attachment = await cat.TryGetAttachmentAsync(attachmentid);
						if (attachment == null)
							return NotFound();

						await attachment.SetContentAsync(fileContent);
						var json = $"{{\"success\":true, \"message\":\"\", \"Data\":{JsonConvert.SerializeObject(attachment)}}}";
						return Content(json);
					}
				}
				else return Content($"{{\"success\":false, \"message\":\"{g["Invalid Request. Was expecting a single file."]}\"}}");
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return Content($"{{\"success\":false, \"message\":\"{g["Unexpected error while uploading file."]}\"}}");
			}
		}


		[HttpPost, Route("/fsm/{storeName}/files/{fileid}/{category}/{attachmentid}/delete")]
		public async Task<OperationResult> DeleteAttachment(string storeName, int fileid, string category, int attachmentid)
		{
			try
			{
				if (!userData.CanSeeVMenu_UploadOrder)
					return OperationResult.Forbid;

				var store = storeManager.OpenStore(storeName);
				var file = await store.TryGetFileAsync(fileid);
				if (file == null)
					return OperationResult.NotFound;

				var cat = await file.GetAttachmentCategoryAsync(category);
				var attachment = await cat.TryGetAttachmentAsync(attachmentid);
				if (attachment == null)
					return OperationResult.NotFound;

				await attachment.DeleteAsync();
				return new OperationResult(true, "Atachment was deleted", null);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, ex.Message);
			}
		}
	}
}