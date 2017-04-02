using System;
using System.Linq;
using Stef.DatabaseQuery.Business.Managers;
using Stef.DatabaseQuery.Business.Managers.Databases;
using Stef.DatabaseQuery.Business.Managers.Sqls;
using Stef.DatabaseQuery.Business.Providers;
using Stef.DatabaseQuery.Business.Interfaces;

namespace Stef.DatabaseQuery.Console
{
    internal class Program
    {
        private static DatabaseInfo _DatabaseInfo;

        private static void Main(string[] args)
        {
            DatabaseManager.Instance.InitializeDatabases();
            //DatabaseManager.Instance.Add(
            //    new SqlDatabaseProvider(),
            //    "Freihof",
            //    "Server=tipdevsql\\sql2012;Database=DM360_FH;User Id=sa;Password=SYSNIK;");

            while (true)
            {
                System.Console.WriteLine("Funktion:");
                var func = System.Console.ReadLine();

                System.Console.Clear();

                switch (func)
                {
                    case "sql":
                        Sql();
                        break;
                    case "query":
                        Query();
                        break;
                    case "nonquery":
                        NonQuery();
                        break;
                }

                System.Console.WriteLine();
                System.Console.WriteLine();
            }
        }
        private static void Sql()
        {
            System.Console.WriteLine("Tabellenname:");
            var tableName = System.Console.ReadLine();

            System.Console.WriteLine(SqlManager.Instance.CreateSelect(_DatabaseInfo, tableName));
        }
        private static void Query()
        {
            System.Console.WriteLine("Abfrage:");
            var query = System.Console.ReadLine();

            using (var connection = _DatabaseInfo.CreateConnection())
            {
                var sqlSelectToken = new SqlSelectToken(_DatabaseInfo, query);
                var result = SqlManager.Instance.ExecuteQuery(_DatabaseInfo, connection, null, sqlSelectToken, 100);
                System.Console.WriteLine(result["data"].ToString(Newtonsoft.Json.Formatting.Indented));
            }
        }
        private static void NonQuery()
        {
            System.Console.WriteLine("Abfrage:");
            var query = System.Console.ReadLine();

            using (var connection = _DatabaseInfo.CreateConnection())
            {
                var result = SqlManager.Instance.ExecuteNonQuery(_DatabaseInfo, connection, null, query);
                System.Console.WriteLine(result);
            }
        }
    }
}
