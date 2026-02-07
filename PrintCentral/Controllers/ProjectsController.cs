using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public class ProjectsController : Controller
    {
        private IProjectRepository repo;
        private ILocalizationService g;
        private ILogService log;
        private IUserData userData;
        private IAppInfo appInfo;
        private ITempFileService temp;

        public ProjectsController(
            IProjectRepository repo,
            ILocalizationService g,
            ILogService log,
            IUserData userData,
            IAppInfo appInfo,
            ITempFileService temp
            )
        {
            this.repo = repo;
            this.g = g;
            this.log = log;
            this.userData = userData;
            this.appInfo = appInfo;
            this.temp = temp;
        }

        [HttpPost, Route("/projects/insert")]
        public OperationResult Insert([FromBody]Project data)
        {
            try
            {
                if (!userData.Admin_Projects_CanAdd)
                    return OperationResult.Forbid;
                var project = repo.Insert(data);
                return new OperationResult(true, g["Project Created!"], project);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                if (ex.IsNameIndexException())
                    return new OperationResult(false, g["There is already an item with that name."]);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/projects/update")]
        public OperationResult Update([FromBody]Project data)
        {
            try
            {
                if (!userData.Admin_Projects_CanEdit)
                    return OperationResult.Forbid;

                // need to update variable data catalog with compo field reference
                var project = repo.Update(data);

                return new OperationResult(true, g["Project saved!"], repo.Update(data));
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                if (ex.IsNameIndexException())
                    return new OperationResult(false, g["There is already an item with that name."]);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/projects/delete/{id}")]
        public OperationResult Delete(int id)
        {
            try
            {
                if (!userData.Admin_Projects_CanDelete)
                    return OperationResult.Forbid;
                repo.Delete(id);
                return new OperationResult(true, g["Project Deleted!"]);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/projects/rename/{id}/{name}")]
        public OperationResult Rename(int id, string name)
        {
            try
            {
                if (!userData.Admin_Projects_CanRename)
                    return OperationResult.Forbid;
                repo.Rename(id, name);
                return new OperationResult(true, g["Project Renamed!"]);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                if (ex.IsNameIndexException())
                    return new OperationResult(false, g["There is already an item with that name."]);
                return new OperationResult(false, g["Unexpected error while renaming project."]);
            }
        }

        [HttpGet, Route("/projects/getbyid/{id}")]
        public IProject GetByID(int id)
        {
            try
            {
                var project = repo.GetByID(id);
                if (project != null)
                {
                    project.FTPClients = repo.DecryptString(project.FTPClients);
                }

                return project;
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpGet, Route("/projects/getlist")]
        public List<IProject> GetList()
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

        [HttpGet, Route("/projects/getbybrand/{brandid}/{showAll}")]
        public List<IProject> GetByBrandID(int brandid, bool showAll)
        {
            try
            {
                return repo.GetByBrandID(brandid, showAll);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }


        [HttpGet, Route("/projects/getbybrandme/{brandid}/{showAll}")]
        public List<IProject> GetByBrandIDMe(int brandid, bool showAll)
        {
            try
            {
                return repo.GetByBrandIDME(brandid, showAll);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }
        [HttpPost, Route("/projects/hide/{id}")]
        public OperationResult Hide(int id)
        {
            try
            {
                if (!userData.Admin_Projects_CanEdit)
                    return OperationResult.Forbid;
                repo.Hide(id);
                return new OperationResult(true, g["Project Hidden!"]);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/projects/fields/{id}")]
        public OperationResult GetFields(int id)
        {
            try
            {
                if (!userData.Admin_Projects_CanSee)
                    return OperationResult.Forbid;
                List<DBFieldInfo> result = repo.GetDBFields(id);
                return new OperationResult(true, null, result);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/projects/assignrfidconfig/{projectid}/{configid}")]
        public OperationResult AssignRFIDConfig(int projectid, int configid)
        {
            try
            {
                if (!userData.Admin_Brands_CanEditRFIDSettings)
                    return OperationResult.Forbid;
                repo.AssignRFIDConfig(projectid, configid);
                return new OperationResult(true, g["RFID configuration updated!"]);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["RFID configuration could not be updated."]);
            }
        }

        [HttpPost, Route("/projects/assignorderworkflowconfig/{projectid}/{configid}")]
        public OperationResult AssignOrderWorkflowConfig(int projectid, int configid)
        {
            try
            {
                if(!userData.Admin_Brands_CanEditRFIDSettings)
                    return OperationResult.Forbid;
                repo.AssignOrderWorkflowConfig(projectid, configid);
                return new OperationResult(true, g["Order Workflow configuration updated!"]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Order Workflow configuration could not be updated."]);
            }
        }

        [HttpGet, Route("/projects/export/{id}")]
		public IActionResult Export(int id)
        {
            try
            {
				var project = GetByID(id);
				var tempFile = temp.GetTempFileName(true, ".zip");
                repo.ExportProject(id, tempFile);
				var stream = System.IO.File.OpenRead(tempFile);
                return File(stream, MimeTypes.GetMimeType(Path.GetExtension(tempFile)), $"{project.Name}.zip");
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return NotFound();
            }
        }

        [Route("/projects/import/{brandId}/{projectId}")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue, ValueLengthLimit = 500000, MultipartHeadersLengthLimit = 500000, KeyLengthLimit = 6)]
        public OperationResult ProjectImport(int brandId, int projectId)
        {
            var filePath = string.Empty;

            try
            {
                if (Request.Form.Files != null && Request.Form.Files.Count == 1)
                {
                    var file = Request.Form.Files[0];

                    if (".zip".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0)
                    {
                        return new OperationResult(false, g["Can only accept .zip files."]);
                    }


                    //create temp file
                    filePath = temp.GetTempFileName(file.FileName);
                    using (var fileStream = new FileStream(filePath, FileMode.OpenOrCreate))
                    {
						fileStream.SetLength(0L);
                        file.CopyTo(fileStream);
                    }

					if(projectId == 0)
					{
						if (brandId == 0)
							throw new InvalidOperationException("BrandID cannot be 0 when ProjectID is 0 as well.");

						Project p = new Project() { Name = "Imported Project", BrandID = brandId };
						var inserted = Insert(p).Data as Project;
						projectId = inserted.ID;
					}

					repo.ImportProject(projectId, filePath);
					return new OperationResult(true, g["Project Successfully Imported."]);
                }

                return new OperationResult(false, g["No file."]);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Unexpected error while uploading current file."]);
            }
            finally
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);

                }
            }
        }
    }
}