using System;
using System.Collections.Generic;

namespace Service.Contracts.Infrastructure.Encoding.Tempe
{
    public interface IEpcRepositoryTempe
    {
        void MarkAsUsedPreencoding(int orderId, int detailId);
        void MarkAsUsed(int orderId, int detailId);
        bool IsFirstTimePreencoding(int orderId, int detailId);
        bool IsFirstTime(int orderId, int detailId);
        bool AnyEpcs(int orderId, int detailId);
        void DeleteOldOrders(TimeSpan timeThreshold);
        void DeleteOrder(OrderStatus order);
        int ExecuteNonQuery(string sql, params object[] args);
        AllocatedEpc GetEpc(string epc);
        LockInfo GetLockInfo(int orderid);
        OrderStatus GetOrder(int orderid);
        OrderDetail GetOrderDetail(int orderid, int detailid);
        int GetOrderDetailEpcCount(int orderid, int detailid);
        List<OrderDetail> GetOrderDetails(int orderid);
        List<AllocatedEpc> GetOrderEpcs(int orderid);
        PreencodingOrderDetail GetPreencodingDetail(int orderid, int detailid);
        List<PreencodingOrderDetail> GetPreencodingDetails(int orderid);
        void InsertEpc(AllocatedEpc epc);
        void InsertLockInfo(LockInfo locks);
        void InsertOrder(OrderStatus order);
        void InsertOrderDetail(OrderDetail detail);
        void InsertOrderDetails(int orderid, List<OrderDetail> details);
        void InsertOrderEpcs(List<AllocatedEpc> epcs);
        void InsertPreencodingOrderDetail(PreencodingOrderDetail detail);
        void InsertPreencodingOrderDetails(int orderid, List<PreencodingOrderDetail> details);
        List<AllocatedEpc> PeekEpcs(int orderId, int detailId, int max);
        void SaveEpcs(IEnumerable<AllocatedEpc> epcs);
        List<AllocatedEpc> TakeAndDeleteEpcs(int orderId, int detailId, int qty);
        void UpdateEpc(AllocatedEpc epc);
        void UpdateLockInfo(LockInfo locks);
        void UpdateOrder(OrderStatus order);
        void UpdateOrderDetail(OrderDetail detail);
        void UpdatePreencodingOrderDetail(PreencodingOrderDetail detail);
    }
}