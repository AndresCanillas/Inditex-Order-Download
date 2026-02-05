using Newtonsoft.Json.Linq;
using Service.Contracts.Documents;
using Service.Contracts.Misc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.Database
{
	partial class DynamicDB
	{
		private readonly char[] fieldSeparators = new char[] { '.', '@', '!' };

		public DataImportResult ImportData(int rootCatalogID, ImportMappings mappings, ImportedData data, Func<bool> keepAlive, Action<int> reportProgress)
		{
			InitializeMappings(mappings);
			var rootCatalog = GetCatalog(rootCatalogID);
			var rootOperation = new Node<ImportOp>(new ImportOp() { Catalog = rootCatalog, OpType = ImportOpType.Insert, Path = "" });
			CreateOperationGraph(rootOperation, mappings);
			data.CurrentRow = 0;
			if (mappings.Result.Errors.Count == 0)
			{
				using (var tx = conn.BeginTransaction())
				{
					ExecuteOperationGraph(rootOperation, mappings, data, keepAlive, reportProgress);
					tx.Commit();
				}
			}
			mappings.Result.Success = mappings.Result.Errors.Count == 0;
			return mappings.Result;
		}


		private void CreateOperationGraph(Node<ImportOp> node, ImportMappings mappings)
		{
			foreach (var mapping in mappings.Map)
			{
				if (String.IsNullOrEmpty(mapping.TargetField)) continue;
				if (!mapping.Visited && mapping.TargetField.StartsWith(node.NodeData.Path))
				{
					if (mapping.Level == node.Level)
					{
						// This mapping is at the same level and same path as the node we are processing right now, so mark it as visited and process it.
						mapping.Visited = true;
						var field = node.NodeData.Catalog.Fields.FirstOrDefault(f => String.Compare(f.Name, mapping.Field, true) == 0);
						if (field == null)
							mappings.Result.Errors.Add(new DataImportError($"Could not find a path to reach {mapping.TargetField}.", mapping, -1, null));
						else if (field.Type == ColumnType.Set || field.Type == ColumnType.Reference)
							mappings.Result.Errors.Add(new DataImportError($"Field {mapping.TargetField} is of type Set or Reference, therefore you cannot assign a value to this field directly, instead you have to drill down a path.", mapping, -1, null));
						else
						{

							if ((int)mapping.OpType > (int)node.NodeData.OpType)
								node.NodeData.OpType = mapping.OpType;
							if (mapping.OpType == ImportOpType.Lookup || mapping.OpType == ImportOpType.UpdateOrInsert)
								node.NodeData.Columns.Add(mapping.SourceField, mapping.Field, field.Type, true, mapping.IsFixedValue, mapping.TargetField);
							else
								node.NodeData.Columns.Add(mapping.SourceField, mapping.Field, field.Type, false, mapping.IsFixedValue, mapping.TargetField);
							if (mapping.IsHash)
							{
								node.NodeData.HashField = mapping.Field;
								node.NodeData.HashTable = new Dictionary<string, int>();
							}
						}
					}
					else if (mapping.Level == node.Level + 1)
					{
						// This mapping is in the path of the current node and is one level higher, therefore we need to drill down to another catalog...
						var field = node.NodeData.Catalog.Fields.FirstOrDefault(f => String.Compare(f.Name, mapping.CatalogName, true) == 0);
						if (field == null)
							mappings.Result.Errors.Add(new DataImportError($"Could not find a path to reach {mapping.TargetField}.", mapping, -1, null));
						else if (field.Type != ColumnType.Set && field.Type != ColumnType.Reference)
							mappings.Result.Errors.Add(new DataImportError($"Field {mapping.TargetField} is not a Set or Reference, cannot drill down this path.", mapping, -1, null));
						else
						{
							node.NodeData.Columns.Add(mapping.SourceField, mapping.CatalogName, field.Type, false, mapping.IsFixedValue, mapping.TargetField);
							var catalog = GetCatalog(field.CatalogID.Value);
							var childnode = new Node<ImportOp>(new ImportOp() { Catalog = catalog, OpType = ImportOpType.Insert, Path = mapping.Path });
							node.Children.Add(childnode);
							if (field.Type == ColumnType.Set)
							{
								childnode.NodeData.IsSet = true;
								childnode.NodeData.ClearSet = node.NodeData.OpType != ImportOpType.Insert;
							}
							CreateOperationGraph(childnode, mappings);
						}
					}
				}
			}
		}


		private void ExecuteOperationGraph(Node<ImportOp> node, ImportMappings mappings, ImportedData data, Func<bool> keepAlive, Action<int> reportProgress)
		{
			do
			{
				if (!keepAlive())
					throw new OperationCanceledException("Operation cancelled by the user.");
				foreach (var child in node.Children)
					ExecuteOperationGraph(child, mappings, data, keepAlive, reportProgress);

				GetColumnValues(node, data);
				if (node.NodeData.HashTable != null)
					ProcessRecordWithHashTable(node, mappings, data);
				else
					ProcessRecord(node, mappings, data);

				if (node.level == 0)
				{
					data.Cols.Add(new ImportedCol(null, "ID"));
					data.Rows[data.CurrentRow].SetValue("ID", node.NodeData.RecordID);
					data.CurrentRow++;
					int progress = (int)Math.Floor(((double)data.CurrentRow / data.Rows.Count) * 100) - 1;
					reportProgress(progress);
				}
				else
				{
					data.Cols.Add(new ImportedCol(null, $"{node.NodeData.Path}.ID"));
					data.Rows[data.CurrentRow].SetValue($"{node.NodeData.Path}.ID", node.NodeData.RecordID);
				}
			} while (data.Rows.Count > data.CurrentRow && node.level == 0);
		}


		private void ProcessRecord(Node<ImportOp> node, ImportMappings mappings, ImportedData data)
		{
			switch (node.NodeData.OpType)
			{
				case ImportOpType.Lookup:
					PerformLookupOp(node, mappings, data.CurrentRow);
					break;
				case ImportOpType.UpdateOrInsert:
					PerformUpdateOrInsertOp(node);
					break;
				default:
					var rowData = node.NodeData.Columns.Coalesce();
					node.NodeData.RecordID = Insert(node.NodeData.Catalog, rowData);
					break;
			}
			InsertRelRecords(node);
		}

		private void ProcessRecordWithHashTable(Node<ImportOp> node, ImportMappings mappings, ImportedData data)
		{
			string key = node.NodeData.Columns.GetValue(node.NodeData.HashField).ToString();
			if (node.NodeData.HashTable.TryGetValue(key, out var id))
			{
				node.NodeData.RecordID = id;
				InsertRelRecords(node);
			}
			else
			{
				ProcessRecord(node, mappings, data);
				node.NodeData.HashTable.Add(key, node.NodeData.RecordID.Value);
			}
		}

		private void InsertRelRecords(Node<ImportOp> node)
		{
			foreach (var column in node.NodeData.Columns)
			{
				if (column.ColumnType == ColumnType.Set)
				{
					var childNode = GetChildResult(node, column);
					var setField = node.NodeData.Catalog.Fields.FirstOrDefault(f => f.Name == column.ColumnName);
					var left = node.NodeData.Catalog;
					var right = childNode.NodeData.Catalog;

					if (node.NodeData.RecordID.HasValue && childNode.NodeData.ClearSet && !childNode.NodeData.ClearedSets.ContainsKey(node.NodeData.RecordID.Value))
					{
						childNode.NodeData.ClearedSets.Add(node.NodeData.RecordID.Value, 0);
						conn.ExecuteNonQuery($@"
							delete t from [{right.Name}_{right.ID}] t 
								join [REL_{left.ID}_{right.ID}_{setField.FieldID}] r
								on t.ID = r.TargetID
								where r.SourceID = @id", node.NodeData.RecordID);
						conn.ExecuteNonQuery($"delete from [REL_{left.ID}_{right.ID}_{setField.FieldID}] where SourceID = @source", node.NodeData.RecordID);
					}
					conn.ExecuteNonQuery($"insert into [REL_{left.ID}_{right.ID}_{setField.FieldID}] values(@source,@target)", node.NodeData.RecordID, (int)column.ColumnValue);
				}
			}
		}

		private void PerformLookupOp(Node<ImportOp> node, ImportMappings mappings, int rowNum)
		{
			var id = Lookup(node.NodeData.Catalog, node.NodeData.Columns);
			if (!id.HasValue || id.Value == 0)
			{
				mappings.Result.Errors.Add(new DataImportError($"Could not find record with given key: {node.NodeData.Catalog.Name} ({node.NodeData.Columns.GetFilterWitValues()})", null, rowNum, null));
				throw new DataImportLookupException($"Could not find record with given key: {node.NodeData.Catalog.Name} ({node.NodeData.Columns.GetFilterWitValues()})", node.NodeData.Catalog.Name, node.NodeData.Columns.GetFilterWitValues());
			}
			node.NodeData.RecordID = id;
		}

		private void PerformUpdateOrInsertOp(Node<ImportOp> node)
		{
			var rowData = node.NodeData.Columns.Coalesce();
			var id = Lookup(node.NodeData.Catalog, node.NodeData.Columns);
			if (!id.HasValue || id.Value == 0)
				id = Insert(node.NodeData.Catalog, rowData);
			else
				Update(node.NodeData.Catalog, id.Value, rowData);
			node.NodeData.RecordID = id;
		}

		private void GetColumnValues(Node<ImportOp> node, ImportedData data)
		{
			foreach (var column in node.NodeData.Columns)
			{
				if (column.ColumnType != ColumnType.Set && column.ColumnType != ColumnType.Reference)
				{
					if (String.IsNullOrWhiteSpace(column.SourceColumn) || column.IsFixedValue)
					{
						// TODO: cuando se procesan los campos fixed, se deberia borrar el nombre del sourcecolumn
						// asi como se modifican las funciones de las columnas ArticleCode/SentTo/BillTo de forma automatica
						// para asegurar que todos las campos fixed sean igualesy siempren tengan el campo sourcecolumn vacio
						if (column.IsFixedValue)
						{
							column.ColumnValue = data.Rows[data.CurrentRow].GetValue(column.TargetFieldPath);
							if (column.ColumnValue == null || String.IsNullOrWhiteSpace(column.ColumnValue.ToString()))
								column.ColumnValue = data.Rows[data.CurrentRow].GetValue(column.ColumnName);
							if (column.ColumnValue == null || String.IsNullOrWhiteSpace(column.ColumnValue.ToString()))
								column.ColumnValue = data.Rows[data.CurrentRow].GetValue(column.SourceColumn);
						}
						else
							column.ColumnValue = data.Rows[data.CurrentRow].GetValue(column.ColumnName);
					}
					else
						column.ColumnValue = data.Rows[data.CurrentRow].GetValue(column.SourceColumn);
				}
				else
					column.ColumnValue = GetChildResult(node, column).NodeData.RecordID;
			}
		}

		private Node<ImportOp> GetChildResult(Node<ImportOp> node, ColumnData column)
		{
			foreach (var child in node.Children)
			{
				if (child.NodeData.Path.EndsWith(column.ColumnName))
					return child;
			}
			return null;
		}

		private void InitializeMappings(ImportMappings mappings)
		{
			mappings.Result.Errors.Clear();
			mappings.Result.Success = false;
			foreach (var mapping in mappings.Map)
			{
				if (String.IsNullOrWhiteSpace(mapping.TargetField)) continue;
				InitMapping(mapping);
			}
		}

		private void InitMapping(ImportMapping mapping)
		{
			mapping.Visited = false;
			mapping.OpType = ImportOpType.NotSet;
			InitMappingLevel(mapping);
			InitMappingPath(mapping);
			if (mapping.OpType == ImportOpType.NotSet)
				mapping.OpType = GetImportOpType(mapping);
		}

		private void InitMappingLevel(ImportMapping mapping)
		{
			int level = 0;
			string fieldName = mapping.TargetField;
			if (fieldName == null)
				throw new Exception("Error in mapping: TargetField is empty");
			if (fieldName.IndexOfAny(fieldSeparators) == 0)
			{
				mapping.OpType = GetImportOpType(fieldName[0]);
				fieldName = mapping.TargetField = fieldName.Substring(1);
			}
			int idx = fieldName.IndexOfAny(fieldSeparators);
			while (idx > 0)
			{
				level++;
				idx = fieldName.IndexOfAny(fieldSeparators, idx + 1);
			}
			mapping.Level = level;
		}
        //Falta agregar excepciones.
		private void InitMappingPath(ImportMapping mapping)
		{
			int level = 0;
			StringBuilder sb = new StringBuilder();
			var tokens = mapping.TargetField.Split(fieldSeparators, StringSplitOptions.RemoveEmptyEntries);
			while (level < mapping.Level)
				sb.Append(tokens[level++]).Append('.');
			if (sb.Length > 0)
				sb.Remove(sb.Length - 1, 1);
			mapping.Path = sb.ToString();
			mapping.Field = tokens[level];
			if (level > 0)
				mapping.CatalogName = tokens[level - 1];
			else
				mapping.CatalogName = "";
			if (mapping.Field.StartsWith("#"))
			{
				mapping.Field = mapping.Field.Substring(1);
				mapping.IsHash = true;
			}
		}

		private ImportOpType GetImportOpType(ImportMapping mapping)
		{
			int idx = mapping.TargetField.LastIndexOfAny(fieldSeparators);
			if (idx < 0) return ImportOpType.Insert;
			char fieldSeparator = mapping.TargetField[idx];
			return GetImportOpType(fieldSeparator);
		}

		private ImportOpType GetImportOpType(char c)
		{
			switch (c)
			{
				case '@': return ImportOpType.UpdateOrInsert;
				case '!': return ImportOpType.Lookup;
				case '.':
				default: return ImportOpType.Insert;
			}
		}
	}

	public class ImportOp
	{
		public CatalogDefinition Catalog;
		public string Path;
		public ImportOpType OpType;
		public RowDataSpec Columns = new RowDataSpec();
		public int? RecordID;
		public string HashField;
		public Dictionary<string, int> HashTable;
		public bool IsSet;
		public bool ClearSet;
		public Dictionary<int, int> ClearedSets = new Dictionary<int, int>();
	}
	// TODO: where si '#' option - @#OrderNumber appear in some mappings
	public enum ImportOpType
	{
		NotSet,             // Not defined yet
		Insert,             // When drilling with "."
		UpdateOrInsert,     // When drilling with "@"
		Lookup              // When drilling with "!"
	}


	public class RowDataSpec : IEnumerable<ColumnData>
	{
		private List<ColumnData> list = new List<ColumnData>();


		public RowDataSpec() { }

		public RowDataSpec(List<ColumnData> data)
		{
			list = data;
		}

		public int Count => list.Count;
		public IEnumerator<ColumnData> GetEnumerator() => list.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();

		public ColumnData this[int index]
		{
			get => list[index];
		}

		public void Add(string sourceColumn, string columnName, ColumnType columnType, bool forLookup, bool isFixed, string targetFieldPath)
		{
			if (isFixed && list.FindIndex(c => c.TargetFieldPath == targetFieldPath) >= 0)
				return;
			ColumnData item = new ColumnData(sourceColumn, columnName, columnType, forLookup, isFixed, targetFieldPath);
			item.Index = list.Count;
			list.Add(item);
		}

		internal RowDataSpec Coalesce()
		{
			List<ColumnData> result = new List<ColumnData>(list);
			var rowData = new RowDataSpec(result);
			rowData.CoalesceInternal();
			return rowData;
		}


		private void CoalesceInternal()
		{
			var idx = 0;
			while (idx < list.Count)
			{
				var col = list[idx];
				var idx2 = list.FindIndex(idx + 1, p => String.Compare(p.ColumnName, col.ColumnName, true) == 0);
				while (idx2 > 0)
				{
					list[idx].Append(list[idx2].ColumnValue);
					list.RemoveAt(idx2);
					idx2 = list.FindIndex(idx2, p => String.Compare(p.ColumnName, col.ColumnName, true) == 0);
				}
				idx++;
			}
		}


		public bool HasValue
		{
			get
			{
				foreach (var col in list)
				{
					if (col.ColumnValue != null && !String.IsNullOrWhiteSpace(col.ColumnValue.ToString()))
						return true;
				}
				return false;
			}
		}

		public object GetValue(string columnName)
		{
			var col = list.Single(p => p.ColumnName == columnName);
			return col.ColumnValue;
		}

		public string GetFilter()
		{
			var sb = new StringBuilder(100);
			foreach (var col in list)
			{
				if (!col.ForLookup) continue;
				sb.Append($" [{col.ColumnName}] = @col{col.Index} and");
			}
			if (sb.Length > 0)
				sb.Remove(sb.Length - 3, 3);
			return sb.ToString();
		}

		public string GetFilterWitValues()
		{
			var sb = new StringBuilder(100);
			foreach (var col in list)
			{
				if (!col.ForLookup) continue;
				sb.Append($" [{col.ColumnName}] = '{col.ColumnValue}' and");
			}
			if (sb.Length > 0)
				sb.Remove(sb.Length - 3, 3);
			return sb.ToString();
		}

		public object[] GetFilterValues()
		{
			var result = new List<object>();
			foreach (var col in list)
			{
				if (!col.ForLookup) continue;
				result.Add(col.ColumnValue);
			}
			return result.ToArray();
		}

		public object[] GetValues()
		{
			var result = new List<object>();
			foreach (var col in list)
			{
				if (col.ForLookup) continue;
				if (col.ColumnType == ColumnType.Set) continue;
				result.Add(col.ColumnValue);
			}
			return result.ToArray();
		}

		public override string ToString()
		{
			var sb = new StringBuilder(100);
			foreach (var col in list)
			{
				sb.Append($"{col.ColumnName} = {col.ColumnValue}, ");
			}
			if (sb.Length > 0)
				sb.Remove(sb.Length - 2, 2);
			return sb.ToString();
		}

		internal JObject ToJsonObject()
		{
			JObject o = new JObject();
			foreach (var c in list)
			{
				if (c.ColumnValue != null)
					o[c.ColumnName] = JToken.FromObject(c.ColumnValue);
				else
					o[c.ColumnName] = null;
			}
			return o;
		}

		internal void FromJsonObject(JObject o)
		{
			foreach (var p in o.Properties())
			{
				var column = list.FirstOrDefault(c => c.ColumnName == p.Name);
				if (column != null)
				{
					switch (column.ColumnType)
					{
						case ColumnType.Bool:
							column.ColumnValue = o.GetValue<bool>(p.Name);
							break;
						case ColumnType.Int:
							column.ColumnValue = o.GetValue<int>(p.Name);
							break;
						case ColumnType.Long:
							column.ColumnValue = o.GetValue<long>(p.Name);
							break;
						case ColumnType.String:
							column.ColumnValue = o.GetValue<string>(p.Name);
							break;
						case ColumnType.Decimal:
							column.ColumnValue = o.GetValue<double>(p.Name);
							break;
						case ColumnType.Date:
							column.ColumnValue = o.GetValue<DateTime>(p.Name);
							break;
					}
				}
			}
		}
	}


	public class ColumnData
	{
		public ColumnData() { }

		public ColumnData(ColumnData src)
		{
			Index = src.Index;
			SourceColumn = src.SourceColumn;
			ColumnName = src.ColumnName;
			ColumnType = src.ColumnType;
			ColumnValue = src.ColumnValue;
			ForLookup = src.ForLookup;
			IsFixedValue = src.IsFixedValue;
			TargetFieldPath = src.TargetFieldPath;
		}

		public ColumnData(string sourceColumn, string columnName, ColumnType columnType, bool forLookup, bool isFixed, string targetFieldPath)
		{
			SourceColumn = sourceColumn;
			ColumnName = columnName;
			ColumnType = columnType;
			ForLookup = forLookup;
			IsFixedValue = isFixed;
			TargetFieldPath = targetFieldPath;
		}

		public void Append(object value)
		{
			if (ColumnType != ColumnType.String)
				throw new Exception("Concat operations are valid only when performed on fields of type 'String'.");
			if (ColumnValue == null)
				ColumnValue = "";
			if (value != null)
				ColumnValue = ColumnValue.ToString() + value.ToString();
		}

		public int Index;
		public string SourceColumn;
		public string ColumnName;
		public ColumnType ColumnType;
		public object ColumnValue;
		public bool ForLookup;
		public bool IsFixedValue;
		public string TargetFieldPath;
	}


	public class DataImportResult
	{
		public bool Success;
		public List<DataImportError> Errors = new List<DataImportError>();
	}

	public class DataImportError
	{
		public string ErrorMessage;
		public ImportMapping Mapping;
		public int RowNumber;
		public string StackTrace;

		public DataImportError(string errorMessage, ImportMapping mapping, int rowNumber = -1, string stackTrace = null)
		{
			this.ErrorMessage = errorMessage;
			this.Mapping = mapping;
			this.RowNumber = rowNumber;
			this.StackTrace = stackTrace;
		}
	}

	public class ImportMappings
	{
		private ImportMappingCollection map = new ImportMappingCollection();
		private DataImportResult result = new DataImportResult();

		public ImportMappings() { }

		public ImportMappings(List<DocumentColMapping> mappings)
		{
			foreach (var item in mappings)
			{
				if (!item.Ignore)
					Map.Add(new ImportMapping(item.InputColumn, item.TargetColumn) { IsFixedValue = item.IsFixedValue });
			}
		}

		public ImportMappingCollection Map { get => map; }
		public DataImportResult Result { get => result; }
	}

	public class ImportMappingCollection : IEnumerable<ImportMapping>
	{
		private List<ImportMapping> list = new List<ImportMapping>();

		public int Count => list.Count;
		public IEnumerator<ImportMapping> GetEnumerator() => list.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();

		public ImportMapping this[int index]
		{
			get => list[index];
		}

		public void Add(ImportMapping item)
		{
			item.Index = list.Count;
			list.Add(item);
		}
	}

	public class ImportMapping
	{
		public ImportMapping(string sourceField, string targetField)
		{
			SourceField = sourceField;
			TargetField = targetField;
		}

		public int Index { get; internal set; }
		public string SourceField { get; set; }
		public string TargetField { get; set; }
		public bool IsFixedValue { get; set; }
		//-------------------------------------//
		internal bool Visited { get; set; }
		internal int Level { get; set; }
		internal string Path { get; set; }
		internal string Field { get; set; }
		internal string CatalogName { get; set; }
		internal ImportOpType OpType { get; set; }
		internal bool IsHash { get; set; }
	}

	class DBSetInfo
	{
		public FieldDefinition Field;
		public JToken SetData;
		public DBSetInfo() { }
		public DBSetInfo(FieldDefinition field, JToken setData)
		{
			Field = field;
			SetData = setData;
		}
	}

	[Serializable]
	public class DataImportException : SystemException
	{
		public DataImportException() : base("") { }
		public DataImportException(string message) : base(message) { }
		public DataImportException(string message, Exception innerException) : base(message, innerException) { }
		public DataImportException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	[Serializable]
	public class DataImportLookupException : DataImportException
	{
		public string Catalog { get; set; }
		public string Columns { get; set; }
		public DataImportLookupException() : base("") { }
		public DataImportLookupException(string message) : base(message) { }
		public DataImportLookupException(string message, Exception innerException) : base(message, innerException) { }
		public DataImportLookupException(string message, string catalog, string columns) : base(message)
		{
			Catalog = catalog;
			Columns = columns;
		}
		public DataImportLookupException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			Catalog = info.GetString("Catalog");
			Columns = info.GetString("Columns");
		}

		[SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}

			info.AddValue("Catalog", Catalog);
			info.AddValue("Columns", Columns);
			base.GetObjectData(info, context);
		}
	}
}
