using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using Stef.DatabaseQuery.Business.Interfaces;

namespace Stef.DatabaseQuery.Business.Managers.Databases
{
    public class DatabaseInfo
    {
        private object _Sync = new object();
        private SchemaInfo _SchemaInfo;

        public DatabaseInfo(int id, IDatabaseProvider provider, string caption, string connectionString)
        {
            Id = id;
            Provider = provider;
            Caption = caption;
            ConnectionString = connectionString;
        }

        [JsonProperty("id")]
        public int Id { get; private set; }
        [JsonIgnore]
        public IDatabaseProvider Provider { get; private set; }
        [JsonProperty("caption")]
        public string Caption { get; private set; }
        [JsonIgnore]
        public string ConnectionString { get; private set; }

        public SchemaInfo SchemaInfo
        {
            get
            {
                if (_SchemaInfo == null)
                {
                    lock (_Sync)
                    {
                        if (_SchemaInfo == null)
                        {
                            var schemaInfo = new SchemaInfo();
                            schemaInfo.RefreshTablesAndColumns(Provider, ConnectionString);

                            _SchemaInfo = schemaInfo;
                        }
                    }
                }

                return _SchemaInfo;
            }
        }

        public IDbConnection CreateConnection()
        {
            var connection = Provider.CreateConnection(ConnectionString);

            if (connection.State != ConnectionState.Open)
                connection.Open();

            return connection;
        }
        public void UpdateData(IDatabaseProvider provider, string caption, string connectionString)
        {
            Provider = provider;
            Caption = caption;
            ConnectionString = connectionString;

            lock (_Sync)
            {
                _SchemaInfo = null;
            }
        }
    }
}
