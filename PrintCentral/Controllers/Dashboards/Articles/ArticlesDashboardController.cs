using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Service.Contracts.WF;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Controllers;

namespace PrintCentral.Controllers.Dashboards.Articles
{
	[Authorize]
	public class ArticlesDashboardController : Controller
	{
		private readonly IArticleTrackingRepository articleTrackingRepo;
		private readonly ILogService log;
		private readonly ILocalizationService g;
		private readonly IUserData userData;

		public ArticlesDashboardController(
			IArticleTrackingRepository articleTrackingRepo,
			ILogService log,
			ILocalizationService g,
			IUserData userData
			)
		{
			this.articleTrackingRepo = articleTrackingRepo;
			this.log = log;
			this.g = g;
			this.userData = userData;
		}

		[HttpGet, Route("/Dashboards/Articles/GetTrackedArticles")]
		public PagedOperationResult GetTrackedArticles(int page, int pageSize, string search, int factoryID)
		{
			List<ArticleTrackingInfo> QuantitiesResult = new List<ArticleTrackingInfo>();

			QuantitiesResult = articleTrackingRepo.GetTrackedArticles(page, pageSize, search, factoryID);

			var totalArticles = articleTrackingRepo.GetTrackedArticlesCount(search);

			return new PagedOperationResult(true, null, totalArticles, QuantitiesResult);
		}

		[HttpGet, Route("/Dashboards/Articles/GetUntrackedArticles")]
		public PagedOperationResult GetUntrackedArticles(int page, int pageSize, string search)
		{
			List<ArticleTrackingInfo> QuantitiesResult = new List<ArticleTrackingInfo>();

			QuantitiesResult = articleTrackingRepo.GetUntrackedArticles(page, pageSize, search);

			var totalArticles = articleTrackingRepo.GetUntrackedArticlesCount(search);

			return new PagedOperationResult(true, null, totalArticles, QuantitiesResult);
		}

		[HttpPost, Route("/Dashboards/Articles/AddArticles")]
		public OperationResult AddArticles([FromBody] ArticleSelectionInfo operationInfo)
		{
			try
			{
				foreach (var article in operationInfo.Articles)
				{
					articleTrackingRepo.AddArticle(article, DateTime.Now, userData.UserName);
				}
				return new OperationResult(true, g["Article added to tracking!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/Dashboards/Articles/RemoveArticles")]
		public OperationResult RemoveArticles([FromBody] ArticleSelectionInfo operationInfo)
		{
			try
			{
				foreach (var article in operationInfo.Articles)
				{
					articleTrackingRepo.RemoveArticle(article);
				}
				return new OperationResult(true, g["Article removed from tracking!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/Dashboards/Articles/ResetDate")]
		public OperationResult ResetDate([FromBody] ArticleSelectionInfo operationInfo)
		{
			try
			{
				foreach (var article in operationInfo.Articles)
				{
					articleTrackingRepo.ResetDate(article, operationInfo.InitialDate, userData.UserName);
				}
				return new OperationResult(true, g["Initial date updated!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		public class ArticleSelectionInfo
		{
			public List<int> Articles;
			public DateTime InitialDate;
		}
	}
}