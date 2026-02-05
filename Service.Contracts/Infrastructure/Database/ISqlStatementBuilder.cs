using System;
using System.Text;
using System.Collections.Generic;

namespace Service.Contracts.Database
{
	/// <summary>
	/// Specifies the methods required to build Insert/Update/Delete Statements. Implementations
	/// of this interface must use the target provider SQL sintax and make use of named parameters.
	/// </summary>
	interface ISqlStatementBuilder
	{
		string BuildInsertStatement(EntityMetadata info);
		string BuildUpdateStatement(EntityMetadata info);
		string BuildDeleteStatement(EntityMetadata info);
		string BuildSelectStatement(EntityMetadata info);
		string BuildSelectPageStatement(EntityMetadata info);
		string StringDelimiter { get; }
	}




	/* ===========================================================
	 *                     SQL Server
	 * =========================================================== */


	/// <summary>
	/// Builds SQL Statements using SQLServer 2005+ sintax
	/// </summary>
	class SqlServerStatementBuilder : ISqlStatementBuilder
	{
        public SqlServerStatementBuilder()
        {
        }

		public string StringDelimiter { get { return "'"; } }

		public string BuildInsertStatement(EntityMetadata info)
		{
			//Target statement:  "set dateformat mdy; insert into {0}( columns ) values( named_parameters ) [select scope_identity() as RowID]"
			StringBuilder sb = new StringBuilder();
			sb.Append("set dateformat mdy; insert into [{0}](");
			foreach (ColumnInfo c in info.Columns)
			{
				if (c.IsIdentity || c.IsIgnore || c.IsReadOnly) continue;
				sb.Append('[').Append(c.CName);
				sb.Append("], ");
			}
			sb.Remove(sb.Length - 2, 2);
			sb.Append(") values(");
			foreach (ColumnInfo c in info.Columns)
			{
				if (c.IsIdentity || c.IsIgnore || c.IsReadOnly) continue;
				sb.Append('@').Append(c.CName);
				sb.Append(", ");
			}
			sb.Remove(sb.Length - 2, 2);
			sb.Append(")");
			if (info.IdentityColumn != null)
				sb.Append(" select Scope_Identity() as RowID");
			return sb.ToString();
		}



		public string BuildUpdateStatement(EntityMetadata info)
		{
			//Target statement:  "set dateformat mdy; update {0} set column1 = @named_param1, ... columnN = @named_paramN where PKcolumn1 = @PKnamed_param1 [and PKcolumn2 = @PKnamed_param2 ...]"
			StringBuilder sb = new StringBuilder();
			sb.Append("set dateformat mdy; update [{0}]").Append(" set ");
			foreach (ColumnInfo c in info.Columns)
			{
				if (c.IsIdentity || c.IsReadOnly || c.IsIgnore || c.IsPrimaryKey) continue;
				sb.Append('[').Append(c.CName).Append("] = @").Append(c.CName);
				sb.Append(", ");
			}
			sb.Remove(sb.Length - 2, 2);
			sb.Append(" where ");
			foreach (ColumnInfo c in info.PK)
			{
				sb.Append('[').Append(c.CName).Append("] = @").Append(c.CName);
				sb.Append(" and ");
			}
			sb.Remove(sb.Length - 5, 5);
			return sb.ToString();
		}



		public string BuildDeleteStatement(EntityMetadata info)
		{
			//Target statement:  "delete from table_name where PKcolumn1 = @PKnamed_param1 [and PKcolumn2 = @PKnamed_param2 ...]"
			StringBuilder sb = new StringBuilder();
			sb.Append("delete from [{0}] where");
			foreach (ColumnInfo c in info.PK)
			{
				sb.Append('[').Append(c.CName).Append("] = @").Append(c.CName);
				sb.Append(" and ");
			}
			sb.Remove(sb.Length - 5, 5);
			return sb.ToString();
		}


		public string BuildSelectStatement(EntityMetadata info)
		{
			//Target statement:  "select [column_list] from table_name where PKcolumn1 = @PKnamed_param1 [and PKcolumn2 = @PKnamed_param2 ...]"
			StringBuilder sb = new StringBuilder();
			sb.Append("select " + SelectColumns(info) + " from [{0}] where");
			foreach (ColumnInfo c in info.PK)
			{
				sb.Append('[').Append(c.CName).Append("] = @").Append(c.CName);
				sb.Append(" and ");
			}
			sb.Remove(sb.Length - 5, 5);
			return sb.ToString();
		}

		private string SelectColumns(EntityMetadata info)
		{
			StringBuilder sb = new StringBuilder(100);
			foreach (ColumnInfo c in info.Columns)
			{
				if (c.IsIgnore || c.IsLazyLoad)
					continue;
				sb.Append(c.CName).Append(',');
			}
			if (sb.Length > 1)
				sb.Remove(sb.Length - 1, 1);
			return sb.ToString();
		}

		public string BuildSelectPageStatement(EntityMetadata info)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("select top {2} * from (select *, ROW_NUMBER() OVER (ORDER BY {1}) as rownum from [{0}] {4}) as T where rownum > {3}");
			return sb.ToString();
		}
	}



    /* ===========================================================
	 *                           Sqlite
	 * =========================================================== */


    /// <summary>
    /// Builds SQL Statements using Sqlite sintax
    /// </summary>
    class SqliteStatementBuilder : ISqlStatementBuilder
    {
        public string StringDelimiter { get { return "'"; } }

        public string BuildInsertStatement(EntityMetadata info)
        {
            //Target statement:  "insert into table( columns ) values( named_parameters ); SELECT last_insert_rowid() AS RowID;"
            StringBuilder sb = new StringBuilder();
            sb.Append("insert into {0}(");
            foreach(ColumnInfo c in info.Columns)
            {
                if(c.IsIdentity || c.IsIgnore || c.IsReadOnly) continue;
                sb.Append(c.CName).Append(", ");
            }
            sb.Remove(sb.Length - 2, 2);
            sb.Append(") values(");
            foreach(ColumnInfo c in info.Columns)
            {
                if(c.IsIdentity || c.IsIgnore || c.IsReadOnly) continue;
                sb.Append('@').Append(c.CName);
                sb.Append(", ");
            }
            sb.Remove(sb.Length - 2, 2);
            sb.AppendLine(");");
            if(info.IdentityColumn != null)
                sb.AppendLine("SELECT last_insert_rowid() AS RowID;");
            return sb.ToString();
        }



        public string BuildUpdateStatement(EntityMetadata info)
        {
            //Target statement:  "update table set column1 = @named_param1, ... columnN = @named_paramN where PKcolumn1 = @PKnamed_param1 [and PKcolumn2 = @PKnamed_param2 ...]"
            StringBuilder sb = new StringBuilder();
            sb.Append("update {0}").Append(" set ");
            foreach(ColumnInfo c in info.Columns)
            {
                if(c.IsIdentity || c.IsReadOnly || c.IsIgnore || c.IsPrimaryKey) continue;
                sb.Append(c.CName).Append(" = @").Append(c.CName);
                sb.Append(", ");
            }
            sb.Remove(sb.Length - 2, 2);
            sb.Append(" where ");
            foreach(ColumnInfo c in info.PK)
            {
                sb.Append(c.CName).Append(" = @").Append(c.CName);
                sb.Append(" and ");
            }
            sb.Remove(sb.Length - 5, 5);
            return sb.ToString();
        }



        public string BuildDeleteStatement(EntityMetadata info)
        {
            //Target statement:  "delete from table_name where PKcolumn1 = @PKnamed_param1 [and PKcolumn2 = @PKnamed_param2 ...]"
            StringBuilder sb = new StringBuilder();
            sb.Append("delete from {0} where");
            foreach(ColumnInfo c in info.PK)
            {
                sb.Append(c.CName).Append(" = @").Append(c.CName);
                sb.Append(" and ");
            }
            sb.Remove(sb.Length - 5, 5);
            return sb.ToString();
        }


        public string BuildSelectStatement(EntityMetadata info)
        {
            //Target statement:  "select [column_list] from table_name where PKcolumn1 = @PKnamed_param1 [and PKcolumn2 = @PKnamed_param2 ...]"
            StringBuilder sb = new StringBuilder();
            sb.Append("select " + SelectColumns(info) + " from {0} where");
            foreach(ColumnInfo c in info.PK)
            {
                sb.Append(c.CName).Append(" = @").Append(c.CName);
                sb.Append(" and ");
            }
            sb.Remove(sb.Length - 5, 5);
            return sb.ToString();
        }

        private string SelectColumns(EntityMetadata info)
        {
            StringBuilder sb = new StringBuilder(100);
            foreach(ColumnInfo c in info.Columns)
            {
                if(c.IsIgnore || c.IsLazyLoad)
                    continue;
                sb.Append(c.CName).Append(',');
            }
            if(sb.Length > 1)
                sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }

        public string BuildSelectPageStatement(EntityMetadata info)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("select * from {0} {4} ORDER BY {1} LIMIT {2} OFFSET {3};");
            return sb.ToString();
        }
    }



    /* ===========================================================
	 *                     MySql
	 * =========================================================== */


    /// <summary>
    /// Builds SQL Statements using SQLServer 2005+ sintax
    /// </summary>
    class MySqlStatementBuilder : ISqlStatementBuilder
	{
		public string StringDelimiter { get { return "'"; } }

		public string BuildInsertStatement(EntityMetadata info)
		{
			//Target statement:  "insert into {0}( columns ) values( named_parameters ) [select scope_identity() as RowID]"
			StringBuilder sb = new StringBuilder();
			sb.Append("insert into {0}(");
			foreach (ColumnInfo c in info.Columns)
			{
				if (c.IsIdentity || c.IsIgnore || c.IsReadOnly) continue;
				sb.Append(c.CName).Append(", ");
			}
			sb.Remove(sb.Length - 2, 2);
			sb.Append(") values(");
			foreach (ColumnInfo c in info.Columns)
			{
				if (c.IsIdentity || c.IsIgnore || c.IsReadOnly) continue;
				sb.Append('@').Append(c.CName);
				sb.Append(", ");
			}
			sb.Remove(sb.Length - 2, 2);
			sb.Append(")");
			if (info.IdentityColumn != null)
				sb.Append(" select LAST_INSERT_ID() as RowID");
			return sb.ToString();
		}



		public string BuildUpdateStatement(EntityMetadata info)
		{
			//Target statement:  "update {0} set column1 = @named_param1, ... columnN = @named_paramN where PKcolumn1 = @PKnamed_param1 [&& PKcolumn2 = @PKnamed_param2 ...]"
			StringBuilder sb = new StringBuilder();
			sb.Append("update {0}").Append(" set ");
			foreach (ColumnInfo c in info.Columns)
			{
				if (c.IsIdentity || c.IsReadOnly || c.IsIgnore || c.IsPrimaryKey) continue;
				sb.Append(c.CName).Append(" = @").Append(c.CName);
				sb.Append(", ");
			}
			sb.Remove(sb.Length - 2, 2);
			sb.Append(" where ");
			foreach (ColumnInfo c in info.PK)
			{
				sb.Append(c.CName).Append(" = @").Append(c.CName);
				sb.Append(" && ");
			}
			sb.Remove(sb.Length - 4, 4);
			return sb.ToString();
		}



		public string BuildDeleteStatement(EntityMetadata info)
		{
			//Target statement:  "delete from table_name where PKcolumn1 = @PKnamed_param1 [&& PKcolumn2 = @PKnamed_param2 ...]"
			StringBuilder sb = new StringBuilder();
			sb.Append("delete from {0} where");
			foreach (ColumnInfo c in info.PK)
			{
				sb.Append(c.CName).Append(" = @").Append(c.CName);
				sb.Append(" && ");
			}
			sb.Remove(sb.Length - 4, 4);
			return sb.ToString();
		}


		public string BuildSelectStatement(EntityMetadata info)
		{
			//Target statement:  "select [column_list] from table_name where PKcolumn1 = @PKnamed_param1 [and PKcolumn2 = @PKnamed_param2 ...]"
			StringBuilder sb = new StringBuilder();
			sb.Append("select " + SelectColumns(info) + " from {0} where");
			foreach (ColumnInfo c in info.PK)
			{
				sb.Append(c.CName).Append(" = @").Append(c.CName);
				sb.Append(" && ");
			}
			sb.Remove(sb.Length - 4, 4);
			return sb.ToString();
		}

		private string SelectColumns(EntityMetadata info)
		{
			StringBuilder sb = new StringBuilder(100);
			foreach (ColumnInfo c in info.Columns)
			{
				if (c.IsIgnore || c.IsLazyLoad)
					continue;
				sb.Append(c.CName).Append(',');
			}
			if (sb.Length > 1)
				sb.Remove(sb.Length - 1, 1);
			return sb.ToString();
		}

		public string BuildSelectPageStatement(EntityMetadata info)
		{
			return "NOTIMPLEMENTED";
		}
	}




	/* ===========================================================
	 *                     Oracle
	 * =========================================================== */


	/// <summary>
	/// Builds SQL Statements using Oracle (PL/SQL) sintax
	/// </summary>
	class OracleStatementBuilder : ISqlStatementBuilder
	{
		public string StringDelimiter { get { return "'"; } }


		public string BuildInsertStatement(EntityMetadata info)
		{
			string columns, values;
			StringBuilder sb = new StringBuilder();

			// gets the column list
			foreach (ColumnInfo c in info.Columns)
			{
				if (c.IsIdentity || c.IsIgnore) continue;
				sb.Append(c.CName);
				sb.Append(", ");
			}
			sb.Remove(sb.Length - 2, 2);
			columns = sb.ToString();

			// gets the values list
			sb.Length = 0;
			foreach (ColumnInfo c in info.Columns)
			{
				if (c.IsIdentity || c.IsIgnore) continue;
				sb.Append('@').Append(c.CName);
				sb.Append(", ");
			}
			sb.Remove(sb.Length - 2, 2);
			values = sb.ToString();

			//Builds the insert statement.
			if (info.IdentityColumn != null)
			{
				//NOTE: This assumes that the column marked with the Identity attribute was also supplied
				//with the name of the sequence used to get the next id.
				return String.Format(@"
					declare
						seq_id number;
					begin
						alter session set NLS_DATE_FORMAT = 'mm/dd/yyyy hh:mi:ss am';
						select {0}.nextVal into seq_id from dual;
						insert into {{0}}({1}, {2}) values(seq_id, {3});
						select seq_id from dual;
					end;", info.SequenceName, info.IdentityColumn, columns, values);
			}
			else
			{
				return String.Format(@"
					begin
						alter session set NLS_DATE_FORMAT = 'mm/dd/yyyy hh:mi:ss am';
						insert into {{0}}( {0} ) values( {1} );
					end;", columns, values);
			}
		}



		public string BuildUpdateStatement(EntityMetadata info)
		{
			string values, conditionals;
			StringBuilder sb = new StringBuilder();
			foreach (ColumnInfo c in info.Columns)
			{
				if (c.IsIdentity || c.IsReadOnly || c.IsIgnore || c.IsPrimaryKey) continue;
				sb.Append(c.CName).Append(" = @").Append(c.CName);
				sb.Append(", ");
			}
			sb.Remove(sb.Length - 2, 2);
			values = sb.ToString();

			sb.Length = 0;
			foreach (ColumnInfo c in info.PK)
			{
				sb.Append(c.CName).Append(" = @").Append(c.CName);
				sb.Append(" and ");
			}
			sb.Remove(sb.Length - 5, 5);
			conditionals = sb.ToString();

			return String.Format(@"
				begin
					alter session set NLS_DATE_FORMAT = 'mm/dd/yyyy hh:mi:ss am';
					update {{0}} set {0} where {1};
				end;", values, conditionals);
		}



		public string BuildDeleteStatement(EntityMetadata info)
		{
			string conditionals;
			StringBuilder sb = new StringBuilder();
			foreach (ColumnInfo c in info.PK)
			{
				sb.Append(c.CName).Append(" = @").Append(c.CName);
				sb.Append(" and ");
			}
			sb.Remove(sb.Length - 5, 5);
			conditionals = sb.ToString();
			return String.Format(@"
				begin
					delete from {{0}} where {0};
				end;", conditionals);
		}


		public string BuildSelectStatement(EntityMetadata info)
		{
			string conditionals;
			StringBuilder sb = new StringBuilder();
			foreach (ColumnInfo c in info.PK)
			{
				sb.Append(c.CName).Append(" = @").Append(c.CName);
				sb.Append(" and ");
			}
			sb.Remove(sb.Length - 5, 5);
			conditionals = sb.ToString();
			return String.Format(@"
				begin
					select * from {{0}} where {0};
				end;", conditionals);
		}

		public string BuildSelectPageStatement(EntityMetadata info)
		{
			throw new NotImplementedException();
		}
	}






	/* ===========================================================
	 *                   Any ODBC Provider
	 * =========================================================== */


	/// <summary>
	/// Builds SQL Statements following Odbc standards for named parameters
	/// </summary>
	class OdbcStatementBuilder : ISqlStatementBuilder
	{
		public string StringDelimiter { get { return "'"; } }


		public string BuildInsertStatement(EntityMetadata info)
		{
			throw new NotImplementedException();
		}

		public string BuildUpdateStatement(EntityMetadata info)
		{
			throw new NotImplementedException();
		}

		public string BuildDeleteStatement(EntityMetadata info)
		{
			throw new NotImplementedException();
		}

		public string BuildSelectStatement(EntityMetadata info)
		{
			throw new NotImplementedException();
		}

		public string BuildSelectPageStatement(EntityMetadata info)
		{
			throw new NotImplementedException();
		}
	}






	/* ===========================================================
	 *                  Any OleDb Provider
	 * =========================================================== */


	/// <summary>
	/// Builds SQL Statements following OleDb standards for named parameters
	/// </summary>
	class OleDbStatementBuilder : ISqlStatementBuilder
	{
		public string StringDelimiter { get { return "'"; } }


		public string BuildInsertStatement(EntityMetadata info)
		{
			throw new NotImplementedException();
		}

		public string BuildUpdateStatement(EntityMetadata info)
		{
			throw new NotImplementedException();
		}

		public string BuildDeleteStatement(EntityMetadata info)
		{
			throw new NotImplementedException();
		}

		public string BuildSelectStatement(EntityMetadata info)
		{
			throw new NotImplementedException();
		}

		public string BuildSelectPageStatement(EntityMetadata info)
		{
			throw new NotImplementedException();
		}
	}
}
