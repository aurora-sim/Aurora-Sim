using NHibernate;

namespace Aurora.DataManager.Repositories
{
    public class DataManagerRepository
    {
        protected DataSessionProvider sessionProvider;

        public DataManagerRepository(DataSessionProvider sessionProvider)
        {
            this.sessionProvider = sessionProvider;
        }

        protected ISession OpenSession()
        {
            return sessionProvider.GetDataSession().OpenSession();
        }
    }
}