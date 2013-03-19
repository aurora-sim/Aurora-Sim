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
using System.Collections.Generic;
using System.Linq;
using Aurora.Framework.Utilities;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using ProtoBuf;

namespace Aurora.Framework.SceneInfo
{
    [Serializable, ProtoContract(UseProtoMembersOnly = false)]
    public class EstateSettings : IDataTransferable
    {
        // private static readonly ILog MainConsole.Instance = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Delegates

        public delegate void SaveDelegate(EstateSettings rs);

        #endregion

        private List<UUID> l_EstateAccess = new List<UUID>();
        private List<EstateBan> l_EstateBans = new List<EstateBan>();
        private List<UUID> l_EstateGroups = new List<UUID>();
        private List<UUID> l_EstateManagers = new List<UUID>();
        private string m_AbuseEmail = String.Empty;
        private bool m_AllowDirectTeleport = true;
        private bool m_AllowLandmark = true;
        private bool m_AllowParcelChanges = true;
        private bool m_AllowSetHome = true;
        private bool m_AllowVoice = true;

        private string m_EstateName = "My Estate";
        private UUID m_EstateOwner = UUID.Zero;

        private uint m_ParentEstateID = 1;
        private int m_PricePerMeter = 1;
        private bool m_PublicAccess = true;
        private bool m_UseGlobalTime = true;

        public EstateSettings()
        {
        }

        [ProtoMember(1)]
        public uint EstateID { get; set; }

        [ProtoMember(2)]
        public string EstateName
        {
            get { return m_EstateName; }
            set { m_EstateName = value; }
        }

        [ProtoMember(3)]
        public bool AllowLandmark
        {
            get { return m_AllowLandmark; }
            set { m_AllowLandmark = value; }
        }

        [ProtoMember(4)]
        public bool AllowParcelChanges
        {
            get { return m_AllowParcelChanges; }
            set { m_AllowParcelChanges = value; }
        }

        [ProtoMember(5)]
        public bool AllowSetHome
        {
            get { return m_AllowSetHome; }
            set { m_AllowSetHome = value; }
        }

        [ProtoMember(6)]
        public uint ParentEstateID
        {
            get { return m_ParentEstateID; }
            set { m_ParentEstateID = value; }
        }

        [ProtoMember(7)]
        public float BillableFactor { get; set; }

        [ProtoMember(8)]
        public int PricePerMeter
        {
            get { return m_PricePerMeter; }
            set { m_PricePerMeter = value; }
        }

        // Used by the sim
        //

        [ProtoMember(9)]
        public bool UseGlobalTime
        {
            get { return m_UseGlobalTime; }
            set { m_UseGlobalTime = value; }
        }

        [ProtoMember(10)]
        public bool FixedSun { get; set; }

        [ProtoMember(11)]
        public double SunPosition { get; set; }

        [ProtoMember(12)]
        public bool AllowVoice
        {
            get { return m_AllowVoice; }
            set { m_AllowVoice = value; }
        }

        [ProtoMember(13)]
        public bool AllowDirectTeleport
        {
            get { return m_AllowDirectTeleport; }
            set { m_AllowDirectTeleport = value; }
        }

        [ProtoMember(14)]
        public bool DenyAnonymous { get; set; }

        [ProtoMember(15)]
        public bool DenyIdentified { get; set; }

        [ProtoMember(16)]
        public bool DenyTransacted { get; set; }

        [ProtoMember(17)]
        public bool AbuseEmailToEstateOwner { get; set; }

        [ProtoMember(18)]
        public bool BlockDwell { get; set; }

        [ProtoMember(19)]
        public bool EstateSkipScripts { get; set; }

        [ProtoMember(20)]
        public bool ResetHomeOnTeleport { get; set; }

        [ProtoMember(21)]
        public bool TaxFree { get; set; }

        [ProtoMember(22)]
        public bool PublicAccess
        {
            get { return m_PublicAccess; }
            set { m_PublicAccess = value; }
        }

        [ProtoMember(23)]
        public string AbuseEmail
        {
            get { return m_AbuseEmail; }
            set { m_AbuseEmail = value; }
        }

        [ProtoMember(24)]
        public UUID EstateOwner
        {
            get { return m_EstateOwner; }
            set { m_EstateOwner = value; }
        }

        [ProtoMember(25)]
        public bool DenyMinors { get; set; }

        // All those lists...
        //

        [ProtoMember(26)]
        public List<UUID> EstateManagers
        {
            get { return l_EstateManagers; }
            set { l_EstateManagers = value; }
        }

        [ProtoMember(27)]
        public List<EstateBan> EstateBans
        {
            get { return l_EstateBans; }
            set { l_EstateBans = value; }
        }

        [ProtoMember(28)]
        public List<UUID> EstateAccess
        {
            get { return l_EstateAccess; }
            set { l_EstateAccess = value; }
        }

        [ProtoMember(29)]
        public List<UUID> EstateGroups
        {
            get { return l_EstateGroups; }
            set { l_EstateGroups = value; }
        }

        public event SaveDelegate OnSave;

        public override void FromOSD(OSDMap v)
        {
            OSDMap values = (OSDMap) v;
            EstateID = (uint) values["EstateID"].AsInteger();
            EstateName = values["EstateName"].AsString();
            AbuseEmailToEstateOwner = values["AbuseEmailToEstateOwner"].AsBoolean();
            DenyAnonymous = values["DenyAnonymous"].AsBoolean();
            ResetHomeOnTeleport = values["ResetHomeOnTeleport"].AsBoolean();
            FixedSun = values["FixedSun"].AsBoolean();
            DenyTransacted = values["DenyTransacted"].AsBoolean();
            BlockDwell = values["BlockDwell"].AsBoolean();
            DenyIdentified = values["DenyIdentified"].AsBoolean();
            AllowVoice = values["AllowVoice"].AsBoolean();
            UseGlobalTime = values["UseGlobalTime"].AsBoolean();
            PricePerMeter = values["PricePerMeter"].AsInteger();
            TaxFree = values["TaxFree"].AsBoolean();
            AllowDirectTeleport = values["AllowDirectTeleport"].AsBoolean();
            ParentEstateID = (uint) values["ParentEstateID"].AsInteger();
            SunPosition = values["SunPosition"].AsReal();
            EstateSkipScripts = values["EstateSkipScripts"].AsBoolean();
            BillableFactor = (float) values["BillableFactor"].AsReal();
            PublicAccess = values["PublicAccess"].AsBoolean();
            AbuseEmail = values["AbuseEmail"].AsString();
            EstateOwner = values["EstateOwner"].AsUUID();
            AllowLandmark = values["AllowLandmark"].AsBoolean();
            AllowParcelChanges = values["AllowParcelChanges"].AsBoolean();
            AllowSetHome = values["AllowSetHome"].AsBoolean();
            DenyMinors = values["DenyMinors"].AsBoolean();

            OSDArray Managers = values["EstateManagers"] as OSDArray;
            if (Managers != null)
                EstateManagers = Managers.ConvertAll<UUID>((o) => o);

            OSDArray Ban = values["EstateBans"] as OSDArray;
            if (Ban != null)
                EstateBans = Ban.ConvertAll<EstateBan>((o) =>
                                                           {
                                                               EstateBan ban = new EstateBan();
                                                               ban.FromOSD(o);
                                                               return ban;
                                                           });

            OSDArray Access = values["EstateAccess"] as OSDArray;
            if (Access != null)
                EstateAccess = Access.ConvertAll<UUID>((o) => o);

            OSDArray Groups = values["EstateGroups"] as OSDArray;
            if (Groups != null)
                EstateGroups = Groups.ConvertAll<UUID>((o) => o);
        }

        public override OSDMap ToOSD()
        {
            OSDMap values = new OSDMap();
            values["EstateID"] = (int) EstateID;
            values["EstateName"] = EstateName;
            values["AbuseEmailToEstateOwner"] = AbuseEmailToEstateOwner;
            values["DenyAnonymous"] = DenyAnonymous;
            values["ResetHomeOnTeleport"] = ResetHomeOnTeleport;
            values["FixedSun"] = FixedSun;
            values["DenyTransacted"] = DenyTransacted;
            values["BlockDwell"] = BlockDwell;
            values["DenyIdentified"] = DenyIdentified;
            values["AllowVoice"] = AllowVoice;
            values["UseGlobalTime"] = UseGlobalTime;
            values["PricePerMeter"] = PricePerMeter;
            values["TaxFree"] = TaxFree;
            values["AllowDirectTeleport"] = AllowDirectTeleport;
            values["ParentEstateID"] = (int) ParentEstateID;
            values["SunPosition"] = SunPosition;
            values["EstateSkipScripts"] = EstateSkipScripts;
            values["BillableFactor"] = BillableFactor;
            values["PublicAccess"] = PublicAccess;
            values["AbuseEmail"] = AbuseEmail;
            values["EstateOwner"] = EstateOwner;
            values["DenyMinors"] = DenyMinors;
            values["AllowLandmark"] = AllowLandmark;
            values["AllowParcelChanges"] = AllowParcelChanges;
            values["AllowSetHome"] = AllowSetHome;

            OSDArray Ban = new OSDArray(EstateBans.Count);
            foreach (EstateBan ban in EstateBans)
            {
                Ban.Add(ban.ToOSD());
            }
            values["EstateBans"] = Ban;

            values["EstateManagers"] = EstateManagers.ToOSDArray();
            values["EstateGroups"] = EstateGroups.ToOSDArray();
            values["EstateAccess"] = EstateAccess.ToOSDArray();

            return values;
        }

        public void Save()
        {
            if (OnSave != null)
                OnSave(this);
        }

        public void AddEstateUser(UUID avatarID)
        {
            if (avatarID == UUID.Zero)
                return;
            if (!l_EstateAccess.Contains(avatarID))
                l_EstateAccess.Add(avatarID);
        }

        public void RemoveEstateUser(UUID avatarID)
        {
            if (l_EstateAccess.Contains(avatarID))
                l_EstateAccess.Remove(avatarID);
        }

        public void AddEstateGroup(UUID avatarID)
        {
            if (avatarID == UUID.Zero)
                return;
            if (!l_EstateGroups.Contains(avatarID))
                l_EstateGroups.Add(avatarID);
        }

        public void RemoveEstateGroup(UUID avatarID)
        {
            if (l_EstateGroups.Contains(avatarID))
                l_EstateGroups.Remove(avatarID);
        }

        public void AddEstateManager(UUID avatarID)
        {
            if (avatarID == UUID.Zero)
                return;
            if (!l_EstateManagers.Contains(avatarID))
                l_EstateManagers.Add(avatarID);
        }

        public void RemoveEstateManager(UUID avatarID)
        {
            if (l_EstateManagers.Contains(avatarID))
                l_EstateManagers.Remove(avatarID);
        }

        public bool IsEstateManager(UUID avatarID)
        {
            if (IsEstateOwner(avatarID))
                return true;

            return l_EstateManagers.Contains(avatarID);
        }

        public bool IsEstateOwner(UUID avatarID)
        {
            if (avatarID == m_EstateOwner)
                return true;

            return false;
        }

        public bool IsBanned(UUID avatarID)
        {
#if (!ISWIN)
            foreach (EstateBan ban in l_EstateBans)
            {
                if (ban.BannedUserID == avatarID) return true;
            }
            return false;
#else
            return l_EstateBans.Any(ban => ban.BannedUserID == avatarID);
#endif
        }

        public void AddBan(EstateBan ban)
        {
            if (ban == null)
                return;
            if (!IsBanned(ban.BannedUserID))
                l_EstateBans.Add(ban);
        }

        public void ClearBans()
        {
            l_EstateBans.Clear();
        }

        public void RemoveBan(UUID avatarID)
        {
#if (!ISWIN)
            foreach (EstateBan ban in new List<EstateBan>(l_EstateBans))
            {
                if (ban.BannedUserID == avatarID) l_EstateBans.Remove(ban);
            }
#else
            foreach (EstateBan ban in new List<EstateBan>(l_EstateBans).Where(ban => ban.BannedUserID == avatarID))
                l_EstateBans.Remove(ban);
#endif
        }

        public bool HasAccess(UUID user)
        {
            if (IsEstateManager(user) || EstateOwner == user)
                return true;

            return l_EstateAccess.Contains(user);
        }

        public void SetFromFlags(ulong regionFlags)
        {
            ResetHomeOnTeleport = ((regionFlags & (ulong) OpenMetaverse.RegionFlags.ResetHomeOnTeleport) ==
                                   (ulong) OpenMetaverse.RegionFlags.ResetHomeOnTeleport);
            BlockDwell = ((regionFlags & (ulong) OpenMetaverse.RegionFlags.BlockDwell) ==
                          (ulong) OpenMetaverse.RegionFlags.BlockDwell);
            AllowLandmark = ((regionFlags & (ulong) OpenMetaverse.RegionFlags.AllowLandmark) ==
                             (ulong) OpenMetaverse.RegionFlags.AllowLandmark);
            AllowParcelChanges = ((regionFlags & (ulong) OpenMetaverse.RegionFlags.AllowParcelChanges) ==
                                  (ulong) OpenMetaverse.RegionFlags.AllowParcelChanges);
            AllowSetHome = ((regionFlags & (ulong) OpenMetaverse.RegionFlags.AllowSetHome) ==
                            (ulong) OpenMetaverse.RegionFlags.AllowSetHome);
        }
    }
}