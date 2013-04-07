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

using Aurora.Framework;
using Aurora.Framework.DatabaseInterfaces;
using Aurora.Framework.Modules;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using System.Collections.Generic;

namespace Aurora.Services.DataService
{
    public class LocalEmailMessagesConnector : ConnectorBase, IEmailConnector
    {
        private IGenericData GD;

        #region IEmailConnector Members

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            GD = GenericData;

            if (source.Configs[Name] != null)
                defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

            if (GD != null)
                GD.ConnectToDatabase(defaultConnectionString, "Generics",
                                     source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

            Framework.Utilities.DataManager.RegisterPlugin(Name + "Local", this);

            if (source.Configs["AuroraConnectors"].GetString("EmailConnector", "LocalConnector") ==
                "LocalConnector")
            {
                Framework.Utilities.DataManager.RegisterPlugin(this);
            }
            Init(simBase, Name);
        }

        public string Name
        {
            get { return "IEmailConnector"; }
        }

        /// <summary>
        ///     Gets all offline messages for the user in GridInstantMessage format.
        /// </summary>
        /// <param name="objectID"></param>
        /// <returns></returns>
        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public List<Email> GetEmails(UUID objectID)
        {
            object remoteValue = DoRemote(objectID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<Email>) remoteValue;

            //Get all the messages
            List<Email> emails = GenericUtils.GetGenerics<Email>(objectID, "Emails", GD);
            GenericUtils.RemoveGenericByType(objectID, "Emails", GD);
            return emails;
        }

        /// <summary>
        ///     Adds a new offline message for the user.
        /// </summary>
        /// <param name="email"></param>
        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public void InsertEmail(Email email)
        {
            object remoteValue = DoRemote(email);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            GenericUtils.AddGeneric(email.toPrimID, "Emails", UUID.Random().ToString(),
                                    email.ToOSD(), GD);
        }

        #endregion
    }
}