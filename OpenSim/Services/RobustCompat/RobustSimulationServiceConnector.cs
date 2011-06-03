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

using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Framework.Capabilities;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using Aurora.Simulation.Base;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Connectors.Simulation;

namespace OpenSim.Services.RobustCompat
{
    public class RobustedAgentCircuitData : AgentCircuitData
    {
        /// <summary>
        /// Agent's account first name
        /// </summary>
        public string firstname;

        /// <summary>
        /// Agent's account last name
        /// </summary>
        public string lastname;

        public override AgentCircuitData Copy ()
        {
            RobustedAgentCircuitData copy = (RobustedAgentCircuitData)base.Copy ();
            copy.firstname = firstname;
            copy.lastname = lastname;
            return copy;
        }

        public override OSDMap PackAgentCircuitData ()
        {
            OSDMap args = base.PackAgentCircuitData ();
            #region OPENSIM ONLY

            args["first_name"] = OSD.FromString (firstname);
            args["last_name"] = OSD.FromString (lastname);

            #endregion

            return args;
        }
    }

    public class RobustSimulationServicesConnector : SimulationServiceConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);
        private IRegistryCore m_registry;

        public override bool CreateAgent (GridRegion destination, ref AgentCircuitData aCircuit, uint teleportFlags, AgentData data, out string reason)
        {
            aCircuit = FixAgentCircuitData (aCircuit);
            return base.CreateAgent (destination, ref aCircuit, teleportFlags, data, out reason);
        }

        private AgentCircuitData FixAgentCircuitData (AgentCircuitData aCircuit)
        {
            IUserAccountService UAS = m_registry.RequestModuleInterface<IUserAccountService> ();
            UserAccount account = UAS.GetUserAccount (UUID.Zero, aCircuit.AgentID);
            RobustedAgentCircuitData newCircuit = new RobustedAgentCircuitData ();
            newCircuit.AgentID = aCircuit.AgentID;
            newCircuit.Appearance = aCircuit.Appearance;
            newCircuit.CapsPath = CapsUtil.GetRandomCapsObjectPath ();
            newCircuit.child = aCircuit.child;
            newCircuit.circuitcode = aCircuit.circuitcode;
            newCircuit.firstname = account.FirstName;
            newCircuit.IPAddress = aCircuit.IPAddress;
            newCircuit.lastname = account.LastName;
            newCircuit.OtherInformation = aCircuit.OtherInformation;
            newCircuit.SecureSessionID = aCircuit.SecureSessionID;
            newCircuit.ServiceSessionID = aCircuit.ServiceSessionID;
            newCircuit.SessionID = aCircuit.SessionID;
            newCircuit.startpos = aCircuit.startpos;
            newCircuit.teleportFlags = aCircuit.teleportFlags;
            return newCircuit;
        }

        #region IService Members

        public override void Initialize (IConfigSource config, IRegistryCore registry)
        {
            IConfig handlers = config.Configs["Handlers"];
            m_registry = registry;
            if (handlers.GetString ("SimulationHandler", "") == "RobustSimulationServiceConnector")
            {
                registry.RegisterModuleInterface<ISimulationService> (this);
                m_localBackend = new LocalSimulationServiceConnector ();
                m_registry.RequestModuleInterface<ISimulationBase> ().EventManager.RegisterEventHandler ("ReleaseAgent", ReleaseAgentHandler);
            }
        }

        private object ReleaseAgentHandler (string mod, object param)
        {
            object[] o = (object[])param;
            CloseAgent ((GridRegion)o[1], (UUID)o[0]);

            return null;
        }

        #endregion
    }
}
