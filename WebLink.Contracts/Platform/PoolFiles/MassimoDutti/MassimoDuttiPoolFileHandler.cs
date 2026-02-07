using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Platform.PoolFiles.MassimoDutti
{
    public class MassimoDuttiPoolFileHandler : IPoolFileHandler
    {
        private readonly IConnectionManager manager;

        public MassimoDuttiPoolFileHandler(IConnectionManager manager)
        {
            this.manager = manager;
        }

        public async Task UploadAsync(IProject project, Stream stream, Action<int, IList<IOrderPool>> result = null)
        {
            string connStr;
            using(var conn = manager.OpenDB("MainDB"))
            {
                connStr = conn.ConnectionString;
                conn.ExecuteNonQuery("DELETE FROM OrderPools WHERE ProjectID = @projectid", project.ID);
            }

            using(var bulkCopy = new SqlBulkCopy(connStr, SqlBulkCopyOptions.TableLock))
            {
                using(var reader = new MassimoDuttiPoolFileDataReader(project.ID, stream))
                {
                    bulkCopy.DestinationTableName = "OrderPools";
                    bulkCopy.EnableStreaming = true;
                    bulkCopy.BulkCopyTimeout = 0;
                    bulkCopy.BatchSize = 10000;
                    await bulkCopy.WriteToServerAsync(reader);
                }
            }
            UpdateSizeSet(project);
            UpdateSizeXX(project);
            UpdatePantTejano(project);
            UpdateNullSection(project);
            UpdateMadeIn(project);
            UpdateSunGlasses(project);

            // TODO: pending to implement, the bulk insertion process not return orders inserted, 
            // for massimo dutty the file received is very large, only report a email with message "new file is recevied"
            //if (result != null)
            //    result(project.ID, new List<IOrderPool>());

        }

        private void UpdateSunGlasses(IProject project)
        {
            try
            {
                using(var conn = manager.OpenDB("MainDB"))
                {
                    conn.ExecuteNonQuery("UPDATE OrderPools SET CategoryText2='C: GAFAS DE SOL'  WHERE ProjectID = @projectid AND CategoryText2 LIKE '%C: GAFAS DE SOL, TIRANTES, RELOJES, BISU%' AND CategoryText3 LIKE '%GAFAS DE SOL%'", project.ID);
                    conn.ExecuteNonQuery("UPDATE OrderPools SET CategoryText2='S: GAFAS DE SOL'  WHERE ProjectID = @projectid AND CategoryText2 LIKE '%BISUTERIA, GAFAS DE SOL, TIRANTES, RELOJ%' AND CategoryText3 LIKE '%GAFAS DE SOL%'", project.ID);
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }

        }

        private void UpdateMadeIn(IProject project)
        {
            string wrongValue = "MAINLAND CHINA";
            string rightValue = "CHINA";
            using(var conn = manager.OpenDB("MainDB"))
            {
                conn.ExecuteNonQuery("UPDATE OrderPools SET CategoryText5=@rightValue  WHERE ProjectID = @projectid AND CategoryText5 = @wrongValue", rightValue, project.ID, wrongValue);
            }
        }

        private void UpdateSizeSet(IProject project)
        {

            string wrongValue = "C: CORBATAS, PAJARITAS, BUFANDAS, PAÑUEL";
            string rightValue = "C: CORBATAS, PAJARITAS, BUFANDAS, PAÑUELOS, FULARES, TOALLAS, PAREO";
            string wrongValueSunglasses = "C: GAFAS DE SOL, TIRANTES, RELOJES, BISU";
            string rightValueSunglasses = "C: GAFAS DE SOL, TIRANTES, RELOJES, BISUTERÍA";
            using(var conn = manager.OpenDB("MainDB"))
            {
                conn.ExecuteNonQuery("UPDATE OrderPools SET CategoryText2=@rightValue  WHERE ProjectID = @projectid AND CategoryText2 = @wrongValue", rightValue, project.ID, wrongValue);
                conn.ExecuteNonQuery("UPDATE OrderPools SET CategoryText2=@rightValueSunglasses  WHERE ProjectID = @projectid AND CategoryText2 = @wrongValueSunglasses", rightValueSunglasses, project.ID, wrongValueSunglasses);
            }


        }

        private void UpdateNullSection(IProject project)
        {
            var sql = "UPDATE OrderPools SET CategoryText1 = CASE   WHEN categorytext2 IS NOT NULL AND LEFT(categorytext2, 1) = 'C' THEN 'Caballero'  WHEN categorytext2 IS NOT NULL AND LEFT(categorytext2, 1) = 'S' THEN 'Señora'  ELSE NULL  END WHERE categorytext1 IS NULL";
            using(var conn = manager.OpenDB("MainDB"))
            {
                conn.ExecuteNonQuery(sql);
            }
        }

        private void UpdatePantTejano(IProject project)
        {
            string wrongValue = "PANT. TEJANO";
            string rightValue = "C: TROUSERS USA";
            using(var conn = manager.OpenDB("MainDB"))
            {
                conn.ExecuteNonQuery("UPDATE OrderPools SET CategoryText2=@rightValue  WHERE ProjectID = @projectid AND CategoryText4 = @wrongValue", rightValue, project.ID, wrongValue);
            }
        }

        private void UpdateSizeXX(IProject project)
        {

            string wrongValue = "XX";
            string rightValue = "XXL";
            using(var conn = manager.OpenDB("MainDB"))
            {
                conn.ExecuteNonQuery("UPDATE OrderPools SET Size=@rightValue  WHERE ProjectID = @projectid AND Size = @wrongValue", rightValue, project.ID, wrongValue);
            }
        }

        public Task InsertListAsync(IProject project, List<OrderPool> orderPools, Action<int, IList<IOrderPool>> result = null)
        {
            throw new NotImplementedException();
        }
    }
}
