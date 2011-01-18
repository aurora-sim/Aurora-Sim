using System;
using System.Collections.Generic;
using System.Linq;
using Aurora.DataManager.Migration.Migrators;
using Aurora.Framework;

namespace Aurora.DataManager.Migration
{
    public class MigrationManager
    {
        private readonly IDataConnector genericData;
        private readonly List<Migrator> migrators = new List<Migrator>();
        private readonly DataSessionProvider sessionProvider;
        private bool executed;
        private MigrationOperationDescription operationDescription;
        private IRestorePoint restorePoint;
        private bool rollback;

        public MigrationManager(DataSessionProvider sessionProvider, IDataConnector genericData)
        {
            this.sessionProvider = sessionProvider;
            this.genericData = genericData;
            migrators.Add(new AuroraMigrator_2010_03_13());
            migrators.Add(new AuroraMigrator_2010_11_4());
            migrators.Add(new AuroraMigrator_2010_12_30());
            migrators.Add(new AuroraMigrator_2011_1_15());
            migrators.Add(new AuroraMigrator_2011_1_16());
        }

        public MigrationManager(DataSessionProvider sessionProvider, IDataConnector genericData, List<Migrator> migrators)
        {
            this.sessionProvider = sessionProvider;
            this.genericData = genericData;
            this.migrators = migrators;
        }

        public Version LatestVersion
        {
            get { return GetLatestVersionMigrator().Version; }
        }

        public MigrationOperationDescription GetDescriptionOfCurrentOperation()
        {
            return operationDescription;
        }

        public void DetermineOperation()
        {
            executed = false;
            Version currentVersion = genericData.GetAuroraVersion();

            //if there is no aurora version, this is likely an entirely new installation
            if (currentVersion == null)
            {
                Migrator defaultMigrator = GetHighestVersionMigratorThatCanProvideDefaultSetup();
                currentVersion = defaultMigrator.Version;
                Migrator startMigrator = GetMigratorAfterVersion(defaultMigrator.Version);
                var latestMigrator = GetLatestVersionMigrator();
                Migrator targetMigrator = defaultMigrator == latestMigrator ? null : latestMigrator;
                operationDescription = new MigrationOperationDescription(MigrationOperationTypes.CreateDefaultAndUpgradeToTarget, currentVersion, startMigrator!=null?startMigrator.Version:null, targetMigrator!=null?targetMigrator.Version:null);
            }
            else
            {
                Migrator startMigrator = GetMigratorAfterVersion(currentVersion);
                if (startMigrator != null)
                {
                    Migrator targetMigrator = GetLatestVersionMigrator();
                    operationDescription = new MigrationOperationDescription(MigrationOperationTypes.UpgradeToTarget, currentVersion, startMigrator.Version, targetMigrator.Version);
                }
                else
                {
                    operationDescription = new MigrationOperationDescription(MigrationOperationTypes.DoNothing, currentVersion);
                }
            }
        }

        private Migrator GetMigratorAfterVersion(Version version)
        {
            if(version==null)
            {
                return null;
            }

            foreach (Migrator migrator in (from m in migrators orderby m.Version ascending select m))
            {
                if (migrator.Version > version)
                {
                    return migrator;
                }
            }
            return null;
        }

        private Migrator GetLatestVersionMigrator()
        {
            return (from m in migrators orderby m.Version descending select m).First();
        }

        private Migrator GetHighestVersionMigratorThatCanProvideDefaultSetup()
        {
            return (from m in migrators where m.CanProvideDefaults orderby m.Version descending select m).First();
        }

        public void ExecuteOperation()
        {
            if (operationDescription != null && executed == false && operationDescription.OperationType != MigrationOperationTypes.DoNothing)
            {
                Migrator currentMigrator = GetMigratorByVersion(operationDescription.CurrentVersion);

                //if we are creating default, do it now
                if (operationDescription.OperationType == MigrationOperationTypes.CreateDefaultAndUpgradeToTarget)
                {
                    try
                    {
                        currentMigrator.CreateDefaults(sessionProvider, genericData);
                    }
                    catch
                    {
                    }
                    executed = true;
                }

                //lets first validate where we think we are
                bool validated = currentMigrator.Validate(sessionProvider, genericData);

                if (!validated)
                {
                    throw new MigrationOperationException(string.Format("Current version {0} did not validate. Stopping here so we don't cause any trouble. No changes were made.", currentMigrator.Version));
                }

                bool restoreTaken = false;
                //Loop through versions from start to end, migrating then validating
                Migrator executingMigrator = GetMigratorByVersion(operationDescription.StartVersion);

                //only restore if we are going to do something
                if (executingMigrator != null)
                {
                    //prepare restore point if something goes wrong
                    restorePoint = currentMigrator.PrepareRestorePoint(sessionProvider, genericData);
                    restoreTaken = true;
                }


                while (executingMigrator != null)
                {
                    try
                    {
                        executingMigrator.Migrate(sessionProvider, genericData);
                    }
                    catch (Exception)
                    {
                        
                    }
                    executed = true;
                    validated = executingMigrator.Validate(sessionProvider, genericData);

                    //if it doesn't validate, rollback
                    if (!validated)
                    {
                        RollBackOperation();
                        throw new MigrationOperationException(string.Format("Migrating to version {0} did not validate. Restoring to restore point.", currentMigrator.Version));
                    }

                    if( executingMigrator.Version == operationDescription.EndVersion )
                    {
                        break;
                    }

                    executingMigrator = GetMigratorAfterVersion(executingMigrator.Version);
                }

                if (restoreTaken )
                {
                    currentMigrator.ClearRestorePoint(genericData);    
                }
            }
        }

        public void RollBackOperation()
        {
            if (operationDescription != null && executed == true && rollback == false && restorePoint != null)
            {
                restorePoint.DoRestore(sessionProvider, genericData);
                rollback = true;
            }
        }

        public bool ValidateVersion(Version version)
        {
            return GetMigratorByVersion(version).Validate(sessionProvider, genericData);
        }

        private Migrator GetMigratorByVersion(Version version)
        {
            if (version == null)
            {
                return null;
            }
            return (from m in migrators where m.Version == version select m).First();
        }
    }
}