using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.Database
{
    public partial class DynamicDB: IDisposable
	{
		private IFactory factory;
		private IEventQueue events;
		private IDBX conn;

		private bool ENABLE_CASCADE = false;

		private ConcurrentDictionary<int, CatalogDefinition> cache = new ConcurrentDictionary<int, CatalogDefinition>();

        public DynamicDB(
            IFactory factory,
			IEventQueue events,
			IAppInfo appInfo
			)
		{
            this.factory = factory;
			this.events = events;
		}

		public IDBX Conn { get { return conn; } }

        public void Open(string connStr)
		{
            var cfg = factory.GetInstance<IDBConfiguration>();
			cfg.ProviderName = CommonDataProviders.SqlServer;
			cfg.ConnectionString = connStr;
			conn = cfg.CreateConnection();
			if (!conn.Exists("select * from sysobjects where name = '_Catalog_'"))
			{
				var catalog = typeof(CatalogDefinition).GetCatalogDefinition();
				CreateTable(catalog, false);
			}
			if (!conn.Exists("select * from sysobjects where name = '_Files_'"))
			{
				var filesTable = typeof(CatalogFile).GetCatalogDefinition();
				CreateTable(filesTable, false);
			}
		}

		public async Task OpenAsync(string connStr)
		{
			var cfg = factory.GetInstance<IDBConfiguration>();
			cfg.ProviderName = CommonDataProviders.SqlServer;
			cfg.ConnectionString = connStr;
			conn = await cfg.CreateConnectionAsync();
			if (!await conn.ExistsAsync("select * from sysobjects where name = '_Catalog_'"))
			{
				var catalog = typeof(CatalogDefinition).GetCatalogDefinition();
				CreateTable(catalog, false);
			}
			if (!conn.Exists("select * from sysobjects where name = '_Files_'"))
			{
				var filesTable = typeof(CatalogFile).GetCatalogDefinition();
				CreateTable(filesTable, false);
			}
		}

		public void Dispose()
		{
			if(conn != null)
				conn.Dispose();
			cache.Clear();
			cache = null;
		}

		public CatalogDefinition GetCatalog(int catalogid)
		{
			if (cache.TryGetValue(catalogid, out var cat))
				return cat;
			cat = conn.SelectOne<CatalogDefinition>(catalogid);
			cache.TryAdd(catalogid, cat);
			return cat;
		}

		public void CreateCatalog(CatalogDefinition catalog)
		{
            //Validations: 
            //	Only one field can be Indentity at most (can be without identity)
            //
			conn.Insert(catalog);
			CreateTable(catalog, true);
		}

        public Dictionary<string, string> DropCatalog(int catalogid)
        {
            var catalog = GetCatalog(catalogid);
            conn.Delete(catalog);
            var relatedTables = DropFKConstraints(catalog);
            //Remove fk from related catalogs definition
            foreach (var table in relatedTables)
            {
				// TODO: the relation table key can be a Relation Table(On To Mnay) or Set Table (Many To Many)
                var catalogId = table.Key.Split('_').Last();// this is not ok for Many to Many use the FieldKey as CatalogID
                var childCatalog = GetCatalog(int.Parse(catalogId));
                var definition = JsonConvert.DeserializeObject<List<FieldDefinition>>(childCatalog.Definition);
                var column = definition.FirstOrDefault(x => x.Name.Equals(table.Value));
                if (column != null)
                {
                    column.CatalogID = null;
                    column.Type = ColumnType.Int;
                    column.Length = 10;

                    childCatalog.Definition = JsonConvert.SerializeObject(definition);
                    conn.Update(childCatalog);
                }

                
            }

            DropTable(catalog, true);

            return relatedTables;
        }

        private Dictionary<string, string> DropFKConstraints(CatalogDefinition catalog)
        {
            var tableList = new Dictionary<string, string>();
            var columnList = new Dictionary<string, string>();

            var reader = conn.ExecuteReader($@"
                exec sp_fkeys '{catalog.Name}_{catalog.ID}';     
            ");

            while (reader.Read())
            {
                tableList.Add(reader["FKTABLE_NAME"].ToString(), reader["FKCOLUMN_NAME"].ToString());
            }

            conn.ExecuteNonQuery($@"BEGIN
                DECLARE @statement VARCHAR(300);

                --Cursor to generate ALTER TABLE DROP CONSTRAINT statements
                DECLARE dropCursor CURSOR FOR
                SELECT 'alter table ' + OBJECT_SCHEMA_NAME(parent_object_id) + '.' + OBJECT_NAME(parent_object_id) +
                        ' drop constraint ' + name
                FROM sys.foreign_keys
                WHERE OBJECT_SCHEMA_NAME(referenced_object_id) = 'dbo' AND
                OBJECT_NAME(referenced_object_id) = '{catalog.Name}_{catalog.ID}';

                OPEN dropCursor;
                FETCH dropCursor INTO @statement;

                --Drop each found foreign key constraint
                WHILE @@FETCH_STATUS = 0
                    BEGIN
                        EXEC(@statement);
                        FETCH dropCursor INTO @statement;
                    END

                    CLOSE dropCursor;
                    DEALLOCATE dropCursor;

            END
            ");

            return tableList;
        }


        public void DropConstraints(int catalogid)
		{
			var references = new List<FieldDefinition>();
			var sets = new List<FieldDefinition>();
			var def = GetCatalog(catalogid);
			foreach(var f in def.Fields)
			{
				if (f.Type == ColumnType.Reference)
					references.Add(f);
				else if(f.Type == ColumnType.Set)
					sets.Add(f);
			}
			DropReferences(def, references);
			DropSets(def, sets);
		}

		private void DropReferences(CatalogDefinition left, List<FieldDefinition> references)
		{
			foreach (var field in references) {
				var right = GetCatalog(field.CatalogID.Value);
				conn.ExecuteNonQuery($"alter table [{left.Name}_{left.ID}] drop constraint [FK_{left.ID}_{right.ID}_{field.FieldID}]");
                //conn.ExecuteNonQuery($"alter table [{left.Name}_{left.ID}] drop constraint IF EXISTS [FK_{left.ID}_{right.ID}_{field.FieldID}]");
            }
		}

		private void DropSets(CatalogDefinition left, List<FieldDefinition> sets)
		{
			foreach (var field in sets)
			{
				var right = GetCatalog(field.CatalogID.Value);
				conn.ExecuteNonQuery($"drop table [REL_{left.ID}_{right.ID}_{field.FieldID}]");
                //conn.ExecuteNonQuery($"drop table IF EXISTS [REL_{left.ID}_{right.ID}_{field.FieldID}]");
            }
		}

		public void AlterCatalog(CatalogDefinition catalog)
		{
			cache.TryRemove(catalog.ID, out _);
            DataRemediation(catalog);
            conn.Update(catalog);
		}

		public void ImportCatalog(CatalogDefinition catalog, string tableDefinition)
		{
			cache.TryRemove(catalog.ID, out _);

			var savedCatalog = GetCatalog(catalog.ID);
			var newCatalogFieldList = catalog.Fields;

			AlterTableAction(savedCatalog, newCatalogFieldList, AlterTableType.Add, "add");
			catalog.Definition = tableDefinition;
			conn.Update(catalog);
		}

		public void CreateTable(CatalogDefinition catalog, bool userTable = true)
		{
			var sets = new List<FieldDefinition>();
			var refs = new List<FieldDefinition>();
			var indexes = new List<FieldDefinition>();
			StringBuilder sb = new StringBuilder(1000);
			if(userTable)
				sb.AppendLine($"create table [{catalog.Name}_{catalog.ID}](");
			else
				sb.AppendLine($"create table [{catalog.Name}](");
			foreach (var f in catalog.Fields)
			{
				if (f.Type == ColumnType.Set) sets.Add(f);
				else
				{
					sb.AppendLine($"[{f.Name}] {GetDBType(f)} {GetIdentity(f)} {GetDBLength(f)} {GetNullable(f)},");
					if ((f.IsUnique || f.IsIndexed) && !f.IsKey) indexes.Add(f);
					if (f.Type == ColumnType.Reference) refs.Add(f);
				}
			}
			sb.AppendLine(CreatePrimaryKey(catalog, userTable));
			sb.AppendLine(")");
			conn.ExecuteNonQuery(sb.ToString());
			CreateRelationTables(catalog, sets);
			CreateIndexes(catalog, indexes);
			CreateReferenceFieldsForeignKeys(catalog, refs);
		}

        public void DropTable(CatalogDefinition catalog, bool userTable = true)
        {
            var sets = new List<FieldDefinition>();
            StringBuilder sb = new StringBuilder(1000);
            if (userTable)
                sb.AppendLine($"drop table [{catalog.Name}_{catalog.ID}]");
            else
                sb.AppendLine($"drop table [{catalog.Name}]");
            foreach (var f in catalog.Fields)
            {
                if (f.Type == ColumnType.Set) sets.Add(f);
            }

            DeleteRelationTables(catalog, sets);
            conn.ExecuteNonQuery(sb.ToString());
        }

		private string CreateSelectStatement(int catalogid)
		{
			int i = 1;
			StringBuilder select = new StringBuilder(1000);
			StringBuilder joins = new StringBuilder(1000);
			var cat = GetCatalog(catalogid);
			var refs = cat.Fields.Where(p => p.Type == ColumnType.Reference);
			foreach (var r in refs)
			{
				var alias = $"T{i++}";
				var rcat = GetCatalog(r.CatalogID.Value);
				var displayField = GetDisplayField(rcat);
				select.Append($", {alias}.{displayField.Name} as _{r.Name}_DISP ");
				joins.Append($" left outer join {rcat.Name}_{rcat.ID} as {alias} on a.{r.Name} = {alias}.ID ");
			}
			//return $"select a.* {select.ToString()} from {cat.Name}_{cat.ID} a {joins.ToString()} order by id  offset 0 rows fetch next 100 rows only  ";
            return $"select a.* {select.ToString()} from {cat.Name}_{cat.ID} a {joins.ToString()}   ";
        }

        private string CreateSelectStatement(int catalogid, int pagenumber, int pageSize)
        {
            int i = 1;
            StringBuilder select = new StringBuilder(1000);
            StringBuilder joins = new StringBuilder(1000);
            var cat = GetCatalog(catalogid);
            var refs = cat.Fields.Where(p => p.Type == ColumnType.Reference);
            foreach(var r in refs)
            {
                var alias = $"T{i++}";
                var rcat = GetCatalog(r.CatalogID.Value);
                var displayField = GetDisplayField(rcat);
                select.Append($", {alias}.{displayField.Name} as _{r.Name}_DISP ");
                joins.Append($" left outer join {rcat.Name}_{rcat.ID} as {alias} on a.{r.Name} = {alias}.ID ");
            }
            return $"select a.* {select.ToString()} from {cat.Name}_{cat.ID} a {joins.ToString()} order by id  offset {pageSize * (pagenumber)} rows fetch next {pageSize} rows only  ";
            //return $"select a.* {select.ToString()} from {cat.Name}_{cat.ID} a {joins.ToString()}   ";
        }

        private FieldDefinition GetDisplayField(CatalogDefinition catalog)
		{
			foreach (var f in catalog.Fields)
				if (f.IsMainDisplay) return f;
			foreach (var f in catalog.Fields)
				if (f.Type == ColumnType.String) return f;
			foreach (var f in catalog.Fields)
				if (f.Name.ToLower() == "id") return f;
			return null;
		}

        private string GetDBType(FieldDefinition field)
		{
			switch (field.Type)
			{
				case ColumnType.Bool: return "bit";
				case ColumnType.Int: return "int";
				case ColumnType.Long: return "bigint";
				case ColumnType.Decimal: return "decimal";
				case ColumnType.Date: return "datetime";
				case ColumnType.String: return "nvarchar";
				case ColumnType.Reference: return "int";
                case ColumnType.Set: return "int";
				default: throw new Exception("Should never get here unless a new type is added and not implemented.");
			}
		}

		private string GetIdentity(FieldDefinition field)
		{
			if (!field.IsIdentity) return "";
			else return "identity(1,1)";
		}

		private string GetDBLength(FieldDefinition field)
		{
			if (field.Type == ColumnType.String)
			{
				if (field.Length.HasValue && (field.Length > 0 && field.Length < 4001))
					return $"({field.Length})";
				else
					return $"(max)";
			}

			if (field.Type == ColumnType.Decimal)
			{
			    return "(20,2)";				
			}

			else return "";
		}

		private string GetNullable(FieldDefinition field)
		{
			if (field.CanBeEmpty || field.Type == ColumnType.Reference || field.Type == ColumnType.Bool)
				return "null";
			else
				return "not null";
		}

		private string CreatePrimaryKey(CatalogDefinition catalog, bool userTable)
		{
			var pks = catalog.Fields.Where(p => p.IsKey).ToList();
			if (pks.Count > 0)
			{
				StringBuilder sb = new StringBuilder(100);
				if(userTable)
					sb.AppendLine($"constraint [PK_{catalog.ID}] primary key clustered (");
				else
					sb.AppendLine($"constraint [PK_{catalog.Name}] primary key clustered (");
				foreach (var f in pks) sb.Append($"[{f.Name}] asc,");
				sb.Remove(sb.Length - 1, 1);
				sb.Append(")");
				return sb.ToString();
			}
			return "";
		}

		private void CreateRelationTables(CatalogDefinition left, List<FieldDefinition> sets)
		{
			foreach(var set in sets)
			{
                if (set.CatalogID == 0) continue;
				var right = conn.SelectOne<CatalogDefinition>(set.CatalogID);
				var rel = new CatalogDefinition()
				{
					Name = $"REL_{left.ID}_{right.ID}_{set.FieldID}",
					Fields = {
						new FieldDefinition(){ Name = "SourceID", IsKey = true, CanBeEmpty = false, Type = ColumnType.Int },
						new FieldDefinition(){ Name = "TargetID", IsKey = true, CanBeEmpty = false, Type = ColumnType.Int }
					}
				};

				
				var onDeleteCascade = string.Empty;

				if (ENABLE_CASCADE)
				{
					onDeleteCascade = "on delete cascade";
				}


				CreateTable(rel, false);
				conn.ExecuteNonQuery($@"
					alter table [{rel.Name}] with check add constraint [FK_{rel.Name}_SourceID] foreign key([SourceID])
					references [{left.Name}_{left.ID}] ([ID]) {onDeleteCascade}

					alter table [{rel.Name}] check constraint [FK_{rel.Name}_SourceID]

					alter table [{rel.Name}] with check add constraint [FK_{rel.Name}_TargetID] foreign key([TargetID])
					references [{right.Name}_{right.ID}] ([ID]) {onDeleteCascade}

					alter table [{rel.Name}] check constraint [FK_{rel.Name}_TargetID]
				");
			}
		}

        private void CreateIndexes(CatalogDefinition catalog, List<FieldDefinition> indexes)
		{
			foreach(var field in indexes)
			{
				var catName = $"{catalog.Name}_{catalog.ID}";
				if (field.IsUnique)
				{
					conn.ExecuteNonQuery($@"
					create unique nonclustered index [IX_{field.FieldID}]
					on [{catName}]( 
						[{field.Name}] asc
					)");
				}
				else if(field.IsIndexed)
				{
					conn.ExecuteNonQuery($@"
					create nonclustered index [IX_{field.FieldID}]
					on [{catName}]( 
						[{field.Name}] asc
					)");
				}
			}
		}

        private void CreateReferenceFieldsForeignKeys(CatalogDefinition left, List<FieldDefinition> refs)
		{
			var onDeleteCascade = string.Empty;

			if (ENABLE_CASCADE)
			{
				onDeleteCascade = "on delete cascade";
			}


			foreach (var field in refs)
			{
				var right = conn.SelectOne<CatalogDefinition>(field.CatalogID);
				var catName = $"{left.Name}_{left.ID}";
				var fkName = $"FK_{left.ID}_{right.ID}_{field.FieldID}";
				conn.ExecuteNonQuery($@"
					alter table [{catName}] with check add constraint
					[{fkName}] foreign key ([{field.Name}])
					references [{right.Name}_{right.ID}]([ID]) 
					{onDeleteCascade}");
				conn.ExecuteNonQuery($"alter table [{catName}] check constraint [{fkName}]");
			}
		}
    }

    [TargetTable("_Catalog_")]
	public class CatalogDefinition: IEntity
	{
		private string definition;
		[PK, Identity]
		public int ID { get; set; }
		public string Name { get; set; }
		public string Definition
		{
			get => definition;
			set
			{
				definition = value;
				if (!String.IsNullOrWhiteSpace(definition))
					Fields = JsonConvert.DeserializeObject<List<FieldDefinition>>(value);
			}
		}
		[IgnoreField]
		public bool IsReadonly { get; set; }
		[IgnoreField]
		public bool IsHidden { get; set; }
		[IgnoreField]
		public CatalogType CatalogType { get; set; }
		[IgnoreField]
        public List<FieldDefinition> Fields { get; private set; } = new List<FieldDefinition>();
    }


	public enum CatalogType
	{
		Inlined = 0,    // Means that this catalog can be seen as an extension of another catalog. When referencing an inlined catalog, it is expected that the program always insert new rows in the inlined catalog (instead of referencing an existing row).
		Lookup = 1      // Default value: Means that this catalog is used to perform lookups (other catalogs referencing this one are expected to use one of the existing records, and only rarely insert data in the lookup catalog)
	}



	[TargetTable("_Files_")]
	public class CatalogFile : IEntity
	{
		[PK, Identity]
		public int ID { get; set; }
	}

	public class FieldDefinition
	{
		public int FieldID { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public bool IsSystem { get; set; }      // Means that the field is defined by default by the system. IMPORTANT: Users cannot change the definition of system fields (only their captions are editable).
		public bool IsKey { get; set; }         // Means the field is used as primary key.
		public bool IsIdentity { get; set; }    // Means the field is the identity column for the table.
		public bool IsLocked { get; set; }      // Means that the properties of the field are locked and cannot be changed by the user. This is always true for physical fields. IF the field IS NOT physical, SysAdmin users can remove this lock in order to be able to edit the properties of the field.
		public bool IsReadOnly { get; set; }    // Means that the field is visible to the user while editing, but cannot change the value
		public bool IsHidden { get; set; }      // If true, users will not see this field while editing the catalog data. Usually this is used for fields that are populated/changed programatically rather than by an end user.
		public bool IsUnique { get; set; }      // If true, the system will create a unique non-clustered index to ensure no other row in the catalog has the same value in this column. NOTE: If IsUnique is true, then CanBeEmpty MUST be false.
		public bool IsIndexed { get; set; }      // If true, the system will create a non-unique non-clustered index to speedup lookups using this field.
		public bool IsMainDisplay { get; set; }    // Determines if this field should be displayed to represent the entire record on screen. This is usefull only for Reference/Set types, for instance if in the UI we need to create a combobox, the text displayed in the combobox will be the value of the first field whose IsMainDisplay is true.
		public ColumnType Type { get; set; }
		public string Captions { get; set; }
		public int? Length { get; set; }
		public int? MinValue { get; set; }
		public int? MaxValue { get; set; }
		public DateTime? MinDate { get; set; }
		public DateTime? MaxDate { get; set; }
		public int? MaxWidth { get; set; }
		public int? MaxHeight { get; set; }
		public bool CanBeEmpty { get; set; } = true;
		public string ValidChars { get; set; }
		public string Regex { get; set; }
		public int? CatalogID { get; set; } // The ID of the referenced catalog (only meaningful when Type is Reference or Set). Note: The ID of the record being referenced is stored in the record itself, not in this definition.
		public List<TransformFunction> Functions { get; set; }

		public override string ToString()
		{
			return Name;
		}
	}

	public class CaptionDef
	{
		public string Language;
		public string Text;
	}

	public enum ColumnType
	{
		Reference = 1,
		Int = 2,
		Long = 3,
		Decimal = 4,
		Bool = 5,
		Date = 6,
		String = 7,
		Set = 9,
	}

	public class TransformFunction
	{
		public TransformFunctionType Type { get; set; }
		public string Arguments { get; set; }
	}


	public enum TransformFunctionType
	{
		FixedOptions = 1,
		Trim = 2,
		Truncate = 3,
		Upper = 4,
		Lower = 5
	}


	public class NameValuePair
	{
		public string Name;
		public object Value;

		public NameValuePair(string name, object value)
		{
			this.Name = name;
			this.Value = value;
		}
	}


	public class TableData
	{
		public int CatalogID { get; set; }
		public string Name { get; set; }
		public string Fields { get; set; }                  // JSON:  List<FieldDefinition>
		public CatalogType CatalogType { get; set; }
		public string Records { get; set; }                 // JSON:  JArray  [{ "ID": 2547, "Name": "Producto X", ...}, {}, {}]
                                                            // Orders, OrderDetails, VariableData, RFIDEncoding
		public TableData()
		{

		}

		public TableData(TableData toClone)
		{
			CatalogID = toClone.CatalogID; 
			Name = toClone.Name;
			Fields = toClone.Fields;
			CatalogType = toClone.CatalogType;
			Records = toClone.Records;
		}
	}


	public enum DataRelationType
	{
		Reference,
		Set
	}
}
