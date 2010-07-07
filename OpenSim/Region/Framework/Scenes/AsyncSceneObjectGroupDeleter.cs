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
using System.Reflection;
using System.Timers;
using log4net;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using Amib.Threading;

namespace OpenSim.Region.Framework.Scenes
{
    class DeleteToInventoryHolder
    {
        public DeRezAction action;
        public UUID agentId;
        public List<SceneObjectGroup> objectGroups;
        public UUID folderID;
        public bool permissionToDelete;
        public bool permissionToTake;
    }

    /// <summary>
    /// Asynchronously derez objects.  This is used to derez large number of objects to inventory without holding
    /// up the main client thread.
    /// </summary>
    public class AsyncSceneObjectGroupDeleter
    {
        private static readonly ILog m_log
            = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <value>
        /// Is the deleter currently enabled?
        /// </value>
        public bool Enabled;

        private readonly Queue<DeleteToInventoryHolder> m_inventoryDeletes = new Queue<DeleteToInventoryHolder>();
        private readonly Queue<DeleteToInventoryHolder> m_sendToInventory = new Queue<DeleteToInventoryHolder>();
        private SmartThreadPool m_ThreadPool;
        private bool DeleteLoopInUse = false;
        private bool SendToInventoryLoopInUse = false;
        private Scene m_scene;

        public AsyncSceneObjectGroupDeleter(Scene scene)
        {
            STPStartInfo startInfo = new STPStartInfo();
            startInfo.IdleTimeout = 60 * 1000; // convert to seconds as stated in .ini
            startInfo.MaxWorkerThreads = 2;
            startInfo.MinWorkerThreads = 0;
            startInfo.ThreadPriority = System.Threading.ThreadPriority.BelowNormal;
            startInfo.StackSize = 262144;
            startInfo.StartSuspended = true;

            m_ThreadPool = new SmartThreadPool(startInfo);

            m_ThreadPool.Start();
            m_scene = scene;
        }

        /// <summary>
        /// Delete the given object from the scene
        /// </summary>
        public void DeleteToInventory(DeRezAction action, UUID folderID,
                List<SceneObjectGroup> objectGroups, UUID AgentId,
                bool permissionToDelete, bool permissionToTake)
        {
            lock (m_inventoryDeletes)
            {
                DeleteToInventoryHolder dtis = new DeleteToInventoryHolder();
                dtis.action = action;
                dtis.folderID = folderID;
                dtis.objectGroups = objectGroups;
                dtis.agentId = AgentId;
                dtis.permissionToDelete = permissionToDelete;
                dtis.permissionToTake = permissionToTake;

                m_inventoryDeletes.Enqueue(dtis);
                m_sendToInventory.Enqueue(dtis);
            }
            foreach(SceneObjectGroup g in objectGroups)
            {
                g.DeleteGroup(false);
            }

            if (!DeleteLoopInUse)
            {
                DeleteLoopInUse = true;
                m_log.Debug("[SCENE]: Starting delete loop");
                m_ThreadPool.QueueWorkItem(this.DoDeleteObject,
                                            new Object[] { 0 });
            }
            if (!SendToInventoryLoopInUse)
            {
                SendToInventoryLoopInUse = true;
                m_log.Debug("[SCENE]: Starting send to inventory loop");
                m_ThreadPool.QueueWorkItem(this.DoSendToInventory,
                                            new Object[] { 0 });
            }
        }

        public object DoDeleteObject(object o)
        {
            if (DeleteObject())
            {
                //Requeue us if there is some left
                m_ThreadPool.QueueWorkItem(this.DoDeleteObject,
                                              new Object[] { 0 });
            }
            else
            {
                DeleteLoopInUse = false;
                m_log.Debug("[SCENE]: Ending delete loop");
            }
            return 0;
        }

        public object DoSendToInventory(object o)
        {
            if (InventoryDeQueueAndDelete())
            {
                //Requeue us if there is some left
                m_ThreadPool.QueueWorkItem(this.DoSendToInventory,
                                              new Object[] { 0 });
            }
            else
            {
                SendToInventoryLoopInUse = false;
                m_log.Debug("[SCENE]: Ending send to inventory loop");
            }
            return 0;
        }

        public bool DeleteObject()
        {
            DeleteToInventoryHolder x = null;

            try
            {
                lock (m_sendToInventory)
                {
                    int left = m_sendToInventory.Count;
                    if (left > 0)
                    {
                        x = m_sendToInventory.Dequeue();

                        if (x.permissionToDelete)
                        {
                            foreach (SceneObjectGroup g in x.objectGroups)
                            {
                                // Force a database backup/update on this SceneObjectGroup
                                // So that we know the database is upto date,
                                // for when deleting the object from it
                                m_scene.ForceSceneObjectBackup(g);
                                m_scene.DeleteSceneObject(g, false, true);
                            }
                        }
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                // We can't put the object group details in here since the root part may have disappeared (which is where these sit).
                // FIXME: This needs to be fixed.
                m_log.ErrorFormat(
                    "[SCENE]: Queued sending of scene object to agent {0} {1} failed: {2}",
                    (x != null ? x.agentId.ToString() : "unavailable"), (x != null ? x.agentId.ToString() : "unavailable"), e.ToString());
            }

            m_log.Debug("[SCENE]: No objects left in delete queue.");
            return false;
        }

        /// <summary>
        /// Move the next object in the queue to inventory.  Then delete it properly from the scene.
        /// </summary>
        /// <returns></returns>
        public bool InventoryDeQueueAndDelete()
        {
            DeleteToInventoryHolder x = null;

            try
            {
                lock (m_inventoryDeletes)
                {
                    int left = m_inventoryDeletes.Count;
                    if (left > 0)
                    {
                        x = m_inventoryDeletes.Dequeue();

                        m_log.DebugFormat(
                            "[SCENE]: Sending object to user's inventory, {0} item(s) remaining.", left);

                        if (x.permissionToTake)
                        {
                            try
                            {
                                IInventoryAccessModule invAccess = m_scene.RequestModuleInterface<IInventoryAccessModule>();
                                if (invAccess != null)
                                    invAccess.DeleteToInventory(x.action, x.folderID, x.objectGroups, x.agentId);
                            }
                            catch (Exception e)
                            {
                                m_log.DebugFormat("Exception background sending object: " + e);
                            }
                        }

                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                // We can't put the object group details in here since the root part may have disappeared (which is where these sit).
                // FIXME: This needs to be fixed.
                m_log.ErrorFormat(
                    "[SCENE]: Queued sending of scene object to agent {0} {1} failed: {2}",
                    (x != null ? x.agentId.ToString() : "unavailable"), (x != null ? x.agentId.ToString() : "unavailable"), e.ToString());
            }

            m_log.Debug("[SCENE]: No objects left in inventory send queue.");
            return false;
        }
    }
}