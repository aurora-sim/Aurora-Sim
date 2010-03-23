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
        public string PasswordHash { get; set; }
        public string IPAddress { get; set; }
        public string Identifier { get; set; }
        public string UserSession { get; set; }
        public string UserName { get; set; }
        public string HomeWorldServerIP { get; set; }
        public string HomeWorldServerPort { get; set; }
        public int PermaBanned { get; set; }
        public int TempBanned { get; set; }
        public bool Temperary = false;
        private string m_AllowPublish;
        private string m_MaturePublish;
        private string m_membershipGroup;
        private List<string> m_interests;
        private Dictionary<UUID, string> m_notes;
        private Dictionary<UUID, string> m_Picks;

        /// <summary>
        /// A UNIX Timestamp (seconds since epoch) for the users creation
        /// </summary>
        private int m_created;

        /// <summary>
        /// The first component of a users account name
        /// </summary>
        private string m_firstname;

        private uint m_homeRegionX;
        private uint m_homeRegionY;

        /// <summary>
        /// A UNIX Timestamp for the users last login date / time
        /// </summary>
        private int m_lastLogin;

        /// <summary>
        /// A salted hash containing the users password, in the format md5(md5(password) + ":" + salt)
        /// </summary>
        /// <remarks>This is double MD5'd because the client sends an unsalted MD5 to the loginserver</remarks>
        private string m_passwordHash;

        /// <summary>
        /// The salt used for the users hash, should be 32 bytes or longer
        /// </summary>
        private string m_passwordSalt;

        /// <summary>
        /// The about text listed in a users profile.
        /// </summary>
        private string m_profileAboutText = String.Empty;

        /// <summary>
        /// A uint mask containing the "I can do" fields of the users profile
        /// </summary>
        private uint m_profileCanDoMask;

        /// <summary>
        /// The profile image for the users first life tab
        /// </summary>
        private UUID m_profileFirstImage;

        /// <summary>
        /// The first life about text listed in a users profile
        /// </summary>
        private string m_profileFirstText = String.Empty;

        /// <summary>
        /// The profile image for an avatar stored on the asset server
        /// </summary>
        private UUID m_profileImage;

        /// <summary>
        /// A uint mask containing the "I want to do" part of the users profile
        /// </summary>
        private uint m_profileWantDoMask; // Profile window "I want to" mask

        /// <summary>
        /// The profile url for an avatar
        /// </summary>
        private string m_profileUrl;

        /// <summary>
        /// The second component of a users account name
        /// </summary>
        private string m_surname;

        /// <summary>
        /// A valid email address for the account.  Useful for password reset requests.
        /// </summary>
        private string m_email = String.Empty;

        // Data for estates and other goodies
        // to get away from per-machine configs a little
        //
        private int m_userFlags;
        private int m_godLevel;
        private string m_customType;
        private string m_partner;
        private string m_ProfileURL = "";

        private string m_homeRegionId;
        /// <summary>
        /// The regionID of the users home region. This is unique;
        /// even if the position of the region changes within the
        /// grid, this will refer to the same region.
        /// </summary>
        public string HomeRegionID
        {
            get { return m_homeRegionId; }
            set { m_homeRegionId = value; }
        }

        // Property wrappers
        public string FirstName
        {
            get { return m_firstname; }
            set { m_firstname = value; }
        }

        public string SurName
        {
            get { return m_surname; }
            set { m_surname = value; }
        }
        
        /// <value>
        /// The concatentation of the various name components.
        /// </value>
        public string Name
        {
            get { return String.Format("{0} {1}", m_firstname, m_surname); }
        }

        public string Email
        {
            get { return m_email; }
            set { m_email = value; }
        }

        public uint HomeRegionX
        {
            get { return m_homeRegionX; }
            set { m_homeRegionX = value; }
        }

        public uint HomeRegionY
        {
            get { return m_homeRegionY; }
            set { m_homeRegionY = value; }
        }

        private float m_homeLocationX;
        private float m_homeLocationY;
        private float m_homeLocationZ;
        // for handy serialization
        public float HomeLocationX
        {
            get { return m_homeLocationX; }
            set { m_homeLocationX = value; }
        }

        public float HomeLocationY
        {
            get { return m_homeLocationY; }
            set { m_homeLocationY = value; }
        }

        public float HomeLocationZ
        {
            get { return m_homeLocationZ; }
            set { m_homeLocationZ = value; }
        }


        private float m_homeLookAtX;
        private float m_homeLookAtY;
        private float m_homeLookAtZ;
        // for handy serialization
        public float HomeLookAtX
        {
            get { return m_homeLookAtX; }
            set { m_homeLookAtX = value; }
        }

        public float HomeLookAtY
        {
            get { return m_homeLookAtY; }
            set { m_homeLookAtY = value; }
        }

        public float HomeLookAtZ
        {
            get { return m_homeLookAtZ; }
            set { m_homeLookAtZ = value; }
        }

        public int Created
        {
            get { return m_created; }
            set { m_created = value; }
        }

        public int LastLogin
        {
            get { return m_lastLogin; }
            set { m_lastLogin = value; }
        }

        public uint CanDoMask
        {
            get { return m_profileCanDoMask; }
            set { m_profileCanDoMask = value; }
        }

        public uint WantDoMask
        {
            get { return m_profileWantDoMask; }
            set { m_profileWantDoMask = value; }
        }

        public string AboutText
        {
            get { return m_profileAboutText; }
            set { m_profileAboutText = value; }
        }

        public string FirstLifeAboutText
        {
            get { return m_profileFirstText; }
            set { m_profileFirstText = value; }
        }

        public UUID Image
        {
            get { return m_profileImage; }
            set { m_profileImage = value; }
        }

        public UUID FirstLifeImage
        {
            get { return m_profileFirstImage; }
            set { m_profileFirstImage = value; }
        }

        public int UserFlags
        {
            get { return m_userFlags; }
            set { m_userFlags = value; }
        }

        public string CustomType
        {
            get { return m_customType; }
            set { m_customType = value; }
        }

        public string Partner
        {
            get { return m_partner; }
            set { m_partner = value; }
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
        public int GodLevel
        {
            get { return m_godLevel; }
            set { m_godLevel = value; }
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
}
