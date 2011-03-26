using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Net;
using OpenSim.Framework.Client;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Nini.Config;
using log4net;
using Aurora.Framework;
using Aurora.DataManager;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules
{
    public class EstateSettingsModule : ISharedRegionStartupModule
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private List<Scene> m_scenes = new List<Scene>();
        private IRegionConnector RegionConnector;
        private Dictionary<UUID, int> TimeSinceLastTeleport = new Dictionary<UUID, int>();
        private float SecondsBeforeNextTeleport = 3;
        private bool m_enabledBlockTeleportSeconds = false;
        private bool m_enabled = false;
        private string[] BanCriteria = new string[0];
        private bool LoginsDisabled = true;
        private bool StartDisabled = false;

        private Dictionary<UUID, int> LastTelehub = new Dictionary<UUID, int>();

        #endregion

        #region ISharedRegionModule
        public string Name { get { return "EstateSettingsModule"; } }

        #endregion

        #region Console Commands

        protected void ProcessLoginCommands(string module, string[] cmd)
        {
            if (cmd.Length < 2)
            {
                m_log.Info ("Syntax: login enable|disable|status");
                return;
            }

            switch (cmd[1])
            {
                case "enable":
                    if (LoginsDisabled)
                        m_log.Warn("Enabling Logins");
                    LoginsDisabled = false;
                    break;
                case "disable":
                    if (!LoginsDisabled)
                        m_log.Warn("Disabling Logins");
                    LoginsDisabled = true;
                    break;
                case "status":
                    m_log.Warn("Logins are " + (LoginsDisabled ? "dis" : "en") + "abled.");
                    break;
                default:
                    m_log.Info ("Syntax: login enable|disable|status");
                    break;
            }
        }

        protected void BanUser(string module, string[] cmdparams)
        {
            if (cmdparams.Length < 4)
            {
                m_log.Warn("Not enough parameters!");
                return;
            }

            IScenePresence SP = ((Scene)MainConsole.Instance.ConsoleScene).SceneGraph.GetScenePresence(cmdparams[2], cmdparams[3]);
            if(SP == null)
            {
                m_log.Warn("Could not find user");
                return;
            }
            EstateSettings ES = ((Scene)MainConsole.Instance.ConsoleScene).RegionInfo.EstateSettings;
            ES.AddBan(new EstateBan()
            {
                BannedHostAddress = "",
                BannedHostIPMask = "",
                BannedHostNameMask = "",
                BannedUserID = SP.UUID,
                EstateID = ES.EstateID
            });
            ES.Save();
            string alert = null;
            if (cmdparams.Length > 4)
                alert = String.Format("\n{0}\n", String.Join(" ", cmdparams, 4, cmdparams.Length - 4));

            if (alert != null)
                SP.ControllingClient.Kick(alert);
            else
                SP.ControllingClient.Kick("\nThe Aurora manager banned and kicked you out.\n");
            
            // kick client...
            IEntityTransferModule transferModule = SP.Scene.RequestModuleInterface<IEntityTransferModule> ();
            if (transferModule != null)
                transferModule.IncomingCloseAgent (SP.Scene, SP.UUID);
        }

        protected void SetRegionInfoOption(string module, string[] cmdparams)
        {
            IScene scene = MainConsole.Instance.ConsoleScene;
            if (scene == null)
                scene = m_scenes[0];
            #region 3 Params needed
            if (cmdparams.Length < 4)
            {
                m_log.Warn("Not enough parameters!");
                return;
            }
            if (cmdparams[2] == "Maturity")
            {
                if (cmdparams[3] == "PG")
                {
                    scene.RegionInfo.AccessLevel = Util.ConvertMaturityToAccessLevel(0);
                }
                else if (cmdparams[3] == "Mature")
                {
                    scene.RegionInfo.AccessLevel = Util.ConvertMaturityToAccessLevel(1);
                }
                else if (cmdparams[3] == "Adult")
                {
                    scene.RegionInfo.AccessLevel = Util.ConvertMaturityToAccessLevel(2);
                }
                else
                {
                    m_log.Warn("Your parameter did not match any existing parameters. Try PG, Mature, or Adult");
                    return;
                }
                scene.RegionInfo.RegionSettings.Save();
                //Tell the grid about the changes
                IGridRegisterModule gridRegModule = scene.RequestModuleInterface<IGridRegisterModule>();
                if (gridRegModule != null)
                    gridRegModule.UpdateGridRegion(scene);
            }
            #endregion
            #region 4 Params needed
            if (cmdparams.Length < 4)
            {
                m_log.Warn("Not enough parameters!");
                return;
            }
            if (cmdparams[2] == "AddEstateBan".ToLower())
            {
                EstateBan EB = new EstateBan();
                EB.BannedUserID = m_scenes[0].UserAccountService.GetUserAccount(UUID.Zero, cmdparams[3], cmdparams[4]).PrincipalID;
                scene.RegionInfo.EstateSettings.AddBan(EB);
            }
            if (cmdparams[2] == "AddEstateManager".ToLower ())
            {
                scene.RegionInfo.EstateSettings.AddEstateManager(m_scenes[0].UserAccountService.GetUserAccount(UUID.Zero, cmdparams[3], cmdparams[4]).PrincipalID);
            }
            if (cmdparams[2] == "AddEstateAccess".ToLower ())
            {
                scene.RegionInfo.EstateSettings.AddEstateUser(m_scenes[0].UserAccountService.GetUserAccount(UUID.Zero, cmdparams[3], cmdparams[4]).PrincipalID);
            }
            if (cmdparams[2] == "RemoveEstateBan".ToLower ())
            {
                scene.RegionInfo.EstateSettings.RemoveBan(m_scenes[0].UserAccountService.GetUserAccount(UUID.Zero, cmdparams[3], cmdparams[4]).PrincipalID);
            }
            if (cmdparams[2] == "RemoveEstateManager".ToLower ())
            {
                scene.RegionInfo.EstateSettings.RemoveEstateManager(m_scenes[0].UserAccountService.GetUserAccount(UUID.Zero, cmdparams[3], cmdparams[4]).PrincipalID);
            }
            if (cmdparams[2] == "RemoveEstateAccess".ToLower ())
            {
                scene.RegionInfo.EstateSettings.RemoveEstateUser(m_scenes[0].UserAccountService.GetUserAccount(UUID.Zero, cmdparams[3], cmdparams[4]).PrincipalID);
            }
            #endregion
            scene.RegionInfo.RegionSettings.Save();
            scene.RegionInfo.EstateSettings.Save();
        }

        #endregion

        #region Client

        void OnNewClient(IClientAPI client)
        {
            client.OnGodlikeMessage += GodlikeMessage;
            client.OnEstateTelehubRequest += GodlikeMessage; //This is ok, we do estate checks and check to make sure that only telehubs are dealt with here
        }

        private void OnClosingClient(IClientAPI client)
        {
            client.OnGodlikeMessage -= GodlikeMessage;
            client.OnEstateTelehubRequest -= GodlikeMessage;
        }

        #endregion

        #region Telehub Settings

        public void GodlikeMessage(IClientAPI client, UUID requester, string Method, List<string> Parameters)
        {
            if (RegionConnector == null)
                return;
            IScenePresence Sp = ((Scene)client.Scene).GetScenePresence (client.AgentId);
            if (!((Scene)client.Scene).Permissions.CanIssueEstateCommand(client.AgentId, false))
                return;

            string parameter1 = Parameters[0];
            if (Method == "telehub")
            {
                if (parameter1 == "spawnpoint remove")
                {
                    Telehub telehub = RegionConnector.FindTelehub(client.Scene.RegionInfo.RegionID, client.Scene.RegionInfo.RegionHandle);
                    if (telehub == null)
                        return;
                    //Remove the one we sent at X
                    telehub.SpawnPos.RemoveAt(int.Parse(Parameters[1]));
                    RegionConnector.AddTelehub(telehub, client.Scene.RegionInfo.RegionHandle);
                    SendTelehubInfo(client);
                }
                if (parameter1 == "spawnpoint add")
                {
                    ISceneChildEntity part = Sp.Scene.GetSceneObjectPart (uint.Parse (Parameters[1]));
                    if (part == null)
                        return;
                    Telehub telehub = RegionConnector.FindTelehub(client.Scene.RegionInfo.RegionID, client.Scene.RegionInfo.RegionHandle);
                    if (telehub == null)
                        return;
                    telehub.RegionLocX = client.Scene.RegionInfo.RegionLocX;
                    telehub.RegionLocY = client.Scene.RegionInfo.RegionLocY;
                    telehub.RegionID = client.Scene.RegionInfo.RegionID;
                    Vector3 pos = new Vector3(telehub.TelehubLocX, telehub.TelehubLocY, telehub.TelehubLocZ);
                    if (telehub.TelehubLocX == 0 && telehub.TelehubLocY == 0)
                        return; //No spawns without a telehub
                    telehub.SpawnPos.Add(part.AbsolutePosition - pos); //Spawns are offsets
                    RegionConnector.AddTelehub(telehub, client.Scene.RegionInfo.RegionHandle);
                    SendTelehubInfo(client);
                }
                if (parameter1 == "delete")
                {
                    RegionConnector.RemoveTelehub(client.Scene.RegionInfo.RegionID, client.Scene.RegionInfo.RegionHandle);
                    SendTelehubInfo(client);
                }
                if (parameter1 == "connect")
                {
                    ISceneChildEntity part = Sp.Scene.GetSceneObjectPart (uint.Parse (Parameters[1]));
                    if (part == null)
                        return;
                    Telehub telehub = RegionConnector.FindTelehub(client.Scene.RegionInfo.RegionID, client.Scene.RegionInfo.RegionHandle);
                    if (telehub == null)
                        telehub = new Telehub();
                    telehub.RegionLocX = client.Scene.RegionInfo.RegionLocX;
                    telehub.RegionLocY = client.Scene.RegionInfo.RegionLocY;
                    telehub.RegionID = client.Scene.RegionInfo.RegionID;
                    telehub.TelehubLocX = part.AbsolutePosition.X;
                    telehub.TelehubLocY = part.AbsolutePosition.Y;
                    telehub.TelehubLocZ = part.AbsolutePosition.Z;
                    telehub.TelehubRotX = part.ParentEntity.Rotation.X;
                    telehub.TelehubRotY = part.ParentEntity.Rotation.Y;
                    telehub.TelehubRotZ = part.ParentEntity.Rotation.Z;
                    telehub.ObjectUUID = part.UUID;
                    telehub.Name = part.Name;
                    RegionConnector.AddTelehub(telehub, client.Scene.RegionInfo.RegionHandle);
                    SendTelehubInfo(client);
                }

                if (parameter1 == "info ui")
                    SendTelehubInfo(client);
            }
        }

        private void SendTelehubInfo(IClientAPI client)
        {
            if (RegionConnector != null)
            {
                Telehub telehub = RegionConnector.FindTelehub(client.Scene.RegionInfo.RegionID, client.Scene.RegionInfo.RegionHandle);
                if (telehub == null)
                {
                    client.SendTelehubInfo(Vector3.Zero, Quaternion.Identity, new List<Vector3>(), UUID.Zero, "");
                }
                else
                {
                    Vector3 pos = new Vector3(telehub.TelehubLocX, telehub.TelehubLocY, telehub.TelehubLocZ);
                    Quaternion rot = new Quaternion(telehub.TelehubRotX, telehub.TelehubRotY, telehub.TelehubRotZ);
                    client.SendTelehubInfo(pos, rot, telehub.SpawnPos, telehub.ObjectUUID, telehub.Name);
                }
            }
        }

        #endregion

        #region Teleport Permissions

        private bool OnAllowedIncomingTeleport(UUID userID, IScene scene, Vector3 Position, out Vector3 newPosition, out string reason)
        {
            newPosition = Position;
            UserAccount account = scene.UserAccountService.GetUserAccount(scene.RegionInfo.ScopeID, userID);

            IScenePresence Sp = scene.GetScenePresence(userID);
            if (account == null)
            {
                reason = "Failed authentication.";
                return false; //NO!
            }

            //Make sure that this user is inside the region as well
            if (Position.X < -2f || Position.Y < -2f || 
                Position.X > scene.RegionInfo.RegionSizeX+2 || Position.Y > scene.RegionInfo.RegionSizeY+2)
            {
                m_log.DebugFormat(
                    "[EstateService]: AllowedIncomingTeleport was given an illegal position of {0} for avatar {1}, {2}. Clamping",
                    Position, Name, userID);
                bool changedX = false;
                bool changedY = false;
                while (Position.X < 0)
                {
                    Position.X += scene.RegionInfo.RegionSizeX;
                    changedX = true;
                }
                while (Position.X > scene.RegionInfo.RegionSizeX)
                {
                    Position.X -= scene.RegionInfo.RegionSizeX;
                    changedX = true;
                }

                while (Position.Y < 0)
                {
                    Position.Y += scene.RegionInfo.RegionSizeY;
                    changedY = true;
                }
                while (Position.Y > scene.RegionInfo.RegionSizeY)
                {
                    Position.Y -= scene.RegionInfo.RegionSizeY;
                    changedY = true;
                }

                if (changedX)
                    Position.X = scene.RegionInfo.RegionSizeX - Position.X;
                if(changedY)
                    Position.Y = scene.RegionInfo.RegionSizeY - Position.Y;
            }

            //Check that we are not underground as well
            float posZLimit = scene.RequestModuleInterface<ITerrainChannel>()[(int)Position.X, (int)Position.Y] + (float)1.25;

            if (posZLimit >= (Position.Z) && !(Single.IsInfinity(posZLimit) || Single.IsNaN(posZLimit)))
            {
                Position.Z = posZLimit;
            }

            IAgentConnector AgentConnector = DataManager.DataManager.RequestPlugin<IAgentConnector>();
            IAgentInfo agentInfo = null;
            if (AgentConnector != null)
                agentInfo = AgentConnector.GetAgent(userID);

            ILandObject ILO = null;
            IParcelManagementModule parcelManagement = scene.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
                ILO = parcelManagement.GetLandObject(Position.X, Position.Y);

            if (ILO == null)
            {
                //Can't find land, give them the first parcel in the region and find a good position for them
                ILO = parcelManagement.AllParcels()[0];
                Position = parcelManagement.GetParcelCenterAtGround(ILO);
            }

            //parcel permissions
            if (ILO.IsBannedFromLand(userID)) //Note: restricted is dealt with in the next block
            {
                if (Sp == null)
                {
                    reason = "Banned from this parcel.";
                    return true;
                }

                if (!FindUnBannedParcel(Position, Sp, userID, out ILO, out newPosition, out reason))
                {
                    //We found a place for them, but we don't need to check any further
                    return true;
                }
            }
            //Move them out of banned parcels
            ParcelFlags parcelflags = (ParcelFlags)ILO.LandData.Flags;
            if ((parcelflags & ParcelFlags.UseAccessGroup) == ParcelFlags.UseAccessGroup &&
                (parcelflags & ParcelFlags.UseAccessList) == ParcelFlags.UseAccessList &&
                (parcelflags & ParcelFlags.UsePassList) == ParcelFlags.UsePassList)
            {
                //One of these is in play then
                if ((parcelflags & ParcelFlags.UseAccessGroup) == ParcelFlags.UseAccessGroup)
                {
                    if (Sp == null)
                    {
                        reason = "Banned from this parcel.";
                        return true;
                    }
                    if (Sp.ControllingClient.ActiveGroupId != ILO.LandData.GroupID)
                    {
                        if (!FindUnBannedParcel(Position, Sp, userID, out ILO, out newPosition, out reason))
                            //We found a place for them, but we don't need to check any further
                            return true;
                    }
                }
                else if ((parcelflags & ParcelFlags.UseAccessList) == ParcelFlags.UseAccessList)
                {
                    if (Sp == null)
                    {
                        reason = "Banned from this parcel.";
                        return true;
                    }
                    //All but the people on the access list are banned
                    if (ILO.IsRestrictedFromLand(userID))
                        if (!FindUnBannedParcel(Position, Sp, userID, out ILO, out newPosition, out reason))
                            //We found a place for them, but we don't need to check any further
                            return true;
                }
                else if ((parcelflags & ParcelFlags.UsePassList) == ParcelFlags.UsePassList)
                {
                    if (Sp == null)
                    {
                        reason = "Banned from this parcel.";
                        return true;
                    }
                    //All but the people on the pass/access list are banned
                    if (ILO.IsRestrictedFromLand(Sp.UUID))
                        if (!FindUnBannedParcel(Position, Sp, userID, out ILO, out newPosition, out reason))
                            //We found a place for them, but we don't need to check any further
                            return true;
                }
            }

            EstateSettings ES = scene.RegionInfo.EstateSettings;

            //Move them to the nearest landing point
            if (!ES.AllowDirectTeleport)
            {
                if (!scene.Permissions.IsGod(userID))
                {
                    Telehub telehub = RegionConnector.FindTelehub(scene.RegionInfo.RegionID, scene.RegionInfo.RegionHandle);
                    if (telehub != null)
                    {
                        if (telehub.SpawnPos.Count == 0)
                        {
                            Position = new Vector3(telehub.TelehubLocX, telehub.TelehubLocY, telehub.TelehubLocZ);
                        }
                        else
                        {
                            int LastTelehubNum = 0;
                            if (!LastTelehub.TryGetValue(scene.RegionInfo.RegionID, out LastTelehubNum))
                                LastTelehubNum = 0;
                            Position = telehub.SpawnPos[LastTelehubNum] + new Vector3(telehub.TelehubLocX, telehub.TelehubLocY, telehub.TelehubLocZ);
                            LastTelehubNum++;
                            if (LastTelehubNum == telehub.SpawnPos.Count)
                                LastTelehubNum = 0;
                            LastTelehub[scene.RegionInfo.RegionID] = LastTelehubNum;
                        }
                    }
                }
            }
            if (!scene.Permissions.GenericParcelPermission(userID, ILO, (ulong)GroupPowers.None))
            {
                if (ILO.LandData.LandingType == 2) //Blocked, force this person off this land
                {
                    //Find a new parcel for them
                    List<ILandObject> Parcels = parcelManagement.ParcelsNearPoint(Position);
                    if (Parcels.Count == 0)
                    {
                        IScenePresence SP;
                        scene.TryGetScenePresence(userID, out SP);
                        newPosition = parcelManagement.GetNearestRegionEdgePosition(SP);
                    }
                    else
                    {
                        bool found = false;
                        //We need to check here as well for bans, can't toss someone into a parcel they are banned from
                        foreach (ILandObject Parcel in Parcels)
                        {
                            if (!Parcel.IsBannedFromLand(userID))
                            {
                                //Now we have to check their userloc
                                if (ILO.LandData.LandingType == 2)
                                    continue; //Blocked, check next one
                                else if (ILO.LandData.LandingType == 1) //Use their landing spot
                                    newPosition = Parcel.LandData.UserLocation;
                                else //They allow for anywhere, so dump them in the center at the ground
                                    newPosition = parcelManagement.GetParcelCenterAtGround(Parcel);
                                found = true;
                            }
                        }
                        if (!found) //Dump them at the edge
                        {
                            if (Sp != null)
                                newPosition = parcelManagement.GetNearestRegionEdgePosition(Sp);
                            else
                            {
                                reason = "Banned from this parcel.";
                                return true;
                            }
                        }
                    }
                }
                else if (ILO.LandData.LandingType == 1) //Move to tp spot
                    if (ILO.LandData.UserLocation != Vector3.Zero)
                        newPosition = ILO.LandData.UserLocation;
                    else // Dump them at the nearest region corner since they havn't set a landing point
                        newPosition = parcelManagement.GetNearestRegionEdgePosition(Sp);
            }

            //We assume that our own region isn't null....
            if (agentInfo != null)
            {
                //Can only enter prelude regions once!
                int flags = scene.GridService.GetRegionFlags(scene.RegionInfo.ScopeID, scene.RegionInfo.RegionID);
                if (flags != -1 && ((flags & (int)Aurora.Framework.RegionFlags.Prelude) == (int)Aurora.Framework.RegionFlags.Prelude) && agentInfo != null)
                {
                    if (agentInfo.OtherAgentInformation.ContainsKey("Prelude" + scene.RegionInfo.RegionID))
                    {
                        reason = "You may not enter this region as you have already been to this prelude region.";
                        return false;
                    }
                    else
                    {
                        agentInfo.OtherAgentInformation.Add("Prelude" + scene.RegionInfo.RegionID, OSD.FromInteger((int)IAgentFlags.PastPrelude));
                        AgentConnector.UpdateAgent(agentInfo); //This only works for standalones... and thats ok
                    }
                }
            }


            if ((ILO.LandData.Flags & (int)ParcelFlags.DenyAnonymous) != 0)
            {
                if ((account.UserFlags & (int)IUserProfileInfo.ProfileFlags.NoPaymentInfoOnFile) == (int)IUserProfileInfo.ProfileFlags.NoPaymentInfoOnFile)
                {
                    reason = "You may not enter this region.";
                    return false;
                }
            }

            if ((ILO.LandData.Flags & (uint)ParcelFlags.DenyAgeUnverified) != 0 && agentInfo != null)
            {
                if ((agentInfo.Flags & IAgentFlags.Minor) == IAgentFlags.Minor)
                {
                    reason = "You may not enter this region.";
                    return false;
                }
            }

            newPosition = Position;
            reason = "";
            return true;
        }

        private bool OnAllowedIncomingAgent(IScene scene, AgentCircuitData agent, bool isRootAgent, out string reason)
        {
            #region Incoming Agent Checks

            UserAccount account = scene.UserAccountService.GetUserAccount(scene.RegionInfo.ScopeID, agent.AgentID);

            IScenePresence Sp = scene.GetScenePresence(agent.AgentID);
            if (account == null)
            {
                reason = "Failed authentication.";
                return false; //NO!
            }

            if (LoginsDisabled)
            {
                reason = "Logins Disabled";
                return false;
            }

            //Check how long its been since the last TP
            if (m_enabledBlockTeleportSeconds && Sp != null && !Sp.IsChildAgent)
            {
                if (TimeSinceLastTeleport.ContainsKey(Sp.Scene.RegionInfo.RegionID))
                {
                    if (TimeSinceLastTeleport[Sp.Scene.RegionInfo.RegionID] > Util.UnixTimeSinceEpoch())
                    {
                        reason = "Too many teleports. Please try again soon.";
                        return false; // Too soon since the last TP
                    }
                }
                TimeSinceLastTeleport[Sp.Scene.RegionInfo.RegionID] = Util.UnixTimeSinceEpoch() + ((int)(SecondsBeforeNextTeleport));
            }

            //Gods tp freely
            if ((Sp != null && Sp.GodLevel != 0) || account.UserLevel != 0)
            {
                reason = "";
                return true;
            }

            //Check whether they fit any ban criteria
            if (Sp != null)
            {
                foreach (string banstr in BanCriteria)
                {
                    if (Sp.Name.Contains(banstr))
                    {
                        reason = "You have been banned from this region.";
                        return false;
                    }
                    else if (((System.Net.IPEndPoint)Sp.ControllingClient.GetClientEP()).Address.ToString().Contains(banstr))
                    {
                        reason = "You have been banned from this region.";
                        return false;
                    }
                }
                //Make sure they exist in the grid right now
                OpenSim.Services.Interfaces.IAgentInfoService presence = scene.RequestModuleInterface<IAgentInfoService>();
                if (presence == null)
                {
                    reason = String.Format("Failed to verify user presence in the grid for {0} in region {1}. Presence service does not exist.", account.Name, scene.RegionInfo.RegionName);
                    return false;
                }

                OpenSim.Services.Interfaces.UserInfo pinfo = presence.GetUserInfo(agent.AgentID.ToString());

                if (pinfo == null || (!pinfo.IsOnline && ((agent.teleportFlags & (uint)TeleportFlags.ViaLogin) == 0)))
                {
                    reason = String.Format("Failed to verify user presence in the grid for {0}, access denied to region {1}.", account.Name, scene.RegionInfo.RegionName);
                    return false;
                }
            }

            EstateSettings ES = scene.RegionInfo.EstateSettings;

            IEntityCountModule entityCountModule = scene.RequestModuleInterface<IEntityCountModule>();
            if (entityCountModule != null && scene.RegionInfo.RegionSettings.AgentLimit 
                < entityCountModule.RootAgents + 1)
            {
                reason = "Too many agents at this time. Please come back later.";
                return false;
            }

            List<EstateBan> EstateBans = new List<EstateBan>(ES.EstateBans);
            int i = 0;
            //Check bans
            foreach (EstateBan ban in EstateBans)
            {
                if (ban.BannedUserID == agent.AgentID)
                {
                    if (Sp != null)
                    {
                        string banIP = ((System.Net.IPEndPoint)Sp.ControllingClient.GetClientEP()).Address.ToString();

                        if (ban.BannedHostIPMask != banIP) //If it changed, ban them again
                        {
                            //Add the ban with the new hostname
                            ES.AddBan(new EstateBan()
                            {
                                BannedHostIPMask = banIP,
                                BannedUserID = ban.BannedUserID,
                                EstateID = ban.EstateID,
                                BannedHostAddress = ban.BannedHostAddress,
                                BannedHostNameMask = ban.BannedHostNameMask
                            });
                            //Update the database
                            ES.Save();
                        }
                    }

                    reason = "Banned from this region.";
                    return false;
                }
                if (Sp != null)
                {
                    IPAddress end = Sp.ControllingClient.EndPoint;
                    IPHostEntry rDNS = null;
                    try
                    {
                        rDNS = Dns.GetHostEntry(end);
                    }
                    catch (System.Net.Sockets.SocketException)
                    {
                        m_log.WarnFormat("[IPBAN] IP address \"{0}\" cannot be resolved via DNS", end);
                        rDNS = null;
                    }
                    if (ban.BannedHostIPMask == agent.IPAddress ||
                            (rDNS != null && rDNS.HostName.Contains(ban.BannedHostIPMask)) ||
                                end.ToString().StartsWith(ban.BannedHostIPMask))
                    {
                        //Ban the new user
                        ES.AddBan(new EstateBan()
                        {
                            EstateID = ES.EstateID,
                            BannedHostIPMask = agent.IPAddress,
                            BannedUserID = agent.AgentID,
                            BannedHostAddress = agent.IPAddress,
                            BannedHostNameMask = agent.IPAddress
                        });
                        ES.Save();

                        reason = "Banned from this region.";
                        return false;
                    }
                }
                i++;
            }
            
            //Estate owners/managers/access list people/access groups tp freely as well
            if (ES.EstateOwner == agent.AgentID ||
                new List<UUID>(ES.EstateManagers).Contains(agent.AgentID) ||
                new List<UUID>(ES.EstateAccess).Contains(agent.AgentID) ||
                (Sp != null && new List<UUID>(ES.EstateGroups).Contains(Sp.ControllingClient.ActiveGroupId)))
            {
                reason = "";
                return true;
            }

            if (ES.DenyAnonymous && ((account.UserFlags & (int)IUserProfileInfo.ProfileFlags.NoPaymentInfoOnFile) == (int)IUserProfileInfo.ProfileFlags.NoPaymentInfoOnFile))
            {
                reason = "You may not enter this region.";
                return false;
            }

            if (ES.DenyIdentified && ((account.UserFlags & (int)IUserProfileInfo.ProfileFlags.PaymentInfoOnFile) == (int)IUserProfileInfo.ProfileFlags.PaymentInfoOnFile))
            {
                reason = "You may not enter this region.";
                return false;
            }

            if (ES.DenyTransacted && ((account.UserFlags & (int)IUserProfileInfo.ProfileFlags.PaymentInfoInUse) == (int)IUserProfileInfo.ProfileFlags.PaymentInfoInUse))
            {
                reason = "You may not enter this region.";
                return false;
            }

            long m_Day = 25 * 60 * 60; //Find out day length in seconds
            if (scene.RegionInfo.RegionSettings.MinimumAge != 0 && (account.Created - Util.UnixTimeSinceEpoch()) < (scene.RegionInfo.RegionSettings.MinimumAge * m_Day))
            {
                reason = "You may not enter this region.";
                return false;
            }

            if (!ES.PublicAccess)
            {
                reason = "You may not enter this region.";
                return false;
            }

            IAgentConnector AgentConnector = DataManager.DataManager.RequestPlugin<IAgentConnector>();
            IAgentInfo agentInfo = null;
            if (AgentConnector != null)
            {
                agentInfo = AgentConnector.GetAgent(agent.AgentID);
                if (agentInfo == null)
                {
                    AgentConnector.CreateNewAgent(agent.AgentID);
                    agentInfo = AgentConnector.GetAgent(agent.AgentID);
                }
            }
            

            if (agentInfo != null && scene.RegionInfo.AccessLevel > Util.ConvertMaturityToAccessLevel((uint)agentInfo.MaturityRating))
            {
                reason = "The region has too high of a maturity level. Blocking teleport.";
                return false;
            }

            if (agentInfo != null && ES.DenyMinors && (agentInfo.Flags & IAgentFlags.Minor) == IAgentFlags.Minor)
            {
                reason = "The region has too high of a maturity level. Blocking teleport.";
                return false;
            }

            #endregion

            reason = "";
            return true;
        }

        private bool FindUnBannedParcel(Vector3 Position, IScenePresence Sp, UUID AgentID, out ILandObject ILO, out Vector3 newPosition, out string reason)
        {
            ILO = null;
            IParcelManagementModule parcelManagement = Sp.Scene.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                List<ILandObject> Parcels = parcelManagement.ParcelsNearPoint(Position);
                if (Parcels.Count == 0)
                {
                    if (Sp == null)
                        newPosition = new Vector3(0, 0, 0);
                    else
                        newPosition = parcelManagement.GetNearestRegionEdgePosition(Sp);
                    ILO = null;

                    //Dumped in the region corner, we will leave them there
                    reason = "";
                    return false;
                }
                else
                {
                    bool FoundParcel = false;
                    foreach (ILandObject lo in Parcels)
                    {
                        if (!lo.IsEitherBannedOrRestricted(AgentID))
                        {
                            newPosition = lo.LandData.UserLocation;
                            ILO = lo; //Update the parcel settings
                            FoundParcel = true;
                            break;
                        }
                    }
                    if (!FoundParcel)
                    {
                        //Dump them in the region corner as they are banned from all nearby parcels
                        if (Sp == null)
                            newPosition = new Vector3(0, 0, 0);
                        else
                            newPosition = parcelManagement.GetNearestRegionEdgePosition(Sp);
                        reason = "";
                        ILO = null;
                        return false;
                    }
                }
            }
            newPosition = Position;
            reason = "";
            return true;
        }

        #endregion

        #region ISharedRegionStartupModule Members

        public void Initialise(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            IConfig config = source.Configs["EstateSettingsModule"];
            if (config != null)
            {
                m_enabled = config.GetBoolean("Enabled", true);
                m_enabledBlockTeleportSeconds = config.GetBoolean("AllowBlockTeleportsMinTime", true);
                SecondsBeforeNextTeleport = config.GetFloat("BlockTeleportsTime", 3);
                StartDisabled = config.GetBoolean("StartDisabled", StartDisabled);

                string banCriteriaString = config.GetString("BanCriteria", "");
                if (banCriteriaString != "")
                    BanCriteria = banCriteriaString.Split(',');
            }

            if (!m_enabled)
                return;

            m_scenes.Add(scene);

            RegionConnector = DataManager.DataManager.RequestPlugin<IRegionConnector>();

            scene.EventManager.OnNewClient += OnNewClient;
            scene.Permissions.OnAllowIncomingAgent += OnAllowedIncomingAgent;
            scene.Permissions.OnAllowedIncomingTeleport += OnAllowedIncomingTeleport;
            scene.EventManager.OnClosingClient += OnClosingClient;

            MainConsole.Instance.Commands.AddCommand (this.Name, true,
                "set regionsetting maturity", "set regionsetting maturity [value]", "Sets a region's maturity - 0(PG),1(Mature),2(Adult)", SetRegionInfoOption);
            MainConsole.Instance.Commands.AddCommand (this.Name, true,
                "set regionsetting addestateban", "set regionsetting addestateban [first] [last]", "Add a user to the estate ban list", SetRegionInfoOption);
            MainConsole.Instance.Commands.AddCommand (this.Name, true,
                "set regionsetting removeestateban", "set regionsetting removeestateban [first] [last]", "Remove a user from the estate ban list", SetRegionInfoOption);
            MainConsole.Instance.Commands.AddCommand (this.Name, true,
                "set regionsetting addestatemanager", "set regionsetting addestatemanager [first] [last]", "Add a user to the estate manager list", SetRegionInfoOption);
            MainConsole.Instance.Commands.AddCommand (this.Name, true,
                "set regionsetting removeestatemanager", "set regionsetting removeestatemanager [first] [last]", "Remove a user from the estate manager list", SetRegionInfoOption);
            MainConsole.Instance.Commands.AddCommand (this.Name, true,
                "set regionsetting addestateaccess", "set regionsetting addestateaccess [first] [last]", "Add a user to the estate access list", SetRegionInfoOption);
            MainConsole.Instance.Commands.AddCommand (this.Name, true,
                "set regionsetting removeestateaccess", "set regionsetting removeestateaccess [first] [last]", "Remove a user from the estate access list", SetRegionInfoOption);
            
            
            MainConsole.Instance.Commands.AddCommand (this.Name, true,
                "ban user", "ban user", "Bans a user from the current estate", BanUser);
            MainConsole.Instance.Commands.AddCommand("access", true,
                    "login enable",
                    "login enable",
                    "Enable simulator logins",
                    String.Empty,
                    ProcessLoginCommands);

            MainConsole.Instance.Commands.AddCommand("access", true,
                    "login disable",
                    "login disable",
                    "Disable simulator logins",
                    String.Empty,
                    ProcessLoginCommands);

            MainConsole.Instance.Commands.AddCommand("access", true,
                    "login status",
                    "login status",
                    "Show login status",
                    String.Empty,
                    ProcessLoginCommands);
        }

        public void PostInitialise(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void FinishStartup(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void PostFinishStartup(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void Close(Scene scene)
        {
            if (!m_enabled)
                return;

            m_scenes.Remove(scene);
            scene.EventManager.OnNewClient -= OnNewClient;
            scene.Permissions.OnAllowIncomingAgent -= OnAllowedIncomingAgent;
            scene.Permissions.OnAllowedIncomingTeleport -= OnAllowedIncomingTeleport;
            scene.EventManager.OnClosingClient -= OnClosingClient;
        }

        public void StartupComplete()
        {
            if (!StartDisabled)
            {
                m_log.DebugFormat("[Region]: Enabling logins");
                LoginsDisabled = false;
            }
        }

        #endregion
    }
}
