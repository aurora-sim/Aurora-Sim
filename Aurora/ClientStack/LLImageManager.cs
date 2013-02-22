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
using System.Reflection;
using Mischel.Collections;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;

namespace OpenSim.Region.ClientStack.LindenUDP
{
    public class LLImageManager
    {
        private readonly IAssetService m_assetCache; //Asset Cache
        private readonly LLClientView m_client; //Client we're assigned to
        private readonly IJ2KDecoder m_j2kDecodeModule; //Our J2K module
        private static AssetBase m_missingImage;
        private readonly PriorityQueue<J2KImage, float> m_queue = new PriorityQueue<J2KImage, float>();
        private readonly object m_syncRoot = new object();
        private bool m_shuttingdown;

        public LLImageManager(LLClientView client, IAssetService pAssetCache, IJ2KDecoder pJ2kDecodeModule)
        {
            m_client = client;
            m_assetCache = pAssetCache;

            if (pAssetCache != null && m_missingImage == null)
                m_missingImage = pAssetCache.Get("5748decc-f629-461c-9a36-a35a221fe21f");

            if (m_missingImage == null)
                MainConsole.Instance.Error(
                    "[ClientView] - Couldn't set missing image asset, falling back to missing image packet. This is known to crash the client");

            m_j2kDecodeModule = pJ2kDecodeModule;
        }

        public LLClientView Client
        {
            get { return m_client; }
        }

        public AssetBase MissingImage
        {
            get { return m_missingImage; }
        }

        /// <summary>
        ///   Handles an incoming texture request or update to an existing texture request
        /// </summary>
        /// <param name = "newRequest"></param>
        public void EnqueueReq(TextureRequestArgs newRequest)
        {
            //Make sure we're not shutting down..
            if (!m_shuttingdown)
            {
                // Do a linear search for this texture download
                J2KImage imgrequest = FindImage(newRequest);

                if (imgrequest != null)
                {
                    if (newRequest.DiscardLevel == -1 && newRequest.Priority == 0f)
                    {
                        //MainConsole.Instance.Debug("[TEX]: (CAN) ID=" + newRequest.RequestedAssetID);

                        try
                        {
                            lock (m_syncRoot)
                                m_queue.Remove(imgrequest);
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else
                    {
                        //MainConsole.Instance.DebugFormat("[TEX]: (UPD) ID={0}: D={1}, S={2}, P={3}",
                        //    newRequest.RequestedAssetID, newRequest.DiscardLevel, newRequest.PacketNumber, newRequest.Priority);

                        //Check the packet sequence to make sure this isn't older than 
                        //one we've already received
                        if (newRequest.requestSequence > imgrequest.LastSequence)
                        {
                            //Update the sequence number of the last RequestImage packet
                            imgrequest.LastSequence = newRequest.requestSequence;

                            //Update the requested discard level
                            imgrequest.DiscardLevel = newRequest.DiscardLevel;

                            //Update the requested packet number
                            imgrequest.StartPacket = Math.Max(1, newRequest.PacketNumber);

                            //Update the requested priority
                            imgrequest.Priority = newRequest.Priority;
                            lock(m_syncRoot)
                                m_queue.Remove(imgrequest);
                            AddImageToQueue(imgrequest);

                            //Run an update
                            imgrequest.RunUpdate();
                        }
                    }
                }
                else
                {
                    if (newRequest.DiscardLevel == -1 && newRequest.Priority == 0f)
                    {
                        //MainConsole.Instance.Debug("[TEX]: (CAN) ID=" + newRequest.RequestedAssetID);
                        //MainConsole.Instance.DebugFormat("[TEX]: (IGN) ID={0}: D={1}, S={2}, P={3}",
                        //    newRequest.RequestedAssetID, newRequest.DiscardLevel, newRequest.PacketNumber, newRequest.Priority);
                    }
                    else
                    {
                        //MainConsole.Instance.DebugFormat("[TEX]: (NEW) ID={0}: D={1}, S={2}, P={3}",
                        //    newRequest.RequestedAssetID, newRequest.DiscardLevel, newRequest.PacketNumber, newRequest.Priority);

                        imgrequest = new J2KImage(this)
                                         {
                                             J2KDecoder = m_j2kDecodeModule,
                                             AssetService = m_assetCache,
                                             AgentID = m_client.AgentId,
                                             InventoryAccessModule =
                                                 m_client.Scene.RequestModuleInterface<IInventoryAccessModule>(),
                                             DiscardLevel = newRequest.DiscardLevel,
                                             StartPacket = Math.Max(1, newRequest.PacketNumber),
                                             Priority = newRequest.Priority,
                                             TextureID = newRequest.RequestedAssetID
                                         };
                        imgrequest.Priority = newRequest.Priority;

                        //Add this download to the priority queue
                        AddImageToQueue(imgrequest);

                        //Run an update
                        imgrequest.RunUpdate();
                    }
                }
            }
        }

        private J2KImage FindImage(TextureRequestArgs newRequest)
        {
            if (newRequest == null)
                return null;

            lock (m_syncRoot)
                return m_queue.Find(new J2KImage(this) {TextureID = newRequest.RequestedAssetID},
                                    new Comparer());
        }

        public bool ProcessImageQueue(int packetsToSend)
        {
            int StartTime = Util.EnvironmentTickCount();

            int packetsSent = 0;
            List<J2KImage> imagesToReAdd = new List<J2KImage>();
            while (packetsSent < packetsToSend)
            {
                J2KImage image = GetHighestPriorityImage();

                // If null was returned, the texture priority queue is currently empty
                if (image == null)
                    break;
                        //Break so that we add any images back that we might remove because they arn't finished decoding

                if (image.IsDecoded)
                {
                    if (image.Layers == null)
                    {
                        //We don't have it, tell the client that it doesn't exist
                        m_client.SendAssetUploadCompleteMessage((sbyte) AssetType.Texture, false, image.TextureID);
                        packetsSent++;
                    }
                    else
                    {
                        int sent;
                        bool imageDone = image.SendPackets(m_client, packetsToSend - packetsSent, out sent);
                        packetsSent += sent;

                        // If the send is complete, destroy any knowledge of this transfer
                        if (!imageDone)
                            AddImageToQueue(image);
                    }
                }
                else
                {
                    //Add it to the other queue and delete it from the top
                    imagesToReAdd.Add(image);
                    packetsSent++; //We tried to send one
                    // UNTODO: This was a limitation of how LLImageManager is currently
                    // written. Undecoded textures should not be going into the priority
                    // queue, because a high priority undecoded texture will clog up the
                    // pipeline for a client
                    //return true;
                }
            }

            //Add all the ones we removed so that we wouldn't block the queue
            if (imagesToReAdd.Count != 0)
            {
                foreach (J2KImage image in imagesToReAdd)
                {
                    AddImageToQueue(image);
                }
            }

            int EndTime = Util.EnvironmentTickCountSubtract(StartTime);
            IMonitorModule module = m_client.Scene.RequestModuleInterface<IMonitorModule>();
            if (module != null)
            {
                IImageFrameTimeMonitor monitor =
                    (IImageFrameTimeMonitor)
                    module.GetMonitor(m_client.Scene.RegionInfo.RegionID.ToString(), MonitorModuleHelper.ImagesFrameTime);
                monitor.AddImageTime(EndTime);
            }

            lock (m_syncRoot)
                return m_queue.Count > 0;
        }

        /// <summary>
        ///   Faux destructor
        /// </summary>
        public void Close()
        {
            m_shuttingdown = true;
        }

        #region Priority Queue Helpers

        private J2KImage GetHighestPriorityImage()
        {
            J2KImage image = null;

            lock (m_syncRoot)
            {
                if (m_queue.Count > 0)
                {
                    try
                    {
                        PriorityQueueItem<J2KImage, float> item;
                        if (m_queue.TryDequeue(out item))
                            image = item.Value;
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            return image;
        }

        private void AddImageToQueue(J2KImage image)
        {
            lock (m_syncRoot)
                try
                {
                    m_queue.Enqueue(image, image.Priority);
                }
                catch (Exception)
                {
                }
        }

        #endregion Priority Queue Helpers

        #region Nested type: Comparer

        private class Comparer : IComparer<J2KImage>
        {
            #region IComparer<J2KImage> Members

            public int Compare(J2KImage x, J2KImage y)
            {
                if (x == null || y == null)
                    return -1;
                if (x.TextureID == y.TextureID)
                    return 2;
                return 0;
            }

            #endregion
        }

        #endregion

        #region Nested type: J2KImageComparer

/*
        private sealed class J2KImageComparer : IComparer<J2KImage>
        {
            #region IComparer<J2KImage> Members

            public int Compare(J2KImage x, J2KImage y)
            {
                return x.Priority.CompareTo(y.Priority);
            }

            #endregion
        }
*/

        #endregion
    }
}