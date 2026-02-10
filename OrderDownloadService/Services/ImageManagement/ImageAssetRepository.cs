using System;
using System.Data.Common;
using System.Threading.Tasks;
using Service.Contracts.Database;
using Service.Contracts.OrderImages;

namespace OrderDonwLoadService.Services.ImageManagement
{
    public class ImageAssetRepository : IImageAssetRepository
    {
        private readonly IConnectionManager db;

        public ImageAssetRepository(IConnectionManager db)
        {
            this.db = db;
        }

        public Task<ImageAssetRecord> GetLatestByUrlAsync(string url)
        {
            return Task.FromResult(GetLatestByUrl(url));
        }

        public ImageAssetRecord GetLatestByUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Url cannot be null or empty.", nameof(url));

            using (var conn = db.OpenDB("LocalDB"))
            using (var reader = conn.ExecuteReader(@"
                SELECT TOP 1
                    ID, Name, Url, Hash, ContentType, Content, Status, IsLatest, CreatedDate, UpdatedDate
                FROM ImageAssets
                WHERE Url = @Url AND IsLatest = 1
                ORDER BY ID DESC", url))
            {
                if (!reader.Read())
                    return null;

                return Map(reader);
            }
        }

        public Task<int> InsertAsync(ImageAssetRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            using (var conn = db.OpenDB("LocalDB"))
            {
                var id = conn.ExecuteScalar(@"
                    INSERT INTO ImageAssets
                        (Name, Url, Hash, ContentType, Content, Status, IsLatest, CreatedDate, UpdatedDate)
                    VALUES
                        (@Name, @Url, @Hash, @ContentType, @Content, @Status, @IsLatest, @CreatedDate, @UpdatedDate);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    record.Name,
                    record.Url,
                    record.Hash,
                    record.ContentType,
                    record.Content,
                    (int)record.Status,
                    record.IsLatest,
                    record.CreatedDate,
                    record.UpdatedDate);

                return Task.FromResult(Convert.ToInt32(id));
            }
        }

        public Task MarkObsoleteAsync(int id)
        {
            using (var conn = db.OpenDB("LocalDB"))
            {
                conn.ExecuteNonQuery(@"
                    UPDATE ImageAssets
                    SET Status = @Status,
                        IsLatest = 0,
                        UpdatedDate = @UpdatedDate
                    WHERE ID = @ID",
                    (int)ImageAssetStatus.Obsolete,
                    DateTime.UtcNow,
                    id);
            }
            return Task.CompletedTask;
        }

        private static ImageAssetRecord Map(DbDataReader reader)
        {
            return new ImageAssetRecord
            {
                ID = reader.GetInt32(reader.GetOrdinal("ID")),
                Name = reader["Name"] as string,
                Url = reader["Url"] as string,
                Hash = reader["Hash"] as string,
                ContentType = reader["ContentType"] as string,
                Content = reader["Content"] == DBNull.Value ? null : (byte[])reader["Content"],
                Status = (ImageAssetStatus)Convert.ToInt32(reader["Status"]),
                IsLatest = Convert.ToBoolean(reader["IsLatest"]),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                UpdatedDate = Convert.ToDateTime(reader["UpdatedDate"])
            };
        }
    }
}
