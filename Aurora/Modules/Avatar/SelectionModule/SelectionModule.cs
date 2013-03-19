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

using Aurora.Framework;
using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.SceneInfo.Entities;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Aurora.Modules.Selection
{
    public class SelectionModule : INonSharedRegionModule
    {
        #region Declares

        private bool m_UseSelectionParticles = true;

        public bool UseSelectionParticles
        {
            get { return m_UseSelectionParticles; }
        }

        #endregion

        #region INonSharedRegionModule Members

        public void Initialise(IConfigSource source)
        {
            IConfig aurorastartupConfig = source.Configs["AuroraStartup"];
            if (aurorastartupConfig != null)
            {
                m_UseSelectionParticles = aurorastartupConfig.GetBoolean("UseSelectionParticles", true);
            }
        }

        public void AddRegion(IScene scene)
        {
            scene.EventManager.OnNewClient += EventManager_OnNewClient;
            scene.EventManager.OnClosingClient += EventManager_OnClosingClient;
            scene.EventManager.OnNewPresence += EventManager_OnNewPresence;
            scene.EventManager.OnRemovePresence += EventManager_OnRemovePresence;
        }

        public void RegionLoaded(IScene scene)
        {
        }

        public void RemoveRegion(IScene scene)
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

        protected void EventManager_OnNewPresence(IScenePresence presence)
        {
            presence.RegisterModuleInterface(new PerClientSelectionParticles(presence, this));
        }

        protected void EventManager_OnRemovePresence(IScenePresence presence)
        {
            PerClientSelectionParticles particles = presence.RequestModuleInterface<PerClientSelectionParticles>();
            if (particles != null)
            {
                particles.Close();
                presence.UnregisterModuleInterface(particles);
            }
        }

        /// <summary>
        ///     Invoked when the client requests a prim.
        /// </summary>
        /// <param name="primLocalID"></param>
        /// <param name="cacheMissType">
        ///     0 => full object (viewer doesn't have it)
        ///     1 => CRC mismatch only
        /// </param>
        /// <param name="remoteClient"></param>
        protected void RequestPrim(uint primLocalID, byte cacheMissType, IClientAPI remoteClient)
        {
            IEntity entity;
            if (remoteClient.Scene.Entities.TryGetChildPrimParent(primLocalID, out entity))
            {
                if (entity is ISceneEntity)
                {
                    IScenePresence SP = remoteClient.Scene.GetScenePresence(remoteClient.AgentId);
                    //We send a forced because we MUST send a full update, as the client doesn't have this prim
                    ((ISceneEntity) entity).ScheduleGroupUpdateToAvatar(SP, PrimUpdateFlags.ForcedFullUpdate);
                    IObjectCache cache = remoteClient.Scene.RequestModuleInterface<IObjectCache>();
                    if (cache != null)
                        cache.RemoveObject(remoteClient.AgentId, entity.LocalId, cacheMissType);
                    MainConsole.Instance.WarnFormat("[ObjectCache]: Avatar didn't have {0}, miss type {1}, CRC {2}",
                                                    primLocalID,
                                                    cacheMissType, ((ISceneEntity) entity).RootChild.CRC);
                }
            }
        }

        /// <summary>
        ///     Invoked when the client selects a prim.
        /// </summary>
        /// <param name="primLocalIDs"></param>
        /// <param name="remoteClient"></param>
        protected void SelectPrim(List<uint> primLocalIDs, IClientAPI remoteClient)
        {
            IScene scene = remoteClient.Scene;
            List<ISceneChildEntity> EntitiesToUpdate = new List<ISceneChildEntity>();
            ISceneChildEntity prim = null;
            foreach (uint primLocalID in primLocalIDs)
            {
                ISceneChildEntity entity = null;
                if (scene.SceneGraph.TryGetPart(primLocalID, out entity))
                {
                    if (entity is ISceneChildEntity)
                    {
                        prim = entity;
                        // changed so that we send select to all the indicated prims
                        // also to root prim (done in prim.IsSelected)
                        // so "edit link parts" keep the object select and not moved by physics
                        // similar changes on deselect
                        // part.IsSelect is on SceneObjectPart.cs
                        // Ubit
                        //if (prim.IsRoot)
                        {
                            //prim.ParentGroup.IsSelected = true;
                            prim.IsSelected = true;
                            scene.AuroraEventManager.FireGenericEventHandler("ObjectSelected", prim);
                        }
                    }
                }
                //Check for avies! They arn't prims!
                if (scene.GetScenePresence(primLocalID) != null)
                    continue;

                if (entity != null)
                {
                    if (!EntitiesToUpdate.Contains(entity))
                        EntitiesToUpdate.Add(entity);
                }
                else
                {
                    MainConsole.Instance.ErrorFormat(
                        "[SCENEPACKETHANDLER]: Could not find prim {0} in SelectPrim, killing prim.",
                        primLocalID);
                    //Send a kill packet to the viewer so it doesn't come up again
                    remoteClient.SendKillObject(scene.RegionInfo.RegionHandle, new uint[1] {primLocalID});
                }
            }
            IScenePresence SP;
            scene.TryGetScenePresence(remoteClient.AgentId, out SP);
            if (SP == null)
                return;
            if (EntitiesToUpdate.Count != 0)
            {
                SP.SceneViewer.QueuePartsForPropertiesUpdate(EntitiesToUpdate.ToArray());
            }
            PerClientSelectionParticles selection = SP.RequestModuleInterface<PerClientSelectionParticles>();
            if (selection != null)
            {
                selection.SelectedUUID = prim;
                selection.IsSelecting = true;
            }
        }

        /// <summary>
        ///     Handle the deselection of a prim from the client.
        /// </summary>
        /// <param name="primLocalID"></param>
        /// <param name="remoteClient"></param>
        protected void DeselectPrim(uint primLocalID, IClientAPI remoteClient)
        {
            IScene scene = remoteClient.Scene;
            ISceneChildEntity part = scene.GetSceneObjectPart(primLocalID);
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

            if (!part.ParentEntity.IsAttachment)
                //This NEEDS to be done because otherwise rotationalVelocity will break! Only for the editing av as the client stops the rotation for them when they are in edit
            {
                if (part.AngularVelocity != Vector3.Zero && !part.ParentEntity.IsDeleted)
                    SP.SceneViewer.QueuePartForUpdate(part, PrimUpdateFlags.ForcedFullUpdate);
            }

            scene.AuroraEventManager.FireGenericEventHandler("ObjectDeselected", part);
        }

        protected void ProcessViewerEffect(IClientAPI remoteClient, List<ViewerEffectEventHandlerArg> args)
        {
            IScene scene = remoteClient.Scene;
            // TODO: don't create new blocks if recycling an old packet
            ViewerEffectPacket.EffectBlock[] effectBlockArray = new ViewerEffectPacket.EffectBlock[args.Count];
            IScenePresence SP;
            scene.TryGetScenePresence(remoteClient.AgentId, out SP);
            for (int i = 0; i < args.Count; i++)
            {
                ViewerEffectPacket.EffectBlock effect = new ViewerEffectPacket.EffectBlock
                                                            {
                                                                AgentID = args[i].AgentID,
                                                                Color = args[i].Color,
                                                                Duration = args[i].Duration,
                                                                ID = args[i].ID,
                                                                Type = args[i].Type,
                                                                TypeData = args[i].TypeData
                                                            };
                effectBlockArray[i] = effect;
                //Save the color
                if (effect.Type == (int) EffectType.Beam || effect.Type == (int) EffectType.Point
                    || effect.Type == (int) EffectType.Sphere)
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

#if (!ISWIN)
                foreach (IScenePresence client in scene.GetScenePresences())
                {
                    if (client.ControllingClient.AgentId != remoteClient.AgentId)
                    {
                        client.ControllingClient.SendViewerEffect(effectBlockArray);
                    }
                }
#else
                foreach (
                    IScenePresence client in
                        scene.GetScenePresences()
                             .Where(client => client.ControllingClient.AgentId != remoteClient.AgentId))
                {
                    client.ControllingClient.SendViewerEffect(effectBlockArray);
                }
#endif
            }
        }

        #endregion

        #region Per Frame Events

        protected class PerClientSelectionParticles
        {
            protected byte[] m_EffectColor = new Color4(1, 0.01568628f, 0, 1).GetBytes();
            protected bool m_IsSelecting;
            protected ISceneChildEntity m_SelectedUUID;
            protected int m_effectsLastSent;
            protected SelectionModule m_module;
            protected IScenePresence m_presence;

            public PerClientSelectionParticles(IScenePresence presence, SelectionModule mod)
            {
                m_presence = presence;
                m_module = mod;
                //Hook up to onFrame so that we can send the updates
                m_presence.Scene.EventManager.OnFrame += EventManager_OnFrame;
            }

            public ISceneChildEntity SelectedUUID
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

            public void Close()
            {
                m_SelectedUUID = null;
                m_IsSelecting = false;
                m_module = null;
                m_EffectColor = null;
                if (m_presence != null)
                    m_presence.Scene.EventManager.OnFrame -= EventManager_OnFrame;
                m_presence = null;
            }

            protected void EventManager_OnFrame()
            {
                if (m_presence == null)
                    return;
                //We can't deregister ourselves... our reference is lost... so just hope we stop getting called soon
                if (!m_presence.IsChildAgent && m_module.UseSelectionParticles && (m_effectsLastSent == 0 ||
                                                                                   Util.EnvironmentTickCountSubtract(
                                                                                       m_effectsLastSent) > 900))
                {
                    SendViewerEffects();
                    m_effectsLastSent = Util.EnvironmentTickCount();
                }
            }

            /// <summary>
            ///     This sends the little particles to the client if they are selecting something or such
            /// </summary>
            protected void SendViewerEffects()
            {
                if (!IsSelecting)
                    return;

                ISceneChildEntity SOP = m_SelectedUUID;
                if (SOP == null) //This IS nesessary, this is how we can clear this out
                {
                    IsSelecting = false;
                    return;
                }

                ViewerEffectPacket.EffectBlock[] effectBlockArray = new ViewerEffectPacket.EffectBlock[1];

                ViewerEffectPacket.EffectBlock effect = new ViewerEffectPacket.EffectBlock
                                                            {
                                                                AgentID = m_presence.UUID,
                                                                Color = EffectColor,
                                                                Duration = 0.9f,
                                                                ID = UUID.Random(),
                                                                Type = (int) EffectType.Beam
                                                            };
                //This seems to be what is passed by SL when its send from the server
                //Bean is the line from hand to object

                byte[] part = new byte[56];
                m_presence.UUID.ToBytes(part, 0); //UUID of av first
                SOP.UUID.ToBytes(part, 16); //UUID of object
                SOP.AbsolutePosition.ToBytes(part, 32); //position of object first

                effect.TypeData = part;
                effectBlockArray[0] = effect;

#if (!ISWIN)
                m_presence.Scene.ForEachClient(
                    delegate(IClientAPI client)
                    {
                        client.SendViewerEffect(effectBlockArray);
                    }
                );
#else
                m_presence.Scene.ForEachClient(
                    client => client.SendViewerEffect(effectBlockArray)
                    );
#endif
            }
        }

        #endregion
    }
}