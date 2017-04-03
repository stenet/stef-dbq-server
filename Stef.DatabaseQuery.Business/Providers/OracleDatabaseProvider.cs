using Stef.DatabaseQuery.Business.Interfaces;
using Stef.DatabaseQuery.Business.Managers.Databases;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Stef.DatabaseQuery.Business.Providers
{
    public class OracleDatabaseProvider : IDatabaseProvider
    {
        private Dictionary<string, object> _ReservedWordsDic;
        private Regex _RegexLowerCase;

        public OracleDatabaseProvider()
        {
            _ReservedWordsDic = Properties
                .Resources
                .OracleReservedWords
                .Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)
                .ToDictionary(c => c.ToLower(), c => (object)null);

            _RegexLowerCase = new Regex("[a-z]");
        }

        public string Name
        {
            get
            {
                return "Oracle";
            }
        }
        public string ParameterPrefix
        {
            get
            {
                return ":";
            }
        }

        public IDbConnection CreateConnection(string connectionString)
        {
            var connection = new System.Data.OracleClient.OracleConnection(connectionString);
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
                    @"select table_name, 'TABLE'
                        from user_tables

                        union

                    select view_name, 'VIEW'
                        from user_views";

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
                    @"select table_name, column_name, data_type, nvl(data_precision, data_length), nullable
                        from user_tab_columns
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
                                reader.GetString(4) == "Y"));
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
                    @"SELECT c_pk.table_name, b.column_name, a.table_name, a.column_name
                        FROM user_cons_columns a
                        join user_constraints c on a.owner = c.owner and a.constraint_name = c.constraint_name
                        join user_constraints c_pk on c.r_owner = c_pk.owner and c.r_constraint_name = c_pk.constraint_name
                        join user_cons_columns b on C_PK.owner = b.owner and C_PK.CONSTRAINT_NAME = b.constraint_name and b.POSITION = a.POSITION     
                        where c.constraint_type = 'R'";

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
                    @"SELECT concat('P_', cons.constraint_name), cols.table_name, cols.column_name, 1, 1
                        FROM user_constraints cons, user_cons_columns cols
                        WHERE cons.constraint_type = 'P'
                        AND cons.constraint_name = cols.constraint_name
                        AND cons.owner = cols.owner

                        union

                        select 
                        concat('I_', a.index_name), a.table_name, a.column_name, 0, 1
                        from user_ind_columns a, user_indexes b
                        where a.index_name = b.index_name 
                        and b.uniqueness = 'UNIQUE'";

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
                                reader.GetInt32(3) == 1,
                                reader.GetInt32(4) == 1));
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
            var tables = GetTables(connection);
            var exists = tables.Any(c => c.TableName == table.TableName);

            if (!exists)
            {
                var sb = new StringBuilder();
                sb.Append("create table ");
                sb.Append(table.TableName);
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
            else if (type == typeof(Guid))
            {
                return Guid.Parse(value.ToString());
            }

            return value;
        }
        public object ConvertToStorageType(object value, Type type)
        {
            if (value == null || value.ToString() == string.Empty)
                return DBNull.Value;

            if (type == typeof(DateTime))
                return DateTime.Parse(value.ToString());
            else if (value is Guid)
                return ((Guid)value).ToString();

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
            else if (_RegexLowerCase.IsMatch(columnName))
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
                case "DATE":
                case "TIMESTAMP":
                case "TIMESTAMP WITH TIME ZONE":
                case "TIMESTAMP WITH LOCAL TIME ZONE":
                    return typeof(DateTime);
                case "NUMBER":
                case "FLOAT":
                    return typeof(decimal);
                case "CHAR":
                case "VARCHAR":
                case "VARCHAR2":
                case "NCHAR":
                case "NVARCHAR2":
                case "LONG":
                case "CLOB":
                case "NCLOB":
                    return typeof(string);
                case "BLOB":
                    return typeof(byte[]);
                default:
                    return typeof(string);
            }
        }
        private string GetSqlColumnTypeFromType(Type type, int maxLength)
        {
            if (type == typeof(bool))
                return "NUMBER(1,0)";
            else if (type == typeof(DateTime))
                return "DATE";
            else if (type == typeof(decimal) || type == typeof(double))
                return "NUMBER(19,5)";
            else if (type == typeof(int))
                return "NUMBER";
            else if (type == typeof(long))
                return "NUMBER";
            else if (type == typeof(short))
                return "NUMBER";
            else if (type == typeof(string))
                if (maxLength <= 0)
                    return "NCLOB";
                else
                    return $"NVARCHAR2({maxLength})";
            else if (type == typeof(Guid))
                return "NVARCHAR2(50)";
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
            sb.Append(GetSqlColumnTypeFromType(column.Type, column.MaxLength));


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
