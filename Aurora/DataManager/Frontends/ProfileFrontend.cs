using System;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenSim.Framework;

namespace Aurora.DataManager.Frontends
{
    public class ProfileFrontend
    {
        private Dictionary<UUID, IUserProfileInfo> UserProfilesCache = new Dictionary<UUID, IUserProfileInfo>();
        private IGenericData GD = null;
        public ProfileFrontend()
        {
            GD = Aurora.DataManager.DataManager.GetDefaultGenericPlugin();
        }
        
        public Classified ReadClassifiedInfoRow(string classifiedID)
        {
            List<string> retval = GD.Query("classifieduuid", classifiedID, "profileclassifieds", "*");
            Classified classified = new Classified();
            try
            {
                classified.ClassifiedUUID = retval[0];
                classified.CreatorUUID = retval[1];
                classified.CreationDate = retval[2];
                classified.ExpirationDate = retval[3];
                classified.Category = retval[4];
                classified.Name = retval[5];
                classified.Description = retval[6];
                classified.ParcelUUID = retval[7];
                classified.ParentEstate = retval[8];
                classified.SnapshotUUID = retval[9];
                classified.SimName = retval[10];
                classified.PosGlobal = retval[11];
                classified.ParcelName = retval[12];
                classified.ClassifiedFlags = retval[13];
                classified.PriceForListing = retval[14];
            }
            catch { }
            return classified;
        }

        public Classified[] ReadClassifedRow(string creatoruuid)
        {
            List<Classified> Classifieds = new List<Classified>();
            List<string> query = GD.Query("creatoruuid", creatoruuid, "profileclassifieds", "classifieduuid");

            try
            {
                for (int i = 0; i < query.Count; i++)
                {
                    Classifieds.Add(ReadClassifiedInfoRow(query[i]));
                }
            }
            catch { }
            return Classifieds.ToArray();
        }

        public ProfilePickInfo[] ReadPickRequestsRow(string creator)
        {
            List<string> query = GD.Query("creatoruuid", creator, "profilepicks", "pickuuid");
            List<ProfilePickInfo> Picks = new List<ProfilePickInfo>();
            try
            {
                for (int i = 0; i < query.Count; i++)
                {
                    Picks.Add(ReadPickInfoRow(query[i]));
                }
            }
            catch { }

            return Picks.ToArray();
        }

        public ProfilePickInfo ReadPickInfoRow(string pickID)
        {
            ProfilePickInfo pick = new ProfilePickInfo();
            List<string> retval = GD.Query("pickuuid", pickID, "profilepicks", "*");
            try
            {
                pick.pickuuid = retval[0];
                pick.creatoruuid = retval[1];
                pick.toppick = retval[2];
                pick.parceluuid = retval[3];
                pick.name = retval[4];
                pick.description = retval[5];
                pick.snapshotuuid = retval[6];
                pick.user = retval[7];
                pick.originalname = retval[8];
                pick.simname = retval[9];
                pick.posglobal = retval[10];
                pick.sortorder = retval[11];
                pick.enabled = retval[12];
            }
            catch { }
            return pick;
        }

        public ProfileInterests ReadInterestsInfoRow(string agentID)
        {
            ProfileInterests interests = new ProfileInterests();
            List<string> results = GD.Query("userUUID", agentID, "usersauth", "profileWantToMask,profileWantToText,profileSkillsMask,profileSkillsText,profileLanguages");

            try
            {
                interests.WantToMask = results[0];
                interests.WantToText = results[1];
                interests.CanDoMask = results[2];
                interests.CanDoText = results[3];
                interests.Languages = results[4];
                if (interests.WantToMask == " ")
                    interests.WantToMask = "0";
                if (interests.CanDoMask == " ")
                    interests.CanDoMask = "0";
            }
            catch { }
            return interests;
        }

        public IUserProfileInfo GetUserProfile(UUID agentID)
        {
            IUserProfileInfo UserProfile = new IUserProfileInfo();
            if (UserProfilesCache.ContainsKey(agentID))
            {
                UserProfilesCache.TryGetValue(agentID, out UserProfile);
                return UserProfile;
            }
            else
            {
                try
                {
                    ProfileInterests Interests = ReadInterestsInfoRow(agentID.ToString());
                    List<string> userauthReturns = GD.Query("userUUID",agentID.ToString(),"usersauth","userLogin,userPass,userGodLevel,membershipGroup,profileMaturePublish,profileAllowPublish,profileURL,AboutText,CustomType,Email,FirstLifeAboutText,FirstLifeImage,Partner,PermaBanned,TempBanned,Image,IsMinor,MatureRating,Created");
                    List<string> notesReturns = GD.Query("userid", agentID.ToString(),"profilenotes","targetuuid,notes");
                    UserProfile.Classifieds = ReadClassifedRow(agentID.ToString());
                    UserProfile.Picks = ReadPickRequestsRow(agentID.ToString());
                    Dictionary<UUID, string> Notes = new Dictionary<UUID, string>();
                    if (notesReturns.Count != 0)
                    {
                        for (int i = 0; i < notesReturns.Count; i = i + 2)
                        {
                            Notes.Add(new UUID(notesReturns[i]), notesReturns[i + 1]);
                        }
                    }
                    if (userauthReturns.Count == 1)
                        return null;
                    if (userauthReturns[2] == " ")
                        userauthReturns[2] = "0";
                    if (userauthReturns[5] == " ")
                        userauthReturns[5] = "0";
                    if (userauthReturns[4] == " ")
                        userauthReturns[4] = "0";
                    if (userauthReturns[11] == " ")
                        userauthReturns[11] = UUID.Zero.ToString();
                    if (userauthReturns[12] == " ")
                        userauthReturns[12] = UUID.Zero.ToString();
                    if (userauthReturns[15] == " ")
                        userauthReturns[15] = UUID.Zero.ToString();
                    UserProfile.FirstName = userauthReturns[0].Split(' ')[0];
                    UserProfile.LastName = userauthReturns[0].Split(' ')[1];
                    UserProfile.PrincipleID = agentID;
                    UserProfile.ProfileURL = userauthReturns[6];
                    UserProfile.Interests = Interests;
                    UserProfile.MembershipGroup = userauthReturns[3];
                    UserProfile.AllowPublish = userauthReturns[5];
                    UserProfile.MaturePublish = userauthReturns[4];
                    UserProfile.ProfileAboutText = userauthReturns[7];
                    UserProfile.Email = userauthReturns[9];
                    UserProfile.ProfileFirstText = userauthReturns[10];
                    UserProfile.ProfileFirstImage = new UUID(userauthReturns[12]);
                    UserProfile.Partner = new UUID(userauthReturns[12]);
                    UserProfile.PermaBanned = Convert.ToInt32(userauthReturns[13]);
                    UserProfile.TempBanned = Convert.ToInt32(userauthReturns[14]);
                    UserProfile.ProfileImage = new UUID(userauthReturns[15]);
                    UserProfile.IsMinor = Convert.ToBoolean(userauthReturns[16]);
                    UserProfile.MaturityRating = Convert.ToInt32(userauthReturns[17]);
                    UserProfile.Created = Convert.ToInt32(userauthReturns[18]);
                    UserProfile.Notes = Notes;
                    UserProfilesCache.Add(agentID, UserProfile);
                    return UserProfile;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public bool UpdateUserProfile(IUserProfileInfo Profile)
        {
            List<string> SetValues = new List<string>();
            List<string> SetRows = new List<string>();
            SetRows.Add("AboutText");
            SetRows.Add("profileAllowPublish");
            SetRows.Add("FirstLifeAboutText");
            SetRows.Add("FirstLifeImage");
            SetRows.Add("Image");
            SetRows.Add("ProfileURL");
            SetRows.Add("TempBanned");
            SetRows.Add("profileWantToMask");
            SetRows.Add("profileWantToText");
            SetRows.Add("profileSkillsMask");
            SetRows.Add("profileSkillsText");
            SetRows.Add("profileLanguages");
            SetRows.Add("IsMinor");
            SetRows.Add("MatureRating");
            SetValues.Add(Profile.ProfileAboutText);
            SetValues.Add(Profile.AllowPublish);
            SetValues.Add(Profile.ProfileFirstText);
            SetValues.Add(Profile.ProfileFirstImage.ToString());
            SetValues.Add(Profile.ProfileImage.ToString());
            SetValues.Add(Profile.ProfileURL);
            SetValues.Add(Profile.TempBanned.ToString());
            SetValues.Add(Profile.Interests.WantToMask);
            SetValues.Add(Profile.Interests.WantToText);
            SetValues.Add(Profile.Interests.CanDoMask);
            SetValues.Add(Profile.Interests.CanDoText);
            SetValues.Add(Profile.Interests.Languages);
            SetValues.Add(Profile.IsMinor.ToString());
            SetValues.Add(Profile.MaturityRating.ToString());
            List<string> KeyValue = new List<string>();
            List<string> KeyRow = new List<string>();
            KeyRow.Add("userUUID");
            KeyValue.Add(Profile.PrincipleID.ToString());
            return GD.Update("usersauth", SetValues.ToArray(), SetRows.ToArray(), KeyRow.ToArray(), KeyValue.ToArray());
        }

        public void CreateNewProfile(UUID UUID, string firstName, string lastName)
        {
            List<string> values = new List<string>();
            values.Add(UUID.ToString());
            values.Add(firstName + " " + lastName);
            values.Add(firstName);
            values.Add(lastName);
            values.Add(" ");
            values.Add(" ");
            values.Add("0");
            values.Add(" ");
            values.Add(" ");
            values.Add("0");
            values.Add("0");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add("0");
            values.Add("0");
            values.Add("1");
            values.Add("0");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add("0");
            values.Add(" ");
            values.Add("0");
            values.Add(" ");
            values.Add(" ");
            values.Add("0");
            values.Add("1");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add("true");
            values.Add("false");
            values.Add("2");
            values.Add("en");
            values.Add("1");
            values.Add(Util.UnixTimeSinceEpoch().ToString());
            var GD = Aurora.DataManager.DataManager.GetDefaultGenericPlugin();
            GD.Insert("usersauth", values.ToArray());
        }

        public void RemoveFromCache(UUID ID)
        {
            UserProfilesCache.Remove(ID);
        }
    }
}
