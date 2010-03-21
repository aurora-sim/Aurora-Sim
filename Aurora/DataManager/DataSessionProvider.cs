using System;
using System.IO;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;

namespace Aurora.DataManager
{
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

        private string DbFile;

        public DataSessionProvider()
        {
            DbFile =  "firstProject.db";
        }

        public DataSessionProvider(string dbFile)
        {
            DbFile = dbFile;
        }

        private ISessionFactory CreateSessionFactory()
        {
            try
            {
                return Fluently.Configure()
                    .Database(
                        SQLiteConfiguration.Standard
                            .UsingFile(DbFile)
                    )
                    .Mappings(m =>
                              m.FluentMappings.AddFromAssemblyOf<DataSessionProvider>())
                    .ExposeConfiguration(BuildSchema)
                    .BuildSessionFactory();
            }
            catch(Exception e)
            {
                throw new Exception("Could not initialize session:" + System.Environment.NewLine + e.Message + ((e.InnerException!=null)?(System.Environment.NewLine + e.InnerException.Message):string.Empty));
            }
        }

        private void BuildSchema(Configuration config)
        {
            // this NHibernate tool takes a configuration (with mapping info in)
            // and exports a database schema from it
            new SchemaExport(config)
              .Create(false, true);
        }

        public void DeleteLocalResources()
        {
            if (File.Exists(DbFile))
            {
                File.Delete(DbFile);
            }
        }
    }
}