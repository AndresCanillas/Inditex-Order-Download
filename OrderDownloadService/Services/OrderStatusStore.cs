using System.Collections.Concurrent;
using System.Collections.Generic;

namespace OrderDonwLoadService.Services
{
    public class OrderStatus
    {
        public string VendorId { get; set; }
        public string OrderId { get; set; }
        public bool SentToPrintCentral { get; set; }
    }

    public interface IOrderStatusStore
    {
        void Add(OrderStatus status);
        List<OrderStatus> GetAndClear();
    }

    public class InMemoryOrderStatusStore : IOrderStatusStore
    {
        private readonly ConcurrentBag<OrderStatus> statuses = new ConcurrentBag<OrderStatus>();

        public void Add(OrderStatus status)
        {
            statuses.Add(status);
        }

        public List<OrderStatus> GetAndClear()
        {
            var list = new List<OrderStatus>();
            while(statuses.TryTake(out var status))
            {
                list.Add(status);
            }
            return list;
        }
    }
}
