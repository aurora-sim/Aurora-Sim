using System;

namespace Aurora.DataManager.Migration
{
    public enum MigrationOperationTypes
    {
        CreateDefaultAndUpgradeToTarget,
        UpgradeToTarget,
        DoNothing
    }

    public class MigrationOperationDescription
    {
        public MigrationOperationDescription(MigrationOperationTypes createDefaultAndUpgradeToTarget, Version currentVersion, Version startVersion, Version endVersion)
        {
            OperationType = createDefaultAndUpgradeToTarget;
            CurrentVersion = currentVersion;
            StartVersion = startVersion;
            EndVersion = endVersion;
        }

        public MigrationOperationDescription(MigrationOperationTypes createDefaultAndUpgradeToTarget, Version currentVersion)
        {
            OperationType = createDefaultAndUpgradeToTarget;
            CurrentVersion = currentVersion;
            StartVersion = null;
            EndVersion = null;
        }

        public Version CurrentVersion { get; private set; }

        public Version EndVersion { get; private set; }

        public MigrationOperationTypes OperationType { get; private set; }

        public Version StartVersion { get; private set; }
    }
}