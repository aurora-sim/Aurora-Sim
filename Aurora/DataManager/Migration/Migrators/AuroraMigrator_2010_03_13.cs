using System;

namespace Aurora.DataManager.Migration.Migrators
{
    public class AuroraMigrator_2010_03_13 : Migrator
    {
        public AuroraMigrator_2010_03_13()
        {
            Version = new Version(2010, 3, 13);
            CanProvideDefaults = true;
        }

        protected override void DoCreateDefaults(DataSessionProvider sessionProvider, IGenericData genericData)
        {
        }

        public override void DoRestore(DataSessionProvider sessionProvider, IGenericData genericData)
        {
        }

        protected override bool DoValidate(DataSessionProvider sessionProvider, IGenericData genericData)
        {
            return true;
        }

        protected override void DoMigrate(DataSessionProvider sessionProvider, IGenericData genericData)
        {
            
        }
    }
}