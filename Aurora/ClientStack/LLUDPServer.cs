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

using Amib.Threading;
using Aurora.Framework;
using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Aurora.ClientStack
{
    /// <summary>
    ///     The LLUDP server for a region. This handles incoming and outgoing
    ///     packets for all UDP connections to the region
    /// </summary>
    public class LLUDPServer : OpenSimUDPBase, IClientNetworkServer
    {
        /// <summary>
        ///     Maximum transmission unit, or UDP packet size, for the LLUDP protocol
        /// </summary>
        public const int MTU = 1400;

        /// <summary>
        ///     Number of packets to send per loop per LLUDPClient
        /// </summary>
        public const int MAX_PACKET_SEND = 4;

        private readonly ThreadMonitor incomingPacketMonitor = new ThreadMonitor();
        private readonly List<IClientAPI> m_currentClients = new List<IClientAPI>();
        private readonly ExpiringCache<UUID, uint> m_inQueueCircuitCodes = new ExpiringCache<UUID, uint>();
        private readonly ThreadMonitor outgoingPacketMonitor = new ThreadMonitor();

        //PacketEventDictionary packetEvents = new PacketEventDictionary();
        /// <summary>
        ///     Handlers for incoming packets
        /// </summary>
        /// <summary>
        ///     Incoming packets that are awaiting handling
        /// </summary>
        private readonly OpenMetaverse.BlockingQueue<IncomingPacket> packetInbox =
            new OpenMetaverse.BlockingQueue<IncomingPacket>();

        /// <summary>
        ///     Number of avatar update packets to put on the queue each time the
        ///     OnQueueEmpty event is triggered
        /// </summary>
        public int AvatarUpdatesPerCallBack;

        public int ClientTimeOut;

        /// <summary>
        ///     Number of prim updates to put on the queue each time the
        ///     OnQueueEmpty event is triggered for updates
        /// </summary>
        public int PrimUpdatesPerCallback;

        /// <summary>
        ///     Number of texture packets to put on the queue each time the
        ///     OnQueueEmpty event is triggered for textures
        /// </summary>
        public int TextureSendLimit;

        /// <summary>
        ///     The measured resolution of Environment.TickCount
        /// </summary>
        public float TickCountResolution;

        /// <summary>
        ///     Flag to process packets asynchronously or synchronously
        /// </summary>
        private bool m_asyncPacketHandling;

        /// <summary>
        ///     Manages authentication for agent circuits
        /// </summary>
        public AgentCircuitManager m_circuitManager;

        private int m_defaultRTO;

        /// <summary>
        ///     Keeps track of the number of 100 millisecond periods elapsed in the outgoing packet handler executed
        /// </summary>
        private int m_elapsed100MSOutgoingPacketHandler;

        /// <summary>
        ///     Keeps track of the number of 500 millisecond periods elapsed in the outgoing packet handler executed
        /// </summary>
        private int m_elapsed500MSOutgoingPacketHandler;

        /// <summary>
        ///     Keeps track of the number of elapsed milliseconds since the last time the outgoing packet handler looped
        /// </summary>
        private int m_elapsedMSOutgoingPacketHandler;

        /// <summary>
        ///     Environment.TickCount of the last time that packet stats were reported to the scene
        /// </summary>
        private int m_elapsedMSSinceLastStatReport;

        private int m_maxRTO;

        /// <summary>
        ///     Tracks whether or not a packet was sent each round so we know
        ///     whether or not to sleep
        /// </summary>
        private bool m_packetSent;

        /// <summary>
        ///     The size of the receive buffer for the UDP socket. This value
        ///     is passed up to the operating system and used in the system networking
        ///     stack. Use zero to leave this value as the default
        /// </summary>
        private int m_recvBufferSize;

        /// <summary>
        ///     Flag to signal when clients should check for resends
        /// </summary>
        private bool m_resendUnacked;

        /// <summary>
        ///     Reference to the scene this UDP server is attached to
        /// </summary>
        public IScene m_scene;

        /// <summary>
        ///     Flag to signal when clients should send ACKs
        /// </summary>
        private bool m_sendAcks;

        /// <summary>
        ///     Flag to signal when clients should send pings
        /// </summary>
        private bool m_sendPing;

        //private UDPClientCollection m_clients = new UDPClientCollection();
        /// <summary>
        /// </summary>
        /// <summary>
        ///     Bandwidth throttle for this UDP server
        /// </summary>
        protected TokenBucket m_throttle;

        /// <summary>
        ///     Bandwidth throttle rates for this UDP server
        /// </summary>
        protected ThrottleRates m_throttleRates;

        /// <summary>
        ///     Environment.TickCount of the last time the outgoing packet handler executed
        /// </summary>
        private int m_tickLastOutgoingPacketHandler;

        public Socket Server
        {
            get { return null; }
        }

        #region IClientNetworkServer Members

        public void Initialise(int port, IConfigSource configSource, AgentCircuitManager circuitManager)
        {
            IConfig networkConfig = configSource.Configs["Network"];
            IPAddress internalIP = IPAddress.Any;
            if (networkConfig != null)
                IPAddress.TryParse(networkConfig.GetString("internal_ip", "0.0.0.0"), out internalIP);

            InitThreadPool(15);

            base.Initialise(internalIP, port);

            #region Environment.TickCount Measurement

            // Measure the resolution of Environment.TickCount
            TickCountResolution = 0f;
            for (int i = 0; i < 5; i++)
            {
                int start = Environment.TickCount;
                int now = start;
                while (now == start)
                    now = Environment.TickCount;
                TickCountResolution += (now - start)*0.2f;
            }
            //MainConsole.Instance.Info("[LLUDPSERVER]: Average Environment.TickCount resolution: " + TickCountResolution + "ms");
            TickCountResolution = (float) Math.Ceiling(TickCountResolution);

            #endregion Environment.TickCount Measurement

            m_circuitManager = circuitManager;
            int sceneThrottleBps = 0;

            IConfig config = configSource.Configs["ClientStack.LindenUDP"];
            if (config != null)
            {
                m_asyncPacketHandling = config.GetBoolean("async_packet_handling", false);
                m_recvBufferSize = config.GetInt("client_socket_rcvbuf_size", 0);
                sceneThrottleBps = config.GetInt("scene_throttle_max_bps", 0);

                PrimUpdatesPerCallback = config.GetInt("PrimUpdatesPerCallback", 60);
                AvatarUpdatesPerCallBack = config.GetInt("AvatarUpdatesPerCallback", 80);
                TextureSendLimit = config.GetInt("TextureSendLimit", 25);

                m_defaultRTO = config.GetInt("DefaultRTO", 1000);
                m_maxRTO = config.GetInt("MaxRTO", 20000);
                ClientTimeOut = config.GetInt("ClientTimeOut", 500);
            }
            else
            {
                PrimUpdatesPerCallback = 60;
                AvatarUpdatesPerCallBack = 80;
                TextureSendLimit = 25;
                ClientTimeOut = 500;
            }

            #region BinaryStats

            config = configSource.Configs["Statistics.Binary"];
            m_shouldCollectStats = false;
            if (config != null)
            {
                if (config.Contains("enabled") && config.GetBoolean("enabled"))
                {
                    if (config.Contains("collect_packet_headers"))
                        m_shouldCollectStats = config.GetBoolean("collect_packet_headers");
                    if (config.Contains("packet_headers_period_seconds"))
                    {
                        binStatsMaxFilesize = TimeSpan.FromSeconds(config.GetInt("region_stats_period_seconds"));
                    }
                    if (config.Contains("stats_dir"))
                    {
                        binStatsDir = config.GetString("stats_dir");
                    }
                }
                else
                {
                    m_shouldCollectStats = false;
                }
            }

            #endregion BinaryStats

            if (sceneThrottleBps != 0)
                m_throttle = new TokenBucket(null, sceneThrottleBps, 0);
            m_throttleRates = new ThrottleRates(configSource);
        }

        public void Start()
        {
            if (m_scene == null)
                throw new InvalidOperationException(
                    "[LLUDPSERVER]: Cannot LLUDPServer.Start() without an IScene reference");

            //MainConsole.Instance.Info("[LLUDPSERVER]: Starting the LLUDP server in " + (m_asyncPacketHandling ? "asynchronous" : "synchronous") + " mode");

            Start(m_recvBufferSize, m_asyncPacketHandling);


            // Start the packet processing threads
            //Give it the heartbeat delegate with an infinite timeout
            incomingPacketMonitor.StartTrackingThread(0, IncomingPacketHandlerLoop);
            //Then start the thread for it with an infinite loop time and no 
            //  sleep overall as the Update delete does it on it's own
            incomingPacketMonitor.StartMonitor(0, 0);

            outgoingPacketMonitor.StartTrackingThread(0, OutgoingPacketHandlerLoop);
            outgoingPacketMonitor.StartMonitor(0, 0);

            m_elapsedMSSinceLastStatReport = Environment.TickCount;
        }

        public new void Stop()
        {
            MainConsole.Instance.Debug("[LLUDPSERVER]: Shutting down the LLUDP server for " +
                                       m_scene.RegionInfo.RegionName);
            incomingPacketMonitor.Stop();
            outgoingPacketMonitor.Stop();
            base.Stop();
            CloseThreadPool();
        }

        public IClientNetworkServer Copy()
        {
            return new LLUDPServer();
        }

        public void AddScene(IScene scene)
        {
            if (m_scene != null)
            {
                MainConsole.Instance.Error("[LLUDPSERVER]: AddScene() called on an LLUDPServer that already has a scene");
                return;
            }
            m_scene = scene;
        }

        public void RemoveClient(IClientAPI client)
        {
            m_currentClients.RemoveAll(testClient => client.AgentId == testClient.AgentId);
        }

        #endregion

        private Amib.Threading.SmartThreadPool m_ThreadPool;
        private volatile bool m_threadPoolRunning;

        public void FireAndForget(Action<object> callback, object obj)
        {
            if (m_ThreadPool == null)
                InitThreadPool(15);
            if (m_threadPoolRunning) //Check if the thread pool should be running
                m_ThreadPool.QueueWorkItem((WorkItemCallback) SmartThreadPoolCallback, new[] {callback, obj});
        }

        private static object SmartThreadPoolCallback(object o)
        {
            object[] array = (object[]) o;
            Action<object> callback = (Action<object>) array[0];
            object obj = array[1];

            callback(obj);
            return null;
        }

        public void CloseThreadPool()
        {
            if (m_threadPoolRunning)
            {
                //This stops more tasks and threads from being started
                m_threadPoolRunning = false;
                m_ThreadPool.WaitForIdle(60*1000);
                //Wait for the threads to be idle, but don't wait for more than a minute
                //Destroy the threadpool now
                m_ThreadPool.Dispose();
                m_ThreadPool = null;
            }
        }

        public void InitThreadPool(int maxThreads)
        {
            m_threadPoolRunning = true;
            m_ThreadPool = new SmartThreadPool(2000, maxThreads, 2);
        }

        public void BroadcastPacket(Packet packet, ThrottleOutPacketType category, bool sendToPausedAgents,
                                    bool allowSplitting, UnackedPacketMethod resendMethod,
                                    UnackedPacketMethod finishedMethod)
        {
            // CoarseLocationUpdate and AvatarGroupsReply packets cannot be split in an automated way
            if ((packet.Type == PacketType.CoarseLocationUpdate || packet.Type == PacketType.AvatarGroupsReply) &&
                allowSplitting)
                allowSplitting = false;

            if (allowSplitting && packet.HasVariableBlocks)
            {
                byte[][] datas = packet.ToBytesMultiple();
                int packetCount = datas.Length;

                if (packetCount < 1)
                    MainConsole.Instance.Error("[LLUDPSERVER]: Failed to split " + packet.Type +
                                               " with estimated length " +
                                               packet.Length);

                for (int i = 0; i < packetCount; i++)
                {
                    byte[] data = datas[i];
                    ForEachInternalClient(
                        delegate(IClientAPI client)
                            {
                                if (client is LLClientView)
                                    SendPacketData(((LLClientView) client).UDPClient, data, packet, category,
                                                   resendMethod, finishedMethod);
                            }
                        );
                }
            }
            else
            {
                byte[] data = packet.ToBytes();
                ForEachInternalClient(
                    delegate(IClientAPI client)
                        {
                            if (client is LLClientView)
                                SendPacketData(((LLClientView) client).UDPClient, data, packet, category, resendMethod,
                                               finishedMethod);
                        }
                    );
            }
        }

        public void SendPacket(LLUDPClient udpClient, Packet packet, ThrottleOutPacketType category, bool allowSplitting,
                               UnackedPacketMethod resendMethod, UnackedPacketMethod finishedMethod)
        {
            // CoarseLocationUpdate packets cannot be split in an automated way
            if (packet.Type == PacketType.CoarseLocationUpdate && allowSplitting)
                allowSplitting = false;

            if (allowSplitting && packet.HasVariableBlocks)
            {
                byte[][] datas = packet.ToBytesMultiple();
                int packetCount = datas.Length;

                if (packetCount < 1)
                    MainConsole.Instance.Error("[LLUDPSERVER]: Failed to split " + packet.Type +
                                               " with estimated length " +
                                               packet.Length);

                for (int i = 0; i < packetCount; i++)
                {
                    byte[] data = datas[i];
                    SendPacketData(udpClient, data, packet, category, resendMethod, finishedMethod);
                    data = null;
                }
                datas = null;
            }
            else
            {
                byte[] data = packet.ToBytes();
                SendPacketData(udpClient, data, packet, category, resendMethod, finishedMethod);
                data = null;
            }
            packet = null;
        }

        public void SendPacketData(LLUDPClient udpClient, byte[] data, Packet packet, ThrottleOutPacketType category,
                                   UnackedPacketMethod resendMethod, UnackedPacketMethod finishedMethod)
        {
            int dataLength = data.Length;
            bool doZerocode = (data[0] & Helpers.MSG_ZEROCODED) != 0;
            bool doCopy = true;

            // Frequency analysis of outgoing packet sizes shows a large clump of packets at each end of the spectrum.
            // The vast majority of packets are less than 200 bytes, although due to asset transfers and packet splitting
            // there are a decent number of packets in the 1000-1140 byte range. We allocate one of two sizes of data here
            // to accomodate for both common scenarios and provide ample room for ACK appending in both
            int bufferSize = dataLength*2;

            UDPPacketBuffer buffer = new UDPPacketBuffer(udpClient.RemoteEndPoint, bufferSize);

            // Zerocode if needed
            if (doZerocode)
            {
                try
                {
                    dataLength = Helpers.ZeroEncode(data, dataLength, buffer.Data);
                    doCopy = false;
                }
                catch (IndexOutOfRangeException)
                {
                    // The packet grew larger than the bufferSize while zerocoding.
                    // Remove the MSG_ZEROCODED flag and send the unencoded data
                    // instead
                    MainConsole.Instance.Debug("[LLUDPSERVER]: Packet exceeded buffer size during zerocoding for " +
                                               packet.Type +
                                               ". DataLength=" + dataLength +
                                               " and BufferLength=" + buffer.Data.Length +
                                               ". Removing MSG_ZEROCODED flag");
                    data[0] = (byte) (data[0] & ~Helpers.MSG_ZEROCODED);
                }
            }

            // If the packet data wasn't already copied during zerocoding, copy it now
            if (doCopy)
            {
                if (dataLength <= buffer.Data.Length)
                {
                    Buffer.BlockCopy(data, 0, buffer.Data, 0, dataLength);
                }
                else
                {
                    bufferSize = dataLength;
                    buffer = new UDPPacketBuffer(udpClient.RemoteEndPoint, bufferSize);

                    // MainConsole.Instance.Error("[LLUDPSERVER]: Packet exceeded buffer size! This could be an indication of packet assembly not obeying the MTU. Type=" +
                    //     type + ", DataLength=" + dataLength + ", BufferLength=" + buffer.Data.Length + ". Dropping packet");
                    Buffer.BlockCopy(data, 0, buffer.Data, 0, dataLength);
                }
            }

            buffer.DataLength = dataLength;

            #region Queue or Send

            OutgoingPacket outgoingPacket = new OutgoingPacket(udpClient, buffer, category, resendMethod, finishedMethod,
                                                               packet);

            if (!outgoingPacket.Client.EnqueueOutgoing(outgoingPacket))
                SendPacketFinal(outgoingPacket);

            #endregion Queue or Send
        }

        public void SendAcks(LLUDPClient udpClient)
        {
            uint ack;

            if (udpClient.PendingAcks.TryDequeue(out ack))
            {
                List<PacketAckPacket.PacketsBlock> blocks = new List<PacketAckPacket.PacketsBlock>();
                PacketAckPacket.PacketsBlock block = new PacketAckPacket.PacketsBlock {ID = ack};
                blocks.Add(block);

                while (udpClient.PendingAcks.TryDequeue(out ack))
                {
                    block = new PacketAckPacket.PacketsBlock {ID = ack};
                    blocks.Add(block);
                }

                PacketAckPacket packet = (PacketAckPacket) PacketPool.Instance.GetPacket(PacketType.PacketAck);
                packet.Header.Reliable = false;
                packet.Packets = blocks.ToArray();

                SendPacket(udpClient, packet, ThrottleOutPacketType.OutBand, true, null, null);
            }
        }

        public void SendPing(LLUDPClient udpClient)
        {
            StartPingCheckPacket pc = (StartPingCheckPacket) PacketPool.Instance.GetPacket(PacketType.StartPingCheck);
            pc.Header.Reliable = false;

            pc.PingID.PingID = udpClient.CurrentPingSequence++;
            // We *could* get OldestUnacked, but it would hurt performance and not provide any benefit
            pc.PingID.OldestUnacked = 0;

            SendPacket(udpClient, pc, ThrottleOutPacketType.OutBand, false, null, null);
        }

        public void CompletePing(LLUDPClient udpClient, byte pingID)
        {
            CompletePingCheckPacket completePing =
                (CompletePingCheckPacket) PacketPool.Instance.GetPacket(PacketType.CompletePingCheck);
            completePing.PingID.PingID = pingID;
            SendPacket(udpClient, completePing, ThrottleOutPacketType.OutBand, false, null, null);
        }

        public void ResendUnacked(LLUDPClient udpClient)
        {
            if (!udpClient.IsConnected)
                return;

            // Disconnect an agent if no packets are received for some time
            if ((Environment.TickCount & Int32.MaxValue) - udpClient.TickLastPacketReceived > 1000*ClientTimeOut &&
                !udpClient.IsPaused)
            {
                MainConsole.Instance.Warn("[LLUDPSERVER]: Ack timeout, disconnecting " + udpClient.AgentID);

                ILoginMonitor monitor =
                    (ILoginMonitor)
                    m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor("", MonitorModuleHelper.LoginMonitor);
                if (monitor != null)
                    monitor.AddAbnormalClientThreadTermination();

                RemoveClient(udpClient);
                return;
            }

            // Get a list of all of the packets that have been sitting unacked longer than udpClient.RTO
            List<OutgoingPacket> expiredPackets = udpClient.NeedAcks.GetExpiredPackets(udpClient.RTO);

            if (expiredPackets != null)
            {
                //MainConsole.Instance.Debug("[LLUDPSERVER]: Resending " + expiredPackets.Count + " packets to " + udpClient.AgentID + ", RTO=" + udpClient.RTO);

                // Exponential backoff of the retransmission timeout
                udpClient.BackoffRTO();

                foreach (OutgoingPacket t in expiredPackets.Where(t => t.UnackedMethod != null))
                {
                    t.UnackedMethod(t);
                }

                // Resend packets
                foreach (OutgoingPacket outgoingPacket in expiredPackets.Where(t => t.UnackedMethod == null))
                {
                    //MainConsole.Instance.DebugFormat("[LLUDPSERVER]: Resending packet #{0} (attempt {1}), {2}ms have passed",
                    //    outgoingPacket.SequenceNumber, outgoingPacket.ResendCount, Environment.TickCount - outgoingPacket.TickCount);

                    // Set the resent flag
                    outgoingPacket.Buffer.Data[0] = (byte) (outgoingPacket.Buffer.Data[0] | Helpers.MSG_RESENT);

                    // resend in its original category
                    outgoingPacket.Category = ThrottleOutPacketType.Resend;

                    // Bump up the resend count on this packet
                    Interlocked.Increment(ref outgoingPacket.ResendCount);
                    //Interlocked.Increment(ref Stats.ResentPackets);

                    // Requeue or resend the packet
                    if (!outgoingPacket.Client.EnqueueOutgoing(outgoingPacket))
                        SendPacketFinal(outgoingPacket);
                }
            }
        }

        public void Flush(LLUDPClient udpClient)
        {
            // FIXME: Implement?
        }

        /// <summary>
        ///     Actually send a packet to a client.
        /// </summary>
        /// <param name="outgoingPacket"></param>
        internal void SendPacketFinal(OutgoingPacket outgoingPacket)
        {
            UDPPacketBuffer buffer = outgoingPacket.Buffer;
            byte flags = buffer.Data[0];
            bool isResend = (flags & Helpers.MSG_RESENT) != 0;
            bool isReliable = (flags & Helpers.MSG_RELIABLE) != 0;
            bool isZerocoded = (flags & Helpers.MSG_ZEROCODED) != 0;
            LLUDPClient udpClient = outgoingPacket.Client;

            if (!udpClient.IsConnected)
                return;

            #region ACK Appending

            int dataLength = buffer.DataLength;

            // NOTE: I'm seeing problems with some viewers when ACKs are appended to zerocoded packets so I've disabled that here
            if (!isZerocoded)
            {
                // Keep appending ACKs until there is no room left in the buffer or there are
                // no more ACKs to append
                uint ackCount = 0;
                uint ack;
                while (dataLength + 5 < buffer.Data.Length && udpClient.PendingAcks.TryDequeue(out ack))
                {
                    Utils.UIntToBytesBig(ack, buffer.Data, dataLength);
                    dataLength += 4;
                    ++ackCount;
                }

                if (ackCount > 0)
                {
                    // Set the last byte of the packet equal to the number of appended ACKs
                    buffer.Data[dataLength++] = (byte) ackCount;
                    // Set the appended ACKs flag on this packet
                    buffer.Data[0] = (byte) (buffer.Data[0] | Helpers.MSG_APPENDED_ACKS);
                }
            }

            buffer.DataLength = dataLength;

            #endregion ACK Appending

            #region Sequence Number Assignment

            bool canBeRemoved = false;
            if (!isResend)
            {
                // Not a resend, assign a new sequence number
                uint sequenceNumber = (uint) Interlocked.Increment(ref udpClient.CurrentSequence);
                Utils.UIntToBytesBig(sequenceNumber, buffer.Data, 1);
                outgoingPacket.SequenceNumber = sequenceNumber;

                if (isReliable)
                {
                    // Add this packet to the list of ACK responses we are waiting on from the server
                    udpClient.NeedAcks.Add(outgoingPacket);
                    Interlocked.Add(ref udpClient.UnackedBytes, outgoingPacket.Buffer.DataLength);
                }
                else
                    canBeRemoved = true;
            }

            #endregion Sequence Number Assignment

            // Stats tracking
            Interlocked.Increment(ref udpClient.PacketsSent);
//            if (isReliable)
//                Interlocked.Add(ref udpClient.UnackedBytes, outgoingPacket.Buffer.DataLength);

            // Put the UDP payload on the wire
//            AsyncBeginSend(buffer);

            SyncSend(buffer);

            // Keep track of when this packet was sent out (right now)
            outgoingPacket.TickCount = Environment.TickCount & Int32.MaxValue;

            if (outgoingPacket.FinishedMethod != null)
                outgoingPacket.FinishedMethod(outgoingPacket);

            if (canBeRemoved)
                outgoingPacket.Destroy(1);
        }

        protected override void PacketReceived(UDPPacketBuffer buffer)
        {
            //MainConsole.Instance.Info("[llupdserver] PacketReceived");
            // Debugging/Profiling
            //try { Thread.CurrentThread.Name = "PacketReceived (" + m_scene.RegionInfo.RegionName + ")"; }
            //catch (Exception) { }

            LLUDPClient udpClient = null;
            Packet packet = null;
            int packetEnd = buffer.DataLength - 1;
            IPEndPoint address = (IPEndPoint) buffer.RemoteEndPoint;

            #region Decoding

            try
            {
                packet = PacketPool.Instance.GetPacket(buffer.Data, ref packetEnd,
                                                       // Only allocate a buffer for zerodecoding if the packet is zerocoded
                                                       ((buffer.Data[0] & Helpers.MSG_ZEROCODED) != 0)
                                                           ? new byte[4096]
                                                           : null);
            }
            catch (MalformedDataException)
            {
            }

            // Fail-safe check
            if (packet == null)
            {
                MainConsole.Instance.ErrorFormat(
                    "[LLUDPSERVER]: Malformed data, cannot parse {0} byte packet from {1}:",
                    buffer.DataLength, buffer.RemoteEndPoint);
                MainConsole.Instance.Error(Utils.BytesToHexString(buffer.Data, buffer.DataLength, null));
                return;
            }

            #endregion Decoding

            #region Packet to Client Mapping

            // UseCircuitCode handling
            if (packet.Type == PacketType.UseCircuitCode)
            {
                UseCircuitCodePacket cPacket = (UseCircuitCodePacket) packet;
                AgentCircuitData sessionData;
                IPEndPoint remoteEndPoint = (IPEndPoint) buffer.RemoteEndPoint;
                if (IsClientAuthorized(cPacket, remoteEndPoint, out sessionData))
                {
                    lock (m_inQueueCircuitCodes)
                    {
                        bool contains = m_inQueueCircuitCodes.Contains(sessionData.AgentID);
                        m_inQueueCircuitCodes.AddOrUpdate(sessionData.AgentID, cPacket.Header.Sequence, 5);
                        if (contains)
                        {
                            MainConsole.Instance.Debug("[LLUDPServer] AddNewClient - already here");
                            return;
                        }
                    }
                    object[] array = new object[] {buffer, packet, sessionData};
                    if (m_asyncPacketHandling)
                        FireAndForget(HandleUseCircuitCode, array);
                    else
                        HandleUseCircuitCode(array);
                }
                else
                    // Don't create circuits for unauthorized clients
                    MainConsole.Instance.WarnFormat(
                        "[LLUDPServer]: Connection request for client {0} connecting with unnotified circuit code {1} from {2}",
                        cPacket.CircuitCode.ID, cPacket.CircuitCode.Code, remoteEndPoint);

                return;
            }

            // Determine which agent this packet came from
            IClientAPI client;
            if (!m_scene.ClientManager.TryGetValue(address, out client) || !(client is LLClientView))
            {
                if (client != null)
                    MainConsole.Instance.Warn("[LLUDPSERVER]: Received a " + packet.Type +
                                              " packet from an unrecognized source: " +
                                              address + " in " + m_scene.RegionInfo.RegionName);
                return;
            }

            udpClient = ((LLClientView) client).UDPClient;

            if (!udpClient.IsConnected)
                return;

            #endregion Packet to Client Mapping

            // Stats tracking
            Interlocked.Increment(ref udpClient.PacketsReceived);

            int now = Environment.TickCount & Int32.MaxValue;
            udpClient.TickLastPacketReceived = now;

            #region ACK Receiving

            // Handle appended ACKs
            if (packet.Header.AppendedAcks && packet.Header.AckList != null)
            {
                foreach (uint t in packet.Header.AckList)
                    udpClient.NeedAcks.Acknowledge(t, now, packet.Header.Resent);
            }

            // Handle PacketAck packets
            if (packet.Type == PacketType.PacketAck)
            {
                PacketAckPacket ackPacket = (PacketAckPacket) packet;

                foreach (PacketAckPacket.PacketsBlock t in ackPacket.Packets)
                    udpClient.NeedAcks.Acknowledge(t.ID, now, packet.Header.Resent);

                // We don't need to do anything else with PacketAck packets
                return;
            }

            #endregion ACK Receiving

            #region ACK Sending

            if (packet.Header.Reliable)
            {
                udpClient.PendingAcks.Enqueue(packet.Header.Sequence);

                // This is a somewhat odd sequence of steps to pull the client.BytesSinceLastACK value out,
                // add the current received bytes to it, test if 2*MTU bytes have been sent, if so remove
                // 2*MTU bytes from the value and send ACKs, and finally add the local value back to
                // client.BytesSinceLastACK. Lockless thread safety
                int bytesSinceLastACK = Interlocked.Exchange(ref udpClient.BytesSinceLastACK, 0);
                bytesSinceLastACK += buffer.DataLength;
                if (bytesSinceLastACK > MTU*2)
                {
                    bytesSinceLastACK -= MTU*2;
                    SendAcks(udpClient);
                }
                Interlocked.Add(ref udpClient.BytesSinceLastACK, bytesSinceLastACK);
            }

            #endregion ACK Sending

            #region Incoming Packet Accounting

            // Check the archive of received reliable packet IDs to see whether we already received this packet
            if (packet.Header.Reliable && !udpClient.PacketArchive.TryEnqueue(packet.Header.Sequence))
            {
                //if (packet.Header.Resent)
                //    MainConsole.Instance.Debug("[LLUDPSERVER]: Received a resend of already processed packet #" + packet.Header.Sequence + ", type: " + packet.Type);
                //else
                //    MainConsole.Instance.Warn("[LLUDPSERVER]: Received a duplicate (not marked as resend) of packet #" + packet.Header.Sequence + ", type: " + packet.Type);

                // Avoid firing a callback twice for the same packet
                return;
            }

            #endregion Incoming Packet Accounting

            #region BinaryStats

            LogPacketHeader(true, udpClient.CircuitCode, 0, packet.Type, (ushort) packet.Length);

            #endregion BinaryStats

            #region Ping Check Handling

            if (packet.Type == PacketType.StartPingCheck)
            {
                // We don't need to do anything else with ping checks
                StartPingCheckPacket startPing = (StartPingCheckPacket) packet;
                CompletePing(udpClient, startPing.PingID.PingID);

                if ((Environment.TickCount - m_elapsedMSSinceLastStatReport) >= 3000)
                {
                    udpClient.SendPacketStats();
                    m_elapsedMSSinceLastStatReport = Environment.TickCount;
                }
                return;
            }
            if (packet.Type == PacketType.CompletePingCheck)
            {
                // We don't currently track client ping times
                return;
            }

            #endregion Ping Check Handling

            // Inbox insertion
            packetInbox.Enqueue(new IncomingPacket(udpClient, packet));
        }

        private void HandleUseCircuitCode(object o)
        {
            MainConsole.Instance.Debug("[LLUDPServer] HandelUserCircuitCode");
            DateTime startTime = DateTime.Now;
            object[] array = (object[]) o;
            UDPPacketBuffer buffer = (UDPPacketBuffer) array[0];
            UseCircuitCodePacket packet = (UseCircuitCodePacket) array[1];
            AgentCircuitData sessionInfo = (AgentCircuitData) array[2];

            IPEndPoint remoteEndPoint = (IPEndPoint) buffer.RemoteEndPoint;

            // Begin the process of adding the client to the simulator
            if (AddClient(packet.CircuitCode.Code, packet.CircuitCode.ID,
                          packet.CircuitCode.SessionID, remoteEndPoint, sessionInfo))
            {
                uint ack = 0;
                lock (m_inQueueCircuitCodes)
                {
                    ack = (uint) m_inQueueCircuitCodes[sessionInfo.AgentID];
                    //And remove it
                    m_inQueueCircuitCodes.Remove(sessionInfo.AgentID);
                }
                // Acknowledge the UseCircuitCode packet
                //Make sure to acknowledge with the newest packet! So use the queue so that it knows all of the newest ones
                SendAckImmediate(remoteEndPoint, ack);

                MainConsole.Instance.InfoFormat(
                    "[LLUDPSERVER]: Handling UseCircuitCode request from {0} took {1}ms",
                    remoteEndPoint, (DateTime.Now - startTime).Milliseconds);
            }
        }

        private void SendAckImmediate(IPEndPoint remoteEndpoint, uint sequenceNumber)
        {
            PacketAckPacket ack = new PacketAckPacket
                                      {Header = {Reliable = false}, Packets = new PacketAckPacket.PacketsBlock[1]};
            ack.Packets[0] = new PacketAckPacket.PacketsBlock {ID = sequenceNumber};

            byte[] packetData = ack.ToBytes();
            int length = packetData.Length;

            UDPPacketBuffer buffer = new UDPPacketBuffer(remoteEndpoint, length) {DataLength = length};

            Buffer.BlockCopy(packetData, 0, buffer.Data, 0, length);

            //            AsyncBeginSend(buffer);
            SyncSend(buffer);
        }

        private bool IsClientAuthorized(UseCircuitCodePacket useCircuitCode, IPEndPoint remoteEndPoint,
                                        out AgentCircuitData sessionInfo)
        {
            UUID agentID = useCircuitCode.CircuitCode.ID;
            UUID sessionID = useCircuitCode.CircuitCode.SessionID;
            uint circuitCode = useCircuitCode.CircuitCode.Code;

            sessionInfo = m_circuitManager.AuthenticateSession(sessionID, agentID, circuitCode, remoteEndPoint);
            return sessionInfo != null;
        }

        private void ForEachInternalClient(Action<IClientAPI> action)
        {
            IClientAPI[] clients = m_currentClients.ToArray();
            foreach (IClientAPI client in clients)
                action(client);
        }

        protected virtual bool AddClient(uint circuitCode, UUID agentID, UUID sessionID, IPEndPoint remoteEndPoint,
                                         AgentCircuitData sessionInfo)
        {
            MainConsole.Instance.Debug("[LLUDPServer] AddClient-" + circuitCode + "-" + agentID + "-" + sessionID + "-" +
                                       remoteEndPoint +
                                       "-" + sessionInfo);
            IScenePresence SP;
            if (!m_scene.TryGetScenePresence(agentID, out SP))
            {
                // Create the LLUDPClient
                LLUDPClient udpClient = new LLUDPClient(this, m_throttleRates, m_throttle, circuitCode, agentID,
                                                        remoteEndPoint, m_defaultRTO, m_maxRTO);
                // Create the LLClientView
                LLClientView client = new LLClientView(remoteEndPoint, m_scene, this, udpClient, sessionInfo, agentID,
                                                       sessionID, circuitCode);
                client.OnLogout += LogoutHandler;

                // Start the IClientAPI
                m_scene.AddNewClient(client, BlankDelegate);
                m_currentClients.Add(client);
            }
            else
            {
                MainConsole.Instance.DebugFormat(
                    "[LLUDPSERVER]: Ignoring a repeated UseCircuitCode ({0}) from {1} at {2} ",
                    circuitCode, agentID, remoteEndPoint);
            }
            return true;
        }

        private void BlankDelegate()
        {
        }

        private void RemoveClient(LLUDPClient udpClient)
        {
            // Remove this client from the scene
            IClientAPI client;
            if (m_scene.ClientManager.TryGetValue(udpClient.AgentID, out client))
            {
                client.IsLoggingOut = true;
                IEntityTransferModule transferModule = m_scene.RequestModuleInterface<IEntityTransferModule>();
                if (transferModule != null)
                    transferModule.IncomingCloseAgent(m_scene, udpClient.AgentID);
                RemoveClient(client);
            }
        }

        protected void LogoutHandler(IClientAPI client)
        {
            client.SendLogoutPacket();
            if (client.IsActive)
                RemoveClient(((LLClientView) client).UDPClient);
            ILoginMonitor monitor =
                (ILoginMonitor)
                m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor("", MonitorModuleHelper.LoginMonitor);
            if (monitor != null)
            {
                monitor.AddLogout();
            }
        }

        private bool OutgoingPacketHandlerLoop()
        {
            if (!IsRunning)
                return false;
            if (!m_scene.ShouldRunHeartbeat)
                return false;

            // Typecast the function to an Action<IClientAPI> once here to avoid allocating a new
            // Action generic every round
            Action<IClientAPI> clientPacketHandler = ClientOutgoingPacketHandler;

            try
            {
                m_packetSent = false;

                #region Update Timers

                m_resendUnacked = false;
                m_sendAcks = false;
                m_sendPing = false;

                // Update elapsed time
                int thisTick = Environment.TickCount & Int32.MaxValue;
                if (m_tickLastOutgoingPacketHandler > thisTick)
                    m_elapsedMSOutgoingPacketHandler += ((Int32.MaxValue - m_tickLastOutgoingPacketHandler) + thisTick);
                else
                    m_elapsedMSOutgoingPacketHandler += (thisTick - m_tickLastOutgoingPacketHandler);

                m_tickLastOutgoingPacketHandler = thisTick;

                // Check for pending outgoing resends every 100ms
                if (m_elapsedMSOutgoingPacketHandler >= 100)
                {
                    m_resendUnacked = true;
                    m_elapsedMSOutgoingPacketHandler = 0;
                    m_elapsed100MSOutgoingPacketHandler += 1;
                }

                // Check for pending outgoing ACKs every 500ms
                if (m_elapsed100MSOutgoingPacketHandler >= 5)
                {
                    m_sendAcks = true;
                    m_elapsed100MSOutgoingPacketHandler = 0;
                    m_elapsed500MSOutgoingPacketHandler += 1;
                }

                // Send pings to clients every 5000ms
                if (m_elapsed500MSOutgoingPacketHandler >= 10)
                {
                    m_sendPing = true;
                    m_elapsed500MSOutgoingPacketHandler = 0;
                }

                #endregion Update Timers

                // Handle outgoing packets, resends, acknowledgements, and pings for each
                // client. m_packetSent will be set to true if a packet is sent
                ForEachInternalClient(clientPacketHandler);

                // If nothing was sent, sleep for the minimum amount of time before a
                // token bucket could get more tokens
                if (!m_packetSent)
                    Thread.Sleep((int) TickCountResolution);
            }
            catch (Exception ex)
            {
                MainConsole.Instance.ErrorFormat(
                    "[LLUDPSERVER]: OutgoingPacketHandler loop threw an exception: {0}", ex.ToString());
            }
            return true;
        }

        private void ClientOutgoingPacketHandler(IClientAPI client)
        {
            try
            {
                if (client is LLClientView)
                {
                    LLUDPClient udpClient = ((LLClientView) client).UDPClient;

                    if (udpClient.IsConnected)
                    {
                        if (m_resendUnacked)
                            ResendUnacked(udpClient);

                        if (m_sendAcks)
                            SendAcks(udpClient);

                        if (m_sendPing)
                            SendPing(udpClient);

                        // Dequeue any outgoing packets that are within the throttle limits
                        if (udpClient.DequeueOutgoing(MAX_PACKET_SEND))
                            // limit number of packets for each client per call
                            m_packetSent = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MainConsole.Instance.ErrorFormat("[LLUDPSERVER]: OutgoingPacketHandler iteration for " + client.Name +
                                           " threw an exception: " + ex.ToString());
                return;
            }
        }

        private bool IncomingPacketHandlerLoop()
        {
            if (!IsRunning)
                return false;
            if (!m_scene.ShouldRunHeartbeat)
                return false;

            // Set this culture for the thread that incoming packets are received
            // on to en-US to avoid number parsing issues
            Culture.SetCurrentCulture();

            try
            {
                IncomingPacket incomingPacket = null;

                // This is a test to try and rate limit packet handling on Mono.
                // If it works, a more elegant solution can be devised
                if (Environment.OSVersion.Platform == PlatformID.Unix && Util.FireAndForgetCount() < 2)
                {
                    //MainConsole.Instance.Debug("[LLUDPSERVER]: Incoming packet handler is sleeping");
                    Thread.Sleep(30);
                }

                if (packetInbox.Dequeue(100, ref incomingPacket))
                    ProcessInPacket(incomingPacket);
            }
            catch (Exception ex)
            {
                MainConsole.Instance.ErrorFormat("[LLUDPSERVER]: Error in the incoming packet handler loop: " + ex.ToString());
            }
            return true;
        }

        private void ProcessInPacket(object state)
        {
            IncomingPacket incomingPacket = (IncomingPacket) state;
            Packet packet = incomingPacket.Packet;
            LLUDPClient udpClient = incomingPacket.Client;
            IClientAPI client;

            // Sanity check
            if (packet == null || udpClient == null)
            {
                MainConsole.Instance.WarnFormat(
                    "[LLUDPSERVER]: Processing a packet with incomplete state. Packet=\"{0}\", UDPClient=\"{1}\"",
                    packet, udpClient);
            }

            // Make sure this client is still alive
            if (udpClient != null && m_scene.ClientManager.TryGetValue(udpClient.AgentID, out client))
            {
                try
                {
                    // Process this packet
                    client.ProcessInPacket(packet);
                }
                catch (Exception e)
                {
                    if (e.Message == "Closing")
                    {
                        // Don't let a failure in an individual client thread crash the whole sim.
                        if (packet != null)
                            MainConsole.Instance.ErrorFormat(
                                "[LLUDPSERVER]: Client packet handler for {0} for packet {1} threw an exception: {2}",
                                udpClient.AgentID, packet.Type, e.ToString());
                    }
                }
            }
            else
            {
                if (packet != null)
                    if (udpClient != null)
                        MainConsole.Instance.DebugFormat(
                            "[LLUDPSERVER]: Dropping incoming {0} packet for dead client {1}", packet.Type,
                            udpClient.AgentID);
            }
        }

        #region BinaryStats

        public static PacketLogger PacketLog;

        protected static bool m_shouldCollectStats;
        // Number of seconds to log for
        private static TimeSpan binStatsMaxFilesize = TimeSpan.FromSeconds(300);
        private static readonly object binStatsLogLock = new object();
        private static string binStatsDir = "";

        public static void LogPacketHeader(bool incoming, uint circuit, byte flags, PacketType packetType, ushort size)
        {
            if (!m_shouldCollectStats) return;

            // Binary logging format is TTTTTTTTCCCCFPPPSS, T=Time, C=Circuit, F=Flags, P=PacketType, S=size

            // Put the incoming bit into the least significant bit of the flags byte
            if (incoming)
                flags |= 0x01;
            else
                flags &= 0xFE;

            // Put the flags byte into the most significant bits of the type integer
            uint type = (uint) packetType;
            type |= (uint) flags << 24;

            // MainConsole.Instance.Debug("1 LogPacketHeader(): Outside lock");
            lock (binStatsLogLock)
            {
                DateTime now = DateTime.Now;

                // MainConsole.Instance.Debug("2 LogPacketHeader(): Inside lock. now is " + now.Ticks);
                try
                {
                    if (PacketLog == null || (now > PacketLog.StartTime + binStatsMaxFilesize))
                    {
                        if (PacketLog != null && PacketLog.Log != null)
                        {
                            PacketLog.Log.Close();
                        }

                        // First log file or time has expired, start writing to a new log file
                        PacketLog = new PacketLogger
                                        {
                                            StartTime = now,
                                            Path = (binStatsDir.Length > 0
                                                        ? binStatsDir + Path.DirectorySeparatorChar.ToString()
                                                        : "")
                                                   + String.Format("packets-{0}.log", now.ToString("yyyyMMddHHmmss"))
                                        };
                        PacketLog.Log = new BinaryWriter(File.Open(PacketLog.Path, FileMode.Append, FileAccess.Write));
                    }

                    // Serialize the data
                    byte[] output = new byte[18];
                    Buffer.BlockCopy(BitConverter.GetBytes(now.Ticks), 0, output, 0, 8);
                    Buffer.BlockCopy(BitConverter.GetBytes(circuit), 0, output, 8, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(type), 0, output, 12, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(size), 0, output, 16, 2);

                    // Write the serialized data to disk
                    if (PacketLog != null && PacketLog.Log != null)
                        PacketLog.Log.Write(output);
                }
                catch (Exception ex)
                {
                    MainConsole.Instance.ErrorFormat("Packet statistics gathering failed: ", ex.ToString());
                    if (PacketLog != null && PacketLog.Log != null)
                    {
                        PacketLog.Log.Close();
                    }
                    PacketLog = null;
                }
            }
        }

        public class PacketLogger
        {
            public BinaryWriter Log;
            public string Path;
            public DateTime StartTime;
        }

        #endregion BinaryStats
    }
}