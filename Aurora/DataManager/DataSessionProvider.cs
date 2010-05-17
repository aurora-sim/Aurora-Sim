using System;
using System.IO;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using Aurora.Framework;

namespace Aurora.DataManager
{
    public static class DataSessionProviderConnector
    {
        public static DataSessionProvider DataSessionProvider;
        public static DataSessionProvider StateSaveDataSessionProvider;
    }

    public class DataSessionProvider
    {
        private ISessionFactory factory;

        public ISessionFactory GetDataSession()
        {
            if( factory == null)
            {
                factory = CreateSessionFactory();
            }
            return factory;
        }

        private IPersistenceConfigurer persistanceConfigurer;

        public DataSessionProvider(DataManagerTechnology technology, string connectionInfo)
        {
            if (technology == DataManagerTechnology.SQLite)
            {
                persistanceConfigurer = SQLiteConfiguration.Standard.ConnectionString(connectionInfo);
            }
            else if (technology == DataManagerTechnology.MySql)
            {
                persistanceConfigurer = MySQLConfiguration.Standard.ConnectionString(connectionInfo);
            }
        }

        private ISessionFactory CreateSessionFactory()
        {
            try
            {
                return Fluently.Configure()
                    .Database(
                        persistanceConfigurer
                    )
                    .Mappings(m =>
                              m.FluentMappings.AddFromAssemblyOf<DataSessionProvider>())
                    .ExposeConfiguration(BuildSchema)
                    .BuildSessionFactory();
            }
            catch (Exception e)
            {
                throw new Exception("Could not initialize session:" + System.Environment.NewLine + e.Message + ((e.InnerException != null) ? (System.Environment.NewLine + e.InnerException.Message) : string.Empty));
            }
        }

        private void BuildSchema(Configuration config)
        {
            // this NHibernate tool takes a configuration (with mapping info in)
            // and exports a database schema from it
            new SchemaExport(config)
              .Create(false, true);
        }
    }
}