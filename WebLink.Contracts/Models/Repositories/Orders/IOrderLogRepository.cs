using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
    public interface IOrderLogRepository : IGenericRepository<IOrderLog>
    {
		Task<IEnumerable<OrderLogDTO>> GetHistoryAsync(int orderID, OrderLogLevel level);
		Task<IEnumerable<OrderLogDTO>> GetHistoryAsync(PrintDB ctx, int orderID, OrderLogLevel level);
		IEnumerable<OrderLogDTO> GetHistory(int orderID, OrderLogLevel level);
		IEnumerable<OrderLogDTO> GetHistory(PrintDB ctx, int orderID, OrderLogLevel level);

        IEnumerable<OrderLogDTO> GetHistoryByMsj(List<int> orderIDs, string msj);
        IEnumerable<OrderLogDTO> GetHistoryByMsj(PrintDB ctx, List<int> orderIDs, string msj);

        Task<IEnumerable<OrderLogDTO>> GetOrderGroupHistoryAsync(int orderGroupID, OrderLogLevel level);
		Task<IEnumerable<OrderLogDTO>> GetOrderGroupHistoryAsync(PrintDB ctx, int orderGroupID, OrderLogLevel level);
		IEnumerable<OrderLogDTO> GetOrderGroupHistory(int orderGroupID, OrderLogLevel level);
		IEnumerable<OrderLogDTO> GetOrderGroupHistory(PrintDB ctx, int orderGroupID, OrderLogLevel level);

		IOrderLog InitLog(int orderID, string msg = null, OrderLogLevel level = OrderLogLevel.DEBUG, string comments = null);

		Task LogAsync(IOrderLog log);
		Task LogAsync(PrintDB ctx, IOrderLog log);
		void Log(IOrderLog log);
		void Log(PrintDB ctx, IOrderLog log);

		Task InfoAsync(IOrderLog log);
		Task InfoAsync(PrintDB ctx, IOrderLog log);
		void Info(IOrderLog log);
		void Info(PrintDB ctx, IOrderLog log);

		Task InfoAsync(int orderID, string msg);
		Task InfoAsync(PrintDB ctx, int orderID, string msg);
		void Info(int orderID, string msg);
		void Info(PrintDB ctx, int orderID, string msg);

		Task WarnAsync(IOrderLog log);
		Task WarnAsync(PrintDB ctx, IOrderLog log);
		void Warn(IOrderLog log);
		void Warn(PrintDB ctx, IOrderLog log);

		Task WarnAsync(int orderID, string msg);
		Task WarnAsync(PrintDB ctx, int orderID, string msg);
		void Warn(int orderID, string msg);
		void Warn(PrintDB ctx, int orderID, string msg);

		Task ErrorAsync(IOrderLog log);
		Task ErrorAsync(PrintDB ctx, IOrderLog log);
		void Error(IOrderLog log);
		void Error(PrintDB ctx, IOrderLog log);

		Task ErrorAsync(int orderID, string msg);
		Task ErrorAsync(PrintDB ctx, int orderID, string msg);
		void Error(int orderID, string msg);
		void Error(PrintDB ctx, int orderID, string msg);

		Task DebugAsync(IOrderLog log);
		Task DebugAsync(PrintDB ctx, IOrderLog log);
		void Debug(IOrderLog log);
		void Debug(PrintDB ctx, IOrderLog log);

		Task DebugAsync(int orderID, string msg);
		Task DebugAsync(PrintDB ctx, int orderID, string msg);
		void Debug(int orderID, string msg);
		void Debug(PrintDB ctx, int orderID, string msg);
	}
}