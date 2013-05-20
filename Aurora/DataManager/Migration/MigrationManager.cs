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
using System.Threading;

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

            List<Migration> migrationsToComplete = new List<Migration>();
            foreach (Migrator m in migratorsToRun.Values)
            {
                Migration migration = new Migration(genericData, m, doBackup);
                migration.DetermineOperation();
                if(migration.OperationDescription != Migration.MigrationOperationTypes.DoNothing)
                    migrationsToComplete.Add(migration);
            }

            if (migrationsToComplete.Count > 0)
            {
                MainConsole.Instance.Info("[DatabaseMigrator]: Database migrations are needed, now beginning the process");

                foreach(Migration migration in migrationsToComplete)
                {
                    migration.ExecuteOperation();
                }

                MainConsole.Instance.Info("[DatabaseMigrator]: Database migrations have been completed");
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
                                                                Type = ColumnTypeDef.Text
                                                            },
                                                        new ColumnDefinition
                                                            {
                                                                Name = COLUMN_NAME,
                                                                Type = ColumnTypeDef.Text
                                                            }
                                                    }, new IndexDefinition[0]);
            }
        }

        private bool CheckAndLockDatabase()
        {
            if (!CheckIfDatabaseIsLocked())
            {
                genericData.Insert(VERSION_TABLE_NAME, new object[2] { 1, "locked" });
                return true;
            }
            return false;
        }

        private bool CheckIfDatabaseIsLocked()
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters.Add(COLUMN_NAME, "locked");
            List<string> exists = genericData.Query(new string[1] { COLUMN_NAME }, VERSION_TABLE_NAME, filter, null, null, null);
            return exists.Count != 0;
        }

        private void WaitForDatabaseLock()
        {
            while (CheckIfDatabaseIsLocked())
            {
                Thread.Sleep(100);
            }
        }

        private void ReleaseDatabaseLock()
        {
            //Set locked = 0 again
            QueryFilter filter = new QueryFilter();
            filter.andFilters.Add(COLUMN_NAME, "locked");
            genericData.Delete(VERSION_TABLE_NAME, filter);
        }
    }

    public class Migration
    {
        private readonly IDataConnector _genericData;
        private readonly Migrator _migrator = null;
        private readonly bool _doBackup;
        public MigrationOperationTypes OperationDescription = MigrationOperationTypes.DoNothing;
        private IRestorePoint _restorePoint;

        public enum MigrationOperationTypes
        {
            CreateDefaultAndUpgradeToTarget,
            UpgradeToTarget,
            DoNothing
        }

        public Migration(IDataConnector genericData, Migrator migrator, bool doBackup)
        {
            this._genericData = genericData;
            this._doBackup = doBackup;
            this._migrator = migrator;
        }

        public void DetermineOperation()
        {
            Version currentVersion = _genericData.GetAuroraVersion(_migrator.MigrationName);

            //if there is no aurora version, this is likely an entirely new installation
            if (currentVersion == null)
                OperationDescription = MigrationOperationTypes.CreateDefaultAndUpgradeToTarget;
            else if(_migrator.Version != currentVersion)
                OperationDescription = MigrationOperationTypes.UpgradeToTarget;
        }

        public void ExecuteOperation()
        {
            //if we are creating default, do it now
            if (OperationDescription == MigrationOperationTypes.CreateDefaultAndUpgradeToTarget)
            {
                MainConsole.Instance.InfoFormat("[DatabaseMigrator]: Now creating default tables for {0}",
                     _migrator.MigrationName);
                try
                {
                    _migrator.CreateDefaults(_genericData);
                }
                catch
                {
                }
            }
            else if (OperationDescription == MigrationOperationTypes.UpgradeToTarget)
            {
                MainConsole.Instance.InfoFormat("[DatabaseMigrator]: Now applying migration {0} for {1}",
                    _migrator.Version, _migrator.MigrationName);
                bool success = true;
                //prepare restore point if something goes wrong
                if (_doBackup)
                    _restorePoint = _migrator.PrepareRestorePoint(_genericData);

                try
                {
                    _migrator.Migrate(_genericData);
                }
                catch (Exception ex)
                {
                    if (_migrator != null)
                    {
                        _migrator.DoRestore(_genericData);
                        _migrator.ClearRestorePoint(_genericData);
                        throw new MigrationOperationException(string.Format("Migrating {0} to version {1} failed, aborting and rolling back all changes. {2}.",
                                                                            _migrator.MigrationName, _migrator.Version, ex));
                    }
                }
                success = _migrator.Validate(_genericData);
                if (!success)
                {
                    if (_doBackup)
                    {
                        _migrator.DoRestore(_genericData);
                        _migrator.ClearRestorePoint(_genericData);
                    }
                    throw new MigrationOperationException(string.Format("Validating {0} version {1} failed, rolling back changes.",
                                                                        _migrator.MigrationName, _migrator.Version));
                }
                _migrator.FinishedMigration(_genericData);

                //clear restore point now that we've successfully finished
                if (success && _doBackup)
                    _migrator.ClearRestorePoint(_genericData);
            }
        }
    }
}