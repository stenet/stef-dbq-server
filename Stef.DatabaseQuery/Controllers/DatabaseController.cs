using Newtonsoft.Json.Linq;
using Stef.DatabaseQuery.Business.Interfaces;
using Stef.DatabaseQuery.Business.Managers;
using Stef.DatabaseQuery.Business.Managers.Databases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace Stef.DatabaseQuery.Controllers
{
    public class DatabaseController : ApiController
    {
        [HttpPost]
        public DatabaseRepository Database(DatabaseRepository databaseRepository)
        {
            return DatabaseManager
                .Instance
                .UpdateDatabase(databaseRepository);
        }
        [HttpPost]
        public object DeleteDatabase(JObject data)
        {
            var id = (int)data["id"];

            DatabaseManager
                .Instance
                .RemoveDatabase(id);

            return new
            {
                ok = true
            };
        }
        [HttpGet]
        public IEnumerable<DatabaseRepository> Databases()
        {
            return DatabaseManager
                .Instance
                .GetDatabases()
                .OrderBy(c => c.Caption);
        }

        [HttpGet]
        public IEnumerable<Table> Tables(int databaseId)
        {
            return DatabaseManager
                .Instance
                .GetDatabaseInfo(databaseId)
                .SchemaInfo
                .GetTables()
                .OrderBy(c => c.TableName);
        }

        [HttpGet]
        public object Providers()
        {
            return CompositionManager
                .Instance
                .GetInstances<IDatabaseProvider>()
                .Select(c => new
                {
                    name = c.Name,
                    fullName = c.GetType().FullName
                });
        }
    }
}
