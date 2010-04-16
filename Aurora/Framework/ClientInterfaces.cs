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
using System.Text;
using OpenSim.Framework;
using OpenMetaverse;
using OpenSim.Services.Interfaces;

namespace Aurora.Framework
{
    public class AuroraProfileData : UserProfileData
    {
        public string Login { get; set; }
        public string IPAddress { get; set; }
        public string Identifier { get; set; }
        public string UserSession { get; set; }
        public string UserName { get; set; }
        public string HomeWorldServerIP { get; set; }
        public string HomeWorldServerPort { get; set; }
        public int PermaBanned { get; set; }
        public int TempBanned { get; set; }
        public bool Temperary = false;
        private string m_AllowPublish = "1";
        private string m_MaturePublish = "0";
        private string m_membershipGroup;
        private List<string> m_interests;
        private Dictionary<UUID, string> m_notes;
        private Dictionary<UUID, string> m_Picks;

       /// <summary>
        /// The about text listed in a users profile.
        /// </summary>
        private string m_profileAboutText = String.Empty;

        /// <summary>
        /// The first life about text listed in a users profile
        /// </summary>
        private string m_profileFirstText = String.Empty;

        /// <summary>
        /// A valid email address for the account.  Useful for password reset requests.
        /// </summary>
        private string m_email = String.Empty;

        private string m_ProfileURL = "";
        private int m_Mature_Rating = 0;
        private bool m_Is_Minor = false;

        public bool Minor
        {
            get { return m_Is_Minor; }
            set { m_Is_Minor = value; }
        }

        public int Mature
        {
            get { return m_Mature_Rating; }
            set { m_Mature_Rating = value; }
        }

        public string AllowPublish
        {
            get { return m_AllowPublish; }
            set { m_AllowPublish = value; }
        }
        public string MaturePublish
        {
            get { return m_MaturePublish; }
            set { m_MaturePublish = value; }
        }
        public string MembershipGroup
        {
            get { return m_membershipGroup; }
            set { m_membershipGroup = value; }
        }
        public string ProfileURL
        {
            get { return m_ProfileURL; }
            set { m_ProfileURL = value; }
        }
        
        public List<string> Interests
        {
            get { return m_interests; }
            set { m_interests = value; }
        }
        public Dictionary<UUID, string> Notes
        {
            get { return m_notes; }
            set { m_notes = value; }
        }
        public Dictionary<UUID, string> Picks
        {
            get { return m_Picks; }
            set { m_Picks = value; }
        }
    }
    public interface IEstateSettingsModule
    {
        bool AllowTeleport(IScene regionID, UUID userID, Vector3 Position, out Vector3 newPosition);
    }
}
