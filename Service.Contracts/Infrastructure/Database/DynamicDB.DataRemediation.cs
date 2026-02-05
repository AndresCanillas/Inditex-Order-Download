using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Service.Contracts.Database
{
	partial class DynamicDB
	{
		//Update Catalog: method called when a Catalog is edited to apply all data remediation
		private void DataRemediation(CatalogDefinition newCatalogDefinition)
		{
			var savedCatalog = GetCatalog(newCatalogDefinition.ID);
			var savedCatalogFieldList = savedCatalog.Fields;
			var newCatalogFieldList = newCatalogDefinition.Fields;
			var actualFieldIdList = newCatalogDefinition.Fields.Select(x => x.FieldID).ToList();
			var savedFieldIdList = savedCatalog.Fields.Select(x => x.FieldID).ToList();
			var newFields = actualFieldIdList.Except(savedFieldIdList).ToList();
			var oldFields = savedFieldIdList.Except(actualFieldIdList).ToList();
			var allFields = actualFieldIdList.Intersect(savedFieldIdList).ToList();
			var updatedFields = new List<int>();

			//Get just updated Catalog fields
			foreach (var field in allFields)
			{
				var updatedField = newCatalogDefinition.Fields.FirstOrDefault(x => Equals(x.FieldID, field));
				var actualDataField = updatedField != null ?
					JsonConvert.SerializeObject(updatedField) : null;

				var savedDataField = savedCatalog.Fields.FirstOrDefault(x => Equals(x.FieldID, field)) != null ?
					JsonConvert.SerializeObject(savedCatalog.Fields.FirstOrDefault(x => Equals(x.FieldID, field))) : null;

				if (updatedField.IsLocked || updatedField.IsSystem)
					continue;

				if (!JToken.DeepEquals(actualDataField, savedDataField))
					updatedFields.Add(field);
			}

			//Remove old fields
			if (oldFields.Count > 0)
			{
				var oldFieldsList = savedCatalog.Fields.Where(x => oldFields.Contains(x.FieldID)).ToList();
				AlterTableAction(savedCatalog, oldFieldsList, AlterTableType.Delete, "drop column ");
			}

			//Update modified fields
			if (updatedFields.Count > 0)
			{
				//Update column Names if there are any
				var updateFieldList = newCatalogFieldList.Where(x => updatedFields.Contains(x.FieldID)).ToList();
				var savedFieldList = savedCatalogFieldList.Where(x => updatedFields.Contains(x.FieldID)).ToList();
				AlterTableRenameColumn(savedCatalog, updateFieldList, savedFieldList);
				UpdateColumnByType(savedCatalog, updateFieldList, savedFieldList);
                AlterTableNullableColumn(savedCatalog, updateFieldList, savedFieldList);
                VariableDataRemediation(savedCatalog, updateFieldList, savedFieldList);
                AlterTableAction(savedCatalog, updateFieldList, AlterTableType.Alter, "alter column");
            }

            //Add new fields
            if (newFields.Count > 0)
			{
				newCatalogFieldList = newCatalogDefinition.Fields.Where(x => newFields.Contains(x.FieldID)).ToList();
				foreach (var f in newCatalogFieldList)
				{
					if (savedCatalog.Fields.FirstOrDefault(existingField => String.Compare(existingField.Name, f.Name, true) == 0) != null)
						newFields.Remove(f.FieldID);
				}

				if (newFields.Count > 0)
					AlterTableAction(savedCatalog, newCatalogFieldList, AlterTableType.Add, "add");
			}

			if (savedCatalog.Name != newCatalogDefinition.Name)
			{
				var oldName = $"{savedCatalog.Name}_{savedCatalog.ID}";
				var newName = $"{newCatalogDefinition.Name}_{savedCatalog.ID}";
				conn.ExecuteSP("sp_rename(@objname, @newname)", $"{oldName}", $"{newName}");
			}
		}


		//Alter Table: Add or delete Catalog fields
		private void AlterTableAction(CatalogDefinition catalog, List<FieldDefinition> fields, AlterTableType type, string value)
		{
			var sets = new List<FieldDefinition>();
			var refs = new List<FieldDefinition>();
			var indexes = new List<FieldDefinition>();
			var totalFields = 0;
			StringBuilder sb = new StringBuilder(1000);
            var statement = $"alter table [{catalog.Name}_{catalog.ID}] {value}";
            var single = type != AlterTableType.Alter ? statement : "";
            var multiple = type == AlterTableType.Alter ? statement : "";

            sb.AppendLine($"{single}");

			foreach (var f in fields)
			{
				if (f.Type == ColumnType.Set) sets.Add(f);
				else
				{
                    totalFields += 1;
                    sb.AppendLine(type != AlterTableType.Delete
                        ? $" {multiple} [{f.Name}] {GetDBType(f)} {GetIdentity(f)} {GetDBLength(f)} {GetNullable(f)} " +
                            $"{(type == AlterTableType.Add ? "," : ";")}" 
                        : $"[{f.Name}],");

                    if (f.IsUnique && !f.IsKey) indexes.Add(f);
					if (f.Type == ColumnType.Reference) refs.Add(f);
				}
			}

			if (totalFields > 0)
			{
				if (type == AlterTableType.Delete)
				{
					DeleteIndexes(catalog, indexes);
					DeleteReferenceFieldsForeignKeys(catalog, refs);
				}

                var lastIndex = type == AlterTableType.Alter ? ";" : ",";
				var query = sb.ToString().LastIndexOf(lastIndex) >= 0 ? sb.ToString().Substring(0, sb.ToString().LastIndexOf(lastIndex)) : sb.ToString();
				conn.ExecuteSQL(query);

				if (type == AlterTableType.Add)
				{
					CreateIndexes(catalog, indexes);
					CreateReferenceFieldsForeignKeys(catalog, refs);
				}
			}

            if (sets.Count > 0)
            {
                if (type == AlterTableType.Delete)
                    DeleteRelationTables(catalog, sets);
                if (type == AlterTableType.Add)
                    CreateRelationTables(catalog, sets);
            }
		}


		//Alter table: Rename column
		private void AlterTableRenameColumn(CatalogDefinition catalog, List<FieldDefinition> updatedFieldList, List<FieldDefinition> savedFieldList)
		{
			var sets = new List<FieldDefinition>();
			var refs = new List<FieldDefinition>();
			var indexes = new List<FieldDefinition>();
			var totalFields = 0;
			StringBuilder sb = new StringBuilder(1000);

			foreach (var field in updatedFieldList)
			{
				var savedField = savedFieldList.FirstOrDefault(x => Equals(x.FieldID, field.FieldID));

				if (savedField != null && !Equals(field.Name, savedField.Name))
				{
					if ((field.Type != ColumnType.Set && savedField.Type == ColumnType.Set) || (field.Type == ColumnType.Set && savedField.Type == ColumnType.Set)) sets.Add(field);
					else
					{
						totalFields += 1;
						if ((field.IsIndexed || field.IsUnique) && !field.IsKey) indexes.Add(field);
						if ((field.Type != ColumnType.Reference && savedField.Type == ColumnType.Reference) ||
							(field.Type == ColumnType.Reference && savedField.Type == ColumnType.Reference)) refs.Add(field);
						sb.AppendLine($"exec sp_rename '[{catalog.Name}_{catalog.ID}].{savedField.Name}', '{field.Name}', 'column';");
					}
				}
			}

			//RenameRelationTables(catalog, sets, savedFieldList);

			if (totalFields > 0)
			{
				//RenameIndexes(catalog, indexes, savedFieldList, newTableName);
				//RenameReferenceFieldsForeignKeys(catalog, refs, savedFieldList, newTableName);
				conn.ExecuteSQL(sb.ToString());
			}
		}

        //Alter table: et if column can be empty or not
        public void AlterTableNullableColumn(CatalogDefinition catalog, List<FieldDefinition> updatedFieldList, List<FieldDefinition> savedFieldList)
        {
            var totalFields = 0;
            StringBuilder sb = new StringBuilder(1000);
           

            foreach (var field in updatedFieldList)
            {
				
				var savedField = savedFieldList.FirstOrDefault(x => Equals(x.FieldID, field.FieldID));

                if (savedField != null && field.CanBeEmpty != savedField.CanBeEmpty)
                {
                    totalFields += 1;
					sb.AppendLine($"alter table [{catalog.Name}_{catalog.ID}]");
					sb.AppendLine($"alter column [{field.Name}] {GetDBType(field)} {GetIdentity(field)} {GetDBLength(field)} {GetNullable(field)};");
                }
            }

            if (totalFields > 0)
            {
                conn.ExecuteSQL(sb.ToString());
            }
        }

        //Alter Table: Called when updating a Catalog Field type
        public void AlterTableUpdateColumnType(CatalogDefinition catalog, FieldDefinition updatedField, FieldDefinition savedField)
        {
            var createSets = new List<FieldDefinition>();
            var deleteSets = new List<FieldDefinition>();
            var createRefs = new List<FieldDefinition>();
            var deleteRefs = new List<FieldDefinition>();
            var createIndexes = new List<FieldDefinition>();
            var deleteIndexes = new List<FieldDefinition>();
            var totalFields = 0;
            StringBuilder sb = new StringBuilder(1000);
            sb.AppendLine($"alter table [{catalog.Name}_{catalog.ID}]");

			if (!Equals(updatedField, null))
			{
				if (!Equals(savedField, null))
				{
					totalFields += 1;
					UpdateSets(updatedField, savedField, createSets, deleteSets, sb);
					if (updatedField.Type != ColumnType.Set && savedField.Type != ColumnType.Set || updatedField.Type == ColumnType.Reference && savedField.Type == ColumnType.Set ||
						updatedField.Type == ColumnType.Set && savedField.Type == ColumnType.Reference)
					{
						if (updatedField.Type != ColumnType.Set && savedField.Type != ColumnType.Set || updatedField.Type == ColumnType.Reference && savedField.Type == ColumnType.Set)
							sb.AppendLine($"alter column [{updatedField.Name}] {GetDBType(updatedField)} {GetIdentity(updatedField)} {GetDBLength(updatedField)} {GetNullable(updatedField)};");
						if (!updatedField.IsKey) UpdateIndexes(updatedField, savedField, createIndexes, deleteIndexes);
						UpdateRefs(updatedField, savedField, createRefs, deleteRefs);
					}
				}
			}

			DeleteRelationTables(catalog, deleteSets);

			if (totalFields > 0)
			{
				DeleteIndexes(catalog, deleteIndexes);
				DeleteReferenceFieldsForeignKeys(catalog, deleteRefs);

				conn.ExecuteSQL(sb.ToString());

				CreateIndexes(catalog, createIndexes);
				CreateReferenceFieldsForeignKeys(catalog, createRefs);
			}

			CreateRelationTables(catalog, createSets);
		}

		//Set Column to specified new value
		private void AlterColumnSetDefaultValue(CatalogDefinition catalog, FieldDefinition updatedField, string value)
		{
			StringBuilder sb = new StringBuilder(1000);
			sb.AppendLine($"alter table [{catalog.Name}_{catalog.ID}] alter column [{updatedField.Name}] varchar(100) null");
			sb.AppendLine($"update [{catalog.Name}_{catalog.ID}] set [{updatedField.Name}] = {value}");
			sb.AppendLine($"alter table [{catalog.Name}_{catalog.ID}] alter column [{updatedField.Name}] {GetDBType(updatedField)} {GetIdentity(updatedField)} {GetDBLength(updatedField)} {GetNullable(updatedField)}");
			conn.ExecuteSQL(sb.ToString());
		}

		//add reference column when updating from set
		private void AlterTableAddReferenceColumn(CatalogDefinition catalog, FieldDefinition updatedField)
		{
			StringBuilder sb = new StringBuilder(1000);
			sb.AppendLine($"alter table [{catalog.Name}_{catalog.ID}] add [{updatedField.Name}] {GetDBType(updatedField)} {GetIdentity(updatedField)} {GetDBLength(updatedField)} {GetNullable(updatedField)}");
			conn.ExecuteSQL(sb.ToString());
		}

		//Alter Table: When Set type is updated to another field type, remove relational table is needed
		private void DeleteRelationTables(CatalogDefinition left, List<FieldDefinition> sets)
		{
			foreach (var set in sets)
			{
				var right = conn.SelectOne<CatalogDefinition>(set.CatalogID);
				if (right == null) continue;
				var rel = new CatalogDefinition()
				{
					Name = $"REL_{left.ID}_{right.ID}_{set.FieldID}"
				};
				conn.ExecuteSQL($@"if exists(select * from sysobjects WHERE id = object_id (N'[dbo].[{rel.Name}]')) drop table [dbo].[{rel.Name}]");
			}
		}

        //Alter Table: Called when the Field Name is updated on a Set field
  //      private void RenameRelationTables(CatalogDefinition left, List<FieldDefinition> sets, List<FieldDefinition> savedFieldList)
  //      {
  //          foreach (var set in sets)
  //          {
  //              var right = conn.SelectOne<CatalogDefinition>(set.CatalogID);
  //              var savedField = savedFieldList.FirstOrDefault(x => Equals(x.FieldID, set.FieldID));

  //              if (savedField != null && set.Type == savedField.Type || (set.Type != ColumnType.Set && savedField.Type == ColumnType.Set))
  //              {
  //                  var oldName = $"REL_{left.Name}_{left.ID}_{right.Name}_{right.ID}";
  //                  var newName = $"REL_{set.Name}_{left.ID}_{right.Name}_{right.ID}";

  //                  conn.ExecuteSQL($@"
		//exec sp_rename '[PK_{oldName}_{savedField.Name}]', 'PK_{newName}_{set.Name}';
		//               exec sp_rename '[FK_{oldName}_{savedField.Name}_SourceID]', 'FK_{newName}_{set.Name}_SourceID';
		//               exec sp_rename '[FK_{oldName}_{savedField.Name}_TargetID]', 'FK_{newName}_{set.Name}_TargetID';
		//               exec sp_rename '[{oldName}_{savedField.Name}]', '{newName}_{set.Name}';");
  //              }
  //          }
  //      }

        //Alter Table: Called when enabling or disabling the IsUnique option
        private void DeleteIndexes(CatalogDefinition catalog, List<FieldDefinition> indexes)
		{
			foreach (var field in indexes)
			{
				if (field.IsUnique)
					conn.ExecuteSQL($@"drop index [IX_{field.Name}_{catalog.Name}_{catalog.ID}]	on [{catalog.Name}_{catalog.ID}]");
				else if (field.IsIndexed)
					conn.ExecuteSQL($@"drop index [IX_{field.Name}_{catalog.Name}_{catalog.ID}]	on [{catalog.Name}_{catalog.ID}]");
			}
		}

		////Alter Table: Called when the Field Name is updated on a field setted as IsUnique
		//private void RenameIndexes(CatalogDefinition left, List<FieldDefinition> indexes, List<FieldDefinition> savedFieldList, string newLeftTableName)
		//{
		//	foreach (var field in indexes)
		//	{
		//		var right = conn.SelectOne<CatalogDefinition>(field.CatalogID);
		//		var savedField = savedFieldList.FirstOrDefault(x => Equals(x.FieldID, field.FieldID));

		//		if (savedField != null)
		//		{
		//			conn.ExecuteSQL($@"
		//			exec sp_rename '[{left.Name}_{left.ID}].IX_{savedField.Name}_{left.Name}_{left.ID}',
		//                  'IX_{field.Name}_{newLeftTableName}_{left.ID}';");
		//		}
		//	}
		//}

		//Alter Table: Called when a Reference field is updated to another type
		private void DeleteReferenceFieldsForeignKeys(CatalogDefinition left, List<FieldDefinition> refs)
		{
			foreach (var field in refs)
			{
				var right = conn.SelectOne<CatalogDefinition>(field.CatalogID);
				conn.ExecuteSQL($@"
					alter table [{left.Name}_{left.ID}] drop constraint
					FK_{left.ID}_{right.ID}_{field.FieldID}");
			}
		}

		////Alter Table: Called when the Field Name is updated on a Reference field
		//private void RenameReferenceFieldsForeignKeys(CatalogDefinition left, List<FieldDefinition> refs, List<FieldDefinition> savedFieldList, string newLeftTableName)
		//{
		//	foreach (var field in refs)
		//	{
		//		var right = conn.SelectOne<CatalogDefinition>(field.CatalogID);
		//		var savedField = savedFieldList.FirstOrDefault(x => Equals(x.FieldID, field.FieldID));

		//		if (savedField != null)
		//		{
		//			conn.ExecuteSQL($@"
		//			exec sp_rename 'FK_{left.Name}_{left.ID}_{right.Name}_{right.ID}_{savedField.Name}',
		//                  'FK_{newLeftTableName}_{left.ID}_{right.Name}_{right.ID}_{field.Name}';");
		//		}
		//	}
		//}

		//Alter Table: get deleted and Created indexes on an alter table process
		private void UpdateIndexes(FieldDefinition updatedField, FieldDefinition savedField, List<FieldDefinition> createIndexes, List<FieldDefinition> deleteIndexes)
		{
			if (savedField.IsUnique && !updatedField.IsUnique)
				deleteIndexes.Add(updatedField);
			if (!savedField.IsUnique && updatedField.IsUnique)
				createIndexes.Add(updatedField);
		}

		//Alter Table: get deleted and Created References on an alter table process
		private void UpdateRefs(FieldDefinition updatedField, FieldDefinition savedField, List<FieldDefinition> createRefs, List<FieldDefinition> deleteRefs)
		{
			if (savedField.Type == ColumnType.Reference && updatedField.Type != ColumnType.Reference)
				deleteRefs.Add(savedField);
			if (savedField.Type != ColumnType.Reference && updatedField.Type == ColumnType.Reference)
				createRefs.Add(updatedField);
		}

		//Alter Table: get deleted and Created Sets on an alter table process
		private void UpdateSets(FieldDefinition updatedField, FieldDefinition savedField, List<FieldDefinition> createSets, List<FieldDefinition> deleteSets, StringBuilder sb)
		{
			if (savedField.Type == ColumnType.Set && updatedField.Type != ColumnType.Set)
			{
				deleteSets.Add(updatedField);
				if (updatedField.Type != ColumnType.Reference)
					sb.AppendLine($"add [{updatedField.Name}] {GetDBType(updatedField)} {GetIdentity(updatedField)} {GetDBLength(updatedField)} {GetNullable(updatedField)};");
			}
			if (savedField.Type != ColumnType.Set && updatedField.Type == ColumnType.Set)
			{
				createSets.Add(updatedField);
				sb.AppendLine($"drop column [{updatedField.Name}];");
			}
		}

		//Used to avoid losing data on a field when editing a Catalog
		private void UpdateFieldLength(FieldDefinition field, FieldDefinition savedField)
		{
			if (field.Length < savedField.Length)
				field.Length = savedField.Length;
		}

		//Alter Table: Support for Numeric options functionality
		private void MapOptions(CatalogDefinition savedCatalog, FieldDefinition field, FieldDefinition savedField, ColumnType fieldType)
		{
			var setDefault = false;

			switch (fieldType)
			{
				case ColumnType.Int:
					{
						if (savedField.Type == ColumnType.Long || savedField.Type == ColumnType.Decimal || savedField.Type == ColumnType.Reference || savedField.Type == ColumnType.Set)
							UpdateNumericColumn(savedCatalog, field, savedField, ref setDefault);
						break;
					}
				case ColumnType.Long:
					{
						if (savedField.Type == ColumnType.Int || savedField.Type == ColumnType.Decimal || savedField.Type == ColumnType.Reference || savedField.Type == ColumnType.Set)
							UpdateNumericColumn(savedCatalog, field, savedField, ref setDefault);
						break;
					}
				case ColumnType.Decimal:
					{
						if (savedField.Type == ColumnType.Int || savedField.Type == ColumnType.Long || savedField.Type == ColumnType.Reference || savedField.Type == ColumnType.Set)
							UpdateNumericColumn(savedCatalog, field, savedField, ref setDefault);
						break;
					}
				default:
					break;
			}

			if (!setDefault)
				AlterColumnSetDefaultValue(savedCatalog, field, "0");
		}

		//Called when updating a numeric field type
		private void UpdateNumericColumn(CatalogDefinition savedCatalog, FieldDefinition field, FieldDefinition savedField, ref bool setDefault)
		{
			setDefault = true;

            if ((field.Type == ColumnType.Int && savedField.Type == ColumnType.Decimal) || (field.Type == ColumnType.Int && savedField.Type == ColumnType.Long))
            {
                StringBuilder sb = new StringBuilder(1000);
                sb.AppendLine($"update [{savedCatalog.Name}_{savedCatalog.ID}] set [{field.Name}] = LEFT({field.Name}, 9)");
                //sb.AppendLine($"alter table [{savedCatalog.Name}_{savedCatalog.ID}] alter column [{field.Name}] {GetDBType(field)} {GetIdentity(field)} {GetDBLength(field)} {GetNullable(field)}");
                conn.ExecuteSQL(sb.ToString());
            }
            else
            {
                AlterTableUpdateColumnType(savedCatalog, field, savedField);
                if (savedField.Type == ColumnType.Reference)
                    AlterColumnSetDefaultValue(savedCatalog, field, "0");
            }
		}


		//Update Catalog: Called when processing fields update options, select the field by new type
		private void UpdateColumnByType(CatalogDefinition savedCatalog, List<FieldDefinition> updateFieldList, List<FieldDefinition> savedFieldList)
		{
			foreach (var field in updateFieldList)
			{
				var savedField = savedFieldList.FirstOrDefault(x => Equals(x.FieldID, field.FieldID));

				if (savedField != null && !Equals(field.Type, savedField.Type))
				{
					UpdateFieldLength(field, savedField);
					field.CanBeEmpty = true;
					switch (field.Type)
					{
						case ColumnType.Reference:
							{
								if (savedField.Type != ColumnType.Set)
									AlterColumnSetDefaultValue(savedCatalog, field, "NULL");
								else
									AlterTableAddReferenceColumn(savedCatalog, field);

								AlterTableUpdateColumnType(savedCatalog, field, savedField);
								break;
							}
						case ColumnType.Set:
							{
								AlterTableUpdateColumnType(savedCatalog, field, savedField);
								break;
							}
						case ColumnType.Int:
						case ColumnType.Long:
						case ColumnType.Decimal:
							{
								MapOptions(savedCatalog, field, savedField, field.Type);
								break;
							}
						case ColumnType.Bool:
							{
								if (savedField.Type == ColumnType.Set || savedField.Type == ColumnType.Reference)
									AlterTableUpdateColumnType(savedCatalog, field, savedField);
								else
									AlterColumnSetDefaultValue(savedCatalog, field, "'False'");
								break;
							}
						case ColumnType.Date:
							{
								if (savedField.Type == ColumnType.Set)
									AlterTableUpdateColumnType(savedCatalog, field, savedField);
								else 
									AlterColumnSetDefaultValue(savedCatalog, field, "NULL");
								break;
							}
						default://string
							{
								AlterTableUpdateColumnType(savedCatalog, field, savedField);
								if (savedField.Type == ColumnType.Reference || savedField.Type == ColumnType.Set)
									AlterColumnSetDefaultValue(savedCatalog, field, "NULL");
								break;
							}
					}
				}
			}
		}

        //Variable data remediation when Catalog is updated
        private void VariableDataRemediation(CatalogDefinition savedCatalog, List<FieldDefinition> updateFieldList, List<FieldDefinition> savedFieldList)
        {
            foreach (var field in updateFieldList)
            {
                var savedField = savedFieldList.FirstOrDefault(x => Equals(x.FieldID, field.FieldID));

                if (savedField != null && Equals(field.Type, savedField.Type))
                {
                    switch (field.Type)
                    {
                        case ColumnType.String:
                            {
                                if (field.Length < savedField.Length)
                                {
                                    conn.ExecuteSQL($@"
				                        update [{savedCatalog.Name}_{savedCatalog.ID}]
				                        set {field.Name} = substring( {field.Name}, 1, {field.Length} )");
                                }
    
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }
                }
            }
        }

        enum AlterTableType
		{
			Add = 1,
			Delete = 2,
            Alter
		}
	}
}
