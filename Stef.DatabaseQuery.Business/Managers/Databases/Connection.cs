using System;
using System.Data;
using System.Linq;

namespace Stef.DatabaseQuery.Business.Managers.Databases
{
    public class Connection
    {
        public Connection(IDbConnection dbConnection, DatabaseInfo databaseInfo)
        {
            DbConnection = dbConnection;
            DatabaseInfo = databaseInfo;
        }

        public IDbConnection DbConnection { get; private set; }
        public DatabaseInfo DatabaseInfo { get; private set; }
    }
}
