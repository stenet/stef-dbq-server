using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Stef.DatabaseQuery.Business.Enumerations;
using Stef.DatabaseQuery.Business.Managers.Databases;

namespace Stef.DatabaseQuery.Business.Managers.Sqls
{
    public class SqlSelectToken : SqlToken
    {
        public SqlSelectToken(DatabaseInfo databaseInfo, string sql)
            : this(databaseInfo, sql, 0, 0)
        {

        }
        public SqlSelectToken(DatabaseInfo databaseInfo, string script, int start, int end)
            : base(databaseInfo, ScriptType.Query, script, start, end)
        {
            TableList = new List<SqlTableToken>();
            ColumnList = new List<SqlColumnToken>();

            ColumnKeyList = new List<SqlColumnToken>();
            ColumnSaveList = new List<SqlColumnToken>();

            ExtractSegments();
        }

        public string From { get; private set; }
        public string Select { get; private set; }
        public string Order { get; private set; }
        public string Group { get; private set; }
        public string Having { get; private set; }
        public string Where { get; private set; }

        public List<SqlTableToken> TableList { get; private set; }
        public List<SqlColumnToken> ColumnList { get; private set; }

        public SqlTableToken TableSave { get; internal set; }
        public List<SqlColumnToken> ColumnKeyList { get; private set; }
        public List<SqlColumnToken> ColumnSaveList { get; private set; }

        public bool CanSave()
        {
            return TableSave != null;
        }
        public bool CanEdit(string fieldName)
        {
            return CanEdit(fieldName, RowState.New);
        }
        public bool CanEdit(string fieldName, RowState rowState)
        {
            var canEdit = ColumnSaveList
                .Any(c => c.InternalFieldName == fieldName);

            if (canEdit)
                return true;

            if (rowState == RowState.New)
            {
                canEdit = ColumnKeyList
                    .Any(c => c.InternalFieldName == fieldName);

                if (canEdit)
                    return true;
            }

            return false;
        }

        private void ExtractSegments()
        {
            var matches = Regex.Matches(Script, @"\b(select|from|where|group|having|order)\b", RegexOptions.IgnoreCase | RegexOptions.Multiline);

            var checkedMatches = matches
                .OfType<Match>()
                .Where(c =>
                    Utils.IsOuterSegment(Script, c.Index))
                .OrderBy(c => c.Index)
                .ToList();

            for (var i = 0; i < checkedMatches.Count; i++)
            {
                var match = checkedMatches[i];
                var start = match.Index + match.Length;

                var length = i + 1 == checkedMatches.Count
                    ? Script.Length
                    : checkedMatches[i + 1].Index;

                var text = Script
                    .Substring(start, length - start)
                    .TrimStart()
                    .TrimEnd();

                switch (match.Value.ToLower())
                {
                    case "select":
                        Select = text;
                        break;
                    case "from":
                        From = text;
                        break;
                    case "order":
                        Order = text;
                        break;
                    case "group":
                        Group = text;
                        break;
                    case "having":
                        Having = text;
                        break;
                    case "where":
                        Where = text;
                        break;
                    default:
                        break;
                }
            }

            ExtractTables();
            ExtractColumns();

            ExtractCanUpdate();
        }
        private void ExtractTables()
        {
            var tableList = new List<SqlTableToken>();

            if (!string.IsNullOrEmpty(From))
            {
                var itemList = Utils.GetCommaSeparatedItemList(From);

                foreach (var item in itemList)
                {
                    if (item.Any(c => char.IsWhiteSpace(c)))
                    {
                        var split = Regex.Split(item, @"\s+");

                        if (split.Length == 2)
                            tableList.Add(new SqlTableToken(split[0], split[1], true));
                        else
                            tableList.Add(new SqlTableToken(item, null, false));
                    }
                    else
                    {
                        tableList.Add(new SqlTableToken(item, null, true));
                    }
                }
            }

            TableList = tableList;
        }
        private void ExtractColumns()
        {
            var columnList = new List<SqlColumnToken>();

            if (!string.IsNullOrEmpty(Select))
            {
                var asRegex = new Regex(@"\b(as)\b", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                var itemList = Utils.GetCommaSeparatedItemList(Select);

                var index = 0;
                foreach (var item in itemList)
                {
                    index++;

                    string internalFieldName = string.Concat("f", index);
                    var column = item;
                    var titleAlias = (string)null;

                    var asMatches = asRegex
                        .Matches(column)
                        .OfType<Match>()
                        .Where(c =>
                            Utils.IsOuterSegment(column, c.Index))
                        .ToList();

                    if (asMatches.Count == 1)
                    {
                        titleAlias = column.Substring(asMatches[0].Index + asMatches[0].Length).TrimStart().TrimEnd();

                        if ((titleAlias.StartsWith("'") && titleAlias.EndsWith("'"))
                            || (titleAlias.StartsWith("\"") && titleAlias.EndsWith("\"")))
                            titleAlias = titleAlias.Substring(1, titleAlias.Length - 2);

                        column = column.Substring(0, asMatches[0].Index).TrimEnd();
                    }

                    if (column.Any(c => char.IsWhiteSpace(c)))
                    {
                        var columnInfo = GetColumnInfo(null, column);
                        columnList.Add(
                            new SqlColumnToken(
                                column, 
                                null, 
                                titleAlias, 
                                internalFieldName, 
                                columnInfo?.Type, 
                                columnInfo?.RelatedTableName, 
                                columnInfo?.RelatedColumnName, 
                                false));
                    }
                    else if (column.Contains("."))
                    {
                        var split = column.Split('.');

                        if (split.Length == 2)
                        {
                            var columnInfo = GetColumnInfo(split[0], split[1]);
                            columnList.Add(
                                new SqlColumnToken(
                                    split[1], 
                                    split[0], 
                                    titleAlias, 
                                    internalFieldName, 
                                    columnInfo?.Type,
                                    columnInfo?.RelatedTableName,
                                    columnInfo?.RelatedColumnName, 
                                    true));
                        }
                        else
                        {
                            var columnInfo = GetColumnInfo(null, column);

                            columnList.Add(
                                new SqlColumnToken(
                                    column,
                                    null,
                                    titleAlias,
                                    internalFieldName,
                                    columnInfo?.Type,
                                    columnInfo?.RelatedTableName,
                                    columnInfo?.RelatedColumnName,
                                    false));
                        }
                    }
                    else
                    {
                        var columnInfo = GetColumnInfo(null, column);
                        columnList.Add(
                            new SqlColumnToken(
                                column, 
                                null, 
                                titleAlias, 
                                internalFieldName, 
                                columnInfo?.Type,
                                columnInfo?.RelatedTableName,
                                columnInfo?.RelatedColumnName, 
                                true));
                    }
                }
            }

            ColumnList = columnList;
        }
        private void ExtractCanUpdate()
        {
            if (DatabaseInfo == null)
                return;

            var tableSave = TableList.FirstOrDefault();

            if (tableSave == null)
                return;

            if (!tableSave.IsValid)
                return;

            var columnSaveList = ColumnList
                .Where(c =>
                    c.Alias == tableSave.Alias
                    || c.Alias == tableSave.TableName)
                .ToList();

            var table = DatabaseInfo
                .SchemaInfo
                .GetTable(tableSave.TableName);

            if (table == null)
                return;

            var dbColumnPKList = new List<string>();

            if (!string.IsNullOrEmpty(table.PrimaryKeyColumn))
            {
                dbColumnPKList.Add(table.PrimaryKeyColumn);
            }

            var columnPKList = new List<SqlColumnToken>();

            foreach (var column in dbColumnPKList)
            {
                var pkColumns = columnSaveList
                    .Where(c => c.ColumnName.Replace("\"", string.Empty).ToLower() == column.ToLower())
                    .ToList();

                if (pkColumns.Count > 1)
                    return;
                if (pkColumns.Count == 0)
                    continue;

                columnPKList.Add(pkColumns.First());
                columnSaveList.Remove(pkColumns.First());
            }

            foreach (var column in columnSaveList.ToList())
            {
                var dbColumn = DatabaseInfo
                    .SchemaInfo
                    .GetColumns(table.TableName)
                    .FirstOrDefault(c => c.ColumnName.ToLower() == column.ColumnName.Replace("\"", string.Empty).ToLower());

                if (dbColumn == null || dbColumn.Type == null)
                    columnSaveList.Remove(column);
            }

            TableSave = tableSave;

            ColumnKeyList = columnPKList.ToList();
            ColumnSaveList = columnSaveList.ToList();
        }
        private Column GetColumnInfo(string alias, string columnName)
        {
            if (columnName.StartsWith("\"") && columnName.EndsWith("\""))
                columnName = columnName.Substring(1, columnName.Length - 2);

            if (string.IsNullOrEmpty(alias))
            {
                var tableName = TableList.FirstOrDefault()?.TableName;
                if (string.IsNullOrEmpty(tableName))
                    return null;

                var column = DatabaseInfo.SchemaInfo.GetColumn(tableName, columnName);

                return column;
            }
            else
            {
                alias = alias.ToLower();

                var tableName = TableList
                    .FirstOrDefault(c => c.Alias.ToLower() == alias)?
                    .TableName;

                if (string.IsNullOrEmpty(tableName))
                {
                    tableName = TableList
                        .FirstOrDefault(c => c.TableName.ToLower() == alias)?
                        .TableName;
                }

                if (string.IsNullOrEmpty(tableName))
                {
                    return null;
                }

                var column = DatabaseInfo.SchemaInfo.GetColumn(tableName, columnName);

                return column;               
            }
        }
    }
}
