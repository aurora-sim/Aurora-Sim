using System;
using System.Collections.Generic;
using OpenMetaverse;

namespace Aurora.Framework
{
    public interface ISceneGraph
    {
        ISceneEntity AddNewPrim(
            UUID ownerID, UUID groupID, Vector3 pos, Quaternion rot, PrimitiveBaseShape shape);
        Vector3 GetNewRezLocation(Vector3 RayStart, Vector3 RayEnd, UUID RayTargetID, Quaternion rot, byte bypassRayCast, byte RayEndIsIntersection, bool frontFacesOnly, Vector3 scale, bool FaceCenter);
        bool GetCoarseLocations(out List<Vector3> coarseLocations, out List<UUID> avatarUUIDs, uint maxLocations);
        IScenePresence GetScenePresence(string firstName, string lastName);
        IScenePresence GetScenePresence(uint localID);
        void ForEachScenePresence(Action<IScenePresence> action);
        bool LinkPartToSOG(ISceneEntity grp, ISceneChildEntity part, int linkNum);
        ISceneEntity DuplicateEntity(ISceneEntity entity);
        bool LinkPartToEntity(ISceneEntity entity, ISceneChildEntity part);
        bool DeLinkPartFromEntity(ISceneEntity entity, ISceneChildEntity part);
        void UpdateEntity(ISceneEntity entity, UUID newID);
        bool TryGetEntity(UUID ID, out IEntity entity);
        bool TryGetPart(uint LocalID, out ISceneChildEntity entity);
        bool TryGetEntity(uint LocalID, out IEntity entity);
        bool TryGetPart(UUID ID, out ISceneChildEntity entity);
        void PrepPrimForAdditionToScene(ISceneEntity entity);
        bool AddPrimToScene(ISceneEntity entity);
        bool RestorePrimToScene(ISceneEntity entity, bool force);
        void DelinkPartToScene(ISceneEntity entity);
        bool DeleteEntity(IEntity entity);
        void CheckAllocationOfLocalIds(ISceneEntity group);
        uint AllocateLocalId();
        int LinkSetSorter(ISceneChildEntity a, ISceneChildEntity b);

        List<EntityIntersection> GetIntersectingPrims(Ray hray, float length, int count, bool frontFacesOnly, bool faceCenters, bool getAvatars, bool getLand, bool getPrims);
        void RegisterEntityCreatorModule(IEntityCreator entityCreator);

        void TaintPresenceForUpdate(IScenePresence sp, PresenceTaint taint);
    }
}
