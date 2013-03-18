using OpenMetaverse;

namespace Aurora.Framework
{
    /// <summary>
    /// Interface to a class that is capable of creating entities
    /// </summary>
    public interface IEntityCreator
    {
        /// <summary>
        /// The entities that this class is capable of creating.  These match the PCode format.
        /// </summary>
        /// <returns></returns>
        PCode[] CreationCapabilities { get; }

        /// <summary>
        /// Create an entity
        /// </summary>
        /// <param name="baseEntity"></param>
        /// <param name="ownerID"></param>
        /// <param name="groupID"></param>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <param name="shape"></param>
        /// <returns>The entity created, or null if the creation failed</returns>
        ISceneEntity CreateEntity(ISceneEntity baseEntity, UUID ownerID, UUID groupID, Vector3 pos, Quaternion rot, PrimitiveBaseShape shape);
    }
}
