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