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
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Xml;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Messages.Linden;
using OpenMetaverse.Packets;
using OpenMetaverse.StructuredData;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using RegionFlags = OpenMetaverse.RegionFlags;

namespace OpenSim.Region.ClientStack.LindenUDP
{
    public delegate bool PacketMethod(IClientAPI simClient, Packet packet);

    /// <summary>
    ///   Handles new client connections
    ///   Constructor takes a single Packet and authenticates everything
    /// </summary>
    public sealed class LLClientView : IClientAPI, IStatsCollector
    {
        /// <value>
        ///   Debug packet level.  See OpenSim.RegisterConsoleCommands() for more details.
        /// </value>
        private int m_debugPacketLevel;

        private readonly bool m_allowUDPInv;

        #region Events

        public event BinaryGenericMessage OnBinaryGenericMessage;
        public event Action<IClientAPI> OnLogout;
        public event ObjectPermissions OnObjectPermissions;
        public event Action<IClientAPI> OnConnectionClosed;
        public event ViewerEffectEventHandler OnViewerEffect;
        public event ImprovedInstantMessage OnInstantMessage;
        public event PreSendImprovedInstantMessage OnPreSendInstantMessage;
        public event ChatMessage OnChatFromClient;
        public event RezObject OnRezObject;
        public event DeRezObject OnDeRezObject;
        public event ModifyTerrain OnModifyTerrain;
        public event Action<IClientAPI> OnRegionHandShakeReply;
        public event GenericCall1 OnRequestWearables;
        public event SetAppearance OnSetAppearance;
        public event AvatarNowWearing OnAvatarNowWearing;
        public event RezSingleAttachmentFromInv OnRezSingleAttachmentFromInv;
        public event UUIDNameRequest OnDetachAttachmentIntoInv;
        public event ObjectAttach OnObjectAttach;
        public event ObjectDeselect OnObjectDetach;
        public event ObjectDrop OnObjectDrop;
        public event GenericCall1 OnCompleteMovementToRegion;
        public event UpdateAgent OnAgentUpdate;
        public event AgentRequestSit OnAgentRequestSit;
        public event AgentSit OnAgentSit;
        public event AvatarPickerRequest OnAvatarPickerRequest;
        public event StartAnim OnStartAnim;
        public event StopAnim OnStopAnim;
        public event Action<IClientAPI> OnRequestAvatarsData;
        public event LinkObjects OnLinkObjects;
        public event DelinkObjects OnDelinkObjects;
        public event GrabObject OnGrabObject;
        public event DeGrabObject OnDeGrabObject;
        public event SpinStart OnSpinStart;
        public event SpinStop OnSpinStop;
        public event ObjectDuplicate OnObjectDuplicate;
        public event ObjectDuplicateOnRay OnObjectDuplicateOnRay;
        public event MoveObject OnGrabUpdate;
        public event SpinObject OnSpinUpdate;
        public event AddNewPrim OnAddPrim;
        public event RequestGodlikePowers OnRequestGodlikePowers;
        public event GodKickUser OnGodKickUser;
        public event ObjectExtraParams OnUpdateExtraParams;
        public event UpdateShape OnUpdatePrimShape;
        public event ObjectRequest OnObjectRequest;
        public event ObjectSelect OnObjectSelect;
        public event ObjectDeselect OnObjectDeselect;
        public event GenericCall7 OnObjectDescription;
        public event GenericCall7 OnObjectName;
        public event GenericCall7 OnObjectClickAction;
        public event GenericCall7 OnObjectMaterial;
        public event ObjectIncludeInSearch OnObjectIncludeInSearch;
        public event RequestObjectPropertiesFamily OnRequestObjectPropertiesFamily;
        public event UpdatePrimFlags OnUpdatePrimFlags;
        public event UpdatePrimTexture OnUpdatePrimTexture;
        public event UpdateVectorWithUpdate OnUpdatePrimGroupPosition;
        public event UpdateVectorWithUpdate OnUpdatePrimSinglePosition;
        public event UpdatePrimRotation OnUpdatePrimGroupRotation;
        public event UpdatePrimSingleRotation OnUpdatePrimSingleRotation;
        public event UpdatePrimSingleRotationPosition OnUpdatePrimSingleRotationPosition;
        public event UpdatePrimGroupRotation OnUpdatePrimGroupMouseRotation;
        public event UpdateVector OnUpdatePrimScale;
        public event UpdateVector OnUpdatePrimGroupScale;

#pragma warning disable 67

        public event StatusChange OnChildAgentStatus;
        public event GenericMessage OnGenericMessage;
        public event BuyObjectInventory OnBuyObjectInventory;
        public event SetEstateTerrainBaseTexture OnSetEstateTerrainBaseTexture;

#pragma warning restore 67

        public event RequestMapBlocks OnRequestMapBlocks;
        public event RequestMapName OnMapNameRequest;
        public event TeleportLocationRequest OnTeleportLocationRequest;
        public event TeleportLandmarkRequest OnTeleportLandmarkRequest;
        public event RequestAvatarProperties OnRequestAvatarProperties;
        public event SetAlwaysRun OnSetAlwaysRun;
        public event FetchInventory OnAgentDataUpdateRequest;
        public event TeleportLocationRequest OnSetStartLocationRequest;
        public event UpdateAvatarProperties OnUpdateAvatarProperties;
        public event CreateNewInventoryItem OnCreateNewInventoryItem;
        public event LinkInventoryItem OnLinkInventoryItem;
        public event CreateInventoryFolder OnCreateNewInventoryFolder;
        public event UpdateInventoryFolder OnUpdateInventoryFolder;
        public event MoveInventoryFolder OnMoveInventoryFolder;
        public event FetchInventoryDescendents OnFetchInventoryDescendents;
        public event PurgeInventoryDescendents OnPurgeInventoryDescendents;
        public event FetchInventory OnFetchInventory;
        public event RequestTaskInventory OnRequestTaskInventory;
        public event UpdateInventoryItem OnUpdateInventoryItem;
        public event ChangeInventoryItemFlags OnChangeInventoryItemFlags;
        public event CopyInventoryItem OnCopyInventoryItem;
        public event MoveInventoryItem OnMoveInventoryItem;
        public event RemoveInventoryItem OnRemoveInventoryItem;
        public event RemoveInventoryFolder OnRemoveInventoryFolder;
        public event UDPAssetUploadRequest OnAssetUploadRequest;
        public event XferReceive OnXferReceive;
        public event RequestXfer OnRequestXfer;
        public event ConfirmXfer OnConfirmXfer;
        public event AbortXfer OnAbortXfer;
        public event RequestTerrain OnRequestTerrain;
        public event RezScript OnRezScript;
        public event UpdateTaskInventory OnUpdateTaskInventory;
        public event MoveTaskInventory OnMoveTaskItem;
        public event RemoveTaskInventory OnRemoveTaskItem;
        public event UUIDNameRequest OnNameFromUUIDRequest;
        public event ParcelAccessListRequest OnParcelAccessListRequest;
        public event ParcelAccessListUpdateRequest OnParcelAccessListUpdateRequest;
        public event ParcelPropertiesRequest OnParcelPropertiesRequest;
        public event ParcelDivideRequest OnParcelDivideRequest;
        public event ParcelJoinRequest OnParcelJoinRequest;
        public event ParcelPropertiesUpdateRequest OnParcelPropertiesUpdateRequest;
        public event ParcelSelectObjects OnParcelSelectObjects;
        public event ParcelObjectOwnerRequest OnParcelObjectOwnerRequest;
        public event ParcelAbandonRequest OnParcelAbandonRequest;
        public event ParcelGodForceOwner OnParcelGodForceOwner;
        public event ParcelReclaim OnParcelReclaim;
        public event ParcelReturnObjectsRequest OnParcelReturnObjectsRequest;
        public event ParcelReturnObjectsRequest OnParcelDisableObjectsRequest;
        public event ParcelDeedToGroup OnParcelDeedToGroup;
        public event RegionInfoRequest OnRegionInfoRequest;
        public event EstateCovenantRequest OnEstateCovenantRequest;
        public event FriendActionDelegate OnApproveFriendRequest;
        public event FriendActionDelegate OnDenyFriendRequest;
        public event FriendshipTermination OnTerminateFriendship;
        public event GrantUserFriendRights OnGrantUserRights;
        public event MoneyTransferRequest OnMoneyTransferRequest;
        public event EconomyDataRequest OnEconomyDataRequest;
        public event MoneyBalanceRequest OnMoneyBalanceRequest;
        public event ParcelBuy OnParcelBuy;
        public event UUIDNameRequest OnTeleportHomeRequest;
        public event UUIDNameRequest OnUUIDGroupNameRequest;
        public event ScriptAnswer OnScriptAnswer;
        public event RequestPayPrice OnRequestPayPrice;
        public event ObjectSaleInfo OnObjectSaleInfo;
        public event ObjectBuy OnObjectBuy;
        public event AgentSit OnUndo;
        public event AgentSit OnRedo;
        public event LandUndo OnLandUndo;
        public event ForceReleaseControls OnForceReleaseControls;
        public event GodLandStatRequest OnLandStatRequest;
        public event RequestObjectPropertiesFamily OnObjectGroupRequest;
        public event DetailedEstateDataRequest OnDetailedEstateDataRequest;
        public event SetEstateFlagsRequest OnSetEstateFlagsRequest;
        public event SetEstateTerrainDetailTexture OnSetEstateTerrainDetailTexture;
        public event SetEstateTerrainTextureHeights OnSetEstateTerrainTextureHeights;
        public event CommitEstateTerrainTextureRequest OnCommitEstateTerrainTextureRequest;
        public event SetRegionTerrainSettings OnSetRegionTerrainSettings;
        public event BakeTerrain OnBakeTerrain;
        public event RequestTerrain OnUploadTerrain;
        public event EstateChangeInfo OnEstateChangeInfo;
        public event EstateRestartSimRequest OnEstateRestartSimRequest;
        public event EstateChangeCovenantRequest OnEstateChangeCovenantRequest;
        public event UpdateEstateAccessDeltaRequest OnUpdateEstateAccessDeltaRequest;
        public event SimulatorBlueBoxMessageRequest OnSimulatorBlueBoxMessageRequest;
        public event EstateBlueBoxMessageRequest OnEstateBlueBoxMessageRequest;
        public event EstateDebugRegionRequest OnEstateDebugRegionRequest;
        public event EstateTeleportOneUserHomeRequest OnEstateTeleportOneUserHomeRequest;
        public event EstateTeleportAllUsersHomeRequest OnEstateTeleportAllUsersHomeRequest;
        public event RegionHandleRequest OnRegionHandleRequest;
        public event ParcelInfoRequest OnParcelInfoRequest;
        public event ScriptReset OnScriptReset;
        public event GetScriptRunning OnGetScriptRunning;
        public event SetScriptRunning OnSetScriptRunning;
        public event UpdateVector OnAutoPilotGo;
        public event ActivateGesture OnActivateGesture;
        public event DeactivateGesture OnDeactivateGesture;
        public event ObjectOwner OnObjectOwner;
        public event DirPlacesQuery OnDirPlacesQuery;
        public event DirFindQuery OnDirFindQuery;
        public event DirLandQuery OnDirLandQuery;
        public event DirPopularQuery OnDirPopularQuery;
        public event DirClassifiedQuery OnDirClassifiedQuery;
        public event EventInfoRequest OnEventInfoRequest;
        public event ParcelSetOtherCleanTime OnParcelSetOtherCleanTime;
        public event MapItemRequest OnMapItemRequest;
        public event OfferCallingCard OnOfferCallingCard;
        public event AcceptCallingCard OnAcceptCallingCard;
        public event DeclineCallingCard OnDeclineCallingCard;
        public event SoundTrigger OnSoundTrigger;
        public event StartLure OnStartLure;
        public event TeleportLureRequest OnTeleportLureRequest;
        public event NetworkStats OnNetworkStatsUpdate;
        public event ClassifiedInfoRequest OnClassifiedInfoRequest;
        public event ClassifiedInfoUpdate OnClassifiedInfoUpdate;
        public event ClassifiedDelete OnClassifiedDelete;
        public event ClassifiedDelete OnClassifiedGodDelete;
        public event EventNotificationAddRequest OnEventNotificationAddRequest;
        public event EventNotificationRemoveRequest OnEventNotificationRemoveRequest;
        public event EventGodDelete OnEventGodDelete;
        public event ParcelDwellRequest OnParcelDwellRequest;
        public event UserInfoRequest OnUserInfoRequest;
        public event UpdateUserInfo OnUpdateUserInfo;
        public event RetrieveInstantMessages OnRetrieveInstantMessages;
        public event PickDelete OnPickDelete;
        public event PickGodDelete OnPickGodDelete;
        public event PickInfoUpdate OnPickInfoUpdate;
        public event AvatarNotesUpdate OnAvatarNotesUpdate;
        public event MuteListRequest OnMuteListRequest;
        public event AvatarInterestUpdate OnAvatarInterestUpdate;
        public event PlacesQuery OnPlacesQuery;
        public event AgentFOV OnAgentFOV;
        public event FindAgentUpdate OnFindAgent;
        public event TrackAgentUpdate OnTrackAgent;
        public event NewUserReport OnUserReport;
        public event SaveStateHandler OnSaveState;
        public event GroupAccountSummaryRequest OnGroupAccountSummaryRequest;
        public event GroupAccountDetailsRequest OnGroupAccountDetailsRequest;
        public event GroupAccountTransactionsRequest OnGroupAccountTransactionsRequest;
        public event FreezeUserUpdate OnParcelFreezeUser;
        public event EjectUserUpdate OnParcelEjectUser;
        public event ParcelBuyPass OnParcelBuyPass;
        public event ParcelGodMark OnParcelGodMark;
        public event GroupActiveProposalsRequest OnGroupActiveProposalsRequest;
        public event GroupVoteHistoryRequest OnGroupVoteHistoryRequest;
        public event SimWideDeletesDelegate OnSimWideDeletes;
        public event SendPostcard OnSendPostcard;
        public event TeleportCancel OnTeleportCancel;
        public event MuteListEntryUpdate OnUpdateMuteListEntry;
        public event MuteListEntryRemove OnRemoveMuteListEntry;
        public event GodlikeMessage OnGodlikeMessage;
        public event GodUpdateRegionInfoUpdate OnGodUpdateRegionInfoUpdate;
        public event GodlikeMessage OnEstateTelehubRequest;
        public event ViewerStartAuction OnViewerStartAuction;
        public event GroupProposalBallotRequest OnGroupProposalBallotRequest;
        public event AgentCachedTextureRequest OnAgentCachedTextureRequest;

        #endregion Events

        #region Enums

        public enum TransferPacketStatus
        {
            MorePacketsToCome = 0,
            Done = 1,
            AssetSkip = 2,
            AssetAbort = 3,
            AssetRequestFailed = -1,
            AssetUnknownSource = -2, // Equivalent of a 404
            InsufficientPermissions = -3
        }

        #endregion

        #region Class Members

        // LLClientView Only
        public delegate void BinaryGenericMessage(Object sender, string method, byte[][] args);

        /// <summary>
        ///   Used to adjust Sun Orbit values so Linden based viewers properly position sun
        /// </summary>
        private const float m_sunPainDaHalfOrbitalCutoff = 4.712388980384689858f;

        private static readonly Dictionary<PacketType, PacketMethod> PacketHandlers =
            new Dictionary<PacketType, PacketMethod>(); //Global/static handlers for all clients

        private readonly LLUDPServer m_udpServer;
        private readonly LLUDPClient m_udpClient;
        private readonly UUID m_sessionId;
        private readonly UUID m_secureSessionId;
        private readonly UUID m_agentId;
        private readonly uint m_circuitCode;
        private readonly byte[] m_channelVersion = Utils.EmptyBytes;
        private readonly Dictionary<string, UUID> m_defaultAnimations = new Dictionary<string, UUID>();
        private readonly IGroupsModule m_GroupsModule;

        private int m_cachedTextureSerial;

        private bool m_disableFacelights;

        ///<value>
        ///  Maintain a record of all the objects killed.  This allows us to stop an update being sent from the
        ///  thread servicing the m_primFullUpdates queue after a kill.  If this happens the object persists as an
        ///  ownerless phantom.
        ///
        ///  All manipulation of this set has to occur under an m_entityUpdates.SyncRoot lock
        ///
        ///</value>
        //protected HashSet<uint> m_killRecord = new HashSet<uint>();
//        protected HashSet<uint> m_attachmentsSent;
        private int m_animationSequenceNumber = 1;

        private bool m_SendLogoutPacketWhenClosing = true;
        private AgentUpdateArgs lastarg;
        private bool m_IsActive = true;

        private readonly Dictionary<PacketType, PacketProcessor> m_packetHandlers =
            new Dictionary<PacketType, PacketProcessor>();

        private readonly Dictionary<string, GenericMessage> m_genericPacketHandlers =
            new Dictionary<string, GenericMessage>();

        //PauPaw:Local Generic Message handlers

        private readonly IScene m_scene;
        private readonly LLImageManager m_imageManager;
        private readonly string m_firstName;
        private readonly string m_lastName;
        private readonly string m_Name;
        private readonly EndPoint m_userEndPoint;
        private UUID m_activeGroupID;
        private string m_activeGroupName = String.Empty;
        private ulong m_activeGroupPowers;
        private uint m_agentFOVCounter;

        private readonly IAssetService m_assetService;
// ReSharper disable ConvertToConstant.Local
        private bool m_checkPackets = true;
// ReSharper restore ConvertToConstant.Local

        #endregion Class Members

        #region Properties

        public LLUDPClient UDPClient
        {
            get { return m_udpClient; }
        }

        public IPEndPoint RemoteEndPoint
        {
            get { return m_udpClient.RemoteEndPoint; }
        }

        public UUID SecureSessionId
        {
            get { return m_secureSessionId; }
        }

        public IScene Scene
        {
            get { return m_scene; }
        }

        public UUID SessionId
        {
            get { return m_sessionId; }
        }

        public Vector3 StartPos { get; set; }

        public UUID AgentId
        {
            get { return m_agentId; }
        }

        public UUID ScopeID
        {
            get;
            set;
        }

        public List<UUID> AllScopeIDs
        {
            get;
            set;
        }

        public UUID ActiveGroupId
        {
            get { return m_activeGroupID; }
        }

        public string ActiveGroupName
        {
            get { return m_activeGroupName; }
        }

        public ulong ActiveGroupPowers
        {
            get { return m_activeGroupPowers; }
        }

        /// <summary>
        ///   First name of the agent/avatar represented by the client
        /// </summary>
        public string FirstName
        {
            get { return m_firstName; }
        }

        /// <summary>
        ///   Last name of the agent/avatar represented by the client
        /// </summary>
        public string LastName
        {
            get { return m_lastName; }
        }

        /// <summary>
        ///   Full name of the client (first name and last name)
        /// </summary>
        public string Name
        {
            get { return m_Name; }
        }

        public uint CircuitCode
        {
            get { return m_circuitCode; }
        }

        public int NextAnimationSequenceNumber
        {
            get { return m_animationSequenceNumber; }
        }

        public bool IsActive
        {
            get { return m_IsActive; }
            set { m_IsActive = value; }
        }

        public bool IsLoggingOut { get; set; }

        public bool DisableFacelights
        {
            get { return m_disableFacelights; }
            set { m_disableFacelights = value; }
        }

        public bool SendLogoutPacketWhenClosing
        {
            set { m_SendLogoutPacketWhenClosing = value; }
        }

        #endregion Properties

        /// <summary>
        ///   Constructor
        /// </summary>
        public LLClientView(EndPoint remoteEP, IScene scene, LLUDPServer udpServer, LLUDPClient udpClient,
                            AgentCircuitData sessionInfo,
                            UUID agentId, UUID sessionId, uint circuitCode)
        {
            InitDefaultAnimations();

            m_scene = scene;

            IConfig advancedConfig = m_scene.Config.Configs["ClientStack.LindenUDP"];
            if (advancedConfig != null)
                m_allowUDPInv = advancedConfig.GetBoolean("AllowUDPInventory", m_allowUDPInv);

            //m_killRecord = new HashSet<uint>();
//            m_attachmentsSent = new HashSet<uint>();

            m_assetService = m_scene.RequestModuleInterface<IAssetService>();
            m_GroupsModule = scene.RequestModuleInterface<IGroupsModule>();
            m_imageManager = new LLImageManager(this, m_assetService, Scene.RequestModuleInterface<IJ2KDecoder>());
            ISimulationBase simulationBase = m_scene.RequestModuleInterface<ISimulationBase>();
            if (simulationBase != null)
                m_channelVersion = Util.StringToBytes256(simulationBase.Version);
            m_agentId = agentId;
            m_sessionId = sessionId;
            m_secureSessionId = sessionInfo.SecureSessionID;
            m_circuitCode = circuitCode;
            m_userEndPoint = remoteEP;
            UserAccount account = m_scene.UserAccountService.GetUserAccount(m_scene.RegionInfo.AllScopeIDs, m_agentId);
            if (account != null)
            {
                m_firstName = account.FirstName;
                m_lastName = account.LastName;
                m_Name = account.Name;
            }
            else
            {
                m_firstName = sessionInfo.firstname;
                m_lastName = sessionInfo.lastname;
                m_Name = sessionInfo.firstname + " " + sessionInfo.lastname;
            }
            StartPos = sessionInfo.startpos;

            m_udpServer = udpServer;
            m_udpClient = udpClient;
            m_udpClient.OnQueueEmpty += HandleQueueEmpty;
            m_udpClient.OnPacketStats += PopulateStats;

            RegisterLocalPacketHandlers();
        }

        public void Reset()
        {
            lastarg = null;
            //Reset the killObjectUpdate packet stats
            //m_killRecord = new HashSet<uint>();
        }

        public void SetDebugPacketLevel(int newDebug)
        {
            m_debugPacketLevel = newDebug;
        }

        #region Client Methods

        public void Stop()
        {
            // Send the STOP packet NOW, otherwise it doesn't get out in time
            DisableSimulatorPacket disable =
                (DisableSimulatorPacket) PacketPool.Instance.GetPacket(PacketType.DisableSimulator);
            OutPacket(disable, ThrottleOutPacketType.Immediate);
        }

        /// <summary>
        ///   Shut down the client view
        /// </summary>
        public void Close(bool forceClose)
        {
            //MainConsole.Instance.DebugFormat(
            //    "[CLIENT]: Close has been called for {0} attached to scene {1}",
            //    Name, m_scene.RegionInfo.RegionName);

            if (forceClose && !IsLoggingOut) //Don't send it to clients that are logging out
            {
                // Send the STOP packet NOW, otherwise it doesn't get out in time
                DisableSimulatorPacket disable =
                    (DisableSimulatorPacket) PacketPool.Instance.GetPacket(PacketType.DisableSimulator);
                OutPacket(disable, ThrottleOutPacketType.Immediate);
            }

            IsActive = false;

            // Shutdown the image manager
            if (m_imageManager != null)
                m_imageManager.Close();

            // Fire the callback for this connection closing
            if (OnConnectionClosed != null)
                OnConnectionClosed(this);

            // Flush all of the packets out of the UDP server for this client
            if (m_udpServer != null)
            {
                m_udpServer.Flush(m_udpClient);
                m_udpServer.RemoveClient(this);
            }

            // Disable UDP handling for this client
            m_udpClient.Shutdown();

            //MainConsole.Instance.InfoFormat("[CLIENTVIEW] Memory pre  GC {0}", System.GC.GetTotalMemory(false));
            //GC.Collect();
            //MainConsole.Instance.InfoFormat("[CLIENTVIEW] Memory post GC {0}", System.GC.GetTotalMemory(true));
        }

        public void Kick(string message)
        {
            if (!ChildAgentStatus())
            {
                KickUserPacket kupack = (KickUserPacket) PacketPool.Instance.GetPacket(PacketType.KickUser);
                kupack.UserInfo.AgentID = AgentId;
                kupack.UserInfo.SessionID = SessionId;
                kupack.TargetBlock.TargetIP = 0;
                kupack.TargetBlock.TargetPort = 0;
                kupack.UserInfo.Reason = Util.StringToBytes256(message);
                OutPacket(kupack, ThrottleOutPacketType.OutBand);
                // You must sleep here or users get no message!
                Thread.Sleep(500);
            }
        }

        #endregion Client Methods

        #region Packet Handling

        public void PopulateStats(int inPackets, int outPackets, int unAckedBytes)
        {
            NetworkStats handlerNetworkStatsUpdate = OnNetworkStatsUpdate;
            if (handlerNetworkStatsUpdate != null)
            {
                handlerNetworkStatsUpdate(inPackets, outPackets, unAckedBytes);
            }
        }

        public static bool AddPacketHandler(PacketType packetType, PacketMethod handler)
        {
            bool result = false;
            lock (PacketHandlers)
            {
                if (!PacketHandlers.ContainsKey(packetType))
                {
                    PacketHandlers.Add(packetType, handler);
                    result = true;
                }
            }
            return result;
        }

        public bool AddLocalPacketHandler(PacketType packetType, PacketMethod handler)
        {
            return AddLocalPacketHandler(packetType, handler, true);
        }

        public bool AddLocalPacketHandler(PacketType packetType, PacketMethod handler, bool runasync)
        {
            bool result = false;
            lock (m_packetHandlers)
            {
                if (!m_packetHandlers.ContainsKey(packetType))
                {
                    m_packetHandlers.Add(packetType, new PacketProcessor {method = handler, Async = runasync});
                    result = true;
                }
            }
            return result;
        }

        public bool AddGenericPacketHandler(string MethodName, GenericMessage handler)
        {
            MethodName = MethodName.ToLower().Trim();

            bool result = false;
            lock (m_genericPacketHandlers)
            {
                if (!m_genericPacketHandlers.ContainsKey(MethodName))
                {
                    m_genericPacketHandlers.Add(MethodName, handler);
                    result = true;
                }
            }
            return result;
        }

        public bool RemoveGenericPacketHandler(string MethodName)
        {
            MethodName = MethodName.ToLower().Trim();

            bool result = false;
            lock (m_genericPacketHandlers)
            {
                if (m_genericPacketHandlers.ContainsKey(MethodName))
                {
                    m_genericPacketHandlers.Remove(MethodName);
                    result = true;
                }
            }
            return result;
        }

        /// <summary>
        ///   Try to process a packet using registered packet handlers
        /// </summary>
        /// <param name = "packet"></param>
        /// <returns>True if a handler was found which successfully processed the packet.</returns>
        private bool ProcessPacketMethod(Packet packet)
        {
            bool result = false;
            PacketProcessor pprocessor;
            if (m_packetHandlers.TryGetValue(packet.Type, out pprocessor))
            {
                //there is a local handler for this packet type
                if (pprocessor.Async)
                {
                    object obj = new AsyncPacketProcess(this, pprocessor.method, packet);
                    Util.FireAndForget(ProcessSpecificPacketAsync, obj);
                    result = true;
                }
                else
                {
                    result = pprocessor.method(this, packet);
                }
            }
            else
            {
                //there is not a local handler so see if there is a Global handler
                PacketMethod method = null;
                bool found;
                lock (PacketHandlers)
                {
                    found = PacketHandlers.TryGetValue(packet.Type, out method);
                }
                if (found)
                {
                    result = method(this, packet);
                }
            }
            return result;
        }

        public void ProcessSpecificPacketAsync(object state)
        {
            AsyncPacketProcess packetObject = (AsyncPacketProcess) state;

            try
            {
                packetObject.result = packetObject.Method(packetObject.ClientView, packetObject.Pack);
            }
            catch (Exception e)
            {
                // Make sure that we see any exception caused by the asynchronous operation.
                MainConsole.Instance.ErrorFormat(
                    "[LLCLIENTVIEW]: Caught exception while processing {0} for {1}, {2} {3}",
                    packetObject.Pack, Name, e.Message, e.StackTrace);
            }
        }

        #endregion Packet Handling

        #region Scene/Avatar to Client

        public void SendRegionHandshake(RegionInfo regionInfo, RegionHandshakeArgs args)
        {
            RegionHandshakePacket handshake =
                (RegionHandshakePacket) PacketPool.Instance.GetPacket(PacketType.RegionHandshake);
            handshake.RegionInfo = new RegionHandshakePacket.RegionInfoBlock
                                       {
                                           BillableFactor = args.billableFactor,
                                           IsEstateManager = args.isEstateManager,
                                           TerrainHeightRange00 = args.terrainHeightRange0,
                                           TerrainHeightRange01 = args.terrainHeightRange1,
                                           TerrainHeightRange10 = args.terrainHeightRange2,
                                           TerrainHeightRange11 = args.terrainHeightRange3,
                                           TerrainStartHeight00 = args.terrainStartHeight0,
                                           TerrainStartHeight01 = args.terrainStartHeight1,
                                           TerrainStartHeight10 = args.terrainStartHeight2,
                                           TerrainStartHeight11 = args.terrainStartHeight3,
                                           SimAccess = args.simAccess,
                                           WaterHeight = args.waterHeight,
                                           RegionFlags = args.regionFlags,
                                           SimName = Util.StringToBytes256(args.regionName),
                                           SimOwner = args.SimOwner,
                                           TerrainBase0 = args.terrainBase0,
                                           TerrainBase1 = args.terrainBase1,
                                           TerrainBase2 = args.terrainBase2,
                                           TerrainBase3 = args.terrainBase3,
                                           TerrainDetail0 = args.terrainDetail0,
                                           TerrainDetail1 = args.terrainDetail1,
                                           TerrainDetail2 = args.terrainDetail2,
                                           TerrainDetail3 = args.terrainDetail3,
                                           CacheID = UUID.Random()
                                       };

            //I guess this is for the client to remember an old setting?
            handshake.RegionInfo2 = new RegionHandshakePacket.RegionInfo2Block {RegionID = regionInfo.RegionID};

            handshake.RegionInfo3 = new RegionHandshakePacket.RegionInfo3Block
                                        {
                                            CPUClassID = 9,
                                            CPURatio = 1,
                                            ColoName = Utils.EmptyBytes,
                                            ProductName = Util.StringToBytes256(regionInfo.RegionType),
                                            ProductSKU = Utils.EmptyBytes
                                        };


            OutPacket(handshake, ThrottleOutPacketType.Task);
        }

        ///<summary>
        ///</summary>
        public void MoveAgentIntoRegion(RegionInfo regInfo, Vector3 pos, Vector3 look)
        {
            AgentMovementCompletePacket mov =
                (AgentMovementCompletePacket) PacketPool.Instance.GetPacket(PacketType.AgentMovementComplete);
            mov.SimData.ChannelVersion = m_channelVersion;
            mov.AgentData.SessionID = m_sessionId;
            mov.AgentData.AgentID = AgentId;
            mov.Data.RegionHandle = regInfo.RegionHandle;
            mov.Data.Timestamp = (uint) Util.UnixTimeSinceEpoch();
            mov.Data.Position = pos;
            mov.Data.LookAt = look;

            // Hack to get this out immediately and skip the throttles
            OutPacket(mov, ThrottleOutPacketType.OutBand);
        }

        public void SendChatMessage(string message, byte type, Vector3 fromPos, string fromName,
                                    UUID fromAgentID, byte source, byte audible)
        {
            ChatFromSimulatorPacket reply =
                (ChatFromSimulatorPacket) PacketPool.Instance.GetPacket(PacketType.ChatFromSimulator);
            reply.ChatData.Audible = audible;
            reply.ChatData.Message = Util.StringToBytes1024(message);
            reply.ChatData.ChatType = type;
            reply.ChatData.SourceType = source;
            reply.ChatData.Position = fromPos;
            reply.ChatData.FromName = Util.StringToBytes256(fromName);
            reply.ChatData.OwnerID = fromAgentID;
            reply.ChatData.SourceID = fromAgentID;

            //Don't split me up!
            reply.HasVariableBlocks = false;
            // Hack to get this out immediately and skip throttles
            OutPacket(reply, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendTelehubInfo(Vector3 TelehubPos, Quaternion TelehubRot, List<Vector3> SpawnPoint, UUID ObjectID,
                                    string nameT)
        {
            TelehubInfoPacket packet = (TelehubInfoPacket) PacketPool.Instance.GetPacket(PacketType.TelehubInfo);
            packet.SpawnPointBlock = new TelehubInfoPacket.SpawnPointBlockBlock[SpawnPoint.Count];
            int i = 0;
            foreach (Vector3 pos in SpawnPoint)
            {
                packet.SpawnPointBlock[i] = new TelehubInfoPacket.SpawnPointBlockBlock {SpawnPointPos = pos};
                i++;
            }
            packet.TelehubBlock.ObjectID = ObjectID;
            packet.TelehubBlock.ObjectName = Utils.StringToBytes(nameT);
            packet.TelehubBlock.TelehubPos = TelehubPos;
            packet.TelehubBlock.TelehubRot = TelehubRot;
            OutPacket(packet, ThrottleOutPacketType.AvatarInfo);
        }

        /// <summary>
        ///   Send an instant message to this client
        /// </summary>
        //
        // Don't remove transaction ID! Groups and item gives need to set it!
        public void SendInstantMessage(GridInstantMessage im)
        {
            if (m_scene.Permissions.CanInstantMessage(im.fromAgentID, im.toAgentID))
            {
                ImprovedInstantMessagePacket msg
                    = (ImprovedInstantMessagePacket) PacketPool.Instance.GetPacket(PacketType.ImprovedInstantMessage);

                msg.AgentData.AgentID = im.fromAgentID;
                msg.AgentData.SessionID = UUID.Zero;
                msg.MessageBlock.FromAgentName = Util.StringToBytes256(im.fromAgentName);
                msg.MessageBlock.Dialog = im.dialog;
                msg.MessageBlock.FromGroup = im.fromGroup;
                if (im.imSessionID == UUID.Zero)
                    msg.MessageBlock.ID = im.fromAgentID ^ im.toAgentID;
                else
                    msg.MessageBlock.ID = im.imSessionID;
                msg.MessageBlock.Offline = im.offline;
                msg.MessageBlock.ParentEstateID = im.ParentEstateID;
                msg.MessageBlock.Position = im.Position;
                msg.MessageBlock.RegionID = im.RegionID;
                msg.MessageBlock.Timestamp = im.timestamp;
                msg.MessageBlock.ToAgentID = im.toAgentID;
                msg.MessageBlock.Message = Util.StringToBytes1024(im.message);
                msg.MessageBlock.BinaryBucket = im.binaryBucket;

                OutPacket(msg, ThrottleOutPacketType.AvatarInfo);
            }
        }

        public void SendGenericMessage(string method, List<string> message)
        {
            List<byte[]> convertedmessage = message.ConvertAll<byte[]>(delegate(string item)
            {
                return Util.StringToBytes256(item);
            });
            SendGenericMessage(method, convertedmessage);
        }

        public void SendGenericMessage(string method, List<byte[]> message)
        {
            GenericMessagePacket gmp = new GenericMessagePacket
                                           {
                                               MethodData = {Method = Util.StringToBytes256(method)},
                                               ParamList = new GenericMessagePacket.ParamListBlock[message.Count]
                                           };
            int i = 0;
            foreach (byte[] val in message)
            {
                gmp.ParamList[i] = new GenericMessagePacket.ParamListBlock();
                gmp.ParamList[i++].Parameter = val;
            }

            OutPacket(gmp, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendGroupActiveProposals(UUID groupID, UUID transactionID, GroupActiveProposals[] Proposals)
        {
            GroupActiveProposalItemReplyPacket GAPIRP = new GroupActiveProposalItemReplyPacket
                                                            {
                                                                AgentData = {AgentID = AgentId, GroupID = groupID},
                                                                TransactionData =
                                                                    {
                                                                        TransactionID = transactionID,
                                                                        TotalNumItems = (uint) Proposals.Length
                                                                    },
                                                                ProposalData =
                                                                    new GroupActiveProposalItemReplyPacket.
                                                                    ProposalDataBlock[Proposals.Length]
                                                            };


            int i = 0;
#if (!ISWIN)
            foreach (GroupActiveProposals proposal in Proposals)
            {
                GroupActiveProposalItemReplyPacket.ProposalDataBlock ProposalData = new GroupActiveProposalItemReplyPacket
                    .ProposalDataBlock
                                                                                        {
                                                                                            VoteCast =
                                                                                                Utils.StringToBytes(
                                                                                                    proposal.VoteCast),
                                                                                            VoteID =
                                                                                                new UUID(proposal.VoteID),
                                                                                            VoteInitiator =
                                                                                                new UUID(
                                                                                                proposal.VoteInitiator),
                                                                                            Majority =
                                                                                                (float)Convert.ToDouble(
                                                                                                    proposal.Majority),
                                                                                            Quorum =
                                                                                                Convert.ToInt32(
                                                                                                    proposal.Quorum),
                                                                                            TerseDateID =
                                                                                                Utils.StringToBytes(
                                                                                                    proposal.TerseDateID),
                                                                                            StartDateTime =
                                                                                                Utils.StringToBytes(
                                                                                                    proposal.
                                                                                                        StartDateTime),
                                                                                            EndDateTime =
                                                                                                Utils.StringToBytes(
                                                                                                    proposal.EndDateTime),
                                                                                            ProposalText =
                                                                                                Utils.StringToBytes(
                                                                                                    proposal.
                                                                                                        ProposalText),
                                                                                            AlreadyVoted = proposal.VoteAlreadyCast
                                                                                        };
                GAPIRP.ProposalData[i] = ProposalData;
                i++;
            }
#else
            foreach (GroupActiveProposalItemReplyPacket.ProposalDataBlock ProposalData in Proposals.Select(Proposal => new GroupActiveProposalItemReplyPacket.ProposalDataBlock
                                                                                        {
                                                                                            VoteCast = Utils.StringToBytes("false"),
                                                                                            VoteID = new UUID(Proposal.VoteID),
                                                                                            VoteInitiator = new UUID(Proposal.VoteInitiator),
                                                                                            Majority = Convert.ToInt32(Proposal.Majority),
                                                                                            Quorum = Convert.ToInt32(Proposal.Quorum),
                                                                                            TerseDateID = Utils.StringToBytes(Proposal.TerseDateID),
                                                                                            StartDateTime = Utils.StringToBytes(Proposal.StartDateTime),
                                                                                            EndDateTime = Utils.StringToBytes(Proposal.EndDateTime),
                                                                                            ProposalText = Utils.StringToBytes(Proposal.ProposalText),
                                                                                            AlreadyVoted = false
                                                                                        }))
            {
                GAPIRP.ProposalData[i] = ProposalData;
                i++;
            }
#endif
            OutPacket(GAPIRP, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendGroupVoteHistory(UUID groupID, UUID transactionID, GroupVoteHistory Vote,
                                         GroupVoteHistoryItem[] VoteItems)
        {
            GroupVoteHistoryItemReplyPacket GVHIRP = new GroupVoteHistoryItemReplyPacket
                                                         {
                                                             AgentData = {AgentID = AgentId, GroupID = groupID},
                                                             TransactionData =
                                                                 {
                                                                     TransactionID = transactionID,
                                                                     TotalNumItems = (uint) VoteItems.Length
                                                                 },
                                                             HistoryItemData =
                                                             {
                                                                     VoteID = new UUID(Vote.VoteID),
                                                                     VoteInitiator = new UUID(Vote.VoteInitiator),
                                                                     Majority = Convert.ToInt32(Vote.Majority),
                                                                     Quorum = Convert.ToInt32(Vote.Quorum),
                                                                     TerseDateID = Utils.StringToBytes(Vote.TerseDateID),
                                                                     StartDateTime =
                                                                         Utils.StringToBytes(Vote.StartDateTime),
                                                                     EndDateTime = Utils.StringToBytes(Vote.EndDateTime),
                                                                     VoteType = Utils.StringToBytes(Vote.VoteType),
                                                                     VoteResult = Utils.StringToBytes(Vote.VoteResult),
                                                                     ProposalText =
                                                                         Utils.StringToBytes(Vote.ProposalText)
                                                                 }
                                                         };


            int i = 0;
            GVHIRP.VoteItem = new GroupVoteHistoryItemReplyPacket.VoteItemBlock[VoteItems.Length];
#if (!ISWIN)
            foreach (GroupVoteHistoryItem item in VoteItems)
            {
                GroupVoteHistoryItemReplyPacket.VoteItemBlock VoteItem = new GroupVoteHistoryItemReplyPacket.
                    VoteItemBlock
                                                                             {
                                                                                 CandidateID = item.CandidateID,
                                                                                 NumVotes = item.NumVotes,
                                                                                 VoteCast =
                                                                                     Utils.StringToBytes(item.VoteCast)
                                                                             };
                GVHIRP.VoteItem[i] = VoteItem;
                i++;
            }
#else
            foreach (GroupVoteHistoryItemReplyPacket.VoteItemBlock VoteItem in VoteItems.Select(item => new GroupVoteHistoryItemReplyPacket.VoteItemBlock
            {
                CandidateID = item.CandidateID,
                NumVotes = item.NumVotes,
                VoteCast = Utils.StringToBytes(item.VoteCast)
            }))
            {
                GVHIRP.VoteItem[i] = VoteItem;
                i++;
            }
#endif

            OutPacket(GVHIRP, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendGroupAccountingDetails(IClientAPI sender, UUID groupID, UUID transactionID, UUID sessionID,
                                               int amt, int currentInterval, int interval, string startDate, GroupAccountHistory[] history)
        {
            GroupAccountDetailsReplyPacket GADRP = new GroupAccountDetailsReplyPacket
                                                       {
                                                           AgentData = new GroupAccountDetailsReplyPacket.AgentDataBlock
                                                                           {AgentID = sender.AgentId, GroupID = groupID},
                                                           HistoryData =
                                                               new GroupAccountDetailsReplyPacket.HistoryDataBlock[history.Length]
                                                       };
            int i = 0;
            foreach (GroupAccountHistory h in history)
            {
                GroupAccountDetailsReplyPacket.HistoryDataBlock History =
                    new GroupAccountDetailsReplyPacket.HistoryDataBlock();

                History.Amount = h.Amount;
                History.Description = Utils.StringToBytes(h.Description);

                GADRP.HistoryData[i++] = History;
            }
            GADRP.MoneyData = new GroupAccountDetailsReplyPacket.MoneyDataBlock
                                  {
                                      CurrentInterval = currentInterval,
                                      IntervalDays = interval,
                                      RequestID = transactionID,
                                      StartDate = Utils.StringToBytes(startDate)
                                  };
            OutPacket(GADRP, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendGroupAccountingSummary(IClientAPI sender, UUID groupID, UUID requestID, int moneyAmt, int totalTierDebit,
                                               int totalTierCredits, string startDate, int currentInterval, int intervalLength,
                                               string taxDate, string lastTaxDate, int parcelDirectoryFee, int landTaxFee, int groupTaxFee, int objectTaxFee)
        {
            GroupAccountSummaryReplyPacket GASRP =
                (GroupAccountSummaryReplyPacket) PacketPool.Instance.GetPacket(
                    PacketType.GroupAccountSummaryReply);

            GASRP.AgentData = new GroupAccountSummaryReplyPacket.AgentDataBlock
                                  {AgentID = sender.AgentId, GroupID = groupID};
            GASRP.MoneyData = new GroupAccountSummaryReplyPacket.MoneyDataBlock
                                  {
                                      Balance = moneyAmt,
                                      TotalCredits = totalTierCredits,
                                      TotalDebits = totalTierDebit,
                                      StartDate = Utils.StringToBytes(startDate + '\n'),
                                      CurrentInterval = currentInterval,
                                      GroupTaxCurrent = groupTaxFee,
                                      GroupTaxEstimate = groupTaxFee,
                                      IntervalDays = intervalLength,
                                      LandTaxCurrent = landTaxFee,
                                      LandTaxEstimate = landTaxFee,
                                      LastTaxDate = Utils.StringToBytes(lastTaxDate),
                                      LightTaxCurrent = 0,
                                      TaxDate = Utils.StringToBytes(taxDate),
                                      RequestID = requestID,
                                      ParcelDirFeeEstimate = parcelDirectoryFee,
                                      ParcelDirFeeCurrent = parcelDirectoryFee,
                                      ObjectTaxEstimate = objectTaxFee,
                                      NonExemptMembers = 0,
                                      ObjectTaxCurrent = objectTaxFee,
                                      LightTaxEstimate = 0
                                  };
            OutPacket(GASRP, ThrottleOutPacketType.Asset);
        }

        public void SendGroupTransactionsSummaryDetails(IClientAPI sender, UUID groupID, UUID transactionID,
                                                        UUID sessionID, int currentInterval, int intervalDays, string startingDate, GroupAccountHistory[] history)
        {
            GroupAccountTransactionsReplyPacket GATRP =
                (GroupAccountTransactionsReplyPacket) PacketPool.Instance.GetPacket(
                    PacketType.GroupAccountTransactionsReply);

            GATRP.AgentData = new GroupAccountTransactionsReplyPacket.AgentDataBlock
                                  {AgentID = sender.AgentId, GroupID = groupID};
            GATRP.MoneyData = new GroupAccountTransactionsReplyPacket.MoneyDataBlock
                                  {
                                      CurrentInterval = currentInterval,
                                      IntervalDays = intervalDays,
                                      RequestID = transactionID,
                                      StartDate = Utils.StringToBytes(startingDate)
                                  };
            GATRP.HistoryData = new GroupAccountTransactionsReplyPacket.HistoryDataBlock[history.Length];
            int i = 0;
            foreach (GroupAccountHistory h in history)
            {
                GroupAccountTransactionsReplyPacket.HistoryDataBlock History =
                    new GroupAccountTransactionsReplyPacket.HistoryDataBlock
                        {
                            Amount = h.Amount,
                            Item =  Utils.StringToBytes(h.Description),
                            Time = Utils.StringToBytes(h.TimeString),
                            Type = 0,
                            User = Utils.StringToBytes(h.UserCausingCharge)
                        };
                GATRP.HistoryData[i++] = History;
            }
            OutPacket(GATRP, ThrottleOutPacketType.Asset);
        }

        /// <summary>
        ///   Send the region heightmap to the client
        /// </summary>
        /// <param name = "map">heightmap</param>
        public void SendLayerData(short[] map)
        {
            DoSendLayerData(map);
            Util.FireAndForget(DoSendLayerData, map);
        }

        /// <summary>
        ///   Send terrain layer information to the client.
        /// </summary>
        /// <param name = "o"></param>
        private void DoSendLayerData(object o)
        {
            short[] map = (short[]) o;
            try
            {
                for (int y = 0; y < m_scene.RegionInfo.RegionSizeY/Constants.TerrainPatchSize; y++)
                {
                    for (int x = 0; x < m_scene.RegionInfo.RegionSizeX/Constants.TerrainPatchSize; x += 4)
                    {
                        SendLayerPacket(map, y, x);
                        //Thread.Sleep(35);
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.Warn("[CLIENT]: ClientView.API.cs: SendLayerData() - Failed with exception " + e);
            }
        }

        /// <summary>
        ///   Sends a set of four patches (x, x+1, ..., x+3) to the client
        /// </summary>
        /// <param name = "map">heightmap</param>
        /// <param name = "x">X coordinate for patches 0..12</param>
        /// <param name = "y">Y coordinate for patches 0..15</param>
        public void SendLayerPacket(short[] map, int y, int x)
        {
            int[] xs = new[] {x + 0, x + 1, x + 2, x + 3};
            int[] ys = new[] {y, y, y, y};

            try
            {
                byte type = (byte) TerrainPatch.LayerType.Land;
                if (m_scene.RegionInfo.RegionSizeX > Constants.RegionSize ||
                    m_scene.RegionInfo.RegionSizeY > Constants.RegionSize)
                {
                    type++;
                }
                LayerDataPacket layerpack = AuroraTerrainCompressor.CreateLandPacket(map, xs, ys, type, m_scene.RegionInfo.RegionSizeX,
                                                                                     m_scene.RegionInfo.RegionSizeY);
                layerpack.Header.Zerocoded = true;
                layerpack.Header.Reliable = true;

                if (layerpack.Length > 1000) // Oversize packet was created
                {
                    for (int xa = 0; xa < 4; xa++)
                    {
                        // Send oversize packet in individual patches
                        //
                        SendLayerData(x + xa, y, map);
                    }
                }
                else
                {
                    OutPacket(layerpack, ThrottleOutPacketType.Land);
                }
            }
            catch (OverflowException)
            {
                for (int xa = 0; xa < 4; xa++)
                {
                    // Send oversize packet in individual patches
                    //
                    SendLayerData(x + xa, y, map);
                }
            }
            catch (IndexOutOfRangeException)
            {
                for (int xa = 0; xa < 4; xa++)
                {
                    // Bad terrain, send individual chunks
                    //
                    SendLayerData(x + xa, y, map);
                }
            }
        }

        /// <summary>
        ///   Sends a specified patch to a client
        /// </summary>
        /// <param name = "px">Patch coordinate (x) 0..regionSize/16</param>
        /// <param name = "py">Patch coordinate (y) 0..regionSize/16</param>
        /// <param name = "map">heightmap</param>
        public void SendLayerData(int px, int py, short[] map)
        {
            try
            {
                int[] x = new[] {px};
                int[] y = new[] {py};

                byte type = (byte) TerrainPatch.LayerType.Land;
                if (m_scene.RegionInfo.RegionSizeX > Constants.RegionSize ||
                    m_scene.RegionInfo.RegionSizeY > Constants.RegionSize)
                {
                    type++;
                }
                LayerDataPacket layerpack = AuroraTerrainCompressor.CreateLandPacket(map, x, y, type,
                                                                                     m_scene.RegionInfo.RegionSizeX,
                                                                                     m_scene.RegionInfo.RegionSizeY);

                OutPacket(layerpack, ThrottleOutPacketType.Land);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[CLIENT]: SendLayerData() Failed with exception: " + e.Message, e);
            }
        }

        /// <summary>
        ///   Sends a specified patch to a client
        /// </summary>
        /// <param name = "x">Patch coordinates (x) 0..regionSize/16</param>
        /// <param name = "y">Patch coordinates (y) 0..regionSize/16</param>
        /// <param name = "map">heightmap</param>
        /// <param name="layertype"></param>
        public void SendLayerData(int[] x, int[] y, short[] map, TerrainPatch.LayerType layertype)
        {
            const int MaxPatches = 10;
            byte type = (byte) layertype;
            if (m_scene.RegionInfo.RegionSizeX > Constants.RegionSize ||
                m_scene.RegionInfo.RegionSizeY > Constants.RegionSize)
            {
                if (layertype == TerrainPatch.LayerType.Land || layertype == TerrainPatch.LayerType.Water)
                    type++;
                else
                    type += 2;
            }
            //Only send 10 at a time
            for (int i = 0; i < x.Length; i += MaxPatches)
            {
                int Size = (x.Length - i) - 10 > 0 ? 10 : (x.Length - i);
                try
                {
                    //Find the size for the array
                    int[] xTemp = new int[Size];
                    int[] yTemp = new int[Size];

                    //Copy the arrays
                    Array.Copy(x, i, xTemp, 0, Size);
                    Array.Copy(y, i, yTemp, 0, Size);

                    //Build the packet
                    LayerDataPacket layerpack = AuroraTerrainCompressor.CreateLandPacket(map, xTemp, yTemp, type,
                                                                                         m_scene.RegionInfo.RegionSizeX,
                                                                                         m_scene.RegionInfo.RegionSizeY);

                    layerpack.Header.Zerocoded = true;
                    layerpack.Header.Reliable = true;

                    if (layerpack.Length > 1000) // Oversize packet was created
                    {
                        for (int xa = 0; xa < Size; xa++)
                        {
                            // Send oversize packet in individual patches
                            //
                            SendLayerData(x[i + xa], y[i + xa], map);
                        }
                    }
                    else
                    {
                        OutPacket(layerpack, ThrottleOutPacketType.Land);
                    }
                }
                catch (OverflowException)
                {
                    for (int xa = 0; xa < Size; xa++)
                    {
                        // Send oversize packet in individual patches
                        //
                        SendLayerData(x[i + xa], y[i + xa], map);
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    for (int xa = 0; xa < Size; xa++)
                    {
                        // Bad terrain, send individual chunks
                        //
                        SendLayerData(x[i + xa], y[i + xa], map);
                    }
                }
            }
        }

        /// <summary>
        ///   Send the wind matrix to the client
        /// </summary>
        /// <param name = "windSpeeds">16x16 array of wind speeds</param>
        public void SendWindData(Vector2[] windSpeeds)
        {
            Util.FireAndForget(DoSendWindData, windSpeeds);
        }

        /// <summary>
        ///   Send the cloud matrix to the client
        /// </summary>
        /// <param name = "cloudDensity">16x16 array of cloud densities</param>
        public void SendCloudData(float[] cloudDensity)
        {
            Util.FireAndForget(DoSendCloudData, cloudDensity);
        }

        /// <summary>
        ///   Send wind layer information to the client.
        /// </summary>
        /// <param name = "o"></param>
        private void DoSendWindData(object o)
        {
            Vector2[] windSpeeds = (Vector2[]) o;
            TerrainPatch[] patches = new TerrainPatch[2];
            patches[0] = new TerrainPatch {Data = new float[16*16]};
            patches[1] = new TerrainPatch {Data = new float[16*16]};


//            for (int y = 0; y < 16*16; y+=16)
//            {
            for (int x = 0; x < 16*16; x++)
            {
                patches[0].Data[x] = windSpeeds[x].X;
                patches[1].Data[x] = windSpeeds[x].Y;
            }
//            }
            byte type = (byte) TerrainPatch.LayerType.Wind;
            if (m_scene.RegionInfo.RegionSizeX > Constants.RegionSize ||
                m_scene.RegionInfo.RegionSizeY > Constants.RegionSize)
            {
                type += 2;
            }
            LayerDataPacket layerpack = AuroraTerrainCompressor.CreateLayerDataPacket(patches, type,
                                                                                      m_scene.RegionInfo.RegionSizeX,
                                                                                      m_scene.RegionInfo.RegionSizeY);
            layerpack.Header.Zerocoded = true;
            OutPacket(layerpack, ThrottleOutPacketType.Wind);
        }

        /// <summary>
        ///   Send cloud layer information to the client.
        /// </summary>
        /// <param name = "o"></param>
        private void DoSendCloudData(object o)
        {
            float[] cloudCover = (float[]) o;
            TerrainPatch[] patches = new TerrainPatch[1];
            patches[0] = new TerrainPatch {Data = new float[16*16]};

//            for (int y = 0; y < 16*16; y+=16)
            {
                for (int x = 0; x < 16*16; x++)
                {
                    patches[0].Data[x] = cloudCover[x];
                }
            }

            byte type = (byte) TerrainPatch.LayerType.Cloud;
            if (m_scene.RegionInfo.RegionSizeX > Constants.RegionSize ||
                m_scene.RegionInfo.RegionSizeY > Constants.RegionSize)
            {
                type += 2;
            }
            LayerDataPacket layerpack = AuroraTerrainCompressor.CreateLayerDataPacket(patches, type,
                                                                                      m_scene.RegionInfo.RegionSizeX,
                                                                                      m_scene.RegionInfo.RegionSizeY);
            layerpack.Header.Zerocoded = true;
            OutPacket(layerpack, ThrottleOutPacketType.Cloud);
        }

        public AgentCircuitData RequestClientInfo()
        {
            AgentCircuitData agentData = new AgentCircuitData
                                             {
                                                 AgentID = AgentId,
                                                 SessionID = m_sessionId,
                                                 SecureSessionID = SecureSessionId,
                                                 circuitcode = m_circuitCode,
                                                 child = false
                                             };

            AgentCircuitData currentAgentCircuit = this.m_udpServer.m_circuitManager.GetAgentCircuitData(CircuitCode);
            if (currentAgentCircuit != null)
            {
                agentData.IPAddress = currentAgentCircuit.IPAddress;
                agentData.ServiceURLs = currentAgentCircuit.ServiceURLs;
            }

            return agentData;
        }

        internal void SendMapBlockSplit(List<MapBlockData> mapBlocks, uint flag)
        {
            MapBlockReplyPacket mapReply = (MapBlockReplyPacket) PacketPool.Instance.GetPacket(PacketType.MapBlockReply);
            // TODO: don't create new blocks if recycling an old packet

            MapBlockData[] mapBlocks2 = mapBlocks.ToArray();

            mapReply.AgentData.AgentID = AgentId;
            mapReply.Data = new MapBlockReplyPacket.DataBlock[mapBlocks2.Length];
            mapReply.Size = new MapBlockReplyPacket.SizeBlock[mapBlocks2.Length];
            mapReply.AgentData.Flags = flag;

            for (int i = 0; i < mapBlocks2.Length; i++)
            {
                mapReply.Data[i] = new MapBlockReplyPacket.DataBlock
                                       {MapImageID = mapBlocks2[i].MapImageID, X = mapBlocks2[i].X, Y = mapBlocks2[i].Y};
                mapReply.Size[i] = new MapBlockReplyPacket.SizeBlock
                                       {SizeX = mapBlocks2[i].SizeX, SizeY = mapBlocks2[i].SizeY};
                mapReply.Data[i].WaterHeight = mapBlocks2[i].WaterHeight;
                mapReply.Data[i].Name = Utils.StringToBytes(mapBlocks2[i].Name);
                mapReply.Data[i].RegionFlags = mapBlocks2[i].RegionFlags;
                mapReply.Data[i].Access = mapBlocks2[i].Access;
                mapReply.Data[i].Agents = mapBlocks2[i].Agents;
            }
            OutPacket(mapReply, ThrottleOutPacketType.Land);
        }

        public void SendMapBlock(List<MapBlockData> mapBlocks, uint flag)
        {
            MapBlockData[] mapBlocks2 = mapBlocks.ToArray();

            const int maxsend = 10;

            //int packets = Math.Ceiling(mapBlocks2.Length / maxsend);

            List<MapBlockData> sendingBlocks = new List<MapBlockData>();

            for (int i = 0; i < mapBlocks2.Length; i++)
            {
                sendingBlocks.Add(mapBlocks2[i]);
                if (((i + 1) == mapBlocks2.Length) || (((i + 1)%maxsend) == 0))
                {
                    SendMapBlockSplit(sendingBlocks, flag);
                    sendingBlocks = new List<MapBlockData>();
                }
            }
        }

        public void SendLocalTeleport(Vector3 position, Vector3 lookAt, uint flags)
        {
            TeleportLocalPacket tpLocal = (TeleportLocalPacket) PacketPool.Instance.GetPacket(PacketType.TeleportLocal);
            tpLocal.Info.AgentID = AgentId;
            tpLocal.Info.TeleportFlags = flags;
            tpLocal.Info.LocationID = 2;
            tpLocal.Info.LookAt = lookAt;
            tpLocal.Info.Position = position;

            // Hack to get this out immediately and skip throttles
            OutPacket(tpLocal, ThrottleOutPacketType.OutBand);
        }

        public void SendRegionTeleport(ulong regionHandle, byte simAccess, IPEndPoint newRegionEndPoint,
                                       uint locationID,
                                       uint flags, string capsURL)
        {
            //TeleportFinishPacket teleport = (TeleportFinishPacket)PacketPool.Instance.GetPacket(PacketType.TeleportFinish);

            TeleportFinishPacket teleport = new TeleportFinishPacket
                                                {
                                                    Info =
                                                        {
                                                            AgentID = AgentId,
                                                            RegionHandle = regionHandle,
                                                            SimAccess = simAccess,
                                                            SeedCapability = Util.StringToBytes256(capsURL)
                                                        }
                                                };


            IPAddress oIP = newRegionEndPoint.Address;
            byte[] byteIP = oIP.GetAddressBytes();
            uint ip = (uint) byteIP[3] << 24;
            ip += (uint) byteIP[2] << 16;
            ip += (uint) byteIP[1] << 8;
            ip += byteIP[0];

            teleport.Info.SimIP = ip;
            teleport.Info.SimPort = (ushort) newRegionEndPoint.Port;
            teleport.Info.LocationID = 4;
            teleport.Info.TeleportFlags = 1 << 4;

            // Hack to get this out immediately and skip throttles.
            OutPacket(teleport, ThrottleOutPacketType.OutBand);
        }

        /// <summary>
        ///   Inform the client that a teleport attempt has failed
        /// </summary>
        public void SendTeleportFailed(string reason)
        {
            TeleportFailedPacket tpFailed =
                (TeleportFailedPacket) PacketPool.Instance.GetPacket(PacketType.TeleportFailed);
            tpFailed.Info.AgentID = AgentId;
            tpFailed.Info.Reason = Util.StringToBytes256(reason);
            tpFailed.AlertInfo = new TeleportFailedPacket.AlertInfoBlock[0];

            // Hack to get this out immediately and skip throttles
            OutPacket(tpFailed, ThrottleOutPacketType.OutBand);
        }

        ///<summary>
        ///</summary>
        public void SendTeleportStart(uint flags)
        {
            TeleportStartPacket tpStart = (TeleportStartPacket) PacketPool.Instance.GetPacket(PacketType.TeleportStart);
            //TeleportStartPacket tpStart = new TeleportStartPacket();
            tpStart.Info.TeleportFlags = flags; //16; // Teleport via location

            // Hack to get this out immediately and skip throttles
            OutPacket(tpStart, ThrottleOutPacketType.OutBand);
        }

        public void SendTeleportProgress(uint flags, string message)
        {
            TeleportProgressPacket tpProgress =
                (TeleportProgressPacket) PacketPool.Instance.GetPacket(PacketType.TeleportProgress);
            tpProgress.AgentData.AgentID = AgentId;
            tpProgress.Info.TeleportFlags = flags;
            tpProgress.Info.Message = Util.StringToBytes256(message);

            // Hack to get this out immediately and skip throttles
            OutPacket(tpProgress, ThrottleOutPacketType.OutBand);
        }

        public void SendMoneyBalance(UUID transaction, bool success, byte[] description, int balance)
        {
            MoneyBalanceReplyPacket money =
                (MoneyBalanceReplyPacket) PacketPool.Instance.GetPacket(PacketType.MoneyBalanceReply);
            money.MoneyData.AgentID = AgentId;
            money.MoneyData.TransactionID = transaction;
            money.MoneyData.TransactionSuccess = success;
            money.MoneyData.Description = description;
            money.MoneyData.MoneyBalance = balance;
            money.TransactionInfo.ItemDescription = Util.StringToBytes256("NONE");
            OutPacket(money, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendPayPrice(UUID objectID, int[] payPrice)
        {
            if (payPrice[0] == 0 &&
                payPrice[1] == 0 &&
                payPrice[2] == 0 &&
                payPrice[3] == 0 &&
                payPrice[4] == 0)
                return;

            PayPriceReplyPacket payPriceReply =
                (PayPriceReplyPacket) PacketPool.Instance.GetPacket(PacketType.PayPriceReply);
            payPriceReply.ObjectData.ObjectID = objectID;
            payPriceReply.ObjectData.DefaultPayPrice = payPrice[0];

            payPriceReply.ButtonData = new PayPriceReplyPacket.ButtonDataBlock[4];
            payPriceReply.ButtonData[0] = new PayPriceReplyPacket.ButtonDataBlock {PayButton = payPrice[1]};
            payPriceReply.ButtonData[1] = new PayPriceReplyPacket.ButtonDataBlock {PayButton = payPrice[2]};
            payPriceReply.ButtonData[2] = new PayPriceReplyPacket.ButtonDataBlock {PayButton = payPrice[3]};
            payPriceReply.ButtonData[3] = new PayPriceReplyPacket.ButtonDataBlock {PayButton = payPrice[4]};

            OutPacket(payPriceReply, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendPlacesQuery(ExtendedLandData[] LandData, UUID queryID, UUID transactionID)
        {
            PlacesReplyPacket PlacesReply = new PlacesReplyPacket();
            PlacesReplyPacket.QueryDataBlock[] Query = new PlacesReplyPacket.QueryDataBlock[LandData.Length];
            //Note: Nothing is ever done with this?????
            int totalarea = 0;
            List<string> RegionTypes = new List<string>();
            for (int i = 0; i < LandData.Length; i++)
            {
                PlacesReplyPacket.QueryDataBlock QueryBlock = new PlacesReplyPacket.QueryDataBlock
                                                                  {
                                                                      ActualArea = LandData[i].LandData.Area,
                                                                      BillableArea = LandData[i].LandData.Area,
                                                                      Desc =
                                                                          Utils.StringToBytes(
                                                                              LandData[i].LandData.Description),
                                                                      Dwell = LandData[i].LandData.Dwell,
                                                                      Flags = 0,
                                                                      GlobalX = LandData[i].GlobalPosX,
                                                                      GlobalY = LandData[i].GlobalPosY,
                                                                      GlobalZ = 0,
                                                                      Name =
                                                                          Utils.StringToBytes(LandData[i].LandData.Name),
                                                                      OwnerID = LandData[i].LandData.OwnerID,
                                                                      Price = LandData[i].LandData.SalePrice,
                                                                      SimName =
                                                                          Utils.StringToBytes(LandData[i].RegionName),
                                                                      SnapshotID = LandData[i].LandData.SnapshotID
                                                                  };
                Query[i] = QueryBlock;
                totalarea += LandData[i].LandData.Area;
                RegionTypes.Add(LandData[i].RegionType);
            }
            PlacesReply.QueryData = Query;
            PlacesReply.AgentData = new PlacesReplyPacket.AgentDataBlock {AgentID = AgentId, QueryID = queryID};
            PlacesReply.TransactionData.TransactionID = transactionID;
            try
            {
                OutPacket(PlacesReply, ThrottleOutPacketType.AvatarInfo);
                //Disabled for now... it doesn't seem to work right...
                /*IEventQueueService eq = Scene.RequestModuleInterface<IEventQueueService>();
                if (eq != null)
                {
                    eq.QueryReply(PlacesReply, AgentId, RegionTypes.ToArray(), Scene.RegionInfo.RegionHandle);
                }*/
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Error("Unable to send group membership data via eventqueue - exception: " + ex);
                MainConsole.Instance.Warn("sending places query data via UDP");
                OutPacket(PlacesReply, ThrottleOutPacketType.AvatarInfo);
            }
        }

        public void SendStartPingCheck(byte seq)
        {
            StartPingCheckPacket pc = (StartPingCheckPacket) PacketPool.Instance.GetPacket(PacketType.StartPingCheck);
            pc.Header.Reliable = false;

            pc.PingID.PingID = seq;
            // We *could* get OldestUnacked, but it would hurt performance and not provide any benefit
            pc.PingID.OldestUnacked = 0;

            OutPacket(pc, ThrottleOutPacketType.OutBand);
        }

        public void SendKillObject(ulong regionHandle, IEntity[] entities)
        {
            if (entities.Length == 0)
                return; //........... why!

//            MainConsole.Instance.DebugFormat("[CLIENT]: Sending KillObjectPacket to {0} for {1} in {2}", Name, localID, regionHandle);

            KillObjectPacket kill = (KillObjectPacket) PacketPool.Instance.GetPacket(PacketType.KillObject);
            kill.ObjectData = new KillObjectPacket.ObjectDataBlock[entities.Length];
            int i = 0;
            bool brokenUpdate = false;

            foreach (IEntity entity in entities)
            {
                if (entity == null)
                {
                    brokenUpdate = true;
                    continue;
                }

                /*if ((entity is SceneObjectPart &&
                    ((SceneObjectPart)entity).IsAttachment) ||
                    (entity is SceneObjectGroup &&
                    ((SceneObjectGroup)entity).RootPart.IsAttachment))
                {
                    // Do nothing
                }
                else if(entity is SceneObjectPart)
                    m_killRecord.Add(entity.LocalId);*/
                KillObjectPacket.ObjectDataBlock block = new KillObjectPacket.ObjectDataBlock {ID = entity.LocalId};
                kill.ObjectData[i] = block;
                i++;
            }
            //If the # of entities is not correct, we have to rebuild the entire packet
            if (brokenUpdate)
            {
#if (!ISWIN)
                int count = 0;
                foreach (KillObjectPacket.ObjectDataBlock block in kill.ObjectData)
                {
                    if (block != null) count++;
                }
#else
                int count = kill.ObjectData.Count(block => block != null);
#endif
                i = 0;
                KillObjectPacket.ObjectDataBlock[] bk = new KillObjectPacket.ObjectDataBlock[count];
#if (!ISWIN)
                foreach (KillObjectPacket.ObjectDataBlock block in kill.ObjectData)
                {
                    if (block != null)
                    {
                        bk[i] = block;
                        i++;
                    }
                }
#else
                foreach (KillObjectPacket.ObjectDataBlock block in kill.ObjectData.Where(block => block != null))
                {
                    bk[i] = block;
                    i++;
                }
#endif
                kill.ObjectData = bk;
            }

            kill.Header.Reliable = true;
            kill.Header.Zerocoded = true;

            OutPacket(kill, ThrottleOutPacketType.Task);
        }

        public void SendKillObject(ulong regionHandle, uint[] entities)
        {
            if (entities.Length == 0)
                return; //........... why!

            //            MainConsole.Instance.DebugFormat("[CLIENT]: Sending KillObjectPacket to {0} for {1} in {2}", Name, localID, regionHandle);

            KillObjectPacket kill = (KillObjectPacket) PacketPool.Instance.GetPacket(PacketType.KillObject);
            kill.ObjectData = new KillObjectPacket.ObjectDataBlock[entities.Length];
            int i = 0;
#if (!ISWIN)
            foreach (uint entity in entities)
            {
                KillObjectPacket.ObjectDataBlock block = new KillObjectPacket.ObjectDataBlock {ID = entity};
                kill.ObjectData[i] = block;
                i++;
            }
#else
            foreach (KillObjectPacket.ObjectDataBlock block in entities.Select(entity => new KillObjectPacket.ObjectDataBlock {ID = entity}))
            {
                kill.ObjectData[i] = block;
                i++;
            }
#endif
            kill.Header.Reliable = true;
            kill.Header.Zerocoded = true;

            OutPacket(kill, ThrottleOutPacketType.Task);
        }

        ///<summary>
        ///  Send information about the items contained in a folder to the client.
        ///
        ///  XXX This method needs some refactoring loving
        ///</summary>
        ///<param name = "ownerID">The owner of the folder</param>
        ///<param name = "folderID">The id of the folder</param>
        ///<param name = "items">The items contained in the folder identified by folderID</param>
        ///<param name = "folders"></param>
        ///<param name="version"></param>
        ///<param name = "fetchFolders">Do we need to send folder information?</param>
        ///<param name = "fetchItems">Do we need to send item information?</param>
        public void SendInventoryFolderDetails(UUID ownerID, UUID folderID, List<InventoryItemBase> items,
                                               List<InventoryFolderBase> folders, int version,
                                               bool fetchFolders, bool fetchItems)
        {
            // An inventory descendents packet consists of a single agent section and an inventory details
            // section for each inventory item.  The size of each inventory item is approximately 550 bytes.
            // In theory, UDP has a maximum packet size of 64k, so it should be possible to send descendent
            // packets containing metadata for in excess of 100 items.  But in practice, there may be other
            // factors (e.g. firewalls) restraining the maximum UDP packet size.  See,
            //
            // http://opensimulator.org/mantis/view.php?id=226
            //
            // for one example of this kind of thing.  In fact, the Linden servers appear to only send about
            // 6 to 7 items at a time, so let's stick with 6
            const int MAX_ITEMS_PER_PACKET = 5;
            const int MAX_FOLDERS_PER_PACKET = 6;

            if (items == null || folders == null)
                return; //This DOES happen when things time out!!

            int totalItems = fetchItems ? items.Count : 0;
            int totalFolders = fetchFolders ? folders.Count : 0;
            int itemsSent = 0;
            int foldersSent = 0;
            int foldersToSend = 0;
            int itemsToSend = 0;

            InventoryDescendentsPacket currentPacket = null;

            // Handle empty folders
            //
            if (totalItems == 0 && totalFolders == 0)
                currentPacket = CreateInventoryDescendentsPacket(ownerID, folderID, version, items.Count + folders.Count,
                                                                 0, 0);

            // To preserve SL compatibility, we will NOT combine folders and items in one packet
            //
            while (itemsSent < totalItems || foldersSent < totalFolders)
            {
                if (currentPacket == null) // Start a new packet
                {
                    foldersToSend = totalFolders - foldersSent;
                    if (foldersToSend > MAX_FOLDERS_PER_PACKET)
                        foldersToSend = MAX_FOLDERS_PER_PACKET;

                    if (foldersToSend == 0)
                    {
                        itemsToSend = totalItems - itemsSent;
                        if (itemsToSend > MAX_ITEMS_PER_PACKET)
                            itemsToSend = MAX_ITEMS_PER_PACKET;
                    }

                    currentPacket = CreateInventoryDescendentsPacket(ownerID, folderID, version,
                                                                     items.Count + folders.Count, foldersToSend,
                                                                     itemsToSend);
                }

                if (foldersToSend-- > 0)
                    currentPacket.FolderData[foldersSent%MAX_FOLDERS_PER_PACKET] =
                        CreateFolderDataBlock(folders[foldersSent++]);
                else if (itemsToSend-- > 0)
                    currentPacket.ItemData[itemsSent%MAX_ITEMS_PER_PACKET] = CreateItemDataBlock(items[itemsSent++]);
                else
                {
                    OutPacket(currentPacket, ThrottleOutPacketType.Asset, false, null);
                    currentPacket = null;
                }
            }

            if (currentPacket != null)
                OutPacket(currentPacket, ThrottleOutPacketType.Asset, false, null);
        }

        private InventoryDescendentsPacket.FolderDataBlock CreateFolderDataBlock(InventoryFolderBase folder)
        {
            InventoryDescendentsPacket.FolderDataBlock newBlock = new InventoryDescendentsPacket.FolderDataBlock
                                                                      {
                                                                          FolderID = folder.ID,
                                                                          Name = Util.StringToBytes256(folder.Name),
                                                                          ParentID = folder.ParentID,
                                                                          Type = (sbyte) folder.Type
                                                                      };

            return newBlock;
        }

        private InventoryDescendentsPacket.ItemDataBlock CreateItemDataBlock(InventoryItemBase item)
        {
            InventoryDescendentsPacket.ItemDataBlock newBlock = new InventoryDescendentsPacket.ItemDataBlock
                                                                    {
                                                                        ItemID = item.ID,
                                                                        AssetID = item.AssetID,
                                                                        CreatorID = item.CreatorIdAsUuid,
                                                                        BaseMask = item.BasePermissions,
                                                                        Description =
                                                                            Util.StringToBytes256(item.Description),
                                                                        EveryoneMask = item.EveryOnePermissions,
                                                                        OwnerMask = item.CurrentPermissions,
                                                                        FolderID = item.Folder,
                                                                        InvType = (sbyte) item.InvType,
                                                                        Name = Util.StringToBytes256(item.Name),
                                                                        NextOwnerMask = item.NextPermissions,
                                                                        OwnerID = item.Owner,
                                                                        Type =
                                                                            Util.CheckMeshType((sbyte) item.AssetType),
                                                                        GroupID = item.GroupID,
                                                                        GroupOwned = item.GroupOwned,
                                                                        GroupMask = item.GroupPermissions,
                                                                        CreationDate = item.CreationDate,
                                                                        SalePrice = item.SalePrice,
                                                                        SaleType = item.SaleType,
                                                                        Flags = item.Flags
                                                                    };


            newBlock.CRC =
                Helpers.InventoryCRC(newBlock.CreationDate, newBlock.SaleType,
                                     newBlock.InvType, newBlock.Type,
                                     newBlock.AssetID, newBlock.GroupID,
                                     newBlock.SalePrice,
                                     newBlock.OwnerID, newBlock.CreatorID,
                                     newBlock.ItemID, newBlock.FolderID,
                                     newBlock.EveryoneMask,
                                     newBlock.Flags, newBlock.OwnerMask,
                                     newBlock.GroupMask, newBlock.NextOwnerMask);

            return newBlock;
        }

        private void AddNullFolderBlockToDecendentsPacket(ref InventoryDescendentsPacket packet)
        {
            packet.FolderData = new InventoryDescendentsPacket.FolderDataBlock[1];
            packet.FolderData[0] = new InventoryDescendentsPacket.FolderDataBlock
                                       {FolderID = UUID.Zero, ParentID = UUID.Zero, Type = -1, Name = new byte[0]};
        }

        private void AddNullItemBlockToDescendentsPacket(ref InventoryDescendentsPacket packet)
        {
            packet.ItemData = new InventoryDescendentsPacket.ItemDataBlock[1];
            packet.ItemData[0] = new InventoryDescendentsPacket.ItemDataBlock
                                     {
                                         ItemID = UUID.Zero,
                                         AssetID = UUID.Zero,
                                         CreatorID = UUID.Zero,
                                         BaseMask = 0,
                                         Description = new byte[0],
                                         EveryoneMask = 0,
                                         OwnerMask = 0,
                                         FolderID = UUID.Zero,
                                         InvType = 0,
                                         Name = new byte[0],
                                         NextOwnerMask = 0,
                                         OwnerID = UUID.Zero,
                                         Type = -1,
                                         GroupID = UUID.Zero,
                                         GroupOwned = false,
                                         GroupMask = 0,
                                         CreationDate = 0,
                                         SalePrice = 0,
                                         SaleType = 0,
                                         Flags = 0
                                     };


            // No need to add CRC
        }

        private InventoryDescendentsPacket CreateInventoryDescendentsPacket(UUID ownerID, UUID folderID, int version,
                                                                            int descendents, int folders, int items)
        {
            InventoryDescendentsPacket descend =
                (InventoryDescendentsPacket) PacketPool.Instance.GetPacket(PacketType.InventoryDescendents);
            descend.Header.Zerocoded = true;
            descend.AgentData.AgentID = AgentId;
            descend.AgentData.OwnerID = ownerID;
            descend.AgentData.FolderID = folderID;
            descend.AgentData.Version = version;
            descend.AgentData.Descendents = descendents;

            if (folders > 0)
                descend.FolderData = new InventoryDescendentsPacket.FolderDataBlock[folders];
            else
                AddNullFolderBlockToDecendentsPacket(ref descend);

            if (items > 0)
                descend.ItemData = new InventoryDescendentsPacket.ItemDataBlock[items];
            else
                AddNullItemBlockToDescendentsPacket(ref descend);

            return descend;
        }

        public void SendInventoryItemDetails(UUID ownerID, InventoryItemBase item)
        {
            const uint FULL_MASK_PERMISSIONS = (uint) PermissionMask.All;

            FetchInventoryReplyPacket inventoryReply =
                (FetchInventoryReplyPacket) PacketPool.Instance.GetPacket(PacketType.FetchInventoryReply);
            // TODO: don't create new blocks if recycling an old packet
            inventoryReply.AgentData.AgentID = AgentId;
            inventoryReply.InventoryData = new FetchInventoryReplyPacket.InventoryDataBlock[1];
            inventoryReply.InventoryData[0] = new FetchInventoryReplyPacket.InventoryDataBlock
                                                  {
                                                      ItemID = item.ID,
                                                      AssetID = item.AssetID,
                                                      CreatorID = item.CreatorIdAsUuid,
                                                      BaseMask = item.BasePermissions,
                                                      CreationDate = item.CreationDate,
                                                      Description = Util.StringToBytes256(item.Description),
                                                      EveryoneMask = item.EveryOnePermissions,
                                                      FolderID = item.Folder,
                                                      InvType = (sbyte) item.InvType,
                                                      Name = Util.StringToBytes256(item.Name),
                                                      NextOwnerMask = item.NextPermissions,
                                                      OwnerID = item.Owner,
                                                      OwnerMask = item.CurrentPermissions,
                                                      Type = Util.CheckMeshType((sbyte) item.AssetType),
                                                      GroupID = item.GroupID,
                                                      GroupOwned = item.GroupOwned,
                                                      GroupMask = item.GroupPermissions,
                                                      Flags = item.Flags,
                                                      SalePrice = item.SalePrice,
                                                      SaleType = item.SaleType
                                                  };


            inventoryReply.InventoryData[0].CRC =
                Helpers.InventoryCRC(
                    1000, 0, inventoryReply.InventoryData[0].InvType,
                    inventoryReply.InventoryData[0].Type, inventoryReply.InventoryData[0].AssetID,
                    inventoryReply.InventoryData[0].GroupID, 100,
                    inventoryReply.InventoryData[0].OwnerID, inventoryReply.InventoryData[0].CreatorID,
                    inventoryReply.InventoryData[0].ItemID, inventoryReply.InventoryData[0].FolderID,
                    FULL_MASK_PERMISSIONS, 1, FULL_MASK_PERMISSIONS, FULL_MASK_PERMISSIONS,
                    FULL_MASK_PERMISSIONS);
            inventoryReply.Header.Zerocoded = true;
            OutPacket(inventoryReply, ThrottleOutPacketType.Asset);
        }

        public void SendBulkUpdateInventory(InventoryFolderBase folder)
        {
            // We will use the same transaction id for all the separate packets to be sent out in this update.
            UUID transactionId = UUID.Random();

            List<BulkUpdateInventoryPacket.FolderDataBlock> folderDataBlocks
                = new List<BulkUpdateInventoryPacket.FolderDataBlock>();

            SendBulkUpdateInventoryFolderRecursive(folder, ref folderDataBlocks, transactionId);

            if (folderDataBlocks.Count > 0)
            {
                // We'll end up with some unsent folder blocks if there were some empty folders at the end of the list
                // Send these now
                BulkUpdateInventoryPacket bulkUpdate
                    = (BulkUpdateInventoryPacket) PacketPool.Instance.GetPacket(PacketType.BulkUpdateInventory);
                bulkUpdate.Header.Zerocoded = true;

                bulkUpdate.AgentData.AgentID = AgentId;
                bulkUpdate.AgentData.TransactionID = transactionId;
                bulkUpdate.FolderData = folderDataBlocks.ToArray();
                List<BulkUpdateInventoryPacket.ItemDataBlock> foo = new List<BulkUpdateInventoryPacket.ItemDataBlock>();
                bulkUpdate.ItemData = foo.ToArray();

                //MainConsole.Instance.Debug("SendBulkUpdateInventory :" + bulkUpdate);
                OutPacket(bulkUpdate, ThrottleOutPacketType.Asset);
            }
        }

        /// <summary>
        ///   Recursively construct bulk update packets to send folders and items
        /// </summary>
        /// <param name = "folder"></param>
        /// <param name = "folderDataBlocks"></param>
        /// <param name = "transactionId"></param>
        private void SendBulkUpdateInventoryFolderRecursive(
            InventoryFolderBase folder, ref List<BulkUpdateInventoryPacket.FolderDataBlock> folderDataBlocks,
            UUID transactionId)
        {
            folderDataBlocks.Add(GenerateBulkUpdateFolderDataBlock(folder));

            const int MAX_ITEMS_PER_PACKET = 5;

            IInventoryService invService = m_scene.RequestModuleInterface<IInventoryService>();
            // If there are any items then we have to start sending them off in this packet - the next folder will have
            // to be in its own bulk update packet.  Also, we can only fit 5 items in a packet (at least this was the limit
            // being used on the Linden grid at 20081203).
            InventoryCollection contents = invService.GetFolderContent(AgentId, folder.ID);
            // folder.RequestListOfItems();
            List<InventoryItemBase> items = contents.Items;
            while (items.Count > 0)
            {
                BulkUpdateInventoryPacket bulkUpdate
                    = (BulkUpdateInventoryPacket) PacketPool.Instance.GetPacket(PacketType.BulkUpdateInventory);
                bulkUpdate.Header.Zerocoded = true;

                bulkUpdate.AgentData.AgentID = AgentId;
                bulkUpdate.AgentData.TransactionID = transactionId;
                bulkUpdate.FolderData = folderDataBlocks.ToArray();

                int itemsToSend = (items.Count > MAX_ITEMS_PER_PACKET ? MAX_ITEMS_PER_PACKET : items.Count);
                bulkUpdate.ItemData = new BulkUpdateInventoryPacket.ItemDataBlock[itemsToSend];

                for (int i = 0; i < itemsToSend; i++)
                {
                    // Remove from the end of the list so that we don't incur a performance penalty
                    bulkUpdate.ItemData[i] = GenerateBulkUpdateItemDataBlock(items[items.Count - 1]);
                    items.RemoveAt(items.Count - 1);
                }

                //MainConsole.Instance.Debug("SendBulkUpdateInventoryRecursive :" + bulkUpdate);
                OutPacket(bulkUpdate, ThrottleOutPacketType.Asset);

                folderDataBlocks = new List<BulkUpdateInventoryPacket.FolderDataBlock>();

                // If we're going to be sending another items packet then it needs to contain just the folder to which those
                // items belong.
                if (items.Count > 0)
                    folderDataBlocks.Add(GenerateBulkUpdateFolderDataBlock(folder));
            }

            List<InventoryFolderBase> subFolders = contents.Folders;
            foreach (InventoryFolderBase subFolder in subFolders)
            {
                SendBulkUpdateInventoryFolderRecursive(subFolder, ref folderDataBlocks, transactionId);
            }
        }

        /// <summary>
        ///   Generate a bulk update inventory data block for the given folder
        /// </summary>
        /// <param name = "folder"></param>
        /// <returns></returns>
        private BulkUpdateInventoryPacket.FolderDataBlock GenerateBulkUpdateFolderDataBlock(InventoryFolderBase folder)
        {
            BulkUpdateInventoryPacket.FolderDataBlock folderBlock = new BulkUpdateInventoryPacket.FolderDataBlock
                                                                        {
                                                                            FolderID = folder.ID,
                                                                            ParentID = folder.ParentID,
                                                                            Type = -1,
                                                                            Name = Util.StringToBytes256(folder.Name)
                                                                        };


            return folderBlock;
        }

        /// <summary>
        ///   Generate a bulk update inventory data block for the given item
        /// </summary>
        /// <param name = "item"></param>
        /// <returns></returns>
        private BulkUpdateInventoryPacket.ItemDataBlock GenerateBulkUpdateItemDataBlock(InventoryItemBase item)
        {
            BulkUpdateInventoryPacket.ItemDataBlock itemBlock = new BulkUpdateInventoryPacket.ItemDataBlock
                                                                    {
                                                                        ItemID = item.ID,
                                                                        AssetID = item.AssetID,
                                                                        CreatorID = item.CreatorIdAsUuid,
                                                                        BaseMask = item.BasePermissions,
                                                                        Description =
                                                                            Util.StringToBytes256(item.Description),
                                                                        EveryoneMask = item.EveryOnePermissions,
                                                                        FolderID = item.Folder,
                                                                        InvType = (sbyte) item.InvType,
                                                                        Name = Util.StringToBytes256(item.Name),
                                                                        NextOwnerMask = item.NextPermissions,
                                                                        OwnerID = item.Owner,
                                                                        OwnerMask = item.CurrentPermissions,
                                                                        Type =
                                                                            Util.CheckMeshType((sbyte) item.AssetType),
                                                                        GroupID = item.GroupID,
                                                                        GroupOwned = item.GroupOwned,
                                                                        GroupMask = item.GroupPermissions,
                                                                        Flags = item.Flags,
                                                                        SalePrice = item.SalePrice,
                                                                        SaleType = item.SaleType,
                                                                        CreationDate = item.CreationDate
                                                                    };


            itemBlock.CRC =
                Helpers.InventoryCRC(
                    1000, 0, itemBlock.InvType,
                    itemBlock.Type, itemBlock.AssetID,
                    itemBlock.GroupID, 100,
                    itemBlock.OwnerID, itemBlock.CreatorID,
                    itemBlock.ItemID, itemBlock.FolderID,
                    (uint) PermissionMask.All, 1, (uint) PermissionMask.All, (uint) PermissionMask.All,
                    (uint) PermissionMask.All);

            return itemBlock;
        }

        public void SendBulkUpdateInventory(InventoryItemBase item)
        {
            const uint FULL_MASK_PERMISSIONS = (uint) PermissionMask.All;

            BulkUpdateInventoryPacket bulkUpdate
                = (BulkUpdateInventoryPacket) PacketPool.Instance.GetPacket(PacketType.BulkUpdateInventory);

            bulkUpdate.AgentData.AgentID = AgentId;
            bulkUpdate.AgentData.TransactionID = UUID.Random();

            bulkUpdate.FolderData = new BulkUpdateInventoryPacket.FolderDataBlock[1];
            bulkUpdate.FolderData[0] = new BulkUpdateInventoryPacket.FolderDataBlock
                                           {FolderID = UUID.Zero, ParentID = UUID.Zero, Type = -1, Name = new byte[0]};

            bulkUpdate.ItemData = new BulkUpdateInventoryPacket.ItemDataBlock[1];
            bulkUpdate.ItemData[0] = new BulkUpdateInventoryPacket.ItemDataBlock
                                         {
                                             ItemID = item.ID,
                                             AssetID = item.AssetID,
                                             CreatorID = item.CreatorIdAsUuid,
                                             BaseMask = item.BasePermissions,
                                             CreationDate = item.CreationDate,
                                             Description = Util.StringToBytes256(item.Description),
                                             EveryoneMask = item.EveryOnePermissions,
                                             FolderID = item.Folder,
                                             InvType = (sbyte) item.InvType,
                                             Name = Util.StringToBytes256(item.Name),
                                             NextOwnerMask = item.NextPermissions,
                                             OwnerID = item.Owner,
                                             OwnerMask = item.CurrentPermissions,
                                             Type = Util.CheckMeshType((sbyte) item.AssetType),
                                             GroupID = item.GroupID,
                                             GroupOwned = item.GroupOwned,
                                             GroupMask = item.GroupPermissions,
                                             Flags = item.Flags,
                                             SalePrice = item.SalePrice,
                                             SaleType = item.SaleType
                                         };


            bulkUpdate.ItemData[0].CRC =
                Helpers.InventoryCRC(1000, 0, bulkUpdate.ItemData[0].InvType,
                                     bulkUpdate.ItemData[0].Type, bulkUpdate.ItemData[0].AssetID,
                                     bulkUpdate.ItemData[0].GroupID, 100,
                                     bulkUpdate.ItemData[0].OwnerID, bulkUpdate.ItemData[0].CreatorID,
                                     bulkUpdate.ItemData[0].ItemID, bulkUpdate.ItemData[0].FolderID,
                                     FULL_MASK_PERMISSIONS, 1, FULL_MASK_PERMISSIONS, FULL_MASK_PERMISSIONS,
                                     FULL_MASK_PERMISSIONS);
            bulkUpdate.Header.Zerocoded = true;
            OutPacket(bulkUpdate, ThrottleOutPacketType.Asset);
        }

        /// <see>IClientAPI.SendInventoryItemCreateUpdate(InventoryItemBase)</see>
        public void SendInventoryItemCreateUpdate(InventoryItemBase Item, uint callbackId)
        {
            const uint FULL_MASK_PERMISSIONS = (uint) PermissionMask.All;

            UpdateCreateInventoryItemPacket InventoryReply
                = (UpdateCreateInventoryItemPacket) PacketPool.Instance.GetPacket(
                    PacketType.UpdateCreateInventoryItem);

            // TODO: don't create new blocks if recycling an old packet
            InventoryReply.AgentData.AgentID = AgentId;
            InventoryReply.AgentData.SimApproved = true;
            InventoryReply.InventoryData = new UpdateCreateInventoryItemPacket.InventoryDataBlock[1];
            InventoryReply.InventoryData[0] = new UpdateCreateInventoryItemPacket.InventoryDataBlock
                                                  {
                                                      ItemID = Item.ID,
                                                      AssetID = Item.AssetID,
                                                      CreatorID = Item.CreatorIdAsUuid,
                                                      BaseMask = Item.BasePermissions,
                                                      Description = Util.StringToBytes256(Item.Description),
                                                      EveryoneMask = Item.EveryOnePermissions,
                                                      FolderID = Item.Folder,
                                                      InvType = (sbyte) Item.InvType,
                                                      Name = Util.StringToBytes256(Item.Name),
                                                      NextOwnerMask = Item.NextPermissions,
                                                      OwnerID = Item.Owner,
                                                      OwnerMask = Item.CurrentPermissions,
                                                      Type = Util.CheckMeshType((sbyte) Item.AssetType),
                                                      CallbackID = callbackId,
                                                      GroupID = Item.GroupID,
                                                      GroupOwned = Item.GroupOwned,
                                                      GroupMask = Item.GroupPermissions,
                                                      Flags = Item.Flags,
                                                      SalePrice = Item.SalePrice,
                                                      SaleType = Item.SaleType,
                                                      CreationDate = Item.CreationDate
                                                  };


            InventoryReply.InventoryData[0].CRC =
                Helpers.InventoryCRC(1000, 0, InventoryReply.InventoryData[0].InvType,
                                     InventoryReply.InventoryData[0].Type, InventoryReply.InventoryData[0].AssetID,
                                     InventoryReply.InventoryData[0].GroupID, 100,
                                     InventoryReply.InventoryData[0].OwnerID, InventoryReply.InventoryData[0].CreatorID,
                                     InventoryReply.InventoryData[0].ItemID, InventoryReply.InventoryData[0].FolderID,
                                     FULL_MASK_PERMISSIONS, 1, FULL_MASK_PERMISSIONS, FULL_MASK_PERMISSIONS,
                                     FULL_MASK_PERMISSIONS);
            InventoryReply.Header.Zerocoded = true;
            OutPacket(InventoryReply, ThrottleOutPacketType.Asset);
        }

        public void SendRemoveInventoryItem(UUID itemID)
        {
            RemoveInventoryItemPacket remove =
                (RemoveInventoryItemPacket) PacketPool.Instance.GetPacket(PacketType.RemoveInventoryItem);
            // TODO: don't create new blocks if recycling an old packet
            remove.AgentData.AgentID = AgentId;
            remove.AgentData.SessionID = m_sessionId;
            remove.InventoryData = new RemoveInventoryItemPacket.InventoryDataBlock[1];
            remove.InventoryData[0] = new RemoveInventoryItemPacket.InventoryDataBlock {ItemID = itemID};
            remove.Header.Zerocoded = true;
            OutPacket(remove, ThrottleOutPacketType.Asset);
        }

        public void SendTakeControls(int controls, bool passToAgent, bool TakeControls)
        {
            ScriptControlChangePacket scriptcontrol =
                (ScriptControlChangePacket) PacketPool.Instance.GetPacket(PacketType.ScriptControlChange);
            ScriptControlChangePacket.DataBlock[] data = new ScriptControlChangePacket.DataBlock[1];
            ScriptControlChangePacket.DataBlock ddata = new ScriptControlChangePacket.DataBlock
                                                            {
                                                                Controls = (uint) controls,
                                                                PassToAgent = passToAgent,
                                                                TakeControls = TakeControls
                                                            };
            data[0] = ddata;
            scriptcontrol.Data = data;
            OutPacket(scriptcontrol, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendTaskInventory(UUID taskID, short serial, byte[] fileName)
        {
            ReplyTaskInventoryPacket replytask =
                (ReplyTaskInventoryPacket) PacketPool.Instance.GetPacket(PacketType.ReplyTaskInventory);
            replytask.InventoryData.TaskID = taskID;
            replytask.InventoryData.Serial = serial;
            replytask.InventoryData.Filename = fileName;
            OutPacket(replytask, ThrottleOutPacketType.Transfer);
        }

        public void SendXferPacket(ulong xferID, uint packet, byte[] data)
        {
            SendXferPacketPacket sendXfer =
                (SendXferPacketPacket) PacketPool.Instance.GetPacket(PacketType.SendXferPacket);
            sendXfer.XferID.ID = xferID;
            sendXfer.XferID.Packet = packet;
            sendXfer.DataPacket.Data = data;
            OutPacket(sendXfer, ThrottleOutPacketType.Transfer);
        }

        public void SendEconomyData(float EnergyEfficiency, int ObjectCapacity, int ObjectCount, int PriceEnergyUnit,
                                    int PriceGroupCreate, int PriceObjectClaim, float PriceObjectRent,
                                    float PriceObjectScaleFactor,
                                    int PriceParcelClaim, float PriceParcelClaimFactor, int PriceParcelRent,
                                    int PricePublicObjectDecay,
                                    int PricePublicObjectDelete, int PriceRentLight, int PriceUpload,
                                    int TeleportMinPrice, float TeleportPriceExponent)
        {
            EconomyDataPacket economyData = (EconomyDataPacket) PacketPool.Instance.GetPacket(PacketType.EconomyData);
            economyData.Info.EnergyEfficiency = EnergyEfficiency;
            economyData.Info.ObjectCapacity = ObjectCapacity;
            economyData.Info.ObjectCount = ObjectCount;
            economyData.Info.PriceEnergyUnit = PriceEnergyUnit;
            economyData.Info.PriceGroupCreate = PriceGroupCreate;
            economyData.Info.PriceObjectClaim = PriceObjectClaim;
            economyData.Info.PriceObjectRent = PriceObjectRent;
            economyData.Info.PriceObjectScaleFactor = PriceObjectScaleFactor;
            economyData.Info.PriceParcelClaim = PriceParcelClaim;
            economyData.Info.PriceParcelClaimFactor = PriceParcelClaimFactor;
            economyData.Info.PriceParcelRent = PriceParcelRent;
            economyData.Info.PricePublicObjectDecay = PricePublicObjectDecay;
            economyData.Info.PricePublicObjectDelete = PricePublicObjectDelete;
            economyData.Info.PriceRentLight = PriceRentLight;
            economyData.Info.PriceUpload = PriceUpload;
            economyData.Info.TeleportMinPrice = TeleportMinPrice;
            economyData.Info.TeleportPriceExponent = TeleportPriceExponent;
            economyData.Header.Reliable = true;
            OutPacket(economyData, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendAvatarPickerReply(AvatarPickerReplyAgentDataArgs AgentData, List<AvatarPickerReplyDataArgs> Data)
        {
            //construct the AvatarPickerReply packet.
#if (!ISWIN)
            List<AvatarPickerReplyPacket.DataBlock> list = new List<AvatarPickerReplyPacket.DataBlock>();
            foreach (AvatarPickerReplyDataArgs arg in Data)
                list.Add(new AvatarPickerReplyPacket.DataBlock
                             {
                                 AvatarID = arg.AvatarID, FirstName = arg.FirstName, LastName = arg.LastName
                             });
            AvatarPickerReplyPacket replyPacket = new AvatarPickerReplyPacket
                                                      {
                                                          AgentData =
                                                              {AgentID = AgentData.AgentID, QueryID = AgentData.QueryID},
                                                          Data =
                                                              list.ToArray()
                                                      };
#else
                AvatarPickerReplyPacket replyPacket = new AvatarPickerReplyPacket
                                                      {
                                                          AgentData =
                                                              {AgentID = AgentData.AgentID, QueryID = AgentData.QueryID},
                                                          Data =
                                                              Data.Select(arg => new AvatarPickerReplyPacket.DataBlock
                                                                                     {
                                                                                         AvatarID = arg.AvatarID,
                                                                                         FirstName = arg.FirstName,
                                                                                         LastName = arg.LastName
                                                                                     }).ToArray()
                                                      };
#endif
            //int i = 0;
            OutPacket(replyPacket, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendAgentDataUpdate(UUID agentid, UUID activegroupid, string firstname, string lastname,
                                        ulong grouppowers, string groupname, string grouptitle)
        {
            m_activeGroupID = activegroupid;
            m_activeGroupName = groupname;
            m_activeGroupPowers = grouppowers;

            AgentDataUpdatePacket sendAgentDataUpdate =
                (AgentDataUpdatePacket) PacketPool.Instance.GetPacket(PacketType.AgentDataUpdate);
            sendAgentDataUpdate.AgentData.ActiveGroupID = activegroupid;
            sendAgentDataUpdate.AgentData.AgentID = agentid;
            sendAgentDataUpdate.AgentData.FirstName = Util.StringToBytes256(firstname);
            sendAgentDataUpdate.AgentData.GroupName = Util.StringToBytes256(groupname);
            sendAgentDataUpdate.AgentData.GroupPowers = grouppowers;
            sendAgentDataUpdate.AgentData.GroupTitle = Util.StringToBytes256(grouptitle);
            sendAgentDataUpdate.AgentData.LastName = Util.StringToBytes256(lastname);
            OutPacket(sendAgentDataUpdate, ThrottleOutPacketType.AvatarInfo);
        }

        /// <summary>
        ///   Send an alert message to the client.  On the Linden client (tested 1.19.1.4), this pops up a brief duration
        ///   blue information box in the bottom right hand corner.
        /// </summary>
        /// <param name = "message"></param>
        public void SendAlertMessage(string message)
        {
            AlertMessagePacket alertPack = (AlertMessagePacket) PacketPool.Instance.GetPacket(PacketType.AlertMessage);
            alertPack.AlertData = new AlertMessagePacket.AlertDataBlock {Message = Util.StringToBytes256(message)};
            alertPack.AlertInfo = new AlertMessagePacket.AlertInfoBlock[0];
            OutPacket(alertPack, ThrottleOutPacketType.AvatarInfo);
        }

        /// <summary>
        ///   Send an agent alert message to the client.
        /// </summary>
        /// <param name = "message"></param>
        /// <param name = "modal">On the linden client, if this true then it displays a one button text box placed in the
        ///   middle of the window.  If false, the message is displayed in a brief duration blue information box (as for
        ///   the AlertMessage packet).</param>
        public void SendAgentAlertMessage(string message, bool modal)
        {
            AgentAlertMessagePacket alertPack =
                (AgentAlertMessagePacket) PacketPool.Instance.GetPacket(PacketType.AgentAlertMessage);
            alertPack.AgentData.AgentID = AgentId;
            alertPack.AlertData.Message = Util.StringToBytes256(message);
            alertPack.AlertData.Modal = modal;
            OutPacket(alertPack, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendLoadURL(string objectname, UUID objectID, UUID ownerID, bool groupOwned, string message,
                                string url)
        {
            LoadURLPacket loadURL = (LoadURLPacket) PacketPool.Instance.GetPacket(PacketType.LoadURL);
            loadURL.Data.ObjectName = Util.StringToBytes256(objectname);
            loadURL.Data.ObjectID = objectID;
            loadURL.Data.OwnerID = ownerID;
            loadURL.Data.OwnerIsGroup = groupOwned;
            loadURL.Data.Message = Util.StringToBytes256(message);
            loadURL.Data.URL = Util.StringToBytes256(url);
            OutPacket(loadURL, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendDialog(string objectname, UUID objectID, UUID ownerID, string ownerFirstName,
                               string ownerLastName, string msg, UUID textureID, int ch, string[] buttonlabels)
        {
            ScriptDialogPacket dialog = (ScriptDialogPacket) PacketPool.Instance.GetPacket(PacketType.ScriptDialog);
            dialog.Data.ObjectID = objectID;
            dialog.Data.ObjectName = Util.StringToBytes256(objectname);
            // this is the username of the *owner*
            dialog.Data.FirstName = Util.StringToBytes256(ownerFirstName);
            dialog.Data.LastName = Util.StringToBytes256(ownerLastName);
            dialog.Data.Message = Util.StringToBytes1024(msg);
            dialog.Data.ImageID = textureID;
            dialog.Data.ChatChannel = ch;
            ScriptDialogPacket.ButtonsBlock[] buttons = new ScriptDialogPacket.ButtonsBlock[buttonlabels.Length];
            for (int i = 0; i < buttonlabels.Length; i++)
            {
                buttons[i] = new ScriptDialogPacket.ButtonsBlock {ButtonLabel = Util.StringToBytes256(buttonlabels[i])};
            }
            dialog.Buttons = buttons;
            dialog.OwnerData = new ScriptDialogPacket.OwnerDataBlock[1];
            dialog.OwnerData[0] = new ScriptDialogPacket.OwnerDataBlock {OwnerID = ownerID};
            OutPacket(dialog, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendPreLoadSound(UUID objectID, UUID ownerID, UUID soundID)
        {
            PreloadSoundPacket preSound = (PreloadSoundPacket) PacketPool.Instance.GetPacket(PacketType.PreloadSound);
            // TODO: don't create new blocks if recycling an old packet
            preSound.DataBlock = new PreloadSoundPacket.DataBlockBlock[1];
            preSound.DataBlock[0] = new PreloadSoundPacket.DataBlockBlock
                                        {ObjectID = objectID, OwnerID = ownerID, SoundID = soundID};
            preSound.Header.Zerocoded = true;
            OutPacket(preSound, ThrottleOutPacketType.Asset);
        }

        public void SendPlayAttachedSound(UUID soundID, UUID objectID, UUID ownerID, float gain, byte flags)
        {
            AttachedSoundPacket sound = (AttachedSoundPacket) PacketPool.Instance.GetPacket(PacketType.AttachedSound);
            sound.DataBlock.SoundID = soundID;
            sound.DataBlock.ObjectID = objectID;
            sound.DataBlock.OwnerID = ownerID;
            sound.DataBlock.Gain = gain;
            sound.DataBlock.Flags = flags;

            OutPacket(sound, ThrottleOutPacketType.Asset);
        }

        public void SendTriggeredSound(UUID soundID, UUID ownerID, UUID objectID, UUID parentID, ulong handle,
                                       Vector3 position, float gain)
        {
            SoundTriggerPacket sound = (SoundTriggerPacket) PacketPool.Instance.GetPacket(PacketType.SoundTrigger);
            sound.SoundData.SoundID = soundID;
            sound.SoundData.OwnerID = ownerID;
            sound.SoundData.ObjectID = objectID;
            sound.SoundData.ParentID = parentID;
            sound.SoundData.Handle = handle;
            sound.SoundData.Position = position;
            sound.SoundData.Gain = gain;

            OutPacket(sound, ThrottleOutPacketType.Asset);
        }

        public void SendAttachedSoundGainChange(UUID objectID, float gain)
        {
            AttachedSoundGainChangePacket sound =
                (AttachedSoundGainChangePacket) PacketPool.Instance.GetPacket(PacketType.AttachedSoundGainChange);
            sound.DataBlock.ObjectID = objectID;
            sound.DataBlock.Gain = gain;

            OutPacket(sound, ThrottleOutPacketType.Asset);
        }

        public void SendSunPos(Vector3 Position, Vector3 Velocity, ulong CurrentTime, uint SecondsPerSunCycle,
                               uint SecondsPerYear, float OrbitalPosition)
        {
            // Viewers based on the Linden viwer code, do wacky things for oribital positions from Midnight to Sunrise
            // So adjust for that
            // Contributed by: Godfrey

            if (OrbitalPosition > m_sunPainDaHalfOrbitalCutoff) // things get weird from midnight to sunrise
            {
                OrbitalPosition = (OrbitalPosition - m_sunPainDaHalfOrbitalCutoff)*0.6666666667f +
                                  m_sunPainDaHalfOrbitalCutoff;
            }

            SimulatorViewerTimeMessagePacket viewertime =
                (SimulatorViewerTimeMessagePacket) PacketPool.Instance.GetPacket(PacketType.SimulatorViewerTimeMessage);
            viewertime.TimeInfo.SunDirection = Position;
            viewertime.TimeInfo.SunAngVelocity = Velocity;

            // Sun module used to add 6 hours to adjust for linden sun hour, adding here
            // to prevent existing code from breaking if it assumed that 6 hours were included.
            // 21600 == 6 hours * 60 minutes * 60 Seconds
            viewertime.TimeInfo.UsecSinceStart = CurrentTime + 21600;

            viewertime.TimeInfo.SecPerDay = SecondsPerSunCycle;
            viewertime.TimeInfo.SecPerYear = SecondsPerYear;
            viewertime.TimeInfo.SunPhase = OrbitalPosition;
            viewertime.Header.Reliable = false;
            viewertime.Header.Zerocoded = true;
            OutPacket(viewertime, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendViewerEffect(ViewerEffectPacket.EffectBlock[] effectBlocks)
        {
            ViewerEffectPacket packet = (ViewerEffectPacket) PacketPool.Instance.GetPacket(PacketType.ViewerEffect);
            packet.Header.Reliable = false;
            packet.Header.Zerocoded = true;

            packet.AgentData.AgentID = AgentId;
            packet.AgentData.SessionID = SessionId;

            packet.Effect = effectBlocks;

            OutPacket(packet, ThrottleOutPacketType.State);
        }

        public void SendAvatarProperties(UUID avatarID, string aboutText, string bornOn, Byte[] charterMember,
                                         string flAbout, uint flags, UUID flImageID, UUID imageID, string profileURL,
                                         UUID partnerID)
        {
            AvatarPropertiesReplyPacket avatarReply =
                (AvatarPropertiesReplyPacket) PacketPool.Instance.GetPacket(PacketType.AvatarPropertiesReply);
            avatarReply.AgentData.AgentID = AgentId;
            avatarReply.AgentData.AvatarID = avatarID;
            avatarReply.PropertiesData.AboutText = aboutText != null ? Util.StringToBytes1024(aboutText) : Utils.EmptyBytes;
            avatarReply.PropertiesData.BornOn = Util.StringToBytes256(bornOn);
            avatarReply.PropertiesData.CharterMember = charterMember;
            avatarReply.PropertiesData.FLAboutText = flAbout != null ? Util.StringToBytes256(flAbout) : Utils.EmptyBytes;
            avatarReply.PropertiesData.Flags = flags;
            avatarReply.PropertiesData.FLImageID = flImageID;
            avatarReply.PropertiesData.ImageID = imageID;
            avatarReply.PropertiesData.ProfileURL = Util.StringToBytes256(profileURL);
            avatarReply.PropertiesData.PartnerID = partnerID;
            OutPacket(avatarReply, ThrottleOutPacketType.AvatarInfo);
        }

        /// <summary>
        ///   Send the client an Estate message blue box pop-down with a single OK button
        /// </summary>
        /// <param name = "FromAvatarID"></param>
        /// <param name = "FromAvatarName"></param>
        /// <param name = "Message"></param>
        public void SendBlueBoxMessage(UUID FromAvatarID, String FromAvatarName, String Message)
        {
            if (!ChildAgentStatus())
                SendInstantMessage(new GridInstantMessage(null, FromAvatarID, FromAvatarName, AgentId, 1, Message, false,
                                                          new Vector3()));

            //SendInstantMessage(FromAvatarID, fromSessionID, Message, AgentId, SessionId, FromAvatarName, (byte)21,(uint) Util.UnixTimeSinceEpoch());
        }

        public void SendLogoutPacket()
        {
            // I know this is a bit of a hack, however there are times when you don't
            // want to send this, but still need to do the rest of the shutdown process
            // this method gets called from the packet server..   which makes it practically
            // impossible to do any other way.

            if (m_SendLogoutPacketWhenClosing)
            {
                LogoutReplyPacket logReply = (LogoutReplyPacket) PacketPool.Instance.GetPacket(PacketType.LogoutReply);
                // TODO: don't create new blocks if recycling an old packet
                logReply.AgentData.AgentID = AgentId;
                logReply.AgentData.SessionID = SessionId;
                logReply.InventoryData = new LogoutReplyPacket.InventoryDataBlock[1];
                logReply.InventoryData[0] = new LogoutReplyPacket.InventoryDataBlock {ItemID = UUID.Zero};

                OutPacket(logReply, ThrottleOutPacketType.OutBand);
            }
        }

        public void SendHealth(float health)
        {
            HealthMessagePacket healthpacket =
                (HealthMessagePacket) PacketPool.Instance.GetPacket(PacketType.HealthMessage);
            healthpacket.HealthData.Health = health;
            OutPacket(healthpacket, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendAgentOnline(UUID[] agentIDs)
        {
            OnlineNotificationPacket onp = new OnlineNotificationPacket();
            OnlineNotificationPacket.AgentBlockBlock[] onpb =
                new OnlineNotificationPacket.AgentBlockBlock[agentIDs.Length];
            for (int i = 0; i < agentIDs.Length; i++)
            {
                OnlineNotificationPacket.AgentBlockBlock onpbl = new OnlineNotificationPacket.AgentBlockBlock
                                                                     {AgentID = agentIDs[i]};
                onpb[i] = onpbl;
            }
            onp.AgentBlock = onpb;
            onp.Header.Reliable = true;
            OutPacket(onp, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendAgentOffline(UUID[] agentIDs)
        {
            OfflineNotificationPacket offp = new OfflineNotificationPacket();
            OfflineNotificationPacket.AgentBlockBlock[] offpb =
                new OfflineNotificationPacket.AgentBlockBlock[agentIDs.Length];
            for (int i = 0; i < agentIDs.Length; i++)
            {
                OfflineNotificationPacket.AgentBlockBlock onpbl = new OfflineNotificationPacket.AgentBlockBlock
                                                                      {AgentID = agentIDs[i]};
                offpb[i] = onpbl;
            }
            offp.AgentBlock = offpb;
            offp.Header.Reliable = true;
            OutPacket(offp, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendSitResponse(UUID TargetID, Vector3 OffsetPos, Quaternion SitOrientation, bool autopilot,
                                    Vector3 CameraAtOffset, Vector3 CameraEyeOffset, bool ForceMouseLook)
        {
            AvatarSitResponsePacket avatarSitResponse = new AvatarSitResponsePacket {SitObject = {ID = TargetID}};
            if (CameraAtOffset != Vector3.Zero)
            {
                avatarSitResponse.SitTransform.CameraAtOffset = CameraAtOffset;
                avatarSitResponse.SitTransform.CameraEyeOffset = CameraEyeOffset;
            }
            avatarSitResponse.SitTransform.ForceMouselook = ForceMouseLook;
            avatarSitResponse.SitTransform.AutoPilot = autopilot;
            avatarSitResponse.SitTransform.SitPosition = OffsetPos;
            avatarSitResponse.SitTransform.SitRotation = SitOrientation;

            OutPacket(avatarSitResponse, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendAdminResponse(UUID Token, uint AdminLevel)
        {
            GrantGodlikePowersPacket respondPacket = new GrantGodlikePowersPacket();
            GrantGodlikePowersPacket.GrantDataBlock gdb = new GrantGodlikePowersPacket.GrantDataBlock();
            GrantGodlikePowersPacket.AgentDataBlock adb = new GrantGodlikePowersPacket.AgentDataBlock
                                                              {AgentID = AgentId, SessionID = SessionId};

            // More security
            gdb.GodLevel = (byte) AdminLevel;
            gdb.Token = Token;
            respondPacket.AgentData = adb;
            respondPacket.GrantData = gdb;
            OutPacket(respondPacket, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendGroupMembership(GroupMembershipData[] GroupMembership)
        {
            AgentGroupDataUpdatePacket Groupupdate = new AgentGroupDataUpdatePacket();
            AgentGroupDataUpdatePacket.GroupDataBlock[] Groups =
                new AgentGroupDataUpdatePacket.GroupDataBlock[GroupMembership.Length];
            for (int i = 0; i < GroupMembership.Length; i++)
            {
                AgentGroupDataUpdatePacket.GroupDataBlock Group = new AgentGroupDataUpdatePacket.GroupDataBlock
                                                                      {
                                                                          AcceptNotices =
                                                                              GroupMembership[i].AcceptNotices,
                                                                          Contribution = GroupMembership[i].Contribution,
                                                                          GroupID = GroupMembership[i].GroupID,
                                                                          GroupInsigniaID =
                                                                              GroupMembership[i].GroupPicture,
                                                                          GroupName =
                                                                              Util.StringToBytes256(
                                                                                  GroupMembership[i].GroupName),
                                                                          GroupPowers = GroupMembership[i].GroupPowers
                                                                      };
                Groups[i] = Group;
            }
            Groupupdate.GroupData = Groups;
            Groupupdate.AgentData = new AgentGroupDataUpdatePacket.AgentDataBlock {AgentID = AgentId};
            OutPacket(Groupupdate, ThrottleOutPacketType.AvatarInfo);

            try
            {
                IEventQueueService eq = Scene.RequestModuleInterface<IEventQueueService>();
                if (eq != null)
                {
                    eq.GroupMembership(Groupupdate, AgentId, Scene.RegionInfo.RegionHandle);
                }
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Error("Unable to send group membership data via eventqueue - exception: " + ex);
                MainConsole.Instance.Warn("sending group membership data via UDP");
                OutPacket(Groupupdate, ThrottleOutPacketType.AvatarInfo);
            }
        }


        public void SendGroupNameReply(UUID groupLLUID, string GroupName)
        {
            UUIDGroupNameReplyPacket pack = new UUIDGroupNameReplyPacket();
            UUIDGroupNameReplyPacket.UUIDNameBlockBlock[] uidnameblock =
                new UUIDGroupNameReplyPacket.UUIDNameBlockBlock[1];
            UUIDGroupNameReplyPacket.UUIDNameBlockBlock uidnamebloc = new UUIDGroupNameReplyPacket.UUIDNameBlockBlock
                                                                          {
                                                                              ID = groupLLUID,
                                                                              GroupName =
                                                                                  Util.StringToBytes256(GroupName)
                                                                          };
            uidnameblock[0] = uidnamebloc;
            pack.UUIDNameBlock = uidnameblock;
            OutPacket(pack, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendLandStatReply(uint reportType, uint requestFlags, uint resultCount, LandStatReportItem[] lsrpia)
        {
            LandStatReplyMessage message = new LandStatReplyMessage
                                               {
                                                   ReportType = reportType,
                                                   RequestFlags = requestFlags,
                                                   TotalObjectCount = resultCount,
                                                   ReportDataBlocks =
                                                       new LandStatReplyMessage.ReportDataBlock[lsrpia.Length]
                                               };

            for (int i = 0; i < lsrpia.Length; i++)
            {
                LandStatReplyMessage.ReportDataBlock block = new LandStatReplyMessage.ReportDataBlock
                                                                 {
                                                                     Location = lsrpia[i].Location,
                                                                     MonoScore = lsrpia[i].Score,
                                                                     OwnerName = lsrpia[i].OwnerName,
                                                                     Score = lsrpia[i].Score,
                                                                     TaskID = lsrpia[i].TaskID,
                                                                     TaskLocalID = lsrpia[i].TaskLocalID,
                                                                     TaskName = lsrpia[i].TaskName,
                                                                     TimeStamp = lsrpia[i].TimeModified
                                                                 };
                message.ReportDataBlocks[i] = block;
            }

            IEventQueueService eventService = m_scene.RequestModuleInterface<IEventQueueService>();
            if (eventService != null)
            {
                eventService.LandStatReply(message, AgentId, m_scene.RegionInfo.RegionHandle);
            }
        }

        public void SendScriptRunningReply(UUID objectID, UUID itemID, bool running)
        {
            ScriptRunningReplyPacket scriptRunningReply = new ScriptRunningReplyPacket
                                                              {
                                                                  Script =
                                                                      {
                                                                          ObjectID = objectID,
                                                                          ItemID = itemID,
                                                                          Running = running
                                                                      }
                                                              };

            OutPacket(scriptRunningReply, ThrottleOutPacketType.AvatarInfo);
        }

        private void SendFailedAsset(AssetRequestToClient req, TransferPacketStatus assetErrors)
        {
            TransferInfoPacket Transfer = new TransferInfoPacket
                                              {
                                                  TransferInfo =
                                                      {
                                                          ChannelType = (int) ChannelType.Asset,
                                                          Status = (int) assetErrors,
                                                          TargetType = 0,
                                                          Params = req.Params,
                                                          Size = 0,
                                                          TransferID = req.TransferRequestID
                                                      },
                                                  Header = {Zerocoded = true}
                                              };
            OutPacket(Transfer, ThrottleOutPacketType.Transfer);
        }

        public void SendAsset(AssetRequestToClient req)
        {
            if (req.AssetInf.Data == null)
            {
                MainConsole.Instance.ErrorFormat("[LLClientView]: Cannot send asset {0} ({1}), asset data is null",
                                  req.AssetInf.ID, req.AssetInf.TypeString);
                return;
            }

            //MainConsole.Instance.Debug("sending asset " + req.RequestAssetID);
            TransferInfoPacket Transfer = new TransferInfoPacket
                                              {
                                                  TransferInfo =
                                                      {
                                                          ChannelType = (int) ChannelType.Asset,
                                                          Status = (int) TransferPacketStatus.MorePacketsToCome,
                                                          TargetType = 0
                                                      }
                                              };
            if (req.AssetRequestSource == 2)
            {
                Transfer.TransferInfo.Params = new byte[20];
                Array.Copy(req.RequestAssetID.GetBytes(), 0, Transfer.TransferInfo.Params, 0, 16);
                int assType = req.AssetInf.Type;
                Array.Copy(Utils.IntToBytes(assType), 0, Transfer.TransferInfo.Params, 16, 4);
            }
            else if (req.AssetRequestSource == 3)
            {
                Transfer.TransferInfo.Params = req.Params;
                // Transfer.TransferInfo.Params = new byte[100];
                //Array.Copy(req.RequestUser.AgentId.GetBytes(), 0, Transfer.TransferInfo.Params, 0, 16);
                //Array.Copy(req.RequestUser.SessionId.GetBytes(), 0, Transfer.TransferInfo.Params, 16, 16);
            }
            Transfer.TransferInfo.Size = req.AssetInf.Data.Length;
            Transfer.TransferInfo.TransferID = req.TransferRequestID;
            Transfer.Header.Zerocoded = true;
            OutPacket(Transfer, ThrottleOutPacketType.Transfer);

            if (req.NumPackets == 1)
            {
                TransferPacketPacket TransferPacket = new TransferPacketPacket
                                                          {
                                                              TransferData =
                                                                  {
                                                                      Packet = 0,
                                                                      ChannelType = (int) ChannelType.Asset,
                                                                      TransferID = req.TransferRequestID,
                                                                      Data = req.AssetInf.Data,
                                                                      Status = (int) TransferPacketStatus.Done
                                                                  },
                                                              Header = {Zerocoded = true}
                                                          };
                OutPacket(TransferPacket, ThrottleOutPacketType.Transfer);
            }
            else
            {
                int processedLength = 0;
                const int maxChunkSize = 1024;
                int packetNumber = 0;
                const int firstPacketSize = 600;

                while (processedLength < req.AssetInf.Data.Length)
                {
                    TransferPacketPacket TransferPacket = new TransferPacketPacket
                                                              {
                                                                  TransferData =
                                                                      {
                                                                          Packet = packetNumber,
                                                                          ChannelType = (int) ChannelType.Asset,
                                                                          TransferID = req.TransferRequestID
                                                                      }
                                                              };

                    int chunkSize = Math.Min(req.AssetInf.Data.Length - processedLength,
                                             packetNumber == 0 ? firstPacketSize : maxChunkSize);

                    byte[] chunk = new byte[chunkSize];
                    Array.Copy(req.AssetInf.Data, processedLength, chunk, 0, chunk.Length);
                    TransferPacket.TransferData.Data = chunk;

                    processedLength += chunkSize;
                    // 0 indicates more packets to come, 1 indicates last packet
                    if (req.AssetInf.Data.Length - processedLength == 0)
                    {
                        TransferPacket.TransferData.Status = (int) TransferPacketStatus.Done;
                    }
                    else
                    {
                        TransferPacket.TransferData.Status = (int) TransferPacketStatus.MorePacketsToCome;
                    }
                    TransferPacket.Header.Zerocoded = true;
                    OutPacket(TransferPacket, ThrottleOutPacketType.Transfer);

                    packetNumber++;
                }
            }
        }

        public void SendRegionHandle(UUID regionID, ulong handle)
        {
            RegionIDAndHandleReplyPacket reply =
                (RegionIDAndHandleReplyPacket) PacketPool.Instance.GetPacket(PacketType.RegionIDAndHandleReply);
            reply.ReplyBlock.RegionID = regionID;
            reply.ReplyBlock.RegionHandle = handle;
            OutPacket(reply, ThrottleOutPacketType.Land);
        }

        public void SendParcelInfo(LandData land, UUID parcelID, uint x, uint y, string SimName)
        {
            ParcelInfoReplyPacket reply =
                (ParcelInfoReplyPacket) PacketPool.Instance.GetPacket(PacketType.ParcelInfoReply);
            reply.AgentData.AgentID = m_agentId;
            reply.Data.ParcelID = parcelID;
            reply.Data.OwnerID = land.OwnerID;
            reply.Data.Name = Utils.StringToBytes(land.Name);
            reply.Data.Desc = Utils.StringToBytes(land.Description);
            reply.Data.ActualArea = land.Area;
            reply.Data.BillableArea = land.Area; // TODO: what is this?

            // Bit 0: Mature, bit 7: on sale, other bits: no idea
            reply.Data.Flags = (byte) (
                                          land.Maturity > 0
                                              ? (1 << 0)
                                              : 0 +
                                                ((land.Flags & (uint) ParcelFlags.ForSale) != 0 ? (1 << 7) : 0));

            Vector3 pos = land.UserLocation;
            if (pos.Equals(Vector3.Zero))
            {
                pos = (land.AABBMax + land.AABBMin)*0.5f;
            }
            reply.Data.GlobalX = x;
            reply.Data.GlobalY = y;
            reply.Data.GlobalZ = pos.Z;
            reply.Data.SimName = Utils.StringToBytes(SimName);
            reply.Data.SnapshotID = land.SnapshotID;
            reply.Data.Dwell = land.Dwell;
            reply.Data.SalePrice = land.SalePrice;
            reply.Data.AuctionID = (int) land.AuctionID;

            OutPacket(reply, ThrottleOutPacketType.Land);
        }

        public void SendScriptTeleportRequest(string objName, string simName, Vector3 pos, Vector3 lookAt)
        {
            ScriptTeleportRequestPacket packet =
                (ScriptTeleportRequestPacket) PacketPool.Instance.GetPacket(PacketType.ScriptTeleportRequest);

            packet.Data.ObjectName = Utils.StringToBytes(objName);
            packet.Data.SimName = Utils.StringToBytes(simName);
            packet.Data.SimPosition = pos;
            packet.Data.LookAt = lookAt;

            OutPacket(packet, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendDirPlacesReply(UUID queryID, DirPlacesReplyData[] data)
        {
            DirPlacesReplyPacket packet =
                (DirPlacesReplyPacket) PacketPool.Instance.GetPacket(PacketType.DirPlacesReply);

            packet.AgentData = new DirPlacesReplyPacket.AgentDataBlock();

            packet.QueryData = new DirPlacesReplyPacket.QueryDataBlock[1];
            packet.QueryData[0] = new DirPlacesReplyPacket.QueryDataBlock();

            packet.AgentData.AgentID = AgentId;

            packet.QueryData[0].QueryID = queryID;

            DirPlacesReplyPacket.QueryRepliesBlock[] replies =
                new DirPlacesReplyPacket.QueryRepliesBlock[0];
            DirPlacesReplyPacket.StatusDataBlock[] status =
                new DirPlacesReplyPacket.StatusDataBlock[0];

            packet.QueryReplies = replies;
            packet.StatusData = status;

            foreach (DirPlacesReplyData d in data)
            {
                int idx = replies.Length;
                Array.Resize(ref replies, idx + 1);
                Array.Resize(ref status, idx + 1);

                replies[idx] = new DirPlacesReplyPacket.QueryRepliesBlock();
                status[idx] = new DirPlacesReplyPacket.StatusDataBlock();
                replies[idx].ParcelID = d.parcelID;
                replies[idx].Name = Utils.StringToBytes(d.name);
                replies[idx].ForSale = d.forSale;
                replies[idx].Auction = d.auction;
                replies[idx].Dwell = d.dwell;
                status[idx].Status = d.Status;

                packet.QueryReplies = replies;
                packet.StatusData = status;

                if (packet.Length >= 1000)
                {
                    OutPacket(packet, ThrottleOutPacketType.AvatarInfo);

                    packet = (DirPlacesReplyPacket) PacketPool.Instance.GetPacket(PacketType.DirPlacesReply);

                    packet.AgentData = new DirPlacesReplyPacket.AgentDataBlock();

                    packet.QueryData = new DirPlacesReplyPacket.QueryDataBlock[1];
                    packet.QueryData[0] = new DirPlacesReplyPacket.QueryDataBlock();

                    packet.AgentData.AgentID = AgentId;

                    packet.QueryData[0].QueryID = queryID;

                    replies = new DirPlacesReplyPacket.QueryRepliesBlock[0];
                    status = new DirPlacesReplyPacket.StatusDataBlock[0];
                }
            }

            packet.HasVariableBlocks = false;
            if (replies.Length > 0 || data.Length == 0)
                OutPacket(packet, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendDirPeopleReply(UUID queryID, DirPeopleReplyData[] data)
        {
            DirPeopleReplyPacket packet =
                (DirPeopleReplyPacket) PacketPool.Instance.GetPacket(PacketType.DirPeopleReply);

            packet.AgentData = new DirPeopleReplyPacket.AgentDataBlock {AgentID = AgentId};

            packet.QueryData = new DirPeopleReplyPacket.QueryDataBlock {QueryID = queryID};

            packet.QueryReplies = new DirPeopleReplyPacket.QueryRepliesBlock[
                data.Length];

            int i = 0;
            foreach (DirPeopleReplyData d in data)
            {
                packet.QueryReplies[i] = new DirPeopleReplyPacket.QueryRepliesBlock
                                             {
                                                 AgentID = d.agentID,
                                                 FirstName = Utils.StringToBytes(d.firstName),
                                                 LastName = Utils.StringToBytes(d.lastName),
                                                 Group = Utils.StringToBytes(d.group),
                                                 Online = d.online,
                                                 Reputation = d.reputation
                                             };
                i++;
            }

            packet.HasVariableBlocks = false;
            OutPacket(packet, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendDirEventsReply(UUID queryID, DirEventsReplyData[] data)
        {
            DirEventsReplyPacket packet =
                (DirEventsReplyPacket) PacketPool.Instance.GetPacket(PacketType.DirEventsReply);

            packet.AgentData = new DirEventsReplyPacket.AgentDataBlock {AgentID = AgentId};

            packet.QueryData = new DirEventsReplyPacket.QueryDataBlock {QueryID = queryID};

            packet.QueryReplies = new DirEventsReplyPacket.QueryRepliesBlock[
                data.Length];

            packet.StatusData = new DirEventsReplyPacket.StatusDataBlock[
                data.Length];

            int i = 0;
            foreach (DirEventsReplyData d in data)
            {
                packet.QueryReplies[i] = new DirEventsReplyPacket.QueryRepliesBlock();
                packet.StatusData[i] = new DirEventsReplyPacket.StatusDataBlock();
                packet.QueryReplies[i].OwnerID = d.ownerID;
                packet.QueryReplies[i].Name =
                    Utils.StringToBytes(d.name);
                packet.QueryReplies[i].EventID = d.eventID;
                packet.QueryReplies[i].Date =
                    Utils.StringToBytes(d.date);
                packet.QueryReplies[i].UnixTime = d.unixTime;
                packet.QueryReplies[i].EventFlags = d.eventFlags;
                packet.StatusData[i].Status = d.Status;
                i++;
            }

            packet.HasVariableBlocks = false;
            OutPacket(packet, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendDirGroupsReply(UUID queryID, DirGroupsReplyData[] data)
        {
            DirGroupsReplyPacket packet =
                (DirGroupsReplyPacket) PacketPool.Instance.GetPacket(PacketType.DirGroupsReply);

            packet.AgentData = new DirGroupsReplyPacket.AgentDataBlock {AgentID = AgentId};

            packet.QueryData = new DirGroupsReplyPacket.QueryDataBlock {QueryID = queryID};

            packet.QueryReplies = new DirGroupsReplyPacket.QueryRepliesBlock[
                data.Length];

            int i = 0;
            foreach (DirGroupsReplyData d in data)
            {
                packet.QueryReplies[i] = new DirGroupsReplyPacket.QueryRepliesBlock
                                             {
                                                 GroupID = d.groupID,
                                                 GroupName = Utils.StringToBytes(d.groupName),
                                                 Members = d.members,
                                                 SearchOrder = d.searchOrder
                                             };
                i++;
            }

            packet.HasVariableBlocks = false;
            OutPacket(packet, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendDirClassifiedReply(UUID queryID, DirClassifiedReplyData[] data)
        {
            DirClassifiedReplyPacket packet =
                (DirClassifiedReplyPacket) PacketPool.Instance.GetPacket(PacketType.DirClassifiedReply);

            packet.AgentData = new DirClassifiedReplyPacket.AgentDataBlock {AgentID = AgentId};

            packet.QueryData = new DirClassifiedReplyPacket.QueryDataBlock {QueryID = queryID};

            packet.QueryReplies = new DirClassifiedReplyPacket.QueryRepliesBlock[
                data.Length];
            packet.StatusData = new DirClassifiedReplyPacket.StatusDataBlock[
                data.Length];

            int i = 0;
            foreach (DirClassifiedReplyData d in data)
            {
                packet.QueryReplies[i] = new DirClassifiedReplyPacket.QueryRepliesBlock();
                packet.StatusData[i] = new DirClassifiedReplyPacket.StatusDataBlock();
                packet.QueryReplies[i].ClassifiedID = d.classifiedID;
                packet.QueryReplies[i].Name =
                    Utils.StringToBytes(d.name);
                packet.QueryReplies[i].ClassifiedFlags = d.classifiedFlags;
                packet.QueryReplies[i].CreationDate = d.creationDate;
                packet.QueryReplies[i].ExpirationDate = d.expirationDate;
                packet.QueryReplies[i].PriceForListing = d.price;
                packet.StatusData[i].Status = d.Status;
                i++;
            }

            packet.HasVariableBlocks = false;
            OutPacket(packet, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendDirLandReply(UUID queryID, DirLandReplyData[] data)
        {
            DirLandReplyPacket packet = (DirLandReplyPacket) PacketPool.Instance.GetPacket(PacketType.DirLandReply);

            packet.AgentData = new DirLandReplyPacket.AgentDataBlock {AgentID = AgentId};

            packet.QueryData = new DirLandReplyPacket.QueryDataBlock {QueryID = queryID};

            packet.QueryReplies = new DirLandReplyPacket.QueryRepliesBlock[
                data.Length];

            int i = 0;
            foreach (DirLandReplyData d in data)
            {
                packet.QueryReplies[i] = new DirLandReplyPacket.QueryRepliesBlock
                                             {
                                                 ParcelID = d.parcelID,
                                                 Name = Utils.StringToBytes(d.name),
                                                 Auction = d.auction,
                                                 ForSale = d.forSale,
                                                 SalePrice = d.salePrice,
                                                 ActualArea = d.actualArea
                                             };
                i++;
            }

            packet.HasVariableBlocks = false;
            OutPacket(packet, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendDirPopularReply(UUID queryID, DirPopularReplyData[] data)
        {
            DirPopularReplyPacket packet =
                (DirPopularReplyPacket) PacketPool.Instance.GetPacket(PacketType.DirPopularReply);

            packet.AgentData = new DirPopularReplyPacket.AgentDataBlock {AgentID = AgentId};

            packet.QueryData = new DirPopularReplyPacket.QueryDataBlock {QueryID = queryID};

            packet.QueryReplies = new DirPopularReplyPacket.QueryRepliesBlock[
                data.Length];

            int i = 0;
            foreach (DirPopularReplyData d in data)
            {
                packet.QueryReplies[i] = new DirPopularReplyPacket.QueryRepliesBlock
                                             {
                                                 ParcelID = d.ParcelID,
                                                 Name = Utils.StringToBytes(d.Name),
                                                 Dwell = d.Dwell
                                             };
                i++;
            }

            packet.HasVariableBlocks = false;
            OutPacket(packet, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendEventInfoReply(EventData data)
        {
            EventInfoReplyPacket packet =
                (EventInfoReplyPacket) PacketPool.Instance.GetPacket(PacketType.EventInfoReply);

            packet.AgentData = new EventInfoReplyPacket.AgentDataBlock {AgentID = AgentId};

            packet.EventData = new EventInfoReplyPacket.EventDataBlock
                                   {
                                       EventID = data.eventID,
                                       Creator = Utils.StringToBytes(data.creator),
                                       Name = Utils.StringToBytes(data.name),
                                       Category = Utils.StringToBytes(data.category),
                                       Desc = Utils.StringToBytes(data.description),
                                       Date = Utils.StringToBytes(data.date),
                                       DateUTC = data.dateUTC,
                                       Duration = data.duration,
                                       Cover = data.cover,
                                       Amount = data.amount,
                                       SimName = Utils.StringToBytes(data.simName),
                                       GlobalPos = new Vector3d(data.globalPos),
                                       EventFlags = data.eventFlags
                                   };

            OutPacket(packet, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendMapItemReply(mapItemReply[] replies, uint mapitemtype, uint flags)
        {
            MapItemReplyPacket mirplk = new MapItemReplyPacket
                                            {
                                                AgentData = {AgentID = AgentId},
                                                RequestData = {ItemType = mapitemtype},
                                                Data = new MapItemReplyPacket.DataBlock[replies.Length]
                                            };
            for (int i = 0; i < replies.Length; i++)
            {
                MapItemReplyPacket.DataBlock mrdata = new MapItemReplyPacket.DataBlock
                                                          {
                                                              X = replies[i].x,
                                                              Y = replies[i].y,
                                                              ID = replies[i].id,
                                                              Extra = replies[i].Extra,
                                                              Extra2 = replies[i].Extra2,
                                                              Name = Utils.StringToBytes(replies[i].name)
                                                          };
                mirplk.Data[i] = mrdata;
            }
            //MainConsole.Instance.Debug(mirplk.ToString());
            OutPacket(mirplk, ThrottleOutPacketType.Land);
        }

        public void SendOfferCallingCard(UUID srcID, UUID transactionID)
        {
            // a bit special, as this uses AgentID to store the source instead
            // of the destination. The destination (the receiver) goes into destID
            OfferCallingCardPacket p =
                (OfferCallingCardPacket) PacketPool.Instance.GetPacket(PacketType.OfferCallingCard);
            p.AgentData.AgentID = srcID;
            p.AgentData.SessionID = UUID.Zero;
            p.AgentBlock.DestID = AgentId;
            p.AgentBlock.TransactionID = transactionID;
            OutPacket(p, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendAcceptCallingCard(UUID transactionID)
        {
            AcceptCallingCardPacket p =
                (AcceptCallingCardPacket) PacketPool.Instance.GetPacket(PacketType.AcceptCallingCard);
            p.AgentData.AgentID = AgentId;
            p.AgentData.SessionID = UUID.Zero;
            p.FolderData = new AcceptCallingCardPacket.FolderDataBlock[1];
            p.FolderData[0] = new AcceptCallingCardPacket.FolderDataBlock {FolderID = UUID.Zero};
            OutPacket(p, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendDeclineCallingCard(UUID transactionID)
        {
            DeclineCallingCardPacket p =
                (DeclineCallingCardPacket) PacketPool.Instance.GetPacket(PacketType.DeclineCallingCard);
            p.AgentData.AgentID = AgentId;
            p.AgentData.SessionID = UUID.Zero;
            p.TransactionBlock.TransactionID = transactionID;
            OutPacket(p, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendTerminateFriend(UUID exFriendID)
        {
            TerminateFriendshipPacket p =
                (TerminateFriendshipPacket) PacketPool.Instance.GetPacket(PacketType.TerminateFriendship);
            p.AgentData.AgentID = AgentId;
            p.AgentData.SessionID = SessionId;
            p.ExBlock.OtherID = exFriendID;
            OutPacket(p, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendAvatarGroupsReply(UUID avatarID, GroupMembershipData[] data)
        {
            OSDMap llsd = new OSDMap(3);
            OSDArray AgentData = new OSDArray(1);
            OSDMap AgentDataMap = new OSDMap(1)
                                      {{"AgentID", OSD.FromUUID(AgentId)}, {"AvatarID", OSD.FromUUID(avatarID)}};
            AgentData.Add(AgentDataMap);
            llsd.Add("AgentData", AgentData);
            OSDArray GroupData = new OSDArray(data.Length);
            OSDArray NewGroupData = new OSDArray(data.Length);
            foreach (GroupMembershipData m in data)
            {
                OSDMap GroupDataMap = new OSDMap(6);
                OSDMap NewGroupDataMap = new OSDMap(1);
                GroupDataMap.Add("GroupPowers", OSD.FromULong(m.GroupPowers));
                GroupDataMap.Add("AcceptNotices", OSD.FromBoolean(m.AcceptNotices));
                GroupDataMap.Add("GroupTitle", OSD.FromString(m.GroupTitle));
                GroupDataMap.Add("GroupID", OSD.FromUUID(m.GroupID));
                GroupDataMap.Add("GroupName", OSD.FromString(m.GroupName));
                GroupDataMap.Add("GroupInsigniaID", OSD.FromUUID(m.GroupPicture));
                NewGroupDataMap.Add("ListInProfile", OSD.FromBoolean(m.ListInProfile));
                GroupData.Add(GroupDataMap);
                NewGroupData.Add(NewGroupDataMap);
            }
            llsd.Add("GroupData", GroupData);
            llsd.Add("NewGroupData", NewGroupData);

            IEventQueueService eq = Scene.RequestModuleInterface<IEventQueueService>();
            if (eq != null)
            {
                eq.Enqueue(BuildEvent("AvatarGroupsReply", llsd), AgentId, Scene.RegionInfo.RegionHandle);
            }
        }

        public void SendJoinGroupReply(UUID groupID, bool success)
        {
            JoinGroupReplyPacket p = (JoinGroupReplyPacket) PacketPool.Instance.GetPacket(PacketType.JoinGroupReply);

            p.AgentData = new JoinGroupReplyPacket.AgentDataBlock {AgentID = AgentId};

            p.GroupData = new JoinGroupReplyPacket.GroupDataBlock {GroupID = groupID, Success = success};

            OutPacket(p, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendEjectGroupMemberReply(UUID agentID, UUID groupID, bool success)
        {
            EjectGroupMemberReplyPacket p =
                (EjectGroupMemberReplyPacket) PacketPool.Instance.GetPacket(PacketType.EjectGroupMemberReply);

            p.AgentData = new EjectGroupMemberReplyPacket.AgentDataBlock {AgentID = agentID};

            p.GroupData = new EjectGroupMemberReplyPacket.GroupDataBlock {GroupID = groupID};

            p.EjectData = new EjectGroupMemberReplyPacket.EjectDataBlock {Success = success};

            OutPacket(p, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendLeaveGroupReply(UUID groupID, bool success)
        {
            LeaveGroupReplyPacket p = (LeaveGroupReplyPacket) PacketPool.Instance.GetPacket(PacketType.LeaveGroupReply);

            p.AgentData = new LeaveGroupReplyPacket.AgentDataBlock {AgentID = AgentId};

            p.GroupData = new LeaveGroupReplyPacket.GroupDataBlock {GroupID = groupID, Success = success};

            OutPacket(p, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendAvatarClassifiedReply(UUID targetID, UUID[] classifiedID, string[] name)
        {
            if (classifiedID.Length != name.Length)
                return;

            AvatarClassifiedReplyPacket ac =
                (AvatarClassifiedReplyPacket) PacketPool.Instance.GetPacket(
                    PacketType.AvatarClassifiedReply);

            ac.AgentData = new AvatarClassifiedReplyPacket.AgentDataBlock {AgentID = AgentId, TargetID = targetID};

            ac.Data = new AvatarClassifiedReplyPacket.DataBlock[classifiedID.Length];
            int i;
            for (i = 0; i < classifiedID.Length; i++)
            {
                ac.Data[i].ClassifiedID = classifiedID[i];
                ac.Data[i].Name = Utils.StringToBytes(name[i]);
            }

            OutPacket(ac, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendClassifiedInfoReply(UUID classifiedID, UUID creatorID, uint creationDate, uint expirationDate,
                                            uint category, string name, string description, UUID parcelID,
                                            uint parentEstate, UUID snapshotID, string simName, Vector3 globalPos,
                                            string parcelName, byte classifiedFlags, int price)
        {
            ClassifiedInfoReplyPacket cr =
                (ClassifiedInfoReplyPacket) PacketPool.Instance.GetPacket(
                    PacketType.ClassifiedInfoReply);

            cr.AgentData = new ClassifiedInfoReplyPacket.AgentDataBlock {AgentID = AgentId};

            cr.Data = new ClassifiedInfoReplyPacket.DataBlock
                          {
                              ClassifiedID = classifiedID,
                              CreatorID = creatorID,
                              CreationDate = creationDate,
                              ExpirationDate = expirationDate,
                              Category = category,
                              Name = Utils.StringToBytes(name),
                              Desc = Utils.StringToBytes(description),
                              ParcelID = parcelID,
                              ParentEstate = parentEstate,
                              SnapshotID = snapshotID,
                              SimName = Utils.StringToBytes(simName),
                              PosGlobal = new Vector3d(globalPos),
                              ParcelName = Utils.StringToBytes(parcelName),
                              ClassifiedFlags = classifiedFlags,
                              PriceForListing = price
                          };

            OutPacket(cr, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendAgentDropGroup(UUID groupID)
        {
            AgentDropGroupPacket dg =
                (AgentDropGroupPacket) PacketPool.Instance.GetPacket(
                    PacketType.AgentDropGroup);

            dg.AgentData = new AgentDropGroupPacket.AgentDataBlock {AgentID = AgentId, GroupID = groupID};

            OutPacket(dg, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendAvatarNotesReply(UUID targetID, string text)
        {
            AvatarNotesReplyPacket an =
                (AvatarNotesReplyPacket) PacketPool.Instance.GetPacket(
                    PacketType.AvatarNotesReply);

            an.AgentData = new AvatarNotesReplyPacket.AgentDataBlock {AgentID = AgentId};

            an.Data = new AvatarNotesReplyPacket.DataBlock {TargetID = targetID, Notes = Utils.StringToBytes(text)};

            OutPacket(an, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendAvatarPicksReply(UUID targetID, Dictionary<UUID, string> picks)
        {
            AvatarPicksReplyPacket ap =
                (AvatarPicksReplyPacket) PacketPool.Instance.GetPacket(
                    PacketType.AvatarPicksReply);

            ap.AgentData = new AvatarPicksReplyPacket.AgentDataBlock {AgentID = AgentId, TargetID = targetID};

            ap.Data = new AvatarPicksReplyPacket.DataBlock[picks.Count];

            int i = 0;
            foreach (KeyValuePair<UUID, string> pick in picks)
            {
                ap.Data[i] = new AvatarPicksReplyPacket.DataBlock
                                 {PickID = pick.Key, PickName = Utils.StringToBytes(pick.Value)};
                i++;
            }

            OutPacket(ap, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendAvatarClassifiedReply(UUID targetID, Dictionary<UUID, string> classifieds)
        {
            AvatarClassifiedReplyPacket ac =
                (AvatarClassifiedReplyPacket) PacketPool.Instance.GetPacket(
                    PacketType.AvatarClassifiedReply);

            ac.AgentData = new AvatarClassifiedReplyPacket.AgentDataBlock {AgentID = AgentId, TargetID = targetID};

            ac.Data = new AvatarClassifiedReplyPacket.DataBlock[classifieds.Count];

            int i = 0;
            foreach (KeyValuePair<UUID, string> classified in classifieds)
            {
                ac.Data[i] = new AvatarClassifiedReplyPacket.DataBlock
                                 {ClassifiedID = classified.Key, Name = Utils.StringToBytes(classified.Value)};
                i++;
            }

            OutPacket(ac, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendParcelDwellReply(int localID, UUID parcelID, float dwell)
        {
            ParcelDwellReplyPacket pd =
                (ParcelDwellReplyPacket) PacketPool.Instance.GetPacket(
                    PacketType.ParcelDwellReply);

            pd.AgentData = new ParcelDwellReplyPacket.AgentDataBlock {AgentID = AgentId};

            pd.Data = new ParcelDwellReplyPacket.DataBlock {LocalID = localID, ParcelID = parcelID, Dwell = dwell};

            OutPacket(pd, ThrottleOutPacketType.Land);
        }

        public void SendUserInfoReply(bool imViaEmail, bool visible, string email)
        {
            UserInfoReplyPacket ur =
                (UserInfoReplyPacket) PacketPool.Instance.GetPacket(
                    PacketType.UserInfoReply);

            string Visible = "hidden";
            if (visible)
                Visible = "default";

            ur.AgentData = new UserInfoReplyPacket.AgentDataBlock {AgentID = AgentId};

            ur.UserData = new UserInfoReplyPacket.UserDataBlock
                              {
                                  IMViaEMail = imViaEmail,
                                  DirectoryVisibility = Utils.StringToBytes(Visible),
                                  EMail = Utils.StringToBytes(email)
                              };

            OutPacket(ur, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendCreateGroupReply(UUID groupID, bool success, string message)
        {
            CreateGroupReplyPacket createGroupReply =
                (CreateGroupReplyPacket) PacketPool.Instance.GetPacket(PacketType.CreateGroupReply);

            createGroupReply.AgentData =
                new CreateGroupReplyPacket.AgentDataBlock();
            createGroupReply.ReplyData =
                new CreateGroupReplyPacket.ReplyDataBlock();

            createGroupReply.AgentData.AgentID = AgentId;
            createGroupReply.ReplyData.GroupID = groupID;

            createGroupReply.ReplyData.Success = success;
            createGroupReply.ReplyData.Message = Utils.StringToBytes(message);
            OutPacket(createGroupReply, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendUseCachedMuteList()
        {
            UseCachedMuteListPacket useCachedMuteList =
                (UseCachedMuteListPacket) PacketPool.Instance.GetPacket(PacketType.UseCachedMuteList);

            useCachedMuteList.AgentData = new UseCachedMuteListPacket.AgentDataBlock {AgentID = AgentId};

            OutPacket(useCachedMuteList, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendMuteListUpdate(string filename)
        {
            MuteListUpdatePacket muteListUpdate =
                (MuteListUpdatePacket) PacketPool.Instance.GetPacket(PacketType.MuteListUpdate);

            muteListUpdate.MuteData = new MuteListUpdatePacket.MuteDataBlock
                                          {AgentID = AgentId, Filename = Utils.StringToBytes(filename)};

            OutPacket(muteListUpdate, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendPickInfoReply(UUID pickID, UUID creatorID, bool topPick, UUID parcelID, string name, string desc,
                                      UUID snapshotID, string user, string originalName, string simName,
                                      Vector3 posGlobal, int sortOrder, bool enabled)
        {
            PickInfoReplyPacket pickInfoReply =
                (PickInfoReplyPacket) PacketPool.Instance.GetPacket(PacketType.PickInfoReply);

            pickInfoReply.AgentData = new PickInfoReplyPacket.AgentDataBlock {AgentID = AgentId};

            pickInfoReply.Data = new PickInfoReplyPacket.DataBlock
                                     {
                                         PickID = pickID,
                                         CreatorID = creatorID,
                                         TopPick = topPick,
                                         ParcelID = parcelID,
                                         Name = Utils.StringToBytes(name),
                                         Desc = Utils.StringToBytes(desc),
                                         SnapshotID = snapshotID,
                                         User = Utils.StringToBytes(user),
                                         OriginalName = Utils.StringToBytes(originalName),
                                         SimName = Utils.StringToBytes(simName),
                                         PosGlobal = new Vector3d(posGlobal),
                                         SortOrder = sortOrder,
                                         Enabled = enabled
                                     };

            OutPacket(pickInfoReply, ThrottleOutPacketType.AvatarInfo);
        }

        #endregion Scene/Avatar to Client

        #region Appearance/ Wearables Methods

        public void SendWearables(AvatarWearable[] wearables, int serial)
        {
            AgentWearablesUpdatePacket aw =
                (AgentWearablesUpdatePacket) PacketPool.Instance.GetPacket(PacketType.AgentWearablesUpdate);
            aw.AgentData.AgentID = AgentId;
            aw.AgentData.SerialNum = (uint) serial;
            aw.AgentData.SessionID = m_sessionId;

#if (!ISWIN)
            int count = 0;
            foreach (AvatarWearable t in wearables)
                count += t.Count;
#else
            int count = wearables.Sum(t => t.Count);
#endif

            // TODO: don't create new blocks if recycling an old packet
            aw.WearableData = new AgentWearablesUpdatePacket.WearableDataBlock[count];
            int idx = 0;
            for (int i = 0; i < wearables.Length; i++)
            {
                for (int j = 0; j < wearables[i].Count; j++)
                {
                    AgentWearablesUpdatePacket.WearableDataBlock awb = new AgentWearablesUpdatePacket.WearableDataBlock
                                                                           {
                                                                               WearableType = (byte) i,
                                                                               AssetID = wearables[i][j].AssetID,
                                                                               ItemID = wearables[i][j].ItemID
                                                                           };
                    aw.WearableData[idx] = awb;
                    idx++;

                    //                                MainConsole.Instance.DebugFormat(
                    //                                    "[APPEARANCE]: Sending wearable item/asset {0} {1} (index {2}) for {3}",
                    //                                    awb.ItemID, awb.AssetID, i, Name);
                }
            }

            //            OutPacket(aw, ThrottleOutPacketType.Texture);
            OutPacket(aw, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendAppearance(UUID agentID, byte[] visualParams, byte[] textureEntry)
        {
            AvatarAppearancePacket avp =
                (AvatarAppearancePacket) PacketPool.Instance.GetPacket(PacketType.AvatarAppearance);
            // TODO: don't create new blocks if recycling an old packet
            avp.VisualParam = new AvatarAppearancePacket.VisualParamBlock[218];
            avp.ObjectData.TextureEntry = textureEntry;

            for (int i = 0; i < visualParams.Length; i++)
            {
                AvatarAppearancePacket.VisualParamBlock avblock = new AvatarAppearancePacket.VisualParamBlock {ParamValue = visualParams[i]};
                avp.VisualParam[i] = avblock;
            }

            avp.Sender.IsTrial = false;
            avp.Sender.ID = agentID;
            //MainConsole.Instance.InfoFormat("[LLClientView]: Sending appearance for {0} to {1}", agentID.ToString(), AgentId.ToString());
            //            OutPacket(avp, ThrottleOutPacketType.Texture);
            OutPacket(avp, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendAnimations(AnimationGroup animations)
        {
            //MainConsole.Instance.DebugFormat("[CLIENT]: Sending animations to {0}", Name);

            AvatarAnimationPacket ani =
                (AvatarAnimationPacket) PacketPool.Instance.GetPacket(PacketType.AvatarAnimation);
            // TODO: don't create new blocks if recycling an old packet
            ani.AnimationSourceList = new AvatarAnimationPacket.AnimationSourceListBlock[animations.Animations.Length];

            ani.Sender = new AvatarAnimationPacket.SenderBlock {ID = animations.AvatarID};

            ani.AnimationList = new AvatarAnimationPacket.AnimationListBlock[animations.Animations.Length];

            ani.PhysicalAvatarEventList = new AvatarAnimationPacket.PhysicalAvatarEventListBlock[1];
            ani.PhysicalAvatarEventList[0] = new AvatarAnimationPacket.PhysicalAvatarEventListBlock
                                                 {TypeData = new byte[0]};

            for (int i = 0; i < animations.Animations.Length; ++i)
            {
                ani.AnimationList[i] = new AvatarAnimationPacket.AnimationListBlock
                                           {
                                               AnimID = animations.Animations[i],
                                               AnimSequenceID = animations.SequenceNums[i] + ((i + 1)*2)
                                           };

                ani.AnimationSourceList[i] = new AvatarAnimationPacket.AnimationSourceListBlock
                                                 {ObjectID = animations.ObjectIDs[i]};
                //if (objectIDs[i] == UUID.Zero)
                //    ani.AnimationSourceList[i].ObjectID = sourceAgentId;
            }
            //We do this here to keep the numbers under control
            m_animationSequenceNumber += (animations.Animations.Length*2);

            ani.Header.Reliable = true;
            ani.HasVariableBlocks = false;
            //            OutPacket(ani, ThrottleOutPacketType.Asset);
            OutPacket(ani, ThrottleOutPacketType.AvatarInfo, true, null,
                      delegate { m_scene.GetScenePresence(AgentId).SceneViewer.FinishedAnimationPacketSend(animations); });
        }

        #endregion

        #region Avatar Packet/Data Sending Methods

        /// <summary>
        ///   Send an ObjectUpdate packet with information about an avatar
        /// </summary>
        public void SendAvatarDataImmediate(IEntity avatar)
        {
            IScenePresence presence = avatar as IScenePresence;
            if (presence == null || presence.IsChildAgent)
                return;

            ObjectUpdatePacket objupdate = (ObjectUpdatePacket) PacketPool.Instance.GetPacket(PacketType.ObjectUpdate);
            objupdate.Header.Zerocoded = true;

            objupdate.RegionData.RegionHandle = presence.Scene.RegionInfo.RegionHandle;
            float TIME_DILATION = presence.Scene.TimeDilation;
            ushort timeDilation = Utils.FloatToUInt16(TIME_DILATION, 0.0f, 1.0f);

            objupdate.RegionData.TimeDilation = timeDilation;

            objupdate.ObjectData = new ObjectUpdatePacket.ObjectDataBlock[1];
            objupdate.ObjectData[0] = CreateAvatarUpdateBlock(presence);

            OutPacket(objupdate, ThrottleOutPacketType.OutBand);
        }

        public void SendCoarseLocationUpdate(List<UUID> users, List<Vector3> CoarseLocations)
        {
            if (!IsActive) return; // We don't need to update inactive clients.

            CoarseLocationUpdatePacket loc =
                (CoarseLocationUpdatePacket) PacketPool.Instance.GetPacket(PacketType.CoarseLocationUpdate);
            loc.Header.Reliable = false;

            // Each packet can only hold around 60 avatar positions and the client clears the mini-map each time
            // a CoarseLocationUpdate packet is received. Oh well.
            int total = Math.Min(CoarseLocations.Count, 60);

            CoarseLocationUpdatePacket.IndexBlock ib = new CoarseLocationUpdatePacket.IndexBlock();

            loc.Location = new CoarseLocationUpdatePacket.LocationBlock[total];
            loc.AgentData = new CoarseLocationUpdatePacket.AgentDataBlock[total];

            int selfindex = -1;
            for (int i = 0; i < total; i++)
            {
                CoarseLocationUpdatePacket.LocationBlock lb =
                    new CoarseLocationUpdatePacket.LocationBlock
                        {
                            X = (byte) CoarseLocations[i].X,
                            Y = (byte) CoarseLocations[i].Y,
                            Z = CoarseLocations[i].Z > 1024 ? (byte) 0 : (byte) (CoarseLocations[i].Z*0.25f)
                        };


                loc.Location[i] = lb;
                loc.AgentData[i] = new CoarseLocationUpdatePacket.AgentDataBlock {AgentID = users[i]};
                if (users[i] == AgentId)
                    selfindex = i;
            }

            ib.You = (short) selfindex;
            ib.Prey = -1;
            loc.Index = ib;

            OutPacket(loc, ThrottleOutPacketType.AvatarInfo);
        }

        #endregion Avatar Packet/Data Sending Methods

        #region Primitive Packet/Data Sending Methods

        /// <summary>
        ///   Generate one of the object update packets based on PrimUpdateFlags
        ///   and broadcast the packet to clients
        /// </summary>
        /// again  presences update preiority was lost. recovering it  fast and dirty
        public void SendAvatarUpdate(IEnumerable<EntityUpdate> updates)
        {
            Aurora.Framework.Lazy<List<ImprovedTerseObjectUpdatePacket.ObjectDataBlock>> terseUpdateBlocks =
                new Aurora.Framework.Lazy<List<ImprovedTerseObjectUpdatePacket.ObjectDataBlock>>();
            List<EntityUpdate> terseUpdates = new List<EntityUpdate>();

            foreach (EntityUpdate update in updates)
            {
                terseUpdates.Add(update);
                terseUpdateBlocks.Value.Add(CreateImprovedTerseBlock(update.Entity,
                                                                     update.Flags.HasFlag(PrimUpdateFlags.Textures)));
            }

            ushort timeDilation = Utils.FloatToUInt16(m_scene.TimeDilation, 0.0f, 1.0f);

            if (terseUpdateBlocks.IsValueCreated)
            {
                List<ImprovedTerseObjectUpdatePacket.ObjectDataBlock> blocks = terseUpdateBlocks.Value;

                ImprovedTerseObjectUpdatePacket packet = new ImprovedTerseObjectUpdatePacket
                                                             {
                                                                 RegionData =
                                                                     {
                                                                         RegionHandle = m_scene.RegionInfo.RegionHandle,
                                                                         TimeDilation = timeDilation
                                                                     },
                                                                 ObjectData =
                                                                     new ImprovedTerseObjectUpdatePacket.ObjectDataBlock
                                                                     [blocks.Count]
                                                             };

                for (int i = 0; i < blocks.Count; i++)
                    packet.ObjectData[i] = blocks[i];

#if (!ISWIN)
                OutPacket(packet, ThrottleOutPacketType.AvatarInfo, true, delegate(OutgoingPacket p)
                {
                    ResendPrimUpdates(terseUpdates, p);
                },
                delegate(OutgoingPacket p)
                {
                    IScenePresence presence = m_scene.GetScenePresence(AgentId);
                    if (presence != null)
                        presence.SceneViewer.FinishedEntityPacketSend(terseUpdates);
                });
#else
                OutPacket(packet, ThrottleOutPacketType.AvatarInfo, true,
                          p => ResendPrimUpdates(terseUpdates, p),
                          delegate
                              {
                                  IScenePresence presence = m_scene.GetScenePresence(AgentId);
                                  if (presence != null)
                                      presence.SceneViewer.FinishedEntityPacketSend(terseUpdates);
                              });
#endif
            }
        }

        public void SendPrimUpdate(IEnumerable<EntityUpdate> updates)
        {
            Aurora.Framework.Lazy<List<ObjectUpdatePacket.ObjectDataBlock>> objectUpdateBlocks =
                new Aurora.Framework.Lazy<List<ObjectUpdatePacket.ObjectDataBlock>>();
            Aurora.Framework.Lazy<List<ObjectUpdateCompressedPacket.ObjectDataBlock>> compressedUpdateBlocks =
                new Aurora.Framework.Lazy<List<ObjectUpdateCompressedPacket.ObjectDataBlock>>();
            Aurora.Framework.Lazy<List<ImprovedTerseObjectUpdatePacket.ObjectDataBlock>> terseUpdateBlocks =
                new Aurora.Framework.Lazy<List<ImprovedTerseObjectUpdatePacket.ObjectDataBlock>>();
            Aurora.Framework.Lazy<List<ObjectUpdateCachedPacket.ObjectDataBlock>> cachedUpdateBlocks =
                new Aurora.Framework.Lazy<List<ObjectUpdateCachedPacket.ObjectDataBlock>>();
            List<EntityUpdate> fullUpdates = new List<EntityUpdate>();
            List<EntityUpdate> compressedUpdates = new List<EntityUpdate>();
            List<EntityUpdate> cachedUpdates = new List<EntityUpdate>();
            List<EntityUpdate> terseUpdates = new List<EntityUpdate>();

            foreach (EntityUpdate update in updates)
            {
                IEntity entity = update.Entity;
                PrimUpdateFlags updateFlags = update.Flags;
                if (entity is ISceneChildEntity)
                {
                    ISceneChildEntity part = (ISceneChildEntity) entity;

                    if (part.ParentEntity.IsAttachment && m_disableFacelights)
                    {
                        if (part.ParentEntity.RootChild.Shape.State != (byte) AttachmentPoint.LeftHand &&
                            part.ParentEntity.RootChild.Shape.State != (byte) AttachmentPoint.RightHand)
                        {
                            part.Shape.LightEntry = false;
                        }
                    }
                }

                bool canUseCompressed = true;
                bool canUseImproved = true;
                bool canUseCached = false;
                //Not possible at the moment without more viewer work... the viewer does some odd things with this

                IObjectCache module = Scene.RequestModuleInterface<IObjectCache>();
                bool isTerse = updateFlags.HasFlag((PrimUpdateFlags.TerseUpdate)) &&
                               !updateFlags.HasFlag(PrimUpdateFlags.FullUpdate) &&
                               !updateFlags.HasFlag(PrimUpdateFlags.ForcedFullUpdate);
                // Compressed and cached object updates only make sense for LL primitives
                if (entity is ISceneChildEntity)
                {
                    // Please do not remove this unless you can demonstrate on the OpenSim mailing list that a client
                    // will never receive an update after a prim kill.  Even then, keeping the kill record may be a good
                    // safety measure.
                    //
                    // If a Linden Lab 1.23.5 client (and possibly later and earlier) receives an object update
                    // after a kill, it will keep displaying the deleted object until relog.  OpenSim currently performs
                    // updates and kills on different threads with different scheduling strategies, hence this protection.
                    //
                    // This doesn't appear to apply to child prims - a client will happily ignore these updates
                    // after the root prim has been deleted.
                    /*if (m_killRecord.Contains(entity.LocalId))
                        {
                        MainConsole.Instance.ErrorFormat(
                            "[CLIENT]: Preventing update for prim with local id {0} after client for user {1} told it was deleted. Mantis this at http://mantis.aurora-sim.org/bug_report_page.php !",
                            entity.LocalId, Name);
                        return;
                        }*/
                    ISceneChildEntity ent = (ISceneChildEntity) entity;
                    if (ent.Shape.PCode == 9 && ent.Shape.State != 0)
                    {
                        //Don't send hud attachments to other avatars except for the owner
                        byte state = ent.Shape.State;
                        if ((state == (byte) AttachmentPoint.HUDBottom ||
                             state == (byte) AttachmentPoint.HUDBottomLeft ||
                             state == (byte) AttachmentPoint.HUDBottomRight ||
                             state == (byte) AttachmentPoint.HUDCenter ||
                             state == (byte) AttachmentPoint.HUDCenter2 ||
                             state == (byte) AttachmentPoint.HUDTop ||
                             state == (byte) AttachmentPoint.HUDTopLeft ||
                             state == (byte) AttachmentPoint.HUDTopRight)
                            &&
                            ent.OwnerID != AgentId)
                            continue;
                    }
                    if (updateFlags != PrimUpdateFlags.TerseUpdate && ent.ParentEntity.SitTargetAvatar.Count > 0)
                    {
                        isTerse = false;
                        updateFlags = PrimUpdateFlags.ForcedFullUpdate;
                    }

                    if (canUseCached && !isTerse && module != null)
                        canUseCached = module.UseCachedObject(AgentId, entity.LocalId, ent.CRC);
                    else
                        //No cache module? Don't use cached then, or it won't stop sending ObjectUpdateCached even when the client requests prims
                        canUseCached = false;
                }

                if (updateFlags.HasFlag(PrimUpdateFlags.FullUpdate))
                {
                    canUseCompressed = false;
                    canUseImproved = false;
                }
                else if (updateFlags.HasFlag(PrimUpdateFlags.ForcedFullUpdate))
                {
                    //If a full update has been requested, DO THE FULL UPDATE.
                    // Don't try to get out of this.... the monster called RepeatObjectUpdateCachedFromTheServer will occur and eat all your prims!
                    canUseCached = false;
                    canUseCompressed = false;
                    canUseImproved = false;
                }
                else
                {
                    if (updateFlags.HasFlag(PrimUpdateFlags.Velocity) ||
                        updateFlags.HasFlag(PrimUpdateFlags.Acceleration) ||
                        updateFlags.HasFlag(PrimUpdateFlags.CollisionPlane) ||
                        updateFlags.HasFlag(PrimUpdateFlags.Joint) ||
                        updateFlags.HasFlag(PrimUpdateFlags.AngularVelocity))
                    {
                        canUseCompressed = false;
                    }

                    if (updateFlags.HasFlag(PrimUpdateFlags.PrimFlags) ||
                        updateFlags.HasFlag(PrimUpdateFlags.ParentID) ||
                        updateFlags.HasFlag(PrimUpdateFlags.AttachmentPoint) ||
                        updateFlags.HasFlag(PrimUpdateFlags.Shape) ||
                        updateFlags.HasFlag(PrimUpdateFlags.PrimData) ||
                        updateFlags.HasFlag(PrimUpdateFlags.Text) ||
                        updateFlags.HasFlag(PrimUpdateFlags.NameValue) ||
                        updateFlags.HasFlag(PrimUpdateFlags.ExtraData) ||
                        updateFlags.HasFlag(PrimUpdateFlags.TextureAnim) ||
                        updateFlags.HasFlag(PrimUpdateFlags.Sound) ||
                        updateFlags.HasFlag(PrimUpdateFlags.Particles) ||
                        updateFlags.HasFlag(PrimUpdateFlags.Material) ||
                        updateFlags.HasFlag(PrimUpdateFlags.ClickAction) ||
                        updateFlags.HasFlag(PrimUpdateFlags.MediaURL) ||
                        updateFlags.HasFlag(PrimUpdateFlags.Joint) ||
                        updateFlags.HasFlag(PrimUpdateFlags.FindBest))
                    {
                        canUseImproved = false;
                    }
                }

                try
                {
                    //Do NOT send cached updates for terse updates
                    //ONLY send full updates for attachments unless you want to figure out all the little screwy things with sending compressed updates and attachments
                    if (entity is ISceneChildEntity &&
                        ((ISceneChildEntity) entity).IsAttachment)
                    {
                        canUseCached = false;
                        canUseImproved = false;
                        canUseCompressed = false;
                    }

                    if (canUseCached && !isTerse)
                    {
                        cachedUpdates.Add(update);
                        cachedUpdateBlocks.Value.Add(CreatePrimCachedUpdateBlock((SceneObjectPart) entity,
                                                                                 m_agentId));
                    }
                    else if (!canUseImproved && !canUseCompressed)
                    {
                        fullUpdates.Add(update);
                        if (entity is IScenePresence)
                        {
                            objectUpdateBlocks.Value.Add(CreateAvatarUpdateBlock((IScenePresence) entity));
                        }
                        else
                        {
                            objectUpdateBlocks.Value.Add(CreatePrimUpdateBlock((SceneObjectPart) entity, m_agentId));
                        }
                    }
                    else if (!canUseImproved)
                    {
                        ISceneChildEntity cEntity = (ISceneChildEntity) entity;
                        compressedUpdates.Add(update);
                        //We are sending a compressed, which the client will save, add it to the cache
                        module.AddCachedObject(AgentId, entity.LocalId, cEntity.CRC);
                        CompressedFlags Flags = CompressedFlags.None;
                        if (updateFlags == PrimUpdateFlags.FullUpdate || updateFlags == PrimUpdateFlags.FindBest)
                        {
                            //Add the defaults
                            updateFlags = PrimUpdateFlags.None;
                        }

                        updateFlags |= PrimUpdateFlags.ClickAction;
                        updateFlags |= PrimUpdateFlags.ExtraData;
                        updateFlags |= PrimUpdateFlags.Shape;
                        updateFlags |= PrimUpdateFlags.Material;
                        updateFlags |= PrimUpdateFlags.Textures;
                        updateFlags |= PrimUpdateFlags.Rotation;
                        updateFlags |= PrimUpdateFlags.PrimFlags;
                        updateFlags |= PrimUpdateFlags.Position;
                        updateFlags |= PrimUpdateFlags.AngularVelocity;

                        //Must send these as well
                        if (cEntity.Text != "")
                            updateFlags |= PrimUpdateFlags.Text;
                        if (cEntity.AngularVelocity != Vector3.Zero)
                            updateFlags |= PrimUpdateFlags.AngularVelocity;
                        if (cEntity.TextureAnimation != null && cEntity.TextureAnimation.Length != 0)
                            updateFlags |= PrimUpdateFlags.TextureAnim;
                        if (cEntity.Sound != UUID.Zero)
                            updateFlags |= PrimUpdateFlags.Sound;
                        if (cEntity.ParticleSystem != null && cEntity.ParticleSystem.Length != 0)
                            updateFlags |= PrimUpdateFlags.Particles;
                        if (!string.IsNullOrEmpty(cEntity.MediaUrl))
                            updateFlags |= PrimUpdateFlags.MediaURL;
                        if (cEntity.ParentEntity.RootChild.IsAttachment)
                            updateFlags |= PrimUpdateFlags.AttachmentPoint;

                        //Make sure that we send this! Otherwise, the client will only see one prim
                        if (cEntity.ParentEntity != null)
                            if (cEntity.ParentEntity.ChildrenEntities().Count != 1)
                                updateFlags |= PrimUpdateFlags.ParentID;

                        if (updateFlags.HasFlag(PrimUpdateFlags.Text) && cEntity.Text == "")
                            updateFlags &= ~PrimUpdateFlags.Text; //Remove the text flag if we don't have text!

                        if (updateFlags.HasFlag(PrimUpdateFlags.AngularVelocity))
                            Flags |= CompressedFlags.HasAngularVelocity;
                        if (updateFlags.HasFlag(PrimUpdateFlags.MediaURL))
                            Flags |= CompressedFlags.MediaURL;
                        if (updateFlags.HasFlag(PrimUpdateFlags.ParentID))
                            Flags |= CompressedFlags.HasParent;
                        if (updateFlags.HasFlag(PrimUpdateFlags.Particles))
                            Flags |= CompressedFlags.HasParticles;
                        if (updateFlags.HasFlag(PrimUpdateFlags.Sound))
                            Flags |= CompressedFlags.HasSound;
                        if (updateFlags.HasFlag(PrimUpdateFlags.Text))
                            Flags |= CompressedFlags.HasText;
                        if (updateFlags.HasFlag(PrimUpdateFlags.TextureAnim))
                            Flags |= CompressedFlags.TextureAnimation;
                        if (updateFlags.HasFlag(PrimUpdateFlags.NameValue) || cEntity.IsAttachment)
                            Flags |= CompressedFlags.HasNameValues;

                        compressedUpdates.Add(update);
                        compressedUpdateBlocks.Value.Add(CreateCompressedUpdateBlock((SceneObjectPart) entity, Flags,
                                                                                     updateFlags));
                    }
                    else
                    {
                        terseUpdates.Add(update);
                        terseUpdateBlocks.Value.Add(CreateImprovedTerseBlock(entity,
                                                                             updateFlags.HasFlag(
                                                                                 PrimUpdateFlags.Textures)));
                    }
                }
                catch (Exception ex)
                {
                    MainConsole.Instance.Warn("[LLCLIENTVIEW]: Issue creating an update block " + ex);
                    return;
                }
            }

            ushort timeDilation = Utils.FloatToUInt16(m_scene.TimeDilation, 0.0f, 1.0f);

            //
            // NOTE: These packets ARE being sent as Unknown for a reason
            //        This method is ONLY being called by the SceneViewer, which is being called by
            //        the LLUDPClient, which is attempting to send these packets out, they just have to 
            //        be created. So instead of sending them as task (which puts them back in the queue),
            //        we send them out immediately, as this is on a seperate thread anyway.
            //
            // SECOND NOTE: These packets are back as Task for now... we shouldn't send them out as unknown
            //        as we cannot be sure that the UDP server is ready for us to send them, so we will
            //        requeue them... even though we probably could send them out fine.
            //

            if (objectUpdateBlocks.IsValueCreated)
            {
                List<ObjectUpdatePacket.ObjectDataBlock> blocks = objectUpdateBlocks.Value;

                ObjectUpdatePacket packet = (ObjectUpdatePacket) PacketPool.Instance.GetPacket(PacketType.ObjectUpdate);
                packet.RegionData.RegionHandle = m_scene.RegionInfo.RegionHandle;
                packet.RegionData.TimeDilation = timeDilation;
                packet.ObjectData = new ObjectUpdatePacket.ObjectDataBlock[blocks.Count];

                for (int i = 0; i < blocks.Count; i++)
                    packet.ObjectData[i] = blocks[i];


                //ObjectUpdatePacket oo = new ObjectUpdatePacket(packet.ToBytes(), ref ii);

#if (!ISWIN)
                OutPacket(packet, ThrottleOutPacketType.Task, true, delegate(OutgoingPacket p)
                {
                    ResendPrimUpdates(fullUpdates, p);
                },
                delegate(OutgoingPacket p)
                {
                    IScenePresence presence = m_scene.GetScenePresence(AgentId);
                    if (presence != null)
                        presence.SceneViewer.FinishedEntityPacketSend(fullUpdates);
                });
#else
                OutPacket(packet, ThrottleOutPacketType.Task, true,
                          p => ResendPrimUpdates(fullUpdates, p),
                          delegate
                              {
                                  IScenePresence presence = m_scene.GetScenePresence(AgentId);
                                  if (presence != null)
                                      presence.SceneViewer.FinishedEntityPacketSend(fullUpdates);
                              });
#endif
            }

            if (compressedUpdateBlocks.IsValueCreated)
            {
                List<ObjectUpdateCompressedPacket.ObjectDataBlock> blocks = compressedUpdateBlocks.Value;

                ObjectUpdateCompressedPacket packet =
                    (ObjectUpdateCompressedPacket) PacketPool.Instance.GetPacket(PacketType.ObjectUpdateCompressed);
                packet.RegionData.RegionHandle = m_scene.RegionInfo.RegionHandle;
                packet.RegionData.TimeDilation = timeDilation;
                packet.ObjectData = new ObjectUpdateCompressedPacket.ObjectDataBlock[blocks.Count];
                packet.Type = PacketType.ObjectUpdate;

                for (int i = 0; i < blocks.Count; i++)
                    packet.ObjectData[i] = blocks[i];

#if (!ISWIN)
                OutPacket(packet, ThrottleOutPacketType.Task, true, delegate(OutgoingPacket p)
                {
                    ResendPrimUpdates(compressedUpdates, p);
                },
                delegate(OutgoingPacket p)
                {
                    IScenePresence presence = m_scene.GetScenePresence(AgentId);
                    if (presence != null)
                        presence.SceneViewer.FinishedEntityPacketSend(compressedUpdates);
                });
#else
                OutPacket(packet, ThrottleOutPacketType.Task, true,
                          p => ResendPrimUpdates(compressedUpdates, p),
                          delegate
                              {
                                  IScenePresence presence = m_scene.GetScenePresence(AgentId);
                                  if (presence != null)
                                      presence.SceneViewer.FinishedEntityPacketSend(compressedUpdates);
                              });
#endif
            }

            if (cachedUpdateBlocks.IsValueCreated)
            {
                List<ObjectUpdateCachedPacket.ObjectDataBlock> blocks = cachedUpdateBlocks.Value;

                ObjectUpdateCachedPacket packet =
                    (ObjectUpdateCachedPacket) PacketPool.Instance.GetPacket(PacketType.ObjectUpdateCached);
                packet.RegionData.RegionHandle = m_scene.RegionInfo.RegionHandle;
                packet.RegionData.TimeDilation = timeDilation;
                packet.ObjectData = new ObjectUpdateCachedPacket.ObjectDataBlock[blocks.Count];

                for (int i = 0; i < blocks.Count; i++)
                    packet.ObjectData[i] = blocks[i];

#if (!ISWIN)
                OutPacket(packet, ThrottleOutPacketType.Task, true, delegate(OutgoingPacket p)
                {
                    ResendPrimUpdates(cachedUpdates, p);
                },
                delegate(OutgoingPacket p)
                {
                    IScenePresence presence = m_scene.GetScenePresence(AgentId);
                    if (presence != null)
                        presence.SceneViewer.FinishedEntityPacketSend(cachedUpdates);
                });
#else
                OutPacket(packet, ThrottleOutPacketType.Task, true,
                          p => ResendPrimUpdates(cachedUpdates, p),
                          delegate
                              {
                                  IScenePresence presence = m_scene.GetScenePresence(AgentId);
                                  if (presence != null)
                                      presence.SceneViewer.FinishedEntityPacketSend(cachedUpdates);
                              });
#endif
            }

            if (terseUpdateBlocks.IsValueCreated)
            {
                List<ImprovedTerseObjectUpdatePacket.ObjectDataBlock> blocks = terseUpdateBlocks.Value;

                ImprovedTerseObjectUpdatePacket packet = new ImprovedTerseObjectUpdatePacket
                                                             {
                                                                 RegionData =
                                                                     {
                                                                         RegionHandle = m_scene.RegionInfo.RegionHandle,
                                                                         TimeDilation = timeDilation
                                                                     },
                                                                 ObjectData =
                                                                     new ImprovedTerseObjectUpdatePacket.ObjectDataBlock
                                                                     [blocks.Count]
                                                             };

                for (int i = 0; i < blocks.Count; i++)
                    packet.ObjectData[i] = blocks[i];

#if (!ISWIN)
                OutPacket(packet, ThrottleOutPacketType.Task, true, delegate(OutgoingPacket p)
                {
                    ResendPrimUpdates(terseUpdates, p);
                },
                delegate(OutgoingPacket p)
                {
                    IScenePresence presence = m_scene.GetScenePresence(AgentId);
                    if (presence != null)
                        presence.SceneViewer.FinishedEntityPacketSend(terseUpdates);
                });
#else
                OutPacket(packet, ThrottleOutPacketType.Task, true,
                          p => ResendPrimUpdates(terseUpdates, p),
                          delegate
                              {
                                  IScenePresence presence = m_scene.GetScenePresence(AgentId);
                                  if (presence != null)
                                      presence.SceneViewer.FinishedEntityPacketSend(terseUpdates);
                              });
#endif
            }
        }

        private void ResendPrimUpdates(IEnumerable<EntityUpdate> updates, OutgoingPacket oPacket)
        {
            // Remove the update packet from the list of packets waiting for acknowledgement
            // because we are requeuing the list of updates. They will be resent in new packets
            // with the most recent state and priority.
            m_udpClient.NeedAcks.Remove(oPacket.SequenceNumber);

            // Count this as a resent packet since we are going to requeue all of the updates contained in it
            Interlocked.Increment(ref m_udpClient.PacketsResent);

            IScenePresence sp = m_scene.GetScenePresence(AgentId);
            if (sp != null)
            {
                ISceneViewer viewer = sp.SceneViewer;
                foreach (EntityUpdate update in updates)
                {
                    if (update.Entity is ISceneChildEntity)
                        viewer.QueuePartForUpdate((ISceneChildEntity) update.Entity, update.Flags);
                    else
                        viewer.QueuePresenceForUpdate((IScenePresence) update.Entity, update.Flags);
                }
            }
        }

        public void DequeueUpdates(int nprimdates, int navadates)
        {
            IScenePresence sp = m_scene.GetScenePresence(AgentId);
            if (sp != null)
            {
                ISceneViewer viewer = sp.SceneViewer;
                viewer.SendPrimUpdates(nprimdates, navadates);
            }
        }

        #endregion Primitive Packet/Data Sending Methods

        private void HandleQueueEmpty(object o)
        {
            // arraytmp  0 contains current number of packets in task
            // arraytmp  1 contains current number of packets in avatarinfo
            // arraytmp  2 contains current number of packets in texture

            int[] arraytmp = (int[]) o;
            int ptmp = m_udpServer.PrimUpdatesPerCallback - arraytmp[0];
            int atmp = m_udpServer.AvatarUpdatesPerCallBack - arraytmp[1];

            if (ptmp < 0)
                ptmp = 0;
            if (atmp < 0)
                atmp = 0;

            if (ptmp + atmp != 0)
                DequeueUpdates(ptmp, atmp);

            if (m_udpServer.TextureSendLimit > arraytmp[2])
                ProcessTextureRequests(m_udpServer.TextureSendLimit);
        }

        private void ProcessTextureRequests(int numPackets)
        {
            //note: tmp is never used
            //int tmp = m_udpClient.GetCurTexPacksInQueue();
            if (m_imageManager != null)
                m_imageManager.ProcessImageQueue(numPackets);
        }

        public void SendAssetUploadCompleteMessage(sbyte AssetType, bool Success, UUID AssetFullID)
        {
            AssetUploadCompletePacket newPack = new AssetUploadCompletePacket
                                                    {
                                                        AssetBlock =
                                                            {Type = AssetType, Success = Success, UUID = AssetFullID},
                                                        Header = {Zerocoded = true}
                                                    };
            OutPacket(newPack, ThrottleOutPacketType.Asset);
        }

        public void SendXferRequest(ulong XferID, short AssetType, UUID vFileID, byte FilePath, byte[] FileName)
        {
            RequestXferPacket newPack = new RequestXferPacket
                                            {
                                                XferID =
                                                    {
                                                        ID = XferID,
                                                        VFileType = AssetType,
                                                        VFileID = vFileID,
                                                        FilePath = FilePath,
                                                        Filename = FileName
                                                    },
                                                Header = {Zerocoded = true}
                                            };
            OutPacket(newPack, ThrottleOutPacketType.Transfer);
        }

        public void SendConfirmXfer(ulong xferID, uint PacketID)
        {
            ConfirmXferPacketPacket newPack = new ConfirmXferPacketPacket
                                                  {
                                                      XferID = {ID = xferID, Packet = PacketID},
                                                      Header = {Zerocoded = true}
                                                  };
            OutPacket(newPack, ThrottleOutPacketType.Transfer);
        }

        public void SendInitiateDownload(string simFileName, string clientFileName)
        {
            InitiateDownloadPacket newPack = new InitiateDownloadPacket
                                                 {
                                                     AgentData = {AgentID = AgentId},
                                                     FileData =
                                                         {
                                                             SimFilename = Utils.StringToBytes(simFileName),
                                                             ViewerFilename = Utils.StringToBytes(clientFileName)
                                                         }
                                                 };
            OutPacket(newPack, ThrottleOutPacketType.Transfer);
        }

        public void SendImageFirstPart(
            ushort numParts, UUID ImageUUID, uint ImageSize, byte[] ImageData, byte imageCodec)
        {
            ImageDataPacket im = new ImageDataPacket
                                     {Header = {Reliable = false}, ImageID = {Packets = numParts, ID = ImageUUID}};

            if (ImageSize > 0)
                im.ImageID.Size = ImageSize;

            im.ImageData.Data = ImageData;
            im.ImageID.Codec = imageCodec;
            im.Header.Zerocoded = true;
            OutPacket(im, ThrottleOutPacketType.Texture);
        }

        public void SendImageNextPart(ushort partNumber, UUID imageUuid, byte[] imageData)
        {
            ImagePacketPacket im = new ImagePacketPacket
                                       {
                                           Header = {Reliable = false},
                                           ImageID = {Packet = partNumber, ID = imageUuid},
                                           ImageData = {Data = imageData}
                                       };

            OutPacket(im, ThrottleOutPacketType.Texture);
        }

        public void SendImageNotFound(UUID imageid)
        {
            ImageNotInDatabasePacket notFoundPacket
                = (ImageNotInDatabasePacket) PacketPool.Instance.GetPacket(PacketType.ImageNotInDatabase);

            notFoundPacket.ImageID.ID = imageid;

            OutPacket(notFoundPacket, ThrottleOutPacketType.Texture);
        }

        private volatile bool m_sendingSimStatsPacket;

        public void SendSimStats(SimStats stats)
        {
            if (m_sendingSimStatsPacket)
                return;

            m_sendingSimStatsPacket = true;

            SimStatsPacket pack = new SimStatsPacket
                                      {Region = stats.RegionBlock, Stat = stats.StatsBlock, Header = {Reliable = false}};


            OutPacket(pack, ThrottleOutPacketType.Task, true, null,
                      delegate { m_sendingSimStatsPacket = false; });
        }

        public void SendObjectPropertiesFamilyData(uint RequestFlags, UUID ObjectUUID, UUID OwnerID, UUID GroupID,
                                                   uint BaseMask, uint OwnerMask, uint GroupMask, uint EveryoneMask,
                                                   uint NextOwnerMask, int OwnershipCost, byte SaleType, int SalePrice,
                                                   uint Category,
                                                   UUID LastOwnerID, string ObjectName, string Description)
        {
            ObjectPropertiesFamilyPacket objPropFamilyPack =
                (ObjectPropertiesFamilyPacket) PacketPool.Instance.GetPacket(PacketType.ObjectPropertiesFamily);
            // TODO: don't create new blocks if recycling an old packet

            ObjectPropertiesFamilyPacket.ObjectDataBlock objPropDB = new ObjectPropertiesFamilyPacket.ObjectDataBlock
                                                                         {
                                                                             RequestFlags = RequestFlags,
                                                                             ObjectID = ObjectUUID,
                                                                             OwnerID =
                                                                                 OwnerID == GroupID
                                                                                     ? UUID.Zero
                                                                                     : OwnerID,
                                                                             GroupID = GroupID,
                                                                             BaseMask = BaseMask,
                                                                             OwnerMask = OwnerMask,
                                                                             GroupMask = GroupMask,
                                                                             EveryoneMask = EveryoneMask,
                                                                             NextOwnerMask = NextOwnerMask,
                                                                             OwnershipCost = OwnershipCost,
                                                                             SaleType = SaleType,
                                                                             SalePrice = SalePrice,
                                                                             Category = Category,
                                                                             LastOwnerID = LastOwnerID,
                                                                             Name = Util.StringToBytes256(ObjectName),
                                                                             Description =
                                                                                 Util.StringToBytes256(Description)
                                                                         };

            objPropFamilyPack.ObjectData = objPropDB;
            objPropFamilyPack.Header.Zerocoded = true;
            objPropFamilyPack.HasVariableBlocks = false;
            OutPacket(objPropFamilyPack, ThrottleOutPacketType.Task);
        }

        public void SendObjectPropertiesReply(List<IEntity> parts)
        {
            //ObjectPropertiesPacket proper = (ObjectPropertiesPacket)PacketPool.Instance.GetPacket(PacketType.ObjectProperties);
            // TODO: don't create new blocks if recycling an old packet

            //Theres automatic splitting, just let it go on through
            ObjectPropertiesPacket proper =
                (ObjectPropertiesPacket) PacketPool.Instance.GetPacket(PacketType.ObjectProperties);

#if (!ISWIN)
            List<ObjectPropertiesPacket.ObjectDataBlock> list = new List<ObjectPropertiesPacket.ObjectDataBlock>();
            foreach (IEntity part in parts)
            {
                ISceneChildEntity entity = part as ISceneChildEntity;
                if (entity != null)
                {
                    ISceneChildEntity part1 = entity as ISceneChildEntity;
                    list.Add(new ObjectPropertiesPacket.ObjectDataBlock
                                 {
                                     ItemID = part1.FromUserInventoryItemID,
                                     CreationDate = (ulong) part1.CreationDate*1000000,
                                     CreatorID = part1.CreatorID,
                                     FolderID = UUID.Zero,
                                     FromTaskID = UUID.Zero,
                                     GroupID = part1.GroupID,
                                     InventorySerial = (short) part1.InventorySerial,
                                     LastOwnerID = part1.LastOwnerID,
                                     ObjectID = part1.UUID,
                                     OwnerID = part1.OwnerID == part1.GroupID ? UUID.Zero : part1.OwnerID,
                                     TouchName = Util.StringToBytes256(part1.ParentEntity.RootChild.TouchName),
                                     TextureID = new byte[0],
                                     SitName = Util.StringToBytes256(part1.ParentEntity.RootChild.SitName),
                                     Name = Util.StringToBytes256(part1.Name),
                                     Description = Util.StringToBytes256(part1.Description),
                                     OwnerMask = part1.ParentEntity.RootChild.OwnerMask,
                                     NextOwnerMask = part1.ParentEntity.RootChild.NextOwnerMask,
                                     GroupMask = part1.ParentEntity.RootChild.GroupMask,
                                     EveryoneMask = part1.ParentEntity.RootChild.EveryoneMask,
                                     BaseMask = part1.ParentEntity.RootChild.BaseMask,
                                     SaleType = part1.ParentEntity.RootChild.ObjectSaleType,
                                     SalePrice = part1.ParentEntity.RootChild.SalePrice
                                 });
                }
            }
            proper.ObjectData = list.ToArray();
#else
            proper.ObjectData = parts.OfType<ISceneChildEntity>().Select(entity => entity as ISceneChildEntity).Select(part => new ObjectPropertiesPacket.ObjectDataBlock
                                                                                                                                   {
                                                                                                                                       ItemID = part.FromUserInventoryItemID, CreationDate = (ulong) part.CreationDate*1000000, CreatorID = part.CreatorID, FolderID = UUID.Zero, FromTaskID = UUID.Zero, GroupID = part.GroupID, InventorySerial = (short) part.InventorySerial, LastOwnerID = part.LastOwnerID, ObjectID = part.UUID, OwnerID = part.OwnerID == part.GroupID ? UUID.Zero : part.OwnerID, TouchName = Util.StringToBytes256(part.ParentEntity.RootChild.TouchName), TextureID = new byte[0], SitName = Util.StringToBytes256(part.ParentEntity.RootChild.SitName), Name = Util.StringToBytes256(part.Name), Description = Util.StringToBytes256(part.Description), OwnerMask = part.ParentEntity.RootChild.OwnerMask, NextOwnerMask = part.ParentEntity.RootChild.NextOwnerMask, GroupMask = part.ParentEntity.RootChild.GroupMask, EveryoneMask = part.ParentEntity.RootChild.EveryoneMask, BaseMask = part.ParentEntity.RootChild.BaseMask, SaleType = part.ParentEntity.RootChild.ObjectSaleType, SalePrice = part.ParentEntity.RootChild.SalePrice
                                                                                                                                   }).ToArray();
#endif

            proper.Header.Zerocoded = true;
            bool hasFinishedSending = false; //Since this packet will be split up, we only want to finish sending once
            OutPacket(proper, ThrottleOutPacketType.State, true, null, delegate
                                                                           {
                                                                               if (hasFinishedSending)
                                                                                   return;
                                                                               hasFinishedSending = true;
                                                                               m_scene.GetScenePresence(AgentId).
                                                                                   SceneViewer.
                                                                                   FinishedPropertyPacketSend(parts);
                                                                           });
        }

        #region Estate Data Sending Methods

        private static bool convertParamStringToBool(byte[] field)
        {
            string s = Utils.BytesToString(field);
            if (s == "1" || s.ToLower() == "y" || s.ToLower() == "yes" || s.ToLower() == "t" || s.ToLower() == "true")
            {
                return true;
            }
            return false;
        }

        public void SendEstateList(UUID invoice, int code, UUID[] Data, uint estateID)

        {
            EstateOwnerMessagePacket packet = new EstateOwnerMessagePacket
                                                  {
                                                      AgentData =
                                                          {
                                                              TransactionID = UUID.Random(),
                                                              AgentID = AgentId,
                                                              SessionID = SessionId
                                                          },
                                                      MethodData =
                                                          {Invoice = invoice, Method = Utils.StringToBytes("setaccess")}
                                                  };

            EstateOwnerMessagePacket.ParamListBlock[] returnblock =
                new EstateOwnerMessagePacket.ParamListBlock[6 + Data.Length];

            for (int i = 0; i < (6 + Data.Length); i++)
            {
                returnblock[i] = new EstateOwnerMessagePacket.ParamListBlock();
            }
            int j = 0;

            returnblock[j].Parameter = Utils.StringToBytes(estateID.ToString());
            j++;
            returnblock[j].Parameter = Utils.StringToBytes(code.ToString());
            j++;
            returnblock[j].Parameter = Utils.StringToBytes("0");
            j++;
            returnblock[j].Parameter = Utils.StringToBytes("0");
            j++;
            returnblock[j].Parameter = Utils.StringToBytes("0");
            j++;
            returnblock[j].Parameter = Utils.StringToBytes("0");
            j++;

            j = 2; // Agents
            if ((code & 2) != 0)
                j = 3; // Groups
            if ((code & 8) != 0)
                j = 5; // Managers

            returnblock[j].Parameter = Utils.StringToBytes(Data.Length.ToString());
            j = 6;

            for (int i = 0; i < Data.Length; i++)
            {
                returnblock[j].Parameter = Data[i].GetBytes();
                j++;
            }
            packet.ParamList = returnblock;
            packet.Header.Reliable = true;
            OutPacket(packet, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendBannedUserList(UUID invoice, EstateBan[] bl, uint estateID)
        {
            List<UUID> BannedUsers =
                (from t in bl where t != null where t.BannedUserID != UUID.Zero select t.BannedUserID).ToList();

            EstateOwnerMessagePacket packet = new EstateOwnerMessagePacket
                                                  {
                                                      AgentData =
                                                          {
                                                              TransactionID = UUID.Random(),
                                                              AgentID = AgentId,
                                                              SessionID = SessionId
                                                          },
                                                      MethodData =
                                                          {Invoice = invoice, Method = Utils.StringToBytes("setaccess")}
                                                  };

            EstateOwnerMessagePacket.ParamListBlock[] returnblock =
                new EstateOwnerMessagePacket.ParamListBlock[6 + BannedUsers.Count];

            for (int i = 0; i < (6 + BannedUsers.Count); i++)
            {
                returnblock[i] = new EstateOwnerMessagePacket.ParamListBlock();
            }
            int j = 0;

            returnblock[j].Parameter = Utils.StringToBytes(estateID.ToString());
            j++;

            returnblock[j].Parameter =
                Utils.StringToBytes(((int) EstateTools.EstateAccessReplyDelta.EstateBans).ToString());
            j++;
            returnblock[j].Parameter = Utils.StringToBytes("0");
            j++;
            returnblock[j].Parameter = Utils.StringToBytes("0");
            j++;
            returnblock[j].Parameter = Utils.StringToBytes(BannedUsers.Count.ToString());
            j++;
            returnblock[j].Parameter = Utils.StringToBytes("0");
            j++;

            foreach (UUID banned in BannedUsers)
            {
                returnblock[j].Parameter = banned.GetBytes();
                j++;
            }
            packet.ParamList = returnblock;
            packet.Header.Reliable = false;
            OutPacket(packet, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendRegionInfoToEstateMenu(RegionInfoForEstateMenuArgs args)
        {
            RegionInfoPacket rinfopack = new RegionInfoPacket();
            RegionInfoPacket.RegionInfoBlock rinfoblk = new RegionInfoPacket.RegionInfoBlock();
            rinfopack.AgentData.AgentID = AgentId;
            rinfopack.AgentData.SessionID = SessionId;
            rinfoblk.BillableFactor = args.billableFactor;
            rinfoblk.EstateID = args.estateID;
            rinfoblk.MaxAgents = args.maxAgents;
            rinfoblk.ObjectBonusFactor = args.objectBonusFactor;
            rinfoblk.ParentEstateID = args.parentEstateID;
            rinfoblk.PricePerMeter = args.pricePerMeter;
            rinfoblk.RedirectGridX = args.redirectGridX;
            rinfoblk.RedirectGridY = args.redirectGridY;
            rinfoblk.RegionFlags = args.regionFlags;
            rinfoblk.SimAccess = args.simAccess;
            rinfoblk.SunHour = args.sunHour;
            rinfoblk.TerrainLowerLimit = args.terrainLowerLimit;
            rinfoblk.TerrainRaiseLimit = args.terrainRaiseLimit;
            rinfoblk.UseEstateSun = args.useEstateSun;
            rinfoblk.WaterHeight = args.waterHeight;
            rinfoblk.SimName = Utils.StringToBytes(args.simName);

            rinfopack.RegionInfo2 = new RegionInfoPacket.RegionInfo2Block
                                        {
                                            HardMaxAgents = uint.MaxValue,
                                            HardMaxObjects = uint.MaxValue,
                                            MaxAgents32 = args.maxAgents,
                                            ProductName = Utils.StringToBytes(args.regionType),
                                            ProductSKU = Utils.EmptyBytes
                                        };

            rinfopack.HasVariableBlocks = true;
            rinfopack.RegionInfo = rinfoblk;
            rinfopack.AgentData = new RegionInfoPacket.AgentDataBlock {AgentID = AgentId, SessionID = SessionId};


            OutPacket(rinfopack, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendEstateCovenantInformation(UUID covenant, int covenantLastUpdated)
        {
            EstateCovenantReplyPacket einfopack = new EstateCovenantReplyPacket();
            EstateCovenantReplyPacket.DataBlock edata = new EstateCovenantReplyPacket.DataBlock
                                                            {
                                                                CovenantID = covenant,
                                                                CovenantTimestamp = (uint) covenantLastUpdated,
                                                                EstateOwnerID =
                                                                    m_scene.RegionInfo.EstateSettings.EstateOwner,
                                                                EstateName =
                                                                    Utils.StringToBytes(
                                                                        m_scene.RegionInfo.EstateSettings.EstateName)
                                                            };
            einfopack.Data = edata;
            OutPacket(einfopack, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendDetailedEstateData(UUID invoice, string estateName, uint estateID, uint parentEstate,
                                           uint estateFlags, uint sunPosition, UUID covenant, int CovenantLastUpdated,
                                           string abuseEmail, UUID estateOwner)
        {
            EstateOwnerMessagePacket packet = new EstateOwnerMessagePacket
                                                  {
                                                      MethodData = {Invoice = invoice},
                                                      AgentData = {TransactionID = UUID.Random()}
                                                  };
            packet.MethodData.Method = Utils.StringToBytes("estateupdateinfo");
            EstateOwnerMessagePacket.ParamListBlock[] returnblock = new EstateOwnerMessagePacket.ParamListBlock[10];

            for (int i = 0; i < 10; i++)
            {
                returnblock[i] = new EstateOwnerMessagePacket.ParamListBlock();
            }

            //Sending Estate Settings
            returnblock[0].Parameter = Utils.StringToBytes(estateName);
            returnblock[1].Parameter = Utils.StringToBytes(estateOwner.ToString());
            returnblock[2].Parameter = Utils.StringToBytes(estateID.ToString());

            returnblock[3].Parameter = Utils.StringToBytes(estateFlags.ToString());
            returnblock[4].Parameter = Utils.StringToBytes(sunPosition.ToString());
            returnblock[5].Parameter = Utils.StringToBytes(parentEstate.ToString());
            returnblock[6].Parameter = Utils.StringToBytes(covenant.ToString());
            returnblock[7].Parameter = Utils.StringToBytes(CovenantLastUpdated.ToString());
            returnblock[8].Parameter = Utils.StringToBytes("1"); // Send to this agent only
            returnblock[9].Parameter = Utils.StringToBytes(abuseEmail);

            packet.ParamList = returnblock;
            packet.Header.Reliable = false;
            //MainConsole.Instance.Debug("[ESTATE]: SIM--->" + packet.ToString());
            OutPacket(packet, ThrottleOutPacketType.AvatarInfo);
        }

        #endregion

        #region Land Data Sending Methods

        public void SendLandParcelOverlay(byte[] data, int sequence_id)
        {
            ParcelOverlayPacket packet = (ParcelOverlayPacket) PacketPool.Instance.GetPacket(PacketType.ParcelOverlay);
            packet.ParcelData.Data = data;
            packet.ParcelData.SequenceID = sequence_id;
            packet.Header.Zerocoded = true;
            OutPacket(packet, ThrottleOutPacketType.Land);
        }

        public void SendLandProperties(int sequence_id, bool snap_selection, int request_result, LandData landData,
                                       float simObjectBonusFactor, int parcelObjectCapacity, int simObjectCapacity,
                                       uint regionFlags)
        {
            ParcelPropertiesMessage updateMessage = new ParcelPropertiesMessage();

            IPrimCountModule primCountModule = m_scene.RequestModuleInterface<IPrimCountModule>();
            if (primCountModule != null)
            {
                IPrimCounts primCounts = primCountModule.GetPrimCounts(landData.GlobalID);
                updateMessage.GroupPrims = primCounts.Group;
                updateMessage.OtherPrims = primCounts.Others;
                updateMessage.OwnerPrims = primCounts.Owner;
                updateMessage.SelectedPrims = primCounts.Selected;
                updateMessage.SimWideTotalPrims = primCounts.Simulator;
                updateMessage.TotalPrims = primCounts.Total;
            }

            updateMessage.AABBMax = landData.AABBMax;
            updateMessage.AABBMin = landData.AABBMin;
            updateMessage.Area = landData.Area;
            updateMessage.AuctionID = landData.AuctionID;
            updateMessage.AuthBuyerID = landData.AuthBuyerID;

            updateMessage.Bitmap = landData.Bitmap;

            updateMessage.Desc = landData.Description;
            updateMessage.Category = landData.Category;
            updateMessage.ClaimDate = Util.ToDateTime(landData.ClaimDate);
            updateMessage.ClaimPrice = landData.ClaimPrice;
            updateMessage.GroupID = landData.GroupID;
            updateMessage.IsGroupOwned = landData.IsGroupOwned;
            updateMessage.LandingType = (LandingType) landData.LandingType;
            updateMessage.LocalID = landData.LocalID;

            updateMessage.MaxPrims = parcelObjectCapacity;

            updateMessage.MediaAutoScale = Convert.ToBoolean(landData.MediaAutoScale);
            updateMessage.MediaID = landData.MediaID;
            updateMessage.MediaURL = landData.MediaURL;
            updateMessage.MusicURL = landData.MusicURL;
            updateMessage.Name = landData.Name;
            updateMessage.OtherCleanTime = landData.OtherCleanTime;
            updateMessage.OtherCount = 0; //TODO: Unimplemented
            updateMessage.OwnerID = landData.OwnerID;
            updateMessage.ParcelFlags = (ParcelFlags) landData.Flags;
            updateMessage.ParcelPrimBonus = simObjectBonusFactor;
            updateMessage.PassHours = landData.PassHours;
            updateMessage.PassPrice = landData.PassPrice;
            updateMessage.PublicCount = 0; //TODO: Unimplemented
            updateMessage.Privacy = landData.Private;

            updateMessage.RegionPushOverride = (regionFlags & (uint) RegionFlags.RestrictPushObject) > 0;
            updateMessage.RegionDenyAnonymous = (regionFlags & (uint) RegionFlags.DenyAnonymous) > 0;

            updateMessage.RegionDenyIdentified = (regionFlags & (uint) RegionFlags.DenyIdentified) > 0;
            updateMessage.RegionDenyTransacted = (regionFlags & (uint) RegionFlags.DenyTransacted) > 0;

            updateMessage.RentPrice = 0;
            updateMessage.RequestResult = (ParcelResult) request_result;
            updateMessage.SalePrice = landData.SalePrice;
            updateMessage.SelfCount = 0; //TODO: Unimplemented
            updateMessage.SequenceID = sequence_id;
            updateMessage.SimWideMaxPrims = simObjectCapacity;
            updateMessage.SnapSelection = snap_selection;
            updateMessage.SnapshotID = landData.SnapshotID;
            updateMessage.Status = landData.Status;
            updateMessage.UserLocation = landData.UserLocation;
            updateMessage.UserLookAt = landData.UserLookAt;

            updateMessage.MediaType = landData.MediaType;
            updateMessage.MediaDesc = landData.MediaDescription;
            updateMessage.MediaWidth = landData.MediaWidth;
            updateMessage.MediaHeight = landData.MediaHeight;
            updateMessage.MediaLoop = landData.MediaLoop;
            updateMessage.ObscureMusic = landData.ObscureMusic;
            updateMessage.ObscureMedia = landData.ObscureMedia;

            try
            {
                IEventQueueService eq = Scene.RequestModuleInterface<IEventQueueService>();
                if (eq != null)
                {
                    eq.ParcelProperties(updateMessage, AgentId, Scene.RegionInfo.RegionHandle);
                }
                else
                {
                    MainConsole.Instance.Warn("No EQ Interface when sending parcel data.");
                }
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Error("Unable to send parcel data via eventqueue - exception: " + ex);
            }
        }

        public void SendLandAccessListData(List<UUID> avatars, uint accessFlag, int localLandID)
        {
            ParcelAccessListReplyPacket replyPacket =
                (ParcelAccessListReplyPacket) PacketPool.Instance.GetPacket(PacketType.ParcelAccessListReply);
            replyPacket.Data.AgentID = AgentId;
            replyPacket.Data.Flags = accessFlag;
            replyPacket.Data.LocalID = localLandID;
            replyPacket.Data.SequenceID = 0;

#if (!ISWIN)
            List<ParcelAccessListReplyPacket.ListBlock> list = new List<ParcelAccessListReplyPacket.ListBlock>();
            foreach (UUID avatar in avatars)
                list.Add(new ParcelAccessListReplyPacket.ListBlock {Flags = accessFlag, ID = avatar, Time = 0});
            replyPacket.List = list.ToArray();
#else
            replyPacket.List =
                avatars.Select(
                    avatar => new ParcelAccessListReplyPacket.ListBlock { Flags = accessFlag, ID = avatar, Time = 0 }).
                    ToArray();
#endif
            replyPacket.Header.Zerocoded = true;
            OutPacket(replyPacket, ThrottleOutPacketType.Land);
        }

        public void SendForceClientSelectObjects(List<uint> ObjectIDs)
        {
            bool firstCall = true;
            const int MAX_OBJECTS_PER_PACKET = 251;
            ForceObjectSelectPacket pack =
                (ForceObjectSelectPacket) PacketPool.Instance.GetPacket(PacketType.ForceObjectSelect);
            while (ObjectIDs.Count > 0)
            {
                if (firstCall)
                {
                    pack._Header.ResetList = true;
                    firstCall = false;
                }
                else
                {
                    pack._Header.ResetList = false;
                }

                ForceObjectSelectPacket.DataBlock[] data = ObjectIDs.Count > MAX_OBJECTS_PER_PACKET ? new ForceObjectSelectPacket.DataBlock[MAX_OBJECTS_PER_PACKET] : new ForceObjectSelectPacket.DataBlock[ObjectIDs.Count];

                int i;
                for (i = 0; i < MAX_OBJECTS_PER_PACKET && ObjectIDs.Count > 0; i++)
                {
                    data[i] = new ForceObjectSelectPacket.DataBlock {LocalID = Convert.ToUInt32(ObjectIDs[0])};
                    ObjectIDs.RemoveAt(0);
                }
                pack.Data = data;
                pack.Header.Zerocoded = true;
                OutPacket(pack, ThrottleOutPacketType.State);
            }
        }

        public void SendCameraConstraint(Vector4 ConstraintPlane)
        {
            CameraConstraintPacket cpack =
                (CameraConstraintPacket) PacketPool.Instance.GetPacket(PacketType.CameraConstraint);
            cpack.CameraCollidePlane = new CameraConstraintPacket.CameraCollidePlaneBlock {Plane = ConstraintPlane};
            //MainConsole.Instance.DebugFormat("[CLIENTVIEW]: Constraint {0}", ConstraintPlane);
            OutPacket(cpack, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendLandObjectOwners(List<LandObjectOwners> objectOwners)
        {
            int notifyCount = objectOwners.Count;

            if (notifyCount > 32)
            {
                MainConsole.Instance.InfoFormat(
                    "[LAND]: Mor e than {0} avatars own prims on this parcel.  Only sending back details of first {0}"
                    + " - a developer might want to investigate whether this is a hard limit", 32);

                notifyCount = 32;
            }

            ParcelObjectOwnersReplyMessage message = new ParcelObjectOwnersReplyMessage
                                                         {
                                                             PrimOwnersBlock =
                                                                 new ParcelObjectOwnersReplyMessage.PrimOwner[
                                                                 notifyCount]
                                                         };

            int num = 0;
            foreach (LandObjectOwners owner in objectOwners)
            {
                message.PrimOwnersBlock[num] = new ParcelObjectOwnersReplyMessage.PrimOwner
                                                   {
                                                       Count = owner.Count,
                                                       IsGroupOwned = owner.GroupOwned,
                                                       OnlineStatus = owner.Online,
                                                       OwnerID = owner.OwnerID,
                                                       TimeStamp = owner.TimeLastRezzed
                                                   };

                num++;

                if (num >= notifyCount)
                    break;
            }

            IEventQueueService eventQueueService = m_scene.RequestModuleInterface<IEventQueueService>();
            if (eventQueueService != null)
            {
                eventQueueService.ParcelObjectOwnersReply(message, AgentId, m_scene.RegionInfo.RegionHandle);
            }
        }

        #endregion

        #region Helper Methods

        private ImprovedTerseObjectUpdatePacket.ObjectDataBlock CreateImprovedTerseBlock(IEntity entity,
                                                                                         bool sendTexture)
        {
            #region ScenePresence/SOP Handling

            bool avatar = (entity is IScenePresence);
            uint localID = entity.LocalId;
            int attachPoint;
            Vector4 collisionPlane;
            Vector3 position, velocity, acceleration, angularVelocity;
            Quaternion rotation;
            byte[] textureEntry;

            if (entity is IScenePresence)
            {
                IScenePresence presence = (IScenePresence) entity;

                attachPoint = 0;
                if (presence.PhysicsActor != null && !presence.PhysicsActor.IsColliding)
                    presence.CollisionPlane = Vector4.UnitW;
                //We have to do this, otherwise the last ground one will be what we have, and it can cause the client to think that it shouldn't fly down, which will cause the agent to fall instead
                collisionPlane = presence.CollisionPlane;
                position = presence.OffsetPosition;
                velocity = presence.Velocity;
                acceleration = Vector3.Zero;
                angularVelocity = Vector3.Zero;
                rotation = presence.Rotation;
                IAvatarAppearanceModule appearance = presence.RequestModuleInterface<IAvatarAppearanceModule>();
                textureEntry = sendTexture ? appearance.Appearance.Texture.GetBytes() : null;
            }
            else
            {
                SceneObjectPart part = (SceneObjectPart) entity;

                attachPoint = part.AttachmentPoint;
                collisionPlane = Vector4.Zero;
                position = part.RelativePosition;
                velocity = part.Velocity;
                acceleration = part.Acceleration;
                angularVelocity = part.AngularVelocity;
                rotation = part.RotationOffset;

                textureEntry = sendTexture ? part.Shape.TextureEntry : null;
            }

            #endregion ScenePresence/SOP Handling

            int pos = 0;
            byte[] data = new byte[(avatar ? 60 : 44)];

            // LocalID
            Utils.UIntToBytes(localID, data, pos);
            pos += 4;

            // Avatar/CollisionPlane
            data[pos] = (byte) ((attachPoint & 0x0f) << 4);
            data[pos++] += (byte) (attachPoint >> 4);

            if (avatar)
            {
                data[pos++] = 1;

                if (collisionPlane == Vector4.Zero)
                    collisionPlane = Vector4.UnitW;
                //MainConsole.Instance.DebugFormat("CollisionPlane: {0}",collisionPlane);
                collisionPlane.ToBytes(data, pos);
                pos += 16;
            }
            else
            {
                ++pos;
            }

            // Position
            position.ToBytes(data, pos);
            pos += 12;

            // Velocity
            //MainConsole.Instance.DebugFormat("Velocity: {0}", velocity);
            Utils.UInt16ToBytes(Utils.FloatToUInt16(velocity.X, -128.0f, 128.0f), data, pos);
            pos += 2;
            Utils.UInt16ToBytes(Utils.FloatToUInt16(velocity.Y, -128.0f, 128.0f), data, pos);
            pos += 2;
            Utils.UInt16ToBytes(Utils.FloatToUInt16(velocity.Z, -128.0f, 128.0f), data, pos);
            pos += 2;

            // Acceleration
            Utils.UInt16ToBytes(Utils.FloatToUInt16(acceleration.X, -64.0f, 64.0f), data, pos);
            pos += 2;
            Utils.UInt16ToBytes(Utils.FloatToUInt16(acceleration.Y, -64.0f, 64.0f), data, pos);
            pos += 2;
            Utils.UInt16ToBytes(Utils.FloatToUInt16(acceleration.Z, -64.0f, 64.0f), data, pos);
            pos += 2;

            // Rotation
            Utils.UInt16ToBytes(Utils.FloatToUInt16(rotation.X, -1.0f, 1.0f), data, pos);
            pos += 2;
            Utils.UInt16ToBytes(Utils.FloatToUInt16(rotation.Y, -1.0f, 1.0f), data, pos);
            pos += 2;
            Utils.UInt16ToBytes(Utils.FloatToUInt16(rotation.Z, -1.0f, 1.0f), data, pos);
            pos += 2;
            Utils.UInt16ToBytes(Utils.FloatToUInt16(rotation.W, -1.0f, 1.0f), data, pos);
            pos += 2;

            // Angular Velocity
            Utils.UInt16ToBytes(Utils.FloatToUInt16(angularVelocity.X, -64.0f, 64.0f), data, pos);
            pos += 2;
            Utils.UInt16ToBytes(Utils.FloatToUInt16(angularVelocity.Y, -64.0f, 64.0f), data, pos);
            pos += 2;
            Utils.UInt16ToBytes(Utils.FloatToUInt16(angularVelocity.Z, -64.0f, 64.0f), data, pos);
            pos += 2;

            ImprovedTerseObjectUpdatePacket.ObjectDataBlock block =
                new ImprovedTerseObjectUpdatePacket.ObjectDataBlock {Data = data};

            if (textureEntry != null && textureEntry.Length > 0)
            {
                byte[] teBytesFinal = new byte[textureEntry.Length + 4];

                // Texture Length
                Utils.IntToBytes(textureEntry.Length, textureEntry, 0);
                // Texture
                Buffer.BlockCopy(textureEntry, 0, teBytesFinal, 4, textureEntry.Length);

                block.TextureEntry = teBytesFinal;
            }
            else
            {
                block.TextureEntry = Utils.EmptyBytes;
            }
            return block;
        }

        private ObjectUpdatePacket.ObjectDataBlock CreateAvatarUpdateBlock(IScenePresence data)
        {
            byte[] objectData = new byte[76];

            //No Zero vectors, as it causes bent knee in the client! Replace with <0, 0, 0, 1>
            if (data.CollisionPlane == Vector4.Zero)
                data.CollisionPlane = Vector4.UnitW;
            //MainConsole.Instance.DebugFormat("CollisionPlane: {0}", data.CollisionPlane);
            data.CollisionPlane.ToBytes(objectData, 0);
            data.OffsetPosition.ToBytes(objectData, 16);
            data.Velocity.ToBytes(objectData, 28);
            //data.Acceleration.ToBytes(objectData, 40);
            data.Rotation.ToBytes(objectData, 52);
            //data.AngularVelocity.ToBytes(objectData, 64);

            ObjectUpdatePacket.ObjectDataBlock update = new ObjectUpdatePacket.ObjectDataBlock
                                                            {
                                                                Data = Utils.EmptyBytes,
                                                                ExtraParams = new byte[1],
                                                                FullID = data.UUID,
                                                                ID = data.LocalId,
                                                                Material = (byte) Material.Flesh,
                                                                MediaURL = Utils.EmptyBytes,
                                                                NameValue =
                                                                    Utils.StringToBytes("FirstName STRING RW SV " +
                                                                                        data.Firstname +
                                                                                        "\nLastName STRING RW SV " +
                                                                                        data.Lastname +
                                                                                        "\nTitle STRING RW SV " +
                                                                                        (m_GroupsModule == null
                                                                                             ? ""
                                                                                             : m_GroupsModule.
                                                                                                   GetGroupTitle(
                                                                                                       data.UUID))),
                                                                ObjectData = objectData
                                                            };

            if (data.ParentID == UUID.Zero)
                update.ParentID = 0;
            else
            {
                ISceneChildEntity part = Scene.GetSceneObjectPart(data.ParentID);
                update.ParentID = part.LocalId;
            }
            update.PathCurve = 16;
            update.PathScaleX = 100;
            update.PathScaleY = 100;
            update.PCode = (byte) PCode.Avatar;
            update.ProfileCurve = 1;
            update.PSBlock = Utils.EmptyBytes;
            update.Scale = new Vector3(0.45f, 0.6f, 1.9f);
            update.Text = Utils.EmptyBytes;
            update.TextColor = new byte[4];
            update.TextureAnim = Utils.EmptyBytes;
            // Don't send texture entry for avatars here - this is accomplished via the AvatarAppearance packet
            update.TextureEntry = Utils.EmptyBytes;
            update.UpdateFlags = (uint) (
                                            PrimFlags.Physics | PrimFlags.ObjectModify | PrimFlags.ObjectCopy |
                                            PrimFlags.ObjectAnyOwner |
                                            PrimFlags.ObjectYouOwner | PrimFlags.ObjectMove | PrimFlags.InventoryEmpty |
                                            PrimFlags.ObjectTransfer |
                                            PrimFlags.ObjectOwnerModify);

            return update;
        }

        private ObjectUpdateCachedPacket.ObjectDataBlock CreatePrimCachedUpdateBlock(SceneObjectPart data,
                                                                                     UUID recipientID)
        {
            ObjectUpdateCachedPacket.ObjectDataBlock odb = new ObjectUpdateCachedPacket.ObjectDataBlock
                                                               {CRC = data.CRC, ID = data.LocalId};

            #region PrimFlags

            PrimFlags flags = (PrimFlags) m_scene.Permissions.GenerateClientFlags(recipientID, data);

            // Don't send the CreateSelected flag to everyone
            flags &= ~PrimFlags.CreateSelected;

            if (recipientID == data.OwnerID)
            {
                if (data.CreateSelected)
                {
                    // Only send this flag once, then unset it
                    flags |= PrimFlags.CreateSelected;
                    data.CreateSelected = false;
                }
            }

            #endregion PrimFlags

            odb.UpdateFlags = (uint) flags;
            return odb;
        }

        private ObjectUpdatePacket.ObjectDataBlock CreatePrimUpdateBlock(SceneObjectPart data, UUID recipientID)
        {
            byte[] objectData = new byte[60];
            data.RelativePosition.ToBytes(objectData, 0);
            data.Velocity.ToBytes(objectData, 12);
            data.Acceleration.ToBytes(objectData, 24);
            try
            {
                data.RotationOffset.ToBytes(objectData, 36);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Warn(
                    "[LLClientView]: exception converting quaternion to bytes, using Quaternion.Identity. Exception: " +
                    e);
                Quaternion.Identity.ToBytes(objectData, 36);
            }
            data.AngularVelocity.ToBytes(objectData, 48);

            ObjectUpdatePacket.ObjectDataBlock update = new ObjectUpdatePacket.ObjectDataBlock
                                                            {
                                                                ClickAction = data.ClickAction,
                                                                CRC = data.CRC,
                                                                ExtraParams = data.Shape.ExtraParams ?? Utils.EmptyBytes,
                                                                FullID = data.UUID,
                                                                ID = data.LocalId,
                                                                Material = (byte) data.Material,
                                                                MediaURL = Utils.StringToBytes(data.CurrentMediaVersion)
                                                            };
            //update.JointAxisOrAnchor = Vector3.Zero; // These are deprecated
            //update.JointPivot = Vector3.Zero;
            //update.JointType = 0;
            if (data.IsAttachment)
            {
                update.NameValue = Util.StringToBytes256("AttachItemID STRING RW SV " + data.FromUserInventoryItemID);
                update.State = (byte) ((data.AttachmentPoint%16)*16 + (data.AttachmentPoint/16));
            }
            else
            {
                update.NameValue = Utils.EmptyBytes;
                // The root part state is the canonical state for all parts of the object.  The other part states in the
                // case for attachments may contain conflicting values that can end up crashing the viewer.
                update.State = data.ParentGroup.RootPart.Shape.State;
            }

            update.ObjectData = objectData;
            update.ParentID = data.ParentID;
            update.PathBegin = data.Shape.PathBegin;
            update.PathCurve = data.Shape.PathCurve;
            update.PathEnd = data.Shape.PathEnd;
            update.PathRadiusOffset = data.Shape.PathRadiusOffset;
            update.PathRevolutions = data.Shape.PathRevolutions;
            update.PathScaleX = data.Shape.PathScaleX;
            update.PathScaleY = data.Shape.PathScaleY;
            update.PathShearX = data.Shape.PathShearX;
            update.PathShearY = data.Shape.PathShearY;
            update.PathSkew = data.Shape.PathSkew;
            update.PathTaperX = data.Shape.PathTaperX;
            update.PathTaperY = data.Shape.PathTaperY;
            update.PathTwist = data.Shape.PathTwist;
            update.PathTwistBegin = data.Shape.PathTwistBegin;
            update.PCode = data.Shape.PCode;
            update.ProfileBegin = data.Shape.ProfileBegin;
            update.ProfileCurve = data.Shape.ProfileCurve;
            update.ProfileEnd = data.Shape.ProfileEnd;
            update.ProfileHollow = data.Shape.ProfileHollow;
            update.PSBlock = data.ParticleSystem ?? Utils.EmptyBytes;
            update.TextColor = data.GetTextColor().GetBytes(false);
            update.TextureAnim = data.TextureAnimation ?? Utils.EmptyBytes;
            update.TextureEntry = data.Shape.TextureEntry ?? Utils.EmptyBytes;
            update.Scale = data.Shape.Scale;
            update.Text = Util.StringToBytes256(data.Text);
            update.MediaURL = Util.StringToBytes256(data.MediaUrl);

            #region PrimFlags

            PrimFlags flags = (PrimFlags) m_scene.Permissions.GenerateClientFlags(recipientID, data);

            // Don't send the CreateSelected flag to everyone
            flags &= ~PrimFlags.CreateSelected;

            if (recipientID == data.OwnerID)
            {
                if (data.CreateSelected)
                {
                    // Only send this flag once, then unset it
                    flags |= PrimFlags.CreateSelected;
                    data.CreateSelected = false;
                }
            }

//            MainConsole.Instance.DebugFormat(
//                "[LLCLIENTVIEW]: Constructing client update for part {0} {1} with flags {2}, localId {3}",
//                data.Name, update.FullID, flags, update.ID);

            update.UpdateFlags = (uint) flags;

            #endregion PrimFlags

            if (data.Sound != UUID.Zero)
            {
                update.Sound = data.Sound;
                update.OwnerID = data.OwnerID;
                update.Gain = (float) data.SoundGain;
                update.Radius = (float) data.SoundRadius;
                update.Flags = data.SoundFlags;
            }

            switch ((PCode) data.Shape.PCode)
            {
                case PCode.Grass:
                case PCode.Tree:
                case PCode.NewTree:
                    update.Data = new[] {data.Shape.State};
                    break;
                default:
                    update.Data = Utils.EmptyBytes;
                    break;
            }

            return update;
        }

        private ObjectUpdateCompressedPacket.ObjectDataBlock CreateCompressedUpdateBlock(SceneObjectPart part,
                                                                                         CompressedFlags updateFlags,
                                                                                         PrimUpdateFlags flags)
        {
            byte[] objectData = new byte[500];
            int i = 0;
            part.UUID.ToBytes(objectData, 0);
            i += 16;
            Utils.UIntToBytes(part.LocalId, objectData, i);
            i += 4;
            objectData[i] = part.Shape.PCode; //Type of prim
            i += 1;

            if (part.Shape.PCode == (byte) PCode.Tree || part.Shape.PCode == (byte) PCode.NewTree)
                updateFlags |= CompressedFlags.Tree;

            //Attachment point
            objectData[i] = (byte) part.AttachmentPoint;
            i += 1;
            //CRC
            Utils.UIntToBytes(part.CRC, objectData, i);
            i += 4;
            objectData[i] = (byte) part.Material;
            i++;
            objectData[i] = part.ClickAction;
            i++;
            part.Shape.Scale.ToBytes(objectData, i);
            i += 12;
            part.RelativePosition.ToBytes(objectData, i);
            i += 12;
            try
            {
                part.RotationOffset.ToBytes(objectData, i);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Warn(
                    "[LLClientView]: exception converting quaternion to bytes, using Quaternion.Identity. Exception: " +
                    e);
                Quaternion.Identity.ToBytes(objectData, i);
            }
            i += 12;
            Utils.UIntToBytes((uint) updateFlags, objectData, i);
            i += 4;
            part.OwnerID.ToBytes(objectData, i);
            i += 16;

            if ((updateFlags & CompressedFlags.HasAngularVelocity) != 0)
            {
                part.AngularVelocity.ToBytes(objectData, i);
                i += 12;
            }
            if ((updateFlags & CompressedFlags.HasParent) != 0)
            {
                if (part.IsAttachment)
                {
                    IScenePresence us = m_scene.GetScenePresence(AgentId);
                    Utils.UIntToBytes(us.LocalId, objectData, i);
                }
                else
                    Utils.UIntToBytes(part.ParentID, objectData, i);
                i += 4;
            }
            if ((updateFlags & CompressedFlags.Tree) != 0)
            {
                objectData[i] = part.Shape.State; //Tree type
                i++;
            }
            else if ((updateFlags & CompressedFlags.ScratchPad) != 0)
            {
                //Remove the flag, we have no clue what to do with this
                updateFlags &= ~(CompressedFlags.ScratchPad);
            }
            if ((updateFlags & CompressedFlags.HasText) != 0)
            {
                byte[] text = Utils.StringToBytes(part.Text);
                Buffer.BlockCopy(text, 0, objectData, i, text.Length);
                i += text.Length;

                byte[] textcolor = part.GetTextColor().GetBytes(false);
                Buffer.BlockCopy(textcolor, 0, objectData, i, textcolor.Length);
                i += 4;
            }
            if ((updateFlags & CompressedFlags.MediaURL) != 0)
            {
                byte[] text = Util.StringToBytes256(part.CurrentMediaVersion);
                Buffer.BlockCopy(text, 0, objectData, i, text.Length);
                i += text.Length;
            }

            if ((updateFlags & CompressedFlags.HasParticles) != 0)
            {
                if (part.ParticleSystem.Length == 0)
                {
                    Primitive.ParticleSystem Sys = new Primitive.ParticleSystem();
                    byte[] pdata = Sys.GetBytes();
                    Buffer.BlockCopy(pdata, 0, objectData, i, pdata.Length);
                    i += pdata.Length; //86
                    //updateFlags = updateFlags & ~CompressedFlags.HasParticles;
                }
                else
                {
                    Buffer.BlockCopy(part.ParticleSystem, 0, objectData, i, part.ParticleSystem.Length);
                    i += part.ParticleSystem.Length; //86
                }
            }

            byte[] ExtraData = part.Shape.ExtraParamsToBytes();
            Buffer.BlockCopy(ExtraData, 0, objectData, i, ExtraData.Length);
            i += ExtraData.Length;

            if ((updateFlags & CompressedFlags.HasSound) != 0)
            {
                part.Sound.ToBytes(objectData, i);
                i += 16;
                Utils.FloatToBytes((float) part.SoundGain, objectData, i);
                i += 4;
                objectData[i] = part.SoundFlags;
                i++;
                Utils.FloatToBytes((float) part.SoundRadius, objectData, i);
                i += 4;
            }
            if ((updateFlags & CompressedFlags.HasNameValues) != 0)
            {
                if (part.IsAttachment)
                {
                    byte[] NV = Util.StringToBytes256("AttachItemID STRING RW SV " + part.FromUserInventoryItemID);
                    Buffer.BlockCopy(NV, 0, objectData, i, NV.Length);
                    i += NV.Length;
                }
            }

            objectData[i] = part.Shape.PathCurve;
            i++;
            Utils.UInt16ToBytes(part.Shape.PathBegin, objectData, i);
            i += 2;
            Utils.UInt16ToBytes(part.Shape.PathEnd, objectData, i);
            i += 2;
            objectData[i] = part.Shape.PathScaleX;
            i++;
            objectData[i] = part.Shape.PathScaleY;
            i++;
            objectData[i] = part.Shape.PathShearX;
            i++;
            objectData[i] = part.Shape.PathShearY;
            i++;
            objectData[i] = (byte) part.Shape.PathTwist;
            i++;
            objectData[i] = (byte) part.Shape.PathTwistBegin;
            i++;
            objectData[i] = (byte) part.Shape.PathRadiusOffset;
            i++;
            objectData[i] = (byte) part.Shape.PathTaperX;
            i++;
            objectData[i] = (byte) part.Shape.PathTaperY;
            i++;
            objectData[i] = part.Shape.PathRevolutions;
            i++;
            objectData[i] = (byte) part.Shape.PathSkew;
            i++;

            objectData[i] = part.Shape.ProfileCurve;
            i++;
            Utils.UInt16ToBytes(part.Shape.ProfileBegin, objectData, i);
            i += 2;
            Utils.UInt16ToBytes(part.Shape.ProfileEnd, objectData, i);
            i += 2;
            Utils.UInt16ToBytes(part.Shape.ProfileHollow, objectData, i);
            i += 2;

            if (part.Shape.TextureEntry != null && part.Shape.TextureEntry.Length > 0)
            {
                // Texture Length
                Utils.IntToBytes(part.Shape.TextureEntry.Length, objectData, i);
                i += 4;
                // Texture
                Buffer.BlockCopy(part.Shape.TextureEntry, 0, objectData, i, part.Shape.TextureEntry.Length);
                i += part.Shape.TextureEntry.Length;
            }
            else
            {
                Utils.IntToBytes(0, objectData, i);
                i += 4;
            }

            if ((updateFlags & CompressedFlags.TextureAnimation) != 0)
            {
                Utils.UInt64ToBytes((ulong) part.TextureAnimation.Length, objectData, i);
                i += 4;
                Buffer.BlockCopy(part.TextureAnimation, 0, objectData, i, part.TextureAnimation.Length);
                i += part.TextureAnimation.Length;
            }

            ObjectUpdateCompressedPacket.ObjectDataBlock update = new ObjectUpdateCompressedPacket.ObjectDataBlock();

            #region PrimFlags

            PrimFlags primflags = (PrimFlags) m_scene.Permissions.GenerateClientFlags(AgentId, part);

            // Don't send the CreateSelected flag to everyone
            primflags &= ~PrimFlags.CreateSelected;

            if (AgentId == part.OwnerID)
            {
                if (part.CreateSelected)
                {
                    // Only send this flag once, then unset it
                    primflags |= PrimFlags.CreateSelected;
                    part.CreateSelected = false;
                }
            }

            update.UpdateFlags = (uint) primflags;

            #endregion PrimFlags

            byte[] PacketObjectData = new byte[i]; //Makes the packet smaller so we can send more!
            Buffer.BlockCopy(objectData, 0, PacketObjectData, 0, i);
            update.Data = PacketObjectData;

            return update;
        }

        public void SendNameReply(UUID profileId, string firstname, string lastname)
        {
            UUIDNameReplyPacket packet = (UUIDNameReplyPacket) PacketPool.Instance.GetPacket(PacketType.UUIDNameReply);
            // TODO: don't create new blocks if recycling an old packet
            packet.UUIDNameBlock = new UUIDNameReplyPacket.UUIDNameBlockBlock[1];
            packet.UUIDNameBlock[0] = new UUIDNameReplyPacket.UUIDNameBlockBlock
                                          {
                                              ID = profileId,
                                              FirstName = Util.StringToBytes256(firstname),
                                              LastName = Util.StringToBytes256(lastname)
                                          };

            OutPacket(packet, ThrottleOutPacketType.Asset);
        }

        /// <summary>
        ///   This is a utility method used by single states to not duplicate kicks and blue card of death messages.
        /// </summary>
        public bool ChildAgentStatus()
        {
            IScenePresence Sp = m_scene.GetScenePresence(AgentId);
            if (Sp == null || (Sp.IsChildAgent))
                return true;
            return false;
        }

        #endregion

        /// <summary>
        ///   This is a different way of processing packets then ProcessInPacket
        /// </summary>
        private void RegisterLocalPacketHandlers()
        {
            AddLocalPacketHandler(PacketType.LogoutRequest, HandleLogout);
            AddLocalPacketHandler(PacketType.AgentUpdate, HandleAgentUpdate, false);
            AddLocalPacketHandler(PacketType.ViewerEffect, HandleViewerEffect, true);
            AddLocalPacketHandler(PacketType.AgentCachedTexture, HandleAgentTextureCached, false);
            AddLocalPacketHandler(PacketType.MultipleObjectUpdate, HandleMultipleObjUpdate, false);
            AddLocalPacketHandler(PacketType.MoneyTransferRequest, HandleMoneyTransferRequest, false);
            AddLocalPacketHandler(PacketType.ParcelBuy, HandleParcelBuyRequest, false);
            AddLocalPacketHandler(PacketType.UUIDGroupNameRequest, HandleUUIDGroupNameRequest, false);
            AddLocalPacketHandler(PacketType.ObjectGroup, HandleObjectGroupRequest, false);
            AddLocalPacketHandler(PacketType.GenericMessage, HandleGenericMessage);
            AddLocalPacketHandler(PacketType.AvatarPropertiesRequest, HandleAvatarPropertiesRequest);
            AddLocalPacketHandler(PacketType.ChatFromViewer, HandleChatFromViewer);
            AddLocalPacketHandler(PacketType.AvatarPropertiesUpdate, HandlerAvatarPropertiesUpdate);
            AddLocalPacketHandler(PacketType.ScriptDialogReply, HandlerScriptDialogReply);
            AddLocalPacketHandler(PacketType.ImprovedInstantMessage, HandlerImprovedInstantMessage, false);
            AddLocalPacketHandler(PacketType.AcceptFriendship, HandlerAcceptFriendship);
            AddLocalPacketHandler(PacketType.DeclineFriendship, HandlerDeclineFriendship);
            AddLocalPacketHandler(PacketType.TerminateFriendship, HandlerTerminateFrendship);
            AddLocalPacketHandler(PacketType.RezObject, HandlerRezObject);
            AddLocalPacketHandler(PacketType.RezObjectFromNotecard, HandlerRezObjectFromNotecard);
            AddLocalPacketHandler(PacketType.DeRezObject, HandlerDeRezObject);
            AddLocalPacketHandler(PacketType.ModifyLand, HandlerModifyLand);
            AddLocalPacketHandler(PacketType.RegionHandshakeReply, HandlerRegionHandshakeReply);
            AddLocalPacketHandler(PacketType.AgentWearablesRequest, HandlerAgentWearablesRequest);
            AddLocalPacketHandler(PacketType.AgentSetAppearance, HandlerAgentSetAppearance);
            AddLocalPacketHandler(PacketType.AgentIsNowWearing, HandlerAgentIsNowWearing);
            AddLocalPacketHandler(PacketType.RezSingleAttachmentFromInv, HandlerRezSingleAttachmentFromInv);
            AddLocalPacketHandler(PacketType.RezRestoreToWorld, HandlerRezRestoreToWorld);
            AddLocalPacketHandler(PacketType.RezMultipleAttachmentsFromInv, HandleRezMultipleAttachmentsFromInv);
            AddLocalPacketHandler(PacketType.DetachAttachmentIntoInv, HandleDetachAttachmentIntoInv);
            AddLocalPacketHandler(PacketType.ObjectAttach, HandleObjectAttach);
            AddLocalPacketHandler(PacketType.ObjectDetach, HandleObjectDetach);
            AddLocalPacketHandler(PacketType.ObjectDrop, HandleObjectDrop);
            AddLocalPacketHandler(PacketType.SetAlwaysRun, HandleSetAlwaysRun, false);
            AddLocalPacketHandler(PacketType.CompleteAgentMovement, HandleCompleteAgentMovement);
            AddLocalPacketHandler(PacketType.AgentAnimation, HandleAgentAnimation, false);
            AddLocalPacketHandler(PacketType.AgentRequestSit, HandleAgentRequestSit);
            AddLocalPacketHandler(PacketType.AgentSit, HandleAgentSit);
            AddLocalPacketHandler(PacketType.SoundTrigger, HandleSoundTrigger);
            AddLocalPacketHandler(PacketType.AvatarPickerRequest, HandleAvatarPickerRequest);
            AddLocalPacketHandler(PacketType.AgentDataUpdateRequest, HandleAgentDataUpdateRequest);
            AddLocalPacketHandler(PacketType.UserInfoRequest, HandleUserInfoRequest);
            AddLocalPacketHandler(PacketType.UpdateUserInfo, HandleUpdateUserInfo);
            AddLocalPacketHandler(PacketType.SetStartLocationRequest, HandleSetStartLocationRequest);
            AddLocalPacketHandler(PacketType.AgentThrottle, HandleAgentThrottle, false);
            AddLocalPacketHandler(PacketType.AgentPause, HandleAgentPause, false);
            AddLocalPacketHandler(PacketType.AgentResume, HandleAgentResume, false);
            AddLocalPacketHandler(PacketType.ForceScriptControlRelease, HandleForceScriptControlRelease);
            AddLocalPacketHandler(PacketType.ObjectLink, HandleObjectLink);
            AddLocalPacketHandler(PacketType.ObjectDelink, HandleObjectDelink);
            AddLocalPacketHandler(PacketType.ObjectAdd, HandleObjectAdd);
            AddLocalPacketHandler(PacketType.ObjectShape, HandleObjectShape);
            AddLocalPacketHandler(PacketType.ObjectExtraParams, HandleObjectExtraParams);
            AddLocalPacketHandler(PacketType.ObjectDuplicate, HandleObjectDuplicate);
            AddLocalPacketHandler(PacketType.RequestMultipleObjects, HandleRequestMultipleObjects);
            AddLocalPacketHandler(PacketType.ObjectSelect, HandleObjectSelect);
            AddLocalPacketHandler(PacketType.ObjectDeselect, HandleObjectDeselect);
            AddLocalPacketHandler(PacketType.ObjectPosition, HandleObjectPosition);
            AddLocalPacketHandler(PacketType.ObjectScale, HandleObjectScale);
            AddLocalPacketHandler(PacketType.ObjectRotation, HandleObjectRotation);
            AddLocalPacketHandler(PacketType.ObjectFlagUpdate, HandleObjectFlagUpdate);

            // Handle ObjectImage (TextureEntry) updates synchronously, since when updating multiple prim faces at once,
            // some clients will send out a separate ObjectImage packet for each face
            AddLocalPacketHandler(PacketType.ObjectImage, HandleObjectImage, false);

            AddLocalPacketHandler(PacketType.ObjectGrab, HandleObjectGrab, false);
            AddLocalPacketHandler(PacketType.ObjectGrabUpdate, HandleObjectGrabUpdate, false);
            AddLocalPacketHandler(PacketType.ObjectDeGrab, HandleObjectDeGrab);
            AddLocalPacketHandler(PacketType.ObjectSpinStart, HandleObjectSpinStart, false);
            AddLocalPacketHandler(PacketType.ObjectSpinUpdate, HandleObjectSpinUpdate, false);
            AddLocalPacketHandler(PacketType.ObjectSpinStop, HandleObjectSpinStop, false);
            AddLocalPacketHandler(PacketType.ObjectDescription, HandleObjectDescription, false);
            AddLocalPacketHandler(PacketType.ObjectName, HandleObjectName, false);
            AddLocalPacketHandler(PacketType.ObjectPermissions, HandleObjectPermissions, false);
            AddLocalPacketHandler(PacketType.Undo, HandleUndo, false);
            AddLocalPacketHandler(PacketType.UndoLand, HandleLandUndo, false);
            AddLocalPacketHandler(PacketType.Redo, HandleRedo, false);
            AddLocalPacketHandler(PacketType.ObjectDuplicateOnRay, HandleObjectDuplicateOnRay);
            AddLocalPacketHandler(PacketType.RequestObjectPropertiesFamily, HandleRequestObjectPropertiesFamily, false);
            AddLocalPacketHandler(PacketType.ObjectIncludeInSearch, HandleObjectIncludeInSearch);
            AddLocalPacketHandler(PacketType.ScriptAnswerYes, HandleScriptAnswerYes, false);
            AddLocalPacketHandler(PacketType.ObjectClickAction, HandleObjectClickAction, false);
            AddLocalPacketHandler(PacketType.ObjectMaterial, HandleObjectMaterial, false);
            AddLocalPacketHandler(PacketType.RequestImage, HandleRequestImage);
            AddLocalPacketHandler(PacketType.TransferRequest, HandleTransferRequest);
            AddLocalPacketHandler(PacketType.AssetUploadRequest, HandleAssetUploadRequest);
            AddLocalPacketHandler(PacketType.RequestXfer, HandleRequestXfer);
            AddLocalPacketHandler(PacketType.SendXferPacket, HandleSendXferPacket);
            AddLocalPacketHandler(PacketType.ConfirmXferPacket, HandleConfirmXferPacket);
            AddLocalPacketHandler(PacketType.AbortXfer, HandleAbortXfer);
            AddLocalPacketHandler(PacketType.CreateInventoryFolder, HandleCreateInventoryFolder);
            AddLocalPacketHandler(PacketType.UpdateInventoryFolder, HandleUpdateInventoryFolder);
            AddLocalPacketHandler(PacketType.MoveInventoryFolder, HandleMoveInventoryFolder);
            AddLocalPacketHandler(PacketType.CreateInventoryItem, HandleCreateInventoryItem);
            AddLocalPacketHandler(PacketType.LinkInventoryItem, HandleLinkInventoryItem);
            if (m_allowUDPInv)
            {
                AddLocalPacketHandler(PacketType.FetchInventory, HandleFetchInventory);
                AddLocalPacketHandler(PacketType.FetchInventoryDescendents, HandleFetchInventoryDescendents);
            }
            AddLocalPacketHandler(PacketType.PurgeInventoryDescendents, HandlePurgeInventoryDescendents);
            AddLocalPacketHandler(PacketType.UpdateInventoryItem, HandleUpdateInventoryItem);
            AddLocalPacketHandler(PacketType.CopyInventoryItem, HandleCopyInventoryItem);
            AddLocalPacketHandler(PacketType.MoveInventoryItem, HandleMoveInventoryItem);
            AddLocalPacketHandler(PacketType.ChangeInventoryItemFlags, HandleChangeInventoryItemFlags);
            AddLocalPacketHandler(PacketType.RemoveInventoryItem, HandleRemoveInventoryItem);
            AddLocalPacketHandler(PacketType.RemoveInventoryFolder, HandleRemoveInventoryFolder);
            AddLocalPacketHandler(PacketType.RemoveInventoryObjects, HandleRemoveInventoryObjects);
            AddLocalPacketHandler(PacketType.RequestTaskInventory, HandleRequestTaskInventory);
            AddLocalPacketHandler(PacketType.UpdateTaskInventory, HandleUpdateTaskInventory);
            AddLocalPacketHandler(PacketType.RemoveTaskInventory, HandleRemoveTaskInventory);
            AddLocalPacketHandler(PacketType.MoveTaskInventory, HandleMoveTaskInventory);
            AddLocalPacketHandler(PacketType.RezScript, HandleRezScript);
            AddLocalPacketHandler(PacketType.MapLayerRequest, HandleMapLayerRequest, false);
            AddLocalPacketHandler(PacketType.MapBlockRequest, HandleMapBlockRequest, false);
            AddLocalPacketHandler(PacketType.MapNameRequest, HandleMapNameRequest, false);
            AddLocalPacketHandler(PacketType.TeleportLandmarkRequest, HandleTeleportLandmarkRequest);
            AddLocalPacketHandler(PacketType.TeleportLocationRequest, HandleTeleportLocationRequest);
            AddLocalPacketHandler(PacketType.UUIDNameRequest, HandleUUIDNameRequest, false);
            AddLocalPacketHandler(PacketType.RegionHandleRequest, HandleRegionHandleRequest);
            AddLocalPacketHandler(PacketType.ParcelInfoRequest, HandleParcelInfoRequest, false);
            AddLocalPacketHandler(PacketType.ParcelAccessListRequest, HandleParcelAccessListRequest, false);
            AddLocalPacketHandler(PacketType.ParcelAccessListUpdate, HandleParcelAccessListUpdate, false);
            AddLocalPacketHandler(PacketType.ParcelPropertiesRequest, HandleParcelPropertiesRequest, false);
            AddLocalPacketHandler(PacketType.ParcelDivide, HandleParcelDivide);
            AddLocalPacketHandler(PacketType.ParcelJoin, HandleParcelJoin);
            AddLocalPacketHandler(PacketType.ParcelPropertiesUpdate, HandleParcelPropertiesUpdate);
            AddLocalPacketHandler(PacketType.ParcelSelectObjects, HandleParcelSelectObjects);
            AddLocalPacketHandler(PacketType.ParcelObjectOwnersRequest, HandleParcelObjectOwnersRequest);
            AddLocalPacketHandler(PacketType.ParcelGodForceOwner, HandleParcelGodForceOwner);
            AddLocalPacketHandler(PacketType.ParcelRelease, HandleParcelRelease);
            AddLocalPacketHandler(PacketType.ParcelReclaim, HandleParcelReclaim);
            AddLocalPacketHandler(PacketType.ParcelReturnObjects, HandleParcelReturnObjects);
            AddLocalPacketHandler(PacketType.ParcelSetOtherCleanTime, HandleParcelSetOtherCleanTime);
            AddLocalPacketHandler(PacketType.LandStatRequest, HandleLandStatRequest);
            AddLocalPacketHandler(PacketType.ParcelDwellRequest, HandleParcelDwellRequest);
            AddLocalPacketHandler(PacketType.EstateOwnerMessage, HandleEstateOwnerMessage);
            AddLocalPacketHandler(PacketType.RequestRegionInfo, HandleRequestRegionInfo, false);
            AddLocalPacketHandler(PacketType.EstateCovenantRequest, HandleEstateCovenantRequest);
            AddLocalPacketHandler(PacketType.RequestGodlikePowers, HandleRequestGodlikePowers);
            AddLocalPacketHandler(PacketType.GodKickUser, HandleGodKickUser);
            AddLocalPacketHandler(PacketType.MoneyBalanceRequest, HandleMoneyBalanceRequest);
            AddLocalPacketHandler(PacketType.EconomyDataRequest, HandleEconomyDataRequest);
            AddLocalPacketHandler(PacketType.RequestPayPrice, HandleRequestPayPrice);
            AddLocalPacketHandler(PacketType.ObjectSaleInfo, HandleObjectSaleInfo);
            AddLocalPacketHandler(PacketType.ObjectBuy, HandleObjectBuy);
            AddLocalPacketHandler(PacketType.GetScriptRunning, HandleGetScriptRunning);
            AddLocalPacketHandler(PacketType.SetScriptRunning, HandleSetScriptRunning);
            AddLocalPacketHandler(PacketType.ScriptReset, HandleScriptReset);
            AddLocalPacketHandler(PacketType.ActivateGestures, HandleActivateGestures);
            AddLocalPacketHandler(PacketType.DeactivateGestures, HandleDeactivateGestures);
            AddLocalPacketHandler(PacketType.ObjectOwner, HandleObjectOwner);
            AddLocalPacketHandler(PacketType.AgentFOV, HandleAgentFOV, false);
            AddLocalPacketHandler(PacketType.ViewerStats, HandleViewerStats);
            AddLocalPacketHandler(PacketType.MapItemRequest, HandleMapItemRequest, true);
            AddLocalPacketHandler(PacketType.TransferAbort, HandleTransferAbort, false);
            AddLocalPacketHandler(PacketType.MuteListRequest, HandleMuteListRequest, true);
            AddLocalPacketHandler(PacketType.UseCircuitCode, HandleUseCircuitCode);
            AddLocalPacketHandler(PacketType.AgentHeightWidth, HandleAgentHeightWidth, false);
            AddLocalPacketHandler(PacketType.DirPlacesQuery, HandleDirPlacesQuery);
            AddLocalPacketHandler(PacketType.DirFindQuery, HandleDirFindQuery);
            AddLocalPacketHandler(PacketType.DirLandQuery, HandleDirLandQuery);
            AddLocalPacketHandler(PacketType.DirPopularQuery, HandleDirPopularQuery);
            AddLocalPacketHandler(PacketType.DirClassifiedQuery, HandleDirClassifiedQuery);
            AddLocalPacketHandler(PacketType.EventInfoRequest, HandleEventInfoRequest);
            AddLocalPacketHandler(PacketType.OfferCallingCard, HandleOfferCallingCard);
            AddLocalPacketHandler(PacketType.AcceptCallingCard, HandleAcceptCallingCard);
            AddLocalPacketHandler(PacketType.DeclineCallingCard, HandleDeclineCallingCard);
            AddLocalPacketHandler(PacketType.ActivateGroup, HandleActivateGroup);
            AddLocalPacketHandler(PacketType.GroupTitlesRequest, HandleGroupTitlesRequest);
            AddLocalPacketHandler(PacketType.GroupProfileRequest, HandleGroupProfileRequest);
            AddLocalPacketHandler(PacketType.GroupMembersRequest, HandleGroupMembersRequest);
            AddLocalPacketHandler(PacketType.GroupRoleDataRequest, HandleGroupRoleDataRequest);
            AddLocalPacketHandler(PacketType.GroupRoleMembersRequest, HandleGroupRoleMembersRequest);
            AddLocalPacketHandler(PacketType.CreateGroupRequest, HandleCreateGroupRequest);
            AddLocalPacketHandler(PacketType.UpdateGroupInfo, HandleUpdateGroupInfo);
            AddLocalPacketHandler(PacketType.SetGroupAcceptNotices, HandleSetGroupAcceptNotices);
            AddLocalPacketHandler(PacketType.GroupTitleUpdate, HandleGroupTitleUpdate);
            AddLocalPacketHandler(PacketType.ParcelDeedToGroup, HandleParcelDeedToGroup);
            AddLocalPacketHandler(PacketType.GroupNoticesListRequest, HandleGroupNoticesListRequest);
            AddLocalPacketHandler(PacketType.GroupNoticeRequest, HandleGroupNoticeRequest);
            AddLocalPacketHandler(PacketType.GroupRoleUpdate, HandleGroupRoleUpdate);
            AddLocalPacketHandler(PacketType.GroupRoleChanges, HandleGroupRoleChanges);
            AddLocalPacketHandler(PacketType.JoinGroupRequest, HandleJoinGroupRequest);
            AddLocalPacketHandler(PacketType.LeaveGroupRequest, HandleLeaveGroupRequest);
            AddLocalPacketHandler(PacketType.EjectGroupMemberRequest, HandleEjectGroupMemberRequest);
            AddLocalPacketHandler(PacketType.InviteGroupRequest, HandleInviteGroupRequest);
            AddLocalPacketHandler(PacketType.StartLure, HandleStartLure);
            AddLocalPacketHandler(PacketType.TeleportLureRequest, HandleTeleportLureRequest);
            AddLocalPacketHandler(PacketType.ClassifiedInfoRequest, HandleClassifiedInfoRequest);
            AddLocalPacketHandler(PacketType.ClassifiedInfoUpdate, HandleClassifiedInfoUpdate);
            AddLocalPacketHandler(PacketType.ClassifiedDelete, HandleClassifiedDelete);
            AddLocalPacketHandler(PacketType.ClassifiedGodDelete, HandleClassifiedGodDelete);
            AddLocalPacketHandler(PacketType.EventGodDelete, HandleEventGodDelete);
            AddLocalPacketHandler(PacketType.EventNotificationAddRequest, HandleEventNotificationAddRequest);
            AddLocalPacketHandler(PacketType.EventNotificationRemoveRequest, HandleEventNotificationRemoveRequest);
            AddLocalPacketHandler(PacketType.RetrieveInstantMessages, HandleRetrieveInstantMessages);
            AddLocalPacketHandler(PacketType.PickDelete, HandlePickDelete);
            AddLocalPacketHandler(PacketType.PickGodDelete, HandlePickGodDelete);
            AddLocalPacketHandler(PacketType.PickInfoUpdate, HandlePickInfoUpdate);
            AddLocalPacketHandler(PacketType.AvatarNotesUpdate, HandleAvatarNotesUpdate);
            AddLocalPacketHandler(PacketType.AvatarInterestsUpdate, HandleAvatarInterestsUpdate);
            AddLocalPacketHandler(PacketType.GrantUserRights, HandleGrantUserRights);
            AddLocalPacketHandler(PacketType.PlacesQuery, HandlePlacesQuery);
            AddLocalPacketHandler(PacketType.UpdateMuteListEntry, HandleUpdateMuteListEntry);
            AddLocalPacketHandler(PacketType.RemoveMuteListEntry, HandleRemoveMuteListEntry);
            AddLocalPacketHandler(PacketType.UserReport, HandleUserReport);
            AddLocalPacketHandler(PacketType.FindAgent, HandleFindAgent);
            AddLocalPacketHandler(PacketType.TrackAgent, HandleTrackAgent);
            AddLocalPacketHandler(PacketType.GodUpdateRegionInfo, HandleGodUpdateRegionInfoUpdate);
            AddLocalPacketHandler(PacketType.GodlikeMessage, HandleGodlikeMessage);
            AddLocalPacketHandler(PacketType.StateSave, HandleSaveStatePacket);
            AddLocalPacketHandler(PacketType.GroupAccountDetailsRequest, HandleGroupAccountDetailsRequest);
            AddLocalPacketHandler(PacketType.GroupAccountSummaryRequest, HandleGroupAccountSummaryRequest);
            AddLocalPacketHandler(PacketType.GroupAccountTransactionsRequest, HandleGroupTransactionsDetailsRequest);
            AddLocalPacketHandler(PacketType.FreezeUser, HandleFreezeUser);
            AddLocalPacketHandler(PacketType.EjectUser, HandleEjectUser);
            AddLocalPacketHandler(PacketType.ParcelBuyPass, HandleParcelBuyPass);
            AddLocalPacketHandler(PacketType.ParcelGodMarkAsContent, HandleParcelGodMarkAsContent);
            AddLocalPacketHandler(PacketType.GroupActiveProposalsRequest, HandleGroupActiveProposalsRequest);
            AddLocalPacketHandler(PacketType.GroupVoteHistoryRequest, HandleGroupVoteHistoryRequest);
            AddLocalPacketHandler(PacketType.GroupProposalBallot, HandleGroupProposalBallot);
            AddLocalPacketHandler(PacketType.SimWideDeletes, HandleSimWideDeletes);
            AddLocalPacketHandler(PacketType.SendPostcard, HandleSendPostcard);
            AddLocalPacketHandler(PacketType.TeleportCancel, HandleTeleportCancel);
            AddLocalPacketHandler(PacketType.ViewerStartAuction, HandleViewerStartAuction);
            AddLocalPacketHandler(PacketType.ParcelDisableObjects, HandleParcelDisableObjects);
        }

        #region Packet Handlers

        #region Scene/Avatar

        private bool HandleAgentUpdate(IClientAPI sener, Packet Pack)
        {
            if (OnAgentUpdate != null)
            {
                bool update = false;
                //bool forcedUpdate = false;
                AgentUpdatePacket agenUpdate = (AgentUpdatePacket) Pack;

                #region Packet Session and User Check

                if (agenUpdate.AgentData.SessionID != SessionId || agenUpdate.AgentData.AgentID != AgentId)
                    return false;

                #endregion

                AgentUpdatePacket.AgentDataBlock x = agenUpdate.AgentData;

                // We can only check when we have something to check
                // against.

                if (lastarg != null)
                {
                    update =
                        (
                            (x.BodyRotation != lastarg.BodyRotation) ||
                            (x.CameraAtAxis != lastarg.CameraAtAxis) ||
                            (x.CameraCenter != lastarg.CameraCenter) ||
                            (x.CameraLeftAxis != lastarg.CameraLeftAxis) ||
                            (x.CameraUpAxis != lastarg.CameraUpAxis) ||
                            (x.ControlFlags != lastarg.ControlFlags) ||
                            (x.Far != lastarg.Far) ||
                            (x.Flags != lastarg.Flags) ||
                            (x.State != lastarg.State) ||
                            (x.HeadRotation != lastarg.HeadRotation) ||
                            (x.SessionID != lastarg.SessionID) ||
                            (x.AgentID != lastarg.AgentID)
                        );
                }
                else
                {
                    //forcedUpdate = true;
                    update = true;
                }

                // These should be ordered from most-likely to
                // least likely to change. I've made an initial
                // guess at that.

                if (update)
                {
                    AgentUpdateArgs arg = new AgentUpdateArgs
                                              {
                                                  AgentID = x.AgentID,
                                                  BodyRotation = x.BodyRotation,
                                                  CameraAtAxis = x.CameraAtAxis,
                                                  CameraCenter = x.CameraCenter,
                                                  CameraLeftAxis = x.CameraLeftAxis,
                                                  CameraUpAxis = x.CameraUpAxis,
                                                  ControlFlags = x.ControlFlags,
                                                  Far = x.Far,
                                                  Flags = x.Flags,
                                                  HeadRotation = x.HeadRotation,
                                                  SessionID = x.SessionID,
                                                  State = x.State
                                              };
                    UpdateAgent handlerAgentUpdate = OnAgentUpdate;
                    lastarg = arg; // save this set of arguments for nexttime
                    if (handlerAgentUpdate != null)
                        OnAgentUpdate(this, arg);

                    handlerAgentUpdate = null;
                }
            }

            return true;
        }

        private bool HandleMoneyTransferRequest(IClientAPI sender, Packet Pack)
        {
            MoneyTransferRequestPacket money = (MoneyTransferRequestPacket) Pack;
            // validate the agent owns the agentID and sessionID
            if (money.MoneyData.SourceID == sender.AgentId && money.AgentData.AgentID == sender.AgentId &&
                money.AgentData.SessionID == sender.SessionId)
            {
                MoneyTransferRequest handlerMoneyTransferRequest = OnMoneyTransferRequest;
                if (handlerMoneyTransferRequest != null)
                {
                    handlerMoneyTransferRequest(money.MoneyData.SourceID, money.MoneyData.DestID,
                                                money.MoneyData.Amount, money.MoneyData.TransactionType,
                                                Util.FieldToString(money.MoneyData.Description));
                }

                return true;
            }

            return false;
        }

        private bool HandleParcelGodMarkAsContent(IClientAPI client, Packet Packet)
        {
            ParcelGodMarkAsContentPacket ParcelGodMarkAsContent =
                (ParcelGodMarkAsContentPacket) Packet;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (ParcelGodMarkAsContent.AgentData.SessionID != SessionId ||
                    ParcelGodMarkAsContent.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ParcelGodMark ParcelGodMarkAsContentHandler = OnParcelGodMark;
            if (ParcelGodMarkAsContentHandler != null)
            {
                ParcelGodMarkAsContentHandler(this,
                                              ParcelGodMarkAsContent.AgentData.AgentID,
                                              ParcelGodMarkAsContent.ParcelData.LocalID);
                return true;
            }
            return false;
        }

        private bool HandleFreezeUser(IClientAPI client, Packet Packet)
        {
            FreezeUserPacket FreezeUser = (FreezeUserPacket) Packet;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (FreezeUser.AgentData.SessionID != SessionId ||
                    FreezeUser.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            FreezeUserUpdate FreezeUserHandler = OnParcelFreezeUser;
            if (FreezeUserHandler != null)
            {
                FreezeUserHandler(this,
                                  FreezeUser.AgentData.AgentID,
                                  FreezeUser.Data.Flags,
                                  FreezeUser.Data.TargetID);
                return true;
            }
            return false;
        }

        private bool HandleEjectUser(IClientAPI client, Packet Packet)
        {
            EjectUserPacket EjectUser =
                (EjectUserPacket) Packet;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (EjectUser.AgentData.SessionID != SessionId ||
                    EjectUser.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            EjectUserUpdate EjectUserHandler = OnParcelEjectUser;
            if (EjectUserHandler != null)
            {
                EjectUserHandler(this,
                                 EjectUser.AgentData.AgentID,
                                 EjectUser.Data.Flags,
                                 EjectUser.Data.TargetID);
                return true;
            }
            return false;
        }

        private bool HandleParcelBuyPass(IClientAPI client, Packet Packet)
        {
            ParcelBuyPassPacket ParcelBuyPass =
                (ParcelBuyPassPacket) Packet;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (ParcelBuyPass.AgentData.SessionID != SessionId ||
                    ParcelBuyPass.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ParcelBuyPass ParcelBuyPassHandler = OnParcelBuyPass;
            if (ParcelBuyPassHandler != null)
            {
                ParcelBuyPassHandler(this,
                                     ParcelBuyPass.AgentData.AgentID,
                                     ParcelBuyPass.ParcelData.LocalID);
                return true;
            }
            return false;
        }

        private bool HandleParcelBuyRequest(IClientAPI sender, Packet Pack)
        {
            ParcelBuyPacket parcel = (ParcelBuyPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (parcel.AgentData.SessionID != SessionId ||
                    parcel.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (parcel.AgentData.AgentID == AgentId && parcel.AgentData.SessionID == SessionId)
            {
                ParcelBuy handlerParcelBuy = OnParcelBuy;
                if (handlerParcelBuy != null)
                {
                    handlerParcelBuy(parcel.AgentData.AgentID, parcel.Data.GroupID, parcel.Data.Final,
                                     parcel.Data.IsGroupOwned,
                                     parcel.Data.RemoveContribution, parcel.Data.LocalID, parcel.ParcelData.Area,
                                     parcel.ParcelData.Price,
                                     false);
                }
                return true;
            }
            return false;
        }

        private bool HandleUUIDGroupNameRequest(IClientAPI sender, Packet Pack)
        {
            UUIDGroupNameRequestPacket upack = (UUIDGroupNameRequestPacket) Pack;

            foreach (UUIDGroupNameRequestPacket.UUIDNameBlockBlock t in upack.UUIDNameBlock)
            {
                UUIDNameRequest handlerUUIDGroupNameRequest = OnUUIDGroupNameRequest;
                if (handlerUUIDGroupNameRequest != null)
                {
                    handlerUUIDGroupNameRequest(t.ID, this);
                }
            }

            return true;
        }

        public bool HandleGenericMessage(IClientAPI sender, Packet pack)
        {
            GenericMessagePacket gmpack = (GenericMessagePacket) pack;
            if (m_genericPacketHandlers.Count == 0) return false;
            if (gmpack.AgentData.SessionID != SessionId) return false;

            GenericMessage handlerGenericMessage = null;

            string method = Util.FieldToString(gmpack.MethodData.Method).ToLower().Trim();

            if (m_genericPacketHandlers.TryGetValue(method, out handlerGenericMessage))
            {
                List<string> msg = new List<string>();
                List<byte[]> msgBytes = new List<byte[]>();

                if (handlerGenericMessage != null)
                {
                    foreach (GenericMessagePacket.ParamListBlock block in gmpack.ParamList)
                    {
                        msg.Add(Util.FieldToString(block.Parameter));
                        msgBytes.Add(block.Parameter);
                    }
                    try
                    {
                        if (OnBinaryGenericMessage != null)
                        {
                            OnBinaryGenericMessage(this, method, msgBytes.ToArray());
                        }
                        handlerGenericMessage(sender, method, msg);
                        return true;
                    }
                    catch (Exception e)
                    {
                        MainConsole.Instance.ErrorFormat(
                            "[LLCLIENTVIEW]: Exeception when handling generic message {0}{1}", e.Message, e.StackTrace);
                    }
                }
            }

            //MainConsole.Instance.Debug("[LLCLIENTVIEW]: Not handling GenericMessage with method-type of: " + method);
            return false;
        }

        public bool HandleObjectGroupRequest(IClientAPI sender, Packet Pack)
        {
            ObjectGroupPacket ogpack = (ObjectGroupPacket) Pack;
            if (ogpack.AgentData.SessionID != SessionId) return false;

            RequestObjectPropertiesFamily handlerObjectGroupRequest = OnObjectGroupRequest;
            if (handlerObjectGroupRequest != null)
            {
                foreach (ObjectGroupPacket.ObjectDataBlock t in ogpack.ObjectData)
                {
                    handlerObjectGroupRequest(this, ogpack.AgentData.GroupID, t.ObjectLocalID,
                                              UUID.Zero);
                }
            }
            return true;
        }

        private bool HandleViewerEffect(IClientAPI sender, Packet Pack)
        {
            ViewerEffectPacket viewer = (ViewerEffectPacket) Pack;
            if (viewer.AgentData.SessionID != SessionId) return false;
            ViewerEffectEventHandler handlerViewerEffect = OnViewerEffect;
            if (handlerViewerEffect != null)
            {
                int length = viewer.Effect.Length;
                List<ViewerEffectEventHandlerArg> args = new List<ViewerEffectEventHandlerArg>(length);
                for (int i = 0; i < length; i++)
                {
                    //copy the effects block arguments into the event handler arg.
                    ViewerEffectEventHandlerArg argument = new ViewerEffectEventHandlerArg
                                                               {
                                                                   AgentID = viewer.Effect[i].AgentID,
                                                                   Color = viewer.Effect[i].Color,
                                                                   Duration = viewer.Effect[i].Duration,
                                                                   ID = viewer.Effect[i].ID,
                                                                   Type = viewer.Effect[i].Type,
                                                                   TypeData = viewer.Effect[i].TypeData
                                                               };
                    args.Add(argument);
                }

                handlerViewerEffect(sender, args);
            }

            return true;
        }

        private bool HandleAvatarPropertiesRequest(IClientAPI sender, Packet Pack)
        {
            AvatarPropertiesRequestPacket avatarProperties = (AvatarPropertiesRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (avatarProperties.AgentData.SessionID != SessionId ||
                    avatarProperties.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            RequestAvatarProperties handlerRequestAvatarProperties = OnRequestAvatarProperties;
            if (handlerRequestAvatarProperties != null)
            {
                handlerRequestAvatarProperties(this, avatarProperties.AgentData.AvatarID);
            }
            return true;
        }

        private bool HandleChatFromViewer(IClientAPI sender, Packet Pack)
        {
            ChatFromViewerPacket inchatpack = (ChatFromViewerPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (inchatpack.AgentData.SessionID != SessionId ||
                    inchatpack.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            string fromName = String.Empty; //ClientAvatar.firstname + " " + ClientAvatar.lastname;
            byte[] message = inchatpack.ChatData.Message;
            byte type = inchatpack.ChatData.Type;
            Vector3 fromPos = new Vector3(); // ClientAvatar.Pos;
            // UUID fromAgentID = AgentId;

            int channel = inchatpack.ChatData.Channel;

            if (OnChatFromClient != null)
            {
                OSChatMessage args = new OSChatMessage
                                         {
                                             Channel = channel,
                                             From = fromName,
                                             Message = Utils.BytesToString(message),
                                             Type = (ChatTypeEnum) type,
                                             Position = fromPos,
                                             Scene = Scene,
                                             Sender = this,
                                             SenderUUID = AgentId
                                         };

                HandleChatFromClient(args);
            }
            return true;
        }

        public void HandleChatFromClient(OSChatMessage args)
        {
            ChatMessage handlerChatFromClient = OnChatFromClient;
            if (handlerChatFromClient != null)
                handlerChatFromClient(this, args);
        }

        private bool HandlerAvatarPropertiesUpdate(IClientAPI sender, Packet Pack)
        {
            AvatarPropertiesUpdatePacket avatarProps = (AvatarPropertiesUpdatePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (avatarProps.AgentData.SessionID != SessionId ||
                    avatarProps.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            UpdateAvatarProperties handlerUpdateAvatarProperties = OnUpdateAvatarProperties;
            if (handlerUpdateAvatarProperties != null)
            {
                AvatarPropertiesUpdatePacket.PropertiesDataBlock Properties = avatarProps.PropertiesData;

                handlerUpdateAvatarProperties(this,
                                              Utils.BytesToString(Properties.AboutText),
                                              Utils.BytesToString(Properties.FLAboutText),
                                              Properties.FLImageID,
                                              Properties.ImageID,
                                              Utils.BytesToString(Properties.ProfileURL),
                                              Properties.AllowPublish,
                                              Properties.MaturePublish);
            }
            return true;
        }

        private bool HandlerScriptDialogReply(IClientAPI sender, Packet Pack)
        {
            ScriptDialogReplyPacket rdialog = (ScriptDialogReplyPacket) Pack;

            //MainConsole.Instance.DebugFormat("[CLIENT]: Received ScriptDialogReply from {0}", rdialog.Data.ObjectID);

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (rdialog.AgentData.SessionID != SessionId ||
                    rdialog.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            int ch = rdialog.Data.ChatChannel;
            byte[] msg = rdialog.Data.ButtonLabel;
            if (OnChatFromClient != null)
            {
                OSChatMessage args = new OSChatMessage
                                         {
                                             Channel = ch,
                                             From = String.Empty,
                                             Message = Utils.BytesToString(msg),
                                             Type = ChatTypeEnum.Shout,
                                             Position = new Vector3(),
                                             Scene = Scene,
                                             Sender = this
                                         };
                ChatMessage handlerChatFromClient2 = OnChatFromClient;
                if (handlerChatFromClient2 != null)
                    handlerChatFromClient2(this, args);
            }

            return true;
        }

        private bool HandlerImprovedInstantMessage(IClientAPI sender, Packet Pack)
        {
            ImprovedInstantMessagePacket msgpack = (ImprovedInstantMessagePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (msgpack.AgentData.SessionID != SessionId ||
                    msgpack.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            string IMfromName = Util.FieldToString(msgpack.MessageBlock.FromAgentName);
            string IMmessage = Utils.BytesToString(msgpack.MessageBlock.Message);
            ImprovedInstantMessage handlerInstantMessage = OnInstantMessage;

            if (handlerInstantMessage != null)
            {
                GridInstantMessage im = new GridInstantMessage(Scene,
                                                               msgpack.AgentData.AgentID,
                                                               IMfromName,
                                                               msgpack.MessageBlock.ToAgentID,
                                                               msgpack.MessageBlock.Dialog,
                                                               msgpack.MessageBlock.FromGroup,
                                                               IMmessage,
                                                               msgpack.MessageBlock.ID,
                                                               msgpack.MessageBlock.Offline != 0,
                                                               msgpack.MessageBlock.Position,
                                                               msgpack.MessageBlock.BinaryBucket);

                PreSendImprovedInstantMessage handlerPreSendInstantMessage = OnPreSendInstantMessage;
                if (handlerPreSendInstantMessage != null)
                {
                    if (handlerPreSendInstantMessage.GetInvocationList().Cast<PreSendImprovedInstantMessage>().Any(
                            d => d(this, im)))
                    {
                        return true; //handled
                    }
                }
                handlerInstantMessage(this, im);
            }
            return true;
        }

        private bool HandlerAcceptFriendship(IClientAPI sender, Packet Pack)
        {
            AcceptFriendshipPacket afriendpack = (AcceptFriendshipPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (afriendpack.AgentData.SessionID != SessionId ||
                    afriendpack.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            // My guess is this is the folder to stick the calling card into

            UUID agentID = afriendpack.AgentData.AgentID;
            UUID transactionID = afriendpack.TransactionBlock.TransactionID;

#if (!ISWIN)
            List<UUID> callingCardFolders = new List<UUID>();
            foreach (AcceptFriendshipPacket.FolderDataBlock t in afriendpack.FolderData)
                callingCardFolders.Add(t.FolderID);
#else
            List<UUID> callingCardFolders = afriendpack.FolderData.Select(t => t.FolderID).ToList();
#endif

            FriendActionDelegate handlerApproveFriendRequest = OnApproveFriendRequest;
            if (handlerApproveFriendRequest != null)
            {
                handlerApproveFriendRequest(this, agentID, transactionID, callingCardFolders);
            }
            return true;
        }

        private bool HandlerDeclineFriendship(IClientAPI sender, Packet Pack)
        {
            DeclineFriendshipPacket dfriendpack = (DeclineFriendshipPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (dfriendpack.AgentData.SessionID != SessionId ||
                    dfriendpack.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (OnDenyFriendRequest != null)
            {
                OnDenyFriendRequest(this,
                                    dfriendpack.AgentData.AgentID,
                                    dfriendpack.TransactionBlock.TransactionID,
                                    null);
            }
            return true;
        }

        private bool HandlerTerminateFrendship(IClientAPI sender, Packet Pack)
        {
            TerminateFriendshipPacket tfriendpack = (TerminateFriendshipPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (tfriendpack.AgentData.SessionID != SessionId ||
                    tfriendpack.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            UUID listOwnerAgentID = tfriendpack.AgentData.AgentID;
            UUID exFriendID = tfriendpack.ExBlock.OtherID;

            FriendshipTermination handlerTerminateFriendship = OnTerminateFriendship;
            if (handlerTerminateFriendship != null)
            {
                handlerTerminateFriendship(this, listOwnerAgentID, exFriendID);
            }
            return true;
        }

        private bool HandleFindAgent(IClientAPI client, Packet Packet)
        {
            FindAgentPacket FindAgent =
                (FindAgentPacket) Packet;

            FindAgentUpdate FindAgentHandler = OnFindAgent;
            if (FindAgentHandler != null)
            {
                FindAgentHandler(this, FindAgent.AgentBlock.Hunter, FindAgent.AgentBlock.Prey);
                return true;
            }
            return false;
        }

        private bool HandleTrackAgent(IClientAPI client, Packet Packet)
        {
            TrackAgentPacket TrackAgent =
                (TrackAgentPacket) Packet;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (TrackAgent.AgentData.SessionID != SessionId ||
                    TrackAgent.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            TrackAgentUpdate TrackAgentHandler = OnTrackAgent;
            if (TrackAgentHandler != null)
            {
                TrackAgentHandler(this,
                                  TrackAgent.AgentData.AgentID,
                                  TrackAgent.TargetData.PreyID);
                return true;
            }
            return false;
        }

        private bool HandlerRezObject(IClientAPI sender, Packet Pack)
        {
            RezObjectPacket rezPacket = (RezObjectPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (rezPacket.AgentData.SessionID != SessionId ||
                    rezPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            RezObject handlerRezObject = OnRezObject;
            if (handlerRezObject != null)
            {
                handlerRezObject(this, rezPacket.InventoryData.ItemID, rezPacket.RezData.RayEnd,
                                 rezPacket.RezData.RayStart, rezPacket.RezData.RayTargetID,
                                 rezPacket.RezData.BypassRaycast, rezPacket.RezData.RayEndIsIntersection,
                                 rezPacket.RezData.RezSelected, rezPacket.RezData.RemoveItem,
                                 rezPacket.RezData.FromTaskID);
            }
            return true;
        }

        private bool HandlerRezObjectFromNotecard(IClientAPI sender, Packet Pack)
        {
            RezObjectFromNotecardPacket rezPacket = (RezObjectFromNotecardPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (rezPacket.AgentData.SessionID != SessionId ||
                    rezPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            RezObject handlerRezObject = OnRezObject;
            if (handlerRezObject != null)
            {
                handlerRezObject(this, rezPacket.InventoryData[0].ItemID, rezPacket.RezData.RayEnd,
                                 rezPacket.RezData.RayStart, rezPacket.RezData.RayTargetID,
                                 rezPacket.RezData.BypassRaycast, rezPacket.RezData.RayEndIsIntersection,
                                 rezPacket.RezData.RezSelected, rezPacket.RezData.RemoveItem,
                                 rezPacket.RezData.FromTaskID);
            }
            return true;
        }

        private bool HandlerDeRezObject(IClientAPI sender, Packet Pack)
        {
            DeRezObjectPacket DeRezPacket = (DeRezObjectPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (DeRezPacket.AgentData.SessionID != SessionId ||
                    DeRezPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            DeRezObject handlerDeRezObject = OnDeRezObject;
            if (handlerDeRezObject != null)
            {
#if (!ISWIN)
                List<uint> deRezIDs = new List<uint>();
                foreach (DeRezObjectPacket.ObjectDataBlock data in DeRezPacket.ObjectData)
                    deRezIDs.Add(data.ObjectLocalID);
#else
                List<uint> deRezIDs = DeRezPacket.ObjectData.Select(data => data.ObjectLocalID).ToList();
#endif

                // It just so happens that the values on the DeRezAction enumerator match the Destination
                // values given by a Second Life client
                handlerDeRezObject(this, deRezIDs,
                                   DeRezPacket.AgentBlock.GroupID,
                                   (DeRezAction) DeRezPacket.AgentBlock.Destination,
                                   DeRezPacket.AgentBlock.DestinationID);
            }
            return true;
        }

        private bool HandlerModifyLand(IClientAPI sender, Packet Pack)
        {
            ModifyLandPacket modify = (ModifyLandPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (modify.AgentData.SessionID != SessionId ||
                    modify.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            //MainConsole.Instance.Info("[LAND]: LAND:" + modify.ToString());
            if (modify.ParcelData.Length > 0)
            {
                if (OnModifyTerrain != null)
                {
                    for (int i = 0; i < modify.ParcelData.Length; i++)
                    {
                        ModifyTerrain handlerModifyTerrain = OnModifyTerrain;
                        if (handlerModifyTerrain != null)
                        {
                            handlerModifyTerrain(AgentId, modify.ModifyBlock.Height, modify.ModifyBlock.Seconds,
                                                 modify.ModifyBlock.BrushSize,
                                                 modify.ModifyBlock.Action, modify.ParcelData[i].North,
                                                 modify.ParcelData[i].West, modify.ParcelData[i].South,
                                                 modify.ParcelData[i].East, AgentId,
                                                 modify.ModifyBlockExtended[i].BrushSize);
                        }
                    }
                }
            }

            return true;
        }

        private bool HandlerRegionHandshakeReply(IClientAPI sender, Packet Pack)
        {
            Action<IClientAPI> handlerRegionHandShakeReply = OnRegionHandShakeReply;
            if (handlerRegionHandShakeReply != null)
            {
                handlerRegionHandShakeReply(this);
            }

            return true;
        }

        private bool HandlerAgentWearablesRequest(IClientAPI sender, Packet Pack)
        {
            GenericCall1 handlerRequestWearables = OnRequestWearables;

            if (handlerRequestWearables != null)
            {
                handlerRequestWearables(this);
            }

            Action<IClientAPI> handlerRequestAvatarsData = OnRequestAvatarsData;

            if (handlerRequestAvatarsData != null)
            {
                handlerRequestAvatarsData(this);
            }

            return true;
        }

        private bool HandlerAgentSetAppearance(IClientAPI sender, Packet Pack)
        {
            AgentSetAppearancePacket appear = (AgentSetAppearancePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (appear.AgentData.SessionID != SessionId ||
                    appear.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            SetAppearance handlerSetAppearance = OnSetAppearance;
            if (handlerSetAppearance != null)
            {
                // Temporarily protect ourselves from the mantis #951 failure.
                // However, we could do this for several other handlers where a failure isn't terminal
                // for the client session anyway, in order to protect ourselves against bad code in plugins
                try
                {
                    byte[] visualparams = new byte[appear.VisualParam.Length];
                    for (int i = 0; i < appear.VisualParam.Length; i++)
                        visualparams[i] = appear.VisualParam[i].ParamValue;

                    Primitive.TextureEntry te = null;
                    if (appear.ObjectData.TextureEntry.Length > 1)
                        te = new Primitive.TextureEntry(appear.ObjectData.TextureEntry, 0,
                                                        appear.ObjectData.TextureEntry.Length);

                    WearableCache[] items = new WearableCache[appear.WearableData.Length];
                    for (int i = 0; i < appear.WearableData.Length; i++)
                    {
                        WearableCache cache = new WearableCache
                                                  {
                                                      CacheID = appear.WearableData[i].CacheID,
                                                      TextureIndex = appear.WearableData[i].TextureIndex
                                                  };
                        items[i] = cache;
                    }
                    handlerSetAppearance(this, te, visualparams, items, appear.AgentData.SerialNum);
                }
                catch (Exception e)
                {
                    MainConsole.Instance.ErrorFormat(
                        "[CLIENT VIEW]: AgentSetApperance packet handler threw an exception, {0}",
                        e);
                }
            }

            return true;
        }

        ///<summary>
        ///  Send a response back to a client when it asks the asset server (via the region server) if it has
        ///  its appearance texture cached.
        ///
        ///  At the moment, we always reply that there is no cached texture.
        ///</summary>
        ///<param name = "simclient"></param>
        ///<param name = "packet"></param>
        ///<returns></returns>
        private bool HandleAgentTextureCached(IClientAPI simclient, Packet packet)
        {
            //MainConsole.Instance.Debug("texture cached: " + packet.ToString());
            AgentCachedTexturePacket cachedtex = (AgentCachedTexturePacket) packet;

            if (cachedtex.AgentData.SessionID != SessionId) return false;

#if (!ISWIN)
            List<CachedAgentArgs> args = new List<CachedAgentArgs>();
            foreach (AgentCachedTexturePacket.WearableDataBlock t in cachedtex.WearableData)
                args.Add(new CachedAgentArgs {ID = t.ID, TextureIndex = t.TextureIndex});
#else
            List<CachedAgentArgs> args =
                cachedtex.WearableData.Select(t => new CachedAgentArgs {ID = t.ID, TextureIndex = t.TextureIndex}).
                    ToList();
#endif

            AgentCachedTextureRequest actr = OnAgentCachedTextureRequest;
            if (actr != null)
                actr(this, args);

            return true;
        }

        public void SendAgentCachedTexture(List<CachedAgentArgs> args)
        {
            AgentCachedTextureResponsePacket cachedresp =
                (AgentCachedTextureResponsePacket) PacketPool.Instance.GetPacket(PacketType.AgentCachedTextureResponse);
            cachedresp.AgentData.AgentID = AgentId;
            cachedresp.AgentData.SessionID = m_sessionId;
            cachedresp.AgentData.SerialNum = m_cachedTextureSerial;
            m_cachedTextureSerial++;
            cachedresp.WearableData =
                new AgentCachedTextureResponsePacket.WearableDataBlock[args.Count];
            for (int i = 0; i < args.Count; i++)
            {
                cachedresp.WearableData[i] = new AgentCachedTextureResponsePacket.WearableDataBlock
                                                 {
                                                     TextureIndex = args[i].TextureIndex,
                                                     TextureID = args[i].ID,
                                                     HostName = new byte[0]
                                                 };
            }

            cachedresp.Header.Zerocoded = true;
            OutPacket(cachedresp, ThrottleOutPacketType.Texture);
        }

        private bool HandlerAgentIsNowWearing(IClientAPI sender, Packet Pack)
        {
            if (OnAvatarNowWearing != null)
            {
                AgentIsNowWearingPacket nowWearing = (AgentIsNowWearingPacket) Pack;

                #region Packet Session and User Check

                if (m_checkPackets)
                {
                    if (nowWearing.AgentData.SessionID != SessionId ||
                        nowWearing.AgentData.AgentID != AgentId)
                        return true;
                }

                #endregion

                AvatarWearingArgs wearingArgs = new AvatarWearingArgs();
#if (!ISWIN)
                foreach (AgentIsNowWearingPacket.WearableDataBlock t in nowWearing.WearableData)
                {
                    AvatarWearingArgs.Wearable wearable = new AvatarWearingArgs.Wearable(t.ItemID, t.WearableType);
                    wearingArgs.NowWearing.Add(wearable);
                }
#else
                foreach (AvatarWearingArgs.Wearable wearable in nowWearing.WearableData.Select(t => new AvatarWearingArgs.Wearable(t.ItemID,
                                                                                                                 t.WearableType)))
                {
                    wearingArgs.NowWearing.Add(wearable);
                }
#endif

                AvatarNowWearing handlerAvatarNowWearing = OnAvatarNowWearing;
                if (handlerAvatarNowWearing != null)
                {
                    handlerAvatarNowWearing(this, wearingArgs);
                }
            }
            return true;
        }

        private bool HandlerRezSingleAttachmentFromInv(IClientAPI sender, Packet Pack)
        {
            RezSingleAttachmentFromInv handlerRezSingleAttachment = OnRezSingleAttachmentFromInv;
            if (handlerRezSingleAttachment != null)
            {
                RezSingleAttachmentFromInvPacket rez = (RezSingleAttachmentFromInvPacket) Pack;

                #region Packet Session and User Check

                if (m_checkPackets)
                {
                    if (rez.AgentData.SessionID != SessionId ||
                        rez.AgentData.AgentID != AgentId)
                        return true;
                }

                #endregion

                handlerRezSingleAttachment(this, rez.ObjectData.ItemID,
                                           rez.ObjectData.AttachmentPt);
            }

            return true;
        }

        private bool HandlerRezRestoreToWorld(IClientAPI sender, Packet Pack)
        {
            RezSingleAttachmentFromInv handlerRezSingleAttachment = OnRezSingleAttachmentFromInv;
            if (handlerRezSingleAttachment != null)
            {
                RezRestoreToWorldPacket rez = (RezRestoreToWorldPacket) Pack;

                #region Packet Session and User Check

                if (m_checkPackets)
                {
                    if (rez.AgentData.SessionID != SessionId ||
                        rez.AgentData.AgentID != AgentId)
                        return true;
                }

                #endregion

                handlerRezSingleAttachment(this, rez.InventoryData.ItemID,
                                           0);
            }

            return true;
        }

        private bool HandleRezMultipleAttachmentsFromInv(IClientAPI sender, Packet Pack)
        {
            RezSingleAttachmentFromInv handlerRezMultipleAttachments = OnRezSingleAttachmentFromInv;

            if (handlerRezMultipleAttachments != null)
            {
                RezMultipleAttachmentsFromInvPacket rez = (RezMultipleAttachmentsFromInvPacket) Pack;

                #region Packet Session and User Check

                if (m_checkPackets)
                {
                    if (rez.AgentData.SessionID != SessionId ||
                        rez.AgentData.AgentID != AgentId)
                        return true;
                }

                #endregion

                foreach (RezMultipleAttachmentsFromInvPacket.ObjectDataBlock obj in rez.ObjectData)
                {
                    handlerRezMultipleAttachments(this, obj.ItemID, obj.AttachmentPt);
                }
            }

            return true;
        }

        private bool HandleDetachAttachmentIntoInv(IClientAPI sender, Packet Pack)
        {
            UUIDNameRequest handlerDetachAttachmentIntoInv = OnDetachAttachmentIntoInv;
            if (handlerDetachAttachmentIntoInv != null)
            {
                DetachAttachmentIntoInvPacket detachtoInv = (DetachAttachmentIntoInvPacket) Pack;

                #region Packet Session and User Check

//TODO!
                // UNSUPPORTED ON THIS PACKET

                #endregion

                UUID itemID = detachtoInv.ObjectData.ItemID;
                // UUID ATTACH_agentID = detachtoInv.ObjectData.AgentID;

                handlerDetachAttachmentIntoInv(itemID, this);
            }
            return true;
        }

        private bool HandleObjectAttach(IClientAPI sender, Packet Pack)
        {
            if (OnObjectAttach != null)
            {
                ObjectAttachPacket att = (ObjectAttachPacket) Pack;

                #region Packet Session and User Check

                if (m_checkPackets)
                {
                    if (att.AgentData.SessionID != SessionId ||
                        att.AgentData.AgentID != AgentId)
                        return true;
                }

                #endregion

                ObjectAttach handlerObjectAttach = OnObjectAttach;

                if (handlerObjectAttach != null)
                {
                    if (att.ObjectData.Length > 0)
                    {
                        handlerObjectAttach(this, att.ObjectData[0].ObjectLocalID, att.AgentData.AttachmentPoint, false);
                    }
                }
            }
            return true;
        }

        private bool HandleObjectDetach(IClientAPI sender, Packet Pack)
        {
            ObjectDetachPacket dett = (ObjectDetachPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (dett.AgentData.SessionID != SessionId ||
                    dett.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            foreach (ObjectDetachPacket.ObjectDataBlock t in dett.ObjectData)
            {
                uint obj = t.ObjectLocalID;
                ObjectDeselect handlerObjectDetach = OnObjectDetach;
                if (handlerObjectDetach != null)
                {
                    handlerObjectDetach(obj, this);
                }
            }
            return true;
        }

        private bool HandleObjectDrop(IClientAPI sender, Packet Pack)
        {
            ObjectDropPacket dropp = (ObjectDropPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (dropp.AgentData.SessionID != SessionId ||
                    dropp.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            foreach (ObjectDropPacket.ObjectDataBlock t in dropp.ObjectData)
            {
                uint obj = t.ObjectLocalID;
                ObjectDrop handlerObjectDrop = OnObjectDrop;
                if (handlerObjectDrop != null)
                {
                    handlerObjectDrop(obj, this);
                }
            }
            return true;
        }

        private bool HandleSetAlwaysRun(IClientAPI sender, Packet Pack)
        {
            SetAlwaysRunPacket run = (SetAlwaysRunPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (run.AgentData.SessionID != SessionId ||
                    run.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            SetAlwaysRun handlerSetAlwaysRun = OnSetAlwaysRun;
            if (handlerSetAlwaysRun != null)
                handlerSetAlwaysRun(this, run.AgentData.AlwaysRun);

            return true;
        }

        private bool HandleCompleteAgentMovement(IClientAPI sender, Packet Pack)
        {
            GenericCall1 handlerCompleteMovementToRegion = OnCompleteMovementToRegion;
            if (handlerCompleteMovementToRegion != null)
            {
                handlerCompleteMovementToRegion(sender);
            }
            handlerCompleteMovementToRegion = null;

            return true;
        }

        private bool HandleAgentAnimation(IClientAPI sender, Packet Pack)
        {
            AgentAnimationPacket AgentAni = (AgentAnimationPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (AgentAni.AgentData.SessionID != SessionId ||
                    AgentAni.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            foreach (AgentAnimationPacket.AnimationListBlock t in AgentAni.AnimationList)
            {
                if (t.StartAnim)
                {
                    StartAnim handlerStartAnim = OnStartAnim;
                    if (handlerStartAnim != null)
                    {
                        handlerStartAnim(this, t.AnimID);
                    }
                }
                else
                {
                    StopAnim handlerStopAnim = OnStopAnim;
                    if (handlerStopAnim != null)
                    {
                        handlerStopAnim(this, t.AnimID);
                    }
                }
            }
            return true;
        }

        private bool HandleAgentRequestSit(IClientAPI sender, Packet Pack)
        {
            if (OnAgentRequestSit != null)
            {
                AgentRequestSitPacket agentRequestSit = (AgentRequestSitPacket) Pack;

                #region Packet Session and User Check

                if (m_checkPackets)
                {
                    if (agentRequestSit.AgentData.SessionID != SessionId ||
                        agentRequestSit.AgentData.AgentID != AgentId)
                        return true;
                }

                #endregion

                AgentRequestSit handlerAgentRequestSit = OnAgentRequestSit;
                if (handlerAgentRequestSit != null)
                    handlerAgentRequestSit(this,
                                           agentRequestSit.TargetObject.TargetID, agentRequestSit.TargetObject.Offset);
            }
            return true;
        }

        private bool HandleAgentSit(IClientAPI sender, Packet Pack)
        {
            if (OnAgentSit != null)
            {
                AgentSitPacket agentSit = (AgentSitPacket) Pack;

                #region Packet Session and User Check

                if (m_checkPackets)
                {
                    if (agentSit.AgentData.SessionID != SessionId ||
                        agentSit.AgentData.AgentID != AgentId)
                        return true;
                }

                #endregion

                AgentSit handlerAgentSit = OnAgentSit;
                if (handlerAgentSit != null)
                {
                    OnAgentSit(this, agentSit.AgentData.AgentID);
                }
            }
            return true;
        }

        private bool HandleSoundTrigger(IClientAPI sender, Packet Pack)
        {
            SoundTriggerPacket soundTriggerPacket = (SoundTriggerPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
//TODO!
                // UNSUPPORTED ON THIS PACKET
            }

            #endregion

            SoundTrigger handlerSoundTrigger = OnSoundTrigger;
            if (handlerSoundTrigger != null)
            {
                // UUIDS are sent as zeroes by the client, substitute agent's id
                handlerSoundTrigger(soundTriggerPacket.SoundData.SoundID, AgentId,
                                    AgentId, AgentId,
                                    soundTriggerPacket.SoundData.Gain, soundTriggerPacket.SoundData.Position,
                                    soundTriggerPacket.SoundData.Handle, 0);
            }
            return true;
        }

        private bool HandleAvatarPickerRequest(IClientAPI sender, Packet Pack)
        {
            AvatarPickerRequestPacket avRequestQuery = (AvatarPickerRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (avRequestQuery.AgentData.SessionID != SessionId ||
                    avRequestQuery.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            AvatarPickerRequestPacket.AgentDataBlock Requestdata = avRequestQuery.AgentData;
            AvatarPickerRequestPacket.DataBlock querydata = avRequestQuery.Data;
            //MainConsole.Instance.Debug("Agent Sends:" + Utils.BytesToString(querydata.Name));

            AvatarPickerRequest handlerAvatarPickerRequest = OnAvatarPickerRequest;
            if (handlerAvatarPickerRequest != null)
            {
                handlerAvatarPickerRequest(this, Requestdata.AgentID, Requestdata.QueryID,
                                           Utils.BytesToString(querydata.Name));
            }
            return true;
        }

        private bool HandleAgentDataUpdateRequest(IClientAPI sender, Packet Pack)
        {
            AgentDataUpdateRequestPacket avRequestDataUpdatePacket = (AgentDataUpdateRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (avRequestDataUpdatePacket.AgentData.SessionID != SessionId ||
                    avRequestDataUpdatePacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            FetchInventory handlerAgentDataUpdateRequest = OnAgentDataUpdateRequest;

            if (handlerAgentDataUpdateRequest != null)
            {
                handlerAgentDataUpdateRequest(this, avRequestDataUpdatePacket.AgentData.AgentID,
                                              avRequestDataUpdatePacket.AgentData.SessionID);
            }

            return true;
        }

        private bool HandleUserInfoRequest(IClientAPI sender, Packet Pack)
        {
            UserInfoRequest handlerUserInfoRequest = OnUserInfoRequest;
            if (handlerUserInfoRequest != null)
            {
                handlerUserInfoRequest(this);
            }
            else
            {
                SendUserInfoReply(false, true, "");
            }
            return true;
        }

        private bool HandleUpdateUserInfo(IClientAPI sender, Packet Pack)
        {
            UpdateUserInfoPacket updateUserInfo = (UpdateUserInfoPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (updateUserInfo.AgentData.SessionID != SessionId ||
                    updateUserInfo.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            UpdateUserInfo handlerUpdateUserInfo = OnUpdateUserInfo;
            if (handlerUpdateUserInfo != null)
            {
                bool visible = true;
                string DirectoryVisibility =
                    Utils.BytesToString(updateUserInfo.UserData.DirectoryVisibility);
                if (DirectoryVisibility == "hidden")
                    visible = false;

                handlerUpdateUserInfo(
                    updateUserInfo.UserData.IMViaEMail,
                    visible, this);
            }
            return true;
        }

        private bool HandleSetStartLocationRequest(IClientAPI sender, Packet Pack)
        {
            SetStartLocationRequestPacket avSetStartLocationRequestPacket = (SetStartLocationRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (avSetStartLocationRequestPacket.AgentData.SessionID != SessionId ||
                    avSetStartLocationRequestPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (avSetStartLocationRequestPacket.AgentData.AgentID == AgentId &&
                avSetStartLocationRequestPacket.AgentData.SessionID == SessionId)
            {
                // Linden Client limitation..
                if (avSetStartLocationRequestPacket.StartLocationData.LocationPos.X == 255.5f
                    || avSetStartLocationRequestPacket.StartLocationData.LocationPos.Y == 255.5f)
                {
                    IScenePresence avatar = null;
                    if (m_scene.TryGetScenePresence(AgentId, out avatar))
                    {
                        if (avSetStartLocationRequestPacket.StartLocationData.LocationPos.X == 255.5f)
                        {
                            avSetStartLocationRequestPacket.StartLocationData.LocationPos.X = avatar.AbsolutePosition.X;
                        }
                        if (avSetStartLocationRequestPacket.StartLocationData.LocationPos.Y == 255.5f)
                        {
                            avSetStartLocationRequestPacket.StartLocationData.LocationPos.Y = avatar.AbsolutePosition.Y;
                        }
                    }
                }
                TeleportLocationRequest handlerSetStartLocationRequest = OnSetStartLocationRequest;
                if (handlerSetStartLocationRequest != null)
                {
                    handlerSetStartLocationRequest(this, 0,
                                                   avSetStartLocationRequestPacket.StartLocationData.LocationPos,
                                                   avSetStartLocationRequestPacket.StartLocationData.LocationLookAt,
                                                   avSetStartLocationRequestPacket.StartLocationData.LocationID);
                }
            }
            return true;
        }

        private bool HandleAgentThrottle(IClientAPI sender, Packet Pack)
        {
            AgentThrottlePacket atpack = (AgentThrottlePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (atpack.AgentData.SessionID != SessionId ||
                    atpack.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            m_udpClient.SetThrottles(atpack.Throttle.Throttles);
            return true;
        }

        private bool HandleAgentPause(IClientAPI sender, Packet Pack)
        {
            #region Packet Session and User Check

            if (m_checkPackets)
            {
                AgentPausePacket agentPausePacket = Pack as AgentPausePacket;
                if (agentPausePacket != null && (agentPausePacket.AgentData.SessionID != SessionId ||
                                                         agentPausePacket.AgentData.AgentID != AgentId))
                    return true;
            }

            #endregion

            m_udpClient.IsPaused = true;
            return true;
        }

        private bool HandleAgentResume(IClientAPI sender, Packet Pack)
        {
            #region Packet Session and User Check

            if (m_checkPackets)
            {
                AgentResumePacket agentResumePacket = Pack as AgentResumePacket;
                if (agentResumePacket != null && (agentResumePacket.AgentData.SessionID != SessionId ||
                                                          agentResumePacket.AgentData.AgentID != AgentId))
                    return true;
            }

            #endregion

            m_udpClient.IsPaused = false;
            SendStartPingCheck(m_udpClient.CurrentPingSequence++);
            return true;
        }

        private bool HandleForceScriptControlRelease(IClientAPI sender, Packet Pack)
        {
            ForceReleaseControls handlerForceReleaseControls = OnForceReleaseControls;
            if (handlerForceReleaseControls != null)
            {
                handlerForceReleaseControls(this, AgentId);
            }
            return true;
        }

        #endregion Scene/Avatar

        #region Objects/m_sceneObjects

        private bool HandleObjectLink(IClientAPI sender, Packet Pack)
        {
            ObjectLinkPacket link = (ObjectLinkPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (link.AgentData.SessionID != SessionId ||
                    link.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            Util.FireAndForget(LinkObjects, Pack);
            return true;
        }

        private void LinkObjects(object Pack)
        {
            ObjectLinkPacket link = (ObjectLinkPacket) Pack;
            uint parentprimid = 0;
            List<uint> childrenprims = new List<uint>();
            if (link.ObjectData.Length > 1)
            {
                parentprimid = link.ObjectData[0].ObjectLocalID;

                for (int i = 1; i < link.ObjectData.Length; i++)
                {
                    childrenprims.Add(link.ObjectData[i].ObjectLocalID);
                }
            }
            LinkObjects handlerLinkObjects = OnLinkObjects;
            if (handlerLinkObjects != null)
            {
                handlerLinkObjects(this, parentprimid, childrenprims);
            }
        }

        private bool HandleObjectDelink(IClientAPI sender, Packet Pack)
        {
            ObjectDelinkPacket delink = (ObjectDelinkPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (delink.AgentData.SessionID != SessionId ||
                    delink.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            Util.FireAndForget(DelinkObjects, Pack);
            return true;
        }

        private void DelinkObjects(object Pack)
        {
            ObjectDelinkPacket delink = (ObjectDelinkPacket) Pack;

            // It appears the prim at index 0 is not always the root prim (for
            // instance, when one prim of a link set has been edited independently
            // of the others).  Therefore, we'll pass all the ids onto the delink
            // method for it to decide which is the root.
#if (!ISWIN)
            List<uint> prims = new List<uint>();
            foreach (ObjectDelinkPacket.ObjectDataBlock t in delink.ObjectData)
                prims.Add(t.ObjectLocalID);
#else
            List<uint> prims = delink.ObjectData.Select(t => t.ObjectLocalID).ToList();
#endif
            DelinkObjects handlerDelinkObjects = OnDelinkObjects;
            if (handlerDelinkObjects != null)
            {
                handlerDelinkObjects(prims, this);
            }
        }

        private bool HandleObjectAdd(IClientAPI sender, Packet Pack)
        {
            if (OnAddPrim != null)
            {
                ObjectAddPacket addPacket = (ObjectAddPacket) Pack;

                #region Packet Session and User Check

                if (m_checkPackets)
                {
                    if (addPacket.AgentData.SessionID != SessionId ||
                        addPacket.AgentData.AgentID != AgentId)
                        return true;
                }

                #endregion

                PrimitiveBaseShape shape = GetShapeFromAddPacket(addPacket);
                // MainConsole.Instance.Info("[REZData]: " + addPacket.ToString());
                //BypassRaycast: 1
                //RayStart: <69.79469, 158.2652, 98.40343>
                //RayEnd: <61.97724, 141.995, 92.58341>
                //RayTargetID: 00000000-0000-0000-0000-000000000000

                //Check to see if adding the prim is allowed; useful for any module wanting to restrict the
                //object from rezing initially

                AddNewPrim handlerAddPrim = OnAddPrim;
                if (handlerAddPrim != null)
                    handlerAddPrim(AgentId, ActiveGroupId, addPacket.ObjectData.RayEnd, addPacket.ObjectData.Rotation,
                                   shape, addPacket.ObjectData.BypassRaycast, addPacket.ObjectData.RayStart,
                                   addPacket.ObjectData.RayTargetID, addPacket.ObjectData.RayEndIsIntersection);
            }
            return true;
        }

        private bool HandleObjectShape(IClientAPI sender, Packet Pack)
        {
            ObjectShapePacket shapePacket = (ObjectShapePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (shapePacket.AgentData.SessionID != SessionId ||
                    shapePacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            foreach (ObjectShapePacket.ObjectDataBlock t in shapePacket.ObjectData)
            {
                UpdateShape handlerUpdatePrimShape = OnUpdatePrimShape;
                if (handlerUpdatePrimShape != null)
                {
                    UpdateShapeArgs shapeData = new UpdateShapeArgs
                                                    {
                                                        ObjectLocalID = t.ObjectLocalID,
                                                        PathBegin = t.PathBegin,
                                                        PathCurve = t.PathCurve,
                                                        PathEnd = t.PathEnd,
                                                        PathRadiusOffset = t.PathRadiusOffset,
                                                        PathRevolutions = t.PathRevolutions,
                                                        PathScaleX = t.PathScaleX,
                                                        PathScaleY = t.PathScaleY,
                                                        PathShearX = t.PathShearX,
                                                        PathShearY = t.PathShearY,
                                                        PathSkew = t.PathSkew,
                                                        PathTaperX = t.PathTaperX,
                                                        PathTaperY = t.PathTaperY,
                                                        PathTwist = t.PathTwist,
                                                        PathTwistBegin = t.PathTwistBegin,
                                                        ProfileBegin = t.ProfileBegin,
                                                        ProfileCurve = t.ProfileCurve,
                                                        ProfileEnd = t.ProfileEnd,
                                                        ProfileHollow = t.ProfileHollow
                                                    };

                    handlerUpdatePrimShape(m_agentId, t.ObjectLocalID,
                                           shapeData);
                }
            }
            return true;
        }

        private bool HandleObjectExtraParams(IClientAPI sender, Packet Pack)
        {
            ObjectExtraParamsPacket extraPar = (ObjectExtraParamsPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (extraPar.AgentData.SessionID != SessionId ||
                    extraPar.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ObjectExtraParams handlerUpdateExtraParams = OnUpdateExtraParams;
            if (handlerUpdateExtraParams != null)
            {
                foreach (ObjectExtraParamsPacket.ObjectDataBlock t in extraPar.ObjectData)
                {
                    handlerUpdateExtraParams(m_agentId, t.ObjectLocalID,
                                             t.ParamType,
                                             t.ParamInUse, t.ParamData);
                }
            }
            return true;
        }

        private bool HandleObjectDuplicate(IClientAPI sender, Packet Pack)
        {
            ObjectDuplicatePacket dupe = (ObjectDuplicatePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (dupe.AgentData.SessionID != SessionId ||
                    dupe.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

//            ObjectDuplicatePacket.AgentDataBlock AgentandGroupData = dupe.AgentData;

            foreach (ObjectDuplicatePacket.ObjectDataBlock t in dupe.ObjectData)
            {
                ObjectDuplicate handlerObjectDuplicate = OnObjectDuplicate;
                if (handlerObjectDuplicate != null)
                {
                    handlerObjectDuplicate(t.ObjectLocalID, dupe.SharedData.Offset,
                                           dupe.SharedData.DuplicateFlags, AgentId,
                                           m_activeGroupID, Quaternion.Identity);
                }
            }

            return true;
        }

        private bool HandleRequestMultipleObjects(IClientAPI sender, Packet Pack)
        {
            RequestMultipleObjectsPacket incomingRequest = (RequestMultipleObjectsPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (incomingRequest.AgentData.SessionID != SessionId ||
                    incomingRequest.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            foreach (RequestMultipleObjectsPacket.ObjectDataBlock t in incomingRequest.ObjectData)
            {
                ObjectRequest handlerObjectRequest = OnObjectRequest;
                if (handlerObjectRequest != null)
                {
                    handlerObjectRequest(t.ID, t.CacheMissType,
                                         this);
                }
            }
            return true;
        }

        private bool HandleObjectSelect(IClientAPI sender, Packet Pack)
        {
            ObjectSelectPacket incomingselect = (ObjectSelectPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (incomingselect.AgentData.SessionID != SessionId ||
                    incomingselect.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ObjectSelect handlerObjectSelect = null;
#if (!ISWIN)
            List<uint> LocalIDs = new List<uint>();
            foreach (ObjectSelectPacket.ObjectDataBlock t in incomingselect.ObjectData)
                LocalIDs.Add(t.ObjectLocalID);
#else
            List<uint> LocalIDs = incomingselect.ObjectData.Select(t => t.ObjectLocalID).ToList();
#endif
            handlerObjectSelect = OnObjectSelect;
            if (handlerObjectSelect != null)
            {
                handlerObjectSelect(LocalIDs, this);
            }
            return true;
        }

        private bool HandleObjectDeselect(IClientAPI sender, Packet Pack)
        {
            ObjectDeselectPacket incomingdeselect = (ObjectDeselectPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (incomingdeselect.AgentData.SessionID != SessionId ||
                    incomingdeselect.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            foreach (ObjectDeselectPacket.ObjectDataBlock t in incomingdeselect.ObjectData)
            {
                ObjectDeselect handlerObjectDeselect = OnObjectDeselect;
                if (handlerObjectDeselect != null)
                {
                    OnObjectDeselect(t.ObjectLocalID, this);
                }
            }
            return true;
        }

        private bool HandleObjectPosition(IClientAPI sender, Packet Pack)
        {
            // DEPRECATED: but till libsecondlife removes it, people will use it
            ObjectPositionPacket position = (ObjectPositionPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (position.AgentData.SessionID != SessionId ||
                    position.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            foreach (ObjectPositionPacket.ObjectDataBlock t in position.ObjectData)
            {
                UpdateVectorWithUpdate handlerUpdateVector = OnUpdatePrimGroupPosition;
                if (handlerUpdateVector != null)
                    handlerUpdateVector(t.ObjectLocalID, t.Position, this,
                                        true);
            }

            return true;
        }

        private bool HandleObjectScale(IClientAPI sender, Packet Pack)
        {
            // DEPRECATED: but till libsecondlife removes it, people will use it
            ObjectScalePacket scale = (ObjectScalePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (scale.AgentData.SessionID != SessionId ||
                    scale.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            foreach (ObjectScalePacket.ObjectDataBlock t in scale.ObjectData)
            {
                UpdateVector handlerUpdatePrimGroupScale = OnUpdatePrimGroupScale;
                if (handlerUpdatePrimGroupScale != null)
                    handlerUpdatePrimGroupScale(t.ObjectLocalID, t.Scale, this);
            }

            return true;
        }

        private bool HandleObjectRotation(IClientAPI sender, Packet Pack)
        {
            // DEPRECATED: but till libsecondlife removes it, people will use it
            ObjectRotationPacket rotation = (ObjectRotationPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (rotation.AgentData.SessionID != SessionId ||
                    rotation.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            foreach (ObjectRotationPacket.ObjectDataBlock t in rotation.ObjectData)
            {
                UpdatePrimRotation handlerUpdatePrimRotation = OnUpdatePrimGroupRotation;
                if (handlerUpdatePrimRotation != null)
                    handlerUpdatePrimRotation(t.ObjectLocalID, t.Rotation,
                                              this);
            }

            return true;
        }

        private bool HandleObjectFlagUpdate(IClientAPI sender, Packet Pack)
        {
            ObjectFlagUpdatePacket flags = (ObjectFlagUpdatePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (flags.AgentData.SessionID != SessionId ||
                    flags.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            UpdatePrimFlags handlerUpdatePrimFlags = OnUpdatePrimFlags;

            if (handlerUpdatePrimFlags != null)
            {
                handlerUpdatePrimFlags(flags.AgentData.ObjectLocalID, flags.AgentData.UsePhysics,
                                       flags.AgentData.IsTemporary, flags.AgentData.IsPhantom, flags.ExtraPhysics, this);
            }
            return true;
        }

        private bool HandleObjectImage(IClientAPI sender, Packet Pack)
        {
            ObjectImagePacket imagePack = (ObjectImagePacket) Pack;

            foreach (ObjectImagePacket.ObjectDataBlock t in imagePack.ObjectData)
            {
                UpdatePrimTexture handlerUpdatePrimTexture = OnUpdatePrimTexture;
                if (handlerUpdatePrimTexture != null)
                {
                    handlerUpdatePrimTexture(t.ObjectLocalID,
                                             t.TextureEntry, this);
                }
            }
            return true;
        }

        private bool HandleObjectGrab(IClientAPI sender, Packet Pack)
        {
            ObjectGrabPacket grab = (ObjectGrabPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (grab.AgentData.SessionID != SessionId ||
                    grab.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            GrabObject handlerGrabObject = OnGrabObject;

            if (handlerGrabObject != null)
            {
                List<SurfaceTouchEventArgs> touchArgs = new List<SurfaceTouchEventArgs>();
                if ((grab.SurfaceInfo != null) && (grab.SurfaceInfo.Length > 0))
                {
#if (!ISWIN)
                    foreach (ObjectGrabPacket.SurfaceInfoBlock surfaceInfo in grab.SurfaceInfo)
                    {
                        SurfaceTouchEventArgs arg = new SurfaceTouchEventArgs
                                                        {
                                                            Binormal = surfaceInfo.Binormal,
                                                            FaceIndex = surfaceInfo.FaceIndex,
                                                            Normal = surfaceInfo.Normal,
                                                            Position = surfaceInfo.Position,
                                                            STCoord = surfaceInfo.STCoord,
                                                            UVCoord = surfaceInfo.UVCoord
                                                        };
                        touchArgs.Add(arg);
                    }
#else
                    touchArgs.AddRange(grab.SurfaceInfo.Select(surfaceInfo => new SurfaceTouchEventArgs
                                                                                  {
                                                                                      Binormal = surfaceInfo.Binormal,
                                                                                      FaceIndex = surfaceInfo.FaceIndex,
                                                                                      Normal = surfaceInfo.Normal,
                                                                                      Position = surfaceInfo.Position,
                                                                                      STCoord = surfaceInfo.STCoord,
                                                                                      UVCoord = surfaceInfo.UVCoord
                                                                                  }));
#endif
                }
                handlerGrabObject(grab.ObjectData.LocalID, grab.ObjectData.GrabOffset, this, touchArgs);
            }
            return true;
        }

        private bool HandleObjectGrabUpdate(IClientAPI sender, Packet Pack)
        {
            ObjectGrabUpdatePacket grabUpdate = (ObjectGrabUpdatePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (grabUpdate.AgentData.SessionID != SessionId ||
                    grabUpdate.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            MoveObject handlerGrabUpdate = OnGrabUpdate;

            if (handlerGrabUpdate != null)
            {
                List<SurfaceTouchEventArgs> touchArgs = new List<SurfaceTouchEventArgs>();
                if ((grabUpdate.SurfaceInfo != null) && (grabUpdate.SurfaceInfo.Length > 0))
                {
#if (!ISWIN)
                    foreach (ObjectGrabUpdatePacket.SurfaceInfoBlock surfaceInfo in grabUpdate.SurfaceInfo)
                    {
                        SurfaceTouchEventArgs arg = new SurfaceTouchEventArgs
                                                        {
                                                            Binormal = surfaceInfo.Binormal,
                                                            FaceIndex = surfaceInfo.FaceIndex,
                                                            Normal = surfaceInfo.Normal,
                                                            Position = surfaceInfo.Position,
                                                            STCoord = surfaceInfo.STCoord,
                                                            UVCoord = surfaceInfo.UVCoord
                                                        };
                        touchArgs.Add(arg);
                    }
#else
                    touchArgs.AddRange(grabUpdate.SurfaceInfo.Select(surfaceInfo => new SurfaceTouchEventArgs
                                                                                        {
                                                                                            Binormal =
                                                                                                surfaceInfo.Binormal,
                                                                                            FaceIndex =
                                                                                                surfaceInfo.FaceIndex,
                                                                                            Normal = surfaceInfo.Normal,
                                                                                            Position =
                                                                                                surfaceInfo.Position,
                                                                                            STCoord =
                                                                                                surfaceInfo.STCoord,
                                                                                            UVCoord =
                                                                                                surfaceInfo.UVCoord
                                                                                        }));
#endif
                }
                handlerGrabUpdate(grabUpdate.ObjectData.ObjectID, grabUpdate.ObjectData.GrabOffsetInitial,
                                  grabUpdate.ObjectData.GrabPosition, this, touchArgs);
            }
            return true;
        }

        private bool HandleObjectDeGrab(IClientAPI sender, Packet Pack)
        {
            ObjectDeGrabPacket deGrab = (ObjectDeGrabPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (deGrab.AgentData.SessionID != SessionId ||
                    deGrab.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            DeGrabObject handlerDeGrabObject = OnDeGrabObject;
            if (handlerDeGrabObject != null)
            {
                List<SurfaceTouchEventArgs> touchArgs = new List<SurfaceTouchEventArgs>();
                if ((deGrab.SurfaceInfo != null) && (deGrab.SurfaceInfo.Length > 0))
                {
#if (!ISWIN)
                    foreach (ObjectDeGrabPacket.SurfaceInfoBlock surfaceInfo in deGrab.SurfaceInfo)
                    {
                        SurfaceTouchEventArgs arg = new SurfaceTouchEventArgs();
                        arg.Binormal = surfaceInfo.Binormal;
                        arg.FaceIndex = surfaceInfo.FaceIndex;
                        arg.Normal = surfaceInfo.Normal;
                        arg.Position = surfaceInfo.Position;
                        arg.STCoord = surfaceInfo.STCoord;
                        arg.UVCoord = surfaceInfo.UVCoord;
                        touchArgs.Add(arg);
                    }
#else
                    touchArgs.AddRange(deGrab.SurfaceInfo.Select(surfaceInfo => new SurfaceTouchEventArgs
                                                                                    {
                                                                                        Binormal = surfaceInfo.Binormal,
                                                                                        FaceIndex =
                                                                                            surfaceInfo.FaceIndex,
                                                                                        Normal = surfaceInfo.Normal,
                                                                                        Position = surfaceInfo.Position,
                                                                                        STCoord = surfaceInfo.STCoord,
                                                                                        UVCoord = surfaceInfo.UVCoord
                                                                                    }));
#endif
                }
                handlerDeGrabObject(deGrab.ObjectData.LocalID, this, touchArgs);
            }
            return true;
        }

        private bool HandleObjectSpinStart(IClientAPI sender, Packet Pack)
        {
            //MainConsole.Instance.Warn("[CLIENT]: unhandled ObjectSpinStart packet");
            ObjectSpinStartPacket spinStart = (ObjectSpinStartPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (spinStart.AgentData.SessionID != SessionId ||
                    spinStart.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            SpinStart handlerSpinStart = OnSpinStart;
            if (handlerSpinStart != null)
            {
                handlerSpinStart(spinStart.ObjectData.ObjectID, this);
            }
            return true;
        }

        private bool HandleObjectSpinUpdate(IClientAPI sender, Packet Pack)
        {
            //MainConsole.Instance.Warn("[CLIENT]: unhandled ObjectSpinUpdate packet");
            ObjectSpinUpdatePacket spinUpdate = (ObjectSpinUpdatePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (spinUpdate.AgentData.SessionID != SessionId ||
                    spinUpdate.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            Vector3 axis;
            float angle;
            spinUpdate.ObjectData.Rotation.GetAxisAngle(out axis, out angle);
            //MainConsole.Instance.Warn("[CLIENT]: ObjectSpinUpdate packet rot axis:" + axis + " angle:" + angle);

            SpinObject handlerSpinUpdate = OnSpinUpdate;
            if (handlerSpinUpdate != null)
            {
                handlerSpinUpdate(spinUpdate.ObjectData.ObjectID, spinUpdate.ObjectData.Rotation, this);
            }
            return true;
        }

        private bool HandleObjectSpinStop(IClientAPI sender, Packet Pack)
        {
            //MainConsole.Instance.Warn("[CLIENT]: unhandled ObjectSpinStop packet");
            ObjectSpinStopPacket spinStop = (ObjectSpinStopPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (spinStop.AgentData.SessionID != SessionId ||
                    spinStop.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            SpinStop handlerSpinStop = OnSpinStop;
            if (handlerSpinStop != null)
            {
                handlerSpinStop(spinStop.ObjectData.ObjectID, this);
            }
            return true;
        }

        private bool HandleObjectDescription(IClientAPI sender, Packet Pack)
        {
            ObjectDescriptionPacket objDes = (ObjectDescriptionPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (objDes.AgentData.SessionID != SessionId ||
                    objDes.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            foreach (ObjectDescriptionPacket.ObjectDataBlock t in objDes.ObjectData)
            {
                GenericCall7 handlerObjectDescription = OnObjectDescription;
                if (handlerObjectDescription != null)
                {
                    handlerObjectDescription(this, t.LocalID,
                                             Util.FieldToString(t.Description));
                }
            }
            return true;
        }

        private bool HandleObjectName(IClientAPI sender, Packet Pack)
        {
            ObjectNamePacket objName = (ObjectNamePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (objName.AgentData.SessionID != SessionId ||
                    objName.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            foreach (ObjectNamePacket.ObjectDataBlock t in objName.ObjectData)
            {
                GenericCall7 handlerObjectName = OnObjectName;
                if (handlerObjectName != null)
                {
                    handlerObjectName(this, t.LocalID,
                                      Util.FieldToString(t.Name));
                }
            }
            return true;
        }

        private bool HandleObjectPermissions(IClientAPI sender, Packet Pack)
        {
            if (OnObjectPermissions != null)
            {
                ObjectPermissionsPacket newobjPerms = (ObjectPermissionsPacket) Pack;

                #region Packet Session and User Check

                if (m_checkPackets)
                {
                    if (newobjPerms.AgentData.SessionID != SessionId ||
                        newobjPerms.AgentData.AgentID != AgentId)
                        return true;
                }

                #endregion

                UUID AgentID = newobjPerms.AgentData.AgentID;
                UUID SessionID = newobjPerms.AgentData.SessionID;

                foreach (ObjectPermissionsPacket.ObjectDataBlock permChanges in newobjPerms.ObjectData)
                {
                    byte field = permChanges.Field;
                    uint localID = permChanges.ObjectLocalID;
                    uint mask = permChanges.Mask;
                    byte set = permChanges.Set;

                    ObjectPermissions handlerObjectPermissions = OnObjectPermissions;

                    if (handlerObjectPermissions != null)
                        handlerObjectPermissions(this, AgentID, SessionID, field, localID, mask, set);
                }
            }

            // Here's our data,
            // PermField contains the field the info goes into
            // PermField determines which mask we're changing
            //
            // chmask is the mask of the change
            // setTF is whether we're adding it or taking it away
            //
            // objLocalID is the localID of the object.

            // Unfortunately, we have to pass the event the packet because objData is an array
            // That means multiple object perms may be updated in a single packet.

            return true;
        }

        private bool HandleUndo(IClientAPI sender, Packet Pack)
        {
            UndoPacket undoitem = (UndoPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (undoitem.AgentData.SessionID != SessionId ||
                    undoitem.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (undoitem.ObjectData.Length > 0)
            {
                foreach (UndoPacket.ObjectDataBlock t in undoitem.ObjectData)
                {
                    UUID objiD = t.ObjectID;
                    AgentSit handlerOnUndo = OnUndo;
                    if (handlerOnUndo != null)
                    {
                        handlerOnUndo(this, objiD);
                    }
                }
            }
            return true;
        }

        private bool HandleLandUndo(IClientAPI sender, Packet Pack)
        {
            UndoLandPacket undolanditem = (UndoLandPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (undolanditem.AgentData.SessionID != SessionId ||
                    undolanditem.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            LandUndo handlerOnUndo = OnLandUndo;
            if (handlerOnUndo != null)
            {
                handlerOnUndo(this);
            }
            return true;
        }

        private bool HandleRedo(IClientAPI sender, Packet Pack)
        {
            RedoPacket redoitem = (RedoPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (redoitem.AgentData.SessionID != SessionId ||
                    redoitem.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (redoitem.ObjectData.Length > 0)
            {
                foreach (RedoPacket.ObjectDataBlock t in redoitem.ObjectData)
                {
                    UUID objiD = t.ObjectID;
                    AgentSit handlerOnRedo = OnRedo;
                    if (handlerOnRedo != null)
                    {
                        handlerOnRedo(this, objiD);
                    }
                }
            }
            return true;
        }

        private bool HandleObjectDuplicateOnRay(IClientAPI sender, Packet Pack)
        {
            ObjectDuplicateOnRayPacket dupeOnRay = (ObjectDuplicateOnRayPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (dupeOnRay.AgentData.SessionID != SessionId ||
                    dupeOnRay.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            foreach (ObjectDuplicateOnRayPacket.ObjectDataBlock t in dupeOnRay.ObjectData)
            {
                ObjectDuplicateOnRay handlerObjectDuplicateOnRay = OnObjectDuplicateOnRay;
                if (handlerObjectDuplicateOnRay != null)
                {
                    handlerObjectDuplicateOnRay(t.ObjectLocalID,
                                                dupeOnRay.AgentData.DuplicateFlags,
                                                AgentId, m_activeGroupID, dupeOnRay.AgentData.RayTargetID,
                                                dupeOnRay.AgentData.RayEnd,
                                                dupeOnRay.AgentData.RayStart, dupeOnRay.AgentData.BypassRaycast,
                                                dupeOnRay.AgentData.RayEndIsIntersection,
                                                dupeOnRay.AgentData.CopyCenters, dupeOnRay.AgentData.CopyRotates);
                }
            }

            return true;
        }

        private bool HandleRequestObjectPropertiesFamily(IClientAPI sender, Packet Pack)
        {
            //This powers the little tooltip that appears when you move your mouse over an object
            RequestObjectPropertiesFamilyPacket packToolTip = (RequestObjectPropertiesFamilyPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (packToolTip.AgentData.SessionID != SessionId ||
                    packToolTip.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            RequestObjectPropertiesFamilyPacket.ObjectDataBlock packObjBlock = packToolTip.ObjectData;

            RequestObjectPropertiesFamily handlerRequestObjectPropertiesFamily = OnRequestObjectPropertiesFamily;

            if (handlerRequestObjectPropertiesFamily != null)
            {
                handlerRequestObjectPropertiesFamily(this, m_agentId, packObjBlock.RequestFlags,
                                                     packObjBlock.ObjectID);
            }

            return true;
        }

        private bool HandleObjectIncludeInSearch(IClientAPI sender, Packet Pack)
        {
            //This lets us set objects to appear in search (stuff like DataSnapshot, etc)
            ObjectIncludeInSearchPacket packInSearch = (ObjectIncludeInSearchPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (packInSearch.AgentData.SessionID != SessionId ||
                    packInSearch.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            foreach (ObjectIncludeInSearchPacket.ObjectDataBlock objData in packInSearch.ObjectData)
            {
                bool inSearch = objData.IncludeInSearch;
                uint localID = objData.ObjectLocalID;

                ObjectIncludeInSearch handlerObjectIncludeInSearch = OnObjectIncludeInSearch;

                if (handlerObjectIncludeInSearch != null)
                {
                    handlerObjectIncludeInSearch(this, inSearch, localID);
                }
            }
            return true;
        }

        private bool HandleScriptAnswerYes(IClientAPI sender, Packet Pack)
        {
            ScriptAnswerYesPacket scriptAnswer = (ScriptAnswerYesPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (scriptAnswer.AgentData.SessionID != SessionId ||
                    scriptAnswer.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ScriptAnswer handlerScriptAnswer = OnScriptAnswer;
            if (handlerScriptAnswer != null)
            {
                handlerScriptAnswer(this, scriptAnswer.Data.TaskID, scriptAnswer.Data.ItemID,
                                    scriptAnswer.Data.Questions);
            }
            return true;
        }

        private bool HandleObjectClickAction(IClientAPI sender, Packet Pack)
        {
            ObjectClickActionPacket ocpacket = (ObjectClickActionPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (ocpacket.AgentData.SessionID != SessionId ||
                    ocpacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            GenericCall7 handlerObjectClickAction = OnObjectClickAction;
            if (handlerObjectClickAction != null)
            {
                foreach (ObjectClickActionPacket.ObjectDataBlock odata in ocpacket.ObjectData)
                {
                    byte action = odata.ClickAction;
                    uint localID = odata.ObjectLocalID;
                    handlerObjectClickAction(this, localID, action.ToString());
                }
            }
            return true;
        }

        private bool HandleObjectMaterial(IClientAPI sender, Packet Pack)
        {
            ObjectMaterialPacket ompacket = (ObjectMaterialPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (ompacket.AgentData.SessionID != SessionId ||
                    ompacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            GenericCall7 handlerObjectMaterial = OnObjectMaterial;
            if (handlerObjectMaterial != null)
            {
                foreach (ObjectMaterialPacket.ObjectDataBlock odata in ompacket.ObjectData)
                {
                    byte material = odata.Material;
                    uint localID = odata.ObjectLocalID;
                    handlerObjectMaterial(this, localID, material.ToString());
                }
            }
            return true;
        }

        #endregion Objects/m_sceneObjects

        #region Inventory/Asset/Other related packets

        private bool HandleRequestImage(IClientAPI sender, Packet Pack)
        {
            RequestImagePacket imageRequest = (RequestImagePacket) Pack;
            //MainConsole.Instance.Debug("image request: " + Pack.ToString());

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (imageRequest.AgentData.SessionID != SessionId ||
                    imageRequest.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            //handlerTextureRequest = null;
            foreach (RequestImagePacket.RequestImageBlock t in imageRequest.RequestImage)
            {
                TextureRequestArgs args = new TextureRequestArgs();

                RequestImagePacket.RequestImageBlock block = t;

                args.RequestedAssetID = block.Image;
                args.DiscardLevel = block.DiscardLevel;
                args.PacketNumber = block.Packet;
                args.Priority = block.DownloadPriority;
                args.requestSequence = imageRequest.Header.Sequence;

                // NOTE: This is not a built in part of the LLUDP protocol, but we double the
                // priority of avatar textures to get avatars rezzing in faster than the
                // surrounding scene
                if ((ImageType) block.Type == ImageType.Baked)
                    args.Priority *= 2.0f;

                // in the end, we null this, so we have to check if it's null
                if (m_imageManager != null)
                {
                    m_imageManager.EnqueueReq(args);
                }
            }
            return true;
        }

        /// <summary>
        ///   This is the entry point for the UDP route by which the client can retrieve asset data.  If the request
        ///   is successful then a TransferInfo packet will be sent back, followed by one or more TransferPackets
        /// </summary>
        /// <param name = "sender"></param>
        /// <param name = "Pack"></param>
        /// <returns>This parameter may be ignored since we appear to return true whatever happens</returns>
        private bool HandleTransferRequest(IClientAPI sender, Packet Pack)
        {
            //MainConsole.Instance.Debug("ClientView.ProcessPackets.cs:ProcessInPacket() - Got transfer request");

            TransferRequestPacket transfer = (TransferRequestPacket) Pack;
            //MainConsole.Instance.Debug("Transfer Request: " + transfer.ToString());
            // Validate inventory transfers
            // Has to be done here, because AssetCache can't do it
            //
            UUID taskID = UUID.Zero;
            if (transfer.TransferInfo.SourceType == (int) SourceType.SimInventoryItem)
            {
                taskID = new UUID(transfer.TransferInfo.Params, 48);
                UUID itemID = new UUID(transfer.TransferInfo.Params, 64);
                UUID requestID = new UUID(transfer.TransferInfo.Params, 80);

//                MainConsole.Instance.DebugFormat(
//                    "[CLIENT]: Got request for asset {0} from item {1} in prim {2} by {3}",
//                    requestID, itemID, taskID, Name);

                if (!m_scene.Permissions.BypassPermissions())
                {
                    if (taskID != UUID.Zero) // Prim
                    {
                        ISceneChildEntity part = m_scene.GetSceneObjectPart(taskID);

                        if (part == null)
                        {
                            MainConsole.Instance.WarnFormat(
                                "[CLIENT]: {0} requested asset {1} from item {2} in prim {3} but prim does not exist",
                                Name, requestID, itemID, taskID);
                            return true;
                        }

                        TaskInventoryItem tii = part.Inventory.GetInventoryItem(itemID);
                        if (tii == null)
                        {
                            MainConsole.Instance.WarnFormat(
                                "[CLIENT]: {0} requested asset {1} from item {2} in prim {3} but item does not exist",
                                Name, requestID, itemID, taskID);
                            return true;
                        }

                        if (tii.Type == (int) AssetType.LSLText)
                        {
                            if (!m_scene.Permissions.CanEditScript(itemID, taskID, AgentId))
                                return true;
                        }
                        else if (tii.Type == (int) AssetType.Notecard)
                        {
                            if (!m_scene.Permissions.CanEditNotecard(itemID, taskID, AgentId))
                                return true;
                        }
                        else
                        {
                            if (!m_scene.Permissions.CanEditObjectInventory(part.UUID, AgentId))
                            {
                                MainConsole.Instance.Warn(
                                    "[LLClientView]: Permissions check for CanEditObjectInventory fell through to standard code!");

                                if (part.OwnerID != AgentId)
                                {
                                    MainConsole.Instance.WarnFormat(
                                        "[CLIENT]: {0} requested asset {1} from item {2} in prim {3} but the prim is owned by {4}",
                                        Name, requestID, itemID, taskID, part.OwnerID);
                                    return true;
                                }

                                if ((part.OwnerMask & (uint) PermissionMask.Modify) == 0)
                                {
                                    MainConsole.Instance.WarnFormat(
                                        "[CLIENT]: {0} requested asset {1} from item {2} in prim {3} but modify permissions are not set",
                                        Name, requestID, itemID, taskID);
                                    return true;
                                }

                                if (tii.OwnerID != AgentId)
                                {
                                    MainConsole.Instance.WarnFormat(
                                        "[CLIENT]: {0} requested asset {1} from item {2} in prim {3} but the item is owned by {4}",
                                        Name, requestID, itemID, taskID, tii.OwnerID);
                                    return true;
                                }

                                if ((
                                        tii.CurrentPermissions &
                                        ((uint) PermissionMask.Modify | (uint) PermissionMask.Copy |
                                         (uint) PermissionMask.Transfer))
                                    !=
                                    ((uint) PermissionMask.Modify | (uint) PermissionMask.Copy |
                                     (uint) PermissionMask.Transfer))
                                {
                                    MainConsole.Instance.WarnFormat(
                                        "[CLIENT]: {0} requested asset {1} from item {2} in prim {3} but item permissions are not modify/copy/transfer",
                                        Name, requestID, itemID, taskID);
                                    return true;
                                }

                                if (tii.AssetID != requestID)
                                {
                                    MainConsole.Instance.WarnFormat(
                                        "[CLIENT]: {0} requested asset {1} from item {2} in prim {3} but this does not match item's asset {4}",
                                        Name, requestID, itemID, taskID, tii.AssetID);
                                    return true;
                                }
                            }
                        }
                    }
                    else // Agent
                    {
                        IInventoryAccessModule invAccess = m_scene.RequestModuleInterface<IInventoryAccessModule>();
                        if (invAccess != null)
                        {
                            if (!invAccess.GetAgentInventoryItem(this, itemID, requestID))
                                return false;
                        }
                        else
                            return false;
                    }
                }
            }

            MakeAssetRequest(transfer, taskID);

            return true;
        }

        private bool HandleAssetUploadRequest(IClientAPI sender, Packet Pack)
        {
            AssetUploadRequestPacket request = (AssetUploadRequestPacket) Pack;


            // MainConsole.Instance.Debug("upload request " + request.ToString());
            // MainConsole.Instance.Debug("upload request was for assetid: " + request.AssetBlock.TransactionID.Combine(this.SecureSessionId).ToString());
            UUID temp = UUID.Combine(request.AssetBlock.TransactionID, SecureSessionId);

            UDPAssetUploadRequest handlerAssetUploadRequest = OnAssetUploadRequest;

            if (handlerAssetUploadRequest != null)
            {
                handlerAssetUploadRequest(this, temp,
                                          request.AssetBlock.TransactionID, request.AssetBlock.Type,
                                          request.AssetBlock.AssetData, request.AssetBlock.StoreLocal,
                                          request.AssetBlock.Tempfile);
            }
            return true;
        }

        private bool HandleRequestXfer(IClientAPI sender, Packet Pack)
        {
            RequestXferPacket xferReq = (RequestXferPacket) Pack;

            RequestXfer handlerRequestXfer = OnRequestXfer;

            if (handlerRequestXfer != null)
            {
                handlerRequestXfer(this, xferReq.XferID.ID, Util.FieldToString(xferReq.XferID.Filename));
            }
            return true;
        }

        private bool HandleSendXferPacket(IClientAPI sender, Packet Pack)
        {
            SendXferPacketPacket xferRec = (SendXferPacketPacket) Pack;

            XferReceive handlerXferReceive = OnXferReceive;
            if (handlerXferReceive != null)
            {
                handlerXferReceive(this, xferRec.XferID.ID, xferRec.XferID.Packet, xferRec.DataPacket.Data);
            }
            return true;
        }

        private bool HandleConfirmXferPacket(IClientAPI sender, Packet Pack)
        {
            ConfirmXferPacketPacket confirmXfer = (ConfirmXferPacketPacket) Pack;

            ConfirmXfer handlerConfirmXfer = OnConfirmXfer;
            if (handlerConfirmXfer != null)
            {
                handlerConfirmXfer(this, confirmXfer.XferID.ID, confirmXfer.XferID.Packet);
            }
            return true;
        }

        private bool HandleAbortXfer(IClientAPI sender, Packet Pack)
        {
            AbortXferPacket abortXfer = (AbortXferPacket) Pack;
            AbortXfer handlerAbortXfer = OnAbortXfer;
            if (handlerAbortXfer != null)
            {
                handlerAbortXfer(this, abortXfer.XferID.ID);
            }

            return true;
        }

        public void SendAbortXferPacket(ulong xferID)
        {
            AbortXferPacket xferItem = (AbortXferPacket) PacketPool.Instance.GetPacket(PacketType.AbortXfer);
            xferItem.XferID.ID = xferID;
            OutPacket(xferItem, ThrottleOutPacketType.Transfer);
        }

        private bool HandleCreateInventoryFolder(IClientAPI sender, Packet Pack)
        {
            CreateInventoryFolderPacket invFolder = (CreateInventoryFolderPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (invFolder.AgentData.SessionID != SessionId ||
                    invFolder.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            CreateInventoryFolder handlerCreateInventoryFolder = OnCreateNewInventoryFolder;
            if (handlerCreateInventoryFolder != null)
            {
                handlerCreateInventoryFolder(this, invFolder.FolderData.FolderID,
                                             (ushort) invFolder.FolderData.Type,
                                             Util.FieldToString(invFolder.FolderData.Name),
                                             invFolder.FolderData.ParentID);
            }

            return true;
        }

        private bool HandleUpdateInventoryFolder(IClientAPI sender, Packet Pack)
        {
            if (OnUpdateInventoryFolder != null)
            {
                UpdateInventoryFolderPacket invFolderx = (UpdateInventoryFolderPacket) Pack;

                #region Packet Session and User Check

                if (m_checkPackets)
                {
                    if (invFolderx.AgentData.SessionID != SessionId ||
                        invFolderx.AgentData.AgentID != AgentId)
                        return true;
                }

                #endregion

                foreach (UpdateInventoryFolderPacket.FolderDataBlock t in from t in invFolderx.FolderData let handlerUpdateInventoryFolder = OnUpdateInventoryFolder where handlerUpdateInventoryFolder != null select t)
                {
                    OnUpdateInventoryFolder(this, t.FolderID,
                                            (ushort) t.Type,
                                            Util.FieldToString(t.Name),
                                            t.ParentID);
                }
            }
            return true;
        }

        private bool HandleMoveInventoryFolder(IClientAPI sender, Packet Pack)
        {
            if (OnMoveInventoryFolder != null)
            {
                #region Packet Session and User Check

                if (m_checkPackets)
                {
                    MoveInventoryFolderPacket invFoldery = (MoveInventoryFolderPacket) Pack;

                    if (invFoldery.AgentData.SessionID != SessionId ||
                        invFoldery.AgentData.AgentID != AgentId)
                        return true;
                }

                #endregion

                Util.FireAndForget(MoveInventoryFolder, Pack);
            }
            return true;
        }

        private void MoveInventoryFolder(object Pack)
        {
            MoveInventoryFolderPacket invFoldery = (MoveInventoryFolderPacket) Pack;

            foreach (MoveInventoryFolderPacket.InventoryDataBlock t in from t in invFoldery.InventoryData let handlerMoveInventoryFolder = OnMoveInventoryFolder where handlerMoveInventoryFolder != null select t)
            {
                OnMoveInventoryFolder(this, t.FolderID,
                                      t.ParentID);
            }
        }

        private bool HandleCreateInventoryItem(IClientAPI sender, Packet Pack)
        {
            CreateInventoryItemPacket createItem = (CreateInventoryItemPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (createItem.AgentData.SessionID != SessionId ||
                    createItem.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            CreateNewInventoryItem handlerCreateNewInventoryItem = OnCreateNewInventoryItem;
            if (handlerCreateNewInventoryItem != null)
            {
                handlerCreateNewInventoryItem(this, createItem.InventoryBlock.TransactionID,
                                              createItem.InventoryBlock.FolderID,
                                              createItem.InventoryBlock.CallbackID,
                                              Util.FieldToString(createItem.InventoryBlock.Description),
                                              Util.FieldToString(createItem.InventoryBlock.Name),
                                              createItem.InventoryBlock.InvType,
                                              createItem.InventoryBlock.Type,
                                              createItem.InventoryBlock.WearableType,
                                              createItem.InventoryBlock.NextOwnerMask,
                                              Util.UnixTimeSinceEpoch());
            }

            return true;
        }

        private bool HandleLinkInventoryItem(IClientAPI sender, Packet Pack)
        {
            LinkInventoryItemPacket createLink = (LinkInventoryItemPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (createLink.AgentData.SessionID != SessionId ||
                    createLink.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            LinkInventoryItem linkInventoryItem = OnLinkInventoryItem;

            if (linkInventoryItem != null)
            {
                linkInventoryItem(
                    this,
                    createLink.InventoryBlock.TransactionID,
                    createLink.InventoryBlock.FolderID,
                    createLink.InventoryBlock.CallbackID,
                    Util.FieldToString(createLink.InventoryBlock.Description),
                    Util.FieldToString(createLink.InventoryBlock.Name),
                    createLink.InventoryBlock.InvType,
                    createLink.InventoryBlock.Type,
                    createLink.InventoryBlock.OldItemID);
            }

            return true;
        }

        private bool HandleFetchInventory(IClientAPI sender, Packet Pack)
        {
            if (OnFetchInventory != null)
            {
                FetchInventoryPacket FetchInventoryx = (FetchInventoryPacket) Pack;

                #region Packet Session and User Check

                if (m_checkPackets)
                {
                    if (FetchInventoryx.AgentData.SessionID != SessionId ||
                        FetchInventoryx.AgentData.AgentID != AgentId)
                        return true;
                }

                #endregion

                foreach (FetchInventoryPacket.InventoryDataBlock t in FetchInventoryx.InventoryData)
                {
                    FetchInventory handlerFetchInventory = OnFetchInventory;

                    if (handlerFetchInventory != null)
                    {
                        OnFetchInventory(this, t.ItemID,
                                         t.OwnerID);
                    }
                }
            }
            return true;
        }

        private bool HandleFetchInventoryDescendents(IClientAPI sender, Packet Pack)
        {
            FetchInventoryDescendentsPacket Fetch = (FetchInventoryDescendentsPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (Fetch.AgentData.SessionID != SessionId ||
                    Fetch.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            FetchInventoryDescendents handlerFetchInventoryDescendents = OnFetchInventoryDescendents;
            if (handlerFetchInventoryDescendents != null)
            {
                handlerFetchInventoryDescendents(this, Fetch.InventoryData.FolderID, Fetch.InventoryData.OwnerID,
                                                 Fetch.InventoryData.FetchFolders, Fetch.InventoryData.FetchItems,
                                                 Fetch.InventoryData.SortOrder);
            }
            return true;
        }

        private bool HandlePurgeInventoryDescendents(IClientAPI sender, Packet Pack)
        {
            PurgeInventoryDescendentsPacket Purge = (PurgeInventoryDescendentsPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (Purge.AgentData.SessionID != SessionId ||
                    Purge.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            Util.FireAndForget(PurgeInventoryDescendents, Pack);

            return true;
        }

        private void PurgeInventoryDescendents(object Pack)
        {
            PurgeInventoryDescendentsPacket Purge = (PurgeInventoryDescendentsPacket) Pack;

            PurgeInventoryDescendents handlerPurgeInventoryDescendents = OnPurgeInventoryDescendents;
            if (handlerPurgeInventoryDescendents != null)
            {
                handlerPurgeInventoryDescendents(this, Purge.InventoryData.FolderID);
            }
        }

        private bool HandleUpdateInventoryItem(IClientAPI sender, Packet Pack)
        {
            UpdateInventoryItemPacket inventoryItemUpdate = (UpdateInventoryItemPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (inventoryItemUpdate.AgentData.SessionID != SessionId ||
                    inventoryItemUpdate.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            Util.FireAndForget(UpdateInventoryItem, Pack);

            return true;
        }

        private void UpdateInventoryItem(object Pack)
        {
            UpdateInventoryItemPacket inventoryItemUpdate = (UpdateInventoryItemPacket) Pack;

            if (OnUpdateInventoryItem != null)
            {
                foreach (UpdateInventoryItemPacket.InventoryDataBlock t in inventoryItemUpdate.InventoryData)
                {
                    UpdateInventoryItem handlerUpdateInventoryItem = OnUpdateInventoryItem;

                    if (handlerUpdateInventoryItem == null) continue;
                    InventoryItemBase itemUpd = new InventoryItemBase
                                                    {
                                                        ID = t.ItemID,
                                                        Name =
                                                            Util.FieldToString(
                                                                t.Name),
                                                        Description =
                                                            Util.FieldToString(
                                                                t.Description),
                                                        GroupID = t.GroupID,
                                                        GroupOwned = t.GroupOwned,
                                                        GroupPermissions =
                                                            t.GroupMask,
                                                        NextPermissions =
                                                            t.NextOwnerMask,
                                                        EveryOnePermissions =
                                                            t.EveryoneMask,
                                                        CreationDate =
                                                            t.CreationDate,
                                                        Folder = t.FolderID,
                                                        InvType = t.InvType,
                                                        SalePrice = t.SalePrice,
                                                        SaleType = t.SaleType,
                                                        Flags = t.Flags
                                                    };

                    OnUpdateInventoryItem(this, t.TransactionID,
                                          t.ItemID,
                                          itemUpd);
                }
            }
        }

        private bool HandleCopyInventoryItem(IClientAPI sender, Packet Pack)
        {
            CopyInventoryItemPacket copyitem = (CopyInventoryItemPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (copyitem.AgentData.SessionID != SessionId ||
                    copyitem.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            Util.FireAndForget(CopyInventoryItem, Pack);

            return true;
        }

        private void CopyInventoryItem(object Pack)
        {
            CopyInventoryItemPacket copyitem = (CopyInventoryItemPacket) Pack;

            if (OnCopyInventoryItem != null)
            {
                foreach (CopyInventoryItemPacket.InventoryDataBlock datablock in copyitem.InventoryData)
                {
                    CopyInventoryItem handlerCopyInventoryItem = OnCopyInventoryItem;
                    if (handlerCopyInventoryItem != null)
                    {
                        handlerCopyInventoryItem(this, datablock.CallbackID, datablock.OldAgentID,
                                                 datablock.OldItemID, datablock.NewFolderID,
                                                 Util.FieldToString(datablock.NewName));
                    }
                }
            }
        }

        private bool HandleMoveInventoryItem(IClientAPI sender, Packet Pack)
        {
            MoveInventoryItemPacket moveitem = (MoveInventoryItemPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (moveitem.AgentData.SessionID != SessionId ||
                    moveitem.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            Util.FireAndForget(MoveInventoryItem, Pack);

            return true;
        }

        private bool HandleChangeInventoryItemFlags(IClientAPI sender, Packet Pack)
        {
            ChangeInventoryItemFlagsPacket inventoryItemUpdate = (ChangeInventoryItemFlagsPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (inventoryItemUpdate.AgentData.SessionID != SessionId ||
                    inventoryItemUpdate.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (OnChangeInventoryItemFlags != null)
            {
                foreach (ChangeInventoryItemFlagsPacket.InventoryDataBlock t in from t in inventoryItemUpdate.InventoryData let handlerUpdateInventoryItem = OnChangeInventoryItemFlags where handlerUpdateInventoryItem != null select t)
                {
                    OnChangeInventoryItemFlags(this,
                                               t.ItemID,
                                               t.Flags);
                }
            }

            return true;
        }

        private void MoveInventoryItem(object Pack)
        {
            MoveInventoryItemPacket moveitem = (MoveInventoryItemPacket) Pack;

            if (OnMoveInventoryItem != null)
            {
                MoveInventoryItem handlerMoveInventoryItem = null;
#if (!ISWIN)
                List<InventoryItemBase> items = new List<InventoryItemBase>();
                foreach (MoveInventoryItemPacket.InventoryDataBlock datablock in moveitem.InventoryData)
                    items.Add(new InventoryItemBase(datablock.ItemID, AgentId) {Folder = datablock.FolderID, Name = Util.FieldToString(datablock.NewName)});
#else
                List<InventoryItemBase> items = moveitem.InventoryData.Select(datablock => new InventoryItemBase(datablock.ItemID, AgentId) {Folder = datablock.FolderID, Name = Util.FieldToString(datablock.NewName)}).ToList();
#endif
                handlerMoveInventoryItem = OnMoveInventoryItem;
                if (handlerMoveInventoryItem != null)
                {
                    handlerMoveInventoryItem(this, items);
                }
            }
        }

        private bool HandleRemoveInventoryItem(IClientAPI sender, Packet Pack)
        {
            RemoveInventoryItemPacket removeItem = (RemoveInventoryItemPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (removeItem.AgentData.SessionID != SessionId ||
                    removeItem.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            Util.FireAndForget(RemoveInventoryItem, Pack);

            return true;
        }

        private void RemoveInventoryItem(object Pack)
        {
            RemoveInventoryItemPacket removeItem = (RemoveInventoryItemPacket) Pack;

            if (OnRemoveInventoryItem != null)
            {
                RemoveInventoryItem handlerRemoveInventoryItem = null;
#if (!ISWIN)
                List<UUID> uuids = new List<UUID>();
                foreach (RemoveInventoryItemPacket.InventoryDataBlock datablock in removeItem.InventoryData)
                    uuids.Add(datablock.ItemID);
#else
                List<UUID> uuids = removeItem.InventoryData.Select(datablock => datablock.ItemID).ToList();
#endif
                handlerRemoveInventoryItem = OnRemoveInventoryItem;
                if (handlerRemoveInventoryItem != null)
                {
                    handlerRemoveInventoryItem(this, uuids);
                }
            }
        }

        private bool HandleRemoveInventoryFolder(IClientAPI sender, Packet Pack)
        {
            RemoveInventoryFolderPacket removeFolder = (RemoveInventoryFolderPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (removeFolder.AgentData.SessionID != SessionId ||
                    removeFolder.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            Util.FireAndForget(RemoveInventoryFolder, Pack);

            return true;
        }

        private void RemoveInventoryFolder(object Pack)
        {
            RemoveInventoryFolderPacket removeFolder = (RemoveInventoryFolderPacket) Pack;

            if (OnRemoveInventoryFolder != null)
            {
                RemoveInventoryFolder handlerRemoveInventoryFolder = null;
#if (!ISWIN)
                List<UUID> uuids = new List<UUID>();
                foreach (RemoveInventoryFolderPacket.FolderDataBlock datablock in removeFolder.FolderData)
                    uuids.Add(datablock.FolderID);
#else
                List<UUID> uuids = removeFolder.FolderData.Select(datablock => datablock.FolderID).ToList();
#endif
                handlerRemoveInventoryFolder = OnRemoveInventoryFolder;
                if (handlerRemoveInventoryFolder != null)
                {
                    handlerRemoveInventoryFolder(this, uuids);
                }
            }
        }

        private bool HandleRemoveInventoryObjects(IClientAPI sender, Packet Pack)
        {
            RemoveInventoryObjectsPacket removeObject = (RemoveInventoryObjectsPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (removeObject.AgentData.SessionID != SessionId ||
                    removeObject.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            Util.FireAndForget(RemoveInventoryObjects, Pack);

            return true;
        }

        private void RemoveInventoryObjects(object Pack)
        {
            RemoveInventoryObjectsPacket removeObject = (RemoveInventoryObjectsPacket) Pack;

            if (OnRemoveInventoryFolder != null)
            {
                RemoveInventoryFolder handlerRemoveInventoryFolder = null;
#if (!ISWIN)
                List<UUID> uuids = new List<UUID>();
                foreach (RemoveInventoryObjectsPacket.FolderDataBlock datablock in removeObject.FolderData)
                    uuids.Add(datablock.FolderID);
#else
                List<UUID> uuids = removeObject.FolderData.Select(datablock => datablock.FolderID).ToList();
#endif
                handlerRemoveInventoryFolder = OnRemoveInventoryFolder;
                if (handlerRemoveInventoryFolder != null)
                {
                    handlerRemoveInventoryFolder(this, uuids);
                }
            }

            if (OnRemoveInventoryItem != null)
            {
                RemoveInventoryItem handlerRemoveInventoryItem = null;
#if (!ISWIN)
                List<UUID> uuids = new List<UUID>();
                foreach (RemoveInventoryObjectsPacket.ItemDataBlock datablock in removeObject.ItemData)
                    uuids.Add(datablock.ItemID);
#else
                List<UUID> uuids = removeObject.ItemData.Select(datablock => datablock.ItemID).ToList();
#endif
                handlerRemoveInventoryItem = OnRemoveInventoryItem;
                if (handlerRemoveInventoryItem != null)
                {
                    handlerRemoveInventoryItem(this, uuids);
                }
            }
        }

        private bool HandleRequestTaskInventory(IClientAPI sender, Packet Pack)
        {
            RequestTaskInventoryPacket requesttask = (RequestTaskInventoryPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (requesttask.AgentData.SessionID != SessionId ||
                    requesttask.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            RequestTaskInventory handlerRequestTaskInventory = OnRequestTaskInventory;
            if (handlerRequestTaskInventory != null)
            {
                handlerRequestTaskInventory(this, requesttask.InventoryData.LocalID);
            }
            return true;
        }

        private bool HandleUpdateTaskInventory(IClientAPI sender, Packet Pack)
        {
            UpdateTaskInventoryPacket updatetask = (UpdateTaskInventoryPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (updatetask.AgentData.SessionID != SessionId ||
                    updatetask.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (OnUpdateTaskInventory != null)
            {
                if (updatetask.UpdateData.Key == 0)
                {
                    UpdateTaskInventory handlerUpdateTaskInventory = OnUpdateTaskInventory;
                    if (handlerUpdateTaskInventory != null)
                    {
                        TaskInventoryItem newTaskItem = new TaskInventoryItem
                                                            {
                                                                ItemID = updatetask.InventoryData.ItemID,
                                                                ParentID = updatetask.InventoryData.FolderID,
                                                                CreatorID = updatetask.InventoryData.CreatorID,
                                                                OwnerID = updatetask.InventoryData.OwnerID,
                                                                GroupID = updatetask.InventoryData.GroupID,
                                                                BasePermissions = updatetask.InventoryData.BaseMask,
                                                                CurrentPermissions = updatetask.InventoryData.OwnerMask,
                                                                GroupPermissions = updatetask.InventoryData.GroupMask,
                                                                EveryonePermissions =
                                                                    updatetask.InventoryData.EveryoneMask,
                                                                NextPermissions = updatetask.InventoryData.NextOwnerMask,
                                                                Type = updatetask.InventoryData.Type,
                                                                InvType = updatetask.InventoryData.InvType,
                                                                Flags = updatetask.InventoryData.Flags,
                                                                SaleType = updatetask.InventoryData.SaleType,
                                                                SalePrice = updatetask.InventoryData.SalePrice,
                                                                Name = Util.FieldToString(updatetask.InventoryData.Name),
                                                                Description =
                                                                    Util.FieldToString(
                                                                        updatetask.InventoryData.Description),
                                                                CreationDate =
                                                                    (uint) updatetask.InventoryData.CreationDate
                                                            };

                        // Unused?  Clicking share with group sets GroupPermissions instead, so perhaps this is something
                        // different
                        //newTaskItem.GroupOwned=updatetask.InventoryData.GroupOwned;
                        handlerUpdateTaskInventory(this, updatetask.InventoryData.TransactionID,
                                                   newTaskItem, updatetask.UpdateData.LocalID);
                    }
                }
            }

            return true;
        }

        private bool HandleRemoveTaskInventory(IClientAPI sender, Packet Pack)
        {
            RemoveTaskInventoryPacket removeTask = (RemoveTaskInventoryPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (removeTask.AgentData.SessionID != SessionId ||
                    removeTask.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            RemoveTaskInventory handlerRemoveTaskItem = OnRemoveTaskItem;

            if (handlerRemoveTaskItem != null)
            {
                handlerRemoveTaskItem(this, removeTask.InventoryData.ItemID, removeTask.InventoryData.LocalID);
            }

            return true;
        }

        private bool HandleMoveTaskInventory(IClientAPI sender, Packet Pack)
        {
            MoveTaskInventoryPacket moveTaskInventoryPacket = (MoveTaskInventoryPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (moveTaskInventoryPacket.AgentData.SessionID != SessionId ||
                    moveTaskInventoryPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            MoveTaskInventory handlerMoveTaskItem = OnMoveTaskItem;

            if (handlerMoveTaskItem != null)
            {
                handlerMoveTaskItem(
                    this, moveTaskInventoryPacket.AgentData.FolderID,
                    moveTaskInventoryPacket.InventoryData.LocalID,
                    moveTaskInventoryPacket.InventoryData.ItemID);
            }

            return true;
        }

        private bool HandleRezScript(IClientAPI sender, Packet Pack)
        {
            //MainConsole.Instance.Debug(Pack.ToString());
            RezScriptPacket rezScriptx = (RezScriptPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (rezScriptx.AgentData.SessionID != SessionId ||
                    rezScriptx.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            RezScript handlerRezScript = OnRezScript;
            InventoryItemBase item = new InventoryItemBase
                                         {
                                             ID = rezScriptx.InventoryBlock.ItemID,
                                             Folder = rezScriptx.InventoryBlock.FolderID,
                                             CreatorId = rezScriptx.InventoryBlock.CreatorID.ToString(),
                                             Owner = rezScriptx.InventoryBlock.OwnerID,
                                             BasePermissions = rezScriptx.InventoryBlock.BaseMask,
                                             CurrentPermissions = rezScriptx.InventoryBlock.OwnerMask,
                                             EveryOnePermissions = rezScriptx.InventoryBlock.EveryoneMask,
                                             NextPermissions = rezScriptx.InventoryBlock.NextOwnerMask,
                                             GroupPermissions = rezScriptx.InventoryBlock.GroupMask,
                                             GroupOwned = rezScriptx.InventoryBlock.GroupOwned,
                                             GroupID = rezScriptx.InventoryBlock.GroupID,
                                             AssetType = rezScriptx.InventoryBlock.Type,
                                             InvType = rezScriptx.InventoryBlock.InvType,
                                             Flags = rezScriptx.InventoryBlock.Flags,
                                             SaleType = rezScriptx.InventoryBlock.SaleType,
                                             SalePrice = rezScriptx.InventoryBlock.SalePrice,
                                             Name = Util.FieldToString(rezScriptx.InventoryBlock.Name),
                                             Description = Util.FieldToString(rezScriptx.InventoryBlock.Description),
                                             CreationDate = rezScriptx.InventoryBlock.CreationDate
                                         };

            if (handlerRezScript != null)
            {
                handlerRezScript(this, item, rezScriptx.InventoryBlock.TransactionID,
                                 rezScriptx.UpdateBlock.ObjectLocalID);
            }
            return true;
        }

        private bool HandleMapLayerRequest(IClientAPI sender, Packet Pack)
        {
            #region Packet Session and User Check

            if (m_checkPackets)
            {
                MapLayerRequestPacket mapLayerRequestPacket = Pack as MapLayerRequestPacket;
                if (mapLayerRequestPacket != null && (mapLayerRequestPacket.AgentData.SessionID != SessionId ||
                                                              mapLayerRequestPacket.AgentData.AgentID != AgentId))
                    return true;
            }

            #endregion

            return true;
        }

        private bool HandleMapBlockRequest(IClientAPI sender, Packet Pack)
        {
            MapBlockRequestPacket MapRequest = (MapBlockRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (MapRequest.AgentData.SessionID != SessionId ||
                    MapRequest.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            RequestMapBlocks handlerRequestMapBlocks = OnRequestMapBlocks;
            if (handlerRequestMapBlocks != null)
            {
                handlerRequestMapBlocks(this, MapRequest.PositionData.MinX, MapRequest.PositionData.MinY,
                                        MapRequest.PositionData.MaxX, MapRequest.PositionData.MaxY,
                                        MapRequest.AgentData.Flags);
            }
            return true;
        }

        private bool HandleMapNameRequest(IClientAPI sender, Packet Pack)
        {
            MapNameRequestPacket map = (MapNameRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (map.AgentData.SessionID != SessionId ||
                    map.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            string mapName = Util.UTF8.GetString(map.NameData.Name, 0,
                                                 map.NameData.Name.Length - 1);
            RequestMapName handlerMapNameRequest = OnMapNameRequest;
            if (handlerMapNameRequest != null)
            {
                handlerMapNameRequest(this, mapName, map.AgentData.Flags);
            }
            return true;
        }

        private bool HandleTeleportLandmarkRequest(IClientAPI sender, Packet Pack)
        {
            TeleportLandmarkRequestPacket tpReq = (TeleportLandmarkRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (tpReq.Info.SessionID != SessionId ||
                    tpReq.Info.AgentID != AgentId)
                    return true;
            }

            #endregion

            UUID lmid = tpReq.Info.LandmarkID;
            AssetLandmark lm;
            if (lmid != UUID.Zero)
            {
                m_assetService.Get(lmid.ToString(), null, (id, s, lma) =>
                    {
                        if (lma == null)
                        {
                            // Failed to find landmark
                            TeleportCancelPacket tpCancel =
                                (TeleportCancelPacket)PacketPool.Instance.GetPacket(PacketType.TeleportCancel);
                            tpCancel.Info.SessionID = tpReq.Info.SessionID;
                            tpCancel.Info.AgentID = tpReq.Info.AgentID;
                            OutPacket(tpCancel, ThrottleOutPacketType.Asset);
                        }

                        try
                        {
                            lm = new AssetLandmark(lma);
                        }
                        catch (NullReferenceException)
                        {
                            // asset not found generates null ref inside the assetlandmark constructor.
                            TeleportCancelPacket tpCancel =
                                (TeleportCancelPacket)PacketPool.Instance.GetPacket(PacketType.TeleportCancel);
                            tpCancel.Info.SessionID = tpReq.Info.SessionID;
                            tpCancel.Info.AgentID = tpReq.Info.AgentID;
                            OutPacket(tpCancel, ThrottleOutPacketType.Asset);
                            return;
                        }
                        TeleportLandmarkRequest handlerTeleportLandmarkRequest = OnTeleportLandmarkRequest;
                        if (handlerTeleportLandmarkRequest != null)
                        {
                            handlerTeleportLandmarkRequest(this, lm.RegionID, lm.Gatekeeper, lm.Position);
                        }
                        else
                        {
                            //no event handler so cancel request


                            TeleportCancelPacket tpCancel =
                                (TeleportCancelPacket)PacketPool.Instance.GetPacket(PacketType.TeleportCancel);
                            tpCancel.Info.AgentID = tpReq.Info.AgentID;
                            tpCancel.Info.SessionID = tpReq.Info.SessionID;
                            OutPacket(tpCancel, ThrottleOutPacketType.Asset);
                        }
                    });
            }
            else
            {
                // Teleport home request
                UUIDNameRequest handlerTeleportHomeRequest = OnTeleportHomeRequest;
                if (handlerTeleportHomeRequest != null)
                {
                    handlerTeleportHomeRequest(AgentId, this);
                }
                return true;
            }

            return true;
        }

        private bool HandleTeleportLocationRequest(IClientAPI sender, Packet Pack)
        {
            TeleportLocationRequestPacket tpLocReq = (TeleportLocationRequestPacket) Pack;
            // MainConsole.Instance.Debug(tpLocReq.ToString());

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (tpLocReq.AgentData.SessionID != SessionId ||
                    tpLocReq.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            TeleportLocationRequest handlerTeleportLocationRequest = OnTeleportLocationRequest;
            if (handlerTeleportLocationRequest != null)
            {
                handlerTeleportLocationRequest(this, tpLocReq.Info.RegionHandle, tpLocReq.Info.Position,
                                               tpLocReq.Info.LookAt, 16);
            }
            else
            {
                //no event handler so cancel request
                TeleportCancelPacket tpCancel =
                    (TeleportCancelPacket) PacketPool.Instance.GetPacket(PacketType.TeleportCancel);
                tpCancel.Info.SessionID = tpLocReq.AgentData.SessionID;
                tpCancel.Info.AgentID = tpLocReq.AgentData.AgentID;
                OutPacket(tpCancel, ThrottleOutPacketType.Asset);
            }
            return true;
        }

        #endregion Inventory/Asset/Other related packets

        private bool HandleUUIDNameRequest(IClientAPI sender, Packet Pack)
        {
            UUIDNameRequestPacket incoming = (UUIDNameRequestPacket) Pack;

            foreach (UUIDNameRequestPacket.UUIDNameBlockBlock UUIDBlock in incoming.UUIDNameBlock)
            {
                UUIDNameRequest handlerNameRequest = OnNameFromUUIDRequest;
                if (handlerNameRequest != null)
                {
                    handlerNameRequest(UUIDBlock.ID, this);
                }
            }
            return true;
        }

        #region Parcel related packets

        private bool HandleRegionHandleRequest(IClientAPI sender, Packet Pack)
        {
            RegionHandleRequestPacket rhrPack = (RegionHandleRequestPacket) Pack;

            RegionHandleRequest handlerRegionHandleRequest = OnRegionHandleRequest;
            if (handlerRegionHandleRequest != null)
            {
                handlerRegionHandleRequest(this, rhrPack.RequestBlock.RegionID);
            }
            return true;
        }

        private bool HandleParcelInfoRequest(IClientAPI sender, Packet Pack)
        {
            ParcelInfoRequestPacket pirPack = (ParcelInfoRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (pirPack.AgentData.SessionID != SessionId ||
                    pirPack.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ParcelInfoRequest handlerParcelInfoRequest = OnParcelInfoRequest;
            if (handlerParcelInfoRequest != null)
            {
                handlerParcelInfoRequest(this, pirPack.Data.ParcelID);
            }
            return true;
        }

        private bool HandleParcelAccessListRequest(IClientAPI sender, Packet Pack)
        {
            ParcelAccessListRequestPacket requestPacket = (ParcelAccessListRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (requestPacket.AgentData.SessionID != SessionId ||
                    requestPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ParcelAccessListRequest handlerParcelAccessListRequest = OnParcelAccessListRequest;

            if (handlerParcelAccessListRequest != null)
            {
                handlerParcelAccessListRequest(requestPacket.AgentData.AgentID, requestPacket.AgentData.SessionID,
                                               requestPacket.Data.Flags, requestPacket.Data.SequenceID,
                                               requestPacket.Data.LocalID, this);
            }
            return true;
        }

        private bool HandleParcelAccessListUpdate(IClientAPI sender, Packet Pack)
        {
            ParcelAccessListUpdatePacket updatePacket = (ParcelAccessListUpdatePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (updatePacket.AgentData.SessionID != SessionId ||
                    updatePacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

#if (!ISWIN)
            List<ParcelManager.ParcelAccessEntry> entries = new List<ParcelManager.ParcelAccessEntry>();
            foreach (ParcelAccessListUpdatePacket.ListBlock block in updatePacket.List)
                entries.Add(new ParcelManager.ParcelAccessEntry
                                {
                                    AgentID = block.ID, Flags = (AccessList) block.Flags, Time = new DateTime()
                                });
#else
            List<ParcelManager.ParcelAccessEntry> entries =
                updatePacket.List.Select(block => new ParcelManager.ParcelAccessEntry
                                                      {
                                                          AgentID = block.ID,
                                                          Flags = (AccessList) block.Flags,
                                                          Time = new DateTime()
                                                      }).ToList();
#endif

            ParcelAccessListUpdateRequest handlerParcelAccessListUpdateRequest = OnParcelAccessListUpdateRequest;
            if (handlerParcelAccessListUpdateRequest != null)
            {
                handlerParcelAccessListUpdateRequest(updatePacket.AgentData.AgentID,
                                                     updatePacket.AgentData.SessionID, updatePacket.Data.Flags,
                                                     updatePacket.Data.LocalID, entries, this);
            }
            return true;
        }

        private bool HandleParcelPropertiesRequest(IClientAPI sender, Packet Pack)
        {
            ParcelPropertiesRequestPacket propertiesRequest = (ParcelPropertiesRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (propertiesRequest.AgentData.SessionID != SessionId ||
                    propertiesRequest.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ParcelPropertiesRequest handlerParcelPropertiesRequest = OnParcelPropertiesRequest;
            if (handlerParcelPropertiesRequest != null)
            {
                handlerParcelPropertiesRequest((int) Math.Round(propertiesRequest.ParcelData.West),
                                               (int) Math.Round(propertiesRequest.ParcelData.South),
                                               (int) Math.Round(propertiesRequest.ParcelData.East),
                                               (int) Math.Round(propertiesRequest.ParcelData.North),
                                               propertiesRequest.ParcelData.SequenceID,
                                               propertiesRequest.ParcelData.SnapSelection, this);
            }
            return true;
        }

        private bool HandleParcelDivide(IClientAPI sender, Packet Pack)
        {
            ParcelDividePacket landDivide = (ParcelDividePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (landDivide.AgentData.SessionID != SessionId ||
                    landDivide.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ParcelDivideRequest handlerParcelDivideRequest = OnParcelDivideRequest;
            if (handlerParcelDivideRequest != null)
            {
                handlerParcelDivideRequest((int) Math.Round(landDivide.ParcelData.West),
                                           (int) Math.Round(landDivide.ParcelData.South),
                                           (int) Math.Round(landDivide.ParcelData.East),
                                           (int) Math.Round(landDivide.ParcelData.North), this);
            }
            return true;
        }

        private bool HandleParcelJoin(IClientAPI sender, Packet Pack)
        {
            ParcelJoinPacket landJoin = (ParcelJoinPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (landJoin.AgentData.SessionID != SessionId ||
                    landJoin.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ParcelJoinRequest handlerParcelJoinRequest = OnParcelJoinRequest;

            if (handlerParcelJoinRequest != null)
            {
                handlerParcelJoinRequest((int) Math.Round(landJoin.ParcelData.West),
                                         (int) Math.Round(landJoin.ParcelData.South),
                                         (int) Math.Round(landJoin.ParcelData.East),
                                         (int) Math.Round(landJoin.ParcelData.North), this);
            }
            return true;
        }

        private bool HandleParcelPropertiesUpdate(IClientAPI sender, Packet Pack)
        {
            ParcelPropertiesUpdatePacket parcelPropertiesPacket = (ParcelPropertiesUpdatePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (parcelPropertiesPacket.AgentData.SessionID != SessionId ||
                    parcelPropertiesPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ParcelPropertiesUpdateRequest handlerParcelPropertiesUpdateRequest = OnParcelPropertiesUpdateRequest;

            if (handlerParcelPropertiesUpdateRequest != null)
            {
                LandUpdateArgs args = new LandUpdateArgs
                                          {
                                              AuthBuyerID = parcelPropertiesPacket.ParcelData.AuthBuyerID,
                                              Category = (ParcelCategory) parcelPropertiesPacket.ParcelData.Category,
                                              Desc = Utils.BytesToString(parcelPropertiesPacket.ParcelData.Desc),
                                              GroupID = parcelPropertiesPacket.ParcelData.GroupID,
                                              LandingType = parcelPropertiesPacket.ParcelData.LandingType,
                                              MediaAutoScale = parcelPropertiesPacket.ParcelData.MediaAutoScale,
                                              MediaID = parcelPropertiesPacket.ParcelData.MediaID,
                                              MediaURL = Utils.BytesToString(parcelPropertiesPacket.ParcelData.MediaURL),
                                              MusicURL = Utils.BytesToString(parcelPropertiesPacket.ParcelData.MusicURL),
                                              Name = Utils.BytesToString(parcelPropertiesPacket.ParcelData.Name),
                                              ParcelFlags = parcelPropertiesPacket.ParcelData.ParcelFlags,
                                              PassHours = parcelPropertiesPacket.ParcelData.PassHours,
                                              PassPrice = parcelPropertiesPacket.ParcelData.PassPrice,
                                              SalePrice = parcelPropertiesPacket.ParcelData.SalePrice,
                                              SnapshotID = parcelPropertiesPacket.ParcelData.SnapshotID,
                                              UserLocation = parcelPropertiesPacket.ParcelData.UserLocation,
                                              UserLookAt = parcelPropertiesPacket.ParcelData.UserLookAt
                                          };

                handlerParcelPropertiesUpdateRequest(args, parcelPropertiesPacket.ParcelData.LocalID, this);
            }
            return true;
        }

        public void FireUpdateParcel(LandUpdateArgs args, int LocalID)
        {
            ParcelPropertiesUpdateRequest handlerParcelPropertiesUpdateRequest = OnParcelPropertiesUpdateRequest;

            if (handlerParcelPropertiesUpdateRequest != null)
            {
                handlerParcelPropertiesUpdateRequest(args, LocalID, this);
            }
        }

        private bool HandleParcelSelectObjects(IClientAPI sender, Packet Pack)
        {
            ParcelSelectObjectsPacket selectPacket = (ParcelSelectObjectsPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (selectPacket.AgentData.SessionID != SessionId ||
                    selectPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

#if (!ISWIN)
            List<UUID> returnIDs = new List<UUID>();
            foreach (ParcelSelectObjectsPacket.ReturnIDsBlock rb in selectPacket.ReturnIDs)
                returnIDs.Add(rb.ReturnID);
#else
            List<UUID> returnIDs = selectPacket.ReturnIDs.Select(rb => rb.ReturnID).ToList();
#endif  

            ParcelSelectObjects handlerParcelSelectObjects = OnParcelSelectObjects;

            if (handlerParcelSelectObjects != null)
            {
                handlerParcelSelectObjects(selectPacket.ParcelData.LocalID,
                                           Convert.ToInt32(selectPacket.ParcelData.ReturnType), returnIDs, this);
            }
            return true;
        }

        private bool HandleParcelObjectOwnersRequest(IClientAPI sender, Packet Pack)
        {
            ParcelObjectOwnersRequestPacket reqPacket = (ParcelObjectOwnersRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (reqPacket.AgentData.SessionID != SessionId ||
                    reqPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ParcelObjectOwnerRequest handlerParcelObjectOwnerRequest = OnParcelObjectOwnerRequest;

            if (handlerParcelObjectOwnerRequest != null)
            {
                handlerParcelObjectOwnerRequest(reqPacket.ParcelData.LocalID, this);
            }
            return true;
        }

        private bool HandleParcelGodForceOwner(IClientAPI sender, Packet Pack)
        {
            ParcelGodForceOwnerPacket godForceOwnerPacket = (ParcelGodForceOwnerPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (godForceOwnerPacket.AgentData.SessionID != SessionId ||
                    godForceOwnerPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ParcelGodForceOwner handlerParcelGodForceOwner = OnParcelGodForceOwner;
            if (handlerParcelGodForceOwner != null)
            {
                handlerParcelGodForceOwner(godForceOwnerPacket.Data.LocalID, godForceOwnerPacket.Data.OwnerID, this);
            }
            return true;
        }

        private bool HandleParcelRelease(IClientAPI sender, Packet Pack)
        {
            ParcelReleasePacket releasePacket = (ParcelReleasePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (releasePacket.AgentData.SessionID != SessionId ||
                    releasePacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ParcelAbandonRequest handlerParcelAbandonRequest = OnParcelAbandonRequest;
            if (handlerParcelAbandonRequest != null)
            {
                handlerParcelAbandonRequest(releasePacket.Data.LocalID, this);
            }
            return true;
        }

        private bool HandleParcelReclaim(IClientAPI sender, Packet Pack)
        {
            ParcelReclaimPacket reclaimPacket = (ParcelReclaimPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (reclaimPacket.AgentData.SessionID != SessionId ||
                    reclaimPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ParcelReclaim handlerParcelReclaim = OnParcelReclaim;
            if (handlerParcelReclaim != null)
            {
                handlerParcelReclaim(reclaimPacket.Data.LocalID, this);
            }
            return true;
        }

        private bool HandleParcelReturnObjects(IClientAPI sender, Packet Pack)
        {
            ParcelReturnObjectsPacket parcelReturnObjects = (ParcelReturnObjectsPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (parcelReturnObjects.AgentData.SessionID != SessionId ||
                    parcelReturnObjects.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            UUID[] puserselectedOwnerIDs = new UUID[parcelReturnObjects.OwnerIDs.Length];
            for (int parceliterator = 0; parceliterator < parcelReturnObjects.OwnerIDs.Length; parceliterator++)
                puserselectedOwnerIDs[parceliterator] = parcelReturnObjects.OwnerIDs[parceliterator].OwnerID;

            UUID[] puserselectedTaskIDs = new UUID[parcelReturnObjects.TaskIDs.Length];

            for (int parceliterator = 0; parceliterator < parcelReturnObjects.TaskIDs.Length; parceliterator++)
                puserselectedTaskIDs[parceliterator] = parcelReturnObjects.TaskIDs[parceliterator].TaskID;

            ParcelReturnObjectsRequest handlerParcelReturnObjectsRequest = OnParcelReturnObjectsRequest;
            if (handlerParcelReturnObjectsRequest != null)
            {
                handlerParcelReturnObjectsRequest(parcelReturnObjects.ParcelData.LocalID,
                                                  parcelReturnObjects.ParcelData.ReturnType, puserselectedOwnerIDs,
                                                  puserselectedTaskIDs, this);
            }
            return true;
        }

        private bool HandleParcelSetOtherCleanTime(IClientAPI sender, Packet Pack)
        {
            ParcelSetOtherCleanTimePacket parcelSetOtherCleanTimePacket = (ParcelSetOtherCleanTimePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (parcelSetOtherCleanTimePacket.AgentData.SessionID != SessionId ||
                    parcelSetOtherCleanTimePacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ParcelSetOtherCleanTime handlerParcelSetOtherCleanTime = OnParcelSetOtherCleanTime;
            if (handlerParcelSetOtherCleanTime != null)
            {
                handlerParcelSetOtherCleanTime(this,
                                               parcelSetOtherCleanTimePacket.ParcelData.LocalID,
                                               parcelSetOtherCleanTimePacket.ParcelData.OtherCleanTime);
            }
            return true;
        }

        private bool HandleLandStatRequest(IClientAPI sender, Packet Pack)
        {
            LandStatRequestPacket lsrp = (LandStatRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (lsrp.AgentData.SessionID != SessionId ||
                    lsrp.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            GodLandStatRequest handlerLandStatRequest = OnLandStatRequest;
            if (handlerLandStatRequest != null)
            {
                handlerLandStatRequest(lsrp.RequestData.ParcelLocalID, lsrp.RequestData.ReportType,
                                       lsrp.RequestData.RequestFlags, Utils.BytesToString(lsrp.RequestData.Filter), this);
            }
            return true;
        }

        private bool HandleParcelDwellRequest(IClientAPI sender, Packet Pack)
        {
            ParcelDwellRequestPacket dwellrq =
                (ParcelDwellRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (dwellrq.AgentData.SessionID != SessionId ||
                    dwellrq.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ParcelDwellRequest handlerParcelDwellRequest = OnParcelDwellRequest;
            if (handlerParcelDwellRequest != null)
            {
                handlerParcelDwellRequest(dwellrq.Data.LocalID, this);
            }
            return true;
        }

        #endregion Parcel related packets

        #region Estate Packets

        private bool HandleEstateOwnerMessage(IClientAPI sender, Packet Pack)
        {
            EstateOwnerMessagePacket messagePacket = (EstateOwnerMessagePacket) Pack;
            //MainConsole.Instance.Debug(messagePacket.ToString());
            GodLandStatRequest handlerLandStatRequest;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (messagePacket.AgentData.SessionID != SessionId ||
                    messagePacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            switch (Utils.BytesToString(messagePacket.MethodData.Method))
            {
                case "getinfo":
                    if (m_scene.Permissions.CanIssueEstateCommand(AgentId, false))
                    {
                        OnDetailedEstateDataRequest(this, messagePacket.MethodData.Invoice);
                    }
                    return true;
                case "setregioninfo":
                    if (m_scene.Permissions.CanIssueEstateCommand(AgentId, false))
                    {
                        OnSetEstateFlagsRequest(this, convertParamStringToBool(messagePacket.ParamList[0].Parameter),
                                                convertParamStringToBool(messagePacket.ParamList[1].Parameter),
                                                convertParamStringToBool(messagePacket.ParamList[2].Parameter),
                                                convertParamStringToBool(messagePacket.ParamList[3].Parameter),
                                                Convert.ToInt16(
                                                    Convert.ToDecimal(
                                                        Utils.BytesToString(messagePacket.ParamList[4].Parameter),
                                                        Culture.NumberFormatInfo)),
                                                (float)
                                                Convert.ToDecimal(
                                                    Utils.BytesToString(messagePacket.ParamList[5].Parameter),
                                                    Culture.NumberFormatInfo),
                                                Convert.ToInt16(Utils.BytesToString(messagePacket.ParamList[6].Parameter)),
                                                convertParamStringToBool(messagePacket.ParamList[7].Parameter),
                                                convertParamStringToBool(messagePacket.ParamList[8].Parameter));
                    }
                    return true;
                    //                            case "texturebase":
                    //                                if (((Scene)m_scene).Permissions.CanIssueEstateCommand(AgentId, false))
                    //                                {
                    //                                    foreach (EstateOwnerMessagePacket.ParamListBlock block in messagePacket.ParamList)
                    //                                    {
                    //                                        string s = Utils.BytesToString(block.Parameter);
                    //                                        string[] splitField = s.Split(' ');
                    //                                        if (splitField.Length == 2)
                    //                                        {
                    //                                            UUID tempUUID = new UUID(splitField[1]);
                    //                                            OnSetEstateTerrainBaseTexture(this, Convert.ToInt16(splitField[0]), tempUUID);
                    //                                        }
                    //                                    }
                    //                                }
                    //                                break;
                case "texturedetail":
                    if (m_scene.Permissions.CanIssueEstateCommand(AgentId, false))
                    {
                        foreach (EstateOwnerMessagePacket.ParamListBlock block in messagePacket.ParamList)
                        {
                            string s = Utils.BytesToString(block.Parameter);
                            string[] splitField = s.Split(' ');
                            if (splitField.Length == 2)
                            {
                                Int16 corner = Convert.ToInt16(splitField[0]);
                                UUID textureUUID = new UUID(splitField[1]);

                                OnSetEstateTerrainDetailTexture(this, corner, textureUUID);
                            }
                        }
                    }

                    return true;
                case "textureheights":
                    if (m_scene.Permissions.CanIssueEstateCommand(AgentId, false))
                    {
                        foreach (EstateOwnerMessagePacket.ParamListBlock block in messagePacket.ParamList)
                        {
                            string s = Utils.BytesToString(block.Parameter);
                            string[] splitField = s.Split(' ');
                            if (splitField.Length == 3)
                            {
                                Int16 corner = Convert.ToInt16(splitField[0]);
                                float lowValue = (float) Convert.ToDecimal(splitField[1], Culture.NumberFormatInfo);
                                float highValue = (float) Convert.ToDecimal(splitField[2], Culture.NumberFormatInfo);

                                OnSetEstateTerrainTextureHeights(this, corner, lowValue, highValue);
                            }
                        }
                    }
                    return true;
                case "texturecommit":
                    if (m_scene.Permissions.CanIssueEstateCommand(AgentId, false))
                    {
                        OnCommitEstateTerrainTextureRequest(this);
                    }
                    return true;
                case "setregionterrain":
                    if (m_scene.Permissions.CanIssueEstateCommand(AgentId, false))
                    {
                        if (messagePacket.ParamList.Length != 9)
                        {
                            MainConsole.Instance.Error("EstateOwnerMessage: SetRegionTerrain method has a ParamList of invalid length");
                        }
                        else
                        {
                            try
                            {
                                string tmp = Utils.BytesToString(messagePacket.ParamList[0].Parameter);
                                if (!tmp.Contains(".")) tmp += ".00";
                                float WaterHeight = (float) Convert.ToDecimal(tmp, Culture.NumberFormatInfo);
                                tmp = Utils.BytesToString(messagePacket.ParamList[1].Parameter);
                                if (!tmp.Contains(".")) tmp += ".00";
                                float TerrainRaiseLimit = (float) Convert.ToDecimal(tmp, Culture.NumberFormatInfo);
                                tmp = Utils.BytesToString(messagePacket.ParamList[2].Parameter);
                                if (!tmp.Contains(".")) tmp += ".00";
                                float TerrainLowerLimit = (float) Convert.ToDecimal(tmp, Culture.NumberFormatInfo);
                                bool UseEstateSun = convertParamStringToBool(messagePacket.ParamList[3].Parameter);
                                bool UseFixedSun = convertParamStringToBool(messagePacket.ParamList[4].Parameter);
                                float SunHour =
                                    (float)
                                    Convert.ToDecimal(Utils.BytesToString(messagePacket.ParamList[5].Parameter),
                                                      Culture.NumberFormatInfo);
                                bool UseGlobal = convertParamStringToBool(messagePacket.ParamList[6].Parameter);
                                bool EstateFixedSun = convertParamStringToBool(messagePacket.ParamList[7].Parameter);
                                float EstateSunHour =
                                    (float)
                                    Convert.ToDecimal(Utils.BytesToString(messagePacket.ParamList[8].Parameter),
                                                      Culture.NumberFormatInfo);

                                OnSetRegionTerrainSettings(AgentId, WaterHeight, TerrainRaiseLimit, TerrainLowerLimit,
                                                           UseEstateSun, UseFixedSun, SunHour, UseGlobal, EstateFixedSun,
                                                           EstateSunHour);
                            }
                            catch (Exception ex)
                            {
                                MainConsole.Instance.Error("EstateOwnerMessage: Exception while setting terrain settings: \n" +
                                            messagePacket + "\n" + ex);
                            }
                        }
                    }

                    return true;
                case "restart":
                    if (m_scene.Permissions.CanIssueEstateCommand(AgentId, false))
                    {
                        // There's only 1 block in the estateResetSim..   and that's the number of seconds till restart.
                        foreach (EstateOwnerMessagePacket.ParamListBlock block in messagePacket.ParamList)
                        {
                            float timeSeconds;
                            Utils.TryParseSingle(Utils.BytesToString(block.Parameter), out timeSeconds);
                            timeSeconds = (int) timeSeconds;
                            OnEstateRestartSimRequest(this, (int) timeSeconds);
                        }
                    }
                    return true;
                case "estatechangecovenantid":
                    if (m_scene.Permissions.CanIssueEstateCommand(AgentId, false))
                    {
#if (!ISWIN)
                        foreach (EstateOwnerMessagePacket.ParamListBlock block in messagePacket.ParamList)
                        {
                            UUID newCovenantID = new UUID(Utils.BytesToString(block.Parameter));
                            OnEstateChangeCovenantRequest(this, newCovenantID);
                        }
#else
                        foreach (UUID newCovenantID in messagePacket.ParamList.Select(block => new UUID(Utils.BytesToString(block.Parameter))))
                        {
                            OnEstateChangeCovenantRequest(this, newCovenantID);
                        }
#endif
                    }
                    return true;
                case "estateaccessdelta": // Estate access delta manages the banlist and allow list too.
                    if (m_scene.Permissions.CanIssueEstateCommand(AgentId, false))
                    {
                        int estateAccessType = Convert.ToInt16(Utils.BytesToString(messagePacket.ParamList[1].Parameter));
                        OnUpdateEstateAccessDeltaRequest(this, messagePacket.MethodData.Invoice, estateAccessType,
                                                         new UUID(
                                                             Utils.BytesToString(messagePacket.ParamList[2].Parameter)));
                    }
                    return true;
                case "simulatormessage":
                    if (m_scene.Permissions.CanIssueEstateCommand(AgentId, false))
                    {
                        UUID invoice = messagePacket.MethodData.Invoice;
                        UUID SenderID = new UUID(Utils.BytesToString(messagePacket.ParamList[2].Parameter));
                        string SenderName = Utils.BytesToString(messagePacket.ParamList[3].Parameter);
                        string Message = Utils.BytesToString(messagePacket.ParamList[4].Parameter);
                        UUID sessionID = messagePacket.AgentData.SessionID;
                        OnSimulatorBlueBoxMessageRequest(this, invoice, SenderID, sessionID, SenderName, Message);
                    }
                    return true;
                case "instantmessage":
                    if (m_scene.Permissions.CanIssueEstateCommand(AgentId, false))
                    {
                        UUID invoice = messagePacket.MethodData.Invoice;
                        UUID sessionID = messagePacket.AgentData.SessionID;
                        string Message = "";
                        string SenderName = "";
                        UUID SenderID = UUID.Zero;
                        if (messagePacket.ParamList.Length < 5)
                        {
                            SenderName = Utils.BytesToString(messagePacket.ParamList[0].Parameter);
                            Message = Utils.BytesToString(messagePacket.ParamList[1].Parameter);
                            SenderID = AgentId;
                        }
                        else
                        {
                            SenderID = new UUID(Utils.BytesToString(messagePacket.ParamList[2].Parameter));
                            SenderName = Utils.BytesToString(messagePacket.ParamList[3].Parameter);
                            Message = Utils.BytesToString(messagePacket.ParamList[4].Parameter);
                        }
                        OnEstateBlueBoxMessageRequest(this, invoice, SenderID, sessionID, SenderName, Message);
                    }
                    return true;
                case "setregiondebug":
                    if (m_scene.Permissions.CanIssueEstateCommand(AgentId, false))
                    {
                        UUID invoice = messagePacket.MethodData.Invoice;
                        UUID SenderID = messagePacket.AgentData.AgentID;
                        bool scripted = convertParamStringToBool(messagePacket.ParamList[0].Parameter);
                        bool collisionEvents = convertParamStringToBool(messagePacket.ParamList[1].Parameter);
                        bool physics = convertParamStringToBool(messagePacket.ParamList[2].Parameter);

                        OnEstateDebugRegionRequest(this, invoice, SenderID, scripted, collisionEvents, physics);
                    }
                    return true;
                case "teleporthomeuser":
                    if (m_scene.Permissions.CanIssueEstateCommand(AgentId, false))
                    {
                        UUID invoice = messagePacket.MethodData.Invoice;
                        UUID SenderID = messagePacket.AgentData.AgentID;
                        UUID Prey;

                        UUID.TryParse(Utils.BytesToString(messagePacket.ParamList[1].Parameter), out Prey);

                        OnEstateTeleportOneUserHomeRequest(this, invoice, SenderID, Prey);
                    }
                    return true;
                case "teleporthomeallusers":
                    if (m_scene.Permissions.CanIssueEstateCommand(AgentId, false))
                    {
                        UUID invoice = messagePacket.MethodData.Invoice;
                        UUID SenderID = messagePacket.AgentData.AgentID;
                        OnEstateTeleportAllUsersHomeRequest(this, invoice, SenderID);
                    }
                    return true;
                case "colliders":
                    if (m_scene.Permissions.CanIssueEstateCommand(AgentId, false))
                    {
                        handlerLandStatRequest = OnLandStatRequest;
                        if (handlerLandStatRequest != null)
                        {
                            handlerLandStatRequest(0, 1, 0, "", this);
                        }
                    }
                    return true;
                case "scripts":
                    if (m_scene.Permissions.CanIssueEstateCommand(AgentId, false))
                    {
                        handlerLandStatRequest = OnLandStatRequest;
                        if (handlerLandStatRequest != null)
                        {
                            handlerLandStatRequest(0, 0, 0, "", this);
                        }
                    }
                    return true;
                case "terrain":
                    if (m_scene.Permissions.CanIssueEstateCommand(AgentId, false))
                    {
                        if (messagePacket.ParamList.Length > 0)
                        {
                            if (Utils.BytesToString(messagePacket.ParamList[0].Parameter) == "bake")
                            {
                                BakeTerrain handlerBakeTerrain = OnBakeTerrain;
                                if (handlerBakeTerrain != null)
                                {
                                    handlerBakeTerrain(this);
                                }
                            }
                            if (Utils.BytesToString(messagePacket.ParamList[0].Parameter) == "download filename")
                            {
                                if (messagePacket.ParamList.Length > 1)
                                {
                                    RequestTerrain handlerRequestTerrain = OnRequestTerrain;
                                    if (handlerRequestTerrain != null)
                                    {
                                        handlerRequestTerrain(this,
                                                              Utils.BytesToString(messagePacket.ParamList[1].Parameter));
                                    }
                                }
                            }
                            if (Utils.BytesToString(messagePacket.ParamList[0].Parameter) == "upload filename")
                            {
                                if (messagePacket.ParamList.Length > 1)
                                {
                                    RequestTerrain handlerUploadTerrain = OnUploadTerrain;
                                    if (handlerUploadTerrain != null)
                                    {
                                        handlerUploadTerrain(this,
                                                             Utils.BytesToString(messagePacket.ParamList[1].Parameter));
                                    }
                                }
                            }
                        }
                    }
                    return true;

                case "estatechangeinfo":
                    if (m_scene.Permissions.CanIssueEstateCommand(AgentId, false))
                    {
                        UUID invoice = messagePacket.MethodData.Invoice;
                        UUID SenderID = messagePacket.AgentData.AgentID;
                        UInt32 param1 = Convert.ToUInt32(Utils.BytesToString(messagePacket.ParamList[1].Parameter));
                        UInt32 param2 = Convert.ToUInt32(Utils.BytesToString(messagePacket.ParamList[2].Parameter));

                        EstateChangeInfo handlerEstateChangeInfo = OnEstateChangeInfo;
                        if (handlerEstateChangeInfo != null)
                        {
                            handlerEstateChangeInfo(this, invoice, SenderID, param1, param2);
                        }
                    }
                    return true;

                case "refreshmapvisibility":
                    if (m_scene.Permissions.CanIssueEstateCommand(AgentId, false))
                    {
                        IMapImageGenerator mapModule = Scene.RequestModuleInterface<IMapImageGenerator>();
                        if(mapModule != null)
                            mapModule.CreateTerrainTexture();
                    }
                    return true;

                case "kickestate":
                    if (m_scene.Permissions.CanIssueEstateCommand(AgentId, false))
                    {
                        UUID Prey;

                        UUID.TryParse(Utils.BytesToString(messagePacket.ParamList[0].Parameter), out Prey);
                        IClientAPI client;
                        m_scene.ClientManager.TryGetValue(Prey, out client);
                        if (client == null)
                            return true;
                        client.Kick("The Aurora Manager has kicked you");
                        IEntityTransferModule transferModule = Scene.RequestModuleInterface<IEntityTransferModule>();
                        if (transferModule != null)
                            transferModule.IncomingCloseAgent(Scene, Prey);
                    }
                    return true;
                case "telehub":
                    if (m_scene.Permissions.CanIssueEstateCommand(AgentId, false))
                    {
#if (!ISWIN)
                        List<string> Parameters = new List<string>();
                        foreach (EstateOwnerMessagePacket.ParamListBlock block in messagePacket.ParamList)
                            Parameters.Add(Utils.BytesToString(block.Parameter));
#else
                        List<string> Parameters =
                            messagePacket.ParamList.Select(block => Utils.BytesToString(block.Parameter)).ToList();
#endif
                        GodlikeMessage handlerEstateTelehubRequest = OnEstateTelehubRequest;
                        if (handlerEstateTelehubRequest != null)
                        {
                            handlerEstateTelehubRequest(this,
                                                        messagePacket.MethodData.Invoice,
                                                        Utils.BytesToString(messagePacket.MethodData.Method),
                                                        Parameters);
                        }
                    }
                    return true;
                case "estateobjectreturn":
                    SimWideDeletesDelegate handlerSimWideDeletesRequest = OnSimWideDeletes;
                    if (handlerSimWideDeletesRequest != null)
                    {
                        UUID Prey;
                        UUID.TryParse(Utils.BytesToString(messagePacket.ParamList[1].Parameter), out Prey);
                        int flags = int.Parse(Utils.BytesToString(messagePacket.ParamList[0].Parameter));
                        handlerSimWideDeletesRequest(this, flags, Prey);
                        return true;
                    }
                    return true;
                default:
                    MainConsole.Instance.Error("EstateOwnerMessage: Unknown method requested " +
                                Utils.BytesToString(messagePacket.MethodData.Method));
                    return true;
            }
        }

        private bool HandleRequestRegionInfo(IClientAPI sender, Packet Pack)
        {
            RequestRegionInfoPacket.AgentDataBlock mPacket = ((RequestRegionInfoPacket) Pack).AgentData;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (mPacket.SessionID != SessionId ||
                    mPacket.AgentID != AgentId)
                    return true;
            }

            #endregion

            RegionInfoRequest handlerRegionInfoRequest = OnRegionInfoRequest;
            if (handlerRegionInfoRequest != null)
            {
                handlerRegionInfoRequest(this);
            }
            return true;
        }

        private bool HandleEstateCovenantRequest(IClientAPI sender, Packet Pack)
        {
            //EstateCovenantRequestPacket.AgentDataBlock epack =
            //     ((EstateCovenantRequestPacket)Pack).AgentData;

            EstateCovenantRequest handlerEstateCovenantRequest = OnEstateCovenantRequest;
            if (handlerEstateCovenantRequest != null)
            {
                handlerEstateCovenantRequest(this);
            }
            return true;
        }

        #endregion Estate Packets

        #region GodPackets

        private bool HandleRequestGodlikePowers(IClientAPI sender, Packet Pack)
        {
            RequestGodlikePowersPacket rglpPack = (RequestGodlikePowersPacket) Pack;
            RequestGodlikePowersPacket.RequestBlockBlock rblock = rglpPack.RequestBlock;
            UUID token = rblock.Token;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (rglpPack.AgentData.SessionID != SessionId ||
                    rglpPack.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            RequestGodlikePowersPacket.AgentDataBlock ablock = rglpPack.AgentData;

            RequestGodlikePowers handlerReqGodlikePowers = OnRequestGodlikePowers;

            if (handlerReqGodlikePowers != null)
            {
                handlerReqGodlikePowers(ablock.AgentID, ablock.SessionID, token, rblock.Godlike, this);
            }

            return true;
        }

        private bool HandleGodUpdateRegionInfoUpdate(IClientAPI client, Packet Packet)
        {
            GodUpdateRegionInfoPacket GodUpdateRegionInfo =
                (GodUpdateRegionInfoPacket) Packet;

            GodUpdateRegionInfoUpdate handlerGodUpdateRegionInfo = OnGodUpdateRegionInfoUpdate;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (GodUpdateRegionInfo.AgentData.SessionID != SessionId ||
                    GodUpdateRegionInfo.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (handlerGodUpdateRegionInfo != null)
            {
                handlerGodUpdateRegionInfo(this,
                                           GodUpdateRegionInfo.RegionInfo.BillableFactor,
                                           GodUpdateRegionInfo.RegionInfo.PricePerMeter,
                                           GodUpdateRegionInfo.RegionInfo.EstateID,
                                           GodUpdateRegionInfo.RegionInfo.RegionFlags,
                                           GodUpdateRegionInfo.RegionInfo.SimName,
                                           GodUpdateRegionInfo.RegionInfo.RedirectGridX,
                                           GodUpdateRegionInfo.RegionInfo.RedirectGridY);
                return true;
            }
            return false;
        }

        private bool HandleSimWideDeletes(IClientAPI client, Packet Packet)
        {
            SimWideDeletesPacket SimWideDeletesRequest =
                (SimWideDeletesPacket) Packet;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (SimWideDeletesRequest.AgentData.SessionID != SessionId ||
                    SimWideDeletesRequest.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            SimWideDeletesDelegate handlerSimWideDeletesRequest = OnSimWideDeletes;
            if (handlerSimWideDeletesRequest != null)
            {
                handlerSimWideDeletesRequest(this, (int) SimWideDeletesRequest.DataBlock.Flags,
                                             SimWideDeletesRequest.DataBlock.TargetID);
                return true;
            }
            return false;
        }

        private bool HandleGodlikeMessage(IClientAPI client, Packet Packet)
        {
            GodlikeMessagePacket GodlikeMessage =
                (GodlikeMessagePacket) Packet;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (GodlikeMessage.AgentData.SessionID != SessionId ||
                    GodlikeMessage.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            GodlikeMessage handlerGodlikeMessage = OnGodlikeMessage;
#if (!ISWIN)
            List<string> Parameters = new List<string>();
            foreach (GodlikeMessagePacket.ParamListBlock block in GodlikeMessage.ParamList)
                Parameters.Add(Utils.BytesToString(block.Parameter));
#else
            List<string> Parameters =
                GodlikeMessage.ParamList.Select(block => Utils.BytesToString(block.Parameter)).ToList();
#endif
            if (handlerGodlikeMessage != null)
            {
                handlerGodlikeMessage(this,
                                      GodlikeMessage.MethodData.Invoice,
                                      Utils.BytesToString(GodlikeMessage.MethodData.Method),
                                      Parameters);
                return true;
            }
            return false;
        }

        private bool HandleSaveStatePacket(IClientAPI client, Packet Packet)
        {
            StateSavePacket SaveStateMessage =
                (StateSavePacket) Packet;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (SaveStateMessage.AgentData.SessionID != SessionId ||
                    SaveStateMessage.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            SaveStateHandler handlerSaveStatePacket = OnSaveState;
            if (handlerSaveStatePacket != null)
            {
                handlerSaveStatePacket(this, SaveStateMessage.AgentData.AgentID);
                return true;
            }
            return false;
        }

        private bool HandleGodKickUser(IClientAPI sender, Packet Pack)
        {
            GodKickUserPacket gkupack = (GodKickUserPacket) Pack;

            if (gkupack.UserInfo.GodSessionID == SessionId && AgentId == gkupack.UserInfo.GodID)
            {
                GodKickUser handlerGodKickUser = OnGodKickUser;
                if (handlerGodKickUser != null)
                {
                    handlerGodKickUser(gkupack.UserInfo.GodID, gkupack.UserInfo.GodSessionID,
                                       gkupack.UserInfo.AgentID, gkupack.UserInfo.KickFlags, gkupack.UserInfo.Reason);
                }
            }
            else
            {
                SendAgentAlertMessage("Kick request denied", false);
            }
            return true;
        }

        #endregion GodPackets

        #region Economy/Transaction Packets

        private bool HandleMoneyBalanceRequest(IClientAPI sender, Packet Pack)
        {
            MoneyBalanceRequestPacket moneybalancerequestpacket = (MoneyBalanceRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (moneybalancerequestpacket.AgentData.SessionID != SessionId ||
                    moneybalancerequestpacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            MoneyBalanceRequest handlerMoneyBalanceRequest = OnMoneyBalanceRequest;

            if (handlerMoneyBalanceRequest != null)
            {
                handlerMoneyBalanceRequest(this, moneybalancerequestpacket.AgentData.AgentID,
                                           moneybalancerequestpacket.AgentData.SessionID,
                                           moneybalancerequestpacket.MoneyData.TransactionID);
            }

            return true;
        }

        private bool HandleEconomyDataRequest(IClientAPI sender, Packet Pack)
        {
            EconomyDataRequest handlerEconomoyDataRequest = OnEconomyDataRequest;
            if (handlerEconomoyDataRequest != null)
            {
                handlerEconomoyDataRequest(this);
            }
            return true;
        }

        private bool HandleRequestPayPrice(IClientAPI sender, Packet Pack)
        {
            RequestPayPricePacket requestPayPricePacket = (RequestPayPricePacket) Pack;

            RequestPayPrice handlerRequestPayPrice = OnRequestPayPrice;
            if (handlerRequestPayPrice != null)
            {
                handlerRequestPayPrice(this, requestPayPricePacket.ObjectData.ObjectID);
            }
            return true;
        }

        private bool HandleObjectSaleInfo(IClientAPI sender, Packet Pack)
        {
            ObjectSaleInfoPacket objectSaleInfoPacket = (ObjectSaleInfoPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (objectSaleInfoPacket.AgentData.SessionID != SessionId ||
                    objectSaleInfoPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ObjectSaleInfo handlerObjectSaleInfo = OnObjectSaleInfo;
            if (handlerObjectSaleInfo != null)
            {
                foreach (ObjectSaleInfoPacket.ObjectDataBlock d
                    in objectSaleInfoPacket.ObjectData)
                {
                    handlerObjectSaleInfo(this,
                                          objectSaleInfoPacket.AgentData.SessionID,
                                          d.LocalID,
                                          d.SaleType,
                                          d.SalePrice);
                }
            }
            return true;
        }

        private bool HandleObjectBuy(IClientAPI sender, Packet Pack)
        {
            ObjectBuyPacket objectBuyPacket = (ObjectBuyPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (objectBuyPacket.AgentData.SessionID != SessionId ||
                    objectBuyPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ObjectBuy handlerObjectBuy = OnObjectBuy;

            if (handlerObjectBuy != null)
            {
                foreach (ObjectBuyPacket.ObjectDataBlock d
                    in objectBuyPacket.ObjectData)
                {
                    handlerObjectBuy(this,
                                     objectBuyPacket.AgentData.SessionID,
                                     objectBuyPacket.AgentData.GroupID,
                                     objectBuyPacket.AgentData.CategoryID,
                                     d.ObjectLocalID,
                                     d.SaleType,
                                     d.SalePrice);
                }
            }
            return true;
        }

        #endregion Economy/Transaction Packets

        #region Script Packets

        private bool HandleGetScriptRunning(IClientAPI sender, Packet Pack)
        {
            GetScriptRunningPacket scriptRunning = (GetScriptRunningPacket) Pack;

            GetScriptRunning handlerGetScriptRunning = OnGetScriptRunning;
            if (handlerGetScriptRunning != null)
            {
                handlerGetScriptRunning(this, scriptRunning.Script.ObjectID, scriptRunning.Script.ItemID);
            }
            return true;
        }

        private bool HandleSetScriptRunning(IClientAPI sender, Packet Pack)
        {
            SetScriptRunningPacket setScriptRunning = (SetScriptRunningPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (setScriptRunning.AgentData.SessionID != SessionId ||
                    setScriptRunning.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            SetScriptRunning handlerSetScriptRunning = OnSetScriptRunning;
            if (handlerSetScriptRunning != null)
            {
                handlerSetScriptRunning(this, setScriptRunning.Script.ObjectID, setScriptRunning.Script.ItemID,
                                        setScriptRunning.Script.Running);
            }
            return true;
        }

        private bool HandleScriptReset(IClientAPI sender, Packet Pack)
        {
            ScriptResetPacket scriptResetPacket = (ScriptResetPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (scriptResetPacket.AgentData.SessionID != SessionId ||
                    scriptResetPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ScriptReset handlerScriptReset = OnScriptReset;
            if (handlerScriptReset != null)
            {
                handlerScriptReset(this, scriptResetPacket.Script.ObjectID, scriptResetPacket.Script.ItemID);
            }
            return true;
        }

        #endregion Script Packets

        #region Gesture Managment

        private bool HandleActivateGestures(IClientAPI sender, Packet Pack)
        {
            ActivateGesturesPacket activateGesturePacket = (ActivateGesturesPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (activateGesturePacket.AgentData.SessionID != SessionId ||
                    activateGesturePacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ActivateGesture handlerActivateGesture = OnActivateGesture;
            if (handlerActivateGesture != null)
            {
                handlerActivateGesture(this,
                                       activateGesturePacket.Data[0].AssetID,
                                       activateGesturePacket.Data[0].ItemID);
            }
            else MainConsole.Instance.Error("Null pointer for activateGesture");

            return true;
        }

        private bool HandleDeactivateGestures(IClientAPI sender, Packet Pack)
        {
            DeactivateGesturesPacket deactivateGesturePacket = (DeactivateGesturesPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (deactivateGesturePacket.AgentData.SessionID != SessionId ||
                    deactivateGesturePacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            DeactivateGesture handlerDeactivateGesture = OnDeactivateGesture;
            if (handlerDeactivateGesture != null)
            {
                handlerDeactivateGesture(this, deactivateGesturePacket.Data[0].ItemID);
            }
            return true;
        }

        private bool HandleObjectOwner(IClientAPI sender, Packet Pack)
        {
            ObjectOwnerPacket objectOwnerPacket = (ObjectOwnerPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (objectOwnerPacket.AgentData.SessionID != SessionId ||
                    objectOwnerPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

#if (!ISWIN)
            List<uint> localIDs = new List<uint>();
            foreach (ObjectOwnerPacket.ObjectDataBlock d in objectOwnerPacket.ObjectData)
                localIDs.Add(d.ObjectLocalID);
#else
            List<uint> localIDs = objectOwnerPacket.ObjectData.Select(d => d.ObjectLocalID).ToList();
#endif

            ObjectOwner handlerObjectOwner = OnObjectOwner;
            if (handlerObjectOwner != null)
            {
                handlerObjectOwner(this, objectOwnerPacket.HeaderData.OwnerID, objectOwnerPacket.HeaderData.GroupID,
                                   localIDs);
            }
            return true;
        }

        #endregion Gesture Managment

        private bool HandleAgentFOV(IClientAPI sender, Packet Pack)
        {
            AgentFOVPacket fovPacket = (AgentFOVPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (fovPacket.AgentData.SessionID != SessionId ||
                    fovPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (fovPacket.FOVBlock.GenCounter > m_agentFOVCounter)
            {
                m_agentFOVCounter = fovPacket.FOVBlock.GenCounter;
                AgentFOV handlerAgentFOV = OnAgentFOV;
                if (handlerAgentFOV != null)
                {
                    handlerAgentFOV(this, fovPacket.FOVBlock.VerticalAngle);
                }
            }
            return true;
        }

        #region unimplemented handlers

        private bool HandleViewerStats(IClientAPI sender, Packet Pack)
        {
            //MainConsole.Instance.Warn("[CLIENT]: unhandled ViewerStats packet");
            return true;
        }

        private bool HandleUseCircuitCode(IClientAPI sender, Packet Pack)
        {
            return true;
        }

        private bool HandleAgentHeightWidth(IClientAPI sender, Packet Pack)
        {
            return true;
        }

        #endregion unimplemented handlers

        private bool HandleMapItemRequest(IClientAPI sender, Packet Pack)
        {
            MapItemRequestPacket mirpk = (MapItemRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (mirpk.AgentData.SessionID != SessionId ||
                    mirpk.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            //MainConsole.Instance.Debug(mirpk.ToString());
            MapItemRequest handlerMapItemRequest = OnMapItemRequest;
            if (handlerMapItemRequest != null)
            {
                handlerMapItemRequest(this, mirpk.AgentData.Flags, mirpk.AgentData.EstateID,
                                      mirpk.AgentData.Godlike, mirpk.RequestData.ItemType,
                                      mirpk.RequestData.RegionHandle);
            }
            return true;
        }

        private bool HandleMuteListRequest(IClientAPI sender, Packet Pack)
        {
            MuteListRequestPacket muteListRequest =
                (MuteListRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (muteListRequest.AgentData.SessionID != SessionId ||
                    muteListRequest.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            MuteListRequest handlerMuteListRequest = OnMuteListRequest;
            if (handlerMuteListRequest != null)
            {
                handlerMuteListRequest(this, muteListRequest.MuteData.MuteCRC);
            }
            else
            {
                SendUseCachedMuteList();
            }
            return true;
        }

        private bool HandleUpdateMuteListEntry(IClientAPI client, Packet Packet)
        {
            UpdateMuteListEntryPacket UpdateMuteListEntry =
                (UpdateMuteListEntryPacket) Packet;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (UpdateMuteListEntry.AgentData.SessionID != SessionId ||
                    UpdateMuteListEntry.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            MuteListEntryUpdate handlerUpdateMuteListEntry = OnUpdateMuteListEntry;
            if (handlerUpdateMuteListEntry != null)
            {
                handlerUpdateMuteListEntry(this, UpdateMuteListEntry.MuteData.MuteID,
                                           Utils.BytesToString(UpdateMuteListEntry.MuteData.MuteName),
                                           UpdateMuteListEntry.MuteData.MuteType,
                                           UpdateMuteListEntry.AgentData.AgentID);
                return true;
            }
            return false;
        }

        private bool HandleRemoveMuteListEntry(IClientAPI client, Packet Packet)
        {
            RemoveMuteListEntryPacket RemoveMuteListEntry =
                (RemoveMuteListEntryPacket) Packet;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (RemoveMuteListEntry.AgentData.SessionID != SessionId ||
                    RemoveMuteListEntry.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            MuteListEntryRemove handlerRemoveMuteListEntry = OnRemoveMuteListEntry;
            if (handlerRemoveMuteListEntry != null)
            {
                handlerRemoveMuteListEntry(this,
                                           RemoveMuteListEntry.MuteData.MuteID,
                                           Utils.BytesToString(RemoveMuteListEntry.MuteData.MuteName),
                                           RemoveMuteListEntry.AgentData.AgentID);
                return true;
            }
            return false;
        }

        private bool HandleUserReport(IClientAPI client, Packet Packet)
        {
            UserReportPacket UserReport =
                (UserReportPacket) Packet;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (UserReport.AgentData.SessionID != SessionId ||
                    UserReport.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            NewUserReport handlerUserReport = OnUserReport;
            if (handlerUserReport != null)
            {
                handlerUserReport(this,
                                  Utils.BytesToString(UserReport.ReportData.AbuseRegionName),
                                  UserReport.ReportData.AbuserID,
                                  UserReport.ReportData.Category,
                                  UserReport.ReportData.CheckFlags,
                                  Utils.BytesToString(UserReport.ReportData.Details),
                                  UserReport.ReportData.ObjectID,
                                  UserReport.ReportData.Position,
                                  UserReport.ReportData.ReportType,
                                  UserReport.ReportData.ScreenshotID,
                                  Utils.BytesToString(UserReport.ReportData.Summary),
                                  UserReport.AgentData.AgentID);
                return true;
            }
            return false;
        }

        private bool HandleSendPostcard(IClientAPI client, Packet packet)
        {
            SendPostcardPacket SendPostcard =
                (SendPostcardPacket) packet;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (SendPostcard.AgentData.SessionID != SessionId ||
                    SendPostcard.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            SendPostcard handlerSendPostcard = OnSendPostcard;
            if (handlerSendPostcard != null)
            {
                handlerSendPostcard(this);
                return true;
            }
            return false;
        }

        private bool HandleViewerStartAuction(IClientAPI client, Packet packet)
        {
            ViewerStartAuctionPacket aPacket = (ViewerStartAuctionPacket) packet;
            ViewerStartAuction handlerStartAuction = OnViewerStartAuction;

            if (handlerStartAuction != null)
            {
                handlerStartAuction(this, aPacket.ParcelData.LocalID, aPacket.ParcelData.SnapshotID);
                return true;
            }
            return false;
        }

        private bool HandleParcelDisableObjects(IClientAPI client, Packet packet)
        {
            ParcelDisableObjectsPacket aPacket = (ParcelDisableObjectsPacket) packet;
            ParcelReturnObjectsRequest handlerParcelDisableObjectsRequest = OnParcelDisableObjectsRequest;

            if (handlerParcelDisableObjectsRequest != null)
            {
#if (!ISWIN)
                List<UUID> list = new List<UUID>();
                foreach (ParcelDisableObjectsPacket.TaskIDsBlock block in aPacket.TaskIDs)
                    list.Add(block.TaskID);
                List<UUID> list1 = new List<UUID>();
                foreach (ParcelDisableObjectsPacket.OwnerIDsBlock block in aPacket.OwnerIDs)
                    list1.Add(block.OwnerID);
                handlerParcelDisableObjectsRequest(aPacket.ParcelData.LocalID, aPacket.ParcelData.ReturnType,
                                                   list1.ToArray(),
                                                   list.ToArray(), this);
#else
                handlerParcelDisableObjectsRequest(aPacket.ParcelData.LocalID, aPacket.ParcelData.ReturnType,
                                                   aPacket.OwnerIDs.Select(block => block.OwnerID).ToArray(),
                                                   aPacket.TaskIDs.Select(block => block.TaskID).ToArray(), this);
#endif
                return true;
            }
            return false;
        }

        private bool HandleTeleportCancel(IClientAPI client, Packet packet)
        {
            TeleportCancel handlerTeleportCancel = OnTeleportCancel;

            if (handlerTeleportCancel != null)
            {
                handlerTeleportCancel(this);
                return true;
            }
            return false;
        }

        #region Dir handlers

        private bool HandleDirPlacesQuery(IClientAPI sender, Packet Pack)
        {
            DirPlacesQueryPacket dirPlacesQueryPacket = (DirPlacesQueryPacket) Pack;
            //MainConsole.Instance.Debug(dirPlacesQueryPacket.ToString());

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (dirPlacesQueryPacket.AgentData.SessionID != SessionId ||
                    dirPlacesQueryPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            DirPlacesQuery handlerDirPlacesQuery = OnDirPlacesQuery;
            if (handlerDirPlacesQuery != null)
            {
                handlerDirPlacesQuery(this,
                                      dirPlacesQueryPacket.QueryData.QueryID,
                                      Utils.BytesToString(
                                          dirPlacesQueryPacket.QueryData.QueryText),
                                      (int) dirPlacesQueryPacket.QueryData.QueryFlags,
                                      dirPlacesQueryPacket.QueryData.Category,
                                      Utils.BytesToString(
                                          dirPlacesQueryPacket.QueryData.SimName),
                                      dirPlacesQueryPacket.QueryData.QueryStart);
            }
            return true;
        }

        private bool HandleDirFindQuery(IClientAPI sender, Packet Pack)
        {
            DirFindQueryPacket dirFindQueryPacket = (DirFindQueryPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (dirFindQueryPacket.AgentData.SessionID != SessionId ||
                    dirFindQueryPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            DirFindQuery handlerDirFindQuery = OnDirFindQuery;
            if (handlerDirFindQuery != null)
            {
                handlerDirFindQuery(this,
                                    dirFindQueryPacket.QueryData.QueryID,
                                    Utils.BytesToString(
                                        dirFindQueryPacket.QueryData.QueryText),
                                    dirFindQueryPacket.QueryData.QueryFlags,
                                    dirFindQueryPacket.QueryData.QueryStart);
            }
            return true;
        }

        private bool HandleDirLandQuery(IClientAPI sender, Packet Pack)
        {
            DirLandQueryPacket dirLandQueryPacket = (DirLandQueryPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (dirLandQueryPacket.AgentData.SessionID != SessionId ||
                    dirLandQueryPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            DirLandQuery handlerDirLandQuery = OnDirLandQuery;
            if (handlerDirLandQuery != null)
            {
                handlerDirLandQuery(this,
                                    dirLandQueryPacket.QueryData.QueryID,
                                    dirLandQueryPacket.QueryData.QueryFlags,
                                    dirLandQueryPacket.QueryData.SearchType,
                                    (uint)dirLandQueryPacket.QueryData.Price,
                                    (uint)dirLandQueryPacket.QueryData.Area,
                                    dirLandQueryPacket.QueryData.QueryStart);
            }
            return true;
        }

        private bool HandleDirPopularQuery(IClientAPI sender, Packet Pack)
        {
            DirPopularQueryPacket dirPopularQueryPacket = (DirPopularQueryPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (dirPopularQueryPacket.AgentData.SessionID != SessionId ||
                    dirPopularQueryPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            DirPopularQuery handlerDirPopularQuery = OnDirPopularQuery;
            if (handlerDirPopularQuery != null)
            {
                handlerDirPopularQuery(this,
                                       dirPopularQueryPacket.QueryData.QueryID,
                                       dirPopularQueryPacket.QueryData.QueryFlags);
            }
            return true;
        }

        private bool HandleDirClassifiedQuery(IClientAPI sender, Packet Pack)
        {
            DirClassifiedQueryPacket dirClassifiedQueryPacket = (DirClassifiedQueryPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (dirClassifiedQueryPacket.AgentData.SessionID != SessionId ||
                    dirClassifiedQueryPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            DirClassifiedQuery handlerDirClassifiedQuery = OnDirClassifiedQuery;
            if (handlerDirClassifiedQuery != null)
            {
                handlerDirClassifiedQuery(this,
                                          dirClassifiedQueryPacket.QueryData.QueryID,
                                          Utils.BytesToString(
                                              dirClassifiedQueryPacket.QueryData.QueryText),
                                          dirClassifiedQueryPacket.QueryData.QueryFlags,
                                          dirClassifiedQueryPacket.QueryData.Category,
                                          dirClassifiedQueryPacket.QueryData.QueryStart);
            }
            return true;
        }

        private bool HandleEventInfoRequest(IClientAPI sender, Packet Pack)
        {
            EventInfoRequestPacket eventInfoRequestPacket = (EventInfoRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (eventInfoRequestPacket.AgentData.SessionID != SessionId ||
                    eventInfoRequestPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (OnEventInfoRequest != null)
            {
                OnEventInfoRequest(this, eventInfoRequestPacket.EventData.EventID);
            }
            return true;
        }

        #endregion

        #region Calling Card

        private bool HandleOfferCallingCard(IClientAPI sender, Packet Pack)
        {
            OfferCallingCardPacket offerCallingCardPacket = (OfferCallingCardPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (offerCallingCardPacket.AgentData.SessionID != SessionId ||
                    offerCallingCardPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (OnOfferCallingCard != null)
            {
                OnOfferCallingCard(this,
                                   offerCallingCardPacket.AgentBlock.DestID,
                                   offerCallingCardPacket.AgentBlock.TransactionID);
            }
            return true;
        }

        private bool HandleAcceptCallingCard(IClientAPI sender, Packet Pack)
        {
            AcceptCallingCardPacket acceptCallingCardPacket = (AcceptCallingCardPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (acceptCallingCardPacket.AgentData.SessionID != SessionId ||
                    acceptCallingCardPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            // according to http://wiki.secondlife.com/wiki/AcceptCallingCard FolderData should
            // contain exactly one entry
            if (OnAcceptCallingCard != null && acceptCallingCardPacket.FolderData.Length > 0)
            {
                OnAcceptCallingCard(this,
                                    acceptCallingCardPacket.TransactionBlock.TransactionID,
                                    acceptCallingCardPacket.FolderData[0].FolderID);
            }
            return true;
        }

        private bool HandleDeclineCallingCard(IClientAPI sender, Packet Pack)
        {
            DeclineCallingCardPacket declineCallingCardPacket = (DeclineCallingCardPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (declineCallingCardPacket.AgentData.SessionID != SessionId ||
                    declineCallingCardPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (OnDeclineCallingCard != null)
            {
                OnDeclineCallingCard(this,
                                     declineCallingCardPacket.TransactionBlock.TransactionID);
            }
            return true;
        }

        #endregion Calling Card

        #region Groups

        private bool HandleActivateGroup(IClientAPI sender, Packet Pack)
        {
            ActivateGroupPacket activateGroupPacket = (ActivateGroupPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (activateGroupPacket.AgentData.SessionID != SessionId ||
                    activateGroupPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (m_GroupsModule != null)
            {
                m_GroupsModule.ActivateGroup(this, activateGroupPacket.AgentData.GroupID);
            }
            return true;
        }

        private bool HandleGroupVoteHistoryRequest(IClientAPI client, Packet Packet)
        {
            GroupVoteHistoryRequestPacket GroupVoteHistoryRequest =
                (GroupVoteHistoryRequestPacket) Packet;
            GroupVoteHistoryRequest handlerGroupVoteHistoryRequest = OnGroupVoteHistoryRequest;
            if (handlerGroupVoteHistoryRequest != null)
            {
                handlerGroupVoteHistoryRequest(this, GroupVoteHistoryRequest.AgentData.AgentID,
                                               GroupVoteHistoryRequest.AgentData.SessionID,
                                               GroupVoteHistoryRequest.GroupData.GroupID,
                                               GroupVoteHistoryRequest.TransactionData.TransactionID);
                return true;
            }
            return false;
        }

        private bool HandleGroupProposalBallot(IClientAPI client, Packet Packet)
        {
            GroupProposalBallotPacket GroupProposalBallotRequest =
                (GroupProposalBallotPacket) Packet;
            GroupProposalBallotRequest handlerGroupActiveProposalsRequest = OnGroupProposalBallotRequest;
            if (handlerGroupActiveProposalsRequest != null)
            {
                handlerGroupActiveProposalsRequest(this, GroupProposalBallotRequest.AgentData.AgentID,
                                                   GroupProposalBallotRequest.AgentData.SessionID,
                                                   GroupProposalBallotRequest.ProposalData.GroupID,
                                                   GroupProposalBallotRequest.ProposalData.ProposalID,
                                                   Utils.BytesToString(GroupProposalBallotRequest.ProposalData.VoteCast));
                return true;
            }
            return false;
        }

        private bool HandleGroupActiveProposalsRequest(IClientAPI client, Packet Packet)
        {
            GroupActiveProposalsRequestPacket GroupActiveProposalsRequest =
                (GroupActiveProposalsRequestPacket) Packet;
            GroupActiveProposalsRequest handlerGroupActiveProposalsRequest = OnGroupActiveProposalsRequest;
            if (handlerGroupActiveProposalsRequest != null)
            {
                handlerGroupActiveProposalsRequest(this, GroupActiveProposalsRequest.AgentData.AgentID,
                                                   GroupActiveProposalsRequest.AgentData.SessionID,
                                                   GroupActiveProposalsRequest.GroupData.GroupID,
                                                   GroupActiveProposalsRequest.TransactionData.TransactionID);
                return true;
            }
            return false;
        }

        private bool HandleGroupAccountDetailsRequest(IClientAPI client, Packet Packet)
        {
            GroupAccountDetailsRequestPacket GroupAccountDetailsRequest =
                (GroupAccountDetailsRequestPacket) Packet;
            GroupAccountDetailsRequest handlerGroupAccountDetailsRequest = OnGroupAccountDetailsRequest;
            if (handlerGroupAccountDetailsRequest != null)
            {
                handlerGroupAccountDetailsRequest(this, GroupAccountDetailsRequest.AgentData.AgentID,
                                                  GroupAccountDetailsRequest.AgentData.GroupID,
                                                  GroupAccountDetailsRequest.MoneyData.RequestID,
                                                  GroupAccountDetailsRequest.AgentData.SessionID,
                                                  GroupAccountDetailsRequest.MoneyData.CurrentInterval,
                                                  GroupAccountDetailsRequest.MoneyData.IntervalDays);
                return true;
            }
            return false;
        }

        private bool HandleGroupAccountSummaryRequest(IClientAPI client, Packet Packet)
        {
            GroupAccountSummaryRequestPacket GroupAccountSummaryRequest =
                (GroupAccountSummaryRequestPacket) Packet;
            GroupAccountSummaryRequest handlerGroupAccountSummaryRequest = OnGroupAccountSummaryRequest;
            if (handlerGroupAccountSummaryRequest != null)
            {
                handlerGroupAccountSummaryRequest(this, GroupAccountSummaryRequest.AgentData.AgentID,
                                                  GroupAccountSummaryRequest.AgentData.GroupID,
                                                  GroupAccountSummaryRequest.MoneyData.RequestID,
                                                  GroupAccountSummaryRequest.MoneyData.CurrentInterval,
                                                  GroupAccountSummaryRequest.MoneyData.IntervalDays);
                return true;
            }
            return false;
        }

        private bool HandleGroupTransactionsDetailsRequest(IClientAPI client, Packet Packet)
        {
            GroupAccountTransactionsRequestPacket GroupAccountTransactionsRequest =
                (GroupAccountTransactionsRequestPacket) Packet;
            GroupAccountTransactionsRequest handlerGroupAccountTransactionsRequest = OnGroupAccountTransactionsRequest;
            if (handlerGroupAccountTransactionsRequest != null)
            {
                handlerGroupAccountTransactionsRequest(this, GroupAccountTransactionsRequest.AgentData.AgentID,
                                                       GroupAccountTransactionsRequest.AgentData.GroupID,
                                                       GroupAccountTransactionsRequest.MoneyData.RequestID,
                                                       GroupAccountTransactionsRequest.AgentData.SessionID,
                                                       GroupAccountTransactionsRequest.MoneyData.CurrentInterval,
                                                       GroupAccountTransactionsRequest.MoneyData.IntervalDays);
                return true;
            }
            return false;
        }

        private bool HandleGroupTitlesRequest(IClientAPI sender, Packet Pack)
        {
            GroupTitlesRequestPacket groupTitlesRequest =
                (GroupTitlesRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (groupTitlesRequest.AgentData.SessionID != SessionId ||
                    groupTitlesRequest.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (m_GroupsModule != null)
            {
                GroupTitlesReplyPacket groupTitlesReply =
                    (GroupTitlesReplyPacket) PacketPool.Instance.GetPacket(PacketType.GroupTitlesReply);

                groupTitlesReply.AgentData =
                    new GroupTitlesReplyPacket.AgentDataBlock
                        {
                            AgentID = AgentId,
                            GroupID = groupTitlesRequest.AgentData.GroupID,
                            RequestID = groupTitlesRequest.AgentData.RequestID
                        };


                List<GroupTitlesData> titles =
                    m_GroupsModule.GroupTitlesRequest(this,
                                                      groupTitlesRequest.AgentData.GroupID);

                groupTitlesReply.GroupData =
                    new GroupTitlesReplyPacket.GroupDataBlock[titles.Count];

                int i = 0;
                foreach (GroupTitlesData d in titles)
                {
                    groupTitlesReply.GroupData[i] =
                        new GroupTitlesReplyPacket.GroupDataBlock
                            {Title = Util.StringToBytes256(d.Name), RoleID = d.UUID, Selected = d.Selected};

                    i++;
                }

                OutPacket(groupTitlesReply, ThrottleOutPacketType.Asset);
            }
            return true;
        }

        private bool HandleGroupProfileRequest(IClientAPI sender, Packet Pack)
        {
            GroupProfileRequestPacket groupProfileRequest =
                (GroupProfileRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (groupProfileRequest.AgentData.SessionID != SessionId ||
                    groupProfileRequest.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (m_GroupsModule != null)
            {
                GroupProfileReplyPacket groupProfileReply =
                    (GroupProfileReplyPacket) PacketPool.Instance.GetPacket(PacketType.GroupProfileReply);

                groupProfileReply.AgentData = new GroupProfileReplyPacket.AgentDataBlock();
                groupProfileReply.GroupData = new GroupProfileReplyPacket.GroupDataBlock();
                groupProfileReply.AgentData.AgentID = AgentId;

                GroupProfileData d = m_GroupsModule.GroupProfileRequest(this,
                                                                        groupProfileRequest.GroupData.GroupID);

                groupProfileReply.GroupData.GroupID = d.GroupID;
                groupProfileReply.GroupData.Name = Util.StringToBytes256(d.Name);
                groupProfileReply.GroupData.Charter = Util.StringToBytes1024(d.Charter);
                groupProfileReply.GroupData.ShowInList = d.ShowInList;
                groupProfileReply.GroupData.MemberTitle = Util.StringToBytes256(d.MemberTitle);
                groupProfileReply.GroupData.PowersMask = d.PowersMask;
                groupProfileReply.GroupData.InsigniaID = d.InsigniaID;
                groupProfileReply.GroupData.FounderID = d.FounderID;
                groupProfileReply.GroupData.MembershipFee = d.MembershipFee;
                groupProfileReply.GroupData.OpenEnrollment = d.OpenEnrollment;
                groupProfileReply.GroupData.Money = d.Money;
                groupProfileReply.GroupData.GroupMembershipCount = d.GroupMembershipCount;
                groupProfileReply.GroupData.GroupRolesCount = d.GroupRolesCount;
                groupProfileReply.GroupData.AllowPublish = d.AllowPublish;
                groupProfileReply.GroupData.MaturePublish = d.MaturePublish;
                groupProfileReply.GroupData.OwnerRole = d.OwnerRole;

                OutPacket(groupProfileReply, ThrottleOutPacketType.Asset);
            }
            return true;
        }

        private bool HandleGroupMembersRequest(IClientAPI sender, Packet Pack)
        {
            GroupMembersRequestPacket groupMembersRequestPacket =
                (GroupMembersRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (groupMembersRequestPacket.AgentData.SessionID != SessionId ||
                    groupMembersRequestPacket.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (m_GroupsModule != null)
            {
                List<GroupMembersData> members =
                    m_GroupsModule.GroupMembersRequest(this, groupMembersRequestPacket.GroupData.GroupID);

                int memberCount = members.Count;

                while (true)
                {
                    int blockCount = members.Count;
                    if (blockCount > 40)
                        blockCount = 40;

                    GroupMembersReplyPacket groupMembersReply =
                        (GroupMembersReplyPacket) PacketPool.Instance.GetPacket(PacketType.GroupMembersReply);

                    groupMembersReply.AgentData =
                        new GroupMembersReplyPacket.AgentDataBlock();
                    groupMembersReply.GroupData =
                        new GroupMembersReplyPacket.GroupDataBlock();
                    groupMembersReply.MemberData =
                        new GroupMembersReplyPacket.MemberDataBlock[
                            blockCount];

                    groupMembersReply.AgentData.AgentID = AgentId;
                    groupMembersReply.GroupData.GroupID =
                        groupMembersRequestPacket.GroupData.GroupID;
                    groupMembersReply.GroupData.RequestID =
                        groupMembersRequestPacket.GroupData.RequestID;
                    groupMembersReply.GroupData.MemberCount = memberCount;

                    for (int i = 0; i < blockCount; i++)
                    {
                        GroupMembersData m = members[0];
                        members.RemoveAt(0);

                        groupMembersReply.MemberData[i] =
                            new GroupMembersReplyPacket.MemberDataBlock
                                {
                                    AgentID = m.AgentID,
                                    Contribution = m.Contribution,
                                    OnlineStatus = Util.StringToBytes256(m.OnlineStatus),
                                    AgentPowers = m.AgentPowers,
                                    Title = Util.StringToBytes256(m.Title),
                                    IsOwner = m.IsOwner
                                };
                    }
                    OutPacket(groupMembersReply, ThrottleOutPacketType.Asset);
                    if (members.Count == 0)
                        return true;
                }
            }
            return true;
        }

        private bool HandleGroupRoleDataRequest(IClientAPI sender, Packet Pack)
        {
            GroupRoleDataRequestPacket groupRolesRequest =
                (GroupRoleDataRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (groupRolesRequest.AgentData.SessionID != SessionId ||
                    groupRolesRequest.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (m_GroupsModule != null)
            {
                GroupRoleDataReplyPacket groupRolesReply =
                    (GroupRoleDataReplyPacket) PacketPool.Instance.GetPacket(PacketType.GroupRoleDataReply);

                groupRolesReply.AgentData =
                    new GroupRoleDataReplyPacket.AgentDataBlock {AgentID = AgentId};


                groupRolesReply.GroupData =
                    new GroupRoleDataReplyPacket.GroupDataBlock
                        {
                            GroupID = groupRolesRequest.GroupData.GroupID,
                            RequestID = groupRolesRequest.GroupData.RequestID
                        };


                List<GroupRolesData> titles =
                    m_GroupsModule.GroupRoleDataRequest(this,
                                                        groupRolesRequest.GroupData.GroupID);

                groupRolesReply.GroupData.RoleCount =
                    titles.Count;

                groupRolesReply.RoleData =
                    new GroupRoleDataReplyPacket.RoleDataBlock[titles.Count];

                int i = 0;
                foreach (GroupRolesData d in titles)
                {
                    groupRolesReply.RoleData[i] =
                        new GroupRoleDataReplyPacket.RoleDataBlock
                            {
                                RoleID = d.RoleID,
                                Name = Util.StringToBytes256(d.Name),
                                Title = Util.StringToBytes256(d.Title),
                                Description = Util.StringToBytes1024(d.Description),
                                Powers = d.Powers,
                                Members = (uint) d.Members
                            };


                    i++;
                }

                OutPacket(groupRolesReply, ThrottleOutPacketType.Asset);
            }
            return true;
        }

        private bool HandleGroupRoleMembersRequest(IClientAPI sender, Packet Pack)
        {
            GroupRoleMembersRequestPacket groupRoleMembersRequest =
                (GroupRoleMembersRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (groupRoleMembersRequest.AgentData.SessionID != SessionId ||
                    groupRoleMembersRequest.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (m_GroupsModule != null)
            {
                List<GroupRoleMembersData> mappings =
                    m_GroupsModule.GroupRoleMembersRequest(this,
                                                           groupRoleMembersRequest.GroupData.GroupID);

                int mappingsCount = mappings.Count;

                while (mappings.Count > 0)
                {
                    int pairs = mappings.Count;
                    if (pairs > 32)
                        pairs = 32;

                    GroupRoleMembersReplyPacket groupRoleMembersReply =
                        (GroupRoleMembersReplyPacket) PacketPool.Instance.GetPacket(PacketType.GroupRoleMembersReply);
                    groupRoleMembersReply.AgentData =
                        new GroupRoleMembersReplyPacket.AgentDataBlock
                            {
                                AgentID = AgentId,
                                GroupID = groupRoleMembersRequest.GroupData.GroupID,
                                RequestID = groupRoleMembersRequest.GroupData.RequestID,
                                TotalPairs = (uint) mappingsCount
                            };


                    groupRoleMembersReply.MemberData =
                        new GroupRoleMembersReplyPacket.MemberDataBlock[pairs];

                    for (int i = 0; i < pairs; i++)
                    {
                        GroupRoleMembersData d = mappings[0];
                        mappings.RemoveAt(0);

                        groupRoleMembersReply.MemberData[i] =
                            new GroupRoleMembersReplyPacket.MemberDataBlock {RoleID = d.RoleID, MemberID = d.MemberID};
                    }

                    OutPacket(groupRoleMembersReply, ThrottleOutPacketType.Asset);
                }
            }
            return true;
        }

        private bool HandleCreateGroupRequest(IClientAPI sender, Packet Pack)
        {
            CreateGroupRequestPacket createGroupRequest =
                (CreateGroupRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (createGroupRequest.AgentData.SessionID != SessionId ||
                    createGroupRequest.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (m_GroupsModule != null)
            {
                m_GroupsModule.CreateGroup(this,
                                           Utils.BytesToString(createGroupRequest.GroupData.Name),
                                           Utils.BytesToString(createGroupRequest.GroupData.Charter),
                                           createGroupRequest.GroupData.ShowInList,
                                           createGroupRequest.GroupData.InsigniaID,
                                           createGroupRequest.GroupData.MembershipFee,
                                           createGroupRequest.GroupData.OpenEnrollment,
                                           createGroupRequest.GroupData.AllowPublish,
                                           createGroupRequest.GroupData.MaturePublish);
            }
            return true;
        }

        private bool HandleUpdateGroupInfo(IClientAPI sender, Packet Pack)
        {
            UpdateGroupInfoPacket updateGroupInfo =
                (UpdateGroupInfoPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (updateGroupInfo.AgentData.SessionID != SessionId ||
                    updateGroupInfo.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (m_GroupsModule != null)
            {
                m_GroupsModule.UpdateGroupInfo(this,
                                               updateGroupInfo.GroupData.GroupID,
                                               Utils.BytesToString(updateGroupInfo.GroupData.Charter),
                                               updateGroupInfo.GroupData.ShowInList,
                                               updateGroupInfo.GroupData.InsigniaID,
                                               updateGroupInfo.GroupData.MembershipFee,
                                               updateGroupInfo.GroupData.OpenEnrollment,
                                               updateGroupInfo.GroupData.AllowPublish,
                                               updateGroupInfo.GroupData.MaturePublish);
            }

            return true;
        }

        private bool HandleSetGroupAcceptNotices(IClientAPI sender, Packet Pack)
        {
            SetGroupAcceptNoticesPacket setGroupAcceptNotices =
                (SetGroupAcceptNoticesPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (setGroupAcceptNotices.AgentData.SessionID != SessionId ||
                    setGroupAcceptNotices.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (m_GroupsModule != null)
            {
                m_GroupsModule.SetGroupAcceptNotices(this,
                                                     setGroupAcceptNotices.Data.GroupID,
                                                     setGroupAcceptNotices.Data.AcceptNotices,
                                                     setGroupAcceptNotices.NewData.ListInProfile);
            }

            return true;
        }

        private bool HandleGroupTitleUpdate(IClientAPI sender, Packet Pack)
        {
            GroupTitleUpdatePacket groupTitleUpdate =
                (GroupTitleUpdatePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (groupTitleUpdate.AgentData.SessionID != SessionId ||
                    groupTitleUpdate.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (m_GroupsModule != null)
            {
                m_GroupsModule.GroupTitleUpdate(this,
                                                groupTitleUpdate.AgentData.GroupID,
                                                groupTitleUpdate.AgentData.TitleRoleID);
            }

            return true;
        }

        private bool HandleParcelDeedToGroup(IClientAPI sender, Packet Pack)
        {
            ParcelDeedToGroupPacket parcelDeedToGroup = (ParcelDeedToGroupPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (parcelDeedToGroup.AgentData.SessionID != SessionId ||
                    parcelDeedToGroup.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (m_GroupsModule != null)
            {
                ParcelDeedToGroup handlerParcelDeedToGroup = OnParcelDeedToGroup;
                if (handlerParcelDeedToGroup != null)
                {
                    handlerParcelDeedToGroup(parcelDeedToGroup.Data.LocalID, parcelDeedToGroup.Data.GroupID, this);
                }
            }

            return true;
        }

        private bool HandleGroupNoticesListRequest(IClientAPI sender, Packet Pack)
        {
            GroupNoticesListRequestPacket groupNoticesListRequest =
                (GroupNoticesListRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (groupNoticesListRequest.AgentData.SessionID != SessionId ||
                    groupNoticesListRequest.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (m_GroupsModule != null)
            {
                GroupNoticeData[] gn =
                    m_GroupsModule.GroupNoticesListRequest(this,
                                                           groupNoticesListRequest.Data.GroupID);

                GroupNoticesListReplyPacket groupNoticesListReply =
                    (GroupNoticesListReplyPacket) PacketPool.Instance.GetPacket(PacketType.GroupNoticesListReply);
                groupNoticesListReply.AgentData =
                    new GroupNoticesListReplyPacket.AgentDataBlock
                        {AgentID = AgentId, GroupID = groupNoticesListRequest.Data.GroupID};

                groupNoticesListReply.Data = new GroupNoticesListReplyPacket.DataBlock[gn.Length];

                int i = 0;
                foreach (GroupNoticeData g in gn)
                {
                    groupNoticesListReply.Data[i] = new GroupNoticesListReplyPacket.DataBlock
                                                        {
                                                            NoticeID = g.NoticeID,
                                                            Timestamp = g.Timestamp,
                                                            FromName = Util.StringToBytes256(g.FromName),
                                                            Subject = Util.StringToBytes256(g.Subject),
                                                            HasAttachment = g.HasAttachment,
                                                            AssetType = g.AssetType
                                                        };
                    i++;
                }

                OutPacket(groupNoticesListReply, ThrottleOutPacketType.Asset);
            }

            return true;
        }

        private bool HandleGroupNoticeRequest(IClientAPI sender, Packet Pack)
        {
            GroupNoticeRequestPacket groupNoticeRequest =
                (GroupNoticeRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (groupNoticeRequest.AgentData.SessionID != SessionId ||
                    groupNoticeRequest.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (m_GroupsModule != null)
            {
                m_GroupsModule.GroupNoticeRequest(this,
                                                  groupNoticeRequest.Data.GroupNoticeID);
            }
            return true;
        }

        private bool HandleGroupRoleUpdate(IClientAPI sender, Packet Pack)
        {
            GroupRoleUpdatePacket groupRoleUpdate =
                (GroupRoleUpdatePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (groupRoleUpdate.AgentData.SessionID != SessionId ||
                    groupRoleUpdate.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (m_GroupsModule != null)
            {
                foreach (GroupRoleUpdatePacket.RoleDataBlock d in
                    groupRoleUpdate.RoleData)
                {
                    m_GroupsModule.GroupRoleUpdate(this,
                                                   groupRoleUpdate.AgentData.GroupID,
                                                   d.RoleID,
                                                   Utils.BytesToString(d.Name),
                                                   Utils.BytesToString(d.Description),
                                                   Utils.BytesToString(d.Title),
                                                   d.Powers,
                                                   d.UpdateType);
                }
                m_GroupsModule.NotifyChange(groupRoleUpdate.AgentData.GroupID);
            }
            return true;
        }

        private bool HandleGroupRoleChanges(IClientAPI sender, Packet Pack)
        {
            GroupRoleChangesPacket groupRoleChanges =
                (GroupRoleChangesPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (groupRoleChanges.AgentData.SessionID != SessionId ||
                    groupRoleChanges.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (m_GroupsModule != null)
            {
                foreach (GroupRoleChangesPacket.RoleChangeBlock d in
                    groupRoleChanges.RoleChange)
                {
                    m_GroupsModule.GroupRoleChanges(this,
                                                    groupRoleChanges.AgentData.GroupID,
                                                    d.RoleID,
                                                    d.MemberID,
                                                    d.Change);
                }
                m_GroupsModule.NotifyChange(groupRoleChanges.AgentData.GroupID);
            }
            return true;
        }

        private bool HandleJoinGroupRequest(IClientAPI sender, Packet Pack)
        {
            JoinGroupRequestPacket joinGroupRequest =
                (JoinGroupRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (joinGroupRequest.AgentData.SessionID != SessionId ||
                    joinGroupRequest.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (m_GroupsModule != null)
            {
                m_GroupsModule.JoinGroupRequest(this,
                                                joinGroupRequest.GroupData.GroupID);
            }
            return true;
        }

        private bool HandleLeaveGroupRequest(IClientAPI sender, Packet Pack)
        {
            LeaveGroupRequestPacket leaveGroupRequest =
                (LeaveGroupRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (leaveGroupRequest.AgentData.SessionID != SessionId ||
                    leaveGroupRequest.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (m_GroupsModule != null)
            {
                m_GroupsModule.LeaveGroupRequest(this,
                                                 leaveGroupRequest.GroupData.GroupID);
            }
            return true;
        }

        private bool HandleEjectGroupMemberRequest(IClientAPI sender, Packet Pack)
        {
            EjectGroupMemberRequestPacket ejectGroupMemberRequest =
                (EjectGroupMemberRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (ejectGroupMemberRequest.AgentData.SessionID != SessionId ||
                    ejectGroupMemberRequest.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (m_GroupsModule != null)
            {
                foreach (EjectGroupMemberRequestPacket.EjectDataBlock e
                    in ejectGroupMemberRequest.EjectData)
                {
                    m_GroupsModule.EjectGroupMemberRequest(this,
                                                           ejectGroupMemberRequest.GroupData.GroupID,
                                                           e.EjecteeID);
                }
            }
            return true;
        }

        private bool HandleInviteGroupRequest(IClientAPI sender, Packet Pack)
        {
            InviteGroupRequestPacket inviteGroupRequest =
                (InviteGroupRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (inviteGroupRequest.AgentData.SessionID != SessionId ||
                    inviteGroupRequest.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            if (m_GroupsModule != null)
            {
                foreach (InviteGroupRequestPacket.InviteDataBlock b in
                    inviteGroupRequest.InviteData)
                {
                    m_GroupsModule.InviteGroupRequest(this,
                                                      inviteGroupRequest.GroupData.GroupID,
                                                      b.InviteeID,
                                                      b.RoleID);
                }
            }
            return true;
        }

        #endregion Groups

        private bool HandleStartLure(IClientAPI sender, Packet Pack)
        {
            StartLurePacket startLureRequest = (StartLurePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (startLureRequest.AgentData.SessionID != SessionId ||
                    startLureRequest.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            StartLure handlerStartLure = OnStartLure;
            if (handlerStartLure != null)
                handlerStartLure(startLureRequest.Info.LureType,
                                 Utils.BytesToString(
                                     startLureRequest.Info.Message),
                                 startLureRequest.TargetData[0].TargetID,
                                 this);
            return true;
        }

        private bool HandleTeleportLureRequest(IClientAPI sender, Packet Pack)
        {
            TeleportLureRequestPacket teleportLureRequest =
                (TeleportLureRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (teleportLureRequest.Info.SessionID != SessionId ||
                    teleportLureRequest.Info.AgentID != AgentId)
                    return true;
            }

            #endregion

            TeleportLureRequest handlerTeleportLureRequest = OnTeleportLureRequest;
            if (handlerTeleportLureRequest != null)
                handlerTeleportLureRequest(
                    teleportLureRequest.Info.LureID,
                    teleportLureRequest.Info.TeleportFlags,
                    this);
            return true;
        }

        private bool HandleClassifiedInfoRequest(IClientAPI sender, Packet Pack)
        {
            ClassifiedInfoRequestPacket classifiedInfoRequest =
                (ClassifiedInfoRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (classifiedInfoRequest.AgentData.SessionID != SessionId ||
                    classifiedInfoRequest.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ClassifiedInfoRequest handlerClassifiedInfoRequest = OnClassifiedInfoRequest;
            if (handlerClassifiedInfoRequest != null)
                handlerClassifiedInfoRequest(
                    classifiedInfoRequest.Data.ClassifiedID,
                    this);
            return true;
        }

        private bool HandleClassifiedInfoUpdate(IClientAPI sender, Packet Pack)
        {
            ClassifiedInfoUpdatePacket classifiedInfoUpdate =
                (ClassifiedInfoUpdatePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (classifiedInfoUpdate.AgentData.SessionID != SessionId ||
                    classifiedInfoUpdate.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ClassifiedInfoUpdate handlerClassifiedInfoUpdate = OnClassifiedInfoUpdate;
            if (handlerClassifiedInfoUpdate != null)
                handlerClassifiedInfoUpdate(
                    classifiedInfoUpdate.Data.ClassifiedID,
                    classifiedInfoUpdate.Data.Category,
                    Utils.BytesToString(
                        classifiedInfoUpdate.Data.Name),
                    Utils.BytesToString(
                        classifiedInfoUpdate.Data.Desc),
                    classifiedInfoUpdate.Data.ParcelID,
                    classifiedInfoUpdate.Data.ParentEstate,
                    classifiedInfoUpdate.Data.SnapshotID,
                    new Vector3(
                        classifiedInfoUpdate.Data.PosGlobal),
                    classifiedInfoUpdate.Data.ClassifiedFlags,
                    classifiedInfoUpdate.Data.PriceForListing,
                    this);
            return true;
        }

        private bool HandleClassifiedDelete(IClientAPI sender, Packet Pack)
        {
            ClassifiedDeletePacket classifiedDelete =
                (ClassifiedDeletePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (classifiedDelete.AgentData.SessionID != SessionId ||
                    classifiedDelete.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ClassifiedDelete handlerClassifiedDelete = OnClassifiedDelete;
            if (handlerClassifiedDelete != null)
                handlerClassifiedDelete(
                    classifiedDelete.Data.ClassifiedID,
                    this);
            return true;
        }

        private bool HandleClassifiedGodDelete(IClientAPI sender, Packet Pack)
        {
            ClassifiedGodDeletePacket classifiedGodDelete =
                (ClassifiedGodDeletePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (classifiedGodDelete.AgentData.SessionID != SessionId ||
                    classifiedGodDelete.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            ClassifiedDelete handlerClassifiedGodDelete = OnClassifiedGodDelete;
            if (handlerClassifiedGodDelete != null)
                handlerClassifiedGodDelete(
                    classifiedGodDelete.Data.ClassifiedID,
                    this);
            return true;
        }

        private bool HandleEventGodDelete(IClientAPI sender, Packet Pack)
        {
            EventGodDeletePacket eventGodDelete =
                (EventGodDeletePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (eventGodDelete.AgentData.SessionID != SessionId ||
                    eventGodDelete.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            EventGodDelete handlerEventGodDelete = OnEventGodDelete;
            if (handlerEventGodDelete != null)
                handlerEventGodDelete(
                    eventGodDelete.EventData.EventID,
                    eventGodDelete.QueryData.QueryID,
                    Utils.BytesToString(
                        eventGodDelete.QueryData.QueryText),
                    eventGodDelete.QueryData.QueryFlags,
                    eventGodDelete.QueryData.QueryStart,
                    this);
            return true;
        }

        private bool HandleEventNotificationAddRequest(IClientAPI sender, Packet Pack)
        {
            EventNotificationAddRequestPacket eventNotificationAdd =
                (EventNotificationAddRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (eventNotificationAdd.AgentData.SessionID != SessionId ||
                    eventNotificationAdd.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            EventNotificationAddRequest handlerEventNotificationAddRequest = OnEventNotificationAddRequest;
            if (handlerEventNotificationAddRequest != null)
                handlerEventNotificationAddRequest(
                    eventNotificationAdd.EventData.EventID, this);
            return true;
        }

        private bool HandleEventNotificationRemoveRequest(IClientAPI sender, Packet Pack)
        {
            EventNotificationRemoveRequestPacket eventNotificationRemove =
                (EventNotificationRemoveRequestPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (eventNotificationRemove.AgentData.SessionID != SessionId ||
                    eventNotificationRemove.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            EventNotificationRemoveRequest handlerEventNotificationRemoveRequest = OnEventNotificationRemoveRequest;
            if (handlerEventNotificationRemoveRequest != null)
                handlerEventNotificationRemoveRequest(
                    eventNotificationRemove.EventData.EventID, this);
            return true;
        }

        private bool HandleRetrieveInstantMessages(IClientAPI sender, Packet Pack)
        {
            RetrieveInstantMessagesPacket rimpInstantMessagePack = (RetrieveInstantMessagesPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (rimpInstantMessagePack.AgentData.SessionID != SessionId ||
                    rimpInstantMessagePack.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            RetrieveInstantMessages handlerRetrieveInstantMessages = OnRetrieveInstantMessages;
            if (handlerRetrieveInstantMessages != null)
                handlerRetrieveInstantMessages(this);
            return true;
        }

        private bool HandlePickDelete(IClientAPI sender, Packet Pack)
        {
            PickDeletePacket pickDelete =
                (PickDeletePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (pickDelete.AgentData.SessionID != SessionId ||
                    pickDelete.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            PickDelete handlerPickDelete = OnPickDelete;
            if (handlerPickDelete != null)
                handlerPickDelete(this, pickDelete.Data.PickID);
            return true;
        }

        private bool HandlePickGodDelete(IClientAPI sender, Packet Pack)
        {
            PickGodDeletePacket pickGodDelete =
                (PickGodDeletePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (pickGodDelete.AgentData.SessionID != SessionId ||
                    pickGodDelete.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            PickGodDelete handlerPickGodDelete = OnPickGodDelete;
            if (handlerPickGodDelete != null)
                handlerPickGodDelete(this,
                                     pickGodDelete.AgentData.AgentID,
                                     pickGodDelete.Data.PickID,
                                     pickGodDelete.Data.QueryID);
            return true;
        }

        private bool HandlePickInfoUpdate(IClientAPI sender, Packet Pack)
        {
            PickInfoUpdatePacket pickInfoUpdate =
                (PickInfoUpdatePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (pickInfoUpdate.AgentData.SessionID != SessionId ||
                    pickInfoUpdate.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            PickInfoUpdate handlerPickInfoUpdate = OnPickInfoUpdate;
            if (handlerPickInfoUpdate != null)
                handlerPickInfoUpdate(this,
                                      pickInfoUpdate.Data.PickID,
                                      pickInfoUpdate.Data.CreatorID,
                                      pickInfoUpdate.Data.TopPick,
                                      Utils.BytesToString(pickInfoUpdate.Data.Name),
                                      Utils.BytesToString(pickInfoUpdate.Data.Desc),
                                      pickInfoUpdate.Data.SnapshotID,
                                      pickInfoUpdate.Data.SortOrder,
                                      pickInfoUpdate.Data.Enabled,
                                      pickInfoUpdate.Data.PosGlobal);
            return true;
        }

        private bool HandleAvatarNotesUpdate(IClientAPI sender, Packet Pack)
        {
            AvatarNotesUpdatePacket avatarNotesUpdate =
                (AvatarNotesUpdatePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (avatarNotesUpdate.AgentData.SessionID != SessionId ||
                    avatarNotesUpdate.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            AvatarNotesUpdate handlerAvatarNotesUpdate = OnAvatarNotesUpdate;
            if (handlerAvatarNotesUpdate != null)
                handlerAvatarNotesUpdate(this,
                                         avatarNotesUpdate.Data.TargetID,
                                         Utils.BytesToString(avatarNotesUpdate.Data.Notes));
            return true;
        }

        private bool HandleAvatarInterestsUpdate(IClientAPI sender, Packet Pack)
        {
            AvatarInterestsUpdatePacket avatarInterestUpdate =
                (AvatarInterestsUpdatePacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (avatarInterestUpdate.AgentData.SessionID != SessionId ||
                    avatarInterestUpdate.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            AvatarInterestUpdate handlerAvatarInterestUpdate = OnAvatarInterestUpdate;
            if (handlerAvatarInterestUpdate != null)
                handlerAvatarInterestUpdate(this,
                                            avatarInterestUpdate.PropertiesData.WantToMask,
                                            Utils.BytesToString(avatarInterestUpdate.PropertiesData.WantToText),
                                            avatarInterestUpdate.PropertiesData.SkillsMask,
                                            Utils.BytesToString(avatarInterestUpdate.PropertiesData.SkillsText),
                                            Utils.BytesToString(avatarInterestUpdate.PropertiesData.LanguagesText));
            return true;
        }

        private bool HandleGrantUserRights(IClientAPI sender, Packet Pack)
        {
            GrantUserRightsPacket GrantUserRights =
                (GrantUserRightsPacket) Pack;

            #region Packet Session and User Check

            if (m_checkPackets)
            {
                if (GrantUserRights.AgentData.SessionID != SessionId ||
                    GrantUserRights.AgentData.AgentID != AgentId)
                    return true;
            }

            #endregion

            GrantUserFriendRights GrantUserRightsHandler = OnGrantUserRights;
            if (GrantUserRightsHandler != null)
                GrantUserRightsHandler(this,
                                       GrantUserRights.AgentData.AgentID,
                                       GrantUserRights.Rights[0].AgentRelated,
                                       GrantUserRights.Rights[0].RelatedRights);
            return true;
        }

        private bool HandlePlacesQuery(IClientAPI sender, Packet Pack)
        {
            PlacesQueryPacket placesQueryPacket =
                (PlacesQueryPacket) Pack;

            PlacesQuery handlerPlacesQuery = OnPlacesQuery;

            if (handlerPlacesQuery != null)
                handlerPlacesQuery(placesQueryPacket.AgentData.QueryID,
                                   placesQueryPacket.TransactionData.TransactionID,
                                   Utils.BytesToString(
                                       placesQueryPacket.QueryData.QueryText),
                                   placesQueryPacket.QueryData.QueryFlags,
                                   (byte) placesQueryPacket.QueryData.Category,
                                   Utils.BytesToString(
                                       placesQueryPacket.QueryData.SimName),
                                   this);
            return true;
        }

        #endregion Packet Handlers

        public void SendScriptQuestion(UUID taskID, string taskName, string ownerName, UUID itemID, int question)
        {
            ScriptQuestionPacket scriptQuestion =
                (ScriptQuestionPacket) PacketPool.Instance.GetPacket(PacketType.ScriptQuestion);
            scriptQuestion.Data = new ScriptQuestionPacket.DataBlock
                                      {
                                          TaskID = taskID,
                                          ItemID = itemID,
                                          Questions = question,
                                          ObjectName = Util.StringToBytes256(taskName),
                                          ObjectOwner = Util.StringToBytes256(ownerName)
                                      };
            // TODO: don't create new blocks if recycling an old packet

            OutPacket(scriptQuestion, ThrottleOutPacketType.AvatarInfo);
        }

        private void InitDefaultAnimations()
        {
            using (XmlTextReader reader = new XmlTextReader("data/avataranimations.xml"))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(reader);
                if (doc.DocumentElement != null)
                    foreach (XmlNode nod in doc.DocumentElement.ChildNodes)
                    {
                        if (nod.Attributes != null && nod.Attributes["name"] != null)
                        {
                            string name = nod.Attributes["name"].Value.ToLower();
                            string id = nod.InnerText;
                            m_defaultAnimations.Add(name, (UUID) id);
                        }
                    }
            }
        }

        public UUID GetDefaultAnimation(string name)
        {
            if (m_defaultAnimations.ContainsKey(name))
                return m_defaultAnimations[name];
            return UUID.Zero;
        }

        /// <summary>
        ///   Handler called when we receive a logout packet.
        /// </summary>
        /// <param name = "client"></param>
        /// <param name = "packet"></param>
        /// <returns></returns>
        private bool HandleLogout(IClientAPI client, Packet packet)
        {
            if (packet.Type == PacketType.LogoutRequest)
            {
                if (((LogoutRequestPacket) packet).AgentData.SessionID != SessionId) return false;
            }

            return Logout(client);
        }

        ///<summary>
        ///</summary>
        ///<param name = "client"></param>
        ///<returns></returns>
        private bool Logout(IClientAPI client)
        {
            //MainConsole.Instance.InfoFormat("[CLIENT]: Got a logout request for {0} in {1}", Name, Scene.RegionInfo.RegionName);

            Action<IClientAPI> handlerLogout = OnLogout;

            if (handlerLogout != null)
            {
                handlerLogout(client);
            }

            return true;
        }

        private bool HandleMultipleObjUpdate(IClientAPI simClient, Packet packet)
        {
            MultipleObjectUpdatePacket multipleupdate = (MultipleObjectUpdatePacket) packet;
            if (multipleupdate.AgentData.SessionID != SessionId) return false;
            // MainConsole.Instance.Debug("new multi update packet " + multipleupdate.ToString());
            IScene tScene = m_scene;

            foreach (MultipleObjectUpdatePacket.ObjectDataBlock block in multipleupdate.ObjectData)
            {
                // Can't act on Null Data
                if (block.Data != null)
                {
                    uint localId = block.ObjectLocalID;
                    ISceneChildEntity part = tScene.GetSceneObjectPart(localId);

                    if (part == null)
                    {
                        // It's a ghost! tell the client to delete it from view.
                        simClient.SendKillObject(Scene.RegionInfo.RegionHandle,
                                                 new IEntity[] {null});
                    }
                    else
                    {
                        // UUID partId = part.UUID;

                        switch (block.Type)
                        {
                            case 1:
                                Vector3 pos1 = new Vector3(block.Data, 0);

                                UpdateVectorWithUpdate handlerUpdatePrimSinglePosition = OnUpdatePrimSinglePosition;
                                if (handlerUpdatePrimSinglePosition != null)
                                {
                                    // MainConsole.Instance.Debug("new movement position is " + pos.X + " , " + pos.Y + " , " + pos.Z);
                                    handlerUpdatePrimSinglePosition(localId, pos1, this, true);
                                }
                                break;
                            case 2:
                                Quaternion rot1 = new Quaternion(block.Data, 0, true);

                                UpdatePrimSingleRotation handlerUpdatePrimSingleRotation = OnUpdatePrimSingleRotation;
                                if (handlerUpdatePrimSingleRotation != null)
                                {
                                    // MainConsole.Instance.Info("new tab rotation is " + rot1.X + " , " + rot1.Y + " , " + rot1.Z + " , " + rot1.W);
                                    handlerUpdatePrimSingleRotation(localId, rot1, this);
                                }
                                break;
                            case 3:
                                Vector3 rotPos = new Vector3(block.Data, 0);
                                Quaternion rot2 = new Quaternion(block.Data, 12, true);

                                UpdatePrimSingleRotationPosition handlerUpdatePrimSingleRotationPosition =
                                    OnUpdatePrimSingleRotationPosition;
                                if (handlerUpdatePrimSingleRotationPosition != null)
                                {
                                    // MainConsole.Instance.Debug("new mouse rotation position is " + rotPos.X + " , " + rotPos.Y + " , " + rotPos.Z);
                                    // MainConsole.Instance.Info("new mouse rotation is " + rot2.X + " , " + rot2.Y + " , " + rot2.Z + " , " + rot2.W);
                                    handlerUpdatePrimSingleRotationPosition(localId, rot2, rotPos, this);
                                }
                                break;
                            case 4:
                            case 20:
                                Vector3 scale4 = new Vector3(block.Data, 0);

                                UpdateVector handlerUpdatePrimScale = OnUpdatePrimScale;
                                if (handlerUpdatePrimScale != null)
                                {
                                    //                                     MainConsole.Instance.Debug("new scale is " + scale4.X + " , " + scale4.Y + " , " + scale4.Z);
                                    handlerUpdatePrimScale(localId, scale4, this);
                                }
                                break;
                            case 5:

                                Vector3 scale1 = new Vector3(block.Data, 12);
                                Vector3 pos11 = new Vector3(block.Data, 0);

                                handlerUpdatePrimScale = OnUpdatePrimScale;
                                if (handlerUpdatePrimScale != null)
                                {
                                    // MainConsole.Instance.Debug("new scale is " + scale.X + " , " + scale.Y + " , " + scale.Z);
                                    handlerUpdatePrimScale(localId, scale1, this);

                                    handlerUpdatePrimSinglePosition = OnUpdatePrimSinglePosition;
                                    if (handlerUpdatePrimSinglePosition != null)
                                    {
                                        handlerUpdatePrimSinglePosition(localId, pos11, this, false);
                                    }
                                }
                                break;
                            case 9:
                                Vector3 pos2 = new Vector3(block.Data, 0);

                                UpdateVectorWithUpdate handlerUpdateVector = OnUpdatePrimGroupPosition;

                                if (handlerUpdateVector != null)
                                {
                                    handlerUpdateVector(localId, pos2, this, true);
                                }
                                break;
                            case 10:
                                Quaternion rot3 = new Quaternion(block.Data, 0, true);

                                UpdatePrimRotation handlerUpdatePrimRotation = OnUpdatePrimGroupRotation;
                                if (handlerUpdatePrimRotation != null)
                                {
                                    //  Console.WriteLine("new rotation is " + rot3.X + " , " + rot3.Y + " , " + rot3.Z + " , " + rot3.W);
                                    handlerUpdatePrimRotation(localId, rot3, this);
                                }
                                break;
                            case 11:
                                Vector3 pos3 = new Vector3(block.Data, 0);
                                Quaternion rot4 = new Quaternion(block.Data, 12, true);

                                UpdatePrimGroupRotation handlerUpdatePrimGroupRotation = OnUpdatePrimGroupMouseRotation;
                                if (handlerUpdatePrimGroupRotation != null)
                                {
                                    //  MainConsole.Instance.Debug("new rotation position is " + pos.X + " , " + pos.Y + " , " + pos.Z);
                                    // MainConsole.Instance.Debug("new group mouse rotation is " + rot4.X + " , " + rot4.Y + " , " + rot4.Z + " , " + rot4.W);
                                    handlerUpdatePrimGroupRotation(localId, pos3, rot4, this);
                                }
                                break;
                            case 12:
                            case 28:
                                Vector3 scale7 = new Vector3(block.Data, 0);

                                UpdateVector handlerUpdatePrimGroupScale = OnUpdatePrimGroupScale;
                                if (handlerUpdatePrimGroupScale != null)
                                {
                                    //                                     MainConsole.Instance.Debug("new scale is " + scale7.X + " , " + scale7.Y + " , " + scale7.Z);
                                    handlerUpdatePrimGroupScale(localId, scale7, this);
                                }
                                break;
                            case 13:
                                Vector3 scale2 = new Vector3(block.Data, 12);
                                Vector3 pos4 = new Vector3(block.Data, 0);

                                handlerUpdatePrimScale = OnUpdatePrimScale;
                                if (handlerUpdatePrimScale != null)
                                {
                                    //MainConsole.Instance.Debug("new scale is " + scale.X + " , " + scale.Y + " , " + scale.Z);
                                    handlerUpdatePrimScale(localId, scale2, this);

                                    // Change the position based on scale (for bug number 246)
                                    handlerUpdatePrimSinglePosition = OnUpdatePrimSinglePosition;
                                    // MainConsole.Instance.Debug("new movement position is " + pos.X + " , " + pos.Y + " , " + pos.Z);
                                    if (handlerUpdatePrimSinglePosition != null)
                                    {
                                        handlerUpdatePrimSinglePosition(localId, pos4, this, false);
                                    }
                                }
                                break;
                            case 29:
                                Vector3 scale5 = new Vector3(block.Data, 12);
                                Vector3 pos5 = new Vector3(block.Data, 0);

                                handlerUpdatePrimGroupScale = OnUpdatePrimGroupScale;
                                if (handlerUpdatePrimGroupScale != null)
                                {
                                    // MainConsole.Instance.Debug("new scale is " + scale.X + " , " + scale.Y + " , " + scale.Z);
                                    handlerUpdatePrimGroupScale(localId, scale5, this);
                                    handlerUpdateVector = OnUpdatePrimGroupPosition;

                                    if (handlerUpdateVector != null)
                                    {
                                        handlerUpdateVector(localId, pos5, this, false);
                                    }
                                }
                                break;
                            case 21:
                                Vector3 scale6 = new Vector3(block.Data, 12);
                                Vector3 pos6 = new Vector3(block.Data, 0);

                                handlerUpdatePrimScale = OnUpdatePrimScale;
                                if (handlerUpdatePrimScale != null)
                                {
                                    // MainConsole.Instance.Debug("new scale is " + scale.X + " , " + scale.Y + " , " + scale.Z);
                                    handlerUpdatePrimScale(localId, scale6, this);
                                    handlerUpdatePrimSinglePosition = OnUpdatePrimSinglePosition;
                                    if (handlerUpdatePrimSinglePosition != null)
                                    {
                                        handlerUpdatePrimSinglePosition(localId, pos6, this, false);
                                    }
                                }
                                break;
                            default:
                                MainConsole.Instance.Debug("[CLIENT] MultipleObjUpdate recieved an unknown packet type: " +
                                            (block.Type));
                                break;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        ///   Sets the throttles from values supplied by the client
        /// </summary>
        /// <param name = "throttles"></param>
        public void SetChildAgentThrottle(byte[] throttles)
        {
            m_udpClient.SetThrottles(throttles);
        }

        /// <summary>
        ///   Get the current throttles for this client as a packed byte array
        /// </summary>
        /// <param name = "multiplier">Unused</param>
        /// <returns></returns>
        public byte[] GetThrottlesPacked(float multiplier)
        {
            return m_udpClient.GetThrottlesPacked(multiplier);
        }

        /// <summary>
        ///   This is the starting point for sending a simulator packet out to the client
        /// </summary>
        /// <param name = "packet">Packet to send</param>
        /// <param name = "throttlePacketType">Throttling category for the packet</param>
        private void OutPacket(Packet packet, ThrottleOutPacketType throttlePacketType)
        {
            #region BinaryStats

            LLUDPServer.LogPacketHeader(false, m_circuitCode, 0, packet.Type, (ushort) packet.Length);

            #endregion BinaryStats

            OutPacket(packet, throttlePacketType, true);
        }

        /// <summary>
        ///   This is the starting point for sending a simulator packet out to the client
        /// </summary>
        /// <param name = "packet">Packet to send</param>
        /// <param name = "throttlePacketType">Throttling category for the packet</param>
        /// <param name = "doAutomaticSplitting">True to automatically split oversized
        ///   packets (the default), or false to disable splitting if the calling code
        ///   handles splitting manually</param>
        private void OutPacket(Packet packet, ThrottleOutPacketType throttlePacketType, bool doAutomaticSplitting)
        {
            OutPacket(packet, throttlePacketType, doAutomaticSplitting, null);
        }

        /// <summary>
        ///   This is the starting point for sending a simulator packet out to the client
        /// </summary>
        /// <param name = "packet">Packet to send</param>
        /// <param name = "throttlePacketType">Throttling category for the packet</param>
        /// <param name = "doAutomaticSplitting">True to automatically split oversized
        ///   packets (the default), or false to disable splitting if the calling code
        ///   handles splitting manually</param>
        /// <param name = "resendMethod">Method that will be called if the packet needs resent</param>
        private void OutPacket(Packet packet, ThrottleOutPacketType throttlePacketType, bool doAutomaticSplitting,
                               UnackedPacketMethod resendMethod)
        {
            OutPacket(packet, throttlePacketType, doAutomaticSplitting, resendMethod, null);
        }

        /// <summary>
        ///   This is the starting point for sending a simulator packet out to the client
        /// </summary>
        /// <param name = "packet">Packet to send</param>
        /// <param name = "throttlePacketType">Throttling category for the packet</param>
        /// <param name = "doAutomaticSplitting">True to automatically split oversized
        ///   packets (the default), or false to disable splitting if the calling code
        ///   handles splitting manually</param>
        /// <param name = "resendMethod">Method that will be called if the packet needs resent</param>
        /// <param name = "finishedMethod">Method that will be called when the packet is sent</param>
        private void OutPacket(Packet packet, ThrottleOutPacketType throttlePacketType, bool doAutomaticSplitting,
                               UnackedPacketMethod resendMethod, UnackedPacketMethod finishedMethod)
        {
            if (m_debugPacketLevel > 0)
            {
                bool outputPacket = true;

                if (m_debugPacketLevel <= 255
                    && (packet.Type == PacketType.SimStats || packet.Type == PacketType.SimulatorViewerTimeMessage))
                    outputPacket = false;

                if (m_debugPacketLevel <= 200
                    && (packet.Type == PacketType.ImagePacket
                        || packet.Type == PacketType.ImageData
                        || packet.Type == PacketType.LayerData
                        || packet.Type == PacketType.CoarseLocationUpdate))
                    outputPacket = false;

                if (m_debugPacketLevel <= 100 &&
                    (packet.Type == PacketType.AvatarAnimation || packet.Type == PacketType.ViewerEffect))
                    outputPacket = false;

                if (outputPacket)
                    MainConsole.Instance.DebugFormat("[CLIENT]: Packet OUT {0}", packet.Type);
            }

            m_udpServer.SendPacket(m_udpClient, packet, throttlePacketType, doAutomaticSplitting, resendMethod,
                                   finishedMethod);
        }

        /// <summary>
        ///   Breaks down the genericMessagePacket into specific events
        /// </summary>
        /// <param name = "gmMethod"></param>
        /// <param name = "gmInvoice"></param>
        /// <param name = "gmParams"></param>
        public void DecipherGenericMessage(string gmMethod, UUID gmInvoice,
                                           GenericMessagePacket.ParamListBlock[] gmParams)
        {
            switch (gmMethod)
            {
                case "autopilot":
                    float locx;
                    float locy;
                    float locz;

                    try
                    {
                        uint regionX;
                        uint regionY;
                        Utils.LongToUInts(Scene.RegionInfo.RegionHandle, out regionX, out regionY);
                        locx = Convert.ToSingle(Utils.BytesToString(gmParams[0].Parameter)) - regionX;
                        locy = Convert.ToSingle(Utils.BytesToString(gmParams[1].Parameter)) - regionY;
                        locz = Convert.ToSingle(Utils.BytesToString(gmParams[2].Parameter));
                    }
                    catch (InvalidCastException)
                    {
                        MainConsole.Instance.Error("[CLIENT]: Invalid autopilot request");
                        return;
                    }

                    UpdateVector handlerAutoPilotGo = OnAutoPilotGo;
                    if (handlerAutoPilotGo != null)
                    {
                        handlerAutoPilotGo(0, new Vector3(locx, locy, locz), this);
                    }
                    MainConsole.Instance.InfoFormat("[CLIENT]: Client Requests autopilot to position <{0},{1},{2}>", locx, locy, locz);


                    break;
                default:
                    MainConsole.Instance.Debug("[CLIENT]: Unknown Generic Message, Method: " + gmMethod + ". Invoice: " + gmInvoice +
                                ".  Dumping Params:");
                    foreach (GenericMessagePacket.ParamListBlock t in gmParams)
                    {
                        Console.WriteLine(t.ToString());
                    }
                    //gmpack.MethodData.
                    break;
            }
        }

        /// <summary>
        ///   Entryway from the client to the simulator.  All UDP packets from the client will end up here
        /// </summary>
        /// <param name = "packet">OpenMetaverse.packet</param>
        public void ProcessInPacket(Packet packet)
        {
            if (m_debugPacketLevel > 0)
            {
                bool outputPacket = true;

                if (m_debugPacketLevel <= 255 && packet.Type == PacketType.AgentUpdate)
                    outputPacket = false;

                if (m_debugPacketLevel <= 200 && packet.Type == PacketType.RequestImage)
                    outputPacket = false;

                if (m_debugPacketLevel <= 100 &&
                    (packet.Type == PacketType.ViewerEffect || packet.Type == PacketType.AgentAnimation))
                    outputPacket = false;

                if (outputPacket)
                    MainConsole.Instance.DebugFormat("[CLIENT]: Packet IN {0}", packet.Type);
            }

            if (!ProcessPacketMethod(packet))
                MainConsole.Instance.Warn("[CLIENT]: unhandled packet " + packet.Type);

            //Give the packet back to the pool now, we've processed it
            PacketPool.Instance.ReturnPacket(packet);
        }

        private static PrimitiveBaseShape GetShapeFromAddPacket(ObjectAddPacket addPacket)
        {
            PrimitiveBaseShape shape = new PrimitiveBaseShape
                                           {
                                               PCode = addPacket.ObjectData.PCode,
                                               State = addPacket.ObjectData.State,
                                               PathBegin = addPacket.ObjectData.PathBegin,
                                               PathEnd = addPacket.ObjectData.PathEnd,
                                               PathScaleX = addPacket.ObjectData.PathScaleX,
                                               PathScaleY = addPacket.ObjectData.PathScaleY,
                                               PathShearX = addPacket.ObjectData.PathShearX,
                                               PathShearY = addPacket.ObjectData.PathShearY,
                                               PathSkew = addPacket.ObjectData.PathSkew,
                                               ProfileBegin = addPacket.ObjectData.ProfileBegin,
                                               ProfileEnd = addPacket.ObjectData.ProfileEnd,
                                               Scale = addPacket.ObjectData.Scale,
                                               PathCurve = addPacket.ObjectData.PathCurve,
                                               ProfileCurve = addPacket.ObjectData.ProfileCurve,
                                               ProfileHollow = addPacket.ObjectData.ProfileHollow,
                                               PathRadiusOffset = addPacket.ObjectData.PathRadiusOffset,
                                               PathRevolutions = addPacket.ObjectData.PathRevolutions,
                                               PathTaperX = addPacket.ObjectData.PathTaperX,
                                               PathTaperY = addPacket.ObjectData.PathTaperY,
                                               PathTwist = addPacket.ObjectData.PathTwist,
                                               PathTwistBegin = addPacket.ObjectData.PathTwistBegin
                                           };

            Primitive.TextureEntry ntex = new Primitive.TextureEntry(new UUID("89556747-24cb-43ed-920b-47caed15465f"));
            shape.TextureEntry = ntex.GetBytes();
            //shape.Textures = ntex;
            return shape;
        }

        public EndPoint GetClientEP()
        {
            return m_userEndPoint;
        }

        #region Media Parcel Members

        public void SendParcelMediaCommand(uint flags, ParcelMediaCommandEnum command, float time)
        {
            ParcelMediaCommandMessagePacket commandMessagePacket = new ParcelMediaCommandMessagePacket
                                                                       {
                                                                           CommandBlock =
                                                                               {
                                                                                   Flags = flags,
                                                                                   Command = (uint) command,
                                                                                   Time = time
                                                                               }
                                                                       };

            OutPacket(commandMessagePacket, ThrottleOutPacketType.Land);
        }

        public void SendParcelMediaUpdate(string mediaUrl, UUID mediaTextureID,
                                          byte autoScale, string mediaType, string mediaDesc, int mediaWidth,
                                          int mediaHeight,
                                          byte mediaLoop)
        {
            ParcelMediaUpdatePacket updatePacket = new ParcelMediaUpdatePacket
                                                       {
                                                           DataBlock =
                                                               {
                                                                   MediaURL = Util.StringToBytes256(mediaUrl),
                                                                   MediaID = mediaTextureID,
                                                                   MediaAutoScale = autoScale
                                                               },
                                                           DataBlockExtended =
                                                               {
                                                                   MediaType = Util.StringToBytes256(mediaType),
                                                                   MediaDesc = Util.StringToBytes256(mediaDesc),
                                                                   MediaWidth = mediaWidth,
                                                                   MediaHeight = mediaHeight,
                                                                   MediaLoop = mediaLoop
                                                               }
                                                       };


            OutPacket(updatePacket, ThrottleOutPacketType.Land);
        }

        #endregion

        #region Camera

        public void SendSetFollowCamProperties(UUID objectID, SortedDictionary<int, float> parameters)
        {
            SetFollowCamPropertiesPacket packet =
                (SetFollowCamPropertiesPacket) PacketPool.Instance.GetPacket(PacketType.SetFollowCamProperties);
            packet.ObjectData.ObjectID = objectID;
            SetFollowCamPropertiesPacket.CameraPropertyBlock[] camPropBlock =
                new SetFollowCamPropertiesPacket.CameraPropertyBlock[parameters.Count];
            uint idx = 0;
#if (!ISWIN)
            foreach (KeyValuePair<int, float> pair in parameters)
            {
                SetFollowCamPropertiesPacket.CameraPropertyBlock block = new SetFollowCamPropertiesPacket.CameraPropertyBlock {Type = pair.Key, Value = pair.Value};
                camPropBlock[idx++] = block;
            }
#else
            foreach (SetFollowCamPropertiesPacket.CameraPropertyBlock block in parameters.Select(pair => new SetFollowCamPropertiesPacket.CameraPropertyBlock {Type = pair.Key, Value = pair.Value}))
            {
                camPropBlock[idx++] = block;
            }
#endif
            packet.CameraProperty = camPropBlock;
            OutPacket(packet, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendClearFollowCamProperties(UUID objectID)
        {
            ClearFollowCamPropertiesPacket packet =
                (ClearFollowCamPropertiesPacket) PacketPool.Instance.GetPacket(PacketType.ClearFollowCamProperties);
            packet.ObjectData.ObjectID = objectID;
            OutPacket(packet, ThrottleOutPacketType.AvatarInfo);
        }

        #endregion

        #region IClientCore

        private readonly Dictionary<Type, object> m_clientInterfaces = new Dictionary<Type, object>();

        /// <summary>
        ///   Register an interface on this client, should only be called in the constructor.
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "iface"></param>
        private void RegisterInterface<T>(T iface)
        {
            lock (m_clientInterfaces)
            {
                if (!m_clientInterfaces.ContainsKey(typeof (T)))
                {
                    m_clientInterfaces.Add(typeof (T), iface);
                }
            }
        }

        public bool TryGet<T>(out T iface)
        {
            if (m_clientInterfaces.ContainsKey(typeof (T)))
            {
                iface = (T) m_clientInterfaces[typeof (T)];
                return true;
            }
            iface = default(T);
            return false;
        }

        public T Get<T>()
        {
            return (T) m_clientInterfaces[typeof (T)];
        }

        #endregion

        public string Report()
        {
            return m_udpClient.GetStats();
        }

        private readonly List<UUID> m_transfersToAbort = new List<UUID>();

        private bool HandleTransferAbort(IClientAPI sender, Packet Pack)
        {
            TransferAbortPacket transferAbort = (TransferAbortPacket) Pack;
            m_transfersToAbort.Add(transferAbort.TransferInfo.TransferID);
            return true;
        }

        /// <summary>
        ///   Make an asset request to the asset service in response to a client request.
        /// </summary>
        /// <param name = "transferRequest"></param>
        /// <param name = "taskID"></param>
        private void MakeAssetRequest(TransferRequestPacket transferRequest, UUID taskID)
        {
            UUID requestID = UUID.Zero;
            switch (transferRequest.TransferInfo.SourceType)
            {
                case (int) SourceType.Asset:
                    requestID = new UUID(transferRequest.TransferInfo.Params, 0);
                    break;
                case (int) SourceType.SimInventoryItem:
                    requestID = new UUID(transferRequest.TransferInfo.Params, 80);
                    break;
            }

            //MainConsole.Instance.InfoFormat("[CLIENT]: {0} requesting asset {1}", Name, requestID);

            m_assetService.Get(requestID.ToString(), transferRequest, AssetReceived);
        }

        /// <summary>
        ///   When we get a reply back from the asset service in response to a client request, send back the data.
        /// </summary>
        /// <param name = "id"></param>
        /// <param name = "sender"></param>
        /// <param name = "asset"></param>
        private void AssetReceived(string id, Object sender, AssetBase asset)
        {
            //MainConsole.Instance.InfoFormat("[CLIENT]: {0} found requested asset", Name);

            TransferRequestPacket transferRequest = (TransferRequestPacket) sender;

            UUID requestID = UUID.Zero;
            byte source = (byte) SourceType.Asset;
            if (transferRequest.TransferInfo.SourceType == (int) SourceType.Asset)
            {
                requestID = new UUID(transferRequest.TransferInfo.Params, 0);
            }
            else if (transferRequest.TransferInfo.SourceType == (int) SourceType.SimInventoryItem)
            {
                requestID = new UUID(transferRequest.TransferInfo.Params, 80);
                source = (byte) SourceType.SimInventoryItem;
            }

            if (m_transfersToAbort.Contains(requestID))
                return; //They wanted to cancel it

            // The asset is known to exist and is in our cache, so add it to the AssetRequests list
            AssetRequestToClient req = new AssetRequestToClient
                                           {
                                               AssetInf = asset,
                                               AssetRequestSource = source,
                                               IsTextureRequest = false,
                                               NumPackets = asset == null ? 0 : CalculateNumPackets(asset.Data),
                                               Params = transferRequest.TransferInfo.Params,
                                               RequestAssetID = requestID,
                                               TransferRequestID = transferRequest.TransferInfo.TransferID
                                           };


            if (asset == null)
            {
                SendFailedAsset(req, TransferPacketStatus.AssetUnknownSource);
                return;
            }
                // Scripts cannot be retrieved by direct request
            if (transferRequest.TransferInfo.SourceType == (int) SourceType.Asset && asset.Type == 10)
            {
                SendFailedAsset(req, TransferPacketStatus.InsufficientPermissions);
                return;
            }

            SendAsset(req);
        }

        /// <summary>
        ///   Calculate the number of packets required to send the asset to the client.
        /// </summary>
        /// <param name = "data"></param>
        /// <returns></returns>
        private static int CalculateNumPackets(byte[] data)
        {
            const uint m_maxPacketSize = 1024;

            int numPackets = 1;

            if (data == null)
                return 0;

            if (data.LongLength > m_maxPacketSize)
            {
                // over max number of bytes so split up file
                long restData = data.LongLength - m_maxPacketSize;
                int restPackets = (int) ((restData + m_maxPacketSize - 1)/m_maxPacketSize);
                numPackets += restPackets;
            }

            return numPackets;
        }

        #region IClientIPEndpoint Members

        public IPAddress EndPoint
        {
            get
            {
                if (m_userEndPoint is IPEndPoint)
                {
                    IPEndPoint ep = (IPEndPoint) m_userEndPoint;

                    return ep.Address;
                }
                return null;
            }
        }

        #endregion

        public void SendRebakeAvatarTextures(UUID textureID)
        {
            RebakeAvatarTexturesPacket pack =
                (RebakeAvatarTexturesPacket) PacketPool.Instance.GetPacket(PacketType.RebakeAvatarTextures);

            pack.TextureData = new RebakeAvatarTexturesPacket.TextureDataBlock {TextureID = textureID};
            //            OutPacket(pack, ThrottleOutPacketType.Texture);
            OutPacket(pack, ThrottleOutPacketType.AvatarInfo);
        }

        #region PriorityQueue

        public struct PacketProcessor
        {
            public bool Async;
            public PacketMethod method;
        }

        public class AsyncPacketProcess
        {
            public readonly LLClientView ClientView;
            public readonly PacketMethod Method;
            public readonly Packet Pack;
            public bool result;

            public AsyncPacketProcess(LLClientView pClientview, PacketMethod pMethod, Packet pPack)
            {
                ClientView = pClientview;
                Method = pMethod;
                Pack = pPack;
            }
        }

        #endregion

        public static OSD BuildEvent(string eventName, OSD eventBody)
        {
            OSDMap osdEvent = new OSDMap(2) {{"message", new OSDString(eventName)}, {"body", eventBody}};

            return osdEvent;
        }

        public void SendAvatarInterestsReply(UUID avatarID, uint wantMask, string wantText, uint skillsMask,
                                             string skillsText, string languages)
        {
            AvatarInterestsReplyPacket packet =
                (AvatarInterestsReplyPacket) PacketPool.Instance.GetPacket(PacketType.AvatarInterestsReply);

            packet.AgentData = new AvatarInterestsReplyPacket.AgentDataBlock {AgentID = AgentId, AvatarID = avatarID};

            packet.PropertiesData = new AvatarInterestsReplyPacket.PropertiesDataBlock
                                        {
                                            WantToMask = wantMask,
                                            WantToText = Utils.StringToBytes(wantText),
                                            SkillsMask = skillsMask,
                                            SkillsText = Utils.StringToBytes(skillsText),
                                            LanguagesText = Utils.StringToBytes(languages)
                                        };
            OutPacket(packet, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendChangeUserRights(UUID agentID, UUID friendID, int rights)
        {
            ChangeUserRightsPacket packet =
                (ChangeUserRightsPacket) PacketPool.Instance.GetPacket(PacketType.ChangeUserRights);

            packet.AgentData = new ChangeUserRightsPacket.AgentDataBlock {AgentID = agentID};

            packet.Rights = new ChangeUserRightsPacket.RightsBlock[1];
            packet.Rights[0] = new ChangeUserRightsPacket.RightsBlock {AgentRelated = friendID, RelatedRights = rights};

            OutPacket(packet, ThrottleOutPacketType.AvatarInfo);
        }

        public void SendTextBoxRequest(string message, int chatChannel, string objectname, string ownerFirstName,
                                       string ownerLastName, UUID objectId)
        {
            ScriptDialogPacket dialog = (ScriptDialogPacket) PacketPool.Instance.GetPacket(PacketType.ScriptDialog);
            dialog.Data.ObjectID = objectId;
            dialog.Data.ChatChannel = chatChannel;
            dialog.Data.ImageID = UUID.Zero;
            dialog.Data.ObjectName = Util.StringToBytes256(objectname);
            // this is the username of the *owner*
            dialog.Data.FirstName = Util.StringToBytes256(ownerFirstName);
            dialog.Data.LastName = Util.StringToBytes256(ownerLastName);
            dialog.Data.Message = Util.StringToBytes256(message);

            ScriptDialogPacket.ButtonsBlock[] buttons = new ScriptDialogPacket.ButtonsBlock[1];
            buttons[0] = new ScriptDialogPacket.ButtonsBlock {ButtonLabel = Util.StringToBytes256("!!llTextBox!!")};
            dialog.Buttons = buttons;
            OutPacket(dialog, ThrottleOutPacketType.AvatarInfo);
        }

        public void StopFlying(IEntity p)
        {
            if (p is IScenePresence)
            {
                IScenePresence presence = p as IScenePresence;
                // It turns out to get the agent to stop flying, you have to feed it stop flying velocities
                // There's no explicit message to send the client to tell it to stop flying..   it relies on the
                // velocity, collision plane and avatar height

                // Add 1/6 the avatar's height to it's position so it doesn't shoot into the air
                // when the avatar stands up

                Vector3 pos = presence.AbsolutePosition;

                IAvatarAppearanceModule appearance = presence.RequestModuleInterface<IAvatarAppearanceModule>();
                if (appearance != null)
                    pos += new Vector3(0f, 0f, (appearance.Appearance.AvatarHeight/6f));

                presence.AbsolutePosition = pos;

                // attach a suitable collision plane regardless of the actual situation to force the LLClient to land.
                // Collision plane below the avatar's position a 6th of the avatar's height is suitable.
                // Mind you, that this method doesn't get called if the avatar's velocity magnitude is greater then a
                // certain amount..   because the LLClient wouldn't land in that situation anyway.

                if (appearance != null)
                    presence.CollisionPlane = new Vector4(0, 0, 0, pos.Z - appearance.Appearance.AvatarHeight/6f);


                ImprovedTerseObjectUpdatePacket.ObjectDataBlock block =
                    CreateImprovedTerseBlock(p, false);

                float TIME_DILATION = m_scene.TimeDilation;
                ushort timeDilation = Utils.FloatToUInt16(TIME_DILATION, 0.0f, 1.0f);


                ImprovedTerseObjectUpdatePacket packet = new ImprovedTerseObjectUpdatePacket
                                                             {
                                                                 RegionData =
                                                                     {
                                                                         RegionHandle = m_scene.RegionInfo.RegionHandle,
                                                                         TimeDilation = timeDilation
                                                                     },
                                                                 ObjectData =
                                                                     new ImprovedTerseObjectUpdatePacket.ObjectDataBlock
                                                                     [1]
                                                             };

                packet.ObjectData[0] = block;

                OutPacket(packet, ThrottleOutPacketType.Task, true);
            }

            //ControllingClient.SendAvatarTerseUpdate(new SendAvatarTerseData(m_rootRegionHandle, (ushort)(m_scene.TimeDilation * ushort.MaxValue), LocalId,
            //        AbsolutePosition, Velocity, Vector3.Zero, m_bodyRot, new Vector4(0,0,1,AbsolutePosition.Z - 0.5f), m_uuid, null, GetUpdatePriority(ControllingClient)));
        }

        public void ForceSendOnAgentUpdate(IClientAPI client, AgentUpdateArgs args)
        {
            OnAgentUpdate(client, args);
        }

        public void OnForceChatFromViewer(IClientAPI sender, OSChatMessage e)
        {
            OnChatFromClient(sender, e);
        }
    }
}