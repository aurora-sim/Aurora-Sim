/*
 *  Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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
using System.Reflection;
using System.Threading;
using log4net;
using OpenMetaverse;
using OpenMetaverse.Packets;
using OpenSim.Framework;
using OpenSim.Services.Interfaces;

namespace OpenSim.Region.Framework.Scenes
{
    public partial class Scene
    {
        private static readonly ILog m_log
            = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public virtual void ProcessMoneyTransferRequest(UUID source, UUID destination, int amount, 
                                                        int transactiontype, string description)
        {
            EventManager.MoneyTransferArgs args = new EventManager.MoneyTransferArgs(source, destination, amount, 
                                                                                     transactiontype, description);

            EventManager.TriggerMoneyTransfer(this, args);
        }

        public virtual void ProcessObjectGrab(uint localID, Vector3 offsetPos, IClientAPI remoteClient, List<SurfaceTouchEventArgs> surfaceArgs)
        {
            SurfaceTouchEventArgs surfaceArg = null;
            if (surfaceArgs != null && surfaceArgs.Count > 0)
                surfaceArg = surfaceArgs[0];
            ISceneEntity childPrim;
            SceneObjectPart part;
            if (m_sceneGraph.TryGetPart(localID, out childPrim))
            {
                part = childPrim as SceneObjectPart;
                SceneObjectGroup obj = part.ParentGroup;
                if (obj.RootPart.BlockGrab)
                    return;
                // Currently only grab/touch for the single prim
                // the client handles rez correctly
                obj.ObjectGrabHandler(localID, offsetPos, remoteClient);

                // If the touched prim handles touches, deliver it
                // If not, deliver to root prim
                EventManager.TriggerObjectGrab(part, part, part.OffsetPosition, remoteClient, surfaceArg);
                // Deliver to the root prim if the touched prim doesn't handle touches
                // or if we're meant to pass on touches anyway. Don't send to root prim
                // if prim touched is the root prim as we just did it
                if ((part.LocalId != obj.RootPart.LocalId))
                {
                    const int PASS_IF_NOT_HANDLED = 0;
                    const int PASS_ALWAYS = 1;
                    const int PASS_NEVER = 2;
                    if (part.PassTouch == PASS_NEVER)
                    {
                    }
                    if (part.PassTouch == PASS_ALWAYS)
                    {
                        EventManager.TriggerObjectGrab(obj.RootPart, part, part.OffsetPosition, remoteClient, surfaceArg);
                    }
                    else if (((part.ScriptEvents & scriptEvents.touch_start) == 0) && part.PassTouch == PASS_IF_NOT_HANDLED) //If no event in this prim, pass to parent
                    {
                        EventManager.TriggerObjectGrab(obj.RootPart, part, part.OffsetPosition, remoteClient, surfaceArg);
                    }
                }
            }
        }

        public virtual void ProcessObjectGrabUpdate(UUID objectID, Vector3 offset, Vector3 pos, IClientAPI remoteClient, List<SurfaceTouchEventArgs> surfaceArgs)
        {
            SurfaceTouchEventArgs surfaceArg = null;
            if (surfaceArgs != null && surfaceArgs.Count > 0)
                surfaceArg = surfaceArgs[0];

            ISceneEntity childPrim;
            SceneObjectPart part;

            if (m_sceneGraph.TryGetPart(objectID, out childPrim))
            {
                part = childPrim as SceneObjectPart;
                SceneObjectGroup obj = part.ParentGroup;
                if (obj.RootPart.BlockGrab)
                    return;

                // If the touched prim handles touches, deliver it
                // If not, deliver to root prim
                EventManager.TriggerObjectGrabbing(part, part, part.OffsetPosition, remoteClient, surfaceArg);
                // Deliver to the root prim if the touched prim doesn't handle touches
                // or if we're meant to pass on touches anyway. Don't send to root prim
                // if prim touched is the root prim as we just did it

                if ((part.LocalId != obj.RootPart.LocalId))
                {
                    const int PASS_IF_NOT_HANDLED = 0;
                    const int PASS_ALWAYS = 1;
                    const int PASS_NEVER = 2;
                    if (part.PassTouch == PASS_NEVER)
                    {
                    }
                    if (part.PassTouch == PASS_ALWAYS)
                    {
                        EventManager.TriggerObjectGrabbing(obj.RootPart, part, part.OffsetPosition, remoteClient, surfaceArg);
                    }
                    else if ((((part.ScriptEvents & scriptEvents.touch_start) == 0) || ((part.ScriptEvents & scriptEvents.touch) == 0)) && part.PassTouch == PASS_IF_NOT_HANDLED) //If no event in this prim, pass to parent
                    {
                        EventManager.TriggerObjectGrabbing(obj.RootPart, part, part.OffsetPosition, remoteClient, surfaceArg);
                    }
                }
            }
         }

        public virtual void ProcessObjectDeGrab(uint localID, IClientAPI remoteClient, List<SurfaceTouchEventArgs> surfaceArgs)
        {
            SurfaceTouchEventArgs surfaceArg = null;
            if (surfaceArgs != null && surfaceArgs.Count > 0)
                surfaceArg = surfaceArgs[0];

            ISceneEntity childPrim;
            SceneObjectPart part;
            if (m_sceneGraph.TryGetPart(localID, out childPrim))
            {
                part = childPrim as SceneObjectPart;
                SceneObjectGroup obj = part.ParentGroup;
                // If the touched prim handles touches, deliver it
                // If not, deliver to root prim
                EventManager.TriggerObjectDeGrab(part, part, remoteClient, surfaceArg);

                if ((part.LocalId != obj.RootPart.LocalId))
                {
                    const int PASS_IF_NOT_HANDLED = 0;
                    const int PASS_ALWAYS = 1;
                    const int PASS_NEVER = 2;
                    if (part.PassTouch == PASS_NEVER)
                    {
                    }
                    if (part.PassTouch == PASS_ALWAYS)
                    {
                        EventManager.TriggerObjectDeGrab(obj.RootPart, part, remoteClient, surfaceArg);
                    }
                    else if ((((part.ScriptEvents & scriptEvents.touch_start) == 0) || ((part.ScriptEvents & scriptEvents.touch_end) == 0)) && part.PassTouch == PASS_IF_NOT_HANDLED) //If no event in this prim, pass to parent
                    {
                        EventManager.TriggerObjectDeGrab(obj.RootPart, part, remoteClient, surfaceArg);
                    }
                }
            }
        }

        public void ProcessAvatarPickerRequest(IClientAPI client, UUID avatarID, UUID RequestID, string query)
        {
            List<UserAccount> accounts = UserAccountService.GetUserAccounts(RegionInfo.ScopeID, query);

            if (accounts == null)
                accounts = new List<UserAccount>(0);

            AvatarPickerReplyPacket replyPacket = (AvatarPickerReplyPacket) PacketPool.Instance.GetPacket(PacketType.AvatarPickerReply);
            // TODO: don't create new blocks if recycling an old packet

            AvatarPickerReplyPacket.DataBlock[] searchData =
                new AvatarPickerReplyPacket.DataBlock[accounts.Count];
            AvatarPickerReplyPacket.AgentDataBlock agentData = new AvatarPickerReplyPacket.AgentDataBlock();

            agentData.AgentID = avatarID;
            agentData.QueryID = RequestID;
            replyPacket.AgentData = agentData;
            //byte[] bytes = new byte[AvatarResponses.Count*32];

            int i = 0;
            foreach (UserAccount item in accounts)
            {
                UUID translatedIDtem = item.PrincipalID;
                searchData[i] = new AvatarPickerReplyPacket.DataBlock();
                searchData[i].AvatarID = translatedIDtem;
                searchData[i].FirstName = Utils.StringToBytes((string) item.FirstName);
                searchData[i].LastName = Utils.StringToBytes((string) item.LastName);
                i++;
            }
            if (accounts.Count == 0)
            {
                searchData = new AvatarPickerReplyPacket.DataBlock[0];
            }
            replyPacket.Data = searchData;

            AvatarPickerReplyAgentDataArgs agent_data = new AvatarPickerReplyAgentDataArgs();
            agent_data.AgentID = replyPacket.AgentData.AgentID;
            agent_data.QueryID = replyPacket.AgentData.QueryID;

            List<AvatarPickerReplyDataArgs> data_args = new List<AvatarPickerReplyDataArgs>();
            for (i = 0; i < replyPacket.Data.Length; i++)
            {
                AvatarPickerReplyDataArgs data_arg = new AvatarPickerReplyDataArgs();
                data_arg.AvatarID = replyPacket.Data[i].AvatarID;
                data_arg.FirstName = replyPacket.Data[i].FirstName;
                data_arg.LastName = replyPacket.Data[i].LastName;
                data_args.Add(data_arg);
            }
            client.SendAvatarPickerReply(agent_data, data_args);
        }

        public void HandleUUIDNameRequest(UUID uuid, IClientAPI remote_client)
        {
            UserAccount account = UserAccountService.GetUserAccount(RegionInfo.ScopeID, uuid);
            if (account != null)
            {
                remote_client.SendNameReply(uuid, account.FirstName, account.LastName);
            }
        }
    }
}
