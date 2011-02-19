using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Services.Interfaces;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using FriendInfo = OpenSim.Services.Interfaces.FriendInfo;

namespace OpenSim.Services.MessagingService
{
    public class FriendsProcessing : IService
    {
        #region Declares

        protected IRegistryCore m_registry;

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            registry.RequestModuleInterface<ISimulationBase>().EventManager.OnGenericEvent += OnGenericEvent;

            //Also look for incoming messages to display
            registry.RequestModuleInterface<IAsyncMessageRecievedService>().OnMessageReceived += OnMessageReceived;
        }

        protected object OnGenericEvent(string FunctionName, object parameters)
        {
            if (FunctionName == "UserStatusChange")
            {
                //A user has logged in or out... we need to update friends lists across the grid

                IAsyncMessagePostService asyncPoster = m_registry.RequestModuleInterface<IAsyncMessagePostService>();
                IFriendsService friendsService = m_registry.RequestModuleInterface<IFriendsService>();
                ICapsService capsService = m_registry.RequestModuleInterface<ICapsService>();
                if (asyncPoster != null && friendsService != null && capsService != null)
                {
                    UserInfo info = (UserInfo)parameters;
                    FriendInfo[] friends = friendsService.GetFriends(UUID.Parse(info.UserID));
                    foreach(FriendInfo friend in friends)
                    {
                        IClientCapsService clientCaps = capsService.GetClientCapsService(UUID.Parse(info.UserID));
                        if(clientCaps != null)
                        {
                            IRegionClientCapsService regionClientCaps = clientCaps.GetRootCapsService();
                            if(regionClientCaps != null)
                            {
                                asyncPoster.Post(regionClientCaps.RegionHandle, SyncMessageHelper.AgentStatusChange(UUID.Parse(info.UserID), friend.PrincipalID, info.IsOnline));
                            }
                        }
                    }
                }
            }
            return null;
        }

        protected OSDMap OnMessageReceived(OSDMap message)
        {
            if (message.ContainsKey("Method") && message["Method"] == "AgentStatusChange")
            {
                //We got a message, now display it
                UUID UserID = message["UserID"].AsUUID();
                UUID FriendID = message["FriendID"].AsUUID();
                bool NewStatus = message["NewStatus"].AsBoolean();

                SceneManager manager = m_registry.RequestModuleInterface<SceneManager>();
                if (manager != null && manager.Scenes.Count > 0)
                {
                    IFriendsModule friendsModule = manager.Scenes[0].RequestModuleInterface<IFriendsModule>();
                    if (friendsModule != null)
                    {
                        friendsModule.SendFriendsStatusMessage(FriendID, UserID, NewStatus);
                    }
                }
            }
            return null;
        }

        #endregion
    }
}
