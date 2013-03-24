/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using System.Linq;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using ProtoBuf;

namespace Aurora.Framework.ClientInterfaces
{
    public interface IAgentData
    {
        UUID AgentID { get; set; }
    }

    /// <summary>
    /// Replacement for ChildAgentDataUpdate
    /// </summary>
    [ProtoContract(UseProtoMembersOnly=true)]
    public class AgentPosition : IDataTransferable, IAgentData
    {
        [ProtoMember(1)]
        public Vector3 AtAxis;
        [ProtoMember(2)]
        public Vector3 Center;
        [ProtoMember(3)]
        public float Far;
        [ProtoMember(4)]
        public Vector3 LeftAxis;
        [ProtoMember(5)]
        public Vector3 Position;
        [ProtoMember(6)]
        public ulong RegionHandle;
        [ProtoMember(7)]
        public Vector3 Size;
        [ProtoMember(8)]
        public Vector3 UpAxis;
        [ProtoMember(9)]
        public bool UserGoingOffline;
        [ProtoMember(10)]
        public Vector3 Velocity;
        [ProtoMember(11)]
        public UUID AgentID { get; set; }

        #region IAgentData Members

        public override OSDMap ToOSD()
        {
            OSDMap args = new OSDMap();
            args["message_type"] = OSD.FromString("AgentPosition");

            args["region_handle"] = OSD.FromString(RegionHandle.ToString());
            args["agent_uuid"] = OSD.FromUUID(AgentID);

            args["position"] = OSD.FromString(Position.ToString());
            args["velocity"] = OSD.FromString(Velocity.ToString());
            args["center"] = OSD.FromString(Center.ToString());
            args["size"] = OSD.FromString(Size.ToString());
            args["at_axis"] = OSD.FromString(AtAxis.ToString());
            args["left_axis"] = OSD.FromString(LeftAxis.ToString());
            args["up_axis"] = OSD.FromString(UpAxis.ToString());
            args["user_going_offline"] = UserGoingOffline;

            args["far"] = OSD.FromReal(Far);

            return args;
        }

        public override void FromOSD(OSDMap args)
        {
            if (args.ContainsKey("region_handle"))
                UInt64.TryParse(args["region_handle"].AsString(), out RegionHandle);

            if (args["agent_uuid"] != null)
                AgentID = args["agent_uuid"].AsUUID();

            if (args["position"] != null)
                Vector3.TryParse(args["position"].AsString(), out Position);

            if (args["velocity"] != null)
                Vector3.TryParse(args["velocity"].AsString(), out Velocity);

            if (args["center"] != null)
                Vector3.TryParse(args["center"].AsString(), out Center);

            if (args["size"] != null)
                Vector3.TryParse(args["size"].AsString(), out Size);

            if (args["at_axis"] != null)
                Vector3.TryParse(args["at_axis"].AsString(), out AtAxis);

            if (args["left_axis"] != null)
                Vector3.TryParse(args["left_axis"].AsString(), out LeftAxis);

            if (args["up_axis"] != null)
                Vector3.TryParse(args["up_axis"].AsString(), out UpAxis);

            if (args["far"] != null)
                Far = (float) (args["far"].AsReal());

            if (args["user_going_offline"] != null)
                UserGoingOffline = args["user_going_offline"];
        }

        #endregion
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class ControllerData : IDataTransferable
    {
        [ProtoMember(1)]
        public uint EventControls;
        [ProtoMember(2)]
        public uint IgnoreControls;
        [ProtoMember(3)]
        public UUID ItemID;
        [ProtoMember(4)]
        public UUID ObjectID;

        public ControllerData(UUID item, UUID objID, uint ignore, uint ev)
        {
            ItemID = item;
            ObjectID = objID;
            IgnoreControls = ignore;
            EventControls = ev;
        }

        public ControllerData(OSDMap args)
        {
            FromOSD(args);
        }

        public override OSDMap ToOSD()
        {
            OSDMap controldata = new OSDMap();
            controldata["item"] = OSD.FromUUID(ItemID);
            controldata["object"] = OSD.FromUUID(ObjectID);
            controldata["ignore"] = OSD.FromInteger(IgnoreControls);
            controldata["event"] = OSD.FromInteger(EventControls);

            return controldata;
        }


        public override void FromOSD(OSDMap args)
        {
            if (args["item"] != null)
                ItemID = args["item"].AsUUID();
            if (args["object"] != null)
                ObjectID = args["object"].AsUUID();
            if (args["ignore"] != null)
                IgnoreControls = (uint) args["ignore"].AsInteger();
            if (args["event"] != null)
                EventControls = (uint) args["event"].AsInteger();
        }
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class SittingObjectData : IDataTransferable
    {
        [ProtoMember(1)]
        public string m_animation = "";
        [ProtoMember(2)]
        public UUID m_objectID = UUID.Zero;
        [ProtoMember(3)]
        public Vector3 m_sitTargetPos = Vector3.Zero;
        [ProtoMember(4)]
        public Quaternion m_sitTargetRot = Quaternion.Identity;
        [ProtoMember(5)]
        public string m_sittingObjectXML = "";

        public SittingObjectData()
        {
        }

        public SittingObjectData(string sittingObjectXML, Vector3 sitTargetPos, Quaternion sitTargetRot,
                                 string animation)
        {
            m_sittingObjectXML = sittingObjectXML;
            m_sitTargetPos = sitTargetPos;
            m_sitTargetRot = sitTargetRot;
            m_animation = animation;
        }

        public SittingObjectData(OSDMap args)
        {
            FromOSD(args);
        }

        public override OSDMap ToOSD()
        {
            OSDMap controldata = new OSDMap();
            controldata["sittingObjectXML"] = m_sittingObjectXML;
            controldata["sitTargetPos"] = m_sitTargetPos;
            controldata["sitTargetRot"] = m_sitTargetRot;
            controldata["animation"] = m_animation;

            return controldata;
        }


        public override void FromOSD(OSDMap args)
        {
            if (args["sittingObjectXML"] != null)
                m_sittingObjectXML = args["sittingObjectXML"];
            if (args["sitTargetPos"] != null)
                m_sitTargetPos = args["sitTargetPos"];
            if (args["sitTargetRot"] != null)
                m_sitTargetRot = args["sitTargetRot"];
            if (args["animation"] != null)
                m_animation = args["animation"];
        }
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class AgentData : IDataTransferable, IAgentData
    {
        [ProtoMember(1)]
        public UUID ActiveGroupID;
        [ProtoMember(2)]
        public Byte AgentAccess;
        [ProtoMember(3)]
        public bool AlwaysRun;
        [ProtoMember(4)]
        public Animation[] Anims;
        [ProtoMember(5)]
        public AvatarAppearance Appearance;

        [ProtoMember(6)]
        public float Aspect;
        [ProtoMember(7)]
        public Vector3 AtAxis;
        [ProtoMember(8)]
        public Quaternion BodyRotation;
        [ProtoMember(9)]
        public Vector3 Center;
        [ProtoMember(10)]
        public uint CircuitCode;
        [ProtoMember(11)]
        public uint ControlFlags;
        [ProtoMember(12)]
        public ControllerData[] Controllers;
        [ProtoMember(13)]
        public float DrawDistance;
        [ProtoMember(14)]
        public float EnergyLevel;
        [ProtoMember(15)]
        public float Far;
        [ProtoMember(16)]
        public Byte GodLevel;
        [ProtoMember(17)]
        public UUID GranterID;
        [ProtoMember(18)]
        public Quaternion HeadRotation;
        [ProtoMember(19)]
        public bool IsCrossing;
        [ProtoMember(20)]
        public Vector3 LeftAxis;
        [ProtoMember(21)]
        public uint LocomotionState;
        [ProtoMember(22)]
        public Vector3 Position;
        [ProtoMember(23)]
        public UUID PreyAgent;
        [ProtoMember(24)]
        public UUID RegionID;
        [ProtoMember(25)]
        public bool SentInitialWearables;
        [ProtoMember(26)]
        public UUID SessionID;
        [ProtoMember(27)]
        public SittingObjectData SittingObjects;
        [ProtoMember(28)]
        public Vector3 Size;
        [ProtoMember(29)]
        public float Speed;
        [ProtoMember(30)]
        public byte[] Throttles;
        [ProtoMember(31)]
        public Vector3 UpAxis;
        [ProtoMember(32)]
        public Vector3 Velocity;
        [ProtoMember(33)]
        public UUID AgentID { get; set; }

        #region IAgentData Members

        // Scripted

        public override OSDMap ToOSD()
        {
            // DEBUG ON
            //MainConsole.Instance.WarnFormat("[CHILDAGENTDATAUPDATE] Pack data");
            // DEBUG OFF

            OSDMap args = new OSDMap();
            args["message_type"] = OSD.FromString("AgentData");

            args["region_id"] = OSD.FromString(RegionID.ToString());
            args["circuit_code"] = OSD.FromString(CircuitCode.ToString());
            args["agent_uuid"] = OSD.FromUUID(AgentID);
            args["session_uuid"] = OSD.FromUUID(SessionID);

            args["position"] = OSD.FromString(Position.ToString());
            args["velocity"] = OSD.FromString(Velocity.ToString());
            args["center"] = OSD.FromString(Center.ToString());
            args["size"] = OSD.FromString(Size.ToString());
            args["at_axis"] = OSD.FromString(AtAxis.ToString());
            args["left_axis"] = OSD.FromString(LeftAxis.ToString());
            args["up_axis"] = OSD.FromString(UpAxis.ToString());

            args["far"] = OSD.FromReal(Far);
            args["aspect"] = OSD.FromReal(Aspect);

            if ((Throttles != null) && (Throttles.Length > 0))
                args["throttles"] = OSD.FromBinary(Throttles);

            args["locomotion_state"] = OSD.FromString(LocomotionState.ToString());
            args["head_rotation"] = OSD.FromString(HeadRotation.ToString());
            args["body_rotation"] = OSD.FromString(BodyRotation.ToString());
            args["control_flags"] = OSD.FromString(ControlFlags.ToString());

            args["energy_level"] = OSD.FromReal(EnergyLevel);
            args["speed"] = OSD.FromString(Speed.ToString());
            args["god_level"] = OSD.FromString(GodLevel.ToString());
            args["draw_distance"] = OSD.FromReal(DrawDistance);
            args["always_run"] = OSD.FromBoolean(AlwaysRun);
            args["sent_initial_wearables"] = OSD.FromBoolean(SentInitialWearables);
            args["prey_agent"] = OSD.FromUUID(PreyAgent);
            args["agent_access"] = OSD.FromString(AgentAccess.ToString());

            args["active_group_id"] = OSD.FromUUID(ActiveGroupID);
            args["IsCrossing"] = IsCrossing;

            args["SittingObjects"] = SittingObjects.PackUpdateMessage();

            if ((Anims != null) && (Anims.Length > 0))
            {
                OSDArray anims = new OSDArray(Anims.Length);
                foreach (Animation aanim in Anims)
                    anims.Add(aanim.PackUpdateMessage());
                args["animations"] = anims;
            }

            if (Appearance != null)
                args["packed_appearance"] = Appearance.Pack();

            if ((Controllers != null) && (Controllers.Length > 0))
            {
                OSDArray controls = new OSDArray(Controllers.Length);
                foreach (ControllerData ctl in Controllers)
                    controls.Add(ctl.PackUpdateMessage());
                args["controllers"] = controls;
            }

            return args;
        }

        public override void FromOSD(OSDMap args)
        {
            if (args.ContainsKey("region_id"))
                UUID.TryParse(args["region_id"].AsString(), out RegionID);

            if (args["circuit_code"] != null)
                UInt32.TryParse(args["circuit_code"].AsString(), out CircuitCode);

            if (args["agent_uuid"] != null)
                AgentID = args["agent_uuid"].AsUUID();

            if (args["session_uuid"] != null)
                SessionID = args["session_uuid"].AsUUID();

            if (args["position"] != null)
                Vector3.TryParse(args["position"].AsString(), out Position);

            if (args["velocity"] != null)
                Vector3.TryParse(args["velocity"].AsString(), out Velocity);

            if (args["center"] != null)
                Vector3.TryParse(args["center"].AsString(), out Center);

            if (args["size"] != null)
                Vector3.TryParse(args["size"].AsString(), out Size);

            if (args["at_axis"] != null)
                Vector3.TryParse(args["at_axis"].AsString(), out AtAxis);

            if (args["left_axis"] != null)
                Vector3.TryParse(args["left_axis"].AsString(), out AtAxis);

            if (args["up_axis"] != null)
                Vector3.TryParse(args["up_axis"].AsString(), out AtAxis);

            if (args["far"] != null)
                Far = (float) (args["far"].AsReal());

            if (args["aspect"] != null)
                Aspect = (float) args["aspect"].AsReal();

            if (args["throttles"] != null)
                Throttles = args["throttles"].AsBinary();

            if (args["locomotion_state"] != null)
                UInt32.TryParse(args["locomotion_state"].AsString(), out LocomotionState);

            if (args["head_rotation"] != null)
                Quaternion.TryParse(args["head_rotation"].AsString(), out HeadRotation);

            if (args["body_rotation"] != null)
                Quaternion.TryParse(args["body_rotation"].AsString(), out BodyRotation);

            if (args["control_flags"] != null)
                UInt32.TryParse(args["control_flags"].AsString(), out ControlFlags);

            if (args["energy_level"] != null)
                EnergyLevel = (float) (args["energy_level"].AsReal());

            //This IS checked later
            if (args["god_level"] != null)
                Byte.TryParse(args["god_level"].AsString(), out GodLevel);

            if (args["speed"] != null)
                float.TryParse(args["speed"].AsString(), out Speed);
            else
                Speed = 1;

            if (args["draw_distance"] != null)
                float.TryParse(args["draw_distance"].AsString(), out DrawDistance);
            else
                DrawDistance = 0;

            //Reset this to fix movement... since regions are being bad about this
            if (Speed == 0)
                Speed = 1;

            if (args["always_run"] != null)
                AlwaysRun = args["always_run"].AsBoolean();

            SentInitialWearables = args["sent_initial_wearables"] != null && args["sent_initial_wearables"].AsBoolean();

            if (args["prey_agent"] != null)
                PreyAgent = args["prey_agent"].AsUUID();

            if (args["agent_access"] != null)
                Byte.TryParse(args["agent_access"].AsString(), out AgentAccess);

            if (args["active_group_id"] != null)
                ActiveGroupID = args["active_group_id"].AsUUID();

            if (args["IsCrossing"] != null)
                IsCrossing = args["IsCrossing"].AsBoolean();

            if ((args["animations"] != null) && (args["animations"]).Type == OSDType.Array)
            {
                OSDArray anims = (OSDArray) (args["animations"]);
                Anims = new Animation[anims.Count];
                int i = 0;

                foreach (OSD o in anims.Where(o => o.Type == OSDType.Map))
                {
                    Anims[i++] = new Animation((OSDMap) o);
                }
            }

            Appearance = new AvatarAppearance(AgentID);

            try
            {
                if (args.ContainsKey("packed_appearance") && (args["packed_appearance"]).Type == OSDType.Map)
                    Appearance = new AvatarAppearance(AgentID, (OSDMap) args["packed_appearance"]);
                    // DEBUG ON
                else
                    MainConsole.Instance.Warn("[CHILDAGENTDATAUPDATE] No packed appearance");
                // DEBUG OFF
            }
            catch
            {
            }

            if ((args["controllers"] != null) && (args["controllers"]).Type == OSDType.Array)
            {
                OSDArray controls = (OSDArray) (args["controllers"]);
                Controllers = new ControllerData[controls.Count];
                int i = 0;

                foreach (OSD o in controls.Where(o => o.Type == OSDType.Map))
                {
                    Controllers[i++] = new ControllerData((OSDMap) o);
                }
            }

            if (args["SittingObjects"] != null && args["SittingObjects"].Type == OSDType.Map)
                SittingObjects = new SittingObjectData((OSDMap) args["SittingObjects"]);
        }

        #endregion

        public void Dump()
        {
            MainConsole.Instance.Debug("------------ AgentData ------------");
            MainConsole.Instance.Debug("UUID: " + AgentID);
            MainConsole.Instance.Debug("Region: " + RegionID);
            MainConsole.Instance.Debug("Position: " + Position);
        }
    }
}