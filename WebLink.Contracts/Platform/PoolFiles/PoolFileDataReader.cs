using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Platform.PoolFiles.MassimoDutti
{
    public abstract class PoolFileDataReader<T> : IDataReader where T : class, new()
	{
		private readonly int projectid;
		private readonly StreamReader stream;
		private readonly List<PropertyInfo> fields;

		public PoolFileDataReader(int projectid, Stream stream)
		{
			this.stream = new StreamReader(stream);
			Parser = new DelimitedColumnParser();

            var excludedFields = new HashSet<string> { "Project", "ProcessedBy", "ProcessedDate", "DeletedDate", "DeletedBy" };

            fields = typeof(OrderPool)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => !excludedFields.Contains(p.Name))
                .ToList();

    //        fields = typeof(OrderPool)
				//.GetProperties(BindingFlags.Instance | BindingFlags.Public)
				//.Where(p=>p.Name != "Project")
				//.ToList();

			// Skip headers
			this.stream.ReadLine();
		}

		protected DelimitedColumnParser Parser { get; }

		protected T Values { get; set; }

		protected Dictionary<int, Func<object>> ValueMapping { get; set; }

		public int Depth { get; }
		public bool IsClosed { get; }
		public int RecordsAffected { get; }
		public int FieldCount { get => fields.Count; }
		public object this[int i] => GetValue(i);

		public void Dispose()
		{
			stream.Dispose();
		}

		public void Close()
		{
			stream.Close();
		}

		public bool Read()
		{
			if(!stream.EndOfStream)
			{
				var line = stream.ReadLine();
				Values = Parser.Bind<T>(line);
				return true;
			}
			return false;
		}

		public object GetValue(int i) => ValueMapping[i]();

		public bool IsDBNull(int i) => ValueMapping[i]() == null;


		#region NotUsedByBulkCopy - NOTE: Does not need implementation as long as we don't break use pattern

		public object this[string name] => throw new NotImplementedException();
		public bool NextResult() => throw new NotImplementedException();
		public DataTable GetSchemaTable() => throw new NotImplementedException();
		public string GetName(int i) => throw new NotImplementedException();
		public int GetOrdinal(string name) => throw new NotImplementedException();
		public string GetString(int i) => throw new NotImplementedException();
		public bool GetBoolean(int i) => throw new NotImplementedException();
		public byte GetByte(int i) => throw new NotImplementedException();
		public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) => throw new NotImplementedException();
		public char GetChar(int i) => throw new NotImplementedException();
		public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) => throw new NotImplementedException();
		public IDataReader GetData(int i) => throw new NotImplementedException();
		public string GetDataTypeName(int i) => throw new NotImplementedException();
		public DateTime GetDateTime(int i) => throw new NotImplementedException();
		public decimal GetDecimal(int i) => throw new NotImplementedException();
		public double GetDouble(int i) => throw new NotImplementedException();
		public Type GetFieldType(int i) => throw new NotImplementedException();
		public float GetFloat(int i) => throw new NotImplementedException();
		public Guid GetGuid(int i) => throw new NotImplementedException();
		public short GetInt16(int i) => throw new NotImplementedException();
		public int GetInt32(int i) => throw new NotImplementedException();
		public long GetInt64(int i) => throw new NotImplementedException();
		public int GetValues(object[] values) => throw new NotImplementedException();

		#endregion
	}
}
