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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using Nini.Config;
using log4net;
using log4net.Core;
using OpenMetaverse;
using OpenMetaverse.Packets;
using OpenSim;
using OpenSim.Framework;

using OpenSim.Region.CoreModules;
using OpenSim.Region.CoreModules.World.Land;
using OpenSim.Region.CoreModules.World.Terrain;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;
using OpenSim.Region.Framework.Scenes.Animation;
using OpenSim.Region.Physics.Manager;
using Aurora.Framework;
using Aurora.ScriptEngine.AuroraDotNetEngine.Plugins;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using Aurora.ScriptEngine.AuroraDotNetEngine.Runtime;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using PrimType = OpenSim.Framework.PrimType;
using AssetLandmark = OpenSim.Framework.AssetLandmark;

using LSL_Float = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLFloat;
using LSL_Integer = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLInteger;
using LSL_Key = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using LSL_List = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.list;
using LSL_Rotation = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.Quaternion;
using LSL_String = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using LSL_Vector = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.Vector3;
using System.Reflection;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.APIs
{
    /// <summary>
    /// Contains all LSL ll-functions. This class will be in Default AppDomain.
    /// </summary>
    [Serializable]
    public class LSL_Api : MarshalByRefObject, ILSL_Api, IScriptApi
    {
        //private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected IScriptModulePlugin m_ScriptEngine;
        protected ISceneChildEntity m_host;
        protected uint m_localID;
        protected UUID m_itemID;
        protected bool throwErrorOnNotImplemented = true;
        protected float m_ScriptDelayFactor = 1.0f;
        protected float m_ScriptDistanceFactor = 1.0f;
        protected float m_MinTimerInterval = 0.1f;

        protected DateTime m_timer = DateTime.Now;
        protected bool m_waitingForScriptAnswer = false;
        protected bool m_automaticLinkPermission = false;
        protected IMessageTransferModule m_TransferModule = null;
        protected int m_notecardLineReadCharsMax = 255;
        protected int m_scriptConsoleChannel = 0;
        protected bool m_scriptConsoleChannelEnabled = false;
        protected IUrlModule m_UrlModule = null;
        internal ScriptProtectionModule ScriptProtection;
        protected IWorldComm m_comms = null;

        // MUST be a ref type
        public class UserInfoCacheEntry
        {
            public int time;
            public UserAccount account;
            public UserInfo pinfo;
        }
        protected Dictionary<UUID, UserInfoCacheEntry> m_userInfoCache =
                new Dictionary<UUID, UserInfoCacheEntry>();

        public void Initialize (IScriptModulePlugin ScriptEngine, ISceneChildEntity host, uint localID, UUID itemID, ScriptProtectionModule module)
        {
            m_ScriptEngine = ScriptEngine;
            m_host = host;
            m_localID = localID;
            m_itemID = itemID;
            ScriptProtection = module;

            m_ScriptDelayFactor =
                m_ScriptEngine.Config.GetFloat("ScriptDelayFactor", 1.0f);
            m_ScriptDistanceFactor =
                m_ScriptEngine.Config.GetFloat("ScriptDistanceLimitFactor", 1.0f);
            m_MinTimerInterval =
                m_ScriptEngine.Config.GetFloat("MinTimerInterval", 0.5f);
            m_automaticLinkPermission =
                m_ScriptEngine.Config.GetBoolean("AutomaticLinkPermission", false);
            m_notecardLineReadCharsMax =
                m_ScriptEngine.Config.GetInt("NotecardLineReadCharsMax", 255);
            if (m_notecardLineReadCharsMax > 65535)
                m_notecardLineReadCharsMax = 65535;

            m_TransferModule =
                    World.RequestModuleInterface<IMessageTransferModule>();
            m_UrlModule = World.RequestModuleInterface<IUrlModule>();
            m_comms = World.RequestModuleInterface<IWorldComm>();
        }

        public IScriptApi Copy()
        {
            return new LSL_Api();
        }

        public string Name
        {
            get { return "ll"; }
        }

        public string InterfaceName
        {
            get { return "ILSL_Api"; }
        }

        /// <summary>
        /// We don't have to add any assemblies here
        /// </summary>
        public string[] ReferencedAssemblies
        {
            get { return new string[0]; }
        }

        /// <summary>
        /// We use the default namespace, so we don't have any to add
        /// </summary>
        public string[] NamespaceAdditions
        {
            get { return new string[0]; }
        }

        public void Dispose()
        {
        }

        public override Object InitializeLifetimeService()
        {
            ILease lease = (ILease)base.InitializeLifetimeService();

            if (lease.CurrentState == LeaseState.Initial)
            {
                lease.InitialLeaseTime = TimeSpan.FromMinutes(0);
                //                lease.RenewOnCallTime = TimeSpan.FromSeconds(10.0);
                //                lease.SponsorshipTimeout = TimeSpan.FromMinutes(1.0);
            }
            return lease;

        }

        protected virtual void ScriptSleep(int delay)
        {
            delay = (int)((float)delay * m_ScriptDelayFactor);
            if (delay == 0)
                return;

            System.Threading.Thread.Sleep(delay);
        }

        public IScene World
        {
            get { return m_host.ParentEntity.Scene; }
        }

        public void state(string newState)
        {
            m_ScriptEngine.SetState(m_itemID, newState);
            throw new EventAbortException();
        }

        /// <summary>
        /// Reset the named script. The script must be present
        /// in the same prim.
        /// </summary>
        public void llResetScript()
        {
        	ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            m_ScriptEngine.ResetScript(m_host.UUID, m_itemID, true);
        }

        public void llResetOtherScript(string name)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            UUID item;

            

            if ((item = ScriptByName(name)) != UUID.Zero)
                m_ScriptEngine.ResetScript(m_host.UUID, item, false);
            else
                ShoutError("llResetOtherScript: script "+name+" not found");
        }

        public LSL_Integer llGetScriptState(string name)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            UUID item;

            

            if ((item = ScriptByName(name)) != UUID.Zero)
            {
                return m_ScriptEngine.GetScriptRunningState(item) ?1:0;
            }

            ShoutError("llGetScriptState: script "+name+" not found");

            // If we didn't find it, then it's safe to
            // assume it is not running.

            return 0;
        }

        public void llSetScriptState(string name, int run)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            UUID item;

            

            // These functions are supposed to be robust,
            // so get the state one step at a time.

            if ((item = ScriptByName(name)) != UUID.Zero)
            {
                m_ScriptEngine.SetScriptRunningState(item, run == 1);
            }
            else
            {
                ShoutError("llSetScriptState: script "+name+" not found");
            }
        }

        public List<ISceneChildEntity> GetLinkParts(int linkType)
        {
            List<ISceneChildEntity> ret = new List<ISceneChildEntity> ();
            ret.Add(m_host);

            if (linkType == ScriptBaseClass.LINK_SET)
            {
                if (m_host.ParentEntity != null)
                    return new List<ISceneChildEntity> (m_host.ParentEntity.ChildrenEntities ());
                return ret;
            }

            if (linkType == ScriptBaseClass.LINK_ROOT)
            {
                if (m_host.ParentEntity != null)
                {
                    ret = new List<ISceneChildEntity> ();
                    ret.Add(m_host.ParentEntity.RootChild);
                    return ret;
                }
                return ret;
            }

            if (linkType == ScriptBaseClass.LINK_ALL_OTHERS)
            {
                if (m_host.ParentEntity == null)
                    return new List<ISceneChildEntity> ();
                ret = new List<ISceneChildEntity> (m_host.ParentEntity.ChildrenEntities());
                if (ret.Contains(m_host))
                    ret.Remove(m_host);
                return ret;
            }

            if (linkType == ScriptBaseClass.LINK_ALL_CHILDREN)
            {
                if (m_host.ParentEntity == null)
                    return new List<ISceneChildEntity> ();
                ret = new List<ISceneChildEntity> (m_host.ParentEntity.ChildrenEntities());
                if (ret.Contains(m_host.ParentEntity.RootChild))
                    ret.Remove(m_host.ParentEntity.RootChild);
                return ret;
            }

            if (linkType == ScriptBaseClass.LINK_THIS)
            {
                return ret;
            }

            else
            {
                if (linkType < 0 || m_host.ParentEntity == null)
                    return new List<ISceneChildEntity> ();
                IEntity target = m_host.ParentEntity.GetLinkNumPart (linkType);
                if (target is ISceneChildEntity)
                {
                    if (target == null)
                        return new List<ISceneChildEntity> ();
                    ret = new List<ISceneChildEntity> ();
                    ret.Add (target as ISceneChildEntity);
                }
                //No allowing scene presences to be found here
                return ret;
            }
        }

        public List<IEntity> GetLinkPartsAndEntities (int linkType)
        {
            List<IEntity> ret = new List<IEntity> ();
            ret.Add(m_host);

            if (linkType == ScriptBaseClass.LINK_SET)
            {
                if (m_host.ParentEntity != null)
                {
                    List<ISceneChildEntity> parts = new List<ISceneChildEntity> (m_host.ParentEntity.ChildrenEntities ());
                    return parts.ConvertAll<IEntity> (new Converter<ISceneChildEntity, IEntity> (delegate (ISceneChildEntity part)
                        {
                            return (IEntity)part;
                        }));
                }
                return ret;
            }

            if (linkType == ScriptBaseClass.LINK_ROOT)
            {
                if (m_host.ParentEntity != null)
                {
                    ret = new List<IEntity> ();
                    ret.Add (m_host.ParentEntity.RootChild);
                    return ret;
                }
                return ret;
            }

            if (linkType == ScriptBaseClass.LINK_ALL_OTHERS)
            {
                if (m_host.ParentEntity == null)
                    return new List<IEntity> ();
                List<ISceneChildEntity> sceneobjectparts = new List<ISceneChildEntity> (m_host.ParentEntity.ChildrenEntities ());
                ret = sceneobjectparts.ConvertAll<IEntity> (new Converter<ISceneChildEntity, IEntity> (delegate (ISceneChildEntity part)
                        {
                            return (IEntity)part;
                        }));
                if (ret.Contains(m_host))
                    ret.Remove(m_host);
                return ret;
            }

            if (linkType == ScriptBaseClass.LINK_ALL_CHILDREN)
            {
                if (m_host.ParentEntity == null)
                    return new List<IEntity> ();
                List<ISceneChildEntity> children = new List<ISceneChildEntity> (m_host.ParentEntity.ChildrenEntities ());
                ret = children.ConvertAll<IEntity> (new Converter<ISceneChildEntity, IEntity> (delegate (ISceneChildEntity part)
                {
                    return (IEntity)part;
                }));
                if (ret.Contains (m_host.ParentEntity.RootChild))
                    ret.Remove (m_host.ParentEntity.RootChild);
                return ret;
            }

            if (linkType == ScriptBaseClass.LINK_THIS)
            {
                return ret;
            }

            else
            {
                if (linkType < 0 || m_host.ParentEntity == null)
                    return new List<IEntity> ();
                IEntity target = m_host.ParentEntity.GetLinkNumPart (linkType);
                if (target == null)
                    return new List<IEntity> ();
                ret = new List<IEntity> ();
                ret.Add(target);

                return ret;
            }
        }

        protected UUID InventorySelf()
        {
            UUID invItemID = new UUID();

            lock (m_host.TaskInventory)
            {
                foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory)
                {
                    if (inv.Value.Type == 10 && inv.Value.ItemID == m_itemID)
                    {
                        invItemID = inv.Key;
                        break;
                    }
                }
            }

            return invItemID;
        }

        protected UUID InventoryKey(string name, int type)
        {
            

            lock (m_host.TaskInventory)
            {
                foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory)
                {
                    if (inv.Value.Name == name)
                    {
                        if (inv.Value.Type != type)
                            return UUID.Zero;

                        return inv.Value.AssetID;
                    }
                }
            }

            return UUID.Zero;
        }

        protected UUID InventoryKey(string name)
        {
            

            lock (m_host.TaskInventory)
            {
                foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory)
                {
                    if (inv.Value.Name == name)
                    {
                        return inv.Value.AssetID;
                    }
                }
            }

            return UUID.Zero;
        }


        /// <summary>
        /// accepts a valid UUID, -or- a name of an inventory item.
        /// Returns a valid UUID or UUID.Zero if key invalid and item not found
        /// in prim inventory.
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        protected UUID KeyOrName(string k)
        {
            UUID key = UUID.Zero;

            // if we can parse the string as a key, use it.
            if (UUID.TryParse(k, out key))
            {
                return key;
            }
            // else try to locate the name in inventory of object. found returns key,
            // not found returns UUID.Zero which will translate to the default particle texture
            else
            {
                return InventoryKey(k);
            }
        }

        // convert a LSL_Rotation to a Quaternion
        protected Quaternion Rot2Quaternion(LSL_Rotation r)
        {
            Quaternion q = new Quaternion((float)r.x, (float)r.y, (float)r.z, (float)r.s);
            q.Normalize();
            return q;
        }

        //These are the implementations of the various ll-functions used by the LSL scripts.
        public LSL_Float llSin(double f)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return (double)Math.Sin(f);
        }

        public LSL_Float llCos(double f)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return (double)Math.Cos(f);
        }

        public LSL_Float llTan(double f)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return (double)Math.Tan(f);
        }

        public LSL_Float llAtan2(double x, double y)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return (double)Math.Atan2(x, y);
        }

        public LSL_Float llSqrt(double f)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return (double)Math.Sqrt(f);
        }

        public LSL_Float llPow(double fbase, double fexponent)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return (double)Math.Pow(fbase, fexponent);
        }

        public LSL_Integer llAbs(int i)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            // changed to replicate LSL behaviour whereby minimum int value is returned untouched.
            
            if (i == Int32.MinValue)
                return i;
            else
                return (int)Math.Abs(i);
        }

        public LSL_Float llFabs(double f)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return (double)Math.Abs(f);
        }

        public LSL_Float llFrand(double mag)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            lock (Util.RandomClass)
            {
                return Util.RandomClass.NextDouble() * mag;
            }
        }

        public LSL_Integer llFloor(double f)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return (int)Math.Floor(f);
        }

        public LSL_Integer llCeil(double f)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return (int)Math.Ceiling(f);
        }

        // Xantor 01/May/2008 fixed midpointrounding (2.5 becomes 3.0 instead of 2.0, default = ToEven)
        public LSL_Integer llRound(double f)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            double RoundedNumber = Math.Round(f, MidpointRounding.AwayFromZero);
            //Attempt to fix rounded numbers like -4.5 arounding away from zero
            if (f < 0)
            {
                if (f + 0.5 == RoundedNumber || f - 0.5 == RoundedNumber)
                {
                    RoundedNumber += 1;
                }
            }
            return (int)RoundedNumber;
        }

        //This next group are vector operations involving squaring and square root. ckrinke
        public LSL_Float llVecMag(LSL_Vector v)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return LSL_Vector.Mag(v);
        }

        public LSL_Vector llVecNorm(LSL_Vector v)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            return LSL_Vector.Norm(v);
        }

        public LSL_Float llVecDist(LSL_Vector a, LSL_Vector b)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            double dx = a.x - b.x;
            double dy = a.y - b.y;
            double dz = a.z - b.z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        //Now we start getting into quaternions which means sin/cos, matrices and vectors. ckrinke

        // Old implementation of llRot2Euler. Normalization not required as Atan2 function will
        // only return values >= -PI (-180 degrees) and <= PI (180 degrees).

        public LSL_Vector llRot2Euler(LSL_Rotation r)
        {
            //This implementation is from http://lslwiki.net/lslwiki/wakka.php?wakka=LibraryRotationFunctions. ckrinke
            LSL_Rotation t = new LSL_Rotation(r.x * r.x, r.y * r.y, r.z * r.z, r.s * r.s);
            double m = (t.x + t.y + t.z + t.s);
            if (m == 0) return new LSL_Vector();
            double n = 2 * (r.y * r.s + r.x * r.z);
            double p = m * m - n * n;
            if (p > 0)
                return new LSL_Vector(Math.Atan2(2.0 * (r.x * r.s - r.y * r.z), (-t.x - t.y + t.z + t.s)),
                                             Math.Atan2(n, Math.Sqrt(p)),
                                             Math.Atan2(2.0 * (r.z * r.s - r.x * r.y), (t.x - t.y - t.z + t.s)));
            else if (n > 0)
                return new LSL_Vector(0.0, Math.PI * 0.5, Math.Atan2((r.z * r.s + r.x * r.y), 0.5 - t.x - t.z));
            else
                return new LSL_Vector(0.0, -Math.PI * 0.5, Math.Atan2((r.z * r.s + r.x * r.y), 0.5 - t.x - t.z));
        }

        /* From wiki:
        The Euler angle vector (in radians) is converted to a rotation by doing the rotations around the 3 axes
        in Z, Y, X order. So llEuler2Rot(<1.0, 2.0, 3.0> * DEG_TO_RAD) generates a rotation by taking the zero rotation,
        a vector pointing along the X axis, first rotating it 3 degrees around the global Z axis, then rotating the resulting
        vector 2 degrees around the global Y axis, and finally rotating that 1 degree around the global X axis.
        */

        /* How we arrived at this llEuler2Rot
         *
         * Experiment in SL to determine conventions:
         *   llEuler2Rot(<PI,0,0>)=<1,0,0,0>
         *   llEuler2Rot(<0,PI,0>)=<0,1,0,0>
         *   llEuler2Rot(<0,0,PI>)=<0,0,1,0>
         *
         * Important facts about Quaternions
         *  - multiplication is non-commutative (a*b != b*a)
         *  - http://en.wikipedia.org/wiki/Quaternion#Basis_multiplication
         *
         * Above SL experiment gives (c1,c2,c3,s1,s2,s3 as defined in our llEuler2Rot):
         *   Qx = c1+i*s1
         *   Qy = c2+j*s2;
         *   Qz = c3+k*s3;
         *
         * Rotations applied in order (from above) Z, Y, X
         * Q = (Qz * Qy) * Qx
         * ((c1+i*s1)*(c2+j*s2))*(c3+k*s3)
         * (c1*c2+i*s1*c2+j*c1*s2+ij*s1*s2)*(c3+k*s3)
         * (c1*c2+i*s1*c2+j*c1*s2+k*s1*s2)*(c3+k*s3)
         * c1*c2*c3+i*s1*c2*c3+j*c1*s2*c3+k*s1*s2*c3+k*c1*c2*s3+ik*s1*c2*s3+jk*c1*s2*s3+kk*s1*s2*s3
         * c1*c2*c3+i*s1*c2*c3+j*c1*s2*c3+k*s1*s2*c3+k*c1*c2*s3 -j*s1*c2*s3 +i*c1*s2*s3   -s1*s2*s3
         * regroup: x=i*(s1*c2*c3+c1*s2*s3)
         *          y=j*(c1*s2*c3-s1*c2*s3)
         *          z=k*(s1*s2*c3+c1*c2*s3)
         *          s=   c1*c2*c3-s1*s2*s3
         *
         * This implementation agrees with the functions found here:
         * http://lslwiki.net/lslwiki/wakka.php?wakka=LibraryRotationFunctions
         * And with the results in SL.
         *
         * It's also possible to calculate llEuler2Rot by direct multiplication of
         * the Qz, Qy, and Qx vectors (as above - and done in the "accurate" function
         * from the wiki).
         * Apparently in some cases this is better from a numerical precision perspective?
         */

        public LSL_Rotation llEuler2Rot(LSL_Vector v)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            double x,y,z,s;

            double c1 = Math.Cos(v.x * 0.5);
            double c2 = Math.Cos(v.y * 0.5);
            double c3 = Math.Cos(v.z * 0.5);
            double s1 = Math.Sin(v.x * 0.5);
            double s2 = Math.Sin(v.y * 0.5);
            double s3 = Math.Sin(v.z * 0.5);

            x = s1 * c2 * c3 + c1 * s2 * s3;
            y = c1 * s2 * c3 - s1 * c2 * s3;
            z = s1 * s2 * c3 + c1 * c2 * s3;
            s = c1 * c2 * c3 - s1 * s2 * s3;

            return new LSL_Rotation(x, y, z, s);
        }

        public LSL_Rotation llAxes2Rot(LSL_Vector fwd, LSL_Vector left, LSL_Vector up)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            double s;
            double tr = fwd.x + left.y + up.z + 1.0;

            if (tr >= 1.0)
            {
                s = 0.5 / Math.Sqrt(tr);
                return new LSL_Rotation(
                        (left.z - up.y) * s,
                        (up.x - fwd.z) * s,
                        (fwd.y - left.x) * s,
                        0.25 / s);
            }
            else
            {
                double max = (left.y > up.z) ? left.y : up.z;

                if (max < fwd.x)
                {
                    s = Math.Sqrt(fwd.x - (left.y + up.z) + 1.0);
                    double x = s * 0.5;
                    s = 0.5 / s;
                    return new LSL_Rotation(
                            x,
                            (fwd.y + left.x) * s,
                            (up.x + fwd.z) * s,
                            (left.z - up.y) * s);
                }
                else if (max == left.y)
                {
                    s = Math.Sqrt(left.y - (up.z + fwd.x) + 1.0);
                    double y = s * 0.5;
                    s = 0.5 / s;
                    return new LSL_Rotation(
                            (fwd.y + left.x) * s,
                            y,
                            (left.z + up.y) * s,
                            (up.x - fwd.z) * s);
                }
                else
                {
                    s = Math.Sqrt(up.z - (fwd.x + left.y) + 1.0);
                    double z = s * 0.5;
                    s = 0.5 / s;
                    return new LSL_Rotation(
                            (up.x + fwd.z) * s,
                            (left.z + up.y) * s,
                            z,
                            (fwd.y - left.x) * s);
                }
            }
        }

        public LSL_Vector llRot2Fwd(LSL_Rotation r)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            double x, y, z, m;

            m = r.x * r.x + r.y * r.y + r.z * r.z + r.s * r.s;
            // m is always greater than zero
            // if m is not equal to 1 then Rotation needs to be normalized
            if (Math.Abs(1.0 - m) > 0.000001) // allow a little slop here for calculation precision
            {
                m = 1.0 / Math.Sqrt(m);
                r.x *= m;
                r.y *= m;
                r.z *= m;
                r.s *= m;
            }

            // Fast Algebric Calculations instead of Vectors & Quaternions Product
            x = r.x * r.x - r.y * r.y - r.z * r.z + r.s * r.s;
            y = 2 * (r.x * r.y + r.z * r.s);
            z = 2 * (r.x * r.z - r.y * r.s);
            return (new LSL_Vector(x, y, z));
        }

        public LSL_Vector llRot2Left(LSL_Rotation r)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            double x, y, z, m;

            m = r.x * r.x + r.y * r.y + r.z * r.z + r.s * r.s;
            // m is always greater than zero
            // if m is not equal to 1 then Rotation needs to be normalized
            if (Math.Abs(1.0 - m) > 0.000001) // allow a little slop here for calculation precision
            {
                m = 1.0 / Math.Sqrt(m);
                r.x *= m;
                r.y *= m;
                r.z *= m;
                r.s *= m;
            }

            // Fast Algebric Calculations instead of Vectors & Quaternions Product
            x = 2 * (r.x * r.y - r.z * r.s);
            y = -r.x * r.x + r.y * r.y - r.z * r.z + r.s * r.s;
            z = 2 * (r.x * r.s + r.y * r.z);
            return (new LSL_Vector(x, y, z));
        }

        public LSL_Vector llRot2Up(LSL_Rotation r)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            double x, y, z, m;

            m = r.x * r.x + r.y * r.y + r.z * r.z + r.s * r.s;
            // m is always greater than zero
            // if m is not equal to 1 then Rotation needs to be normalized
            if (Math.Abs(1.0 - m) > 0.000001) // allow a little slop here for calculation precision
            {
                m = 1.0 / Math.Sqrt(m);
                r.x *= m;
                r.y *= m;
                r.z *= m;
                r.s *= m;
            }

            // Fast Algebric Calculations instead of Vectors & Quaternions Product
            x = 2 * (r.x * r.z + r.y * r.s);
            y = 2 * (-r.x * r.s + r.y * r.z);
            z = -r.x * r.x - r.y * r.y + r.z * r.z + r.s * r.s;
            return (new LSL_Vector(x, y, z));
        }

        public LSL_Rotation llRotBetween(LSL_Vector a, LSL_Vector b)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            //A and B should both be normalized
            
            LSL_Rotation rotBetween;
            // Check for zero vectors. If either is zero, return zero rotation. Otherwise,
            // continue calculation.
            if (a == new LSL_Vector(0.0f, 0.0f, 0.0f) || b == new LSL_Vector(0.0f, 0.0f, 0.0f))
            {
                rotBetween = new LSL_Rotation(0.0f, 0.0f, 0.0f, 1.0f);
            }
            else
            {
                a = LSL_Vector.Norm(a);
                b = LSL_Vector.Norm(b);
                double dotProduct = LSL_Vector.Dot(a, b);
                // There are two degenerate cases possible. These are for vectors 180 or
                // 0 degrees apart. These have to be detected and handled individually.
                //
                // Check for vectors 180 degrees apart.
                // A dot product of -1 would mean the angle between vectors is 180 degrees.
                if (dotProduct < -0.9999999f)
                {
                    // First assume X axis is orthogonal to the vectors.
                    LSL_Vector orthoVector = new LSL_Vector(1.0f, 0.0f, 0.0f);
                    orthoVector = orthoVector - a * (a.x / LSL_Vector.Dot(a, a));
                    // Check for near zero vector. A very small non-zero number here will create
                    // a rotation in an undesired direction.
                    if (LSL_Vector.Mag(orthoVector) > 0.0001)
                    {
                        rotBetween = new LSL_Rotation(orthoVector.x, orthoVector.y, orthoVector.z, 0.0f);
                    }
                    // If the magnitude of the vector was near zero, then assume the X axis is not
                    // orthogonal and use the Z axis instead.
                    else
                    {
                        // Set 180 z rotation.
                        rotBetween = new LSL_Rotation(0.0f, 0.0f, 1.0f, 0.0f);
                    }
                }
                // Check for parallel vectors.
                // A dot product of 1 would mean the angle between vectors is 0 degrees.
                else if (dotProduct > 0.9999999f)
                {
                    // Set zero rotation.
                    rotBetween = new LSL_Rotation(0.0f, 0.0f, 0.0f, 1.0f);
                }
                else
                {
                    // All special checks have been performed so get the axis of rotation.
                    LSL_Vector crossProduct = LSL_Vector.Cross(a, b);
                    // Quarternion s value is the length of the unit vector + dot product.
                    double qs = 1.0 + dotProduct;
                    rotBetween = new LSL_Rotation(crossProduct.x, crossProduct.y, crossProduct.z, qs);
                    // Normalize the rotation.
                    double mag = LSL_Rotation.Mag(rotBetween);
                    // We shouldn't have to worry about a divide by zero here. The qs value will be
                    // non-zero because we already know if we're here, then the dotProduct is not -1 so
                    // qs will not be zero. Also, we've already handled the input vectors being zero so the
                    // crossProduct vector should also not be zero.
                    rotBetween.x = rotBetween.x / mag;
                    rotBetween.y = rotBetween.y / mag;
                    rotBetween.z = rotBetween.z / mag;
                    rotBetween.s = rotBetween.s / mag;
                    // Check for undefined values and set zero rotation if any found. This code might not actually be required
                    // any longer since zero vectors are checked for at the top.
                    if (Double.IsNaN(rotBetween.x) || Double.IsNaN(rotBetween.y) || Double.IsNaN(rotBetween.z) || Double.IsNaN(rotBetween.s))
                    {
                        rotBetween = new LSL_Rotation(0.0f, 0.0f, 0.0f, 1.0f);
                    }
                }
            }
            return rotBetween;
        }

        public void llWhisper(int channelID, string text)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            if (text.Length > 1023)
                text = text.Substring(0, 1023);

            IChatModule chatModule = World.RequestModuleInterface<IChatModule>();
            if (chatModule != null)
                chatModule.SimChat(text, ChatTypeEnum.Whisper, channelID,
                    m_host.ParentEntity.RootChild.AbsolutePosition, m_host.Name, m_host.UUID, false, World);

            if (m_comms != null)
                m_comms.DeliverMessage(ChatTypeEnum.Whisper, channelID, m_host.Name, m_host.UUID, text);
        }

        public void llSay(int channelID, object m_text)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            string text = m_text.ToString();

            if (m_scriptConsoleChannelEnabled && (channelID == m_scriptConsoleChannel))
            {
                Console.WriteLine(text);
            }
            else
            {
                if (text.Length > 1023)
                    text = text.Substring(0, 1023);

                IChatModule chatModule = World.RequestModuleInterface<IChatModule>();
                if (chatModule != null)
                    chatModule.SimChat(text, ChatTypeEnum.Say, channelID,
                        m_host.ParentEntity.RootChild.AbsolutePosition, m_host.Name, m_host.UUID, false, World);

                if (m_comms != null)
                    m_comms.DeliverMessage(ChatTypeEnum.Say, channelID, m_host.Name, m_host.UUID, text);
            }
        }

        public void llShout(int channelID, string text)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            if (text.Length > 1023)
                text = text.Substring(0, 1023);

            IChatModule chatModule = World.RequestModuleInterface<IChatModule>();
            if (chatModule != null)
                chatModule.SimChat(text, ChatTypeEnum.Shout, channelID,
                    m_host.ParentEntity.RootChild.AbsolutePosition, m_host.Name, m_host.UUID, true, World);

            if (m_comms != null)
                m_comms.DeliverMessage(ChatTypeEnum.Shout, channelID, m_host.Name, m_host.UUID, text);
        }

        public void llRegionSay(int channelID, string text)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            if (channelID == 0)
            {
                ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "LSL", m_host, "LSL");
            	if (text.Length > 1023)
                text = text.Substring(0, 1023);

            	

            	if (m_comms != null)
                    m_comms.DeliverMessage(ChatTypeEnum.Region, channelID, m_host.Name, m_host.UUID, text);
            }

            if (text.Length > 1023)
                text = text.Substring(0, 1023);

            

            if (m_comms != null)
                m_comms.DeliverMessage(ChatTypeEnum.Region, channelID, m_host.Name, m_host.UUID, text);
        }

        public LSL_Integer llListen(int channelID, string name, string ID, string msg)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            UUID keyID;
            UUID.TryParse(ID, out keyID);
            if (m_comms != null)
                return m_comms.Listen(m_itemID, m_host.UUID, channelID, name, keyID, msg);
            else
                return -1;
        }

        public void llListenControl(int number, int active)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            if (m_comms != null)
                m_comms.ListenControl(m_itemID, number, active);
        }

        public void llListenRemove(int number)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            if (m_comms != null)
                m_comms.ListenRemove(m_itemID, number);
        }

        public void llSensor(string name, string id, int type, double range, double arc)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            UUID keyID = UUID.Zero;
            UUID.TryParse(id, out keyID);
            SensorRepeatPlugin sensorPlugin = (SensorRepeatPlugin)m_ScriptEngine.GetScriptPlugin("SensorRepeat");
            sensorPlugin.SenseOnce(m_host.UUID, m_itemID, name, keyID, type, range, arc, m_host);
       }

        public void llSensorRepeat(string name, string id, int type, double range, double arc, double rate)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            UUID keyID = UUID.Zero;
            UUID.TryParse(id, out keyID);

            SensorRepeatPlugin sensorPlugin = (SensorRepeatPlugin)m_ScriptEngine.GetScriptPlugin("SensorRepeat");
            sensorPlugin.SetSenseRepeatEvent(m_host.UUID, m_itemID, name, keyID, type, range, arc, rate, m_host);
        }

        public void llSensorRemove()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            SensorRepeatPlugin sensorPlugin = (SensorRepeatPlugin)m_ScriptEngine.GetScriptPlugin("SensorRepeat");
            sensorPlugin.RemoveScript(m_host.UUID, m_itemID);
        }

        public string resolveName(UUID objecUUID)
        {
            // try avatar username surname
            UserAccount account = World.UserAccountService.GetUserAccount(World.RegionInfo.ScopeID, objecUUID);
            if (account != null)
                return account.Name;

            // try an scene object
            ISceneChildEntity SOP = World.GetSceneObjectPart (objecUUID);
            if (SOP != null)
                return SOP.Name;

            IEntity SensedObject;
            if(!World.Entities.TryGetValue(objecUUID, out SensedObject))
            {
                IGroupsModule groups = World.RequestModuleInterface<IGroupsModule>();
                if (groups != null)
                {
                    GroupRecord gr = groups.GetGroupRecord(objecUUID);
                    if (gr != null)
                        return gr.GroupName;
                }
                return String.Empty;
            }

            return SensedObject.Name;
        }

        public LSL_String llDetectedName(int number)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, number);
            if (detectedParams == null)
                return String.Empty;
            return detectedParams.Name;
        }

        public LSL_String llDetectedKey(int number)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, number);
            if (detectedParams == null)
                return String.Empty;
            return detectedParams.Key.ToString();
        }

        public LSL_String llDetectedOwner(int number)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, number);
            if (detectedParams == null)
                return String.Empty;
            return detectedParams.Owner.ToString();
        }

        public LSL_Integer llDetectedType(int number)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, number);
            if (detectedParams == null)
                return 0;
            return new LSL_Integer(detectedParams.Type);
        }

        public LSL_Vector llDetectedPos(int number)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, number);
            if (detectedParams == null)
                return new LSL_Vector();
            return detectedParams.Position;
        }

        public LSL_Vector llDetectedVel(int number)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, number);
            if (detectedParams == null)
                return new LSL_Vector();
            return detectedParams.Velocity;
        }

        public LSL_Vector llDetectedGrab(int number)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            DetectParams parms = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, number);
            if (parms == null)
                return new LSL_Vector(0, 0, 0);

            return parms.OffsetPos;
        }

        public LSL_Rotation llDetectedRot(int number)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, number);
            if (detectedParams == null)
                return new LSL_Rotation();
            return detectedParams.Rotation;
        }

        public LSL_Integer llDetectedGroup(int number)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, number);
            if (detectedParams == null)
                return new LSL_Integer(0);
            if (m_host.GroupID == detectedParams.Group)
                return new LSL_Integer(1);
            return new LSL_Integer(0);
        }

        public LSL_Integer llDetectedLinkNumber(int number)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            DetectParams parms = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, number);
            if (parms == null)
                return new LSL_Integer(0);

            return new LSL_Integer(parms.LinkNum);
        }

        /// <summary>
        /// See http://wiki.secondlife.com/wiki/LlDetectedTouchBinormal for details
        /// </summary>
        public LSL_Vector llDetectedTouchBinormal(int index)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, index);
            if (detectedParams == null)
                return new LSL_Vector();
            return detectedParams.TouchBinormal;
        }

        /// <summary>
        /// See http://wiki.secondlife.com/wiki/LlDetectedTouchFace for details
        /// </summary>
        public LSL_Integer llDetectedTouchFace(int index)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, index);
            if (detectedParams == null)
                return new LSL_Integer(-1);
            return new LSL_Integer(detectedParams.TouchFace);
        }

        /// <summary>
        /// See http://wiki.secondlife.com/wiki/LlDetectedTouchNormal for details
        /// </summary>
        public LSL_Vector llDetectedTouchNormal(int index)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, index);
            if (detectedParams == null)
                return new LSL_Vector();
            return detectedParams.TouchNormal;
        }

        /// <summary>
        /// See http://wiki.secondlife.com/wiki/LlDetectedTouchPos for details
        /// </summary>
        public LSL_Vector llDetectedTouchPos(int index)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, index);
            if (detectedParams == null)
                return new LSL_Vector();
            return detectedParams.TouchPos;
        }

        /// <summary>
        /// See http://wiki.secondlife.com/wiki/LlDetectedTouchST for details
        /// </summary>
        public LSL_Vector llDetectedTouchST(int index)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, index);
            if (detectedParams == null)
                return new LSL_Vector(-1.0, -1.0, 0.0);
            return detectedParams.TouchST;
        }

        /// <summary>
        /// See http://wiki.secondlife.com/wiki/LlDetectedTouchUV for details
        /// </summary>
        public LSL_Vector llDetectedTouchUV(int index)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, index);
            if (detectedParams == null)
                return new LSL_Vector(-1.0, -1.0, 0.0);
            return detectedParams.TouchUV;
        }

        public virtual void llDie()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            throw new SelfDeleteException();
        }

        public LSL_Float llGround(LSL_Vector offset)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            Vector3 pos = m_host.GetWorldPosition() + new Vector3((float)offset.x,
                                                                  (float)offset.y,
                                                                  (float)offset.z);

            //Get the slope normal.  This gives us the equation of the plane tangent to the slope.
            LSL_Vector vsn = llGroundNormal(offset);
            ITerrainChannel heightmap = World.RequestModuleInterface<ITerrainChannel>();
            // Clamp to valid position
            if (pos.X < 0)
                pos.X = 0;
            else if (pos.X >= heightmap.Width)
                pos.X = heightmap.Width - 1;
            if (pos.Y < 0)
                pos.Y = 0;
            else if (pos.Y >= heightmap.Height)
                pos.Y = heightmap.Height - 1;

            //Get the height for the integer coordinates from the Heightmap
            float baseheight = heightmap[(int)pos.X, (int)pos.Y];

            //Calculate the difference between the actual coordinates and the integer coordinates
            float xdiff = pos.X - (float)((int)pos.X);
            float ydiff = pos.Y - (float)((int)pos.Y);

            //Use the equation of the tangent plane to adjust the height to account for slope

            return (((vsn.x * xdiff) + (vsn.y * ydiff)) / (-1 * vsn.z)) + baseheight;
        }

        public LSL_Float llCloud(LSL_Vector offset)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            float cloudCover = 0f;
            ICloudModule module = World.RequestModuleInterface<ICloudModule>();
            if (module != null)
            {
                Vector3 pos = m_host.GetWorldPosition();
                int x = (int)(pos.X + offset.x);
                int y = (int)(pos.Y + offset.y);

                cloudCover = module.CloudCover(x, y, 0);

            }
            return cloudCover;
        }

        public LSL_Vector llWind(LSL_Vector offset)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            LSL_Vector wind = new LSL_Vector(0, 0, 0);
            IWindModule module = World.RequestModuleInterface<IWindModule>();
            if (module != null)
            {
                Vector3 pos = m_host.GetWorldPosition();
                int x = (int)(pos.X + offset.x);
                int y = (int)(pos.Y + offset.y);

                Vector3 windSpeed = module.WindSpeed(x, y, 0);

                wind.x = windSpeed.X;
                wind.y = windSpeed.Y;
            }
            return wind;
        }

        public void llSetStatus(int status, int value)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            int statusrotationaxis = 0;

            if ((status & ScriptBaseClass.STATUS_PHYSICS) == ScriptBaseClass.STATUS_PHYSICS)
            {
                if (value != 0)
                {
                    ISceneEntity group = m_host.ParentEntity;
                    if (group == null)
                        return;
                    bool allow = true;
                    foreach (ISceneChildEntity part in group.ChildrenEntities())
                    {
                        IOpenRegionSettingsModule WSModule = group.Scene.RequestModuleInterface<IOpenRegionSettingsModule>();
                        if (WSModule != null)
                        {
                            Vector3 tmp = part.Scale;
                            if (tmp.X > WSModule.MaximumPhysPrimScale ||
                                tmp.Y > WSModule.MaximumPhysPrimScale ||
                                tmp.Z > WSModule.MaximumPhysPrimScale)
                            {
                                allow = false;
                                break;
                            }
                        }
                    }

                    if (!allow)
                        return;
                    m_host.ScriptSetPhysicsStatus(true);
                }
                else
                    m_host.ScriptSetPhysicsStatus(false);
            }

            if ((status & ScriptBaseClass.STATUS_PHANTOM) == ScriptBaseClass.STATUS_PHANTOM)
            {
                if (value != 0)
                    m_host.ScriptSetPhantomStatus(true);
                else
                    m_host.ScriptSetPhantomStatus(false);
            }

            if ((status & ScriptBaseClass.STATUS_CAST_SHADOWS) == ScriptBaseClass.STATUS_CAST_SHADOWS)
            {
                m_host.AddFlag(PrimFlags.CastShadows);
            }

            if ((status & ScriptBaseClass.STATUS_ROTATE_X) == ScriptBaseClass.STATUS_ROTATE_X)
            {
                statusrotationaxis |= ScriptBaseClass.STATUS_ROTATE_X;
            }

            if ((status & ScriptBaseClass.STATUS_ROTATE_Y) == ScriptBaseClass.STATUS_ROTATE_Y)
            {
                statusrotationaxis |= ScriptBaseClass.STATUS_ROTATE_Y;
            }

            if ((status & ScriptBaseClass.STATUS_ROTATE_Z) == ScriptBaseClass.STATUS_ROTATE_Z)
            {
                statusrotationaxis |= ScriptBaseClass.STATUS_ROTATE_Z;
            }

            if ((status & ScriptBaseClass.STATUS_BLOCK_GRAB) == ScriptBaseClass.STATUS_BLOCK_GRAB)
            {
                if (value != 0)
                    m_host.SetBlockGrab(true);
                else
                    m_host.SetBlockGrab(false);
            }

            if ((status & ScriptBaseClass.STATUS_DIE_AT_EDGE) == ScriptBaseClass.STATUS_DIE_AT_EDGE)
            {
                if (value != 0)
                    m_host.SetDieAtEdge(true);
                else
                    m_host.SetDieAtEdge(false);
            }

            if ((status & ScriptBaseClass.STATUS_RETURN_AT_EDGE) == ScriptBaseClass.STATUS_RETURN_AT_EDGE)
            {
                if (value != 0)
                    m_host.SetReturnAtEdge(true);
                else
                    m_host.SetReturnAtEdge(false);
            }

            if ((status & ScriptBaseClass.STATUS_SANDBOX) == ScriptBaseClass.STATUS_SANDBOX)
            {
                if (value != 0)
                    m_host.SetStatusSandbox(true);
                else
                    m_host.SetStatusSandbox(false);
            }

            if (statusrotationaxis != 0)
            {
                m_host.SetAxisRotation(statusrotationaxis, value);
            }
        }

        public LSL_Integer llGetStatus(int status)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            // m_log.Debug(m_host.ToString() + " status is " + m_host.GetEffectiveObjectFlags().ToString());
            if (status == ScriptBaseClass.STATUS_PHYSICS)
            {
                if ((m_host.GetEffectiveObjectFlags() & (uint)PrimFlags.Physics) == (uint)PrimFlags.Physics)
                {
                    return 1;
                }
                return 0;
            }

            else if (status == ScriptBaseClass.STATUS_PHANTOM)
            {
                if ((m_host.GetEffectiveObjectFlags() & (uint)PrimFlags.Phantom) == (uint)PrimFlags.Phantom)
                {
                    return 1;
                }
                return 0;
            }

            else if (status == ScriptBaseClass.STATUS_CAST_SHADOWS)
            {
                if ((m_host.GetEffectiveObjectFlags() & (uint)PrimFlags.CastShadows) == (uint)PrimFlags.CastShadows)
                {
                    return 1;
                }
                return 0;
            }

            else if (status == ScriptBaseClass.STATUS_BLOCK_GRAB)
            {
                if (m_host.GetBlockGrab())
                    return 1;
                else
                    return 0;
            }

            else if (status == ScriptBaseClass.STATUS_DIE_AT_EDGE)
            {
                if (m_host.GetDieAtEdge())
                    return 1;
                else
                    return 0;
            }

            else if (status == ScriptBaseClass.STATUS_RETURN_AT_EDGE)
            {
                if (m_host.GetReturnAtEdge())
                    return 1;
                else
                    return 0;
            }

            else if (status == ScriptBaseClass.STATUS_ROTATE_X)
            {
                if (m_host.GetAxisRotation(2) == 2)
                    return 1;
                else
                    return 0;
            }

            else if (status == ScriptBaseClass.STATUS_ROTATE_Y)
            {
                if (m_host.GetAxisRotation(4) == 4)
                    return 1;
                else
                    return 0;
            }

            else if (status == ScriptBaseClass.STATUS_ROTATE_Z)
            {
                if (m_host.GetAxisRotation(8) == 8)
                    return 1;
                else
                    return 0;
            }

            else if (status == ScriptBaseClass.STATUS_SANDBOX)
            {
                if (m_host.GetStatusSandbox())
                    return 1;
                else
                    return 0;
            }
            return 0;
        }

        public void llSetScale(LSL_Vector scale)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            SetScale(m_host, scale);
        }

        protected void SetScale(ISceneChildEntity part, LSL_Vector scale)
        {
            if (part == null || part.ParentEntity == null || part.ParentEntity.IsDeleted)
                return;

            IOpenRegionSettingsModule WSModule = m_host.ParentEntity.Scene.RequestModuleInterface<IOpenRegionSettingsModule> ();
            if (WSModule != null)
            {
                if (WSModule.MinimumPrimScale != -1)
                {
                    if (scale.x < WSModule.MinimumPrimScale)
                        scale.x = WSModule.MinimumPrimScale;
                    if (scale.y < WSModule.MinimumPrimScale)
                        scale.y = WSModule.MinimumPrimScale;
                    if (scale.z < WSModule.MinimumPrimScale)
                        scale.z = WSModule.MinimumPrimScale;
                }

                if (part.ParentEntity.RootChild.PhysActor != null && part.ParentEntity.RootChild.PhysActor.IsPhysical &&
                    WSModule.MaximumPhysPrimScale != -1)
                {
                    if (scale.x > WSModule.MaximumPhysPrimScale)
                        scale.x = WSModule.MaximumPhysPrimScale;
                    if (scale.y > WSModule.MaximumPhysPrimScale)
                        scale.y = WSModule.MaximumPhysPrimScale;
                    if (scale.z > WSModule.MaximumPhysPrimScale)
                        scale.z = WSModule.MaximumPhysPrimScale;
                }

                if (WSModule.MaximumPrimScale != -1)
                {
                    if (scale.x > WSModule.MaximumPrimScale)
                        scale.x = WSModule.MaximumPrimScale;
                    if (scale.y > WSModule.MaximumPrimScale)
                        scale.y = WSModule.MaximumPrimScale;
                    if (scale.z > WSModule.MaximumPrimScale)
                        scale.z = WSModule.MaximumPrimScale;
                }
            }

            Vector3 tmp = part.Scale;
            tmp.X = (float)scale.x;
            tmp.Y = (float)scale.y;
            tmp.Z = (float)scale.z;
            part.Scale = tmp;
            part.ScheduleUpdate(PrimUpdateFlags.FindBest);
            part.ParentEntity.HasGroupChanged = true;
        }

        public LSL_Vector llGetScale()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            Vector3 tmp = m_host.Scale;
            return new LSL_Vector(tmp.X, tmp.Y, tmp.Z);
        }

        public void llSetClickAction(int action)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.ClickAction = (byte)action;
            if (m_host.ParentEntity != null) 
                m_host.ParentEntity.HasGroupChanged = true;
            m_host.ScheduleUpdate(PrimUpdateFlags.FindBest);
        }

        public void llSetColor(LSL_Vector color, int face)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            if (face == ScriptBaseClass.ALL_SIDES)
                face = SceneObjectPart.ALL_SIDES;
            
            m_host.SetFaceColor(new Vector3((float)color.x, (float)color.y, (float)color.z), face);
        }

        public void SetTexGen(SceneObjectPart part, int face,int style)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            MappingType textype;
            textype = MappingType.Default;
            if (style == (int)ScriptBaseClass.PRIM_TEXGEN_PLANAR)
                textype = MappingType.Planar;

            if (face >= 0 && face < GetNumberOfSides(part))
            {
                tex.CreateFace((uint) face);
                tex.FaceTextures[face].TexMapType = textype;
                part.UpdateTexture(tex);
                return;
            }
            else if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (uint i = 0; i < GetNumberOfSides(part); i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].TexMapType = textype;
                    }
                    tex.DefaultTexture.TexMapType = textype;
                }
                part.UpdateTexture(tex);
                return;
            }
        }

        public void SetGlow(SceneObjectPart part, int face, float glow)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            if (face >= 0 && face < GetNumberOfSides(part))
            {
                tex.CreateFace((uint) face);
                tex.FaceTextures[face].Glow = glow;
                part.UpdateTexture(tex);
                return;
            }
            else if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (uint i = 0; i < GetNumberOfSides(part); i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].Glow = glow;
                    }
                    tex.DefaultTexture.Glow = glow;
                }
                part.UpdateTexture(tex);
                return;
            }
        }

        public void SetShiny(SceneObjectPart part, int face, int shiny, Bumpiness bump)
        {

            Shininess sval = new Shininess();

            switch (shiny)
            {
            case 0:
                sval = Shininess.None;
                break;
            case 1:
                sval = Shininess.Low;
                break;
            case 2:
                sval = Shininess.Medium;
                break;
            case 3:
                sval = Shininess.High;
                break;
            default:
                sval = Shininess.None;
                break;
            }

            Primitive.TextureEntry tex = part.Shape.Textures;
            if (face >= 0 && face < GetNumberOfSides(part))
            {
                tex.CreateFace((uint) face);
                tex.FaceTextures[face].Shiny = sval;
                tex.FaceTextures[face].Bump = bump;
                part.UpdateTexture(tex);
                return;
            }
            else if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (uint i = 0; i < GetNumberOfSides(part); i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].Shiny = sval;
                        tex.FaceTextures[i].Bump = bump;;
                    }
                    tex.DefaultTexture.Shiny = sval;
                    tex.DefaultTexture.Bump = bump;
                }
                part.UpdateTexture(tex);
                return;
            }
        }

        public void SetFullBright(SceneObjectPart part, int face, bool bright)
        {
             Primitive.TextureEntry tex = part.Shape.Textures;
             if (face >= 0 && face < GetNumberOfSides(part))
             {
                 tex.CreateFace((uint) face);
                 tex.FaceTextures[face].Fullbright = bright;
                 part.UpdateTexture(tex);
                 return;
             }
             else if (face == ScriptBaseClass.ALL_SIDES)
             {
                 for (uint i = 0; i < GetNumberOfSides(part); i++)
                 {
                     if (tex.FaceTextures[i] != null)
                     {
                         tex.FaceTextures[i].Fullbright = bright;
                     }
                 }
                 tex.DefaultTexture.Fullbright = bright;
                 part.UpdateTexture(tex);
                 return;
             }
         }

        public LSL_Float llGetAlpha(int face)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            return GetAlpha(m_host, face);
        }

        protected LSL_Float GetAlpha (ISceneChildEntity part, int face)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            if (face == ScriptBaseClass.ALL_SIDES)
            {
                int i;
                double sum = 0.0;
                for (i = 0 ; i < GetNumberOfSides(part); i++)
                    sum += (double)tex.GetFace((uint)i).RGBA.A;
                return sum;
            }
            if (face >= 0 && face < GetNumberOfSides(part))
            {
                return (double)tex.GetFace((uint)face).RGBA.A;
            }
            return 0.0;
        }

        public void llSetAlpha(double alpha, int face)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            SetAlpha(m_host, alpha, face);
        }

        public void llSetLinkAlpha(int linknumber, double alpha, int face)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");


            List<ISceneChildEntity> parts = GetLinkParts (linknumber);

            foreach (ISceneChildEntity part in parts)
                SetAlpha(part, alpha, face);
        }

        protected void SetAlpha (ISceneChildEntity part, double alpha, int face)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            Color4 texcolor;
            if (face >= 0 && face < GetNumberOfSides(part))
            {
                texcolor = tex.CreateFace((uint)face).RGBA;
                texcolor.A = Util.Clip((float)alpha, 0.0f, 1.0f);
                tex.FaceTextures[face].RGBA = texcolor;
                part.UpdateTexture(tex);
            }
            else if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (int i = 0; i < GetNumberOfSides(part); i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        texcolor = tex.FaceTextures[i].RGBA;
                        texcolor.A = Util.Clip((float)alpha, 0.0f, 1.0f);
                        tex.FaceTextures[i].RGBA = texcolor;
                    }
                }
                texcolor = tex.DefaultTexture.RGBA;
                texcolor.A = Util.Clip((float)alpha, 0.0f, 1.0f);
                tex.DefaultTexture.RGBA = texcolor;
                part.UpdateTexture(tex);
            }
            part.ScheduleUpdate(PrimUpdateFlags.FullUpdate);
        }

        /// <summary>
        /// Set flexi parameters of a part.
        ///
        /// FIXME: Much of this code should probably be within the part itself.
        /// </summary>
        /// <param name="part"></param>
        /// <param name="flexi"></param>
        /// <param name="softness"></param>
        /// <param name="gravity"></param>
        /// <param name="friction"></param>
        /// <param name="wind"></param>
        /// <param name="tension"></param>
        /// <param name="Force"></param>
        protected void SetFlexi(SceneObjectPart part, bool flexi, int softness, float gravity, float friction,
            float wind, float tension, LSL_Vector Force)
        {
            if (part == null)
                return;

            if (flexi)
            {
                part.Shape.PathCurve |= (byte)Extrusion.Flexible;
                part.Shape.FlexiEntry = true;   // this setting flexi true isn't working, but the below parameters do
                // work once the prim is already flexi
                part.Shape.FlexiSoftness = softness;
                part.Shape.FlexiGravity = gravity;
                part.Shape.FlexiDrag = friction;
                part.Shape.FlexiWind = wind;
                part.Shape.FlexiTension = tension;
                part.Shape.FlexiForceX = (float)Force.x;
                part.Shape.FlexiForceY = (float)Force.y;
                part.Shape.FlexiForceZ = (float)Force.z;
                part.Shape.PathCurve = 0x80;
            }
            else
            {
                int curve = part.Shape.PathCurve;
                curve &= (int)(~(Extrusion.Flexible));
                part.Shape.PathCurve = (byte)curve;
                part.Shape.FlexiEntry = false;
            }


            part.ParentGroup.HasGroupChanged = true;
            part.ScheduleUpdate(PrimUpdateFlags.FullUpdate);
        }

        /// <summary>
        /// Set a light point on a part
        /// </summary>
        /// FIXME: Much of this code should probably be in SceneObjectGroup
        ///
        /// <param name="part"></param>
        /// <param name="light"></param>
        /// <param name="color"></param>
        /// <param name="intensity"></param>
        /// <param name="radius"></param>
        /// <param name="falloff"></param>
        protected void SetPointLight(SceneObjectPart part, bool light, LSL_Vector color, float intensity, float radius, float falloff)
        {
            if (part == null)
                return;

            if (light)
            {
                part.Shape.LightEntry = true;
                part.Shape.LightColorR = Util.Clip((float)color.x, 0.0f, 1.0f);
                part.Shape.LightColorG = Util.Clip((float)color.y, 0.0f, 1.0f);
                part.Shape.LightColorB = Util.Clip((float)color.z, 0.0f, 1.0f);
                part.Shape.LightIntensity = intensity;
                part.Shape.LightRadius = radius;
                part.Shape.LightFalloff = falloff;
            }
            else
            {
                part.Shape.LightEntry = false;
            }

            part.ParentGroup.HasGroupChanged = true;
            part.ScheduleUpdate(PrimUpdateFlags.FindBest);
        }

        public LSL_Vector llGetColor(int face)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return GetColor(m_host, face);
        }

        protected LSL_Vector GetColor (ISceneChildEntity part, int face)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            Color4 texcolor;
            LSL_Vector rgb = new LSL_Vector();
            int ns = GetNumberOfSides(part);
            if (face == ScriptBaseClass.ALL_SIDES)
            {
                int i;               

                for (i = 0 ; i < ns ; i++)
                {
                    texcolor = tex.GetFace((uint)i).RGBA;
                    rgb.x += texcolor.R;
                    rgb.y += texcolor.G;
                    rgb.z += texcolor.B;
                }

                float tmp = 1f / (float)ns;
                rgb.x *= tmp;
                rgb.y *= tmp;
                rgb.z *= tmp;

                return rgb;
            }
            if (face >= 0 && face < ns)
            {
                texcolor = tex.GetFace((uint)face).RGBA;
                rgb.x = texcolor.R;
                rgb.y = texcolor.G;
                rgb.z = texcolor.B;
                return rgb;
            }
            else
            {
                return new LSL_Vector();
            }
        }

        public void llSetTexture(string texture, int face)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            SetTexture(m_host, texture, face);
            ScriptSleep(200);
        }

        public void llSetLinkTexture(int linknumber, string texture, int face)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");


            List<ISceneChildEntity> parts = GetLinkParts(linknumber);

            foreach (ISceneChildEntity part in parts)
                SetTexture(part, texture, face);

            ScriptSleep(100);
        }

        protected void SetTexture (ISceneChildEntity part, string texture, int face)
        {
            UUID textureID=new UUID();
            int ns = GetNumberOfSides(part);

             textureID = InventoryKey(texture, (int)AssetType.Texture);
             if (textureID == UUID.Zero)
             {
                 if (!UUID.TryParse(texture, out textureID))
                     return;
             }

            Primitive.TextureEntry tex = part.Shape.Textures;

            if (face >= 0 && face < ns)
            {
                Primitive.TextureEntryFace texface = tex.CreateFace((uint)face);
                texface.TextureID = textureID;
                tex.FaceTextures[face] = texface;
                part.UpdateTexture(tex);
                return;
            }
            else if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (uint i = 0; i < ns; i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].TextureID = textureID;
                    }
                }
                tex.DefaultTexture.TextureID = textureID;
                part.UpdateTexture(tex);
                return;
            }
        }

        public void llScaleTexture(double u, double v, int face)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");


            ScaleTexture(m_host, u, v, face);
            ScriptSleep(200);
        }

        protected void ScaleTexture (ISceneChildEntity part, double u, double v, int face)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            int ns = GetNumberOfSides(part);
            if (face >= 0 && face < ns)
            {
                Primitive.TextureEntryFace texface = tex.CreateFace((uint)face);
                texface.RepeatU = (float)u;
                texface.RepeatV = (float)v;
                tex.FaceTextures[face] = texface;
                part.UpdateTexture(tex);
                return;
            }
            if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (int i = 0; i < ns; i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].RepeatU = (float)u;
                        tex.FaceTextures[i].RepeatV = (float)v;
                    }
                }
                tex.DefaultTexture.RepeatU = (float)u;
                tex.DefaultTexture.RepeatV = (float)v;
                part.UpdateTexture(tex);
                return;
            }
        }

        public void llOffsetTexture(double u, double v, int face)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            OffsetTexture(m_host, u, v, face);
            ScriptSleep(200);
        }

        protected void OffsetTexture (ISceneChildEntity part, double u, double v, int face)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            int ns = GetNumberOfSides(part);
            if (face >= 0 && face < ns)
            {
                Primitive.TextureEntryFace texface = tex.CreateFace((uint)face);
                texface.OffsetU = (float)u;
                texface.OffsetV = (float)v;
                tex.FaceTextures[face] = texface;
                part.UpdateTexture(tex);
                return;
            }
            if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (int i = 0; i < ns; i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].OffsetU = (float)u;
                        tex.FaceTextures[i].OffsetV = (float)v;
                    }
                }
                tex.DefaultTexture.OffsetU = (float)u;
                tex.DefaultTexture.OffsetV = (float)v;
                part.UpdateTexture(tex);
                return;
            }
        }

        public void llRotateTexture(double rotation, int face)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            RotateTexture(m_host, rotation, face);
            ScriptSleep(200);
        }

        protected void RotateTexture(ISceneChildEntity part, double rotation, int face)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            int ns = GetNumberOfSides(part);
            if (face >= 0 && face < ns)
            {
                Primitive.TextureEntryFace texface = tex.CreateFace((uint)face);
                texface.Rotation = (float)rotation;
                tex.FaceTextures[face] = texface;
                part.UpdateTexture(tex);
                return;
            }
            if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (int i = 0; i < ns; i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].Rotation = (float)rotation;
                    }
                }
                tex.DefaultTexture.Rotation = (float)rotation;
                part.UpdateTexture(tex);
                return;
            }
        }

        public LSL_String llGetTexture(int face)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return GetTexture(m_host, face);
        }

        protected LSL_String GetTexture (ISceneChildEntity part, int face)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            
            if (face == ScriptBaseClass.ALL_SIDES)
            {
                face = 0;
            }
            if (face >= 0 && face < GetNumberOfSides(part))
            {
                Primitive.TextureEntryFace texface;
                texface = tex.GetFace((uint)face);
                TaskInventoryItem item = null;
                m_host.TaskInventory.TryGetValue(texface.TextureID, out item);
                if (item != null)
                    return item.Name.ToString();
                else
                    return texface.TextureID.ToString();
            }
            else
            {
                return String.Empty;
            }
        }

        public void llSetPos(LSL_Vector pos)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            SetPos(m_host, pos);

            ScriptSleep(200);
        }

        // Capped movemment if distance > 10m (http://wiki.secondlife.com/wiki/LlSetPos)
        // note linked setpos is capped "differently"
        private LSL_Vector SetPosAdjust(LSL_Vector start, LSL_Vector end)
        {
            if (llVecDist(start, end) > 10.0f * m_ScriptDistanceFactor)
                return start + m_ScriptDistanceFactor * 10.0f * llVecNorm(end - start);
            else
                return end;
        }

        protected void SetPos(ISceneChildEntity part, LSL_Vector targetPos)
        {
            // Capped movemment if distance > 10m (http://wiki.secondlife.com/wiki/LlSetPos)
            LSL_Vector currentPos = GetPartLocalPos(part);
            float ground = 0;
            bool disable_underground_movement = m_ScriptEngine.Config.GetBoolean("DisableUndergroundMovement", true);

            ITerrainChannel heightmap = World.RequestModuleInterface<ITerrainChannel>();
            if(heightmap != null)
                ground = heightmap.GetNormalizedGroundHeight((int)(float)targetPos.x, (int)(float)targetPos.y);
            if (part.ParentEntity == null)
            {
                if (ground != 0 && (targetPos.z < ground) && disable_underground_movement && m_host.AttachmentPoint == 0)
                    targetPos.z = ground;
                    LSL_Vector real_vec = SetPosAdjust(currentPos, targetPos);
                    part.UpdateOffSet(new Vector3((float)real_vec.x, (float)real_vec.y, (float)real_vec.z));
            }
            else if (part.ParentEntity.RootChild == part)
            {
                ISceneEntity parent = part.ParentEntity;
                if (!part.IsAttachment)
                {
                    if (ground != 0 && (targetPos.z < ground) && disable_underground_movement)
                        targetPos.z = ground;
                }
                LSL_Vector real_vec = SetPosAdjust(currentPos, targetPos);
                parent.UpdateGroupPosition(new Vector3((float)real_vec.x, (float)real_vec.y, (float)real_vec.z), true);
            }
            else
            {
                LSL_Vector rel_vec = SetPosAdjust(currentPos, targetPos);
                part.FixOffsetPosition((new Vector3((float)rel_vec.x, (float)rel_vec.y, (float)rel_vec.z)),true);
                ISceneEntity parent = part.ParentEntity;
                parent.HasGroupChanged = true;
                parent.ScheduleGroupTerseUpdate();
            }
        }

        protected LSL_Vector GetPartLocalPos(ISceneChildEntity part)
        {
            Vector3 tmp;
            if (part.ParentID == 0)
            {
                tmp = part.AbsolutePosition;
                return new LSL_Vector(tmp.X,
                                      tmp.Y,
                                      tmp.Z);
            }
            else
            {
                if (m_host.IsRoot)
                {
                    tmp = m_host.AttachedPos;
                    return new LSL_Vector(tmp.X,
                                          tmp.Y,
                                          tmp.Z);
                }
                else
                {
                    tmp = part.OffsetPosition;
                    return new LSL_Vector(tmp.X,
                                          tmp.Y,
                                          tmp.Z);
                }
            }
        }

        public LSL_Vector llGetPos()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            Vector3 pos = m_host.GetWorldPosition();
            return new LSL_Vector(pos.X, pos.Y, pos.Z);
        }

        public LSL_Vector llGetLocalPos()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            Vector3 tmp;
            if (m_host.ParentID != 0)
            {
                tmp = m_host.OffsetPosition;
                return new LSL_Vector(tmp.X,
                                      tmp.Y,
                                      tmp.Z);
            }
            else
            {
                tmp = m_host.AbsolutePosition;
                return new LSL_Vector(tmp.X,
                                      tmp.Y,
                                      tmp.Z);
            }
        }

        public void llSetRot(LSL_Rotation rot)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            // try to let this work as in SL...
            if (m_host.ParentID == 0)
            {
                // special case: If we are root, rotate complete SOG to new rotation
                SetRot(m_host, Rot2Quaternion(rot));
            }
            else
            {
                // we are a child. The rotation values will be set to the one of root modified by rot, as in SL. Don't ask.
                ISceneEntity group = m_host.ParentEntity;
                if (group != null) // a bit paranoid, maybe
                {
                    ISceneChildEntity rootPart = group.RootChild;
                    if (rootPart != null) // again, better safe than sorry
                    {
                        SetRot(m_host, rootPart.RotationOffset * Rot2Quaternion(rot));
                    }
                }
            }
            ScriptSleep(200);
        }

        public void llSetLocalRot(LSL_Rotation rot)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            SetRot(m_host, Rot2Quaternion(rot));
            ScriptSleep(200);
        }

        protected void SetRot(ISceneChildEntity part, Quaternion rot)
        {
            part.UpdateRotation(rot);
            // Update rotation does not move the object in the physics scene if it's a linkset.

            //KF:  Do NOT use this next line if using ODE physics engine.
            //   This need a switch based on .ini Phys Engine type
            //part.ParentGroup.ResetChildPrimPhysicsPositions()
            
            // So, after thinking about this for a bit, the issue with the part.ParentGroup.AbsolutePosition = part.ParentGroup.AbsolutePosition line
            // is it isn't compatible with vehicles because it causes the vehicle body to have to be broken down and rebuilt
            // It's perfectly okay when the object is not an active physical body though.
            // So, part.ParentGroup.ResetChildPrimPhysicsPositions(); does the thing that Kitto is warning against
            // but only if the object is not physial and active.   This is important for rotating doors.
            // without the absoluteposition = absoluteposition happening, the doors do not move in the physics
            // scene
            if (part.PhysActor != null && !part.PhysActor.IsPhysical)
            {
                part.ParentEntity.ResetChildPrimPhysicsPositions ();
            }
        }

        /// <summary>
        /// See http://lslwiki.net/lslwiki/wakka.php?wakka=ChildRotation
        /// </summary>
        public LSL_Rotation llGetRot()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            // unlinked or root prim then use llRootRotation
            // see llRootRotaion for references.
            if (m_host.LinkNum == 0 || m_host.LinkNum == 1)
            {
                return llGetRootRotation();
            }
            
            Quaternion q = m_host.GetWorldRotation();
            return new LSL_Rotation(q.X, q.Y, q.Z, q.W);
        }

        private LSL_Rotation GetPartRot (ISceneChildEntity part)
        {
            Quaternion q;
            if (part.LinkNum == 0 || part.LinkNum == 1) // unlinked or root prim
            {
                if (part.ParentEntity.RootChild.AttachmentPoint != 0)
                {
                    IScenePresence avatar = World.GetScenePresence(part.AttachedAvatar);
                    if (avatar != null)
                    {
                        if ((avatar.AgentControlFlags & (uint)AgentManager.ControlFlags.AGENT_CONTROL_MOUSELOOK) != 0)
                            q = avatar.CameraRotation; // Mouselook
                        else
                            q = avatar.Rotation; // Currently infrequently updated so may be inaccurate
                    }
                    else
                        q = part.ParentEntity.GroupRotation; // Likely never get here but just in case
                }
                else
                    q = part.ParentEntity.GroupRotation; // just the group rotation
                return new LSL_Rotation(q.X, q.Y, q.Z, q.W);
            }
            q = part.GetWorldRotation();
            return new LSL_Rotation(q.X, q.Y, q.Z, q.W);
        }

        public LSL_Rotation llGetLocalRot()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return new LSL_Rotation(m_host.RotationOffset.X, m_host.RotationOffset.Y, m_host.RotationOffset.Z, m_host.RotationOffset.W);
        }

        public void llSetForce(LSL_Vector force, int local)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");


            if (m_host.ParentEntity != null)
            {
                if (!m_host.ParentEntity.IsDeleted)
                {
                    if (local != 0)
                        force *= llGetRot();

                    m_host.ParentEntity.RootChild.SetForce (new Vector3 ((float)force.x, (float)force.y, (float)force.z));
                }
            }
        }

        public LSL_Vector llGetForce()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            LSL_Vector force = new LSL_Vector(0.0, 0.0, 0.0);

            

            if (m_host.ParentEntity != null)
            {
                if (!m_host.ParentEntity.IsDeleted)
                {
                    Vector3 tmpForce = m_host.ParentEntity.RootChild.GetForce();
                    force.x = tmpForce.X;
                    force.y = tmpForce.Y;
                    force.z = tmpForce.Z;
                }
            }

            return force;
        }

        public LSL_Integer llTarget(LSL_Vector position, double range)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return m_host.registerTargetWaypoint(new Vector3((float)position.x, (float)position.y, (float)position.z), (float)range);
        }

        public void llTargetRemove(int number)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.unregisterTargetWaypoint(number);
        }

        public LSL_Integer llRotTarget(LSL_Rotation rot, double error)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return m_host.registerRotTargetWaypoint(new Quaternion((float)rot.x, (float)rot.y, (float)rot.z, (float)rot.s), (float)error);
        }

        public void llRotTargetRemove(int number)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.unregisterRotTargetWaypoint(number);
        }

        public void llMoveToTarget(LSL_Vector target, double tau)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.MoveToTarget(new Vector3((float)target.x, (float)target.y, (float)target.z), (float)tau);
        }

        public void llStopMoveToTarget()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.StopMoveToTarget();
        }

        public void llApplyImpulse(LSL_Vector force, int local)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            //No energy force yet
            Vector3 v = new Vector3((float)force.x, (float)force.y, (float)force.z);
            float len = v.Length();
            if (len > 20000.0f)
            {
//                v.Normalize();
                v = v * 20000.0f/len;
            }
            m_host.ApplyImpulse(v, local != 0);
        }

        public void llApplyRotationalImpulse(LSL_Vector force, int local)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.ApplyAngularImpulse(new Vector3((float)force.x, (float)force.y, (float)force.z), local != 0);
        }

        public void llSetTorque(LSL_Vector torque, int local)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.SetAngularImpulse(new Vector3((float)torque.x, (float)torque.y, (float)torque.z), local != 0);
        }

        public LSL_Vector llGetTorque()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            Vector3 torque = m_host.GetTorque();
            return new LSL_Vector(torque.X,torque.Y,torque.Z);
        }

        public void llSetForceAndTorque(LSL_Vector force, LSL_Vector torque, int local)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            llSetForce(force, local);
            llSetTorque(torque, local);
        }

        public LSL_Vector llGetVel()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            Vector3 tmp = m_host.Velocity;
            return new LSL_Vector(tmp.X, tmp.Y, tmp.Z);
        }

        public LSL_Vector llGetAccel()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            Vector3 tmp = m_host.Acceleration;
            return new LSL_Vector(tmp.X, tmp.Y, tmp.Z);
        }

        public LSL_Vector llGetOmega()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            Vector3 tmp = m_host.AngularVelocity;
            return new LSL_Vector(tmp.X, tmp.Y, tmp.Z);
        }

        public LSL_Float llGetTimeOfDay() // this is not sl compatible see wiki
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return (double)((DateTime.Now.TimeOfDay.TotalMilliseconds / 1000) % (3600 * 4));
        }

        public LSL_Float llGetWallclock()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return DateTime.Now.TimeOfDay.TotalSeconds;
        }

        public LSL_Float llGetTime()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            TimeSpan ScriptTime = DateTime.Now - m_timer;
            return (double)(ScriptTime.TotalMilliseconds / 1000);
        }

        public void llResetTime()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_timer = DateTime.Now;
        }

        public LSL_Float llGetAndResetTime()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            TimeSpan ScriptTime = DateTime.Now - m_timer;
            m_timer = DateTime.Now;
            return (double)(ScriptTime.TotalMilliseconds / 1000);
        }

        public void llSound(string sound, double volume, int queue, int loop)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            // This function has been deprecated
            // see http://www.lslwiki.net/lslwiki/wakka.php?wakka=llSound
            Deprecated("llSound");
        }

        // Xantor 20080528 PlaySound updated so it accepts an objectinventory name -or- a key to a sound
        // 20080530 Updated to remove code duplication
        public void llPlaySound(string sound, double volume)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            // send the sound, once, to all clients in range
            m_host.SendSound(KeyOrName(sound).ToString(), volume, false, 0, 0, false, false);
        }

        // Xantor 20080528 we should do this differently.
        // 1) apply the sound to the object
        // 2) schedule full update
        // just sending the sound out once doesn't work so well when other avatars come in view later on
        // or when the prim gets moved, changed, sat on, whatever
        // see large number of mantises (mantes?)
        // 20080530 Updated to remove code duplication
        // 20080530 Stop sound if there is one, otherwise volume only changes don't work
        public void llLoopSound(string sound, double volume)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            if (m_host.Sound != UUID.Zero)
                llStopSound();

            m_host.Sound = KeyOrName(sound);
            m_host.SoundGain = volume;
            m_host.SoundFlags = (int)SoundFlags.Loop;      // looping
            m_host.SoundRadius = 20;    // Magic number, 20 seems reasonable. Make configurable?

            m_host.ScheduleUpdate(PrimUpdateFlags.FindBest);
        }

        public void llLoopSoundMaster(string sound, double volume)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            m_host.ParentEntity.LoopSoundMasterPrim = m_host;
            lock (m_host.ParentEntity.LoopSoundSlavePrims)
            {
                foreach (ISceneChildEntity prim in m_host.ParentEntity.LoopSoundSlavePrims)
                {
                    if (prim.Sound != UUID.Zero)
                        llStopSound();

                    prim.Sound = KeyOrName(sound);
                    prim.SoundGain = volume;
                    prim.SoundFlags = 1;      // looping
                    prim.SoundRadius = 20;    // Magic number, 20 seems reasonable. Make configurable?

                    prim.ScheduleUpdate(PrimUpdateFlags.FindBest);
                }
            }
            if (m_host.Sound != UUID.Zero)
                llStopSound();

            m_host.Sound = KeyOrName(sound);
            m_host.SoundGain = volume;
            m_host.SoundFlags = 1;      // looping
            m_host.SoundRadius = 20;    // Magic number, 20 seems reasonable. Make configurable?

            m_host.ScheduleUpdate(PrimUpdateFlags.FindBest);
        }

        public void llLoopSoundSlave(string sound, double volume)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            lock (m_host.ParentEntity.LoopSoundSlavePrims)
            {
                m_host.ParentEntity.LoopSoundSlavePrims.Add (m_host);
            }
        }

        public void llPlaySoundSlave(string sound, double volume)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            // send the sound, once, to all clients in range
            m_host.SendSound(KeyOrName(sound).ToString(), volume, false, 0, 0, true, false);
        }

        public void llTriggerSound(string sound, double volume)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            // send the sound, once, to all clients in range
            m_host.SendSound(KeyOrName(sound).ToString(), volume, true, 0, 0, false, false);
        }

        // Xantor 20080528: Clear prim data of sound instead
        public void llStopSound()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            if (m_host.ParentEntity.LoopSoundSlavePrims.Contains (m_host))
            {
                if (m_host.ParentEntity.LoopSoundMasterPrim == m_host)
                {
                    foreach (SceneObjectPart part in m_host.ParentEntity.LoopSoundSlavePrims)
                    {
                        part.Sound = UUID.Zero;
                        part.SoundGain = 0;
                        part.SoundFlags = 0;
                        part.SoundRadius = 0;
                        part.ScheduleUpdate(PrimUpdateFlags.FindBest);
                    }
                    m_host.ParentEntity.LoopSoundMasterPrim = null;
                    m_host.ParentEntity.LoopSoundSlavePrims.Clear ();
                }
                else
                {
                    m_host.Sound = UUID.Zero;
                    m_host.SoundGain = 0;
                    m_host.SoundFlags = 0;
                    m_host.SoundRadius = 0;
                    m_host.ScheduleUpdate(PrimUpdateFlags.FindBest);
                }
            }
            else
            {
                m_host.Sound = UUID.Zero;
                m_host.SoundGain = 0;
                m_host.SoundFlags = 0;
                m_host.SoundRadius = 0;
                m_host.ScheduleUpdate(PrimUpdateFlags.FindBest);
            }
        }

        public void llPreloadSound(string sound)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            m_host.PreloadSound(sound);
            ScriptSleep(1000);
        }

        /// <summary>
        /// Return a portion of the designated string bounded by
        /// inclusive indices (start and end). As usual, the negative
        /// indices, and the tolerance for out-of-bound values, makes
        /// this more complicated than it might otherwise seem.
        /// </summary>

        public LSL_String llGetSubString(string src, int start, int end)
        {

            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            // Normalize indices (if negative).
            // After normlaization they may still be
            // negative, but that is now relative to
            // the start, rather than the end, of the
            // sequence.

            if (start < 0)
            {
                start = src.Length+start;
            }
            if (end < 0)
            {
                end = src.Length+end;
            }

            // Conventional substring
            if (start <= end)
            {
                // Implies both bounds are out-of-range.
                if (end < 0 || start >= src.Length)
                {
                    return String.Empty;
                }
                // If end is positive, then it directly
                // corresponds to the lengt of the substring
                // needed (plus one of course). BUT, it
                // must be within bounds.
                if (end >= src.Length)
                {
                    end = src.Length-1;
                }

                if (start < 0)
                {
                    return src.Substring(0,end+1);
                }
                // Both indices are positive
                return src.Substring(start, (end+1) - start);
            }

            // Inverted substring (end < start)
            else
            {
                // Implies both indices are below the
                // lower bound. In the inverted case, that
                // means the entire string will be returned
                // unchanged.
                if (start < 0)
                {
                    return src;
                }
                // If both indices are greater than the upper
                // bound the result may seem initially counter
                // intuitive.
                if (end >= src.Length)
                {
                    return src;
                }

                if (end < 0)
                {
                    if (start < src.Length)
                    {
                        return src.Substring(start);
                    }
                    else
                    {
                        return String.Empty;
                    }
                }
                else
                {
                    if (start < src.Length)
                    {
                        return src.Substring(0,end+1) + src.Substring(start);
                    }
                    else
                    {
                        return src.Substring(0,end+1);
                    }
                }
            }
         }

        /// <summary>
        /// Delete substring removes the specified substring bounded
        /// by the inclusive indices start and end. Indices may be
        /// negative (indicating end-relative) and may be inverted,
        /// i.e. end < start.
        /// </summary>

        public LSL_String llDeleteSubString(string src, int start, int end)
        {

            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            // Normalize indices (if negative).
            // After normlaization they may still be
            // negative, but that is now relative to
            // the start, rather than the end, of the
            // sequence.
            if (start < 0)
            {
                start = src.Length+start;
            }
            if (end < 0)
            {
                end = src.Length+end;
            }
            // Conventionally delimited substring
            if (start <= end)
            {
                // If both bounds are outside of the existing
                // string, then return unchanges.
                if (end < 0 || start >= src.Length)
                {
                    return src;
                }
                // At least one bound is in-range, so we
                // need to clip the out-of-bound argument.
                if (start < 0)
                {
                    start = 0;
                }

                if (end >= src.Length)
                {
                    end = src.Length-1;
                }

                return src.Remove(start,end-start+1);
            }
            // Inverted substring
            else
            {
                // In this case, out of bounds means that
                // the existing string is part of the cut.
                if (start < 0 || end >= src.Length)
                {
                    return String.Empty;
                }

                if (end > 0)
                {
                    if (start < src.Length)
                    {
                        return src.Remove(start).Remove(0,end+1);
                    }
                    else
                    {
                        return src.Remove(0,end+1);
                    }
                }
                else
                {
                    if (start < src.Length)
                    {
                        return src.Remove(start);
                    }
                    else
                    {
                        return src;
                    }
                }
            }
        }

        /// <summary>
        /// Insert string inserts the specified string identified by src
        /// at the index indicated by index. Index may be negative, in
        /// which case it is end-relative. The index may exceed either
        /// string bound, with the result being a concatenation.
        /// </summary>

        public LSL_String llInsertString(string dest, int index, string src)
        {

            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            // Normalize indices (if negative).
            // After normlaization they may still be
            // negative, but that is now relative to
            // the start, rather than the end, of the
            // sequence.
            if (index < 0)
            {
                index = dest.Length+index;

                // Negative now means it is less than the lower
                // bound of the string.

                if (index < 0)
                {
                    return src+dest;
                }

            }

            if (index >= dest.Length)
            {
                return dest+src;
            }

            // The index is in bounds.
            // In this case the index refers to the index that will
            // be assigned to the first character of the inserted string.
            // So unlike the other string operations, we do not add one
            // to get the correct string length.
            return dest.Substring(0,index)+src+dest.Substring(index);

        }

        public LSL_String llToUpper(string src)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return src.ToUpper();
        }

        public LSL_String llToLower(string src)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return src.ToLower();
        }

        public LSL_Integer llGiveMoney(string destination, int amount)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            UUID invItemID=InventorySelf();
            if (invItemID == UUID.Zero)
                return 0;

            

            TaskInventoryItem item = m_host.TaskInventory[invItemID];

            lock (m_host.TaskInventory)
            {
                item = m_host.TaskInventory[invItemID];
            }

            if (item.PermsGranter == UUID.Zero)
                return 0;

            if ((item.PermsMask & ScriptBaseClass.PERMISSION_DEBIT) == 0)
            {
                LSLError("No permissions to give money");
                return 0;
            }

            UUID toID = new UUID();

            if (!UUID.TryParse(destination, out toID))
            {
                LSLError("Bad key in llGiveMoney");
                return 0;
            }

            IMoneyModule money = World.RequestModuleInterface<IMoneyModule>();

            if (money == null)
            {
                NotImplemented("llGiveMoney");
                return 0;
            }

            bool result = money.ObjectGiveMoney(
                m_host.ParentEntity.UUID, m_host.OwnerID, toID, amount);

            if (result)
                return 1;

            return 0;
        }

        public void llMakeExplosion(int particles, double scale, double vel, double lifetime, double arc, string texture, LSL_Vector offset)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            /*llParticleSystem([
        PSYS_PART_FLAGS,            PSYS_PART_INTERP_COLOR_MASK | PSYS_PART_INTERP_SCALE_MASK | PSYS_PART_EMISSIVE_MASK | PSYS_PART_WIND_MASK,
        PSYS_SRC_PATTERN,           PSYS_SRC_PATTERN_ANGLE_CONE,
        PSYS_PART_START_COLOR,      <1.0, 1.0, 1.0>,
        PSYS_PART_END_COLOR,        <1.0, 1.0, 1.0>,
        PSYS_PART_START_ALPHA,      0.50,
        PSYS_PART_END_ALPHA,        0.25,
        PSYS_PART_START_SCALE,      <particle_scale, particle_scale, 0.0>,
        PSYS_PART_END_SCALE,        <particle_scale * 2 + particle_lifetime, particle_scale * 2 + particle_lifetime, 0.0>,
        PSYS_PART_MAX_AGE,          particle_lifetime,
        PSYS_SRC_ACCEL,             <0.0, 0.0, 0.0>,
        PSYS_SRC_TEXTURE,           source_texture_id,
        PSYS_SRC_BURST_RATE,        1.0,
        PSYS_SRC_ANGLE_BEGIN,       0.0,
        PSYS_SRC_ANGLE_END,         source_cone * PI,
        PSYS_SRC_BURST_PART_COUNT,  particle_count / 2,
        PSYS_SRC_BURST_RADIUS,      0.0,
        PSYS_SRC_BURST_SPEED_MIN,   particle_speed / 3,
        PSYS_SRC_BURST_SPEED_MAX,   particle_speed * 2/3,
        PSYS_SRC_MAX_AGE,           particle_lifetime / 2,
        PSYS_SRC_OMEGA,             <0.0, 0.0, 0.0>
        ]);*/

            List<object> list = new List<object>();
            list.Add(ScriptBaseClass.PSYS_PART_FLAGS);
            list.Add(ScriptBaseClass.PSYS_PART_INTERP_COLOR_MASK | ScriptBaseClass.PSYS_PART_INTERP_SCALE_MASK | ScriptBaseClass.PSYS_PART_EMISSIVE_MASK | ScriptBaseClass.PSYS_PART_WIND_MASK);
            list.Add(ScriptBaseClass.PSYS_SRC_PATTERN);
            list.Add(ScriptBaseClass.PSYS_SRC_PATTERN_ANGLE_CONE);
            list.Add(ScriptBaseClass.PSYS_PART_START_COLOR);
            list.Add(new LSL_Vector(1, 1, 1));
            list.Add(ScriptBaseClass.PSYS_PART_END_COLOR);
            list.Add(new LSL_Vector(1, 1, 1));
            list.Add(ScriptBaseClass.PSYS_PART_START_ALPHA);
            list.Add(new LSL_Float(0.50));
            list.Add(ScriptBaseClass.PSYS_PART_END_ALPHA);
            list.Add(new LSL_Float(0.25));
            list.Add(ScriptBaseClass.PSYS_PART_START_SCALE);
            list.Add(new LSL_Vector(scale, scale, 0));
            list.Add(ScriptBaseClass.PSYS_PART_END_SCALE);
            list.Add(new LSL_Vector(scale * 2 + lifetime, scale * 2 + lifetime, 0));
            list.Add(ScriptBaseClass.PSYS_PART_MAX_AGE);
            list.Add(new LSL_Float(lifetime));
            list.Add(ScriptBaseClass.PSYS_SRC_ACCEL);
            list.Add(new LSL_Vector(0, 0, 0));
            list.Add(ScriptBaseClass.PSYS_SRC_TEXTURE);
            list.Add(new LSL_String(texture));
            list.Add(ScriptBaseClass.PSYS_SRC_BURST_RATE);
            list.Add(new LSL_Float(1));
            list.Add(ScriptBaseClass.PSYS_SRC_ANGLE_BEGIN);
            list.Add(new LSL_Float(0.0));
            list.Add(ScriptBaseClass.PSYS_SRC_ANGLE_END);
            list.Add(new LSL_Float(arc * Math.PI));
            list.Add(ScriptBaseClass.PSYS_SRC_BURST_PART_COUNT);
            list.Add(new LSL_Integer(particles / 2));
            list.Add(ScriptBaseClass.PSYS_SRC_BURST_RADIUS);
            list.Add(new LSL_Float(0.0));
            list.Add(ScriptBaseClass.PSYS_SRC_BURST_SPEED_MIN);
            list.Add(new LSL_Float(vel / 3));
            list.Add(ScriptBaseClass.PSYS_SRC_BURST_SPEED_MAX);
            list.Add(new LSL_Float(vel * 2 / 3));
            list.Add(ScriptBaseClass.PSYS_SRC_MAX_AGE);
            list.Add(new LSL_Float(lifetime / 2));
            list.Add(ScriptBaseClass.PSYS_SRC_OMEGA);
            list.Add(new LSL_Vector(0, 0, 0));

            llParticleSystem(new LSL_Types.list(list.ToArray()));

            ScriptSleep(100);
        }

        public void llMakeFountain(int particles, double scale, double vel, double lifetime, double arc, int bounce, string texture, LSL_Vector offset, double bounce_offset)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            /*llParticleSystem([
        PSYS_PART_FLAGS,            PSYS_PART_INTERP_COLOR_MASK | PSYS_PART_INTERP_SCALE_MASK | PSYS_PART_WIND_MASK | PSYS_PART_BOUNCE_MASK | PSYS_PART_EMISSIVE_MASK,
        PSYS_SRC_PATTERN,           PSYS_SRC_PATTERN_ANGLE_CONE,
        PSYS_PART_START_COLOR,      <1.0, 1.0, 1.0>,
        PSYS_PART_END_COLOR,        <1.0, 1.0, 1.0>,
        PSYS_PART_START_ALPHA,      0.50,
        PSYS_PART_END_ALPHA,        0.25,
        PSYS_PART_START_SCALE,      <particle_scale/1.5, particle_scale/1.5, 0.0>,
        PSYS_PART_END_SCALE,        <0.0, 0.0, 0.0>,
        PSYS_PART_MAX_AGE,          3.0,
        PSYS_SRC_ACCEL,             <1.0, 0.0, -4>,
        PSYS_SRC_TEXTURE,           source_texture_id,
        PSYS_SRC_BURST_RATE,        5/particle_count,
        PSYS_SRC_ANGLE_BEGIN,       0.0,
        PSYS_SRC_ANGLE_END,         source_cone*PI,
        PSYS_SRC_BURST_PART_COUNT,  1,
        PSYS_SRC_BURST_RADIUS,      0.0,
        PSYS_SRC_BURST_SPEED_MIN,   particle_speed,
        PSYS_SRC_BURST_SPEED_MAX,   particle_speed,
        PSYS_SRC_MAX_AGE,           particle_lifetime/2,
        PSYS_SRC_OMEGA,             <0.0, 0.0, 0.0>
    ]);*/

            List<object> list = new List<object>();
            list.Add(ScriptBaseClass.PSYS_PART_FLAGS);
            list.Add(ScriptBaseClass.PSYS_PART_INTERP_COLOR_MASK | ScriptBaseClass.PSYS_PART_INTERP_SCALE_MASK | ScriptBaseClass.PSYS_PART_WIND_MASK | ScriptBaseClass.PSYS_PART_BOUNCE_MASK | ScriptBaseClass.PSYS_PART_EMISSIVE_MASK);
            list.Add(ScriptBaseClass.PSYS_SRC_PATTERN);
            list.Add(ScriptBaseClass.PSYS_SRC_PATTERN_ANGLE_CONE);
            list.Add(ScriptBaseClass.PSYS_PART_START_COLOR);
            list.Add(new LSL_Vector(1, 1, 1));
            list.Add(ScriptBaseClass.PSYS_PART_END_COLOR);
            list.Add(new LSL_Vector(1, 1, 1));
            list.Add(ScriptBaseClass.PSYS_PART_START_ALPHA);
            list.Add(new LSL_Float(0.50));
            list.Add(ScriptBaseClass.PSYS_PART_END_ALPHA);
            list.Add(new LSL_Float(0.25));
            list.Add(ScriptBaseClass.PSYS_PART_START_SCALE);
            list.Add(new LSL_Vector(scale / 1.5, scale / 1.5, 0));
            list.Add(ScriptBaseClass.PSYS_PART_END_SCALE);
            list.Add(new LSL_Vector(0, 0, 0));
            list.Add(ScriptBaseClass.PSYS_PART_MAX_AGE);
            list.Add(new LSL_Float(3));
            list.Add(ScriptBaseClass.PSYS_SRC_ACCEL);
            list.Add(new LSL_Vector(1, 0, -4));
            list.Add(ScriptBaseClass.PSYS_SRC_TEXTURE);
            list.Add(new LSL_String(texture));
            list.Add(ScriptBaseClass.PSYS_SRC_BURST_RATE);
            list.Add(new LSL_Float(5 / particles));
            list.Add(ScriptBaseClass.PSYS_SRC_ANGLE_BEGIN);
            list.Add(new LSL_Float(0.0));
            list.Add(ScriptBaseClass.PSYS_SRC_ANGLE_END);
            list.Add(new LSL_Float(arc * Math.PI));
            list.Add(ScriptBaseClass.PSYS_SRC_BURST_PART_COUNT);
            list.Add(new LSL_Integer(1));
            list.Add(ScriptBaseClass.PSYS_SRC_BURST_RADIUS);
            list.Add(new LSL_Float(0.0));
            list.Add(ScriptBaseClass.PSYS_SRC_BURST_SPEED_MIN);
            list.Add(new LSL_Float(vel));
            list.Add(ScriptBaseClass.PSYS_SRC_BURST_SPEED_MAX);
            list.Add(new LSL_Float(vel));
            list.Add(ScriptBaseClass.PSYS_SRC_MAX_AGE);
            list.Add(new LSL_Float(lifetime / 2));
            list.Add(ScriptBaseClass.PSYS_SRC_OMEGA);
            list.Add(new LSL_Vector(0, 0, 0));

            llParticleSystem(new LSL_Types.list(list.ToArray()));

            ScriptSleep(100);
        }

        public void llMakeSmoke(int particles, double scale, double vel, double lifetime, double arc, string texture, LSL_Vector offset)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            /*llParticleSystem([
       PSYS_PART_FLAGS,            PSYS_PART_INTERP_COLOR_MASK | PSYS_PART_INTERP_SCALE_MASK | PSYS_PART_EMISSIVE_MASK | PSYS_PART_WIND_MASK,
       PSYS_SRC_PATTERN,           PSYS_SRC_PATTERN_ANGLE_CONE,
       PSYS_PART_START_COLOR,      <1.0, 1.0, 1.0>,
       PSYS_PART_END_COLOR,        <1.0, 1.0, 1.0>,
       PSYS_PART_START_ALPHA,      1.00,
       PSYS_PART_END_ALPHA,        0.05,
       PSYS_PART_START_SCALE,      <particle_scale, particle_scale, 0.0>,
       PSYS_PART_END_SCALE,        <10, 10, 0.0>,
       PSYS_PART_MAX_AGE,          3.0,
       PSYS_SRC_ACCEL,             <0.0, 0.0, 0.0>,
       PSYS_SRC_TEXTURE,           source_texture_id,
       PSYS_SRC_BURST_RATE,        10.0 / particle_count,
       PSYS_SRC_ANGLE_BEGIN,       0.0,
       PSYS_SRC_ANGLE_END,         source_cone * PI,
       PSYS_SRC_BURST_PART_COUNT,  1,
       PSYS_SRC_BURST_RADIUS,      0.0,
       PSYS_SRC_BURST_SPEED_MIN,   particle_speed,
       PSYS_SRC_BURST_SPEED_MAX,   particle_speed,
       PSYS_SRC_MAX_AGE,           particle_lifetime / 2,
       PSYS_SRC_OMEGA,             <0.0, 0.0, 0.0>
       ]);*/
            List<object> list = new List<object>();
            list.Add(ScriptBaseClass.PSYS_PART_FLAGS);
            list.Add(ScriptBaseClass.PSYS_PART_INTERP_COLOR_MASK | ScriptBaseClass.PSYS_PART_INTERP_SCALE_MASK | ScriptBaseClass.PSYS_PART_EMISSIVE_MASK | ScriptBaseClass.PSYS_PART_WIND_MASK);
            list.Add(ScriptBaseClass.PSYS_SRC_PATTERN);
            list.Add(ScriptBaseClass.PSYS_SRC_PATTERN_ANGLE_CONE);
            list.Add(ScriptBaseClass.PSYS_PART_START_COLOR);
            list.Add(new LSL_Vector(1, 1, 1));
            list.Add(ScriptBaseClass.PSYS_PART_END_COLOR);
            list.Add(new LSL_Vector(1, 1, 1));
            list.Add(ScriptBaseClass.PSYS_PART_START_ALPHA);
            list.Add(new LSL_Float(1));
            list.Add(ScriptBaseClass.PSYS_PART_END_ALPHA);
            list.Add(new LSL_Float(0.05));
            list.Add(ScriptBaseClass.PSYS_PART_START_SCALE);
            list.Add(new LSL_Vector(scale, scale, 0));
            list.Add(ScriptBaseClass.PSYS_PART_END_SCALE);
            list.Add(new LSL_Vector(10, 10, 0));
            list.Add(ScriptBaseClass.PSYS_PART_MAX_AGE);
            list.Add(new LSL_Float(3));
            list.Add(ScriptBaseClass.PSYS_SRC_ACCEL);
            list.Add(new LSL_Vector(0, 0, 0));
            list.Add(ScriptBaseClass.PSYS_SRC_TEXTURE);
            list.Add(new LSL_String(texture));
            list.Add(ScriptBaseClass.PSYS_SRC_BURST_RATE);
            list.Add(new LSL_Float(10 / particles));
            list.Add(ScriptBaseClass.PSYS_SRC_ANGLE_BEGIN);
            list.Add(new LSL_Float(0.0));
            list.Add(ScriptBaseClass.PSYS_SRC_ANGLE_END);
            list.Add(new LSL_Float(arc * Math.PI));
            list.Add(ScriptBaseClass.PSYS_SRC_BURST_PART_COUNT);
            list.Add(new LSL_Integer(1));
            list.Add(ScriptBaseClass.PSYS_SRC_BURST_RADIUS);
            list.Add(new LSL_Float(0.0));
            list.Add(ScriptBaseClass.PSYS_SRC_BURST_SPEED_MIN);
            list.Add(new LSL_Float(vel));
            list.Add(ScriptBaseClass.PSYS_SRC_BURST_SPEED_MAX);
            list.Add(new LSL_Float(vel));
            list.Add(ScriptBaseClass.PSYS_SRC_MAX_AGE);
            list.Add(new LSL_Float(lifetime / 2));
            list.Add(ScriptBaseClass.PSYS_SRC_OMEGA);
            list.Add(new LSL_Vector(0, 0, 0));

            llParticleSystem(new LSL_Types.list(list.ToArray()));
            ScriptSleep(100);
        }

        public void llMakeFire(int particles, double scale, double vel, double lifetime, double arc, string texture, LSL_Vector offset)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            /*llParticleSystem([
        PSYS_PART_FLAGS,            PSYS_PART_INTERP_COLOR_MASK | PSYS_PART_INTERP_SCALE_MASK | PSYS_PART_EMISSIVE_MASK | PSYS_PART_WIND_MASK,
        PSYS_SRC_PATTERN,           PSYS_SRC_PATTERN_ANGLE_CONE,
        PSYS_PART_START_COLOR,      <1.0, 1.0, 1.0>,
        PSYS_PART_END_COLOR,        <1.0, 1.0, 1.0>,
        PSYS_PART_START_ALPHA,      0.50,
        PSYS_PART_END_ALPHA,        0.10,
        PSYS_PART_START_SCALE,      <particle_scale/2, particle_scale/2, 0.0>,
        PSYS_PART_END_SCALE,        <particle_scale, particle_scale, 0.0>,
        PSYS_PART_MAX_AGE,          0.5,
        PSYS_SRC_ACCEL,             <0.0, 0.0, 0.0>,
        PSYS_SRC_TEXTURE,           source_texture_id,
        PSYS_SRC_BURST_RATE,        5 / particle_count,
        PSYS_SRC_ANGLE_BEGIN,       0.0,
        PSYS_SRC_ANGLE_END,         source_cone * PI,
        PSYS_SRC_BURST_PART_COUNT,  1,
        PSYS_SRC_BURST_RADIUS,      0.0,
        PSYS_SRC_BURST_SPEED_MIN,   particle_speed,
        PSYS_SRC_BURST_SPEED_MAX,   particle_speed,
        PSYS_SRC_MAX_AGE,           particle_lifetime / 2,
        PSYS_SRC_OMEGA,             <0.0, 0.0, 0.0>
        ]);*/

            List<object> list = new List<object>();
            list.Add(ScriptBaseClass.PSYS_PART_FLAGS);
            list.Add(ScriptBaseClass.PSYS_PART_INTERP_COLOR_MASK | ScriptBaseClass.PSYS_PART_INTERP_SCALE_MASK | ScriptBaseClass.PSYS_PART_EMISSIVE_MASK | ScriptBaseClass.PSYS_PART_WIND_MASK);
            list.Add(ScriptBaseClass.PSYS_SRC_PATTERN);
            list.Add(ScriptBaseClass.PSYS_SRC_PATTERN_ANGLE_CONE);
            list.Add(ScriptBaseClass.PSYS_PART_START_COLOR);
            list.Add(new LSL_Vector(1, 1, 1));
            list.Add(ScriptBaseClass.PSYS_PART_END_COLOR);
            list.Add(new LSL_Vector(1, 1, 1));
            list.Add(ScriptBaseClass.PSYS_PART_START_ALPHA);
            list.Add(new LSL_Float(0.50));
            list.Add(ScriptBaseClass.PSYS_PART_END_ALPHA);
            list.Add(new LSL_Float(0.10));
            list.Add(ScriptBaseClass.PSYS_PART_START_SCALE);
            list.Add(new LSL_Vector(scale / 2, scale / 2, 0));
            list.Add(ScriptBaseClass.PSYS_PART_END_SCALE);
            list.Add(new LSL_Vector(scale, scale, 0));
            list.Add(ScriptBaseClass.PSYS_PART_MAX_AGE);
            list.Add(new LSL_Float(0.50));
            list.Add(ScriptBaseClass.PSYS_SRC_ACCEL);
            list.Add(new LSL_Vector(0, 0, 0));
            list.Add(ScriptBaseClass.PSYS_SRC_TEXTURE);
            list.Add(new LSL_String(texture));
            list.Add(ScriptBaseClass.PSYS_SRC_BURST_RATE);
            list.Add(new LSL_Float(5 / particles));
            list.Add(ScriptBaseClass.PSYS_SRC_ANGLE_BEGIN);
            list.Add(new LSL_Float(0.0));
            list.Add(ScriptBaseClass.PSYS_SRC_ANGLE_END);
            list.Add(new LSL_Float(arc * Math.PI));
            list.Add(ScriptBaseClass.PSYS_SRC_BURST_PART_COUNT);
            list.Add(new LSL_Integer(1));
            list.Add(ScriptBaseClass.PSYS_SRC_BURST_RADIUS);
            list.Add(new LSL_Float(0.0));
            list.Add(ScriptBaseClass.PSYS_SRC_BURST_SPEED_MIN);
            list.Add(new LSL_Float(vel));
            list.Add(ScriptBaseClass.PSYS_SRC_BURST_SPEED_MAX);
            list.Add(new LSL_Float(vel));
            list.Add(ScriptBaseClass.PSYS_SRC_MAX_AGE);
            list.Add(new LSL_Float(lifetime / 2));
            list.Add(ScriptBaseClass.PSYS_SRC_OMEGA);
            list.Add(new LSL_Vector(0, 0, 0));

            llParticleSystem(new LSL_Types.list(list.ToArray()));
            ScriptSleep(100);
        }

        public void llRezAtRoot(string inventory, LSL_Vector pos, LSL_Vector vel, LSL_Rotation rot, int param)
        {
            llRezPrim(inventory, pos, vel, rot, param, true, true, true, true);
        }

        /// <summary>
        /// This isn't really an LSL function, just a way to merge llRezAtRoot and llRezObject into one
        /// </summary>
        /// <param name="inventory"></param>
        /// <param name="pos"></param>
        /// <param name="vel"></param>
        /// <param name="rot"></param>
        /// <param name="param"></param>
        /// <param name="isRezAtRoot"></param>
        /// <returns></returns>
        public void llRezPrim(string inventory, LSL_Types.Vector3 pos, LSL_Types.Vector3 vel, LSL_Types.Quaternion rot, int param, bool isRezAtRoot, bool doRecoil, bool SetDieAtEdge, bool CheckPos)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "LSL", m_host, "LSL");

            if (m_ScriptEngine.Config.GetBoolean("AllowllRezObject", true))
            {
                if (Double.IsNaN(rot.x) || Double.IsNaN(rot.y) || Double.IsNaN(rot.z) || Double.IsNaN(rot.s))
                    return;
                if (CheckPos)
                {
                    float dist = (float)llVecDist(llGetPos(), pos);

                    if (dist > m_ScriptDistanceFactor * 10.0f)
                        return;
                }

                TaskInventoryDictionary partInventory = (TaskInventoryDictionary)m_host.TaskInventory.Clone();

                foreach (KeyValuePair<UUID, TaskInventoryItem> inv in partInventory)
                {
                    if (inv.Value.Name == inventory)
                    {
                        // make sure we're an object.
                        if (inv.Value.InvType != (int)InventoryType.Object)
                        {
                            llSay(0, "Unable to create requested object. Object is missing from database.");
                            return;
                        }

                        Vector3 llpos = new Vector3((float)pos.x, (float)pos.y, (float)pos.z);
                        Vector3 llvel = new Vector3((float)vel.x, (float)vel.y, (float)vel.z);

                        // need the magnitude later
                        float velmag = (float)Util.GetMagnitude(llvel);

                        SceneObjectGroup new_group = RezObject(m_host, inv.Value, llpos, Rot2Quaternion(rot), llvel, param, m_host.UUID, isRezAtRoot);

                        // If either of these are null, then there was an unknown error.
                        if (new_group == null)
                            continue;
                        if (new_group.RootPart == null)
                            continue;

                        // objects rezzed with this method are die_at_edge by default.
                        if(SetDieAtEdge)
                            new_group.RootPart.SetDieAtEdge(true);

                        new_group.ResumeScripts();

                        //Object_rez should be dealt with by the script engine, especially in an async script engine...

                        float groupmass = new_group.GetMass();

                        if (new_group.RootPart.PhysActor != null && new_group.RootPart.PhysActor.IsPhysical && llvel != Vector3.Zero)
                        {
                            //Apply the velocity to the object
                            //llApplyImpulse(new LSL_Vector(llvel.X * groupmass, llvel.Y * groupmass, llvel.Z * groupmass), 0);
                            // @Above: Err.... no. Read http://lslwiki.net/lslwiki/wakka.php?wakka=llRezObject
                            //    Notice the "Creates ("rezzes") object's inventory object centered at position pos (in region coordinates) with velocity vel"
                            //    This means SET the velocity to X, not just temperarily add it!
                            //   -- Revolution Smythe
                            llSetForce(new LSL_Vector(llvel.X * groupmass, llvel.Y * groupmass, llvel.Z * groupmass), 0);
                        }

                        //Recoil to the av
                        if (m_host.IsAttachment && doRecoil)
                        {
                            IScenePresence SP = m_host.ParentEntity.Scene.GetScenePresence(m_host.OwnerID);
                            if (SP != null)
                            {
                                //Push the av backwards (For every action, there is an equal, but opposite reaction)
                                SP.PushForce(llvel * groupmass);
                            }
                        }

                        // Variable script delay? (see (http://wiki.secondlife.com/wiki/LSL_Delay)
                        ScriptSleep((int)((groupmass * velmag) / 10) + 100);
                    }
                }

                llSay(0, "Could not find object " + inventory);
            }
        }

        /// <summary>
        /// Rez an object into the scene from a prim's inventory.
        /// </summary>
        /// <param name="sourcePart"></param>
        /// <param name="item"></param>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <param name="vel"></param>
        /// <param name="param"></param>
        /// <returns>The SceneObjectGroup rezzed or null if rez was unsuccessful</returns>
        public SceneObjectGroup RezObject(
            ISceneChildEntity sourcePart, TaskInventoryItem item,
            Vector3 pos, Quaternion rot, Vector3 vel, int param, UUID RezzedFrom, bool RezObjectAtRoot)
        {
            if (item != null)
            {
                UUID ownerID = item.OwnerID;

                AssetBase rezAsset = World.AssetService.Get(item.AssetID.ToString());

                if (rezAsset != null)
                {
                    string xmlData = Utils.BytesToString(rezAsset.Data);
                    SceneObjectGroup group = SceneObjectSerializer.FromOriginalXmlFormat(xmlData, World);
                    if (group == null)
                        return null;

                    group.IsDeleted = false;
                    group.m_isLoaded = true;
                    foreach (SceneObjectPart part in group.ChildrenList)
                    {
                        part.IsLoading = false;
                    }
                    string reason;
                    if (!World.Permissions.CanRezObject(group.ChildrenList.Count, ownerID, pos, out reason))
                    {
                        World.GetScenePresence(ownerID).ControllingClient.SendAlertMessage("You do not have permission to rez objects here: " + reason);
                        return null;
                    }

                    World.SceneGraph.AddPrimToScene(group);
                    
                    SceneObjectPart rootPart = (SceneObjectPart)group.GetChildPart(group.UUID);
                    List<SceneObjectPart> partList = new List<SceneObjectPart>(group.ChildrenList);

                    // we set it's position in world.
                    // llRezObject sets the whole group at the position, while llRezAtRoot rezzes the group based on the root prim's position
                    // See: http://lslwiki.net/lslwiki/wakka.php?wakka=llRezAtRoot
                    // Shorthand: llRezAtRoot rezzes the root prim of the group at the position
                    //            llRezObject rezzes the center of group at the position
                    if (RezObjectAtRoot)
                        //This sets it right...
                        group.AbsolutePosition = pos;
                    else
                    {
                        //TODO: Make sure this still works
                    /*
                    //Find the 'center' of the group
                    //  Note: In SL, this is based on max - min
                    Vector3 MinPos = new Vector3(100000, 100000, 100000);
                    Vector3 MaxPos = Vector3.Zero;
                    foreach (SceneObjectPart child in partList)
                    {
                        if (child.AbsolutePosition.X < MinPos.X)
                            MinPos.X = child.AbsolutePosition.X;
                        if (child.AbsolutePosition.Y < MinPos.Y)
                            MinPos.Y = child.AbsolutePosition.Y;
                        if (child.AbsolutePosition.Z < MinPos.Z)
                            MinPos.Z = child.AbsolutePosition.Z;

                        if (child.AbsolutePosition.X > MaxPos.X)
                            MaxPos.X = child.AbsolutePosition.X;
                        if (child.AbsolutePosition.Y > MaxPos.Y)
                            MaxPos.Y = child.AbsolutePosition.Y;
                        if (child.AbsolutePosition.Z > MaxPos.Z)
                            MaxPos.Z = child.AbsolutePosition.Z;
                    }
                    Vector3 GroupAvg = ((MaxPos + MinPos) / 2);
                     * 
                         
                    Vector3 GroupAvg = group.GroupScale();
                    Vector3 offset = group.AbsolutePosition - GroupAvg;
*/
                    
                        // center is on average of all positions
                        // less root prim position

                        Vector3 offset = Vector3.Zero;
                        foreach (SceneObjectPart child in partList)
                            {
                            offset += child.AbsolutePosition;
                            }
                        offset /= partList.Count;
                        offset -= group.AbsolutePosition;
                        offset += pos;
                        group.AbsolutePosition = offset;
                    }

                    // Since renaming the item in the inventory does not affect the name stored
                    // in the serialization, transfer the correct name from the inventory to the
                    // object itself before we rez.
                    rootPart.Name = item.Name;
                    rootPart.Description = item.Description;


                    group.SetGroup(sourcePart.GroupID, null);

                    if (rootPart.OwnerID != item.OwnerID)
                    {
                        if (World.Permissions.PropagatePermissions())
                        {
                            if ((item.CurrentPermissions & 8) != 0)
                            {
                                foreach (SceneObjectPart part in partList)
                                {
                                    part.EveryoneMask = item.EveryonePermissions;
                                    part.NextOwnerMask = item.NextPermissions;
                                }
                            }
                            group.ApplyNextOwnerPermissions();
                        }
                    }

                    foreach (SceneObjectPart part in partList)
                    {
                        if (part.OwnerID != item.OwnerID)
                        {
                            part.LastOwnerID = part.OwnerID;
                            part.OwnerID = item.OwnerID;
                            part.Inventory.ChangeInventoryOwner(item.OwnerID);
                        }
                        else if ((item.CurrentPermissions & 8) != 0) // Slam!
                        {
                            part.EveryoneMask = item.EveryonePermissions;
                            part.NextOwnerMask = item.NextPermissions;
                        }
                    }

                    rootPart.TrimPermissions();

                    if (group.RootPart.Shape.PCode == (byte)PCode.Prim)
                    {
                        group.ClearPartAttachmentData();
                    }

                    group.UpdateGroupRotationR(rot);

                    //group.ApplyPhysics(m_physicalPrim);
                    if (group.RootPart.PhysActor != null && group.RootPart.PhysActor.IsPhysical && vel != Vector3.Zero)
                    {
                        group.RootPart.ApplyImpulse((vel * group.GetMass()), false);
                        group.Velocity = vel;
                    }
                    group.CreateScriptInstances(param, true, 2, RezzedFrom);

                    if (!World.Permissions.BypassPermissions())
                    {
                        if ((item.CurrentPermissions & (uint)PermissionMask.Copy) == 0)
                            sourcePart.Inventory.RemoveInventoryItem(item.ItemID);
                    }

                    group.ScheduleGroupUpdate(PrimUpdateFlags.FullUpdate);

                    return rootPart.ParentGroup;
                }
            }

            return null;
        }

        public void llRezObject(string inventory, LSL_Vector pos, LSL_Vector vel, LSL_Rotation rot, int param)
        {
            llRezPrim(inventory, pos, vel, rot, param, false, true, true, true);
        }

        public void llLookAt(LSL_Vector target, double strength, double damping)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            // Determine where we are looking from
            LSL_Vector from = llGetPos();

            // Work out the normalised vector from the source to the target
            LSL_Vector delta = llVecNorm(target - from);
            LSL_Vector angle = new LSL_Vector(0,0,0);

            // Calculate the yaw
            // subtracting PI_BY_TWO is required to compensate for the odd SL co-ordinate system
            angle.x = llAtan2(delta.z, delta.y) - ScriptBaseClass.PI_BY_TWO;

            // Calculate pitch
            angle.y = llAtan2(delta.x, llSqrt((delta.y * delta.y) + (delta.z * delta.z)));

            // we need to convert from a vector describing
            // the angles of rotation in radians into rotation value

            LSL_Types.Quaternion rot = llEuler2Rot(angle);
            Quaternion rotation = new Quaternion((float)rot.x, (float)rot.y, (float)rot.z, (float)rot.s);
            m_host.startLookAt(rotation, (float)damping, (float)strength);
            // Orient the object to the angle calculated
            //llSetRot(rot);
        }

        public void llRotLookAt(LSL_Rotation target, double strength, double damping)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            Quaternion rot = new Quaternion((float)target.x, (float)target.y, (float)target.z, (float)target.s);
            m_host.RotLookAt(rot, (float)strength, (float)damping);
        }

        public void llStopLookAt()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.StopLookAt();
        }

        public void llSetTimerEvent(double sec)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            if (sec != 0.0 && sec < m_MinTimerInterval)
                sec = m_MinTimerInterval;
            
            // Setting timer repeat
            TimerPlugin timerPlugin = (TimerPlugin)m_ScriptEngine.GetScriptPlugin("Timer");
            timerPlugin.SetTimerEvent(m_host.UUID, m_itemID, sec);
        }

        public virtual void llSleep(double sec)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            ScriptSleep((int)(sec * 1000));
        }

        public LSL_Float llGetObjectMass(string id)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            UUID key = new UUID();
            if (UUID.TryParse(id, out key))
            {
                try
                {
                    ISceneChildEntity obj = World.GetSceneObjectPart (key);
                    if (obj != null)
                        return (double)obj.GetMass();
                    // the object is null so the key is for an avatar
                    IScenePresence avatar = World.GetScenePresence (key);
                    if (avatar != null)
                        if (avatar.IsChildAgent)
                            // reference http://www.lslwiki.net/lslwiki/wakka.php?wakka=llGetObjectMass
                            // child agents have a mass of 1.0
                            return 1;
                        else
                            return (double)avatar.PhysicsActor.Mass;
                }
                catch (KeyNotFoundException)
                {
                    return 0; // The Object/Agent not in the region so just return zero
                }
            }
            return 0;
        }

        public LSL_Float llGetMass()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            if (m_host.IsAttachment)
            {
                IScenePresence SP = m_host.ParentEntity.Scene.GetScenePresence (m_host.OwnerID);
                if (SP != null)
                    return SP.PhysicsActor.Mass;
                else
                    return 0.0;
            }
            else
                return m_host.GetMass();
        }

        public void llCollisionFilter(string name, string id, int accept)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.CollisionFilter.Clear();
            if (id != null)
            {
                m_host.CollisionFilter.Add(accept,id);
            }
            else
            {
                m_host.CollisionFilter.Add(accept,name);
            }
        }

        public void llTakeControls(int controls, int accept, int pass_on)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            TaskInventoryItem item;

            lock (m_host.TaskInventory)
            {
                if (!m_host.TaskInventory.ContainsKey(InventorySelf()))
                    return;
                else
                    item = m_host.TaskInventory[InventorySelf()];
            }

            if (item.PermsGranter != UUID.Zero)
            {
                IScenePresence presence = World.GetScenePresence(item.PermsGranter);

                if (presence != null)
                {
                    if ((item.PermsMask & ScriptBaseClass.PERMISSION_TAKE_CONTROLS) != 0)
                    {
                        IScriptControllerModule m = presence.RequestModuleInterface<IScriptControllerModule> ();
                        if(m != null)
                            m.RegisterControlEventsToScript(controls, accept, pass_on, m_host, m_itemID);
                    }
                }
            }

            
        }

        public void llReleaseControls()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            TaskInventoryItem item;

            lock (m_host.TaskInventory)
            {
                if (!m_host.TaskInventory.ContainsKey(InventorySelf()))
                    return;
                else
                    item = m_host.TaskInventory[InventorySelf()];
            }

            

            if (item.PermsGranter != UUID.Zero)
            {
                IScenePresence presence = World.GetScenePresence(item.PermsGranter);

                if (presence != null)
                {
                    if ((item.PermsMask & ScriptBaseClass.PERMISSION_TAKE_CONTROLS) != 0)
                    {
                        // Unregister controls from Presence
                        IScriptControllerModule m = presence.RequestModuleInterface<IScriptControllerModule> ();
                        if (m != null)
                            m.UnRegisterControlEventsToScript (m_localID, m_itemID);
                        // Remove Take Control permission.
                        item.PermsMask &= ~ScriptBaseClass.PERMISSION_TAKE_CONTROLS;
                    }
                }
            }
        }

        public void llReleaseURL(string url)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            if (m_UrlModule != null)
                m_UrlModule.ReleaseURL(url);
        }

        public void llAttachToAvatar(int attachment)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            if (m_host.ParentEntity.RootChild.AttachmentPoint != 0)
                return;

            TaskInventoryItem item;

            lock (m_host.TaskInventory)
            {
                if (!m_host.TaskInventory.ContainsKey(InventorySelf()))
                    return;
                else
                    item = m_host.TaskInventory[InventorySelf()];
            }

            if (item.PermsGranter != m_host.OwnerID)
                return;

            if ((item.PermsMask & ScriptBaseClass.PERMISSION_ATTACH) != 0)
            {
                ISceneEntity grp = m_host.ParentEntity;

                IScenePresence presence = World.GetScenePresence (m_host.OwnerID);

                IAttachmentsModule attachmentsModule = World.RequestModuleInterface<IAttachmentsModule>();
                if (attachmentsModule != null)
                    attachmentsModule.AttachObjectFromInworldObject(m_host.LocalId,
                        presence.ControllingClient, grp,
                        attachment);
            }
        }

        public void llDetachFromAvatar()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            if (m_host.ParentEntity.RootChild.AttachmentPoint == 0)
                return;

            TaskInventoryItem item;

            lock (m_host.TaskInventory)
            {
                if (!m_host.TaskInventory.ContainsKey(InventorySelf()))
                    return;
                else
                    item = m_host.TaskInventory[InventorySelf()];
            }

            if (item.PermsGranter != m_host.OwnerID)
                return;

            if ((item.PermsMask & ScriptBaseClass.PERMISSION_ATTACH) != 0)
            {
                IAttachmentsModule attachmentsModule = World.RequestModuleInterface<IAttachmentsModule>();
                if (attachmentsModule != null)
                    Util.FireAndForget(DetachWrapper, m_host);
            }
        }

        private void DetachWrapper(object o)
        {
            ISceneChildEntity host = (ISceneChildEntity)o;

            ISceneEntity grp = host.ParentEntity;
            UUID itemID = grp.RootChild.FromUserInventoryItemID;
            IScenePresence presence = World.GetScenePresence (host.OwnerID);

            IAttachmentsModule attachmentsModule = World.RequestModuleInterface<IAttachmentsModule>();
            if (attachmentsModule != null)
                attachmentsModule.DetachSingleAttachmentToInventory(itemID, presence.ControllingClient);
        }

        public void llTakeCamera(string avatar)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            Deprecated("llTakeCamera");
        }

        public void llReleaseCamera(string avatar)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            Deprecated("llReleaseCamera");
        }

        public LSL_String llGetOwner()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            return m_host.OwnerID.ToString();
        }

        public void llInstantMessage(string user, string message)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            // We may be able to use ClientView.SendInstantMessage here, but we need a client instance.
            // InstantMessageModule.OnInstantMessage searches through a list of scenes for a client matching the toAgent,
            // but I don't think we have a list of scenes available from here.
            // (We also don't want to duplicate the code in OnInstantMessage if we can avoid it.)

            UUID friendTransactionID = UUID.Random();

            GridInstantMessage msg = new GridInstantMessage();
            msg.fromAgentID = new Guid(m_host.UUID.ToString());
            msg.toAgentID = new Guid(user); 
            msg.imSessionID = new Guid(friendTransactionID.ToString()); // This is the item we're mucking with here
            msg.fromAgentName = m_host.Name;
            
            // Cap the message length at 1024.
            if (message != null && message.Length > 1024)
                msg.message = message.Substring(0, 1024);
            else
                msg.message = message;
            
            msg.dialog = (byte)InstantMessageDialog.MessageFromObject;
            msg.fromGroup = false;
            msg.offline = (byte)0; 
            msg.ParentEstateID = 0;
            msg.Position = m_host.AbsolutePosition;
            msg.RegionID = World.RegionInfo.RegionID.Guid;
            msg.binaryBucket
                = Util.StringToBytes256(
                    "{0}/{1}/{2}/{3}",
                    World.RegionInfo.RegionName,
                    (int)Math.Floor(m_host.AbsolutePosition.X),
                    (int)Math.Floor(m_host.AbsolutePosition.Y),
                    (int)Math.Floor(m_host.AbsolutePosition.Z));

            if (m_TransferModule != null)
            {
                m_TransferModule.SendInstantMessage(msg, delegate(bool success) {});
            }
            ScriptSleep(2000);
      }

        public void llEmail(string address, string subject, string message)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "llEmail", m_host, "LSL");
            
            IEmailModule emailModule = World.RequestModuleInterface<IEmailModule>();
            if (emailModule == null)
            {
                ShoutError("llEmail: email module not configured");
                return;
            }

            emailModule.SendEmail(m_host.UUID, address, subject, message);
            ScriptSleep(20000);
        }

        public void llGetNextEmail(string address, string subject)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            IEmailModule emailModule = World.RequestModuleInterface<IEmailModule>();
            if (emailModule == null)
            {
                ShoutError("llGetNextEmail: email module not configured");
                return;
            }
            Email email;

            email = emailModule.GetNextEmail(m_host.UUID, address, subject);

            if (email == null)
                return;

            m_ScriptEngine.AddToObjectQueue(m_host.UUID, "email",
                    new DetectParams[0], -1, new Object[] {
                        new LSL_String(email.time),
                        new LSL_String(email.sender),
                        new LSL_String(email.subject),
                        new LSL_String(email.message),
                        new LSL_Integer(email.numLeft)}
                    );

        }

        public LSL_String llGetKey()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return m_host.UUID.ToString();
        }

        public void llSetBuoyancy(double buoyancy)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            if (m_host.ParentEntity != null)
            {
                if (!m_host.ParentEntity.IsDeleted)
                {
                    m_host.ParentEntity.RootChild.SetBuoyancy ((float)buoyancy);
                }
            }
        }

        /// <summary>
        /// Attempt to clamp the object on the Z axis at the given height over tau seconds.
        /// </summary>
        /// <param name="height">Height to hover.  Height of zero disables hover.</param>
        /// <param name="water">False if height is calculated just from ground, otherwise uses ground or water depending on whichever is higher</param>
        /// <param name="tau">Number of seconds over which to reach target</param>
        public void llSetHoverHeight(double height, int water, double tau)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            if (m_host.PhysActor != null)
            {
                PIDHoverType hoverType = PIDHoverType.Ground;
                if (water != 0)
                {
                    hoverType = PIDHoverType.GroundAndWater;
                }

                m_host.SetHoverHeight((float)height, hoverType, (float)tau);
            }
        }

        public void llStopHover()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            if (m_host.PhysActor != null)
            {
                m_host.SetHoverHeight(0f, PIDHoverType.Ground, 0f);
            }
        }

        public void llMinEventDelay(double delay)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_ScriptEngine.SetMinEventDelay(m_itemID, m_host.UUID, delay);
        }

        /// <summary>
        /// llSoundPreload is deprecated. In SL this appears to do absolutely nothing
        /// and is documented to have no delay.
        /// </summary>
        public void llSoundPreload(string sound)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
        }

        public LSL_Integer llStringLength(string str)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            if (str.Length > 0)
            {
                return str.Length;
            }
            else
            {
                return 0;
            }
        }

        public void llStartAnimation(string anim)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            UUID invItemID = InventorySelf();
            if (invItemID == UUID.Zero)
                return;

            TaskInventoryItem item;

            lock (m_host.TaskInventory)
            {
                if (!m_host.TaskInventory.ContainsKey(InventorySelf()))
                    return;
                else
                    item = m_host.TaskInventory[InventorySelf()];
            }

            if (item.PermsGranter == UUID.Zero)
                return;

            if ((item.PermsMask & ScriptBaseClass.PERMISSION_TRIGGER_ANIMATION) != 0)
            {
                IScenePresence presence = World.GetScenePresence(item.PermsGranter);

                if (presence != null)
                {
                    // Do NOT try to parse UUID, animations cannot be triggered by ID
                    UUID animID = InventoryKey(anim, (int)AssetType.Animation);
                    if (animID == UUID.Zero)
                    {
                        if (UUID.TryParse(anim, out animID))
                            presence.Animator.AddAnimation(animID, m_host.UUID);
                        else
                        {
                            bool RetVal = presence.Animator.AddAnimation(anim, m_host.UUID);
                            if (!RetVal)
                            {
                                IChatModule chatModule = World.RequestModuleInterface<IChatModule>();
                                if (chatModule != null)
                                    chatModule.SimChat("Could not find animation '" + anim + "'.",
                                        ChatTypeEnum.DebugChannel, 2147483647, m_host.AbsolutePosition, 
                                        m_host.Name, m_host.UUID, false, World);
                            }
                        }
                    }
                    else
                        presence.Animator.AddAnimation(animID, m_host.UUID);
                }
            }
        }

        public void llStopAnimation(string anim)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            UUID invItemID=InventorySelf();
            if (invItemID == UUID.Zero)
                return;

            TaskInventoryItem item;

            lock (m_host.TaskInventory)
            {
                if (!m_host.TaskInventory.ContainsKey(InventorySelf()))
                    return;
                else
                    item = m_host.TaskInventory[InventorySelf()];
            }

            if (item.PermsGranter == UUID.Zero)
                return;

            if ((item.PermsMask & ScriptBaseClass.PERMISSION_TRIGGER_ANIMATION) != 0)
            {
                UUID animID = new UUID();

                if (!UUID.TryParse(anim, out animID))
                {
                    animID=InventoryKey(anim);
                }

                IScenePresence presence = World.GetScenePresence(item.PermsGranter);

                if (presence != null)
                {
                    if (animID == UUID.Zero)
                    {
                        if (UUID.TryParse(anim, out animID))
                            presence.Animator.RemoveAnimation(animID);
                        else
                        {
                            bool RetVal = presence.Animator.RemoveAnimation(anim);
                            if (!RetVal)
                            {
                                IChatModule chatModule = World.RequestModuleInterface<IChatModule>();
                                if (chatModule != null)
                                    chatModule.SimChat("Could not find animation '" + anim + "'.", 
                                        ChatTypeEnum.DebugChannel, 2147483647, m_host.AbsolutePosition, 
                                        m_host.Name, m_host.UUID, false, World);
                            }
                        }
                    }
                    else
                        presence.Animator.RemoveAnimation(animID);
                }
            }
        }

        public void llPointAt(LSL_Vector pos)
        {
        }

        public void llStopPointAt()
        {
        }

        public void llTargetOmega(LSL_Vector axis, double spinrate, double gain)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.AngularVelocity = new Vector3((float)(axis.x * spinrate), (float)(axis.y * spinrate), (float)(axis.z * spinrate));
            ScriptData script = ScriptProtection.GetScript(this.m_itemID);
            if (script != null)
                script.TargetOmegaWasSet = true;
            m_host.ScheduleTerseUpdate();
            //m_host.SendTerseUpdateToAllClients();
            m_host.ParentEntity.HasGroupChanged = true;
        }

        public LSL_Integer llGetStartParameter()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return m_ScriptEngine.GetStartParameter(m_itemID, m_host.UUID);
        }

        public void llGodLikeRezObject(string inventory, LSL_Vector pos)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            if (m_ScriptEngine.Config.GetBoolean("AllowGodFunctions", false))
            {
                if (World.Permissions.CanRunConsoleCommand(m_host.OwnerID))
                {
                    AssetBase asset = World.AssetService.Get(inventory);
                    SceneObjectGroup group
                                        = SceneObjectSerializer.FromOriginalXmlFormat(UUID.Zero, Utils.BytesToString(asset.Data), World);
                    if (group == null)
                        return;

                    group.IsDeleted = false;
                    group.m_isLoaded = true;
                    foreach (SceneObjectPart part in group.ChildrenList)
                    {
                        part.IsLoading = false;
                    }
                    group.OwnerID = m_host.OwnerID;

                    group.RootPart.AddFlag(PrimFlags.CreateSelected);
                    // If we're rezzing an attachment then don't ask AddNewSceneObject() to update the client since
                    // we'll be doing that later on.  Scheduling more than one full update during the attachment
                    // process causes some clients to fail to display the attachment properly.
                    World.SceneGraph.AddPrimToScene(group);

                    //  m_log.InfoFormat("ray end point for inventory rezz is {0} {1} {2} ", RayEnd.X, RayEnd.Y, RayEnd.Z);
                    // if attachment we set it's asset id so object updates can reflect that
                    // if not, we set it's position in world.
                    group.AbsolutePosition = new Vector3((float)pos.x, (float)pos.y, (float)pos.z);
                    group.ScheduleGroupUpdate(PrimUpdateFlags.FullUpdate);

                    SceneObjectPart rootPart = null;
                    rootPart = (SceneObjectPart)group.GetChildPart(group.UUID);

                    IScenePresence SP = World.GetScenePresence (m_host.OwnerID);
                    if (SP != null)
                        group.SetGroup(m_host.GroupID, SP.ControllingClient);

                    if (group.RootPart.Shape.PCode == (byte)PCode.Prim)
                    {
                        group.ClearPartAttachmentData();
                    }

                    // Fire on_rez
                    group.CreateScriptInstances(0, true, 0, UUID.Zero);
                    rootPart.ParentGroup.ResumeScripts();
                }
            }
        }

        public void llRequestPermissions(string agent, int perm)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            UUID agentID = new UUID();

            if (!UUID.TryParse(agent, out agentID))
                return;

            UUID invItemID = InventorySelf();

            if (invItemID == UUID.Zero)
                return; // Not in a prim? How??

            TaskInventoryItem item;

            lock (m_host.TaskInventory)
            {
                item = m_host.TaskInventory[invItemID];
            }

            if (agentID == UUID.Zero || perm == 0) // Releasing permissions
            {
                llReleaseControls();

                item.PermsGranter = UUID.Zero;
                item.PermsMask = 0;

                m_ScriptEngine.PostScriptEvent(m_itemID, m_host.UUID, new EventParams(
                        "run_time_permissions", new Object[] {
                        new LSL_Integer(0) },
                        new DetectParams[0]));

                return;
            }

            if (item.PermsGranter != agentID || (perm & ScriptBaseClass.PERMISSION_TAKE_CONTROLS) == 0)
                llReleaseControls();



            if (m_host.ParentEntity.IsAttachment && (UUID)agent == m_host.ParentEntity.RootChild.AttachedAvatar)
            {
                // When attached, certain permissions are implicit if requested from owner
                int implicitPerms = ScriptBaseClass.PERMISSION_TAKE_CONTROLS |
                        ScriptBaseClass.PERMISSION_TRIGGER_ANIMATION |
                        ScriptBaseClass.PERMISSION_CONTROL_CAMERA |
                        ScriptBaseClass.PERMISSION_ATTACH |
                        ScriptBaseClass.PERMISSION_TRACK_CAMERA;

                if ((perm & (~implicitPerms)) == 0) // Requested only implicit perms
                {
                    lock (m_host.TaskInventory)
                    {
                        m_host.TaskInventory[invItemID].PermsGranter = agentID;
                        m_host.TaskInventory[invItemID].PermsMask = perm;
                    }

                    m_ScriptEngine.PostScriptEvent(m_itemID, m_host.UUID, new EventParams(
                            "run_time_permissions", new Object[] {
                            new LSL_Integer(perm) },
                            new DetectParams[0]));

                    return;
                }
            }
            else if (m_host.SitTargetAvatar.Contains(agentID)) // Sitting avatar
            {
                // When agent is sitting, certain permissions are implicit if requested from sitting agent
                int implicitPerms = ScriptBaseClass.PERMISSION_TRIGGER_ANIMATION |
                    ScriptBaseClass.PERMISSION_CONTROL_CAMERA |
                    ScriptBaseClass.PERMISSION_TRACK_CAMERA |
                    ScriptBaseClass.PERMISSION_TAKE_CONTROLS;

                if ((perm & (~implicitPerms)) == 0) // Requested only implicit perms
                {
                    lock (m_host.TaskInventory)
                    {
                        m_host.TaskInventory[invItemID].PermsGranter = agentID;
                        m_host.TaskInventory[invItemID].PermsMask = perm;
                    }

                    m_ScriptEngine.PostScriptEvent(m_itemID, m_host.UUID, new EventParams(
                            "run_time_permissions", new Object[] {
                            new LSL_Integer(perm) },
                            new DetectParams[0]));

                    return;
                }
            }

            IScenePresence presence = World.GetScenePresence (agentID);

            if (presence != null)
            {
                string ownerName = "";
                IScenePresence ownerPresence = World.GetScenePresence (m_host.ParentEntity.RootChild.OwnerID);
                if (ownerPresence == null)
                    ownerName = resolveName(m_host.OwnerID);
                else
                    ownerName = ownerPresence.Name;

                if (ownerName == String.Empty)
                    ownerName = "(hippos)";

                if (!m_waitingForScriptAnswer)
                {
                    lock (m_host.TaskInventory)
                    {
                        m_host.TaskInventory[invItemID].PermsGranter = agentID;
                        m_host.TaskInventory[invItemID].PermsMask = 0;
                    }

                    presence.ControllingClient.OnScriptAnswer += handleScriptAnswer;
                    m_waitingForScriptAnswer=true;
                }

                presence.ControllingClient.SendScriptQuestion(
                    m_host.UUID, m_host.ParentEntity.RootChild.Name, ownerName, invItemID, perm);

                return;
            }

            // Requested agent is not in range, refuse perms
            m_ScriptEngine.PostScriptEvent(m_itemID, m_host.UUID, new EventParams(
                    "run_time_permissions", new Object[] {
                    new LSL_Integer(0) },
                    new DetectParams[0]));
        }

        void handleScriptAnswer(IClientAPI client, UUID taskID, UUID itemID, int answer)
        {
            if (taskID != m_host.UUID)
                return;

            UUID invItemID = InventorySelf();

            if (invItemID == UUID.Zero)
                return;

            client.OnScriptAnswer-=handleScriptAnswer;
            m_waitingForScriptAnswer=false;

            if ((answer & ScriptBaseClass.PERMISSION_TAKE_CONTROLS) == 0)
                llReleaseControls();

            lock (m_host.TaskInventory)
            {
                m_host.TaskInventory[invItemID].PermsMask = answer;
            }

            m_ScriptEngine.PostScriptEvent(m_itemID, m_host.UUID, new EventParams(
                    "run_time_permissions", new Object[] {
                    new LSL_Integer(answer) },
                    new DetectParams[0]));
        }

        public LSL_String llGetPermissionsKey()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            lock (m_host.TaskInventory)
            {
                foreach (TaskInventoryItem item in m_host.TaskInventory.Values)
                {
                    if (item.Type == 10 && item.ItemID == m_itemID)
                    {
                        return item.PermsGranter.ToString();
                    }
                }
            }

            return UUID.Zero.ToString();
        }

        public LSL_Integer llGetPermissions()
        {
            

            lock (m_host.TaskInventory)
            {
                foreach (TaskInventoryItem item in m_host.TaskInventory.Values)
                {
                    if (item.Type == 10 && item.ItemID == m_itemID)
                    {
                        int perms = item.PermsMask;
                        if (m_automaticLinkPermission)
                            perms |= ScriptBaseClass.PERMISSION_CHANGE_LINKS;
                        return perms;
                    }
                }
            }

            return 0;
        }

        public LSL_Integer llGetLinkNumber()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");


            if (m_host.ParentEntity.ChildrenEntities().Count > 1)
            {
                return m_host.LinkNum;
            }
            else
            {
                return 0;
            }
        }

        public void llSetLinkColor(int linknumber, LSL_Vector color, int face)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            List<ISceneChildEntity> parts = GetLinkParts(linknumber);

            foreach (ISceneChildEntity part in parts)
                part.SetFaceColor(new Vector3((float)color.x, (float)color.y, (float)color.z), face);
        }

        public void llCreateLink(string target, int parent)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            UUID invItemID = InventorySelf();
            UUID targetID;

            if (!UUID.TryParse(target, out targetID))
                return;

            TaskInventoryItem item;
            lock (m_host.TaskInventory)
            {
                item = m_host.TaskInventory[invItemID];
            }

            if ((item.PermsMask & ScriptBaseClass.PERMISSION_CHANGE_LINKS) == 0
                && !m_automaticLinkPermission)
            {
                ShoutError("Script trying to link but PERMISSION_CHANGE_LINKS permission not set!");
                return;
            }

            IClientAPI client = null;
            IScenePresence sp = World.GetScenePresence(item.PermsGranter);
            if (sp != null)
                client = sp.ControllingClient;

            ISceneChildEntity targetPart = World.GetSceneObjectPart (targetID);

            if (targetPart.ParentEntity.RootChild.AttachmentPoint != 0)
                return; ; // Fail silently if attached
            ISceneEntity parentPrim = null, childPrim = null;

            if (targetPart != null)
            {
                if (parent != 0) {
                    parentPrim = m_host.ParentEntity;
                    childPrim = targetPart.ParentEntity;
                }
                else
                {
                    parentPrim = targetPart.ParentEntity;
                    childPrim = m_host.ParentEntity;
                }
//                byte uf = childPrim.RootPart.UpdateFlag;
                parentPrim.LinkToGroup(childPrim);
//                if (uf != (Byte)0)
//                    parent.RootPart.UpdateFlag = uf;
            }

            parentPrim.TriggerScriptChangedEvent(Changed.LINK);
            parentPrim.RootChild.CreateSelected = true;
            parentPrim.HasGroupChanged = true;
            parentPrim.ScheduleGroupUpdate(PrimUpdateFlags.FullUpdate);

            if (client != null)
                parentPrim.GetProperties(client);

            ScriptSleep(1000);
        }

        public void llBreakLink(int linknum)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            UUID invItemID = InventorySelf();

            lock (m_host.TaskInventory)
            {
                if ((m_host.TaskInventory[invItemID].PermsMask & ScriptBaseClass.PERMISSION_CHANGE_LINKS) == 0
                    && !m_automaticLinkPermission)
                {
                    ShoutError("Script trying to link but PERMISSION_CHANGE_LINKS permission not set!");
                    return;
                }
            }

            if (linknum < ScriptBaseClass.LINK_THIS)
                return;

            ISceneEntity parentPrim = m_host.ParentEntity;

            if (parentPrim.RootChild.AttachmentPoint != 0)
                return; // Fail silently if attached
            ISceneChildEntity childPrim = null;

            if(linknum == ScriptBaseClass.LINK_ROOT)
            {
            }
            else if (linknum == ScriptBaseClass.LINK_SET ||
                ScriptBaseClass.LINK_ALL_OTHERS ||
                ScriptBaseClass.LINK_ALL_CHILDREN ||
                ScriptBaseClass.LINK_THIS)
            {
                foreach (ISceneChildEntity part in parentPrim.ChildrenEntities())
                {
                    if (part.UUID != m_host.UUID)
                    {
                        childPrim = part;
                        break;
                    }
                }
            }
            else
            {
                IEntity target = m_host.ParentEntity.GetLinkNumPart (linknum);
                if (target is ISceneChildEntity)
                {
                    childPrim = target as ISceneChildEntity;
                }
                else
                    return;
                if (childPrim.UUID == m_host.UUID)
                    childPrim = null;
            }

            if (linknum == ScriptBaseClass.LINK_ROOT)
            {
                // Restructuring Multiple Prims.
                List<ISceneChildEntity> parts = new List<ISceneChildEntity> (parentPrim.ChildrenEntities());
                parts.Remove(parentPrim.RootChild);
                foreach (ISceneChildEntity part in parts)
                {
                    parentPrim.DelinkFromGroup(part, true);
                }
                parentPrim.HasGroupChanged = true;
                parentPrim.ScheduleGroupUpdate(PrimUpdateFlags.FullUpdate);
                parentPrim.TriggerScriptChangedEvent(Changed.LINK);

                if (parts.Count > 0)
                {
                    ISceneChildEntity newRoot = parts[0];
                    parts.Remove(newRoot);
                    foreach (SceneObjectPart part in parts)
                    {
                        newRoot.ParentEntity.LinkToGroup (part.ParentGroup);
                    }
                    newRoot.ParentEntity.HasGroupChanged = true;
                    newRoot.ParentEntity.ScheduleGroupUpdate (PrimUpdateFlags.FullUpdate);
                }
            }
            else
            {
                if (childPrim == null)
                    return;

                parentPrim.DelinkFromGroup(childPrim, true);
                parentPrim.HasGroupChanged = true;
                parentPrim.ScheduleGroupUpdate(PrimUpdateFlags.FullUpdate);
                parentPrim.TriggerScriptChangedEvent(Changed.LINK);
            }
        }

        public void llBreakAllLinks()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            ISceneEntity parentPrim = m_host.ParentEntity;
            if (parentPrim.RootChild.AttachmentPoint != 0)
                return; // Fail silently if attached

            List<ISceneChildEntity> parts = new List<ISceneChildEntity> (parentPrim.ChildrenEntities ());
            parts.Remove(parentPrim.RootChild);

            foreach (ISceneChildEntity part in parts)
            {
                parentPrim.DelinkFromGroup(part, true);
                parentPrim.TriggerScriptChangedEvent(Changed.LINK);
            }
            parentPrim.HasGroupChanged = true;
            parentPrim.ScheduleGroupUpdate(PrimUpdateFlags.FullUpdate);
        }

        public LSL_String llGetLinkKey(int linknum)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            IEntity target = m_host.ParentEntity.GetLinkNumPart (linknum);
            if (target != null)
            {
                return target.UUID.ToString();
            }
            else
            {
                return UUID.Zero.ToString();
            }
        }

        /// <summary>
        /// The rules governing the returned name are not simple. The only
        /// time a blank name is returned is if the target prim has a blank
        /// name. If no prim with the given link number can be found then
        /// usually NULL_KEY is returned but there are exceptions.
        ///
        /// In a single unlinked prim, A call with 0 returns the name, all
        /// other values for link number return NULL_KEY
        ///
        /// In link sets it is more complicated.
        ///
        /// If the script is in the root prim:-
        ///     A zero link number returns NULL_KEY.
        ///     Positive link numbers return the name of the prim, or NULL_KEY
        ///     if a prim does not exist at that position.
        ///     Negative link numbers return the name of the first child prim.
        ///
        /// If the script is in a child prim:-
        ///     Link numbers 0 or 1 return the name of the root prim.
        ///     Positive link numbers return the name of the prim or NULL_KEY
        ///     if a prim does not exist at that position.
        ///     Negative numbers return the name of the root prim.
        ///
        /// References
        /// http://lslwiki.net/lslwiki/wakka.php?wakka=llGetLinkName
        /// Mentions NULL_KEY being returned
        /// http://wiki.secondlife.com/wiki/LlGetLinkName
        /// Mentions using the LINK_* constants, some of which are negative
        /// </summary>
        public LSL_String llGetLinkName(int linknum)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            // simplest case, this prims link number
            if (m_host.LinkNum == linknum)
                return m_host.Name;

            // Single prim
            if (m_host.LinkNum == 0)
            {
                if (linknum == 1)
                    return m_host.Name;
                else
                {
                    IEntity entity = m_host.ParentEntity.GetLinkNumPart (linknum);
                    if (entity != null)
                        return entity.Name;
                    else
                        return UUID.Zero.ToString();
                }
            }
            // Link set
            IEntity part = null;
            if (m_host.LinkNum == 1) // this is the Root prim
            {
                if (linknum < 0)
                {
                    part = m_host.ParentEntity.GetLinkNumPart (2);
                }
                else
                {
                    part = m_host.ParentEntity.GetLinkNumPart (linknum);
                }
            }
            else // this is a child prim
            {
                if (linknum < 2)
                {
                    part = m_host.ParentEntity.GetLinkNumPart (1);
                }
                else
                {
                    part = m_host.ParentEntity.GetLinkNumPart (linknum);
                }
            }
            if (part != null)
                return part.Name;
            else
                return UUID.Zero.ToString();
        }

        public LSL_Integer llGetInventoryNumber(int type)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            int count = 0;

            lock (m_host.TaskInventory)
            {
                foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory)
                {
                    if (inv.Value.Type == type || type == -1)
                    {
                        count = count + 1;
                    }
                }
            }

            return count;
        }

        public LSL_String llGetInventoryName(int type, int number)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            ArrayList keys = new ArrayList();

            lock (m_host.TaskInventory)
            {
                foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory)
                {
                    if (inv.Value.Type == type || type == -1)
                    {
                        keys.Add(inv.Value.Name);
                    }
                }
            }

            if (keys.Count == 0)
            {
                return String.Empty;
            }
            keys.Sort();
            if (keys.Count > number)
            {
                return (string)keys[number];
            }
            return String.Empty;
        }

        public LSL_Float llGetEnergy()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return 1.0f;
        }

        public void llGiveInventory(string destination, string inventory)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            bool found = false;
            UUID destId = UUID.Zero;
            UUID objId = UUID.Zero;
            int assetType = 0;
            string objName = String.Empty;

            if (!UUID.TryParse(destination, out destId))
            {
                llSay(0, "Could not parse key " + destination);
                return;
            }

            // move the first object found with this inventory name
            lock (m_host.TaskInventory)
            {
                foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory)
                {
                    if (inv.Value.Name == inventory)
                    {
                        found = true;
                        objId = inv.Key;
                        assetType = inv.Value.Type;
                        objName = inv.Value.Name;
                        break;
                    }
                }
            }

            if (!found)
            {
                llSay(0, String.Format("Could not find object '{0}'", inventory));
                throw new Exception(String.Format("The inventory object '{0}' could not be found", inventory));
            }

            UserInfo info = m_host.ParentEntity.Scene.RequestModuleInterface<IAgentInfoService> ().GetUserInfo (destId.ToString ());

            // check if destination is an avatar
            if ((info != null && info.IsOnline) || World.GetScenePresence(destId) != null)
            {
                // destination is an avatar
                InventoryItemBase agentItem = null;
                ILLClientInventory inventoryModule = World.RequestModuleInterface<ILLClientInventory>();
                if(inventoryModule != null)
                    agentItem = inventoryModule.MoveTaskInventoryItemToUserInventory(destId, UUID.Zero, m_host, objId);

                if (agentItem == null)
                    return;

                byte[] bucket = new byte[17];
                bucket[0] = (byte)assetType;
                byte[] objBytes = agentItem.ID.GetBytes();
                Array.Copy(objBytes, 0, bucket, 1, 16);

                //m_log.Debug("Giving inventory to " + destId + " from " + m_host.Name);
                GridInstantMessage msg = new GridInstantMessage(World,
                        m_host.UUID, m_host.Name+", an object owned by "+
                        resolveName(m_host.OwnerID)+",", destId,
                        (byte)InstantMessageDialog.InventoryOffered,
                        false, objName+"'\n'"+m_host.Name+"' is located at "+
                        m_host.AbsolutePosition.ToString() + " in '" + World.RegionInfo.RegionName,
                        agentItem.ID, true, m_host.AbsolutePosition,
                        bucket);

                if (m_TransferModule != null)
                    m_TransferModule.SendInstantMessage(msg);
            }
            else
            {
                // destination is an object
                ILLClientInventory inventoryModule = World.RequestModuleInterface<ILLClientInventory>();
                if (inventoryModule != null)
                    inventoryModule.MoveTaskInventoryItemToObject(destId, m_host, objId);
            }
            ScriptSleep(3000);
        }

        public void llRemoveInventory(string name)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            lock (m_host.TaskInventory)
            {
                foreach (TaskInventoryItem item in m_host.TaskInventory.Values)
                {
                    if (item.Name == name)
                    {
                        if (item.ItemID == m_itemID)
                            throw new ScriptDeleteException();
                        else
                            m_host.Inventory.RemoveInventoryItem(item.ItemID);
                        return;
                    }
                }
            }
        }

        public void llSetText(string text, LSL_Vector color, LSL_Float alpha)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            Vector3 av3 = new Vector3(Util.Clip((float)color.x, 0.0f, 1.0f),
                                      Util.Clip((float)color.y, 0.0f, 1.0f),
                                      Util.Clip((float)color.z, 0.0f, 1.0f));
            m_host.SetText(text, av3, Util.Clip((float)alpha, 0.0f, 1.0f));
            //m_host.ParentGroup.HasGroupChanged = true;
            //m_host.ParentGroup.ScheduleGroupForFullUpdate();
        }

        public LSL_Float llWater(LSL_Vector offset)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return World.RegionInfo.RegionSettings.WaterHeight;
        }

        public void llPassTouches(int pass)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.PassTouch = pass;
        }

        public LSL_Key llRequestAgentData(string id, int data)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            UUID uuid = (UUID)id;
            UserInfo pinfo = null;
            UserAccount account;

            UserInfoCacheEntry ce;
            if (!m_userInfoCache.TryGetValue(uuid, out ce))
            {
                account = World.UserAccountService.GetUserAccount(World.RegionInfo.ScopeID, uuid);
                if (account == null)
                {
                    m_userInfoCache[uuid] = null; // Cache negative
                    return UUID.Zero.ToString();
                }

                ce = new UserInfoCacheEntry();
                ce.time = Util.EnvironmentTickCount();
                ce.account = account;
                pinfo = World.RequestModuleInterface<IAgentInfoService>().GetUserInfo(uuid.ToString());
                ce.pinfo = pinfo;
            }
            else
            {
                if (ce == null)
                {
                    return (LSL_Key)UUID.Zero.ToString();
                }
                account = ce.account;
                pinfo = ce.pinfo;
            }

            if (Util.EnvironmentTickCount() < ce.time || (Util.EnvironmentTickCount() - ce.time) >= 20000)
            {
                ce.time = Util.EnvironmentTickCount();
                ce.pinfo = World.RequestModuleInterface<IAgentInfoService>().GetUserInfo(uuid.ToString());
                pinfo = ce.pinfo;
            }

            string reply = String.Empty;

            switch (data)
            {
                case 1: // DATA_ONLINE (0|1)
                    if (pinfo != null && pinfo.IsOnline)
                        reply = "1";
                    else
                        reply = "0";
                    break;
                case 2: // DATA_NAME (First Last)
                    reply = account.Name;
                    break;
                case 3: // DATA_BORN (YYYY-MM-DD)
                    DateTime born = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    born = born.AddSeconds(account.Created);
                    reply = born.ToString("yyyy-MM-dd");
                    break;
                case 4: // DATA_RATING (0,0,0,0,0,0)
                    reply = "0,0,0,0,0,0";
                    break;
                case 8: // DATA_PAYINFO (0|1|2|3)
                    reply = "0";
                    break;
                default:
                    return (LSL_Key) UUID.Zero.ToString(); // Raise no event
            }

            UUID rq = UUID.Random();

            DataserverPlugin dataserverPlugin = (DataserverPlugin)m_ScriptEngine.GetScriptPlugin("Dataserver");
            UUID tid = dataserverPlugin.RegisterRequest(m_host.UUID,
                                             m_itemID, rq.ToString());

            dataserverPlugin.AddReply(rq.ToString(), reply, 100);

            ScriptSleep(200);
            return (LSL_Key)tid.ToString();
            
        }

        public LSL_Key llRequestInventoryData(string name)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            TaskInventoryDictionary itemDictionary = (TaskInventoryDictionary)m_host.TaskInventory.Clone();

            foreach (TaskInventoryItem item in itemDictionary.Values)
            {
                if (item.Type == 3 && item.Name == name)
                {
                    DataserverPlugin dataserverPlugin = (DataserverPlugin)m_ScriptEngine.GetScriptPlugin("Dataserver");

                    UUID tid = dataserverPlugin.RegisterRequest(m_host.UUID,
                                                     m_itemID, item.AssetID.ToString());

                    Vector3 region = new Vector3(
                        World.RegionInfo.RegionLocX,
                        World.RegionInfo.RegionLocY,
                        0);

                    World.AssetService.Get(item.AssetID.ToString(), this,
                        delegate(string i, object sender, AssetBase a)
                        {
                            AssetLandmark lm = new AssetLandmark(a);

                            float rx = (uint)(lm.RegionHandle >> 32);
                            float ry = (uint)lm.RegionHandle;
                            region = lm.Position + new Vector3(rx, ry, 0) - region;

                            string reply = region.ToString();
                            dataserverPlugin.AddReply(item.AssetID.ToString(),
                                                             reply, 1000);
                        });

                    ScriptSleep(1000);
                    return (LSL_Key)tid.ToString();                   
                }
            }
            ScriptSleep(1000);
            return (LSL_Key) String.Empty;
        }

        public void llSetDamage(double damage)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.ParentEntity.Damage = (float)damage;
        }

        public void llTeleportAgentHome(LSL_Key _agent)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            string agent = _agent.ToString();

            UUID agentId = new UUID();
            if (UUID.TryParse(agent, out agentId))
            {
                IScenePresence presence = World.GetScenePresence (agentId);
                if (presence != null)
                {
                    // agent must be over the owners land
                    IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
                    if (parcelManagement != null)
                    {
                        if (m_host.OwnerID != parcelManagement.GetLandObject(
                            presence.AbsolutePosition.X, presence.AbsolutePosition.Y).LandData.OwnerID &&
                            !World.Permissions.CanIssueEstateCommand(m_host.OwnerID, false))
                        {
                            ScriptSleep(5000);
                        }
                    }

                    //Send disable cancel so that the agent cannot attempt to stay in the region
                    presence.ControllingClient.SendTeleportStart((uint)TeleportFlags.DisableCancel);
                    IEntityTransferModule transferModule = World.RequestModuleInterface<IEntityTransferModule>();
                    if (transferModule != null)
                        transferModule.TeleportHome(agentId, presence.ControllingClient);
                    else
                        presence.ControllingClient.SendTeleportFailed("Unable to perform teleports on this simulator.");
                }
            }
            ScriptSleep(5000);
        }

        public void llTextBox(string agent, string message, int chatChannel)
        {
            IDialogModule dm = World.RequestModuleInterface<IDialogModule>();

            if (dm == null)
                return;

            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            UUID av = new UUID();
            if (!UUID.TryParse(agent, out av))
            {
                LSLError("First parameter to llDialog needs to be a key");
                return;
            }

            if (message != null && message.Length > 1024)
                message = message.Substring(0, 1024);

            dm.SendTextBoxToUser(av, message, chatChannel, m_host.Name, m_host.UUID, m_host.OwnerID);
            ScriptSleep(1000);
        }

        public void llModifyLand(int action, int brush)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            ITerrainModule tm = World.RequestModuleInterface<ITerrainModule>();
            if (tm != null)
            {
                tm.ModifyTerrain(m_host.OwnerID, m_host.AbsolutePosition, (byte) brush, (byte) action, m_host.OwnerID);
            }
        }

        public void llCollisionSound(string impact_sound, double impact_volume)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            UUID soundId = UUID.Zero;
            if (!UUID.TryParse(impact_sound, out soundId))
            {
                lock (m_host.TaskInventory)
                {
                    foreach (TaskInventoryItem item in m_host.TaskInventory.Values)
                    {
                        if (item.Type == (int)AssetType.Sound && item.Name == impact_sound)
                        {
                            soundId = item.AssetID;
                            break;
                        }
                    }
                }
            }
            if (soundId != UUID.Zero)
            {
                m_host.CollisionSound = soundId;
                m_host.CollisionSoundVolume = (float)impact_volume;
            }
        }

        public void llCollisionSprite(string impact_sprite)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            // Since this is broken in SL, we can do this however we want, until they fix it.
            m_host.CollisionSprite = UUID.Parse(impact_sprite);
        }

        public LSL_String llGetAnimation(string id)
        {
            // This should only return a value if the avatar is in the same region
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            UUID avatar = (UUID)id;
            IScenePresence presence = World.GetScenePresence(avatar);
            if (presence == null)
                return "";

            if (m_host.ParentEntity.Scene.RegionInfo.RegionHandle == presence.Scene.RegionInfo.RegionHandle)
            {
                Dictionary<UUID, string> animationstateNames = AnimationSet.Animations.AnimStateNames;

                if (presence != null)
                {
                    AnimationSet currentAnims = presence.Animator.Animations;
                    string currentAnimationState = String.Empty;
                    if (animationstateNames.TryGetValue(currentAnims.DefaultAnimation.AnimID, out currentAnimationState))
                        return currentAnimationState;
                }
            }
            
            return String.Empty;
        }

        public void llMessageLinked(int linknumber, int num, string msg, string id)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");


            List<ISceneChildEntity> parts = GetLinkParts (linknumber);

            UUID partItemID;
            foreach (ISceneChildEntity part in parts)
            {
                TaskInventoryDictionary itemsDictionary = (TaskInventoryDictionary)((ICloneable)part.TaskInventory).Clone();
                foreach (TaskInventoryItem item in itemsDictionary.Values)
                {
                    if (item.Type == ScriptBaseClass.INVENTORY_SCRIPT)
                    {
                        partItemID = item.ItemID;
                        int linkNumber = m_host.LinkNum;
                        if (m_host.ParentEntity.ChildrenEntities().Count == 1)
                            linkNumber = 0;

                        object[] resobj = new object[]
                                  {
                                      new LSL_Integer(linkNumber), new LSL_Integer(num), new LSL_String(msg), new LSL_String(id)
                                  };

                        m_ScriptEngine.PostScriptEvent(partItemID, part.UUID,
                                new EventParams("link_message",
                                resobj, new DetectParams[0]));
                    }
                }
            }
        }

        public void llPushObject(string target, LSL_Vector impulse, LSL_Vector ang_impulse, int local)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            bool pushrestricted = World.RegionInfo.RegionSettings.RestrictPushing;
            bool pushAllowed = false;

            bool pusheeIsAvatar = false;
            UUID targetID = UUID.Zero;

            if (!UUID.TryParse(target,out targetID))
                return;

            IScenePresence pusheeav = null;
            Vector3 PusheePos = Vector3.Zero;
            ISceneChildEntity pusheeob = null;

            IScenePresence avatar = World.GetScenePresence (targetID);
            if (avatar != null)
            {
                pusheeIsAvatar = true;

                // Pushee doesn't have a physics actor
                if (avatar.PhysicsActor == null)
                    return;

                // Pushee is in GodMode this pushing object isn't owned by them
                if (avatar.GodLevel > 0 && m_host.OwnerID != targetID)
                    return;

                pusheeav = avatar;

                // Find pushee position
                // Pushee Linked?
                if (pusheeav.ParentID != UUID.Zero)
                {
                    ISceneChildEntity parentobj = World.GetSceneObjectPart (pusheeav.ParentID);
                    if (parentobj != null)
                    {
                        PusheePos = parentobj.AbsolutePosition;
                    }
                    else
                    {
                        PusheePos = pusheeav.AbsolutePosition;
                    }
                }
                else
                {
                    PusheePos = pusheeav.AbsolutePosition;
                }
            }

            if (!pusheeIsAvatar)
            {
                // not an avatar so push is not affected by parcel flags
                pusheeob = World.GetSceneObjectPart((UUID)target);

                // We can't find object
                if (pusheeob == null)
                    return;

                // Object not pushable.  Not an attachment and has no physics component
                if (!pusheeob.IsAttachment && pusheeob.PhysActor == null)
                    return;

                PusheePos = pusheeob.AbsolutePosition;
                pushAllowed = true;
            }
            else
            {
                IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
                if (pushrestricted)
                {
                    if (parcelManagement != null)
                    {
                        ILandObject targetlandObj = parcelManagement.GetLandObject(PusheePos.X, PusheePos.Y);

                        // We didn't find the parcel but region is push restricted so assume it is NOT ok
                        if (targetlandObj == null)
                            return;

                        // Need provisions for Group Owned here
                        if (m_host.OwnerID == targetlandObj.LandData.OwnerID ||
                            targetlandObj.LandData.IsGroupOwned || m_host.OwnerID == targetID)
                        {
                            pushAllowed = true;
                        }
                    }
                }
                else
                {
                    if (parcelManagement != null)
                    {
                        ILandObject targetlandObj = parcelManagement.GetLandObject(PusheePos.X, PusheePos.Y);
                        if (targetlandObj == null)
                        {
                            // We didn't find the parcel but region isn't push restricted so assume it's ok
                            pushAllowed = true;
                        }
                        else
                        {
                            // Parcel push restriction
                            if ((targetlandObj.LandData.Flags & (uint)ParcelFlags.RestrictPushObject) == (uint)ParcelFlags.RestrictPushObject)
                            {
                                //This takes care of everything
                                pushAllowed = m_host.ParentEntity.Scene.Permissions.CanPushObject (m_host.OwnerID, targetlandObj);
                                // Need provisions for Group Owned here
                                /*if (m_host.OwnerID == targetlandObj.LandData.OwnerID || 
                                    targetlandObj.LandData.IsGroupOwned || 
                                    m_host.OwnerID == targetID)
                                {
                                    pushAllowed = true;
                                }*/

                                //ParcelFlags.RestrictPushObject
                                //pushAllowed = true;
                            }
                            else
                            {
                                // Parcel isn't push restricted
                                pushAllowed = true;
                            }
                        }
                    }
                }
            }
            if (pushAllowed)
            {
                float distance = (PusheePos - m_host.AbsolutePosition).Length();
                float distance_term = distance * distance * distance; // Script Energy
                float pusher_mass = m_host.GetMass();

                float PUSH_ATTENUATION_DISTANCE = 17f;
                float PUSH_ATTENUATION_SCALE = 5f;
                float distance_attenuation = 1f;
                if (distance > PUSH_ATTENUATION_DISTANCE)
                {
                    float normalized_units = 1f + (distance - PUSH_ATTENUATION_DISTANCE) / PUSH_ATTENUATION_SCALE;
                    distance_attenuation = 1f / normalized_units;
                }

                Vector3 applied_linear_impulse = new Vector3((float)impulse.x, (float)impulse.y, (float)impulse.z);
                {
                    float impulse_length = applied_linear_impulse.Length();

                    float desired_energy = impulse_length * pusher_mass;
                    if (desired_energy > 0f)
                        desired_energy += distance_term;

                    float scaling_factor = 1f;
                    scaling_factor *= distance_attenuation;
                    applied_linear_impulse *= scaling_factor;

                }
                if (pusheeIsAvatar)
                {
                    if (pusheeav != null)
                    {
                        if (pusheeav.PhysicsActor != null)
                        {
                            if (local != 0)
                            {
                                applied_linear_impulse *= m_host.GetWorldRotation();
                            }
                            //Put a limit on it...
                            int MaxPush = (int)pusheeav.PhysicsActor.Mass * 25;

                            if (applied_linear_impulse.X > 0 &&
                                Math.Abs(applied_linear_impulse.X) > MaxPush)
                                applied_linear_impulse.X = MaxPush;
                            if (applied_linear_impulse.X < 0 &&
                                Math.Abs(applied_linear_impulse.X) > MaxPush)
                                applied_linear_impulse.X = -MaxPush;

                            if (applied_linear_impulse.Y > 0 &&
                                Math.Abs(applied_linear_impulse.X) > MaxPush)
                                applied_linear_impulse.Y = MaxPush;
                            if (applied_linear_impulse.Y < 0 &&
                                Math.Abs(applied_linear_impulse.X) > MaxPush)
                                applied_linear_impulse.Y = -MaxPush;

                            if (applied_linear_impulse.Z > 0 &&
                                Math.Abs(applied_linear_impulse.X) > MaxPush)
                                applied_linear_impulse.Z = MaxPush;
                            if (applied_linear_impulse.Z < 0 &&
                                Math.Abs(applied_linear_impulse.Z) > MaxPush)
                                applied_linear_impulse.Z = -MaxPush;

                            pusheeav.PhysicsActor.AddForce(applied_linear_impulse, true);
                        }
                    }
                }
                else
                {
                    if (pusheeob != null)
                    {
                        if (pusheeob.PhysActor != null)
                        {
                            pusheeob.ApplyImpulse(applied_linear_impulse, local != 0);
                        }
                    }
                }
            }
        }

        public void llPassCollisions(int pass)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.PassCollisions = pass;
        }

        public LSL_String llGetScriptName()
        {
            string result = String.Empty;

            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            lock (m_host.TaskInventory)
            {
                foreach (TaskInventoryItem item in m_host.TaskInventory.Values)
                {
                    if (item.Type == 10 && item.ItemID == m_itemID)
                    {
                        result = item.Name != null ? item.Name : String.Empty;
                        break;
                    }
                }
            }

            return result;
        }

        public LSL_Integer llGetNumberOfSides()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            return GetNumberOfSides(m_host);
        }

        protected int GetNumberOfSides (ISceneChildEntity part)
        {
            int sides = part.GetNumberOfSides();

            if (part.GetPrimType() == PrimType.SPHERE && part.Shape.ProfileHollow > 0)
            {
                // Make up for a bug where LSL shows 4 sides rather than 2
                sides += 2;
            }

            return sides;
        }
        

        /* The new / changed functions were tested with the following LSL script:

        default
        {
            state_entry()
            {
                rotation rot = llEuler2Rot(<0,70,0> * DEG_TO_RAD);

                llOwnerSay("to get here, we rotate over: "+ (string) llRot2Axis(rot));
                llOwnerSay("and we rotate for: "+ (llRot2Angle(rot) * RAD_TO_DEG));

                // convert back and forth between quaternion <-> vector and angle

                rotation newrot = llAxisAngle2Rot(llRot2Axis(rot),llRot2Angle(rot));

                llOwnerSay("Old rotation was: "+(string) rot);
                llOwnerSay("re-converted rotation is: "+(string) newrot);

                llSetRot(rot);  // to check the parameters in the prim
            }
        }
        */

        // Xantor 29/apr/2008
        // Returns rotation described by rotating angle radians about axis.
        // q = cos(a/2) + i (x * sin(a/2)) + j (y * sin(a/2)) + k (z * sin(a/2))
        public LSL_Rotation llAxisAngle2Rot(LSL_Vector axis, double angle)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            double x, y, z, s, t;

            s = Math.Cos(angle * 0.5);
            t = Math.Sin(angle * 0.5); // temp value to avoid 2 more sin() calcs
            x = axis.x * t;
            y = axis.y * t;
            z = axis.z * t;

            return new LSL_Rotation(x,y,z,s);
        }


        // Xantor 29/apr/2008
        // converts a Quaternion to X,Y,Z axis rotations
        public LSL_Vector llRot2Axis(LSL_Rotation rot)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            double x,y,z;

            if (rot.s > 1) // normalization needed
            {
                double length = Math.Sqrt(rot.x * rot.x + rot.y * rot.y +
                        rot.z * rot.z + rot.s * rot.s);
                if (length == 0)
                    return new LSL_Vector(0, 0, 0);
                length = 1 / length;
                rot.x *= length;
                rot.y *= length;
                rot.z *= length;
                rot.s *= length;
            }

            // double angle = 2 * Math.Acos(rot.s);
            double s = Math.Sqrt(1 - rot.s * rot.s);
            if (s < 0.001)
            {
                x = 1;
                y = z = 0;
            }
            else
            {
                s = 1 / s;
                x = rot.x * s; // normalise axis
                y = rot.y * s;
                z = rot.z * s;
            }

            return new LSL_Vector(x,y,z);
        }


        // Returns the angle of a quaternion (see llRot2Axis for the axis)
        public LSL_Float llRot2Angle(LSL_Rotation rot)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            if (rot.s > 1) // normalization needed
            {
                double length = Math.Sqrt(rot.x * rot.x + rot.y * rot.y +
                        rot.z * rot.z + rot.s * rot.s);

                if (length == 0)
                    return 0;
//                rot.x /= length;
//                rot.y /= length;
//                rot.z /= length;
                rot.s /= length;
            }

            double angle = 2 * Math.Acos(rot.s);

            return angle;
        }

        public LSL_Float llAcos(double val)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return (double)Math.Acos(val);
        }

        public LSL_Float llAsin(double val)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return (double)Math.Asin(val);
        }

        // Xantor 30/apr/2008
        public LSL_Float llAngleBetween(LSL_Rotation a, LSL_Rotation b)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            double angle = Math.Acos(a.x * b.x + a.y * b.y + a.z * b.z + a.s * b.s) * 2;
            if (angle < 0) angle = -angle;
            if (angle > Math.PI) return (Math.PI * 2 - angle);
            return angle;
        }

        public LSL_String llGetInventoryKey(string name)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            lock (m_host.TaskInventory)
            {
                foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory)
                {
                    if (inv.Value.Name == name)
                    {
                        if ((inv.Value.CurrentPermissions & (uint)(PermissionMask.Copy | PermissionMask.Transfer | PermissionMask.Modify)) == (uint)(PermissionMask.Copy | PermissionMask.Transfer | PermissionMask.Modify))
                        {
                            return inv.Value.AssetID.ToString();
                        }
                        else
                        {
                            return UUID.Zero.ToString();
                        }
                    }
                }
            }

            return UUID.Zero.ToString();
        }

        public void llAllowInventoryDrop(int add)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            if (add != 0)
                m_host.ParentEntity.RootChild.AllowedDrop = true;
            else
                m_host.ParentEntity.RootChild.AllowedDrop = false;

            // Update the object flags
            m_host.ParentEntity.RootChild.aggregateScriptEvents ();
        }

        public LSL_Vector llGetSunDirection()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            LSL_Vector SunDoubleVector3;
            Vector3 SunFloatVector3;

            // sunPosition estate setting is set in OpenSim.Region.CoreModules.SunModule
            // have to convert from Vector3 (float) to LSL_Vector (double)
            SunFloatVector3 = World.RegionInfo.RegionSettings.SunVector;
            SunDoubleVector3.x = (double)SunFloatVector3.X;
            SunDoubleVector3.y = (double)SunFloatVector3.Y;
            SunDoubleVector3.z = (double)SunFloatVector3.Z;

            return SunDoubleVector3;
        }

        public LSL_Vector llGetTextureOffset(int face)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return GetTextureOffset(m_host, face);
        }

        protected LSL_Vector GetTextureOffset (ISceneChildEntity part, int face)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            LSL_Vector offset = new LSL_Vector();
            if (face == ScriptBaseClass.ALL_SIDES)
            {
                face = 0;
            }
            if (face >= 0 && face < GetNumberOfSides(part))
            {
                offset.x = tex.GetFace((uint)face).OffsetU;
                offset.y = tex.GetFace((uint)face).OffsetV;
                offset.z = 0.0;
                return offset;
            }
            else
            {
                return offset;
            }
        }

        public LSL_Vector llGetTextureScale(int side)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            Primitive.TextureEntry tex = m_host.Shape.Textures;
            LSL_Vector scale;
            if (side == -1)
            {
                side = 0;
            }
            scale.x = tex.GetFace((uint)side).RepeatU;
            scale.y = tex.GetFace((uint)side).RepeatV;
            scale.z = 0.0;
            return scale;
        }

        public LSL_Float llGetTextureRot(int face)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return GetTextureRot(m_host, face);
        }

        protected LSL_Float GetTextureRot (ISceneChildEntity part, int face)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            if (face == -1)
            {
                face = 0;
            }
            if (face >= 0 && face < GetNumberOfSides(part))
            {
                return tex.GetFace((uint)face).Rotation;
            }
            else
            {
                return 0.0;
            }
        }

        public LSL_Integer llSubStringIndex(string source, string pattern)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return source.IndexOf(pattern);
        }

        public LSL_String llGetOwnerKey(string id)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            UUID key = new UUID();
            if (UUID.TryParse(id, out key))
            {
                try
                {
                    ISceneChildEntity obj = World.GetSceneObjectPart (key);
                    if (obj == null)
                        return id; // the key is for an agent so just return the key
                    else
                        return obj.OwnerID.ToString();
                }
                catch (KeyNotFoundException)
                {
                    return id; // The Object/Agent not in the region so just return the key
                }
            }
            else
            {
                return UUID.Zero.ToString();
            }
        }

        public LSL_Vector llGetCenterOfMass()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            Vector3 center = m_host.GetGeometricCenter();
            return new LSL_Vector(center.X,center.Y,center.Z);
        }

        public LSL_List llListSort(LSL_List src, int stride, int ascending)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            if (stride <= 0)
            {
                stride = 1;
            }
            return src.Sort(stride, ascending);
        }

        public LSL_Integer llGetListLength(LSL_List src)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            if (src == new LSL_List(new object[0]))
            {
                return 0;
            }
            else
            {
                return src.Length;
            }
        }

        public LSL_Integer llList2Integer(LSL_List src, int index)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            if (index < 0)
            {
                index = src.Length + index;
            }
            if (index >= src.Length || index < 0)
            {
                return 0;
            }
            try
            {
                if (src.Data[index] is LSL_Integer)
                    return (LSL_Integer) src.Data[index];
                else if (src.Data[index] is LSL_Float)
                    return Convert.ToInt32(((LSL_Float) src.Data[index]).value);
                return new LSL_Integer(src.Data[index].ToString());
            }
            catch (FormatException)
            {
                return 0;
            }
            catch (InvalidCastException)
            {
                return 0;
            }
        }

        public LSL_Float llList2Float(LSL_List src, int index)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            if (index < 0)
            {
                index = src.Length + index;
            }
            if (index >= src.Length || index < 0)
            {
                return 0.0;
            }
            try
            {
                if (src.Data[index] is LSL_Integer)
                    return Convert.ToDouble(((LSL_Integer)src.Data[index]).value);
                else if (src.Data[index] is LSL_Float)
                    return Convert.ToDouble(((LSL_Float)src.Data[index]).value);
                else if (src.Data[index] is LSL_String)
                    return Convert.ToDouble(((LSL_String)src.Data[index]).m_string);
                return Convert.ToDouble(src.Data[index]);
            }
            catch (FormatException)
            {
                return 0.0;
            }
            catch (InvalidCastException)
            {
                return 0.0;
            }
        }

        public LSL_String llList2String(LSL_List src, int index)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            if (index < 0)
            {
                index = src.Length + index;
            }
            if (index >= src.Length || index < 0)
            {
                return String.Empty;
            }
            return new LSL_String(src.Data[index].ToString());
        }

        public LSL_String llList2Key(LSL_List src, int index)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            if (index < 0)
            {
                index = src.Length + index;
            }
            if (index >= src.Length || index < 0)
            {
                return "";
            }
            return src.Data[index].ToString();
        }

        public LSL_Vector llList2Vector(LSL_List src, int index)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            if (index < 0)
            {
                index = src.Length + index;
            }
            if (index >= src.Length || index < 0)
            {
                return new LSL_Vector(0, 0, 0);
            }
            if (src.Data[index].GetType() == typeof(LSL_Vector))
            {
                return (LSL_Vector)src.Data[index];
            }
            else
            {
                return new LSL_Vector(src.Data[index].ToString());
            }
        }

        public LSL_Rotation llList2Rot(LSL_List src, int index)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            if (index < 0)
            {
                index = src.Length + index;
            }
            if (index >= src.Length || index < 0)
            {
                return new LSL_Rotation(0, 0, 0, 1);
            }
            if (src.Data[index].GetType() == typeof(LSL_Rotation))
            {
                return (LSL_Rotation)src.Data[index];
            }
            else
            {
                return new LSL_Rotation(src.Data[index].ToString());
            }
        }

        public LSL_List llList2List(LSL_List src, int start, int end)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return src.GetSublist(start, end);
        }

        public LSL_List llDeleteSubList(LSL_List src, int start, int end)
        {
            return src.DeleteSublist(start, end);
        }

        public LSL_Integer llGetListEntryType(LSL_List src, int index)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            if (index < 0)
            {
                index = src.Length + index;
            }
            if (index >= src.Length)
            {
                return 0;
            }

            if (src.Data[index] is LSL_Integer || src.Data[index] is Int32)
                return 1;
            if (src.Data[index] is LSL_Float || src.Data[index] is Single || src.Data[index] is Double)
                return 2;
            if (src.Data[index] is LSL_String || src.Data[index] is String)
            {
                UUID tuuid;
                if (UUID.TryParse(src.Data[index].ToString(), out tuuid))
                {
                    return 4;
                }
                else
                {
                    return 3;
                }
            }
            if (src.Data[index] is LSL_Vector)
                return 5;
            if (src.Data[index] is LSL_Rotation)
                return 6;
            if (src.Data[index] is LSL_List)
                return 7;
            return 0;

        }

        /// <summary>
        /// Process the supplied list and return the
        /// content of the list formatted as a comma
        /// separated list. There is a space after
        /// each comma.
        /// </summary>

        public LSL_String llList2CSV(LSL_List src)
        {

            string ret = String.Empty;
            int    x   = 0;

            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            if (src.Data.Length > 0)
            {
                ret = src.Data[x++].ToString();
                for (; x < src.Data.Length; x++)
                {
                    ret += ", "+src.Data[x].ToString();
                }
            }

            return ret;
        }

        /// <summary>
        /// The supplied string is scanned for commas
        /// and converted into a list. Commas are only
        /// effective if they are encountered outside
        /// of '<' '>' delimiters. Any whitespace
        /// before or after an element is trimmed.
        /// </summary>

        public LSL_List llCSV2List(string src)
        {

            LSL_List result = new LSL_List();
            int parens = 0;
            int start  = 0;
            int length = 0;

            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            for (int i = 0; i < src.Length; i++)
            {
                switch (src[i])
                {
                    case '<':
                        parens++;
                        length++;
                        break;
                    case '>':
                        if (parens > 0)
                            parens--;
                        length++;
                        break;
                    case ',':
                        if (parens == 0)
                        {
                            result.Add(new LSL_String(src.Substring(start,length).Trim()));
                            start += length+1;
                            length = 0;
                        }
                        else
                        {
                            length++;
                        }
                        break;
                    default:
                        length++;
                        break;
                }
            }

            result.Add(src.Substring(start,length).Trim());

            return result;
        }

        ///  <summary>
        ///  Randomizes the list, be arbitrarily reordering
        ///  sublists of stride elements. As the stride approaches
        ///  the size of the list, the options become very
        ///  limited.
        ///  </summary>
        ///  <remarks>
        ///  This could take a while for very large list
        ///  sizes.
        ///  </remarks>

        public LSL_List llListRandomize(LSL_List src, int stride)
        {
            LSL_List result;
            Random rand           = new Random();

            int   chunkk;
            int[] chunks;

            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            if (stride <= 0)
            {
                stride = 1;
            }

            // Stride MUST be a factor of the list length
            // If not, then return the src list. This also
            // traps those cases where stride > length.

            if (src.Length != stride && src.Length%stride == 0)
            {
                chunkk = src.Length/stride;

                chunks = new int[chunkk];

                for (int i = 0; i < chunkk; i++)
                    chunks[i] = i;

                // Knuth shuffle the chunkk index
                for (int i = chunkk - 1; i >= 1; i--)
                {
                    // Elect an unrandomized chunk to swap
                    int index = rand.Next(i + 1);
                    int tmp;

                    // and swap position with first unrandomized chunk
                    tmp = chunks[i];
                    chunks[i] = chunks[index];
                    chunks[index] = tmp;
                }

                // Construct the randomized list

                result = new LSL_List();

                for (int i = 0; i < chunkk; i++)
                {
                    for (int j = 0; j < stride; j++)
                    {
                        result.Add(src.Data[chunks[i]*stride+j]);
                    }
                }
            }
            else {
                object[] array = new object[src.Length];
                Array.Copy(src.Data, 0, array, 0, src.Length);
                result = new LSL_List(array);
            }

            return result;
        }

        /// <summary>
        /// Elements in the source list starting with 0 and then
        /// every i+stride. If the stride is negative then the scan
        /// is backwards producing an inverted result.
        /// Only those elements that are also in the specified
        /// range are included in the result.
        /// </summary>

        public LSL_List llList2ListStrided(LSL_List src, int start, int end, int stride)
        {

            LSL_List result = new LSL_List();
            int[] si = new int[2];
            int[] ei = new int[2];
            bool twopass = false;

            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            //  First step is always to deal with negative indices

            if (start < 0)
                start = src.Length+start;
            if (end   < 0)
                end   = src.Length+end;

            //  Out of bounds indices are OK, just trim them
            //  accordingly

            if (start > src.Length)
                start = src.Length;

            if (end > src.Length)
                end = src.Length;

            if (stride == 0)
                stride = 1;

            //  There may be one or two ranges to be considered

            if (start != end)
            {

                if (start <= end)
                {
                   si[0] = start;
                   ei[0] = end;
                }
                else
                {
                   si[1] = start;
                   ei[1] = src.Length;
                   si[0] = 0;
                   ei[0] = end;
                   twopass = true;
                }

                //  The scan always starts from the beginning of the
                //  source list, but members are only selected if they
                //  fall within the specified sub-range. The specified
                //  range values are inclusive.
                //  A negative stride reverses the direction of the
                //  scan producing an inverted list as a result.

                if (stride > 0)
                {
                    for (int i = 0; i < src.Length; i += stride)
                    {
                        if (i<=ei[0] && i>=si[0])
                            result.Add(src.Data[i]);
                        if (twopass && i>=si[1] && i<=ei[1])
                            result.Add(src.Data[i]);
                    }
                }
                else if (stride < 0)
                {
                    for (int i = src.Length - 1; i >= 0; i += stride)
                    {
                        if (i <= ei[0] && i >= si[0])
                            result.Add(src.Data[i]);
                        if (twopass && i >= si[1] && i <= ei[1])
                            result.Add(src.Data[i]);
                    }
                }
            }
            else
            {
                if (start%stride == 0)
                {
                    result.Add(src.Data[start]);
                }
            }

            return result;
        }

        public LSL_Integer llGetRegionAgentCount()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            IEntityCountModule entityCountModule = World.RequestModuleInterface<IEntityCountModule>();
            if (entityCountModule != null)
                return new LSL_Integer(entityCountModule.RootAgents);
            else
                return new LSL_Integer(0);
        }

        public LSL_Vector llGetRegionCorner()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return new LSL_Vector(World.RegionInfo.RegionLocX, World.RegionInfo.RegionLocY, 0);
        }

        /// <summary>
        /// Insert the list identified by <src> into the
        /// list designated by <dest> such that the first
        /// new element has the index specified by <index>
        /// </summary>

        public LSL_List llListInsertList(LSL_List dest, LSL_List src, int index)
        {
			ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            LSL_List pref = null;
            LSL_List suff = null;

            

            if (index < 0)
            {
                index = index+dest.Length;
                if (index < 0)
                {
                    index = 0;
                }
            }

            if (index != 0)
            {
                pref = dest.GetSublist(0,index-1);
                if (index < dest.Length)
                {
                    suff = dest.GetSublist(index,-1);
                    return pref + src + suff;
                }
                else
                {
                    return pref + src;
                }
            }
            else
            {
                if (index < dest.Length)
                {
                    suff = dest.GetSublist(index,-1);
                    return src + suff;
                }
                else
                {
                    return src;
                }
            }

        }

        /// <summary>
        /// Returns the index of the first occurrence of test
        /// in src.
        /// </summary>

        public LSL_Integer llListFindList(LSL_List src, LSL_List test)
        {

            int index  = -1;
            int length = src.Length - test.Length + 1;

            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            // If either list is empty, do not match

            if (src.Length != 0 && test.Length != 0)
            {
                for (int i = 0; i < length; i++)
                {
                    if (src.Data[i].Equals(test.Data[0]))
                    {
                        int j;
                        for (j = 1; j < test.Length; j++)
                            if (!src.Data[i+j].Equals(test.Data[j]))
                                break;
                        if (j == test.Length)
                        {
                            index = i;
                            break;
                        }
                    }
                }
            }

            return index;

        }

        public LSL_String llGetObjectName()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return m_host.Name!=null?m_host.Name:String.Empty;
        }

        public void llSetObjectName(string name)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.Name = name!=null?name:String.Empty;
        }

        public LSL_String llGetDate()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            DateTime date = DateTime.Now.ToUniversalTime();
            string result = date.ToString("yyyy-MM-dd");
            return result;
        }

        public LSL_Integer llEdgeOfWorld(LSL_Vector pos, LSL_Vector dir)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            // edge will be used to pass the Region Coordinates offset
            // we want to check for a neighboring sim
            LSL_Vector edge = new LSL_Vector(0, 0, 0);

            if (dir.x == 0)
            {
                if (dir.y == 0)
                {
                    // Direction vector is 0,0 so return
                    // false since we're staying in the sim
                    return 0;
                }
                else
                {
                    // Y is the only valid direction
                    edge.y = dir.y / Math.Abs(dir.y);
                }
            }
            else
            {
                LSL_Float mag;
                if (dir.x > 0)
                {
                    mag = (World.RegionInfo.RegionSizeX - pos.x) / dir.x;
                }
                else
                {
                    mag = (pos.x/dir.x);
                }

                mag = Math.Abs(mag);

                edge.y = pos.y + (dir.y * mag);

                if (edge.y > World.RegionInfo.RegionSizeY || edge.y < 0)
                {
                    // Y goes out of bounds first
                    edge.y = dir.y / Math.Abs(dir.y);
                }
                else
                {
                    // X goes out of bounds first or its a corner exit
                    edge.y = 0;
                    edge.x = dir.x / Math.Abs(dir.x);
                }
            }
            INeighborService service = World.RequestModuleInterface<INeighborService>();
            List<GridRegion> neighbors = new List<GridRegion>();
            if (service != null)
            {
                neighbors = service.GetNeighbors(World.RegionInfo);
            }

            int neighborX = World.RegionInfo.RegionLocX + (int)dir.x;
            int neighborY = World.RegionInfo.RegionLocY + (int)dir.y;

            foreach (GridRegion sri in neighbors)
            {
                if (sri.RegionLocX == neighborX && sri.RegionLocY == neighborY)
                    return 0;
            }

            return 1;
        }

        /// <summary>
        /// Fully implemented
        /// </summary>
        public LSL_Integer llGetAgentInfo(string id)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            UUID key = new UUID();
            if (!UUID.TryParse(id, out key))
            {
                return 0;
            }

            int flags = 0;

            IScenePresence agent = World.GetScenePresence (key);
            if (agent == null)
            {
                return 0;
            }

            if (agent.IsChildAgent)
                return 0; // Fail if they are not in the same region

            // note: in OpenSim, sitting seems to cancel AGENT_ALWAYS_RUN, unlike SL
            if (agent.SetAlwaysRun)
            {
                flags |= ScriptBaseClass.AGENT_ALWAYS_RUN;
            }
            IAttachmentsModule attachMod = World.RequestModuleInterface<IAttachmentsModule>();
            if (attachMod != null)
            {
                ISceneEntity[] att = attachMod.GetAttachmentsForAvatar (agent.UUID);
                if (att.Length > 0)
                {
                    flags |= ScriptBaseClass.AGENT_ATTACHMENTS;
                    foreach (ISceneEntity gobj in att)
                    {
                        if (gobj != null)
                        {
                            if (gobj.RootChild.Inventory.ContainsScripts())
                            {
                                flags |= ScriptBaseClass.AGENT_SCRIPTED;
                                break;
                            }
                        }
                    }
                }
            }

            if ((agent.AgentControlFlags & (uint)AgentManager.ControlFlags.AGENT_CONTROL_FLY) != 0)
            {
                flags |= ScriptBaseClass.AGENT_FLYING;
                flags |= ScriptBaseClass.AGENT_IN_AIR; // flying always implies in-air, even if colliding with e.g. a wall
            }

            if ((agent.AgentControlFlags & (uint)AgentManager.ControlFlags.AGENT_CONTROL_AWAY) != 0)
            {
                flags |= ScriptBaseClass.AGENT_AWAY;
            }

            // seems to get unset, even if in mouselook, when avatar is sitting on a prim???
            if ((agent.AgentControlFlags & (uint)AgentManager.ControlFlags.AGENT_CONTROL_MOUSELOOK) != 0)
            {
                flags |= ScriptBaseClass.AGENT_MOUSELOOK;
            }

            if ((agent.State & (byte)AgentState.Typing) != (byte)0)
            {
                flags |= ScriptBaseClass.AGENT_TYPING;
            }

            if (agent.IsBusy)
            {
                flags |= ScriptBaseClass.AGENT_BUSY;
            }

            string agentMovementAnimation = agent.Animator.CurrentMovementAnimation;

            if (agentMovementAnimation == "CROUCH")
            {
                flags |= ScriptBaseClass.AGENT_CROUCHING;
            }

            if (agentMovementAnimation == "WALK" || agentMovementAnimation == "CROUCHWALK")
            {
                flags |= ScriptBaseClass.AGENT_WALKING;
            }

            // not colliding implies in air. Note: flying also implies in-air, even if colliding (see above)

            // note: AGENT_IN_AIR and AGENT_WALKING seem to be mutually exclusive states in SL.

            // note: this may need some tweaking when walking downhill. you "fall down" for a brief instant
            // and don't collide when walking downhill, which instantly registers as in-air, briefly. should
            // there be some minimum non-collision threshold time before claiming the avatar is in-air?
            if ((flags & ScriptBaseClass.AGENT_WALKING) == 0 &&
                agent.PhysicsActor != null &&
                !agent.PhysicsActor.IsColliding)
            {
                    flags |= ScriptBaseClass.AGENT_IN_AIR;
            }

            if (agent.ParentID != UUID.Zero)
             {
                 flags |= ScriptBaseClass.AGENT_ON_OBJECT;
                 flags |= ScriptBaseClass.AGENT_SITTING;
             }

             if (agent.Animator.Animations.DefaultAnimation.AnimID 
                == AnimationSet.Animations.AnimsUUID["SIT_GROUND_CONSTRAINED"])
             {
                 flags |= ScriptBaseClass.AGENT_SITTING;
             }

            return flags;
        }

        public LSL_String llGetAgentLanguage(string id)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            Aurora.Framework.IAgentConnector AgentFrontend = Aurora.DataManager.DataManager.RequestPlugin<Aurora.Framework.IAgentConnector>();
            if (AgentFrontend == null)
                return "en-us";
            Aurora.Framework.IAgentInfo Agent = AgentFrontend.GetAgent(new UUID(id));
            if (Agent == null)
                return "en-us";
            if (Agent.LanguageIsPublic)
            {
                return Agent.Language;
            }
            else
                return "en-us";
        }

        public void llAdjustSoundVolume(double volume)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            m_host.AdjustSoundGain(volume);
            ScriptSleep(100);
        }

        public void llSetSoundQueueing(int queue)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.SetSoundQueueing(queue);
        }

        public void llSetSoundRadius(double radius)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.SoundRadius = radius;
        }

        public LSL_String llGetDisplayName(string id)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            UUID key = new UUID();
            if (UUID.TryParse(id, out key))
            {
                IScenePresence presence = World.GetScenePresence(key);

                if (presence != null)
                {
                    IProfileConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IProfileConnector>();
                    if (connector != null)
                        return connector.GetUserProfile(presence.UUID).DisplayName;
                }
            }
            return String.Empty;
        }

        public LSL_String llGetUsername(string id)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            UUID key = new UUID();
            if (UUID.TryParse(id, out key))
            {
                IScenePresence presence = World.GetScenePresence(key);

                if (presence != null)
                    return presence.Name;
            }
            return String.Empty;
        }

        public LSL_String llKey2Name(string id)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            UUID key = new UUID();
            if (UUID.TryParse(id,out key))
            {
                IScenePresence presence = World.GetScenePresence (key);

                if (presence != null)
                    return presence.Name;

                if (World.GetSceneObjectPart(key) != null)
                {
                    return World.GetSceneObjectPart(key).Name;
                }
            }
            return String.Empty;
        }



        public void llSetTextureAnim(int mode, int face, int sizex, int sizey, double start, double length, double rate)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            SetTextureAnim(m_host, mode, face, sizex, sizey, start, length, rate);
        }

        public void llSetLinkTextureAnim(int linknumber, int mode, int face, int sizex, int sizey, double start, double length, double rate)
        {


            List<ISceneChildEntity> parts = GetLinkParts (linknumber);

            foreach (var part in parts)
            {
                SetTextureAnim(part, mode, face, sizex, sizey, start, length, rate);
            }
        }

        private void SetTextureAnim (ISceneChildEntity part, int mode, int face, int sizex, int sizey, double start, double length, double rate)
        {

            Primitive.TextureAnimation pTexAnim = new Primitive.TextureAnimation();
            pTexAnim.Flags = (Primitive.TextureAnimMode)mode;

            //ALL_SIDES
            if (face == ScriptBaseClass.ALL_SIDES)
                face = 255;

            pTexAnim.Face = (uint)face;
            pTexAnim.Length = (float)length;
            pTexAnim.Rate = (float)rate;
            pTexAnim.SizeX = (uint)sizex;
            pTexAnim.SizeY = (uint)sizey;
            pTexAnim.Start = (float)start;

            part.AddTextureAnimation(pTexAnim);
            part.ScheduleUpdate(PrimUpdateFlags.FindBest);
            part.ParentEntity.HasGroupChanged = true;
        }

        public void llTriggerSoundLimited(string sound, double volume, LSL_Vector top_north_east,
                                          LSL_Vector bottom_south_west)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            float radius1 = (float)llVecDist(llGetPos(), top_north_east);
            float radius2 = (float)llVecDist(llGetPos(), bottom_south_west);
            float radius = Math.Abs(radius1 - radius2);
            m_host.SendSound(KeyOrName(sound).ToString(), volume, true, 0, radius, false, false);
        }

        public void llEjectFromLand(string pest)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            UUID agentId = new UUID();
            if (UUID.TryParse(pest, out agentId))
            {
                IScenePresence presence = World.GetScenePresence (agentId);
                if (presence != null)
                {
                    // agent must be over the owners land
                    IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
                    if (parcelManagement != null)
                    {
                        if (m_host.OwnerID != parcelManagement.GetLandObject(
                                       presence.AbsolutePosition.X, presence.AbsolutePosition.Y).LandData.OwnerID &&
                            !World.Permissions.CanIssueEstateCommand(m_host.OwnerID, false))
                        {
                            ScriptSleep(5000);
                        }
                    }
                    IEntityTransferModule transferModule = World.RequestModuleInterface<IEntityTransferModule>();
                    if (transferModule != null)
                        transferModule.TeleportHome(agentId, presence.ControllingClient);
                    else
                        presence.ControllingClient.SendTeleportFailed("Unable to perform teleports on this simulator.");
                }
            }
            ScriptSleep(5000);
        }

        public LSL_Integer llOverMyLand(string id)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            UUID key = new UUID();
            if (UUID.TryParse(id, out key))
            {
                IScenePresence presence = World.GetScenePresence (key);
                IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
                if (presence != null) // object is an avatar
                {
                    if (parcelManagement != null)
                    {
                        if (m_host.OwnerID
                            == parcelManagement.GetLandObject(
                                presence.AbsolutePosition.X, presence.AbsolutePosition.Y).LandData.OwnerID)
                            return 1;
                    }
                }
                else // object is not an avatar
                {
                    ISceneChildEntity obj = World.GetSceneObjectPart (key);
                    if (obj != null)
                        if (parcelManagement != null)
                        {
                            if (m_host.OwnerID
                                == parcelManagement.GetLandObject(
                                    obj.AbsolutePosition.X, obj.AbsolutePosition.Y).LandData.OwnerID)
                                return 1;
                        }
                }
            }

            return 0;
        }

        public LSL_String llGetLandOwnerAt(LSL_Vector pos)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                ILandObject land = parcelManagement.GetLandObject((float)pos.x, (float)pos.y);
                if (land != null)
                    return land.LandData.OwnerID.ToString();
            }
            return UUID.Zero.ToString();
        }

        /// <summary>
        /// According to http://lslwiki.net/lslwiki/wakka.php?wakka=llGetAgentSize
        /// only the height of avatars vary and that says:
        /// Width (x) and depth (y) are constant. (0.45m and 0.6m respectively).
        /// </summary>
        public LSL_Vector llGetAgentSize(string id)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            IScenePresence avatar = World.GetScenePresence ((UUID)id);
            LSL_Vector agentSize;
            if (avatar == null || avatar.IsChildAgent) // Fail if not in the same region
            {
                agentSize = ScriptBaseClass.ZERO_VECTOR;
            }
            else
            {
                IAvatarAppearanceModule appearance = avatar.RequestModuleInterface<IAvatarAppearanceModule> ();
                if (appearance != null)
                    agentSize = new LSL_Vector (0.45, 0.6, appearance.Appearance.AvatarHeight);
                else
                    agentSize = ScriptBaseClass.ZERO_VECTOR;
            }
            return agentSize;
        }

        public LSL_Integer llSameGroup(string agent)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            UUID agentId = new UUID();
            if (!UUID.TryParse(agent, out agentId))
                return new LSL_Integer(0);
            IScenePresence presence = World.GetScenePresence(agentId);
            if (presence == null || presence.IsChildAgent) // Return flase for child agents
                return new LSL_Integer(0);
            IClientAPI client = presence.ControllingClient;
            if (m_host.GroupID == client.ActiveGroupId)
                return new LSL_Integer(1);
            else
                return new LSL_Integer(0);
        }

        public void llUnSit(string id)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            UUID key = new UUID();
            if (UUID.TryParse(id, out key))
            {
                IScenePresence av = World.GetScenePresence(key);

                if (av != null)
                {
                    if (m_host.SitTargetAvatar.Contains(key))
                    {
                        // if the avatar is sitting on this object, then
                        // we can unsit them.  We don't want random scripts unsitting random people
                        // Lets avoid the popcorn avatar scenario.
                        av.StandUp();
                    }
                    else
                    {
                        // If the object owner also owns the parcel
                        // or
                        // if the land is group owned and the object is group owned by the same group
                        // or
                        // if the object is owned by a person with estate access.

                        IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
                        if (parcelManagement != null)
                        {
                            ILandObject parcel = parcelManagement.GetLandObject(av.AbsolutePosition.X, av.AbsolutePosition.Y);
                            if (parcel != null)
                            {
                                if (m_host.OwnerID == parcel.LandData.OwnerID ||
                                    (m_host.OwnerID == m_host.GroupID && m_host.GroupID == parcel.LandData.GroupID
                                    && parcel.LandData.IsGroupOwned) || World.Permissions.IsGod(m_host.OwnerID))
                                {
                                    av.StandUp();
                                }
                            }
                        }
                    }
                }

            }

        }

        public LSL_Vector llGroundSlope(LSL_Vector offset)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            //Get the slope normal.  This gives us the equation of the plane tangent to the slope.
            LSL_Vector vsn = llGroundNormal(offset);

            //Plug the x,y coordinates of the slope normal into the equation of the plane to get
            //the height of that point on the plane.  The resulting vector gives the slope.
            Vector3 vsl = new Vector3();
            vsl.X = (float)vsn.x;
            vsl.Y = (float)vsn.y;
            vsl.Z = (float)(((vsn.x * vsn.x) + (vsn.y * vsn.y)) / (-1 * vsn.z));
            vsl.Normalize();
            //Normalization might be overkill here

            return new LSL_Vector(vsl.X, vsl.Y, vsl.Z);
        }

        public LSL_Vector llGroundNormal(LSL_Vector offset)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            Vector3 pos = m_host.GetWorldPosition() + new Vector3((float)offset.x,
                                                                (float)offset.y,
                                                                (float)offset.z);
            ITerrainChannel heightmap = World.RequestModuleInterface<ITerrainChannel>();
            // Clamp to valid position
            if (pos.X < 0)
                pos.X = 0;
            else if (pos.X >= heightmap.Width)
                pos.X = heightmap.Width - 1;
            if (pos.Y < 0)
                pos.Y = 0;
            else if (pos.Y >= heightmap.Height)
                pos.Y = heightmap.Height - 1;

            //Find two points in addition to the position to define a plane
            Vector3 p0 = new Vector3(pos.X, pos.Y,
                                     heightmap[(int)pos.X, (int)pos.Y]);
            Vector3 p1 = new Vector3();
            Vector3 p2 = new Vector3();
            if ((pos.X + 1.0f) >= heightmap.Width)
                p1 = new Vector3(pos.X + 1.0f, pos.Y,
                            heightmap[(int)pos.X, (int)pos.Y]);
            else
                p1 = new Vector3(pos.X + 1.0f, pos.Y,
                            heightmap[(int)(pos.X + 1.0f), (int)pos.Y]);
            if ((pos.Y + 1.0f) >= heightmap.Height)
                p2 = new Vector3(pos.X, pos.Y + 1.0f,
                            heightmap[(int)pos.X, (int)pos.Y]);
            else
                p2 = new Vector3(pos.X, pos.Y + 1.0f,
                            heightmap[(int)pos.X, (int)(pos.Y + 1.0f)]);

            //Find normalized vectors from p0 to p1 and p0 to p2
            Vector3 v0 = new Vector3(p1.X - p0.X, p1.Y - p0.Y, p1.Z - p0.Z);
            Vector3 v1 = new Vector3(p2.X - p0.X, p2.Y - p0.Y, p2.Z - p0.Z);
//            v0.Normalize();
//            v1.Normalize();

            //Find the cross product of the vectors (the slope normal).
            Vector3 vsn = new Vector3();
            vsn.X = (v0.Y * v1.Z) - (v0.Z * v1.Y);
            vsn.Y = (v0.Z * v1.X) - (v0.X * v1.Z);
            vsn.Z = (v0.X * v1.Y) - (v0.Y * v1.X);
            vsn.Normalize();
            //I believe the crossproduct of two normalized vectors is a normalized vector so
            //this normalization may be overkill
            // then don't normalize them just the result

            return new LSL_Vector(vsn.X, vsn.Y, vsn.Z);
        }

        public LSL_Vector llGroundContour(LSL_Vector offset)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            LSL_Vector x = llGroundSlope(offset);
            return new LSL_Vector(-x.y, x.x, 0.0);
        }

        public LSL_Integer llGetAttached()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            return (int)m_host.ParentEntity.RootChild.AttachmentPoint;
        }

        public LSL_Integer llGetFreeMemory()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            // Make scripts designed for LSO happy
            return 16384;
        }

        public LSL_Integer llGetFreeURLs()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            if (m_UrlModule != null)
                return new LSL_Integer(m_UrlModule.GetFreeUrls());
            return new LSL_Integer(0);
        }


        public LSL_String llGetRegionName()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            return World.RegionInfo.RegionName;
        }

        public LSL_Float llGetRegionTimeDilation()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return (double)World.TimeDilation;
        }

        /// <summary>
        /// Returns the value reported in the client Statistics window
        /// </summary>
        public LSL_Float llGetRegionFPS()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            ISimFrameMonitor reporter = (ISimFrameMonitor)World.RequestModuleInterface<IMonitorModule>().GetMonitor(World.RegionInfo.RegionID.ToString(), "SimFrameStats");
            if (reporter != null)
                return reporter.LastReportedSimFPS;
            return 0;
        }
        

        /* particle system rules should be coming into this routine as doubles, that is
        rule[0] should be an integer from this list and rule[1] should be the arg
        for the same integer. wiki.secondlife.com has most of this mapping, but some
        came from http://www.caligari-designs.com/p4u2

        We iterate through the list for 'Count' elements, incrementing by two for each
        iteration and set the members of Primitive.ParticleSystem, one at a time.
        */

        public enum PrimitiveRule : int
        {
            PSYS_PART_FLAGS = 0,
            PSYS_PART_START_COLOR = 1,
            PSYS_PART_START_ALPHA = 2,
            PSYS_PART_END_COLOR = 3,
            PSYS_PART_END_ALPHA = 4,
            PSYS_PART_START_SCALE = 5,
            PSYS_PART_END_SCALE = 6,
            PSYS_PART_MAX_AGE = 7,
            PSYS_SRC_ACCEL = 8,
            PSYS_SRC_PATTERN = 9,
            PSYS_SRC_INNERANGLE = 10,
            PSYS_SRC_OUTERANGLE = 11,
            PSYS_SRC_TEXTURE = 12,
            PSYS_SRC_BURST_RATE = 13,
            PSYS_SRC_BURST_PART_COUNT = 15,
            PSYS_SRC_BURST_RADIUS = 16,
            PSYS_SRC_BURST_SPEED_MIN = 17,
            PSYS_SRC_BURST_SPEED_MAX = 18,
            PSYS_SRC_MAX_AGE = 19,
            PSYS_SRC_TARGET_KEY = 20,
            PSYS_SRC_OMEGA = 21,
            PSYS_SRC_ANGLE_BEGIN = 22,
            PSYS_SRC_ANGLE_END = 23
        }

        internal Primitive.ParticleSystem.ParticleDataFlags ConvertUINTtoFlags(uint flags)
        {
            Primitive.ParticleSystem.ParticleDataFlags returnval = Primitive.ParticleSystem.ParticleDataFlags.None;

            return returnval;
        }

        protected Primitive.ParticleSystem getNewParticleSystemWithSLDefaultValues()
        {
            Primitive.ParticleSystem ps = new Primitive.ParticleSystem();

            // TODO find out about the other defaults and add them here
            ps.PartStartColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
            ps.PartEndColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
            ps.PartStartScaleX = 1.0f;
            ps.PartStartScaleY = 1.0f;
            ps.PartEndScaleX = 1.0f;
            ps.PartEndScaleY = 1.0f;
            ps.BurstSpeedMin = 1.0f;
            ps.BurstSpeedMax = 1.0f;
            ps.BurstRate = 0.1f;
            ps.PartMaxAge = 10.0f;
            ps.BurstPartCount = 10;
            return ps;
        }

        public void llLinkParticleSystem(int linknumber, LSL_List rules)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");


            List<ISceneChildEntity> parts = GetLinkParts (linknumber);

            foreach (var part in parts)
            {
                SetParticleSystem(part, rules);
            }
        }

        public void llParticleSystem(LSL_List rules)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            SetParticleSystem(m_host, rules);
        }

        private void SetParticleSystem (ISceneChildEntity part, LSL_List rules)
        {
            if (rules.Length == 0)
            {
                part.RemoveParticleSystem();
                part.ParentEntity.HasGroupChanged = true;
            }
            else
            {
                Primitive.ParticleSystem prules = getNewParticleSystemWithSLDefaultValues();
                LSL_Vector tempv = new LSL_Vector();

                float tempf = 0;

                for (int i = 0; i < rules.Length; i += 2)
                {
                    LSL_Integer rule = rules.GetLSLIntegerItem(i);
                    if (rule == (int)ScriptBaseClass.PSYS_PART_FLAGS)
                    {
                        prules.PartDataFlags = (Primitive.ParticleSystem.ParticleDataFlags)(uint)rules.GetLSLIntegerItem(i + 1);
                    }

                    else if (rule == (int)ScriptBaseClass.PSYS_PART_START_COLOR)
                    {
                        tempv = rules.GetVector3Item(i + 1);
                        prules.PartStartColor.R = (float)tempv.x;
                        prules.PartStartColor.G = (float)tempv.y;
                        prules.PartStartColor.B = (float)tempv.z;
                    }

                    else if (rule == (int)ScriptBaseClass.PSYS_PART_START_ALPHA)
                    {
                        tempf = (float)rules.GetLSLFloatItem(i + 1);
                        prules.PartStartColor.A = tempf;
                    }

                    else if (rule == (int)ScriptBaseClass.PSYS_PART_END_COLOR)
                    {
                        tempv = rules.GetVector3Item(i + 1);
                        prules.PartEndColor.R = (float)tempv.x;
                        prules.PartEndColor.G = (float)tempv.y;
                        prules.PartEndColor.B = (float)tempv.z;
                    }

                    else if (rule == (int)ScriptBaseClass.PSYS_PART_END_ALPHA)
                    {
                        tempf = (float)rules.GetLSLFloatItem(i + 1);
                        prules.PartEndColor.A = tempf;
                    }

                    else if (rule == (int)ScriptBaseClass.PSYS_PART_START_SCALE)
                    {
                        tempv = rules.GetVector3Item(i + 1);
                        prules.PartStartScaleX = (float)tempv.x;
                        prules.PartStartScaleY = (float)tempv.y;
                    }

                    else if (rule == (int)ScriptBaseClass.PSYS_PART_END_SCALE)
                    {
                        tempv = rules.GetVector3Item(i + 1);
                        prules.PartEndScaleX = (float)tempv.x;
                        prules.PartEndScaleY = (float)tempv.y;
                    }

                    else if (rule == (int)ScriptBaseClass.PSYS_PART_MAX_AGE)
                    {
                        tempf = (float)rules.GetLSLFloatItem(i + 1);
                        prules.PartMaxAge = tempf;
                    }

                    else if (rule == (int)ScriptBaseClass.PSYS_SRC_ACCEL)
                    {
                        tempv = rules.GetVector3Item(i + 1);
                        prules.PartAcceleration.X = (float)tempv.x;
                        prules.PartAcceleration.Y = (float)tempv.y;
                        prules.PartAcceleration.Z = (float)tempv.z;
                    }

                    else if (rule == (int)ScriptBaseClass.PSYS_SRC_PATTERN)
                    {
                        int tmpi = (int)rules.GetLSLIntegerItem(i + 1);
                        prules.Pattern = (Primitive.ParticleSystem.SourcePattern)tmpi;
                    }

                    // PSYS_SRC_INNERANGLE and PSYS_SRC_ANGLE_BEGIN use the same variables. The
                    // PSYS_SRC_OUTERANGLE and PSYS_SRC_ANGLE_END also use the same variable. The
                    // client tells the difference between the two by looking at the 0x02 bit in
                    // the PartFlags variable.
                    else if (rule == (int)ScriptBaseClass.PSYS_SRC_INNERANGLE)
                    {
                        tempf = (float)rules.GetLSLFloatItem(i + 1);
                        prules.InnerAngle = (float)tempf;
                        prules.PartFlags &= 0xFFFFFFFD; // Make sure new angle format is off.
                    }

                    else if (rule == (int)ScriptBaseClass.PSYS_SRC_OUTERANGLE)
                    {
                        tempf = (float)rules.GetLSLFloatItem(i + 1);
                        prules.OuterAngle = (float)tempf;
                        prules.PartFlags &= 0xFFFFFFFD; // Make sure new angle format is off.
                    }

                    else if (rule == (int)ScriptBaseClass.PSYS_SRC_TEXTURE)
                    {
                        prules.Texture = KeyOrName(rules.GetLSLStringItem(i + 1));
                    }

                    else if (rule == (int)ScriptBaseClass.PSYS_SRC_BURST_RATE)
                    {
                        tempf = (float)rules.GetLSLFloatItem(i + 1);
                        prules.BurstRate = (float)tempf;
                    }

                    else if (rule == (int)ScriptBaseClass.PSYS_SRC_BURST_PART_COUNT)
                    {
                        prules.BurstPartCount = (byte)(int)rules.GetLSLIntegerItem(i + 1);
                    }

                    else if (rule == (int)ScriptBaseClass.PSYS_SRC_BURST_RADIUS)
                    {
                        tempf = (float)rules.GetLSLFloatItem(i + 1);
                        prules.BurstRadius = (float)tempf;
                    }

                    else if (rule == (int)ScriptBaseClass.PSYS_SRC_BURST_SPEED_MIN)
                    {
                        tempf = (float)rules.GetLSLFloatItem(i + 1);
                        prules.BurstSpeedMin = (float)tempf;
                    }

                    else if (rule == (int)ScriptBaseClass.PSYS_SRC_BURST_SPEED_MAX)
                    {
                        tempf = (float)rules.GetLSLFloatItem(i + 1);
                        prules.BurstSpeedMax = (float)tempf;
                    }

                    else if (rule == (int)ScriptBaseClass.PSYS_SRC_MAX_AGE)
                    {
                        tempf = (float)rules.GetLSLFloatItem(i + 1);
                        prules.MaxAge = (float)tempf;
                    }

                    else if (rule == (int)ScriptBaseClass.PSYS_SRC_TARGET_KEY)
                    {
                        UUID key = UUID.Zero;
                        if (UUID.TryParse(rules.Data[i + 1].ToString(), out key))
                        {
                            prules.Target = key;
                        }
                        else
                        {
                            prules.Target = part.UUID;
                        }
                    }

                    else if (rule == (int)ScriptBaseClass.PSYS_SRC_OMEGA)
                    {
                        // AL: This is an assumption, since it is the only thing that would match.
                        tempv = rules.GetVector3Item(i + 1);
                        prules.AngularVelocity.X = (float)tempv.x;
                        prules.AngularVelocity.Y = (float)tempv.y;
                        prules.AngularVelocity.Z = (float)tempv.z;
                    }

                    else if (rule == (int)ScriptBaseClass.PSYS_SRC_ANGLE_BEGIN)
                    {
                        tempf = (float)rules.GetLSLFloatItem(i + 1);
                        prules.InnerAngle = (float)tempf;
                        prules.PartFlags |= 0x02; // Set new angle format.
                    }

                    else if (rule == (int)ScriptBaseClass.PSYS_SRC_ANGLE_END)
                    {
                        tempf = (float)rules.GetLSLFloatItem(i + 1);
                        prules.OuterAngle = (float)tempf;
                        prules.PartFlags |= 0x02; // Set new angle format.
                    }
                }
                prules.CRC = 1;

                part.AddNewParticleSystem(prules);
                part.ParentEntity.HasGroupChanged = true;
            }
            part.ScheduleUpdate(PrimUpdateFlags.Particles);
        }

        public void llGroundRepel(double height, int water, double tau)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            if (m_host.PhysActor != null)
            {
                float ground = (float)llGround(new LSL_Types.Vector3(0, 0, 0));
                float waterLevel = (float)llWater(new LSL_Types.Vector3(0, 0, 0)); 
                PIDHoverType hoverType = PIDHoverType.Ground;
                if (water != 0)
                {
                    hoverType = PIDHoverType.GroundAndWater;
                    if (ground < waterLevel)
                        height += waterLevel;
                    else
                        height += ground;
                }
                else
                {
                    height += ground;
                }
                
                m_host.SetHoverHeight((float)height, hoverType, (float)tau);
            }
        }

        protected UUID GetTaskInventoryItem(string name)
        {
            lock (m_host.TaskInventory)
            {
                foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory)
                {
                    if (inv.Value.Name == name)
                        return inv.Key;
                }
            }

            return UUID.Zero;
        }

        public void llGiveInventoryList(string destination, string category, LSL_List inventory)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            UUID destID;
            if (!UUID.TryParse(destination, out destID))
                return;

            List<UUID> itemList = new List<UUID>();

            foreach (Object item in inventory.Data)
            {
                UUID itemID;
                if (UUID.TryParse(item.ToString(), out itemID))
                {
                    itemList.Add(itemID);
                }
                else
                {
                    itemID = GetTaskInventoryItem(item.ToString());
                    if (itemID != UUID.Zero)
                        itemList.Add(itemID);
                }
            }

            if (itemList.Count == 0)
                return;
            UUID folderID = UUID.Zero;
            ILLClientInventory inventoryModule = World.RequestModuleInterface<ILLClientInventory>();
            if (inventoryModule != null)
                folderID = inventoryModule.MoveTaskInventoryItemsToUserInventory(destID, category, m_host, itemList);

            if (folderID == UUID.Zero)
                return;

            byte[] bucket = new byte[17];
            bucket[0] = (byte)AssetType.Folder;
            byte[] objBytes = folderID.GetBytes();
            Array.Copy(objBytes, 0, bucket, 1, 16);

            GridInstantMessage msg = new GridInstantMessage(World,
                    m_host.UUID, m_host.Name+", an object owned by "+
                    resolveName(m_host.OwnerID)+",", destID,
                    (byte)InstantMessageDialog.InventoryOffered,
                    false, category+"\n"+m_host.Name+" is located at "+
                    World.RegionInfo.RegionName+" "+
                    m_host.AbsolutePosition.ToString(),
                    folderID, true, m_host.AbsolutePosition,
                    bucket);

            if (m_TransferModule != null)
                m_TransferModule.SendInstantMessage(msg);
        }

        public void llSetVehicleType(int type)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            if (m_host.ParentEntity != null)
            {
                if (!m_host.ParentEntity.IsDeleted)
                {
                    m_host.ParentEntity.RootChild.SetVehicleType (type);
                }
            }
        }

        //CFK 9/28: Most, but not all of the underlying plumbing between here and the physics modules is in
        //CFK 9/28: so these are not complete yet.
        public void llSetVehicleFloatParam(int param, LSL_Float value)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");


            if (m_host.ParentEntity != null)
            {
                if (!m_host.ParentEntity.IsDeleted)
                {
                    m_host.ParentEntity.RootChild.SetVehicleFloatParam (param, (float)value);
                }
            }
        }

        //CFK 9/28: Most, but not all of the underlying plumbing between here and the physics modules is in
        //CFK 9/28: so these are not complete yet.
        public void llSetVehicleVectorParam(int param, LSL_Vector vec)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            if (m_host.ParentEntity != null)
            {
                if (!m_host.ParentEntity.IsDeleted)
                {
                    m_host.ParentEntity.RootChild.SetVehicleVectorParam (param,
                        new Vector3((float)vec.x, (float)vec.y, (float)vec.z));
                }
            }
        }

        //CFK 9/28: Most, but not all of the underlying plumbing between here and the physics modules is in
        //CFK 9/28: so these are not complete yet.
        public void llSetVehicleRotationParam(int param, LSL_Rotation rot)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            if (m_host.ParentEntity != null)
            {
                if (!m_host.ParentEntity.IsDeleted)
                {
                    m_host.ParentEntity.RootChild.SetVehicleRotationParam (param,
                        Rot2Quaternion(rot));
                }
            }
        }

        public void llSetVehicleFlags(int flags)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            if (m_host.ParentEntity != null)
            {
                if (!m_host.ParentEntity.IsDeleted)
                {
                    m_host.ParentEntity.RootChild.SetVehicleFlags (flags, false);
                }
            }
        }

        public void llRemoveVehicleFlags(int flags)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            if (m_host.ParentEntity != null)
            {
                if (!m_host.ParentEntity.IsDeleted)
                {
                    m_host.ParentEntity.RootChild.SetVehicleFlags (flags, true);
                }
            }
        }

        public void llSitTarget(LSL_Vector offset, LSL_Rotation rot)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            // LSL quaternions can normalize to 0, normal Quaternions can't.
            if (rot.s == 0 && rot.x == 0 && rot.y == 0 && rot.z == 0)
                rot.z = 1; // ZERO_ROTATION = 0,0,0,1

            m_host.SitTargetPosition = new Vector3((float)offset.x, (float)offset.y, (float)offset.z);
            m_host.SitTargetOrientation = Rot2Quaternion(rot);
            m_host.ParentEntity.HasGroupChanged = true;
        }

        public LSL_String llAvatarOnSitTarget()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            if (m_host.GetAvatarOnSitTarget().Count != 0)
                return m_host.GetAvatarOnSitTarget()[0].ToString();
            else
                return ScriptBaseClass.NULL_KEY;
        }

        public void llAddToLandPassList(string avatar, double hours)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            UUID key;
            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                LandData land = parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y).LandData;
                if (land.OwnerID == m_host.OwnerID)
                {
                    ParcelManager.ParcelAccessEntry entry = new ParcelManager.ParcelAccessEntry();
                    if (UUID.TryParse(avatar, out key))
                    {
                        entry.AgentID = key;
                        entry.Flags = AccessList.Access;
                        entry.Time = DateTime.Now.AddHours(hours);
                        land.ParcelAccessList.Add(entry);
                    }
                }
            }
            ScriptSleep(100);
        }

        public void llSetTouchText(string text)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.TouchName = text;
        }

        public void llSetSitText(string text)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.SitName = text;
        }

        public void llSetCameraEyeOffset(LSL_Vector offset)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.CameraEyeOffset = new Vector3((float)offset.x, (float)offset.y, (float)offset.z);
        }

        public void llSetCameraAtOffset(LSL_Vector offset)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.CameraAtOffset = new Vector3((float)offset.x, (float)offset.y, (float)offset.z);
        }

        public LSL_String llDumpList2String(LSL_List src, string seperator)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            if (src.Length == 0)
            {
                return String.Empty;
            }
            string ret = String.Empty;
            foreach (object o in src.Data)
            {
                ret = ret + o.ToString() + seperator;
            }
            ret = ret.Substring(0, ret.Length - seperator.Length);
            return ret;
        }

        public LSL_Integer llScriptDanger(LSL_Vector pos)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            bool result = m_ScriptEngine.PipeEventsForScript(m_host, new Vector3((float)pos.x, (float)pos.y, (float)pos.z));
            if (result)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public void llDialog(string avatar, string message, LSL_List buttons, int chat_channel)
        {
            IDialogModule dm = World.RequestModuleInterface<IDialogModule>();

            if (dm == null)
                return;

            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            UUID av = new UUID();
            if (!UUID.TryParse(avatar,out av))
            {
                //Silently accepted in in SL NOTE: it does sleep though!
                //LSLError("First parameter to llDialog needs to be a key");
                ScriptSleep(1000);
            }
            if (buttons.Length > 12)
            {
                LSLError("No more than 12 buttons can be shown");
                return;
            }
            string[] buts = new string[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons.Data[i].ToString() == String.Empty)
                {
                    LSLError("button label cannot be blank");
                    return;
                }
                if (buttons.Data[i].ToString().Length > 24)
                {
                    LSLError("button label cannot be longer than 24 characters");
                    return;
                }
                buts[i] = buttons.Data[i].ToString();
            }
            if (buts.Length == 0)
                buts = new string[1] { "OK" };

            dm.SendDialogToUser(
                av, m_host.Name, m_host.UUID, m_host.OwnerID,
                message, new UUID("00000000-0000-2222-3333-100000001000"), chat_channel, buts);

            ScriptSleep(1000);
        }

        public void llVolumeDetect(int detect)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            if (m_host.ParentEntity != null)
            {
                if (!m_host.ParentEntity.IsDeleted)
                {
                    m_host.ParentEntity.RootChild.ScriptSetVolumeDetect (detect != 0);
                }
            }
        }

        /// <summary>
        /// This is a depecated function so this just replicates the result of
        /// invoking it in SL
        /// </summary>

        public void llRemoteLoadScript(string target, string name, int running, int start_param)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            // Report an error as it does in SL
            ShoutError("Deprecated. Please use llRemoteLoadScriptPin instead.");
            ScriptSleep(3000);
        }

        public void llSetRemoteScriptAccessPin(int pin)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.ScriptAccessPin = pin;
        }

        public void llRemoteLoadScriptPin(string target, string name, int pin, int running, int start_param)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            bool found = false;
            UUID destId = UUID.Zero;
            UUID srcId = UUID.Zero;

            if (!UUID.TryParse(target, out destId))
            {
                llSay(0, "Could not parse key " + target);
                return;
            }

            // target must be a different prim than the one containing the script
            if (m_host.UUID == destId)
                return;

            // copy the first script found with this inventory name
            lock (m_host.TaskInventory)
            {
                foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory)
                {
                    if (inv.Value.Name == name)
                    {
                        // make sure the object is a script
                        if (10 == inv.Value.Type)
                        {
                            found = true;
                            srcId = inv.Key;
                            break;
                        }
                    }
                }
            }

            if (!found)
            {
                llSay(0, "Could not find script " + name);
                return;
            }

            // the rest of the permission checks are done in RezScript, so check the pin there as well
            ILLClientInventory inventoryModule = World.RequestModuleInterface<ILLClientInventory>();
            if (inventoryModule != null)
                inventoryModule.RezScript(srcId, m_host, destId, pin, running, start_param);
            // this will cause the delay even if the script pin or permissions were wrong - seems ok
            ScriptSleep(3000); 
        }

        public void llOpenRemoteDataChannel()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            IXMLRPC xmlrpcMod = World.RequestModuleInterface<IXMLRPC>();
            if (xmlrpcMod.IsEnabled())
            {
                UUID channelID = xmlrpcMod.OpenXMLRPCChannel(m_host.UUID, m_itemID, UUID.Zero);
                IXmlRpcRouter xmlRpcRouter = World.RequestModuleInterface<IXmlRpcRouter>();
                if (xmlRpcRouter != null)
                {
                    string ExternalHostName = World.RegionInfo.ExternalHostName;
                    
                    xmlRpcRouter.RegisterNewReceiver(m_ScriptEngine.ScriptModule, channelID, m_host.UUID, 
                                                     m_itemID, String.Format("http://{0}:{1}/", ExternalHostName, 
                                                                             xmlrpcMod.Port.ToString()));
                }
                object[] resobj = new object[] 
                    { 
                        new LSL_Integer(1), 
                        new LSL_String(channelID.ToString()), 
                        new LSL_String(UUID.Zero.ToString()), 
                        new LSL_String(String.Empty), 
                        new LSL_Integer(0), 
                        new LSL_String(String.Empty) 
                    };
                m_ScriptEngine.PostScriptEvent(m_itemID, m_host.UUID, new EventParams("remote_data", resobj,
                                                                         new DetectParams[0]));
            }
            ScriptSleep(1000);
        }

        public LSL_Key llSendRemoteData(string channel, string dest, int idata, string sdata)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            IXMLRPC xmlrpcMod = World.RequestModuleInterface<IXMLRPC>();
            ScriptSleep(3000);
            return (LSL_Key)(xmlrpcMod.SendRemoteData(m_host.UUID, m_itemID, channel, dest, idata, sdata)).ToString();
        }

        public void llRemoteDataReply(string channel, string message_id, string sdata, int idata)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            IXMLRPC xmlrpcMod = World.RequestModuleInterface<IXMLRPC>();
            xmlrpcMod.RemoteDataReply(channel, message_id, sdata, idata);
            ScriptSleep(100);
        }

        public void llCloseRemoteDataChannel(object _channel)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            IXMLRPC xmlrpcMod = World.RequestModuleInterface<IXMLRPC>();
            xmlrpcMod.CloseXMLRPCChannel(UUID.Parse(_channel.ToString()));
            ScriptSleep(100); 
        }

        public LSL_String llMD5String(string src, int nonce)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return Util.Md5Hash(String.Format("{0}:{1}", src, nonce.ToString()));
        }

        public LSL_String llSHA1String(string src)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return Util.SHA1Hash(src).ToLower();
        }

        protected ObjectShapePacket.ObjectDataBlock SetPrimitiveBlockShapeParams(SceneObjectPart part, int holeshape, LSL_Vector cut, float hollow, LSL_Vector twist)
        {
            ObjectShapePacket.ObjectDataBlock shapeBlock = new ObjectShapePacket.ObjectDataBlock();

            if (holeshape != (int)ScriptBaseClass.PRIM_HOLE_DEFAULT &&
                holeshape != (int)ScriptBaseClass.PRIM_HOLE_CIRCLE &&
                holeshape != (int)ScriptBaseClass.PRIM_HOLE_SQUARE &&
                holeshape != (int)ScriptBaseClass.PRIM_HOLE_TRIANGLE)
            {
                holeshape = (int)ScriptBaseClass.PRIM_HOLE_DEFAULT;
            }
            shapeBlock.ProfileCurve = (byte)holeshape;
            if (cut.x < 0f)
            {
                cut.x = 0f;
            }
            if (cut.x > 1f)
            {
                cut.x = 1f;
            }
            if (cut.y < 0f)
            {
                cut.y = 0f;
            }
            if (cut.y > 1f)
            {
                cut.y = 1f;
            }
            if (cut.y - cut.x < 0.05f)
            {
                cut.x = cut.y - 0.05f;
                if (cut.x < 0.0f)
                {
                    cut.x = 0.0f;
                    cut.y = 0.05f;
                }
            }
            shapeBlock.ProfileBegin = (ushort)(50000 * cut.x);
            shapeBlock.ProfileEnd = (ushort)(50000 * (1 - cut.y));
            if (hollow < 0f)
            {
                hollow = 0f;
            }
            if (hollow > 0.95)
            {
                hollow = 0.95f;
            }
            shapeBlock.ProfileHollow = (ushort)(50000 * hollow);
            if (twist.x < -1.0f)
            {
                twist.x = -1.0f;
            }
            if (twist.x > 1.0f)
            {
                twist.x = 1.0f;
            }
            if (twist.y < -1.0f)
            {
                twist.y = -1.0f;
            }
            if (twist.y > 1.0f)
            {
                twist.y = 1.0f;
            }
            shapeBlock.PathTwistBegin = (sbyte)(100 * twist.x);
            shapeBlock.PathTwist = (sbyte)(100 * twist.y);

            shapeBlock.ObjectLocalID = part.LocalId;

            // retain pathcurve
            shapeBlock.PathCurve = part.Shape.PathCurve;

            part.Shape.SculptEntry = false;
            return shapeBlock;
        }

        protected void SetPrimitiveShapeParams(SceneObjectPart part, int holeshape, LSL_Vector cut, float hollow, LSL_Vector twist, LSL_Vector taper_b, LSL_Vector topshear, byte fudge)
        {
            ObjectShapePacket.ObjectDataBlock shapeBlock;

            shapeBlock = SetPrimitiveBlockShapeParams(part, holeshape, cut, hollow, twist);

            shapeBlock.ProfileCurve += fudge;

            if (taper_b.x < 0f)
            {
                taper_b.x = 0f;
            }
            if (taper_b.x > 2f)
            {
                taper_b.x = 2f;
            }
            if (taper_b.y < 0f)
            {
                taper_b.y = 0f;
            }
            if (taper_b.y > 2f)
            {
                taper_b.y = 2f;
            }
            shapeBlock.PathScaleX = (byte)(100 * (2.0 - taper_b.x));
            shapeBlock.PathScaleY = (byte)(100 * (2.0 - taper_b.y));
            if (topshear.x < -0.5f)
            {
                topshear.x = -0.5f;
            }
            if (topshear.x > 0.5f)
            {
                topshear.x = 0.5f;
            }
            if (topshear.y < -0.5f)
            {
                topshear.y = -0.5f;
            }
            if (topshear.y > 0.5f)
            {
                topshear.y = 0.5f;
            }
            shapeBlock.PathShearX = (byte)(100 * topshear.x);
            shapeBlock.PathShearY = (byte)(100 * topshear.y);

            part.Shape.SculptEntry = false;
            part.UpdateShape(shapeBlock);
        }

        protected void SetPrimitiveShapeParams(SceneObjectPart part, int holeshape, LSL_Vector cut, float hollow, LSL_Vector twist, LSL_Vector dimple, byte fudge)
        {
            ObjectShapePacket.ObjectDataBlock shapeBlock;

            shapeBlock = SetPrimitiveBlockShapeParams(part, holeshape, cut, hollow, twist);

            // profile/path swapped for a sphere
            shapeBlock.PathBegin = shapeBlock.ProfileBegin;
            shapeBlock.PathEnd = shapeBlock.ProfileEnd;

            shapeBlock.ProfileCurve += fudge;

            shapeBlock.PathScaleX = 100;
            shapeBlock.PathScaleY = 100;

            if (dimple.x < 0f)
            {
                dimple.x = 0f;
            }
            if (dimple.x > 1f)
            {
                dimple.x = 1f;
            }
            if (dimple.y < 0f)
            {
                dimple.y = 0f;
            }
            if (dimple.y > 1f)
            {
                dimple.y = 1f;
            }
            if (dimple.y - cut.x < 0.05f)
            {
                dimple.x = cut.y - 0.05f;
            }
            shapeBlock.ProfileBegin = (ushort)(50000 * dimple.x);
            shapeBlock.ProfileEnd   = (ushort)(50000 * (1 - dimple.y));

            part.Shape.SculptEntry = false;
            part.UpdateShape(shapeBlock);
        }

        protected void SetPrimitiveShapeParams(SceneObjectPart part, int holeshape, LSL_Vector cut, float hollow, LSL_Vector twist, LSL_Vector holesize, LSL_Vector topshear, LSL_Vector profilecut, LSL_Vector taper_a, float revolutions, float radiusoffset, float skew, byte fudge)
        {
            ObjectShapePacket.ObjectDataBlock shapeBlock;

            shapeBlock = SetPrimitiveBlockShapeParams(part, holeshape, cut, hollow, twist);

            shapeBlock.ProfileCurve += fudge;

            // profile/path swapped for a torrus, tube, ring
            shapeBlock.PathBegin = shapeBlock.ProfileBegin;
            shapeBlock.PathEnd = shapeBlock.ProfileEnd;

            if (holesize.x < 0.05f)
            {
                holesize.x = 0.05f;
            }
            if (holesize.x > 1f)
            {
                holesize.x = 1f;
            }
            if (holesize.y < 0.05f)
            {
                holesize.y = 0.05f;
            }
            if (holesize.y > 0.5f)
            {
                holesize.y = 0.5f;
            }
            shapeBlock.PathScaleX = (byte)(100 * (2 - holesize.x));
            shapeBlock.PathScaleY = (byte)(100 * (2 - holesize.y));
            if (topshear.x < -0.5f)
            {
                topshear.x = -0.5f;
            }
            if (topshear.x > 0.5f)
            {
                topshear.x = 0.5f;
            }
            if (topshear.y < -0.5f)
            {
                topshear.y = -0.5f;
            }
            if (topshear.y > 0.5f)
            {
                topshear.y = 0.5f;
            }
            shapeBlock.PathShearX = (byte)(100 * topshear.x);
            shapeBlock.PathShearY = (byte)(100 * topshear.y);
            if (profilecut.x < 0f)
            {
                profilecut.x = 0f;
            }
            if (profilecut.x > 1f)
            {
                profilecut.x = 1f;
            }
            if (profilecut.y < 0f)
            {
                profilecut.y = 0f;
            }
            if (profilecut.y > 1f)
            {
                profilecut.y = 1f;
            }
            if (profilecut.y - profilecut.x < 0.05f)
            {
                profilecut.x = profilecut.y - 0.05f;
                if (profilecut.x < 0.0f)
                {
                    profilecut.x = 0.0f;
                    profilecut.y = 0.05f;
                }
            }
            shapeBlock.ProfileBegin = (ushort)(50000 * profilecut.x);
            shapeBlock.ProfileEnd = (ushort)(50000 * (1 - profilecut.y));
            if (taper_a.x < -1f)
            {
                taper_a.x = -1f;
            }
            if (taper_a.x > 1f)
            {
                taper_a.x = 1f;
            }
            if (taper_a.y < -1f)
            {
                taper_a.y = -1f;
            }
            if (taper_a.y > 1f)
            {
                taper_a.y = 1f;
            }
            shapeBlock.PathTaperX = (sbyte)(100 * taper_a.x);
            shapeBlock.PathTaperY = (sbyte)(100 * taper_a.y);
            if (revolutions < 1f)
            {
                revolutions = 1f;
            }
            if (revolutions > 4f)
            {
                revolutions = 4f;
            }
            shapeBlock.PathRevolutions = (byte)(66.666667 * (revolutions - 1.0));
            // limits on radiusoffset depend on revolutions and hole size (how?) seems like the maximum range is 0 to 1
            if (radiusoffset < 0f)
            {
                radiusoffset = 0f;
            }
            if (radiusoffset > 1f)
            {
                radiusoffset = 1f;
            }
            shapeBlock.PathRadiusOffset = (sbyte)(100 * radiusoffset);
            if (skew < -0.95f)
            {
                skew = -0.95f;
            }
            if (skew > 0.95f)
            {
                skew = 0.95f;
            }
            shapeBlock.PathSkew = (sbyte)(100 * skew);

            part.Shape.SculptEntry = false;
            part.UpdateShape(shapeBlock);
        }

        protected void SetPrimitiveShapeParams(SceneObjectPart part, string map, int type)
        {
            ObjectShapePacket.ObjectDataBlock shapeBlock = new ObjectShapePacket.ObjectDataBlock();
            UUID sculptId;

            if (!UUID.TryParse(map, out sculptId))
            {
                sculptId = InventoryKey(map, (int)AssetType.Texture);
            }

            if (sculptId == UUID.Zero)
                return;

            shapeBlock.ObjectLocalID = part.LocalId;
            shapeBlock.PathScaleX = 100;
            shapeBlock.PathScaleY = 150;

            if (type != (int)ScriptBaseClass.PRIM_SCULPT_TYPE_CYLINDER &&
                type != (int)ScriptBaseClass.PRIM_SCULPT_TYPE_PLANE &&
                type != (int)ScriptBaseClass.PRIM_SCULPT_TYPE_SPHERE &&
                type != (int)ScriptBaseClass.PRIM_SCULPT_TYPE_TORUS)
            {
                // default
                type = (int)ScriptBaseClass.PRIM_SCULPT_TYPE_SPHERE;
            }

            // retain pathcurve
            shapeBlock.PathCurve = part.Shape.PathCurve;

            part.Shape.SetSculptData((byte)type, sculptId);
            part.Shape.SculptEntry = true;
            part.UpdateShape(shapeBlock);
        }

        public void llSetPrimitiveParams(LSL_List rules)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            SetPrimParams(m_host, rules);
        }

        public void llSetLinkPrimitiveParams(int linknumber, LSL_List rules)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");


            List<IEntity> parts = GetLinkPartsAndEntities (linknumber);

            foreach (IEntity part in parts)
                SetPrimParams(part, rules);
        }

        public void llSetLinkPrimitiveParamsFast(int linknumber, LSL_List rules)
        {
            List<ISceneChildEntity> parts = GetLinkParts (linknumber);

            foreach (ISceneChildEntity part in parts)
                SetPrimParams(part, rules);
        }

        public LSL_Integer llGetLinkNumberOfSides(int LinkNum)
        {
            int faces = 0;
            List<ISceneChildEntity> Parts = GetLinkParts (LinkNum);
            foreach (ISceneChildEntity part in Parts)
            {
                faces += GetNumberOfSides(part);
            }
            return new LSL_Integer(faces);
        }

        protected void SetPrimParams (IEntity part, LSL_List rules)
        {
            int idx = 0;

            while (idx < rules.Length)
            {
                int code = rules.GetLSLIntegerItem(idx++);

                int remain = rules.Length - idx;

                int face;
                LSL_Vector v;

                if (code == (int)ScriptBaseClass.PRIM_NAME)
                {
                    if (remain < 1)
                        return;

                    string name = rules.Data[idx++].ToString();
                    if (part is SceneObjectPart)
                        (part as SceneObjectPart).Name = name;
                }

                else if (code == (int)ScriptBaseClass.PRIM_DESC)
                {
                    if (remain < 1)
                        return;

                    string desc = rules.Data[idx++].ToString();
                    if (part is SceneObjectPart)
                        (part as SceneObjectPart).Description = desc;
                }

                else if (code == (int)ScriptBaseClass.PRIM_ROT_LOCAL)
                {
                    if (remain < 1)
                        return;
                    LSL_Rotation lr = rules.GetQuaternionItem(idx++);
                    if (part is SceneObjectPart)
                        SetRot((part as SceneObjectPart), Rot2Quaternion(lr));
                }

                else if (code == (int)ScriptBaseClass.PRIM_POSITION)
                {
                    if (remain < 1)
                        return;

                    v = rules.GetVector3Item(idx++);
                    if (part is SceneObjectPart)
                        SetPos(part as SceneObjectPart, v);
                    else if (part is IScenePresence)
                    {
                        (part as IScenePresence).OffsetPosition = new Vector3 ((float)v.x, (float)v.y, (float)v.z);
                        (part as IScenePresence).SendTerseUpdateToAllClients ();
                    }
                }
                else if (code == (int)ScriptBaseClass.PRIM_SIZE)
                {
                    if (remain < 1)
                        return;


                    v = rules.GetVector3Item(idx++);
                    if (part is SceneObjectPart)
                        SetScale(part as SceneObjectPart, v);

                }
                else if (code == (int)ScriptBaseClass.PRIM_ROTATION)
                {
                    if (remain < 1)
                        return;

                    if (part is SceneObjectPart) { }
                    else return;

                    LSL_Rotation q = rules.GetQuaternionItem(idx++);
                    // try to let this work as in SL...
                    if ((part as SceneObjectPart).ParentID == 0)
                    {
                        // special case: If we are root, rotate complete SOG to new rotation
                        SetRot(part as SceneObjectPart, Rot2Quaternion(q));
                    }
                    else
                    {
                        // we are a child. The rotation values will be set to the one of root modified by rot, as in SL. Don't ask.
                        SceneObjectGroup group = (part as SceneObjectPart).ParentGroup;
                        if (group != null) // a bit paranoid, maybe
                        {
                            SceneObjectPart rootPart = group.RootPart;
                            if (rootPart != null) // again, better safe than sorry
                            {
                                SetRot((part as SceneObjectPart), rootPart.RotationOffset * Rot2Quaternion(q));
                            }
                        }
                    }

                }

                else if (code == (int)ScriptBaseClass.PRIM_TYPE)
                {
                    if (remain < 3)
                        return;

                    if (part is SceneObjectPart) { }
                    else
                        return;

                    code = (int)rules.GetLSLIntegerItem(idx++);

                    remain = rules.Length - idx;
                    float hollow;
                    LSL_Vector twist;
                    LSL_Vector taper_b;
                    LSL_Vector topshear;
                    float revolutions;
                    float radiusoffset;
                    float skew;
                    LSL_Vector holesize;
                    LSL_Vector profilecut;

                    if (code == (int)ScriptBaseClass.PRIM_TYPE_BOX)
                    {
                        if (remain < 6)
                            return;

                        face = (int)rules.GetLSLIntegerItem(idx++);
                        v = rules.GetVector3Item(idx++); // cut
                        hollow = (float)rules.GetLSLFloatItem(idx++);
                        twist = rules.GetVector3Item(idx++);
                        taper_b = rules.GetVector3Item(idx++);
                        topshear = rules.GetVector3Item(idx++);

                        (part as SceneObjectPart).Shape.PathCurve = (byte)Extrusion.Straight;
                        SetPrimitiveShapeParams((part as SceneObjectPart), face, v, hollow, twist, taper_b, topshear, 1);

                    }

                    else if (code == (int)ScriptBaseClass.PRIM_TYPE_CYLINDER)
                    {
                        if (remain < 6)
                            return;

                        face = (int)rules.GetLSLIntegerItem(idx++); // holeshape
                        v = rules.GetVector3Item(idx++); // cut
                        hollow = (float)rules.GetLSLFloatItem(idx++);
                        twist = rules.GetVector3Item(idx++);
                        taper_b = rules.GetVector3Item(idx++);
                        topshear = rules.GetVector3Item(idx++);
                        (part as SceneObjectPart).Shape.ProfileShape = ProfileShape.Circle;
                        (part as SceneObjectPart).Shape.PathCurve = (byte)Extrusion.Straight;
                        SetPrimitiveShapeParams((part as SceneObjectPart), face, v, hollow, twist, taper_b, topshear, 0);

                    }

                    else if (code == (int)ScriptBaseClass.PRIM_TYPE_PRISM)
                    {
                        if (remain < 6)
                            return;

                        face = (int)rules.GetLSLIntegerItem(idx++); // holeshape
                        v = rules.GetVector3Item(idx++); //cut
                        hollow = (float)rules.GetLSLFloatItem(idx++);
                        twist = rules.GetVector3Item(idx++);
                        taper_b = rules.GetVector3Item(idx++);
                        topshear = rules.GetVector3Item(idx++);
                        (part as SceneObjectPart).Shape.PathCurve = (byte)Extrusion.Straight;
                        SetPrimitiveShapeParams((part as SceneObjectPart), face, v, hollow, twist, taper_b, topshear, 3);

                    }

                    else if (code == (int)ScriptBaseClass.PRIM_TYPE_SPHERE)
                    {
                        if (remain < 5)
                            return;

                        face = (int)rules.GetLSLIntegerItem(idx++); // holeshape
                        v = rules.GetVector3Item(idx++); // cut
                        hollow = (float)rules.GetLSLFloatItem(idx++);
                        twist = rules.GetVector3Item(idx++);
                        taper_b = rules.GetVector3Item(idx++); // dimple
                        (part as SceneObjectPart).Shape.PathCurve = (byte)Extrusion.Curve1;
                        SetPrimitiveShapeParams((part as SceneObjectPart), face, v, hollow, twist, taper_b, 5);

                    }

                    else if (code == (int)ScriptBaseClass.PRIM_TYPE_TORUS)
                    {
                        if (remain < 11)
                            return;

                        face = (int)rules.GetLSLIntegerItem(idx++); // holeshape
                        v = rules.GetVector3Item(idx++); //cut
                        hollow = (float)rules.GetLSLFloatItem(idx++);
                        twist = rules.GetVector3Item(idx++);
                        holesize = rules.GetVector3Item(idx++);
                        topshear = rules.GetVector3Item(idx++);
                        profilecut = rules.GetVector3Item(idx++);
                        taper_b = rules.GetVector3Item(idx++); // taper_a
                        revolutions = (float)rules.GetLSLFloatItem(idx++);
                        radiusoffset = (float)rules.GetLSLFloatItem(idx++);
                        skew = (float)rules.GetLSLFloatItem(idx++);
                        (part as SceneObjectPart).Shape.PathCurve = (byte)Extrusion.Curve1;
                        SetPrimitiveShapeParams((part as SceneObjectPart), face, v, hollow, twist, holesize, topshear, profilecut, taper_b,
                                                revolutions, radiusoffset, skew, 0);
                    }

                    else if (code == (int)ScriptBaseClass.PRIM_TYPE_TUBE)
                    {
                        if (remain < 11)
                            return;

                        face = (int)rules.GetLSLIntegerItem(idx++); // holeshape
                        v = rules.GetVector3Item(idx++); //cut
                        hollow = (float)rules.GetLSLFloatItem(idx++);
                        twist = rules.GetVector3Item(idx++);
                        holesize = rules.GetVector3Item(idx++);
                        topshear = rules.GetVector3Item(idx++);
                        profilecut = rules.GetVector3Item(idx++);
                        taper_b = rules.GetVector3Item(idx++); // taper_a
                        revolutions = (float)rules.GetLSLFloatItem(idx++);
                        radiusoffset = (float)rules.GetLSLFloatItem(idx++);
                        skew = (float)rules.GetLSLFloatItem(idx++);
                        (part as SceneObjectPart).Shape.PathCurve = (byte)Extrusion.Curve1;
                        SetPrimitiveShapeParams((part as SceneObjectPart), face, v, hollow, twist, holesize, topshear, profilecut, taper_b,
                                                revolutions, radiusoffset, skew, 1);
                    }

                    else if (code == (int)ScriptBaseClass.PRIM_TYPE_RING)
                    {
                        if (remain < 11)
                            return;

                        face = (int)rules.GetLSLIntegerItem(idx++); // holeshape
                        v = rules.GetVector3Item(idx++); //cut
                        hollow = (float)rules.GetLSLFloatItem(idx++);
                        twist = rules.GetVector3Item(idx++);
                        holesize = rules.GetVector3Item(idx++);
                        topshear = rules.GetVector3Item(idx++);
                        profilecut = rules.GetVector3Item(idx++);
                        taper_b = rules.GetVector3Item(idx++); // taper_a
                        revolutions = (float)rules.GetLSLFloatItem(idx++);
                        radiusoffset = (float)rules.GetLSLFloatItem(idx++);
                        skew = (float)rules.GetLSLFloatItem(idx++);
                        (part as SceneObjectPart).Shape.PathCurve = (byte)Extrusion.Curve1;
                        SetPrimitiveShapeParams((part as SceneObjectPart), face, v, hollow, twist, holesize, topshear, profilecut, taper_b,
                                                revolutions, radiusoffset, skew, 3);
                    }

                    else if (code == (int)ScriptBaseClass.PRIM_TYPE_SCULPT)
                    {
                        if (remain < 2)
                            return;

                        string map = rules.Data[idx++].ToString();
                        face = (int)rules.GetLSLIntegerItem(idx++); // type
                        (part as SceneObjectPart).Shape.PathCurve = (byte)Extrusion.Curve1;
                        SetPrimitiveShapeParams((part as SceneObjectPart), map, face);
                    }
                }

                else if (code == (int)ScriptBaseClass.PRIM_TEXTURE)
                {
                    if (remain < 5)
                        return;
                    if (part is SceneObjectPart) { }
                    else
                        return;
                    face = (int)rules.GetLSLIntegerItem(idx++);
                    string tex = rules.Data[idx++].ToString();
                    LSL_Vector repeats = rules.GetVector3Item(idx++);
                    LSL_Vector offsets = rules.GetVector3Item(idx++);
                    double rotation = (double)rules.GetLSLFloatItem(idx++);

                    SetTexture((part as SceneObjectPart), tex, face);
                    ScaleTexture((part as SceneObjectPart), repeats.x, repeats.y, face);
                    OffsetTexture((part as SceneObjectPart), offsets.x, offsets.y, face);
                    RotateTexture((part as SceneObjectPart), rotation, face);

                }

                else if (code == (int)ScriptBaseClass.PRIM_COLOR)
                {
                    if (remain < 3)
                        return;
                    if (part is SceneObjectPart) { }
                    else
                        return;
                    face = (int)rules.GetLSLIntegerItem(idx++);
                    LSL_Vector color = rules.GetVector3Item(idx++);
                    double alpha = (double)rules.GetLSLFloatItem(idx++);

                    (part as SceneObjectPart).SetFaceColor(new Vector3((float)color.x, (float)color.y, (float)color.z), face);
                    SetAlpha((part as SceneObjectPart), alpha, face);

                }

                else if (code == (int)ScriptBaseClass.PRIM_FLEXIBLE)
                {
                    if (remain < 7)
                        return;
                    if (!(part is SceneObjectPart))
                        return;
                    bool flexi = rules.GetLSLIntegerItem(idx++);
                    int softness = rules.GetLSLIntegerItem(idx++);
                    float gravity = (float)rules.GetLSLFloatItem(idx++);
                    float friction = (float)rules.GetLSLFloatItem(idx++);
                    float wind = (float)rules.GetLSLFloatItem(idx++);
                    float tension = (float)rules.GetLSLFloatItem(idx++);
                    LSL_Vector force = rules.GetVector3Item(idx++);

                    SetFlexi((part as SceneObjectPart), flexi, softness, gravity, friction, wind, tension, force);
                }
                else if (code == (int)ScriptBaseClass.PRIM_POINT_LIGHT)
                {
                    if (remain < 5)
                        return;
                    if (!(part is SceneObjectPart))
                        return;
                    bool light = rules.GetLSLIntegerItem(idx++);
                    LSL_Vector lightcolor = rules.GetVector3Item(idx++);
                    float intensity = (float)rules.GetLSLFloatItem(idx++);
                    float radius = (float)rules.GetLSLFloatItem(idx++);
                    float falloff = (float)rules.GetLSLFloatItem(idx++);

                    SetPointLight((part as SceneObjectPart), light, lightcolor, intensity, radius, falloff);

                }

                else if (code == (int)ScriptBaseClass.PRIM_GLOW)
                {
                    if (remain < 2)
                        return;
                    if (!(part is SceneObjectPart))
                        return;
                    face = rules.GetLSLIntegerItem(idx++);
                    float glow = (float)rules.GetLSLFloatItem(idx++);

                    SetGlow((part as SceneObjectPart), face, glow);

                }
                else if (code == (int)ScriptBaseClass.PRIM_BUMP_SHINY)
                {
                    if (remain < 3)
                        return;
                    if (!(part is SceneObjectPart))
                        return;
                    face = (int)rules.GetLSLIntegerItem(idx++);
                    int shiny = (int)rules.GetLSLIntegerItem(idx++);
                    Bumpiness bump = (Bumpiness)Convert.ToByte((int)rules.GetLSLIntegerItem(idx++));

                    SetShiny((part as SceneObjectPart), face, shiny, bump);

                }
                else if (code == (int)ScriptBaseClass.PRIM_FULLBRIGHT)
                {
                    if (remain < 2)
                        return;
                    if (!(part is SceneObjectPart))
                        return;
                    face = rules.GetLSLIntegerItem(idx++);
                    bool st = rules.GetLSLIntegerItem(idx++);
                    SetFullBright((part as SceneObjectPart), face, st);
                }

                else if (code == (int)ScriptBaseClass.PRIM_MATERIAL)
                {
                    if (remain < 1)
                        return;
                    if (!(part is SceneObjectPart))
                        return;
                    int mat = rules.GetLSLIntegerItem(idx++);
                    if (mat < 0 || mat > 7)
                        return;

                    (part as SceneObjectPart).Material = Convert.ToByte(mat);
                }
                else if (code == (int)ScriptBaseClass.PRIM_PHANTOM)
                {
                    if (remain < 1)
                        return;
                    if (!(part is SceneObjectPart))
                        return;
                    string ph = rules.Data[idx++].ToString();
                    bool phantom;

                    if (ph.Equals("1"))
                        phantom = true;
                    else
                        phantom = false;

                    (part as SceneObjectPart).ScriptSetPhantomStatus(phantom);
                }
                else if (code == (int)ScriptBaseClass.PRIM_PHYSICS)
                {
                    if (remain < 1)
                        return;
                    if (!(part is SceneObjectPart))
                        return;
                    string phy = rules.Data[idx++].ToString();
                    bool physics;

                    if (phy.Equals("1"))
                        physics = true;
                    else
                        physics = false;

                    (part as SceneObjectPart).ScriptSetPhysicsStatus(physics);
                }
                else if (code == (int)ScriptBaseClass.PRIM_TEMP_ON_REZ)
                {
                    if (remain < 1)
                        return;
                    if (!(part is SceneObjectPart))
                        return;
                    string temp = rules.Data[idx++].ToString();
                    bool tempOnRez;

                    if (temp.Equals("1"))
                        tempOnRez = true;
                    else
                        tempOnRez = false;

                    (part as SceneObjectPart).ScriptSetTemporaryStatus(tempOnRez);
                }
                else if (code == (int)ScriptBaseClass.PRIM_TEXGEN)
                {
                    if (remain < 2)
                        return;
                    if (!(part is SceneObjectPart))
                        return;
                    //face,type
                    face = rules.GetLSLIntegerItem(idx++);
                    int style = rules.GetLSLIntegerItem(idx++);
                    SetTexGen((part as SceneObjectPart), face, style);
                }
                else if (code == (int)ScriptBaseClass.PRIM_TEXT)
                {
                    if (remain < 3)
                        return;
                    if (!(part is SceneObjectPart))
                        return;
                    string primText = rules.GetLSLStringItem(idx++);
                    LSL_Vector primTextColor = rules.GetVector3Item(idx++);
                    LSL_Float primTextAlpha = rules.GetLSLFloatItem(idx++);
                    Vector3 av3 = new Vector3(Util.Clip((float)primTextColor.x, 0.0f, 1.0f),
                                  Util.Clip((float)primTextColor.y, 0.0f, 1.0f),
                                  Util.Clip((float)primTextColor.z, 0.0f, 1.0f));
                    (part as SceneObjectPart).SetText(primText, av3, Util.Clip((float)primTextAlpha, 0.0f, 1.0f));

                }
            }
        }

        public LSL_String llStringToBase64(string str)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            try
            {
                byte[] encData_byte = new byte[str.Length];
                encData_byte = Util.UTF8.GetBytes(str);
                string encodedData = Convert.ToBase64String(encData_byte);
                return encodedData;
            }
            catch (Exception e)
            {
                throw new Exception("Error in base64Encode" + e.ToString());
            }
        }

        public LSL_String llBase64ToString(string str)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            try
            {
                return Util.Base64ToString(str);
            }
            catch (Exception e)
            {
                throw new Exception("Error in base64Decode" + e.ToString());
            }
        }

        public LSL_String llXorBase64Strings(string str1, string str2)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            Deprecated("llXorBase64Strings");
            ScriptSleep(300);
            return (LSL_String)String.Empty;
        }

        public void llRemoteDataSetRegion()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            Deprecated("llRemoteDataSetRegion");
        }

        public LSL_Float llLog10(double val)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return (double)Math.Log10(val);
        }

        public LSL_Float llLog(double val)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return (double)Math.Log(val);
        }

        public LSL_List llGetAnimationList(string id)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            

            LSL_List l = new LSL_List();
            IScenePresence av = World.GetScenePresence((UUID)id);
            if (av == null || av.IsChildAgent) // only if in the region
                return l;
            UUID[] anims;
            anims = av.Animator.GetAnimationArray();
            foreach (UUID foo in anims)
                l.Add(foo.ToString());
            return l;
        }

        public void llSetParcelMusicURL(string url)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                ILandObject land = parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y);

                if (land == null)
                    return;

                if (!World.Permissions.CanEditParcel(m_host.OwnerID, land))
                    return;

                land.SetMusicUrl(url);
            }

            ScriptSleep(2000); 
        }

        public LSL_Vector llGetRootPosition()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            return new LSL_Vector (m_host.ParentEntity.AbsolutePosition.X, m_host.ParentEntity.AbsolutePosition.Y,
                                  m_host.ParentEntity.AbsolutePosition.Z);
        }

        /// <summary>
        /// http://lslwiki.net/lslwiki/wakka.php?wakka=llGetRot
        /// http://lslwiki.net/lslwiki/wakka.php?wakka=ChildRotation
        /// Also tested in sl in regards to the behaviour in attachments/mouselook
        /// In the root prim:-
        ///     Returns the object rotation if not attached
        ///     Returns the avatars rotation if attached
        ///     Returns the camera rotation if attached and the avatar is in mouselook
        /// </summary>
        public LSL_Rotation llGetRootRotation()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            Quaternion q;
            if (m_host.ParentEntity.RootChild.AttachmentPoint != 0)
            {
                IScenePresence avatar = World.GetScenePresence (m_host.AttachedAvatar);
                if (avatar != null)
                    if ((avatar.AgentControlFlags & (uint)AgentManager.ControlFlags.AGENT_CONTROL_MOUSELOOK) != 0)
                        q = avatar.CameraRotation; // Mouselook
                    else
                        q = avatar.Rotation; // Currently infrequently updated so may be inaccurate
                else
                    q = m_host.ParentEntity.GroupRotation; // Likely never get here but just in case
            }
            else
                q = m_host.ParentEntity.GroupRotation; // just the group rotation
            return new LSL_Rotation(q.X, q.Y, q.Z, q.W);
        }

        public LSL_String llGetObjectDesc()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return m_host.Description != null ? m_host.Description : String.Empty;
        }

        public void llSetObjectDesc(string desc)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.Description = desc != null ? desc : String.Empty;
        }

        public LSL_String llGetCreator()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return m_host.CreatorID.ToString();
        }

        public LSL_String llGetTimestamp()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
        }

        public LSL_Integer llGetNumberOfPrims()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            int avatarCount = m_host.SitTargetAvatar.Count;
            
            /*World.ForEachScenePresence(delegate(ScenePresence presence)
            {
                if (!presence.IsChildAgent && presence.ParentID != UUID.Zero && m_host.ParentGroup.HasChildPrim(presence.ParentID))
                        avatarCount++;
            });*/

            return m_host.ParentEntity.PrimCount + avatarCount;
        }

        /// <summary>
        /// A partial implementation.
        /// http://lslwiki.net/lslwiki/wakka.php?wakka=llGetBoundingBox
        /// So far only valid for standing/flying/ground sitting avatars and single prim objects.
        /// If the object has multiple prims and/or a sitting avatar then the bounding
        /// box is for the root prim only.
        /// </summary>
        public LSL_List llGetBoundingBox(string obj)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            UUID objID = UUID.Zero;
            LSL_List result = new LSL_List();
            if (!UUID.TryParse(obj, out objID))
            {
                result.Add(new LSL_Vector());
                result.Add(new LSL_Vector());
                return result;
            }
            IScenePresence presence = World.GetScenePresence(objID);
            if (presence != null)
            {
                if (presence.ParentID == UUID.Zero) // not sat on an object
                {
                    LSL_Vector lower = new LSL_Vector();
                    LSL_Vector upper = new LSL_Vector();
                    if (presence.Animator.Animations.DefaultAnimation.AnimID 
                        == AnimationSet.Animations.AnimsUUID["SIT_GROUND_CONSTRAINED"])
                    {
                        // This is for ground sitting avatars
                        IAvatarAppearanceModule appearance = presence.RequestModuleInterface<IAvatarAppearanceModule> ();
                        if (appearance != null)
                        {
                            float height = appearance.Appearance.AvatarHeight / 2.66666667f;
                            lower = new LSL_Vector (-0.3375f, -0.45f, height * -1.0f);
                            upper = new LSL_Vector (0.3375f, 0.45f, 0.0f);
                        }
                    }
                    else
                    {
                        // This is for standing/flying avatars
                        IAvatarAppearanceModule appearance = presence.RequestModuleInterface<IAvatarAppearanceModule> ();
                        if (appearance != null)
                        {
                            float height = appearance.Appearance.AvatarHeight / 2.0f;
                            lower = new LSL_Vector (-0.225f, -0.3f, height * -1.0f);
                            upper = new LSL_Vector (0.225f, 0.3f, height + 0.05f);
                        }
                    }
                    result.Add(lower);
                    result.Add(upper);
                    return result;
                }
                else
                {
                    // sitting on an object so we need the bounding box of that
                    // which should include the avatar so set the UUID to the
                    // UUID of the object the avatar is sat on and allow it to fall through
                    // to processing an object
                    ISceneChildEntity p = World.GetSceneObjectPart (presence.ParentID);
                    objID = p.UUID;
                }
            }
            ISceneChildEntity part = World.GetSceneObjectPart (objID);
            // Currently only works for single prims without a sitting avatar
            if (part != null)
            {
                Vector3 halfSize = part.Scale * 0.5f;
                LSL_Vector lower = new LSL_Vector(halfSize.X * -1.0f, halfSize.Y * -1.0f, halfSize.Z * -1.0f);
                LSL_Vector upper = new LSL_Vector(halfSize.X, halfSize.Y, halfSize.Z);
                result.Add(lower);
                result.Add(upper);
                return result;
            }

            // Not found so return empty values
            result.Add(new LSL_Vector());
            result.Add(new LSL_Vector());
            return result;
        }

        public LSL_Vector llGetGeometricCenter()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            Vector3 MinPos = new Vector3(100000, 100000, 100000);
            Vector3 MaxPos = new Vector3(-100000, -100000, -100000);
            foreach (SceneObjectPart child in m_host.ParentEntity.ChildrenEntities())
            {
                Vector3 tmp = child.AbsolutePosition;
                if (tmp.X < MinPos.X)
                    MinPos.X = tmp.X;
                if (tmp.Y < MinPos.Y)
                    MinPos.Y = tmp.Y;
                if (tmp.Z < MinPos.Z)
                    MinPos.Z = tmp.Z;

                if (tmp.X > MaxPos.X)
                    MaxPos.X = tmp.X;
                if (tmp.Y > MaxPos.Y)
                    MaxPos.Y = tmp.Y;
                if (tmp.Z > MaxPos.Z)
                    MaxPos.Z = tmp.Z;
            }
            Vector3 GroupAvg = ((MaxPos + MinPos) / 2);
            return new LSL_Vector(GroupAvg.X, GroupAvg.Y, GroupAvg.Z);

            //Just plain wrong!
            //return new LSL_Vector(m_host.GetGeometricCenter().X, m_host.GetGeometricCenter().Y, m_host.GetGeometricCenter().Z);
        }

        public LSL_List llGetPrimitiveParams(LSL_List rules)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return GetLinkPrimitiveParams(m_host, rules);
        }

        public LSL_List llGetLinkPrimitiveParams(int linknumber, LSL_List rules)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");


            List<ISceneChildEntity> parts = GetLinkParts (linknumber);

            LSL_List res = new LSL_List();

            foreach (var part in parts)
            {
                LSL_List partRes = GetLinkPrimitiveParams(part, rules);
                res += partRes;
            }

            return res;
        }

        public LSL_List GetLinkPrimitiveParams (ISceneChildEntity part, LSL_List rules)
        {
            LSL_List res = new LSL_List();
            int idx = 0;
            while (idx < rules.Length)
            {
                int code = (int)rules.GetLSLIntegerItem(idx++);
                int remain = rules.Length - idx;
                Primitive.TextureEntry tex = part.Shape.Textures;
                int face = 0;
                if (idx < rules.Length)
                    face = (int)rules.GetLSLIntegerItem(idx++);

                if (code == (int)ScriptBaseClass.PRIM_NAME)
                {
                    res.Add(new LSL_Integer(part.Name));
                }

                if (code == (int)ScriptBaseClass.PRIM_DESC)
                {
                    res.Add(new LSL_Integer(part.Description));
                }

                if (code == (int)ScriptBaseClass.PRIM_MATERIAL)
                {
                    res.Add(new LSL_Integer(part.Material));
                }

                if (code == (int)ScriptBaseClass.PRIM_PHYSICS)
                {
                    if ((part.GetEffectiveObjectFlags() & (uint)PrimFlags.Physics) != 0)
                        res.Add(new LSL_Integer(1));
                    else
                        res.Add(new LSL_Integer(0));
                }

                if (code == (int)ScriptBaseClass.PRIM_TEMP_ON_REZ)
                {
                    if ((part.GetEffectiveObjectFlags() & (uint)PrimFlags.TemporaryOnRez) != 0)
                        res.Add(new LSL_Integer(1));
                    else
                        res.Add(new LSL_Integer(0));
                }

                if (code == (int)ScriptBaseClass.PRIM_PHANTOM)
                {
                    if ((part.GetEffectiveObjectFlags() & (uint)PrimFlags.Phantom) != 0)
                        res.Add(new LSL_Integer(1));
                    else
                        res.Add(new LSL_Integer(0));
                }

                if (code == (int)ScriptBaseClass.PRIM_POSITION)
                {
                    Vector3 tmp = part.AbsolutePosition;
                    LSL_Vector v = new LSL_Vector(tmp.X,
                                                  tmp.Y,
                                                  tmp.Z);
                    // For some reason, the part.AbsolutePosition.* values do not change if the
                    // linkset is rotated; they always reflect the child prim's world position
                    // as though the linkset is unrotated. This is incompatible behavior with SL's
                    // implementation, so will break scripts imported from there (not to mention it
                    // makes it more difficult to determine a child prim's actual inworld position).
                    if (part.ParentID != 0)
                        {
                        LSL_Rotation rtmp = llGetRootRotation();
                        LSL_Vector rpos = llGetRootPosition();
                        v = ((v - rpos) * rtmp) + rpos;
                        }
                    res.Add(v);
                }

                if (code == (int)ScriptBaseClass.PRIM_SIZE)
                {
                    Vector3 tmp = part.Scale;
                    res.Add(new LSL_Vector(tmp.X,
                                                  tmp.Y,
                                                  tmp.Z));
                }

                if (code == (int)ScriptBaseClass.PRIM_ROTATION)
                {
                    res.Add(GetPartRot(part));
                }

                if (code == (int)ScriptBaseClass.PRIM_TYPE)
                {
                    // implementing box
                    PrimitiveBaseShape Shape = part.Shape;
                    int primType = (int)part.GetPrimType();
                    res.Add(new LSL_Integer(primType));
                    double topshearx = (double)(sbyte)Shape.PathShearX / 100.0; // Fix negative values for PathShearX
                    double topsheary = (double)(sbyte)Shape.PathShearY / 100.0; // and PathShearY.
                    if (primType == ScriptBaseClass.PRIM_TYPE_BOX ||
                         ScriptBaseClass.PRIM_TYPE_CYLINDER ||
                         ScriptBaseClass.PRIM_TYPE_PRISM)
                    {
                        res.Add(new LSL_Integer(Shape.ProfileCurve));
                        res.Add(new LSL_Vector(Shape.ProfileBegin / 50000.0, 1 - Shape.ProfileEnd / 50000.0, 0));
                        res.Add(new LSL_Float(Shape.ProfileHollow / 50000.0));
                        res.Add(new LSL_Vector(Shape.PathTwistBegin / 100.0, Shape.PathTwist / 100.0, 0));
                        res.Add(new LSL_Vector(1 - (Shape.PathScaleX / 100.0 - 1), 1 - (Shape.PathScaleY / 100.0 - 1), 0));
                        res.Add(new LSL_Vector(topshearx, topsheary, 0));
                    }

                    if (primType == ScriptBaseClass.PRIM_TYPE_SPHERE)
                    {
                        res.Add(new LSL_Integer(Shape.ProfileCurve));
                        res.Add(new LSL_Vector(Shape.PathBegin / 50000.0, 1 - Shape.PathEnd / 50000.0, 0));
                        res.Add(new LSL_Float(Shape.ProfileHollow / 50000.0));
                        res.Add(new LSL_Vector(Shape.PathTwistBegin / 100.0, Shape.PathTwist / 100.0, 0));
                        res.Add(new LSL_Vector(Shape.ProfileBegin / 50000.0, 1 - Shape.ProfileEnd / 50000.0, 0));
                    }

                    if (primType == ScriptBaseClass.PRIM_TYPE_SCULPT)
                    {
                        res.Add(Shape.SculptTexture.ToString());
                        res.Add(new LSL_Integer(Shape.SculptType));
                    }
                    if (primType == ScriptBaseClass.PRIM_TYPE_RING ||
                     ScriptBaseClass.PRIM_TYPE_TUBE ||
                     ScriptBaseClass.PRIM_TYPE_TORUS)
                    {
                        // holeshape
                        res.Add(new LSL_Integer(Shape.ProfileCurve));

                        // cut
                        res.Add(new LSL_Vector(Shape.PathBegin / 50000.0, 1 - Shape.PathEnd / 50000.0, 0));

                        // hollow
                        res.Add(new LSL_Float(Shape.ProfileHollow / 50000.0));

                        // twist
                        res.Add(new LSL_Vector(Shape.PathTwistBegin / 100.0, Shape.PathTwist / 100.0, 0));

                        // vector holesize
                        res.Add(new LSL_Vector(1 - (Shape.PathScaleX / 100.0 - 1), 1 - (Shape.PathScaleY / 100.0 - 1), 0));

                        // vector topshear
                        res.Add(new LSL_Vector(topshearx, topsheary, 0));

                        // vector profilecut
                        res.Add(new LSL_Vector(Shape.ProfileBegin / 50000.0, 1 - Shape.ProfileEnd / 50000.0, 0));

                        // vector tapera
                        res.Add(new LSL_Vector(Shape.PathTaperX / 100.0, Shape.PathTaperY / 100.0, 0));

                        // float revolutions
                        res.Add(new LSL_Float((Shape.PathRevolutions * 0.015) + 1.0)); // Slightly inaccurate, because an unsigned
                        // byte is being used to represent the entire
                        // range of floating-point values from 1.0
                        // through 4.0 (which is how SL does it). 

                        // float radiusoffset
                        res.Add(new LSL_Float(Shape.PathRadiusOffset / 100.0));

                        // float skew
                        res.Add(new LSL_Float(Shape.PathSkew / 100.0));
                    }
                }

                if (code == (int)ScriptBaseClass.PRIM_TEXTURE)
                {
                    if (remain < 1)
                        return res;

                    if (face == ScriptBaseClass.ALL_SIDES)
                    {
                        for (face = 0; face < GetNumberOfSides(part); face++)
                        {
                            Primitive.TextureEntryFace texface = tex.GetFace((uint)face);

                            res.Add(new LSL_String(texface.TextureID.ToString()));
                            res.Add(new LSL_Vector(texface.RepeatU,
                                                   texface.RepeatV,
                                                   0));
                            res.Add(new LSL_Vector(texface.OffsetU,
                                                   texface.OffsetV,
                                                   0));
                            res.Add(new LSL_Float(texface.Rotation));
                        }
                    }
                    else
                    {
                        if (face >= 0 && face < GetNumberOfSides(part))
                        {
                            Primitive.TextureEntryFace texface = tex.GetFace((uint)face);

                            res.Add(new LSL_String(texface.TextureID.ToString()));
                            res.Add(new LSL_Vector(texface.RepeatU,
                                                   texface.RepeatV,
                                                   0));
                            res.Add(new LSL_Vector(texface.OffsetU,
                                                   texface.OffsetV,
                                                   0));
                            res.Add(new LSL_Float(texface.Rotation));
                        }
                    }
                }

                if (code == (int)ScriptBaseClass.PRIM_COLOR)
                {
                    if (remain < 1)
                        return res;

                    tex = part.Shape.Textures;
                    Color4 texcolor;
                    if (face == ScriptBaseClass.ALL_SIDES)
                    {
                        for (face = 0; face < GetNumberOfSides(part); face++)
                        {
                            texcolor = tex.GetFace((uint)face).RGBA;
                            res.Add(new LSL_Vector(texcolor.R,
                                                   texcolor.G,
                                                   texcolor.B));
                            res.Add(new LSL_Float(texcolor.A));
                        }
                    }
                    else
                    {
                        texcolor = tex.GetFace((uint)face).RGBA;
                        res.Add(new LSL_Vector(texcolor.R,
                                               texcolor.G,
                                               texcolor.B));
                        res.Add(new LSL_Float(texcolor.A));
                    }
                }

                if (code == (int)ScriptBaseClass.PRIM_BUMP_SHINY)
                {
                    if (remain < 1)
                        return res;

                    face = (int)rules.GetLSLIntegerItem(idx++);

                    if (face == ScriptBaseClass.ALL_SIDES)
                    {
                        for (face = 0; face < GetNumberOfSides(part); face++)
                        {
                            Primitive.TextureEntryFace texface = tex.GetFace((uint)face);
                            // Convert Shininess to PRIM_SHINY_*
                            res.Add(new LSL_Integer((uint)texface.Shiny >> 6));
                            // PRIM_BUMP_*
                            res.Add(new LSL_Integer((int)texface.Bump));
                        }
                    }
                    else
                    {
                        if (face >= 0 && face < GetNumberOfSides(part))
                        {
                            Primitive.TextureEntryFace texface = tex.GetFace((uint)face);
                            // Convert Shininess to PRIM_SHINY_*
                            res.Add(new LSL_Integer((uint)texface.Shiny >> 6));
                            // PRIM_BUMP_*
                            res.Add(new LSL_Integer((int)texface.Bump));
                        }
                    }
                }

                if (code == (int)ScriptBaseClass.PRIM_FULLBRIGHT)
                {
                    if (remain < 1)
                        return res;

                    face = (int)rules.GetLSLIntegerItem(idx++);
                    tex = part.Shape.Textures;
                    if (face == ScriptBaseClass.ALL_SIDES)
                    {
                        for (face = 0; face < GetNumberOfSides(part); face++)
                        {
                            Primitive.TextureEntryFace texface = tex.GetFace((uint)face);
                            res.Add(new LSL_Integer(texface.Fullbright ? 1 : 0));
                        }
                    }
                    else
                    {
                        if (face >= 0 && face < GetNumberOfSides(part))
                        {
                            Primitive.TextureEntryFace texface = tex.GetFace((uint)face);
                            res.Add(new LSL_Integer(texface.Fullbright ? 1 : 0));
                        }
                    }
                }

                if (code == (int)ScriptBaseClass.PRIM_FLEXIBLE)
                {
                    PrimitiveBaseShape shape = part.Shape;

                    if (shape.FlexiEntry)
                        res.Add(new LSL_Integer(1));              // active
                    else
                        res.Add(new LSL_Integer(0));
                    res.Add(new LSL_Integer(shape.FlexiSoftness));// softness
                    res.Add(new LSL_Float(shape.FlexiGravity));   // gravity
                    res.Add(new LSL_Float(shape.FlexiDrag));      // friction
                    res.Add(new LSL_Float(shape.FlexiWind));      // wind
                    res.Add(new LSL_Float(shape.FlexiTension));   // tension
                    res.Add(new LSL_Vector(shape.FlexiForceX,       // force
                                           shape.FlexiForceY,
                                           shape.FlexiForceZ));
                }

                if (code == (int)ScriptBaseClass.PRIM_TEXGEN)
                {
                    if (remain < 1)
                        return res;

                    face = (int)rules.GetLSLIntegerItem(idx++);
                    if (face == ScriptBaseClass.ALL_SIDES)
                    {
                        for (face = 0; face < GetNumberOfSides(part); face++)
                        {
                            MappingType texgen = tex.GetFace((uint)face).TexMapType;
                            // Convert MappingType to PRIM_TEXGEN_DEFAULT, PRIM_TEXGEN_PLANAR etc.
                            res.Add(new LSL_Integer((uint)texgen >> 1));
                        }
                    }
                    else
                    {
                        if (face >= 0 && face < GetNumberOfSides(part))
                        {
                            MappingType texgen = tex.GetFace((uint)face).TexMapType;
                            res.Add(new LSL_Integer((uint)texgen >> 1));
                        }
                    }
                }

                if (code == (int)ScriptBaseClass.PRIM_POINT_LIGHT)
                {
                    PrimitiveBaseShape shape = part.Shape;

                    if (shape.LightEntry)
                        res.Add(new LSL_Integer(1));              // active
                    else
                        res.Add(new LSL_Integer(0));
                    res.Add(new LSL_Vector(shape.LightColorR,       // color
                                           shape.LightColorG,
                                           shape.LightColorB));
                    res.Add(new LSL_Float(shape.LightIntensity)); // intensity
                    res.Add(new LSL_Float(shape.LightRadius));    // radius
                    res.Add(new LSL_Float(shape.LightFalloff));   // falloff
                }

                if (code == (int)ScriptBaseClass.PRIM_GLOW)
                {
                    if (remain < 1)
                        return res;

                    face = (int)rules.GetLSLIntegerItem(idx++);
                    if (face == ScriptBaseClass.ALL_SIDES)
                    {
                        for (face = 0; face < GetNumberOfSides(part); face++)
                        {
                            Primitive.TextureEntryFace texface = tex.GetFace((uint)face);
                            res.Add(new LSL_Float(texface.Glow));
                        }
                    }
                    else
                    {
                        if (face >= 0 && face < GetNumberOfSides(part))
                        {
                            Primitive.TextureEntryFace texface = tex.GetFace((uint)face);
                            res.Add(new LSL_Float(texface.Glow));
                        }
                    }
                }

                if (code == (int)ScriptBaseClass.PRIM_TEXT)
                {
                    Color4 textColor = part.GetTextColor();
                    res.Add(part.Text);
                    res.Add(new LSL_Vector(textColor.R,
                                           textColor.G,
                                           textColor.B));
                    res.Add(new LSL_Float(textColor.A));
                }
                if (code == (int)ScriptBaseClass.PRIM_ROT_LOCAL)
                {
                    Quaternion rtmp = part.RotationOffset;
                    res.Add(new LSL_Rotation(rtmp.X, rtmp.Y, rtmp.Z, rtmp.W));
                }
            }
            return res;
        }

        //  <remarks>
        //  <para>
        //  The .NET definition of base 64 is:
        //  <list>
        //  <item>
        //  Significant: A-Z a-z 0-9 + -
        //  </item>
        //  <item>
        //  Whitespace: \t \n \r ' '
        //  </item>
        //  <item>
        //  Valueless: =
        //  </item>
        //  <item>
        //  End-of-string: \0 or '=='
        //  </item>
        //  </list>
        //  </para>
        //  <para>
        //  Each point in a base-64 string represents
        //  a 6 bit value. A 32-bit integer can be
        //  represented using 6 characters (with some
        //  redundancy).
        //  </para>
        //  <para>
        //  LSL requires a base64 string to be 8
        //  characters in length. LSL also uses '/'
        //  rather than '-' (MIME compliant).
        //  </para>
        //  <para>
        //  RFC 1341 used as a reference (as specified
        //  by the SecondLife Wiki).
        //  </para>
        //  <para>
        //  SL do not record any kind of exception for
        //  these functions, so the string to integer
        //  conversion returns '0' if an invalid
        //  character is encountered during conversion.
        //  </para>
        //  <para>
        //  References
        //  <list>
        //  <item>
        //  http://lslwiki.net/lslwiki/wakka.php?wakka=Base64
        //  </item>
        //  <item>
        //  </item>
        //  </list>
        //  </para>
        //  </remarks>

        //  <summary>
        //  Table for converting 6-bit integers into
        //  base-64 characters
        //  </summary>

        protected static readonly char[] i2ctable =
        {
            'A','B','C','D','E','F','G','H',
            'I','J','K','L','M','N','O','P',
            'Q','R','S','T','U','V','W','X',
            'Y','Z',
            'a','b','c','d','e','f','g','h',
            'i','j','k','l','m','n','o','p',
            'q','r','s','t','u','v','w','x',
            'y','z',
            '0','1','2','3','4','5','6','7',
            '8','9',
            '+','/'
        };

        //  <summary>
        //  Table for converting base-64 characters
        //  into 6-bit integers.
        //  </summary>

        protected static readonly int[] c2itable =
        {
            -1,-1,-1,-1,-1,-1,-1,-1,    // 0x
            -1,-1,-1,-1,-1,-1,-1,-1,
            -1,-1,-1,-1,-1,-1,-1,-1,    // 1x
            -1,-1,-1,-1,-1,-1,-1,-1,
            -1,-1,-1,-1,-1,-1,-1,-1,    // 2x
            -1,-1,-1,63,-1,-1,-1,64,
            53,54,55,56,57,58,59,60,    // 3x
            61,62,-1,-1,-1,0,-1,-1,
            -1,1,2,3,4,5,6,7,           // 4x
            8,9,10,11,12,13,14,15,
            16,17,18,19,20,21,22,23,    // 5x
            24,25,26,-1,-1,-1,-1,-1,
            -1,27,28,29,30,31,32,33,    // 6x
            34,35,36,37,38,39,40,41,
            42,43,44,45,46,47,48,49,    // 7x
            50,51,52,-1,-1,-1,-1,-1,
            -1,-1,-1,-1,-1,-1,-1,-1,    // 8x
            -1,-1,-1,-1,-1,-1,-1,-1,
            -1,-1,-1,-1,-1,-1,-1,-1,    // 9x
            -1,-1,-1,-1,-1,-1,-1,-1,
            -1,-1,-1,-1,-1,-1,-1,-1,    // Ax
            -1,-1,-1,-1,-1,-1,-1,-1,
            -1,-1,-1,-1,-1,-1,-1,-1,    // Bx
            -1,-1,-1,-1,-1,-1,-1,-1,
            -1,-1,-1,-1,-1,-1,-1,-1,    // Cx
            -1,-1,-1,-1,-1,-1,-1,-1,
            -1,-1,-1,-1,-1,-1,-1,-1,    // Dx
            -1,-1,-1,-1,-1,-1,-1,-1,
            -1,-1,-1,-1,-1,-1,-1,-1,    // Ex
            -1,-1,-1,-1,-1,-1,-1,-1,
            -1,-1,-1,-1,-1,-1,-1,-1,    // Fx
            -1,-1,-1,-1,-1,-1,-1,-1
        };

        //  <summary>
        //  Converts a 32-bit integer into a Base64
        //  character string. Base64 character strings
        //  are always 8 characters long. All iinteger
        //  values are acceptable.
        //  </summary>
        //  <param name="number">
        //  32-bit integer to be converted.
        //  </param>
        //  <returns>
        //  8 character string. The 1st six characters
        //  contain the encoded number, the last two
        //  characters are padded with "=".
        //  </returns>

        public LSL_String llIntegerToBase64(int number)
        {
            // uninitialized string

            char[] imdt = new char[8];

            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            
            // Manually unroll the loop

            imdt[7] = '=';
            imdt[6] = '=';
            imdt[5] = i2ctable[number<<4  & 0x3F];
            imdt[4] = i2ctable[number>>2  & 0x3F];
            imdt[3] = i2ctable[number>>8  & 0x3F];
            imdt[2] = i2ctable[number>>14 & 0x3F];
            imdt[1] = i2ctable[number>>20 & 0x3F];
            imdt[0] = i2ctable[number>>26 & 0x3F];

            return new string(imdt);
        }

        //  <summary>
        //  Converts an eight character base-64 string
        //  into a 32-bit integer.
        //  </summary>
        //  <param name="str">
        //  8 characters string to be converted. Other
        //  length strings return zero.
        //  </param>
        //  <returns>
        //  Returns an integer representing the
        //  encoded value providedint he 1st 6
        //  characters of the string.
        //  </returns>
        //  <remarks>
        //  This is coded to behave like LSL's
        //  implementation (I think), based upon the
        //  information available at the Wiki.
        //  If more than 8 characters are supplied,
        //  zero is returned.
        //  If a NULL string is supplied, zero will
        //  be returned.
        //  If fewer than 6 characters are supplied, then
        //  the answer will reflect a partial
        //  accumulation.
        //  <para>
        //  The 6-bit segments are
        //  extracted left-to-right in big-endian mode,
        //  which means that segment 6 only contains the
        //  two low-order bits of the 32 bit integer as
        //  its high order 2 bits. A short string therefore
        //  means loss of low-order information. E.g.
        //
        //  |<---------------------- 32-bit integer ----------------------->|<-Pad->|
        //  |<--Byte 0----->|<--Byte 1----->|<--Byte 2----->|<--Byte 3----->|<-Pad->|
        //  |3|3|2|2|2|2|2|2|2|2|2|2|1|1|1|1|1|1|1|1|1|1| | | | | | | | | | |P|P|P|P|
        //  |1|0|9|8|7|6|5|4|3|2|1|0|9|8|7|6|5|4|3|2|1|0|9|8|7|6|5|4|3|2|1|0|P|P|P|P|
        //  |  str[0]   |  str[1]   |  str[2]   |  str[3]   |  str[4]   |  str[6]   |
        //
        //  </para>
        //  </remarks>

        public LSL_Integer llBase64ToInteger(string str)
        {
            int number = 0;
            int digit;

            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            
            //    Require a well-fromed base64 string

            if (str.Length > 8)
                return 0;

            //    The loop is unrolled in the interests
            //    of performance and simple necessity.
            //
            //    MUST find 6 digits to be well formed
            //      -1 == invalid
            //       0 == padding

            if ((digit = c2itable[str[0]]) <= 0)
            {
                return digit < 0 ? (int)0 : number;
            }
            number += --digit<<26;

            if ((digit = c2itable[str[1]]) <= 0)
            {
                return digit < 0 ? (int)0 : number;
            }
            number += --digit<<20;

            if ((digit = c2itable[str[2]]) <= 0)
            {
                return digit < 0 ? (int)0 : number;
            }
            number += --digit<<14;

            if ((digit = c2itable[str[3]]) <= 0)
            {
                return digit < 0 ? (int)0 : number;
            }
            number += --digit<<8;

            if ((digit = c2itable[str[4]]) <= 0)
            {
                return digit < 0 ? (int)0 : number;
            }
            number += --digit<<2;

            if ((digit = c2itable[str[5]]) <= 0)
            {
                return digit < 0 ? (int)0 : number;
            }
            number += --digit>>4;

            // ignore trailing padding

            return number;
        }

        public LSL_Float llGetGMTclock()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return DateTime.UtcNow.TimeOfDay.TotalSeconds;
        }

        public LSL_String llGetHTTPHeader(LSL_Key request_id, string header)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            
           if (m_UrlModule != null)
               return m_UrlModule.GetHttpHeader(new UUID(request_id), header);
           return String.Empty;
        }


        public LSL_String llGetSimulatorHostname()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return System.Environment.MachineName;
        }

        //  <summary>
        //  Scan the string supplied in 'src' and
        //  tokenize it based upon two sets of
        //  tokenizers provided in two lists,
        //  separators and spacers.
        //  </summary>
        //
        //  <remarks>
        //  Separators demarcate tokens and are
        //  elided as they are encountered. Spacers
        //  also demarcate tokens, but are themselves
        //  retained as tokens.
        //
        //  Both separators and spacers may be arbitrarily
        //  long strings. i.e. ":::".
        //
        //  The function returns an ordered list
        //  representing the tokens found in the supplied
        //  sources string. If two successive tokenizers
        //  are encountered, then a NULL entry is added
        //  to the list.
        //
        //  It is a precondition that the source and
        //  toekizer lisst are non-null. If they are null,
        //  then a null pointer exception will be thrown
        //  while their lengths are being determined.
        //
        //  A small amount of working memoryis required
        //  of approximately 8*#tokenizers.
        //
        //  There are many ways in which this function
        //  can be implemented, this implementation is
        //  fairly naive and assumes that when the
        //  function is invooked with a short source
        //  string and/or short lists of tokenizers, then
        //  performance will not be an issue.
        //
        //  In order to minimize the perofrmance
        //  effects of long strings, or large numbers
        //  of tokeizers, the function skips as far as
        //  possible whenever a toekenizer is found,
        //  and eliminates redundant tokenizers as soon
        //  as is possible.
        //
        //  The implementation tries to avoid any copying
        //  of arrays or other objects.
        //  </remarks>

        private LSL_List ParseString(string src, LSL_List separators, LSL_List spacers, bool keepNulls)
        {
            int beginning = 0;
            int srclen = src.Length;
            int seplen = separators.Length;
            object[] separray = separators.Data;
            int spclen = spacers.Length;
            object[] spcarray = spacers.Data;
            int mlen = seplen + spclen;

            int[] offset = new int[mlen + 1];
            bool[] active = new bool[mlen];

            int best;
            int j;

            //    Initial capacity reduces resize cost

            LSL_List tokens = new LSL_List();

            //    All entries are initially valid

            for (int i = 0; i < mlen; i++)
                active[i] = true;

            offset[mlen] = srclen;

            while (beginning < srclen)
            {

                best = mlen;    // as bad as it gets

                //    Scan for separators

                for (j = 0; j < seplen; j++)
                {
                    if (separray[j].ToString() == String.Empty)
                        active[j] = false;

                    if (active[j])
                    {
                        // scan all of the markers
                        if ((offset[j] = src.IndexOf(separray[j].ToString(), beginning)) == -1)
                        {
                            // not present at all
                            active[j] = false;
                        }
                        else
                        {
                            // present and correct
                            if (offset[j] < offset[best])
                            {
                                // closest so far
                                best = j;
                                if (offset[best] == beginning)
                                    break;
                            }
                        }
                    }
                }

                //    Scan for spacers

                if (offset[best] != beginning)
                {
                    for (j = seplen; (j < mlen) && (offset[best] > beginning); j++)
                    {
                        if (spcarray[j - seplen].ToString() == String.Empty)
                            active[j] = false;

                        if (active[j])
                        {
                            // scan all of the markers
                            if ((offset[j] = src.IndexOf(spcarray[j - seplen].ToString(), beginning)) == -1)
                            {
                                // not present at all
                                active[j] = false;
                            }
                            else
                            {
                                // present and correct
                                if (offset[j] < offset[best])
                                {
                                    // closest so far
                                    best = j;
                                }
                            }
                        }
                    }
                }

                //    This is the normal exit from the scanning loop

                if (best == mlen)
                {
                    // no markers were found on this pass
                    // so we're pretty much done
                    if ((keepNulls) || ((!keepNulls) && (srclen - beginning) > 0))
                        tokens.Add(new LSL_String(src.Substring(beginning, srclen - beginning)));
                    break;
                }

                //    Otherwise we just add the newly delimited token
                //    and recalculate where the search should continue.
                if ((keepNulls) || ((!keepNulls) && (offset[best] - beginning) > 0))
                    tokens.Add(new LSL_String(src.Substring(beginning, offset[best] - beginning)));

                if (best < seplen)
                {
                    beginning = offset[best] + (separray[best].ToString()).Length;
                }
                else
                {
                    beginning = offset[best] + (spcarray[best - seplen].ToString()).Length;
                    string str = spcarray[best - seplen].ToString();
                    if ((keepNulls) || ((!keepNulls) && (str.Length > 0)))
                        tokens.Add(new LSL_String(str));
                }
            }

            //    This an awkward an not very intuitive boundary case. If the
            //    last substring is a tokenizer, then there is an implied trailing
            //    null list entry. Hopefully the single comparison will not be too
            //    arduous. Alternatively the 'break' could be replced with a return
            //    but that's shabby programming.

            if ((beginning == srclen) && (keepNulls))
            {
                if (srclen != 0)
                    tokens.Add(new LSL_String(""));
            }

            return tokens;
        }

        public LSL_List llParseString2List(string src, LSL_List separators, LSL_List spacers)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "llParseString2List", m_host, "LSL");
            return this.ParseString(src, separators, spacers, false);
        }

        public LSL_List llParseStringKeepNulls(string src, LSL_List separators, LSL_List spacers)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "llParseStringKeepNulls", m_host, "LSL");
            return this.ParseString(src, separators, spacers, true);
        }

        public LSL_Integer llGetObjectPermMask(int mask)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            
            int permmask = 0;

            if (mask == ScriptBaseClass.MASK_BASE)//0
            {
                permmask = (int)m_host.BaseMask;
            }

            else if (mask == ScriptBaseClass.MASK_OWNER)//1
            {
                permmask = (int)m_host.OwnerMask;
            }

            else if (mask == ScriptBaseClass.MASK_GROUP)//2
            {
                permmask = (int)m_host.GroupMask;
            }

            else if (mask == ScriptBaseClass.MASK_EVERYONE)//3
            {
                permmask = (int)m_host.EveryoneMask;
            }

            else if (mask == ScriptBaseClass.MASK_NEXT)//4
            {
                permmask = (int)m_host.NextOwnerMask;
            }

            return permmask;
        }

        public void llSetObjectPermMask(int mask, int value)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            
            if (m_ScriptEngine.Config.GetBoolean("AllowGodFunctions", false))
            {
                if (World.Permissions.CanRunConsoleCommand(m_host.OwnerID))
                {
                    if (mask == ScriptBaseClass.MASK_BASE)//0
                    {
                        m_host.BaseMask = (uint)value;
                    }

                    else if (mask == ScriptBaseClass.MASK_OWNER)//1
                    {
                        m_host.OwnerMask = (uint)value;
                    }

                    else if (mask == ScriptBaseClass.MASK_GROUP)//2
                    {
                        m_host.GroupMask = (uint)value;
                    }

                    else if (mask == ScriptBaseClass.MASK_EVERYONE)//3
                    {
                        m_host.EveryoneMask = (uint)value;
                    }

                    else if (mask == ScriptBaseClass.MASK_NEXT)//4
                    {
                        m_host.NextOwnerMask = (uint)value;
                    }
                }
            }
        }

        public LSL_Integer llGetInventoryPermMask(string item, int mask)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            
            lock (m_host.TaskInventory)
            {
                foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory)
                {
                    if (inv.Value.Name == item)
                    {
                        switch (mask)
                        {
                            case 0:
                                return (int)inv.Value.BasePermissions;
                            case 1:
                                return (int)inv.Value.CurrentPermissions;
                            case 2:
                                return (int)inv.Value.GroupPermissions;
                            case 3:
                                return (int)inv.Value.EveryonePermissions;
                            case 4:
                                return (int)inv.Value.NextPermissions;
                        }
                    }
                }
            }

            return -1;
        }

        public void llSetInventoryPermMask(string item, int mask, int value)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            if (m_ScriptEngine.Config.GetBoolean("AllowGodFunctions", false))
            {
                if (World.Permissions.CanRunConsoleCommand(m_host.OwnerID))
                {
                    lock (m_host.TaskInventory)
                    {
                        foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory)
                        {
                            if (inv.Value.Name == item)
                            {
                                switch (mask)
                                {
                                    case 0:
                                        inv.Value.BasePermissions = (uint)value;
                                        break;
                                    case 1:
                                        inv.Value.CurrentPermissions = (uint)value;
                                        break;
                                    case 2:
                                        inv.Value.GroupPermissions = (uint)value;
                                        break;
                                    case 3:
                                        inv.Value.EveryonePermissions = (uint)value;
                                        break;
                                    case 4:
                                        inv.Value.NextPermissions = (uint)value;
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }

        public LSL_String llGetInventoryCreator(string item)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            
            lock (m_host.TaskInventory)
            {
                foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory)
                {
                    if (inv.Value.Name == item)
                    {
                        return inv.Value.CreatorID.ToString();
                    }
                }
            }

            llSay(0, "No item name '" + item + "'");

            return String.Empty;
        }

        public void llOwnerSay(string msg)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            IChatModule chatModule = World.RequestModuleInterface<IChatModule>();
            if (chatModule != null)
                chatModule.SimChatBroadcast(msg, ChatTypeEnum.Owner, 0,
                                   m_host.AbsolutePosition, m_host.Name, m_host.UUID, false, UUID.Zero, World);
        }

        public LSL_String llRequestSecureURL()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            if (m_UrlModule != null)
                return m_UrlModule.RequestSecureURL(m_ScriptEngine.ScriptModule, m_host, m_itemID).ToString();
            return UUID.Zero.ToString();
        }

        public LSL_String llGetEnv(LSL_String name)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            if (name == "sim_channel")
                return "Aurora-Sim Server";
            else if (name == "sim_version")
                return World.RequestModuleInterface<ISimulationBase>().Version;
            return "";
        }

        public LSL_Key llRequestSimulatorData(string simulator, int data)
        {
            UUID tid = UUID.Zero;

            try
            {
                ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

                string reply = String.Empty;

                GridRegion info;

                if (World.RegionInfo.RegionName == simulator)
                    info = new GridRegion(World.RegionInfo);
                else
                    info = World.GridService.GetRegionByName(World.RegionInfo.ScopeID, simulator);

                switch (data)
                {
                    case 5: // DATA_SIM_POS
                        if (info == null)
                        {
                            break;
                        }
                        reply = new LSL_Vector(
                            info.RegionLocX,
                            info.RegionLocY,
                            0).ToString();
                        break;
                    case 6: // DATA_SIM_STATUS
                        if (info != null)
                            reply = "up"; // Duh!
                        else
                            reply = "unknown";
                        break;
                    case 7: // DATA_SIM_RATING
                        if (info == null)
                        {
                            break;
                        }
                        uint access = Util.ConvertAccessLevelToMaturity(info.Access);
                        if (access == 0)
                            reply = "PG";
                        else if (access == 1)
                            reply = "MATURE";
                        else if (access == 2)
                            reply = "ADULT";
                        else
                            reply = "UNKNOWN";
                        break;
                    case 128:
                        ScriptProtection.CheckThreatLevel(ThreatLevel.High, "llRequestSimulatorData", m_host, "LSL");
                        reply = "OpenSim";
                        break;
                    default:
                        break;
                }
                UUID rq = UUID.Random();

                DataserverPlugin dataserverPlugin = (DataserverPlugin)m_ScriptEngine.GetScriptPlugin("Dataserver");

                tid = dataserverPlugin.RegisterRequest(m_host.UUID, m_itemID, rq.ToString());

                dataserverPlugin.AddReply(rq.ToString(), reply, 1000);
            }
            catch (Exception)
            {
                //m_log.Error("[LSL_API]: llRequestSimulatorData" + e.ToString());
            }

            ScriptSleep(1000);
            return (LSL_Key)tid.ToString();
        }

        public LSL_String llRequestURL()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            
            if (m_UrlModule != null)
                return m_UrlModule.RequestURL(m_ScriptEngine.ScriptModule, m_host, m_itemID).ToString();
            return UUID.Zero.ToString();
        }

        public void llForceMouselook(int mouselook)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            m_host.ForceMouselook = (mouselook != 0);
        }

        /// <summary>
        /// illListReplaceList removes the sub-list defined by the inclusive indices
        /// start and end and inserts the src list in its place. The inclusive
        /// nature of the indices means that at least one element must be deleted
        /// if the indices are within the bounds of the existing list. I.e. 2,2
        /// will remove the element at index 2 and replace it with the source
        /// list. Both indices may be negative, with the usual interpretation. An
        /// interesting case is where end is lower than start. As these indices
        /// bound the list to be removed, then 0->end, and start->lim are removed
        /// and the source list is added as a suffix.
        /// </summary>

        public LSL_List llListReplaceList(LSL_List dest, LSL_List src, int start, int end)
        {
            LSL_List pref = null;

            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            
            // Note that although we have normalized, both
            // indices could still be negative.
            if (start < 0)
            {
                start = start+dest.Length;
            }

            if (end < 0)
            {
                end = end+dest.Length;
            }
            // The comventional case, remove a sequence starting with
            // start and ending with end. And then insert the source
            // list.
            if (start <= end)
            {
                // If greater than zero, then there is going to be a
                // surviving prefix. Otherwise the inclusive nature
                // of the indices mean that we're going to add the
                // source list as a prefix.
                if (start > 0)
                {
                    pref = dest.GetSublist(0,start-1);
                    // Only add a suffix if there is something
                    // beyond the end index (it's inclusive too).
                    if (end + 1 < dest.Length)
                    {
                        return pref + src + dest.GetSublist(end + 1, -1);
                    }
                    else
                    {
                        return pref + src;
                    }
                }
                // If start is less than or equal to zero, then
                // the new list is simply a prefix. We still need to
                // figure out any necessary surgery to the destination
                // based upon end. Note that if end exceeds the upper
                // bound in this case, the entire destination list
                // is removed.
                else
                {
                    if (end + 1 < dest.Length)
                    {
                        return src + dest.GetSublist(end + 1, -1);
                    }
                    else
                    {
                        return src;
                    }
                }
            }
            // Finally, if start > end, we strip away a prefix and
            // a suffix, to leave the list that sits <between> ens
            // and start, and then tag on the src list. AT least
            // that's my interpretation. We can get sublist to do
            // this for us. Note that one, or both of the indices
            // might have been negative.
            else
            {
                return dest.GetSublist(end + 1, start - 1) + src;
            }
        }

        public void llLoadURL(string avatar_id, string message, string url)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            
            IDialogModule dm = World.RequestModuleInterface<IDialogModule>();
            if (null != dm)
                dm.SendUrlToUser(
                    new UUID(avatar_id), m_host.Name, m_host.UUID, m_host.OwnerID, false, message, url);

            ScriptSleep(10000);
        }

        public void llParcelMediaCommandList(LSL_List commandList)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            
            // according to the docs, this command only works if script owner and land owner are the same
            // lets add estate owners and gods, too, and use the generic permission check.
            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                ILandObject landObject = parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y);
                if(landObject == null)
                    return;
                if (!World.Permissions.CanEditParcel(m_host.OwnerID, landObject)) 
                    return;

                bool update = false; // send a ParcelMediaUpdate (and possibly change the land's media URL)?
                byte loop = 0;

                LandData landData = landObject.LandData;
                string url = landData.MediaURL;
                string texture = landData.MediaID.ToString();
                bool autoAlign = landData.MediaAutoScale != 0;
                string mediaType = landData.MediaType;
                string description = landData.MediaDescription;
                int width = landData.MediaWidth;
                int height = landData.MediaHeight;
                float mediaLoopSet = landData.MediaLoopSet;

                ParcelMediaCommandEnum? commandToSend = null;
                float time = 0.0f; // default is from start

                IScenePresence presence = null;

                for (int i = 0; i < commandList.Data.Length; i++)
                {
                    int tmp = ((LSL_Integer) commandList.Data[i]).value;
                    ParcelMediaCommandEnum command = (ParcelMediaCommandEnum)tmp;
                    switch (command)
                    {
                        case ParcelMediaCommandEnum.Agent:
                            // we send only to one agent
                            if ((i + 1) < commandList.Length)
                            {
                                if (commandList.Data[i + 1] is LSL_String)
                                {
                                    UUID agentID;
                                    if (UUID.TryParse((LSL_String)commandList.Data[i + 1], out agentID))
                                    {
                                        presence = World.GetScenePresence(agentID);
                                    }
                                }
                                else ShoutError("The argument of PARCEL_MEDIA_COMMAND_AGENT must be a key");
                                ++i;
                            }
                            break;

                        case ParcelMediaCommandEnum.Loop:
                            loop = 1;
                            commandToSend = command;
                            update = true; //need to send the media update packet to set looping
                            break;

                        case ParcelMediaCommandEnum.LoopSet:
                            if ((i + 1) < commandList.Length)
                            {
                                if (commandList.Data[i + 1] is LSL_Float)
                                {
                                    mediaLoopSet = (float)((LSL_Float)commandList.Data[i + 1]).value;
                                }
                                else ShoutError("The argument of PARCEL_MEDIA_COMMAND_LOOP_SET must be a float");
                                ++i;
                            }
                            commandToSend = command;
                            break;

                        case ParcelMediaCommandEnum.Play:
                            loop = 0;
                            commandToSend = command;
                            update = true; //need to send the media update packet to make sure it doesn't loop
                            break;

                        case ParcelMediaCommandEnum.Pause:
                        case ParcelMediaCommandEnum.Stop:
                        case ParcelMediaCommandEnum.Unload:
                            commandToSend = command;
                            break;

                        case ParcelMediaCommandEnum.Url:
                            if ((i + 1) < commandList.Length)
                            {
                                if (commandList.Data[i + 1] is LSL_String)
                                {
                                    url = (LSL_String)commandList.Data[i + 1];
                                    update = true;
                                }
                                else ShoutError("The argument of PARCEL_MEDIA_COMMAND_URL must be a string.");
                                ++i;
                            }
                            break;

                        case ParcelMediaCommandEnum.Texture:
                            if ((i + 1) < commandList.Length)
                            {
                                if (commandList.Data[i + 1] is LSL_String)
                                {
                                    texture = (LSL_String)commandList.Data[i + 1];
                                    update = true;
                                }
                                else ShoutError("The argument of PARCEL_MEDIA_COMMAND_TEXTURE must be a string or key.");
                                ++i;
                            }
                            break;

                        case ParcelMediaCommandEnum.Time:
                            if ((i + 1) < commandList.Length)
                            {
                                if (commandList.Data[i + 1] is LSL_Float)
                                {
                                    time = (float)(LSL_Float)commandList.Data[i + 1];
                                }
                                else ShoutError("The argument of PARCEL_MEDIA_COMMAND_TIME must be a float.");
                                ++i;
                            }
                            commandToSend = command;
                            break;

                        case ParcelMediaCommandEnum.AutoAlign:
                            if ((i + 1) < commandList.Length)
                            {
                                if (commandList.Data[i + 1] is LSL_Integer)
                                {
                                    autoAlign = (LSL_Integer)commandList.Data[i + 1];
                                    update = true;
                                }

                                else ShoutError("The argument of PARCEL_MEDIA_COMMAND_AUTO_ALIGN must be an integer.");
                                ++i;
                            }
                            break;

                        case ParcelMediaCommandEnum.Type:
                            if ((i + 1) < commandList.Length)
                            {
                                if (commandList.Data[i + 1] is LSL_String)
                                {
                                    mediaType = (LSL_String)commandList.Data[i + 1];
                                    update = true;
                                }
                                else ShoutError("The argument of PARCEL_MEDIA_COMMAND_TYPE must be a string.");
                                ++i;
                            }
                            break;

                        case ParcelMediaCommandEnum.Desc:
                            if ((i + 1) < commandList.Length)
                            {
                                if (commandList.Data[i + 1] is LSL_String)
                                {
                                    description = (LSL_String)commandList.Data[i + 1];
                                    update = true;
                                }
                                else ShoutError("The argument of PARCEL_MEDIA_COMMAND_DESC must be a string.");
                                ++i;
                            }
                            break;
                        case ParcelMediaCommandEnum.Size:
                            if ((i + 2) < commandList.Length)
                            {
                                if (commandList.Data[i + 1] is LSL_Integer)
                                {
                                    if (commandList.Data[i + 2] is LSL_Integer)
                                    {
                                        width = (LSL_Integer)commandList.Data[i + 1];
                                        height = (LSL_Integer)commandList.Data[i + 2];
                                        update = true;
                                    }
                                    else ShoutError("The second argument of PARCEL_MEDIA_COMMAND_SIZE must be an integer.");
                                }
                                else ShoutError("The first argument of PARCEL_MEDIA_COMMAND_SIZE must be an integer.");
                                i += 2;
                            }
                            break;

                        default:
                            NotImplemented("llParcelMediaCommandList parameter not supported yet: " + Enum.Parse(typeof(ParcelMediaCommandEnum), commandList.Data[i].ToString()).ToString());
                            break;
                    }//end switch
                }//end for

                // if we didn't get a presence, we send to all and change the url
                // if we did get a presence, we only send to the agent specified, and *don't change the land settings*!

                // did something important change or do we only start/stop/pause?
                if (update)
                {
                    if (presence == null)
                    {
                        // we send to all
                        landData.MediaID = new UUID(texture);
                        landData.MediaAutoScale = autoAlign ? (byte)1 : (byte)0;
                        landData.MediaWidth = width;
                        landData.MediaHeight = height;
                        landData.MediaType = mediaType;
                        landData.MediaDescription = description;
                        landData.MediaLoop = loop == 1;
                        landData.MediaLoopSet = mediaLoopSet;

                        // do that one last, it will cause a ParcelPropertiesUpdate
                        landObject.SetMediaUrl(url);

                        // now send to all (non-child) agents
                        World.ForEachScenePresence(delegate(IScenePresence sp)
                        {
                            if (!sp.IsChildAgent && (sp.CurrentParcelUUID == landData.GlobalID))
                            {
                                sp.ControllingClient.SendParcelMediaUpdate(landData.MediaURL,
                                                                              landData.MediaID,
                                                                              landData.MediaAutoScale,
                                                                              mediaType,
                                                                              description,
                                                                              width, height,
                                                                              loop);
                            }
                        });
                    }
                    else if (!presence.IsChildAgent)
                    {
                        // we only send to one (root) agent
                        presence.ControllingClient.SendParcelMediaUpdate(url,
                                                                         new UUID(texture),
                                                                         autoAlign ? (byte)1 : (byte)0,
                                                                         mediaType,
                                                                         description,
                                                                         width, height,
                                                                         loop);
                    }
                }

                if (commandToSend != null)
                {
                    float ParamToSend = time;
                    if ((ParcelMediaCommandEnum)commandToSend == ParcelMediaCommandEnum.LoopSet)
                        ParamToSend = mediaLoopSet;

                    // the commandList contained a start/stop/... command, too
                    if (presence == null)
                    {
                        // send to all (non-child) agents
                        World.ForEachScenePresence(delegate(IScenePresence sp)
                        {
                            if (!sp.IsChildAgent)
                            {
                                sp.ControllingClient.SendParcelMediaCommand(landData.Flags,
                                                                               (ParcelMediaCommandEnum)commandToSend,
                                                                               ParamToSend);
                            }
                        });
                    }
                    else if (!presence.IsChildAgent)
                    {
                        presence.ControllingClient.SendParcelMediaCommand(landData.Flags,
                                                                          (ParcelMediaCommandEnum)commandToSend,
                                                                          ParamToSend);
                    }
                }
            }
            ScriptSleep(2000);
        }

        public LSL_List llParcelMediaQuery(LSL_List aList)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            LSL_List list = new LSL_List();
            for (int i = 0; i < aList.Data.Length; i++)
            {
                if (aList.Data[i] != null)
                {
                    IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
                    if (parcelManagement != null)
                    {
                        LSL_Integer tmp = (LSL_Integer)aList.Data[i];
                        switch ((ParcelMediaCommandEnum)tmp.value)
                        {
                            case ParcelMediaCommandEnum.Url:
                                list.Add(new LSL_String(parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y).LandData.MediaURL));
                                break;
                            case ParcelMediaCommandEnum.Desc:
                                list.Add(new LSL_String(parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y).LandData.MediaDescription));
                                break;
                            case ParcelMediaCommandEnum.Texture:
                                list.Add(new LSL_String(parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y).LandData.MediaID.ToString()));
                                break;
                            case ParcelMediaCommandEnum.Type:
                                list.Add(new LSL_String(parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y).LandData.MediaType));
                                break;
                            case ParcelMediaCommandEnum.Loop:
                                list.Add(new LSL_Integer(parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y).LandData.MediaLoop ? 1 : 0));
                                break;
                            case ParcelMediaCommandEnum.LoopSet:
                                list.Add(new LSL_Integer(parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y).LandData.MediaLoopSet));
                                break;
                            case ParcelMediaCommandEnum.Size:
                                list.Add(new LSL_String(parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y).LandData.MediaHeight));
                                list.Add(new LSL_String(parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y).LandData.MediaWidth));
                                break;
                            default:
                                ParcelMediaCommandEnum mediaCommandEnum = ParcelMediaCommandEnum.Url;
                                NotImplemented("llParcelMediaQuery parameter do not supported yet: " + Enum.Parse(mediaCommandEnum.GetType(), aList.Data[i].ToString()).ToString());
                                break;
                        }
                    }
                }
            }
            ScriptSleep(2000);
            return list;
        }

        public LSL_List llGetPrimMediaParams(LSL_Integer face, LSL_List rules)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            ScriptSleep(1000);

            // LSL Spec http://wiki.secondlife.com/wiki/LlGetPrimMediaParams says to fail silently if face is invalid
            // TODO: Need to correctly handle case where a face has no media (which gives back an empty list).
            // Assuming silently fail means give back an empty list.  Ideally, need to check this.
            if (face < 0 || face > m_host.GetNumberOfSides() - 1)
                return new LSL_List();
            else
                return GetPrimMediaParams(face, rules);
        }

        private LSL_List GetPrimMediaParams(int face, LSL_List rules)
        {
            IMoapModule module = World.RequestModuleInterface<IMoapModule>();
            if (null == module)
                throw new Exception("Media on a prim functions not available");

            MediaEntry me = module.GetMediaEntry(m_host, face);

            // As per http://wiki.secondlife.com/wiki/LlGetPrimMediaParams
            if (null == me)
                return new LSL_List();

            LSL_List res = new LSL_List();

            for (int i = 0; i < rules.Length; i++)
            {
                int code = (int)rules.GetLSLIntegerItem(i);

                if (code == ScriptBaseClass.PRIM_MEDIA_ALT_IMAGE_ENABLE)
                {
                    // Not implemented
                    res.Add(new LSL_Integer(0));
                }

                else if (code == ScriptBaseClass.PRIM_MEDIA_CONTROLS)
                {
                    if (me.Controls == MediaControls.Standard)
                        res.Add(new LSL_Integer(ScriptBaseClass.PRIM_MEDIA_CONTROLS_STANDARD));
                    else
                        res.Add(new LSL_Integer(ScriptBaseClass.PRIM_MEDIA_CONTROLS_MINI));
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_CURRENT_URL)
                {
                    res.Add(new LSL_String(me.CurrentURL));
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_HOME_URL)
                {
                    res.Add(new LSL_String(me.HomeURL));
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_AUTO_LOOP)
                {
                    res.Add(me.AutoLoop ? ScriptBaseClass.TRUE : ScriptBaseClass.FALSE);
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_AUTO_PLAY)
                {
                    res.Add(me.AutoPlay ? ScriptBaseClass.TRUE : ScriptBaseClass.FALSE);
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_AUTO_SCALE)
                {
                    res.Add(me.AutoScale ? ScriptBaseClass.TRUE : ScriptBaseClass.FALSE);
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_AUTO_ZOOM)
                {
                    res.Add(me.AutoZoom ? ScriptBaseClass.TRUE : ScriptBaseClass.FALSE);
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_FIRST_CLICK_INTERACT)
                {
                    res.Add(me.InteractOnFirstClick ? ScriptBaseClass.TRUE : ScriptBaseClass.FALSE);
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_WIDTH_PIXELS)
                {
                    res.Add(new LSL_Integer(me.Width));
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_HEIGHT_PIXELS)
                {
                    res.Add(new LSL_Integer(me.Height));
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_WHITELIST_ENABLE)
                {
                    res.Add(me.EnableWhiteList ? ScriptBaseClass.TRUE : ScriptBaseClass.FALSE);
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_WHITELIST)
                {
                    string[] urls = (string[])me.WhiteList.Clone();

                    for (int j = 0; j < urls.Length; j++)
                        urls[j] = Uri.EscapeDataString(urls[j]);

                    res.Add(new LSL_String(string.Join(", ", urls)));
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_PERMS_INTERACT)
                {
                    res.Add(new LSL_Integer((int)me.InteractPermissions));
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_PERMS_CONTROL)
                {
                    res.Add(new LSL_Integer((int)me.ControlPermissions));
                }
            }

            return res;
        }

        public LSL_Integer llClearPrimMedia(LSL_Integer face)
        {
            ScriptSleep(1000);

            // LSL Spec http://wiki.secondlife.com/wiki/LlClearPrimMedia says to fail silently if face is invalid
            // Assuming silently fail means sending back LSL_STATUS_OK.  Ideally, need to check this.
            // FIXME: Don't perform the media check directly
            if (face < 0 || face > m_host.GetNumberOfSides() - 1)
                {
                return (LSL_Integer)ScriptBaseClass.LSL_STATUS_OK;
                }

            IMoapModule module = World.RequestModuleInterface<IMoapModule>();
            if (null == module)
                throw new Exception("Media on a prim functions not available");

            module.ClearMediaEntry(m_host, face);

            return (LSL_Integer)ScriptBaseClass.LSL_STATUS_OK;
        }

        public LSL_Integer llSetPrimMediaParams(LSL_Integer face, LSL_List rules)
        {
            ScriptSleep(1000);

            // LSL Spec http://wiki.secondlife.com/wiki/LlSetPrimMediaParams says to fail silently if face is invalid
            // Assuming silently fail means sending back LSL_STATUS_OK.  Ideally, need to check this.
            // Don't perform the media check directly
            if (face < 0 || face > m_host.GetNumberOfSides() - 1)
                return (LSL_Integer)ScriptBaseClass.LSL_STATUS_OK;
            else
                return (LSL_Integer)SetPrimMediaParams(face, rules);
        }

        public LSL_Integer SetPrimMediaParams(int face, LSL_List rules)
        {
            IMoapModule module = World.RequestModuleInterface<IMoapModule>();
            if (null == module)
                throw new Exception("Media on a prim functions not available");

            MediaEntry me = module.GetMediaEntry(m_host, face);
            if (null == me)
                me = new MediaEntry();

            int i = 0;

            while (i < rules.Length - 1)
            {
                int code = rules.GetLSLIntegerItem(i++);

                if (code == ScriptBaseClass.PRIM_MEDIA_ALT_IMAGE_ENABLE)
                {
                    me.EnableAlterntiveImage = (rules.GetLSLIntegerItem(i++) != 0 ? true : false);
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_CONTROLS)
                {
                    int v = rules.GetLSLIntegerItem(i++);
                    if (ScriptBaseClass.PRIM_MEDIA_CONTROLS_STANDARD == v)
                        me.Controls = MediaControls.Standard;
                    else
                        me.Controls = MediaControls.Mini;
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_CURRENT_URL)
                {
                    me.CurrentURL = rules.GetLSLStringItem(i++);
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_HOME_URL)
                {
                    me.HomeURL = rules.GetLSLStringItem(i++);
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_AUTO_LOOP)
                {
                    me.AutoLoop = (ScriptBaseClass.TRUE == rules.GetLSLIntegerItem(i++) ? true : false);
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_AUTO_PLAY)
                {
                    me.AutoPlay = (ScriptBaseClass.TRUE == rules.GetLSLIntegerItem(i++) ? true : false);
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_AUTO_SCALE)
                {
                    me.AutoScale = (ScriptBaseClass.TRUE == rules.GetLSLIntegerItem(i++) ? true : false);
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_AUTO_ZOOM)
                {
                    me.AutoZoom = (ScriptBaseClass.TRUE == rules.GetLSLIntegerItem(i++) ? true : false);
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_FIRST_CLICK_INTERACT)
                {
                    me.InteractOnFirstClick = (ScriptBaseClass.TRUE == rules.GetLSLIntegerItem(i++) ? true : false);
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_WIDTH_PIXELS)
                {
                    me.Width = (int)rules.GetLSLIntegerItem(i++);
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_HEIGHT_PIXELS)
                {
                    me.Height = (int)rules.GetLSLIntegerItem(i++);
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_WHITELIST_ENABLE)
                {
                    me.EnableWhiteList = (ScriptBaseClass.TRUE == rules.GetLSLIntegerItem(i++) ? true : false);
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_WHITELIST)
                {
                    string[] rawWhiteListUrls = rules.GetLSLStringItem(i++).ToString().Split(new char[] { ',' });
                    List<string> whiteListUrls = new List<string>();
                    Array.ForEach(
                        rawWhiteListUrls, delegate(string rawUrl) { whiteListUrls.Add(rawUrl.Trim()); });
                    me.WhiteList = whiteListUrls.ToArray();
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_PERMS_INTERACT)
                {
                    me.InteractPermissions = (MediaPermission)(byte)(int)rules.GetLSLIntegerItem(i++);
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_PERMS_CONTROL)
                {
                    me.ControlPermissions = (MediaPermission)(byte)(int)rules.GetLSLIntegerItem(i++);
                }
            }

            module.SetMediaEntry(m_host, face, me);

            return ScriptBaseClass.LSL_STATUS_OK;
        }

        public LSL_Integer llModPow(int a, int b, int c)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            Int64 tmp = 0;
            Math.DivRem(Convert.ToInt64(Math.Pow(a, b)), c, out tmp);
            ScriptSleep(100);
            return (LSL_Integer)Convert.ToInt32(tmp);
        }

        public LSL_Integer llGetInventoryType(string name)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            
            lock (m_host.TaskInventory)
            {
                foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory)
                {
                    if (inv.Value.Name == name)
                    {
                        return inv.Value.Type;
                    }
                }
            }

            return -1;
        }

        public void llSetPayPrice(int price, LSL_List quick_pay_buttons)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            
            if (quick_pay_buttons.Data.Length < 4)
            {
                LSLError("List must have at least 4 elements");
                return;
            }
            m_host.ParentEntity.RootChild.PayPrice[0] = price;

            m_host.ParentEntity.RootChild.PayPrice[1] = (LSL_Integer)quick_pay_buttons.Data[0];
            m_host.ParentEntity.RootChild.PayPrice[2] = (LSL_Integer)quick_pay_buttons.Data[1];
            m_host.ParentEntity.RootChild.PayPrice[3] = (LSL_Integer)quick_pay_buttons.Data[2];
            m_host.ParentEntity.RootChild.PayPrice[4] = (LSL_Integer)quick_pay_buttons.Data[3];
            m_host.ParentEntity.HasGroupChanged = true;
        }

        public LSL_Vector llGetCameraPos()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            UUID invItemID = InventorySelf();

            if (invItemID == UUID.Zero)
                return new LSL_Vector();

            lock (m_host.TaskInventory)
            {
                if (m_host.TaskInventory[invItemID].PermsGranter == UUID.Zero)
                   return new LSL_Vector();

                if ((m_host.TaskInventory[invItemID].PermsMask & ScriptBaseClass.PERMISSION_TRACK_CAMERA) == 0)
                {
                    ShoutError("No permissions to track the camera");
                    return new LSL_Vector();
                }
            }

            IScenePresence presence = World.GetScenePresence (m_host.OwnerID);
            if (presence != null)
            {
                LSL_Vector pos = new LSL_Vector(presence.CameraPosition.X, presence.CameraPosition.Y, presence.CameraPosition.Z);
                return pos;
            }
            return new LSL_Vector();
        }

        public LSL_Rotation llGetCameraRot()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            UUID invItemID = InventorySelf();
            if (invItemID == UUID.Zero)
                return new LSL_Rotation();

            lock (m_host.TaskInventory)
            {
                if (m_host.TaskInventory[invItemID].PermsGranter == UUID.Zero)
                   return new LSL_Rotation();

                if ((m_host.TaskInventory[invItemID].PermsMask & ScriptBaseClass.PERMISSION_TRACK_CAMERA) == 0)
                {
                    ShoutError("No permissions to track the camera");
                    return new LSL_Rotation();
                }
            }

            IScenePresence presence = World.GetScenePresence (m_host.OwnerID);
            if (presence != null)
            {
                return new LSL_Rotation(presence.CameraRotation.X, presence.CameraRotation.Y, presence.CameraRotation.Z, presence.CameraRotation.W);
            }

            return new LSL_Rotation();
        }

        /// <summary>
        /// The SL implementation does nothing, it is deprecated
        /// This duplicates SL
        /// </summary>
        public void llSetPrimURL(string url)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            ScriptSleep(2000);
        }

        /// <summary>
        /// The SL implementation shouts an error, it is deprecated
        /// This duplicates SL
        /// </summary>
        public void llRefreshPrimURL()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            Deprecated("llRefreshPrimURL");
            ScriptSleep(20000);
        }

        public LSL_String llEscapeURL(string url)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            try
            {
                return Uri.EscapeDataString(url);
            }
            catch (Exception ex)
            {
                return "llEscapeURL: " + ex.ToString();
            }
        }

        public LSL_String llUnescapeURL(string url)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            try
            {
                return Uri.UnescapeDataString(url);
            }
            catch (Exception ex)
            {
                return "llUnescapeURL: " + ex.ToString();
            }
        }

        public void llMapDestination(string simname, LSL_Vector pos, LSL_Vector lookAt)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            UUID avatarID = m_host.OwnerID;
            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, 0);
            // only works on the first detected avatar
            //This only works in touch events or if the item is attached to the avatar
            if (detectedParams == null && !m_host.IsAttachment)
                return; 

            if (detectedParams != null)
                avatarID = detectedParams.Key;

            IScenePresence avatar = World.GetScenePresence (avatarID);
            if (avatar != null)
            {
                Aurora.Framework.IMuteListModule module = m_host.ParentEntity.Scene.RequestModuleInterface<Aurora.Framework.IMuteListModule> ();
                if (module != null)
                {
                    bool cached = false; //Unneeded
                    foreach (Aurora.Framework.MuteList mute in module.GetMutes(avatar.UUID, out cached))
                    {
                        if (mute.MuteID == m_host.OwnerID)
                            return;//If the avatar is muted, they don't get any contact from the muted av
                    }
                }
                avatar.ControllingClient.SendScriptTeleportRequest(m_host.Name, simname,
                                                                   new Vector3((float)pos.x, (float)pos.y, (float)pos.z),
                                                                   new Vector3((float)lookAt.x, (float)lookAt.y, (float)lookAt.z));
            }
            ScriptSleep(1000);
        }

        public void llAddToLandBanList(string avatar, double hours)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            UUID key;
            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                LandData land = parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y).LandData;
                if (land.OwnerID == m_host.OwnerID)
                {
                    ParcelManager.ParcelAccessEntry entry = new ParcelManager.ParcelAccessEntry();
                    if (UUID.TryParse(avatar, out key))
                    {
                        entry.AgentID = key;
                        entry.Flags = AccessList.Ban;
                        entry.Time = DateTime.Now.AddHours(hours);
                        land.ParcelAccessList.Add(entry);
                    }
                }
            }
            ScriptSleep(100);
        }

        public void llRemoveFromLandPassList(string avatar)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            UUID key;
            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                LandData land = parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y).LandData;
                if (land.OwnerID == m_host.OwnerID)
                {
                    if (UUID.TryParse(avatar, out key))
                    {
                        foreach (ParcelManager.ParcelAccessEntry entry in land.ParcelAccessList)
                        {
                            if (entry.AgentID == key && entry.Flags == AccessList.Access)
                            {
                                land.ParcelAccessList.Remove(entry);
                                break;
                            }
                        }
                    }
                }
            }
            ScriptSleep(100);
        }

        public void llRemoveFromLandBanList(string avatar)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            UUID key;
            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                LandData land = parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y).LandData;
                if (land.OwnerID == m_host.OwnerID)
                {
                    if (UUID.TryParse(avatar, out key))
                    {
                        foreach (ParcelManager.ParcelAccessEntry entry in land.ParcelAccessList)
                        {
                            if (entry.AgentID == key && entry.Flags == AccessList.Ban)
                            {
                                land.ParcelAccessList.Remove(entry);
                                break;
                            }
                        }
                    }
                }
            }
            ScriptSleep(100);
        }

        public void llSetCameraParams(LSL_List rules)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            
            // our key in the object we are in
            UUID invItemID = InventorySelf();
            if (invItemID == UUID.Zero) return;

            // the object we are in
            UUID objectID = m_host.ParentUUID;
            if (objectID == UUID.Zero) return;

            UUID agentID;
            lock (m_host.TaskInventory)
            {
                // we need the permission first, to know which avatar we want to set the camera for
                agentID = m_host.TaskInventory[invItemID].PermsGranter;

                if (agentID == UUID.Zero) return;
                if ((m_host.TaskInventory[invItemID].PermsMask & ScriptBaseClass.PERMISSION_CONTROL_CAMERA) == 0) return;
            }

            IScenePresence presence = World.GetScenePresence (agentID);

            // we are not interested in child-agents
            if (presence.IsChildAgent) return;

            SortedDictionary<int, float> parameters = new SortedDictionary<int, float>();
            object[] data = rules.Data;
            for (int i = 0; i < data.Length; ++i)
            {
                int type = Convert.ToInt32(data[i++].ToString());
                if (i >= data.Length) break; // odd number of entries => ignore the last

                // some special cases: Vector parameters are split into 3 float parameters (with type+1, type+2, type+3)
                if (type == ScriptBaseClass.CAMERA_FOCUS ||
                    type == ScriptBaseClass.CAMERA_FOCUS_OFFSET ||
                    type == ScriptBaseClass.CAMERA_POSITION)
                {
                    LSL_Vector v = (LSL_Vector)data[i];
                    parameters.Add(type + 1, (float)v.x);
                    parameters.Add(type + 2, (float)v.y);
                    parameters.Add(type + 3, (float)v.z);
                }
                else
                {
                    if (data[i] is LSL_Float)
                        parameters.Add(type, (float)((LSL_Float)data[i]).value);
                    else if (data[i] is LSL_Integer)
                        parameters.Add(type, (float)((LSL_Integer)data[i]).value);
                    else parameters.Add(type, Convert.ToSingle(data[i]));
                }
            }
            if (parameters.Count > 0) presence.ControllingClient.SendSetFollowCamProperties(objectID, parameters);
        }

        public void llClearCameraParams()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            
            // our key in the object we are in
            UUID invItemID=InventorySelf();
            if (invItemID == UUID.Zero) return;

            // the object we are in
            UUID objectID = m_host.ParentUUID;
            if (objectID == UUID.Zero) return;

            // we need the permission first, to know which avatar we want to clear the camera for
            UUID agentID;
            lock (m_host.TaskInventory)
            {
                agentID = m_host.TaskInventory[invItemID].PermsGranter;
                if (agentID == UUID.Zero) return;
                if ((m_host.TaskInventory[invItemID].PermsMask & ScriptBaseClass.PERMISSION_CONTROL_CAMERA) == 0) return;
            }

            IScenePresence presence = World.GetScenePresence (agentID);

            // we are not interested in child-agents
            if (presence.IsChildAgent) return;

            presence.ControllingClient.SendClearFollowCamProperties(objectID);
        }

        public LSL_Float llListStatistics(int operation, LSL_List src)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            LSL_List nums = LSL_List.ToDoubleList(src);
            if (operation == ScriptBaseClass.LIST_STAT_RANGE)
                return nums.Range();
            else if (operation == ScriptBaseClass.LIST_STAT_MIN)
                return nums.Min();
            else if (operation == ScriptBaseClass.LIST_STAT_MAX)
                return nums.Max();
            else if (operation == ScriptBaseClass.LIST_STAT_MEAN)
                return nums.Mean();
            else if (operation == ScriptBaseClass.LIST_STAT_MEDIAN)
                return nums.Median();
            else if (operation == ScriptBaseClass.LIST_STAT_NUM_COUNT)
                return nums.NumericLength();
            else if (operation == ScriptBaseClass.LIST_STAT_STD_DEV)
                return nums.StdDev();
            else if (operation == ScriptBaseClass.LIST_STAT_SUM)
                return nums.Sum();
            else if (operation == ScriptBaseClass.LIST_STAT_SUM_SQUARES)
                return nums.SumSqrs();
            else if (operation == ScriptBaseClass.LIST_STAT_GEOMETRIC_MEAN)
                return nums.GeometricMean();
            else if (operation == ScriptBaseClass.LIST_STAT_HARMONIC_MEAN)
                return nums.HarmonicMean();
            else
                return 0.0;
        }

        public LSL_Integer llGetUnixTime()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            return Util.UnixTimeSinceEpoch();
        }

        public LSL_Integer llGetParcelFlags(LSL_Vector pos)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                return (int)parcelManagement.GetLandObject((float)pos.x, (float)pos.y).LandData.Flags;
            }
            return 0;
        }

        public LSL_Integer llGetRegionFlags()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            IEstateModule estate = World.RequestModuleInterface<IEstateModule>();
            if (estate == null)
                return 67108864;
            return (int)estate.GetRegionFlags();
        }

        public LSL_String llXorBase64StringsCorrect(string str1, string str2)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            string ret = String.Empty;
            string src1 = llBase64ToString(str1);
            string src2 = llBase64ToString(str2);
            int c = 0;
            for (int i = 0; i < src1.Length; i++)
            {
                ret += (char) (src1[i] ^ src2[c]);

                c++;
                if (c >= src2.Length)
                    c = 0;
            }
            return llStringToBase64(ret);
        }

        public LSL_String llHTTPRequest(string url, LSL_List parameters, string body)
        {
            // Partial implementation: support for parameter flags needed
            //   see http://wiki.secondlife.com/wiki/LlHTTPRequest
            // parameter flags support are implemented in ScriptsHttpRequests.cs
            //   in StartHttpRequest

            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            IHttpRequestModule httpScriptMod =
                World.RequestModuleInterface<IHttpRequestModule>();
            List<string> param = new List<string>();
            foreach (object o in parameters.Data)
            {
                param.Add(o.ToString());
            }

            Vector3 position = m_host.AbsolutePosition;
            Vector3 velocity = m_host.Velocity;
            Quaternion rotation = m_host.RotationOffset;
            string ownerName = String.Empty;
            IScenePresence scenePresence = World.GetScenePresence (m_host.OwnerID);
            if (scenePresence == null)
                ownerName = resolveName(m_host.OwnerID);
            else
                ownerName = scenePresence.Name;

            RegionInfo regionInfo = World.RegionInfo;

            Dictionary<string, string> httpHeaders = new Dictionary<string, string>();

            string shard = "OpenSim";
            IConfigSource config = m_ScriptEngine.ConfigSource;
            if (config.Configs["LSLRemoting"] != null)
                shard = config.Configs["LSLRemoting"].GetString("shard", shard);

            httpHeaders["X-SecondLife-Shard"] = shard;
            httpHeaders["X-SecondLife-Object-Name"] = m_host.Name;
            httpHeaders["X-SecondLife-Object-Key"] = m_host.UUID.ToString();
            httpHeaders["X-SecondLife-Region"] = string.Format("{0} ({1}, {2})", regionInfo.RegionName, regionInfo.RegionLocX, regionInfo.RegionLocY);
            httpHeaders["X-SecondLife-Local-Position"] = string.Format("({0:0.000000}, {1:0.000000}, {2:0.000000})", position.X, position.Y, position.Z);
            httpHeaders["X-SecondLife-Local-Velocity"] = string.Format("({0:0.000000}, {1:0.000000}, {2:0.000000})", velocity.X, velocity.Y, velocity.Z);
            httpHeaders["X-SecondLife-Local-Rotation"] = string.Format("({0:0.000000}, {1:0.000000}, {2:0.000000}, {3:0.000000})", rotation.X, rotation.Y, rotation.Z, rotation.W);
            httpHeaders["X-SecondLife-Owner-Name"] = ownerName;
            httpHeaders["X-SecondLife-Owner-Key"] = m_host.OwnerID.ToString();
            string userAgent = "";
            if (config.Configs["LSLRemoting"] != null)
                shard = config.Configs["LSLRemoting"].GetString("user_agent", null);

            if (userAgent != null)
                httpHeaders["User-Agent"] = userAgent;

            string authregex = @"^(https?:\/\/)(\w+):(\w+)@(.*)$";
            Regex r = new Regex(authregex);
            int[] gnums = r.GetGroupNumbers();
            Match m = r.Match(url);
            if (m.Success) {
                for (int i = 1; i < gnums.Length; i++) {
                    //System.Text.RegularExpressions.Group g = m.Groups[gnums[i]];
                    //CaptureCollection cc = g.Captures;
                }
                if (m.Groups.Count == 5) {
                    httpHeaders["Authorization"] = String.Format("Basic {0}", Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(m.Groups[2].ToString() + ":" + m.Groups[3].ToString())));
                    url = m.Groups[1].ToString() + m.Groups[4].ToString();
                }
            }

            UUID reqID = httpScriptMod.
                StartHttpRequest(m_host.UUID, m_itemID, url, param, httpHeaders, body);

            if (reqID != UUID.Zero)
                return reqID.ToString();
            else
                return new LSL_String("");
        }


        public void llHTTPResponse(LSL_Key id, int status, string body)
        {
            // Partial implementation: support for parameter flags needed
            //   see http://wiki.secondlife.com/wiki/llHTTPResponse

            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            
            if (m_UrlModule != null)
                m_UrlModule.HttpResponse(new UUID(id), status,body);
        }

        public void llResetLandBanList()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                LandData land = parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y).LandData;
                if (land.OwnerID == m_host.OwnerID)
                {
                    foreach (ParcelManager.ParcelAccessEntry entry in land.ParcelAccessList)
                    {
                        if (entry.Flags == AccessList.Ban)
                        {
                            land.ParcelAccessList.Remove(entry);
                        }
                    }
                }
            }
            ScriptSleep(100);
        }

        public void llResetLandPassList()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                LandData land = parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y).LandData;
                if (land.OwnerID == m_host.OwnerID)
                {
                    foreach (ParcelManager.ParcelAccessEntry entry in land.ParcelAccessList)
                    {
                        if (entry.Flags == AccessList.Access)
                        {
                            land.ParcelAccessList.Remove(entry);
                        }
                    }
                }
            }
            ScriptSleep(100);
        }

        public LSL_Integer llGetParcelPrimCount(LSL_Vector pos, int category, int sim_wide)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                LandData land = parcelManagement.GetLandObject((float)pos.x, (float)pos.y).LandData;

                if (land == null)
                {
                    return 0;
                }
                else
                {
                    IPrimCountModule primCountsModule = World.RequestModuleInterface<IPrimCountModule>();
                    if (primCountsModule != null)
                    {
                        IPrimCounts primCounts = primCountsModule.GetPrimCounts(land.GlobalID);
                        if (sim_wide != 0)
                        {
                            if (category == 0)
                            {
                                return primCounts.Simulator;
                            }
                            else
                            {
                                return 0;
                            }
                        }
                        else
                        {
                            if (category == 0)//Total Prims
                            {
                                return primCounts.Total;//land.
                            }
                            else if (category == 1)//Owner Prims
                            {
                                return primCounts.Owner;
                            }
                            else if (category == 2)//Group Prims
                            {
                                return primCounts.Group;
                            }
                            else if (category == 3)//Other Prims
                            {
                                return primCounts.Others;
                            }
                            else if (category == 4)//Selected
                            {
                                return primCounts.Selected;
                            }
                            else if (category == 5)//Temp
                            {
                                return primCounts.Temporary;//land.
                            }
                        }
                    }
                }
            }
            return 0;
        }

        public LSL_List llGetParcelPrimOwners(LSL_Vector pos)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            LSL_List ret = new LSL_List();
            if (parcelManagement != null)
            {
                ILandObject land = parcelManagement.GetLandObject((float)pos.x, (float)pos.y);
                if (land != null)
                {
                    IPrimCountModule primCountModule = World.RequestModuleInterface<IPrimCountModule>();
                    if (primCountModule != null)
                    {
                        IPrimCounts primCounts = primCountModule.GetPrimCounts(land.LandData.GlobalID);
                        foreach (KeyValuePair<UUID, int> detectedParams in primCounts.GetAllUserCounts())
                        {
                            ret.Add(detectedParams.Key.ToString());
                            ret.Add(detectedParams.Value);
                        }
                    }
                }
            }
            ScriptSleep(2000);
            return (LSL_List) ret;
        }

        public LSL_Integer llGetObjectPrimCount(string object_id)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            ISceneChildEntity part = World.GetSceneObjectPart (new UUID (object_id));
            if (part == null)
            {
                return 0;
            }
            else
            {
                return part.ParentEntity.PrimCount;
            }
        }

        public LSL_Integer llGetParcelMaxPrims(LSL_Vector pos, int sim_wide)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            // Alondria: This currently just is utilizing the normal grid's 0.22 prims/m2 calculation
            // Which probably will be irrelevent in OpenSim....
            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                LandData land = parcelManagement.GetLandObject((float)pos.x, (float)pos.y).LandData;

                float bonusfactor = (float)World.RegionInfo.RegionSettings.ObjectBonus;

                if (land == null)
                {
                    return 0;
                }
                else if (sim_wide != 0)
                {
                    decimal v = land.SimwideArea * (decimal)(0.22) * (decimal)bonusfactor;

                    return (int)v;
                }
                else
                {
                    decimal v = land.Area * (decimal)(0.22) * (decimal)bonusfactor;

                    return (int)v;
                }
            }
            return 0;
        }

        public LSL_List llGetParcelDetails(LSL_Vector pos, LSL_List param)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");

            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            LSL_List ret = new LSL_List();
            if (parcelManagement != null)
            {
                LandData land = parcelManagement.GetLandObject((float)pos.x, (float)pos.y).LandData;
                if (land == null)
                {
                    return new LSL_List(0);
                }
                foreach (object o in param.Data)
                {
                    switch (o.ToString())
                    {
                        case "0":
                            ret = ret + new LSL_List(land.Name);
                            break;
                        case "1":
                            ret = ret + new LSL_List(land.Description);
                            break;
                        case "2":
                            ret = ret + new LSL_List(land.OwnerID.ToString());
                            break;
                        case "3":
                            ret = ret + new LSL_List(land.GroupID.ToString());
                            break;
                        case "4":
                            ret = ret + new LSL_List(land.Area);
                            break;
                        case "5":
                            ret = ret + new LSL_List(land.InfoUUID);
                            //Returning the InfoUUID so that we can use this for landmarks outside of this region
                            // http://wiki.secondlife.com/wiki/PARCEL_DETAILS_ID
                            break;
                        default:
                            ret = ret + new LSL_List(0);
                            break;
                    }
                }
            }
            return ret;
        }

        public LSL_String llStringTrim(string src, int type)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            if (type == (int)ScriptBaseClass.STRING_TRIM_HEAD) { return src.TrimStart(); }
            if (type == (int)ScriptBaseClass.STRING_TRIM_TAIL) { return src.TrimEnd(); }
            if (type == (int)ScriptBaseClass.STRING_TRIM) { return src.Trim(); }
            return src;
        }

        public LSL_List llGetObjectDetails(string id, LSL_List args)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            LSL_List ret = new LSL_List();
            UUID key = new UUID();
            if (UUID.TryParse(id, out key))
            {
                IScenePresence av = World.GetScenePresence (key);

                if (av != null)
                {
                    foreach (object o in args.Data)
                    {
                        if ((LSL_Integer)o == ScriptBaseClass.OBJECT_NAME)
                        {
                            ret.Add(av.Name);
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_DESC)
                        {
                            ret.Add("");
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_POS)
                        {
                            Vector3 tmp = av.AbsolutePosition;
                            ret.Add(new LSL_Vector((double)tmp.X, (double)tmp.Y, (double)tmp.Z));
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_ROT)
                        {
                            Quaternion rtmp = av.Rotation;
                            ret.Add(new LSL_Rotation((double)rtmp.X, (double)rtmp.Y, (double)rtmp.Z, (double)rtmp.W));
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_VELOCITY)
                        {
                            Vector3 tmp = av.Velocity;
                            ret.Add(new LSL_Vector(tmp.X, tmp.Y, tmp.Z));
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_OWNER)
                        {
                            ret.Add(id);
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_GROUP)
                        {
                            ret.Add(UUID.Zero.ToString());
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_CREATOR)
                        {
                            ret.Add(UUID.Zero.ToString());
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_RUNNING_SCRIPT_COUNT)
                        {
                            IScriptModule[] modules = World.RequestModuleInterfaces<IScriptModule>();
                            int activeScripts = 0;
                            foreach (IScriptModule module in modules)
                            {
                                activeScripts += module.GetActiveScripts(av);
                            }
                            ret.Add(activeScripts);
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_TOTAL_SCRIPT_COUNT)
                        {
                            IScriptModule[] modules = World.RequestModuleInterfaces<IScriptModule>();
                            int totalScripts = 0;
                            foreach (IScriptModule module in modules)
                            {
                                totalScripts += module.GetTotalScripts(av);
                            }
                            ret.Add(totalScripts);
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_SCRIPT_MEMORY)
                        {
                            ret.Add(0);
                        }
                    }
                    return ret;
                }
                ISceneChildEntity obj = World.GetSceneObjectPart (key);
                if (obj != null)
                {
                    foreach (object o in args.Data)
                    {
                        if ((LSL_Integer)o == ScriptBaseClass.OBJECT_NAME)
                        {
                            ret.Add(obj.Name);
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_DESC)
                        {
                            ret.Add(obj.Description);
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_POS)
                        {
                            Vector3 tmp = obj.AbsolutePosition;
                            ret.Add(new LSL_Vector(tmp.X, tmp.Y, tmp.Z));
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_ROT)
                        {
                            Quaternion rtmp = obj.RotationOffset;
                            ret.Add(new LSL_Rotation(rtmp.X, rtmp.Y, rtmp.Z, rtmp.W));
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_VELOCITY)
                        {
                            Vector3 tmp = obj.Velocity;
                            ret.Add(new LSL_Vector(tmp.X, tmp.Y, tmp.Z));
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_OWNER)
                        {
                            ret.Add(obj.OwnerID.ToString());
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_GROUP)
                        {
                            ret.Add(obj.GroupID.ToString());
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_CREATOR)
                        {
                            ret.Add(obj.CreatorID.ToString());
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_RUNNING_SCRIPT_COUNT)
                        {
                            IScriptModule[] modules = World.RequestModuleInterfaces<IScriptModule>();
                            int activeScripts = 0;
                            foreach(IScriptModule module in modules)
                            {
                                activeScripts += module.GetActiveScripts(obj);
                            }
                            ret.Add(activeScripts);
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_TOTAL_SCRIPT_COUNT)
                        {
                            IScriptModule[] modules = World.RequestModuleInterfaces<IScriptModule>();
                            int totalScripts = 0;
                            foreach (IScriptModule module in modules)
                            {
                                totalScripts += module.GetTotalScripts(obj);
                            }
                            ret.Add(totalScripts);
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_SCRIPT_MEMORY)
                        {
                            ret.Add(0);
                        }
                    }
                    return ret;
                }
            }
            return new LSL_List();
        }

        internal UUID ScriptByName(string name)
        {
            lock (m_host.TaskInventory)
            {
                foreach (TaskInventoryItem item in m_host.TaskInventory.Values)
                {
                    if (item.Type == 10 && item.Name == name)
                        return item.ItemID;
                }
            }

            return UUID.Zero;
        }

        internal void ShoutError(string msg)
        {
            llShout(ScriptBaseClass.DEBUG_CHANNEL, msg);
        }

        internal void NotImplemented(string command)
        {
            if (throwErrorOnNotImplemented)
                throw new NotImplementedException("Command not implemented: " + command);
        }

        internal void Deprecated(string command)
        {
            throw new Exception("Command deprecated: " + command);
        }

        internal void LSLError(string msg)
        {
            throw new Exception("LSL Runtime Error: " + msg);
        }

        public delegate void AssetRequestCallback(UUID assetID, AssetBase asset);
        protected void WithNotecard(UUID assetID, AssetRequestCallback cb)
        {
            World.AssetService.Get(assetID.ToString(), this,
                delegate(string i, object sender, AssetBase a)
                {
                    UUID uuid = UUID.Zero;
                    UUID.TryParse(i, out uuid);
                    cb(uuid, a);
                });
        }

        public LSL_List llCastRay(LSL_Vector start, LSL_Vector end, LSL_List options)
        {
            Vector3 dir = new Vector3((float)(end-start).x, (float)(end-start).y, (float)(end-start).z);
            Vector3 startvector = new Vector3((float)start.x, (float)start.y, (float)start.z);
            Vector3 endvector = new Vector3((float)end.x, (float)end.y, (float)end.z);


            int count = 0;
            int detectPhantom = 0;
            int dataFlags = 0;
            int rejectTypes = 0;

            for (int i = 0; i < options.Length; i += 2)
            {
                if (options.GetLSLIntegerItem(i) == ScriptBaseClass.RC_MAX_HITS)
                {
                    count = options.GetLSLIntegerItem(i + 1);
                }
                else if (options.GetLSLIntegerItem(i) == ScriptBaseClass.RC_DETECT_PHANTOM)
                {
                    detectPhantom = options.GetLSLIntegerItem(i + 1);
                }
                else if (options.GetLSLIntegerItem(i) == ScriptBaseClass.RC_DATA_FLAGS)
                {
                    dataFlags = options.GetLSLIntegerItem(i + 1);
                }
                else if (options.GetLSLIntegerItem(i) == ScriptBaseClass.RC_REJECT_TYPES)
                {
                    rejectTypes = options.GetLSLIntegerItem(i + 1);
                }
            }

            LSL_List list = new LSL_List();
            List<ContactResult> results = World.PhysicsScene.RaycastWorld(startvector, dir, dir.Length(), count);

            double distance = Util.GetDistanceTo(startvector, endvector);
            if (distance == 0)
                distance = 0.001;
            Vector3 posToCheck = startvector;
            ITerrainChannel channel = World.RequestModuleInterface<ITerrainChannel>();
            List<IScenePresence> presences = new List<IScenePresence>(World.Entities.GetPresences(startvector, (float)distance));
            bool checkTerrain = true;
            for (float i = 0; i <= distance; i += 0.1f)
            {
                posToCheck += (dir * (i / (float)distance));
                if (checkTerrain && channel[(int)(posToCheck.X + startvector.X), (int)(posToCheck.Y + startvector.Y)] < posToCheck.Z)
                {
                    ContactResult result = new ContactResult();
                    result.ConsumerID = 0;
                    result.Depth = 0;
                    result.Normal = Vector3.Zero;
                    result.Pos = posToCheck;
                    results.Add(result);
                    checkTerrain = false;
                }
                for(int presenceCount = 0; i < presences.Count; i++)
                {
                    IScenePresence sp = presences[presenceCount];
                    if (sp.AbsolutePosition.ApproxEquals(posToCheck, sp.PhysicsActor.Size.X * 2))
                    {
                        ContactResult result = new ContactResult();
                        result.ConsumerID = sp.LocalId;
                        result.Depth = 0;
                        result.Normal = Vector3.Zero;
                        result.Pos = posToCheck;
                        results.Add(result);
                        presences.RemoveAt(presenceCount);
                        i--; //Reset its position since we removed this one
                    }
                }
            }
            foreach (ContactResult result in results)
            {
                if ((rejectTypes & ScriptBaseClass.RC_REJECT_LAND) == ScriptBaseClass.RC_REJECT_LAND &&
                    result.ConsumerID == 0)
                    continue;

                IEntity entity = World.GetSceneObjectPart(result.ConsumerID);
                if (entity == null && (rejectTypes & ScriptBaseClass.RC_REJECT_AGENTS) != ScriptBaseClass.RC_REJECT_AGENTS)
                    entity = World.GetScenePresence(result.ConsumerID); //Only check if we should be looking for agents
                if (entity == null)
                {
                    list.Add(UUID.Zero);
                    if ((dataFlags & ScriptBaseClass.RC_GET_LINK_NUM) == ScriptBaseClass.RC_GET_LINK_NUM)
                        list.Add(0);
                    list.Add(result.Pos);
                    if ((dataFlags & ScriptBaseClass.RC_GET_NORMAL) == ScriptBaseClass.RC_GET_NORMAL)
                        list.Add(result.Normal);
                    continue; //Can't find it, so add UUID.Zero
                }

                /*if (detectPhantom == 0 && intersection.obj is ISceneChildEntity &&
                    ((ISceneChildEntity)intersection.obj).PhysActor == null)
                    continue;*/ //Can't do this ATM, physics engine knows only of non phantom objects

                if ((dataFlags & ScriptBaseClass.RC_GET_ROOT_KEY) == ScriptBaseClass.RC_GET_ROOT_KEY && entity is ISceneChildEntity)
                    list.Add(((ISceneChildEntity)entity).ParentEntity.UUID);
                else
                    list.Add(entity.UUID);

                if ((dataFlags & ScriptBaseClass.RC_GET_LINK_NUM) == ScriptBaseClass.RC_GET_LINK_NUM)
                    if (entity is ISceneChildEntity)
                        list.Add(((ISceneChildEntity)entity).LinkNum);
                    else
                        list.Add(0);

                list.Add(result.Pos);
                if ((dataFlags & ScriptBaseClass.RC_GET_NORMAL) == ScriptBaseClass.RC_GET_NORMAL)
                    list.Add(result.Normal);
            }

            list.Add(results.Count); //The status code, either the # of contacts, RCERR_SIM_PERF_LOW, or RCERR_CAST_TIME_EXCEEDED

            return list;
        }

        public LSL_Key llGetNumberOfNotecardLines(string name)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            
            TaskInventoryDictionary itemsDictionary = (TaskInventoryDictionary)m_host.TaskInventory.Clone();

            UUID assetID = UUID.Zero;

            if (!UUID.TryParse(name, out assetID))
            {
                foreach (TaskInventoryItem item in itemsDictionary.Values)
                {
                    if (item.Type == 7 && item.Name == name)
                    {
                        assetID = item.AssetID;
                        break;
                    }
                }
            }

            if (assetID == UUID.Zero)
            {
                // => complain loudly, as specified by the LSL docs
                ShoutError("Notecard '" + name + "' could not be found.");

                return (LSL_Key)UUID.Zero.ToString();
            }

            // was: UUID tid = tid = m_ScriptEngine.
            DataserverPlugin dataserverPlugin = (DataserverPlugin)m_ScriptEngine.GetScriptPlugin("Dataserver");
            UUID tid = dataserverPlugin.RegisterRequest(m_host.UUID, m_itemID, assetID.ToString());

            if (NotecardCache.IsCached(assetID))
            {
                dataserverPlugin.AddReply(assetID.ToString(),
                NotecardCache.GetLines(assetID).ToString(), 100);
                ScriptSleep(100);
                return (LSL_Key) tid.ToString();
            }

            WithNotecard(assetID, delegate(UUID id, AssetBase a)
                {
                    if (a == null || a.Type != 7)
                        {
                        ShoutError("Notecard '" + name + "' could not be found.");
                        tid = UUID.Zero;
                        }
                    else
                        {
                        System.Text.UTF8Encoding enc =
                            new System.Text.UTF8Encoding();
                        string data = enc.GetString(a.Data);
                        //m_log.Debug(data);
                        NotecardCache.Cache(id, data);
                        dataserverPlugin.AddReply(assetID.ToString(),
                                NotecardCache.GetLines(id).ToString(), 100);
                        }
                });

            ScriptSleep(100);
            return tid.ToString();
        }

        public LSL_Key llRequestUsername(LSL_Key uuid)
        {
            UUID userID = UUID.Zero;

            if (!UUID.TryParse(uuid, out userID))
            {
                // => complain loudly, as specified by the LSL docs
                ShoutError("Failed to parse uuid for avatar.");

                return (LSL_Key)UUID.Zero.ToString();
            }

            DataserverPlugin dataserverPlugin = (DataserverPlugin)m_ScriptEngine.GetScriptPlugin("Dataserver");
            UUID tid = dataserverPlugin.RegisterRequest(m_host.UUID, m_itemID, uuid.ToString());

            Util.FireAndForget(delegate(object o)
            {
                string name = "";
                UserAccount info = World.UserAccountService.GetUserAccount(World.RegionInfo.ScopeID, userID);
                if (info != null)
                    name = info.Name;
                dataserverPlugin.AddReply(uuid.ToString(),
                    name, 100);
            });

            ScriptSleep(100);
            return (LSL_Key)tid.ToString();
        }

        public LSL_Key llRequestDisplayName(LSL_Key uuid)
        {
            UUID userID = UUID.Zero;

            if (!UUID.TryParse(uuid, out userID))
            {
                // => complain loudly, as specified by the LSL docs
                ShoutError("Failed to parse uuid for avatar.");

                return (LSL_Key)UUID.Zero.ToString();
            }

            DataserverPlugin dataserverPlugin = (DataserverPlugin)m_ScriptEngine.GetScriptPlugin("Dataserver");
            UUID tid = dataserverPlugin.RegisterRequest(m_host.UUID, m_itemID, uuid.ToString());

            Util.FireAndForget(delegate(object o)
            {
                string name = "";
                IProfileConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IProfileConnector>();
                if (connector != null)
                {
                    IUserProfileInfo info = connector.GetUserProfile(userID);
                    if (info != null)
                        name = info.DisplayName;
                }
                dataserverPlugin.AddReply(uuid.ToString(),
                    name, 100);
            });

            ScriptSleep(100);
            return (LSL_Key)tid.ToString();
        }

        public LSL_Key llGetNotecardLine(string name, int line)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL");
            
            
            TaskInventoryDictionary itemsDictionary = (TaskInventoryDictionary)m_host.TaskInventory.Clone();

            UUID assetID = UUID.Zero;

            if (!UUID.TryParse(name, out assetID))
            {
                foreach (TaskInventoryItem item in itemsDictionary.Values)
                {
                    if (item.Type == 7 && item.Name == name)
                    {
                        assetID = item.AssetID;
                        break;
                    }
                }
            }

            if (assetID == UUID.Zero)
            {
                // => complain loudly, as specified by the LSL docs
                ShoutError("Notecard '" + name + "' could not be found.");

                return (LSL_Key) UUID.Zero.ToString();
            }

            // was: UUID tid = tid = m_ScriptEngine.
            DataserverPlugin dataserverPlugin = (DataserverPlugin)m_ScriptEngine.GetScriptPlugin("Dataserver");
            UUID tid = dataserverPlugin.RegisterRequest(m_host.UUID, m_itemID, assetID.ToString());

            if (NotecardCache.IsCached(assetID))
            {
                dataserverPlugin.AddReply(assetID.ToString(),
                                                               NotecardCache.GetLine(assetID, line, m_notecardLineReadCharsMax), 100);
                ScriptSleep(100);
                return (LSL_Key)tid.ToString();
            }

            WithNotecard(assetID, delegate(UUID id, AssetBase a)
                 {
                     if (a == null || a.Type != 7)
                     {
                         ShoutError("Notecard '" + name + "' could not be found.");
                     }
                     else
                     {
                         System.Text.UTF8Encoding enc =
                             new System.Text.UTF8Encoding();
                         string data = enc.GetString(a.Data);
                         //m_log.Debug(data);
                         NotecardCache.Cache(id, data);
                         dataserverPlugin.AddReply(assetID.ToString(),
                            NotecardCache.GetLine(id, line, m_notecardLineReadCharsMax), 100);
                     }
                 });

            ScriptSleep(100);
            return (LSL_Key)tid.ToString();
        }

        public void SetPrimitiveParamsEx(LSL_Key prim, LSL_List rules)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osSetPrimitiveParams", m_host, "OSSL");
            ISceneChildEntity obj = World.GetSceneObjectPart (new UUID (prim));
            if (obj == null)
                return;

            if (obj.OwnerID != m_host.OwnerID)
                return;

            SetPrimParams(obj, rules);
        }

        public LSL_List GetLinkPrimitiveParamsEx(LSL_Key prim, LSL_List rules)
        {
            ISceneChildEntity obj = World.GetSceneObjectPart (new UUID (prim));
            if (obj == null)
                return new LSL_List();

            if (obj.OwnerID != m_host.OwnerID)
                return new LSL_List();

            return GetLinkPrimitiveParams(obj, rules);
        }

        public void print(string str)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Severe, "print", m_host, "LSL");

            if (m_ScriptEngine.Config.GetBoolean("AllowosConsoleCommand", false))
            {
                if (World.Permissions.CanRunConsoleCommand(m_host.OwnerID))
                {
                    // yes, this is a real LSL function. See: http://wiki.secondlife.com/wiki/Print
                    MainConsole.Instance.Output("LSL print():" + str, Level.Info);
                }
            }
        }
    }

    public class NotecardCache
    {
        protected class Notecard
        {
            public string[] text;
            public DateTime lastRef;
        }

        protected static Dictionary<UUID, Notecard> m_Notecards =
            new Dictionary<UUID, Notecard>();

        public static void Cache(UUID assetID, string text)
        {
            CacheCheck();

            lock (m_Notecards)
            {
                if (m_Notecards.ContainsKey(assetID))
                    return;

                Notecard nc = new Notecard();
                nc.lastRef = DateTime.Now;
                nc.text = SLUtil.ParseNotecardToList(text).ToArray();
                m_Notecards[assetID] = nc;
            }
        }

        public static bool IsCached(UUID assetID)
        {
            lock (m_Notecards)
            {
                return m_Notecards.ContainsKey(assetID);
            }
        }

        public static int GetLines(UUID assetID)
        {
            if (!IsCached(assetID))
                return -1;

            lock (m_Notecards)
            {
                m_Notecards[assetID].lastRef = DateTime.Now;
                return m_Notecards[assetID].text.Length;
            }
        }

        public static string GetLine(UUID assetID, int line, int maxLength)
        {
            if (line < 0)
                return "";

            string data;

            if (!IsCached(assetID))
                return "";

            lock (m_Notecards)
            {
                m_Notecards[assetID].lastRef = DateTime.Now;

                if (line >= m_Notecards[assetID].text.Length)
                    return "\n\n\n";

                data = m_Notecards[assetID].text[line];
                if (data.Length > maxLength)
                    data = data.Substring(0, maxLength);

                return data;
            }
        }

        public static void CacheCheck()
        {
            foreach (UUID key in new List<UUID>(m_Notecards.Keys))
            {
                Notecard nc = m_Notecards[key];
                if (nc.lastRef.AddSeconds(30) < DateTime.Now)
                    m_Notecards.Remove(key);
            }
        }
    }
}
