using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace DBComparer
{
    internal sealed class TableName
    {
        public string Schema, Name;
        public string Sql
        {
            get
            {
                return Database.Quote(Schema) + "." + Database.Quote(Name);
            }
        }
        public override string ToString()
        {
            return Schema + "." + Name;
        }
    }

    internal static class Database
    {
        public static string Quote(string name)
        {
            return "[" + name.Replace("]", "]]") + "]";
        }
        public static string InDatabase(string connection, string database)
        {
            var builder = new SqlConnectionStringBuilder(connection) { InitialCatalog = database };
            return builder.ConnectionString;
        }
        public static async Task<DataTable> Query(string connection, string sql, params SqlParameter[] parameters)
        {
            using (var cn = new SqlConnection(connection))
            using (var cmd = new SqlCommand(sql, cn) { CommandTimeout = 120 })
            {
                cmd.Parameters.AddRange(parameters);
                await cn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var result = new DataTable();
                    result.Load(reader);
                    return result;
                }
            }
        }
        public static async Task<List<string>> Databases(string connection)
        {
            var data = await Query(connection, "SELECT name FROM sys.databases WHERE state = 0 AND HAS_DBACCESS(name) = 1 ORDER BY name");
            return data.Rows.Cast<DataRow>().Select(r => (string)r[0]).ToList();
        }
        public static async Task<List<TableName>> Tables(string connection)
        {
            var data = await Query(connection, "SELECT s.name, t.name FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id ORDER BY s.name,t.name");
            return data.Rows.Cast<DataRow>().Select(r => new TableName { Schema = (string)r[0], Name = (string)r[1] }).ToList();
        }
        public static async Task<List<string>> Columns(string connection, TableName table)
        {
            // Read the actual result schema without loading any table rows.
            var data = await Query(connection, "SELECT TOP (0) * FROM " + table.Sql);
            return data.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
        }
        public static Task<DataTable> Read(string connection, TableName table, string[] columns, string filter, int limit)
        {
            // The predicate is user SQL. A read-only SQL account is required; this is not a SQL sandbox.
            if (filter.IndexOf(';') >= 0 || filter.Contains("--") || filter.Contains("/*") || filter.Contains("*/"))
                throw new ArgumentException("조회 조건에는 세미콜론이나 SQL 주석을 사용할 수 없습니다.");
            var sql = "SELECT TOP (" + (limit + 1) + ") " + string.Join(",", columns.Select(Quote)) + " FROM " + table.Sql;
            if (!string.IsNullOrWhiteSpace(filter))
                sql += " WHERE (" + filter + ")";
            return Query(connection, sql);
        }
    }
}
