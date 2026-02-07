using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Services
{
    public class GrupoTendamManualEntryGrouppingService : IManualEntryGrouppingService
    {
        private readonly IFactory factory;
        private IGrupoTendamWriter writer;

        public GrupoTendamManualEntryGrouppingService(IFactory factory, IGrupoTendamWriter writer)
        {
            this.factory = factory;
            this.writer = writer;
        }

        public async Task<OperationResult> GroupOrders(OrderPoolGrouping orderPoolGrouping)
        {
            // Validate input parameters
            if (orderPoolGrouping == null)
            {
                throw new ArgumentNullException(nameof(orderPoolGrouping));
            }

            if (orderPoolGrouping.Orders == null || !orderPoolGrouping.Orders.Any())
            {
                throw new ArgumentException("Orders list cannot be null or empty.", nameof(orderPoolGrouping.Orders));
            }

            if (string.IsNullOrWhiteSpace(orderPoolGrouping.Pattern))
            {
                throw new ArgumentException("Pattern cannot be null or empty.", nameof(orderPoolGrouping.Pattern));
            }

            using (var ctx = factory.GetInstance<PrintDB>())
            {
                if (ctx == null)
                {
                    throw new InvalidOperationException("Failed to create PrintDB context.");
                }

                
                var orders = ctx.OrderPools
                        .Where(o => o.ProjectID == orderPoolGrouping.ProjectID && 
                                    o.CategoryCode1 == orderPoolGrouping.Pattern && 
                                    orderPoolGrouping.Orders.Contains(o.OrderNumber) &&
                                    o.ProcessedDate == null)
                        .ToList();

     
                
                if (!orders.Any())
                {
                    // Log or handle the case where no orders were found
                    return null;
                }

                // Group by ArticleCode, Color, and Size and sum Quantity
                var groupedOrders = orders
                    .GroupBy(o => new { o.CategoryText2, o.CategoryText3,  })
                    .Select(g => new OrderPool
                    {
                        ArticleCode = g.First().ArticleCode,
                        ColorCode = g.First().ColorCode,
                        Size = g.First().Size,
                        Quantity = g.Sum(o => o.Quantity),
                        ProjectID = g.First().ProjectID,
                        CategoryCode1 = g.First().CategoryCode1,
                        OrderNumber = orderPoolGrouping.Pattern,
                        ProcessedDate = g.First().ProcessedDate,
                        CategoryCode2 = g.First().CategoryCode2,
                        CategoryCode3 = g.First().CategoryCode3,
                        CategoryCode4 = g.First().CategoryCode4,
                        CategoryCode5 = g.First().CategoryCode5,
                        CategoryCode6 = g.First().CategoryCode6,
                        CategoryText1 = g.First().CategoryText1,
                        CategoryText2 = g.Key.CategoryText2,
                        CategoryText3 = g.Key.CategoryText3,
                        CategoryText4 = g.First().CategoryText4,
                        CategoryText5 = g.First().CategoryText5,
                        CategoryText6 = g.First().CategoryText6,
                        ColorName = g.First().ColorName,
                        CreationDate = g.First().CreationDate,
                        Price1 = g.First().Price1,
                        Price2 = g.First().Price2,
                        ProviderCode1 = g.First().ProviderCode1,
                        ProviderCode2 = g.First().ProviderCode2, 
                        ProviderName1 = g.First().ProviderName1,
                        ProviderName2 = g.First().ProviderName2,    
                        ExtraData = g.First().ExtraData
                        
                    })
                    .ToList();

                if(!groupedOrders.Any())
                {
                    return new OperationResult() { Success = false, Message="No orders processed" };
                }

                var result =  await writer.WriteTendamFile(GetDataEntryRq(orderPoolGrouping.CompanyID, orderPoolGrouping.BrandID, orderPoolGrouping.ProjectID),
                                       GetFileName(groupedOrders.First()),
                                       TendamToOrderPoolMapper.MapToTendamMappingList(groupedOrders)); 

                if(result.Success)
                {
                    SetProceesedDate(orderPoolGrouping.Orders, 
                                     orderPoolGrouping.ProjectID, 
                                     orderPoolGrouping.Pattern,
                                     groupedOrders.First().CategoryText2, 
                                     groupedOrders.First().CategoryText3);
                }
                return result; 
            }
        }

        private void SetProceesedDate(List<string> orders, int projectid, string pattern, string EanCode, string format)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {

                var ordersToUpdate = ctx.OrderPools
                    .Where(o => o.ProjectID == projectid && 
                                orders.Contains(o.OrderNumber) &&
                                o.CategoryCode1 == pattern &&

                                o.ProcessedDate == null)
                    .ToList();

                if (ordersToUpdate.Any())
                {
                    var currentDate = DateTime.Now;
                    foreach (var order in ordersToUpdate)
                    {
                        order.ProcessedDate = currentDate;
                    }

                    ctx.SaveChanges();
                }
            }
        }

        private string GetFileName(OrderPool orderPool)
        {
            return $"CTF_01_{orderPool.OrderNumber}_{orderPool.CategoryCode1}_X_00000-0.txt";
        }

        private DataEntryRq GetDataEntryRq(int CompanyID, int BrandID,int projectID)
        {
           return new DataEntryRq()
            {
                CompanyID = CompanyID,
                BrandID = BrandID,
                SeasonID = projectID,   
            };

        }
    }
}
