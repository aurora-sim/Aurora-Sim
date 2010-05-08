using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;

namespace Aurora.Framework
{
    public class IUserProfileInfo
    {
        /// <summary>
        /// Banned forever
        /// </summary>
        public int PermaBanned = 0;

        /// <summary>
        /// Temperarily banned
        /// </summary>
        public int TempBanned = 0;

        /// <summary>
        /// Is this account temperary?
        /// </summary>
        public bool Temperary = false;

        /// <summary>
        /// Show in search
        /// </summary>
        public string AllowPublish = "1";

        /// <summary>
        /// Allow for mature publishing
        /// </summary>
        public string MaturePublish = "0";

        /// <summary>
        /// The group that the user is assigned to, ex: Premium
        /// </summary>
        public string MembershipGroup;

        /// <summary>
        /// The interests of the user
        /// </summary>
        public ProfileInterests Interests;

        /// <summary>
        /// All of the notes of the user
        /// </summary>
        public Dictionary<UUID, string> Notes;

        /// <summary>
        /// The picks of the user
        /// </summary>
        public ProfilePickInfo[] Picks;

        /// <summary>
        /// All the classifieds of the user
        /// </summary>
        public Classified[] Classifieds;

        /// <summary>
        /// The about text listed in a users profile.
        /// </summary>
        public string ProfileAboutText = String.Empty;

        /// <summary>
        /// The profile image for the users first life tab
        /// </summary>
        public UUID ProfileFirstImage;

        /// <summary>
        /// The first life about text listed in a users profile
        /// </summary>
        public string ProfileFirstText = String.Empty;

        /// <summary>
        /// The profile image for an avatar stored on the asset server
        /// </summary>
        public UUID ProfileImage;

        /// <summary>
        /// A valid email address for the account.  Useful for password reset requests.
        /// </summary>
        public string Email = String.Empty;

        /// <summary>
        /// the web address of the Profile URL
        /// </summary>
        public string ProfileURL = "";

        /// <summary>
        /// Max maturity rating the user wishes to see
        /// </summary>
        public int MaturityRating = 0;

        /// <summary>
        /// Is this user a minor?
        /// </summary>
        public bool IsMinor = false;

        /// <summary>
        /// The ID value for this user
        /// </summary>
        public UUID PrincipleID;

        /// <summary>
        /// The first component of a users account name
        /// </summary>
        public string FirstName;

        /// <summary>
        /// The second component of a users account name
        /// </summary>
        public string LastName;

        /// <summary>
        /// A UNIX Timestamp (seconds since epoch) for the users creation
        /// </summary>
        public int Created;

        /// <summary>
        /// The partner of this user
        /// </summary>
        public UUID Partner;
    }
    public class ProfileInterests
    {
        public string WantToMask;
        public string WantToText;
        public string CanDoMask;
        public string CanDoText;
        public string Languages;
    }

    public class Classified
    {
        public string ClassifiedUUID;
        public string CreatorUUID;
        public string CreationDate;
        public string ExpirationDate;
        public string Category;
        public string Name;
        public string Description;
        public string ParcelUUID;
        public string ParentEstate;
        public string SnapshotUUID;
        public string SimName;
        public string PosGlobal;
        public string ParcelName;
        public string ClassifiedFlags;
        public string PriceForListing;
    }

    public class ProfilePickInfo
    {
        public string pickuuid;
        public string creatoruuid;
        public string toppick;
        public string parceluuid;
        public string name;
        public string description;
        public string snapshotuuid;
        public string user;
        public string originalname;
        public string simname;
        public string posglobal;
        public string sortorder;
        public string enabled;
    }
}
