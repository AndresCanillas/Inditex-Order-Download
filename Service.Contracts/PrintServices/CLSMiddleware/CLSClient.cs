using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.CLSMiddleware
{
	public interface ICLSClient
	{
		string Url { get; set; }
		Task SendJobsAsync(List<PrintedTagData> data);
		Task CloseOrderAsync(ClosedOrder orderToClose);
		Task CloseOrdersAsync(List<ClosedOrder> ordersToClose);
	}


	public class CLSClient: BaseServiceClient, ICLSClient
	{
		public async Task SendJobsAsync(List<PrintedTagData> data)
		{
			await InvokeAsync<List<PrintedTagData>>("/api/v1/PrintLocal/LoadOrderByService", data);
		}

		public async Task CloseOrderAsync(ClosedOrder ordersToClose)
		{
			await InvokeAsync<List<ClosedOrder>>("/api/v1/PrintLocal/OrderClosedByMDOrder", new List<ClosedOrder>() { ordersToClose });
		}

		public async Task CloseOrdersAsync(List<ClosedOrder> ordersToClose)
		{
			await InvokeAsync<List<ClosedOrder>>("/api/v1/PrintLocal/OrderClosedByMDOrder", ordersToClose);
		}
	}
}
