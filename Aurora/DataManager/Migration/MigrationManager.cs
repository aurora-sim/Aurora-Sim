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
        private bool executed;
        private MigrationOperationDescription operationDescription;
        private IRestorePoint restorePoint;
        private bool rollback;
        private string migratorName;
        private bool validateTables;

        public MigrationManager(IDataConnector genericData, string migratorName, bool validateTables)
        {
            this.genericData = genericData;
            this.migratorName = migratorName;
            this.validateTables = validateTables;
            List<IMigrator> allMigrators = Aurora.Framework.AuroraModuleLoader.PickupModules<IMigrator>();
            foreach (IMigrator m in allMigrators)
            {
                if (m.MigrationName == null)
                    continue;
                if (m.MigrationName == migratorName)
                {
                    migrators.Add((Migrator)m);
                }
            }
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
            Version currentVersion = genericData.GetAuroraVersion(migratorName);

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
            return (from m in migrators orderby m.Version descending select m).First();
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
                        currentMigrator.CreateDefaults(genericData);
                    }
                    catch
                    {
                    }
                    executed = true;
                }

                if (validateTables)
                {
                    //lets first validate where we think we are
                    bool validated = currentMigrator.Validate(genericData);

                    if (!validated)
                    {
                        throw new MigrationOperationException(string.Format("Current version {0} did not validate. Stopping here so we don't cause any trouble. No changes were made.", currentMigrator.Version));
                    }
                }

                bool restoreTaken = false;
                //Loop through versions from start to end, migrating then validating
                Migrator executingMigrator = GetMigratorByVersion(operationDescription.StartVersion);

                //only restore if we are going to do something
                if (executingMigrator != null)
                {
                    if (validateTables)
                    {
                        //prepare restore point if something goes wrong
                        restorePoint = currentMigrator.PrepareRestorePoint(genericData);
                        restoreTaken = true;
                    }
                }


                while (executingMigrator != null)
                {
                    try
                    {
                        executingMigrator.Migrate(genericData);
                    }
                    catch (Exception)
                    {
                        
                    }
                    executed = true;
                    if (validateTables)
                    {
                        bool validated = executingMigrator.Validate(genericData);

                        //if it doesn't validate, rollback
                        if (!validated)
                        {
                            RollBackOperation();
                            throw new MigrationOperationException(string.Format("Migrating to version {0} did not validate. Restoring to restore point.", currentMigrator.Version));
                        }
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
                restorePoint.DoRestore(genericData);
                rollback = true;
            }
        }

        public bool ValidateVersion(Version version)
        {
            return GetMigratorByVersion(version).Validate(genericData);
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