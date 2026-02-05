using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Service.Contracts.Documents
{
    public class DocumentImportConfiguration
    {
        public string JobID { get; set; }
        public string User { get; set; }
        public int CompanyID { get; set; }
        public int BrandID { get; set; }
        public int ProjectID { get; set; }
        public string FileName { get; set; }
        public Guid FileGUID { get; set; }
        public InputConfiguration Input { get; set; } = new InputConfiguration();
        public OutputConfiguration Output { get; set; } = new OutputConfiguration();
    }

    public class InputConfiguration
    {
        public string SourceType { get; set; }
        public string SourceCulture { get; set; }
        public string Encoding { get; set; }
        public string LineDelimiter { get; set; }
        public char ColumnDelimiter { get; set; }
        public char QuotationChar { get; set; }
        public bool IncludeHeader { get; set; }
        public string Plugin { get; set; }
    }

    public class ColInfo
    {
        public string Name { get; set; }
        public int Length { get; set; }
        public DocumentColumnType Type { get; set; }
    }

    public class OutputConfiguration
    {
        public string TargetDB { get; set; }
        public string TargetTable { get; set; }
        public List<DocumentColMapping> Mappings = new List<DocumentColMapping>();
        public int CatalogID;
    }

    public class DocumentColMapping
    {
        public string InputColumn { get; set; }
        public bool Ignore { get; set; }
        public DocumentColumnType Type { get; set; }
        public bool IsFixedValue { get; set; }
        public string FixedValue { get; set; }
        public int? MaxLength { get; set; }
        public int? MinLength { get; set; }
        public long? MinValue { get; set; }
        public long? MaxValue { get; set; }
        public DateTime? MinDate { get; set; }
        public DateTime? MaxDate { get; set; }
        public string DateFormat { get; set; }
        public int? DecimalPlaces { get; set; }
        public int? Function { get; set; }
        public string FunctionArguments { get; set; }
        public bool CanBeEmpty { get; set; }
        public string TargetColumn { get; set; }
        public bool IsPK { get; set; }
        public bool Visited { get; set; }
    }

    public enum DocumentColumnType
    {
        Text = 1,       // Represent a regular string
        Int32 = 2,      // Int32, Int64 and Decimal will use the specified SourceCulture to parse the text into the corresponding number.
        Int64 = 3,
        Boolean = 4,    // "1", "true" or "TRUE" will all be interpreted as boolean value true, any otrer value will be interpreted as false.
        Decimal = 5,
        DateTime = 6    // Standard date time representation based of the specified SourceCulture. NOTE: Support for other date formats might require us to define other Date types in this enumeration and implement them in code.
    }

    public enum ValidationFunction
    {
        None = 0,
        EAN13 = 1,                      // Ensures thesupplied value is a valid EAN13, no FunctionArguments are needed.
        SetLookup = 2,                  // Ensures that the value given to the column is contained within the set. FunctionArguments must contain a comma delimited list of the values that are acepted.
        ValueMapping = 3,               // Interprets the value in the file as a key, then looks up the key in the dictionary and replaces said key with its corresponding value. FunctionArguments must contain the dictionary as a json object.
        SystemDate = 4,                 // Initializes field with current system date, no FunctionArguments are needed.
        ParseFileNameByIndex = 5,       // Get Data from filename from start index and length
        Append = 6,                     // Used to append simple values to target column
        Concat = 7,                     // Used to append multiple values to target column and allowing for table lookups, insertion of constant strings and other complex operations.
        Substring = 8,                  // Gets a substring from the current field value, arguments can include a start index and length, separated by comma (length is optional)
        GS1CheckDigit = 9,              // Calculates the GS1 check digit using the GS1Mod algorithm, arguments include the type of GS1 barcode expected, which can be any of the following: GTIN-8, GTIN-12, GTIN-13, GTIN-14, GSIN or SSCC and optional start index (length is implicit from the barcode type)
        SetDefaultIsEmpty = 10,         // Determine if field is empty or null and assign one value by default that receive by parameter
        TransformIfAreEqual = 11,       // Compare the field value with user input, if true return a user value, if false return the same value, use parameter to return empty if value is false


    }

    public class DocumentImportProgress
    {
        public volatile int Progress;       // overall job progress (include both the extraction of data from the document and the insertion of data in the database)
        public volatile int ReadProgress;   // read progress, covers only the extraction of data from the document
        public volatile int WriteProgress;  // write progress, covers only the writing of data to the database

        public static DocumentImportProgress Completed = new DocumentImportProgress() { Progress = 100, ReadProgress = 100, WriteProgress = 100 };
    }

    public class DocumentImportResult
    {
        public bool JobCompleted { get; set; }      // True if the job has been completed in its entirety, false otherwise.
        public bool Success { get; set; }           // True if no errors have been found, false otherwise
        public bool ReadCompleted { get; set; }     // True if the extraction of data from the document has been completed, false otherwise (ImportedData is meaningless until ReadCompleted is true).
        public bool WriteCompleted { get; set; }    // True if the data extracted from the document has been sucessfully incorporated to the database, false otherwise.
        public int TotalRows { get; set; }          // The number of rows extracted from the document
        public List<DocumentImportError> Errors { get; set; } = new List<DocumentImportError>(); // A list of the errors found during the processing of the document.

        public string GetErrors()
        {
            StringBuilder sb = new StringBuilder(1000);
            if(Errors != null && Errors.Count > 0)
            {
                foreach(var e in Errors)
                    sb.AppendLine($"\t\tRow: {e.Row}, Col: {e.Column}, Field: {e.FieldName}, ErrCode: {e.ErrorCode}, ErrMsg: {e.ErrorMessage}");
            }
            else
            {
                sb.Append("Errors is null or empty");
            }
            return sb.ToString();
        }
    }

    public class DocumentImportError
    {
        public DocumentImportError() { }

        public DocumentImportError(string fieldName, int column, int row, int errorCode, string error)
        {
            FieldName = fieldName;
            Column = column;
            Row = row;
            ErrorCode = errorCode;
            ErrorMessage = error;
        }

        public string FieldName { get; set; }
        public int Column { get; set; }
        public int Row { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string PluginName;
        public string MethodName;
        public string CatalogName;
        public string ColumnName;
        public string InputValue;
    }

    public class DocumentImportErrors
    {
        public const int OPERATION_CANCELLED = 0;
        public const int UNEXPECTED_ERROR = 1;
        public const int CELL_RANGE_ERROR = 2;
        public const int NO_DATA_ERROR = 3;
        public const int UNEXPECTED_COLUMN = 4;
        public const int INDETERMINATE_VALUE = 5;
        public const int COLUMN_NAME_MISMATCH = 6;
        public const int EMPTY_VALUE = 7;
        public const int MIN_LENGTH_ERROR = 8;
        public const int MAX_LENGTH_ERROR = 9;
        public const int NOT_A_BOOLEAN = 10;
        public const int NOT_AN_INT32 = 11;
        public const int NOT_AN_INT64 = 12;
        public const int NOT_A_DECIMAL = 13;
        public const int NOT_A_DATE = 14;
        public const int MIN_VALUE_ERROR = 15;
        public const int MAX_VALUE_ERROR = 16;
        public const int MIN_DATE_ERROR = 17;
        public const int MAX_DATE_ERROR = 18;
        public const int DECIMAL_PLACES_ERROR = 19;
        public const int CELL_VALIDATION_ERROR = 20;
        public const int MISSING_COLUMN = 21;
        public const int TARGET_DB_NOTCONFIGURED = 40;
        public const int INVALID_DB_CONFIGURATION = 41;
        public const int GENERAL_ERROR = 42;
        public const int INVALID_OPERATION = 43;
        public const int PLUGIN_ERROR = 44;
        public const int CATALOGLOOKUP_ERROR = 45;
        public const int INVALID_ENCODING = 46;
    }

    public class ImportedData
    {
        public int CurrentRow { get; set; }
        public List<ImportedCol> Cols { get; set; } = new List<ImportedCol>();
        public List<ImportedRow> Rows { get; set; } = new List<ImportedRow>();

        public List<int> GetImportedRecordIDs()
        {
            List<int> IDs = new List<int>();
            foreach(var row in Rows)
            {
                var id = Convert.ToInt32(row.GetValue("ID"));
                if(!IDs.Contains(id))
                    IDs.Add(id);
            }
            return IDs;
        }

        public bool HasProperty(string fieldName)
        {
            var col = Cols.FirstOrDefault(p => String.Compare(p.InputColumn, fieldName, true) == 0);
            return col != null;
        }

        public ImportedCol GetColumnByName(string fieldName, bool throwIfNotFound = true)
        {
            var col = GetInputColumnByName(fieldName);
            if(col == null)
                col = GetTargetColumnByName(fieldName);
            if(col == null && throwIfNotFound)
                throw new InvalidOperationException($"Field {fieldName} is not defined in the document mappings.");
            return col;
        }

        public ImportedCol GetInputColumnByName(string inputColumn)
        {
            if(inputColumn == null)
                return null;
            inputColumn = RemoveOpCodes(inputColumn);
            var col = Cols.FirstOrDefault(p => p.InputColumn != null && String.Compare(RemoveOpCodes(p.InputColumn), inputColumn, true) == 0);
            return col;
        }

        public ImportedCol GetTargetColumnByName(string targetColumn)
        {
            if(targetColumn == null)
                return null;
            targetColumn = RemoveOpCodes(targetColumn);
            var col = Cols.FirstOrDefault(p => p.TargetColumn != null && String.Compare(RemoveOpCodes(p.TargetColumn), targetColumn, true) == 0);
            return col;
        }

        public object GetValue(string fieldName)
        {
            var col = GetColumnByName(fieldName);
            return Rows[CurrentRow].GetValue(col);
        }

        public void SetValue(string fieldName, object value)
        {
            var col = GetColumnByName(fieldName);
            var row = Rows[CurrentRow];
            row.SetValue(col, value);
        }

        public string GetRecordValue(int id, string targetColumn)
        {
            var col = GetTargetColumnByName(targetColumn);
            if(col == null)
                throw new Exception($"Cannot find the column {targetColumn} in the imported data.");
            foreach(var row in Rows)
            {
                var rowid = Convert.ToInt32(row.GetValue("ID"));
                if(id == rowid)
                {
                    return (string)row.GetValue(col);
                }
            }
            throw new Exception($"Cannot find a row with ID {id} in the imported data.");
        }


        public int Sum(int id, string targetColumnName)
        {
            int sum = 0;
            var col = Cols.First(p => p.TargetColumn.EndsWith(targetColumnName, StringComparison.OrdinalIgnoreCase));
            foreach(var row in Rows)
            {
                var rowid = Convert.ToInt32(row.GetValue("ID"));
                if(id == rowid)
                {
                    var fieldName = col.InputColumn;
                    if(String.IsNullOrWhiteSpace(fieldName))
                        fieldName = col.TargetColumn;
                    int value = Convert.ToInt32(row.GetValue(fieldName));
                    sum += value;
                }
            }
            return sum;
        }


        public int Sum(string targetColumnName)
        {
            int sum = 0;
            var col = GetColumnByName(targetColumnName);
            foreach(var row in Rows)
            {
                var srcValue = row.GetValue(col);
                int value = Convert.ToInt32(srcValue);
                sum += value;
            }
            return sum;
        }


        public List<RowGroup> GroupBy(params string[] columnNames)
        {
            if(columnNames == null)
                throw new ArgumentNullException(nameof(columnNames));

            var groups = Rows.GroupBy((r) =>
            {
                var groupKey = new StringBuilder(250);
                foreach(var colName in columnNames)
                {
                    var col = GetColumnByName(colName);
                    groupKey.Append(r.GetValue(col));
                }
                return groupKey.ToString();
            }).ToList();

            var result = new List<RowGroup>(groups.Count);
            foreach(var g in groups)
                result.Add(new RowGroup(g.Key, g));

            return result;
        }


        public bool HasColumn(string columnName)
        {
            var col = GetColumnByName(columnName, false);
            if(col == null)
                return false;
            return true;
        }


        private string RemoveOpCodes(string column)
        {
            if(column == null)
                return null;
            column = column.Replace('!', '.');
            column = column.Replace('@', '.');
            column = column.Replace("#", "");
            return column;
        }


        public void AddColumn(string inputColumn, string targetColumn, object value)
        {
            Cols.Add(new ImportedCol(inputColumn, targetColumn));
            foreach(var row in Rows)
            {
                if(!String.IsNullOrWhiteSpace(inputColumn))
                    row.SetValue(inputColumn, value);
                if(!String.IsNullOrWhiteSpace(targetColumn))
                    row.SetValue(targetColumn, value);
            }
        }


        public void ForEach(Action<ImportedRow> action)
        {
            CurrentRow = 0;
            while(CurrentRow < Rows.Count)
            {
                var row = Rows[CurrentRow];
                action(row);
                CurrentRow++;
            }
        }


        public void ClearRows()
        {
            foreach(var row in Rows)
            {
                row.originalData.Clear();
                row.modifiedData.Clear();
            }
            Rows.Clear();
        }
    }


    public class ImportedRow
    {
        public ImportedRow() { }

        public ImportedRow(params object[] data)
        {
            if(data == null)
                throw new ArgumentNullException(nameof(data));
            if(data.Length % 2 != 0)
                throw new Exception("Invalid data supplied");
            for(int i = 0; i < data.Length; i += 2)
            {
                originalData[data[i].ToString()] = data[i + 1];
                Data[data[i].ToString()] = data[i + 1];
            }
        }

        internal Dictionary<string, object> originalData = new Dictionary<string, object>();
        internal Dictionary<string, object> modifiedData = new Dictionary<string, object>();

        public Dictionary<string, object> Data
        {
            get
            {
                return modifiedData;
            }
            set
            {
                originalData = value;
                foreach(var key in value.Keys)
                    modifiedData[key] = value[key];
            }
        }

        public void SetValues(params object[] args)
        {
            for(int i = 0; i < args.Length; i += 2)
            {
                originalData[args[i].ToString()] = args[i + 1];
                Data[args[i].ToString()] = args[i + 1];
            }
        }

        public void SetValue(string fieldName, object value)
        {
            if(!originalData.ContainsKey(fieldName))
                originalData[fieldName] = value;
            modifiedData[fieldName] = value;
        }

        public void AppendValue(string fieldName, object value)
        {
            if(fieldName == null)
                throw new ArgumentNullException(nameof(fieldName));

            if(value == null)
                value = "";

            if(!originalData.ContainsKey(fieldName))
                originalData[fieldName] = value.ToString();

            if(modifiedData.ContainsKey(fieldName))
            {
                modifiedData[fieldName] = modifiedData[fieldName].ToString() + value.ToString();
            }
            else modifiedData[fieldName] = value.ToString();
        }

        public object GetValue(string fieldName)
        {
            if(fieldName == null)
                return null;
            if(Data.TryGetValue(fieldName, out var value))
                return value;
            else
                return "";
        }

        public object GetOriginalValue(string fieldName)
        {
            if(originalData.TryGetValue(fieldName, out var value))
                return value;
            else
                return "";
        }

        public bool HasProperty(string fieldName)
        {
            return Data.ContainsKey(fieldName);
        }


        public object GetValue(ImportedCol col)
        {
            if(col == null)
                throw new ArgumentNullException(nameof(col));
            if(!String.IsNullOrWhiteSpace(col.InputColumn) && Data.TryGetValue(col.InputColumn, out var value))
                return value;
            else if(!String.IsNullOrWhiteSpace(col.TargetColumn) && Data.TryGetValue(col.TargetColumn, out value))
                return value;
            else
                return "";
        }


        public void SetValue(ImportedCol col, object value)
        {
            if(col == null)
                throw new ArgumentNullException(nameof(col));
            if(!String.IsNullOrWhiteSpace(col.InputColumn) && Data.ContainsKey(col.InputColumn))
            {
                Data[col.InputColumn] = value;
            }
            else if(!String.IsNullOrWhiteSpace(col.TargetColumn) && Data.ContainsKey(col.TargetColumn))
            {
                Data[col.TargetColumn] = value;
            }
            else
            {
                if(!String.IsNullOrWhiteSpace(col.TargetColumn))
                    Data[col.TargetColumn] = value;
                else
                    Data[col.InputColumn] = value;
            }
        }


        public object GetOriginalValue(ImportedCol col)
        {
            if(col == null)
                throw new ArgumentNullException(nameof(col));
            if(originalData.TryGetValue(col.InputColumn, out var value))
                return value;
            else if(originalData.TryGetValue(col.TargetColumn, out value))
                return value;
            else
                return "";
        }
    }


    public class ImportedCol
    {
        public ImportedCol() { }

        public ImportedCol(string inputColumn, string targetColumn)
        {
            InputColumn = inputColumn;
            TargetColumn = targetColumn;
        }

        public string InputColumn { get; set; }
        public string TargetColumn { get; set; }

        public override string ToString()
        {
            return $"{InputColumn} => {TargetColumn}";
        }
    }


    public class RowGroup : IEnumerable<ImportedRow>
    {
        private List<ImportedRow> rows;

        public RowGroup(string key, IEnumerable<ImportedRow> rows)
        {
            Key = key;
            this.rows = new List<ImportedRow>(rows);
        }


        public string Key { get; private set; }


        public List<ImportedRow> Rows { get => rows; }


        public IEnumerator<ImportedRow> GetEnumerator()
        {
            return rows.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return rows.GetEnumerator();
        }
    }


    public class PluginError : DocumentImportError
    {
        public PluginError(string plugin, string method, Exception ex)
            : base("", -1, -1, DocumentImportErrors.PLUGIN_ERROR, $"Error executing plugin {plugin}, method {method}: {ex.Message}")
        {
            PluginName = plugin;
            MethodName = method;
        }
    }

    public class MissingColumnError : DocumentImportError
    {
        public MissingColumnError(int colIndex, int rowIndex, string columnName)
            : base("", colIndex, rowIndex, DocumentImportErrors.MISSING_COLUMN, $"The file is missing a required column: {columnName}")
        {
            ColumnName = columnName;
        }
    }

    public class CatalogLookupError : DocumentImportError
    {
        public CatalogLookupError(int colIndex, int rowIndex, string catalogName, string columnName, string value)
            : base("", colIndex, rowIndex, DocumentImportErrors.CATALOGLOOKUP_ERROR, $"Catalog lookup did not find any matching record. Catalog: [{catalogName}], Column: [{columnName}], Input Value: [{value}]")
        {
            CatalogName = catalogName;
            ColumnName = columnName;
            InputValue = value;
        }
    }
}
