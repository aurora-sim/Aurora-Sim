/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.ModuleLoader;
using Aurora.Framework.Utilities;
using Aurora.Framework.Services;

namespace Aurora.DataManager.Migration
{
    public class MigrationManager
    {
        private readonly IDataConnector genericData;
        private readonly List<Migrator> migrators = new List<Migrator>();
        private readonly bool doBackup = true;
        private const string VERSION_TABLE_NAME = "aurora_migrator_version";
        private const string COLUMN_NAME = "name";
        private const string COLUMN_VERSION = "version";

        public MigrationManager(IDataConnector genericData, bool doBackup)
        {
            this.genericData = genericData;
            this.doBackup = doBackup;
            migrators = AuroraModuleLoader.PickupModules<IMigrator>().Cast<Migrator>().ToList();
        }

        public void ExecuteMigration()
        {
            CheckVersionTableExists();
            if (!CheckAndLockDatabase())
            {
                //We need to wait for the database to finish migrating, then we can just move on with startup, given that some other instance did the migrations for us
                WaitForDatabaseLock();
                return;
            }

            //Get the newest migrators
            Dictionary<string, Migrator> migratorsToRun = new Dictionary<string, Migrator>();
            foreach (Migrator m in migrators)
            {
                if (m.MigrationName == null)
                    continue;
                if (!migratorsToRun.ContainsKey(m.MigrationName))
                    migratorsToRun.Add(m.MigrationName, m);
                else if (m.Version > migratorsToRun[m.MigrationName].Version)
                    migratorsToRun[m.MigrationName] = m;
            }

            foreach (Migrator m in migratorsToRun.Values)
            {
                Migration migration = new Migration(genericData, m, doBackup);
                migration.DetermineOperation();
                migration.ExecuteOperation();
            }
            ReleaseDatabaseLock();
        }

        private void CheckVersionTableExists()
        {
            if (!genericData.TableExists(VERSION_TABLE_NAME))
            {
                genericData.CreateTable(VERSION_TABLE_NAME, new[]
                                                    {
                                                        new ColumnDefinition
                                                            {
                                                                Name = COLUMN_VERSION,
                                                                Type =
                                                                    new ColumnTypeDef
                                                                        {
                                                                            Type = ColumnType.String,
                                                                            Size = 100
                                                                        }
                                                            }
                                                    }, new IndexDefinition[0]);
            }
        }

        private bool CheckAndLockDatabase()
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters.Add(COLUMN_NAME, "locked");
            List<string> exists = genericData.Query(new string[1] { COLUMN_NAME }, VERSION_TABLE_NAME, filter, null, null, null);
            if (exists.Count == 0)
            {
                genericData.Insert(VERSION_TABLE_NAME, new object[2] { 1, "locked" });
                return true;
            }
            //Set locked = 1 again
            //If locked == 1, then we will not be migrating anything, but we also must stop this process until the migration is complete
            //  so that we don't end up doing this while migration is going on
            Dictionary<string, object> updateVals = new Dictionary<string,object>();
            updateVals[COLUMN_VERSION] = 1;
            return genericData.Update(VERSION_TABLE_NAME, updateVals, null, filter, null, null);
        }

        private void WaitForDatabaseLock()
        {
            throw new NotImplementedException("Database lock wait - not implemented");
        }

        private void ReleaseDatabaseLock()
        {
            //Set locked = 0 again
            Dictionary<string, object> updateVals = new Dictionary<string, object>();
            updateVals[COLUMN_VERSION] = 0;
            QueryFilter filter = new QueryFilter();
            filter.andFilters.Add(COLUMN_NAME, "locked");
            genericData.Update(VERSION_TABLE_NAME, updateVals, null, filter, null, null);
        }
    }

    public class Migration
    {
        private readonly IDataConnector genericData;
        private readonly Migrator migrator = null;
        private readonly bool doBackup;
        private bool executed;
        private MigrationOperationTypes operationDescription = MigrationOperationTypes.DoNothing;
        private IRestorePoint restorePoint;
        private bool rollback;

        public enum MigrationOperationTypes
        {
            CreateDefaultAndUpgradeToTarget,
            UpgradeToTarget,
            DoNothing
        }

        public Migration(IDataConnector genericData, Migrator migrator, bool doBackup)
        {
            this.genericData = genericData;
            this.doBackup = doBackup;
            this.migrator = migrator;
        }

        public void DetermineOperation()
        {
            executed = false;
            Version currentVersion = genericData.GetAuroraVersion(migrator.MigrationName);

            //if there is no aurora version, this is likely an entirely new installation
            if (currentVersion == null)
                operationDescription = MigrationOperationTypes.CreateDefaultAndUpgradeToTarget;
            else if(migrator.Version != currentVersion)
                operationDescription = MigrationOperationTypes.UpgradeToTarget;
        }

        public void ExecuteOperation()
        {
            //if we are creating default, do it now
            if (operationDescription == MigrationOperationTypes.CreateDefaultAndUpgradeToTarget)
            {
                try
                {
                    migrator.CreateDefaults(genericData);
                }
                catch
                {
                }
                executed = true;
            }
            else if (operationDescription == MigrationOperationTypes.UpgradeToTarget)
            {
                bool success = true;
                //prepare restore point if something goes wrong
                if (doBackup)
                    restorePoint = migrator.PrepareRestorePoint(genericData);

                try
                {
                    migrator.Migrate(genericData);
                }
                catch (Exception ex)
                {
                    if (migrator != null)
                    {
                        migrator.DoRestore(genericData);
                        migrator.ClearRestorePoint(genericData);
                        throw new MigrationOperationException(string.Format("Migrating {0} to version {1} failed, aborting and rolling back all changes. {2}.",
                                                                            migrator.MigrationName, migrator.Version, ex));
                    }
                }
                success = migrator.Validate(genericData);
                if (!success)
                {
                    if (doBackup)
                    {
                        migrator.DoRestore(genericData);
                        migrator.ClearRestorePoint(genericData);
                    }
                    throw new MigrationOperationException(string.Format("Validating {0} version {1} failed, rolling back changes.",
                                                                        migrator.MigrationName, migrator.Version));
                }
                migrator.FinishedMigration(genericData);

                //clear restore point now that we've successfully finished
                if (success && doBackup)
                    migrator.ClearRestorePoint(genericData);
            }
        }
    }
}