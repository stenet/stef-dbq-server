using Newtonsoft.Json.Linq;
using Stef.DatabaseQuery.Business.Enumerations;
using Stef.DatabaseQuery.Business.Interfaces;
using Stef.DatabaseQuery.Business.Managers.Repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Stef.DatabaseQuery.Business.Managers
{
    public class RepositoryManager
    {
        private static object _SyncLock = new object();
        private static RepositoryManager _Instance;

        private IDatabaseProvider _DatabaseProvider;
        private string _ConnectionString;

        private RepositoryManager()
        {
        }

        public static RepositoryManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_SyncLock)
                    {
                        if (_Instance == null)
                        {
                            _Instance = new RepositoryManager();
                        }
                    }
                }

                return _Instance;
            }
        }

        public IDbConnection CreateConnection()
        {
            if (_DatabaseProvider == null)
            {
                lock (_SyncLock)
                {
                    if (_DatabaseProvider == null)
                    {
                        var dbqConnection = System.Configuration.ConfigurationManager.ConnectionStrings["DBQ"];
                        var connectionString = dbqConnection.ConnectionString;
                        var tokens = connectionString.Split(';').ToList();

                        var providerToken = tokens.FirstOrDefault(c => c.StartsWith("ProviderName="));
                        tokens.Remove(providerToken);

                        providerToken = providerToken.Remove(0, "ProviderName=".Length);

                        _ConnectionString = string.Join(";", tokens);

                        _DatabaseProvider = CompositionManager
                            .Instance
                            .GetInstance<IDatabaseProvider>(providerToken);

                        CreateTable();
                    }
                }
            }

            return _DatabaseProvider.CreateConnection(_ConnectionString);
        }

        public List<RepositoryItem> Get(RepositoryItemType? type = null, Guid? id = null)
        {
            using (var connection = CreateConnection())
            {
                var command = connection.CreateCommand();

                var sb = new StringBuilder();
                sb.Append("select \"ID\", \"TYPE\", \"NAME\", \"DATA\", \"CREATION_DATE\", \"CREATOR\", \"MODIFICATION_DATE\", \"MODIFIER\" ");
                sb.Append("from \"SETTINGS\" ");

                var whereList = new List<string>();

                {
                    whereList.Add($"\"IS_DELETED\" = {_DatabaseProvider.ParameterPrefix}p_isDeleted");
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "p_isDeleted";
                    parameter.Value = _DatabaseProvider.ConvertToStorageType(false, typeof(bool));
                    command.Parameters.Add(parameter);
                }
                if (type.HasValue)
                {
                    whereList.Add($"\"TYPE\" = {_DatabaseProvider.ParameterPrefix}p_type");
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "p_type";
                    parameter.Value = _DatabaseProvider.ConvertToStorageType((int)type.Value, typeof(int));
                    command.Parameters.Add(parameter);
                }
                if (id.HasValue)
                {
                    whereList.Add($"\"ID\" = {_DatabaseProvider.ParameterPrefix}p_id");
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "p_id";
                    parameter.Value = _DatabaseProvider.ConvertToStorageType(id.Value, typeof(Guid));
                    command.Parameters.Add(parameter);
                }

                if (whereList.Any())
                {
                    sb.Append("WHERE ");
                    sb.Append(string.Join(" and ", whereList));
                }

                command.CommandText = sb.ToString();
                var resultList = new List<RepositoryItem>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        resultList.Add(
                            new RepositoryItem()
                            {
                                Id = reader.GetGuid(0),
                                Type = (RepositoryItemType)reader.GetInt32(1),
                                Name = reader.GetString(2),
                                Data = reader.GetString(3),
                                CreationDate = reader.GetDateTime(4),
                                Creator = reader.GetString(5),
                                ModificationDate = reader.GetDateTime(6),
                                Modifier = reader.GetString(7)
                            });
                    }
                }

                return resultList;
            }
        }
        public Guid Save(RepositoryItem item)
        {
            if (item.Id == Guid.Empty)
            {
                return Insert(item);
            }
            else
            {
                return Update(item);
            }
        }
        private Guid Insert(RepositoryItem item)
        {
            using (var connection = CreateConnection())
            {
                var command = connection.CreateCommand();

                var sb = new StringBuilder();
                sb.Append("insert into \"SETTINGS\" (\"ID\", \"TYPE\", \"NAME\", \"DATA\", \"IS_DELETED\", \"CREATION_DATE\", \"CREATOR\", \"MODIFICATION_DATE\", \"MODIFIER\") values ( ");
                sb.Append(_DatabaseProvider.ParameterPrefix);
                sb.Append("p_id, ");
                sb.Append(_DatabaseProvider.ParameterPrefix);
                sb.Append("p_type, ");
                sb.Append(_DatabaseProvider.ParameterPrefix);
                sb.Append("p_name, ");
                sb.Append(_DatabaseProvider.ParameterPrefix);
                sb.Append("p_data, ");
                sb.Append(_DatabaseProvider.ParameterPrefix);
                sb.Append("p_isDeleted, ");
                sb.Append(_DatabaseProvider.ParameterPrefix);
                sb.Append("p_creationDate, ");
                sb.Append(_DatabaseProvider.ParameterPrefix);
                sb.Append("p_creator, ");
                sb.Append(_DatabaseProvider.ParameterPrefix);
                sb.Append("p_modificationDate, ");
                sb.Append(_DatabaseProvider.ParameterPrefix);
                sb.Append("p_modifier)");

                item.Id = Guid.NewGuid();

                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "p_id";
                    parameter.Value = _DatabaseProvider.ConvertToStorageType(item.Id, typeof(Guid));
                    command.Parameters.Add(parameter);
                }
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "p_type";
                    parameter.Value = _DatabaseProvider.ConvertToStorageType((int)item.Type, typeof(int));
                    command.Parameters.Add(parameter);
                }
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "p_name";
                    parameter.Value = _DatabaseProvider.ConvertToStorageType(item.Name, typeof(string));
                    command.Parameters.Add(parameter);
                }
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "p_data";
                    parameter.Value = _DatabaseProvider.ConvertToStorageType(item.Data, typeof(string));
                    command.Parameters.Add(parameter);
                }
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "p_creationDate";
                    parameter.Value = _DatabaseProvider.ConvertToStorageType(DateTime.Now, typeof(DateTime));
                    command.Parameters.Add(parameter);
                }
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "p_creator";
                    parameter.Value = _DatabaseProvider.ConvertToStorageType(SecurityManager.Instance.GetCurrentUser(), typeof(string));
                    command.Parameters.Add(parameter);
                }
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "p_modificationDate";
                    parameter.Value = _DatabaseProvider.ConvertToStorageType(DateTime.Now, typeof(DateTime));
                    command.Parameters.Add(parameter);
                }
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "p_modifier";
                    parameter.Value = _DatabaseProvider.ConvertToStorageType(SecurityManager.Instance.GetCurrentUser(), typeof(string));
                    command.Parameters.Add(parameter);
                }
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "p_isDeleted";
                    parameter.Value = _DatabaseProvider.ConvertToStorageType(false, typeof(bool));
                    command.Parameters.Add(parameter);
                }

                command.CommandText = sb.ToString();
                command.ExecuteNonQuery();
            }

            return item.Id;
        }
        private Guid Update(RepositoryItem item)
        {
            using (var connection = CreateConnection())
            {
                var command = connection.CreateCommand();

                var sb = new StringBuilder();
                sb.Append("update \"SETTINGS\" set \"NAME\" = ");
                sb.Append(_DatabaseProvider.ParameterPrefix);
                sb.Append("p_name, \"DATA\" = ");
                sb.Append(_DatabaseProvider.ParameterPrefix);
                sb.Append("p_data, \"MODIFICATION_DATE\" = ");
                sb.Append(_DatabaseProvider.ParameterPrefix);
                sb.Append("p_modificationDate, \"MODIFIER\" = ");
                sb.Append(_DatabaseProvider.ParameterPrefix);
                sb.Append("p_modifier where \"ID\" = ");
                sb.Append(_DatabaseProvider.ParameterPrefix);
                sb.Append("p_id");

                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "p_name";
                    parameter.Value = _DatabaseProvider.ConvertToStorageType(item.Name, typeof(string));
                    command.Parameters.Add(parameter);
                }
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "p_data";
                    parameter.Value = _DatabaseProvider.ConvertToStorageType(item.Data, typeof(string));
                    command.Parameters.Add(parameter);
                }
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "p_modificationDate";
                    parameter.Value = _DatabaseProvider.ConvertToStorageType(DateTime.Now, typeof(DateTime));
                    command.Parameters.Add(parameter);
                }
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "p_modifier";
                    parameter.Value = _DatabaseProvider.ConvertToStorageType(SecurityManager.Instance.GetCurrentUser(), typeof(string));
                    command.Parameters.Add(parameter);
                }
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "p_id";
                    parameter.Value = _DatabaseProvider.ConvertToStorageType(item.Id, typeof(Guid));
                    command.Parameters.Add(parameter);
                }

                command.CommandText = sb.ToString();
                command.ExecuteNonQuery();
            }

            return item.Id;
        }
        public void Delete(Guid id)
        {
            using (var connection = CreateConnection())
            {
                var command = connection.CreateCommand();

                var sb = new StringBuilder();
                sb.Append("update \"SETTINGS\" set \"IS_DELETED\" = ");
                sb.Append(_DatabaseProvider.ParameterPrefix);
                sb.Append("p_isDeleted where \"ID\" = ");
                sb.Append(_DatabaseProvider.ParameterPrefix);
                sb.Append("p_id");

                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "p_isDeleted";
                    parameter.Value = _DatabaseProvider.ConvertToStorageType(true, typeof(bool));
                    command.Parameters.Add(parameter);
                }
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "p_id";
                    parameter.Value = _DatabaseProvider.ConvertToStorageType(id, typeof(Guid));
                    command.Parameters.Add(parameter);
                }

                command.CommandText = sb.ToString();
                command.ExecuteNonQuery();
            }
        }

        private void CreateTable()
        {
            using (var connection = CreateConnection())
            {
                const string TABLE_NAME = "SETTINGS";

                _DatabaseProvider.CreateTableIfNotExists(
                    connection,
                    new Databases.Table(TABLE_NAME, false, "ID"),
                    new List<Databases.Column>()
                    {
                        new Databases.Column(TABLE_NAME, "ID", typeof(Guid), null, 0, false),
                        new Databases.Column(TABLE_NAME, "TYPE", typeof(int), null, 0, false),
                        new Databases.Column(TABLE_NAME, "NAME", typeof(string), null, 100, false),
                        new Databases.Column(TABLE_NAME, "DATA", typeof(string), null, 0, false),
                        new Databases.Column(TABLE_NAME, "CREATION_DATE", typeof(DateTime), null, 0, false),
                        new Databases.Column(TABLE_NAME, "CREATOR", typeof(string), null, 50, false),
                        new Databases.Column(TABLE_NAME, "MODIFICATION_DATE", typeof(DateTime), null, 0, false),
                        new Databases.Column(TABLE_NAME, "MODIFIER", typeof(string), null, 50, false),
                        new Databases.Column(TABLE_NAME, "IS_DELETED", typeof(bool), null, 0, false)
                    });
            }
        }
    }
}
