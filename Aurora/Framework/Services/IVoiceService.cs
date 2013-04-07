using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework.Services
{
    public interface IVoiceService
    {
        void VoiceAccountRequest(IRegionClientCapsService regionClient, out string agentname, out string password,
                                 out string vivoxSipUri, out string vivoxVoiceAccountApi);

        void ParcelVoiceRequest(IRegionClientCapsService regionClient, out string channel_uri, out int localID);

        OSDMap GroupConferenceCallRequest(IRegionClientCapsService caps, UUID sessionid);
    }
}