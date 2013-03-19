using OpenMetaverse;

namespace Aurora.Framework
{
    public interface IEntity
    {
        UUID UUID { get; set; }
        uint LocalId { get; set; }
        int LinkNum { get; set; }
        Vector3 AbsolutePosition { get; set; }
        Vector3 Velocity { get; set; }
        Quaternion Rotation { get; set; }
        string Name { get; set; }
    }
}