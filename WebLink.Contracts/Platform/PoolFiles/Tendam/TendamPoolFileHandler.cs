using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Platform.PoolFiles.Tendam
{
    public class TendamPoolFileHandler : IPoolFileHandler
    {
        private IFactory factory;

        public TendamPoolFileHandler(IFactory factory)
        {
            this.factory = factory;
        }

        public async Task InsertListAsync(IProject project, List<OrderPool> orderPools, Action<int, IList<IOrderPool>> result = null)
        {
            try
            {
                using(var ctx = factory.GetInstance<PrintDB>())
                {
                    foreach(var orderPool in orderPools)
                    {
                        // Search for existing record with same ProjectID, OrderNumber, CategoryText1, CategoryText2, CategoryText3
                        var existingRecord = await ctx.OrderPools
                            .FirstOrDefaultAsync(op =>
                                op.ProjectID == orderPool.ProjectID &&
                                op.OrderNumber == orderPool.OrderNumber &&
                                op.CategoryCode1 == orderPool.CategoryCode1 &&
                                op.CategoryText2 == orderPool.CategoryText2 &&
                                op.CategoryText3 == orderPool.CategoryText3 && 
                                op.ProcessedDate == null);

                        if(existingRecord != null)
                        {
                            existingRecord.OrderNumber = orderPool.OrderNumber;
                            existingRecord.ProjectID = orderPool.ProjectID;
                            existingRecord.CategoryCode1 = orderPool.CategoryCode1;
                            existingRecord.CategoryText2 = orderPool.CategoryText2;
                            existingRecord.CategoryText3 = orderPool.CategoryText3;

                            // Update all other properties from OrderPool interface
                            // Copy all properties except ID
                            foreach(var property in typeof(OrderPool).GetProperties())
                            {
                                if(property.Name != "ID" && property.CanWrite)
                                {
                                    var value = property.GetValue(orderPool);
                                    property.SetValue(existingRecord, value);
                                }
                            }

                            ctx.OrderPools.Update(existingRecord);
                        }
                        else
                        {
                            // No match found -> Create new record
                            ctx.OrderPools.Add(orderPool);
                        }
                    }

                    ctx.SaveChanges();
                }
            }
            catch(Exception ex)
            {

                throw ex;
            }
        }

        public Task UploadAsync(IProject project, Stream stream, Action<int, IList<IOrderPool>> result = null)
        {
            throw new NotImplementedException();
        }
    }
}
