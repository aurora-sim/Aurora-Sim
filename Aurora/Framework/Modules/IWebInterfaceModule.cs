using OpenMetaverse;

namespace Aurora.Framework
{
    public interface IWebInterfaceModule
    {
        string LoginScreenURL { get; }
        string WebProfileURL { get; }
        string RegistrationScreenURL { get; }
    }

    public interface IWebHttpTextureService
    {
        string GetTextureURL(UUID textureID);
    }
}
