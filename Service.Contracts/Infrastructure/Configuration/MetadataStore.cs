using Newtonsoft.Json;
using Service.Contracts.Database;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Service.Contracts
{
    public class MetadataStore : IMetadataStore
    {
        private readonly ConfigJSONConverter jsonConverter;
        private readonly IConnectionManager connManager;


        public MetadataStore(IConnectionManager connManager, ConfigJSONConverter jsonConverter)
        {
            this.connManager = connManager;
            this.jsonConverter = jsonConverter;

            if(connManager.GetDBConfiguration("MainDB") == null)
                return;

            using(var conn = connManager.OpenDB("MainDB"))
            {
                conn.ExecuteNonQuery(@"
                    if object_id('ConfigSystemMetadata') is null
                    begin
                        create table ConfigSystemMetadata(
	                        [Path] varchar(50) NOT NULL,
	                        [Metadata] nvarchar(max) NOT NULL,
	                        constraint [PK_ConfigSystemMetadata] primary key clustered
	                        (
		                        [Path] asc
	                        )
                        )
                    end");
            }
        }

        public void SetMeta<T>(string path, T meta)
            where T : class, new()
        {
            if(connManager.GetDBConfiguration("MainDB") == null)
                return;

            var metadata = JsonConvert.SerializeObject(meta);

            InsertOrUpdateMetadata(path, metadata);
        }

        public void SetDefaultConfiguration<T>(string path, T meta)
            where T : class, new()
        {
            if(connManager.GetDBConfiguration("MainDB") == null)
                return;

            var metadata = JsonConvert.SerializeObject(meta, jsonConverter.Settings);

            InsertOrUpdateMetadata(path, metadata);
        }


        private void InsertOrUpdateMetadata(string path, string metadata)
        {
            using(var conn = connManager.OpenDB("MainDB"))
            {
                conn.ExecuteNonQuery(@"
                    merge into ConfigSystemMetadata as target
                    using (select @path as Path, @metadata as Metadata) as source
                    on target.Path = source.Path
                    when matched then
                        Update set target.Metadata = source.Metadata
                    when not matched then
                        insert (Path, Metadata) values (source.Path, source.Metadata);
                ", path, metadata);
            }
        }

        public bool TryGetValue(string path, out string result)
        {
            result = null;

            if(connManager.GetDBConfiguration("MainDB") == null)
                return false;

            using(var conn = connManager.OpenDB("MainDB"))
            {
                result = conn.SelectColumn<string>("select Metadata from ConfigSystemMetadata where Path = @path", path).FirstOrDefault();
                return result != null;
            }
        }

        public T GetMeta<T>(string path)
            where T : class, new()
        {
            if(connManager.GetDBConfiguration("MainDB") == null)
                return default;

            using(var conn = connManager.OpenDB("MainDB"))
            {
                var metadata = conn.SelectColumn<string>("select Metadata from ConfigSystemMetadata where Path = @path", path).FirstOrDefault();
                if(metadata != null)
                {
                    return JsonConvert.DeserializeObject<T>(metadata);
                }
                else return null;
            }
        }
    }
}

