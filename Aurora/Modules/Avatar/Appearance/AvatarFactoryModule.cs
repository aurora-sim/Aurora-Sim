/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Timers;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;
using Timer = System.Timers.Timer;

namespace Aurora.Modules.Appearance
{
    public class AvatarFactoryModule : IAvatarFactory, INonSharedRegionModule
    {
        #region Declares

        private readonly TimedSaving<AvatarAppearance> _saveQueue = new TimedSaving<AvatarAppearance>();
        private readonly TimedSaving<AvatarAppearance> _sendQueue = new TimedSaving<AvatarAppearance>();
        private readonly TimedSaving<AvatarAppearance> _initialSendQueue = new TimedSaving<AvatarAppearance>();
        private readonly object m_setAppearanceLock = new object();
        private int m_initialsendtime = 3; // seconds to wait before sending the initial appearance
        private int m_savetime = 5; // seconds to wait before saving changed appearance
        private int m_sendtime = 2; // seconds to wait before sending appearances
        private IScene m_scene;

        #endregion

        #region Default UnderClothes

        private static UUID m_underShirtUUID = UUID.Zero;

        private static UUID m_underPantsUUID = UUID.Zero;

        private const string m_defaultUnderPants = @"LLWearable version 22
New Underpants

	permissions 0
	{
		base_mask	7fffffff
		owner_mask	7fffffff
		group_mask	00000000
		everyone_mask	00000000
		next_owner_mask	00082000
		creator_id	05948863-b678-433e-87a4-e44d17678d1d
		owner_id	05948863-b678-433e-87a4-e44d17678d1d
		last_owner_id	00000000-0000-0000-0000-000000000000
		group_id	00000000-0000-0000-0000-000000000000
	}
	sale_info	0
	{
		sale_type	not
		sale_price	10
	}
type 11
parameters 5
619 .3
624 .8
824 1
825 1
826 1
textures 1
17 5748decc-f629-461c-9a36-a35a221fe21f";

        private const string m_defaultUnderShirt = @"LLWearable version 22
New Undershirt

	permissions 0
	{
		base_mask	7fffffff
		owner_mask	7fffffff
		group_mask	00000000
		everyone_mask	00000000
		next_owner_mask	00082000
		creator_id	05948863-b678-433e-87a4-e44d17678d1d
		owner_id	05948863-b678-433e-87a4-e44d17678d1d
		last_owner_id	00000000-0000-0000-0000-000000000000
		group_id	00000000-0000-0000-0000-000000000000
	}
	sale_info	0
	{
		sale_type	not
		sale_price	10
	}
type 10
parameters 7
603 .4
604 .85
605 .84
779 .84
821 1
822 1
823 1
textures 1
16 5748decc-f629-461c-9a36-a35a221fe21f";

        #endregion

        #region INonSharedRegionModule Members

        public void Initialise(IConfigSource config)
        {
            if (config != null)
            {
                IConfig sconfig = config.Configs["Startup"];
                if (sconfig != null)
                {
                    m_savetime = sconfig.GetInt("DelayBeforeAppearanceSave", m_savetime);
                    m_sendtime = sconfig.GetInt("DelayBeforeAppearanceSend", m_sendtime);
                    m_initialsendtime = sconfig.GetInt("DelayBeforeInitialAppearanceSend", m_initialsendtime);
                    // MainConsole.Instance.InfoFormat("[AVFACTORY] configured for {0} save and {1} send",m_savetime,m_sendtime);
                }
            }
        }

        public void AddRegion(IScene scene)
        {
            if (m_scene == null)
                m_scene = scene;

            scene.RegisterModuleInterface<IAvatarFactory>(this);
            scene.EventManager.OnNewClient += NewClient;
            scene.EventManager.OnClosingClient += RemoveClient;
            scene.EventManager.OnNewPresence += EventManager_OnNewPresence;
            scene.EventManager.OnRemovePresence += EventManager_OnRemovePresence;

            if (MainConsole.Instance != null)
                MainConsole.Instance.Commands.AddCommand("force send appearance", "force send appearance",
                                                         "Force send the avatar's appearance",
                                                         HandleConsoleForceSendAppearance);

            _saveQueue.Start(m_savetime, HandleAppearanceSave);
            _sendQueue.Start(m_sendtime, HandleAppearanceSend);
            _initialSendQueue.Start(m_initialsendtime, HandleInitialAppearanceSend);
        }

        public void RemoveRegion(IScene scene)
        {
            scene.UnregisterModuleInterface<IAvatarFactory>(this);
            scene.EventManager.OnNewClient -= NewClient;
            scene.EventManager.OnClosingClient -= RemoveClient;
            scene.EventManager.OnNewPresence -= EventManager_OnNewPresence;
            scene.EventManager.OnRemovePresence -= EventManager_OnRemovePresence;
        }

        public void RegionLoaded(IScene scene)
        {
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "Default Avatar Factory"; }
        }

        #endregion

        public void NewClient(IClientAPI client)
        {
            client.OnRequestWearables += RequestWearables;
            client.OnSetAppearance += SetAppearance;
            client.OnAvatarNowWearing += AvatarIsWearing;
            client.OnAgentCachedTextureRequest += AgentCachedTexturesRequest;
        }

        public void RemoveClient(IClientAPI client)
        {
            client.OnRequestWearables -= RequestWearables;
            client.OnSetAppearance -= SetAppearance;
            client.OnAvatarNowWearing -= AvatarIsWearing;
            client.OnAgentCachedTextureRequest -= AgentCachedTexturesRequest;
        }

        private void EventManager_OnNewPresence(IScenePresence presence)
        {
            AvatarApperanceModule m = new AvatarApperanceModule(presence);
            presence.RegisterModuleInterface<IAvatarAppearanceModule>(m);
        }

        private void EventManager_OnRemovePresence(IScenePresence presence)
        {
            AvatarApperanceModule m = (AvatarApperanceModule) presence.RequestModuleInterface<IAvatarAppearanceModule>();
            if (m != null)
            {
                m.Close();
                presence.UnregisterModuleInterface<IAvatarAppearanceModule>(m);
            }
        }

        #region Validate Baked Textures

        /// <summary>
        ///   Check for the existence of the baked texture assets.
        /// </summary>
        /// <param name = "client"></param>
        public bool ValidateBakedTextureCache(IClientAPI client)
        {
            return ValidateBakedTextureCache(client, true);
        }

        /// <summary>
        ///   Check for the existence of the baked texture assets. Request a rebake
        ///   unless checkonly is true.
        /// </summary>
        /// <param name = "client"></param>
        /// <param name = "checkonly"></param>
        private bool ValidateBakedTextureCache(IClientAPI client, bool checkonly)
        {
            IScenePresence sp = m_scene.GetScenePresence(client.AgentId);
            IAvatarAppearanceModule appearance = sp.RequestModuleInterface<IAvatarAppearanceModule>();

            bool defonly = true; // are we only using default textures

            // Process the texture entry
            foreach (int idx in AvatarAppearance.BAKE_INDICES)
            {
                Primitive.TextureEntryFace face = appearance.Appearance.Texture.FaceTextures[idx];

                // if there is no texture entry, skip it
                if (face == null)
                    continue;

                // if the texture is one of the "defaults" then skip it
                // this should probably be more intelligent (skirt texture doesnt matter
                // if the avatar isnt wearing a skirt) but if any of the main baked 
                // textures is default then the rest should be as well
                if (face.TextureID == UUID.Zero || face.TextureID == AppearanceManager.DEFAULT_AVATAR_TEXTURE)
                    continue;

                defonly = false; // found a non-default texture reference

                if (!CheckBakedTextureAsset(client, face.TextureID, idx))
                {
                    // the asset didn't exist if we are only checking, then we found a bad
                    // one and we're done otherwise, ask for a rebake
                    if (checkonly) return false;

                    MainConsole.Instance.InfoFormat("[AvatarFactory] missing baked texture {0}, request rebake", face.TextureID);
                    client.SendRebakeAvatarTextures(face.TextureID);
                }
            }

            // If we only found default textures, then the appearance is not cached
            return (!defonly);
        }

        #endregion

        #region Set Appearance

        /// <summary>
        ///   Set appearance data (textureentry and slider settings) received from the client
        /// </summary>
        /// <param name = "textureEntry"></param>
        /// <param name = "visualParams"></param>
        /// <param name="client"></param>
        /// <param name="wearables"></param>
        public void SetAppearance(IClientAPI client, Primitive.TextureEntry textureEntry, byte[] visualParams,
                                  WearableCache[] wearables, uint serial)
        {
            IScenePresence sp = m_scene.GetScenePresence(client.AgentId);
            IAvatarAppearanceModule appearance = sp.RequestModuleInterface<IAvatarAppearanceModule>();

            appearance.Appearance.Serial = (int)serial;
            //MainConsole.Instance.InfoFormat("[AVFACTORY]: start SetAppearance for {0}", client.AgentId);

            // Process the texture entry transactionally, this doesn't guarantee that Appearance is
            // going to be handled correctly but it does serialize the updates to the appearance
            lock (m_setAppearanceLock)
            {
                bool texturesChanged = false;
                bool visualParamsChanged = false;

                if (textureEntry != null)
                {
                    List<UUID> ChangedTextures = new List<UUID>();
                    texturesChanged = appearance.Appearance.SetTextureEntries(textureEntry, out ChangedTextures);

                    // MainConsole.Instance.WarnFormat("[AVFACTORY]: Prepare to check textures for {0}",client.AgentId);

                    //Do this async as it accesses the asset service (could be remote) multiple times
                    Util.FireAndForget(delegate
                                           {
                                               //Validate all the textures now that we've updated
                                               ValidateBakedTextureCache(client, false);
                                               //The client wants us to cache the baked textures
                                               CacheWearableData(sp, textureEntry, wearables);
                                           });

                    // MainConsole.Instance.WarnFormat("[AVFACTORY]: Complete texture check for {0}",client.AgentId);
                }
                // Process the visual params, this may change height as well
                if (visualParams != null)
                {
                    //Now update the visual params and see if they have changed
                    visualParamsChanged = appearance.Appearance.SetVisualParams(visualParams);

                    //Fix the height only if the parameters have changed
                    if (visualParamsChanged && appearance.Appearance.AvatarHeight > 0)
                        sp.SetHeight(appearance.Appearance.AvatarHeight);
                }

                // If something changed in the appearance then queue an appearance save
                if (texturesChanged || visualParamsChanged)
                    QueueAppearanceSave(client.AgentId);

            }
            // And always queue up an appearance update to send out
            QueueAppearanceSend(client.AgentId);

            // MainConsole.Instance.WarnFormat("[AVFACTORY]: complete SetAppearance for {0}:\n{1}",client.AgentId,sp.Appearance.ToString());
        }

        /// <summary>
        ///   Tell the Avatar Service about these baked textures and items
        /// </summary>
        /// <param name = "sp"></param>
        /// <param name = "textureEntry"></param>
        /// <param name = "wearables"></param>
        private void CacheWearableData(IScenePresence sp, Primitive.TextureEntry textureEntry, WearableCache[] wearables)
        {
            /*if (textureEntry == null || wearables.Length == 0)
                return;

            AvatarWearable cachedWearable = new AvatarWearable {MaxItems = 0};
            //Unlimited items
#if (!ISWIN)
            foreach (WearableCache item in wearables)
            {
                if (textureEntry.FaceTextures[item.TextureIndex] != null)
                {
                    cachedWearable.Add(item.CacheID, textureEntry.FaceTextures[item.TextureIndex].TextureID);
                }
            }
#else
            foreach (WearableCache item in wearables.Where(item => textureEntry.FaceTextures[item.TextureIndex] != null))
            {
                cachedWearable.Add(item.CacheID, textureEntry.FaceTextures[item.TextureIndex].TextureID);
            }
#endif
            m_scene.AvatarService.CacheWearableData(sp.UUID, cachedWearable);*/
        }

        /// <summary>
        ///   The client wants to know whether we already have baked textures for the given items
        /// </summary>
        /// <param name = "client"></param>
        /// <param name = "args"></param>
        public void AgentCachedTexturesRequest(IClientAPI client, List<CachedAgentArgs> args)
        {
            List<CachedAgentArgs> resp = (from arg in args let cachedID = UUID.Zero select new CachedAgentArgs {ID = cachedID, TextureIndex = arg.TextureIndex}).ToList();

            //AvatarData ad = m_scene.AvatarService.GetAvatar(client.AgentId);
            //Send all with UUID zero for now so that we don't confuse the client about baked textures...

            client.SendAgentCachedTexture(resp);
        }

        /// <summary>
        ///   Checks for the existance of a baked texture asset and
        ///   requests the viewer rebake if the asset is not found
        /// </summary>
        /// <param name = "client"></param>
        /// <param name = "textureID"></param>
        /// <param name = "idx"></param>
        private bool CheckBakedTextureAsset(IClientAPI client, UUID textureID, int idx)
        {
            if (m_scene.AssetService.Get(textureID.ToString()) == null)
            {
                MainConsole.Instance.WarnFormat("[AvatarFactory]: Missing baked texture {0} ({1}) for avatar {2}",
                                 textureID, idx, client.Name);
                return false;
            }
            return true;
        }

        #endregion

        #region UpdateAppearanceTimer

        /// <summary>
        ///   Queue up a request to send appearance, makes it possible to
        ///   accumulate changes without sending out each one separately.
        /// </summary>
        public void QueueAppearanceSend(UUID agentid)
        {
            // MainConsole.Instance.WarnFormat("[AVFACTORY]: Queue appearance send for {0}", agentid);

            // 10000 ticks per millisecond, 1000 milliseconds per second
            _sendQueue.Add(agentid);
        }

        public void QueueAppearanceSave(UUID agentid)
        {
            // MainConsole.Instance.WarnFormat("[AVFACTORY]: Queue appearance save for {0}", agentid);

            // 10000 ticks per millisecond, 1000 milliseconds per second
            IScenePresence sp = m_scene.GetScenePresence(agentid);
            if (sp == null)
            {
                MainConsole.Instance.WarnFormat("[AvatarFactory]: Agent {0} no longer in the scene", agentid);
                return;
            }
            IAvatarAppearanceModule appearance = sp.RequestModuleInterface<IAvatarAppearanceModule>();
            _saveQueue.Add(agentid, appearance.Appearance);
        }

        public void QueueInitialAppearanceSend(UUID agentid)
        {
            // MainConsole.Instance.WarnFormat("[AVFACTORY]: Queue initial appearance send for {0}", agentid);

            // 10000 ticks per millisecond, 1000 milliseconds per second
            _initialSendQueue.Add(agentid);
        }

        /// <summary>
        /// Sends an avatars appearance (only called by the TimeSender)
        /// </summary>
        /// <param name="agentid"></param>
        /// <param name="app">ALWAYS NULL</param>
        private void HandleAppearanceSend(UUID agentid, AvatarAppearance app)
        {
            IScenePresence sp = m_scene.GetScenePresence(agentid);
            if (sp == null)
            {
                MainConsole.Instance.WarnFormat("[AvatarFactory]: Agent {0} no longer in the scene to send appearance for.", agentid);
                return;
            }
            IAvatarAppearanceModule appearance = sp.RequestModuleInterface<IAvatarAppearanceModule>();

            // MainConsole.Instance.WarnFormat("[AvatarFactory]: Handle appearance send for {0}", agentid);

            // Send the appearance to everyone in the scene
            appearance.SendAppearanceToAllOtherAgents();

            // Send animations back to the avatar as well
            sp.Animator.SendAnimPack();
        }

        /// <summary>
        /// Saves a user's appearance
        /// </summary>
        /// <param name="agentid"></param>
        /// <param name="app"></param>
        private void HandleAppearanceSave(UUID agentid, AvatarAppearance app)
        {
            //If the avatar changes appearance, then proptly logs out, this will break!
            //ScenePresence sp = m_scene.GetScenePresence(agentid);
            //if (sp == null)
            //{
            //    MainConsole.Instance.WarnFormat("[AvatarFactory]: Agent {0} no longer in the scene", agentid);
            //    return;
            //}

            // MainConsole.Instance.WarnFormat("[AvatarFactory] avatar {0} save appearance",agentid);

            IScenePresence sp = m_scene.GetScenePresence(agentid);
            if (sp == null)
                return;

            AvatarAppearance appearance = sp != null
                                                ? sp.RequestModuleInterface<IAvatarAppearanceModule>().Appearance
                                                : app;

            m_scene.AvatarService.SetAppearance(agentid, appearance);
        }

        /// <summary>
        ///   Do everything required once a client completes its movement into a region and becomes
        ///   a root agent.
        /// </summary>
        /// <param name="agentid">Agent to send appearance for</param>
        /// <param name="app">ALWAYS NULL</param>
        private void HandleInitialAppearanceSend(UUID agentid, AvatarAppearance app)
        {
            IScenePresence sp = m_scene.GetScenePresence(agentid);
            if (sp == null)
            {
                MainConsole.Instance.WarnFormat("[AvatarFactory]: Agent {0} no longer in the scene to send appearance for.", agentid);
                return;
            }
            IAvatarAppearanceModule appearance = sp.RequestModuleInterface<IAvatarAppearanceModule>();

            //MainConsole.Instance.InfoFormat("[AvatarFactory]: Handle initial appearance send for {0}", agentid);

            //Only set this if we actually have sent the wearables
            appearance.InitialHasWearablesBeenSent = true;

            // This agent just became root. We are going to tell everyone about it.
            appearance.SendAvatarDataToAllAgents(true);
            if (ValidateBakedTextureCache(sp.ControllingClient))
                appearance.SendAppearanceToAgent(sp);
            else
                MainConsole.Instance.ErrorFormat("[AvatarFactory]: baked textures are NOT in the cache for {0}", sp.Name);

            sp.ControllingClient.SendWearables(appearance.Appearance.Wearables, appearance.Appearance.Serial);

            // If the avatars baked textures are all in the cache, then we have a 
            // complete appearance... send it out, if not, then we'll send it when
            // the avatar finishes updating its appearance
            appearance.SendAppearanceToAllOtherAgents();

            // This agent just became root. We are going to tell everyone about it. The process of
            // getting other avatars information was initiated in the constructor... don't do it 
            // again here... 
            appearance.SendAvatarDataToAllAgents(true);

            //Tell us about everyone else as well now that we are here
            appearance.SendOtherAgentsAppearanceToMe();
        }

        #endregion

        #region Wearables

        //private Dictionary<UUID, UUID> incomingLinks = new Dictionary<UUID, UUID>();

        public void NewAppearanceLink(InventoryItemBase item)
        {
            /*if (item.InvType == (int)InventoryType.Wearable)
            {
                incomingLinks[item.AssetID] = item.ID;
            }*/
        }

        /// <summary>
        ///   Tell the client for this scene presence what items it should be wearing now
        /// </summary>
        public void RequestWearables(IClientAPI client)
        {
            IScenePresence sp = m_scene.GetScenePresence(client.AgentId);
            if (sp == null)
            {
                MainConsole.Instance.WarnFormat("[AvatarFactory]: SendWearables unable to find presence for {0}", client.AgentId);
                return;
            }

            //Don't send the wearables immediately, make sure that everything is loaded first
            QueueInitialAppearanceSend(client.AgentId);
            //client.SendWearables(sp.Appearance.Wearables, sp.Appearance.Serial);
        }

        /// <summary>
        ///   Update what the avatar is wearing using an item from their inventory.
        /// </summary>
        /// <param name = "client"></param>
        /// <param name = "e"></param>
        public void AvatarIsWearing(IClientAPI client, AvatarWearingArgs e)
        {
            IScenePresence sp = m_scene.GetScenePresence(client.AgentId);
            if (sp == null)
            {
                MainConsole.Instance.WarnFormat("[AvatarFactory]: AvatarIsWearing unable to find presence for {0}", client.AgentId);
                return;
            }

            MainConsole.Instance.DebugFormat("[AvatarFactory]: AvatarIsWearing called for {0}", client.AgentId);

            // operate on a copy of the appearance so we don't have to lock anything
            IAvatarAppearanceModule appearance = sp.RequestModuleInterface<IAvatarAppearanceModule>();
            AvatarAppearance avatAppearance = new AvatarAppearance(appearance.Appearance, false);

            IOpenRegionSettingsModule module = m_scene.RequestModuleInterface<IOpenRegionSettingsModule>();

            bool NeedsRebake = false;
            if (module != null && module.EnableTeenMode)
            {
                foreach (AvatarWearingArgs.Wearable wear in e.NowWearing)
                {
                    if (wear.Type == 10 & wear.ItemID == UUID.Zero && module.DefaultUnderpants != UUID.Zero)
                    {
                        NeedsRebake = true;
                        wear.ItemID = module.DefaultUnderpants;
                        InventoryItemBase item = new InventoryItemBase(UUID.Random())
                                                     {
                                                         InvType = (int) InventoryType.Wearable,
                                                         AssetType = (int) AssetType.Clothing,
                                                         Name = "Default Underpants",
                                                         Folder =
                                                             m_scene.InventoryService.GetFolderForType(client.AgentId,
                                                                                                       InventoryType.
                                                                                                           Wearable,
                                                                                                       AssetType.
                                                                                                           Clothing).ID,
                                                         Owner = client.AgentId,
                                                         CurrentPermissions = 0,
                                                         CreatorId = UUID.Zero.ToString(),
                                                         AssetID = module.DefaultUnderpants
                                                     };
                        //Locked
                        client.SendInventoryItemCreateUpdate(item, 0);
                    }
                    else if (wear.Type == 10 & wear.ItemID == UUID.Zero)
                    {
                        NeedsRebake = true;
                        InventoryItemBase item = new InventoryItemBase(UUID.Random())
                                                     {
                                                         InvType = (int) InventoryType.Wearable,
                                                         AssetType = (int) AssetType.Clothing,
                                                         Name = "Default Underpants",
                                                         Folder =
                                                             m_scene.InventoryService.GetFolderForType(client.AgentId,
                                                                                                       InventoryType.
                                                                                                           Wearable,
                                                                                                       AssetType.
                                                                                                           Clothing).ID,
                                                         Owner = client.AgentId,
                                                         CurrentPermissions = 0
                                                     };
                        //Locked
                        if (m_underPantsUUID == UUID.Zero)
                        {
                            m_underPantsUUID = UUID.Random();
                            AssetBase asset = new AssetBase(m_underPantsUUID, "Default Underpants", AssetType.Clothing,
                                                            UUID.Zero) {Data = Utils.StringToBytes(m_defaultUnderPants)};
                            asset.FillHash();
                            asset.ID = m_scene.AssetService.Store(asset);
                            m_underPantsUUID = asset.ID;
                        }
                        item.CreatorId = UUID.Zero.ToString();
                        item.AssetID = m_underPantsUUID;
                        m_scene.InventoryService.AddItemAsync(item, null);
                        client.SendInventoryItemCreateUpdate(item, 0);
                        wear.ItemID = item.ID;
                    }
                    if (wear.Type == 11 && wear.ItemID == UUID.Zero && module.DefaultUndershirt != UUID.Zero)
                    {
                        NeedsRebake = true;
                        wear.ItemID = module.DefaultUndershirt;
                        InventoryItemBase item = new InventoryItemBase(UUID.Random())
                                                     {
                                                         InvType = (int) InventoryType.Wearable,
                                                         AssetType = (int) AssetType.Clothing,
                                                         Name = "Default Undershirt",
                                                         Folder =
                                                             m_scene.InventoryService.GetFolderForType(client.AgentId,
                                                                                                       InventoryType.
                                                                                                           Wearable,
                                                                                                       AssetType.
                                                                                                           Clothing).ID,
                                                         Owner = client.AgentId,
                                                         CurrentPermissions = 0,
                                                         CreatorId = UUID.Zero.ToString(),
                                                         AssetID = module.DefaultUndershirt
                                                     };
                        //Locked
                        client.SendInventoryItemCreateUpdate(item, 0);
                    }
                    else if (wear.Type == 11 & wear.ItemID == UUID.Zero)
                    {
                        NeedsRebake = true;
                        InventoryItemBase item = new InventoryItemBase(UUID.Random())
                                                     {
                                                         InvType = (int) InventoryType.Wearable,
                                                         AssetType = (int) AssetType.Clothing,
                                                         Name = "Default Undershirt",
                                                         Folder =
                                                             m_scene.InventoryService.GetFolderForType(client.AgentId,
                                                                                                       InventoryType.
                                                                                                           Wearable,
                                                                                                       AssetType.
                                                                                                           Clothing).ID,
                                                         Owner = client.AgentId,
                                                         CurrentPermissions = 0
                                                     };
                        //Locked
                        if (m_underShirtUUID == UUID.Zero)
                        {
                            m_underShirtUUID = UUID.Random();
                            AssetBase asset = new AssetBase(m_underShirtUUID, "Default Undershirt", AssetType.Clothing,
                                                            UUID.Zero) {Data = Utils.StringToBytes(m_defaultUnderShirt)};
                            asset.FillHash();
                            asset.ID = m_scene.AssetService.Store(asset);
                            m_underShirtUUID = asset.ID;
                        }
                        item.CreatorId = UUID.Zero.ToString();
                        item.AssetID = m_underShirtUUID;
                        m_scene.InventoryService.AddItemAsync(item, null);
                        client.SendInventoryItemCreateUpdate(item, 0);
                        wear.ItemID = item.ID;
                    }
                }
            }

            foreach (AvatarWearingArgs.Wearable wear in e.NowWearing.Where(wear => wear.Type < AvatarWearable.MAX_WEARABLES))
            {
                /*if (incomingLinks.ContainsKey (wear.ItemID))
                {
                    wear.ItemID = incomingLinks[wear.ItemID];
                }*/
                avatAppearance.Wearables[wear.Type].Add(wear.ItemID, UUID.Zero);
            }

            avatAppearance.GetAssetsFrom(appearance.Appearance);

            // This could take awhile since it needs to pull inventory
            SetAppearanceAssets(sp.UUID, e.NowWearing, appearance.Appearance, ref avatAppearance);

            // could get fancier with the locks here, but in the spirit of "last write wins"
            // this should work correctly, also, we don't need to send the appearance here
            // since the "iswearing" will trigger a new set of visual param and baked texture changes
            // when those complete, the new appearance will be sent
            appearance.Appearance = avatAppearance;
            if (NeedsRebake)
            {
                //Tell the client about the new things it is wearing
                sp.ControllingClient.SendWearables(appearance.Appearance.Wearables, appearance.Appearance.Serial);
                //Then forcefully tell it to rebake
#if (!ISWIN)
                foreach (Primitive.TextureEntryFace t in appearance.Appearance.Texture.FaceTextures)
                {
                    Primitive.TextureEntryFace face = (t);
                    if (face != null)
                    {
                        sp.ControllingClient.SendRebakeAvatarTextures(face.TextureID);
                    }
                }
#else
                foreach (Primitive.TextureEntryFace face in appearance.Appearance.Texture.FaceTextures.Select(t => (t)).Where(face => face != null))
                {
                    sp.ControllingClient.SendRebakeAvatarTextures(face.TextureID);
                }
#endif
            }
            QueueAppearanceSave(sp.UUID);
            //Send the wearables HERE so that the client knows what it is wearing
            //sp.ControllingClient.SendWearables(sp.Appearance.Wearables, sp.Appearance.Serial);
            //Do not save or send the appearance! The client loops back and sends a bunch of SetAppearance
            //  (handled above) and that takes care of it
        }

        private void SetAppearanceAssets(UUID userID, List<AvatarWearingArgs.Wearable> nowWearing, AvatarAppearance oldAppearance, ref AvatarAppearance appearance)
        {
            IInventoryService invService = m_scene.InventoryService;

            for (int i = 0; i < AvatarWearable.MAX_WEARABLES; i++)
            {
                for (int j = 0; j < appearance.Wearables[j].Count; j++)
                {
                    if (appearance.Wearables[i][j].ItemID == UUID.Zero)
                        continue;

                    // Ignore ruth's assets
                    if (appearance.Wearables[i][j].ItemID == AvatarWearable.DefaultWearables[i][j].ItemID)
                    {
                        //MainConsole.Instance.ErrorFormat(
                        //    "[AvatarFactory]: Found an asset for the default avatar, itemID {0}, wearable {1}, asset {2}" +
                        //    ", setting to default asset {3}.",
                        //    appearance.Wearables[i][j].ItemID, (WearableType)i, appearance.Wearables[i][j].AssetID,
                        //    AvatarWearable.DefaultWearables[i][j].AssetID);
                        appearance.Wearables[i].Add(appearance.Wearables[i][j].ItemID,
                                                    appearance.Wearables[i][j].AssetID);
                        continue;
                    }

                    if (nowWearing[i].ItemID == oldAppearance.Wearables[i][j].ItemID)
                        continue;//Don't relookup items that are the same and have already been found earlier

                    InventoryItemBase baseItem = new InventoryItemBase(appearance.Wearables[i][j].ItemID, userID);
                    baseItem = invService.GetItem(baseItem);

                    if (baseItem != null)
                    {
                        if (baseItem.AssetType == (int) AssetType.Link)
                        {
                            baseItem = new InventoryItemBase(baseItem.AssetID, userID);
                            baseItem = invService.GetItem(baseItem);
                        }
                        appearance.Wearables[i].Add(baseItem.ID, baseItem.AssetID);
                    }
                    else
                    {
                        MainConsole.Instance.ErrorFormat(
                            "[AvatarFactory]: Can't find inventory item {0} for {1}, setting to default",
                            appearance.Wearables[i][j].ItemID, (WearableType) i);

                        appearance.Wearables[i].RemoveItem(appearance.Wearables[i][j].ItemID);
                        appearance.Wearables[i].Add(AvatarWearable.DefaultWearables[i][j].ItemID,
                                                    AvatarWearable.DefaultWearables[i][j].AssetID);
                    }
                }
            }
        }

        #endregion

        #region Console Commands

        public void ForceSendAvatarAppearance(UUID agentid)
        {
            //If the avatar changes appearance, then proptly logs out, this will break!
            IScenePresence sp = m_scene.GetScenePresence(agentid);
            if (sp == null || sp.IsChildAgent)
            {
                MainConsole.Instance.WarnFormat("[AvatarFactory]: Agent {0} no longer in the scene", agentid);
                return;
            }

            //Force send!
            IAvatarAppearanceModule appearance = sp.RequestModuleInterface<IAvatarAppearanceModule>();
            sp.ControllingClient.SendWearables(appearance.Appearance.Wearables, appearance.Appearance.Serial);
            Thread.Sleep(100);
            appearance.SendAvatarDataToAllAgents(true);
            Thread.Sleep(100);
            appearance.SendAppearanceToAgent(sp);
            Thread.Sleep(100);
            appearance.SendAppearanceToAllOtherAgents();
            MainConsole.Instance.Info("Resent appearance");
        }

        private void HandleConsoleForceSendAppearance(string[] cmds)
        {
            //Make sure its set to the right region
            if (MainConsole.Instance.ConsoleScene != m_scene && MainConsole.Instance.ConsoleScene != null)
                return;

            if (cmds.Length != 5)
            {
                if(MainConsole.Instance.ConsoleScene != null)
                    MainConsole.Instance.Info("Wrong number of commands.");
                return;
            }
            string firstName = cmds[3], lastName = cmds[4];

            IScenePresence SP;
            if (m_scene.TryGetAvatarByName(firstName + " " + lastName, out SP))
            {
                ForceSendAvatarAppearance(SP.UUID);
            }
            else
                MainConsole.Instance.Info("Could not find user's account.");
        }

        #endregion

        #region Nested type: AvatarApperanceModule

        public class AvatarApperanceModule : IAvatarAppearanceModule
        {
            private bool m_InitialHasWearablesBeenSent;
            protected AvatarAppearance m_appearance;
            public IScenePresence m_sp;

            public AvatarApperanceModule(IScenePresence sp)
            {
                m_sp = sp;
                m_sp.Scene.EventManager.OnMakeRootAgent += EventManager_OnMakeRootAgent;
            }

            #region IAvatarAppearanceModule Members

            public bool InitialHasWearablesBeenSent
            {
                get { return m_InitialHasWearablesBeenSent; }
                set { m_InitialHasWearablesBeenSent = value; }
            }

            /// <summary>
            ///   Send this agent's avatar data to all other root and child agents in the scene
            ///   This agent must be root. This avatar will receive its own update.
            /// </summary>
            public void SendAvatarDataToAllAgents(bool sendAppearance)
            {
                // only send update from root agents to other clients; children are only "listening posts"
                if (m_sp.IsChildAgent)
                {
                    MainConsole.Instance.Warn("[SCENEPRESENCE] attempt to send avatar data from a child agent");
                    return;
                }

                int count = 0;
                m_sp.Scene.ForEachScenePresence(delegate(IScenePresence scenePresence)
                                                    {
                                                        SendAvatarDataToAgent(scenePresence, sendAppearance);
                                                        count++;
                                                    });

                IAgentUpdateMonitor reporter =
                    (IAgentUpdateMonitor)
                    m_sp.Scene.RequestModuleInterface<IMonitorModule>().GetMonitor(
                        m_sp.Scene.RegionInfo.RegionID.ToString(), MonitorModuleHelper.AgentUpdateCount);
                if (reporter != null)
                {
                    reporter.AddAgentUpdates(count);
                }
            }

            /// <summary>
            ///   Send avatar data to an agent.
            /// </summary>
            /// <param name = "avatar"></param>
            /// <param name="sendAppearance"></param>
            public void SendAvatarDataToAgent(IScenePresence avatar, bool sendAppearance)
            {
                //MainConsole.Instance.WarnFormat("[SP] Send avatar data from {0} to {1}",m_uuid,avatar.ControllingClient.AgentId);
                if (!sendAppearance)
                    avatar.SceneViewer.SendPresenceFullUpdate(m_sp);
                else
                    avatar.SceneViewer.QueuePresenceForFullUpdate(m_sp, false);
            }

            /// <summary>
            ///   Send this agent's appearance to all other root and child agents in the scene
            ///   This agent must be root.
            /// </summary>
            public void SendAppearanceToAllOtherAgents()
            {
                // only send update from root agents to other clients; children are only "listening posts"
                if (m_sp.IsChildAgent)
                {
                    MainConsole.Instance.Warn("[SCENEPRESENCE] attempt to send avatar data from a child agent");
                    return;
                }

                int count = 0;
                m_sp.Scene.ForEachScenePresence(delegate(IScenePresence scenePresence)
                                                    {
                                                        if (scenePresence.UUID == m_sp.UUID)
                                                            return;

                                                        SendAppearanceToAgent(scenePresence);
                                                        count++;
                                                    });

                IAgentUpdateMonitor reporter =
                    (IAgentUpdateMonitor)
                    m_sp.Scene.RequestModuleInterface<IMonitorModule>().GetMonitor(
                        m_sp.Scene.RegionInfo.RegionID.ToString(), MonitorModuleHelper.AgentUpdateCount);
                if (reporter != null)
                {
                    reporter.AddAgentUpdates(count);
                }
            }

            /// <summary>
            ///   Send appearance from all other root agents to this agent. this agent
            ///   can be either root or child
            /// </summary>
            public void SendOtherAgentsAppearanceToMe()
            {
                int count = 0;
                m_sp.Scene.ForEachScenePresence(delegate(IScenePresence scenePresence)
                                                    {
                                                        // only send information about root agents
                                                        if (scenePresence.IsChildAgent)
                                                            return;

                                                        // only send information about other root agents
                                                        if (scenePresence.UUID == m_sp.UUID)
                                                            return;

                                                        IAvatarAppearanceModule appearance =
                                                            scenePresence.RequestModuleInterface
                                                                <IAvatarAppearanceModule>();
                                                        if (appearance != null)
                                                            appearance.SendAppearanceToAgent(m_sp);
                                                        count++;
                                                    });

                IAgentUpdateMonitor reporter =
                    (IAgentUpdateMonitor)
                    m_sp.Scene.RequestModuleInterface<IMonitorModule>().GetMonitor(
                        m_sp.Scene.RegionInfo.RegionID.ToString(), MonitorModuleHelper.AgentUpdateCount);
                if (reporter != null)
                {
                    reporter.AddAgentUpdates(count);
                }
            }

            /// <summary>
            ///   Send appearance data to an agent.
            /// </summary>
            /// <param name = "avatar"></param>
            public void SendAppearanceToAgent(IScenePresence avatar)
            {
                avatar.ControllingClient.SendAppearance(
                    Appearance.Owner, Appearance.VisualParams, Appearance.Texture.GetBytes());
            }

            public AvatarAppearance Appearance
            {
                get { return m_appearance; }
                set { m_appearance = value; }
            }

            #endregion

            public void Close()
            {
                m_sp.Scene.EventManager.OnMakeRootAgent -= EventManager_OnMakeRootAgent;
                m_sp = null;
            }

            private void EventManager_OnMakeRootAgent(IScenePresence presence)
            {
                if (m_sp != null && presence.UUID == m_sp.UUID)
                {
                    //Send everyone to me!
                    SendOtherAgentsAvatarDataToMe();
                    //Check to make sure that we have sent all the appearance info 10 seconds later
                    Timer t = new Timer(10*1000);
                    t.Elapsed += CheckToMakeSureWearablesHaveBeenSent;
                    t.AutoReset = false;
                    t.Start();
                }
            }

            /// <summary>
            ///   Send avatar data for all other root agents to this agent, this agent
            ///   can be either a child or root
            /// </summary>
            public void SendOtherAgentsAvatarDataToMe()
            {
                int count = 0;
                m_sp.Scene.ForEachScenePresence(delegate(IScenePresence scenePresence)
                                                    {
                                                        // only send information about root agents
                                                        if (scenePresence.IsChildAgent)
                                                            return;

                                                        // only send information about other root agents
                                                        if (scenePresence.UUID == m_sp.UUID)
                                                            return;

                                                        IAvatarAppearanceModule appearance =
                                                            scenePresence.RequestModuleInterface
                                                                <IAvatarAppearanceModule>();
                                                        if (appearance != null)
                                                            appearance.SendAvatarDataToAgent(m_sp, true);
                                                        count++;
                                                    });

                IAgentUpdateMonitor reporter =
                    (IAgentUpdateMonitor)
                    m_sp.Scene.RequestModuleInterface<IMonitorModule>().GetMonitor(
                        m_sp.Scene.RegionInfo.RegionID.ToString(), MonitorModuleHelper.AgentUpdateCount);
                if (reporter != null)
                {
                    reporter.AddAgentUpdates(count);
                }
            }

            /// <summary>
            ///   This makes sure that after the agent has entered the sim that they have their clothes and that they all exist
            /// </summary>
            /// <param name = "sender"></param>
            /// <param name = "e"></param>
            private void CheckToMakeSureWearablesHaveBeenSent(object sender, ElapsedEventArgs e)
            {
                if (m_sp == null)
                    return;
                if (!m_InitialHasWearablesBeenSent)
                {
                    //Force send!
                    m_InitialHasWearablesBeenSent = true;
                    MainConsole.Instance.Warn("[AvatarAppearanceModule]: Been 10 seconds since root agent " + m_sp.Name +
                               " was added and appearance was not sent, force sending now.");

                    m_sp.ControllingClient.SendWearables(Appearance.Wearables, Appearance.Serial);

                    //Send rebakes if needed
                    // NOTE: Do NOT send this! It seems to make the client become a cloud
                    //sp.SendAppearanceToAgent(sp);

                    // If the avatars baked textures are all in the cache, then we have a 
                    // complete appearance... send it out, if not, then we'll send it when
                    // the avatar finishes updating its appearance
                    SendAppearanceToAllOtherAgents();

                    // This agent just became roo t. We are going to tell everyone about it. The process of
                    // getting other avatars information was initiated in the constructor... don't do it 
                    // again here... 
                    SendAvatarDataToAllAgents(true);

                    //Tell us about everyone else as well now that we are here
                    SendOtherAgentsAppearanceToMe();
                }
            }
        }

        #endregion
    }
}