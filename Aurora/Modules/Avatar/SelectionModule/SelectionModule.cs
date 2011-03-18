using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using OpenMetaverse.Packets;
using log4net;
using Nini.Config;

namespace Aurora.Modules
{
    public class SelectionModule : ISharedRegionModule
    {
        #region Declares

        private static readonly ILog m_log
            = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private bool m_UseSelectionParticles = true;

        public bool UseSelectionParticles
        {
            get { return m_UseSelectionParticles; }
        }

        #endregion

        #region ISharedRegionModule Members

        public void Initialise(IConfigSource source)
        {
            IConfig aurorastartupConfig = source.Configs["AuroraStartup"];
            if (aurorastartupConfig != null)
            {
                m_UseSelectionParticles = aurorastartupConfig.GetBoolean("UseSelectionParticles", true);
            }
        }

        public void PostInitialise()
        {
        }

        public void AddRegion(Scene scene)
        {
            scene.EventManager.OnNewClient += EventManager_OnNewClient;
            scene.EventManager.OnClosingClient += EventManager_OnClosingClient;
            scene.EventManager.OnNewPresence += EventManager_OnNewPresence;
            scene.EventManager.OnRemovePresence += EventManager_OnRemovePresence;
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
            scene.EventManager.OnNewClient -= EventManager_OnNewClient;
            scene.EventManager.OnClosingClient -= EventManager_OnClosingClient;
            scene.EventManager.OnNewPresence -= EventManager_OnNewPresence;
            scene.EventManager.OnRemovePresence -= EventManager_OnRemovePresence;
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "SelectionModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        #region Selection Events

        protected void EventManager_OnNewClient(IClientAPI client)
        {
            client.OnObjectRequest += RequestPrim;
            client.OnObjectSelect += SelectPrim;
            client.OnObjectDeselect += DeselectPrim;
            client.OnViewerEffect += ProcessViewerEffect;
        }

        protected void EventManager_OnClosingClient(IClientAPI client)
        {
            client.OnObjectRequest -= RequestPrim;
            client.OnObjectSelect -= SelectPrim;
            client.OnObjectDeselect -= DeselectPrim;
            client.OnViewerEffect -= ProcessViewerEffect;
        }

        protected void EventManager_OnNewPresence (IScenePresence presence)
        {
            presence.RegisterModuleInterface<PerClientSelectionParticles>(new PerClientSelectionParticles(presence, this));
        }

        protected void EventManager_OnRemovePresence (IScenePresence presence)
        {
            PerClientSelectionParticles particles = presence.RequestModuleInterface<PerClientSelectionParticles>();
            if (particles != null)
            {
                particles.Close();
                presence.UnregisterModuleInterface<PerClientSelectionParticles>(particles);
            }
        }

        /// <summary>
        /// Invoked when the client requests a prim.
        /// </summary>
        /// <param name="primLocalID"></param>
        /// <param name="cacheMissType">0 => full object (viewer doesn't have it)
        /// 1 => CRC mismatch only</param>
        /// <param name="remoteClient"></param>
        protected void RequestPrim(uint primLocalID, byte cacheMissType, IClientAPI remoteClient)
        {
            Scene scene = ((Scene)remoteClient.Scene);
            IEntity entity;
            if (scene.Entities.TryGetChildPrimParent(primLocalID, out entity))
            {
                if (entity is SceneObjectGroup)
                {
                    IScenePresence SP = scene.GetScenePresence(remoteClient.AgentId);
                    ((SceneObjectGroup)entity).ScheduleGroupUpdateToAvatar(SP, PrimUpdateFlags.FullUpdate);
                }
            }
        }

        /// <summary>
        /// Invoked when the client selects a prim.
        /// </summary>
        /// <param name="primLocalID"></param>
        /// <param name="remoteClient"></param>
        protected void SelectPrim(List<uint> primLocalIDs, IClientAPI remoteClient)
        {
            Scene scene = ((Scene)remoteClient.Scene);
            List<IEntity> EntitiesToUpdate = new List<IEntity> ();
            SceneObjectPart prim = null;
            foreach (uint primLocalID in primLocalIDs)
            {
                ISceneChildEntity entity = null;
                if (scene.SceneGraph.TryGetPart(primLocalID, out entity))
                {
                    if (entity is SceneObjectPart)
                    {
                        prim = entity as SceneObjectPart;
                        // changed so that we send select to all the indicated prims
                        // also to root prim (done in prim.IsSelected)
                        // so "edit link parts" keep the object select and not moved by physics
                        // similar changes on deselect
                        // part.IsSelect is on SceneObjectPart.cs
                        // Ubit
                        //                        if (prim.IsRoot)
                        {
                            //                            prim.ParentGroup.IsSelected = true;
                            prim.IsSelected = true;
                            scene.AuroraEventManager.FireGenericEventHandler("ObjectSelected", prim);
                        }
                    }
                }
                IEntity entitybase;
                //Check for avies! They arn't prims!
                if (scene.SceneGraph.TryGetEntity(primLocalID, out entitybase))
                {
                    if (entitybase is IScenePresence)
                        continue;
                }
                if (entity != null)
                    EntitiesToUpdate.Add(entity);
                else
                {
                    m_log.Error("[SCENEPACKETHANDLER]: Could not find prim in SelectPrim, killing prim.");
                    //Send a kill packet to the viewer so it doesn't come up again
                    remoteClient.SendKillObject(scene.RegionInfo.RegionHandle, new uint[1] { primLocalID });
                }
            }
            if (EntitiesToUpdate.Count != 0)
                remoteClient.SendObjectPropertiesReply(EntitiesToUpdate);
            IScenePresence SP;
            scene.TryGetScenePresence(remoteClient.AgentId, out SP);
            PerClientSelectionParticles selection = SP.RequestModuleInterface<PerClientSelectionParticles>();
            if (selection != null)
            {
                selection.SelectedUUID = prim;
                selection.IsSelecting = true;
            }
        }

        /// <summary>
        /// Handle the deselection of a prim from the client.
        /// </summary>
        /// <param name="primLocalID"></param>
        /// <param name="remoteClient"></param>
        protected void DeselectPrim(uint primLocalID, IClientAPI remoteClient)
        {
            IScene scene = remoteClient.Scene;
            ISceneChildEntity part = scene.GetSceneObjectPart (primLocalID);
            //Do this first... As if its null, this wont be fired.
            IScenePresence SP;
            scene.TryGetScenePresence(remoteClient.AgentId, out SP);

            if (SP == null)
                return;

            PerClientSelectionParticles selection = SP.RequestModuleInterface<PerClientSelectionParticles>();
            if (selection != null)
            {
                selection.SelectedUUID = null;
                selection.IsSelecting = false;
            }

            if (part == null)
                return;

            // The prim is in the process of being deleted.
            if (null == part.ParentEntity.RootChild)
                return;

            // A deselect packet contains all the local prims being deselected.  However, since selection is still
            // group based we only want the root prim to trigger a full update - otherwise on objects with many prims
            // we end up sending many duplicate ObjectUpdates
            //            if (part.ParentGroup.RootPart.LocalId != part.LocalId)
            //                return;

            //            part.ParentGroup.IsSelected = false;
            part.IsSelected = false;

            if (!part.ParentEntity.IsAttachment) //This NEEDS to be done because otherwise rotationalVelocity will break! Only for the editing av as the client stops the rotation for them when they are in edit
            {
                if (part.ParentEntity.RootChild.AngularVelocity != Vector3.Zero && !part.ParentEntity.IsDeleted)
                    part.ParentEntity.ScheduleGroupUpdateToAvatar (SP, PrimUpdateFlags.FullUpdate);
            }

            scene.AuroraEventManager.FireGenericEventHandler("ObjectDeselected", part);
        }

        protected void ProcessViewerEffect(IClientAPI remoteClient, List<ViewerEffectEventHandlerArg> args)
        {
            Scene scene = ((Scene)remoteClient.Scene);
            // TODO: don't create new blocks if recycling an old packet
            ViewerEffectPacket.EffectBlock[] effectBlockArray = new ViewerEffectPacket.EffectBlock[args.Count];
            IScenePresence SP;
            scene.TryGetScenePresence(remoteClient.AgentId, out SP);
            for (int i = 0; i < args.Count; i++)
            {
                ViewerEffectPacket.EffectBlock effect = new ViewerEffectPacket.EffectBlock();
                effect.AgentID = args[i].AgentID;
                effect.Color = args[i].Color;
                effect.Duration = args[i].Duration;
                effect.ID = args[i].ID;
                effect.Type = args[i].Type;
                effect.TypeData = args[i].TypeData;
                effectBlockArray[i] = effect;
                //Save the color
                if (effect.Type == (int)EffectType.Beam || effect.Type == (int)EffectType.Point
                    || effect.Type == (int)EffectType.Sphere)
                {
                    Color4 color = new Color4(effect.Color, 0, false);
                    if (SP != null && !(color.R == 0 && color.G == 0 && color.B == 0))
                    {
                        PerClientSelectionParticles selection = SP.RequestModuleInterface<PerClientSelectionParticles>();
                        if (selection != null)
                        {
                            selection.EffectColor = args[i].Color;
                        }
                    }
                }

                foreach (IScenePresence client in scene.GetScenePresences ())
                {
                    if (client.ControllingClient.AgentId != remoteClient.AgentId)
                        client.ControllingClient.SendViewerEffect(effectBlockArray);
                }
            }
        }

        #endregion

        #region Per Frame Events

        protected class PerClientSelectionParticles
        {
            protected int SendEffectPackets = -1;
            protected IScenePresence m_presence;
            protected SelectionModule m_module;
            protected bool m_IsSelecting = false;
            protected SceneObjectPart m_SelectedUUID = null;
            protected byte[] m_EffectColor = new Color4(1, 0.01568628f, 0, 1).GetBytes();

            public PerClientSelectionParticles (IScenePresence presence, SelectionModule mod)
            {
                m_presence = presence;
                m_module = mod;
                //Hook up to onFrame so that we can send the updates
                m_presence.Scene.EventManager.OnFrame += EventManager_OnFrame;
            }

            public void Close()
            {
                m_presence.Scene.EventManager.OnFrame -= EventManager_OnFrame;
                m_SelectedUUID = null;
                m_IsSelecting = false;
                m_module = null;
                m_EffectColor = null;
                SendEffectPackets = 0;
                m_presence = null;
            }

            public SceneObjectPart SelectedUUID
            {
                get { return m_SelectedUUID; }
                set { m_SelectedUUID = value; }
            }

            public byte[] EffectColor
            {
                get { return m_EffectColor; }
                set { m_EffectColor = value; }
            }

            public bool IsSelecting
            {
                get { return m_IsSelecting; }
                set { m_IsSelecting = value; }
            }

            protected void EventManager_OnFrame()
            {
                if (m_presence == null)
                    return; //We can't deregister ourselves... our reference is lost... so just hope we stop getting called soon
                if (!m_presence.IsChildAgent && m_module.UseSelectionParticles && SendEffectPackets > 7)
                {
                    SendViewerEffects();
                    SendEffectPackets = -1;
                }
                SendEffectPackets++;
            }

            /// <summary>
            /// This sends the little particles to the client if they are selecting something or such
            /// </summary>
            protected void SendViewerEffects()
            {
                if (!IsSelecting)
                    return;

                SceneObjectPart SOP = m_SelectedUUID;
                if (SOP == null) //This IS nesessary, this is how we can clear this out
                {
                    IsSelecting = false;
                    return;
                }

                ViewerEffectPacket.EffectBlock[] effectBlockArray = new ViewerEffectPacket.EffectBlock[1];

                ViewerEffectPacket.EffectBlock effect = new ViewerEffectPacket.EffectBlock();
                effect.AgentID = m_presence.UUID;
                effect.Color = EffectColor;
                effect.Duration = 0.9f;
                effect.ID = UUID.Random(); //This seems to be what is passed by SL when its send from the server
                effect.Type = (int)EffectType.Beam; //Bean is the line from hand to object

                byte[] part = new byte[56];
                m_presence.UUID.ToBytes(part, 0);//UUID of av first
                SOP.UUID.ToBytes(part, 16);//UUID of object
                SOP.AbsolutePosition.ToBytes(part, 32);//position of object first

                effect.TypeData = part;
                effectBlockArray[0] = effect;

                m_presence.Scene.ForEachClient(
                    delegate(IClientAPI client)
                    {
                        client.SendViewerEffect(effectBlockArray);
                    }
                );
            }
        }

        #endregion
    }
}
