using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Stef.DatabaseQuery.Business.Interfaces;
using Stef.DatabaseQuery.Business.Managers.Databases;
using System.Text;

namespace Stef.DatabaseQuery.Business.Providers
{
    public class SqlDatabaseProvider : IDatabaseProvider
    {
        private Dictionary<string, object> _ReservedWordsDic;

        public SqlDatabaseProvider()
        {
            _ReservedWordsDic = Properties
                .Resources
                .SqlReservedWords
                .Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)
                .ToDictionary(c => c.ToLower(), c => (object)null);
        }

        public string Name
        {
            get
            {
                return "SQL-Server";
            }
        }
        public string ParameterPrefix
        {
            get
            {
                return "@";
            }
        }

        public IDbConnection CreateConnection(string connectionString)
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();

            return connection;
        }

        public List<Table> GetTables(IDbConnection connection)
        {
            var primaryKeyDic = GetTablePrimaryKeys(connection);

            Func<string, string> getPrimaryKeyColumn = (tableName) =>
            {
                string columnName;

                primaryKeyDic.TryGetValue(tableName, out columnName);

                return columnName;
            };

            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"select concat(table_schema, '.', table_name), table_type
                        from information_schema.tables";

                using (var reader = command.ExecuteReader())
                {
                    var tables = new List<Table>();

                    while (reader.Read())
                    {
                        tables.Add(
                            new Table(
                                reader.GetString(0),
                                reader.GetString(1) == "VIEW",
                                getPrimaryKeyColumn(reader.GetString(0))));
                    }

                    return tables;
                }
            }
        }
        public List<Column> GetColumns(IDbConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"select concat(table_schema, '.', table_name), column_name, data_type, character_maximum_length, is_nullable
                        from information_schema.columns
                        order by 1, 2";

                using (var reader = command.ExecuteReader())
                {
                    var columns = new List<Column>();

                    while (reader.Read())
                    {
                        columns.Add(
                            new Column(
                                reader.GetString(0),
                                reader.GetString(1),
                                GetTypeFromSqlColumnType(reader.GetString(2)),
                                reader.GetString(2),
                                reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                                reader.GetString(4) == "YES"));
                    }

                    return columns;
                }
            }
        }
        public List<Relation> GetRelations(IDbConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT concat(object_schema_name(fk.referenced_object_id), '.', object_name(fk.referenced_object_id)), c2.name, concat(object_schema_name(fk.parent_object_id), '.', object_name(fk.parent_object_id)), c1.name
                        FROM sys.foreign_keys fk
                        inner join sys.foreign_key_columns fkc on fkc.constraint_object_id = fk.object_id
                        inner join sys.columns c1 on fkc.parent_column_id = c1.column_id AND fkc.parent_object_id = c1.object_id
                        inner join sys.columns c2 ON fkc.referenced_column_id = c2.column_id AND fkc.referenced_object_id = c2.object_id";

                using (var reader = command.ExecuteReader())
                {
                    var relations = new List<Relation>();

                    while (reader.Read())
                    {
                        relations.Add(
                            new Relation(
                                reader.GetString(0),
                                reader.GetString(1),
                                reader.GetString(2),
                                reader.GetString(3)));
                    }

                    return relations;
                }
            }
        }
        private Dictionary<string, string> GetTablePrimaryKeys(IDbConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"select ind.name, concat(object_schema_name(ind.object_id), '.', object_name(ind.object_id)), col.name, ind.is_primary_key, ind.is_unique
                        from sys.indexes ind
                        inner join sys.index_columns ic on ind.object_id = ic.object_id and ind.index_id = ic.index_id
                        inner join sys.columns col on ic.object_id = col.object_id and ic.column_id = col.column_id
                        inner join sys.tables t on ind.object_id = t.object_id
                        where t.is_ms_shipped = 0 and (ind.is_primary_key = 1 or ind.is_unique = 1)";

                using (var reader = command.ExecuteReader())
                {
                    var primaryKeys = new List<PrimaryKeyHelper>();

                    while (reader.Read())
                    {
                        primaryKeys.Add(
                            new PrimaryKeyHelper(
                                reader.GetString(0),
                                reader.GetString(1),
                                reader.GetString(2),
                                reader.GetBoolean(3),
                                reader.GetBoolean(4)));
                    }

                    var result = new Dictionary<string, string>();

                    primaryKeys
                        .Where(c => c.IsPrimaryKey)
                        .GroupBy(c => c.TableName)
                        .Where(c => c.Count() == 1)
                        .ToList()
                        .ForEach(c => result.Add(c.Key, c.First().ColumnName));

                    primaryKeys
                        .Where(c =>
                            c.IsPrimaryKey == false
                            && !result.ContainsKey(c.TableName))
                        .GroupBy(c => new
                        {
                            IndexName = c.IndexName,
                            TableName = c.TableName
                        })
                        .Where(c => c.Count() == 1)
                        .SelectMany(c => c)
                        .GroupBy(c => c.TableName)
                        .ToList()
                        .ForEach(c => result.Add(c.Key, c.First().ColumnName));


                    return result;
                }
            }
        }

        public void CreateTableIfNotExists(IDbConnection connection, Table table, List<Column> columns)
        {
            string tableName = string.Concat("dbo.", table.TableName);

            var tables = GetTables(connection);
            var exists = tables.Any(c => c.TableName == tableName);

            if (!exists)
            {
                var sb = new StringBuilder();
                sb.Append("create table ");
                sb.Append(tableName);
                sb.Append(" (");

                sb.Append(string.Join(", ", columns.Select(c => GetCreateColumn(c))));
                sb.Append(")");

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sb.ToString();
                    command.ExecuteNonQuery();
                }
            }
        }

        public object ConvertFromStorageType(object value, Type type)
        {
            if (value is DBNull)
                return null;

            if (value is DateTime)
            {
                var date = (DateTime)value;

                if (date.Hour != 0 || date.Minute != 0 || date.Second != 0)
                    return date.ToString("dd.MM.yyyy HH:mm:ss");
                else
                    return date.ToString("dd.MM.yyyy");
            }

            return value;
        }
        public object ConvertToStorageType(object value, Type type)
        {
            if (value == null || value.ToString() == string.Empty)
                return DBNull.Value;

            if (type == typeof(DateTime))
                return DateTime.Parse(value.ToString());

            return value;
        }

        public string GetSafeColumnName(string columnName)
        {
            if (columnName.Contains(" "))
            {
                return string.Concat("\"", columnName, "\"");
            }
            else if (_ReservedWordsDic.ContainsKey(columnName.ToLower()))
            {
                return string.Concat("\"", columnName, "\"");
            }
            else
            {
                return columnName.ToLower();
            }
        }
        private Type GetTypeFromSqlColumnType(string type)
        {
            switch (type)
            {
                case "bit":
                    return typeof(bool);
                case "date":
                case "datetime":
                    return typeof(DateTime);
                case "time":
                    return typeof(TimeSpan);
                case "decimal":
                case "money":
                case "smallmoney":
                case "numeric":
                    return typeof(decimal);
                case "int":
                    return typeof(int);
                case "smallint":
                case "tinyint":
                    return typeof(short);
                case "bigint":
                    return typeof(long);
                case "nchar":
                case "nvarchar":
                case "varchar":
                case "xml":
                    return typeof(string);
                case "uniqueidentifier":
                    return typeof(Guid);
                case "varbinary":
                    return typeof(byte[]);
                default:
                    return typeof(string);
            }
        }
        private string GetSqlColumnTypeFromType(Type type)
        {
            if (type == typeof(bool))
                return "bit";
            else if (type == typeof(DateTime))
                return "datetime";
            else if (type == typeof(decimal) || type == typeof(double))
                return "money";
            else if (type == typeof(double))
                return "money";
            else if (type == typeof(int))
                return "int";
            else if (type == typeof(long))
                return "bigint";
            else if (type == typeof(short))
                return "tinyint";
            else if (type == typeof(string))
                return "nvarchar";
            else if (type == typeof(Guid))
                return "uniqueidentifier";
            else
                throw new NotImplementedException(type.FullName);
        }
        private string GetCreateColumn(Column column)
        {
            var sb = new StringBuilder();
            sb.Append("\"");
            sb.Append(column.ColumnName);
            sb.Append("\"");
            sb.Append(" ");
            sb.Append(GetSqlColumnTypeFromType(column.Type));

            if (column.Type == typeof(string))
            {
                if (column.MaxLength <= 0)
                {
                    sb.Append("(max)");
                }
                else
                {
                    sb.Append("(");
                    sb.Append(column.MaxLength);
                    sb.Append(")");
                }
            }

            if (!column.IsNullable)
                sb.Append(" NOT NULL");

            return sb.ToString();
        }

        private class PrimaryKeyHelper
        {
            public PrimaryKeyHelper(string indexName, string tableName, string columnName, bool isPrimaryKey, bool isUnique)
            {
                IndexName = indexName;
                TableName = tableName;
                ColumnName = columnName;
                IsPrimaryKey = isPrimaryKey;
                IsUnique = isUnique;
            }

            public string IndexName { get; private set; }
            public string TableName { get; private set; }
            public string ColumnName { get; private set; }
            public bool IsPrimaryKey { get; private set; }
            public bool IsUnique { get; private set; }
        }
    }
}
