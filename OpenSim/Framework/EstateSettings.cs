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

        private bool m_AllowLandmark = true;

        public bool AllowLandmark
        {
            get { return m_AllowLandmark; }
            set { m_AllowLandmark = value; }
        }

        private bool m_AllowParcelChanges = true;

        public bool AllowParcelChanges
        {
            get { return m_AllowParcelChanges; }
            set { m_AllowParcelChanges = value; }
        }

        private bool m_AllowSetHome = true;

        public bool AllowSetHome
        {
            get { return m_AllowSetHome; }
            set { m_AllowSetHome = value; }
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

        public Dictionary<string,object> ToKeyValuePairs(bool Local)
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            values["EstateID"] = (int)EstateID;
            values["EstateName"] = EstateName;
            values["AbuseEmailToEstateOwner"] = (int)(AbuseEmailToEstateOwner ? 1 : 0);
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
            values["ParentEstateID"] = (int)ParentEstateID;
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
            if(Local)
                values["EstatePass"] = EstatePass; //For security, this is not sent unless it is for local

            Dictionary<string, object> Ban = new Dictionary<string, object>();
            int i = 0;
            foreach (EstateBan ban in EstateBans)
            {
                Ban[ConvertDecString(i)] = ban.ToKeyValuePairs();
                i++;
            }
            values["EstateBans"] = Ban;
            i *= 0;

            Dictionary<string, object> Managers = new Dictionary<string, object>();
            foreach (UUID ID in EstateManagers)
            {
                Managers[ConvertDecString(i)] = ID;
                i++;
            }
            values["EstateManagers"] = Managers;
            i *= 0;

            Dictionary<string, object> Groups = new Dictionary<string, object>();
            foreach (UUID ID in EstateGroups)
            {
                Groups[ConvertDecString(i)] = ID;
                i++;
            }
            values["EstateGroups"] = Groups;
            i *= 0;

            Dictionary<string, object> Access = new Dictionary<string, object>();
            foreach (UUID ID in EstateAccess)
            {
                Access[ConvertDecString(i)] = ID;
                i++;
            }
            values["EstateAccess"] = Access;
            i *= 0;
            return values;
        }

        // http://social.msdn.microsoft.com/forums/en-US/csharpgeneral/thread/68f7ca38-5cd1-411f-b8d4-e4f7a688bc03
        // By: A Million Lemmings
        public string ConvertDecString(int dvalue)
        {

            string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            string retVal = string.Empty;

            double value = Convert.ToDouble(dvalue);

            do
            {

                double remainder = value - (26 * Math.Truncate(value / 26));

                retVal = retVal + CHARS.Substring((int)remainder, 1);

                value = Math.Truncate(value / 26);

            }
            while (value > 0);



            return retVal;

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
            if (IsEstateManager(user) || EstateOwner == user)
                return true;

            return l_EstateAccess.Contains(user);
        }

        public void SetFromFlags(ulong regionFlags)
        {
            ResetHomeOnTeleport = ((regionFlags & (ulong)RegionFlags.ResetHomeOnTeleport) == (ulong)RegionFlags.ResetHomeOnTeleport);
            BlockDwell = ((regionFlags & (ulong)RegionFlags.BlockDwell) == (ulong)RegionFlags.BlockDwell);
            AllowLandmark = ((regionFlags & (ulong)RegionFlags.AllowLandmark) == (ulong)RegionFlags.AllowLandmark);
            AllowParcelChanges = ((regionFlags & (ulong)RegionFlags.AllowParcelChanges) == (ulong)RegionFlags.AllowParcelChanges);
            AllowSetHome = ((regionFlags & (ulong)RegionFlags.AllowSetHome) == (ulong)RegionFlags.AllowSetHome);
        }
    }
}
