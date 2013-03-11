using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.Framework
{
    public interface IVoiceService
    {
        void VoiceAccountRequest(IRegionClientCapsService regionClient, out string agentname, out string password, out string vivoxSipUri, out string vivoxVoiceAccountApi);
        void ParcelVoiceRequest(IRegionClientCapsService regionClient, out string channel_uri, out int localID);

        OSDMap GroupConferenceCallRequest(IRegionClientCapsService caps, UUID sessionid);
    }
}
