using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
    /// TODO: could be nice use LoggerFactory that use custom repository
    /// we need create database report over order logs
    public class OrderLogRepository : GenericRepository<IOrderLog, OrderLog>, IOrderLogRepository
    {
        public OrderLogRepository(IFactory factory)
            : base(factory, (ctx) => ctx.OrderLogs)
        {
        }

        protected override string TableName => "CompanyOrderLogs";


        protected override void UpdateEntity(PrintDB ctx, IUserData userData, OrderLog actual, IOrderLog data)
        {
            // DISABLE UPDATE ORDER LOGS
            throw new NotImplementedException();
        }


        public IEnumerable<OrderLogDTO> GetHistoryByMsj(List<int> orderIDs, string msj)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetHistoryByMsj(ctx, orderIDs, msj);
            }
        }

        public IEnumerable<OrderLogDTO> GetHistoryByMsj(PrintDB ctx, List<int> orderIDs, string msj)
        {

            var qry = (from o in ctx.OrderLogs                     
                       where orderIDs.Any(a => a == o.OrderID)  && o.Message == msj
                       orderby o.CreatedDate
                       select new OrderLogDTO
                       {
                           OrderID = o.OrderID,
                           Message = o.Message,
                           Comments = o.Comments,
                           CreatedBy = o.CreatedBy
                       });

            var q = qry.ToList();
            return q;
        }


        public IEnumerable<OrderLogDTO> GetHistory(int orderID, OrderLogLevel level)
        {
            return GetHistoryAsync(orderID, level).Result;
        }

        public IEnumerable<OrderLogDTO> GetHistory(PrintDB ctx, int orderID, OrderLogLevel level)
        {
            return GetHistoryAsync(ctx, orderID, level).Result;
        }

        public async Task<IEnumerable<OrderLogDTO>> GetHistoryAsync(int orderID, OrderLogLevel level)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return await GetHistoryAsync(ctx, orderID, level);
            }
        }

        public async Task<IEnumerable<OrderLogDTO>> GetHistoryAsync(PrintDB ctx, int orderID, OrderLogLevel level)
        {
            var orderRow = await ctx.CompanyOrders.Where(w => w.ID.Equals(orderID)).FirstAsync();

            var qry = (from o in ctx.OrderLogs
                       join c in ctx.CompanyOrders on o.OrderID equals c.ID
                       join p in ctx.PrinterJobs on c.ID equals p.CompanyOrderID
                       join a in ctx.Articles on p.ArticleID equals a.ID
                       where c.ID == orderID && c.ProjectID == orderRow.ProjectID && o.Level <= level
                       orderby o.CreatedDate
                       select new OrderLogDTO
                       {
                           LogID = o.ID,
                           OrderID = c.ID,
                           OrderNumber = c.OrderNumber,
                           Level = o.Level,
                           LevelText = o.Level.GetText(),
                           Message = o.Message,
                           Comments = o.Comments,
                           CreatedBy = o.CreatedBy,
                           CreatedDate = o.CreatedDate,
                           ArticleCode = a.ArticleCode
                           
                       });

            var q = await qry.ToListAsync();
            return q;
		}

        public IEnumerable<OrderLogDTO> GetOrderGroupHistory(int orderGroupID, OrderLogLevel level)
        {
            return GetOrderGroupHistoryAsync(orderGroupID, level).Result;
        }

        public async Task<IEnumerable<OrderLogDTO>> GetOrderGroupHistoryAsync(int orderGroupID, OrderLogLevel level)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return await GetOrderGroupHistoryAsync(ctx, orderGroupID, level);
			}
		}


        public IEnumerable<OrderLogDTO> GetOrderGroupHistory(PrintDB ctx, int orderGroupID, OrderLogLevel level)
        {
            return GetOrderGroupHistoryAsync(ctx, orderGroupID, level).Result;
        }

        public async Task<IEnumerable<OrderLogDTO>> GetOrderGroupHistoryAsync(PrintDB ctx, int orderGroupID, OrderLogLevel level)
		{
			var orderGroup = await ctx.OrderGroups.Where(w => w.ID.Equals(orderGroupID)).FirstAsync();

            var qry = (from o in ctx.OrderLogs
                       join c in ctx.CompanyOrders on o.OrderID equals c.ID
                       join p in ctx.PrinterJobs on c.ID equals p.CompanyOrderID
                       join a in ctx.Articles on p.ArticleID equals a.ID
                       where c.OrderGroupID == orderGroupID && c.ProjectID == orderGroup.ProjectID && o.Level <= level
                       orderby o.CreatedDate
                       select new OrderLogDTO
                       {
                           LogID = o.ID,
                           OrderID = c.ID,
                           OrderNumber = c.OrderNumber,
                           Level = o.Level,
                           LevelText = o.Level.GetText(),
                           Message = o.Message,
                           Comments = o.Comments,
                           CreatedBy = o.CreatedBy,
                           CreatedDate = o.CreatedDate,
                           ArticleCode = a.ArticleCode

                       });

            var q = await qry.ToListAsync();
            return q;
		}


		public IOrderLog InitLog(int orderID, string msg = null, OrderLogLevel level = OrderLogLevel.DEBUG, string comments = null)
		{
			var reg = Create();
			reg.OrderID = orderID;
			reg.Message = msg;
			reg.Level = level;
			reg.Comments = comments;
			return reg;
		}


		public async Task LogAsync(IOrderLog log)
		{
			await InsertAsync(log);
		}


		public async Task LogAsync(PrintDB ctx, IOrderLog log)
		{
			await InsertAsync(ctx, log);
		}


		public void Log(IOrderLog log)
		{
			Insert(log);
		}


		public void Log(PrintDB ctx, IOrderLog log)
		{
			Insert(ctx, log);
		}


		public async Task InfoAsync(IOrderLog log)
		{
			log.Level = OrderLogLevel.INFO;
			await InsertAsync(log);
		}


		public async Task InfoAsync(PrintDB ctx, IOrderLog log)
		{
			log.Level = OrderLogLevel.INFO;
			await InsertAsync(ctx, log);
		}

		public async Task InfoAsync(int orderID, string msg)
		{
			var log = InitLog(orderID, msg);
			await InfoAsync(log);
		}


		public async Task InfoAsync(PrintDB ctx, int orderID, string msg)
		{
			var log = InitLog(orderID, msg);
			await InfoAsync(ctx, log);
		}


		public void Info(IOrderLog log)
		{
			log.Level = OrderLogLevel.INFO;
			Insert(log);
		}


		public void Info(PrintDB ctx, IOrderLog log)
		{
			log.Level = OrderLogLevel.INFO;
			Insert(ctx, log);
		}


		public void Info(int orderID, string msg)
		{
			var log = InitLog(orderID, msg);
			Info(log);
		}


		public void Info(PrintDB ctx, int orderID, string msg)
		{
			var log = InitLog(orderID, msg);
			Info(ctx, log);
		}


		public async Task WarnAsync(IOrderLog log)
		{
			log.Level = OrderLogLevel.WARN;
			await InsertAsync(log);
		}


		public async Task WarnAsync(PrintDB ctx, IOrderLog log)
		{
			log.Level = OrderLogLevel.WARN;
			await InsertAsync(ctx, log);
		}


		public async Task WarnAsync(int orderID, string msg)
		{
			var log = InitLog(orderID, msg);
			await WarnAsync(log);
		}


		public async Task WarnAsync(PrintDB ctx, int orderID, string msg)
		{
			var log = InitLog(orderID, msg);
			await WarnAsync(ctx, log);
		}


		public void Warn(IOrderLog log)
		{
			log.Level = OrderLogLevel.WARN;
			Insert(log);
		}


		public void Warn(PrintDB ctx, IOrderLog log)
		{
			log.Level = OrderLogLevel.WARN;
			Insert(ctx, log);
		}


		public void Warn(int orderID, string msg)
		{
			var log = InitLog(orderID, msg);
			Warn(log);
		}


		public void Warn(PrintDB ctx, int orderID, string msg)
		{
			var log = InitLog(orderID, msg);
			Warn(ctx, log);
		}


		public async Task ErrorAsync(IOrderLog log)
		{
			log.Level = OrderLogLevel.ERROR;
			await InsertAsync(log);
		}


		public async Task ErrorAsync(PrintDB ctx, IOrderLog log)
		{
			log.Level = OrderLogLevel.ERROR;
			await InsertAsync(ctx, log);
		}


		public async Task ErrorAsync(int orderID, string msg)
		{
			var log = InitLog(orderID, msg);
			await ErrorAsync(log);
		}


		public async Task ErrorAsync(PrintDB ctx, int orderID, string msg)
		{
			var log = InitLog(orderID, msg);
			await ErrorAsync(ctx, log);
		}


		public void Error(IOrderLog log)
		{
			log.Level = OrderLogLevel.ERROR;
			Insert(log);
		}


		public void Error(PrintDB ctx, IOrderLog log)
		{
			log.Level = OrderLogLevel.ERROR;
			Insert(ctx, log);
		}


		public void Error(int orderID, string msg)
		{
			var log = InitLog(orderID, msg);
			Error(log);
		}


		public void Error(PrintDB ctx, int orderID, string msg)
		{
			var log = InitLog(orderID, msg);
			Error(ctx, log);
		}


		public async Task DebugAsync(IOrderLog log)
		{
			log.Level = OrderLogLevel.DEBUG;
			await InsertAsync(log);
		}


		public async Task DebugAsync(PrintDB ctx, IOrderLog log)
		{
			log.Level = OrderLogLevel.DEBUG;
			await InsertAsync(ctx, log);
		}


		public async Task DebugAsync(int orderID, string msg)
		{
			var log = InitLog(orderID, msg);
			await DebugAsync(log);
		}


		public async Task DebugAsync(PrintDB ctx, int orderID, string msg)
		{
			var log = InitLog(orderID, msg);
			await DebugAsync(ctx, log);
		}


		public void Debug(IOrderLog log)
		{
			log.Level = OrderLogLevel.DEBUG;
			Insert(log);
		}


		public void Debug(PrintDB ctx, IOrderLog log)
		{
			log.Level = OrderLogLevel.DEBUG;
			Insert(ctx, log);
		}


		public void Debug(int orderID, string msg)
		{
			var log = InitLog(orderID, msg);
			Debug(log);
		}


		public void Debug(PrintDB ctx, int orderID, string msg)
		{
			var log = InitLog(orderID, msg);
			Debug(ctx, log);
		}
	}
}
