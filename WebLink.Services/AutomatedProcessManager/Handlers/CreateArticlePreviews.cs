using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using Services.Core;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services.Automated
{
	public class CreateArticlePreviews: EQEventHandler<StartOrderProcessingEvent>
	{
		private IFactory factory;
		private IOrderRepository orderRepo;

		public CreateArticlePreviews(
			IFactory factory,
			IOrderRepository orderRepo
			)
		{
			this.factory = factory;
			this.orderRepo = orderRepo;
		}

		public override EQEventHandlerResult HandleEvent(StartOrderProcessingEvent e)
		{
			// Eval each article in the received order to see if we can create the article preview or if we need to wait for
			// data entry. If an article does not require any data entry, we can generate the preview now, and have it ready
			// for later. Notice this handler does not raise any additional events.

			// TODO: return if the order will be validated automatically

			var list = new List<ArticlePreviewData>();
			var articles = orderRepo.GetOrderArticles(new OrderArticlesFilter() { OrderID = new List<int>() { e.OrderID } }, ProductDetails.None);

			foreach (var article in articles)
			{
				if (article.IsItem) continue;
				if (article.RequiresDataEntry ?? false) continue;
				if (!article.LabelID.HasValue) continue;

				list.Add(new ArticlePreviewData() {
					LabelID = article.LabelID.Value,
					ProductDataID = article.ProductDataID
				});
			}

			if(list.Count > 0)
				_ = CreatePreviews(factory, e.OrderID, list);

			return new EQEventHandlerResult() { Success = true };
		}


		private async Task CreatePreviews(IFactory factory, int orderid, List<ArticlePreviewData> list)
		{
			ILabelRepository labelRepo = factory.GetInstance<ILabelRepository>();
			IOrderLogService orderLog = factory.GetInstance<IOrderLogService>();
			ILogService log = factory.GetInstance<ILogService>();
			try
			{
				foreach (var previewData in list)
				{
					using (var stream = await labelRepo.GetArticlePreviewAsync(previewData.LabelID, orderid, previewData.ProductDataID))
					{
					}
				}
				orderLog.Log(orderid, $"Created previews of all articles that do not require data entry for order {orderid}.", OrderLogLevel.DEBUG);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				orderLog.Log(orderid, $"Error while creating previews for order {orderid}.", OrderLogLevel.DEBUG);
			}
		}

		class ArticlePreviewData
		{
			public int LabelID;
			public int ProductDataID;
		}
	}
}
