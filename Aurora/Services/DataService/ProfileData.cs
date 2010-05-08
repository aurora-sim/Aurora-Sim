using System;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenSim.Framework;

namespace Aurora.Services.DataService
{
    public class ProfileData: IProfileData
    {

        #region IProfileData Members

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
