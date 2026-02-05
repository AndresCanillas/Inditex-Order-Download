using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Service.Contracts
{
	public static class TableDataExtensions
	{
		public static JObject FlattenObject(this List<TableData> tables, string rootCatalogName, int rootObjectID = 0)
		{
			StringBuilder sb = new StringBuilder(10000);
			var result = new JObject();
			var targetCatalog = tables.FirstOrDefault(c => String.Compare(c.Name, rootCatalogName, true) == 0);
			if (targetCatalog == null)
				throw new InvalidOperationException($"Catalog {rootCatalogName} is not present in the collection.");
			if(String.IsNullOrWhiteSpace(targetCatalog.Records))
				throw new InvalidOperationException($"Catalog {rootCatalogName} does not have any record information.");
			var rows = JArray.Parse(targetCatalog.Records);
			if(rows.Count == 0)
				throw new InvalidOperationException($"Catalog {rootCatalogName} does not have any record information.");

			JObject targetRow;
			if(rootObjectID != 0)
				targetRow = rows.First(p => (p as JObject).GetValue<int>("ID") == rootObjectID) as JObject;
			else
				targetRow = rows[0] as JObject;

			AttachProperties(result, "", tables, targetCatalog, targetRow);
			return result;
		}

		private static void AttachProperties(JObject result, string path, List<TableData> tables, TableData targetCatalog, JObject targetRow)
		{
			var fields = JsonConvert.DeserializeObject<List<FieldDefinition>>(targetCatalog.Fields);
			foreach (var f in fields)
			{
				if (f.Type != ColumnType.Set && f.Type != ColumnType.Reference)
				{
					result[path + f.Name] = targetRow[f.Name];
				}
			}
			foreach (var f in fields)
			{
				if (f.Type == ColumnType.Reference)
				{
					var rowID = targetRow[f.Name];
					if(rowID != null && rowID.Type != JTokenType.Null)
					{
						int id = rowID.Value<int>();
						string subPath = path + f.Name + ".";
						var referencedCatalog = tables.FirstOrDefault(c => c.CatalogID == f.CatalogID);
						if (referencedCatalog == null)
							throw new InvalidOperationException($"Catalog referenced by field {f.Name} is not present in the collection.");
						var rows = JArray.Parse(referencedCatalog.Records);
						var referencedRow = rows.First(p => (p as JObject).GetValue<int>("ID") == id) as JObject;
						AttachProperties(result, subPath, tables, referencedCatalog, referencedRow);
					}
				}
			}
		}

		public static void AddProperties(this JObject result, string path, TableData table, int rowID = 0)
		{
			if (table == null)
				throw new ArgumentNullException(nameof(table));
			if (String.IsNullOrWhiteSpace(table.Records))
				throw new InvalidOperationException($"Catalog {table.Name} does not have any record information.");

			var rows = JArray.Parse(table.Records);
			if (rows.Count == 0)
				throw new InvalidOperationException($"Catalog {table.Name} does not have any record information.");

			JObject targetRow;
			if (rowID != 0)
				targetRow = rows.First(p => (p as JObject).GetValue<int>("ID") == rowID) as JObject;
			else
				targetRow = rows[0] as JObject;

			var fields = JsonConvert.DeserializeObject<List<FieldDefinition>>(table.Fields);
			foreach (var f in fields)
			{
				if (f.Type != ColumnType.Set && f.Type != ColumnType.Reference)
				{
					result[path + f.Name] = targetRow[f.Name];
				}
			}
		}

		public static List<TableData> ExportData(this List<TableData> tables, string rootCatalogName, int rootObjectID)
		{
			var result = new List<TableData>();
			var targetCatalog = tables.FirstOrDefault(c => String.Compare(c.Name, rootCatalogName, true) == 0);
			if (targetCatalog == null)
				throw new InvalidOperationException($"Catalog {rootCatalogName} is not present in the collection.");
			var rows = JArray.Parse(targetCatalog.Records);
			var targetRow = rows.First(p => (p as JObject).GetValue<int>("ID") == rootObjectID) as JObject;
			AppendTable(result, tables, targetCatalog, targetRow);
			return result;
		}

		private static void AppendTable(List<TableData> result, List<TableData> tables, TableData target, JObject targetRow)
		{
			TableData t = new TableData();
			t.CatalogID = target.CatalogID;
			t.Name = target.Name;
			t.CatalogType = target.CatalogType;
			t.Fields = target.Fields;
			t.Records = $"[{targetRow.ToString()}]";
			result.Add(t);

			var fields = JsonConvert.DeserializeObject<List<FieldDefinition>>(t.Fields);
			foreach(var f in fields)
			{
				if(f.Type == ColumnType.Reference)
				{
					var childTable = tables.FirstOrDefault(p => p.CatalogID == f.CatalogID);
					if (childTable == null)
						continue;
					var rows = JArray.Parse(childTable.Records);
					var childID = targetRow.GetValue<int>(f.Name);
					var childRow = rows.First(p => (p as JObject).GetValue<int>("ID") == childID) as JObject;
					AppendTable(result, tables, childTable, childRow);
				}
			}
		}
	}
}
