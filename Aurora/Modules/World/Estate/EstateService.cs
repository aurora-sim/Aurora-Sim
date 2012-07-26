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
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;
using RegionFlags = Aurora.Framework.RegionFlags;

namespace Aurora.Modules.Estate
{
    public class EstateSettingsModule : ISharedRegionStartupModule
    {
        #region Declares

        private readonly Dictionary<UUID, int> LastTelehub = new Dictionary<UUID, int>();

        private readonly Dictionary<UUID, int> TimeSinceLastTeleport = new Dictionary<UUID, int>();
        private readonly List<IScene> m_scenes = new List<IScene>();
        private string[] BanCriteria = new string[0];
        private bool ForceLandingPointsOnCrossing;
        private bool LoginsDisabled = true;
        private IRegionConnector RegionConnector;
        private float SecondsBeforeNextTeleport = 3;
        private bool StartDisabled;
        private bool m_enabled;
        private bool m_enabledBlockTeleportSeconds;
        private bool m_checkMaturityLevel = true;

        #endregion

        #region ISharedRegionModule

        public string Name
        {
            get { return "EstateSettingsModule"; }
        }

        #endregion

        #region Console Commands

        protected void ProcessLoginCommands(string[] cmd)
        {
            if (cmd.Length < 2)
            {
                MainConsole.Instance.Info("Syntax: login enable|disable|status");
                return;
            }

            switch (cmd[1])
            {
                case "enable":
                    if (LoginsDisabled)
                        MainConsole.Instance.Warn("Enabling Logins");
                    LoginsDisabled = false;
                    break;
                case "disable":
                    if (!LoginsDisabled)
                        MainConsole.Instance.Warn("Disabling Logins");
                    LoginsDisabled = true;
                    break;
                case "status":
                    MainConsole.Instance.Warn("Logins are " + (LoginsDisabled ? "dis" : "en") + "abled.");
                    break;
                default:
                    MainConsole.Instance.Info("Syntax: login enable|disable|status");
                    break;
            }
        }

        protected void BanUser(string[] cmdparams)
        {
            if (cmdparams.Length < 4)
            {
                MainConsole.Instance.Warn("Not enough parameters!");
                return;
            }

            IScenePresence SP = MainConsole.Instance.ConsoleScene.SceneGraph.GetScenePresence(cmdparams[2], cmdparams[3]);
            if (SP == null)
            {
                MainConsole.Instance.Warn("Could not find user");
                return;
            }
            EstateSettings ES = MainConsole.Instance.ConsoleScene.RegionInfo.EstateSettings;
            AgentCircuitData circuitData =
                MainConsole.Instance.ConsoleScene.AuthenticateHandler.GetAgentCircuitData(SP.UUID);

            ES.AddBan(new EstateBan
                          {
                              BannedHostAddress = circuitData.IPAddress,
                              BannedHostIPMask = circuitData.IPAddress,
                              BannedHostNameMask = circuitData.IPAddress,
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
            IEntityTransferModule transferModule = SP.Scene.RequestModuleInterface<IEntityTransferModule>();
            if (transferModule != null)
                transferModule.IncomingCloseAgent(SP.Scene, SP.UUID);
        }

        protected void UnBanUser(string[] cmdparams)
        {
            if (cmdparams.Length < 4)
            {
                MainConsole.Instance.Warn("Not enough parameters!");
                return;
            }
            UserAccount account = MainConsole.Instance.ConsoleScene.UserAccountService.GetUserAccount(null,
                                                                                                      Util.CombineParams
                                                                                                          (cmdparams, 2));
            if (account == null)
            {
                MainConsole.Instance.Warn("Could not find user");
                return;
            }
            EstateSettings ES = MainConsole.Instance.ConsoleScene.RegionInfo.EstateSettings;
            ES.RemoveBan(account.PrincipalID);
            ES.Save();
        }

        protected void SetRegionInfoOption(string[] cmdparams)
        {
            IScene scene = MainConsole.Instance.ConsoleScene;
            if (scene == null)
                scene = m_scenes[0];

            #region 3 Params needed

            if (cmdparams.Length < 4)
            {
                MainConsole.Instance.Warn("Not enough parameters!");
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
                    MainConsole.Instance.Warn("Your parameter did not match any existing parameters. Try PG, Mature, or Adult");
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
                MainConsole.Instance.Warn("Not enough parameters!");
                return;
            }
            if (cmdparams[2] == "AddEstateBan".ToLower())
            {
                EstateBan EB = new EstateBan
                                   {
                                       BannedUserID =
                                           m_scenes[0].UserAccountService.GetUserAccount(null, cmdparams[3],
                                                                                         cmdparams[4]).PrincipalID
                                   };
                scene.RegionInfo.EstateSettings.AddBan(EB);
            }
            if (cmdparams[2] == "AddEstateManager".ToLower())
            {
                scene.RegionInfo.EstateSettings.AddEstateManager(
                    m_scenes[0].UserAccountService.GetUserAccount(null, cmdparams[3], cmdparams[4]).PrincipalID);
            }
            if (cmdparams[2] == "AddEstateAccess".ToLower())
            {
                scene.RegionInfo.EstateSettings.AddEstateUser(
                    m_scenes[0].UserAccountService.GetUserAccount(null, cmdparams[3], cmdparams[4]).PrincipalID);
            }
            if (cmdparams[2] == "RemoveEstateBan".ToLower())
            {
                scene.RegionInfo.EstateSettings.RemoveBan(
                    m_scenes[0].UserAccountService.GetUserAccount(null, cmdparams[3], cmdparams[4]).PrincipalID);
            }
            if (cmdparams[2] == "RemoveEstateManager".ToLower())
            {
                scene.RegionInfo.EstateSettings.RemoveEstateManager(
                    m_scenes[0].UserAccountService.GetUserAccount(null, cmdparams[3], cmdparams[4]).PrincipalID);
            }
            if (cmdparams[2] == "RemoveEstateAccess".ToLower())
            {
                scene.RegionInfo.EstateSettings.RemoveEstateUser(
                    m_scenes[0].UserAccountService.GetUserAccount(null, cmdparams[3], cmdparams[4]).PrincipalID);
            }

            #endregion

            scene.RegionInfo.RegionSettings.Save();
            scene.RegionInfo.EstateSettings.Save();
        }

        #endregion

        #region Client

        private void OnNewClient(IClientAPI client)
        {
            client.OnGodlikeMessage += GodlikeMessage;
            client.OnEstateTelehubRequest += GodlikeMessage;
                //This is ok, we do estate checks and check to make sure that only telehubs are dealt with here
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
            IScenePresence Sp = client.Scene.GetScenePresence(client.AgentId);
            if (!client.Scene.Permissions.CanIssueEstateCommand(client.AgentId, false))
                return;

            string parameter1 = Parameters[0];
            if (Method == "telehub")
            {
                if (parameter1 == "spawnpoint remove")
                {
                    Telehub telehub = RegionConnector.FindTelehub(client.Scene.RegionInfo.RegionID,
                                                                  client.Scene.RegionInfo.RegionHandle);
                    if (telehub == null)
                        return;
                    //Remove the one we sent at X
                    telehub.SpawnPos.RemoveAt(int.Parse(Parameters[1]));
                    RegionConnector.AddTelehub(telehub, client.Scene.RegionInfo.RegionHandle);
                    SendTelehubInfo(client);
                }
                if (parameter1 == "spawnpoint add")
                {
                    ISceneChildEntity part = Sp.Scene.GetSceneObjectPart(uint.Parse(Parameters[1]));
                    if (part == null)
                        return;
                    Telehub telehub = RegionConnector.FindTelehub(client.Scene.RegionInfo.RegionID,
                                                                  client.Scene.RegionInfo.RegionHandle);
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
                    ISceneChildEntity part = Sp.Scene.GetSceneObjectPart(uint.Parse(Parameters[1]));
                    if (part == null)
                        return;
                    Telehub telehub = RegionConnector.FindTelehub(client.Scene.RegionInfo.RegionID,
                                                                  client.Scene.RegionInfo.RegionHandle);
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
                Telehub telehub = RegionConnector.FindTelehub(client.Scene.RegionInfo.RegionID,
                                                              client.Scene.RegionInfo.RegionHandle);
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

        private bool OnAllowedIncomingTeleport(UUID userID, IScene scene, Vector3 Position, uint TeleportFlags,
                                               out Vector3 newPosition, out string reason)
        {
            newPosition = Position;
            UserAccount account = scene.UserAccountService.GetUserAccount(scene.RegionInfo.AllScopeIDs, userID);

            IScenePresence Sp = scene.GetScenePresence(userID);
            if (account == null)
            {
                IUserAgentService uas = scene.RequestModuleInterface<IUserAgentService>();
                AgentCircuitData circuit;
                if (uas == null ||
                    (circuit = scene.AuthenticateHandler.GetAgentCircuitData(userID)) != null ||
                    !uas.VerifyAgent(circuit))
                {
                    reason = "Failed authentication.";
                    return false; //NO!
                }
            }


            //Make sure that this user is inside the region as well
            if (Position.X < -2f || Position.Y < -2f ||
                Position.X > scene.RegionInfo.RegionSizeX + 2 || Position.Y > scene.RegionInfo.RegionSizeY + 2)
            {
                MainConsole.Instance.DebugFormat(
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
                if (changedY)
                    Position.Y = scene.RegionInfo.RegionSizeY - Position.Y;
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
                if (Sp != null)
                    Sp.ClearSavedVelocity(); //If we are moving the agent, clear their velocity
                //Can't find land, give them the first parcel in the region and find a good position for them
                ILO = parcelManagement.AllParcels()[0];
                Position = parcelManagement.GetParcelCenterAtGround(ILO);
            }

            //parcel permissions
            if (ILO.IsBannedFromLand(userID)) //Note: restricted is dealt with in the next block
            {
                if (Sp != null)
                    Sp.ClearSavedVelocity(); //If we are moving the agent, clear their velocity
                if (Sp == null)
                {
                    reason = "Banned from this parcel.";
                    return false;
                }

                if (!FindUnBannedParcel(Position, Sp, userID, out ILO, out newPosition, out reason))
                {
                    //We found a place for them, but we don't need to check any further on positions here
                    //return true;
                }
            }
            //Move them out of banned parcels
            ParcelFlags parcelflags = (ParcelFlags) ILO.LandData.Flags;
            if ((parcelflags & ParcelFlags.UseAccessGroup) == ParcelFlags.UseAccessGroup &&
                (parcelflags & ParcelFlags.UseAccessList) == ParcelFlags.UseAccessList &&
                (parcelflags & ParcelFlags.UsePassList) == ParcelFlags.UsePassList)
            {
                if (Sp != null)
                    Sp.ClearSavedVelocity(); //If we are moving the agent, clear their velocity
                //One of these is in play then
                if ((parcelflags & ParcelFlags.UseAccessGroup) == ParcelFlags.UseAccessGroup)
                {
                    if (Sp == null)
                    {
                        reason = "Banned from this parcel.";
                        return false;
                    }
                    if (Sp.ControllingClient.ActiveGroupId != ILO.LandData.GroupID)
                    {
                        if (!FindUnBannedParcel(Position, Sp, userID, out ILO, out newPosition, out reason))
                        {
                            //We found a place for them, but we don't need to check any further on positions here
                            //return true;
                        }
                    }
                }
                else if ((parcelflags & ParcelFlags.UseAccessList) == ParcelFlags.UseAccessList)
                {
                    if (Sp == null)
                    {
                        reason = "Banned from this parcel.";
                        return false;
                    }
                    //All but the people on the access list are banned
                    if (ILO.IsRestrictedFromLand(userID))
                        if (!FindUnBannedParcel(Position, Sp, userID, out ILO, out newPosition, out reason))
                        {
                            //We found a place for them, but we don't need to check any further on positions here
                            //return true;
                        }
                }
                else if ((parcelflags & ParcelFlags.UsePassList) == ParcelFlags.UsePassList)
                {
                    if (Sp == null)
                    {
                        reason = "Banned from this parcel.";
                        return false;
                    }
                    //All but the people on the pass/access list are banned
                    if (ILO.IsRestrictedFromLand(Sp.UUID))
                        if (!FindUnBannedParcel(Position, Sp, userID, out ILO, out newPosition, out reason))
                        {
                            //We found a place for them, but we don't need to check any further on positions here
                            //return true;
                        }
                }
            }

            EstateSettings ES = scene.RegionInfo.EstateSettings;
            TeleportFlags tpflags = (TeleportFlags) TeleportFlags;
            const TeleportFlags allowableFlags = OpenMetaverse.TeleportFlags.ViaLandmark | OpenMetaverse.TeleportFlags.ViaHome |
                                                 OpenMetaverse.TeleportFlags.ViaLure |
                                                 OpenMetaverse.TeleportFlags.ForceRedirect |
                                                 OpenMetaverse.TeleportFlags.Godlike | OpenMetaverse.TeleportFlags.NineOneOne;

            //If the user wants to force landing points on crossing, we act like they are not crossing, otherwise, check the child property and that the ViaRegionID is set
            bool isCrossing = !ForceLandingPointsOnCrossing && (Sp != null && Sp.IsChildAgent &&
                                                                ((tpflags & OpenMetaverse.TeleportFlags.ViaRegionID) ==
                                                                 OpenMetaverse.TeleportFlags.ViaRegionID));
            //Move them to the nearest landing point
            if (!((tpflags & allowableFlags) != 0) && !isCrossing && !ES.AllowDirectTeleport)
            {
                if (Sp != null)
                    Sp.ClearSavedVelocity(); //If we are moving the agent, clear their velocity
                if (!scene.Permissions.IsGod(userID))
                {
                    Telehub telehub = RegionConnector.FindTelehub(scene.RegionInfo.RegionID,
                                                                  scene.RegionInfo.RegionHandle);
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
                            Position = telehub.SpawnPos[LastTelehubNum] +
                                       new Vector3(telehub.TelehubLocX, telehub.TelehubLocY, telehub.TelehubLocZ);
                            LastTelehubNum++;
                            if (LastTelehubNum == telehub.SpawnPos.Count)
                                LastTelehubNum = 0;
                            LastTelehub[scene.RegionInfo.RegionID] = LastTelehubNum;
                        }
                    }
                }
            }
            else if (!((tpflags & allowableFlags) != 0) && !isCrossing &&
                     !scene.Permissions.GenericParcelPermission(userID, ILO, (ulong) GroupPowers.None))
                //Telehubs override parcels
            {
                if (Sp != null)
                    Sp.ClearSavedVelocity(); //If we are moving the agent, clear their velocity
                if (ILO.LandData.LandingType == (int) LandingType.None) //Blocked, force this person off this land
                {
                    //Find a new parcel for them
                    List<ILandObject> Parcels = parcelManagement.ParcelsNearPoint(Position);
                    if (Parcels.Count > 1)
                    {
                        newPosition = parcelManagement.GetNearestRegionEdgePosition(Sp);
                    }
                    else
                    {
                        bool found = false;
                        //We need to check here as well for bans, can't toss someone into a parcel they are banned from
#if (!ISWIN)
                        foreach (ILandObject Parcel in Parcels)
                        {
                            if (!Parcel.IsBannedFromLand(userID))
                            {
                                //Now we have to check their userloc
                                if (ILO.LandData.LandingType == (int) LandingType.None)
                                    continue; //Blocked, check next one
                                else if (ILO.LandData.LandingType == (int) LandingType.LandingPoint)
                                    //Use their landing spot
                                    newPosition = Parcel.LandData.UserLocation;
                                else //They allow for anywhere, so dump them in the center at the ground
                                    newPosition = parcelManagement.GetParcelCenterAtGround(Parcel);
                                found = true;
                            }
                        }
#else
                        foreach (ILandObject Parcel in Parcels.Where(Parcel => !Parcel.IsBannedFromLand(userID)))
                        {
                            //Now we have to check their userloc
                            if (ILO.LandData.LandingType == (int) LandingType.None)
                                continue; //Blocked, check next one
                            else if (ILO.LandData.LandingType == (int) LandingType.LandingPoint)
                                //Use their landing spot
                                newPosition = Parcel.LandData.UserLocation;
                            else //They allow for anywhere, so dump them in the center at the ground
                                newPosition = parcelManagement.GetParcelCenterAtGround(Parcel);
                            found = true;
                        }
#endif
                        if (!found) //Dump them at the edge
                        {
                            if (Sp != null)
                                newPosition = parcelManagement.GetNearestRegionEdgePosition(Sp);
                            else
                            {
                                reason = "Banned from this parcel.";
                                return false;
                            }
                        }
                    }
                }
                else if (ILO.LandData.LandingType == (int) LandingType.LandingPoint) //Move to tp spot
                    newPosition = ILO.LandData.UserLocation != Vector3.Zero
                                      ? ILO.LandData.UserLocation
                                      : parcelManagement.GetNearestRegionEdgePosition(Sp);
            }

            //We assume that our own region isn't null....
            if (agentInfo != null)
            {
                //Can only enter prelude regions once!
                if (scene.RegionInfo.RegionFlags != -1 && ((scene.RegionInfo.RegionFlags & (int)RegionFlags.Prelude) == (int)RegionFlags.Prelude) &&
                    agentInfo != null)
                {
                    if (agentInfo.OtherAgentInformation.ContainsKey("Prelude" + scene.RegionInfo.RegionID))
                    {
                        reason = "You may not enter this region as you have already been to this prelude region.";
                        return false;
                    }
                    else
                    {
                        agentInfo.OtherAgentInformation.Add("Prelude" + scene.RegionInfo.RegionID,
                                                            OSD.FromInteger((int) IAgentFlags.PastPrelude));
                        AgentConnector.UpdateAgent(agentInfo);
                    }
                }
                if (agentInfo.OtherAgentInformation.ContainsKey("LimitedToEstate"))
                {
                    int LimitedToEstate = agentInfo.OtherAgentInformation["LimitedToEstate"];
                    if (scene.RegionInfo.EstateSettings.EstateID != LimitedToEstate)
                    {
                        reason = "You may not enter this reason, as it is outside of the estate you are limited to.";
                        return false;
                    }
                }
            }


            if ((ILO.LandData.Flags & (int) ParcelFlags.DenyAnonymous) != 0)
            {
                if (account != null &&
                    (account.UserFlags & (int) IUserProfileInfo.ProfileFlags.NoPaymentInfoOnFile) ==
                    (int) IUserProfileInfo.ProfileFlags.NoPaymentInfoOnFile)
                {
                    reason = "You may not enter this region.";
                    return false;
                }
            }

            if ((ILO.LandData.Flags & (uint) ParcelFlags.DenyAgeUnverified) != 0 && agentInfo != null)
            {
                if ((agentInfo.Flags & IAgentFlags.Minor) == IAgentFlags.Minor)
                {
                    reason = "You may not enter this region.";
                    return false;
                }
            }

            //Check that we are not underground as well
            ITerrainChannel chan = scene.RequestModuleInterface<ITerrainChannel>();
            if (chan != null)
            {
                float posZLimit = chan[(int) newPosition.X, (int) newPosition.Y] + (float) 1.25;

                if (posZLimit >= (newPosition.Z) && !(Single.IsInfinity(posZLimit) || Single.IsNaN(posZLimit)))
                {
                    newPosition.Z = posZLimit;
                }
            }

            //newPosition = Position;
            reason = "";
            return true;
        }

        private bool OnAllowedIncomingAgent(IScene scene, AgentCircuitData agent, bool isRootAgent, out string reason)
        {
            #region Incoming Agent Checks

            UserAccount account = scene.UserAccountService.GetUserAccount(scene.RegionInfo.AllScopeIDs, agent.AgentID);
            bool foreign = false;
            IScenePresence Sp = scene.GetScenePresence(agent.AgentID);
            if (account == null)
            {
                IUserAgentService uas = scene.RequestModuleInterface<IUserAgentService>();
                if (uas != null) //SOO hate doing this...
                    foreign = true;
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
                TimeSinceLastTeleport[Sp.Scene.RegionInfo.RegionID] = Util.UnixTimeSinceEpoch() +
                                                                      ((int) (SecondsBeforeNextTeleport));
            }

            //Gods tp freely
            if ((Sp != null && Sp.GodLevel != 0) || (account != null && account.UserLevel != 0))
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
                    else if (((IPEndPoint) Sp.ControllingClient.GetClientEP()).Address.ToString().Contains(banstr))
                    {
                        reason = "You have been banned from this region.";
                        return false;
                    }
                }
                //Make sure they exist in the grid right now
                IAgentInfoService presence = scene.RequestModuleInterface<IAgentInfoService>();
                if (presence == null)
                {
                    reason =
                        String.Format(
                            "Failed to verify user presence in the grid for {0} in region {1}. Presence service does not exist.",
                            account.Name, scene.RegionInfo.RegionName);
                    return false;
                }

                UserInfo pinfo = presence.GetUserInfo(agent.AgentID.ToString());

                if (!foreign &&
                    (pinfo == null || (!pinfo.IsOnline && ((agent.teleportFlags & (uint) TeleportFlags.ViaLogin) == 0))))
                {
                    reason =
                        String.Format(
                            "Failed to verify user presence in the grid for {0}, access denied to region {1}.",
                            account.Name, scene.RegionInfo.RegionName);
                    return false;
                }
            }

            EstateSettings ES = scene.RegionInfo.EstateSettings;

            IEntityCountModule entityCountModule = scene.RequestModuleInterface<IEntityCountModule>();
            if (entityCountModule != null && scene.RegionInfo.RegionSettings.AgentLimit
                < entityCountModule.RootAgents + 1 && scene.RegionInfo.RegionSettings.AgentLimit > 0)
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
                        string banIP = ((IPEndPoint) Sp.ControllingClient.GetClientEP()).Address.ToString();

                        if (ban.BannedHostIPMask != banIP) //If it changed, ban them again
                        {
                            //Add the ban with the new hostname
                            ES.AddBan(new EstateBan
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
                    catch (SocketException)
                    {
                        MainConsole.Instance.WarnFormat("[IPBAN] IP address \"{0}\" cannot be resolved via DNS", end);
                        rDNS = null;
                    }
                    if (ban.BannedHostIPMask == agent.IPAddress ||
                        (rDNS != null && rDNS.HostName.Contains(ban.BannedHostIPMask)) ||
                        end.ToString().StartsWith(ban.BannedHostIPMask))
                    {
                        //Ban the new user
                        ES.AddBan(new EstateBan
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
                CheckEstateGroups(ES, agent))
            {
                reason = "";
                return true;
            }

            if (account != null && ES.DenyAnonymous &&
                ((account.UserFlags & (int) IUserProfileInfo.ProfileFlags.NoPaymentInfoOnFile) ==
                 (int) IUserProfileInfo.ProfileFlags.NoPaymentInfoOnFile))
            {
                reason = "You may not enter this region.";
                return false;
            }

            if (account != null && ES.DenyIdentified &&
                ((account.UserFlags & (int) IUserProfileInfo.ProfileFlags.PaymentInfoOnFile) ==
                 (int) IUserProfileInfo.ProfileFlags.PaymentInfoOnFile))
            {
                reason = "You may not enter this region.";
                return false;
            }

            if (account != null && ES.DenyTransacted &&
                ((account.UserFlags & (int) IUserProfileInfo.ProfileFlags.PaymentInfoInUse) ==
                 (int) IUserProfileInfo.ProfileFlags.PaymentInfoInUse))
            {
                reason = "You may not enter this region.";
                return false;
            }

            const long m_Day = 25*60*60; //Find out day length in seconds
            if (account != null && scene.RegionInfo.RegionSettings.MinimumAge != 0 &&
                (account.Created - Util.UnixTimeSinceEpoch()) < (scene.RegionInfo.RegionSettings.MinimumAge*m_Day))
            {
                reason = "You may not enter this region.";
                return false;
            }

            if (!ES.PublicAccess)
            {
                reason = "You may not enter this region, Public access has been turned off.";
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

            if (m_checkMaturityLevel)
            {
                if (agentInfo != null &&
                    scene.RegionInfo.AccessLevel > Util.ConvertMaturityToAccessLevel((uint) agentInfo.MaturityRating))
                {
                    reason = "The region has too high of a maturity level. Blocking teleport.";
                    return false;
                }

                if (agentInfo != null && ES.DenyMinors && (agentInfo.Flags & IAgentFlags.Minor) == IAgentFlags.Minor)
                {
                    reason = "The region has too high of a maturity level. Blocking teleport.";
                    return false;
                }
            }

            #endregion

            reason = "";
            return true;
        }

        private bool CheckEstateGroups(EstateSettings ES, AgentCircuitData agent)
        {
            IGroupsModule gm = m_scenes.Count == 0 ? null : m_scenes[0].RequestModuleInterface<IGroupsModule>();
            if (gm != null && ES.EstateGroups.Length > 0)
            {
                List<UUID> esGroups = new List<UUID>(ES.EstateGroups);
                GroupMembershipData[] gmds = gm.GetMembershipData(agent.AgentID);
#if (!ISWIN)
                foreach (GroupMembershipData gmd in gmds)
                {
                    if (esGroups.Contains(gmd.GroupID)) return true;
                }
                return false;
#else
                return gmds.Any(gmd => esGroups.Contains(gmd.GroupID));
#endif
            }
            return false;
        }

        private bool FindUnBannedParcel(Vector3 Position, IScenePresence Sp, UUID AgentID, out ILandObject ILO,
                                        out Vector3 newPosition, out string reason)
        {
            ILO = null;
            IParcelManagementModule parcelManagement = Sp.Scene.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                List<ILandObject> Parcels = parcelManagement.ParcelsNearPoint(Position);
                if (Parcels.Count == 0)
                {
                    newPosition = Sp == null ? new Vector3(0, 0, 0) : parcelManagement.GetNearestRegionEdgePosition(Sp);
                    ILO = null;

                    //Dumped in the region corner, we will leave them there
                    reason = "";
                    return false;
                }
                else
                {
                    bool FoundParcel = false;
#if (!ISWIN)
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
#else
                    foreach (ILandObject lo in Parcels.Where(lo => !lo.IsEitherBannedOrRestricted(AgentID)))
                    {
                        newPosition = lo.LandData.UserLocation;
                        ILO = lo; //Update the parcel settings
                        FoundParcel = true;
                        break;
                    }
#endif
                    if (!FoundParcel)
                    {
                        //Dump them in the region corner as they are banned from all nearby parcels
                        newPosition = Sp == null ? new Vector3(0, 0, 0) : parcelManagement.GetNearestRegionEdgePosition(Sp);
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

        public void Initialise(IScene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            IConfig config = source.Configs["EstateSettingsModule"];
            if (config != null)
            {
                m_enabled = config.GetBoolean("Enabled", true);
                m_enabledBlockTeleportSeconds = config.GetBoolean("AllowBlockTeleportsMinTime", true);
                SecondsBeforeNextTeleport = config.GetFloat("BlockTeleportsTime", 3);
                StartDisabled = config.GetBoolean("StartDisabled", StartDisabled);
                ForceLandingPointsOnCrossing = config.GetBoolean("ForceLandingPointsOnCrossing",
                                                                 ForceLandingPointsOnCrossing);
                m_checkMaturityLevel = config.GetBoolean("CheckMaturityLevel", true);

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
            if (MainConsole.Instance != null)
            {
                MainConsole.Instance.Commands.AddCommand(
                    "set regionsetting maturity", "set regionsetting maturity [value]",
                    "Sets a region's maturity - 0(PG),1(Mature),2(Adult)", SetRegionInfoOption);
                MainConsole.Instance.Commands.AddCommand(
                    "set regionsetting addestateban", "set regionsetting addestateban [first] [last]",
                    "Add a user to the estate ban list", SetRegionInfoOption);
                MainConsole.Instance.Commands.AddCommand(
                    "set regionsetting removeestateban", "set regionsetting removeestateban [first] [last]",
                    "Remove a user from the estate ban list", SetRegionInfoOption);
                MainConsole.Instance.Commands.AddCommand(
                    "set regionsetting addestatemanager", "set regionsetting addestatemanager [first] [last]",
                    "Add a user to the estate manager list", SetRegionInfoOption);
                MainConsole.Instance.Commands.AddCommand(
                    "set regionsetting removeestatemanager", "set regionsetting removeestatemanager [first] [last]",
                    "Remove a user from the estate manager list", SetRegionInfoOption);
                MainConsole.Instance.Commands.AddCommand(
                    "set regionsetting addestateaccess", "set regionsetting addestateaccess [first] [last]",
                    "Add a user to the estate access list", SetRegionInfoOption);
                MainConsole.Instance.Commands.AddCommand(
                    "set regionsetting removeestateaccess", "set regionsetting removeestateaccess [first] [last]",
                    "Remove a user from the estate access list", SetRegionInfoOption);


                MainConsole.Instance.Commands.AddCommand(
                    "estate ban user", "estate ban user", "Bans a user from the current estate", BanUser);
                MainConsole.Instance.Commands.AddCommand(
                    "estate unban user", "estate unban user", "Bans a user from the current estate", UnBanUser);
                MainConsole.Instance.Commands.AddCommand(
                    "login enable",
                    "login enable",
                    "Enable simulator logins",
                    ProcessLoginCommands);

                MainConsole.Instance.Commands.AddCommand(
                    "login disable",
                    "login disable",
                    "Disable simulator logins",
                    ProcessLoginCommands);

                MainConsole.Instance.Commands.AddCommand(
                    "login status",
                    "login status",
                    "Show login status",
                    ProcessLoginCommands);
            }
        }

        public void PostInitialise(IScene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void FinishStartup(IScene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void PostFinishStartup(IScene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void Close(IScene scene)
        {
            if (!m_enabled)
                return;

            m_scenes.Remove(scene);
            scene.EventManager.OnNewClient -= OnNewClient;
            scene.Permissions.OnAllowIncomingAgent -= OnAllowedIncomingAgent;
            scene.Permissions.OnAllowedIncomingTeleport -= OnAllowedIncomingTeleport;
            scene.EventManager.OnClosingClient -= OnClosingClient;
        }

        public void DeleteRegion(IScene scene)
        {
        }

        public void StartupComplete()
        {
            if (!StartDisabled)
            {
                MainConsole.Instance.DebugFormat("[Region]: Enabling logins");
                LoginsDisabled = false;
            }
        }

        #endregion
    }
}