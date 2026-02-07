using Service.Contracts;
using Services.Core;
using System.Linq;

namespace WebLink.Contracts.Models
{
    public class OrderUpdatePropertiesRepository : GenericRepository<IOrderUpdateProperties, OrderUpdateProperties>, IOrderUpdatePropertiesRepository
    {
        private ILogService log;

        public OrderUpdatePropertiesRepository(
            IFactory factory,
            ILogService log
            )
            : base(factory, (ctx) => ctx.OrderUpdateProperties)
        {
            this.log = log;
        }


        protected override string TableName => "OrderValidationSettings";// TODO: this property is not used -> refactor required to remove


        //protected override void AuthorizeOperation(PrintDB ctx, IUserData userData, OrderUpdateProperties data)
        //      {
        //          if (userData.IsIDT || userData.UserName == "SYSTEM") return;  // Do not restrict access to IDT Users/SYSTEM

        //          var order = ctx.CompanyOrders.FirstOrDefault(x => x.ID == data.OrderID);

        //          if (order.CompanyID != userData.SelectedCompanyID && order.SendToCompanyID != userData.SelectedCompanyID)
        //          {
        //              var ex = new Exception($"Not authorized, order[{order.ID}] user[{userData.UserName}] SelectedCompanyID [{userData.SelectedCompanyID}]  "); // TODO: customize exception to save data inner
        //              log.LogException("User Not Autorized to Execute Operation over OrderUpdateProperties", ex);
        //              throw ex;
        //          }
        //      }


        protected override void UpdateEntity(PrintDB ctx, IUserData userData, OrderUpdateProperties actual, IOrderUpdateProperties data)
        {
            actual.IsActive = data.IsActive;
            actual.IsRejected = data.IsRejected;
        }


        public IOrderUpdateProperties GetByOrderID(int orderID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByOrderID(ctx, orderID);
            }
        }


        public IOrderUpdateProperties GetByOrderID(PrintDB ctx, int orderID)
        {
            return ctx.OrderUpdateProperties
                .FirstOrDefault(f => f.OrderID.Equals(orderID));
        }
    }
}
