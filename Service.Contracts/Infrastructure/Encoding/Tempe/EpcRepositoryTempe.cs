using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Service.Contracts.Infrastructure.Encoding.Tempe
{

    public partial class EpcRepositoryTempe : IEpcRepositoryTempe
    {
        private static object syncObj = new object();
        private static bool dbInitialized;
        private IConnectionManager connectionManager;

        public EpcRepositoryTempe(IConnectionManager connectionManager)
        {
            this.connectionManager = connectionManager;
            InitializeDatabase();
        }

        public OrderStatus GetOrder(int orderid)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                return conn.SelectOne<OrderStatus>("select * from OrderStatus where OrderID = @orderid", orderid);
            }
        }

        public void InsertOrder(OrderStatus order)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                conn.Insert(order);
            }
        }

        public void UpdateOrder(OrderStatus order)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                conn.Update(order);
            }
        }

        public void DeleteOrder(OrderStatus order)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                if(GetOrderEpcCount(order.OrderID) == 0)
                {
                    conn.ExecuteNonQuery("delete from LockInfo where OrderID = @orderid", order.OrderID);
                    conn.ExecuteNonQuery("delete from OrderDetail where OrderID = @orderid", order.OrderID);
                    conn.ExecuteNonQuery("delete from OrderStatus where OrderID = @orderid", order.OrderID);
                    conn.ExecuteNonQuery("delete from PreencodingOrderDetail where OrderID = @orderid", order.OrderID);
                    conn.Delete(order);
                }

            }
        }

        public List<OrderDetail> GetOrderDetails(int orderid)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                return conn.Select<OrderDetail>("select * from OrderDetail where OrderID = @orderid", orderid);
            }
        }

        public OrderDetail GetOrderDetail(int orderid, int detailid)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                return conn.SelectOne<OrderDetail>("select * from OrderDetail where OrderID = @orderid and DetailID = @detailid", orderid, detailid);
            }
        }

        public int GetOrderDetailEpcCount(int orderid, int detailid)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                return Convert.ToInt32(
                    conn.ExecuteScalar(@"
						select
							count(*)
						from
							AllocatedEpc
						where 
							OrderID = @orderid and
							DetailID = @detailid", orderid, detailid));
            }
        }

        public void InsertOrderDetails(int orderid, List<OrderDetail> details)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                foreach(var detail in details)
                {
                    conn.Insert(detail);
                }
            }
        }

        public void InsertOrderDetail(OrderDetail detail)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                conn.Insert(detail);
            }
        }

        public void UpdateOrderDetail(OrderDetail detail)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                conn.Update(detail);
            }
        }

        public List<PreencodingOrderDetail> GetPreencodingDetails(int orderid)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                return conn.Select<PreencodingOrderDetail>("select * from PreencodingOrderDetail where OrderID = @orderid", orderid);
            }
        }

        public PreencodingOrderDetail GetPreencodingDetail(int orderid, int detailid)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                return conn.SelectOne<PreencodingOrderDetail>("select * from PreencodingOrderDetail where OrderID = @orderid and DetailID = @detailid", orderid, detailid);
            }
        }

        public void InsertPreencodingOrderDetails(int orderid, List<PreencodingOrderDetail> details)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                foreach(var detail in details)
                {
                    conn.Insert(detail);
                }
            }
        }

        public void InsertPreencodingOrderDetail(PreencodingOrderDetail detail)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                conn.Insert(detail);
            }
        }

        public void UpdatePreencodingOrderDetail(PreencodingOrderDetail detail)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                conn.Update(detail);
            }
        }

        public LockInfo GetLockInfo(int orderid)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                return conn.SelectOne<LockInfo>("select * from LockInfo where OrderID = @orderid", orderid);
            }
        }

        public void InsertLockInfo(LockInfo locks)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                conn.Insert(locks);
            }
        }

        public void UpdateLockInfo(LockInfo locks)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                conn.Update(locks);
            }
        }

        public List<AllocatedEpc> GetOrderEpcs(int orderid)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                return conn.Select<AllocatedEpc>("select * from AllocatedEpc where OrderID = @orderid", orderid);
            }
        }

        public void InsertOrderEpcs(List<AllocatedEpc> epcs)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                foreach(var epc in epcs)
                {
                    conn.Insert(epc);
                }
            }
        }

        public AllocatedEpc GetEpc(string epc)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                return conn.SelectOne<AllocatedEpc>("select * from AllocatedEpc where Epc = @epc", epc);
            }
        }

        public void InsertEpc(AllocatedEpc epc)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                conn.Insert(epc);
            }
        }

        public void UpdateEpc(AllocatedEpc epc)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                conn.Update(epc);
            }
        }


        private static string GetInCondition(IEnumerable<string> list)
        {
            if(list == null)
                throw new ArgumentNullException(nameof(list));
            StringBuilder sb = new StringBuilder(100);
            foreach(var e in list)
                sb.Append($"'{e}',");

            if(sb.Length > 0)
                sb.Remove(sb.Length - 1, 1);

            return sb.ToString();
        }

        public void DeleteOldOrders(TimeSpan timeThreshold)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                var orders = conn.Select<OrderStatus>("select top 100 * from OrderStatus where AllocationDate < @date",
                    DateTime.Now.Subtract(timeThreshold));

                foreach(var order in orders)
                    DeleteOrder(order);
            }
        }

        public List<AllocatedEpc> PeekEpcs(int orderId, int detailId, int max)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                return conn.Select<AllocatedEpc>($@"
                    select top {max} * 
                    from AllocatedEpc
                    where OrderID=@orderId and DetailID=@detailId
                    order by Epc", orderId, detailId);
            }
        }

        public List<AllocatedEpc> TakeAndDeleteEpcs(int orderId, int detailId, int qty)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                // DELETE con OUTPUT devuelve y elimina en un solo round‐trip
                return conn.Select<AllocatedEpc>(@"
                    DELETE TOP(@qty)
                    FROM   AllocatedEpc
                    OUTPUT DELETED.*
                    WHERE  (OrderID   = @orderId
                      AND  DetailID  = @detailId)",
                  qty, orderId, detailId);
            }
        }

        public int ExecuteNonQuery(string sql, params object[] args)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                return conn.ExecuteNonQuery(sql, args);
            }
        }


        public void BulkInsert(IEnumerable<AllocatedEpc> epcs)
        {
            // Construye un DataTable con la misma estructura de tu tabla AllocatedEpc
            var table = new DataTable();
            table.Columns.Add("Epc", typeof(string));
            table.Columns.Add("OrderID", typeof(int));
            table.Columns.Add("DetailID", typeof(int));
            table.Columns.Add("UserMemory", typeof(string));
            table.Columns.Add("AccessPassword", typeof(string));
            table.Columns.Add("KillPassword", typeof(string));

            foreach(var e in epcs)
            {
                table.Rows.Add(
                    e.Epc,
                    e.OrderID,
                    e.DetailID,
                    e.UserMemory,
                    e.AccessPassword,
                    e.KillPassword
                );
            }

            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                using(var bulk = new SqlBulkCopy(conn.ConnectionString))
                {
                    bulk.DestinationTableName = "AllocatedEpc";
                    bulk.ColumnMappings.Add("Epc", "Epc");
                    bulk.ColumnMappings.Add("OrderID", "OrderID");
                    bulk.ColumnMappings.Add("DetailID", "DetailID");
                    bulk.ColumnMappings.Add("UserMemory", "UserMemory");
                    bulk.ColumnMappings.Add("AccessPassword", "AccessPassword");
                    bulk.ColumnMappings.Add("KillPassword", "KillPassword");

                    bulk.WriteToServer(table);
                }
            }
        }

        public void SaveEpcs(IEnumerable<AllocatedEpc> epcs)
        {

            if(epcs.Count() < 200)
            {
                // INSERT individual, simples, ligeros
                using(var conn = connectionManager.OpenDB("TempeEpcService"))
                {
                    foreach(var e in epcs) conn.Insert(e);
                }
            }
            else
            {
                // Bulk sólo para volúmenes grandes
                BulkInsert(epcs);
            }
        }



        public bool AnyEpcs(int orderId, int detailId)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                var resultado = conn.ExecuteScalar(
                   "select count(1) from AllocatedEpc where OrderID = @orderId and DetailID = @detailId",
                   orderId, detailId);

                return Convert.ToInt32(resultado) > 0;
            }
        }

        public bool IsFirstTimePreencoding(int orderId, int detailId)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                var resultado = conn.ExecuteScalar(
                   "select Used from PreencodingOrderDetail where OrderID = @orderId and DetailID = @detailId",
                   orderId, detailId);

                return Convert.ToInt32(resultado) > 0;
            }
        }

        public bool IsFirstTime(int orderId, int detailId)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                var resultado = conn.ExecuteScalar(
                   "select Used from OrderDetail where OrderID = @orderId and DetailID = @detailId",
                   orderId, detailId);

                return Convert.ToInt32(resultado) > 0;
            }
        }

        public void MarkAsUsed(int orderId, int detailId)
        {

            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                var resultado = conn.ExecuteScalar(
                   "update OrderDetail set Used=1 where OrderID = @orderId and DetailID = @detailId",
                   orderId, detailId);
            }
        }

        public void MarkAsUsedPreencoding(int orderId, int detailId)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                var resultado = conn.ExecuteScalar(
                   "update PreencodingOrderDetail set Used=1 where OrderID = @orderId and DetailID = @detailId",
                   orderId, detailId);
            }
        }

        private int GetOrderEpcCount(int orderid)
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                return Convert.ToInt32(
                    conn.ExecuteScalar(@"
						select
							count(*)
						from
							AllocatedEpc
						where 
							OrderID = @orderid"
                        , orderid));
            }
        }


    }

    public class OrderStatus
    {
        [PK]
        public int OrderID { get; set; }
        public string OrderNumber { get; set; }
        public AllocationStatus AllocationStatus { get; set; }
        public DateTime AllocationDate { get; set; }
    }

    public enum AllocationStatus
    {
        Pending = 0,
        Validated = 1,
        Allocated = 2
    }

    public class OrderDetail
    {
        [PK]
        public int OrderID { get; set; }
        [PK]
        public int DetailID { get; set; }
        public int Quantity { get; set; }
        public int Model { get; set; }
        public int Quality { get; set; }
        public int Color { get; set; }
        public int Size { get; set; }
        public int TagType { get; set; }
        public int TagSubType { get; set; }
        public bool Allocated { get; set; }
        public int RfidRequest { get; set; }
        public bool Used { get; set; }
    }

    public class PreencodingOrderDetail
    {
        [PK]
        public int OrderID { get; set; }
        [PK]
        public int DetailID { get; set; }
        public int Quantity { get; set; }
        public int BrandId { get; set; }
        public int ProductTypeCode { get; set; }
        public int Color { get; set; }
        public int Size { get; set; }
        public int TagType { get; set; }
        public int TagSubType { get; set; }
        public bool Allocated { get; set; }
        public int RfidRequest { get; set; }
        public bool Used { get; set; }
    }

    public class LockInfo
    {
        [PK]
        public int OrderID { get; set; }
        public RFIDLockType EpcLock { get; set; }
        public RFIDLockType UserMemoryLock { get; set; }
        public RFIDLockType KillPasswordLock { get; set; }
        public RFIDLockType AccessPasswordLock { get; set; }
    }

    public class AllocatedEpc
    {
        [PK]
        public string Epc { get; set; }
        public int OrderID { get; set; }
        public int DetailID { get; set; }
        public string UserMemory { get; set; }
        public string AccessPassword { get; set; }
        public string KillPassword { get; set; }

    }
}
