using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts.Models;
using WebLink.Contracts.Services;

namespace WebLink.Services
{
    public class OrderWithArticleDetailedService : IOrderWithArticleDetailedService
    {
        private IOrderRepository orderRepo { get; set; }
        private ILogService log;
        public OrderWithArticleDetailedService(IOrderRepository orderRepo, ILogService log)
        {
            this.orderRepo = orderRepo;
            this.log = log;
        }

        public void Execute(int orderId, 
                            int sendToComapnyID,
                            Action<string ,string, int, OrderDetailDTO> callbackDetailed,
                            Action<int, OrderDetailDTO> callbackNotDetailed,
                            Action <int, OrderDetailDTO, int> callbackArtifacts = null )
        {
            var articleFilter = new OrderArticlesFilter() { OrderID = new List<int>() { orderId }, SendToCompanyID = sendToComapnyID, ActiveFilter = OrderActiveFilter.Active, ArticleType = ArticleTypeFilter.All };
            var articles = orderRepo.GetOrderArticles(articleFilter, ProductDetails.Label).ToList();

            var grouped = articles.GroupBy(g => g.ArticleID).ToList();
            var lineNumber = 0;

            foreach (var grp in grouped)
            {
                int Total = grp.Sum(itmGrp => itmGrp.Quantity);
                var a = articles.Find(f => f.ArticleID.Equals(grp.Key));
                if (a.IsDetailedArticle && !a.IsItem)
                {
                    foreach (var item in grp)
                    {
                        if (item.ProductData != null)
                        {
                            if (item.Quantity > 0)
                            {
                                lineNumber++;
                                var size = item.ProductData["Size"] ?? string.Empty;
                                var color = item.ProductData["Color"] ?? string.Empty;
                                callbackDetailed?.Invoke($"{orderId}-{lineNumber}", $"T:{size} C:{color}", item.Quantity, a);
                            }
                        }
                        else
                        {
                            var labelID = item.LabelID.HasValue ? item.LabelID.ToString() : "Empty";
                            log.LogWarning($"Synchronizing IRIS/SAGE OrderID [{orderId}] - Article: [{a.ArticleID}] - IsItem: [{item.IsItem}] - LabelId [{labelID}] - has no Product Data");
                        }
                    }
                }
                else
                {
                    callbackNotDetailed?.Invoke(Total, a); 
                }

                if (callbackArtifacts!= null) 
                {
                    //lineNumber += 1;
                    callbackArtifacts.Invoke(Total, a, lineNumber); 
                }

            };
        }
    }
}

