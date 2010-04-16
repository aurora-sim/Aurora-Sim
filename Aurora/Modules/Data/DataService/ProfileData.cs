using System;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenSim.Framework;

namespace Aurora.Modules
{
    public class ProfileData: IProfileData
    {

        #region IProfileData Members

        public List<string> ReadClassifiedInfoRow(string classifiedID)
        {
            if (Aurora.DataManager.DataManager.DefaultProfilePlugin != null)
                return Aurora.DataManager.DataManager.DefaultProfilePlugin.ReadClassifiedInfoRow(classifiedID);
            else
            {
                foreach (IProfileData plugin in Aurora.DataManager.DataManager.AllProfilePlugins)
                {
                    List<string> success = plugin.ReadClassifiedInfoRow(classifiedID);
                    if (success.Count != 0)
                        return success;
                }
            }
            return new List<string>();
        }

        public Dictionary<OpenMetaverse.UUID, string> ReadClassifedRow(string creatoruuid)
        {
            if (Aurora.DataManager.DataManager.DefaultProfilePlugin != null)
                return Aurora.DataManager.DataManager.DefaultProfilePlugin.ReadClassifedRow(creatoruuid);
            else
            {
                foreach (IProfileData plugin in Aurora.DataManager.DataManager.AllProfilePlugins)
                {
                    Dictionary<OpenMetaverse.UUID, string> success = plugin.ReadClassifedRow(creatoruuid);
                    if (success.Count != 0)
                        return success;
                }
            }
            return new Dictionary<OpenMetaverse.UUID, string>();
        }

        public Dictionary<OpenMetaverse.UUID, string> ReadPickRow(string creator)
        {
            if (Aurora.DataManager.DataManager.DefaultProfilePlugin != null)
                return Aurora.DataManager.DataManager.DefaultProfilePlugin.ReadPickRow(creator);
            else
            {
                foreach (IProfileData plugin in Aurora.DataManager.DataManager.AllProfilePlugins)
                {
                    Dictionary<OpenMetaverse.UUID, string> success = plugin.ReadPickRow(creator);
                    if (success.Count != 0)
                        return success;
                }
            }
            return new Dictionary<OpenMetaverse.UUID, string>();
        }

        public List<string> ReadInterestsInfoRow(string agentID)
        {
            if (Aurora.DataManager.DataManager.DefaultProfilePlugin != null)
                return Aurora.DataManager.DataManager.DefaultProfilePlugin.ReadInterestsInfoRow(agentID);
            else
            {
                foreach (IProfileData plugin in Aurora.DataManager.DataManager.AllProfilePlugins)
                {
                    List<string> success = plugin.ReadInterestsInfoRow(agentID);
                    if (success.Count != 0)
                        return success;
                }
            }
            return new List<string>();
        }

        public List<string> ReadPickInfoRow(string creator, string pickID)
        {
            if (Aurora.DataManager.DataManager.DefaultProfilePlugin != null)
                return Aurora.DataManager.DataManager.DefaultProfilePlugin.ReadPickInfoRow(creator, pickID);
            else
            {
                foreach (IProfileData plugin in Aurora.DataManager.DataManager.AllProfilePlugins)
                {
                    List<string> success = plugin.ReadPickInfoRow(creator,pickID);
                    if (success.Count != 0)
                        return success;
                }
            }
            return new List<string>();
        }

        public AuroraProfileData GetProfileNotes(OpenMetaverse.UUID agentID, OpenMetaverse.UUID target)
        {
            if (Aurora.DataManager.DataManager.DefaultProfilePlugin != null)
                return Aurora.DataManager.DataManager.DefaultProfilePlugin.GetProfileNotes(agentID,target);
            else
            {
                foreach (IProfileData plugin in Aurora.DataManager.DataManager.AllProfilePlugins)
                {
                    AuroraProfileData success = plugin.GetProfileNotes(agentID,target);
                    if (success != null)
                        return success;
                }
            }
            return null;
        }

        public bool InvalidateProfileNotes(OpenMetaverse.UUID target)
        {
            if (Aurora.DataManager.DataManager.DefaultProfilePlugin != null)
                Aurora.DataManager.DataManager.DefaultProfilePlugin.InvalidateProfileNotes(target);
            else
            {
                foreach (IProfileData plugin in Aurora.DataManager.DataManager.AllProfilePlugins)
                {
                    bool success = plugin.InvalidateProfileNotes(target);
                    if (success)
                        return true;
                }
            }
            return false;
        }

        public bool FullUpdateUserProfile(AuroraProfileData Profile)
        {
            if (Aurora.DataManager.DataManager.DefaultProfilePlugin != null)
                Aurora.DataManager.DataManager.DefaultProfilePlugin.FullUpdateUserProfile(Profile);
            else
            {
                foreach (IProfileData plugin in Aurora.DataManager.DataManager.AllProfilePlugins)
                {
                    bool success = plugin.FullUpdateUserProfile(Profile);
                    if (success)
                        return true;
                }
            }
            return false;
        }

        public AuroraProfileData GetProfileInfo(OpenMetaverse.UUID agentID)
        {
            if (Aurora.DataManager.DataManager.DefaultProfilePlugin != null)
                return Aurora.DataManager.DataManager.DefaultProfilePlugin.GetProfileInfo(agentID);
            else
            {
                foreach (IProfileData plugin in Aurora.DataManager.DataManager.AllProfilePlugins)
                {
                    AuroraProfileData success = plugin.GetProfileInfo(agentID);
                    if (success != null)
                        return success;
                }
            }
            return null;
        }

        public bool UpdateUserProfile(AuroraProfileData Profile)
        {
            if (Aurora.DataManager.DataManager.DefaultProfilePlugin != null)
                Aurora.DataManager.DataManager.DefaultProfilePlugin.UpdateUserProfile(Profile);
            else
            {
                foreach (IProfileData plugin in Aurora.DataManager.DataManager.AllProfilePlugins)
                {
                    bool success = plugin.UpdateUserProfile(Profile);
                    if (success)
                        return true;
                }
            }
            return false;
        }

        public AuroraProfileData CreateTemperaryAccount(string client, string first, string last)
        {
            if (Aurora.DataManager.DataManager.DefaultProfilePlugin != null)
                return Aurora.DataManager.DataManager.DefaultProfilePlugin.CreateTemperaryAccount(client, first, last);
            else
            {
                foreach (IProfileData plugin in Aurora.DataManager.DataManager.AllProfilePlugins)
                {
                    AuroraProfileData success = plugin.CreateTemperaryAccount(client, first, last);
                    if (success != null)
                        return success;
                }
            }
            return null;
        }

        public DirPlacesReplyData[] PlacesQuery(string queryText, string category, string table, string wantedValue, int StartQuery)
        {
            if (Aurora.DataManager.DataManager.DefaultProfilePlugin != null)
                return Aurora.DataManager.DataManager.DefaultProfilePlugin.PlacesQuery(queryText, category, table, wantedValue, StartQuery);
            else
            {
                foreach (IProfileData plugin in Aurora.DataManager.DataManager.AllProfilePlugins)
                {
                    DirPlacesReplyData[] success = plugin.PlacesQuery(queryText, category, table, wantedValue, StartQuery);
                    if (success.Length != 0)
                        return success;
                }
            }
            return new List<DirPlacesReplyData>().ToArray();
        }

        public DirLandReplyData[] LandForSaleQuery(string searchType, string price, string area, string table, string wantedValue, int StartQuery)
        {
            if (Aurora.DataManager.DataManager.DefaultProfilePlugin != null)
                return Aurora.DataManager.DataManager.DefaultProfilePlugin.LandForSaleQuery(searchType,price,area , table, wantedValue, StartQuery);
            else
            {
                foreach (IProfileData plugin in Aurora.DataManager.DataManager.AllProfilePlugins)
                {
                    DirLandReplyData[] success = plugin.LandForSaleQuery(searchType, price, area, table, wantedValue, StartQuery);
                    if (success.Length != 0)
                        return success;
                }
            }
            return new List<DirLandReplyData>().ToArray();
        }

        public DirClassifiedReplyData[] ClassifiedsQuery(string queryText, string category, string queryFlags, int StartQuery)
        {
            if (Aurora.DataManager.DataManager.DefaultProfilePlugin != null)
                return Aurora.DataManager.DataManager.DefaultProfilePlugin.ClassifiedsQuery(queryText, category, queryFlags, StartQuery);
            else
            {
                foreach (IProfileData plugin in Aurora.DataManager.DataManager.AllProfilePlugins)
                {
                    DirClassifiedReplyData[] success = plugin.ClassifiedsQuery(queryText, category, queryFlags, StartQuery);
                    if (success.Length != 0)
                        return success;
                }
            }
            return new List<DirClassifiedReplyData>().ToArray();
        }

        public DirEventsReplyData[] EventQuery(string queryText, string flags, string table, string wantedValue, int StartQuery)
        {
            if (Aurora.DataManager.DataManager.DefaultProfilePlugin != null)
                return Aurora.DataManager.DataManager.DefaultProfilePlugin.EventQuery(queryText, flags, table, wantedValue, StartQuery);
            else
            {
                foreach (IProfileData plugin in Aurora.DataManager.DataManager.AllProfilePlugins)
                {
                    DirEventsReplyData[] success = plugin.EventQuery(queryText,flags,table,wantedValue,StartQuery);
                    if (success.Length != 0)
                        return success;
                }
            }
            return new List<DirEventsReplyData>().ToArray();
        }

        public EventData GetEventInfo(string p)
        {
            if (Aurora.DataManager.DataManager.DefaultProfilePlugin != null)
                return Aurora.DataManager.DataManager.DefaultProfilePlugin.GetEventInfo(p);
            else
            {
                foreach (IProfileData plugin in Aurora.DataManager.DataManager.AllProfilePlugins)
                {
                    EventData success = plugin.GetEventInfo(p);
                    if (success != null)
                        return success;
                }
            }
            return null;
        }

        public DirEventsReplyData[] GetAllEventsNearXY(string table, int X, int Y)
        {
            if (Aurora.DataManager.DataManager.DefaultProfilePlugin != null)
                return Aurora.DataManager.DataManager.DefaultProfilePlugin.GetAllEventsNearXY(table, X, Y);
            else
            {
                foreach (IProfileData plugin in Aurora.DataManager.DataManager.AllProfilePlugins)
                {
                    DirEventsReplyData[] success = plugin.GetAllEventsNearXY(table, X, Y);
                    if (success.Length != 0)
                        return success;
                }
            }
            return new List<DirEventsReplyData>().ToArray();
        }

        public EventData[] GetEvents()
        {
            if (Aurora.DataManager.DataManager.DefaultProfilePlugin != null)
                return Aurora.DataManager.DataManager.DefaultProfilePlugin.GetEvents();
            else
            {
                foreach (IProfileData plugin in Aurora.DataManager.DataManager.AllProfilePlugins)
                {
                    EventData[] success = plugin.GetEvents();
                    if (success.Length != 0)
                        return success;
                }
            }
            return new List<EventData>().ToArray();
        }

        public Classified[] GetClassifieds()
        {
            if (Aurora.DataManager.DataManager.DefaultProfilePlugin != null)
                return Aurora.DataManager.DataManager.DefaultProfilePlugin.GetClassifieds();
            else
            {
                foreach (IProfileData plugin in Aurora.DataManager.DataManager.AllProfilePlugins)
                {
                    Classified[] success = plugin.GetClassifieds();
                    if (success.Length != 0)
                        return success;
                }
            }
            return new List<Classified>().ToArray();
        }

        #endregion
    }
}
