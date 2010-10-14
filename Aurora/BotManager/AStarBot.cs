using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using log4net;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace Aurora.BotManager
{
    public interface IAStarBot : IRexBot
    {
        int[,] ReadMap(string filename, int X, int Y, int CornerStoneX, int CornerStoneY);
        void FindPath(Vector3 currentPos, Vector3 finishVector);
        void FollowAvatar(string avatarName);
    }

    public class AStarBot : RexBot, IAStarBot
    {
        #region Declares

        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public ScenePresence m_Sp = null;
        private const float followDistance = 2.5f;

        public int cornerStoneX = 128;
        public int cornerStoneY = 128;
        int[,] currentMap = new int[5, 5];
        public bool ShouldFly = false;

        public bool IsFollowing = false;
        public UUID FollowID = UUID.Zero;
        public ScenePresence FollowSP = null;
        public string FollowName = "";
        private const float FollowTimeBeforeUpdate = 10;
        private float CurrentFollowTimeBeforeUpdate = 0;

        public bool IsOnAPath = false;
        public List<Vector3> WayPoints = new List<Vector3>();
        public int CurrentWayPoint = 0;

        #endregion

        #region Constructor

        public AStarBot(Scene scene)
            : base(scene)
        {
        }

        #endregion

        #region Events

        public void Initialize(ScenePresence SP)
        {
            m_Sp = SP;
            SP.Scene.EventManager.OnFrame += OnFrame;
        }

        void OnFrame()
        {
            //This is our main updating loop
            // We use this to deal with moving the av to the next location that it needs to go to

            if (IsFollowing)
            {
                // FOLLOW an avatar - this is looking for an avatar UUID so wont follow a prim here  - yet
                if (FollowSP == null)
                {
                    m_Sp.Scene.TryGetAvatarByName(FollowName, out FollowSP);
                }
                //If its still null, the person doesn't exist, cancel the follow and return
                if (FollowSP == null)
                {
                    IsFollowing = false;
                    m_log.Warn("Could not find avatar " + FollowName);
                }
                else
                {
                    //Only check so many times
                    CurrentFollowTimeBeforeUpdate++;
                    if (CurrentFollowTimeBeforeUpdate == FollowTimeBeforeUpdate)
                    {
                        NavMesh mesh = new NavMesh();
                        
                        mesh.AddEdge(0, 1, ShouldFly ? Aurora.Framework.TravelMode.Fly : Aurora.Framework.TravelMode.Walk);
                        mesh.AddNode(m_Sp.AbsolutePosition); //Give it the current pos so that it will know where to start
                        
                        mesh.AddEdge(1, 2, ShouldFly ? Aurora.Framework.TravelMode.Fly : Aurora.Framework.TravelMode.Walk);
                        mesh.AddNode(FollowSP.AbsolutePosition); //Give it the new point so that it will head toward it
                        
                        SetPath(mesh, 0, false, 10000); //Set and go
                        //Reset the time
                        CurrentFollowTimeBeforeUpdate = -1;
                    }
                }
            }
            else if (IsOnAPath)
            {
                lock(WayPoints)
                {
                    if (WayPoints[CurrentWayPoint].ApproxEquals(m_Sp.AbsolutePosition, 1)) //Are we about to the new position?
                    {
                        //We need to update the waypoint then and send the av to a new location
                        CurrentWayPoint++;
                        if (WayPoints.Count >= CurrentWayPoint)
                        {
                            //We are at the last point, end the path checking
                            IsOnAPath = false;
                            return;
                        }
                        NavMesh mesh = new NavMesh();
                        //Build the next mesh to tell the bot where to go
                        mesh.AddEdge(0, 1, ShouldFly ? Aurora.Framework.TravelMode.Fly : Aurora.Framework.TravelMode.Walk);
                        mesh.AddNode(m_Sp.AbsolutePosition); //Give it the current pos so that it will know where to start
                        mesh.AddEdge(1, 2, ShouldFly ? Aurora.Framework.TravelMode.Fly : Aurora.Framework.TravelMode.Walk);
                        mesh.AddNode(WayPoints[CurrentWayPoint]); //Give it the new point so that it will head toward it
                        SetPath(mesh, 0, false, 10000); //Set and go
                    }
                }
            }
        }

        #endregion

        #region IAStarBot

        public int[,] ReadMap(string filename, int X, int Y, int CornerStoneX, int CornerStoneY)
        {
            cornerStoneX = CornerStoneX;
            cornerStoneY = CornerStoneY;
            if (File.Exists(Environment.CurrentDirectory + "/bot/" + filename))
            {
                m_log.Debug("Attempting to load map " + filename + ", X - " + X + ", Y - " + Y);
                currentMap = Games.Pathfinding.AStar2DTest.StartPath.ReadMap(Environment.CurrentDirectory + "/bot/" + filename, X, Y);
                if (currentMap[0, 0] == -99)
                {
                    m_log.Warn("The map was found but failed to load. Check bin\botMaps.");
                }
                else
                {
                    m_log.Debug(filename + " loaded successfully.");
                }
            }
            else
            {
                m_log.Warn("Map not loaded...check the bin\botMap folder. Also check the name in the cornerStone description to see if it matches a text file inside that folder.");
            }
            return currentMap;
        }

        public void FindPath(Vector3 currentPos, Vector3 finishVector)
        {
            // Bot position converted to map coordinates -maybe here we can check if on Map
            int startX = (int)currentPos.X - cornerStoneX;
            int startY = (int)currentPos.Y - cornerStoneY;

            m_log.Debug("My Pos " + currentPos.ToString() + " , End Pos " + finishVector.ToString());

            // Goal position converted to map coordinates
            int finishX = (int)finishVector.X - cornerStoneX;
            int finishY = (int)finishVector.Y - cornerStoneY;
            int finishZ = 25;

            m_Sp.StandUp(); //Can't follow a path if sitting

            IsFollowing = false; //Turn off following

            CurrentWayPoint = 0; //Reset to the beginning of the list
            List<string> points = Games.Pathfinding.AStar2DTest.StartPath.Path(startX, startY, finishX, finishY, finishZ, cornerStoneX, cornerStoneY);

            if (points.Contains("no_path"))
            {
                m_log.Debug("I'm sorry I could not find a solution to that path. Teleporting instead");
                m_Sp.Teleport(finishVector);
                return;
            }
            else
            {
                IsOnAPath = true;
            }

            lock (WayPoints)
            {
                foreach (string s in points)
                {
                    m_log.Debug(s);
                    string[] Vector = s.Split(',');

                    if (Vector.Length != 3)
                        continue;

                    WayPoints.Add(new Vector3(float.Parse(Vector[0]),
                        float.Parse(Vector[1]),
                        float.Parse(Vector[2])));
                }
            }
        }

        public void FollowAvatar(string avatarName)
        {
            m_Sp.StandUp(); //Can't follow if sitting
            IsFollowing = true;
            FollowName = avatarName;
            FollowID = UUID.Zero;
        }

        #endregion
    }
}
