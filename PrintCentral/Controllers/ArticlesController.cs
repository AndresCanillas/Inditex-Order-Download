using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Controllers
{
    [Authorize]
	public class ArticlesController : Controller
	{
		private IArticleRepository repo;
		private ILabelRepository labelRepo;
		private IArtifactRepository artifactRepo;
        private IUserData userData;
        private ILocalizationService g;
		private ILogService log;
        


        public ArticlesController(
            IArticleRepository repo,
            ILabelRepository labelRepo,
            IArtifactRepository artifactRepo,
            IUserData userData,
            ILocalizationService g,
            ILogService log
        )
        {
            this.repo = repo;
            this.labelRepo = labelRepo;
            this.artifactRepo = artifactRepo;
            this.userData = userData;
            this.g = g;
            this.log = log;
            
        }

        [HttpPost, Route("/articles/insert")]
		public OperationResult Insert([FromBody]Article data)
		{
			try
			{
                if (!userData.Admin_Articles_CanAdd)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Article Created!"], repo.Insert(data));
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/articles/update")]
		public OperationResult Update([FromBody]Article data)
		{
			try
			{
                if (!userData.Admin_Articles_CanEdit)
                    return OperationResult.Forbid;
                var article = repo.Update(data);
                var compositionConfig = repo.GetArticleCompositionConfig(article.ProjectID.Value, article.ID);
                if(compositionConfig != null)
                {
                    compositionConfig.ArticleCode = article.ArticleCode;
                    repo.SaveArticleComposition(compositionConfig, userData.UserName);
                }
                return new OperationResult(true, g["Article saved!"], article);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/articles/delete/{id}")]
		public OperationResult Delete(int id)
		{
			try
			{
                if (!userData.Admin_Articles_CanDelete)
                    return OperationResult.Forbid;
                repo.Delete(id);
				return new OperationResult(true, g["Article Deleted!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/articles/rename/{id}/{name}")]
		public OperationResult Rename(int id, string name)
		{
			try
			{
                if (!userData.Admin_Articles_CanRename)
                    return OperationResult.Forbid;
                repo.Rename(id, name);
				return new OperationResult(true, g["Article Renamed!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Unexpected error while renaming Article."]);
			}
		}

		[HttpGet, Route("/articles/getbyid/{id}")]
		public IArticle GetByID(int id)
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

		[HttpGet, Route("/articles/getbycode/{code}")]
		public IArticle GetByID(string code)
		{
			try
			{
				return repo.GetByCode(code);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[HttpGet, Route("/articles/getfull/{id}")]
		public ArticleViewModel GetFullArticle(int id)
		{
			try
			{
				return repo.GetFullArticle(id);
			}
			catch(Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[HttpGet, Route("/articles/getlist")]
		public List<IArticle> GetList()
		{
			try
			{
				return repo.GetList().ToList();
			}
			catch(Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[HttpGet, Route("/articles/getbypackid/{packid}")]
		public List<PackArticleViewModel> GetByPackID(int packid)
		{
			try
			{
				return repo.GetByPackID(packid);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

        [HttpGet, Route("/articles/getbyprojectid/{projectid}")]
        public List<IArticle> GetByProjectID(int projectid)
        {
            try
            {
                return repo.GetByProjectID(projectid);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }


        [HttpGet, Route("/articles/getfullbyprojectid/{projectid}")]
		public List<ArticleViewModel> GetFullByProjectID(int projectid)
		{
			try
			{
				return repo.GetFullByProjectID(new ArticleByProjectFilter() { ProjectID = projectid});
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[HttpPost, Route("/articles/getfullbyprojectidfiltered")]
		public List<ArticleViewModel> GetFullByProjectIDFiltered([FromBody]ArticleByProjectFilter filter)
		{
			try
			{
				return repo.GetFullByProjectID(filter);
            }
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}


		[Route("/articles/uploadpreview/{id}")]
		public IActionResult UploadPreview(int id)
		{
			try
			{
                if (!userData.Admin_Articles_CanEdit)
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
						repo.SetArticlePreview(id, ms.ToArray());
						return Content($"{{\"success\":true, \"message\":\"\", \"FileID\":{id}}}");
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


		[Route("/articles/getpreview/{id}")]
		public IActionResult GetArticlePreview(int id)
		{
			try
			{
				var image = repo.GetArticlePreview(id);
				if (image != null)
					return File(image, "image/png", $"preview_{id}.png");
				else
					return File("/images/no_preview.png", "image/png", "no_preview.png");
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return File("/images/no_preview.png", "image/png", "no_preview.png");
			}
		}

        [Route("/articles/getfixedpreview/{id}")]
        public IActionResult GetFixedArticlePreview(int id)
        {
            try
            {
                var image = repo.GetFixedArticlePreview(id);
                if (image != null)
                    return File(image, "image/png", $"preview_{id}.png");
                else
                    return File("/images/no_preview.png", "image/png", "no_preview.png");
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return File("/images/no_preview.png", "image/png", "no_preview.png");
            }
        }

        [HttpPost, Route("/articles/getarticleswithlabels/{projectid}")]
        public List<ArticleWithLabelDTO> GetArticlesWithLabels([FromBody] List<ArticleWithLabelDTO> articles,int projectid)
        {
            try
            {
                return repo.GetArticlesWithLabels(articles, projectid);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        #region Artifacts

        [HttpPost, Route("/articles/addartifact")]
		public OperationResult AddArtifact([FromBody]Artifact data)
		{
			try
			{
				if (!userData.Admin_Artifact_CanAdd)
					return OperationResult.Forbid;

				var artifact = artifactRepo.AddArtifactToArticle(data.ArticleID.Value, data.LabelID.Value);

				return new OperationResult(true, g["Artifact added!"], artifact);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				
				return new OperationResult(false, g["Unexpected error while trying to add Artifact."]);
			}
		}

        [HttpPost, Route("/article/updateartifact")]
        public OperationResult UpdateArtifact([FromBody]Artifact data)
        {
            try
            {
                if (!userData.Admin_Artifact_CanAdd)
                    return OperationResult.Forbid;

                var artifact = artifactRepo.Update(data);

                return new OperationResult(true, g["Artifact updated!"], artifact);
            }
            catch (Exception ex)
            {
                log.LogException(ex);

                return new OperationResult(false, g["Unexpected error while trying to add Artifact."]);
            }
        }



        [HttpPost, Route("/articles/removeartifact")]
		public OperationResult RemoveArtifact([FromBody]Artifact data)
		{
			try
			{
				if (!userData.Admin_Artifact_CanDelete)
					return OperationResult.Forbid;
				artifactRepo.Delete(data.ID);
				return new OperationResult(true, g["Artifact was removed!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				
				return new OperationResult(false, g["Unexpected error while trying to add Artifact."]);
			}
		}

		[HttpGet, Route("/articles/getartifacts/{articleID}")]
		public OperationResult GetArtifacts(int articleID)
		{
			try
			{
				return new OperationResult(true, g["Success"], artifactRepo.GetByArticle(articleID));
			}
			catch (Exception ex)
			{
				log.LogException(ex);

				return new OperationResult(false, g["Unexpected error while trying to add Artifact."]);
			}
		}

        [HttpPost, Route("/articles/getartifactsbyarticles")]
        public OperationResult GetArtifactsByArticles([FromBody]List<int> articleIds)
        {
            try
            {
                return new OperationResult(true, g["Success"], artifactRepo.GetByArticles(articleIds));
            }
            catch (Exception ex)
            {
                log.LogException(ex);

                return new OperationResult(false, g["Unexpected error while trying to add Artifact."]);
            }
        }

        #endregion Artifacts

        #region Composition  
        [HttpGet, Route("/articles/compostionConfig/{projectid}/{articleid}")] 
        public ArticleCompositionConfig GetArticleCompositionConfig (int projectid, int articleid)
        {
            try
            {
                return repo.GetArticleCompositionConfig(projectid, articleid);   
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return null;     
            }

        }
        [HttpPost, Route("/articles/savecompositionconfig")]
        public OperationResult SetArticleCompositionConfig([FromBody] ArticleCompositionConfig articleCompositionConfig)
        {
            try
            {
                repo.SaveArticleComposition(articleCompositionConfig, userData.UserName);     
                return new OperationResult(true, "Data saved!"); 
            }
            catch(Exception ex)
            {
                log.LogException(ex); 
                return new OperationResult(false, "An error has occurred");
            }
        }

        [HttpPost, Route("/articles/saveaccessblockconfig")]
        public OperationResult SaveArticleAccessBlockConfig([FromBody] ArticleAccessBlockConfig config)
        {
            try
            {
                if(config == null || config.ArticleID <= 0)
                    return new OperationResult(false, "Invalid data");

                repo.SaveArticleAccessBlockConfig(config, userData.UserName);

                return new OperationResult(true, "Data saved!");
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, "An error has occurred");
            }
        }




        #endregion
    }
}