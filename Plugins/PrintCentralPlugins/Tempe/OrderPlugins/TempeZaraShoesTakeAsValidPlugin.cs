using Service.Contracts.Database;
using Service.Contracts;
using Service.Contracts.PrintCentral;
using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts.Models;
using System.Linq;

namespace SmartdotsPlugins.Tempe.OrderPlugins
{

        [FriendlyName("Tempe.Zara.TakeAsValidPlugin")]
        [Description("Tempe.Zara.TakeAsValidPlugin")]
        public class TempeZaraShoesTakeAsValidPlugin : IOrderSetValidatorPlugin
        {
            private readonly IOrderRepository orderRepo;

            private List<string> TakeAsValidArticles; // TODO: create a catalog for dynamic upates
            public TempeZaraShoesTakeAsValidPlugin(IOrderRepository orderRepo)
            {
                this.orderRepo = orderRepo;
                TakeAsValidArticles = new List<string>() { "INSIDE_NOSIZE_MANUAL", "INSIDE_SIZE_MANUAL" };
                // TODO move the list to dynamic catalog
            }

            public int TakeAsValidated(OrderPluginData orderData)
            {

                var filter = new OrderArticlesFilter()
                {
                    OrderID = new List<int> { orderData.OrderID}
                };

                var articleDetails = orderRepo.GetOrderArticles(filter);

                var article = articleDetails.FirstOrDefault();

                if(article == null) return 0;// throw new Exception($"TempeZaraShoesTakeAsValidPlugin order [{orderData.OrderID}] not contain Article Details");

                if(TakeAsValidArticles.Contains(article.ArticleCode)) return 1;

                return 0;

            }

            public void Dispose()
            {
            }
        }
}
