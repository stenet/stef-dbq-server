using Stef.DatabaseQuery.Business.Managers.Databases;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Linq;

namespace Stef.DatabaseQuery.Business.Interfaces
{
    [InheritedExport]
    public interface IDatabaseProvider
    {
        string Name { get; }
        string ParameterPrefix { get; }

        List<Table> GetTables(IDbConnection connection);
        List<Column> GetColumns(IDbConnection connection);
        List<Relation> GetRelations(IDbConnection connection);

        void CreateTableIfNotExists(IDbConnection connection, Table table, List<Column> columns);

        IDbConnection CreateConnection(string connectionString);

        string GetSafeColumnName(string columnName);

        object ConvertFromStorageType(object value, Type type);
        object ConvertToStorageType(object value, Type type);
    }
}
