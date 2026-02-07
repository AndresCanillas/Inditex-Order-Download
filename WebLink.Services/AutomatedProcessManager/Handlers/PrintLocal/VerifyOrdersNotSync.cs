using Service.Contracts;
using Service.Contracts.PrintLocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services
{
    public class VerifyOrdersNotSync : EQEventHandler<NotifyOrdersSyncEvent>
    {
        protected IFactory factory;
        public VerifyOrdersNotSync(IFactory factory)
        {
            this.factory = factory;
        }

        public override EQEventHandlerResult HandleEvent(NotifyOrdersSyncEvent e)
        {

            using (var ctx = factory.GetInstance<PrintDB>())
            {

            }


            return EQEventHandlerResult.OK;
        }

        public IEnumerable<int> CompareOrders(PrintDB ctx, IEnumerable<int> orders, int factoryID, double deltaTime)
        {
            if (orders.Count() < 1)
            {
                return new List<int>();
            }

            var fromDate = DateTime.Now.AddHours(-1 * deltaTime);
            var pending = ctx.CompanyOrders
                .Where(w => w.LocationID == factoryID && !orders.Contains(w.ID) && w.CreatedDate >= fromDate  && w.OrderStatus == OrderStatus.ProdReady )
                .Select(s => s.ID)
                .ToList();

            return pending;
        }
    }
}
