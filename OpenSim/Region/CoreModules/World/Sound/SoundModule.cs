/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Mono.Addins;

namespace OpenSim.Region.CoreModules.World.Sound
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]
    public class SoundModule : INonSharedRegionModule, ISoundModule
    {
        //private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        protected Scene m_scene;
        
        public void Initialise(IConfigSource source)
        {
        }

        public void AddRegion(Scene scene)
        {
            m_scene = scene;

            m_scene.EventManager.OnNewClient += OnNewClient;

            m_scene.RegisterModuleInterface<ISoundModule>(this);
        }

        public void RemoveRegion(Scene scene)
        {

        }

        public void RegionLoaded(Scene scene)
        {

        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }
        
        public void PostInitialise() {}
        public void Close() {}
        public string Name { get { return "Sound Module"; } }
        public bool IsSharedModule { get { return false; } }
        
        private void OnNewClient(IClientAPI client)
        {
            client.OnSoundTrigger += TriggerSound;
        }
        
        public virtual void PlayAttachedSound(
            UUID soundID, UUID ownerID, UUID objectID, double gain, Vector3 position, byte flags, float radius)
        {
            m_scene.ForEachScenePresence(delegate(ScenePresence sp)
            {
                if (Cones.Count != 0)
                {
                    foreach (ConeOfSilence CS in Cones.Values)
                    {
                        if (Util.GetDistanceTo(sp.AbsolutePosition, CS.Position) > CS.Radius)
                        {
                            // Presence is outside of the Cone of silence

                            if (Util.GetDistanceTo(CS.Position, position) < CS.Radius)
                            {
                                //Sound was triggered inside the cone, but avatar is outside
                                continue;
                            }
                        }
                        else
                        {
                            // Avatar is inside the cone of silence
                            if (Util.GetDistanceTo(CS.Position, position) > CS.Radius)
                            {
                                //Sound was triggered outside of the cone, but avatar is inside of the cone.
                                continue;
                            }
                        }
                    }
                }
                else
                {
                    if (sp.IsChildAgent)
                        return;

                    double dis = Util.GetDistanceTo(sp.AbsolutePosition, position);
                    if (dis > 100.0) // Max audio distance
                        return;

                    // Scale by distance
                    if (radius == 0)
                        gain = (float)((double)gain * ((100.0 - dis) / 100.0));
                    else
                        gain = (float)((double)gain * ((radius - dis) / radius));

                    if (sp.Scene.GetSceneObjectPart(objectID).m_UseSoundQueue == 1)
                        flags += (int)OpenMetaverse.SoundFlags.Queue;
                    sp.ControllingClient.SendPlayAttachedSound(soundID, objectID, ownerID, (float)gain, flags);
                }
            });
        }

        private class ConeOfSilence
        {
            public Vector3 Position;
            public float Radius;
        }

        private Dictionary<UUID, ConeOfSilence> Cones = new Dictionary<UUID, ConeOfSilence>();

        public virtual void AddConeOfSilence(UUID objectID, Vector3 position, float Radius)
        {
            ConeOfSilence CS = new ConeOfSilence();
            CS.Position = position;
            CS.Radius = Radius;
            Cones.Add(objectID, CS);
        }

        public virtual void RemoveConeOfSilence(UUID objectID)
        {
            Cones.Remove(objectID);
        }

        public virtual void TriggerSound(
            UUID soundId, UUID ownerID, UUID objectID, UUID parentID, double gain, Vector3 position, UInt64 handle, float radius)
        {
            m_scene.ForEachScenePresence(delegate(ScenePresence sp)
            {
                if (Cones.Count != 0)
                {
                    foreach (ConeOfSilence CS in Cones.Values)
                    {
                        if (Util.GetDistanceTo(sp.AbsolutePosition, CS.Position) > CS.Radius)
                        {
                            // Presence is outside of the Cone of silence

                            if (Util.GetDistanceTo(CS.Position, position) < CS.Radius)
                            {
                                //Sound was triggered inside the cone, but avatar is outside
                                continue;
                            }
                        }
                        else
                        {
                            // Avatar is inside the cone of silence
                            if (Util.GetDistanceTo(CS.Position, position) > CS.Radius)
                            {
                                //Sound was triggered outside of the cone, but avatar is inside of the cone.
                                continue;
                            }
                        }
                    }
                }
                else
                {
                    if (sp.IsChildAgent)
                        return;

                    double dis = Util.GetDistanceTo(sp.AbsolutePosition, position);
                    if (dis > 100.0) // Max audio distance
                        return;

                    // Scale by distance
                    if (radius == 0)
                        gain = (float)((double)gain * ((100.0 - dis) / 100.0));
                    else
                        gain = (float)((double)gain * ((radius - dis) / radius));

                    sp.ControllingClient.SendTriggeredSound(
                        soundId, ownerID, objectID, parentID, handle, position, (float)gain);
                }
            });
        }
    }
}
