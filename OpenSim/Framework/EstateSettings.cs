/*
 * Copyright (c) Contributors, http://opensimulator.org/
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
using System.IO;
using OpenMetaverse;

namespace OpenSim.Framework
{
    public class EstateSettings
    {
        // private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public delegate void SaveDelegate(EstateSettings rs);

        public event SaveDelegate OnSave;

        // Only the client uses these
        //
        private uint m_EstateID = 0;

        public uint EstateID
        {
            get { return m_EstateID; }
            set { m_EstateID = value; }
        }

        private string m_EstateName = "My Estate";

        public string EstateName
        {
            get { return m_EstateName; }
            set { m_EstateName = value; }
        }

        private string m_EstatePass = "";

        public string EstatePass
        {
            get { return m_EstatePass; }
            set { m_EstatePass = value; }
        }

        private uint m_ParentEstateID = 1;

        public uint ParentEstateID
        {
            get { return m_ParentEstateID; }
            set { m_ParentEstateID = value; }
        }

        private float m_BillableFactor = 0.0f;

        public float BillableFactor
        {
            get { return m_BillableFactor; }
            set { m_BillableFactor = value; }
        }

        private int m_PricePerMeter = 1;

        public int PricePerMeter
        {
            get { return m_PricePerMeter; }
            set { m_PricePerMeter = value; }
        }

        private int m_RedirectGridX = 0;

        public int RedirectGridX
        {
            get { return m_RedirectGridX; }
            set { m_RedirectGridX = value; }
        }

        private int m_RedirectGridY = 0;

        public int RedirectGridY
        {
            get { return m_RedirectGridY; }
            set { m_RedirectGridY = value; }
        }

        // Used by the sim
        //
        private bool m_UseGlobalTime = true;

        public bool UseGlobalTime
        {
            get { return m_UseGlobalTime; }
            set { m_UseGlobalTime = value; }
        }

        private bool m_FixedSun = false;

        public bool FixedSun
        {
            get { return m_FixedSun; }
            set { m_FixedSun = value; }
        }

        private double m_SunPosition = 0.0;

        public double SunPosition
        {
            get { return m_SunPosition; }
            set { m_SunPosition = value; }
        }

        private bool m_AllowVoice = true;

        public bool AllowVoice
        {
            get { return m_AllowVoice; }
            set { m_AllowVoice = value; }
        }

        private bool m_AllowDirectTeleport = true;

        public bool AllowDirectTeleport
        {
            get { return m_AllowDirectTeleport; }
            set { m_AllowDirectTeleport = value; }
        }

        private bool m_DenyAnonymous = false;

        public bool DenyAnonymous
        {
            get { return m_DenyAnonymous; }
            set { m_DenyAnonymous = value; }
        }

        private bool m_DenyIdentified = false;

        public bool DenyIdentified
        {
            get { return m_DenyIdentified; }
            set { m_DenyIdentified = value; }
        }

        private bool m_DenyTransacted = false;

        public bool DenyTransacted
        {
            get { return m_DenyTransacted; }
            set { m_DenyTransacted = value; }
        }

        private bool m_AbuseEmailToEstateOwner = false;

        public bool AbuseEmailToEstateOwner
        {
            get { return m_AbuseEmailToEstateOwner; }
            set { m_AbuseEmailToEstateOwner = value; }
        }

        private bool m_BlockDwell = false;

        public bool BlockDwell
        {
            get { return m_BlockDwell; }
            set { m_BlockDwell = value; }
        }

        private bool m_EstateSkipScripts = false;

        public bool EstateSkipScripts
        {
            get { return m_EstateSkipScripts; }
            set { m_EstateSkipScripts = value; }
        }

        private bool m_ResetHomeOnTeleport = false;

        public bool ResetHomeOnTeleport
        {
            get { return m_ResetHomeOnTeleport; }
            set { m_ResetHomeOnTeleport = value; }
        }

        private bool m_TaxFree = false;

        public bool TaxFree
        {
            get { return m_TaxFree; }
            set { m_TaxFree = value; }
        }

        private bool m_PublicAccess = true;

        public bool PublicAccess
        {
            get { return m_PublicAccess; }
            set { m_PublicAccess = value; }
        }

        private string m_AbuseEmail = String.Empty;

        public string AbuseEmail
        {
            get { return m_AbuseEmail; }
            set { m_AbuseEmail= value; }
        }

        private UUID m_EstateOwner = UUID.Zero;

        public UUID EstateOwner
        {
            get { return m_EstateOwner; }
            set { m_EstateOwner = value; }
        }

        private bool m_DenyMinors = false;

        public bool DenyMinors
        {
            get { return m_DenyMinors; }
            set { m_DenyMinors = value; }
        }

        // All those lists...
        //
        private List<UUID> l_EstateManagers = new List<UUID>();

        public UUID[] EstateManagers
        {
            get { return l_EstateManagers.ToArray(); }
            set { l_EstateManagers = new List<UUID>(value); }
        }

        private List<EstateBan> l_EstateBans = new List<EstateBan>();

        public EstateBan[] EstateBans
        {
            get { return l_EstateBans.ToArray(); }
            set { l_EstateBans = new List<EstateBan>(value); }
        }

        private List<UUID> l_EstateAccess = new List<UUID>();

        public UUID[] EstateAccess
        {
            get { return l_EstateAccess.ToArray(); }
            set { l_EstateAccess = new List<UUID>(value); }
        }

        private List<UUID> l_EstateGroups = new List<UUID>();

        public UUID[] EstateGroups
        {
            get { return l_EstateGroups.ToArray(); }
            set { l_EstateGroups = new List<UUID>(value); }
        }

        public EstateSettings()
        {
        }

        public EstateSettings(Dictionary<string,object> values)
        {
            EstateID = uint.Parse(values["EstateID"].ToString());
            EstateName = values["EstateName"].ToString();
            AbuseEmailToEstateOwner = bool.Parse(values["AbuseEmailToEstateOwner"].ToString());
            DenyAnonymous = bool.Parse(values["DenyAnonymous"].ToString());
            ResetHomeOnTeleport = bool.Parse(values["ResetHomeOnTeleport"].ToString());
            FixedSun = bool.Parse(values["FixedSun"].ToString());
            DenyTransacted = bool.Parse(values["DenyTransacted"].ToString());
            BlockDwell = bool.Parse(values["BlockDwell"].ToString());
            DenyIdentified = bool.Parse(values["DenyIdentified"].ToString());
            AllowVoice = bool.Parse(values["AllowVoice"].ToString());
            UseGlobalTime = bool.Parse(values["UseGlobalTime"].ToString());
            PricePerMeter = int.Parse(values["PricePerMeter"].ToString());
            TaxFree = bool.Parse(values["TaxFree"].ToString());
            AllowDirectTeleport = bool.Parse(values["AllowDirectTeleport"].ToString());
            RedirectGridX = int.Parse(values["RedirectGridX"].ToString());
            RedirectGridY = int.Parse(values["RedirectGridY"].ToString());
            ParentEstateID = uint.Parse(values["ParentEstateID"].ToString());
            SunPosition = double.Parse(values["SunPosition"].ToString());
            EstateSkipScripts = bool.Parse(values["EstateSkipScripts"].ToString());
            BillableFactor = float.Parse(values["BillableFactor"].ToString());
            PublicAccess = bool.Parse(values["PublicAccess"].ToString());
            AbuseEmail = values["AbuseEmail"].ToString();
            EstateOwner = new UUID(values["EstateOwner"].ToString());
            DenyMinors = bool.Parse(values["DenyMinors"].ToString());
            EstatePass = values["EstatePass"].ToString();

            Dictionary<string, object> Managers = values["EstateManagers"] as Dictionary<string, object>;
            List<UUID> NewManagers = new List<UUID>();
            foreach (object UUID in Managers.Values)
            {
                NewManagers.Add(new UUID(UUID.ToString()));
            }
            EstateManagers = NewManagers.ToArray();

            Dictionary<string, object> Ban = values["EstateBans"] as Dictionary<string, object>;
            List<EstateBan> NewBan = new List<EstateBan>();
            foreach (object BannedUser in Ban.Values)
            {
                NewBan.Add(new EstateBan((Dictionary<string,object>)BannedUser));
            }
            EstateBans = NewBan.ToArray();

            Dictionary<string, object> Access = values["EstateAccess"] as Dictionary<string, object>;
            List<UUID> NewAccess = new List<UUID>();
            foreach (object UUID in Access.Values)
            {
                NewAccess.Add(new UUID(UUID.ToString()));
            }
            EstateAccess = NewAccess.ToArray();

            Dictionary<string, object> Groups = values["EstateGroups"] as Dictionary<string, object>;
            List<UUID> NewGroups = new List<UUID>();
            foreach (object UUID in Groups.Values)
            {
                NewGroups.Add(new UUID(UUID.ToString()));
            }
            EstateGroups = NewGroups.ToArray();
        }

        public Dictionary<string,object> ToKeyValuePairs()
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            values["EstateID"] = EstateID;
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
            values["ParentEstateID"] = ParentEstateID;
            values["SunPosition"] = SunPosition;
            values["EstateSkipScripts"] = EstateSkipScripts;
            values["BillableFactor"] = BillableFactor;
            values["PublicAccess"] = PublicAccess;
            values["AbuseEmail"] = AbuseEmail;
            values["EstateOwner"] = EstateOwner;
            values["DenyMinors"] = DenyMinors;
            values["EstatePass"] = EstatePass;
            Dictionary<string, object> Ban = new Dictionary<string, object>();
            int i = 0;
            foreach (EstateBan ban in EstateBans)
            {
                Ban.Add(i.ToString(), ban.ToKeyValuePairs());
                i++;
            }
            values["EstateBans"] = Ban;
            i *= 0;

            Dictionary<string, object> Managers = new Dictionary<string, object>();
            i = 0;
            foreach (UUID ID in EstateManagers)
            {
                Managers.Add(i.ToString(), ID);
                i++;
            }
            values["EstateManagers"] = Managers;
            i *= 0;

            Dictionary<string, object> Groups = new Dictionary<string, object>();
            i = 0;
            foreach (UUID ID in EstateGroups)
            {
                Groups.Add(i.ToString(), ID);
                i++;
            }
            values["EstateGroups"] = Groups;
            i *= 0;

            Dictionary<string, object> Access = new Dictionary<string, object>();
            i = 0;
            foreach (UUID ID in EstateAccess)
            {
                Access.Add(i.ToString(), ID);
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
            foreach (EstateBan ban in l_EstateBans)
                if (ban.BannedUserID == avatarID)
                    return true;
            return false;
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
            foreach (EstateBan ban in new List<EstateBan>(l_EstateBans))
                if (ban.BannedUserID == avatarID)
                    l_EstateBans.Remove(ban);
        }

        public bool HasAccess(UUID user)
        {
            if (IsEstateManager(user))
                return true;

            return l_EstateAccess.Contains(user);
        }
    }
}
