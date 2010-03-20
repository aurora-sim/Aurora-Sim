using System;

namespace Aurora.DataManager.Migration
{
    public class Migrator : IRestorePoint
    {
        public Version Version { get; protected set; }

        public bool CanProvideDefaults { get; protected set; }

        #region IRestorePoint Members

        public virtual void DoRestore(DataSessionProvider sessionProvider, IGenericData genericData)
        {
        }

        #endregion

        public bool Validate(DataSessionProvider sessionProvider, IGenericData genericData)
        {
            if (genericData.GetAuroraVersion() != Version)
            {
                return false;
            }
            return DoValidate(sessionProvider, genericData);
        }

        protected virtual bool DoValidate(DataSessionProvider sessionProvider, IGenericData genericData)
        {
            return false;
        }

        public virtual IRestorePoint PrepareRestorePoint()
        {
            return this;
        }

        public void Migrate(DataSessionProvider sessionProvider, IGenericData genericData)
        {
            DoMigrate(sessionProvider, genericData);
            genericData.WriteAuroraVersion(Version);
        }

        protected virtual void DoMigrate(DataSessionProvider sessionProvider, IGenericData genericData)
        {
        }

        public void CreateDefaults(DataSessionProvider sessionProvider, IGenericData genericData)
        {
            DoCreateDefaults(sessionProvider, genericData);
            genericData.WriteAuroraVersion(Version);
        }

        protected virtual void DoCreateDefaults(DataSessionProvider sessionProvider, IGenericData genericData)
        {
        }
    }
}