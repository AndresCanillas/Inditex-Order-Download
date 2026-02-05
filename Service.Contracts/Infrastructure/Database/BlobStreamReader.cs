using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts.Database
{
    public class BlobStreamReader : Stream
    {
		private IDBConfiguration db;
		private IDBX conn;
		private DbDataReader reader;
        private long length;
        private long pos;

        public BlobStreamReader(IDBConfiguration db)
        {
			this.db = db;
        }

		public async Task<bool> OpenBlobAsync<T>(string blobFieldName, params object[] pkValues) where T : class
		{
			conn = await db.CreateConnectionAsync();
			reader = await conn.ReadBlobAsync<T>(blobFieldName, pkValues);
			if (reader != null)
			{
				var lenObj = reader[0];
				if (lenObj is DBNull)
					length = 0;
				else
					length = Convert.ToInt64(lenObj);
				return true;
			}
			else return false;
		}

		protected override void Dispose(bool disposing)
        {
			if (disposing)
			{
				if(reader != null)
					reader.Dispose();
				conn.Dispose();
			}

            base.Dispose(disposing);
        }

        public override bool CanRead { get => true; }

        public override bool CanSeek { get => false; }

        public override bool CanWrite { get => false; }

        public override long Length { get => length; }

        public override long Position
        {
            get => pos;
            set { throw new InvalidOperationException(); }
        }


        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException();
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
			if (length == 0)
				return 0;
			long readBytes = reader.GetBytes(1, pos, buffer, offset, count);
            pos += readBytes;
            return (int)readBytes;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
			if (length == 0)
				return Task.FromResult(0);
            long readBytes = reader.GetBytes(1, pos, buffer, offset, count);
            pos += readBytes;
            return Task.FromResult((int)readBytes);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }
    }
}
