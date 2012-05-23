/*
 * Copyright (c) Contributors, http://aurora-sim.org/
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
using System.Runtime.Serialization;
using OpenMetaverse;
using Aurora.Framework;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    [Serializable]
    public class EventAbortException : Exception
    {
        public EventAbortException()
        {
        }

        protected EventAbortException(
            SerializationInfo info,
            StreamingContext context)
        {
        }
    }

    [Serializable]
    public class MinEventDelayException : Exception
    {
        public MinEventDelayException()
        {
        }

        protected MinEventDelayException(
            SerializationInfo info,
            StreamingContext context)
        {
        }
    }

    [Serializable]
    public class SelfDeleteException : Exception
    {
        public SelfDeleteException()
        {
        }

        protected SelfDeleteException(
            SerializationInfo info,
            StreamingContext context)
        {
        }
    }

    [Serializable]
    public class ScriptDeleteException : Exception
    {
        public ScriptDeleteException()
        {
        }

        protected ScriptDeleteException(
            SerializationInfo info,
            StreamingContext context)
        {
        }
    }

    [Serializable]
    public class ScriptPermissionsException : Exception
    {
        public ScriptPermissionsException(string message)
            : base(message)
        {
        }

        protected ScriptPermissionsException(
            SerializationInfo info,
            StreamingContext context)
        {
        }
    }

    public class DetectParams
    {
        public UUID Group;
        public UUID Key;
        public int LinkNum;
        public string Name;
        public LSL_Types.Vector3 OffsetPos;
        public UUID Owner;
        public LSL_Types.Vector3 Position;
        public LSL_Types.Quaternion Rotation;
        public int Type;
        public LSL_Types.Vector3 Velocity;
        private LSL_Types.Vector3 touchBinormal;
        private int touchFace;
        private LSL_Types.Vector3 touchNormal;
        private LSL_Types.Vector3 touchPos;

        private LSL_Types.Vector3 touchST;
        private LSL_Types.Vector3 touchUV;

        public DetectParams()
        {
            Key = UUID.Zero;
            OffsetPos = new LSL_Types.Vector3();
            LinkNum = 0;
            Group = UUID.Zero;
            Name = String.Empty;
            Owner = UUID.Zero;
            Position = new LSL_Types.Vector3();
            Rotation = new LSL_Types.Quaternion();
            Type = 0;
            Velocity = new LSL_Types.Vector3();
            initializeSurfaceTouch();
        }

        public LSL_Types.Vector3 TouchST
        {
            get { return touchST; }
        }

        public LSL_Types.Vector3 TouchNormal
        {
            get { return touchNormal; }
        }

        public LSL_Types.Vector3 TouchBinormal
        {
            get { return touchBinormal; }
        }

        public LSL_Types.Vector3 TouchPos
        {
            get { return touchPos; }
        }

        public LSL_Types.Vector3 TouchUV
        {
            get { return touchUV; }
        }

        public int TouchFace
        {
            get { return touchFace; }
        }

        // This can be done in two places including the constructor
        // so be carefull what gets added here

        /*
         * Set up the surface touch detected values
         */

        public SurfaceTouchEventArgs SurfaceTouchArgs
        {
            set
            {
                if (value == null)
                {
                    // Initialise to defaults if no value
                    initializeSurfaceTouch();
                }
                else
                {
                    // Set the values from the touch data provided by the client
                    touchST = new LSL_Types.Vector3(value.STCoord.X, value.STCoord.Y, value.STCoord.Z);
                    touchUV = new LSL_Types.Vector3(value.UVCoord.X, value.UVCoord.Y, value.UVCoord.Z);
                    touchNormal = new LSL_Types.Vector3(value.Normal.X, value.Normal.Y, value.Normal.Z);
                    touchBinormal = new LSL_Types.Vector3(value.Binormal.X, value.Binormal.Y, value.Binormal.Z);
                    touchPos = new LSL_Types.Vector3(value.Position.X, value.Position.Y, value.Position.Z);
                    touchFace = value.FaceIndex;
                }
            }
        }

        private void initializeSurfaceTouch()
        {
            touchST = new LSL_Types.Vector3(-1.0, -1.0, 0.0);
            touchNormal = new LSL_Types.Vector3();
            touchBinormal = new LSL_Types.Vector3();
            touchPos = new LSL_Types.Vector3();
            touchUV = new LSL_Types.Vector3(-1.0, -1.0, 0.0);
            touchFace = -1;
        }

        public void Populate(IScene scene)
        {
            ISceneChildEntity part = scene.GetSceneObjectPart(Key);
            Vector3 tmp;
            if (part == null) // Avatar, maybe?
            {
                IScenePresence presence = scene.GetScenePresence(Key);
                if (presence == null)
                    return;

                Name = presence.Name;
                Owner = Key;

                tmp = presence.AbsolutePosition;
                Position = new LSL_Types.Vector3(
                    tmp.X,
                    tmp.Y,
                    tmp.Z);
                Quaternion rtmp = presence.Rotation;
                Rotation = new LSL_Types.Quaternion(
                    rtmp.X,
                    rtmp.Y,
                    rtmp.Z,
                    rtmp.W);
                tmp = presence.Velocity;
                Velocity = new LSL_Types.Vector3(
                    tmp.X,
                    tmp.Y,
                    tmp.Z);

                Type = 0x01; // Avatar
                if (presence.Velocity != Vector3.Zero)
                    Type |= 0x02; // Active

                Group = presence.ControllingClient.ActiveGroupId;

                return;
            }

            part = part.ParentEntity.RootChild; // We detect objects only

            LinkNum = 0; // Not relevant

            Group = part.GroupID;
            Name = part.Name;
            Owner = part.OwnerID;
            Type = part.Velocity == Vector3.Zero ? 0x04 : 0x02;

            foreach (ISceneChildEntity child in part.ParentEntity.ChildrenEntities())
                if (child.Inventory.ContainsScripts())
                    Type |= 0x08; // Scripted

            tmp = part.AbsolutePosition;
            Position = new LSL_Types.Vector3(tmp.X,
                                             tmp.Y,
                                             tmp.Z);

            Quaternion wr = part.ParentEntity.GroupRotation;
            Rotation = new LSL_Types.Quaternion(wr.X, wr.Y, wr.Z, wr.W);

            tmp = part.Velocity;
            Velocity = new LSL_Types.Vector3(tmp.X,
                                             tmp.Y,
                                             tmp.Z);
        }
    }

    /// <summary>
    ///   Holds all the data required to execute a scripting event.
    /// </summary>
    public class EventParams
    {
        public DetectParams[] DetectParams;
        public string EventName;
        public Object[] Params;

        public EventParams(string eventName, Object[] eventParams, DetectParams[] detectParams)
        {
            EventName = eventName;
            Params = eventParams;
            DetectParams = detectParams;
        }
    }

    /// <summary>
    ///   Queue item structure
    /// </summary>
    public class QueueItemStruct
    {
        public EnumeratorInfo CurrentlyAt;
        public ScriptEventsProcData EventsProcData;
        public ScriptData ID;
        public int RunningNumber;
        //The currently running state that this event will be fired under
        public string State;
        public long VersionID;
        //Name of the method to fire
        public string functionName;
        //Params to give the script event
        public DetectParams[] llDetectParams;
        //Parameters to fire the function
        public object[] param;
        //This is the current spot that the event is at in processing
    }

    public struct StateQueueItem
    {
        public bool Create;
        public ScriptData ID;
    }

    // Load/Unload structure
    public struct LUStruct
    {
        public LUType Action;
        public ScriptData ID;
    }

    public enum LUType
    {
        Unknown = 0,
        Load = 1,
        Unload = 2,
        Reupload = 3
    }

    public enum EventPriority
    {
        FirstStart = 0,
        Suspended = 1,
        Continued = 2
    }

    public enum ScriptEventsState
    {
        Idle = 0,
        Sleep = 1,
        Suspended = 2,
        Running = 3, //?
        InExec = 4,
        InExecAbort = 5,
        Delete = 6,
        Deleted = -1 //?
    }

    public class ScriptEventsProcData
    {
        public ScriptEventsState State;
        public DateTime TimeCheck;
    }

    public enum LoadPriority
    {
        FirstStart = 0,
        Restart = 1,
        Stop = 2
    }

    /// <summary>
    /// Threat Level for a scripting function
    /// </summary>
    public enum ThreatLevel
    {
        /// <summary>
        /// Function is no threat at all. It doesn't constitute a threat to either users or the system and has no known side effects
        /// </summary>
        None = 0,

        /// <summary>
        /// Abuse of this command can cause a nuisance to the region operator, such as log message spew
        /// </summary>
        Nuisance = 1,

        /// <summary>
        /// Extreme levels of abuse of this function can cause impaired functioning of the region, or very gullible users can be tricked into experiencing harmless effects
        /// </summary>
        VeryLow = 2,

        /// <summary>
        /// Intentional abuse can cause crashes or malfunction under certain circumstances, which can easily be rectified, or certain users can be tricked into certain situations in an avoidable manner.
        /// </summary>
        Low = 3,

        /// <summary>
        /// Intentional abuse can cause denial of service and crashes with potential of data or state loss, or trusting users can be tricked into embarrassing or uncomfortable situations.
        /// </summary>
        Moderate = 4,

        /// <summary>
        /// Casual abuse can cause impaired functionality or temporary denial of service conditions. Intentional abuse can easily cause crashes with potential data loss, or can be used to trick experienced and cautious users into unwanted situations, or changes global data permanently and without undo ability
        /// Malicious scripting can allow theft of content
        /// </summary>
        High = 5,

        /// <summary>
        /// Even normal use may, depending on the number of instances, or frequency of use, result in severe service impairment or crash with loss of data, or can be used to cause unwanted or harmful effects on users without giving the user a means to avoid it.
        /// </summary>
        VeryHigh = 6,

        /// <summary>
        /// Even casual use is a danger to region stability, or function allows console or OS command execution, or function allows taking money without consent, or allows deletion or modification of user data, or allows the compromise of sensitive data by design.
        /// </summary>
        Severe = 7,

        NoAccess = 8
    }
}