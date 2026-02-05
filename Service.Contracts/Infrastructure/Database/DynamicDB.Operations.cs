using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.Database
{
    partial class DynamicDB
	{
		public int GetRowCount(int catalogid)
		{
			var cat = GetCatalog(catalogid);
			return Convert.ToInt32(conn.ExecuteScalar($"select count(*) as RowCounter from {cat.Name}_{cat.ID}"));
		}


		public string Insert(int catalogid, string json, bool allowIdentityInsert = false)
		{
			var cat = GetCatalog(catalogid);
			var o = JObject.Parse(json);
			Insert(cat, o, allowIdentityInsert);
			return o.ToString();
		}


		public int Insert(CatalogDefinition cat, string json, bool allowIdentityInsert = false)
		{
			var o = JObject.Parse(json);
			return Insert(cat, o, allowIdentityInsert);
		}

		public int Insert(int catalogid, JObject o, bool allowIdentityInsert = false)
		{
			var cat = GetCatalog(catalogid);
			return Insert(cat, o, allowIdentityInsert);
		}


		public int Insert(CatalogDefinition cat, JObject o, bool allowIdentityInsert = false)
		{
			ValidateData(cat, o);
			var e = new DynamicDBBeforeInsert(cat.ID, o);
			events.InvokeSubscribers(e);
			if (e.CancelEvent)
				throw new Exception("Operation cancelled by Plugin");
			int id = Convert.ToInt32(InsertOperation(cat, o, allowIdentityInsert));
			o["ID"] = id;
			return id;
		}


		private object InsertOperation(CatalogDefinition cat, JObject data, bool allowIdentityInsert = false)
		{
			object id = null;
			List<object> args = new List<object>();
			var identity = cat.Fields.FirstOrDefault(p => p.IsIdentity);
			int identityValue = 0;
			var ins = new StringBuilder(1000);
			var vals = new StringBuilder(1000);
			var sets = new List<DBSetInfo>();
			ins.Append($"insert into {cat.Name}_{cat.ID}(");
			foreach (var f in cat.Fields)
			{
				if (f.Type == ColumnType.Set)
				{
					sets.Add(new DBSetInfo(f, data[f.Name]));
				}
				else
				{
					var p = data.Property(f.Name);
					if (p != null)
					{
						var ptype = GetPropertyType(p.Value.Type);
						if (identity != null && identity.Name == f.Name)
						{
							if (!allowIdentityInsert)
								continue;
							identityValue = p.Value.ToObject<int>();
						}

						ins.Append(f.Name).Append(',');
						vals.Append('@').Append(f.Name.ToLower()).Append(',');
						if (ptype == null)
							args.Add(null);
						else
							args.Add(p.Value.ToObject(ptype));
					}
				}
			}

			if (ins.Length > 0)
			{
				ins.Remove(ins.Length - 1, 1);
				vals.Remove(vals.Length - 1, 1);
				ins.AppendLine($") values({vals.ToString()})");

				if (identity != null)
				{
					if (allowIdentityInsert)
					{
						ins.Insert(0, $"SET IDENTITY_INSERT {cat.Name}_{cat.ID} ON\r\n");
						ins.AppendLine($"select {identityValue} as RowID");
						ins.AppendLine($"SET IDENTITY_INSERT {cat.Name}_{cat.ID} OFF");
					}
					else
					{
						ins.AppendLine(" select Scope_Identity() as RowID");
					}
				}

				//Executes the statement and checks if there is anything to return.
				IDataReader rd = conn.ExecuteReader(ins.ToString(), args.ToArray());
				try
				{
					if (identity != null)
					{
						if (rd.Read() && rd.FieldCount > 0)
						{
							id = rd[0];
							data[identity.Name] = Convert.ToInt32(id);
						}
					}
				}
				finally
				{
					rd.Close();
				}
			}
			foreach (var s in sets)
				InsertIntoRel(cat, data.GetValue<int>("ID"), s.Field, s.SetData);
			return id;
		}

		private Type GetPropertyType(JTokenType type)
		{
			switch (type)
			{
				case JTokenType.Boolean: return typeof(bool);
				case JTokenType.Date: return typeof(DateTime);
				case JTokenType.Float: return typeof(float);
				case JTokenType.Integer: return typeof(int);
				case JTokenType.Null: return null;
				default: return typeof(string);
			}
		}


		public void InsertIntoRel(CatalogDefinition cat, int recid, FieldDefinition field, JToken setData)
		{
			if (setData == null) return;
			var ids = setData.Value<string>();
			if (String.IsNullOrWhiteSpace(ids)) return;
			var right = conn.SelectOne<CatalogDefinition>("select * from _Catalog_ where ID = @catid", field.CatalogID);
			var relName = $"REL_{cat.ID}_{right.ID}_{field.FieldID}";
			conn.ExecuteNonQuery($"delete from [{relName}] where SourceID = @recid", recid);
			var insertStatement = $"insert into [{relName}] values(@source, @target)";
			var idArray = ids.Split(',');
			foreach (var id in idArray)
			{
				var targetid = Convert.ToInt32(id);
				if (targetid != 0)
					conn.ExecuteNonQuery(insertStatement, recid, targetid);
			}
		}


		public int Insert(CatalogDefinition cat, RowDataSpec data)
		{
			var o = data.ToJsonObject();
			ValidateData(cat, o);
			var e = new DynamicDBBeforeInsert(cat.ID, o);
			events.InvokeSubscribers(e);
			if (e.CancelEvent)
				throw new Exception("Operation cancelled by Plugin");
			else
				data.FromJsonObject(e.Data);

			object id = null;
			List<object> args = new List<object>();
			var identity = cat.Fields.FirstOrDefault(p => p.IsIdentity);
			var ins = new StringBuilder(1000);
			var vals = new StringBuilder(1000);
			var sets = new List<DBSetInfo>();
			ins.Append($"insert into [{cat.Name}_{cat.ID}](");
			foreach (var c in data)
			{
				if (identity != null && String.Compare(identity.Name, c.ColumnName, true) == 0) continue;
				var fieldDef = cat.Fields.First(p => String.Compare(p.Name, c.ColumnName, true) == 0);
				if (fieldDef.Type == ColumnType.Set) continue;
				ins.Append(c.ColumnName).Append(',');
				vals.Append('@').Append(c.ColumnName).Append(',');
				args.Add(c.ColumnValue);
			}
			ins.Remove(ins.Length - 1, 1);
			vals.Remove(vals.Length - 1, 1);
			ins.Append(") values(").Append(vals.ToString()).Append(") ");
			ins.Append("select Scope_Identity() as RowID");
			IDataReader rd = conn.ExecuteReader(ins.ToString(), args.ToArray());
			try
			{
				if (rd.Read() && rd.FieldCount > 0)
				{
					id = rd[0];
				}
			}
			catch(Exception ex)
			{
				throw new Exception($"Error while inserting record: {ex.Message}. Statement: {ins.ToString()} {args.Print()}", ex);
			}
			finally
			{
				rd.Close();
			}
			return Convert.ToInt32(id);
		}


		public void InsertRel(int leftcatalogid, int rightcatalogid, int fieldId, int leftID, int rightID)
		{
			var left = GetCatalog(leftcatalogid);
			var right = GetCatalog(rightcatalogid);
			conn.ExecuteNonQuery($"insert into [REL_{left.ID}_{right.ID}_{fieldId}] values(@left, @right)", leftID, rightID);
		}

		public void InsertRel(CatalogDefinition left, CatalogDefinition right, string fieldName, int leftID, int rightID)
		{
			var field = left.Fields.FirstOrDefault(f => f.Name == fieldName);
			if (field == null) throw new Exception($"Field {fieldName} dest not exits.");
			conn.ExecuteNonQuery($"insert into [REL_{left.ID}_{right.ID}_{field.FieldID}] values(@left, @right)", leftID, rightID);
		}

		public void InsertRel(CatalogDefinition left, CatalogDefinition right, int fieldId, int leftID, int rightID)
		{
			conn.ExecuteNonQuery($"insert into [REL_{left.ID}_{right.ID}_{fieldId}] values(@left, @right)", leftID, rightID);
		}

		public void Update(int catalogid, string json)
		{
			var cat = GetCatalog(catalogid);
			var o = JObject.Parse(json);
			Update(cat, o);
		}


		public void Update(int catalogid, JObject o)
		{
			var cat = GetCatalog(catalogid);
			Update(cat, o);
		}


		public void Update(CatalogDefinition cat, JObject o)
		{
			ValidateData(cat, o);
			var e = new DynamicDBBeforeUpdate(cat.ID, o);
			events.InvokeSubscribers(e);
			if (e.CancelEvent)
				throw new Exception("Operation cancelled by Plugin");
			UpdateOperation(cat, o);
		}


		public int UpdateOperation(CatalogDefinition cat, JObject data)
		{
			List<object> args = new List<object>();
			var sets = new List<DBSetInfo>();
			var identity = cat.Fields.FirstOrDefault(p => p.IsIdentity);
			if (identity == null)
				throw new Exception("Cannot use this Update if table has no identity column.");
			else if (data[identity.Name] == null)
				throw new Exception("Value for the identity column was not supplied.");
			var statement = new StringBuilder(1000);
			statement.Append($"update {cat.Name}_{cat.ID} set ");
			foreach (var f in cat.Fields)
			{
				if (identity != null && identity.Name == f.Name) continue;
				if (f.Type == ColumnType.Set)
				{
					sets.Add(new DBSetInfo(f, data[f.Name]));
				}
				else
				{
					var p = data.Property(f.Name);
					if (p != null)
					{
						statement.Append(f.Name).Append("=@").Append(f.Name.ToLower()).Append(',');
						var ptype = GetPropertyType(p.Value.Type);
						if (ptype == null)
							args.Add(null);
						else
							args.Add(p.Value.ToObject(ptype));
					}
				}
			}
			statement.Remove(statement.Length - 1, 1);
			statement.Append(" where ");
			statement.Append($"{identity.Name} = @_identity_");
			args.Add(data[identity.Name].ToObject(typeof(int)));
			var recordCount = conn.ExecuteNonQuery(statement.ToString(), args.ToArray());
			foreach (var s in sets)
				InsertIntoRel(cat, data.GetValue<int>("ID"), s.Field, s.SetData);
			return recordCount;
		}


		public int Update(CatalogDefinition cat, int id, RowDataSpec data)
		{
			var statement = new StringBuilder(1000);
			try
			{
				JObject o = data.ToJsonObject();
				ValidateData(cat, o);
				var e = new DynamicDBBeforeUpdate(cat.ID, o);
				events.InvokeSubscribers(e);
				if (e.CancelEvent)
					throw new Exception("Operation cancelled by Plugin");
				else
					data.FromJsonObject(e.ProposedData);

				var args = new List<object>();
				statement.Append($"update [{cat.Name}_{cat.ID}] set ");
				foreach (var c in data)
				{
					if (c.ForLookup) continue;
					if (c.ColumnType == ColumnType.Set) continue;
					statement.Append(c.ColumnName).Append("=@").Append(c.ColumnName).Append(',');
				}
				statement.Remove(statement.Length - 1, 1);
				statement.Append(" where ID = @id");
				args.AddRange(data.GetValues());
				args.Add(id);
				var recordCount = conn.ExecuteNonQuery(statement.ToString(), args.ToArray());
				return recordCount;
			}
			catch (Exception ex)
			{
				var log = factory.GetInstance<ILogService>();
				log.LogException($"DynamicDB error while executing: {statement.ToString()}", ex);
				throw;
			}
		}


		public void Delete(int catalogid, int id, CatalogDefinition parentCatalog,int? parentId = null)
		{
			var cat = GetCatalog(catalogid);
			Delete(cat, id, parentCatalog, parentId);
		}


		public void Delete(CatalogDefinition cat, int id, CatalogDefinition parentCatalog, int? parentId = null)
		{
			var data = conn.SelectOneToJson($"select * from {cat.Name}_{cat.ID} where id = @id", id);

            if (data != null)   
			{
				var e = new DynamicDBBeforeDelete(cat.ID, data);
				events.InvokeSubscribers(e);
				if (e.CancelEvent)
					throw new Exception("Operation cancelled by Plugin");

				// Delete set elements in the "related" table of each set
				var sets = cat.Fields.Where(f => f.Type == ColumnType.Set);
				foreach (var setField in sets)
				{
					var right = GetCatalog(setField.CatalogID.Value);
					conn.ExecuteNonQuery($@"
	                        select r.TargetID into #tmpt from [{right.Name}_{right.ID}] t
	                        join [REL_{cat.ID}_{right.ID}_{setField.FieldID}] r
	                        on t.ID = r.TargetID
	                        where r.SourceID = @id

							delete from [REL_{cat.ID}_{right.ID}_{setField.FieldID}]								
							where SourceID = @id;

                            --delete from [{right.Name}_{right.ID}]
                            --where ID in (select TargetID from #tmpt)
                            
                            IF OBJECT_ID('tempdb.dbo.#tmpt', 'U') IS NOT NULL
                            drop table #tmpt; ", id);
                }

                //remove rel data if any
                if (parentCatalog != null)
                {
                    var setField = parentCatalog.Fields.FirstOrDefault(x => x.Type == ColumnType.Set && x.CatalogID == cat.ID);
                    conn.ExecuteNonQuery($@"
							delete r from 
								[REL_{parentCatalog.ID}_{cat.ID}_{setField.FieldID}] r
								where r.TargetID = @id and r.SourceID = @idparent", id, parentId);
                }
                else
                {
                    conn.ExecuteNonQuery($"delete from {cat.Name}_{cat.ID} where id = @id", id);
                }

                // Support for image/file types requires rework, it needs to be integrated with IFileStoreManager and use FileGUIDs to reference system files.
                //var fileFields = cat.Fields.Where(f => f.Type == ColumnType.Image || f.Type == ColumnType.File);
                //foreach (var fileField in fileFields)
                //{
                //	var fileid = data.GetValue<int>(fileField.Name);
                //	if (fileid != 0)
                //	{
                //		fileStore.DeleteFile(fileid);
                //		conn.ExecuteNonQuery("delete from _Files_ where ID = @fileid", fileid);
                //	}
                //}
            }
		}


		public async Task RecursiveDeleteAsync(List<CatalogDefinition> allCatalogs, CatalogDefinition targetCatalog, string query, params object[] arguments)
		{
			var deleteInfo = new RecuriveDeteleInfo();
			deleteInfo.VisitedCatalogs.Add(targetCatalog);

			query = query.Replace("#TABLE", $"[{targetCatalog.Name}_{targetCatalog.ID}]");

			deleteInfo.Commands.Add($@"
				declare @tmp1 CatReference
				insert into @tmp1 {query}
			");

			RecursiveDeleteCatalog(allCatalogs, targetCatalog, "@tmp1", deleteInfo);

			deleteInfo.Commands.Add($@"
				delete t1 from {targetCatalog.Name}_{targetCatalog.ID} t1 join @tmp1 t2 on t1.ID = t2.ID
			");


			StringBuilder sb = new StringBuilder(10000);
			foreach(var cmd in deleteInfo.Commands)
				sb.AppendLine(cmd);

			var script = sb.ToString();
			var log = factory.GetInstance<ILogService>().GetSection("DynamicDB");
			try
			{
				Conn.ExecuteNonQuery(@"
					if TYPE_ID(N'CatReference') is null
						create type CatReference as table (ID int NOT NULL index IX_ID)
				");

				await Conn.ExecuteNonQueryAsync(script, arguments);
			}
			catch(Exception ex)
			{
				log.LogMessage("----------------------------------------------------\r\nDynamicDB.RecursiveDelete executed the following SQL Script:\r\n{0}", script);
				log.LogException(ex);  // Logs the error in the DynamicDB section, then rethrows
				throw;
			}
		}


		private void RecursiveDeleteCatalog(List<CatalogDefinition> allCatalogs, CatalogDefinition cat, string tempTable, RecuriveDeteleInfo deleteInfo)
		{
			var references = cat.Fields.Where(f => f.Type == ColumnType.Reference);

			// Delete from reference fields
			foreach (var reference in references)
			{
				var right = allCatalogs.First(c => c.ID == reference.CatalogID.Value);
                if(right.CatalogType == CatalogType.Lookup || deleteInfo.VisitedCatalogs.Contains(right))
                    continue;// already add

                deleteInfo.VisitedCatalogs.Add(right);

				var refTable = $"@tmp{right.Name}_{reference.FieldID}";

				deleteInfo.Commands.Add($@"
					declare {refTable} CatReference

					insert into {refTable} select t1.ID from [{right.Name}_{right.ID}] t1
						join [{cat.Name}_{cat.ID}] t2 on t1.ID = t2.{reference.Name}
						join {tempTable} t3 on t2.ID = t3.ID
				");

				RecursiveDeleteCatalog(allCatalogs, right, refTable, deleteInfo);

				deleteInfo.Commands.Add($@"
					delete t1 from {right.Name}_{right.ID} t1 join {refTable} t2 on t1.ID = t2.ID
				");
				
			}

			// Delete from set fields
			var sets = cat.Fields.Where(f => f.Type == ColumnType.Set);
			foreach (var setField in sets)
			{
				var right = allCatalogs.First(c => c.ID == setField.CatalogID.Value);

                if(deleteInfo.VisitedCatalogs.Contains(right))
                    continue;// already add

				deleteInfo.VisitedCatalogs.Add(right);

				var refTable = $"@tmp{right.Name}_{setField.FieldID}";

				deleteInfo.Commands.Add($@"
					declare {refTable} CatReference

					insert into {refTable} select t1.ID from [{right.Name}_{right.ID}] t1
						join [REL_{cat.ID}_{right.ID}_{setField.FieldID}] t2 on t1.ID = t2.TargetID
						join {tempTable} t3 on t2.SourceID = t3.ID
				");

				deleteInfo.Commands.Add($@"
					delete t1 from [REL_{cat.ID}_{right.ID}_{setField.FieldID}] t1 join {tempTable} t2 on t1.SourceID = t2.ID
				");

				deleteInfo.Commands.Add($@"
					delete t1 from {right.Name}_{right.ID} t1 join {refTable} t2 on t1.ID = t2.ID
				");

				RecursiveDeleteCatalog(allCatalogs, right, refTable, deleteInfo);
			}

			var referencingCatalogs = allCatalogs.Where(c => c.Fields.Where(f => f.Type == ColumnType.Reference && f.CatalogID == cat.ID).Count() > 0).ToList();
			foreach (var refCatalog in referencingCatalogs)
			{
				if (deleteInfo.VisitedCatalogs.FirstOrDefault(c => c.ID == refCatalog.ID) == null)
				{
					deleteInfo.VisitedCatalogs.Add(refCatalog);

					RecursiveDeleteCatalog(allCatalogs, refCatalog, tempTable, deleteInfo);

					deleteInfo.Commands.Add($@"
						delete t1 from [{refCatalog.Name}_{refCatalog.ID}] t1 join {tempTable} t2 on t1.ID = t2.ID
					");
				}
			}
		}


		class RecuriveDeteleInfo
		{
			public List<string> Commands = new List<string>();
			public List<CatalogDefinition> VisitedCatalogs = new List<CatalogDefinition>();
		}


		public void DeleteAll(int catalogid)
		{
			var cat = GetCatalog(catalogid);
			var setFields = cat.Fields.Where(f => f.Type == ColumnType.Set).ToList();
			var data = conn.SelectToJson($"select * from {cat.Name}_{cat.ID}");
			if (data != null)
			{
				foreach (JObject item in data)
				{
					var rowid = item.GetValue<int>("ID");
					Delete(cat, rowid, null);
				}
			}
		}


		public string GetPage(int catalogid, int pagenum, int pagesize, string orderby)
		{
			throw new NotImplementedException();
		}


		public JObject SelectOne(int catalogid, int id)
		{
			var select = CreateSelectStatement(catalogid);
			return conn.SelectOneToJson($"{select} where a.ID = @id", id);
		}


		public JObject SelectOne(int catalogid, string query, params object[] args)
		{
			var cat = GetCatalog(catalogid);
			if (cat == null)
				return new JObject();
			var select = query.Replace("#TABLE", $"[{cat.Name}_{cat.ID}]");
			return conn.SelectOneToJson(select, args);
		}

		public JObject SelectOne(CatalogDefinition cat, string query, params object[] args)
		{
			var select = query.Replace("#TABLE", $"[{cat.Name}_{cat.ID}]");
			return conn.SelectOneToJson(select, args);
		}


		public int? Lookup(CatalogDefinition catalog, RowDataSpec data)
		{
			if (data.HasValue)
			{
				var qry = $"select ID from [{catalog.Name}_{catalog.ID}] where {data.GetFilter()}";
				var id = conn.ExecuteScalar(qry, data.GetFilterValues());
				if (id == null) return 0;
				return Convert.ToInt32(id);
			}
			return null;
		}


		public JArray Select(int catalogid)
		{
			var select = CreateSelectStatement(catalogid);
			return conn.SelectToJson(select);
		}



        public JArray Select (int catalogid, int pageNumber, int pageSize)
        {
            var select = CreateSelectStatement(catalogid, pageNumber, pageSize);
            return conn.SelectToJson(select); 
        }

		public JArray Select(int catalogid, string query, params object[] args)
		{
			var cat = GetCatalog(catalogid);
			var select = query.Replace("#TABLE", $"[{cat.Name}_{cat.ID}]");
			return conn.SelectToJson(select, args);
		}


		public JArray Select(CatalogDefinition cat, string query, params object[] args)
		{
			var select = query.Replace("#TABLE", $"[{cat.Name}_{cat.ID}]");
			return conn.SelectToJson(select, args);
		}


		public int GetMaxID(int catalogid)
		{
			var cat = GetCatalog(catalogid);
			var select = $"select top 1 ID from [{cat.Name}_{cat.ID}] order by ID desc";
			var result = conn.ExecuteScalar(select);
			if (result != null)
				return Convert.ToInt32(result);
			else
				return 1;
		}


		public JArray FreeTextSearch(int catalogid, params TextSearchFilter[] filter)
		{
			var cat = GetCatalog(catalogid);
			CreateFreeTextSearch(cat, filter, "", out var where, out var values);
			return conn.SelectToJson($"select * from {cat.Name}_{cat.ID} where {where}", values.ToArray());
		}

        public int FreeTextSearchCount(int catalgoid, params TextSearchFilter[] filter)
        {
            var cat = GetCatalog(catalgoid);
            CreateFreeTextSearch(cat, filter, "", out var where, out var values);
            return Convert.ToInt32(conn.ExecuteScalar($"select count(*) from {cat.Name}_{cat.ID} where {where}", values.ToArray())); 
        }

        public JArray FreeTextSearch(int catalogid, int pageNumber, int pageSize, params TextSearchFilter[] filter)
        {
            var cat = GetCatalog(catalogid);
            CreateFreeTextSearch(cat, filter, "", out var where, out var values);
            return conn.SelectToJson($"select * from {cat.Name}_{cat.ID} where {where} order by id  offset {pageSize * (pageNumber)} rows fetch next {pageSize} rows only ", values.ToArray());
        }

		public JArray SubsetFreeTextSearch(int catalogid, int id, string fieldName, params TextSearchFilter[] filter)
		{
			var cat = GetCatalog(catalogid);
			if (CreateSubsetSelect(cat, fieldName, out var setCatalog, out var select))
			{
				CreateFreeTextSearch(setCatalog, filter, "b.", out var where, out var values);
				values.Insert(0, id);
				return conn.SelectToJson($@"{select} where a.SourceID = @id and {where}", values.ToArray());
			}
			return null;
		}


		public JArray GetSubset(int catalogid, int id, string fieldName)
		{
			var cat = GetCatalog(catalogid);
			if (CreateSubsetSelect(cat, fieldName, out _, out var select))
				return conn.SelectToJson($@"{select} where a.SourceID = @id", id);
			return null;
		}

        public JArray GetFullSubset(int catalogid, string fieldName)
        {
            var cat = GetCatalog(catalogid);
            if (CreateFullSubsetSelect(cat, fieldName, out _, out var select))
                return conn.SelectToJson($@"{select} ");
            return null;
        }


        public JArray SearchMultiple(int catalogid, string fieldName, List<string> values)
		{
			if (values == null || values.Count == 0)
				throw new Exception("values argument cannot be null or empty");
			var cat = GetCatalog(catalogid);
			var field = cat.Fields.FirstOrDefault(p => p.Name == fieldName);
			if (field != null)
			{
				int i = 1;
				StringBuilder vals = new StringBuilder();
				foreach (var v in values)
					vals.Append($"@val_{i++},");
				vals.Remove(vals.Length - 1, 1);
				return conn.SelectToJson($@"select * from [{cat.Name}_{cat.ID}] where {fieldName} in ({vals.ToString()})", values.ToArray());
			}
			else throw new Exception($"Field {fieldName} is not defined.");
		}


		private void CreateFreeTextSearch(CatalogDefinition cat, TextSearchFilter[] filter, string alias, out string where, out List<object> values)
		{
			values = new List<object>();
			StringBuilder sb = new StringBuilder(1000);
			foreach (var f in filter)
			{
				var field = cat.Fields.FirstOrDefault(p => p.Name == f.FieldName);
				if (field != null)
				{
					values.Add(f.Value);
					switch (f.SearchType)
					{
						case TextSearchFilterType.Contains:
							sb.Append($"CHARINDEX(@value{values.Count}, {alias}{field.Name}) > 0 and");
							break;
						default:
							sb.Append($"{alias}{field.Name} = @value{values.Count} and");
							break;
					}
				}
			}
			if (sb.Length > 0)
				sb.Remove(sb.Length - 3, 3);
			where = sb.ToString();
		}


		private bool CreateSubsetSelect(CatalogDefinition cat, string fieldName, out CatalogDefinition set, out string select)
		{
			int i = 1;
			select = null;
			set = null;
			var field = cat.Fields.FirstOrDefault(p => p.Name == fieldName);
			if (field != null)
			{
				set = GetCatalog(field.CatalogID.Value);
				var RefNames = new StringBuilder(100);
				var RefJoins = new StringBuilder(100);
				foreach (var f in set.Fields)
				{
					if (f.Type == ColumnType.Reference)
					{
						var alias = $"T{i++}";
						var rcat = GetCatalog(f.CatalogID.Value);
						var displayField = GetDisplayField(rcat);
						RefNames.Append($", [{alias}].{displayField.Name} as _{f.Name}_DISP ");
						RefJoins.Append($" left outer join {rcat.Name}_{rcat.ID} as {alias} on b.{f.Name} = {alias}.ID  ");
					}
				}
				select = $@"select b.* {RefNames.ToString()} from [REL_{cat.ID}_{set.ID}_{field.FieldID}] a 
						join [{set.Name}_{set.ID}] b on a.TargetID = b.ID
						{RefJoins.ToString()}";
				return true;
			}
			return false;
		}

        private bool CreateFullSubsetSelect(CatalogDefinition cat, string fieldName, out CatalogDefinition set, out string select)
        {
            int i = 1;
            select = null;
            set = null;
            var field = cat.Fields.FirstOrDefault(p => p.Name == fieldName);
            if (field != null)
            {
                set = GetCatalog(field.CatalogID.Value);
                var RefNames = new StringBuilder(100);
                var RefJoins = new StringBuilder(100);
                foreach (var f in set.Fields)
                {
                    if (f.Type == ColumnType.Reference)
                    {
                        var alias = $"T{i++}";
                        var rcat = GetCatalog(f.CatalogID.Value);
                        var displayField = GetDisplayField(rcat);
                        RefNames.Append($", [{alias}].{displayField.Name} as _{f.Name}_DISP ");
                        RefJoins.Append($" left outer join {rcat.Name}_{rcat.ID} as {alias} on b.{f.Name} = {alias}.ID  ");
                    }
                }
                select = $@"select b.*, a.* {RefNames.ToString()} from [REL_{cat.ID}_{set.ID}_{field.FieldID}] a 
						join [{set.Name}_{set.ID}] b on a.TargetID = b.ID
						{RefJoins.ToString()}";
                return true;
            }
            return false;
        }


        public List<TableData> ExportData(int catalogid, bool recursive, params NameValuePair[] filter)
		{
			var root = GetCatalog(catalogid);
			return ExportData(root, recursive, filter);
		}


		public List<TableData> ExportData(CatalogDefinition root, bool recursive, params NameValuePair[] filter)
		{
			var args = new List<object>();
			var sb = new StringBuilder(100);
			JArray rows;

			// Retrieve the data of the root catalog
			foreach (var v in filter)
			{
				sb.Append($" [{v.Name}] = @p{v.Name} and");
				args.Add(v.Value);
			}
			if (sb.Length > 0)
			{
				sb.Remove(sb.Length - 3, 3);
				rows = Select(root, $"select top 1 * from #TABLE where {sb.ToString()}", args.ToArray());
			}
			else
			{
				rows = Select(root, $"select top 1 * from #TABLE");
			}

			var result = new List<TableData>();
			
			var referencedTables = new Dictionary<int, ExportReferenceInfo>();
			ExportCatalog(root, rows, referencedTables, recursive);
			foreach (var elm in referencedTables)
			{
				result.Add(new TableData()
				{
					CatalogID = elm.Value.Table.ID,
					Name = elm.Value.Table.Name,
					Fields = elm.Value.Table.Definition,
					CatalogType = elm.Value.Table.CatalogType,
					Records = elm.Value.Data.ToString(),
				});
			}

			return result;
		}


		class ExportReferenceInfo
		{
			public CatalogDefinition Table;
			public Dictionary<int, bool> RowIDs = new Dictionary<int, bool>();
			public JArray Data = new JArray();
		}

		private void ExportCatalog(CatalogDefinition cat, JArray rows, Dictionary<int, ExportReferenceInfo> result, bool recursive)
		{
			// If not already present, then add the specified catalog to the results.
			if (!result.TryGetValue(cat.ID, out var catalog))
			{
				catalog = new ExportReferenceInfo();
				catalog.Table = cat;
				result.Add(cat.ID, catalog);
			}

			// Add the row data (skip rows already exported)
			foreach (var row in rows)
			{
				var id = (row as JObject).GetValue<int>("ID");
				if (!catalog.RowIDs.ContainsKey(id))
				{
					catalog.RowIDs.Add(id, true);
					catalog.Data.Add(row);
				}
			}

			if (recursive)
			{
				ExportReferences(cat, rows, result);
				ExportSets(cat, rows, result);
			}
		}

		private void ExportReferences(CatalogDefinition cat, JArray rows, Dictionary<int, ExportReferenceInfo> result)
		{
			// Process any "Reference" fields
			foreach (var field in cat.Fields)
			{
				// Look for reference fields
				if (field.Type == ColumnType.Reference)
				{
					// Get the catalog definition from the results, or if not present, add it...
					if (!result.TryGetValue(field.CatalogID.Value, out var info))
					{
						var referencedCatalog = GetCatalog(field.CatalogID.Value);
						info = new ExportReferenceInfo() { Table = referencedCatalog };
						result.Add(field.CatalogID.Value, info);
					}

					// Add the IDs being referenced to the results (checking if they have not already been added before)
					foreach (var row in rows)
					{
						var jo = row as JObject;
						var rowid = jo.GetValue<int?>(field.Name);
						if (rowid.HasValue)
						{
							if (!info.RowIDs.ContainsKey(rowid.Value))
								info.RowIDs.Add(rowid.Value, false);
						}
					}

					// Materialize the rows from the referenced catalog
					var nonVisitedRowIDs = (from r in info.RowIDs where r.Value == false select r.Key).Merge(",");
					if (nonVisitedRowIDs.Length > 0)
					{
						// Mark all rows as visited. This is to avoid querying the same table for the same rows multiple times (as can happen when a catalog references the same table from multiple columns, or when the same table is referenced from multiple other related tables).
						foreach (var key in info.RowIDs.Keys.ToArray())
							info.RowIDs[key] = true;

						// Retrieve the rows that had not been visited yet and recursively export the data of any other referenced tables
						var referencedRows = Select(info.Table, $"select * from #TABLE where ID in ({nonVisitedRowIDs})");
						info.Data.Append(referencedRows);
					}
					ExportCatalog(info.Table, info.Data, result, true);
				}
			}
		}

		public int relid = -1;

		private void ExportSets(CatalogDefinition leftCatalog, JArray rows, Dictionary<int, ExportReferenceInfo> result)
		{
			CatalogDefinition rightCatalog;
			
			foreach (var field in leftCatalog.Fields)
			{
				// Look for set fields
				if (field.Type == ColumnType.Set)
				{
					// Get definition of the right side catalog from results, or if not present, add it...
					if (!result.TryGetValue(field.CatalogID.Value, out var info))
					{
						rightCatalog = GetCatalog(field.CatalogID.Value);
						result.Add(field.CatalogID.Value, new ExportReferenceInfo() { Table = rightCatalog });
					}
					else rightCatalog = info.Table;

					// Get definition of the REL table from result, or if not present, add it...
					var relTableName = $"REL_{leftCatalog.ID}_{rightCatalog.ID}_{field.FieldID}";
					var relFriendlyName = $"REL_{leftCatalog.ID}_{rightCatalog.ID}_{field.FieldID}";
					var relInfo = result.Values.FirstOrDefault(p => p.Table.Name == relFriendlyName);
					var fields = new List<FieldDefinition>(){
						new FieldDefinition() { Name = "SourceID", IsKey = true, CanBeEmpty = false, Type = ColumnType.Int },
						new FieldDefinition() { Name = "TargetID", IsKey = true, CanBeEmpty = false, Type = ColumnType.Int }
					};
					if (relInfo == null)
					{
						var relTable = new CatalogDefinition()
						{
							ID = relid,
							Name = relFriendlyName,
							Definition = JsonConvert.SerializeObject(fields)
						};
						relInfo = new ExportReferenceInfo() { Table = relTable };
						result.Add(relid--, relInfo);
					}

					// Get the IDs for the set
					var leftCatalogIDs = new List<int>(100);
					foreach (var row in rows)
					{
						var jo = row as JObject;
						leftCatalogIDs.Add(jo.GetValue<int>("ID"));
					}
					if (leftCatalogIDs.Count > 0)
					{
						var leftIds = leftCatalogIDs.Merge(",");
						var qry = $"select SourceID, TargetID from {relTableName} where SourceID in ({leftIds})";
						relInfo.Data = conn.SelectToJson(qry);
					}

					// Now get the records of the referenced catalog
					var rightCatalogIDs = new List<int>(100);
					foreach (var row in relInfo.Data)
					{
						var jo = row as JObject;
						rightCatalogIDs.Add(jo.GetValue<int>("TargetID"));
					}
					if (rightCatalogIDs.Count > 0)
					{
						var rightIds = rightCatalogIDs.Merge(",");
						var referencedRows = conn.SelectToJson($"select * from {rightCatalog.Name}_{rightCatalog.ID} where ID in ({rightIds})");
						ExportCatalog(rightCatalog, referencedRows, result, true);
					}
					
				}
			}
		}


		public Dictionary<int, string> GetCatalogDefinitions(int catalogid)
		{
			var result = new Dictionary<int, string>();
			var root = GetCatalog(catalogid);
			AppendReferencedCatalogs(root, result);
			return result;
		}

		private void AppendReferencedCatalogs(CatalogDefinition catalog, Dictionary<int, string> result)
		{
			if (!result.ContainsKey(catalog.ID))
			{
				result.Add(catalog.ID, catalog.Definition);
				foreach (var refField in catalog.Fields.Where(f => f.Type == ColumnType.Reference))
				{
					var reference = GetCatalog(refField.CatalogID.Value);
					AppendReferencedCatalogs(reference, result);
				}
			}
		}

		public Dictionary<string, string> FlattenObject(int catalogid, bool removeIds, params NameValuePair[] filter)
		{
			var result = new Dictionary<string, string>();
			var args = new List<object>();
			var sb = new StringBuilder(100);
			var cat = GetCatalog(catalogid);
			foreach (var v in filter)
			{
				sb.Append($" [{v.Name}] = @p{v.Name} and");
				args.Add(v.Value);
			}
			if (sb.Length > 0)
				sb.Remove(sb.Length - 3, 3);
			var root = SelectOne(catalogid, $"select top 1 * from #TABLE where {sb.ToString()} ", args.ToArray());

			foreach (var refField in cat.Fields.Where(f => f.Type == ColumnType.Reference))
				AppendReferenceData(root, "", refField, removeIds);
			foreach (var p in root.Properties())
				result[p.Name] = p.Value.ToString();
			return result;
		}

		private void AppendReferenceData(JObject obj, string path, FieldDefinition field, bool removeIds)
		{
			var cat = GetCatalog(field.CatalogID.Value);
			var idKey = String.IsNullOrWhiteSpace(path)? field.Name : path + "." + field.Name;
            var idDisplayKey = String.IsNullOrWhiteSpace(path) ? field.Name : path + "._" + field.Name + "_DISP";
            int? rowID = obj[idKey] == null ? null: obj[idKey].Value<int?>();
            if (removeIds)
            {
                obj.Remove(idKey);
                obj.Remove(idDisplayKey);
                obj.Remove("ID");
            }

            if (!rowID.HasValue)
			{
				return;
			}

			var data = SelectOne(cat.ID, rowID.Value);
            var properties = removeIds ? data.Properties().Where(x => !x.Name.Equals("ID")) : data.Properties();
            if (path.Length > 0)
				path += "." + field.Name;
			else
				path = field.Name;
			foreach (var p in properties)
				    obj[path + "." + p.Name] = data[p.Name];
			foreach (var refField in cat.Fields.Where(f => f.Type == ColumnType.Reference))
				AppendReferenceData(obj, path, refField, removeIds);
		}


		public IEnumerable<Dictionary<string,string>> FlattenObjectsByIds(int detailCatalogID, int productCatalogID, bool removeIds, bool showDetailId, params int[] selectedIds)
		{
			var result = new List<Dictionary<string, string>>();
			var args = new List<object>();
			var detailCatalog = GetCatalog(detailCatalogID);
			var productCatalog = GetCatalog(productCatalogID);

			var detailKey = detailCatalog.Fields.Find(f => f.IsKey); // multiple key fields is not supported
			var productKey = productCatalog.Fields.Find(f => f.IsKey); // multiple key fields is not supported
			
			var allDetailsCondition = $" #TABLE.{detailKey.Name} in ({string.Join(",", selectedIds)})";
			var details = Select(detailCatalogID, $@"SELECT * FROM #TABLE WHERE {allDetailsCondition} ");
			// TODO: use join with details table and product table to get product details
			var productIds = details.Select(o => int.Parse(((JObject)o).Property("Product").Value.ToString())).ToArray();
			var allProductsCondition = $" #TABLE.{productKey.Name} in ({string.Join(",", productIds)})";
			var products = Select(productCatalogID, $"select * from #TABLE where {allProductsCondition} ");

			// remove null references
			var includeReferences = new List<FieldDefinition>();
			var allReferences = productCatalog.Fields.Where(w => w.Type == ColumnType.Reference);

			foreach (var referenceField in allReferences)
			{
				var hasValue = products.Count(w => !string.IsNullOrEmpty(((JObject)w).Property(referenceField.Name).Value.ToString()));

				if (hasValue > 0)
					includeReferences.Add(referenceField);
			}

			var query = DeepSelectStatement(detailCatalogID, productCatalogID, includeReferences, removeIds, showDetailId);

			var DetailToProductRef = "Product"; // hardcode
			conn.CommandTimeout = 600;
			var allRows = conn.SelectToJson($"{query} WHERE [{DetailToProductRef}] IN ({string.Join(",", productIds)}) ORDER BY {DetailToProductRef}");

			foreach (JObject row in allRows)
			{
				var r = new Dictionary<string, string>();

				foreach (var p in row.Properties())
					r[p.Name] = p.Value.ToString();

				result.Add(r);
			}

			return result;

			//return allRows;
		}

        // Deep To All References, not browser on Set Relations
		private string DeepSelectStatement(int detailCatalogId, int productCatalogId, IEnumerable<FieldDefinition> includeProductReferences, bool removeIds, bool showDetailId = false)
		{
			StringBuilder select = new StringBuilder();
			StringBuilder joins = new StringBuilder(4000);
			var cat = GetCatalog(detailCatalogId);
			Queue<ReferenceTree> allRef = new Queue<ReferenceTree>();

			var root = new ReferenceTree() { Parent = null, Current = cat, Alias = cat.Name, ColumnPath = "" };

			allRef.Enqueue(root);

			while (allRef.Count() > 0)
			{

				var node = allRef.Dequeue();

				var refs = new List<FieldDefinition>();

				if (node.Current.ID == productCatalogId && includeProductReferences != null && includeProductReferences.Count() > 0)
				{
					refs = includeProductReferences.ToList();
				}
				else
				{
					refs = node.Current.Fields.Where(p => p.Type == ColumnType.Reference).ToList();
				}

				foreach (var r in refs)
				{
					var refCat = GetCatalog(r.CatalogID.Value);
					var alias = refCat.Name;
					var path = string.IsNullOrEmpty(node.ColumnPath) ? "" : $"{node.ColumnPath}.";
					var childNode = new ReferenceTree() { Current = refCat, Parent = node.Current, Alias = alias, ColumnPath = $"{path}{r.Name}" };
					

					allRef.Enqueue(childNode);

					select.Append($", {SelectAllColumnsStatement(refCat, removeIds, alias, childNode.ColumnPath)}{Environment.NewLine}");
					joins.Append($" LEFT OUTER JOIN {refCat.Name}_{refCat.ID} AS {alias} ON {node.Alias}.{r.Name} = {alias}.ID {Environment.NewLine}");
				}

			}

			// hard code to add one column and hard code column name for comparer
			if (showDetailId)
				select.Append($", {root.Current.Name}.ID as dataId");


			return $@"
                    SELECT {SelectAllColumnsStatement(cat, removeIds, root.Alias, root.ColumnPath)}  {select.ToString()}
                    FROM {cat.Name}_{cat.ID} {root.Alias}
                    {joins.ToString()} ";
		}

		private string SelectAllColumnsStatement(CatalogDefinition cat, bool removeIds, string alias = "T", string path = "")
		{

			if (!string.IsNullOrEmpty(path))
				path = $"{path}.";

			var selectColumns = cat.Fields
				.Where(w => w.Type != ColumnType.Reference && w.Type != ColumnType.Set);

			if (removeIds)
				selectColumns = selectColumns.Where(w => !w.IsKey);

			var columnsListStr = selectColumns.Select(s => $"{alias}.[{s.Name}] as [{path}{s.Name}]");
			//var columnsList = selectColumns.Select(s => new SelectColumn() 
			//{ 
			//	TableName = alias,
			//	ColumnName = s.Name,
			//	ColumnAlias = $"[{path}{s.Name}]",
			//	Field = s
			//});

			return string.Join(", ", columnsListStr);
		}

        public JArray GetBaseDataFromOrderId(int catalogID, string orderId)
        {



            throw new NotImplementedException();
        }
    }

	public class ReferenceTree
	{
		public CatalogDefinition Current { get; set; }
		public CatalogDefinition Parent { get; set; }
		public string Alias { get; set; }
		public string ColumnPath { get; set; }
	}

	//public class SelectColumn
	//{
	//	public string TableName { get; set; }
	//	public string ColumnName { get; set; }
	//	public string ColumnAlias { get; set; }
	//	public FieldDefinition Field { get; set; }
	//}


	public class TextSearchFilter
	{
		public string FieldName;
		public object Value;
		public TextSearchFilterType SearchType;
	}

	public enum TextSearchFilterType
	{
		Contains = 0,
		ExactMatch = 1
	}

	public class DynamicDBBeforeInsert : EQEventInfo
	{
		public int CatalogID { get; private set; }
		public JObject Data { get; private set; }
		public bool CancelEvent { get; set; }
		public string CancelReason { get; set; }

		public DynamicDBBeforeInsert(int catalogid, JObject o)
		{
			CatalogID = catalogid;
			Data = o;
		}
	}


	public class DynamicDBBeforeUpdate : EQEventInfo
	{
		public int CatalogID { get; private set; }
		public JObject CurrentData { get; private set; }
		public JObject ProposedData { get; private set; }
		public bool CancelEvent { get; set; }
		public string CancelReason { get; set; }

		public DynamicDBBeforeUpdate(int catalogid, JObject o)
		{
			CatalogID = catalogid;
			ProposedData = o;
		}
	}


	public class DynamicDBBeforeDelete : EQEventInfo
	{
		public int CatalogID { get; private set; }
		public JObject Data { get; private set; }
		public bool CancelEvent { get; set; }
		public string CancelReason { get; set; }

		public DynamicDBBeforeDelete(int catalogid, JObject o)
		{
			CatalogID = catalogid;
			Data = o;
		}
	}


	public static class ArgsExtensions
	{
		public static string Print(this List<object> args)
		{
			if (args == null)
				return "null";
			StringBuilder sb = new StringBuilder(1000);
			sb.Append("(");
			foreach (object o in args)
			{
				if (o != null)
					sb.Append(o.ToString()).Append(",");
				else
					sb.Append("null,");
			}
			if (sb.Length > 0)
				sb.Remove(sb.Length - 1, 1);
			sb.Append(")");
			return sb.ToString();
		}
	}
}
