using System;
using System.Data;
using System.Linq;

namespace Stef.DatabaseQuery.Business.Managers.ChangeData
{
    public class ChangeDataConnection
    {
        public ChangeDataConnection(IDbConnection connection)
        {
            Connection = connection;
            Transaction = connection.BeginTransaction();
        }

        public IDbConnection Connection { get; private set; }
        public IDbTransaction Transaction { get; private set; }
        public int Changes { get; set; }
    }
}
