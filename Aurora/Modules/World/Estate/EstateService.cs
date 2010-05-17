using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using Nini.Config;
using log4net;
using Aurora.Framework;
using Aurora.DataManager;

namespace Aurora.Modules
{
    public class EstateSettingsModule : ISharedRegionModule, IEstateSettingsModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        Scene m_scene;
        ISimMapDataConnector SimMapConnector;
        IRegionConnector RegionConnector;

        public void Initialise(IConfigSource source)
        {
        }

        public void AddRegion(Scene scene)
        {
            SimMapConnector = DataManager.DataManager.ISimMapConnector;
            RegionConnector = DataManager.DataManager.IRegionConnector;
            scene.RegisterModuleInterface<IEstateSettingsModule>(this);
            m_scene = scene;
            scene.AddCommand(this, "set regionsetting", "set regionsetting", "Sets a region setting for the given region. Valid params: Maturity - 0(PG),1(Mature),2(Adult); AddEstateBan,RemoveEstateBan,AddEstateManager,RemoveEstateManager - First name, Last name", SetRegionInfoOption);
        }

        public void RemoveRegion(Scene scene)
        {

        }

        public void RegionLoaded(Scene scene)
        {
            
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        protected void SetRegionInfoOption(string module, string[] cmdparams)
        {
            #region 3 Params needed
            if (cmdparams.Length < 3)
            {
                m_log.Warn("Not enough parameters!");
                return;
            }
            EstateSettings ES = m_scene.EstateService.LoadEstateSettings(m_scene.RegionInfo.RegionID, false);
            if (cmdparams[2] == "Maturity")
            {
                SimMap map = SimMapConnector.GetSimMap(m_scene.RegionInfo.RegionID);
                if (cmdparams[3] == "PG")
                {
                    map.SimFlags = map.SimFlags & SimMapFlags.PG;
                    m_scene.RegionInfo.RegionSettings.Maturity = 0;
                }
                else if (cmdparams[3] == "Mature")
                {
                    map.SimFlags = map.SimFlags & SimMapFlags.Mature;
                    m_scene.RegionInfo.RegionSettings.Maturity = 1;
                }
                else if (cmdparams[3] == "Adult")
                {
                    map.SimFlags = map.SimFlags & SimMapFlags.Adult;
                    m_scene.RegionInfo.RegionSettings.Maturity = 2;
                }
                else
                {
                    m_log.Warn("Your parameter did not match any existing parameters. Try PG, Mature, or Adult");
                    return;
                }
                SimMapConnector.SetSimMap(map);
                m_scene.RegionInfo.RegionSettings.Save();
            }
            #endregion
            #region 4 Params needed
            if (cmdparams.Length < 4)
            {
                m_log.Warn("Not enough parameters!");
                return;
            }
            if (cmdparams[2] == "AddEstateBan")
            {
                EstateBan EB = new EstateBan();
                EB.BannedUserID = m_scene.UserAccountService.GetUserAccount(UUID.Zero,cmdparams[3],cmdparams[4]).PrincipalID;
                ES.AddBan(EB);
            }
            if (cmdparams[2] == "AddEstateManager")
            {
                ES.AddEstateManager(m_scene.UserAccountService.GetUserAccount(UUID.Zero, cmdparams[3], cmdparams[4]).PrincipalID);
            }
            if (cmdparams[2] == "RemoveEstateBan")
            {
                ES.RemoveBan(m_scene.UserAccountService.GetUserAccount(UUID.Zero, cmdparams[3], cmdparams[4]).PrincipalID);
            }
            if (cmdparams[2] == "RemoveEstateManager")
            {
                ES.RemoveEstateManager(m_scene.UserAccountService.GetUserAccount(UUID.Zero, cmdparams[3], cmdparams[4]).PrincipalID);
            }
            #endregion
            m_scene.RegionInfo.RegionSettings.Save();
            ES.Save();
        }

        public void PostInitialise()
        {
        }

        public void Close() { }

        public string Name { get { return "EstateSettingsModule"; } }

        public bool IsSharedModule { get { return true; } }

        public bool AllowTeleport(IScene scene, UUID userID, Vector3 Position, out Vector3 newPosition)
        {
            newPosition = Position;
            EstateSettings ES = ((Scene)scene).EstateService.LoadEstateSettings(scene.RegionInfo.RegionID, false);
            IAgentConnector data = DataManager.DataManager.IAgentConnector;
            IAgentInfo Profile = data.GetAgent(userID);
            
            if (((Scene)scene).RegionInfo.RegionSettings.Maturity > Profile.MaxMaturity)
                return false;

            if (ES.DenyMinors && Profile.IsMinor)
                return false;

            if (!ES.PublicAccess)
            {
                if (!new List<UUID>(ES.EstateManagers).Contains(userID) || ES.EstateOwner != userID)
                    return false;
            }
            if (!ES.AllowDirectTeleport)
            {
                Telehub telehub = RegionConnector.FindTelehub(m_scene.RegionInfo.RegionID);
                if (telehub != null)
                    newPosition = new Vector3(telehub.TelehubX, telehub.TelehubY, telehub.TelehubZ);
            }
            else
            {
                ILandObject ILO = ((Scene)scene).LandChannel.GetLandObject(Position.X, Position.Y);
                if (ILO != null)
                {
                    if (ILO.LandData.LandingType == 2)
                    {
                        List<ILandObject> Parcels = ParcelsNearPoint(((Scene)scene), Position, ILO);
                        if (Parcels.Count == 0)
                        {
                            ScenePresence SP;
                            ((Scene)scene).TryGetScenePresence(userID, out SP);
                            newPosition = GetNearestRegionEdgePosition(SP);
                        }
                        else
                            newPosition = Parcels[0].LandData.UserLocation;
                    }
                    if (ILO.LandData.LandingType == 1)
                        newPosition = ILO.LandData.UserLocation;
                }
            }


            return true;
        }

        #region Helpers
        private Vector3 GetPositionAtGround(Scene scene, float x, float y)
        {
            return new Vector3(x, y, GetGroundHeight(scene, x, y));
        }

        public float GetGroundHeight(Scene scene, float x, float y)
        {
            if (x < 0)
                x = 0;
            if (x >= scene.Heightmap.Width)
                x = scene.Heightmap.Width - 1;
            if (y < 0)
                y = 0;
            if (y >= scene.Heightmap.Height)
                y = scene.Heightmap.Height - 1;

            Vector3 p0 = new Vector3(x, y, (float)scene.Heightmap[(int)x, (int)y]);
            Vector3 p1 = new Vector3(p0);
            Vector3 p2 = new Vector3(p0);

            p1.X += 1.0f;
            if (p1.X < scene.Heightmap.Width)
                p1.Z = (float)scene.Heightmap[(int)p1.X, (int)p1.Y];

            p2.Y += 1.0f;
            if (p2.Y < scene.Heightmap.Height)
                p2.Z = (float)scene.Heightmap[(int)p2.X, (int)p2.Y];

            Vector3 v0 = new Vector3(p1.X - p0.X, p1.Y - p0.Y, p1.Z - p0.Z);
            Vector3 v1 = new Vector3(p2.X - p0.X, p2.Y - p0.Y, p2.Z - p0.Z);

            v0.Normalize();
            v1.Normalize();

            Vector3 vsn = new Vector3();
            vsn.X = (v0.Y * v1.Z) - (v0.Z * v1.Y);
            vsn.Y = (v0.Z * v1.X) - (v0.X * v1.Z);
            vsn.Z = (v0.X * v1.Y) - (v0.Y * v1.X);
            vsn.Normalize();

            float xdiff = x - (float)((int)x);
            float ydiff = y - (float)((int)y);

            return (((vsn.X * xdiff) + (vsn.Y * ydiff)) / (-1 * vsn.Z)) + p0.Z;
        }

        private Vector3 GetPositionAtAvatarHeightOrGroundHeight(ScenePresence avatar, float x, float y)
        {
            Vector3 ground = GetPositionAtGround(avatar.Scene, x, y);
            if (avatar.AbsolutePosition.Z > ground.Z)
            {
                ground.Z = avatar.AbsolutePosition.Z;
            }
            return ground;
        }

        private Vector3 GetNearestRegionEdgePosition(ScenePresence avatar)
        {
            float xdistance = avatar.AbsolutePosition.X < Constants.RegionSize / 2 ? avatar.AbsolutePosition.X : Constants.RegionSize - avatar.AbsolutePosition.X;
            float ydistance = avatar.AbsolutePosition.Y < Constants.RegionSize / 2 ? avatar.AbsolutePosition.Y : Constants.RegionSize - avatar.AbsolutePosition.Y;

            //find out what vertical edge to go to
            if (xdistance < ydistance)
            {
                if (avatar.AbsolutePosition.X < Constants.RegionSize / 2)
                {
                    return GetPositionAtAvatarHeightOrGroundHeight(avatar, 0.0f, avatar.AbsolutePosition.Y);
                }
                else
                {
                    return GetPositionAtAvatarHeightOrGroundHeight(avatar, Constants.RegionSize, avatar.AbsolutePosition.Y);
                }
            }
            //find out what horizontal edge to go to
            else
            {
                if (avatar.AbsolutePosition.Y < Constants.RegionSize / 2)
                {
                    return GetPositionAtAvatarHeightOrGroundHeight(avatar, avatar.AbsolutePosition.X, 0.0f);
                }
                else
                {
                    return GetPositionAtAvatarHeightOrGroundHeight(avatar, avatar.AbsolutePosition.X, Constants.RegionSize);
                }
            }
        }

        public List<ILandObject> ParcelsNearPoint(Scene scene, Vector3 position, ILandObject currentparcel)
        {
            List<ILandObject> parcelsNear = new List<ILandObject>();
            parcelsNear.Add(currentparcel);
            for (int x = -4; x <= 4; x += 4)
            {
                for (int y = -4; y <= 4; y += 4)
                {
                    ILandObject check = scene.LandChannel.GetLandObject(position.X + x, position.Y + y);
                    if (check != null)
                    {
                        if (!parcelsNear.Contains(check))
                        {
                            parcelsNear.Add(check);
                        }
                    }
                }
            }

            return parcelsNear;
        }
        #endregion
    }
}
