using System;
using System.Collections.Generic;
using System.Linq;
using Stef.DatabaseQuery.Business.Interfaces;

namespace Stef.DatabaseQuery.Business.Managers.Databases
{
    public class SchemaInfo
    {
        private List<Table> _Tables;
        private Dictionary<string, Table> _TableDic;
        private Dictionary<string, List<Column>> _ColumnDic;
        private Dictionary<string, Column> _TableColumnDic;

        public SchemaInfo()
        {
        }

        public Table GetTable(string tableName)
        {
            Table result;

            _TableDic.TryGetValue(tableName.ToLower(), out result);

            return result;
        }
        public IEnumerable<Table> GetTables()
        {
            return _Tables;
        }
        public Column GetColumn(string tableName, string columnName)
        {
            Column result;
            var key = string.Concat(tableName.ToLower(), ";", columnName.ToLower());

            _TableColumnDic.TryGetValue(key, out result);

            return result;
        }
        public IEnumerable<Column> GetColumns(string tableName)
        {
            tableName = tableName.ToLower();

            List<Column> columns;

            if (!_ColumnDic.TryGetValue(tableName, out columns))
                return new List<Column>();

            return columns;
        }

        public void RefreshTablesAndColumns(IDatabaseProvider provider, string connectionString)
        {
            using (var connection = provider.CreateConnection(connectionString))
            {
                _Tables = provider.GetTables(connection);

                _TableDic = _Tables
                    .ToDictionary(c => c.TableName.ToLower());

                _ColumnDic = provider
                    .GetColumns(connection)
                    .GroupBy(c => c.TableName.ToLower())
                    .ToDictionary(c => c.Key, c => c.ToList());

                _TableColumnDic = _ColumnDic
                    .Values
                    .SelectMany(c => c)
                    .GroupBy(c => string.Concat(c.TableName.ToLower(), ";", c.ColumnName.ToLower()))
                    .ToDictionary(c => c.Key, c => c.FirstOrDefault());

                provider
                    .GetRelations(connection)
                    .ForEach(c =>
                    {
                        Column parentColumn = GetColumn(c.ParentTable, c.ParentColumn);
                        Column childColumn = GetColumn(c.ChildTable, c.ChildColumn);

                        if (parentColumn == null || childColumn == null)
                            return;

                        childColumn.RelatedTableName = parentColumn.TableName;
                        childColumn.RelatedColumnName = parentColumn.ColumnName;
                    });
            }
        }
    }
}
