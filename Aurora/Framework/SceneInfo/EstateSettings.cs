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
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{
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

        private string m_EstatePass = "";
        private uint m_ParentEstateID = 1;
        private int m_PricePerMeter = 1;
        private bool m_PublicAccess = true;
        private bool m_UseGlobalTime = true;

        public EstateSettings()
        {
        }

        public EstateSettings(Dictionary<string, object> values)
        {
            FromKVP(values);
        }

        public uint EstateID { get; set; }

        public string EstateName
        {
            get { return m_EstateName; }
            set { m_EstateName = value; }
        }

        public string EstatePass
        {
            get { return m_EstatePass; }
            set { m_EstatePass = value; }
        }

        public bool AllowLandmark
        {
            get { return m_AllowLandmark; }
            set { m_AllowLandmark = value; }
        }

        public bool AllowParcelChanges
        {
            get { return m_AllowParcelChanges; }
            set { m_AllowParcelChanges = value; }
        }

        public bool AllowSetHome
        {
            get { return m_AllowSetHome; }
            set { m_AllowSetHome = value; }
        }


        public uint ParentEstateID
        {
            get { return m_ParentEstateID; }
            set { m_ParentEstateID = value; }
        }

        public float BillableFactor { get; set; }

        public int PricePerMeter
        {
            get { return m_PricePerMeter; }
            set { m_PricePerMeter = value; }
        }

        public int RedirectGridX { get; set; }

        public int RedirectGridY { get; set; }

        // Used by the sim
        //

        public bool UseGlobalTime
        {
            get { return m_UseGlobalTime; }
            set { m_UseGlobalTime = value; }
        }

        public bool FixedSun { get; set; }

        public double SunPosition { get; set; }

        public bool AllowVoice
        {
            get { return m_AllowVoice; }
            set { m_AllowVoice = value; }
        }

        public bool AllowDirectTeleport
        {
            get { return m_AllowDirectTeleport; }
            set { m_AllowDirectTeleport = value; }
        }

        public bool DenyAnonymous { get; set; }

        public bool DenyIdentified { get; set; }

        public bool DenyTransacted { get; set; }

        public bool AbuseEmailToEstateOwner { get; set; }

        public bool BlockDwell { get; set; }

        public bool EstateSkipScripts { get; set; }

        public bool ResetHomeOnTeleport { get; set; }

        public bool TaxFree { get; set; }

        public bool PublicAccess
        {
            get { return m_PublicAccess; }
            set { m_PublicAccess = value; }
        }

        public string AbuseEmail
        {
            get { return m_AbuseEmail; }
            set { m_AbuseEmail = value; }
        }

        public UUID EstateOwner
        {
            get { return m_EstateOwner; }
            set { m_EstateOwner = value; }
        }

        public bool DenyMinors { get; set; }

        // All those lists...
        //

        public UUID[] EstateManagers
        {
            get { return l_EstateManagers.ToArray(); }
            set { l_EstateManagers = new List<UUID>(value); }
        }

        public EstateBan[] EstateBans
        {
            get { return l_EstateBans.ToArray(); }
            set { l_EstateBans = new List<EstateBan>(value); }
        }

        public UUID[] EstateAccess
        {
            get { return l_EstateAccess.ToArray(); }
            set { l_EstateAccess = new List<UUID>(value); }
        }

        public UUID[] EstateGroups
        {
            get { return l_EstateGroups.ToArray(); }
            set { l_EstateGroups = new List<UUID>(value); }
        }

        public event SaveDelegate OnSave;

        public override void FromOSD(OSDMap v)
        {
            OSDMap values = (OSDMap) v;
            EstateID = (uint) values["EstateID"].AsInteger();
            EstateName = values["EstateName"].AsString();
            AbuseEmailToEstateOwner = values["AbuseEmailToEstateOwner"].AsInteger() == 1;
            DenyAnonymous = values["DenyAnonymous"].AsInteger() == 1;
            ResetHomeOnTeleport = values["ResetHomeOnTeleport"].AsInteger() == 1;
            FixedSun = values["FixedSun"].AsInteger() == 1;
            DenyTransacted = values["DenyTransacted"].AsInteger() == 1;
            BlockDwell = values["BlockDwell"].AsInteger() == 1;
            DenyIdentified = values["DenyIdentified"].AsInteger() == 1;
            AllowVoice = values["AllowVoice"].AsInteger() == 1;
            UseGlobalTime = values["UseGlobalTime"].AsInteger() == 1;
            PricePerMeter = values["PricePerMeter"].AsInteger();
            TaxFree = values["TaxFree"].AsInteger() == 1;
            AllowDirectTeleport = values["AllowDirectTeleport"].AsInteger() == 1;
            RedirectGridX = values["RedirectGridX"].AsInteger();
            RedirectGridY = values["RedirectGridY"].AsInteger();
            ParentEstateID = (uint) values["ParentEstateID"].AsInteger();
            SunPosition = values["SunPosition"].AsReal();
            EstateSkipScripts = values["EstateSkipScripts"].AsInteger() == 1;
            BillableFactor = (float) values["BillableFactor"].AsReal();
            PublicAccess = values["PublicAccess"].AsInteger() == 1;
            AbuseEmail = values["AbuseEmail"].AsString();
            EstateOwner = values["EstateOwner"].AsUUID();
            AllowLandmark = values["AllowLandmark"].AsInteger() == 1;
            AllowParcelChanges = values["AllowParcelChanges"].AsInteger() == 1;
            AllowSetHome = values["AllowSetHome"].AsInteger() == 1;
            DenyMinors = values["DenyMinors"].AsInteger() == 1;
            //We always try to pull this in if it exists
            EstatePass = values["EstatePass"].AsString();

            OSDMap Managers = values["EstateManagers"] as OSDMap;
#if (!ISWIN)
            List<UUID> list = new List<UUID>();
            foreach (OSD id in Managers.Values)
                list.Add(id.AsUUID());
            EstateManagers = list.ToArray();
#else
            EstateManagers = Managers.Values.Select(ID => ID.AsUUID()).ToArray();
#endif

            OSDMap Ban = values["EstateBans"] as OSDMap;
            List<EstateBan> NewBan = new List<EstateBan>();
            foreach (OSD BannedUser in Ban.Values)
            {
                EstateBan ban = new EstateBan();
                ban.FromOSD(BannedUser);
                NewBan.Add(ban);
            }
            EstateBans = NewBan.ToArray();

            OSDMap Access = values["EstateAccess"] as OSDMap;
#if (!ISWIN)
            List<UUID> list1 = new List<UUID>();
            foreach (OSD uuid in Access.Values)
                list1.Add(uuid.AsUUID());
            EstateAccess = list1.ToArray();
#else
            EstateAccess = Access.Values.Select(UUID => UUID.AsUUID()).ToArray();
#endif

            OSDMap Groups = values["EstateGroups"] as OSDMap;
#if (!ISWIN)
            List<UUID> list2 = new List<UUID>();
            foreach (OSD uuid in Groups.Values)
                list2.Add(uuid.AsUUID());
            EstateGroups = list2.ToArray();
#else
            EstateGroups = Groups.Values.Select(UUID => UUID.AsUUID()).ToArray();
#endif
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
            values["RedirectGridX"] = RedirectGridX;
            values["RedirectGridY"] = RedirectGridY;
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
            values["EstatePass"] = EstatePass; //For security, this is not sent unless it is for local

            OSDMap Ban = new OSDMap();
            int i = 0;
            foreach (EstateBan ban in EstateBans)
            {
                Ban[Util.ConvertDecString(i)] = ban.ToOSD();
                i++;
            }
            values["EstateBans"] = Ban;
            i *= 0;

            OSDMap Managers = new OSDMap();
            foreach (UUID ID in EstateManagers)
            {
                Managers[Util.ConvertDecString(i)] = ID;
                i++;
            }
            values["EstateManagers"] = Managers;
            i *= 0;

            OSDMap Groups = new OSDMap();
            foreach (UUID ID in EstateGroups)
            {
                Groups[Util.ConvertDecString(i)] = ID;
                i++;
            }
            values["EstateGroups"] = Groups;
            i *= 0;

            OSDMap Access = new OSDMap();
            foreach (UUID ID in EstateAccess)
            {
                Access[Util.ConvertDecString(i)] = ID;
                i++;
            }
            values["EstateAccess"] = Access;
            i *= 0;
            return values;
        }

        public override void FromKVP(Dictionary<string, object> values)
        {
            EstateID = (uint)int.Parse(values["EstateID"].ToString());
            EstateName = values["EstateName"].ToString();
            AbuseEmailToEstateOwner = int.Parse(values["AbuseEmailToEstateOwner"].ToString()) == 1;
            DenyAnonymous = int.Parse(values["DenyAnonymous"].ToString()) == 1;
            ResetHomeOnTeleport = int.Parse(values["ResetHomeOnTeleport"].ToString()) == 1;
            FixedSun = int.Parse(values["FixedSun"].ToString()) == 1;
            DenyTransacted = int.Parse(values["DenyTransacted"].ToString()) == 1;
            BlockDwell = int.Parse(values["BlockDwell"].ToString()) == 1;
            DenyIdentified = int.Parse(values["DenyIdentified"].ToString()) == 1;
            AllowVoice = int.Parse(values["AllowVoice"].ToString()) == 1;
            UseGlobalTime = int.Parse(values["UseGlobalTime"].ToString()) == 1;
            PricePerMeter = int.Parse(values["PricePerMeter"].ToString());
            TaxFree = int.Parse(values["TaxFree"].ToString()) == 1;
            AllowDirectTeleport = int.Parse(values["AllowDirectTeleport"].ToString()) == 1;
            RedirectGridX = int.Parse(values["RedirectGridX"].ToString());
            RedirectGridY = int.Parse(values["RedirectGridY"].ToString());
            ParentEstateID = (uint)int.Parse(values["ParentEstateID"].ToString());
            SunPosition = double.Parse(values["SunPosition"].ToString());
            EstateSkipScripts = int.Parse(values["EstateSkipScripts"].ToString()) == 1;
            BillableFactor = float.Parse(values["BillableFactor"].ToString());
            PublicAccess = int.Parse(values["PublicAccess"].ToString()) == 1;
            AbuseEmail = values["AbuseEmail"].ToString();
            EstateOwner = new UUID(values["EstateOwner"].ToString());
            AllowLandmark = int.Parse(values["AllowLandmark"].ToString()) == 1;
            AllowParcelChanges = int.Parse(values["AllowParcelChanges"].ToString()) == 1;
            AllowSetHome = int.Parse(values["AllowSetHome"].ToString()) == 1;
            DenyMinors = int.Parse(values["DenyMinors"].ToString()) == 1;
            //We always try to pull this in if it exists
            if (values.ContainsKey("EstatePass"))
                EstatePass = values["EstatePass"].ToString();

            Dictionary<string, object> Managers = values["EstateManagers"] as Dictionary<string, object>;
#if (!ISWIN)
            List<UUID> list = new List<UUID>();
            foreach (object uuid in Managers.Values)
                list.Add(new UUID(uuid.ToString()));
            EstateManagers = list.ToArray();
#else
            EstateManagers = Managers.Values.Select(UUID => new UUID(UUID.ToString())).ToArray();
#endif

            Dictionary<string, object> Ban = values["EstateBans"] as Dictionary<string, object>;
#if (!ISWIN)
            List<EstateBan> list1 = new List<EstateBan>();
            foreach (object bannedUser in Ban.Values)
                list1.Add(new EstateBan((Dictionary<string, object>)bannedUser));
            EstateBans = list1.ToArray();
#else
            EstateBans = Ban.Values.Select(BannedUser => new EstateBan((Dictionary<string, object>) BannedUser)).ToArray();
#endif

            Dictionary<string, object> Access = values["EstateAccess"] as Dictionary<string, object>;
#if (!ISWIN)
            List<UUID> list2 = new List<UUID>();
            foreach (object uuid in Access.Values)
                list2.Add(new UUID(uuid.ToString()));
            EstateAccess = list2.ToArray();
#else
            EstateAccess = Access.Values.Select(UUID => new UUID(UUID.ToString())).ToArray();
#endif

            Dictionary<string, object> Groups = values["EstateGroups"] as Dictionary<string, object>;
#if (!ISWIN)
            List<UUID> list3 = new List<UUID>();
            foreach (object uuid in Groups.Values)
                list3.Add(new UUID(uuid.ToString()));
            EstateGroups = list3.ToArray();
#else
            EstateGroups = Groups.Values.Select(UUID => new UUID(UUID.ToString())).ToArray();
#endif
        }

        public override Dictionary<string, object> ToKVP()
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            values["EstateID"] = (int) EstateID;
            values["EstateName"] = EstateName;
            values["AbuseEmailToEstateOwner"] = (AbuseEmailToEstateOwner ? 1 : 0);
            values["DenyAnonymous"] = DenyAnonymous ? 1 : 0;
            values["ResetHomeOnTeleport"] = ResetHomeOnTeleport ? 1 : 0;
            values["FixedSun"] = FixedSun ? 1 : 0;
            values["DenyTransacted"] = DenyTransacted ? 1 : 0;
            values["BlockDwell"] = BlockDwell ? 1 : 0;
            values["DenyIdentified"] = DenyIdentified ? 1 : 0;
            values["AllowVoice"] = AllowVoice ? 1 : 0;
            values["UseGlobalTime"] = UseGlobalTime ? 1 : 0;
            values["PricePerMeter"] = PricePerMeter;
            values["TaxFree"] = TaxFree ? 1 : 0;
            values["AllowDirectTeleport"] = AllowDirectTeleport ? 1 : 0;
            values["RedirectGridX"] = RedirectGridX;
            values["RedirectGridY"] = RedirectGridY;
            values["ParentEstateID"] = (int) ParentEstateID;
            values["SunPosition"] = SunPosition;
            values["EstateSkipScripts"] = EstateSkipScripts ? 1 : 0;
            values["BillableFactor"] = BillableFactor;
            values["PublicAccess"] = PublicAccess ? 1 : 0;
            values["AbuseEmail"] = AbuseEmail;
            values["EstateOwner"] = EstateOwner;
            values["DenyMinors"] = DenyMinors ? 1 : 0;
            values["AllowLandmark"] = AllowLandmark ? 1 : 0;
            values["AllowParcelChanges"] = AllowParcelChanges ? 1 : 0;
            values["AllowSetHome"] = AllowSetHome ? 1 : 0;
            values["EstatePass"] = EstatePass; //For security, this is not sent unless it is for local

            Dictionary<string, object> Ban = new Dictionary<string, object>();
            int i = 0;
            foreach (EstateBan ban in EstateBans)
            {
                Ban[Util.ConvertDecString(i)] = ban.ToKeyValuePairs();
                i++;
            }
            values["EstateBans"] = Ban;
            i *= 0;

            Dictionary<string, object> Managers = new Dictionary<string, object>();
            foreach (UUID ID in EstateManagers)
            {
                Managers[Util.ConvertDecString(i)] = ID;
                i++;
            }
            values["EstateManagers"] = Managers;
            i *= 0;

            Dictionary<string, object> Groups = new Dictionary<string, object>();
            foreach (UUID ID in EstateGroups)
            {
                Groups[Util.ConvertDecString(i)] = ID;
                i++;
            }
            values["EstateGroups"] = Groups;
            i *= 0;

            Dictionary<string, object> Access = new Dictionary<string, object>();
            foreach (UUID ID in EstateAccess)
            {
                Access[Util.ConvertDecString(i)] = ID;
                i++;
            }
            values["EstateAccess"] = Access;
            i *= 0;
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
                                   (ulong)OpenMetaverse.RegionFlags.ResetHomeOnTeleport);
            BlockDwell = ((regionFlags & (ulong)OpenMetaverse.RegionFlags.BlockDwell) == (ulong)OpenMetaverse.RegionFlags.BlockDwell);
            AllowLandmark = ((regionFlags & (ulong)OpenMetaverse.RegionFlags.AllowLandmark) == (ulong)OpenMetaverse.RegionFlags.AllowLandmark);
            AllowParcelChanges = ((regionFlags & (ulong)OpenMetaverse.RegionFlags.AllowParcelChanges) ==
                                  (ulong)OpenMetaverse.RegionFlags.AllowParcelChanges);
            AllowSetHome = ((regionFlags & (ulong)OpenMetaverse.RegionFlags.AllowSetHome) == (ulong)OpenMetaverse.RegionFlags.AllowSetHome);
        }
    }
}