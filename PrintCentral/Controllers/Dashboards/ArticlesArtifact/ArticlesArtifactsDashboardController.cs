using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Contracts.Models.Repositories.Artifact.DTO;

namespace PrintCentral.Controllers.Dashboards.Articles
{
    [Authorize]
	public class ArticlesArtifactsDashboardController : Controller
	{
		private readonly IArtifactRepository artifactRepo;
        private readonly ILabelRepository labelRepo;
        private readonly IArticleRepository articleRepository;
		private readonly ILogService log;
		private readonly ILocalizationService g;
		private readonly IUserData userData;

		public ArticlesArtifactsDashboardController(
			IArtifactRepository artifactRepo,
            ILabelRepository labelRepo,
			ILogService log,
			ILocalizationService g,
			IUserData userData,
            IArticleRepository articleRepository
			)
		{
			this.artifactRepo = artifactRepo;
            this.labelRepo = labelRepo;
			this.log = log;
			this.g = g;
			this.userData = userData;
            this.articleRepository = articleRepository;
		}

		[HttpGet, Route("/Dashboards/ArticlesArtifacts/GetArticlesArtifacts")]
		public PagedOperationResult GetTrackedArticles(int page, int pageSize, string projectid)
		{
			//List<ArticleTrackingInfo> QuantitiesResult = new List<ArticleTrackingInfo>();

            int.TryParse (projectid, out int projectID);     

            var result = artifactRepo.GetDashboardData(page, pageSize, projectID);

			var totalArtifacts = artifactRepo.GetDashboardDataCount(projectID);

			return new PagedOperationResult(true, null, totalArtifacts, result);
		}

        [HttpPost, Route("/Dashboards/ArticlesArtifacts/SaveValidationLabel")]
        public OperationResult SetValidationLabelCheckbox([FromBody] LabelValidationCheckbox labelDTO)
        {
            try
            {
                var labelFull = labelRepo.GetByID(labelDTO.LabelID);
                labelFull.IsValid = labelDTO.IsValid;
                labelRepo.Update(labelFull);

                return new OperationResult(true, g["Label is validated and updated!"], null);

            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."], null);
            }
        }

        [HttpGet, Route("/Dashboards/ArticlesArtifacts/Search")]
        public OperationResult SearchLabel(string value, string table, string projectID, int page, int pageSize)
        {
            int.TryParse(projectID, out int projectId);
            var result = new List<ArticleArtifactDashboardDTO>();
            if(table == "ArticleCode")
                result = artifactRepo.GetSearchArticlesArtifactsData(projectId, page, pageSize,$" AND report.ArticleCode LIKE '%{value}%'").ToList();
            else if(table == "LabelCode")
                result = artifactRepo.GetSearchArticlesArtifactsData(projectId, page, pageSize,$" AND report.LabelName LIKE '%{value}%'").ToList();
            else
            {
                int.TryParse(value, out int valueInteger);
                result = artifactRepo.GetSearchArticlesArtifactsData(projectId, page, pageSize, $" AND report.IsValid = {valueInteger}").ToList();
            }
                

            var countResult = artifactRepo.GetDashboardDataCount(projectId);

            return new PagedOperationResult(true, null, countResult, result);
        }

        public class LabelValidationCheckbox
        {
            public int LabelID;
            public bool IsValid;
        }

        /*[HttpPost, Route("/Dashboards/ArticlesArtifacts/")]
        public OperationResult FreeTextSearchCount([FromBody] TextSearchFilter[] filter)
        {
            try
            {
                return new OperationResult(true, null, repo.GetListCount(catalogid, filter));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."], null);
            }
        }*/


    }
}