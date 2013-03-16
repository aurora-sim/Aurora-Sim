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
using OpenMetaverse;
using Aurora.Framework;

namespace Aurora.Modules.Estate
{
    public class EstateTerrainXferHandler
    {
        //private static readonly ILog MainConsole.Instance = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Delegates

        public delegate void TerrainUploadComplete(string name, byte[] filedata, IClientAPI remoteClient);

        #endregion

        private readonly AssetBase m_asset;
        private readonly object _lock = new object();

        private TerrainUploadComplete handlerTerrainUploadDone;
        public ulong mXferID;

        public EstateTerrainXferHandler(IClientAPI pRemoteClient, string pClientFilename)
        {
            m_asset = new AssetBase(UUID.Zero, pClientFilename, AssetType.Texture, pRemoteClient.AgentId)
                          {Data = new byte[0], Description = "empty", Flags = AssetFlags.Temporary | AssetFlags.Local};
        }

        public ulong XferID
        {
            get { return mXferID; }
        }

        public event TerrainUploadComplete TerrainUploadDone;

        public void RequestStartXfer(IClientAPI pRemoteClient)
        {
            mXferID = Util.GetNextXferID();
            pRemoteClient.SendXferRequest(mXferID, short.Parse(m_asset.Type.ToString()), m_asset.ID, 0,
                                          Utils.StringToBytes(m_asset.Name));
        }

        /// <summary>
        ///   Process transfer data received from the client.
        /// </summary>
        /// <param name = "xferID"></param>
        /// <param name = "packetID"></param>
        /// <param name = "data"></param>
        public void XferReceive(IClientAPI remoteClient, ulong xferID, uint packetID, byte[] data)
        {
            if (mXferID == xferID)
            {
                lock (_lock)
                {
                    if (m_asset.Data.Length > 1)
                    {
                        byte[] destinationArray = new byte[m_asset.Data.Length + data.Length];
                        Array.Copy(m_asset.Data, 0, destinationArray, 0, m_asset.Data.Length);
                        Array.Copy(data, 0, destinationArray, m_asset.Data.Length, data.Length);
                        m_asset.Data = destinationArray;
                    }
                    else
                    {
                        byte[] buffer2 = new byte[data.Length - 4];
                        Array.Copy(data, 4, buffer2, 0, data.Length - 4);
                        m_asset.Data = buffer2;
                    }

                    remoteClient.SendConfirmXfer(xferID, packetID);

                    if ((packetID & 0x80000000) != 0)
                    {
                        SendCompleteMessage(remoteClient);
                    }
                }
            }
        }

        public void SendCompleteMessage(IClientAPI remoteClient)
        {
            handlerTerrainUploadDone = TerrainUploadDone;
            if (handlerTerrainUploadDone != null)
            {
                handlerTerrainUploadDone(m_asset.Name, m_asset.Data, remoteClient);
            }
        }
    }
}