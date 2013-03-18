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

//#define Debug

using Aurora.Framework;
using OpenMetaverse;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;

namespace Aurora.ClientStack
{

    #region Delegates

    /// <summary>
    ///   Fired when updated networking stats are produced for this client
    /// </summary>
    /// <param name = "inPackets">Number of incoming packets received since this
    ///   event was last fired</param>
    /// <param name = "outPackets">Number of outgoing packets sent since this
    ///   event was last fired</param>
    /// <param name = "unAckedBytes">Current total number of bytes in packets we
    ///   are waiting on ACKs for</param>
    public delegate void PacketStats(int inPackets, int outPackets, int unAckedBytes);

    /// <summary>
    ///   Fired when the queue for one or more packet categories is empty. This 
    ///   event can be hooked to put more data on the empty queues
    /// </summary>
    public delegate void QueueEmpty(object o);

    #endregion Delegates

    public class UDPprioQueue
    {
        public int Count;
        public int nlevels;
        public int[] promotioncntr;
        public int promotionratemask;
        public ConcurrentQueue<object>[] queues;

        public UDPprioQueue(int NumberOfLevels, int PromRateMask)
        {
            // PromRatemask:  0x03 promotes on each 4 calls, 0x1 on each 2 calls etc
            nlevels = NumberOfLevels;
            queues = new ConcurrentQueue<object>[nlevels];
            promotioncntr = new int[nlevels];
            for (int i = 0; i < nlevels; i++)
            {
                queues[i] = new ConcurrentQueue<object>();
                promotioncntr[i] = 0;
            }
            promotionratemask = PromRateMask;
        }

        public bool Enqueue(int prio, object o)
            // object so it can be a complex info with methods to call etc to get packets on dequeue 
        {
            if (prio < 0 || prio >= nlevels) // safe than sorrow
                return false;

            queues[prio].Enqueue(o); // store it in its level
            Interlocked.Increment(ref Count);

            Interlocked.Increment(ref promotioncntr[prio]);


            if ((promotioncntr[prio] & promotionratemask) == 0)
                // keep top free of lower priority things
                // time to move objects up in priority
                // so they don't get stalled if high trafic on higher levels               
            {
                int i = prio;

                while (--i >= 0)
                {
                    object ob;
                    if(queues[i].TryDequeue(out ob))
                        queues[i + 1].Enqueue(ob);
                }
            }

            return true;
        }

        public bool Dequeue(out OutgoingPacket pack)
        {
            int i = nlevels;

            while (--i >= 0) // go down levels looking for data
            {
                object o;
                if (!queues[i].TryDequeue(out o)) continue;
                if (!(o is OutgoingPacket)) continue;
                pack = (OutgoingPacket) o;
                Interlocked.Decrement(ref Count);
                return true;
                // else  do call to a funtion that will return the packet or whatever
            }

            pack = null;
            return false;
        }
    }

    /// <summary>
    ///   Tracks state for a client UDP connection and provides client-specific methods
    /// </summary>
    public sealed class LLUDPClient
    {
        /// <summary>
        ///   Percentage of the task throttle category that is allocated to avatar and prim
        ///   state updates
        /// </summary>
        private const float STATE_TASK_PERCENTAGE = 0.3f;

        private const float TRANSFER_ASSET_PERCENTAGE = 0.9f;
        private const float AVATAR_INFO_STATE_PERCENTAGE = 0.5f;
        private const int MAXPERCLIENTRATE = 625000;
        private const int MINPERCLIENTRATE = 6250;
        private const int STARTPERCLIENTRATE = 25000;
        private const int MAX_PACKET_SKIP_RATE = 4;

        /// <summary>
        ///   AgentID for this client
        /// </summary>
        public readonly UUID AgentID;

        /// <summary>
        ///   Circuit code that this client is connected on
        /// </summary>
        public readonly uint CircuitCode;

        /// <summary>
        ///   Packets we have sent that need to be ACKed by the client
        /// </summary>
        public readonly UnackedPacketCollection NeedAcks = new UnackedPacketCollection();

        /// <summary>
        ///   Sequence numbers of packets we've received (for duplicate checking)
        /// </summary>
        public readonly IncomingPacketHistoryCollection PacketArchive = new IncomingPacketHistoryCollection(200);

        //        private readonly TokenBucket[] m_throttleCategories;
        /// <summary>
        ///   Throttle buckets for each packet category
        /// </summary>
        /// <summary>
        ///   Outgoing queues for throttled packets
        /// </summary>
//        private readonly Aurora.Framework.LocklessQueue<OutgoingPacket>[] m_packetOutboxes = new Aurora.Framework.LocklessQueue<OutgoingPacket>[(int)ThrottleOutPacketType.Count];
        private readonly int[] PacketsCounts = new int[(int) ThrottleOutPacketType.Count];

        /// <summary>
        ///   ACKs that are queued up, waiting to be sent to the client
        /// </summary>
        public readonly ConcurrentQueue<uint> PendingAcks = new ConcurrentQueue<uint>();

        private readonly int[] Rates;

        /// <summary>
        ///   The remote address of the connected client
        /// </summary>
        public readonly IPEndPoint RemoteEndPoint;

        private readonly int m_defaultRTO = 1000;
        private readonly int m_maxRTO = 20000;

        private readonly UDPprioQueue m_outbox = new UDPprioQueue(8, 0x01);
                                      // 8  priority levels (7 max , 0 lowest), autopromotion on every 2 enqueues

        /// <summary>
        ///   Throttle bucket for this agent's connection
        /// </summary>
        private readonly TokenBucket m_throttle;

        /// <summary>
        ///   A reference to the LLUDPServer that is managing this client
        /// </summary>
        private readonly LLUDPServer m_udpServer;

        /// <summary>
        ///   Number of bytes received since the last acknowledgement was sent out. This is used
        ///   to loosely follow the TCP delayed ACK algorithm in RFC 1122 (4.2.3.2)
        /// </summary>
        public int BytesSinceLastACK;

        /// <summary>
        ///   Current ping sequence number
        /// </summary>
        public byte CurrentPingSequence;

        /// <summary>
        ///   Current packet sequence number
        /// </summary>
        public int CurrentSequence;

        /// <summary>
        ///   True when this connection is alive, otherwise false
        /// </summary>
        public bool IsConnected = true;

        /// <summary>
        ///   True when this connection is paused, otherwise false
        /// </summary>
        public bool IsPaused;

        public int[] MapCatsToPriority = new int[(int) ThrottleOutPacketType.Count];

        /// <summary>
        ///   Number of packets received from this client
        /// </summary>
        public int PacketsReceived;

        /// <summary>
        ///   Total byte count of unacked packets sent to this client
        /// </summary>
        public int PacketsResent;

        /// <summary>
        ///   Number of packets sent to this client
        /// </summary>
        public int PacketsSent;

        /// <summary>
        ///   Retransmission timeout. Packets that have not been acknowledged in this number of
        ///   milliseconds or longer will be resent
        /// </summary>
        /// <remarks>
        ///   Calculated from <seealso cref = "SRTT" /> and <seealso cref = "RTTVAR" /> using the
        ///   guidelines in RFC 2988
        /// </remarks>
        public int RTO;

        /// <summary>
        ///   Round-trip time variance. Measures the consistency of round-trip times
        /// </summary>
        public float RTTVAR;

        /// <summary>
        ///   Smoothed round-trip time. A smoothed average of the round-trip time for sending a
        ///   reliable packet to the client and receiving an ACK
        /// </summary>
        public float SRTT;

        /// <summary>
        ///   Environment.TickCount when the last packet was received for this client
        /// </summary>
        public int TickLastPacketReceived;

        private int TotalRateMin;
        private int TotalRateRequested;

        /// <summary>
        ///   Total byte count of unacked packets sent to this client
        /// </summary>
        public int UnackedBytes;

        /// <summary>
        ///   Holds the Environment.TickCount value of when the next OnQueueEmpty can be fired
        /// </summary>
        private int m_nextOnQueueEmpty = 1;


        private OutgoingPacket m_nextOutPacket;

        /// <summary>
        ///   Caches packed throttle information
        /// </summary>
        private byte[] m_packedThrottles;

        /// <summary>
        ///   Total number of received packets that we have reported to the OnPacketStats event(s)
        /// </summary>
        private int m_packetsReceivedReported;

        /// <summary>
        ///   Total number of sent packets that we have reported to the OnPacketStats event(s)
        /// </summary>
        private int m_packetsSentReported;

        /// <summary>
        ///   Default constructor
        /// </summary>
        /// <param name = "server">Reference to the UDP server this client is connected to</param>
        /// <param name = "rates">Default throttling rates and maximum throttle limits</param>
        /// <param name = "parentThrottle">Parent HTB (hierarchical token bucket)
        ///   that the child throttles will be governed by</param>
        /// <param name = "circuitCode">Circuit code for this connection</param>
        /// <param name = "agentID">AgentID for the connected agent</param>
        /// <param name = "remoteEndPoint">Remote endpoint for this connection</param>
        /// <param name="defaultRTO"></param>
        /// <param name="maxRTO"></param>
        public LLUDPClient(LLUDPServer server, ThrottleRates rates, TokenBucket parentThrottle, uint circuitCode,
                           UUID agentID, IPEndPoint remoteEndPoint, int defaultRTO, int maxRTO)
        {
            AgentID = agentID;
            RemoteEndPoint = remoteEndPoint;
            CircuitCode = circuitCode;
            m_udpServer = server;
            if (defaultRTO != 0)
                m_defaultRTO = defaultRTO;
            if (maxRTO != 0)
                m_maxRTO = maxRTO;

            // Create a token bucket throttle for this client that has the scene token bucket as a parent
            m_throttle = new TokenBucket(parentThrottle, rates.TotalLimit, 0);

            // remember the rates the client requested
            Rates = new int[(int) ThrottleOutPacketType.Count];

            for (int i = 0; i < (int) ThrottleOutPacketType.Count; i++)
            {
                PacketsCounts[i] = 0;
            }

            //Set the priorities for the different packet types
            //Higher is more important
            MapCatsToPriority[(int) ThrottleOutPacketType.Resend] = 7;
            MapCatsToPriority[(int) ThrottleOutPacketType.Land] = 1;
            MapCatsToPriority[(int) ThrottleOutPacketType.Wind] = 0;
            MapCatsToPriority[(int) ThrottleOutPacketType.Cloud] = 0;
            MapCatsToPriority[(int) ThrottleOutPacketType.Task] = 4;
            MapCatsToPriority[(int) ThrottleOutPacketType.Texture] = 2;
            MapCatsToPriority[(int) ThrottleOutPacketType.Asset] = 3;
            MapCatsToPriority[(int) ThrottleOutPacketType.Transfer] = 5;
            MapCatsToPriority[(int) ThrottleOutPacketType.State] = 5;
            MapCatsToPriority[(int) ThrottleOutPacketType.AvatarInfo] = 6;
            MapCatsToPriority[(int) ThrottleOutPacketType.OutBand] = 7;

            // Default the retransmission timeout to one second
            RTO = m_defaultRTO;

            // Initialize this to a sane value to prevent early disconnects
            TickLastPacketReceived = Environment.TickCount & Int32.MaxValue;
        }

        /// <summary>
        ///   Fired when updated networking stats are produced for this client
        /// </summary>
        public event PacketStats OnPacketStats;

        /// <summary>
        ///   Fired when the queue for a packet category is empty. This event can be
        ///   hooked to put more data on the empty queue
        /// </summary>
        public event QueueEmpty OnQueueEmpty;

        /// <summary>
        ///   Shuts down this client connection
        /// </summary>
        public void Shutdown()
        {
            IsConnected = false;

            for (int i = 0; i < (int) ThrottleOutPacketType.Count; i++)
            {
                PacketsCounts[i] = 0;
            }
            OnPacketStats = null;
            OnQueueEmpty = null;
        }

        public string GetStats()
        {
            return string.Format(
                "{0,7} {1,7} {2,7} {3,9} {4,7} {5,7} {6,7} {7,7} {8,7} {9,8} {10,7} {11,7}",
                PacketsReceived,
                PacketsSent,
                PacketsResent,
                UnackedBytes,
                PacketsCounts[(int) ThrottleOutPacketType.Resend],
                PacketsCounts[(int) ThrottleOutPacketType.Land],
                PacketsCounts[(int) ThrottleOutPacketType.Wind],
                PacketsCounts[(int) ThrottleOutPacketType.Cloud],
                PacketsCounts[(int) ThrottleOutPacketType.Task],
                PacketsCounts[(int) ThrottleOutPacketType.Texture],
                PacketsCounts[(int) ThrottleOutPacketType.Asset],
                PacketsCounts[(int) ThrottleOutPacketType.State],
                PacketsCounts[(int) ThrottleOutPacketType.OutBand]
                );
        }

        public void SendPacketStats()
        {
            PacketStats callback = OnPacketStats;
            if (callback != null)
            {
                int newPacketsReceived = PacketsReceived - m_packetsReceivedReported;
                int newPacketsSent = PacketsSent - m_packetsSentReported;

                callback(newPacketsReceived, newPacketsSent, UnackedBytes);

                m_packetsReceivedReported += newPacketsReceived;
                m_packetsSentReported += newPacketsSent;
            }
        }

        public void SlowDownSend()
        {
            float tmp = m_throttle.MaxBurst*0.95f;
            if (tmp < TotalRateMin)
                tmp = TotalRateMin;
            m_throttle.MaxBurst = (int) tmp;
            m_throttle.DripRate = (int) tmp;
        }

        public void SetThrottles(byte[] throttleData)
        {
            byte[] adjData;
            int pos = 0;

            if (!BitConverter.IsLittleEndian)
            {
                byte[] newData = new byte[7*4];
                Buffer.BlockCopy(throttleData, 0, newData, 0, 7*4);

                for (int i = 0; i < 7; i++)
                    Array.Reverse(newData, i*4, 4);

                adjData = newData;
            }
            else
            {
                adjData = throttleData;
            }

            // 0.125f converts from bits to bytes
            int resend = (int) (BitConverter.ToSingle(adjData, pos)*0.125f);
            pos += 4;
            int land = (int) (BitConverter.ToSingle(adjData, pos)*0.125f);
            pos += 4;
            int wind = (int) (BitConverter.ToSingle(adjData, pos)*0.125f);
            pos += 4;
            int cloud = (int) (BitConverter.ToSingle(adjData, pos)*0.125f);
            pos += 4;
            int task = (int) (BitConverter.ToSingle(adjData, pos)*0.125f);
            pos += 4;
            int texture = (int) (BitConverter.ToSingle(adjData, pos)*0.125f);
            pos += 4;
            int asset = (int) (BitConverter.ToSingle(adjData, pos)*0.125f);

            int total = resend + land + wind + cloud + task + texture + asset;

            // These are subcategories of task that we allocate a percentage to
            int state = (int) (task*STATE_TASK_PERCENTAGE);
            task -= state;

            int transfer = (int) (asset*TRANSFER_ASSET_PERCENTAGE);
            asset -= transfer;

            // avatar info cames out from state
            int avatarinfo = (int) (state*AVATAR_INFO_STATE_PERCENTAGE);
            state -= avatarinfo;

//            int total = resend + land + wind + cloud + task + texture + asset + state + avatarinfo;

            // Make sure none of the throttles are set below our packet MTU,
            // otherwise a throttle could become permanently clogged


            Rates[(int) ThrottleOutPacketType.Resend] = resend;
            Rates[(int) ThrottleOutPacketType.Land] = land;
            Rates[(int) ThrottleOutPacketType.Wind] = wind;
            Rates[(int) ThrottleOutPacketType.Cloud] = cloud;
            Rates[(int) ThrottleOutPacketType.Task] = task + state + avatarinfo;
            Rates[(int) ThrottleOutPacketType.Texture] = texture;
            Rates[(int) ThrottleOutPacketType.Asset] = asset + transfer;
            Rates[(int) ThrottleOutPacketType.State] = state;

            TotalRateRequested = total;
            TotalRateMin = (int) (total*0.1);
            if (TotalRateMin < MINPERCLIENTRATE)
                TotalRateMin = MINPERCLIENTRATE;
            total = TotalRateMin; // let it grow slowlly


            //MainConsole.Instance.WarnFormat("[LLUDPCLIENT]: {0} is setting throttles. Resend={1}, Land={2}, Wind={3}, Cloud={4}, Task={5}, Texture={6}, Asset={7}, State={8}, AvatarInfo={9}, Transfer={10}, TaskFull={11}, Total={12}",
            //    AgentID, resend, land, wind, cloud, task, texture, asset, state, avatarinfo, transfer, task + state + avatarinfo, total);

            // Update the token buckets with new throttle values

            TokenBucket bucket = m_throttle;
            bucket.DripRate = total;
            bucket.MaxBurst = total;
            // Reset the packed throttles cached data
            m_packedThrottles = null;
        }

        public byte[] GetThrottlesPacked(float multiplier)
        {
            byte[] data = m_packedThrottles;

            if (data == null)
            {
                data = new byte[7*4];
                int i = 0;

                Buffer.BlockCopy(Utils.FloatToBytes((float) Rates[(int) ThrottleOutPacketType.Resend]*8*multiplier), 0,
                                 data, i, 4);
                i += 4;
                Buffer.BlockCopy(Utils.FloatToBytes((float) Rates[(int) ThrottleOutPacketType.Land]*8*multiplier), 0,
                                 data, i, 4);
                i += 4;
                Buffer.BlockCopy(Utils.FloatToBytes((float) Rates[(int) ThrottleOutPacketType.Wind]*8*multiplier), 0,
                                 data, i, 4);
                i += 4;
                Buffer.BlockCopy(Utils.FloatToBytes((float) Rates[(int) ThrottleOutPacketType.Cloud]*8*multiplier), 0,
                                 data, i, 4);
                i += 4;
                Buffer.BlockCopy(Utils.FloatToBytes((float) Rates[(int) ThrottleOutPacketType.Task]*8*multiplier), 0,
                                 data, i, 4);
                i += 4;
                Buffer.BlockCopy(Utils.FloatToBytes((float) Rates[(int) ThrottleOutPacketType.Texture]*8*multiplier), 0,
                                 data, i, 4);
                i += 4;
                Buffer.BlockCopy(Utils.FloatToBytes((float) Rates[(int) ThrottleOutPacketType.Asset]*8*multiplier), 0,
                                 data, i, 4);

                m_packedThrottles = data;
            }

            return data;
        }

        public bool EnqueueOutgoing(OutgoingPacket packet)
        {
            int category = (int) packet.Category;

            if (category >= 0 && category < (int) ThrottleOutPacketType.Count)
            {
                //All packets are enqueued, except those that don't have a queue
                int prio = MapCatsToPriority[category];
                m_outbox.Enqueue(prio, packet);
                return true;
            }
            // We don't have a token bucket for this category,
            //  so it will not be queued and sent immediately
            return false;
        }

        /// <summary>
        ///   tries to send queued packets
        /// </summary>
        /// <remarks>
        ///   This function is only called from a synchronous loop in the
        ///   UDPServer so we don't need to bother making this thread safe
        /// </remarks>
        /// <returns>True if any packets were sent, otherwise false</returns>
        public bool DequeueOutgoing(int MaxNPacks)
        {
            bool packetSent = false;

            for (int i = 0; i < MaxNPacks; i++)
            {
                OutgoingPacket packet;
                if (m_nextOutPacket != null)
                {
                    packet = m_nextOutPacket;
                    if (m_throttle.RemoveTokens(packet.Buffer.DataLength))
                    {
                        // Send the packet
                        m_udpServer.SendPacketFinal(packet);
                        m_nextOutPacket = null;
                        packetSent = true;
                    }
                }
                    // No dequeued packet waiting to be sent, try to pull one off
                    // this queue
                else if (m_outbox.Dequeue(out packet))
                {
                    MainConsole.Instance.Output(AgentID + " - " + packet.Packet.Type, "Verbose");
                    // A packet was pulled off the queue. See if we have
                    // enough tokens in the bucket to send it out
                    if (packet.Category == ThrottleOutPacketType.OutBand ||
                        m_throttle.RemoveTokens(packet.Buffer.DataLength))
                    {
                        packetSent = true;
                        //Send the packet
                        PacketsCounts[(int) packet.Category] += packet.Packet.Length;
                        m_udpServer.SendPacketFinal(packet);
                        PacketsSent++;
                    }
                    else
                    {
                        m_nextOutPacket = packet;
                        break;
                    }
                }
                else
                    break;
            }

            if (packetSent)
            {
                if (m_throttle.MaxBurst < TotalRateRequested)
                {
                    float tmp = m_throttle.MaxBurst*1.005f;
                    m_throttle.DripRate = (int) tmp;
                    m_throttle.MaxBurst = (int) tmp;
                }
            }


            if (m_nextOnQueueEmpty != 0 && Util.EnvironmentTickCountSubtract(m_nextOnQueueEmpty) >= 0)
            {
                // Use a value of 0 to signal that FireQueueEmpty is running
                m_nextOnQueueEmpty = 0;
                // Asynchronously run the callback
                int ptmp = m_outbox.queues[MapCatsToPriority[(int) ThrottleOutPacketType.Task]].Count;
                int atmp = m_outbox.queues[MapCatsToPriority[(int) ThrottleOutPacketType.AvatarInfo]].Count;
                int ttmp = m_outbox.queues[MapCatsToPriority[(int) ThrottleOutPacketType.Texture]].Count;
                int[] arg = {ptmp, atmp, ttmp};
                Util.FireAndForget(FireQueueEmpty, arg);
            }

            return packetSent;
        }

        public int GetCurTexPacksInQueue()
        {
            return m_outbox.queues[MapCatsToPriority[(int) ThrottleOutPacketType.Texture]].Count;
        }

        public int GetCurTaskPacksInQueue()
        {
            return m_outbox.queues[MapCatsToPriority[(int) ThrottleOutPacketType.Task]].Count;
        }

        /// <summary>
        ///   Called when an ACK packet is received and a round-trip time for a
        ///   packet is calculated. This is used to calculate the smoothed
        ///   round-trip time, round trip time variance, and finally the
        ///   retransmission timeout
        /// </summary>
        /// <param name = "r">Round-trip time of a single packet and its
        ///   acknowledgement</param>
        public void UpdateRoundTrip(float r)
        {
            const float ALPHA = 0.125f;
            const float BETA = 0.25f;
            const float K = 4.0f;

            if (RTTVAR == 0.0f)
            {
                // First RTT measurement
                SRTT = r;
                RTTVAR = r*0.5f;
            }
            else
            {
                // Subsequence RTT measurement
                RTTVAR = (1.0f - BETA)*RTTVAR + BETA*Math.Abs(SRTT - r);
                SRTT = (1.0f - ALPHA)*SRTT + ALPHA*r;
            }

            int rto = (int) (SRTT + Math.Max(m_udpServer.TickCountResolution, K*RTTVAR));

            // Clamp the retransmission timeout to manageable values
            rto = Utils.Clamp(rto, m_defaultRTO, m_maxRTO);

            RTO = rto;

            //MainConsole.Instance.Debug("[LLUDPCLIENT]: Setting agent " + this.Agent.FullName + "'s RTO to " + RTO + "ms with an RTTVAR of " +
            //    RTTVAR + " based on new RTT of " + r + "ms");
        }

        /// <summary>
        ///   Exponential backoff of the retransmission timeout, per section 5.5
        ///   of RFC 2988
        /// </summary>
        public void BackoffRTO()
        {
            // Reset SRTT and RTTVAR, we assume they are bogus since things
            // didn't work out and we're backing off the timeout
            SRTT = 0.0f;
            RTTVAR = 0.0f;

            // Double the retransmission timeout
            RTO = Math.Min(RTO*2, m_maxRTO);
        }

        /// <summary>
        ///   Fires the OnQueueEmpty callback and sets the minimum time that it
        ///   can be called again
        /// </summary>
        /// <param name = "o">Throttle categories to fire the callback for,
        ///   stored as an object to match the WaitCallback delegate
        ///   signature</param>
        private void FireQueueEmpty(object o)
        {
            const int MIN_CALLBACK_MS = 30;

            QueueEmpty callback = OnQueueEmpty;

            int start = Util.EnvironmentTickCount();

            if (callback != null)
            {
                try
                {
                    callback(o);
                }
                catch (Exception e)
                {
                    MainConsole.Instance.Error("[LLUDPCLIENT]: OnQueueEmpty() threw an exception: " + e.Message, e);
                }
            }

            m_nextOnQueueEmpty = start + MIN_CALLBACK_MS;
//            if (m_nextOnQueueEmpty == 0)
//                m_nextOnQueueEmpty = 1;
        }
    }
}