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

using OpenMetaverse;

namespace Aurora.Framework.Modules
{
    public interface IXmlRpcRequestInfo
    {
        bool IsProcessed();
        UUID GetChannelKey();
        void SetProcessed(bool processed);
        void SetStrRetval(string resp);
        string GetStrRetval();
        void SetIntRetval(int resp);
        int GetIntRetval();
        UUID GetPrimID();
        UUID GetItemID();
        string GetStrVal();
        int GetIntValue();
        UUID GetMessageID();
    }

    public interface ISendRemoteDataRequest : IServiceRequest
    {
        UUID GetReqID();
        string Channel { get; set; }
        int Idata { get; set; }
        string Sdata { get; set; }
    }

    public interface IXMLRPC
    {
        int Port { get; }
        UUID OpenXMLRPCChannel(UUID primID, UUID itemID, UUID channelID);
        void CloseXMLRPCChannel(UUID channelKey);
        bool hasRequests();
        void RemoteDataReply(string channel, string message_id, string sdata, int idata);
        bool IsEnabled();
        IXmlRpcRequestInfo GetNextCompletedRequest();
        void RemoveCompletedRequest(UUID id);
        void DeleteChannels(UUID itemID);
        UUID SendRemoteData(UUID primID, UUID itemID, string channel, string dest, int idata, string sdata);
        IServiceRequest GetNextCompletedSRDRequest();
        void RemoveCompletedSRDRequest(UUID id);
        void CancelSRDRequests(UUID itemID);
    }
}