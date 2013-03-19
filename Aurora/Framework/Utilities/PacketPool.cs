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

using System;
using System.Collections.Generic;
using Aurora.Framework.ConsoleFramework;
using OpenMetaverse;
using OpenMetaverse.Packets;

namespace Aurora.Framework.Utilities
{
    public sealed class PacketPool
    {
        private static readonly PacketPool instance = new PacketPool();

        private static readonly Dictionary<Type, Stack<Object>> DataBlocks =
            new Dictionary<Type, Stack<Object>>();

        private readonly object m_poolLock = new object();
        private readonly Dictionary<int, Stack<Packet>> pool = new Dictionary<int, Stack<Packet>>();
        private bool dataBlockPoolEnabled = true;
        private bool packetPoolEnabled = true;

        public static PacketPool Instance
        {
            get { return instance; }
        }

        public bool RecyclePackets
        {
            set { packetPoolEnabled = value; }
            get { return packetPoolEnabled; }
        }

        public bool RecycleDataBlocks
        {
            set { dataBlockPoolEnabled = value; }
            get { return dataBlockPoolEnabled; }
        }

        /// <summary>
        ///     For outgoing packets that just have the packet type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Packet GetPacket(PacketType type)
        {
            int t = (int) type;
            Packet packet;

            if (!packetPoolEnabled)
                return Packet.BuildPacket(type);

            lock (m_poolLock)
            {
                if (!pool.ContainsKey(t) || (pool[t]).Count == 0)
                {
                    // Creating a new packet if we cannot reuse an old package
                    packet = Packet.BuildPacket(type);
                }
                else
                {
                    // Recycle old packages
#if Debug
                    MainConsole.Instance.Info("[PacketPool]: Using " + type);
#endif
                    packet = (pool[t]).Pop();
                }
            }

            return packet;
        }

        private static PacketType GetType(byte[] bytes)
        {
            byte[] decoded_header = new byte[10 + 8];
            ushort id;
            PacketFrequency freq;

            if ((bytes[0] & Helpers.MSG_ZEROCODED) != 0)
            {
                Helpers.ZeroDecode(bytes, 16, decoded_header);
            }
            else
            {
                Buffer.BlockCopy(bytes, 0, decoded_header, 0, 10);
            }

            if (decoded_header[6] == 0xFF)
            {
                if (decoded_header[7] == 0xFF)
                {
                    id = (ushort) ((decoded_header[8] << 8) + decoded_header[9]);
                    freq = PacketFrequency.Low;
                }
                else
                {
                    id = decoded_header[7];
                    freq = PacketFrequency.Medium;
                }
            }
            else
            {
                id = decoded_header[6];
                freq = PacketFrequency.High;
            }

            return Packet.GetType(id, freq);
        }

        /// <summary>
        ///     For incoming packets that are just types
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="packetEnd"></param>
        /// <param name="zeroBuffer"></param>
        /// <returns></returns>
        public Packet GetPacket(byte[] bytes, ref int packetEnd, byte[] zeroBuffer)
        {
            PacketType type = GetType(bytes);

            if (zeroBuffer != null)
                Array.Clear(zeroBuffer, 0, zeroBuffer.Length);

            int i = 0;
            Packet packet = GetPacket(type);
            if (packet == null)
                MainConsole.Instance.WarnFormat("[PACKETPOOL]: Failed to get packet of type {0}", type);
            else
                packet.FromBytes(bytes, ref i, ref packetEnd, zeroBuffer);
            return packet;
        }

        /// <summary>
        ///     Return a packet to the packet pool
        /// </summary>
        /// <param name="packet"></param>
        public bool ReturnPacket(Packet packet)
        {
            /*if (dataBlockPoolEnabled)
            {
                switch (packet.Type)
                {
                    case PacketType.ObjectUpdate:
                        ObjectUpdatePacket oup = (ObjectUpdatePacket)packet;

                        foreach (ObjectUpdatePacket.ObjectDataBlock oupod in oup.ObjectData)
                            ReturnDataBlock<ObjectUpdatePacket.ObjectDataBlock>(oupod);
                        oup.ObjectData = null;
                        break;

                    case PacketType.ImprovedTerseObjectUpdate:
                        ImprovedTerseObjectUpdatePacket itoup =
                                (ImprovedTerseObjectUpdatePacket)packet;

                        foreach(ImprovedTerseObjectUpdatePacket.ObjectDataBlock itoupod in itoup.ObjectData)
                            ReturnDataBlock<ImprovedTerseObjectUpdatePacket.ObjectDataBlock>(itoupod);
                        itoup.ObjectData = null;
                        break;
                }
            }*/

            if (packetPoolEnabled)
            {
                switch (packet.Type)
                {
                        // List pooling packets here

                    case PacketType.ObjectUpdate:
                        lock (m_poolLock)
                        {
                            //Special case, this packet gets sent as a ObjectUpdate for both compressed and non compressed
                            int t = (int) packet.Type;
                            if (packet is ObjectUpdateCompressedPacket)
                                t = (int) PacketType.ObjectUpdateCompressed;

#if Debug
                            MainConsole.Instance.Info("[PacketPool]: Returning " + type);
#endif

                            if (!pool.ContainsKey(t))
                                pool[t] = new Stack<Packet>();

                            if ((pool[t]).Count < 50)
                                (pool[t]).Push(packet);
                        }
                        return true;
                        //Outgoing packets:
                    case PacketType.ObjectUpdateCompressed:
                    case PacketType.ObjectUpdateCached:
                    case PacketType.ImprovedTerseObjectUpdate:
                    case PacketType.ObjectDelete:
                    case PacketType.LayerData:
                    case PacketType.FetchInventoryReply:
                    case PacketType.PacketAck:
                    case PacketType.StartPingCheck:
                    case PacketType.CompletePingCheck:
                    case PacketType.InventoryDescendents:
                        //Incoming packets:
                    case PacketType.AgentUpdate:
                    case PacketType.AgentAnimation:
                    case PacketType.AvatarAnimation:
                    case PacketType.CoarseLocationUpdate:
                    case PacketType.ImageData:
                    case PacketType.ImagePacket:
                    case PacketType.MapBlockReply:
                    case PacketType.MapBlockRequest:
                    case PacketType.MapItemReply:
                    case PacketType.MapItemRequest:
                    case PacketType.SendXferPacket:
                    case PacketType.TransferPacket:
                        lock (m_poolLock)
                        {
                            int t = (int) packet.Type;

#if Debug
                            MainConsole.Instance.Info("[PacketPool]: Returning " + type);
#endif

                            if (!pool.ContainsKey(t))
                                pool[t] = new Stack<Packet>();

                            if ((pool[t]).Count < 50)
                                (pool[t]).Push(packet);
                        }
                        return true;

                        // Other packets wont pool
                    default:
                        break;
                }
            }
            return false;
        }

        public static T GetDataBlock<T>() where T : new()
        {
            lock (DataBlocks)
            {
                Stack<Object> s;

                if (DataBlocks.TryGetValue(typeof (T), out s))
                {
                    if (s.Count > 0)
                        return (T) s.Pop();
                }
                else
                {
                    DataBlocks[typeof (T)] = new Stack<Object>();
                }
                return new T();
            }
        }

        public static void ReturnDataBlock<T>(T block) where T : new()
        {
            if (block == null)
                return;

            lock (DataBlocks)
            {
                if (!DataBlocks.ContainsKey(typeof (T)))
                    DataBlocks[typeof (T)] = new Stack<Object>();

                if (DataBlocks[typeof (T)].Count < 50)
                    DataBlocks[typeof (T)].Push(block);
            }
        }
    }
}