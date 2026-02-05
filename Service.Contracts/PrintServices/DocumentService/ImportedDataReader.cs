//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Service.Contracts.Documents
//{
//	public class eraseandrewind : IEnumerable<ImportedRowReader>
//	{
//		internal ImportedColumnCollection columns;
//		internal ImportedRowCollection rows;
//		internal ImportedData data;

//		public eraseandrewind(ImportedData data)
//		{
//			this.data = data;
//			columns = new ImportedColumnCollection(this);
//			rows = new ImportedRowCollection(this);
//		}


//		public ImportedData Data { get => data; }


//		/// <summary>
//		/// Determines the active row index. Some methods affect the active row by default.
//		/// </summary>
//		public int CurrentRow { get; set; } = 0;


//		/// <summary>
//		/// Returns the number of available rows
//		/// </summary>
//		public int RowCount { get => rows.Count; }


//		/// <summary>
//		/// Returns the number of available columns
//		/// </summary>
//		public int ColumnCount { get => columns.Count; }


//		/// <summary>
//		/// Checks to see if the document defines the specified column name
//		/// </summary>
//		public bool HasColumn(string columnName)
//		{
//			var col = columns.GetColumnByName(columnName, false);
//			return col != null;
//		}


//		public ImportedColumnReader GetColumn(string columnName)
//		{
//			return columns.GetColumnByName(columnName);
//		}


//		public ImportedColumnReader GetColumn(int columnIndex)
//		{
//			return columns[columnIndex];
//		}


//		public void AddColumn(string columnName)
//		{
//			columns.AddColumn(columnName);
//		}


//		public void AddColumn(ImportedCol column)
//		{
//			columns.AddColumn(column);
//		}


//		public void InsertColumn(int columnIndex, string columnName)
//		{
//			columns.InsertColumn(columnIndex, columnName);
//		}


//		public void InsertColumn(int columnIndex, ImportedCol column)
//		{
//			columns.InsertColumn(columnIndex, column);
//		}


//		public void RemoveColumn(string columnName)
//		{
//			columns.RemoveColumn(columnName);
//		}


//		public void RemoveColumn(ImportedCol column)
//		{
//			columns.RemoveColumn(column);
//		}


//		/// <summary>
//		/// Gets or sets the value of the specified column from the currently active row. See CurrentRow property to determine which row is active.
//		/// </summary>
//		public object this[string columnName]
//		{
//			get => GetValue(columnName);
//			set => SetValue(columnName, value);
//		}


//		/// <summary>
//		/// Gets or sets the value on the specified row index and column name.
//		/// </summary>
//		public object this[int rowIndex, string columnName]
//		{
//			get => GetValue(rowIndex, columnName);
//			set => SetValue(rowIndex, columnName, value);
//		}


//		/// <summary>
//		/// Gets or sets the value of the specified row and column by index.
//		/// </summary>
//		public object this[int rowIndex, int columnIndex]
//		{
//			get => GetValue(rowIndex, columnIndex);
//			set => SetValue(rowIndex, columnIndex, value);
//		}


//		/// <summary>
//		/// Gets the value of the specified column from the currently active row. See CurrentRow property to determine which row is active.
//		/// </summary>
//		/// <param name="columnName">Name of the column to retrieve</param>
//		public object GetValue(string columnName)
//		{
//			if (rows.Count == 0)
//				throw new InvalidOperationException("Data set is empty");

//			var col = columns.GetColumnByName(columnName);
//			return rows[CurrentRow].GetValue(col.Index);
//		}

//		/// <summary>
//		/// Gets the value of the specified column from the given row index.
//		/// </summary>
//		/// <param name="rowIndex">The index of the row to access</param>
//		/// <param name="columnName">The name of the column to access</param>
//		public object GetValue(int rowIndex, string columnName)
//		{
//			var col = columns.GetColumnByName(columnName);
//			return rows[rowIndex].GetValue(col.Index);
//		}


//		/// <summary>
//		/// Gets the value of the specified row and column by index.
//		/// </summary>
//		/// <param name="rowIndex">The index of the row to access</param>
//		/// <param name="columnIndex">The index of the column to access</param>
//		public object GetValue(int rowIndex, int columnIndex)
//		{
//			return rows[rowIndex].GetValue(columnIndex);
//		}


//		/// <summary>
//		/// Sets the value of the specified column from the currently active row. See CurrentRow property to determine which row is active.
//		/// </summary>
//		/// <param name="columnName">The name of the column to access</param>
//		/// <param name="value">The value to be stored</param>
//		public void SetValue(string columnName, object value)
//		{
//			if (rows.Count == 0)
//				throw new InvalidOperationException("Data set is empty");

//			var col = columns.GetColumnByName(columnName);
//			rows[CurrentRow].SetValue(col.Index, value);
//		}


//		/// <summary>
//		/// Sets the value of the specified column from the specified row index.
//		/// </summary>
//		/// <param name="rowIndex">The index of the row to access</param>
//		/// <param name="columnName">The name of the column to access</param>
//		/// <param name="value">The value to be stored</param>
//		public void SetValue(int rowIndex, string columnName, object value)
//		{
//			var col = columns.GetColumnByName(columnName);
//			rows[rowIndex].SetValue(col.Index, value);
//		}


//		/// <summary>
//		/// Sets the value of the specified column from the specified row and column index.
//		/// </summary>
//		/// <param name="rowIndex">The index of the row to access</param>
//		/// <param name="columnIndex">The index of the column to access</param>
//		/// <param name="value">The value to be stored</param>
//		public void SetValue(int rowIndex, int columnIndex, object value)
//		{
//			rows[rowIndex].SetValue(columnIndex, value);
//		}


//		// NOTE: This makes no sense, only one row will have the specified ID, use GetRecordValue instead.
//		///// <summary>
//		///// Gets the sum of the specified column, filtering records by ID
//		///// </summary>
//		///// <param name="id"></param>
//		///// <param name="columnName"></param>
//		///// <returns></returns>
//		//public int Sum(int id, string columnName)
//		//{
//		//	CheckLinkedState();
//		//	int sum = 0;
//		//	var idColumn = Cols.GetColumnByName("ID");
//		//	var col = Cols.GetColumnByName(columnName);
//		//	foreach (var row in Rows)
//		//	{
//		//		var rowid = Convert.ToInt32(row.GetValue(idColumn.Index));
//		//		if (id == rowid)
//		//		{
//		//          int value = Convert.ToInt32(row.GetValue(col.Index));
//		//			sum += value;
//		//		}
//		//	}
//		//	return sum;
//		//}


//		/// <summary>
//		/// Calculates the sum of the specified column across the entire data set.
//		/// </summary>
//		/// <param name="columnName">The name of the column to access</param>
//		public int Sum(string columnName)
//		{
//			int sum = 0;
//			var col = columns.GetColumnByName(columnName);
//			foreach (var row in rows)
//			{
//				int value = Convert.ToInt32(row.GetValue(col.Index));
//				sum += value;
//			}
//			return sum;
//		}


//		public List<RowGroup> GroupBy(params string[] columnNames)
//		{
//			if (columnNames == null)
//				throw new ArgumentNullException(nameof(columnNames));

//			var colIndices = new List<int>(columnNames.Length);
//			foreach (var columnName in columnNames)
//			{
//				var col = columns.GetColumnByName(columnName);
//				colIndices.Add(col.Index);
//			}

//			var groups = rows.GroupBy((r) =>
//			{
//				var groupKey = new StringBuilder(250);
//				foreach (var idx in colIndices)
//					groupKey.Append(r.GetValue(idx));
//				return groupKey.ToString();
//			}).ToList();

//			var result = new List<RowGroup>(groups.Count);
//			foreach (var g in groups)
//				result.Add(new RowGroup(g.Key, g));

//			return result;
//		}

//		/// <summary>
//		/// Iterates through the entire data set calling the specified delegate and modifying the CurrentRow property on each iteration.
//		/// </summary>
//		/// <param name="action">The action to be executed</param>
//		public void ForEach(Action<ImportedDataReader> action)
//		{
//			CurrentRow = 0;
//			while (CurrentRow < rows.Count)
//			{
//				action(this);
//				CurrentRow++;
//			}
//		}


//		public string GetInputColumnName(string fieldName)
//		{
//			var col = columns.GetColumnByName(fieldName);
//			return col.InputColumn;
//		}


//		public string GetTargetColumnName(string fieldName)
//		{
//			var col = columns.GetColumnByName(fieldName);
//			return col.TargetColumn;
//		}


//		/// <summary>
//		/// Returns the ID column from all records found in the document
//		/// </summary>
//		public List<int> GetImportedRecordIDs()
//		{
//			var idColumn = columns.GetColumnByName("ID");
//			List<int> ids = new List<int>();
//			foreach (var row in rows)
//			{
//				var id = Convert.ToInt32(row.GetValue(idColumn.Index));
//				if (!ids.Contains(id))
//					ids.Add(id);
//			}
//			return ids;
//		}


//		public object GetRecordValue(int id, string columnName)
//		{
//			var idColumn = columns.GetColumnByName("ID");
//			var col = columns.GetColumnByName(columnName);
//			foreach (var row in rows)
//			{
//				var rowid = Convert.ToInt32(row.GetValue(idColumn.Index));
//				if (id == rowid)
//				{
//					return row.GetValue(col.Index);
//				}
//			}
//			throw new Exception($"Cannot find a row with id {id} in the imported data.");
//		}


//		public T GetRecordValue<T>(int id, string columnName)
//		{
//			var idColumn = columns.GetColumnByName("ID");
//			var col = columns.GetColumnByName(columnName);
//			foreach (var row in rows)
//			{
//				var rowid = Convert.ToInt32(row.GetValue(idColumn.Index));
//				if (id == rowid)
//				{
//					return (T)Convert.ChangeType(row.GetValue(col.Index), typeof(T));
//				}
//			}
//			throw new Exception($"Cannot find a row with id {id} in the imported data.");
//		}


//		/// <summary>
//		/// Gets the row reader object associated to the currently active row. See CurrentRow property to determine which row is active.
//		/// </summary>
//		public ImportedRowReader GetCurrentRow()
//		{
//			if (rows.Count == 0)
//				throw new InvalidOperationException("Data set is empty");

//			return rows[CurrentRow];
//		}


//		/// <summary>
//		/// Gets the row reader object associated to the specified row index.
//		/// </summary>
//		public ImportedRowReader GetRow(int rowIndex)
//		{
//			return rows[rowIndex];
//		}


//		/// <summary>
//		/// Gets the values of the currently active row. See CurrentRow property to determine which row is active.
//		/// </summary>
//		public IEnumerable<object> GetCurrentRowValues()
//		{
//			if (rows.Count == 0)
//				throw new InvalidOperationException("Data set is empty");

//			return rows[CurrentRow].Values;
//		}


//		/// <summary>
//		/// Gets the values of the specified row index.
//		/// </summary>
//		public IEnumerable<object> GetRowValues(int rowIndex)
//		{
//			return rows[rowIndex].Values;
//		}


//		public ImportedRowReader AddRow()
//		{
//			var row = rows.AddRow();
//			return row;
//		}


//		public ImportedRowReader AddRow(IEnumerable<object> values)
//		{
//			var row = rows.AddRow(values);
//			return row;
//		}


//		public ImportedRowReader InsertRow(int rowIndex)
//		{
//			var row = rows.InsertRow(rowIndex);
//			return row;
//		}


//		public ImportedRowReader InsertRow(int rowIndex, IEnumerable<object> values)
//		{
//			var row = rows.InsertRow(rowIndex, values);
//			return row;
//		}


//		public void RemoveRow(int rowIndex)
//		{
//			rows.RemoveRow(rowIndex);
//		}


//		/// <summary>
//		/// Clears the rows collection
//		/// </summary>
//		public void ClearRows()
//		{
//			rows.ClearRows();
//		}


//		internal void AddValue(string columnName, object value)
//		{
//			foreach (var row in rows)
//				row.AddValue(columnName, value);
//		}


//		internal void RemoveValue(string columnName)
//		{
//			foreach (var row in rows)
//				row.RemoveValue(columnName);
//		}

//		public IEnumerator<ImportedRowReader> GetEnumerator()
//		{
//			foreach (var row in rows)
//				yield return row;
//		}

//		IEnumerator IEnumerable.GetEnumerator()
//		{
//			foreach (var row in rows)
//				yield return row;
//		}
//	}


//	public class RowGroup : IEnumerable<ImportedRowReader>
//	{
//		private IEnumerable<ImportedRowReader> rows;

//		public RowGroup(string key, IEnumerable<ImportedRowReader> rows)
//		{
//			Key = key;
//			this.rows = rows;
//		}


//		public string Key { get; private set; }


//		public List<ImportedRow> Rows
//		{
//			get
//			{
//				var list = new List<ImportedRow>();
//				foreach (var row in rows)
//					list.Add(new ImportedRow(row.Values));
//				return list;
//			}
//		}


//		public IEnumerator<ImportedRowReader> GetEnumerator()
//		{
//			return rows.GetEnumerator();
//		}

//		IEnumerator IEnumerable.GetEnumerator()
//		{
//			return rows.GetEnumerator();
//		}
//	}


//	class ImportedColumnCollection : IEnumerable<ImportedColumnReader>
//	{
//		private ImportedDataReader reader;
//		private List<ImportedColumnReader> list;


//		public ImportedColumnCollection(ImportedDataReader reader)
//		{
//			this.reader = reader;
//			list = new List<ImportedColumnReader>(reader.data.Cols.Count);
//			foreach (var col in reader.data.Cols)
//				list.Add(new ImportedColumnReader(col, list.Count));
//		}


//		public int Count { get => list.Count; }


//		public ImportedColumnReader this[int index]
//		{
//			get => list[index];
//		}


//		public ImportedColumnReader GetColumnByName(string name, bool throwIfNotFound = true)
//		{
//			if (name == null)
//				throw new ArgumentNullException(nameof(name));
//			var col = list.FirstOrDefault(p => String.Compare(p.InputColumn, name, true) == 0);
//			if (col == null)
//			{
//				name = RemoveOpCodes(name);
//				col = list.FirstOrDefault(p => String.Compare(RemoveOpCodes(p.TargetColumn), name, true) == 0);
//			}
//			if (throwIfNotFound && col == null)
//				throw new InvalidOperationException($"Field {name} is not defined in the document mappings.");
//			return col;
//		}


//		private string RemoveOpCodes(string column)
//		{
//			if (column == null)
//				return null;
//			column = column.Replace('!', '.');
//			column = column.Replace('@', '.');
//			column = column.Replace("#", "");
//			return column;
//		}


//		public void AddColumn(string columnName)
//		{
//			var col = GetColumnByName(columnName, false);
//			if (col != null)
//				throw new InvalidOperationException($"Column collection already contains another field called {col}, cannot add a duplicated column.");

//			var column = new ImportedCol() { InputColumn = columnName, TargetColumn = columnName };
//			col = new ImportedColumnReader(column, list.Count);
//			reader.data.Cols.Add(column);
//			list.Add(col);

//			// Append value to all rows
//			reader.AddValue(columnName, null);
//		}


//		public void AddColumn(ImportedCol column)
//		{
//			var columnName = column.InputColumn;
//			if (String.IsNullOrWhiteSpace(columnName))
//				columnName = column.TargetColumn;
//			if (String.IsNullOrWhiteSpace(columnName))
//				throw new InvalidOperationException("Mapping must have either an InputColumn name or a TargetColumn name.");

//			var col = GetColumnByName(columnName, false);
//			if (col != null)
//				throw new InvalidOperationException($"Column collection already contains another field called ({col}), cannot add a duplicated column.");

//			col = new ImportedColumnReader(column, list.Count);
//			reader.data.Cols.Add(column);
//			list.Add(col);

//			// Append value to all rows
//			reader.AddValue(columnName, null);
//		}


//		public void RemoveColumn(string columnName)
//		{
//			if (columnName == null)
//				throw new ArgumentNullException(nameof(columnName));

//			var col = GetColumnByName(columnName);
//			if (list.Remove(col))
//			{
//				for (var i = 0; i < list.Count; i++)
//					list[i].Index = i;
//				reader.data.Cols.RemoveAt(col.Index);
//				reader.RemoveValue(columnName);
//			}
//		}


//		public void RemoveColumn(ImportedCol column)
//		{
//			if (column == null)
//				throw new ArgumentNullException(nameof(column));

//			var col = GetColumnByName(column.TargetColumn);
//			if (list.Remove(col))
//			{
//				for (var i = 0; i < list.Count; i++)
//					list[i].Index = i;
//				reader.data.Cols.RemoveAt(col.Index);
//				reader.RemoveValue(col.Index);
//			}
//		}


//		public IEnumerator<ImportedColumnReader> GetEnumerator()
//		{
//			foreach (var e in list)
//				yield return e;
//		}


//		IEnumerator IEnumerable.GetEnumerator()
//		{
//			foreach (var e in list)
//				yield return e;
//		}
//	}


//	public class ImportedColumnReader
//	{
//		private ImportedCol column;

//		public ImportedColumnReader(ImportedCol column, int index)
//		{
//			this.column = column;
//			this.Index = index;
//		}

//		public int Index { get; internal set; }
//		public string InputColumn { get => column.InputColumn; }
//		public string TargetColumn { get => column.TargetColumn; }
//	}


//	class ImportedRowCollection : IEnumerable<ImportedRowReader>
//	{
//		private ImportedDataReader reader;
//		private List<ImportedRowReader> list;

//		public ImportedRowCollection(ImportedDataReader reader)
//		{
//			this.reader = reader;
//			list = new List<ImportedRowReader>();
//			foreach (var row in reader.data.Rows)
//				list.Add(new ImportedRowReader(reader, row));
//		}


//		public int Count { get => list.Count; }


//		public ImportedRowReader this[int index]
//		{
//			get => list[index];
//		}


//		public ImportedRowReader AddRow()
//		{
//			var defaultValues = new object[reader.data.Cols.Count];
//			var row = new ImportedRow(defaultValues);
//			var rowReader = new ImportedRowReader(reader, row);
//			reader.data.Rows.Add(row);
//			list.Add(rowReader);

//			for (int i = 0; i < list.Count; i++)
//				list[i].Index = i;

//			return rowReader;
//		}


//		public ImportedRowReader AddRow(IEnumerable<object> values)
//		{
//			var valueCount = values.Count();
//			if (valueCount != reader.ColumnCount)
//				throw new InvalidOperationException($"The number of supplied values ({valueCount}) does not match the number of columns present in the document ({reader.ColumnCount}).");

//			var row = new ImportedRow(values);
//			var rowReader = new ImportedRowReader(reader, row);
//			reader.data.Rows.Add(row);
//			list.Add(rowReader);

//			for (int i = 0; i < list.Count; i++)
//				list[i].Index = i;

//			return rowReader;
//		}

//		public ImportedRowReader InsertRow(int index)
//		{
//			if (index < 0 || index > list.Count)
//				throw new IndexOutOfRangeException($"Cannot insert row into the collection becuase the specified index ({index}) is outside the valid range: [0, {list.Count}].");

//			var defaultValues = new object[reader.data.Cols.Count];
//			var row = new ImportedRow(defaultValues);
//			var rowReader = new ImportedRowReader(reader, row);
//			reader.data.Rows.Insert(index, row);
//			list.Insert(index, rowReader);

//			for (int i = 0; i < list.Count; i++)
//				list[i].Index = i;

//			return rowReader;
//		}


//		public ImportedRowReader InsertRow(int index, IEnumerable<object> values)
//		{
//			if (index < 0 || index > list.Count)
//				throw new IndexOutOfRangeException($"Cannot insert row into the collection becuase the specified index ({index}) is outside the valid range: [0, {list.Count}].");

//			var valueCount = values.Count();
//			if (valueCount != reader.ColumnCount)
//				throw new InvalidOperationException($"The number of supplied values ({valueCount}) does not match the number of columns present in the document ({reader.ColumnCount}).");

//			var row = new ImportedRow(values);
//			var rowReader = new ImportedRowReader(reader, row);
//			reader.data.Rows.Insert(index, row);
//			list.Insert(index, rowReader);

//			for (int i = 0; i < list.Count; i++)
//				list[i].Index = i;

//			return rowReader;
//		}


//		public void RemoveRow(int rowIndex)
//		{
//			if (rowIndex < 0 || rowIndex > list.Count)
//				throw new IndexOutOfRangeException($"Cannot insert row into the collection becuase the specified index ({rowIndex}) is outside the valid range: [0, {list.Count}].");

//			reader.data.Rows.RemoveAt(rowIndex);
//			list.RemoveAt(rowIndex);

//			for (int i = 0; i < list.Count; i++)
//				list[i].Index = i;
//		}


//		public void ClearRows()
//		{
//			reader.data.Rows.Clear();
//			list.Clear();
//		}


//		public IEnumerator<ImportedRowReader> GetEnumerator()
//		{
//			foreach (var e in list)
//				yield return e;
//		}


//		IEnumerator IEnumerable.GetEnumerator()
//		{
//			foreach (var e in list)
//				yield return e;
//		}
//	}


//	public class ImportedRowReader : IEnumerable<ColumnInfo>
//	{
//		private bool modified;
//		private ImportedRow row;
//		private ImportedDataReader reader;

//		/// <summary>
//		/// Initializes a new instance of ImportedRowReader
//		/// </summary>
//		internal ImportedRowReader(ImportedDataReader reader, ImportedRow row)
//		{
//			if (reader == null)
//				throw new ArgumentNullException(nameof(reader));
//			if (row == null)
//				throw new ArgumentNullException(nameof(row));

//			this.reader = reader;
//			this.row = row;
//			modified = false;
//		}


//		/// <summary>
//		/// Get the values assigned to this row
//		/// </summary>
//		public IEnumerable<object> Values
//		{
//			get
//			{
//				foreach(var key in row.Data.Keys)
//				{
//					yield return row.Data[key];
//				}
//			}
//		}


//		/// <summary>
//		/// Gets the number of values available in this row
//		/// </summary>
//		public int Count
//		{
//			get => row.Data.Count;
//		}


//		/// <summary>
//		/// Gets the index of this row within the rows collection
//		/// </summary>
//		public int Index { get; internal set; }


//		/// <summary>
//		/// Gets the value of the given column name
//		/// </summary>
//		public object this[string columnName]
//		{
//			get => GetValue(columnName);
//			set => SetValue(columnName, value);
//		}


//		/// <summary>
//		/// Get or sets a flag indicating if the values in this row have been edited. Methods used to set row values automatically update this flag to true.
//		/// </summary>
//		public bool Modified { get => modified; }


//		/// <summary>
//		/// Gets the value of the specified column name
//		/// </summary>
//		public object GetValue(string columnName)
//		{
//			if (row.Data.TryGetValue(columnName, out var value))
//				return value;
//			else
//				throw new KeyNotFoundException($"Column [{columnName}] was not found in the dictionary.");
//		}


//		/// <summary>
//		/// Sets the value of the specified column name
//		/// </summary>
//		public void SetValue(string columnName, object value)
//		{
//			if (row.Data.ContainsKey(columnName))
//				row.Data[columnName] = value;
//			else
//				throw new KeyNotFoundException($"Column [{columnName}] was not found in the dictionary.");
//			modified = true;
//		}


//		/// <summary>
//		/// Gets the original value of the specified column name (as it was when the object was initialized)
//		/// </summary>
//		public object GetOriginalValue(string columnName)
//		{
//			return row.GetOriginalValue(columnName);
//		}


//		/// <summary>
//		/// Adds a new value at the end of the values collection. A matching column must exist in order for this method to succeed.
//		/// </summary>
//		internal void AddValue(string columnName, object value)
//		{

//			if (reader.HasColumn(columnName))
//				throw new InvalidOperationException($"Specified column [{columnName}] is already present in the column collection.");

//			row.originalData.Add(columnName, value);
//			row.modifiedData.Add(columnName, value);
//			modified = true;
//		}


//		/// <summary>
//		/// Removes the value at the specified column index. The number of columns in the reader must match the number of values that will be left in the collection.
//		/// </summary>
//		internal void RemoveValue(string columnName)
//		{
//			if (!reader.HasColumn(columnName))
//				throw new InvalidOperationException($"Specified column [{columnName}] is not present in the column collection.");

//			row.originalData.Remove(columnName);
//			row.modifiedData.Remove(columnName);
//			modified = true;
//		}

//		public bool HasColumn(string columnName)
//		{
//			return reader.HasColumn(columnName);
//		}

//		public IEnumerator<ColumnInfo> GetEnumerator()
//		{
//			foreach (var key in row.modifiedData.Keys)
//			{
//				yield return new ColumnInfo(key, row.modifiedData[key], row.originalData[key]);
//			}
//		}

//		IEnumerator IEnumerable.GetEnumerator()
//		{
//			foreach (var key in row.modifiedData.Keys)
//			{
//				yield return new ColumnInfo(key, row.modifiedData[key], row.originalData[key]);
//			}
//		}
//	}


//	public class ColumnInfo
//	{
//		public string ColumnName { get; }
//		public object Value { get; }
//		public object OriginalValue { get; }

//		public ColumnInfo(string columnName, object value, object originalValue)
//		{
//			ColumnName = columnName;
//			Value = value;
//			OriginalValue = originalValue;
//		}
//	}
//}
