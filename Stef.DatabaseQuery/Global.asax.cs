using System;
using System.Linq;
using System.Web.Http;
using Stef.DatabaseQuery.Business.Managers;
using Stef.DatabaseQuery.Business.Providers;
using Stef.DatabaseQuery.Business.Interfaces;

namespace Stef.DatabaseQuery
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            //DatabaseManager.Instance.Add(
            //    "Server=.\sqlexpress;Database=AdventureWorks2014;User Id=stefan;Password=stefan;",
            //    CompositionManager.Instance.GetInstance<IDatabaseProvider, SqlDatabaseProvider>(),
            //    "Stefan");

            DatabaseManager.Instance.InitializeDatabases();

            //DatabaseManager.Instance.Add(
            //    new SqlDatabaseProvider(),
            //    "Freihof",
            //    "Server=tipdevsql\\sql2012;Database=DM360_FH;User Id=sa;Password=SYSNIK;");

            TransactionManager.Instance.Start();
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            if (Request.Headers.AllKeys.Contains("Origin") && Request.HttpMethod == "OPTIONS")
            {
                Response.StatusCode = 200;
                Response.End();
            }
        }
    }
}
