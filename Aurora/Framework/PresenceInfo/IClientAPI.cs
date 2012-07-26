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
using System.Net;
using Aurora.Framework;
using OpenMetaverse;
using OpenMetaverse.Packets;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{

    #region Client API Delegate definitions

    public delegate void ViewerEffectEventHandler(IClientAPI sender, List<ViewerEffectEventHandlerArg> args);

    public delegate void ChatMessage(IClientAPI sender, OSChatMessage e);

    public delegate void GenericMessage(Object sender, string method, List<String> args);

    public delegate void TextureRequest(Object sender, TextureRequestArgs e);

    public delegate void AvatarNowWearing(IClientAPI sender, AvatarWearingArgs e);

    public delegate void ImprovedInstantMessage(IClientAPI remoteclient, GridInstantMessage im);

    public delegate bool PreSendImprovedInstantMessage(IClientAPI remoteclient, GridInstantMessage im);

    public delegate void RezObject(IClientAPI remoteClient, UUID itemID, Vector3 RayEnd, Vector3 RayStart,
                                   UUID RayTargetID, byte BypassRayCast, bool RayEndIsIntersection,
                                   bool RezSelected, bool RemoveItem, UUID fromTaskID);

    public delegate UUID RezSingleAttachmentFromInv(IClientAPI remoteClient, UUID itemID, int AttachmentPt);

    public delegate void RezMultipleAttachmentsFromInv(
        IClientAPI remoteClient, RezMultipleAttachmentsFromInvPacket.HeaderDataBlock header,
        RezMultipleAttachmentsFromInvPacket.ObjectDataBlock[] objects);

    public delegate void ObjectAttach(
        IClientAPI remoteClient, uint objectLocalID, int AttachmentPt, bool silent);

    public delegate void ModifyTerrain(UUID user,
                                       float height, float seconds, byte size, byte action, float north, float west,
                                       float south, float east,
                                       UUID agentId, float BrushSize);

    public delegate void NetworkStats(int inPackets, int outPackets, int unAckedBytes);

    public delegate void SetAppearance(
        IClientAPI remoteClient, Primitive.TextureEntry textureEntry, byte[] visualParams, WearableCache[] wearables, uint serial);

    public delegate void StartAnim(IClientAPI remoteClient, UUID animID);

    public delegate void StopAnim(IClientAPI remoteClient, UUID animID);

    public delegate void LinkObjects(IClientAPI remoteClient, uint parent, List<uint> children);

    public delegate void DelinkObjects(List<uint> primIds, IClientAPI client);

    public delegate void RequestMapBlocks(IClientAPI remoteClient, int minX, int minY, int maxX, int maxY, uint flags);

    public delegate void RequestMapName(IClientAPI remoteClient, string mapName, uint flags);

    public delegate void TeleportLocationRequest(
        IClientAPI remoteClient, ulong regionHandle, Vector3 position, Vector3 lookAt, uint flags);

    public delegate void TeleportLandmarkRequest(
        IClientAPI remoteClient, UUID regionID, string gatekeeper, Vector3 position);

    public delegate void DisconnectUser();

    public delegate void RequestAvatarProperties(IClientAPI remoteClient, UUID avatarID);

    public delegate void UpdateAvatarProperties(
        IClientAPI remoteClient, string AboutText, string FLAboutText, UUID FLImageID, UUID ImageID,
        string WebProfileURL, bool AllowPublish, bool MaturePublish);

    public delegate void SetAlwaysRun(IClientAPI remoteClient, bool SetAlwaysRun);

    public delegate void GenericCall1(IClientAPI remoteClient);

    public delegate void GenericCall2();

    // really don't want to be passing packets in these events, so this is very temporary.
    public delegate void GenericCall4(Packet packet, IClientAPI remoteClient);

    public delegate void DeRezObject(
        IClientAPI remoteClient, List<uint> localIDs, UUID groupID, DeRezAction action, UUID destinationID);

    public delegate void GenericCall5(IClientAPI remoteClient, bool status);

    public delegate void GenericCall7(IClientAPI remoteClient, uint localID, string message);

    public delegate void UpdateShape(UUID agentID, uint localID, UpdateShapeArgs shapeBlock);

    public delegate void ObjectExtraParams(UUID agentID, uint localID, ushort type, bool inUse, byte[] data);

    public delegate void ObjectSelect(List<uint> localIDs, IClientAPI remoteClient);

    public delegate void ObjectRequest(uint localID, byte cacheMissType, IClientAPI remoteClient);

    public delegate void RequestObjectPropertiesFamily(
        IClientAPI remoteClient, UUID AgentID, uint RequestFlags, UUID TaskID);

    public delegate void ObjectDeselect(uint localID, IClientAPI remoteClient);

    public delegate void ObjectDrop(uint localID, IClientAPI remoteClient);

    public delegate void UpdatePrimFlags(
        uint localID, bool UsePhysics, bool IsTemporary, bool IsPhantom,
        ObjectFlagUpdatePacket.ExtraPhysicsBlock[] blocks, IClientAPI remoteClient);

    public delegate void UpdatePrimTexture(uint localID, byte[] texture, IClientAPI remoteClient);

    public delegate void UpdateVector(uint localID, Vector3 pos, IClientAPI remoteClient);

    public delegate void UpdateVectorWithUpdate(uint localID, Vector3 pos, IClientAPI remoteClient, bool SaveUpdate);

    public delegate void UpdatePrimRotation(uint localID, Quaternion rot, IClientAPI remoteClient);

    public delegate void UpdatePrimSingleRotation(uint localID, Quaternion rot, IClientAPI remoteClient);

    public delegate void UpdatePrimSingleRotationPosition(
        uint localID, Quaternion rot, Vector3 pos, IClientAPI remoteClient);

    public delegate void UpdatePrimGroupRotation(uint localID, Vector3 pos, Quaternion rot, IClientAPI remoteClient);

    public delegate bool ObjectDuplicate(
        uint localID, Vector3 offset, uint dupeFlags, UUID AgentID, UUID GroupID, Quaternion rot);

    public delegate void ObjectDuplicateOnRay(uint localID, uint dupeFlags, UUID AgentID, UUID GroupID,
                                              UUID RayTargetObj, Vector3 RayEnd, Vector3 RayStart,
                                              bool BypassRaycast, bool RayEndIsIntersection, bool CopyCenters,
                                              bool CopyRotates);

    public delegate void StatusChange(bool status);

    public delegate void NewAvatar(IClientAPI remoteClient, UUID agentID, bool status);

    public delegate void UpdateAgent(IClientAPI remoteClient, AgentUpdateArgs agentData);

    public delegate void AgentRequestSit(IClientAPI remoteClient, UUID targetID, Vector3 offset);

    public delegate void AgentSit(IClientAPI remoteClient, UUID agentID);

    public delegate void LandUndo(IClientAPI remoteClient);

    public delegate void AvatarPickerRequest(IClientAPI remoteClient, UUID agentdata, UUID queryID, string UserQuery);

    public delegate void GrabObject(
        uint localID, Vector3 pos, IClientAPI remoteClient, List<SurfaceTouchEventArgs> surfaceArgs);

    public delegate void DeGrabObject(
        uint localID, IClientAPI remoteClient, List<SurfaceTouchEventArgs> surfaceArgs);

    public delegate void MoveObject(
        UUID objectID, Vector3 offset, Vector3 grapPos, IClientAPI remoteClient, List<SurfaceTouchEventArgs> surfaceArgs
        );

    public delegate void SpinStart(UUID objectID, IClientAPI remoteClient);

    public delegate void SpinObject(UUID objectID, Quaternion rotation, IClientAPI remoteClient);

    public delegate void SpinStop(UUID objectID, IClientAPI remoteClient);

    public delegate void ParcelAccessListRequest(
        UUID agentID, UUID sessionID, uint flags, int sequenceID, int landLocalID, IClientAPI remote_client);

    public delegate void ParcelAccessListUpdateRequest(
        UUID agentID, UUID sessionID, uint flags, int landLocalID, List<ParcelManager.ParcelAccessEntry> entries,
        IClientAPI remote_client);

    public delegate void ParcelPropertiesRequest(
        int start_x, int start_y, int end_x, int end_y, int sequence_id, bool snap_selection, IClientAPI remote_client);

    public delegate void ParcelDivideRequest(int west, int south, int east, int north, IClientAPI remote_client);

    public delegate void ParcelJoinRequest(int west, int south, int east, int north, IClientAPI remote_client);

    public delegate void ParcelPropertiesUpdateRequest(LandUpdateArgs args, int local_id, IClientAPI remote_client);

    public delegate void ParcelSelectObjects(
        int land_local_id, int request_type, List<UUID> returnIDs, IClientAPI remote_client);

    public delegate void ParcelObjectOwnerRequest(int local_id, IClientAPI remote_client);

    public delegate void ParcelAbandonRequest(int local_id, IClientAPI remote_client);

    public delegate void ParcelGodForceOwner(int local_id, UUID ownerID, IClientAPI remote_client);

    public delegate void ParcelReclaim(int local_id, IClientAPI remote_client);

    public delegate void ParcelReturnObjectsRequest(
        int local_id, uint return_type, UUID[] agent_ids, UUID[] selected_ids, IClientAPI remote_client);

    public delegate void ParcelDeedToGroup(int local_id, UUID group_id, IClientAPI remote_client);

    public delegate void EstateOwnerMessageRequest(
        UUID AgentID, UUID SessionID, UUID TransactionID, UUID Invoice, byte[] Method, byte[][] Parameters,
        IClientAPI remote_client);

    public delegate void RegionInfoRequest(IClientAPI remote_client);

    public delegate void EstateCovenantRequest(IClientAPI remote_client);

    public delegate void UUIDNameRequest(UUID id, IClientAPI remote_client);

    public delegate void AddNewPrim(
        UUID ownerID, UUID groupID, Vector3 RayEnd, Quaternion rot, PrimitiveBaseShape shape, byte bypassRaycast,
        Vector3 RayStart,
        UUID RayTargetID,
        byte RayEndIsIntersection);

    public delegate void RequestGodlikePowers(
        UUID AgentID, UUID SessionID, UUID token, bool GodLike, IClientAPI remote_client);

    public delegate void GodKickUser(
        UUID GodAgentID, UUID GodSessionID, UUID AgentID, uint kickflags, byte[] reason);

    public delegate void CreateInventoryFolder(
        IClientAPI remoteClient, UUID folderID, ushort folderType, string folderName, UUID parentID);

    public delegate void UpdateInventoryFolder(
        IClientAPI remoteClient, UUID folderID, ushort type, string name, UUID parentID);

    public delegate void MoveInventoryFolder(
        IClientAPI remoteClient, UUID folderID, UUID parentID);

    public delegate void CreateNewInventoryItem(
        IClientAPI remoteClient, UUID transActionID, UUID folderID, uint callbackID, string description, string name,
        sbyte invType, sbyte type, byte wearableType, uint nextOwnerMask, int creationDate);

    public delegate void LinkInventoryItem(
        IClientAPI remoteClient, UUID transActionID, UUID folderID, uint callbackID, string description, string name,
        sbyte invType, sbyte type, UUID olditemID);

    public delegate void FetchInventoryDescendents(
        IClientAPI remoteClient, UUID folderID, UUID ownerID, bool fetchFolders, bool fetchItems, int sortOrder);

    public delegate void PurgeInventoryDescendents(
        IClientAPI remoteClient, UUID folderID);

    public delegate void FetchInventory(IClientAPI remoteClient, UUID itemID, UUID ownerID);

    public delegate void RequestTaskInventory(IClientAPI remoteClient, uint localID);

/*    public delegate void UpdateInventoryItem(
        IClientAPI remoteClient, UUID transactionID, UUID itemID, string name, string description,
        uint nextOwnerMask);*/

    public delegate void UpdateInventoryItem(
        IClientAPI remoteClient, UUID transactionID, UUID itemID, InventoryItemBase itemUpd);

    public delegate void ChangeInventoryItemFlags(
        IClientAPI remoteClient, UUID itemID, uint flags);

    public delegate void CopyInventoryItem(
        IClientAPI remoteClient, uint callbackID, UUID oldAgentID, UUID oldItemID, UUID newFolderID,
        string newName);

    public delegate void MoveInventoryItem(
        IClientAPI remoteClient, List<InventoryItemBase> items);

    public delegate void RemoveInventoryItem(
        IClientAPI remoteClient, List<UUID> itemIDs);

    public delegate void RemoveInventoryFolder(
        IClientAPI remoteClient, List<UUID> folderIDs);

    public delegate void AbortXfer(IClientAPI remoteClient, ulong xferID);

    public delegate void RezScript(IClientAPI remoteClient, InventoryItemBase item, UUID transactionID, uint localID);

    public delegate void UpdateTaskInventory(
        IClientAPI remoteClient, UUID transactionID, TaskInventoryItem item, uint localID);

    public delegate void MoveTaskInventory(IClientAPI remoteClient, UUID folderID, uint localID, UUID itemID);

    public delegate void RemoveTaskInventory(IClientAPI remoteClient, UUID itemID, uint localID);

    public delegate void UDPAssetUploadRequest(
        IClientAPI remoteClient, UUID assetID, UUID transaction, sbyte type, byte[] data, bool storeLocal,
        bool tempFile);

    public delegate void XferReceive(IClientAPI remoteClient, ulong xferID, uint packetID, byte[] data);

    public delegate void RequestXfer(IClientAPI remoteClient, ulong xferID, string fileName);

    public delegate void ConfirmXfer(IClientAPI remoteClient, ulong xferID, uint packetID);

    public delegate void FriendActionDelegate(
        IClientAPI remoteClient, UUID agentID, UUID transactionID, List<UUID> callingCardFolders);

    public delegate void FriendshipTermination(IClientAPI remoteClient, UUID agentID, UUID ExID);

    public delegate void MoneyTransferRequest(
        UUID sourceID, UUID destID, int amount, int transactionType, string description);

    public delegate void ParcelBuy(UUID agentId, UUID groupId, bool final, bool groupOwned,
                                   bool removeContribution, int parcelLocalID, int parcelArea, int parcelPrice,
                                   bool authenticated);

    // We keep all this information for fraud purposes in the future.
    public delegate void MoneyBalanceRequest(IClientAPI remoteClient, UUID agentID, UUID sessionID, UUID TransactionID);

    public delegate void ObjectPermissions(
        IClientAPI controller, UUID agentID, UUID sessionID, byte field, uint localId, uint mask, byte set);

    public delegate void EconomyDataRequest(IClientAPI remoteClient);

    public delegate void ObjectIncludeInSearch(IClientAPI remoteClient, bool IncludeInSearch, uint localID);

    public delegate void ScriptAnswer(IClientAPI remoteClient, UUID objectID, UUID itemID, int answer);

    public delegate void RequestPayPrice(IClientAPI remoteClient, UUID objectID);

    public delegate void ObjectSaleInfo(
        IClientAPI remoteClient, UUID sessionID, uint localID, byte saleType, int salePrice);

    public delegate void ObjectBuy(
        IClientAPI remoteClient, UUID sessionID, UUID groupID, UUID categoryID, uint localID,
        byte saleType, int salePrice);

    public delegate void BuyObjectInventory(
        IClientAPI remoteClient, UUID sessionID, UUID objectID, UUID itemID, UUID folderID);

    public delegate void ForceReleaseControls(IClientAPI remoteClient, UUID agentID);

    public delegate void GodLandStatRequest(
        int parcelID, uint reportType, uint requestflags, string filter, IClientAPI remoteClient);

    //Estate Requests
    public delegate void DetailedEstateDataRequest(IClientAPI remoteClient, UUID invoice);

    public delegate void SetEstateFlagsRequest(IClientAPI remoteClient,
                                               bool blockTerraform, bool noFly, bool allowDamage, bool blockLandResell,
                                               int maxAgents, float objectBonusFactor,
                                               int matureLevel, bool restrictPushObject, bool allowParcelChanges);

    public delegate void SetEstateTerrainBaseTexture(IClientAPI remoteClient, int corner, UUID side);

    public delegate void SetEstateTerrainDetailTexture(IClientAPI remoteClient, int corner, UUID side);

    public delegate void SetEstateTerrainTextureHeights(IClientAPI remoteClient, int corner, float lowVal, float highVal
        );

    public delegate void CommitEstateTerrainTextureRequest(IClientAPI remoteClient);

    public delegate void SetRegionTerrainSettings(UUID AgentID,
                                                  float waterHeight, float terrainRaiseLimit, float terrainLowerLimit,
                                                  bool estateSun, bool fixedSun,
                                                  float sunHour, bool globalSun, bool estateFixed, float estateSunHour);

    public delegate void EstateChangeInfo(IClientAPI client, UUID invoice, UUID senderID, UInt32 param1, UInt32 param2);

    public delegate void RequestTerrain(IClientAPI remoteClient, string clientFileName);

    public delegate void BakeTerrain(IClientAPI remoteClient);


    public delegate void EstateRestartSimRequest(IClientAPI remoteClient, int secondsTilReboot);

    public delegate void EstateChangeCovenantRequest(IClientAPI remoteClient, UUID newCovenantID);

    public delegate void UpdateEstateAccessDeltaRequest(
        IClientAPI remote_client, UUID invoice, int estateAccessType, UUID user);

    public delegate void SimulatorBlueBoxMessageRequest(
        IClientAPI remoteClient, UUID invoice, UUID senderID, UUID sessionID, string senderName, string message);

    public delegate void EstateBlueBoxMessageRequest(
        IClientAPI remoteClient, UUID invoice, UUID senderID, UUID sessionID, string senderName, string message);

    public delegate void EstateDebugRegionRequest(
        IClientAPI remoteClient, UUID invoice, UUID senderID, bool scripted, bool collisionEvents, bool physics);

    public delegate void EstateTeleportOneUserHomeRequest(
        IClientAPI remoteClient, UUID invoice, UUID senderID, UUID prey);

    public delegate void EstateTeleportAllUsersHomeRequest(IClientAPI remoteClient, UUID invoice, UUID senderID);

    public delegate void RegionHandleRequest(IClientAPI remoteClient, UUID regionID);

    public delegate void ParcelInfoRequest(IClientAPI remoteClient, UUID parcelID);

    public delegate void ScriptReset(IClientAPI remoteClient, UUID objectID, UUID itemID);

    public delegate void GetScriptRunning(IClientAPI remoteClient, UUID objectID, UUID itemID);

    public delegate void SetScriptRunning(IClientAPI remoteClient, UUID objectID, UUID itemID, bool running);

    public delegate void ActivateGesture(IClientAPI client, UUID gestureid, UUID assetId);

    public delegate void DeactivateGesture(IClientAPI client, UUID gestureid);

    public delegate void ObjectOwner(IClientAPI remoteClient, UUID ownerID, UUID groupID, List<uint> localIDs);

    public delegate void DirPlacesQuery(
        IClientAPI remoteClient, UUID queryID, string queryText, int queryFlags, int category, string simName,
        int queryStart);

    public delegate void DirFindQuery(
        IClientAPI remoteClient, UUID queryID, string queryText, uint queryFlags, int queryStart);

    public delegate void DirLandQuery(IClientAPI remoteClient, UUID queryID, uint queryFlags, uint searchType, uint price, uint area, int queryStart);

    public delegate void DirPopularQuery(IClientAPI remoteClient, UUID queryID, uint queryFlags);

    public delegate void DirClassifiedQuery(
        IClientAPI remoteClient, UUID queryID, string queryText, uint queryFlags, uint category, int queryStart);

    public delegate void EventInfoRequest(IClientAPI remoteClient, uint eventID);

    public delegate void ParcelSetOtherCleanTime(IClientAPI remoteClient, int localID, int otherCleanTime);

    public delegate void MapItemRequest(
        IClientAPI remoteClient, uint flags, uint EstateID, bool godlike, uint itemtype, ulong regionhandle);

    public delegate void OfferCallingCard(IClientAPI remoteClient, UUID destID, UUID transactionID);

    public delegate void AcceptCallingCard(IClientAPI remoteClient, UUID transactionID, UUID folderID);

    public delegate void DeclineCallingCard(IClientAPI remoteClient, UUID transactionID);

    public delegate void SoundTrigger(
        UUID soundId, UUID ownerid, UUID objid, UUID parentid, double Gain, Vector3 Position, UInt64 Handle,
        float radius);

    public delegate void StartLure(byte lureType, string message, UUID targetID, IClientAPI client);

    public delegate void TeleportLureRequest(UUID lureID, uint teleportFlags, IClientAPI client);

    public delegate void ClassifiedInfoRequest(UUID classifiedID, IClientAPI client);

    public delegate void ClassifiedInfoUpdate(
        UUID classifiedID, uint category, string name, string description, UUID parcelID, uint parentEstate,
        UUID snapshotID, Vector3 globalPos, byte classifiedFlags, int price, IClientAPI client);

    public delegate void ClassifiedDelete(UUID classifiedID, IClientAPI client);

    public delegate void EventNotificationAddRequest(uint EventID, IClientAPI client);

    public delegate void EventNotificationRemoveRequest(uint EventID, IClientAPI client);

    public delegate void EventGodDelete(
        uint eventID, UUID queryID, string queryText, uint queryFlags, int queryStart, IClientAPI client);

    public delegate void ParcelDwellRequest(int localID, IClientAPI client);

    public delegate void UserInfoRequest(IClientAPI client);

    public delegate void UpdateUserInfo(bool imViaEmail, bool visible, IClientAPI client);

    public delegate void RetrieveInstantMessages(IClientAPI client);

    public delegate void PickDelete(IClientAPI client, UUID pickID);

    public delegate void PickGodDelete(IClientAPI client, UUID agentID, UUID pickID, UUID queryID);

    public delegate void PickInfoUpdate(
        IClientAPI client, UUID pickID, UUID creatorID, bool topPick, string name, string desc, UUID snapshotID,
        int sortOrder, bool enabled, Vector3d globalPos);

    public delegate void AvatarNotesUpdate(IClientAPI client, UUID targetID, string notes);

    public delegate void MuteListRequest(IClientAPI client, uint muteCRC);

    public delegate void AvatarInterestUpdate(
        IClientAPI client, uint wantmask, string wanttext, uint skillsmask, string skillstext, string languages);

    public delegate void GrantUserFriendRights(IClientAPI client, UUID requester, UUID target, int rights);

    public delegate void PlacesQuery(
        UUID QueryID, UUID TransactionID, string QueryText, uint QueryFlags, byte Category, string SimName,
        IClientAPI client);

    public delegate void AgentFOV(IClientAPI client, float verticalAngle);

    public delegate void MuteListEntryUpdate(IClientAPI client, UUID MuteID, string Name, int Flags, UUID AgentID);

    public delegate void MuteListEntryRemove(IClientAPI client, UUID MuteID, string Name, UUID AgentID);

    public delegate void AvatarInterestReply(
        IClientAPI client, UUID target, uint wantmask, string wanttext, uint skillsmask, string skillstext,
        string languages);

    public delegate void FindAgentUpdate(IClientAPI client, UUID hunter, UUID target);

    public delegate void TrackAgentUpdate(IClientAPI client, UUID hunter, UUID target);

    public delegate void FreezeUserUpdate(IClientAPI client, UUID parcelowner, uint flags, UUID target);

    public delegate void EjectUserUpdate(IClientAPI client, UUID parcelowner, uint flags, UUID target);

    public delegate void NewUserReport(
        IClientAPI client, string regionName, UUID abuserID, byte catagory, byte checkflags, string details,
        UUID objectID, Vector3 postion, byte reportType, UUID screenshotID, string Summary, UUID reporter);

    public delegate void GodUpdateRegionInfoUpdate(
        IClientAPI client, float BillableFactor, int PricePerMeter, ulong EstateID, ulong RegionFlags, byte[] SimName,
        int RedirectX, int RedirectY);

    public delegate void GodlikeMessage(IClientAPI client, UUID requester, string Method, List<string> Parameter);

    public delegate void ViewerStartAuction(IClientAPI client, int LocalID, UUID SnapshotID);

    public delegate void SaveStateHandler(IClientAPI client, UUID agentID);

    public delegate void GroupAccountSummaryRequest(IClientAPI client, UUID agentID, UUID groupID, UUID transactionID, int currentInterval, int intervalDays);

    public delegate void GroupAccountDetailsRequest(
        IClientAPI client, UUID agentID, UUID groupID, UUID transactionID, UUID sessionID, int currentInterval, int intervalDays);

    public delegate void GroupAccountTransactionsRequest(
        IClientAPI client, UUID agentID, UUID groupID, UUID transactionID, UUID sessionID, int currentInterval, int intervalDays);

    public delegate void ParcelBuyPass(IClientAPI client, UUID agentID, int ParcelLocalID);

    public delegate void ParcelGodMark(IClientAPI client, UUID agentID, int ParcelLocalID);

    public delegate void GroupActiveProposalsRequest(
        IClientAPI client, UUID agentID, UUID groupID, UUID transactionID, UUID sessionID);

    public delegate void GroupVoteHistoryRequest(
        IClientAPI client, UUID agentID, UUID groupID, UUID transactionID, UUID sessionID);

    public delegate void GroupProposalBallotRequest(
        IClientAPI client, UUID agentID, UUID sessionID, UUID groupID, UUID ProposalID, string vote);

    public delegate void SimWideDeletesDelegate(IClientAPI client, int flags, UUID targetID);

    public delegate void SendPostcard(IClientAPI client);

    public delegate void TeleportCancel(IClientAPI client);

    public delegate void AgentCachedTextureRequest(IClientAPI client, List<CachedAgentArgs> args);

    #endregion

    public class WearableCache
    {
        public UUID CacheID;
        public int TextureIndex;
    }

    public class LandObjectOwners
    {
        public int Count;
        public bool GroupOwned;
        public bool Online;
        public UUID OwnerID = UUID.Zero;
        public DateTime TimeLastRezzed;
    }

    public class GroupAccountHistory : IDataTransferable
    {
        public int Amount;
        public string Description;
        public string TimeString;
        public string UserCausingCharge;
        private bool _ispayment = true;
        public bool Payment { get { return _ispayment; } set { _ispayment = value; } }
        public bool Stipend { get { return !_ispayment; } set { _ispayment = !value; } }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();

            map["Amount"] = Amount;
            map["Description"] = Description;
            map["TimeString"] = TimeString;
            map["UserCausingCharge"] = UserCausingCharge;
            map["Payment"] = Payment;

            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            Amount = map["Amount"];
            Description = map["Description"];
            TimeString = map["TimeString"];
            Payment = map["Payment"];
            UserCausingCharge = map["UserCausingCharge"];
        }
    }

    public class DirPlacesReplyData : IDataTransferable
    {
        public uint Status;
        public bool auction;
        public float dwell;
        public bool forSale;
        public string name;
        public UUID parcelID;

        public DirPlacesReplyData()
        {
        }

        public DirPlacesReplyData(Dictionary<string, object> KVP)
        {
            FromKVP(KVP);
        }

        public override Dictionary<string, object> ToKVP()
        {
            Dictionary<string, object> KVP = new Dictionary<string, object>();
            KVP["parcelID"] = parcelID;
            KVP["name"] = name;
            KVP["forSale"] = forSale;
            KVP["auction"] = auction;
            KVP["dwell"] = dwell;
            KVP["Status"] = Status;
            return KVP;
        }

        public override void FromKVP(Dictionary<string, object> KVP)
        {
            Status = uint.Parse(KVP["Status"].ToString());
            dwell = float.Parse(KVP["dwell"].ToString());
            auction = bool.Parse(KVP["auction"].ToString());
            forSale = bool.Parse(KVP["forSale"].ToString());
            name = KVP["name"].ToString();
            parcelID = UUID.Parse(KVP["parcelID"].ToString());
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();

            map["parcelID"] = parcelID;
            map["name"] = name;
            map["forSale"] = forSale;
            map["auction"] = auction;
            map["dwell"] = dwell;
            map["Status"] = Status;

            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            Status = map["Status"];
            dwell = map["dwell"];
            auction = map["auction"];
            forSale = map["forSale"];
            name = map["name"];
            parcelID = map["parcelID"];
        }
    }

    public struct DirPeopleReplyData
    {
        public UUID agentID;
        public string firstName;
        public string group;
        public string lastName;
        public bool online;
        public int reputation;
    }

    public class DirEventsReplyData : IDataTransferable
    {
        public uint Status;
        public string date;
        public uint eventFlags;
        public uint eventID;
        public string name;
        public UUID ownerID;
        public uint unixTime;

        public DirEventsReplyData()
        {
        }

        public DirEventsReplyData(Dictionary<string, object> KVP)
        {
            FromKVP(KVP);
        }

        public override void FromKVP(Dictionary<string, object> KVP)
        {
            Status = uint.Parse(KVP["Status"].ToString());
            eventFlags = uint.Parse(KVP["eventFlags"].ToString());
            unixTime = uint.Parse(KVP["unixTime"].ToString());
            date = KVP["date"].ToString();
            eventID = uint.Parse(KVP["eventID"].ToString());
            name = KVP["name"].ToString();
            ownerID = UUID.Parse(KVP["ownerID"].ToString());
        }

        public override Dictionary<string, object> ToKVP()
        {
            Dictionary<string, object> KVP = new Dictionary<string, object>();
            KVP["ownerID"] = ownerID;
            KVP["name"] = name;
            KVP["eventID"] = eventID;
            KVP["date"] = date;
            KVP["unixTime"] = unixTime;
            KVP["eventFlags"] = eventFlags;
            KVP["Status"] = Status;
            return KVP;
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["ownerID"] = ownerID;
            map["name"] = name;
            map["eventID"] = eventID;
            map["date"] = date;
            map["unixTime"] = unixTime;
            map["eventFlags"] = eventFlags;
            map["Status"] = Status;
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            ownerID = map["ownerID"];
            name = map["name"];
            eventID = map["eventID"];
            date = map["date"];
            unixTime = map["unixTime"];
            eventFlags = map["eventFlags"];
            Status = map["Status"];
        }
    }

    public class DirGroupsReplyData : IDataTransferable
    {
        public UUID groupID;
        public string groupName;
        public int members;
        public float searchOrder;

        public DirGroupsReplyData()
        {
        }

        public DirGroupsReplyData(Dictionary<string, object> KVP)
        {
            FromKVP(KVP);
        }

        public override Dictionary<string, object> ToKVP()
        {
            Dictionary<string, object> KVP = new Dictionary<string, object>();
            KVP["groupID"] = groupID;
            KVP["groupName"] = groupName;
            KVP["members"] = members;
            KVP["searchOrder"] = searchOrder;
            return KVP;
        }

        public override void FromKVP(Dictionary<string, object> KVP)
        {
            groupID = UUID.Parse(KVP["groupID"].ToString());
            groupName = KVP["groupName"].ToString();
            members = int.Parse(KVP["members"].ToString());
            searchOrder = float.Parse(KVP["searchOrder"].ToString());
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["groupID"] = groupID;
            map["groupName"] = groupName;
            map["members"] = members;
            map["searchOrder"] = searchOrder;
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            groupID = map["groupID"];
            groupName = map["groupName"];
            members = map["members"];
            searchOrder = map["searchOrder"];
        }
    }

    public class DirClassifiedReplyData : IDataTransferable
    {
        public uint Status;
        public byte classifiedFlags;
        public UUID classifiedID;
        public uint creationDate;
        public uint expirationDate;
        public string name;
        public int price;

        public DirClassifiedReplyData()
        {
        }

        public DirClassifiedReplyData(Dictionary<string, object> KVP)
        {
            FromKVP(KVP);
        }

        public override Dictionary<string, object> ToKVP()
        {
            Dictionary<string, object> KVP = new Dictionary<string, object>();
            KVP["classifiedID"] = classifiedID;
            KVP["name"] = name;
            KVP["classifiedFlags"] = classifiedFlags;
            KVP["creationDate"] = creationDate;
            KVP["expirationDate"] = expirationDate;
            KVP["price"] = price;
            KVP["Status"] = Status;
            return KVP;
        }

        public override void FromKVP(Dictionary<string, object> KVP)
        {
            Status = uint.Parse(KVP["Status"].ToString());
            price = int.Parse(KVP["price"].ToString());
            expirationDate = uint.Parse(KVP["expirationDate"].ToString());
            creationDate = uint.Parse(KVP["creationDate"].ToString());
            classifiedFlags = byte.Parse(KVP["classifiedFlags"].ToString());
            name = KVP["name"].ToString();
            classifiedID = UUID.Parse(KVP["classifiedID"].ToString());
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["classifiedID"] = classifiedID;
            map["name"] = name;
            map["classifiedFlags"] = (int)classifiedFlags;
            map["creationDate"] = creationDate;
            map["expirationDate"] = expirationDate;
            map["price"] = price;
            map["Status"] = Status;
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            classifiedID = map["classifiedID"];
            name = map["name"];
            classifiedFlags = (byte)(int)map["classifiedFlags"];
            creationDate = map["creationDate"];
            expirationDate = map["expirationDate"];
            price = map["price"];
            Status = map["Status"];
        }
    }

    public class DirLandReplyData : IDataTransferable
    {
        public int actualArea;
        public bool auction;
        public bool forSale;
        public string name;
        public UUID parcelID;
        public int salePrice;

        public DirLandReplyData()
        {
        }

        public DirLandReplyData(Dictionary<string, object> KVP)
        {
            FromKVP(KVP);
        }

        public override Dictionary<string, object> ToKVP()
        {
            Dictionary<string, object> KVP = new Dictionary<string, object>();
            KVP["parcelID"] = parcelID;
            KVP["name"] = name;
            KVP["forSale"] = forSale;
            KVP["auction"] = auction;
            KVP["salePrice"] = salePrice;
            KVP["actualArea"] = actualArea;
            return KVP;
        }

        public override void FromKVP(Dictionary<string, object> KVP)
        {
            actualArea = int.Parse(KVP["actualArea"].ToString());
            salePrice = int.Parse(KVP["salePrice"].ToString());
            auction = bool.Parse(KVP["auction"].ToString());
            forSale = bool.Parse(KVP["forSale"].ToString());
            name = KVP["name"].ToString();
            parcelID = UUID.Parse(KVP["parcelID"].ToString());
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["parcelID"] = parcelID;
            map["name"] = name;
            map["forSale"] = forSale;
            map["auction"] = auction;
            map["salePrice"] = salePrice;
            map["actualArea"] = actualArea;
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            parcelID = map["parcelID"];
            name = map["name"];
            forSale = map["forSale"];
            auction = map["auction"];
            salePrice = map["salePrice"];
            actualArea = map["actualArea"];
        }
    }

    public class DirPopularReplyData : IDataTransferable
    {
        public float Dwell;
        public string Name;
        public UUID ParcelID;

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["Dwell"] = Dwell;
            map["ParcelID"] = ParcelID;
            map["Name"] = Name;
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            Dwell = map["Dwell"];
            ParcelID = map["ParcelID"];
            Name = map["Name"];
        }
    }

    public class EntityUpdate
    {
        public IEntity Entity;
        public PrimUpdateFlags Flags;
        public int Version;

        public EntityUpdate(IEntity entity, PrimUpdateFlags flags)
        {
            Entity = entity;
            Flags = flags;
        }
    }

    /// <summary>
    ///   Specifies the fields that have been changed when sending a prim or
    ///   avatar update
    /// </summary>
    [Flags]
    public enum PrimUpdateFlags : uint
    {
        None = 0,
        AttachmentPoint = 1 << 0,
        Material = 1 << 1,
        ClickAction = 1 << 2,
        Shape = 1 << 3,
        ParentID = 1 << 4,
        PrimFlags = 1 << 5,
        PrimData = 1 << 6,
        MediaURL = 1 << 7,
        ScratchPad = 1 << 8,
        Textures = 1 << 9,
        TextureAnim = 1 << 10,
        NameValue = 1 << 11,
        Position = 1 << 12,
        Rotation = 1 << 13,
        Velocity = 1 << 14,
        Acceleration = 1 << 15,
        AngularVelocity = 1 << 16,
        CollisionPlane = 1 << 17,
        Text = 1 << 18,
        Particles = 1 << 19,
        ExtraData = 1 << 20,
        Sound = 1 << 21,
        Joint = 1 << 22,
        FindBest = 1 << 23,
        ForcedFullUpdate = UInt32.MaxValue - 1,
        FullUpdate = UInt32.MaxValue,

        TerseUpdate = Position | Rotation | Velocity
                      | Acceleration | AngularVelocity
    }

    public static class PrimUpdateFlagsExtensions
    {
        public static bool HasFlag(this PrimUpdateFlags updateFlags, PrimUpdateFlags flag)
        {
            return (updateFlags & flag) == flag;
        }
    }

    public interface IClientAPI
    {
        Vector3 StartPos { get; set; }

        UUID AgentId { get; }

        UUID SessionId { get; }

        UUID SecureSessionId { get; }

        UUID ActiveGroupId { get; }

        string ActiveGroupName { get; }

        ulong ActiveGroupPowers { get; }

        string FirstName { get; }

        string LastName { get; }

        IScene Scene { get; }

        IPAddress EndPoint { get; }

        // [Obsolete("LLClientView Specific - Replace with ???")]
        int NextAnimationSequenceNumber { get; }

        /// <summary>
        ///   Returns the full name of the agent/avatar represented by this client
        /// </summary>
        string Name { get; }

        /// <value>
        ///   Determines whether the client thread is doing anything or not.
        /// </value>
        bool IsActive { get; set; }

        /// <value>
        ///   Determines whether the client is logging out or not.
        /// </value>
        bool IsLoggingOut { get; set; }

        bool SendLogoutPacketWhenClosing { set; }

        // [Obsolete("LLClientView Specific - Circuits are unique to LLClientView")]
        uint CircuitCode { get; }

        IPEndPoint RemoteEndPoint { get; }
        bool TryGet<T>(out T iface);
        T Get<T>();

        event GenericMessage OnGenericMessage;

        // [Obsolete("LLClientView Specific - Replace with more bare-bones arguments.")]
        event ImprovedInstantMessage OnInstantMessage;
        event PreSendImprovedInstantMessage OnPreSendInstantMessage;
        // [Obsolete("LLClientView Specific - Replace with more bare-bones arguments. Rename OnChat.")]
        event ChatMessage OnChatFromClient;
        // [Obsolete("LLClientView Specific - Remove bitbuckets. Adam, can you be more specific here..  as I don't see any bit buckets.")]
        event RezObject OnRezObject;
        // [Obsolete("LLClientView Specific - Replace with more suitable arguments.")]
        event ModifyTerrain OnModifyTerrain;
        event BakeTerrain OnBakeTerrain;
        event EstateChangeInfo OnEstateChangeInfo;
        // [Obsolete("LLClientView Specific.")]
        event SetAppearance OnSetAppearance;
        // [Obsolete("LLClientView Specific - Replace and rename OnAvatarUpdate. Difference from SetAppearance?")]
        event AvatarNowWearing OnAvatarNowWearing;
        event RezSingleAttachmentFromInv OnRezSingleAttachmentFromInv;
        event UUIDNameRequest OnDetachAttachmentIntoInv;
        event ObjectAttach OnObjectAttach;
        event ObjectDeselect OnObjectDetach;
        event ObjectDrop OnObjectDrop;
        event StartAnim OnStartAnim;
        event StopAnim OnStopAnim;
        event LinkObjects OnLinkObjects;
        event DelinkObjects OnDelinkObjects;
        event RequestMapBlocks OnRequestMapBlocks;
        event RequestMapName OnMapNameRequest;
        event TeleportLocationRequest OnTeleportLocationRequest;
        event RequestAvatarProperties OnRequestAvatarProperties;
        event SetAlwaysRun OnSetAlwaysRun;
        event TeleportLandmarkRequest OnTeleportLandmarkRequest;
        event DeRezObject OnDeRezObject;
        event Action<IClientAPI> OnRegionHandShakeReply;
        event GenericCall1 OnRequestWearables;
        event GenericCall1 OnCompleteMovementToRegion;
        event UpdateAgent OnAgentUpdate;
        event AgentRequestSit OnAgentRequestSit;
        event AgentSit OnAgentSit;
        event AvatarPickerRequest OnAvatarPickerRequest;
        event Action<IClientAPI> OnRequestAvatarsData;
        event AddNewPrim OnAddPrim;

        event FetchInventory OnAgentDataUpdateRequest;
        event TeleportLocationRequest OnSetStartLocationRequest;

        event RequestGodlikePowers OnRequestGodlikePowers;
        event GodKickUser OnGodKickUser;

        event ObjectDuplicate OnObjectDuplicate;
        event ObjectDuplicateOnRay OnObjectDuplicateOnRay;
        event GrabObject OnGrabObject;
        event DeGrabObject OnDeGrabObject;
        event MoveObject OnGrabUpdate;
        event SpinStart OnSpinStart;
        event SpinObject OnSpinUpdate;
        event SpinStop OnSpinStop;

        event UpdateShape OnUpdatePrimShape;
        event ObjectExtraParams OnUpdateExtraParams;
        event ObjectRequest OnObjectRequest;
        event ObjectSelect OnObjectSelect;
        event ObjectDeselect OnObjectDeselect;
        event GenericCall7 OnObjectDescription;
        event GenericCall7 OnObjectName;
        event GenericCall7 OnObjectClickAction;
        event GenericCall7 OnObjectMaterial;
        event RequestObjectPropertiesFamily OnRequestObjectPropertiesFamily;
        event UpdatePrimFlags OnUpdatePrimFlags;
        event UpdatePrimTexture OnUpdatePrimTexture;
        event UpdateVectorWithUpdate OnUpdatePrimGroupPosition;
        event UpdateVectorWithUpdate OnUpdatePrimSinglePosition;
        event UpdatePrimRotation OnUpdatePrimGroupRotation;
        event UpdatePrimSingleRotation OnUpdatePrimSingleRotation;
        event UpdatePrimSingleRotationPosition OnUpdatePrimSingleRotationPosition;
        event UpdatePrimGroupRotation OnUpdatePrimGroupMouseRotation;
        event UpdateVector OnUpdatePrimScale;
        event UpdateVector OnUpdatePrimGroupScale;
        event StatusChange OnChildAgentStatus;
        event ObjectPermissions OnObjectPermissions;

        event CreateNewInventoryItem OnCreateNewInventoryItem;
        event LinkInventoryItem OnLinkInventoryItem;
        event CreateInventoryFolder OnCreateNewInventoryFolder;
        event UpdateInventoryFolder OnUpdateInventoryFolder;
        event MoveInventoryFolder OnMoveInventoryFolder;
        event FetchInventoryDescendents OnFetchInventoryDescendents;
        event PurgeInventoryDescendents OnPurgeInventoryDescendents;
        event FetchInventory OnFetchInventory;
        event RequestTaskInventory OnRequestTaskInventory;
        event UpdateInventoryItem OnUpdateInventoryItem;
        event CopyInventoryItem OnCopyInventoryItem;
        event MoveInventoryItem OnMoveInventoryItem;
        event RemoveInventoryFolder OnRemoveInventoryFolder;
        event RemoveInventoryItem OnRemoveInventoryItem;
        event UDPAssetUploadRequest OnAssetUploadRequest;
        event XferReceive OnXferReceive;
        event RequestXfer OnRequestXfer;
        event ConfirmXfer OnConfirmXfer;
        event AbortXfer OnAbortXfer;
        event RezScript OnRezScript;
        event UpdateTaskInventory OnUpdateTaskInventory;
        event MoveTaskInventory OnMoveTaskItem;
        event RemoveTaskInventory OnRemoveTaskItem;

        event UUIDNameRequest OnNameFromUUIDRequest;

        event ParcelAccessListRequest OnParcelAccessListRequest;
        event ParcelAccessListUpdateRequest OnParcelAccessListUpdateRequest;
        event ParcelPropertiesRequest OnParcelPropertiesRequest;
        event ParcelDivideRequest OnParcelDivideRequest;
        event ParcelJoinRequest OnParcelJoinRequest;
        event ParcelPropertiesUpdateRequest OnParcelPropertiesUpdateRequest;
        event ParcelSelectObjects OnParcelSelectObjects;
        event ParcelObjectOwnerRequest OnParcelObjectOwnerRequest;
        event ParcelAbandonRequest OnParcelAbandonRequest;
        event ParcelGodForceOwner OnParcelGodForceOwner;
        event ParcelReclaim OnParcelReclaim;
        event ParcelReturnObjectsRequest OnParcelReturnObjectsRequest;
        event ParcelReturnObjectsRequest OnParcelDisableObjectsRequest;
        event ParcelDeedToGroup OnParcelDeedToGroup;
        event RegionInfoRequest OnRegionInfoRequest;
        event EstateCovenantRequest OnEstateCovenantRequest;

        event FriendActionDelegate OnApproveFriendRequest;
        event FriendActionDelegate OnDenyFriendRequest;
        event FriendshipTermination OnTerminateFriendship;

        // Financial packets
        event MoneyTransferRequest OnMoneyTransferRequest;
        event EconomyDataRequest OnEconomyDataRequest;

        event MoneyBalanceRequest OnMoneyBalanceRequest;
        event UpdateAvatarProperties OnUpdateAvatarProperties;
        event ParcelBuy OnParcelBuy;
        event RequestPayPrice OnRequestPayPrice;
        event ObjectSaleInfo OnObjectSaleInfo;
        event ObjectBuy OnObjectBuy;
        event BuyObjectInventory OnBuyObjectInventory;

        event RequestTerrain OnRequestTerrain;

        event RequestTerrain OnUploadTerrain;

        event ObjectIncludeInSearch OnObjectIncludeInSearch;

        event UUIDNameRequest OnTeleportHomeRequest;

        event ScriptAnswer OnScriptAnswer;

        event AgentSit OnUndo;
        event AgentSit OnRedo;
        event LandUndo OnLandUndo;

        event ForceReleaseControls OnForceReleaseControls;
        event GodLandStatRequest OnLandStatRequest;

        event DetailedEstateDataRequest OnDetailedEstateDataRequest;
        event SetEstateFlagsRequest OnSetEstateFlagsRequest;
        event SetEstateTerrainBaseTexture OnSetEstateTerrainBaseTexture;
        event SetEstateTerrainDetailTexture OnSetEstateTerrainDetailTexture;
        event SetEstateTerrainTextureHeights OnSetEstateTerrainTextureHeights;
        event CommitEstateTerrainTextureRequest OnCommitEstateTerrainTextureRequest;
        event SetRegionTerrainSettings OnSetRegionTerrainSettings;
        event EstateRestartSimRequest OnEstateRestartSimRequest;
        event EstateChangeCovenantRequest OnEstateChangeCovenantRequest;
        event UpdateEstateAccessDeltaRequest OnUpdateEstateAccessDeltaRequest;
        event SimulatorBlueBoxMessageRequest OnSimulatorBlueBoxMessageRequest;
        event EstateBlueBoxMessageRequest OnEstateBlueBoxMessageRequest;
        event EstateDebugRegionRequest OnEstateDebugRegionRequest;
        event EstateTeleportOneUserHomeRequest OnEstateTeleportOneUserHomeRequest;
        event EstateTeleportAllUsersHomeRequest OnEstateTeleportAllUsersHomeRequest;
        event UUIDNameRequest OnUUIDGroupNameRequest;

        event RegionHandleRequest OnRegionHandleRequest;
        event ParcelInfoRequest OnParcelInfoRequest;

        event RequestObjectPropertiesFamily OnObjectGroupRequest;
        event ScriptReset OnScriptReset;
        event GetScriptRunning OnGetScriptRunning;
        event SetScriptRunning OnSetScriptRunning;
        event UpdateVector OnAutoPilotGo;

        event ActivateGesture OnActivateGesture;
        event DeactivateGesture OnDeactivateGesture;
        event ObjectOwner OnObjectOwner;

        event DirPlacesQuery OnDirPlacesQuery;
        event DirFindQuery OnDirFindQuery;
        event DirLandQuery OnDirLandQuery;
        event DirPopularQuery OnDirPopularQuery;
        event DirClassifiedQuery OnDirClassifiedQuery;
        event EventInfoRequest OnEventInfoRequest;
        event ParcelSetOtherCleanTime OnParcelSetOtherCleanTime;

        event MapItemRequest OnMapItemRequest;

        event OfferCallingCard OnOfferCallingCard;
        event AcceptCallingCard OnAcceptCallingCard;
        event DeclineCallingCard OnDeclineCallingCard;
        event SoundTrigger OnSoundTrigger;

        event StartLure OnStartLure;
        event TeleportLureRequest OnTeleportLureRequest;
        event NetworkStats OnNetworkStatsUpdate;

        event ClassifiedInfoRequest OnClassifiedInfoRequest;
        event ClassifiedInfoUpdate OnClassifiedInfoUpdate;
        event ClassifiedDelete OnClassifiedDelete;
        event ClassifiedDelete OnClassifiedGodDelete;

        event EventNotificationAddRequest OnEventNotificationAddRequest;
        event EventNotificationRemoveRequest OnEventNotificationRemoveRequest;
        event EventGodDelete OnEventGodDelete;

        event ParcelDwellRequest OnParcelDwellRequest;

        event UserInfoRequest OnUserInfoRequest;
        event UpdateUserInfo OnUpdateUserInfo;

        event RetrieveInstantMessages OnRetrieveInstantMessages;

        event PickDelete OnPickDelete;
        event PickGodDelete OnPickGodDelete;
        event PickInfoUpdate OnPickInfoUpdate;
        event AvatarNotesUpdate OnAvatarNotesUpdate;
        event AvatarInterestUpdate OnAvatarInterestUpdate;
        event GrantUserFriendRights OnGrantUserRights;

        event MuteListRequest OnMuteListRequest;

        event PlacesQuery OnPlacesQuery;

        event FindAgentUpdate OnFindAgent;
        event TrackAgentUpdate OnTrackAgent;
        event NewUserReport OnUserReport;
        event SaveStateHandler OnSaveState;
        event GroupAccountSummaryRequest OnGroupAccountSummaryRequest;
        event GroupAccountDetailsRequest OnGroupAccountDetailsRequest;
        event GroupAccountTransactionsRequest OnGroupAccountTransactionsRequest;
        event FreezeUserUpdate OnParcelFreezeUser;
        event EjectUserUpdate OnParcelEjectUser;
        event ParcelBuyPass OnParcelBuyPass;
        event ParcelGodMark OnParcelGodMark;
        event GroupActiveProposalsRequest OnGroupActiveProposalsRequest;
        event GroupVoteHistoryRequest OnGroupVoteHistoryRequest;
        event SimWideDeletesDelegate OnSimWideDeletes;
        event GroupProposalBallotRequest OnGroupProposalBallotRequest;
        event SendPostcard OnSendPostcard;
        event MuteListEntryUpdate OnUpdateMuteListEntry;
        event MuteListEntryRemove OnRemoveMuteListEntry;
        event GodlikeMessage OnGodlikeMessage;
        event GodUpdateRegionInfoUpdate OnGodUpdateRegionInfoUpdate;

        event ChangeInventoryItemFlags OnChangeInventoryItemFlags;
        event TeleportCancel OnTeleportCancel;
        event GodlikeMessage OnEstateTelehubRequest;
        event ViewerStartAuction OnViewerStartAuction;
        event AgentCachedTextureRequest OnAgentCachedTextureRequest;

        /// <summary>
        ///   Set the debug level at which packet output should be printed to console.
        /// </summary>
        void SetDebugPacketLevel(int newDebug);

        void ProcessInPacket(Packet NewPack);
        void Close(bool forceClose);
        void Stop();
        void Kick(string message);

        //     void ActivateGesture(UUID assetId, UUID gestureId);

        /// <summary>
        ///   Tell this client what items it should be wearing now
        /// </summary>
        void SendWearables(AvatarWearable[] wearables, int serial);

        void SendAgentCachedTexture(List<CachedAgentArgs> args);

        /// <summary>
        ///   Send information about the given agent's appearance to another client.
        /// </summary>
        /// <param name = "agentID">The id of the agent associated with the appearance</param>
        /// <param name = "visualParams"></param>
        /// <param name = "textureEntry"></param>
        void SendAppearance(UUID agentID, byte[] visualParams, byte[] textureEntry);

        void SendStartPingCheck(byte seq);

        /// <summary>
        ///   Tell the client that an object has been deleted
        /// </summary>
        /// <param name = "regionHandle"></param>
        /// <param name = "localID"></param>
        void SendKillObject(ulong regionHandle, IEntity[] entities);

        void SendKillObject(ulong regionHandle, uint[] entities);

        void SendAnimations(AnimationGroup animations);
        void SendRegionHandshake(RegionInfo regionInfo, RegionHandshakeArgs args);

        void SendChatMessage(string message, byte type, Vector3 fromPos, string fromName, UUID fromAgentID, byte source,
                             byte audible);

        void SendInstantMessage(GridInstantMessage im);

        void SendGenericMessage(string method, List<string> message);
        void SendGenericMessage(string method, List<byte[]> message);

        /// <summary>
        ///   Send the entire terrain map to the client
        /// </summary>
        /// <param name = "map"></param>
        void SendLayerData(short[] map);

        void ForceSendOnAgentUpdate(IClientAPI client, AgentUpdateArgs args);
        void OnForceChatFromViewer(IClientAPI sender, OSChatMessage e);

        /// <summary>
        ///   Send one patch to the client
        ///   Note: x and y variables are NOT positions, they are terrain patches!
        /// </summary>
        /// <param name = "x"></param>
        /// <param name = "y"></param>
        /// <param name = "map"></param>
        void SendLayerData(int px, int py, short[] map);

        /// <summary>
        ///   Send an array of patches to the client
        ///   Note: all x and y variables are NOT positions, they are terrain patches!
        /// </summary>
        /// <param name = "x"></param>
        /// <param name = "y"></param>
        /// <param name = "map"></param>
        void SendLayerData(int[] x, int[] y, short[] map, TerrainPatch.LayerType type);

        void SendWindData(Vector2[] windSpeeds);
        void SendCloudData(float[] cloudCover);

        void MoveAgentIntoRegion(RegionInfo regInfo, Vector3 pos, Vector3 look);

        /// <summary>
        ///   Return circuit information for this client.
        /// </summary>
        /// <returns></returns>
        AgentCircuitData RequestClientInfo();

        void SendMapBlock(List<MapBlockData> mapBlocks, uint flag);
        void SendLocalTeleport(Vector3 position, Vector3 lookAt, uint flags);

        void SendRegionTeleport(ulong regionHandle, byte simAccess, IPEndPoint regionExternalEndPoint, uint locationID,
                                uint flags, string capsURL);

        void SendTeleportFailed(string reason);
        void SendTeleportStart(uint flags);
        void SendTeleportProgress(uint flags, string message);

        void SendMoneyBalance(UUID transaction, bool success, byte[] description, int balance);
        void SendPayPrice(UUID objectID, int[] payPrice);

        void SendCoarseLocationUpdate(List<UUID> users, List<Vector3> CoarseLocations);

        void SetChildAgentThrottle(byte[] throttle);

        void SendAvatarDataImmediate(IEntity avatar);
        void SendAvatarUpdate(IEnumerable<EntityUpdate> updates);
        void SendPrimUpdate(IEnumerable<EntityUpdate> updates);

        void SendInventoryFolderDetails(UUID ownerID, UUID folderID, List<InventoryItemBase> items,
                                        List<InventoryFolderBase> folders, int version, bool fetchFolders,
                                        bool fetchItems);

        void SendInventoryItemDetails(UUID ownerID, InventoryItemBase item);

        /// <summary>
        ///   Tell the client that we have created the item it requested.
        /// </summary>
        /// <param name = "Item"></param>
        void SendInventoryItemCreateUpdate(InventoryItemBase Item, uint callbackId);

        void SendRemoveInventoryItem(UUID itemID);

        void SendTakeControls(int controls, bool passToAgent, bool TakeControls);

        void SendTaskInventory(UUID taskID, short serial, byte[] fileName);

        /// <summary>
        ///   Used by the server to inform the client of new inventory items.
        /// </summary>
        /// <param name = "node"></param>
        void SendBulkUpdateInventory(InventoryItemBase node);

        /// <summary>
        ///   Used by the server to inform the client of new inventory items/folders.
        /// </summary>
        /// <param name = "node"></param>
        void SendBulkUpdateInventory(InventoryFolderBase node);

        void SendXferPacket(ulong xferID, uint packet, byte[] data);

        void SendAbortXferPacket(ulong xferID);

        void SendEconomyData(float EnergyEfficiency, int ObjectCapacity, int ObjectCount, int PriceEnergyUnit,
                             int PriceGroupCreate, int PriceObjectClaim, float PriceObjectRent,
                             float PriceObjectScaleFactor,
                             int PriceParcelClaim, float PriceParcelClaimFactor, int PriceParcelRent,
                             int PricePublicObjectDecay,
                             int PricePublicObjectDelete, int PriceRentLight, int PriceUpload, int TeleportMinPrice,
                             float TeleportPriceExponent);

        void SendAvatarPickerReply(AvatarPickerReplyAgentDataArgs AgentData, List<AvatarPickerReplyDataArgs> Data);

        void SendAgentDataUpdate(UUID agentid, UUID activegroupid, string firstname, string lastname, ulong grouppowers,
                                 string groupname, string grouptitle);

        void SendPreLoadSound(UUID objectID, UUID ownerID, UUID soundID);
        void SendPlayAttachedSound(UUID soundID, UUID objectID, UUID ownerID, float gain, byte flags);

        void SendTriggeredSound(UUID soundID, UUID ownerID, UUID objectID, UUID parentID, ulong handle, Vector3 position,
                                float gain);

        void SendAttachedSoundGainChange(UUID objectID, float gain);

        void SendNameReply(UUID profileId, string firstname, string lastname);
        void SendAlertMessage(string message);

        void SendAgentAlertMessage(string message, bool modal);
        void SendLoadURL(string objectname, UUID objectID, UUID ownerID, bool groupOwned, string message, string url);

        /// <summary>
        ///   Open a dialog box on the client.
        /// </summary>
        /// <param name = "objectname"></param>
        /// <param name = "objectID"></param>
        /// <param name = "ownerID">/param>
        ///   <param name = "ownerFirstName"></param>
        ///   <param name = "ownerLastName"></param>
        ///   <param name = "msg"></param>
        ///   <param name = "textureID"></param>
        ///   <param name = "ch"></param>
        ///   <param name = "buttonlabels"></param>
        void SendDialog(string objectname, UUID objectID, UUID ownerID, string ownerFirstName, string ownerLastName,
                        string msg, UUID textureID, int ch,
                        string[] buttonlabels);

        /// <summary>
        ///   Update the client as to where the sun is currently located.
        /// </summary>
        /// <param name = "sunPos"></param>
        /// <param name = "sunVel"></param>
        /// <param name = "CurrentTime">Seconds since Unix Epoch 01/01/1970 00:00:00</param>
        /// <param name = "SecondsPerSunCycle"></param>
        /// <param name = "SecondsPerYear"></param>
        /// <param name = "OrbitalPosition">The orbital position is given in radians, and must be "adjusted" for the linden client, see LLClientView</param>
        void SendSunPos(Vector3 sunPos, Vector3 sunVel, ulong CurrentTime, uint SecondsPerSunCycle, uint SecondsPerYear,
                        float OrbitalPosition);

        void SendViewerEffect(ViewerEffectPacket.EffectBlock[] effectBlocks);
        UUID GetDefaultAnimation(string name);

        void SendAvatarProperties(UUID avatarID, string aboutText, string bornOn, Byte[] charterMember, string flAbout,
                                  uint flags, UUID flImageID, UUID imageID, string profileURL, UUID partnerID);

        void SendScriptQuestion(UUID taskID, string taskName, string ownerName, UUID itemID, int question);
        void SendHealth(float health);


        void SendEstateList(UUID invoice, int code, UUID[] Data, uint estateID);

        void SendBannedUserList(UUID invoice, EstateBan[] banlist, uint estateID);

        void SendRegionInfoToEstateMenu(RegionInfoForEstateMenuArgs args);
        void SendEstateCovenantInformation(UUID covenant, int covenantLastUpdated);

        void SendDetailedEstateData(UUID invoice, string estateName, uint estateID, uint parentEstate, uint estateFlags,
                                    uint sunPosition, UUID covenant, int covenantLastUpdated, string abuseEmail,
                                    UUID estateOwner);

        void SendLandProperties(int sequence_id, bool snap_selection, int request_result, LandData landData,
                                float simObjectBonusFactor, int parcelObjectCapacity, int simObjectCapacity,
                                uint regionFlags);

        void SendLandAccessListData(List<UUID> avatars, uint accessFlag, int localLandID);
        void SendForceClientSelectObjects(List<uint> objectIDs);
        void SendCameraConstraint(Vector4 ConstraintPlane);
        void SendLandObjectOwners(List<LandObjectOwners> objOwners);
        void SendLandParcelOverlay(byte[] data, int sequence_id);

        void SendAssetUploadCompleteMessage(sbyte AssetType, bool Success, UUID AssetFullID);
        void SendConfirmXfer(ulong xferID, uint PacketID);
        void SendXferRequest(ulong XferID, short AssetType, UUID vFileID, byte FilePath, byte[] FileName);

        void SendInitiateDownload(string simFileName, string clientFileName);

        /// <summary>
        ///   Send the first part of a texture.  For sufficiently small textures, this may be the only packet.
        /// </summary>
        /// <param name = "numParts"></param>
        /// <param name = "ImageUUID"></param>
        /// <param name = "ImageSize"></param>
        /// <param name = "ImageData"></param>
        /// <param name = "imageCodec"></param>
        void SendImageFirstPart(ushort numParts, UUID ImageUUID, uint ImageSize, byte[] ImageData, byte imageCodec);

        /// <summary>
        ///   Send the next packet for a series of packets making up a single texture, 
        ///   as established by SendImageFirstPart()
        /// </summary>
        /// <param name = "partNumber"></param>
        /// <param name = "imageUuid"></param>
        /// <param name = "imageData"></param>
        void SendImageNextPart(ushort partNumber, UUID imageUuid, byte[] imageData);

        /// <summary>
        ///   Tell the client that the requested texture cannot be found
        /// </summary>
        void SendImageNotFound(UUID imageid);

        /// <summary>
        ///   Send statistical information about the sim to the client.
        /// </summary>
        /// <param name = "stats"></param>
        void SendSimStats(SimStats stats);

        void SendObjectPropertiesFamilyData(uint RequestFlags, UUID ObjectUUID, UUID OwnerID, UUID GroupID,
                                            uint BaseMask, uint OwnerMask, uint GroupMask, uint EveryoneMask,
                                            uint NextOwnerMask, int OwnershipCost, byte SaleType, int SalePrice,
                                            uint Category,
                                            UUID LastOwnerID, string ObjectName, string Description);

        void SendObjectPropertiesReply(List<IEntity> part);

        void SendAgentOffline(UUID[] agentIDs);

        void SendAgentOnline(UUID[] agentIDs);

        void SendSitResponse(UUID TargetID, Vector3 OffsetPos, Quaternion SitOrientation, bool autopilot,
                             Vector3 CameraAtOffset, Vector3 CameraEyeOffset, bool ForceMouseLook);

        void SendAdminResponse(UUID Token, uint AdminLevel);

        void SendGroupMembership(GroupMembershipData[] GroupMembership);

        void SendGroupNameReply(UUID groupLLUID, string GroupName);

        void SendJoinGroupReply(UUID groupID, bool success);

        void SendEjectGroupMemberReply(UUID agentID, UUID groupID, bool success);

        void SendLeaveGroupReply(UUID groupID, bool success);

        void SendCreateGroupReply(UUID groupID, bool success, string message);

        void SendLandStatReply(uint reportType, uint requestFlags, uint resultCount, LandStatReportItem[] lsrpia);

        void SendScriptRunningReply(UUID objectID, UUID itemID, bool running);

        void SendAsset(AssetRequestToClient req);

        byte[] GetThrottlesPacked(float multiplier);

        event ViewerEffectEventHandler OnViewerEffect;
        event Action<IClientAPI> OnLogout;
        event Action<IClientAPI> OnConnectionClosed;

        void SendBlueBoxMessage(UUID FromAvatarID, String FromAvatarName, String Message);

        void SendLogoutPacket();
        EndPoint GetClientEP();

        void SendSetFollowCamProperties(UUID objectID, SortedDictionary<int, float> parameters);
        void SendClearFollowCamProperties(UUID objectID);

        void SendRegionHandle(UUID regoinID, ulong handle);
        void SendParcelInfo(LandData land, UUID parcelID, uint x, uint y, string SimName);
        void SendScriptTeleportRequest(string objName, string simName, Vector3 pos, Vector3 lookAt);

        void SendDirPlacesReply(UUID queryID, DirPlacesReplyData[] data);
        void SendDirPeopleReply(UUID queryID, DirPeopleReplyData[] data);
        void SendDirEventsReply(UUID queryID, DirEventsReplyData[] data);
        void SendDirGroupsReply(UUID queryID, DirGroupsReplyData[] data);
        void SendDirClassifiedReply(UUID queryID, DirClassifiedReplyData[] data);
        void SendDirLandReply(UUID queryID, DirLandReplyData[] data);
        void SendDirPopularReply(UUID queryID, DirPopularReplyData[] data);
        void SendEventInfoReply(EventData info);

        void SendMapItemReply(mapItemReply[] replies, uint mapitemtype, uint flags);

        void SendAvatarGroupsReply(UUID avatarID, GroupMembershipData[] data);
        void SendOfferCallingCard(UUID srcID, UUID transactionID);
        void SendAcceptCallingCard(UUID transactionID);
        void SendDeclineCallingCard(UUID transactionID);

        void SendTerminateFriend(UUID exFriendID);

        void SendAvatarClassifiedReply(UUID targetID, UUID[] classifiedID, string[] name);

        void SendClassifiedInfoReply(UUID classifiedID, UUID creatorID, uint creationDate, uint expirationDate,
                                     uint category, string name, string description, UUID parcelID, uint parentEstate,
                                     UUID snapshotID, string simName, Vector3 globalPos, string parcelName,
                                     byte classifiedFlags, int price);

        void SendAgentDropGroup(UUID groupID);
        void SendAvatarNotesReply(UUID targetID, string text);
        void SendAvatarPicksReply(UUID targetID, Dictionary<UUID, string> picks);

        void SendPickInfoReply(UUID pickID, UUID creatorID, bool topPick, UUID parcelID, string name, string desc,
                               UUID snapshotID, string user, string originalName, string simName, Vector3 posGlobal,
                               int sortOrder, bool enabled);

        void SendAvatarClassifiedReply(UUID targetID, Dictionary<UUID, string> classifieds);

        void SendParcelDwellReply(int localID, UUID parcelID, float dwell);

        void SendUserInfoReply(bool imViaEmail, bool visible, string email);

        void SendUseCachedMuteList();

        void SendMuteListUpdate(string filename);

        void SendGroupActiveProposals(UUID groupID, UUID transactionID, GroupActiveProposals[] Proposals);

        void SendGroupVoteHistory(UUID groupID, UUID transactionID, GroupVoteHistory Vote, GroupVoteHistoryItem[] Items);

        bool AddGenericPacketHandler(string MethodName, GenericMessage handler);

        bool RemoveGenericPacketHandler(string MethodName);

        void SendRebakeAvatarTextures(UUID textureID);

        void SendAvatarInterestsReply(UUID avatarID, uint wantMask, string wantText, uint skillsMask, string skillsText,
                                      string languages);

        void SendGroupAccountingDetails(IClientAPI sender, UUID groupID, UUID transactionID, UUID sessionID, int amt, int currentInterval, int interval, string startDate, GroupAccountHistory[] history);

        void SendGroupAccountingSummary(IClientAPI sender, UUID groupID, UUID requestID, int moneyAmt, int totalTier, int usedTier, string startDate, int currentInterval, int intervalLength, string taxDate, string lastTaxDate, int parcelDirectoryFee, int landTaxFee, int groupTaxFee, int objectTaxFee);

        void SendGroupTransactionsSummaryDetails(IClientAPI sender, UUID groupID, UUID transactionID,
                                                        UUID sessionID, int currentInterval, int intervalDays, string startingDate, GroupAccountHistory[] history);

        void SendChangeUserRights(UUID agentID, UUID friendID, int rights);

        void SendTextBoxRequest(string message, int chatChannel, string objectname, string ownerFirstName,
                                string ownerLastName, UUID objectId);

        void SendPlacesQuery(ExtendedLandData[] LandData, UUID queryID, UUID transactionID);
        void FireUpdateParcel(LandUpdateArgs args, int LocalID);

        void SendTelehubInfo(Vector3 TelehubPos, Quaternion TelehubRot, List<Vector3> SpawnPoint, UUID ObjectID,
                             string nameT);

        void StopFlying(IEntity presence);

        void Reset();

        void HandleChatFromClient(OSChatMessage args);

        #region Parcel Methods

        void SendParcelMediaCommand(uint flags, ParcelMediaCommandEnum command, float time);

        void SendParcelMediaUpdate(string mediaUrl, UUID mediaTextureID,
                                   byte autoScale, string mediaType, string mediaDesc, int mediaWidth, int mediaHeight,
                                   byte mediaLoop);

        #endregion

        UUID ScopeID { get; set; }
        List<UUID> AllScopeIDs { get; set; }
    }
}