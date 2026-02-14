using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WebLink.Contracts;
using WebLink.Contracts.Models;


namespace PrintCentral.Controllers
{
    [Authorize]
    public class ImagesController : Controller
    {

        private IProjectImageRepository repo;
        private IUserData userData;
        private ILocalizationService g;
        private ILogService log;

        public ImagesController(
            IProjectImageRepository repo,
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


        [HttpGet, Route("/images/getbyproject/{projectid}")]
        public IEnumerable<ImageMetadata> GetListByProjectID(int projectid)
        {
            return repo.GetListByProjectID(projectid);
        }

        [HttpGet, Route("/images/getbyproject/{projectid}/filterby/{filename}")]
        public IEnumerable<ImageMetadata> GetListByProjectID(int projectid, string filename)
        {
            return GetListByProjectID(projectid)
                .Where(w => w.FileName.Contains(filename));
        }

        [HttpGet, Route("/images/getimagemetadata/{projectid}/{filename}")]
        public ImageMetadata GetImageMetadata(int projectid, string filename)
        {
            return repo.GetImageMetadata(projectid, filename);
        }


        [HttpPost, Route("/images/updateimagemetadata")]
        public OperationResult UpdateImageMetadata([FromBody] ImageMetadata metadata)
        {
            try
            {
                if(!userData.Admin_Projects_CanEditImages)
                    return OperationResult.Forbid;
                repo.UpdateImageMetadata(metadata);
                return new OperationResult(true, g["Image Updated!"]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [Route("/images/uploadimageopen/{projectid}")]
        public IActionResult UploadImageOpen(int projectid)
        {
            try
            {
                if(Request.Form.Files != null && Request.Form.Files.Count == 1)
                {
                    var file = Request.Form.Files[0];
                    if(".png,.jpg,.jpeg,.gif,.svg,.tif,.tiff,.bmp".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0)
                        return Content($"{{\"success\":false, \"message\":\"{g["Can only accept .png, .jpg, .jpeg, .tif, .tiff, .gif, .bmp and .svg files"]}\"}}");

                    using(Stream fileContent = file.OpenReadStream())
                    {
                        var metadata = repo.UploadImage(projectid, file.FileName, fileContent);
                        var json = $"{{\"success\":true, \"message\":\"\", \"Data\":{JsonConvert.SerializeObject(metadata)}}}";
                        return Content(json);
                    }
                }
                else return Content($"{{\"success\":false, \"message\":\"{g["Invalid Request. Was expecting a single file."]}\"}}");
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return Content($"{{\"success\":false, \"message\":\"{g["Unexpected error while uploading image file."]}\"}}");
            }
        }

        [Route("/images/uploadimage/{projectid}")]
        public IActionResult UploadImage(int projectid)
        {
            try
            {
                // TODO: validate if user has permissions to upload images
                // use company repository to check permissions
                if(Request.Form.Files != null && Request.Form.Files.Count == 1)
                {
                    var file = Request.Form.Files[0];
                    var fileSize = file.Length / 1000000.0;
                    if(fileSize > 5)
                        return Content($"{{\"success\":false, \"message\":\"{g["Max file size is 5MB, your file size is: {0}MB", fileSize]}\"}}");

                    var newFileName = Request.Form["NewFileName"].FirstOrDefault();
                    if(".png,.jpg,.jpeg,.gif,.svg,.tif,.tiff,.bmp".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0)
                        return Content($"{{\"success\":false, \"message\":\"{g["Can only accept .png, .jpg, .jpeg, .tif, .tiff, .gif, .bmp and .svg files"]}\"}}");

                    var finalFileName = !string.IsNullOrEmpty(newFileName) ? $"{Path.GetFileNameWithoutExtension(newFileName)}{Path.GetExtension(file.FileName).ToLower()}" : file.FileName;
                    using(Stream fileContent = file.OpenReadStream())
                    {
                        var metadata = repo.UploadImage(projectid, finalFileName, fileContent);
                        var json = $"{{\"success\":true, \"message\":\"\", \"Data\":{JsonConvert.SerializeObject(metadata)}}}";

                        return Content(json);
                    }
                }
                else return Content($"{{\"success\":false, \"message\":\"{g["Invalid Request. Was expecting a single file."]}\"}}");
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return Content($"{{\"success\":false, \"message\":\"{g["Unexpected error while uploading image file."]}\"}}");
            }
        }

        [HttpGet, Route("/images/getimage/{projectid}/{filename}")]
        public IActionResult GetImage(int projectid, string filename)
        {
            try
            {
                var image = repo.GetImage(projectid, filename);
                Response.Headers.Add("X-Content-Type-Options", "nosniff");
                Response.Headers[HeaderNames.CacheControl] = "no-cache";
                return File(image, MimeTypes.GetMimeType(Path.GetExtension(filename)), filename);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return File("~/images/no_preview.png", "image/png", "no_preview.png");
            }
        }


        [HttpGet, Route("/images/getthumbnail/{projectid}/{filename}")]
        public IActionResult GetThumbnail(int projectid, string filename)
        {

            try
            {
                Stream thumb;
                if(Path.GetExtension(filename).ToLower() == ".svg")
                    thumb = repo.GetImage(projectid, filename);
                else
                    thumb = repo.GetThumbnail(projectid, filename);
                Response.Headers.Add("X-Content-Type-Options", "nosniff");
                Response.Headers[HeaderNames.CacheControl] = "no-cache";
                return File(thumb, MimeTypes.GetMimeType(Path.GetExtension(filename)), filename);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return File("~/images/no_preview.png", "image/png", "no_preview.png");
            }
        }

        [HttpPost, Route("/images/deleteopen/{projectid}/{filename}")]
        public OperationResult DeleteOpen(int projectid, string filename)
        {
            try
            {
                
                repo.DeleteImage(projectid, filename);

                return new OperationResult(true, "");
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/images/exists/{projectid}/{filename}")]
        public OperationResult Exists(int projectid, string filename)
        {
            try
            {
                var image = repo.GetImage(projectid, filename);
                if (image!=null && image.Length!=0)
                    return new OperationResult(true, "Image exist!");
                else
                    return new OperationResult(false, "Image not found!");
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }



        [HttpPost, Route("/images/delete/{projectid}/{filename}")]
        public OperationResult Delete(int projectid, string filename)
        {
            try
            {
                repo.DeleteImage(projectid, filename);
                return new OperationResult(true, g["Image Deleted!"]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }
    }
}