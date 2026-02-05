using System;
using System.Text;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using Service.Contracts.Database;
using System.Linq;
using System.Data.Common;

namespace Service.Contracts.Database
{
	enum MemberCType
	{
		Property,
		Field
	}

	class ColumnInfo
	{
		public string CName;
		public string MName;
		public bool IsPrimaryKey;
		public bool IsIdentity;
		public bool IsNullable;
		public bool IsReadOnly;
		public bool IsIgnore;
		public bool IsLazyLoad;
		public Type DataType;
		public MemberCType MemberType;
		public PropertyInfo pInfo;
		public FieldInfo fInfo;
	}

	class FKInfo
	{
		public Type FKEntity;
		public string LeftColumns;
		public string RightColumns;
	}

	class EntityMetadata
	{
		private object syncObj = new object();
		private string _insert;
		private string _update;
		private string _delete;
		private string _select;
		private string _selectPage;

		public Type Type;
		public string TableName;
		public List<ColumnInfo> Columns;
		public List<ColumnInfo> PK;
		public List<FKInfo> FKs;
		public string IdentityColumn;
		public string SequenceName; // required by oracle


		public string InsertStatement()
		{
			return String.Format(_insert, this.TableName);
		}


		public string UpdateStatement()
		{
			return String.Format(_update, this.TableName);
		}


		public string DeleteStatement()
		{
			return String.Format(_delete, this.TableName);
		}


		public string SelectStatement()
		{
			return String.Format(_select, this.TableName);
		}

		public string SelectPageStatement(string table, int page, int pageSize, string orderBy, string whereStatement)
		{
			ColumnInfo orderByColumn = null;
			foreach (ColumnInfo c in Columns)
			{
				if (c.CName.ToLower() == orderBy.ToLower())
				{
					orderByColumn = c;
					break;
				}
				if (c.MName.ToLower() == orderBy.ToLower())
				{
					orderByColumn = c;
					break;
				}
			}
			if (orderByColumn == null)
				throw new Exception("Column " + orderBy + " does not exist in table " + table);

			whereStatement = whereStatement.Trim();
			string where = whereStatement.ToLower();
			if (where.StartsWith("where"))
				whereStatement = whereStatement.Substring(5);

			var result = String.Format(_selectPage, table, orderByColumn.CName, pageSize, page * pageSize, "where " + whereStatement);
			return result;
		}


		public DynamicMethod CopyEntityMethod;
		public DynamicMethod MakeListMethod;

		/// <summary>
		/// Reflects on the given object in order to get required metadata.
		/// </summary>
		public static EntityMetadata GetMetadata(object entity, ISqlStatementBuilder builder)
		{
			string cname, mname, sequencename = "";
			bool isreadonly, ishidden, islazyload, ispk, isidentity, isnullable;
			Type t = entity.GetType();
			EntityMetadata info = new EntityMetadata();
			info.Type = t;
			info.TableName = t.Name;
			info.Columns = new List<ColumnInfo>();
			info.PK = new List<ColumnInfo>();
			info.FKs = new List<FKInfo>();

			//Process class level attributes
			object[] attrs = t.GetCustomAttributes(false);
			foreach (object o in attrs)
			{
				if (o is TargetTableAttribute)
					info.TableName = (o as TargetTableAttribute).TableName;
				else if (o is FKAttribute)
					info.AddFK(o as FKAttribute);
			}

			//Process public fields
			FieldInfo[] fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public);
			foreach (FieldInfo f in fields)
			{
				ishidden = false;
				islazyload = false;
				isreadonly = false;
				ispk = false;
				isidentity = false;
				isnullable = false;
				mname = f.Name;  //Member name  (name of the field in the entity object)
				cname = f.Name;  //Column name  (name of the corresponding column in the DB)
				object[] arr = f.GetCustomAttributes(false);
				foreach (object o in arr)
				{
					if (o is IgnoreFieldAttribute)
						ishidden = true;
					else if (o is LazyLoadAttribute)
						islazyload = true;
					else if (o is Readonly)
						isreadonly = true;
					else if (o is TargetColumnAttribute)
						cname = (o as TargetColumnAttribute).ColumnName;
					else if (o is PKAttribute)
						ispk = true;
					else if (o is IdentityAttribute)
					{
						isidentity = true;
						sequencename = (o as IdentityAttribute).SequenceName;
					}
					else if (o is NullableAttribute)
						isnullable = true;
					if (o.GetType().Name == "TimestampAttribute")
						ishidden = true;
				}
				ColumnInfo c = new ColumnInfo();
				c.MName = mname;
				c.CName = cname;
				c.IsIdentity = isidentity;
				c.IsPrimaryKey = ispk;
				c.IsNullable = isnullable;
				c.IsReadOnly = isreadonly;
				c.IsIgnore = ishidden;
				c.IsLazyLoad = islazyload;
				c.DataType = f.FieldType;
				c.MemberType = MemberCType.Field;
				c.fInfo = f;
				info.Columns.Add(c);
				if (ispk)
					info.PK.Add(c);
				if (isidentity)
				{
					info.IdentityColumn = cname;
					info.SequenceName = sequencename;
				}
				if (Nullable.GetUnderlyingType(f.FieldType) != null)
					c.IsNullable = true;
				if(f.FieldType == typeof(string))
					c.IsNullable = true;
			}

			//Process public properties
			PropertyInfo[] properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);
			foreach (PropertyInfo p in properties)
			{
				if (!(p.CanRead && p.CanWrite)) continue;
				ishidden = false;
				islazyload = false;
				isreadonly = false;
				ispk = false;
				isidentity = false;
				isnullable = false;
				mname = p.Name;  //Member name  (name of the field in the entity object)
				cname = p.Name;  //Column name  (name of the corresponding column in the DB)
				object[] arr = p.GetCustomAttributes(false);
				foreach (object o in arr)
				{
					if (o is IgnoreFieldAttribute)
						ishidden = true;
					else if (o is LazyLoadAttribute)
						islazyload = true;
					else if (o is Readonly)
						isreadonly = true;
					else if (o is TargetColumnAttribute)
						cname = (o as TargetColumnAttribute).ColumnName;
					else if (o is PKAttribute)
						ispk = true;
					else if (o is IdentityAttribute)
					{
						isidentity = true;
						sequencename = (o as IdentityAttribute).SequenceName;
					}
					else if (o is NullableAttribute)
						isnullable = true;
					if (o.GetType().Name == "TimestampAttribute")
						ishidden = true;
				}
				ColumnInfo c = new ColumnInfo();
				c.MName = mname;
				c.CName = cname;
				c.IsIdentity = isidentity;
				c.IsPrimaryKey = ispk;
				c.IsNullable = isnullable;
				c.IsReadOnly = isreadonly;
				c.IsIgnore = ishidden;
				c.IsLazyLoad = islazyload;
				c.DataType = p.PropertyType;
				c.MemberType = MemberCType.Property;
				c.pInfo = p;
				info.Columns.Add(c);
				if (ispk)
					info.PK.Add(c);
				if (isidentity)
				{
					info.IdentityColumn = cname;
					info.SequenceName = sequencename;
				}
				if (Nullable.GetUnderlyingType(p.PropertyType) != null)
					c.IsNullable = true;
				if (p.PropertyType == typeof(string))
					c.IsNullable = true;
			}

			//Build the different statements using named parameters
			info._insert = builder.BuildInsertStatement(info);
			info._update = builder.BuildUpdateStatement(info);
			info._delete = builder.BuildDeleteStatement(info);
			info._select = builder.BuildSelectStatement(info);
			info._selectPage = builder.BuildSelectPageStatement(info);

			//Returns the TableMetadata object
			return info;
		}

		private void AddFK(FKAttribute attr)
		{
			FKInfo fk = new FKInfo();
			fk.FKEntity = attr.FKEntity;
			fk.LeftColumns = attr.LeftColumns;
			fk.RightColumns = attr.RightColumns;
			FKs.Add(fk);
		}


		internal void InitDynamicCode(IDataReader rd, object sample, Type listType)
		{
			lock (syncObj)
			{
				if (CopyEntityMethod == null)
					CopyEntityMethod = DynamicCodeGenerator.FillEntityFromDataReader(sample.GetType(), this, rd);
				if (MakeListMethod == null)
					MakeListMethod = DynamicCodeGenerator.MakeListFromDataReader(sample.GetType(), listType, this, rd);
			}
		}


		public List<object> GetInsertValues(object entity)
		{
			List<object> lst = new List<object>();
			foreach (ColumnInfo c in Columns)
			{
				object value;
				if (c.IsIdentity || c.IsIgnore || c.IsReadOnly) continue;
				if (c.MemberType == MemberCType.Field)
					value = c.fInfo.GetValue(entity);
				else
					value = c.pInfo.GetValue(entity, BindingFlags.Public | BindingFlags.Instance, null, null, null);
				if (c.IsNullable && c.DataType.IsValueType && IsDefaultValue(value, c.DataType))
					value = null;
				lst.Add(value);
			}
			return lst;
		}


		public List<object> GetInsertValues(object entity, string fields)
		{
			if (string.IsNullOrWhiteSpace(fields))
				throw new ArgumentNullException("fields");
			List<object> lst = new List<object>();
			string[] tokens = fields.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string field in tokens)
			{
				var c = Columns.FirstOrDefault(p => String.Compare(p.CName, field.Trim(), true) == 0);
				if (c != null)
				{
					object value;
					if (c.IsIdentity || c.IsIgnore || c.IsReadOnly) continue;
					if (c.MemberType == MemberCType.Field)
						value = c.fInfo.GetValue(entity);
					else
						value = c.pInfo.GetValue(entity, BindingFlags.Public | BindingFlags.Instance, null, null, null);
					if (c.IsNullable && c.DataType.IsValueType && IsDefaultValue(value, c.DataType))
						value = null;
					lst.Add(value);
				}
			}
			return lst;
		}


		public List<DbParameter> GetInsertDBParameters(object entity, DbProviderFactory factory)
		{
			List<DbParameter> lst = new List<DbParameter>();
			foreach (ColumnInfo c in Columns)
			{
				object value;
				if (c.IsIdentity || c.IsIgnore || c.IsReadOnly) continue;
				if (c.MemberType == MemberCType.Field)
					value = c.fInfo.GetValue(entity);
				else
					value = c.pInfo.GetValue(entity, BindingFlags.Public | BindingFlags.Instance, null, null, null);
				if (c.IsNullable && c.DataType.IsValueType && IsDefaultValue(value, c.DataType))
					value = null;

				var p = factory.CreateParameter();
				p.ParameterName = $"@{c.CName}";
				if (value == null)
					p.Value = DBNull.Value;
				else
					p.Value = value;
				p.DbType = GetDbType(c.DataType);
                if(value != null && value is string strValue)
                {
                    // Stabilize query store loging
                    if(strValue.Length > 4000)
                        p.Size = -1;  // assign MAX length
                    else
                        p.Size = 4000; // assign length to 4000
                }
				lst.Add(p);
			}
			return lst;
		}


		private DbType GetDbType(Type dataType)
		{
			if (dataType == typeof(byte[]))
				return DbType.Binary;
			if (dataType == typeof(bool))
				return DbType.Boolean;
			if (dataType == typeof(byte))
				return DbType.Byte;
			if (dataType == typeof(int))
				return DbType.Int32;
			if (dataType == typeof(long))
				return DbType.Int64;
			if (dataType == typeof(float))
				return DbType.Single;
			if (dataType == typeof(double))
				return DbType.Double;
			if (dataType == typeof(Decimal))
				return DbType.Decimal;
			if (dataType == typeof(Guid))
				return DbType.Guid;
			if (dataType == typeof(DateTime))
				return DbType.DateTime2;
			if (dataType == typeof(string))
				return DbType.String;
			if (dataType.IsEnum)
				return GetDbType(dataType.GetEnumUnderlyingType());
			if (dataType.IsNullable())
				return GetDbType(dataType.GetGenericArguments()[0]);

			throw new InvalidOperationException($"Fields of type {dataType.Name} are not supported");
		}


		private bool IsDefaultValue(object value, Type t)
		{
			if (t == typeof(byte))
				return (byte)value == 0;
			if (t == typeof(char))
				return (char)value == '\0';
			if (t == typeof(int))
				return (int)value == 0;
			if (t == typeof(bool))
				return (bool)value == false;
			if (t == typeof(long))
				return (long)value == 0L;
			if (t == typeof(float))
				return (float)value == 0.0f;
			if (t == typeof(double))
				return (double)value == 0.0d;
			if (t == typeof(decimal))
				return (decimal)value == 0.0m;
			if (t == typeof(DateTime))
				return (DateTime)value == DateTime.MinValue;
			if (t == typeof(TimeSpan))
				return (TimeSpan)value == TimeSpan.Zero;
			else
				return false;
		}


		public object[] GetUpdateValues(object entity)
		{
			List<object> lst = new List<object>();
			foreach (ColumnInfo c in Columns)
			{
				object value;
				if (c.IsIdentity || c.IsReadOnly || c.IsPrimaryKey || c.IsIgnore) continue;
				if (c.MemberType == MemberCType.Field)
					value = c.fInfo.GetValue(entity);
				else
					value = c.pInfo.GetValue(entity, BindingFlags.Public | BindingFlags.Instance, null, null, null);
				if (c.IsNullable && c.DataType.IsValueType && IsDefaultValue(value, c.DataType))
					value = null;
				lst.Add(value);
			}
			foreach (ColumnInfo c in PK)
			{
				if (c.MemberType == MemberCType.Field)
					lst.Add(c.fInfo.GetValue(entity));
				else
					lst.Add(c.pInfo.GetValue(entity, BindingFlags.Public | BindingFlags.Instance, null, null, null));
			}
			return lst.ToArray();
		}


		public List<DbParameter> GetUpdateDBParameters(object entity, DbProviderFactory factory)
		{
			List<DbParameter> lst = new List<DbParameter>();
			foreach (ColumnInfo c in Columns)
			{
				object value;
				if (c.IsIdentity || c.IsReadOnly || c.IsPrimaryKey || c.IsIgnore) continue;
				if (c.MemberType == MemberCType.Field)
					value = c.fInfo.GetValue(entity);
				else
					value = c.pInfo.GetValue(entity, BindingFlags.Public | BindingFlags.Instance, null, null, null);
				if (c.IsNullable && c.DataType.IsValueType && IsDefaultValue(value, c.DataType))
					value = null;
				var p = factory.CreateParameter();
				p.ParameterName = $"@{c.CName}";
				if (value == null)
					p.Value = DBNull.Value;
				else
					p.Value = value;
				p.DbType = GetDbType(c.DataType);
				lst.Add(p);
			}
			foreach (ColumnInfo c in PK)
			{
				object value;
				if (c.MemberType == MemberCType.Field)
					value = c.fInfo.GetValue(entity);
				else
					value = c.pInfo.GetValue(entity, BindingFlags.Public | BindingFlags.Instance, null, null, null);
				var p = factory.CreateParameter();
				p.ParameterName = c.CName;
				p.Value = value;
				p.DbType = GetDbType(c.DataType);
				lst.Add(p);
			}
			return lst;
		}


		public object[] GetDeleteValues(object entity)
		{
			List<object> lst = new List<object>();
			foreach (ColumnInfo c in PK)
			{
				if (c.MemberType == MemberCType.Field)
					lst.Add(c.fInfo.GetValue(entity));
				else
					lst.Add(c.pInfo.GetValue(entity, BindingFlags.Public | BindingFlags.Instance, null, null, null));
			}
			return lst.ToArray();
		}


		internal object[] GetSelectValues(object entity)
		{
			List<object> lst = new List<object>();
			foreach (ColumnInfo c in PK)
			{
				if (c.MemberType == MemberCType.Field)
					lst.Add(c.fInfo.GetValue(entity));
				else
					lst.Add(c.pInfo.GetValue(entity, BindingFlags.Public | BindingFlags.Instance, null, null, null));
			}
			return lst.ToArray();
		}
	}
}
