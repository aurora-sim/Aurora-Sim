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

using Aurora.Framework;
using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.Modules;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/*****************************************************
 *
 * WorldCommModule
 *
 *
 * Holding place for world comms - basically llListen
 * function implementation.
 *
 * lLListen(integer channel, string name, key id, string msg)
 * The name, id, and msg arguments specify the filtering
 * criteria. You can pass the empty string
 * (or NULL_KEY for id) for these to set a completely
 * open filter; this causes the listen() event handler to be
 * invoked for all chat on the channel. To listen only
 * for chat spoken by a specific object or avatar,
 * specify the name and/or id arguments. To listen
 * only for a specific command, specify the
 * (case-sensitive) msg argument. If msg is not empty,
 * listener will only hear strings which are exactly equal
 * to msg. You can also use all the arguments to establish
 * the most restrictive filtering criteria.
 *
 * It might be useful for each listener to maintain a message
 * digest, with a list of recent messages by UUID.  This can
 * be used to prevent in-world repeater loops.  However, the
 * linden functions do not have this capability, so for now
 * thats the way it works.
 * Instead it blocks messages originating from the same prim.
 * (not Object!)
 *
 * For LSL compliance, note the following:
 * (Tested again 1.21.1 on May 2, 2008)
 * 1. 'id' has to be parsed into a UUID. None-UUID keys are
 *    to be replaced by the ZeroID key. (Well, TryParse does
 *    that for us.
 * 2. Setting up an listen event from the same script, with the
 *    same filter settings (including step 1), returns the same
 *    handle as the original filter.
 * 3. (TODO) handles should be script-local. Starting from 1.
 *    Might be actually easier to map the global handle into
 *    script-local handle in the ScriptEngine. Not sure if its
 *    worth the effort tho.
 *
 * **************************************************/

namespace Aurora.Modules.Scripting
{
    public class WorldCommModule : INonSharedRegionModule, IWorldComm
    {
        // private static readonly ILog MainConsole.Instance =
        //     LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected static Vector3 CenterOfRegion = new Vector3(128, 128, 20);
        private readonly List<int> BlockedChannels = new List<int>();
        private ListenerManager m_listenerManager;
        private Queue m_pending;
        private Queue m_pendingQ;
        private int m_saydistance = 30;
        private IScene m_scene;
        private IScriptModule m_scriptModule;
        private int m_shoutdistance = 100;
        private int m_whisperdistance = 10;

        #region INonSharedRegionModule Members

        public void Initialise(IConfigSource config)
        {
            // wrap this in a try block so that defaults will work if
            // the config file doesn't specify otherwise.
            int maxlisteners = 1000;
            int maxhandles = 64;
            try
            {
                m_whisperdistance = config.Configs["AuroraChat"].GetInt("whisper_distance", m_whisperdistance);
                m_saydistance = config.Configs["AuroraChat"].GetInt("say_distance", m_saydistance);
                m_shoutdistance = config.Configs["AuroraChat"].GetInt("shout_distance", m_shoutdistance);
                maxlisteners = config.Configs["AuroraChat"].GetInt("max_listens_per_region", maxlisteners);
                maxhandles = config.Configs["AuroraChat"].GetInt("max_listens_per_script", maxhandles);
            }
            catch (Exception)
            {
            }
            if (maxlisteners < 1) maxlisteners = int.MaxValue;
            if (maxhandles < 1) maxhandles = int.MaxValue;
            m_listenerManager = new ListenerManager(maxlisteners, maxhandles);
            m_pendingQ = new Queue();
            m_pending = Queue.Synchronized(m_pendingQ);
        }

        public void AddRegion(IScene scene)
        {
            m_scene = scene;
            m_scene.RegisterModuleInterface<IWorldComm>(this);
            m_scene.EventManager.OnChatFromClient += DeliverClientMessage;
            m_scene.EventManager.OnChatBroadcast += DeliverClientMessage;
        }

        public void RemoveRegion(IScene scene)
        {
        }

        public void RegionLoaded(IScene scene)
        {
            m_scriptModule = scene.RequestModuleInterface<IScriptModule>();
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
            get { return "WorldCommModule"; }
        }

        #endregion

        #region IWorldComm Members

        public void AddBlockedChannel(int channel)
        {
            //Make sure that the cmd handler thread is running
            m_scriptModule.PokeThreads(UUID.Zero);
            if (!BlockedChannels.Contains(channel))
                BlockedChannels.Add(channel);
        }

        public void RemoveBlockedChannel(int channel)
        {
            if (BlockedChannels.Contains(channel))
                BlockedChannels.Remove(channel);
        }

        /// <summary>
        ///     Create a listen event callback with the specified filters.
        ///     The parameters localID,itemID are needed to uniquely identify
        ///     the script during 'peek' time. Parameter hostID is needed to
        ///     determine the position of the script.
        /// </summary>
        /// <param name="itemID">UUID of the script engine</param>
        /// <param name="hostID">UUID of the SceneObjectPart</param>
        /// <param name="channel">channel to listen on</param>
        /// <param name="name">name to filter on</param>
        /// <param name="id">key to filter on (user given, could be totally faked)</param>
        /// <param name="msg">msg to filter on</param>
        /// <returns>number of the scripts handle</returns>
        public int Listen(UUID itemID, UUID hostID, int channel, string name, UUID id, string msg, int regexBitfield)
        {
            //Make sure that the cmd handler thread is running
            m_scriptModule.PokeThreads(itemID);
            return m_listenerManager.AddListener(itemID, hostID, channel, name, id, msg, 0);
        }

        /// <summary>
        ///     Sets the listen event with handle as active (active = TRUE) or inactive (active = FALSE).
        ///     The handle used is returned from Listen()
        /// </summary>
        /// <param name="itemID">UUID of the script engine</param>
        /// <param name="handle">handle returned by Listen()</param>
        /// <param name="active">temp. activate or deactivate the Listen()</param>
        public void ListenControl(UUID itemID, int handle, int active)
        {
            //Make sure that the cmd handler thread is running
            m_scriptModule.PokeThreads(itemID);
            if (active == 1)
                m_listenerManager.Activate(itemID, handle);
            else if (active == 0)
                m_listenerManager.Dectivate(itemID, handle);
        }

        /// <summary>
        ///     Removes the listen event callback with handle
        /// </summary>
        /// <param name="itemID">UUID of the script engine</param>
        /// <param name="handle">handle returned by Listen()</param>
        public void ListenRemove(UUID itemID, int handle)
        {
            //Make sure that the cmd handler thread is running
            m_scriptModule.PokeThreads(itemID);
            m_listenerManager.Remove(itemID, handle);
        }

        /// <summary>
        ///     Removes all listen event callbacks for the given itemID
        ///     (script engine)
        /// </summary>
        /// <param name="itemID">UUID of the script engine</param>
        public void DeleteListener(UUID itemID)
        {
            //Make sure that the cmd handler thread is running
            m_scriptModule.PokeThreads(itemID);
            m_listenerManager.DeleteListener(itemID);
        }


        public void DeliverMessage(ChatTypeEnum type, int channel, string name, UUID id, string msg)
        {
            Vector3 position;
            ISceneChildEntity source;
            IScenePresence avatar;

            if ((source = m_scene.GetSceneObjectPart(id)) != null)
                position = source.AbsolutePosition;
            else if ((avatar = m_scene.GetScenePresence(id)) != null)
                position = avatar.AbsolutePosition;
            else if (ChatTypeEnum.Region == type)
                position = CenterOfRegion;
            else
                return;

            DeliverMessage(type, channel, name, id, msg, position, -1, UUID.Zero);
        }

        public void DeliverMessage(ChatTypeEnum type, int channel, string name, UUID id, UUID toID, string msg)
        {
            Vector3 position;
            ISceneChildEntity source;
            IScenePresence avatar;

            if ((source = m_scene.GetSceneObjectPart(id)) != null)
                position = source.AbsolutePosition;
            else if ((avatar = m_scene.GetScenePresence(id)) != null)
                position = avatar.AbsolutePosition;
            else if (ChatTypeEnum.Region == type)
                position = CenterOfRegion;
            else
                return;

            DeliverMessage(type, channel, name, id, msg, position, -1, toID);
        }

        public void DeliverMessage(ChatTypeEnum type, int channel, string name, UUID id, string msg, float Range)
        {
            Vector3 position;
            ISceneChildEntity source;
            IScenePresence avatar;

            if ((source = m_scene.GetSceneObjectPart(id)) != null)
                position = source.AbsolutePosition;
            else if ((avatar = m_scene.GetScenePresence(id)) != null)
                position = avatar.AbsolutePosition;
            else if (ChatTypeEnum.Region == type)
                position = CenterOfRegion;
            else
                return;

            DeliverMessage(type, channel, name, id, msg, position, Range, UUID.Zero);
        }

        /// <summary>
        ///     Are there any listen events ready to be dispatched?
        /// </summary>
        /// <returns>boolean indication</returns>
        public bool HasMessages()
        {
            return m_pending.Count != 0;
        }

        public bool HasListeners()
        {
            return m_listenerManager.GetListenersCount() > 0;
        }

        /// <summary>
        ///     Pop the first availlable listen event from the queue
        /// </summary>
        /// <returns>ListenerInfo with filter filled in</returns>
        public IWorldCommListenerInfo GetNextMessage()
        {
            ListenerInfo li = null;

            lock (m_pending.SyncRoot)
            {
                li = (ListenerInfo) m_pending.Dequeue();
            }

            return li;
        }

        /********************************************************************
         *
         * Listener Stuff
         *
         * *****************************************************************/

        public OSD GetSerializationData(UUID itemID, UUID primID)
        {
            if (m_scene.GetSceneObjectPart(primID) != null)
                return m_listenerManager.GetSerializationData(itemID);
            else
                return null;
        }

        public void CreateFromData(UUID itemID, UUID hostID,
                                   OSD data)
        {
            if (m_scene.GetSceneObjectPart(hostID) != null)
                m_listenerManager.AddFromData(itemID, hostID, data);
        }

        #endregion

        /// <summary>
        ///     This method scans over the objects which registered an interest in listen callbacks.
        ///     For everyone it finds, it checks if it fits the given filter. If it does,  then
        ///     enqueue the message for delivery to the objects listen event handler.
        ///     The enqueued ListenerInfo no longer has filter values, but the actually trigged values.
        ///     Objects that do an llSay have their messages delivered here and for nearby avatars,
        ///     the OnChatFromClient event is used.
        /// </summary>
        /// <param name="type">type of delvery (whisper,say,shout or regionwide)</param>
        /// <param name="channel">channel to sent on</param>
        /// <param name="name">name of sender (object or avatar)</param>
        /// <param name="fromID">key of sender (object or avatar)</param>
        /// <param name="msg">msg to sent</param>
        /// <param name="position"></param>
        /// <param name="range"></param>
        /// <param name="toID"></param>
        public void DeliverMessage(ChatTypeEnum type, int channel, string name, UUID fromID, string msg,
                                   Vector3 position,
                                   float range, UUID toID)
        {
            //Make sure that the cmd handler thread is running
            m_scriptModule.PokeThreads(UUID.Zero);
            if (BlockedChannels.Contains(channel))
                return;
            // MainConsole.Instance.DebugFormat("[WorldComm] got[2] type {0}, channel {1}, name {2}, id {3}, msg {4}",
            //                   type, channel, name, id, msg);

            // Determine which listen event filters match the given set of arguments, this results
            // in a limited set of listeners, each belonging a host. If the host is in range, add them
            // to the pending queue.
            foreach (ListenerInfo li in m_listenerManager.GetListeners(UUID.Zero, channel, name, fromID, msg))
            {
                // Dont process if this message is from yourself!
                if (li.GetHostID().Equals(fromID))
                    continue;

                ISceneChildEntity sPart = m_scene.GetSceneObjectPart(li.GetHostID());
                if (sPart == null)
                    continue;

                if (toID != UUID.Zero)
                    if (sPart.UUID != toID &&
                        sPart.AttachedAvatar != toID)
                        continue;
                //Only allow the message to go on if it is an attachment with the given avatars ID or the part ID is right

                double dis = Util.GetDistanceTo(sPart.AbsolutePosition, position);
                switch (type)
                {
                    case ChatTypeEnum.Whisper:
                        if (dis < m_whisperdistance)
                            QueueMessage(new ListenerInfo(li, name, fromID, msg));
                        break;

                    case ChatTypeEnum.Say:
                        if (dis < m_saydistance)
                            QueueMessage(new ListenerInfo(li, name, fromID, msg));
                        break;

                    case ChatTypeEnum.ObsoleteSay:
                        if (dis < m_saydistance)
                            QueueMessage(new ListenerInfo(li, name, fromID, msg));
                        break;

                    case ChatTypeEnum.Shout:
                        if (dis < m_shoutdistance)
                            QueueMessage(new ListenerInfo(li, name, fromID, msg));
                        break;

                    case ChatTypeEnum.Custom:
                        if (dis < range)
                            QueueMessage(new ListenerInfo(li, name, fromID, msg));
                        break;

                    case ChatTypeEnum.Region:
                        QueueMessage(new ListenerInfo(li, name, fromID, msg));
                        break;
                }
            }
        }

        protected void QueueMessage(ListenerInfo li)
        {
            //Make sure that the cmd handler thread is running
            m_scriptModule.PokeThreads(li.GetItemID());
            lock (m_pending.SyncRoot)
            {
                m_pending.Enqueue(li);
            }
        }

        private void DeliverClientMessage(Object sender, OSChatMessage e)
        {
            if (null != e.Sender)
                DeliverMessage(e.Type, e.Channel, e.Sender.Name, e.Sender.AgentId, e.Message, e.Position, -1, UUID.Zero);
            else
                DeliverMessage(e.Type, e.Channel, e.From, UUID.Zero, e.Message, e.Position, -1, UUID.Zero);
        }
    }

    public class ListenerManager
    {
        private readonly Dictionary<int, List<ListenerInfo>> m_listeners = new Dictionary<int, List<ListenerInfo>>();
        private readonly int m_maxhandles;
        private readonly int m_maxlisteners;
        private int m_curlisteners;

        public ListenerManager(int maxlisteners, int maxhandles)
        {
            m_maxlisteners = maxlisteners;
            m_maxhandles = maxhandles;
            m_curlisteners = 0;
        }

        public int AddListener(UUID itemID, UUID hostID, int channel, string name, UUID id, string msg, int regexBitfield)
        {
            // do we already have a match on this particular filter event?
            List<ListenerInfo> coll = GetListeners(itemID, channel, name, id, msg);

            if (coll.Count > 0)
            {
                // special case, called with same filter settings, return same handle
                // (2008-05-02, tested on 1.21.1 server, still holds)
                return coll[0].GetHandle();
            }

            if (m_curlisteners < m_maxlisteners)
            {
                lock (m_listeners)
                {
                    int newHandle = GetNewHandle(itemID);

                    if (newHandle > 0)
                    {
                        ListenerInfo li = new ListenerInfo(newHandle, itemID, hostID, channel, name, id, msg, regexBitfield);

                        List<ListenerInfo> listeners;
                        if (!m_listeners.TryGetValue(channel, out listeners))
                        {
                            listeners = new List<ListenerInfo>();
                            m_listeners.Add(channel, listeners);
                        }
                        listeners.Add(li);
                        m_curlisteners++;

                        return newHandle;
                    }
                }
            }
            return -1;
        }

        public void Remove(UUID itemID, int handle)
        {
            lock (m_listeners)
            {
                foreach (KeyValuePair<int, List<ListenerInfo>> lis in m_listeners)
                {
                    foreach (
                        ListenerInfo li in
                            lis.Value.Where(li => li.GetItemID().Equals(itemID) && li.GetHandle().Equals(handle)))
                    {
                        lis.Value.Remove(li);
                        if (lis.Value.Count == 0)
                        {
                            m_listeners.Remove(lis.Key);
                            m_curlisteners--;
                        }
                        // there should be only one, so we bail out early
                        return;
                    }
                }
            }
        }

        public void DeleteListener(UUID itemID)
        {
            List<int> emptyChannels = new List<int>();
            List<ListenerInfo> removedListeners = new List<ListenerInfo>();

            lock (m_listeners)
            {
                foreach (KeyValuePair<int, List<ListenerInfo>> lis in m_listeners)
                {
                    removedListeners.AddRange(lis.Value.Where(li => li.GetItemID().Equals(itemID)));

                    foreach (ListenerInfo li in removedListeners)
                    {
                        lis.Value.Remove(li);
                        m_curlisteners--;
                    }
                    removedListeners.Clear();
                    if (lis.Value.Count == 0)
                    {
                        // again, store first, remove later
                        emptyChannels.Add(lis.Key);
                    }
                }
                foreach (int channel in emptyChannels)
                {
                    m_listeners.Remove(channel);
                }
            }
        }

        public void Activate(UUID itemID, int handle)
        {
            lock (m_listeners)
            {
                foreach (
                    ListenerInfo li in
                        from lis in m_listeners
                        from li in lis.Value
                        where li.GetItemID().Equals(itemID) && li.GetHandle() == handle
                        select li)
                {
                    li.Activate();
                    // only one, bail out
                    return;
                }
            }
        }

        public void Dectivate(UUID itemID, int handle)
        {
            lock (m_listeners)
            {
                foreach (
                    ListenerInfo li in
                        from lis in m_listeners
                        from li in lis.Value
                        where li.GetItemID().Equals(itemID) && li.GetHandle() == handle
                        select li)
                {
                    li.Deactivate();
                    // only one, bail out
                    return;
                }
            }
        }

        // non-locked access, since its always called in the context of the lock
        private int GetNewHandle(UUID itemID)
        {
            List<int> handles =
                (from lis in m_listeners from li in lis.Value where li.GetItemID().Equals(itemID) select li.GetHandle())
                    .ToList();

            // build a list of used keys for this specific itemID...

            // Note: 0 is NOT a valid handle for llListen() to return
            for (int i = 1; i <= m_maxhandles; i++)
            {
                if (!handles.Contains(i))
                    return i;
            }

            return -1;
        }

        public int GetListenersCount()
        {
            return m_listeners.Count;
        }

        /// These are duplicated from ScriptBaseClass
        /// http://opensimulator.org/mantis/view.php?id=6106#c21945
        #region Constants for the bitfield parameter of osListenRegex

        /// <summary>
        /// process name parameter as regex
        /// </summary>
        public const int OS_LISTEN_REGEX_NAME = 0x1;

        /// <summary>
        /// process message parameter as regex
        /// </summary>
        public const int OS_LISTEN_REGEX_MESSAGE = 0x2;

        #endregion

        // Theres probably a more clever and efficient way to
        // do this, maybe with regex.
        // PM2008: Ha, one could even be smart and define a specialized Enumerator.
        public List<ListenerInfo> GetListeners(UUID itemID, int channel, string name, UUID id, string msg)
        {
            List<ListenerInfo> collection = new List<ListenerInfo>();

            lock (m_listeners)
            {
                List<ListenerInfo> listeners;
                if (!m_listeners.TryGetValue(channel, out listeners))
                {
                    return collection;
                }

                collection.AddRange(from li in listeners
                                    where li.IsActive()
                                    where itemID.Equals(UUID.Zero) || li.GetItemID().Equals(itemID)
                                    where li.GetName().Length <= 0 || li.GetName().Equals(name)
                                    where li.GetName().Length > 0 && ((li.RegexBitfield & OS_LISTEN_REGEX_NAME) != OS_LISTEN_REGEX_NAME && !li.GetName().Equals(name)) ||
                                            ((li.RegexBitfield & OS_LISTEN_REGEX_NAME) == OS_LISTEN_REGEX_NAME && !Regex.IsMatch(name, li.GetName()))
                                    where li.GetName().Length > 0 && ((li.RegexBitfield & OS_LISTEN_REGEX_MESSAGE) != OS_LISTEN_REGEX_MESSAGE && !li.GetMessage().Equals(msg)) ||
                                            ((li.RegexBitfield & OS_LISTEN_REGEX_MESSAGE) == OS_LISTEN_REGEX_MESSAGE && !Regex.IsMatch(msg, li.GetMessage()))
                                    where li.GetID().Equals(UUID.Zero) || li.GetID().Equals(id)
                                    where li.GetMessage().Length <= 0 || li.GetMessage().Equals(msg)
                                    select li);
            }
            return collection;
        }

        public OSD GetSerializationData(UUID itemID)
        {
            OSDMap data = new OSDMap();

            lock (m_listeners)
            {
                foreach (
                    ListenerInfo l in
                        from list in m_listeners.Values from l in list where l.GetItemID() == itemID select l)
                {
                    data[itemID.ToString()] = l.GetSerializationData();
                }
            }
            return data;
        }

        public void AddFromData(UUID itemID, UUID hostID,
                                OSD data)
        {
            OSDMap save = (OSDMap) data;
            foreach (KeyValuePair<string, OSD> kvp in save)
            {
                OSDMap item = (OSDMap) kvp.Value;
                ListenerInfo info = ListenerInfo.FromData(itemID, hostID, item);
                AddListener(info.GetItemID(), info.GetHostID(), info.GetChannel(), info.GetName(), info.GetID(),
                            info.GetMessage(), info.RegexBitfield);
            }
        }
    }

    public class ListenerInfo : IWorldCommListenerInfo
    {
        private bool m_active; // Listener is active or not
        private int m_channel; // Channel
        private int m_handle; // Assigned handle of this listener
        //private uint m_localID; // Local ID from script engine
        private UUID m_hostID; // ID of the host/scene part
        private UUID m_id; // ID to filter messages from
        private UUID m_itemID; // ID of the host script engine
        private string m_message; // The message
        private string m_name; // Object name to filter messages from
        public int RegexBitfield { get; private set; }

        public ListenerInfo(int handle, UUID ItemID, UUID hostID, int channel, string name, UUID id, string message, int regexBitfield)
        {
            Initialise(handle, ItemID, hostID, channel, name, id, message, regexBitfield);
        }

        public ListenerInfo(ListenerInfo li, string name, UUID id, string message)
        {
            Initialise(li.m_handle, li.m_itemID, li.m_hostID, li.m_channel, name, id, message, 0);
        }

        #region IWorldCommListenerInfo Members

        public OSD GetSerializationData()
        {
            OSDMap data = new OSDMap();

            data["Active"] = m_active;
            data["Handle"] = m_handle;
            data["Channel"] = m_channel;
            data["Name"] = m_name;
            data["ID"] = m_id;
            data["Message"] = m_message;

            return data;
        }

        public UUID GetItemID()
        {
            return m_itemID;
        }

        public UUID GetHostID()
        {
            return m_hostID;
        }

        public int GetChannel()
        {
            return m_channel;
        }

        public int GetHandle()
        {
            return m_handle;
        }

        public string GetMessage()
        {
            return m_message;
        }

        public string GetName()
        {
            return m_name;
        }

        public bool IsActive()
        {
            return m_active;
        }

        public void Deactivate()
        {
            m_active = false;
        }

        public void Activate()
        {
            m_active = true;
        }

        public UUID GetID()
        {
            return m_id;
        }

        #endregion

        private void Initialise(int handle, UUID ItemID, UUID hostID, int channel, string name,
                                UUID id, string message, int regexBitfield)
        {
            m_active = true;
            m_handle = handle;
            m_itemID = ItemID;
            m_hostID = hostID;
            m_channel = channel;
            m_name = name;
            m_id = id;
            m_message = message;
            RegexBitfield = regexBitfield;
        }

        public static ListenerInfo FromData(UUID ItemID, UUID hostID, OSDMap data)
        {
            int Handle = data["Handle"].AsInteger();
            int Channel = data["Channel"].AsInteger();
            string Name = data["Name"].AsString();
            string Message = data["Message"].AsString();
            UUID ID = data["ID"].AsUUID();
            bool Active = data["Active"].AsBoolean();
            int RegexBitfield = data["RegexBitfield"].AsInteger();

            ListenerInfo linfo = new ListenerInfo(Handle,
                                                  ItemID, hostID, Channel, Name,
                                                  ID, Message, RegexBitfield) { m_active = Active };

            return linfo;
        }
    }
}