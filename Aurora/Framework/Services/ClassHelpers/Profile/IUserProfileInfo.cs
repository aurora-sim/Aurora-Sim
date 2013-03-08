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
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;

namespace Aurora.Framework
{
    [Flags]
    public enum IAgentFlags : uint
    {
        Foreign = 1,
        Temperary = 2,
        Minor = 4,
        Locked = 8,
        PermBan = 16,
        TempBan = 32,
        Blocked = 64,
        Local = 128,
        LocalOnly = 256,
        PastPrelude = 512
    }

    public class IAgentInfo : IDataTransferable, BaseCacheAccount
    {
        /// <summary>
        ///   Did this user accept the TOS?
        /// </summary>
        public bool AcceptTOS;

        /// <summary>
        ///   AgentFlags
        /// </summary>
        public IAgentFlags Flags = 0;

        /// <summary>
        ///   Current language
        /// </summary>
        public string Language = "en-us";

        /// <summary>
        ///   Is the users language public
        /// </summary>
        public bool LanguageIsPublic = true;

        /// <summary>
        ///   Max maturity rating the user wishes to see
        /// </summary>
        public int MaturityRating = 2;

        /// <summary>
        ///   Max maturity rating the user can ever to see
        /// </summary>
        public int MaxMaturity = 2;

        /// <summary>
        ///   Other information can be stored in here.
        ///   For ex, temperary ban info for this user
        /// </summary>
        public OSDMap OtherAgentInformation = new OSDMap();

        /// <summary>
        ///   The ID value for this user
        /// </summary>
        public UUID PrincipalID { get; set; }

        /// <summary>
        /// Unused, only exists for caching purposes
        /// </summary>
        public string Name { get; set; }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap
                             {
                                 {"PrincipalID", OSD.FromUUID(PrincipalID)},
                                 {"Flags", OSD.FromInteger((int) Flags)},
                                 {"MaxMaturity", OSD.FromInteger(MaxMaturity)},
                                 {"MaturityRating", OSD.FromInteger(MaturityRating)},
                                 {"Language", OSD.FromString(Language)},
                                 {"AcceptTOS", OSD.FromBoolean(AcceptTOS)},
                                 {"LanguageIsPublic", OSD.FromBoolean(LanguageIsPublic)},
                                 {
                                     "OtherAgentInformation",
                                     OSD.FromString(OSDParser.SerializeLLSDXmlString(OtherAgentInformation))
                                     }
                             };

            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            PrincipalID = map["PrincipalID"].AsUUID();
            Flags = (IAgentFlags) map["Flags"].AsInteger();
            MaxMaturity = Convert.ToInt32(map["MaxMaturity"].AsInteger());
            MaturityRating = Convert.ToInt32(map["MaturityRating"].AsInteger());
            Language = map["Language"].AsString();
            AcceptTOS = map["AcceptTOS"].AsBoolean();
            LanguageIsPublic = map["LanguageIsPublic"].AsBoolean();
            if (map.ContainsKey("OtherAgentInformation"))
                OtherAgentInformation = (OSDMap) OSDParser.DeserializeLLSDXml(map["OtherAgentInformation"].AsString());
        }
    }

    public class IUserProfileInfo : IDataTransferable
    {
        #region ProfileFlags enum

        public enum ProfileFlags
        {
            NoPaymentInfoOnFile = 2,
            PaymentInfoOnFile = 4,
            PaymentInfoInUse = 8,
            AgentOnline = 16
        }

        #endregion

        /// <summary>
        ///   The appearance archive to load for this user
        /// </summary>
        public string AArchiveName = String.Empty;

        /// <summary>
        ///   The about text listed in a users profile.
        /// </summary>
        public string AboutText = String.Empty;

        /// <summary>
        ///   Show in search
        /// </summary>
        public bool AllowPublish = true;

        /// <summary>
        ///   A UNIX Timestamp (seconds since epoch) for the users creation
        /// </summary>
        public int Created = Util.UnixTimeSinceEpoch();

        /// <summary>
        ///   The type of the user
        /// </summary>
        public string CustomType = String.Empty;

        /// <summary>
        ///   The display name of the avatar
        /// </summary>
        public string DisplayName = String.Empty;

        /// <summary>
        ///   The first life about text listed in a users profile
        /// </summary>
        public string FirstLifeAboutText = String.Empty;

        /// <summary>
        ///   The profile image for the users first life tab
        /// </summary>
        public UUID FirstLifeImage = UUID.Zero;

        /// <summary>
        ///   Should IM's be sent to the user's email?
        /// </summary>
        public bool IMViaEmail;

        /// <summary>
        ///   The profile image for an avatar stored on the asset server
        /// </summary>
        public UUID Image = UUID.Zero;

        /// <summary>
        ///   The interests of the user
        /// </summary>
        public ProfileInterests Interests = new ProfileInterests();

        /// <summary>
        ///   Is the user a new user?
        /// </summary>
        public bool IsNewUser = true;

        /// <summary>
        ///   Allow for mature publishing
        /// </summary>
        public bool MaturePublish;

        /// <summary>
        ///   The group that the user is assigned to, ex: Premium
        /// </summary>
        public string MembershipGroup = String.Empty;

        /// <summary>
        ///   All of the notes of the user
        /// </summary>
        /// UUID - target agent
        /// string - notes
        public OSDMap Notes = new OSDMap();

        /// <summary>
        ///   The partner of this user
        /// </summary>
        public UUID Partner = UUID.Zero;

        /// <summary>
        ///   The ID value for this user
        /// </summary>
        public UUID PrincipalID = UUID.Zero;

        /// <summary>
        ///   Is this user's online status visible to others?
        /// </summary>
        public bool Visible = true;

        /// <summary>
        ///   the web address of the Profile URL
        /// </summary>
        public string WebURL = String.Empty;

        public override OSDMap ToOSD()
        {
            return ToOSD(true);
        }

        /// <summary>
        ///   This method creates a smaller OSD that 
        ///   does not contain sensitive information
        ///   if the trusted boolean is false
        /// </summary>
        /// <param name = "secure"></param>
        /// <returns></returns>
        public OSDMap ToOSD(bool trusted)
        {
            OSDMap map = new OSDMap
                             {
                                 {"PrincipalID", OSD.FromUUID(PrincipalID)},
                                 {"AllowPublish", OSD.FromBoolean(AllowPublish)},
                                 {"MaturePublish", OSD.FromBoolean(MaturePublish)},
                                 {"WantToMask", OSD.FromUInteger(Interests.WantToMask)},
                                 {"WantToText", OSD.FromString(Interests.WantToText)},
                                 {"CanDoMask", OSD.FromUInteger(Interests.CanDoMask)},
                                 {"CanDoText", OSD.FromString(Interests.CanDoText)},
                                 {"Languages", OSD.FromString(Interests.Languages)},
                                 {"AboutText", OSD.FromString(AboutText)},
                                 {"FirstLifeImage", OSD.FromUUID(FirstLifeImage)},
                                 {"FirstLifeAboutText", OSD.FromString(FirstLifeAboutText)},
                                 {"Image", OSD.FromUUID(Image)},
                                 {"WebURL", OSD.FromString(WebURL)},
                                 {"Created", OSD.FromInteger(Created)},
                                 {"DisplayName", OSD.FromString(DisplayName)},
                                 {"Partner", OSD.FromUUID(Partner)},
                                 {"Visible", OSD.FromBoolean(Visible)},
                                 {"CustomType", OSD.FromString(CustomType)}
                             };
            if (trusted)
            {
                map.Add("AArchiveName", OSD.FromString(AArchiveName));
                map.Add("IMViaEmail", OSD.FromBoolean(IMViaEmail));
                map.Add("IsNewUser", OSD.FromBoolean(IsNewUser));
                map.Add("MembershipGroup", OSD.FromString(MembershipGroup));
            }

            map.Add("Notes", OSD.FromString(OSDParser.SerializeJsonString(Notes)));
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            PrincipalID = map["PrincipalID"].AsUUID();
            AllowPublish = map["AllowPublish"].AsBoolean();
            MaturePublish = map["MaturePublish"].AsBoolean();

            //Interests
            Interests = new ProfileInterests
                            {
                                WantToMask = map["WantToMask"].AsUInteger(),
                                WantToText = map["WantToText"].AsString(),
                                CanDoMask = map["CanDoMask"].AsUInteger(),
                                CanDoText = map["CanDoText"].AsString(),
                                Languages = map["Languages"].AsString()
                            };
            //End interests

            try
            {
                if (map.ContainsKey("Notes"))
                    Notes = (OSDMap) OSDParser.DeserializeJson(map["Notes"].AsString());
            }
            catch
            {
            }

            AboutText = map["AboutText"].AsString();
            FirstLifeImage = map["FirstLifeImage"].AsUUID();
            FirstLifeAboutText = map["FirstLifeAboutText"].AsString();
            Image = map["Image"].AsUUID();
            WebURL = map["WebURL"].AsString();
            Created = map["Created"].AsInteger();
            DisplayName = map["DisplayName"].AsString();
            Partner = map["Partner"].AsUUID();
            Visible = map["Visible"].AsBoolean();
            AArchiveName = map["AArchiveName"].AsString();
            CustomType = map["CustomType"].AsString();
            IMViaEmail = map["IMViaEmail"].AsBoolean();
            IsNewUser = map["IsNewUser"].AsBoolean();
            MembershipGroup = map["MembershipGroup"].AsString();
        }
    }

    public class ProfileInterests
    {
        public uint CanDoMask;
        public string CanDoText = "";
        public string Languages = "";
        public uint WantToMask;
        public string WantToText = "";
    }

    public class Classified : IDataTransferable
    {
        public uint Category;
        public byte ClassifiedFlags;
        public UUID ClassifiedUUID;
        public uint CreationDate;
        public UUID CreatorUUID;
        public string Description;
        public uint ExpirationDate;
        public Vector3 GlobalPos;
        public string Name;
        public string ParcelName;
        public UUID ParcelUUID;
        public uint ParentEstate;
        public int PriceForListing;
        public UUID ScopeID;
        public string SimName;
        public UUID SnapshotUUID;

        public override OSDMap ToOSD()
        {
            OSDMap Classified = new OSDMap
                                    {
                                        {"ClassifiedUUID", OSD.FromUUID(ClassifiedUUID)},
                                        {"CreatorUUID", OSD.FromUUID(CreatorUUID)},
                                        {"CreationDate", OSD.FromUInteger(CreationDate)},
                                        {"ExpirationDate", OSD.FromUInteger(ExpirationDate)},
                                        {"Category", OSD.FromUInteger(Category)},
                                        {"Name", OSD.FromString(Name)},
                                        {"Description", OSD.FromString(Description)},
                                        {"ParcelUUID", OSD.FromUUID(ParcelUUID)},
                                        {"ParentEstate", OSD.FromUInteger(ParentEstate)},
                                        {"SnapshotUUID", OSD.FromUUID(SnapshotUUID)},
                                        {"ScopeID", OSD.FromUUID(ScopeID)},
                                        {"SimName", OSD.FromString(SimName)},
                                        {"GlobalPos", OSD.FromVector3(GlobalPos)},
                                        {"ParcelName", OSD.FromString(ParcelName)},
                                        {"ClassifiedFlags", OSD.FromInteger(ClassifiedFlags)},
                                        {"PriceForListing", OSD.FromInteger(PriceForListing)}
                                    };
            return Classified;
        }

        public override void FromOSD(OSDMap map)
        {
            ClassifiedUUID = map["ClassifiedUUID"].AsUUID();
            CreatorUUID = map["CreatorUUID"].AsUUID();
            CreationDate = map["CreationDate"].AsUInteger();
            ExpirationDate = map["ExpirationDate"].AsUInteger();
            Category = map["Category"].AsUInteger();
            Name = map["Name"].AsString();
            Description = map["Description"].AsString();
            ParcelUUID = map["ParcelUUID"].AsUUID();
            ParentEstate = map["ParentEstate"].AsUInteger();
            SnapshotUUID = map["SnapshotUUID"].AsUUID();
            ScopeID = map["ScopeID"].AsUUID();
            SimName = map["SimName"].AsString();
            GlobalPos = map["GlobalPos"].AsVector3();
            ParcelName = map["ParcelName"].AsString();
            ClassifiedFlags = (byte) map["ClassifiedFlags"].AsInteger();
            PriceForListing = map["PriceForListing"].AsInteger();
        }
    }

    public class ProfilePickInfo : IDataTransferable
    {
        public UUID CreatorUUID;
        public string Description;
        public int Enabled;
        public Vector3 GlobalPos;
        public string Name;
        public string OriginalName;
        public UUID ParcelUUID;
        public UUID PickUUID;
        public string SimName;
        public UUID SnapshotUUID;
        public int SortOrder;
        public int TopPick;
        public string User;

        public override OSDMap ToOSD()
        {
            OSDMap Pick = new OSDMap
                              {
                                  {"PickUUID", OSD.FromUUID(PickUUID)},
                                  {"CreatorUUID", OSD.FromUUID(CreatorUUID)},
                                  {"TopPick", OSD.FromInteger(TopPick)},
                                  {"ParcelUUID", OSD.FromUUID(ParcelUUID)},
                                  {"Name", OSD.FromString(Name)},
                                  {"Description", OSD.FromString(Description)},
                                  {"SnapshotUUID", OSD.FromUUID(SnapshotUUID)},
                                  {"User", OSD.FromString(User)},
                                  {"OriginalName", OSD.FromString(OriginalName)},
                                  {"SimName", OSD.FromString(SimName)},
                                  {"GlobalPos", OSD.FromVector3(GlobalPos)},
                                  {"SortOrder", OSD.FromInteger(SortOrder)},
                                  {"Enabled", OSD.FromInteger(Enabled)}
                              };
            return Pick;
        }

        public override void FromOSD(OSDMap map)
        {
            PickUUID = map["PickUUID"].AsUUID();
            CreatorUUID = map["CreatorUUID"].AsUUID();
            TopPick = map["TopPick"].AsInteger();
            ParcelUUID = map["AsString"].AsUUID();
            Name = map["Name"].AsString();
            Description = map["Description"].AsString();
            SnapshotUUID = map["SnapshotUUID"].AsUUID();
            User = map["User"].AsString();
            OriginalName = map["OriginalName"].AsString();
            SimName = map["SimName"].AsString();
            GlobalPos = map["GlobalPos"].AsVector3();
            SortOrder = map["SortOrder"].AsInteger();
            Enabled = map["Enabled"].AsInteger();
        }
    }
}