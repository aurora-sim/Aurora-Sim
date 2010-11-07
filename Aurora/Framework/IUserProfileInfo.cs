using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;

namespace Aurora.Framework
{
    public enum IAgentFlags : uint
    {
        Foreign = 1,
        Temperary = 2,
        Minor = 3,
        Locked = 4,
        PermBan = 5,
        TempBan = 6,
        Blocked = 7,
        Local = 8,
        LocalOnly = 9,
        PastPrelude = 10
    }

    public class IAgentInfo : IDataTransferable
    {
        /// <summary>
        /// The ID value for this user
        /// </summary>
        public UUID PrincipalID = UUID.Zero;

        /// <summary>
        /// AgentFlags
        /// </summary>
        public IAgentFlags Flags = 0;

        /// <summary>
        /// Max maturity rating the user wishes to see
        /// </summary>
        public int MaturityRating = 2;

        /// <summary>
        /// Max maturity rating the user can ever to see
        /// </summary>
        public int MaxMaturity = 2;

        /// <summary>
        /// Did this user accept the TOS?
        /// </summary>
        public bool AcceptTOS = false;

        /// <summary>
        /// Current language
        /// </summary>
        public string Language = "en-us";

        /// <summary>
        /// Is the users language public
        /// </summary>
        public bool LanguageIsPublic = true;

        public override Dictionary<string, object> ToKeyValuePairs()
        {
            return Util.OSDToDictionary(ToOSD());
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();

            map.Add("PrincipalID", OSD.FromUUID(PrincipalID));
            map.Add("Flags", OSD.FromInteger((int)Flags));
            map.Add("MaxMaturity", OSD.FromInteger(MaxMaturity));
            map.Add("MaturityRating", OSD.FromInteger(MaturityRating));
            map.Add("Language", OSD.FromString(Language));
            map.Add("AcceptTOS", OSD.FromBoolean(AcceptTOS));
            map.Add("LanguageIsPublic", OSD.FromBoolean(LanguageIsPublic));
            
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            PrincipalID = map["PrincipalID"].AsUUID();
            Flags = (IAgentFlags)map["Flags"].AsInteger();
            MaxMaturity = Convert.ToInt32(map["MaxMaturity"].AsInteger());
            MaturityRating = Convert.ToInt32(map["MaturityRating"].AsInteger());
            Language = map["Language"].AsString();
            AcceptTOS = map["AcceptTOS"].AsBoolean();
            LanguageIsPublic = map["LanguageIsPublic"].AsBoolean();
        }

        public override void FromKVP(Dictionary<string, object> RetVal)
        {
            FromOSD(Util.DictionaryToOSD(RetVal));
        }

        public override IDataTransferable Duplicate()
        {
            IAgentInfo m = new IAgentInfo();
            m.FromOSD(ToOSD());
            return m;
        }
    }

    public class IUserProfileInfo : IDataTransferable
    {
        /// <summary>
        /// The ID value for this user
        /// </summary>
        public UUID PrincipalID = UUID.Zero;

        /// <summary>
        /// Show in search
        /// </summary>
        public bool AllowPublish = true;

        /// <summary>
        /// Allow for mature publishing
        /// </summary>
        public bool MaturePublish = false;

        /// <summary>
        /// The partner of this user
        /// </summary>
        public UUID Partner = UUID.Zero;

        /// <summary>
        /// the web address of the Profile URL
        /// </summary>
        public string WebURL = String.Empty;

        /// <summary>
        /// The about text listed in a users profile.
        /// </summary>
        public string AboutText = String.Empty;

        /// <summary>
        /// The first life about text listed in a users profile
        /// </summary>
        public string FirstLifeAboutText = String.Empty;

        /// <summary>
        /// The profile image for an avatar stored on the asset server
        /// </summary>
        public UUID Image = UUID.Zero;

        /// <summary>
        /// The profile image for the users first life tab
        /// </summary>
        public UUID FirstLifeImage = UUID.Zero;

        /// <summary>
        /// The type of the user
        /// </summary>
        public string CustomType = String.Empty;

        /// <summary>
        /// Is this user's online status visible to others?
        /// </summary>
        public bool Visible = true;

        /// <summary>
        /// Should IM's be sent to the user's email?
        /// </summary>
        public bool IMViaEmail = false;

        /// <summary>
        /// The appearance archive to load for this user
        /// </summary>
        public string AArchiveName = String.Empty;

        /// <summary>
        /// Is the user a new user?
        /// </summary>
        public bool IsNewUser = true;

        /// <summary>
        /// The group that the user is assigned to, ex: Premium
        /// </summary>
        public string MembershipGroup = String.Empty;

        /// <summary>
        /// A UNIX Timestamp (seconds since epoch) for the users creation
        /// </summary>
        public int Created = OpenSim.Framework.Util.UnixTimeSinceEpoch();

        /// <summary>
        /// The display name of the avatar
        /// </summary>
        public string DisplayName = String.Empty;

        /// <summary>
        /// The interests of the user
        /// </summary>
        public ProfileInterests Interests = new ProfileInterests();

        /// <summary>
        /// All of the notes of the user
        /// </summary>
        /// UUID - target agent
        /// string - notes
        public Dictionary<string, object> Notes = new Dictionary<string, object>();
        /// <summary>
        /// The picks of the user
        /// </summary>
        public Dictionary<string, object> Picks = new Dictionary<string,object>();

        /// <summary>
        /// All the classifieds of the user
        /// </summary>
        public Dictionary<string, object> Classifieds = new Dictionary<string,object>();

        public override Dictionary<string, object> ToKeyValuePairs()
        {
            return Util.OSDToDictionary(ToOSD());
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map.Add("PrincipalID", OSD.FromUUID(PrincipalID));
            map.Add("AllowPublish", OSD.FromBoolean(AllowPublish));
            map.Add("MaturePublish", OSD.FromBoolean(MaturePublish));
            map.Add("WantToMask", OSD.FromUInteger(Interests.WantToMask));
            map.Add("WantToText", OSD.FromString(Interests.WantToText));
            map.Add("CanDoMask", OSD.FromUInteger(Interests.CanDoMask));
            map.Add("CanDoText", OSD.FromString(Interests.CanDoText));
            map.Add("Languages", OSD.FromString(Interests.Languages));
            map.Add("AboutText", OSD.FromString(AboutText));
            map.Add("FirstLifeImage", OSD.FromUUID(FirstLifeImage));
            map.Add("FirstLifeAboutText", OSD.FromString(FirstLifeAboutText));
            map.Add("Image", OSD.FromUUID(Image));
            map.Add("WebURL", OSD.FromString(WebURL));
            map.Add("Created", OSD.FromInteger(Created));
            map.Add("DisplayName", OSD.FromString(DisplayName));
            map.Add("Partner", OSD.FromUUID(Partner));
            map.Add("Visible", OSD.FromBoolean(Visible));
            map.Add("AArchiveName", OSD.FromString(AArchiveName));
            map.Add("CustomType", OSD.FromString(CustomType));
            map.Add("IMViaEmail", OSD.FromBoolean(IMViaEmail));
            map.Add("IsNewUser", OSD.FromBoolean(IsNewUser));
            map.Add("MembershipGroup", OSD.FromString(MembershipGroup));

            map.Add("Classifieds", OSD.FromString(OSDParser.SerializeJsonString(Util.DictionaryToOSD(Classifieds))));
            map.Add("Picks", OSD.FromString(OSDParser.SerializeJsonString(Util.DictionaryToOSD(Picks))));
            map.Add("Notes", OSD.FromString(OSDParser.SerializeJsonString(Util.DictionaryToOSD(Notes))));
            
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            PrincipalID = map["PrincipalID"].AsUUID();
            AllowPublish = map["AllowPublish"].AsBoolean();
            MaturePublish = map["MaturePublish"].AsBoolean();

            //Interests
            Interests = new ProfileInterests();
            Interests.WantToMask = map["WantToMask"].AsUInteger();
            Interests.WantToText = map["WantToText"].AsString();
            Interests.CanDoMask = map["CanDoMask"].AsUInteger();
            Interests.CanDoText = map["CanDoText"].AsString();
            Interests.Languages = map["Languages"].AsString();
            //End interests

            if (map.ContainsKey("Classifieds"))
                Classifieds = Util.OSDToDictionary((OSDMap)OSDParser.DeserializeJson(map["Classifieds"].AsString()));

            if (map.ContainsKey("Picks"))
                Picks = Util.OSDToDictionary((OSDMap)OSDParser.DeserializeJson(map["Picks"].AsString()));

            if (map.ContainsKey("Notes"))
                Notes = Util.OSDToDictionary((OSDMap)OSDParser.DeserializeJson(map["Notes"].AsString()));

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

        public override void FromKVP(Dictionary<string, object> RetVal)
        {
            FromOSD(Util.DictionaryToOSD(RetVal));
        }

        public override IDataTransferable Duplicate()
        {
            IUserProfileInfo m = new IUserProfileInfo();
            m.FromOSD(ToOSD());
            return m;
        }
    }

    public class ProfileInterests
    {
        public uint WantToMask = 0;
        public string WantToText = "";
        public uint CanDoMask = 0;
        public string CanDoText = "";
        public string Languages = "";
    }

    public class Classified : IDataTransferable
    {
        public UUID ClassifiedUUID;
        public UUID CreatorUUID;
        public uint CreationDate;
        public uint ExpirationDate;
        public uint Category;
        public string Name;
        public string Description;
        public UUID ParcelUUID;
        public uint ParentEstate;
        public UUID SnapshotUUID;
        public string SimName;
        public Vector3 GlobalPos;
        public string ParcelName;
        public byte ClassifiedFlags;
        public int PriceForListing;

        public override OSDMap ToOSD()
        {
            OSDMap Classified = new OSDMap();
            Classified.Add("ClassifiedUUID", OSD.FromUUID(ClassifiedUUID));
            Classified.Add("CreatorUUID", OSD.FromUUID(CreatorUUID));
            Classified.Add("CreationDate", OSD.FromUInteger(CreationDate));
            Classified.Add("ExpirationDate", OSD.FromUInteger(ExpirationDate));
            Classified.Add("Category", OSD.FromUInteger(Category));
            Classified.Add("Name", OSD.FromString(Name));
            Classified.Add("Description", OSD.FromString(Description));
            Classified.Add("ParcelUUID", OSD.FromUUID(ParcelUUID));
            Classified.Add("ParentEstate", OSD.FromUInteger(ParentEstate));
            Classified.Add("SnapshotUUID", OSD.FromUUID(SnapshotUUID));
            Classified.Add("SimName", OSD.FromString(SimName));
            Classified.Add("GlobalPos", OSD.FromVector3(GlobalPos));
            Classified.Add("ParcelName", OSD.FromString(ParcelName));
            Classified.Add("ClassifiedFlags", OSD.FromInteger(ClassifiedFlags));
            Classified.Add("PriceForListing", OSD.FromInteger(PriceForListing));
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
            SimName = map["SimName"].AsString();
            GlobalPos = map["GlobalPos"].AsVector3();
            ParcelName = map["ParcelName"].AsString();
            ClassifiedFlags = (byte)map["ClassifiedFlags"].AsInteger();
            PriceForListing = map["PriceForListing"].AsInteger();
        }

        public override void FromKVP(Dictionary<string, object> KVP)
        {
            FromOSD(Util.DictionaryToOSD(KVP));
        }

        public override Dictionary<string, object> ToKeyValuePairs()
        {
            return Util.OSDToDictionary(ToOSD());
        }
    }

    public class ProfilePickInfo : IDataTransferable
    {
        public UUID PickUUID;
        public UUID CreatorUUID;
        public int TopPick;
        public UUID ParcelUUID;
        public string Name;
        public string Description;
        public UUID SnapshotUUID;
        public string User;
        public string OriginalName;
        public string SimName;
        public Vector3 GlobalPos;
        public int SortOrder;
        public int Enabled;

        public override OSDMap ToOSD()
        {
            OSDMap Pick = new OSDMap();
            Pick.Add("PickUUID",OSD.FromUUID(PickUUID));
            Pick.Add("CreatorUUID", OSD.FromUUID(CreatorUUID));
            Pick.Add("TopPick", OSD.FromInteger(TopPick));
            Pick.Add("ParcelUUID", OSD.FromUUID(ParcelUUID));
            Pick.Add("Name", OSD.FromString(Name));
            Pick.Add("Description", OSD.FromString(Description));
            Pick.Add("SnapshotUUID", OSD.FromUUID(SnapshotUUID));
            Pick.Add("User", OSD.FromString(User));
            Pick.Add("OriginalName", OSD.FromString(OriginalName));
            Pick.Add("SimName", OSD.FromString(SimName));
            Pick.Add("GlobalPos", OSD.FromVector3(GlobalPos));
            Pick.Add("SortOrder", OSD.FromInteger(SortOrder));
            Pick.Add("Enabled", OSD.FromInteger(Enabled));
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

        public override void FromKVP(Dictionary<string, object> KVP)
        {
            FromOSD(Util.DictionaryToOSD(KVP));
        }

        public override Dictionary<string, object> ToKeyValuePairs()
        {
            return Util.OSDToDictionary(ToOSD());
        }
    }
}
