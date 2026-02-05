using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Database;
using Services.Core;

namespace Service.Contracts
{
	public class HerculesEncode : IHerculesEncode
	{
        public static ILogService log;
        private IConnectionManager connManager;

        public HerculesEncode(IFactory Factory)
        {
            log = Factory.GetInstance<ILogService>();
            connManager = Factory.GetInstance<IConnectionManager>();
        }

        private int InsertData(IDBX conn, string tableName, JObject data, string child)
        {
            List<object> args = new List<object>();
            var ins = new StringBuilder(1000);
            var vals = new StringBuilder(1000);
            ins.Append($"insert into `{tableName}` (");
            vals.Append(" values(");
            foreach (var prop in data.Properties())
            {
                if (String.Compare(prop.Name, child, true) != 0)
                {
                    ins.Append($"{prop.Name},");
                    vals.Append($"@{prop.Name.ToLower()},");
                    var ptype = GetPropertyType(prop.Value.Type);
                    if (ptype == null)
                        args.Add(null);
                    else
                        args.Add(prop.Value.ToObject(ptype));
                }
            }
            ins.Remove(ins.Length - 1, 1);
            vals.Remove(vals.Length - 1, 1);
            ins.Append(")");
            vals.Append("); ");
            return Convert.ToInt32(conn.ExecuteScalar($"{ins.ToString()}{vals.ToString()} SELECT LAST_INSERT_ID() as id;", args.ToArray()));
        }

        private Type GetPropertyType(JTokenType type)
        {
            switch (type)
            {
                case JTokenType.Boolean: return typeof(bool);
                case JTokenType.Date: return typeof(DateTime);
                case JTokenType.Float: return typeof(float);
                case JTokenType.Integer: return typeof(int);
                case JTokenType.Null: return null;
                default: return typeof(string);
            }
        }

        public int AddPrint(IDBX conn, Print print)
        {
            var printId = GetPrintId(conn, print.OrderMD);

            if (printId == 0)
            {
                var data = (JObject)JToken.FromObject(print);
                return InsertData(conn, "print", data, "PrintHeader");
            }

            return 0;
        }

        public int AddRFIDHeader(IDBX conn, PrintHeader header)
        {
            var data = (JObject)JToken.FromObject(header);
            return InsertData(conn, "rfidheader", data, "HeaderDetail");
        }

        public int AddRFIDDetail(IDBX conn, PrintHeaderDetail detail)
        {
            var data = (JObject)JToken.FromObject(detail);
            return InsertData(conn, "rfiddetail", data, "");
        }

        //public int GetNextId()
        //{
        //    using (IDBX conn = connManager.OpenDB("MainDB"))
        //    {
        //        var jobId = conn.ExecuteScalar($@"
        //            select top 1 [Current] from HerculesJobIdRange");

        //        return jobId == null ? 0 : (int)jobId;
        //    }
        //}

        //public int GetRFIDHeaderId(IDBX conn, int idPrint, string barcode)
        //{
        //    return Convert.ToInt32(conn.ExecuteScalar($@"
        //        select idRfidHeader from `rfidheader`
        //        where idPrint = @idPrint and barcode = @barcode
        //        limit 1", idPrint, barcode));
        //}

        public void DeletePrintData(IDBX conn, int printId)
        {
            conn.ExecuteScalar($@"
                DELETE p, rh, rd 
                FROM `print` p 
                JOIN `rfidheader` rh 
                    ON rh.idPrint = p.idPrint
                JOIN `rfiddetail` rd 
                    ON rd.idRfidHeader = rd.idRfidHeader 
                WHERE p.idPrint = @printId;
                ", printId);
        }

        public int GetPrintId(IDBX conn, string order)
        {
            return Convert.ToInt32(conn.ExecuteScalar($@"
                select idPrint from `print`
                where orderMD = @order
                limit 1", order));
        }

        public Dictionary<int, string> GetBarcodeHeader(IDBX conn, int printId)
        {
            var reader = conn.ExecuteReader($@"
                select idRfidHeader, barcode from `rfidheader`
                where idPrint = @idPrint
                order by idRfidHeader", printId);

            var data = new Dictionary<int, string>();

            while (reader.Read())
            {
                data.Add(Convert.ToInt32(reader["idRfidHeader"]), reader["barcode"].ToString());
            }

            return data;
        }
    }
}
