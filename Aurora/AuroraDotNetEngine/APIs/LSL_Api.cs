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
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Packets;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;
using Aurora.ScriptEngine.AuroraDotNetEngine.Plugins;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using Aurora.ScriptEngine.AuroraDotNetEngine.Runtime;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using PrimType = Aurora.Framework.PrimType;
using AssetLandmark = Aurora.Framework.AssetLandmark;

using LSL_Float = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLFloat;
using LSL_Integer = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLInteger;
using LSL_Key = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using LSL_List = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.list;
using LSL_Rotation = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.Quaternion;
using LSL_String = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using LSL_Vector = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.Vector3;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.APIs
{
    /// <summary>
    /// Contains all LSL ll-functions. This class will be in Default AppDomain.
    /// </summary>
    [Serializable]
    public class LSL_Api : MarshalByRefObject, ILSL_Api, IScriptApi
    {
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

        public void Initialize(IScriptModulePlugin ScriptEngine, ISceneChildEntity host, uint localID, UUID itemID, ScriptProtectionModule module)
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

            if (lease != null && lease.CurrentState == LeaseState.Initial)
            {
                lease.InitialLeaseTime = TimeSpan.FromMinutes(0);
                //                lease.RenewOnCallTime = TimeSpan.FromSeconds(10.0);
                //                lease.SponsorshipTimeout = TimeSpan.FromMinutes(1.0);
            }
            return lease;

        }

        protected virtual void ScriptSleep(int delay)
        {
            delay = (int)(delay * m_ScriptDelayFactor);
            if (delay == 0)
                return;

            Thread.Sleep(delay);
        }

        /// <summary>
        /// This is the new sleep implementation that allows for us to not freeze the script thread while we run
        /// </summary>
        /// <param name="delay"></param>
        /// <returns></returns>
        protected DateTime PScriptSleep(int delay)
        {
            double dly = (delay * m_ScriptDelayFactor);
            if (dly == 0.0)
                return DateTime.Now;

            DateTime timeToStopSleeping = DateTime.Now.AddMilliseconds(dly);
            return timeToStopSleeping;
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;
            m_ScriptEngine.ResetScript(m_host.UUID, m_itemID, true);
        }

        public void llResetOtherScript(string name)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;
            UUID item;



            if ((item = ScriptByName(name)) != UUID.Zero)
                m_ScriptEngine.ResetScript(m_host.UUID, item, false);
            else
                ShoutError("llResetOtherScript: script " + name + " not found");
        }


        public LSL_Integer llGetScriptState(string name)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID))
                return new LSL_Integer();
            UUID item;



            if ((item = ScriptByName(name)) != UUID.Zero)
            {
                return m_ScriptEngine.GetScriptRunningState(item) ? 1 : 0;
            }

            ShoutError("llGetScriptState: script " + name + " not found");

            // If we didn't find it, then it's safe to
            // assume it is not running.

            return 0;
        }

        public LSL_Key llGenerateKey()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Key();

            return UUID.Random().ToString();
        }

        public void llSetScriptState(string name, int run)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;
            UUID item;



            // These functions are supposed to be robust,
            // so get the state one step at a time.

            if ((item = ScriptByName(name)) != UUID.Zero)
            {
                m_ScriptEngine.SetScriptRunningState(item, run == 1);
            }
            else
            {
                ShoutError("llSetScriptState: script " + name + " not found");
            }
        }

        public List<ISceneChildEntity> GetLinkParts(int linkType)
        {
            List<ISceneChildEntity> ret = new List<ISceneChildEntity> { m_host };

            if (linkType == ScriptBaseClass.LINK_SET)
            {
                if (m_host.ParentEntity != null)
                    return new List<ISceneChildEntity>(m_host.ParentEntity.ChildrenEntities());
                return ret;
            }

            if (linkType == ScriptBaseClass.LINK_ROOT)
            {
                if (m_host.ParentEntity != null)
                {
                    ret = new List<ISceneChildEntity> { m_host.ParentEntity.RootChild };
                    return ret;
                }
                return ret;
            }

            if (linkType == ScriptBaseClass.LINK_ALL_OTHERS)
            {
                if (m_host.ParentEntity == null)
                    return new List<ISceneChildEntity>();
                ret = new List<ISceneChildEntity>(m_host.ParentEntity.ChildrenEntities());
                if (ret.Contains(m_host))
                    ret.Remove(m_host);
                return ret;
            }

            if (linkType == ScriptBaseClass.LINK_ALL_CHILDREN)
            {
                if (m_host.ParentEntity == null)
                    return new List<ISceneChildEntity>();
                ret = new List<ISceneChildEntity>(m_host.ParentEntity.ChildrenEntities());
                if (ret.Contains(m_host.ParentEntity.RootChild))
                    ret.Remove(m_host.ParentEntity.RootChild);
                return ret;
            }

            if (linkType == ScriptBaseClass.LINK_THIS)
            {
                return ret;
            }

            if (linkType < 0 || m_host.ParentEntity == null)
                return new List<ISceneChildEntity>();
            IEntity target = m_host.ParentEntity.GetLinkNumPart(linkType);
            if (target is ISceneChildEntity)
            {
                ret = new List<ISceneChildEntity> { target as ISceneChildEntity };
            }
            //No allowing scene presences to be found here
            return ret;
        }

        public List<IEntity> GetLinkPartsAndEntities(int linkType)
        {
            List<IEntity> ret = new List<IEntity> { m_host };

            if (linkType == ScriptBaseClass.LINK_SET)
            {
                if (m_host.ParentEntity != null)
                {
                    List<ISceneChildEntity> parts = new List<ISceneChildEntity>(m_host.ParentEntity.ChildrenEntities());
#if (!ISWIN)
                    return parts.ConvertAll<IEntity>(new Converter<ISceneChildEntity, IEntity>(delegate(ISceneChildEntity part)
                    {
                        return (IEntity)part;
                    }));
#else
                    return parts.ConvertAll (part => (IEntity) part);
#endif
                }
                return ret;
            }

            if (linkType == ScriptBaseClass.LINK_ROOT)
            {
                if (m_host.ParentEntity != null)
                {
                    ret = new List<IEntity> { m_host.ParentEntity.RootChild };
                    return ret;
                }
                return ret;
            }

            if (linkType == ScriptBaseClass.LINK_ALL_OTHERS)
            {
                if (m_host.ParentEntity == null)
                    return new List<IEntity>();
                List<ISceneChildEntity> sceneobjectparts = new List<ISceneChildEntity>(m_host.ParentEntity.ChildrenEntities());
#if (!ISWIN)
                ret = sceneobjectparts.ConvertAll<IEntity>(new Converter<ISceneChildEntity, IEntity>(delegate(ISceneChildEntity part)
                {
                    return (IEntity)part;
                }));
#else
                ret = sceneobjectparts.ConvertAll (part => (IEntity) part);
#endif
                if (ret.Contains(m_host))
                    ret.Remove(m_host);
                return ret;
            }

            if (linkType == ScriptBaseClass.LINK_ALL_CHILDREN)
            {
                if (m_host.ParentEntity == null)
                    return new List<IEntity>();
                List<ISceneChildEntity> children = new List<ISceneChildEntity>(m_host.ParentEntity.ChildrenEntities());
#if (!ISWIN)
                ret = children.ConvertAll<IEntity>(new Converter<ISceneChildEntity, IEntity>(delegate(ISceneChildEntity part)
                {
                    return (IEntity)part;
                }));
#else
                ret = children.ConvertAll (part => (IEntity) part);
#endif
                if (ret.Contains(m_host.ParentEntity.RootChild))
                    ret.Remove(m_host.ParentEntity.RootChild);
                return ret;
            }

            if (linkType == ScriptBaseClass.LINK_THIS)
            {
                return ret;
            }

            if (linkType < 0 || m_host.ParentEntity == null)
                return new List<IEntity>();
            IEntity target = m_host.ParentEntity.GetLinkNumPart(linkType);
            if (target == null)
                return new List<IEntity>();
            ret = new List<IEntity> { target };

            return ret;
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

        protected UUID InventoryKey(string name, bool throwExceptionIfDoesNotExist)
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

            if (throwExceptionIfDoesNotExist)
            {
                IChatModule chatModule = World.RequestModuleInterface<IChatModule>();
                if (chatModule != null)
                    chatModule.SimChat("Could not find sound '" + name + "'.",
                        ChatTypeEnum.DebugChannel, 2147483647, m_host.AbsolutePosition,
                        m_host.Name, m_host.UUID, false, World);
            }

            return UUID.Zero;
        }


        /// <summary>
        /// accepts a valid UUID, -or- a name of an inventory item.
        /// Returns a valid UUID or UUID.Zero if key invalid and item not found
        /// in prim inventory.
        /// </summary>
        /// <param name="k"></param>
        /// <param name="throwExceptionIfDoesNotExist"></param>
        /// <returns></returns>
        protected UUID KeyOrName(string k, bool throwExceptionIfDoesNotExist)
        {
            UUID key = UUID.Zero;

            // if we can parse the string as a key, use it.
            if (UUID.TryParse(k, out key))
            {
                return key;
            }
            // else try to locate the name in inventory of object. found returns key,
            // not found returns UUID.Zero which will translate to the default particle texture
            return InventoryKey(k, throwExceptionIfDoesNotExist);
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            return Math.Sin(f);
        }

        public LSL_Float llCos(double f)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            return Math.Cos(f);
        }

        public LSL_Float llTan(double f)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            return Math.Tan(f);
        }

        public LSL_Float llAtan2(double x, double y)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            return Math.Atan2(x, y);
        }

        public LSL_Float llSqrt(double f)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            return Math.Sqrt(f);
        }

        public LSL_Float llPow(double fbase, double fexponent)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            return Math.Pow(fbase, fexponent);
        }

        public LSL_Integer llAbs(int i)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Integer();
            // changed to replicate LSL behaviour whereby minimum int value is returned untouched.

            if (i == Int32.MinValue)
                return i;
            return Math.Abs(i);
        }

        public LSL_Float llFabs(double f)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            return Math.Abs(f);
        }

        public LSL_Float llFrand(double mag)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            lock (Util.RandomClass)
            {
                return Util.RandomClass.NextDouble() * mag;
            }
        }

        public LSL_Integer llFloor(double f)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Integer();

            return (int)Math.Floor(f);
        }

        public LSL_Integer llCeil(double f)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Integer();

            return (int)Math.Ceiling(f);
        }

        // Xantor 01/May/2008 fixed midpointrounding (2.5 becomes 3.0 instead of 2.0, default = ToEven)
        public LSL_Integer llRound(double f)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Integer();

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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            return LSL_Vector.Mag(v);
        }

        public LSL_Vector llVecNorm(LSL_Vector v)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();
            return LSL_Vector.Norm(v);
        }

        public LSL_Float llVecDist(LSL_Vector a, LSL_Vector b)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

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
            if (n > 0)
                return new LSL_Vector(0.0, Math.PI * 0.5, Math.Atan2((r.z * r.s + r.x * r.y), 0.5 - t.x - t.z));
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Rotation();


            double c1 = Math.Cos(v.x * 0.5);
            double c2 = Math.Cos(v.y * 0.5);
            double c3 = Math.Cos(v.z * 0.5);
            double s1 = Math.Sin(v.x * 0.5);
            double s2 = Math.Sin(v.y * 0.5);
            double s3 = Math.Sin(v.z * 0.5);

            double x = s1 * c2 * c3 + c1 * s2 * s3;
            double y = c1 * s2 * c3 - s1 * c2 * s3;
            double z = s1 * s2 * c3 + c1 * c2 * s3;
            double s = c1 * c2 * c3 - s1 * s2 * s3;

            return new LSL_Rotation(x, y, z, s);
        }

        public LSL_Rotation llAxes2Rot(LSL_Vector fwd, LSL_Vector left, LSL_Vector up)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Rotation();

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
            if (max == left.y)
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
            s = Math.Sqrt(up.z - (fwd.x + left.y) + 1.0);
            double z = s * 0.5;
            s = 0.5 / s;
            return new LSL_Rotation(
                (up.x + fwd.z) * s,
                (left.z + up.y) * s,
                z,
                (fwd.y - left.x) * s);
        }

        public LSL_Vector llRot2Fwd(LSL_Rotation r)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID))
                return new LSL_Vector();


            double m = r.x * r.x + r.y * r.y + r.z * r.z + r.s * r.s;
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
            double x = r.x * r.x - r.y * r.y - r.z * r.z + r.s * r.s;
            double y = 2 * (r.x * r.y + r.z * r.s);
            double z = 2 * (r.x * r.z - r.y * r.s);
            return (new LSL_Vector(x, y, z));
        }

        public LSL_Vector llRot2Left(LSL_Rotation r)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();


            double m = r.x * r.x + r.y * r.y + r.z * r.z + r.s * r.s;
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
            double x = 2 * (r.x * r.y - r.z * r.s);
            double y = -r.x * r.x + r.y * r.y - r.z * r.z + r.s * r.s;
            double z = 2 * (r.x * r.s + r.y * r.z);
            return (new LSL_Vector(x, y, z));
        }

        public LSL_Vector llRot2Up(LSL_Rotation r)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

            double m = r.x * r.x + r.y * r.y + r.z * r.z + r.s * r.s;
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
            double x = 2 * (r.x * r.z + r.y * r.s);
            double y = 2 * (-r.x * r.s + r.y * r.z);
            double z = -r.x * r.x - r.y * r.y + r.z * r.z + r.s * r.s;
            return (new LSL_Vector(x, y, z));
        }

        public LSL_Rotation llRotBetween(LSL_Vector a, LSL_Vector b)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Rotation();
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
                    rotBetween = LSL_Vector.Mag(orthoVector) > 0.0001 ? new LSL_Rotation(orthoVector.x, orthoVector.y, orthoVector.z, 0.0f) : new LSL_Rotation(0.0f, 0.0f, 1.0f, 0.0f);
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;
            if (text.Length > 1023)
                text = text.Substring(0, 1023);

            if (channelID == 0) //0 isn't normally allowed, so check against a higher threat level
                if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "LSL", m_host, "LSL", m_itemID)) return;
            
            IChatModule chatModule = World.RequestModuleInterface<IChatModule>();
            if (chatModule != null)
                chatModule.SimChat(text, ChatTypeEnum.Region, channelID,
                    m_host.ParentEntity.RootChild.AbsolutePosition, m_host.Name, m_host.UUID, false, World);
            
            if (m_comms != null)
                m_comms.DeliverMessage(ChatTypeEnum.Region, channelID, m_host.Name, m_host.UUID, text);
        }

        public void llRegionSayTo(LSL_Key toID, int channelID, string text)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            IChatModule chatModule = World.RequestModuleInterface<IChatModule>();

            if (text.Length > 1023)
                text = text.Substring(0, 1023);
            if (channelID == 0)
            {
                IScenePresence presence = World.GetScenePresence(UUID.Parse(toID.m_string));
                if (presence != null)
                {
                    if (chatModule != null)
                        chatModule.TrySendChatMessage(presence, m_host.AbsolutePosition, m_host.AbsolutePosition,
                            m_host.UUID, m_host.Name, ChatTypeEnum.Say, text, ChatSourceType.Object, 10000);
                }
            }

            if (m_comms != null)
                m_comms.DeliverMessage(ChatTypeEnum.Region, channelID, m_host.Name, m_host.UUID, UUID.Parse(toID.m_string), text);
        }

        public LSL_Integer llListen(int channelID, string name, string ID, string msg)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Integer();

            UUID keyID;
            UUID.TryParse(ID, out keyID);
            if (m_comms != null)
                return m_comms.Listen(m_itemID, m_host.UUID, channelID, name, keyID, msg);
            return -1;
        }

        public void llListenControl(int number, int active)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            if (m_comms != null)
                m_comms.ListenControl(m_itemID, number, active);
        }

        public void llListenRemove(int number)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            if (m_comms != null)
                m_comms.ListenRemove(m_itemID, number);
        }

        public void llSensor(string name, string id, int type, double range, double arc)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            UUID keyID = UUID.Zero;
            UUID.TryParse(id, out keyID);
            SensorRepeatPlugin sensorPlugin = (SensorRepeatPlugin)m_ScriptEngine.GetScriptPlugin("SensorRepeat");
            sensorPlugin.SenseOnce(m_host.UUID, m_itemID, name, keyID, type, range, arc, m_host);
        }

        public void llSensorRepeat(string name, string id, int type, double range, double arc, double rate)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            UUID keyID = UUID.Zero;
            UUID.TryParse(id, out keyID);

            SensorRepeatPlugin sensorPlugin = (SensorRepeatPlugin)m_ScriptEngine.GetScriptPlugin("SensorRepeat");
            sensorPlugin.SetSenseRepeatEvent(m_host.UUID, m_itemID, name, keyID, type, range, arc, rate, m_host);
        }

        public void llSensorRemove()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            SensorRepeatPlugin sensorPlugin = (SensorRepeatPlugin)m_ScriptEngine.GetScriptPlugin("SensorRepeat");
            sensorPlugin.RemoveScript(m_host.UUID, m_itemID);
        }

        public string resolveName(UUID objecUUID)
        {
            // try avatar username surname
            UserAccount account = World.UserAccountService.GetUserAccount(World.RegionInfo.AllScopeIDs, objecUUID);
            if (account != null)
                return account.Name;

            // try an scene object
            ISceneChildEntity SOP = World.GetSceneObjectPart(objecUUID);
            if (SOP != null)
                return SOP.Name;

            IEntity SensedObject;
            if (!World.Entities.TryGetValue(objecUUID, out SensedObject))
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_String();

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, number);
            if (detectedParams == null)
                return String.Empty;
            return detectedParams.Name;
        }

        public LSL_String llDetectedKey(int number)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_String();

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, number);
            if (detectedParams == null)
                return String.Empty;
            return detectedParams.Key.ToString();
        }

        public LSL_String llDetectedOwner(int number)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_String();

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, number);
            if (detectedParams == null)
                return String.Empty;
            return detectedParams.Owner.ToString();
        }

        public LSL_Integer llDetectedType(int number)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Integer();

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, number);
            if (detectedParams == null)
                return 0;
            return new LSL_Integer(detectedParams.Type);
        }

        public LSL_Vector llDetectedPos(int number)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, number);
            if (detectedParams == null)
                return new LSL_Vector();
            return detectedParams.Position;
        }

        public LSL_Vector llDetectedVel(int number)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, number);
            if (detectedParams == null)
                return new LSL_Vector();
            return detectedParams.Velocity;
        }

        public LSL_Vector llDetectedGrab(int number)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

            DetectParams parms = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, number);
            if (parms == null)
                return new LSL_Vector(0, 0, 0);

            return parms.OffsetPos;
        }

        public LSL_Rotation llDetectedRot(int number)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Rotation();

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, number);
            if (detectedParams == null)
                return new LSL_Rotation();
            return detectedParams.Rotation;
        }

        public LSL_Integer llDetectedGroup(int number)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Integer();

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, number);
            if (detectedParams == null)
                return new LSL_Integer(0);
            if (m_host.GroupID == detectedParams.Group)
                return new LSL_Integer(1);
            return new LSL_Integer(0);
        }

        public LSL_Integer llDetectedLinkNumber(int number)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Integer();

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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Integer();

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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, index);
            if (detectedParams == null)
                return new LSL_Vector(-1.0, -1.0, 0.0);
            return detectedParams.TouchUV;
        }

        public virtual void llDie()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            throw new SelfDeleteException();
        }

        public LSL_Float llGround(LSL_Vector offset)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

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
            float xdiff = pos.X - (int)pos.X;
            float ydiff = pos.Y - (int)pos.Y;

            //Use the equation of the tangent plane to adjust the height to account for slope

            return (((vsn.x * xdiff) + (vsn.y * ydiff)) / (-1 * vsn.z)) + baseheight;
        }

        public LSL_Float llCloud(LSL_Vector offset)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


            int statusrotationaxis = 0;

            if ((status & ScriptBaseClass.STATUS_PHYSICS) == ScriptBaseClass.STATUS_PHYSICS)
            {
                if (value != 0)
                {
                    ISceneEntity group = m_host.ParentEntity;
                    if (group == null)
                        return;
#if (!ISWIN)
                    bool allow = true;
                    foreach (ISceneChildEntity part in group.ChildrenEntities())
                    {
                        IOpenRegionSettingsModule WSModule = group.Scene.RequestModuleInterface<IOpenRegionSettingsModule>();
                        if (WSModule != null && WSModule.MaximumPhysPrimScale != -1)
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
#else
                    bool allow = !(from part in @group.ChildrenEntities()
                                   let WSModule = @group.Scene.RequestModuleInterface<IOpenRegionSettingsModule>()
                                   where WSModule != null && WSModule.MaximumPhysPrimScale != -1
                                   let tmp = part.Scale
                                   where
                                       tmp.X > WSModule.MaximumPhysPrimScale || tmp.Y > WSModule.MaximumPhysPrimScale ||
                                       tmp.Z > WSModule.MaximumPhysPrimScale
                                   select WSModule).Any();
#endif


                    if (!allow)
                        return;
                    ((SceneObjectGroup)m_host.ParentEntity).ScriptSetPhysicsStatus(true);
                }
                else
                {
                    ((SceneObjectGroup)m_host.ParentEntity).ScriptSetPhysicsStatus(false);
                }
            }

            if ((status & ScriptBaseClass.STATUS_PHANTOM) == ScriptBaseClass.STATUS_PHANTOM)
            {
                m_host.ScriptSetPhantomStatus(value != 0);
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
                m_host.SetBlockGrab(value != 0, false);
            }

            if ((status & ScriptBaseClass.STATUS_BLOCK_GRAB_OBJECT) == ScriptBaseClass.STATUS_BLOCK_GRAB_OBJECT)
            {
                m_host.SetBlockGrab(value != 0, true);
            }

            if ((status & ScriptBaseClass.STATUS_DIE_AT_EDGE) == ScriptBaseClass.STATUS_DIE_AT_EDGE)
            {
                m_host.SetDieAtEdge(value != 0);
            }

            if ((status & ScriptBaseClass.STATUS_RETURN_AT_EDGE) == ScriptBaseClass.STATUS_RETURN_AT_EDGE)
            {
                m_host.SetReturnAtEdge(value != 0);
            }

            if ((status & ScriptBaseClass.STATUS_SANDBOX) == ScriptBaseClass.STATUS_SANDBOX)
            {
                m_host.SetStatusSandbox(value != 0);
            }

            if (statusrotationaxis != 0)
            {
                m_host.SetAxisRotation(statusrotationaxis, value);
            }
        }

        public LSL_Integer llGetStatus(int status)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Integer();

            if (status == ScriptBaseClass.STATUS_PHYSICS)
            {
                return (m_host.GetEffectiveObjectFlags() & (uint)PrimFlags.Physics) == (uint)PrimFlags.Physics ? new LSL_Integer(1) : new LSL_Integer(0);
            }

            if (status == ScriptBaseClass.STATUS_PHANTOM)
            {
                return (m_host.GetEffectiveObjectFlags() & (uint)PrimFlags.Phantom) == (uint)PrimFlags.Phantom ? new LSL_Integer(1) : new LSL_Integer(0);
            }

            if (status == ScriptBaseClass.STATUS_CAST_SHADOWS)
            {
                if ((m_host.GetEffectiveObjectFlags() & (uint)PrimFlags.CastShadows) == (uint)PrimFlags.CastShadows)
                    return new LSL_Integer(1);
                return new LSL_Integer(0);
            }
            if (status == ScriptBaseClass.STATUS_BLOCK_GRAB)
            {
                return m_host.GetBlockGrab(false) ? new LSL_Integer(1) : new LSL_Integer(0);
            }

            if (status == ScriptBaseClass.STATUS_BLOCK_GRAB_OBJECT)
            {
                return m_host.GetBlockGrab(true) ? new LSL_Integer(1) : new LSL_Integer(0);
            }

            if (status == ScriptBaseClass.STATUS_DIE_AT_EDGE)
            {
                return m_host.GetDieAtEdge() ? new LSL_Integer(1) : new LSL_Integer(0);
            }

            if (status == ScriptBaseClass.STATUS_RETURN_AT_EDGE)
            {
                return m_host.GetReturnAtEdge() ? new LSL_Integer(1) : new LSL_Integer(0);
            }

            if (status == ScriptBaseClass.STATUS_ROTATE_X)
            {
                return m_host.GetAxisRotation(2) == 2 ? new LSL_Integer(1) : new LSL_Integer(0);
            }

            if (status == ScriptBaseClass.STATUS_ROTATE_Y)
            {
                return m_host.GetAxisRotation(4) == 4 ? new LSL_Integer(1) : new LSL_Integer(0);
            }

            if (status == ScriptBaseClass.STATUS_ROTATE_Z)
            {
                return m_host.GetAxisRotation(8) == 8 ? new LSL_Integer(1) : new LSL_Integer(0);
            }

            if (status == ScriptBaseClass.STATUS_SANDBOX)
            {
                return m_host.GetStatusSandbox() ? new LSL_Integer(1) : new LSL_Integer(0);
            }
            return new LSL_Integer(0);
        }

        public void llSetScale(LSL_Vector scale)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            SetScale(m_host, scale);
        }

        protected void SetScale(ISceneChildEntity part, LSL_Vector scale)
        {
            if (part == null || part.ParentEntity == null || part.ParentEntity.IsDeleted)
                return;

            IOpenRegionSettingsModule WSModule = m_host.ParentEntity.Scene.RequestModuleInterface<IOpenRegionSettingsModule>();
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
        }

        public LSL_Vector llGetScale()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();
            Vector3 tmp = m_host.Scale;
            return new LSL_Vector(tmp.X, tmp.Y, tmp.Z);
        }

        public void llSetClickAction(int action)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.ClickAction = (byte)action;
            m_host.ScheduleUpdate(PrimUpdateFlags.FindBest);
        }

        public void llSetColor(LSL_Vector color, int face)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


            if (face == ScriptBaseClass.ALL_SIDES)
                face = SceneObjectPart.ALL_SIDES;

            m_host.SetFaceColor(new Vector3((float)color.x, (float)color.y, (float)color.z), face);
        }

        public void SetTexGen(ISceneChildEntity part, int face, int style)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            MappingType textype = MappingType.Default;
            if (style == (int)ScriptBaseClass.PRIM_TEXGEN_PLANAR)
                textype = MappingType.Planar;

            if (face >= 0 && face < GetNumberOfSides(part))
            {
                tex.CreateFace((uint)face);
                tex.FaceTextures[face].TexMapType = textype;
                part.UpdateTexture(tex, false);
                return;
            }
            if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (uint i = 0; i < GetNumberOfSides(part); i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].TexMapType = textype;
                    }
                    tex.DefaultTexture.TexMapType = textype;
                }
                part.UpdateTexture(tex, false);
            }
        }

        public void SetGlow(ISceneChildEntity part, int face, float glow)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            if (face >= 0 && face < GetNumberOfSides(part))
            {
                tex.CreateFace((uint)face);
                tex.FaceTextures[face].Glow = glow;
                part.UpdateTexture(tex, false);
                return;
            }
            if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (uint i = 0; i < GetNumberOfSides(part); i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].Glow = glow;
                    }
                    tex.DefaultTexture.Glow = glow;
                }
                part.UpdateTexture(tex, false);
            }
        }

        public void SetShiny(ISceneChildEntity part, int face, int shiny, Bumpiness bump)
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
                tex.CreateFace((uint)face);
                tex.FaceTextures[face].Shiny = sval;
                tex.FaceTextures[face].Bump = bump;
                part.UpdateTexture(tex, false);
                return;
            }
            if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (uint i = 0; i < GetNumberOfSides(part); i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].Shiny = sval;
                        tex.FaceTextures[i].Bump = bump;
                    }
                    tex.DefaultTexture.Shiny = sval;
                    tex.DefaultTexture.Bump = bump;
                }
                part.UpdateTexture(tex, false);
            }
        }

        public void SetFullBright(ISceneChildEntity part, int face, bool bright)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            if (face >= 0 && face < GetNumberOfSides(part))
            {
                tex.CreateFace((uint)face);
                tex.FaceTextures[face].Fullbright = bright;
                part.UpdateTexture(tex, false);
                return;
            }
            if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (uint i = 0; i < GetNumberOfSides(part); i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].Fullbright = bright;
                    }
                }
                tex.DefaultTexture.Fullbright = bright;
                part.UpdateTexture(tex, false);
            }
        }

        public LSL_Float llGetAlpha(int face)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();


            return GetAlpha(m_host, face);
        }

        protected LSL_Float GetAlpha(ISceneChildEntity part, int face)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            if (face == ScriptBaseClass.ALL_SIDES)
            {
                int i;
                double sum = 0.0;
                for (i = 0; i < GetNumberOfSides(part); i++)
                    sum += tex.GetFace((uint)i).RGBA.A;
                return sum;
            }
            if (face >= 0 && face < GetNumberOfSides(part))
            {
                return tex.GetFace((uint)face).RGBA.A;
            }
            return 0.0;
        }

        public void llSetAlpha(double alpha, int face)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


            SetAlpha(m_host, alpha, face);
        }

        public void llSetLinkAlpha(int linknumber, double alpha, int face)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


            List<ISceneChildEntity> parts = GetLinkParts(linknumber);

            foreach (ISceneChildEntity part in parts)
                SetAlpha(part, alpha, face);
        }

        protected void SetAlpha(ISceneChildEntity part, double alpha, int face)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            Color4 texcolor;
            bool changed = false;
            if (face >= 0 && face < GetNumberOfSides(part))
            {
                texcolor = tex.CreateFace((uint)face).RGBA;
                if (texcolor.A != alpha)
                    changed = true;
                texcolor.A = Util.Clip((float)alpha, 0.0f, 1.0f);
                tex.FaceTextures[face].RGBA = texcolor;
                if (changed)
                    part.UpdateTexture(tex, false);
            }
            else if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (int i = 0; i < GetNumberOfSides(part); i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        texcolor = tex.FaceTextures[i].RGBA;
                        if (texcolor.A != alpha)
                            changed = true;
                        texcolor.A = Util.Clip((float)alpha, 0.0f, 1.0f);
                        tex.FaceTextures[i].RGBA = texcolor;
                    }
                }
                texcolor = tex.DefaultTexture.RGBA;
                if (texcolor.A != alpha)
                    changed = true;
                texcolor.A = Util.Clip((float)alpha, 0.0f, 1.0f);
                tex.DefaultTexture.RGBA = texcolor;
                if (changed)
                    part.UpdateTexture(tex, false);
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
        protected void SetFlexi(ISceneChildEntity part, bool flexi, int softness, float gravity, float friction,
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


            part.ParentEntity.HasGroupChanged = true;
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
        protected void SetPointLight(ISceneChildEntity part, bool light, LSL_Vector color, float intensity, float radius, float falloff)
        {
            if (part == null)
                return;

            bool same = true;
            if (light)
            {
                if (part.Shape.LightEntry != true)
                    same = false;
                part.Shape.LightEntry = true;
                if (part.Shape.LightColorR != Util.Clip((float)color.x, 0.0f, 1.0f))
                    same = false;
                part.Shape.LightColorR = Util.Clip((float)color.x, 0.0f, 1.0f);
                if (part.Shape.LightColorG != Util.Clip((float)color.y, 0.0f, 1.0f))
                    same = false;
                part.Shape.LightColorG = Util.Clip((float)color.y, 0.0f, 1.0f);
                if (part.Shape.LightColorB != Util.Clip((float)color.z, 0.0f, 1.0f))
                    same = false;
                part.Shape.LightColorB = Util.Clip((float)color.z, 0.0f, 1.0f);
                if (part.Shape.LightIntensity != intensity)
                    same = false;
                part.Shape.LightIntensity = intensity;
                if (part.Shape.LightRadius != radius)
                    same = false;
                part.Shape.LightRadius = radius;
                if (part.Shape.LightFalloff != falloff)
                    same = false;
                part.Shape.LightFalloff = falloff;
            }
            else
            {
                if (part.Shape.LightEntry)
                    same = false;
                part.Shape.LightEntry = false;
            }

            if (!same)
            {
                part.ParentEntity.HasGroupChanged = true;
                part.ScheduleUpdate(PrimUpdateFlags.FindBest);
            }
        }

        public LSL_Vector llGetColor(int face)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

            return GetColor(m_host, face);
        }

        protected LSL_Vector GetColor(ISceneChildEntity part, int face)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            Color4 texcolor;
            LSL_Vector rgb = new LSL_Vector();
            int ns = GetNumberOfSides(part);
            if (face == ScriptBaseClass.ALL_SIDES)
            {
                int i;

                for (i = 0; i < ns; i++)
                {
                    texcolor = tex.GetFace((uint)i).RGBA;
                    rgb.x += texcolor.R;
                    rgb.y += texcolor.G;
                    rgb.z += texcolor.B;
                }

                float tmp = 1f / ns;
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
            return new LSL_Vector();
        }

        public DateTime llSetTexture(string texture, int face)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            bool found = SetTexture(m_host, texture, face);
            if (!found)
                ShoutError("Could not find texture '" + texture + "'");
            return PScriptSleep(200);
        }

        public DateTime llSetLinkTexture(int linknumber, string texture, int face)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;


            List<ISceneChildEntity> parts = GetLinkParts(linknumber);

            foreach (ISceneChildEntity part in parts)
                SetTexture(part, texture, face);

            return PScriptSleep(100);
        }

        protected bool SetTexture(ISceneChildEntity part, string texture, int face)
        {
            UUID textureID = new UUID();
            int ns = GetNumberOfSides(part);

            textureID = InventoryKey(texture, (int)AssetType.Texture);
            if (textureID == UUID.Zero)
            {
                if (!UUID.TryParse(texture, out textureID))
                    return false;
            }

            Primitive.TextureEntry tex = part.Shape.Textures;

            if (face >= 0 && face < ns)
            {
                Primitive.TextureEntryFace texface = tex.CreateFace((uint)face);
                texface.TextureID = textureID;
                tex.FaceTextures[face] = texface;
                part.UpdateTexture(tex, false);
            }
            if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (uint i = 0; i < ns; i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].TextureID = textureID;
                    }
                }
                tex.DefaultTexture.TextureID = textureID;
                part.UpdateTexture(tex, false);
            }
            return true;
        }

        public DateTime llScaleTexture(double u, double v, int face)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;


            ScaleTexture(m_host, u, v, face);
            return PScriptSleep(200);
        }

        protected void ScaleTexture(ISceneChildEntity part, double u, double v, int face)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            int ns = GetNumberOfSides(part);
            if (face >= 0 && face < ns)
            {
                Primitive.TextureEntryFace texface = tex.CreateFace((uint)face);
                texface.RepeatU = (float)u;
                texface.RepeatV = (float)v;
                tex.FaceTextures[face] = texface;
                part.UpdateTexture(tex, false);
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
                part.UpdateTexture(tex, false);
            }
        }

        public DateTime llOffsetTexture(double u, double v, int face)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            OffsetTexture(m_host, u, v, face);
            return PScriptSleep(200);
        }

        protected void OffsetTexture(ISceneChildEntity part, double u, double v, int face)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            int ns = GetNumberOfSides(part);
            if (face >= 0 && face < ns)
            {
                Primitive.TextureEntryFace texface = tex.CreateFace((uint)face);
                texface.OffsetU = (float)u;
                texface.OffsetV = (float)v;
                tex.FaceTextures[face] = texface;
                part.UpdateTexture(tex, false);
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
                part.UpdateTexture(tex, false);
            }
        }

        public DateTime llRotateTexture(double rotation, int face)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            RotateTexture(m_host, rotation, face);
            return PScriptSleep(200);
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
                part.UpdateTexture(tex, false);
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
                part.UpdateTexture(tex, false);
                return;
            }
        }

        public LSL_String llGetTexture(int face)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_String();

            return GetTexture(m_host, face);
        }

        protected LSL_String GetTexture(ISceneChildEntity part, int face)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;

            if (face == ScriptBaseClass.ALL_SIDES)
            {
                face = 0;
            }
            if (face >= 0 && face < GetNumberOfSides(part))
            {
                Primitive.TextureEntryFace texface = tex.GetFace((uint)face);
                TaskInventoryItem item = null;
                m_host.TaskInventory.TryGetValue(texface.TextureID, out item);
                if (item != null)
                    return item.Name.ToString();
                return texface.TextureID.ToString();
            }
            return String.Empty;
        }

        public DateTime llSetPos(LSL_Vector pos)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;


            SetPos(m_host, pos, true);

            return PScriptSleep(200);
        }

        public LSL_Integer llSetRegionPos(LSL_Vector pos)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return ScriptBaseClass.FALSE;


            SetPos(m_host, pos, false);

            return ScriptBaseClass.TRUE;
        }

        // Capped movemment if distance > 10m (http://wiki.secondlife.com/wiki/LlSetPos)
        // note linked setpos is capped "differently"
        private LSL_Vector SetPosAdjust(LSL_Vector start, LSL_Vector end)
        {
            if (llVecDist(start, end) > 10.0f * m_ScriptDistanceFactor)
                return start + m_ScriptDistanceFactor * 10.0f * llVecNorm(end - start);
            return end;
        }

        protected void SetPos(ISceneChildEntity part, LSL_Vector targetPos, bool checkPos)
        {
            // Capped movemment if distance > 10m (http://wiki.secondlife.com/wiki/LlSetPos)
            LSL_Vector currentPos = GetPartLocalPos(part);
            float ground = 0;
            bool disable_underground_movement = m_ScriptEngine.Config.GetBoolean("DisableUndergroundMovement", true);

            ITerrainChannel heightmap = World.RequestModuleInterface<ITerrainChannel>();
            if (heightmap != null)
                ground = heightmap.GetNormalizedGroundHeight((int)(float)targetPos.x, (int)(float)targetPos.y);
            if (part.ParentEntity == null)
                return;
            if (part.ParentEntity.RootChild == part)
            {
                ISceneEntity parent = part.ParentEntity;
                if (!part.IsAttachment)
                {
                    if (ground != 0 && (targetPos.z < ground) && disable_underground_movement)
                        targetPos.z = ground;
                }
                LSL_Vector real_vec = checkPos ? SetPosAdjust(currentPos, targetPos) : targetPos;
                parent.UpdateGroupPosition(new Vector3((float)real_vec.x, (float)real_vec.y, (float)real_vec.z), true);
            }
            else
            {
                LSL_Vector rel_vec = checkPos ? SetPosAdjust(currentPos, targetPos) : targetPos;
                part.FixOffsetPosition((new Vector3((float)rel_vec.x, (float)rel_vec.y, (float)rel_vec.z)), true);
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
            if (m_host.IsRoot)
            {
                tmp = m_host.AttachedPos;
                return new LSL_Vector(tmp.X,
                                      tmp.Y,
                                      tmp.Z);
            }
            tmp = part.OffsetPosition;
            return new LSL_Vector(tmp.X,
                                  tmp.Y,
                                  tmp.Z);
        }

        public LSL_Vector llGetPos()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

            Vector3 pos = m_host.GetWorldPosition();
            return new LSL_Vector(pos.X, pos.Y, pos.Z);
        }

        public LSL_Vector llGetLocalPos()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();
            return GetLocalPos(m_host);
        }

        private LSL_Vector GetLocalPos(ISceneChildEntity entity)
        {
            Vector3 tmp;
            if (entity.ParentID != 0)
            {
                tmp = entity.OffsetPosition;
                return new LSL_Vector(tmp.X,
                                      tmp.Y,
                                      tmp.Z);
            }
            tmp = entity.AbsolutePosition;
            return new LSL_Vector(tmp.X,
                                  tmp.Y,
                                  tmp.Z);
        }

        public DateTime llSetRot(LSL_Rotation rot)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;


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
            return PScriptSleep(200);
        }

        public DateTime llSetLocalRot(LSL_Rotation rot)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            SetRot(m_host, Rot2Quaternion(rot));
            return PScriptSleep(200);
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
                part.ParentEntity.ResetChildPrimPhysicsPositions();
            }
        }

        /// <summary>
        /// See http://lslwiki.net/lslwiki/wakka.php?wakka=ChildRotation
        /// </summary>
        public LSL_Rotation llGetRot()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Rotation();
            // unlinked or root prim then use llRootRotation
            // see llRootRotaion for references.
            if (m_host.LinkNum == 0 || m_host.LinkNum == 1)
            {
                return llGetRootRotation();
            }

            Quaternion q = m_host.GetWorldRotation();
            return new LSL_Rotation(q.X, q.Y, q.Z, q.W);
        }

        private LSL_Rotation GetPartRot(ISceneChildEntity part)
        {
            Quaternion q;
            if (part.LinkNum == 0 || part.LinkNum == 1) // unlinked or root prim
            {
                if (part.ParentEntity.RootChild.AttachmentPoint != 0)
                {
                    IScenePresence avatar = World.GetScenePresence(part.AttachedAvatar);
                    if (avatar != null)
                    {
                        q = (avatar.AgentControlFlags & (uint)AgentManager.ControlFlags.AGENT_CONTROL_MOUSELOOK) != 0 ? avatar.CameraRotation : avatar.Rotation;
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Rotation();

            return new LSL_Rotation(m_host.RotationOffset.X, m_host.RotationOffset.Y, m_host.RotationOffset.Z, m_host.RotationOffset.W);
        }

        public void llSetForce(LSL_Vector force, int local)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


            if (m_host.ParentEntity != null)
            {
                if (!m_host.ParentEntity.IsDeleted)
                {
                    if (local != 0)
                        force *= llGetRot();

                    m_host.ParentEntity.RootChild.SetForce(new Vector3((float)force.x, (float)force.y, (float)force.z));
                }
            }
        }

        public LSL_Vector llGetForce()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();
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

        public LSL_Integer llTarget(LSL_Vector position, LSL_Float range)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;

            return m_host.registerTargetWaypoint(new Vector3((float)position.x, (float)position.y, (float)position.z), (float)range);
        }

        public void llTargetRemove(int number)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.unregisterTargetWaypoint(number);
        }

        public LSL_Integer llRotTarget(LSL_Rotation rot, double error)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;

            return m_host.registerRotTargetWaypoint(new Quaternion((float)rot.x, (float)rot.y, (float)rot.z, (float)rot.s), (float)error);
        }

        public void llRotTargetRemove(int number)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.unregisterRotTargetWaypoint(number);
        }

        public void llMoveToTarget(LSL_Vector target, double tau)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.MoveToTarget(new Vector3((float)target.x, (float)target.y, (float)target.z), (float)tau);
        }

        public void llStopMoveToTarget()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.StopMoveToTarget();
        }

        public void llApplyImpulse(LSL_Vector force, int local)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            //No energy force yet
            Vector3 v = new Vector3((float)force.x, (float)force.y, (float)force.z);
            float len = v.Length();
            if (len > 20000.0f)
            {
                //                v.Normalize();
                v = v * 20000.0f / len;
            }
            m_host.ApplyImpulse(v, local != 0);
        }

        public void llApplyRotationalImpulse(LSL_Vector force, int local)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.ApplyAngularImpulse(new Vector3((float)force.x, (float)force.y, (float)force.z), local != 0);
        }

        public void llSetTorque(LSL_Vector torque, int local)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.SetAngularImpulse(new Vector3((float)torque.x, (float)torque.y, (float)torque.z), local != 0);
        }

        public LSL_Vector llGetTorque()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

            Vector3 torque = m_host.ParentEntity.GetTorque();
            return new LSL_Vector(torque.X, torque.Y, torque.Z);
        }

        public void llSetForceAndTorque(LSL_Vector force, LSL_Vector torque, int local)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            llSetForce(force, local);
            llSetTorque(torque, local);
        }

        public LSL_Vector llGetVel()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();
            Vector3 tmp = m_host.IsAttachment ? m_host.ParentEntity.Scene.GetScenePresence(m_host.AttachedAvatar).Velocity : m_host.Velocity;
            return new LSL_Vector(tmp.X, tmp.Y, tmp.Z);
        }

        public void llSetVelocity(LSL_Vector force, LSL_Integer local)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;
            Vector3 velocity = new Vector3((float)force.x, (float)force.y, (float)force.z);
            if (local == 1)
            {
                Quaternion grot = m_host.GetWorldRotation();
                Quaternion AXgrot = grot;
                Vector3 AXimpulsei = velocity;
                Vector3 newimpulse = AXimpulsei * AXgrot;
                velocity = newimpulse;
            }

            if (m_host.ParentEntity.RootChild.PhysActor != null)
                m_host.ParentEntity.RootChild.PhysActor.Velocity = velocity;
        }

        public void llSetAngularVelocity(LSL_Vector force, LSL_Integer local)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;
            Vector3 rotvelocity = new Vector3((float)force.x, (float)force.y, (float)force.z);
            if (local == 1)
            {
                Quaternion grot = m_host.GetWorldRotation();
                Quaternion AXgrot = grot;
                Vector3 AXimpulsei = rotvelocity;
                Vector3 newimpulse = AXimpulsei * AXgrot;
                rotvelocity = newimpulse;
            }

            if (m_host.ParentEntity.RootChild.PhysActor != null)
                m_host.ParentEntity.RootChild.PhysActor.RotationalVelocity = rotvelocity;
        }

        public LSL_Vector llGetAccel()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();
            Vector3 tmp = m_host.Acceleration;
            return new LSL_Vector(tmp.X, tmp.Y, tmp.Z);
        }

        public LSL_Vector llGetOmega()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();
            Vector3 tmp = m_host.AngularVelocity;
            return new LSL_Vector(tmp.X, tmp.Y, tmp.Z);
        }

        public LSL_Float llGetTimeOfDay() // this is not sl compatible see wiki
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            return (DateTime.Now.TimeOfDay.TotalMilliseconds / 1000) % (3600 * 4);
        }

        public LSL_Float llGetWallclock()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            return DateTime.Now.TimeOfDay.TotalSeconds;
        }

        public LSL_Float llGetTime()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            TimeSpan ScriptTime = DateTime.Now - m_timer;
            return ScriptTime.TotalMilliseconds / 1000;
        }

        public void llResetTime()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_timer = DateTime.Now;
        }

        public LSL_Float llGetAndResetTime()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            TimeSpan ScriptTime = DateTime.Now - m_timer;
            m_timer = DateTime.Now;
            return ScriptTime.TotalMilliseconds / 1000;
        }

        public void llSound(string sound, double volume, int queue, int loop)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            // This function has been deprecated
            // see http://www.lslwiki.net/lslwiki/wakka.php?wakka=llSound
            Deprecated("llSound");
            if (loop == 1)
                llLoopSound(sound, volume);
            else
                llPlaySound(sound, volume);
            llSetSoundQueueing(queue);
        }

        // Xantor 20080528 PlaySound updated so it accepts an objectinventory name -or- a key to a sound
        // 20080530 Updated to remove code duplication
        public void llPlaySound(string sound, double volume)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


            // send the sound, once, to all clients in range
            m_host.SendSound(KeyOrName(sound, true).ToString(), volume, false, 0, 0, false, false);
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            if (m_host.Sound == KeyOrName(sound, true))
                return;

            if (m_host.Sound != UUID.Zero)
                llStopSound();

            m_host.Sound = KeyOrName(sound, true);
            m_host.SoundGain = volume;
            m_host.SoundFlags = (byte)SoundFlags.Loop;      // looping
            if (m_host.SoundRadius == 0)
                m_host.SoundRadius = 20;    // Magic number, 20 seems reasonable. Make configurable?

            m_host.ScheduleUpdate(PrimUpdateFlags.FindBest);
        }

        public void llLoopSoundMaster(string sound, double volume)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.ParentEntity.LoopSoundMasterPrim = m_host;
            lock (m_host.ParentEntity.LoopSoundSlavePrims)
            {
                foreach (ISceneChildEntity prim in m_host.ParentEntity.LoopSoundSlavePrims)
                {
                    if (prim.Sound != UUID.Zero)
                        llStopSound();

                    prim.Sound = KeyOrName(sound, true);
                    prim.SoundGain = volume;
                    prim.SoundFlags = (byte)SoundFlags.Loop;      // looping
                    if (prim.SoundRadius == 0)
                        prim.SoundRadius = 20;    // Magic number, 20 seems reasonable. Make configurable?

                    prim.ScheduleUpdate(PrimUpdateFlags.FindBest);
                }
            }
            if (m_host.Sound != UUID.Zero)
                llStopSound();

            m_host.Sound = KeyOrName(sound, true);
            m_host.SoundGain = volume;
            m_host.SoundFlags = (byte)SoundFlags.Loop;      // looping
            if (m_host.SoundRadius == 0)
                m_host.SoundRadius = 20;    // Magic number, 20 seems reasonable. Make configurable?

            m_host.ScheduleUpdate(PrimUpdateFlags.FindBest);
        }

        public void llLoopSoundSlave(string sound, double volume)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            lock (m_host.ParentEntity.LoopSoundSlavePrims)
            {
                m_host.ParentEntity.LoopSoundSlavePrims.Add(m_host);
            }
        }

        public void llPlaySoundSlave(string sound, double volume)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


            // send the sound, once, to all clients in range
            m_host.SendSound(KeyOrName(sound, true).ToString(), volume, false, 0, 0, true, false);
        }

        public void llTriggerSound(string sound, double volume)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            // send the sound, once, to all clients in range
            m_host.SendSound(KeyOrName(sound, true).ToString(), volume, true, 0, 0, false, false);
        }

        // Xantor 20080528: Clear prim data of sound instead
        public void llStopSound()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            if (m_host.ParentEntity.LoopSoundSlavePrims.Contains(m_host))
            {
                if (m_host.ParentEntity.LoopSoundMasterPrim == m_host)
                {
                    foreach (ISceneChildEntity part in m_host.ParentEntity.LoopSoundSlavePrims)
                    {
                        part.Sound = UUID.Zero;
                        part.SoundGain = 0;
                        part.SoundFlags = (byte)SoundFlags.None;
                        part.ScheduleUpdate(PrimUpdateFlags.FindBest);
                    }
                    m_host.ParentEntity.LoopSoundMasterPrim = null;
                    m_host.ParentEntity.LoopSoundSlavePrims.Clear();
                }
                else
                {
                    m_host.Sound = UUID.Zero;
                    m_host.SoundGain = 0;
                    m_host.SoundFlags = (byte)SoundFlags.None;
                    m_host.ScheduleUpdate(PrimUpdateFlags.FindBest);
                }
            }
            else
            {
                m_host.Sound = UUID.Zero;
                m_host.SoundGain = 0;
                m_host.SoundFlags = (byte)SoundFlags.Stop | (byte)SoundFlags.None;
                m_host.ScheduleUpdate(PrimUpdateFlags.FindBest);
            }
        }

        public DateTime llPreloadSound(string sound)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            m_host.PreloadSound(sound);
            return PScriptSleep(1000);
        }

        /// <summary>
        /// Return a portion of the designated string bounded by
        /// inclusive indices (start and end). As usual, the negative
        /// indices, and the tolerance for out-of-bound values, makes
        /// this more complicated than it might otherwise seem.
        /// </summary>

        public LSL_String llGetSubString(string src, int start, int end)
        {

            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_String();


            // Normalize indices (if negative).
            // After normlaization they may still be
            // negative, but that is now relative to
            // the start, rather than the end, of the
            // sequence.

            if (start < 0)
            {
                start = src.Length + start;
            }
            if (end < 0)
            {
                end = src.Length + end;
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
                    end = src.Length - 1;
                }

                if (start < 0)
                {
                    return src.Substring(0, end + 1);
                }
                // Both indices are positive
                return src.Substring(start, (end + 1) - start);
            }

            // Inverted substring (end < start)
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
                return start < src.Length ? src.Substring(start) : String.Empty;
            }
            if (start < src.Length)
            {
                return src.Substring(0, end + 1) + src.Substring(start);
            }
            return src.Substring(0, end + 1);
        }

        /// <summary>
        /// Delete substring removes the specified substring bounded
        /// by the inclusive indices start and end. Indices may be
        /// negative (indicating end-relative) and may be inverted,
        /// i.e. end < start./>
        /// </summary>

        public LSL_String llDeleteSubString(string src, int start, int end)
        {

            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_String();


            // Normalize indices (if negative).
            // After normlaization they may still be
            // negative, but that is now relative to
            // the start, rather than the end, of the
            // sequence.
            if (start < 0)
            {
                start = src.Length + start;
            }
            if (end < 0)
            {
                end = src.Length + end;
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
                    end = src.Length - 1;
                }

                return src.Remove(start, end - start + 1);
            }
            // Inverted substring
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
                    return src.Remove(start).Remove(0, end + 1);
                }
                return src.Remove(0, end + 1);
            }
            if (start < src.Length)
            {
                return src.Remove(start);
            }
            return src;
        }

        /// <summary>
        /// Insert string inserts the specified string identified by src
        /// at the index indicated by index. Index may be negative, in
        /// which case it is end-relative. The index may exceed either
        /// string bound, with the result being a concatenation.
        /// </summary>

        public LSL_String llInsertString(string dest, int index, string src)
        {

            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_String();


            // Normalize indices (if negative).
            // After normlaization they may still be
            // negative, but that is now relative to
            // the start, rather than the end, of the
            // sequence.
            if (index < 0)
            {
                index = dest.Length + index;

                // Negative now means it is less than the lower
                // bound of the string.

                if (index < 0)
                {
                    return src + dest;
                }

            }

            if (index >= dest.Length)
            {
                return dest + src;
            }

            // The index is in bounds.
            // In this case the index refers to the index that will
            // be assigned to the first character of the inserted string.
            // So unlike the other string operations, we do not add one
            // to get the correct string length.
            return dest.Substring(0, index) + src + dest.Substring(index);

        }

        public LSL_String llToUpper(string src)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_String();

            return src.ToUpper();
        }

        public LSL_String llToLower(string src)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_String();

            return src.ToLower();
        }

        public LSL_Integer llGiveMoney(string destination, int amount)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Integer();
            UUID invItemID = InventorySelf();
            if (invItemID == UUID.Zero)
                return 0;



            TaskInventoryItem item = m_host.TaskInventory[invItemID];

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

        public DateTime llMakeExplosion(int particles, double scale, double vel, double lifetime, double arc, string texture, LSL_Vector offset)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;


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

            List<object> list = new List<object>
                                    {
                                        ScriptBaseClass.PSYS_PART_FLAGS,
                                        ScriptBaseClass.PSYS_PART_INTERP_COLOR_MASK |
                                        ScriptBaseClass.PSYS_PART_INTERP_SCALE_MASK |
                                        ScriptBaseClass.PSYS_PART_EMISSIVE_MASK | ScriptBaseClass.PSYS_PART_WIND_MASK,
                                        ScriptBaseClass.PSYS_SRC_PATTERN,
                                        ScriptBaseClass.PSYS_SRC_PATTERN_ANGLE_CONE,
                                        ScriptBaseClass.PSYS_PART_START_COLOR,
                                        new LSL_Vector(1, 1, 1),
                                        ScriptBaseClass.PSYS_PART_END_COLOR,
                                        new LSL_Vector(1, 1, 1),
                                        ScriptBaseClass.PSYS_PART_START_ALPHA,
                                        new LSL_Float(0.50),
                                        ScriptBaseClass.PSYS_PART_END_ALPHA,
                                        new LSL_Float(0.25),
                                        ScriptBaseClass.PSYS_PART_START_SCALE,
                                        new LSL_Vector(scale, scale, 0),
                                        ScriptBaseClass.PSYS_PART_END_SCALE,
                                        new LSL_Vector(scale*2 + lifetime, scale*2 + lifetime, 0),
                                        ScriptBaseClass.PSYS_PART_MAX_AGE,
                                        new LSL_Float(lifetime),
                                        ScriptBaseClass.PSYS_SRC_ACCEL,
                                        new LSL_Vector(0, 0, 0),
                                        ScriptBaseClass.PSYS_SRC_TEXTURE,
                                        new LSL_String(texture),
                                        ScriptBaseClass.PSYS_SRC_BURST_RATE,
                                        new LSL_Float(1),
                                        ScriptBaseClass.PSYS_SRC_ANGLE_BEGIN,
                                        new LSL_Float(0.0),
                                        ScriptBaseClass.PSYS_SRC_ANGLE_END,
                                        new LSL_Float(arc*Math.PI),
                                        ScriptBaseClass.PSYS_SRC_BURST_PART_COUNT,
                                        new LSL_Integer(particles/2),
                                        ScriptBaseClass.PSYS_SRC_BURST_RADIUS,
                                        new LSL_Float(0.0),
                                        ScriptBaseClass.PSYS_SRC_BURST_SPEED_MIN,
                                        new LSL_Float(vel/3),
                                        ScriptBaseClass.PSYS_SRC_BURST_SPEED_MAX,
                                        new LSL_Float(vel*2/3),
                                        ScriptBaseClass.PSYS_SRC_MAX_AGE,
                                        new LSL_Float(lifetime/2),
                                        ScriptBaseClass.PSYS_SRC_OMEGA,
                                        new LSL_Vector(0, 0, 0)
                                    };

            llParticleSystem(new LSL_Types.list(list.ToArray()));

            return PScriptSleep(100);
        }

        public DateTime llMakeFountain(int particles, double scale, double vel, double lifetime, double arc, int bounce, string texture, LSL_Vector offset, double bounce_offset)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;


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

            List<object> list = new List<object>
                                    {
                                        ScriptBaseClass.PSYS_PART_FLAGS,
                                        ScriptBaseClass.PSYS_PART_INTERP_COLOR_MASK |
                                        ScriptBaseClass.PSYS_PART_INTERP_SCALE_MASK | ScriptBaseClass.PSYS_PART_WIND_MASK |
                                        ScriptBaseClass.PSYS_PART_BOUNCE_MASK | ScriptBaseClass.PSYS_PART_EMISSIVE_MASK,
                                        ScriptBaseClass.PSYS_SRC_PATTERN,
                                        ScriptBaseClass.PSYS_SRC_PATTERN_ANGLE_CONE,
                                        ScriptBaseClass.PSYS_PART_START_COLOR,
                                        new LSL_Vector(1, 1, 1),
                                        ScriptBaseClass.PSYS_PART_END_COLOR,
                                        new LSL_Vector(1, 1, 1),
                                        ScriptBaseClass.PSYS_PART_START_ALPHA,
                                        new LSL_Float(0.50),
                                        ScriptBaseClass.PSYS_PART_END_ALPHA,
                                        new LSL_Float(0.25),
                                        ScriptBaseClass.PSYS_PART_START_SCALE,
                                        new LSL_Vector(scale/1.5, scale/1.5, 0),
                                        ScriptBaseClass.PSYS_PART_END_SCALE,
                                        new LSL_Vector(0, 0, 0),
                                        ScriptBaseClass.PSYS_PART_MAX_AGE,
                                        new LSL_Float(3),
                                        ScriptBaseClass.PSYS_SRC_ACCEL,
                                        new LSL_Vector(1, 0, -4),
                                        ScriptBaseClass.PSYS_SRC_TEXTURE,
                                        new LSL_String(texture),
                                        ScriptBaseClass.PSYS_SRC_BURST_RATE,
                                        new LSL_Float(5/particles),
                                        ScriptBaseClass.PSYS_SRC_ANGLE_BEGIN,
                                        new LSL_Float(0.0),
                                        ScriptBaseClass.PSYS_SRC_ANGLE_END,
                                        new LSL_Float(arc*Math.PI),
                                        ScriptBaseClass.PSYS_SRC_BURST_PART_COUNT,
                                        new LSL_Integer(1),
                                        ScriptBaseClass.PSYS_SRC_BURST_RADIUS,
                                        new LSL_Float(0.0),
                                        ScriptBaseClass.PSYS_SRC_BURST_SPEED_MIN,
                                        new LSL_Float(vel),
                                        ScriptBaseClass.PSYS_SRC_BURST_SPEED_MAX,
                                        new LSL_Float(vel),
                                        ScriptBaseClass.PSYS_SRC_MAX_AGE,
                                        new LSL_Float(lifetime/2),
                                        ScriptBaseClass.PSYS_SRC_OMEGA,
                                        new LSL_Vector(0, 0, 0)
                                    };

            llParticleSystem(new LSL_Types.list(list.ToArray()));

            return PScriptSleep(100);
        }

        public DateTime llMakeSmoke(int particles, double scale, double vel, double lifetime, double arc, string texture, LSL_Vector offset)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

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
            List<object> list = new List<object>
                                    {
                                        ScriptBaseClass.PSYS_PART_FLAGS,
                                        ScriptBaseClass.PSYS_PART_INTERP_COLOR_MASK |
                                        ScriptBaseClass.PSYS_PART_INTERP_SCALE_MASK |
                                        ScriptBaseClass.PSYS_PART_EMISSIVE_MASK | ScriptBaseClass.PSYS_PART_WIND_MASK,
                                        ScriptBaseClass.PSYS_SRC_PATTERN,
                                        ScriptBaseClass.PSYS_SRC_PATTERN_ANGLE_CONE,
                                        ScriptBaseClass.PSYS_PART_START_COLOR,
                                        new LSL_Vector(1, 1, 1),
                                        ScriptBaseClass.PSYS_PART_END_COLOR,
                                        new LSL_Vector(1, 1, 1),
                                        ScriptBaseClass.PSYS_PART_START_ALPHA,
                                        new LSL_Float(1),
                                        ScriptBaseClass.PSYS_PART_END_ALPHA,
                                        new LSL_Float(0.05),
                                        ScriptBaseClass.PSYS_PART_START_SCALE,
                                        new LSL_Vector(scale, scale, 0),
                                        ScriptBaseClass.PSYS_PART_END_SCALE,
                                        new LSL_Vector(10, 10, 0),
                                        ScriptBaseClass.PSYS_PART_MAX_AGE,
                                        new LSL_Float(3),
                                        ScriptBaseClass.PSYS_SRC_ACCEL,
                                        new LSL_Vector(0, 0, 0),
                                        ScriptBaseClass.PSYS_SRC_TEXTURE,
                                        new LSL_String(texture),
                                        ScriptBaseClass.PSYS_SRC_BURST_RATE,
                                        new LSL_Float(10/particles),
                                        ScriptBaseClass.PSYS_SRC_ANGLE_BEGIN,
                                        new LSL_Float(0.0),
                                        ScriptBaseClass.PSYS_SRC_ANGLE_END,
                                        new LSL_Float(arc*Math.PI),
                                        ScriptBaseClass.PSYS_SRC_BURST_PART_COUNT,
                                        new LSL_Integer(1),
                                        ScriptBaseClass.PSYS_SRC_BURST_RADIUS,
                                        new LSL_Float(0.0),
                                        ScriptBaseClass.PSYS_SRC_BURST_SPEED_MIN,
                                        new LSL_Float(vel),
                                        ScriptBaseClass.PSYS_SRC_BURST_SPEED_MAX,
                                        new LSL_Float(vel),
                                        ScriptBaseClass.PSYS_SRC_MAX_AGE,
                                        new LSL_Float(lifetime/2),
                                        ScriptBaseClass.PSYS_SRC_OMEGA,
                                        new LSL_Vector(0, 0, 0)
                                    };

            llParticleSystem(new LSL_Types.list(list.ToArray()));
            return PScriptSleep(100);
        }

        public DateTime llMakeFire(int particles, double scale, double vel, double lifetime, double arc, string texture, LSL_Vector offset)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;


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

            List<object> list = new List<object>
                                    {
                                        ScriptBaseClass.PSYS_PART_FLAGS,
                                        ScriptBaseClass.PSYS_PART_INTERP_COLOR_MASK |
                                        ScriptBaseClass.PSYS_PART_INTERP_SCALE_MASK |
                                        ScriptBaseClass.PSYS_PART_EMISSIVE_MASK | ScriptBaseClass.PSYS_PART_WIND_MASK,
                                        ScriptBaseClass.PSYS_SRC_PATTERN,
                                        ScriptBaseClass.PSYS_SRC_PATTERN_ANGLE_CONE,
                                        ScriptBaseClass.PSYS_PART_START_COLOR,
                                        new LSL_Vector(1, 1, 1),
                                        ScriptBaseClass.PSYS_PART_END_COLOR,
                                        new LSL_Vector(1, 1, 1),
                                        ScriptBaseClass.PSYS_PART_START_ALPHA,
                                        new LSL_Float(0.50),
                                        ScriptBaseClass.PSYS_PART_END_ALPHA,
                                        new LSL_Float(0.10),
                                        ScriptBaseClass.PSYS_PART_START_SCALE,
                                        new LSL_Vector(scale/2, scale/2, 0),
                                        ScriptBaseClass.PSYS_PART_END_SCALE,
                                        new LSL_Vector(scale, scale, 0),
                                        ScriptBaseClass.PSYS_PART_MAX_AGE,
                                        new LSL_Float(0.50),
                                        ScriptBaseClass.PSYS_SRC_ACCEL,
                                        new LSL_Vector(0, 0, 0),
                                        ScriptBaseClass.PSYS_SRC_TEXTURE,
                                        new LSL_String(texture),
                                        ScriptBaseClass.PSYS_SRC_BURST_RATE,
                                        new LSL_Float(5/particles),
                                        ScriptBaseClass.PSYS_SRC_ANGLE_BEGIN,
                                        new LSL_Float(0.0),
                                        ScriptBaseClass.PSYS_SRC_ANGLE_END,
                                        new LSL_Float(arc*Math.PI),
                                        ScriptBaseClass.PSYS_SRC_BURST_PART_COUNT,
                                        new LSL_Integer(1),
                                        ScriptBaseClass.PSYS_SRC_BURST_RADIUS,
                                        new LSL_Float(0.0),
                                        ScriptBaseClass.PSYS_SRC_BURST_SPEED_MIN,
                                        new LSL_Float(vel),
                                        ScriptBaseClass.PSYS_SRC_BURST_SPEED_MAX,
                                        new LSL_Float(vel),
                                        ScriptBaseClass.PSYS_SRC_MAX_AGE,
                                        new LSL_Float(lifetime/2),
                                        ScriptBaseClass.PSYS_SRC_OMEGA,
                                        new LSL_Vector(0, 0, 0)
                                    };

            llParticleSystem(new LSL_Types.list(list.ToArray()));
            return PScriptSleep(100);
        }

        public DateTime llRezAtRoot(string inventory, LSL_Vector pos, LSL_Vector vel, LSL_Rotation rot, int param)
        {
            return llRezPrim(inventory, pos, vel, rot, param, true, true, true, true);
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
        /// <param name="SetDieAtEdge"></param>
        /// <param name="CheckPos"></param>
        /// <returns></returns>
        public DateTime llRezPrim(string inventory, LSL_Types.Vector3 pos, LSL_Types.Vector3 vel, LSL_Types.Quaternion rot, int param, bool isRezAtRoot, bool SetDieAtEdge, bool CheckPos)
        {
            return llRezPrim(inventory, pos, vel, rot, param, isRezAtRoot, false, SetDieAtEdge, CheckPos);
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
        /// <param name="doRecoil"></param>
        /// <param name="SetDieAtEdge"></param>
        /// <param name="CheckPos"></param>
        /// <returns></returns>
        public DateTime llRezPrim(string inventory, LSL_Types.Vector3 pos, LSL_Types.Vector3 vel, LSL_Types.Quaternion rot, int param, bool isRezAtRoot, bool doRecoil, bool SetDieAtEdge, bool CheckPos)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "llRezPrim", m_host, "LSL", m_itemID)) return DateTime.Now;

            if (m_ScriptEngine.Config.GetBoolean("AllowllRezObject", true))
            {
                if (Double.IsNaN(rot.x) || Double.IsNaN(rot.y) || Double.IsNaN(rot.z) || Double.IsNaN(rot.s))
                    return DateTime.Now;
                if (CheckPos)
                {
                    float dist = (float)llVecDist(llGetPos(), pos);

                    if (dist > m_ScriptDistanceFactor * 10.0f)
                        return DateTime.Now;
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
                            return DateTime.Now;
                        }

                        Vector3 llpos = new Vector3((float)pos.x, (float)pos.y, (float)pos.z);
                        Vector3 llvel = new Vector3((float)vel.x, (float)vel.y, (float)vel.z);

                        ISceneEntity new_group = RezObject(m_host, inv.Value, llpos, Rot2Quaternion(rot), llvel, param, m_host.UUID, isRezAtRoot);
                        new_group.OnFinishedPhysicalRepresentationBuilding += delegate()
                        {
                            //Do this after the physics engine has built the prim
                            float groupmass = new_group.GetMass();

                            //Recoil to the av
                            if (m_host.IsAttachment && doRecoil && (new_group.RootChild.Flags & PrimFlags.Physics) == PrimFlags.Physics)
                            {
                                IScenePresence SP = m_host.ParentEntity.Scene.GetScenePresence(m_host.OwnerID);
                                if (SP != null)
                                {
                                    //Push the av backwards (For every action, there is an equal, but opposite reaction)
                                    Vector3 impulse = llvel * groupmass;
                                    impulse.X = impulse.X < 1 ? impulse.X : impulse.X > -1 ? impulse.X : -1;
                                    impulse.Y = impulse.Y < 1 ? impulse.Y : impulse.Y > -1 ? impulse.Y : -1;
                                    impulse.Z = impulse.Z < 1 ? impulse.Z : impulse.Z > -1 ? impulse.Z : -1;
                                    SP.PushForce(impulse);
                                }
                            }
                        };
                        // If either of these are null, then there was an unknown error.
                        if (new_group == null || new_group.RootChild == null)
                            continue;

                        // objects rezzed with this method are die_at_edge by default.
                        if (SetDieAtEdge)
                            new_group.RootChild.SetDieAtEdge(true);

                        // Variable script delay? (see (http://wiki.secondlife.com/wiki/LSL_Delay)
                        return PScriptSleep(100);
                    }
                }

                llSay(0, "Could not find object " + inventory);
            }
            return DateTime.Now;
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
        /// <param name="RezzedFrom"></param>
        /// <param name="RezObjectAtRoot"></param>
        /// <returns>The SceneObjectGroup rezzed or null if rez was unsuccessful</returns>
        public ISceneEntity RezObject(
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

                    string reason;
                    if (!World.Permissions.CanRezObject(group.ChildrenList.Count, ownerID, pos, out reason))
                    {
                        World.GetScenePresence(ownerID).ControllingClient.SendAlertMessage("You do not have permission to rez objects here: " + reason);
                        return null;
                    }

                    List<ISceneChildEntity> partList = group.ChildrenEntities();
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
                        // center is on average of all positions
                        // less root prim position
#if (!ISWIN)
                        Vector3 offset = Vector3.Zero;
                        foreach (ISceneChildEntity child in partList)
                        {
                            offset += child.AbsolutePosition;
                        }
#else
                        Vector3 offset = partList.Aggregate(Vector3.Zero, (current, child) => current + child.AbsolutePosition);
#endif
                        offset /= partList.Count;
                        offset -= group.AbsolutePosition;
                        offset += pos;
                        group.AbsolutePosition = offset;
                    }

                    ISceneChildEntity rootPart = group.GetChildPart(group.UUID);

                    // Since renaming the item in the inventory does not affect the name stored
                    // in the serialization, transfer the correct name from the inventory to the
                    // object itself before we rez.
                    rootPart.Name = item.Name;
                    rootPart.Description = item.Description;

                    group.SetGroup(sourcePart.GroupID, group.OwnerID, false);

                    if (rootPart.OwnerID != item.OwnerID)
                    {
                        if (World.Permissions.PropagatePermissions())
                        {
                            if ((item.CurrentPermissions & 8) != 0)
                            {
                                foreach (ISceneChildEntity part in partList)
                                {
                                    part.EveryoneMask = item.EveryonePermissions;
                                    part.NextOwnerMask = item.NextPermissions;
                                }
                            }
                            group.ApplyNextOwnerPermissions();
                        }
                    }

                    foreach (ISceneChildEntity part in partList)
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
                    World.SceneGraph.AddPrimToScene(group);
                    if ((group.RootPart.Flags & PrimFlags.Physics) == PrimFlags.Physics)
                    {
                        group.RootPart.PhysActor.OnPhysicalRepresentationChanged += delegate
                        {
                            float groupmass = group.GetMass();
                            //Apply the velocity to the object
                            //llApplyImpulse(new LSL_Vector(llvel.X * groupmass, llvel.Y * groupmass, llvel.Z * groupmass), 0);
                            // @Above: Err.... no. Read http://lslwiki.net/lslwiki/wakka.php?wakka=llRezObject
                            //    Notice the "Creates ("rezzes") object's inventory object centered at position pos (in region coordinates) with velocity vel"
                            //    This means SET the velocity to X, not just temperarily add it!
                            //   -- Revolution Smythe
                            llSetForce(new LSL_Vector(vel * groupmass), 0);
                            group.RootPart.PhysActor.ForceSetVelocity(vel * groupmass);
                            group.RootPart.PhysActor.Velocity = vel * groupmass;
                        };
                    }

                    group.CreateScriptInstances(param, true, StateSource.ScriptedRez, RezzedFrom, false);

                    if (!World.Permissions.BypassPermissions())
                    {
                        if ((item.CurrentPermissions & (uint)PermissionMask.Copy) == 0)
                            sourcePart.Inventory.RemoveInventoryItem(item.ItemID);
                    }

                    group.ScheduleGroupUpdate(PrimUpdateFlags.FullUpdate);

                    return rootPart.ParentEntity;
                }
            }

            return null;
        }

        public DateTime llRezObject(string inventory, LSL_Vector pos, LSL_Vector vel, LSL_Rotation rot, int param)
        {
            return llRezPrim(inventory, pos, vel, rot, param, false, true, true, true);
        }

        public void llLookAt(LSL_Vector target, double strength, double damping)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            // Determine where we are looking from
            LSL_Vector from = llGetPos();

            // Work out the normalised vector from the source to the target
            LSL_Vector delta = llVecNorm(target - from);
            LSL_Vector angle = new LSL_Vector(0, 0, 0)
                                   {
                                       x = llAtan2(delta.z, delta.y) - ScriptBaseClass.PI_BY_TWO,
                                       y = llAtan2(delta.x, llSqrt((delta.y * delta.y) + (delta.z * delta.z)))
                                   };

            // Calculate the yaw
            // subtracting PI_BY_TWO is required to compensate for the odd SL co-ordinate system

            // Calculate pitch

            // we need to convert from a vector describing
            // the angles of rotation in radians into rotation value

            LSL_Types.Quaternion rot = llEuler2Rot(angle);
            //If the strength is 0, or we are non-physical, set the rotation
            if (strength == 0 || m_host.PhysActor == null || !m_host.PhysActor.IsPhysical)
                llSetRot(rot);
            else
                m_host.startLookAt(Rot2Quaternion(rot), (float)strength, (float)damping);
        }

        public void llRotLookAt(LSL_Rotation target, double strength, double damping)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            Quaternion rot = new Quaternion((float)target.x, (float)target.y, (float)target.z, (float)target.s);
            m_host.RotLookAt(rot, (float)strength, (float)damping);
        }

        public void llStopLookAt()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.StopLookAt();
        }

        public void llSetTimerEvent(double sec)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;
            if (sec != 0.0 && sec < m_MinTimerInterval)
                sec = m_MinTimerInterval;

            // Setting timer repeat
            TimerPlugin timerPlugin = (TimerPlugin)m_ScriptEngine.GetScriptPlugin("Timer");
            timerPlugin.SetTimerEvent(m_host.UUID, m_itemID, sec);
        }

        public virtual DateTime llSleep(double sec)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            return PScriptSleep((int)(sec * 1000));
        }

        public LSL_Float llGetObjectMass(string id)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            UUID key = new UUID();
            if (UUID.TryParse(id, out key))
            {
                try
                {
                    ISceneChildEntity obj = World.GetSceneObjectPart(key);
                    if (obj != null)
                        return obj.GetMass();
                    // the object is null so the key is for an avatar
                    IScenePresence avatar = World.GetScenePresence(key);
                    if (avatar != null)
                        if (avatar.IsChildAgent)
                            // reference http://www.lslwiki.net/lslwiki/wakka.php?wakka=llGetObjectMass
                            // child agents have a mass of 1.0
                            return 1;
                        else
                            return avatar.PhysicsActor.Mass;
                }
                catch (KeyNotFoundException)
                {
                    return 0; // The Object/Agent not in the region so just return zero
                }
            }
            return 0;
        }

        public LSL_Float llGetMassMKS()
        {
            return llGetMass() * 100;
        }

        public LSL_Float llGetMass()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();
            if (m_host.IsAttachment)
            {
                IScenePresence SP = m_host.ParentEntity.Scene.GetScenePresence(m_host.OwnerID);
                return SP != null ? SP.PhysicsActor.Mass : 0.0;
            }
            return m_host.GetMass();
        }

        public void llCollisionFilter(string name, string id, int accept)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.CollisionFilter.Clear();
            m_host.CollisionFilter.Add(accept, id ?? name);
        }

        public void llTakeControls(int controls, int accept, int pass_on)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;
            TaskInventoryItem item;

            lock (m_host.TaskInventory)
            {
                if (!m_host.TaskInventory.ContainsKey(InventorySelf()))
                    return;
                item = m_host.TaskInventory[InventorySelf()];
            }

            if (item.PermsGranter != UUID.Zero)
            {
                IScenePresence presence = World.GetScenePresence(item.PermsGranter);

                if (presence != null)
                {
                    if ((item.PermsMask & ScriptBaseClass.PERMISSION_TAKE_CONTROLS) != 0)
                    {
                        IScriptControllerModule m = presence.RequestModuleInterface<IScriptControllerModule>();
                        if (m != null)
                            m.RegisterControlEventsToScript(controls, accept, pass_on, m_host, m_itemID);
                    }
                }
            }


        }

        public void llReleaseControls()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;
            TaskInventoryItem item;

            lock (m_host.TaskInventory)
            {
                if (!m_host.TaskInventory.ContainsKey(InventorySelf()))
                    return;
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
                        IScriptControllerModule m = presence.RequestModuleInterface<IScriptControllerModule>();
                        if (m != null)
                            m.UnRegisterControlEventsToScript(m_localID, m_itemID);
                        // Remove Take Control permission.
                        item.PermsMask &= ~ScriptBaseClass.PERMISSION_TAKE_CONTROLS;
                    }
                }
            }
        }

        public void llReleaseURL(string url)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            if (m_UrlModule != null)
                m_UrlModule.ReleaseURL(url);
        }

        /// <summary>
        /// Attach the object containing this script to the avatar that owns it.
        /// </summary>
        /// <returns>true if the attach suceeded, false if it did not</returns>
        public bool AttachToAvatar(int attachmentPoint)
        {
          var grp = (SceneObjectGroup) m_host.ParentEntity;
          ScenePresence presence = (ScenePresence) World.GetScenePresence(m_host.OwnerID);
          IAttachmentsModule attachmentsModule = World.RequestModuleInterface<IAttachmentsModule>();
             if (attachmentsModule != null)
                return attachmentsModule.AttachObjectFromInworldObject(m_localID, presence.ControllingClient, grp, attachmentPoint);
            else
                return false;
        }
        /// <summary>
        /// Detach the object containing this script from the avatar it is attached to.
        /// </summary>
        /// <remarks>
        /// Nothing happens if the object is not attached.
        /// </remarks>
        public void DetachFromAvatar()
        {
            Util.FireAndForget(DetachWrapper, m_host);
        }
        private void DetachWrapper(object o)
        {
           SceneObjectPart host = (SceneObjectPart)o;
           SceneObjectGroup grp = host.ParentGroup;
           UUID itemID = grp.GroupID;
           ScenePresence presence = (ScenePresence) World.GetScenePresence(host.OwnerID);
           IAttachmentsModule attachmentsModule = World.RequestModuleInterface<IAttachmentsModule>();
          if (attachmentsModule != null)
                attachmentsModule.DetachSingleAttachmentToInventory(itemID, presence.ControllingClient);
         }

        public void llAttachToAvatar(int attachmentPoint)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


            if (m_host.ParentEntity.RootChild.AttachmentPoint != 0)
                return;

            TaskInventoryItem item;

            lock (m_host.TaskInventory)
            {
                if (!m_host.TaskInventory.ContainsKey(InventorySelf()))
                    return;
                item = m_host.TaskInventory[InventorySelf()];
            }

            if (item.PermsGranter != m_host.OwnerID)
                return;

            if ((item.PermsMask & ScriptBaseClass.PERMISSION_ATTACH) != 0)
            {
                AttachToAvatar(attachmentPoint);
            }
        }

        public void llDetachFromAvatar()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            if (m_host.ParentEntity.RootChild.AttachmentPoint == 0)
                return;

            TaskInventoryItem item;

            lock (m_host.TaskInventory)
            {
                if (!m_host.TaskInventory.ContainsKey(InventorySelf()))
                    return;
                item = m_host.TaskInventory[InventorySelf()];
            }

            if (item.PermsGranter != m_host.OwnerID)
                return;

            if ((item.PermsMask & ScriptBaseClass.PERMISSION_ATTACH) != 0)
                DetachFromAvatar();
        }

        public void llTakeCamera(string avatar)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            Deprecated("llTakeCamera");

        }

        public void llReleaseCamera(string avatar)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            Deprecated("llReleaseCamera");
            llClearCameraParams();
        }

        public LSL_String llGetOwner()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_String();


            return m_host.OwnerID.ToString();
        }

        public DateTime llInstantMessage(string user, string message)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;


            // We may be able to use ClientView.SendInstantMessage here, but we need a client instance.
            // InstantMessageModule.OnInstantMessage searches through a list of scenes for a client matching the toAgent,
            // but I don't think we have a list of scenes available from here.
            // (We also don't want to duplicate the code in OnInstantMessage if we can avoid it.)

            UUID friendTransactionID = UUID.Random();

            GridInstantMessage msg = new GridInstantMessage
                                         {
                                             fromAgentID = m_host.UUID,
                                             toAgentID = UUID.Parse(user),
                                             imSessionID = friendTransactionID,
                                             fromAgentName = m_host.Name
                                         };

            // This is the item we're mucking with here

            // Cap the message length at 1024.
            if (message != null && message.Length > 1024)
                msg.message = message.Substring(0, 1024);
            else
                msg.message = message;

            msg.dialog = (byte)InstantMessageDialog.MessageFromObject;
            msg.fromGroup = false;
            msg.offline = 0;
            msg.ParentEstateID = 0;
            msg.Position = m_host.AbsolutePosition;
            msg.RegionID = World.RegionInfo.RegionID;
            msg.binaryBucket
                = Util.StringToBytes256(
                    "{0}/{1}/{2}/{3}",
                    World.RegionInfo.RegionName,
                    (int)Math.Floor(m_host.AbsolutePosition.X),
                    (int)Math.Floor(m_host.AbsolutePosition.Y),
                    (int)Math.Floor(m_host.AbsolutePosition.Z));

            if (m_TransferModule != null)
            {
                m_TransferModule.SendInstantMessage(msg);
            }
            return PScriptSleep(2000);
        }

        public DateTime llEmail(string address, string subject, string message)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "llEmail", m_host, "LSL", m_itemID)) return DateTime.Now;
            IEmailModule emailModule = World.RequestModuleInterface<IEmailModule>();
            if (emailModule == null)
            {
                ShoutError("llEmail: email module not configured");
                return DateTime.Now;
            }

            emailModule.SendEmail(m_host.UUID, address, subject, message, World);
            return PScriptSleep(20000);
        }

        public void llGetNextEmail(string address, string subject)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            IEmailModule emailModule = World.RequestModuleInterface<IEmailModule>();
            if (emailModule == null)
            {
                ShoutError("llGetNextEmail: email module not configured");
                return;
            }

            emailModule.GetNextEmailAsync(m_host.UUID, address, subject, email =>
            {
                if (email == null)
                    return;

                m_ScriptEngine.PostScriptEvent(m_itemID, m_host.UUID, "email",
                        new Object[] {
                        new LSL_String(email.time),
                        new LSL_String(email.sender),
                        new LSL_String(email.subject),
                        new LSL_String(email.message),
                        new LSL_Integer(email.numLeft)}
                        );
            }, World);
        }

        public LSL_String llGetKey()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            return m_host.UUID.ToString();
        }

        public void llSetBuoyancy(double buoyancy)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            if (m_host.ParentEntity != null)
            {
                if (!m_host.ParentEntity.IsDeleted)
                {
                    m_host.ParentEntity.RootChild.SetBuoyancy((float)buoyancy);
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            if (m_host.PhysActor != null)
            {
                m_host.SetHoverHeight(0f, PIDHoverType.Ground, 0f);
            }
        }

        public void llMinEventDelay(double delay)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_ScriptEngine.SetMinEventDelay(m_itemID, m_host.UUID, delay);
        }

        /// <summary>
        /// llSoundPreload is deprecated. In SL this appears to do absolutely nothing
        /// and is documented to have no delay.
        /// </summary>
        public void llSoundPreload(string sound)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

        }

        public LSL_Integer llStringLength(string str)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;

            if (str.Length > 0)
            {
                return str.Length;
            }
            return 0;
        }

        public void llStartAnimation(string anim)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


            UUID invItemID = InventorySelf();
            if (invItemID == UUID.Zero)
                return;

            TaskInventoryItem item;

            lock (m_host.TaskInventory)
            {
                if (!m_host.TaskInventory.ContainsKey(InventorySelf()))
                    return;
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


            UUID invItemID = InventorySelf();
            if (invItemID == UUID.Zero)
                return;

            TaskInventoryItem item;

            lock (m_host.TaskInventory)
            {
                if (!m_host.TaskInventory.ContainsKey(InventorySelf()))
                    return;
                item = m_host.TaskInventory[InventorySelf()];
            }

            if (item.PermsGranter == UUID.Zero)
                return;

            if ((item.PermsMask & ScriptBaseClass.PERMISSION_TRIGGER_ANIMATION) != 0)
            {
                UUID animID = new UUID();

                if (!UUID.TryParse(anim, out animID))
                {
                    animID = InventoryKey(anim, false);
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

        public void llTargetOmega(LSL_Vector axis, LSL_Float spinrate, LSL_Float gain)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.OmegaAxis = new Vector3((float)axis.x, (float)axis.y, (float)axis.z);
            m_host.OmegaGain = gain;
            m_host.OmegaSpinRate = spinrate;

            m_host.GenerateRotationalVelocityFromOmega();
            ScriptData script = ScriptProtection.GetScript(m_itemID);
            if (script != null)
                script.TargetOmegaWasSet = true;
            m_host.ScheduleTerseUpdate();
            //m_host.SendTerseUpdateToAllClients();
        }

        public LSL_Integer llGetStartParameter()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;

            return m_ScriptEngine.GetStartParameter(m_itemID, m_host.UUID);
        }

        public void llGodLikeRezObject(string inventory, LSL_Vector pos)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

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

                    // if attachment we set it's asset id so object updates can reflect that
                    // if not, we set it's position in world.
                    group.AbsolutePosition = new Vector3((float)pos.x, (float)pos.y, (float)pos.z);

                    IScenePresence SP = World.GetScenePresence(m_host.OwnerID);
                    if (SP != null)
                        group.SetGroup(m_host.GroupID, SP.UUID, false);

                    if (group.RootPart.Shape.PCode == (byte)PCode.Prim)
                        group.ClearPartAttachmentData();

                    // Fire on_rez
                    group.CreateScriptInstances(0, true, StateSource.ScriptedRez, UUID.Zero, false);
                    group.ScheduleGroupUpdate(PrimUpdateFlags.ForcedFullUpdate);
                }
            }
        }

        public void llRequestPermissions(string agent, int perm)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;
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
                        new DetectParams[0]), EventPriority.FirstStart);

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
                            new DetectParams[0]), EventPriority.FirstStart);

                    return;
                }
            }
            else if (m_host.ParentEntity.SitTargetAvatar.Contains(agentID)) // Sitting avatar
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
                            new DetectParams[0]), EventPriority.FirstStart);

                    return;
                }
            }

            IScenePresence presence = World.GetScenePresence(agentID);

            if (presence != null)
            {
                string ownerName = "";
                IScenePresence ownerPresence = World.GetScenePresence(m_host.ParentEntity.RootChild.OwnerID);
                ownerName = ownerPresence == null ? resolveName(m_host.OwnerID) : ownerPresence.Name;

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
                    m_waitingForScriptAnswer = true;
                }

                presence.ControllingClient.SendScriptQuestion(
                    m_host.UUID, m_host.ParentEntity.RootChild.Name, ownerName, invItemID, perm);

                return;
            }

            // Requested agent is not in range, refuse perms
            m_ScriptEngine.PostScriptEvent(m_itemID, m_host.UUID, new EventParams(
                    "run_time_permissions", new Object[] {
                    new LSL_Integer(0) },
                    new DetectParams[0]), EventPriority.FirstStart);
        }

        void handleScriptAnswer(IClientAPI client, UUID taskID, UUID itemID, int answer)
        {
            if (taskID != m_host.UUID)
                return;

            UUID invItemID = InventorySelf();

            if (invItemID == UUID.Zero)
                return;

            client.OnScriptAnswer -= handleScriptAnswer;
            m_waitingForScriptAnswer = false;

            if ((answer & ScriptBaseClass.PERMISSION_TAKE_CONTROLS) == 0)
                llReleaseControls();

            lock (m_host.TaskInventory)
            {
                m_host.TaskInventory[invItemID].PermsMask = answer;
            }

            m_ScriptEngine.PostScriptEvent(m_itemID, m_host.UUID, new EventParams(
                    "run_time_permissions", new Object[] {
                    new LSL_Integer(answer) },
                    new DetectParams[0]), EventPriority.FirstStart);
        }

        public LSL_String llGetPermissionsKey()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";


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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;


            if (m_host.ParentEntity.ChildrenEntities().Count > 1)
            {
                return m_host.LinkNum;
            }
            return 0;
        }

        public void llSetLinkColor(int linknumber, LSL_Vector color, int face)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            List<ISceneChildEntity> parts = GetLinkParts(linknumber);

            foreach (ISceneChildEntity part in parts)
                part.SetFaceColor(new Vector3((float)color.x, (float)color.y, (float)color.z), face);
        }

        public DateTime llCreateLink(string target, int parent)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID))
                return DateTime.Now;

            UUID invItemID = InventorySelf();
            UUID targetID;

            if (!UUID.TryParse(target, out targetID))
                return DateTime.Now;

            TaskInventoryItem item;
            lock (m_host.TaskInventory)
            {
                item = m_host.TaskInventory[invItemID];
            }

            if ((item.PermsMask & ScriptBaseClass.PERMISSION_CHANGE_LINKS) == 0
                && !m_automaticLinkPermission)
            {
                ShoutError("Script trying to link but PERMISSION_CHANGE_LINKS permission not set!");
                return DateTime.Now;
            }

            IClientAPI client = null;
            IScenePresence sp = World.GetScenePresence(item.PermsGranter);
            if (sp != null)
                client = sp.ControllingClient;

            ISceneChildEntity targetPart = World.GetSceneObjectPart(targetID);

            if (targetPart.ParentEntity.RootChild.AttachmentPoint != 0)
                return DateTime.Now;
            // Fail silently if attached
            ISceneEntity parentPrim = null;
            ISceneEntity childPrim = null;
            if (parent != 0)
            {
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

            parentPrim.TriggerScriptChangedEvent(Changed.LINK);
            parentPrim.RootChild.CreateSelected = true;
            parentPrim.ScheduleGroupUpdate(PrimUpdateFlags.ForcedFullUpdate);

            if (client != null)
                parentPrim.GetProperties(client);

            return PScriptSleep(1000);
        }

        public void llBreakLink(int linknum)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

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

            if (linknum == ScriptBaseClass.LINK_ROOT)
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
                IEntity target = m_host.ParentEntity.GetLinkNumPart(linknum);
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
                List<ISceneChildEntity> parts = new List<ISceneChildEntity>(parentPrim.ChildrenEntities());
                parts.Remove(parentPrim.RootChild);
                foreach (ISceneChildEntity part in parts)
                {
                    parentPrim.DelinkFromGroup(part, true);
                }
                parentPrim.ScheduleGroupUpdate(PrimUpdateFlags.ForcedFullUpdate);
                parentPrim.TriggerScriptChangedEvent(Changed.LINK);

                if (parts.Count > 0)
                {
                    ISceneChildEntity newRoot = parts[0];
                    parts.Remove(newRoot);
                    foreach (ISceneChildEntity part in parts)
                    {
                        newRoot.ParentEntity.LinkToGroup(part.ParentEntity);
                    }
                    newRoot.ParentEntity.ScheduleGroupUpdate(PrimUpdateFlags.ForcedFullUpdate);
                }
            }
            else
            {
                if (childPrim == null)
                    return;

                parentPrim.DelinkFromGroup(childPrim, true);
                childPrim.ParentEntity.ScheduleGroupUpdate(PrimUpdateFlags.ForcedFullUpdate);
                parentPrim.ScheduleGroupUpdate(PrimUpdateFlags.ForcedFullUpdate);
                parentPrim.TriggerScriptChangedEvent(Changed.LINK);
            }
        }

        public void llBreakAllLinks()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            ISceneEntity parentPrim = m_host.ParentEntity;
            if (parentPrim.RootChild.AttachmentPoint != 0)
                return; // Fail silently if attached

            List<ISceneChildEntity> parts = new List<ISceneChildEntity>(parentPrim.ChildrenEntities());
            parts.Remove(parentPrim.RootChild);

            foreach (ISceneChildEntity part in parts)
            {
                parentPrim.DelinkFromGroup(part, true);
                parentPrim.TriggerScriptChangedEvent(Changed.LINK);
                part.ParentEntity.ScheduleGroupUpdate(PrimUpdateFlags.ForcedFullUpdate);
            }
            parentPrim.ScheduleGroupUpdate(PrimUpdateFlags.ForcedFullUpdate);
        }

        public LSL_String llGetLinkKey(int linknum)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            IEntity target = m_host.ParentEntity.GetLinkNumPart(linknum);
            if (target != null)
            {
                return target.UUID.ToString();
            }
            return UUID.Zero.ToString();
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";


            // simplest case, this prims link number
            if (m_host.LinkNum == linknum)
                return m_host.Name;

            // Single prim
            if (m_host.LinkNum == 0)
            {
                if (linknum == 1)
                    return m_host.Name;
                IEntity entity = m_host.ParentEntity.GetLinkNumPart(linknum);
                if (entity != null)
                    return entity.Name;
                return UUID.Zero.ToString();
            }
            // Link set
            IEntity part = null;
            part = m_host.LinkNum == 1 ? m_host.ParentEntity.GetLinkNumPart(linknum < 0 ? 2 : linknum) : m_host.ParentEntity.GetLinkNumPart(linknum < 2 ? 1 : linknum);
            if (part != null)
                return part.Name;
            return UUID.Zero.ToString();
        }

        public LSL_Integer llGetInventoryNumber(int type)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;

            int count = 0;

            lock (m_host.TaskInventory)
            {
#if (!ISWIN)
                foreach (TaskInventoryItem item in m_host.TaskInventory.Values)
                    if (item.Type == type || type == -1)
                        count++;
#else
                count += m_host.TaskInventory.Values.Count(item => item.Type == type || type == -1);
#endif
            }

            return count;
        }

        public LSL_String llGetInventoryName(int type, int number)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

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
                return String.Empty;

            keys.Sort();
            if (keys.Count > number)
            {
                return (string)keys[number];
            }
            return String.Empty;
        }

        public LSL_Float llGetEnergy()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            return 1.0f;
        }

        public DateTime llGiveInventory(string destination, string inventory)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            bool found = false;
            UUID destId = UUID.Zero;
            UUID objId = UUID.Zero;
            int assetType = 0;
            string objName = String.Empty;

            if (!UUID.TryParse(destination, out destId))
            {
                llSay(0, "Could not parse key " + destination);
                return DateTime.Now;
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

            // check if destination is an avatar
            if (World.GetScenePresence(destId) != null || m_host.ParentEntity.Scene.RequestModuleInterface<IAgentInfoService>().GetUserInfo(destId.ToString()) != null)
            {
                // destination is an avatar
                InventoryItemBase agentItem = null;
                ILLClientInventory inventoryModule = World.RequestModuleInterface<ILLClientInventory>();
                if (inventoryModule != null)
                    agentItem = inventoryModule.MoveTaskInventoryItemToUserInventory(destId, UUID.Zero, m_host, objId, false);

                if (agentItem == null)
                    return DateTime.Now;

                byte[] bucket = new byte[17];
                bucket[0] = (byte)assetType;
                byte[] objBytes = agentItem.ID.GetBytes();
                Array.Copy(objBytes, 0, bucket, 1, 16);

                GridInstantMessage msg = new GridInstantMessage(World,
                        m_host.UUID, m_host.Name + ", an object owned by " +
                        resolveName(m_host.OwnerID) + ",", destId,
                        (byte)InstantMessageDialog.InventoryOffered,
                        false, objName + "'\n'" + m_host.Name + "' is located at " +
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
            return PScriptSleep(3000);
        }

        public void llRemoveInventory(string name)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


            lock (m_host.TaskInventory)
            {
                foreach (TaskInventoryItem item in m_host.TaskInventory.Values)
                {
                    if (item.Name == name)
                    {
                        if (item.ItemID == m_itemID)
                            throw new ScriptDeleteException();
                        m_host.Inventory.RemoveInventoryItem(item.ItemID);
                        return;
                    }
                }
            }
        }

        public void llSetText(string text, LSL_Vector color, LSL_Float alpha)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            Vector3 av3 = new Vector3(Util.Clip((float)color.x, 0.0f, 1.0f),
                                      Util.Clip((float)color.y, 0.0f, 1.0f),
                                      Util.Clip((float)color.z, 0.0f, 1.0f));
            m_host.SetText(text.Length > 254 ? text.Remove(254) : text, av3, Util.Clip((float)alpha, 0.0f, 1.0f));
            //m_host.ParentGroup.HasGroupChanged = true;
            //m_host.ParentGroup.ScheduleGroupForFullUpdate();
        }

        public LSL_Float llWater(LSL_Vector offset)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            return World.RegionInfo.RegionSettings.WaterHeight;
        }

        public void llPassTouches(int pass)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.PassTouch = pass;
        }

        public LSL_Key llRequestAgentData(string id, int data)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            UUID uuid = (UUID)id;
            UserInfo pinfo = null;
            UserAccount account;

            UserInfoCacheEntry ce;
            if (!m_userInfoCache.TryGetValue(uuid, out ce))
            {
                account = World.UserAccountService.GetUserAccount(World.RegionInfo.AllScopeIDs, uuid);
                if (account == null)
                {
                    m_userInfoCache[uuid] = null; // Cache negative
                    return UUID.Zero.ToString();
                }

                ce = new UserInfoCacheEntry { time = Util.EnvironmentTickCount(), account = account };
                pinfo = World.RequestModuleInterface<IAgentInfoService>().GetUserInfo(uuid.ToString());
                ce.pinfo = pinfo;
                m_userInfoCache[uuid] = ce;
            }
            else
            {
                if (ce == null)
                {
                    return UUID.Zero.ToString();
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
                    if ((account.UserFlags & ScriptBaseClass.PAYMENT_INFO_ON_FILE) == ScriptBaseClass.PAYMENT_INFO_ON_FILE)
                        reply = ScriptBaseClass.PAYMENT_INFO_ON_FILE.ToString();
                    if ((account.UserFlags & ScriptBaseClass.PAYMENT_INFO_USED) == ScriptBaseClass.PAYMENT_INFO_USED)
                        reply = ScriptBaseClass.PAYMENT_INFO_USED.ToString();
                    reply = "0";
                    break;
                default:
                    return UUID.Zero.ToString(); // Raise no event
            }

            UUID rq = UUID.Random();

            DataserverPlugin dataserverPlugin = (DataserverPlugin)m_ScriptEngine.GetScriptPlugin("Dataserver");
            UUID tid = dataserverPlugin.RegisterRequest(m_host.UUID,
                                             m_itemID, rq.ToString());

            dataserverPlugin.AddReply(rq.ToString(), reply, 100);

            ScriptSleep(200);
            return tid.ToString();

        }

        public LSL_Key llRequestInventoryData(string name)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";


            TaskInventoryDictionary itemDictionary = (TaskInventoryDictionary)m_host.TaskInventory.Clone();

            foreach (TaskInventoryItem item in itemDictionary.Values)
            {
                if (item.Type == 3 && item.Name == name)
                {
                    UUID rq = UUID.Random();
                    DataserverPlugin dataserverPlugin = (DataserverPlugin)m_ScriptEngine.GetScriptPlugin("Dataserver");

                    UUID tid = dataserverPlugin.RegisterRequest(m_host.UUID,
                                                     m_itemID, rq.ToString());

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
                            dataserverPlugin.AddReply(rq.ToString(),
                                                             reply, 1000);
                        });

                    ScriptSleep(1000);
                    return tid.ToString();
                }
            }
            ScriptSleep(1000);
            return String.Empty;
        }

        public void llSetDamage(double damage)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.ParentEntity.Damage = (float)damage;

            ICombatModule combatModule = World.RequestModuleInterface<ICombatModule>();
            if (combatModule != null)
                combatModule.AddDamageToPrim(m_host.ParentEntity);
        }

        public DateTime llTeleportAgentHome(LSL_Key _agent)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            string agent = _agent.ToString();

            UUID agentId = new UUID();
            if (UUID.TryParse(agent, out agentId))
            {
                IScenePresence presence = World.GetScenePresence(agentId);
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
                            return PScriptSleep(5000);
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
            return PScriptSleep(5000);
        }

        public DateTime llTextBox(string agent, string message, int chatChannel)
        {
            IDialogModule dm = World.RequestModuleInterface<IDialogModule>();

            if (dm == null)
                return DateTime.Now;

            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            UUID av = new UUID();
            if (!UUID.TryParse(agent, out av))
            {
                LSLError("First parameter to llDialog needs to be a key");
                return DateTime.Now;
            }

            if (message != null && message.Length > 1024)
                message = message.Substring(0, 1024);

            dm.SendTextBoxToUser(av, message, chatChannel, m_host.Name, m_host.UUID, m_host.OwnerID);
            return PScriptSleep(1000);
        }

        public void llModifyLand(int action, int brush)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            ITerrainModule tm = World.RequestModuleInterface<ITerrainModule>();
            if (tm != null)
            {
                tm.ModifyTerrain(m_host.OwnerID, m_host.AbsolutePosition, (byte)brush, (byte)action, m_host.OwnerID);
            }
        }

        public void llCollisionSound(string impact_sound, double impact_volume)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

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
            //We do allow UUID.Zero here for scripts that want to disable the collision sound (such as "")
            m_host.CollisionSound = soundId;
            m_host.CollisionSoundVolume = (float)impact_volume;
        }

        public void llCollisionSprite(string impact_sprite)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            // Since this is broken in SL, we can do this however we want, until they fix it.
            m_host.CollisionSprite = UUID.Parse(impact_sprite);
        }

        public LSL_String llGetAnimation(string id)
        {
            // This should only return a value if the avatar is in the same region
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            UUID avatar = (UUID)id;
            IScenePresence presence = World.GetScenePresence(avatar);
            if (presence == null)
                return "";

            if (m_host.ParentEntity.Scene.RegionInfo.RegionHandle == presence.Scene.RegionInfo.RegionHandle)
            {
                Dictionary<UUID, string> animationstateNames = AnimationSet.Animations.AnimStateNames;
                AnimationSet currentAnims = presence.Animator.Animations;
                string currentAnimationState = String.Empty;
                if (animationstateNames.TryGetValue(currentAnims.DefaultAnimation.AnimID, out currentAnimationState))
                    return currentAnimationState;
            }

            return String.Empty;
        }

        public void llMessageLinked(int linknumber, int num, string msg, string id)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


            List<ISceneChildEntity> parts = GetLinkParts(linknumber);

            foreach (ISceneChildEntity part in parts)
            {
                int linkNumber = m_host.LinkNum;
                if (m_host.ParentEntity.ChildrenEntities().Count == 1)
                    linkNumber = 0;

                object[] resobj = new object[]
                                  {
                                      new LSL_Integer(linkNumber), new LSL_Integer(num), new LSL_String(msg), new LSL_String(id)
                                  };
                m_ScriptEngine.PostObjectEvent(part.UUID, "link_message", resobj);
            }
        }

        public void llPushObject(string target, LSL_Vector impulse, LSL_Vector ang_impulse, int local)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            bool pushAllowed = false;

            bool pusheeIsAvatar = false;
            UUID targetID = UUID.Zero;

            if (!UUID.TryParse(target, out targetID))
                return;

            IScenePresence pusheeav = null;
            Vector3 PusheePos = Vector3.Zero;
            ISceneChildEntity pusheeob = null;

            IScenePresence avatar = World.GetScenePresence(targetID);
            if (avatar != null)
            {
                pusheeIsAvatar = true;

                // Pushee is in GodMode this pushing object isn't owned by them
                if (avatar.GodLevel > 0 && m_host.OwnerID != targetID)
                    return;

                pusheeav = avatar;

                // Find pushee position
                // Pushee Linked?
                if (pusheeav.ParentID != UUID.Zero)
                {
                    ISceneChildEntity parentobj = World.GetSceneObjectPart(pusheeav.ParentID);
                    PusheePos = parentobj != null ? parentobj.AbsolutePosition : pusheeav.AbsolutePosition;
                }
                else
                {
                    PusheePos = pusheeav.AbsolutePosition;
                }
            }

            if (!pusheeIsAvatar)
            {
                // not an avatar so push is not affected by parcel flags
                pusheeob = World.GetSceneObjectPart(UUID.Parse(target));

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
                if (World.RegionInfo.RegionSettings.RestrictPushing)
                {
                    pushAllowed = m_host.OwnerID == targetID || m_host.ParentEntity.Scene.Permissions.IsGod(m_host.OwnerID);
                }
                else
                {
                    if (parcelManagement != null)
                    {
                        ILandObject targetlandObj = parcelManagement.GetLandObject(PusheePos.X, PusheePos.Y);
                        if (targetlandObj == null)
                            // We didn't find the parcel but region isn't push restricted so assume it's ok
                            pushAllowed = true;
                        else
                        {
                            // Parcel push restriction
                            pushAllowed = (targetlandObj.LandData.Flags & (uint)ParcelFlags.RestrictPushObject) !=
                                          (uint)ParcelFlags.RestrictPushObject ||
                                          m_host.ParentEntity.Scene.Permissions.CanPushObject(m_host.OwnerID,
                                                                                              targetlandObj);
                        }
                    }
                }
            }
            if (pushAllowed)
            {
                float distance = (PusheePos - m_host.AbsolutePosition).Length();
                float distance_term = distance * distance * distance; // Script Energy
                float pusher_mass = m_host.GetMass();

                const float PUSH_ATTENUATION_DISTANCE = 17f;
                const float PUSH_ATTENUATION_SCALE = 5f;
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
                else
                {
                    if (pusheeob.PhysActor != null)
                    {
                        pusheeob.ApplyImpulse(applied_linear_impulse, local != 0);
                    }
                }
            }
        }

        public void llPassCollisions(int pass)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.PassCollisions = pass;
        }

        public LSL_String llGetScriptName()
        {
            string result = String.Empty;

            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";


            lock (m_host.TaskInventory)
            {
                foreach (TaskInventoryItem item in m_host.TaskInventory.Values)
                {
                    if (item.Type == 10 && item.ItemID == m_itemID)
                    {
                        result = item.Name ?? String.Empty;
                        break;
                    }
                }
            }

            return result;
        }

        public LSL_Integer llGetNumberOfSides()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;


            return GetNumberOfSides(m_host);
        }

        protected int GetNumberOfSides(ISceneChildEntity part)
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
        public LSL_Rotation llAxisAngle2Rot(LSL_Vector axis, LSL_Float angle)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Rotation();


            double s = Math.Cos(angle * 0.5);
            double t = Math.Sin(angle * 0.5);
            double x = axis.x * t;
            double y = axis.y * t;
            double z = axis.z * t;

            return new LSL_Rotation(x, y, z, s);
        }


        // Xantor 29/apr/2008
        // converts a Quaternion to X,Y,Z axis rotations
        public LSL_Vector llRot2Axis(LSL_Rotation rot)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

            double x, y, z;

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

            return new LSL_Vector(x, y, z);
        }


        // Returns the angle of a quaternion (see llRot2Axis for the axis)
        public LSL_Float llRot2Angle(LSL_Rotation rot)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();


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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            return Math.Acos(val);
        }

        public LSL_Float llAsin(double val)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            return Math.Asin(val);
        }

        public LSL_Float llAngleBetween(LSL_Rotation a, LSL_Rotation b)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();


            double aa = (a.x * a.x + a.y * a.y + a.z * a.z + a.s * a.s);
            double bb = (b.x * b.x + b.y * b.y + b.z * b.z + b.s * b.s);
            double aa_bb = aa * bb;
            if (aa_bb == 0) return 0.0;
            double ab = (a.x * b.x + a.y * b.y + a.z * b.z + a.s * b.s);
            double quotient = (ab * ab) / aa_bb;
            if (quotient >= 1.0) return 0.0;
            return Math.Acos(2 * quotient - 1);
        }

        public LSL_String llGetInventoryKey(string name)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_String();


            lock (m_host.TaskInventory)
            {
                foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory)
                {
                    if (inv.Value.Name == name)
                    {
                        return (inv.Value.CurrentPermissions &
                                (uint)(PermissionMask.Copy | PermissionMask.Transfer | PermissionMask.Modify)) ==
                               (uint)(PermissionMask.Copy | PermissionMask.Transfer | PermissionMask.Modify)
                                   ? inv.Value.AssetID.ToString()
                                   : UUID.Zero.ToString();
                    }
                }
            }

            return UUID.Zero.ToString();
        }

        public void llAllowInventoryDrop(int add)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


            m_host.ParentEntity.RootChild.AllowedDrop = add != 0;

            // Update the object flags
            m_host.ParentEntity.RootChild.aggregateScriptEvents();
        }

        public LSL_Vector llGetSunDirection()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();


            LSL_Vector SunDoubleVector3;

            // sunPosition estate setting is set in OpenSim.Region.CoreModules.SunModule
            // have to convert from Vector3 (float) to LSL_Vector (double)
            Vector3 SunFloatVector3 = World.RegionInfo.RegionSettings.SunVector;
            SunDoubleVector3.x = SunFloatVector3.X;
            SunDoubleVector3.y = SunFloatVector3.Y;
            SunDoubleVector3.z = SunFloatVector3.Z;

            return SunDoubleVector3;
        }

        public LSL_Vector llGetTextureOffset(int face)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

            return GetTextureOffset(m_host, face);
        }

        protected LSL_Vector GetTextureOffset(ISceneChildEntity part, int face)
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
            return offset;
        }

        public LSL_Vector llGetTextureScale(int side)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            return GetTextureRot(m_host, face);
        }

        protected LSL_Float GetTextureRot(ISceneChildEntity part, int face)
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
            return 0.0;
        }

        public LSL_Integer llSubStringIndex(string source, string pattern)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;

            return source.IndexOf(pattern);
        }

        public LSL_String llGetOwnerKey(string id)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            UUID key = new UUID();
            if (UUID.TryParse(id, out key))
            {
                try
                {
                    ISceneChildEntity obj = World.GetSceneObjectPart(key);
                    if (obj == null)
                        return id; // the key is for an agent so just return the key
                    return obj.OwnerID.ToString();
                }
                catch (KeyNotFoundException)
                {
                    return id; // The Object/Agent not in the region so just return the key
                }
            }
            return UUID.Zero.ToString();
        }

        public LSL_Vector llGetCenterOfMass()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

            Vector3 center = m_host.GetGeometricCenter();
            return new LSL_Vector(center.X, center.Y, center.Z);
        }

        public LSL_List llListSort(LSL_List src, int stride, int ascending)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_List();


            if (stride <= 0)
            {
                stride = 1;
            }
            return src.Sort(stride, ascending);
        }

        public LSL_Integer llGetListLength(LSL_List src)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;


            if (src == new LSL_List(new object[0]))
            {
                return 0;
            }
            return src.Length;
        }

        public LSL_Integer llList2Integer(LSL_List src, int index)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;

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
                    return (LSL_Integer)src.Data[index];
                if (src.Data[index] is LSL_Float)
                    return Convert.ToInt32(((LSL_Float)src.Data[index]).value);
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

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
                if (src.Data[index] is LSL_Float)
                    return Convert.ToDouble(((LSL_Float)src.Data[index]).value);
                if (src.Data[index] is LSL_String)
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

            if (index < 0)
            {
                index = src.Length + index;
            }
            if (index >= src.Length || index < 0)
            {
                return new LSL_Vector(0, 0, 0);
            }
            if (src.Data[index] is LSL_Vector)
            {
                return (LSL_Vector)src.Data[index];
            }
            return new LSL_Vector(src.Data[index].ToString());
        }

        public LSL_Rotation llList2Rot(LSL_List src, int index)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Rotation();

            if (index < 0)
            {
                index = src.Length + index;
            }
            if (index >= src.Length || index < 0)
            {
                return new LSL_Rotation(0, 0, 0, 1);
            }
            if (src.Data[index] is LSL_Rotation)
            {
                return (LSL_Rotation)src.Data[index];
            }
            return new LSL_Rotation(src.Data[index].ToString());
        }

        public LSL_List llList2List(LSL_List src, int start, int end)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_List();

            return src.GetSublist(start, end);
        }

        public LSL_List llDeleteSubList(LSL_List src, int start, int end)
        {
            return src.DeleteSublist(start, end);
        }

        public LSL_Integer llGetListEntryType(LSL_List src, int index)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;

            if (index < 0)
            {
                index = src.Length + index;
            }
            if (index >= src.Length)
            {
                return 0;
            }

            if (src.Data[index] is LSL_Integer || src.Data[index] is Int32)
                return ScriptBaseClass.TYPE_INTEGER;
            if (src.Data[index] is LSL_Float || src.Data[index] is Single || src.Data[index] is Double)
                return ScriptBaseClass.TYPE_FLOAT;
            if (src.Data[index] is LSL_String || src.Data[index] is String)
            {
                UUID tuuid;
                if (UUID.TryParse(src.Data[index].ToString(), out tuuid))
                {
                    return ScriptBaseClass.TYPE_KEY;
                }
                return ScriptBaseClass.TYPE_STRING;
            }
            if (src.Data[index] is LSL_Vector)
                return ScriptBaseClass.TYPE_VECTOR;
            if (src.Data[index] is LSL_Rotation)
                return ScriptBaseClass.TYPE_ROTATION;
            if (src.Data[index] is LSL_List)
                return 7; //Extension of LSL by us
            return ScriptBaseClass.TYPE_INVALID;

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
            int x = 0;

            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";


            if (src.Data.Length > 0)
            {
                ret = src.Data[x++].ToString();
                for (; x < src.Data.Length; x++)
                {
                    ret += ", " + src.Data[x].ToString();
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
            int start = 0;
            int length = 0;

            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_List();


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
                            result.Add(new LSL_String(src.Substring(start, length).Trim()));
                            start += length + 1;
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

            result.Add(new LSL_String(src.Substring(start, length).Trim()));

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
            Random rand = new Random();

            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_List();


            if (stride <= 0)
            {
                stride = 1;
            }

            // Stride MUST be a factor of the list length
            // If not, then return the src list. This also
            // traps those cases where stride > length.

            if (src.Length != stride && src.Length % stride == 0)
            {
                int chunkk = src.Length / stride;

                int[] chunks = new int[chunkk];

                for (int i = 0; i < chunkk; i++)
                    chunks[i] = i;

                // Knuth shuffle the chunkk index
                for (int i = chunkk - 1; i >= 1; i--)
                {
                    // Elect an unrandomized chunk to swap
                    int index = rand.Next(i + 1);

                    // and swap position with first unrandomized chunk
                    int tmp = chunks[i];
                    chunks[i] = chunks[index];
                    chunks[index] = tmp;
                }

                // Construct the randomized list

                result = new LSL_List();

                for (int i = 0; i < chunkk; i++)
                {
                    for (int j = 0; j < stride; j++)
                    {
                        result.Add(src.Data[chunks[i] * stride + j]);
                    }
                }
            }
            else
            {
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

            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_List();


            //  First step is always to deal with negative indices

            if (start < 0)
                start = src.Length + start;
            if (end < 0)
                end = src.Length + end;

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
                        if (i <= ei[0] && i >= si[0])
                            result.Add(src.Data[i]);
                        if (twopass && i >= si[1] && i <= ei[1])
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
                if (start % stride == 0)
                {
                    result.Add(src.Data[start]);
                }
            }

            return result;
        }

        public LSL_Integer llGetRegionAgentCount()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;
            IEntityCountModule entityCountModule = World.RequestModuleInterface<IEntityCountModule>();
            if (entityCountModule != null)
                return new LSL_Integer(entityCountModule.RootAgents);
            else
                return new LSL_Integer(0);
        }

        public LSL_Vector llGetRegionCorner()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

            return new LSL_Vector(World.RegionInfo.RegionLocX, World.RegionInfo.RegionLocY, 0);
        }

        /// <summary>
        /// Insert the list identified by <src> into the
        /// list designated by <dest> such that the first
        /// new element has the index specified by <index>
        /// </summary>

        public LSL_List llListInsertList(LSL_List dest, LSL_List src, int index)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_List();

            LSL_List pref = null;
            LSL_List suff = null;



            if (index < 0)
            {
                index = index + dest.Length;
                if (index < 0)
                {
                    index = 0;
                }
            }

            if (index != 0)
            {
                pref = dest.GetSublist(0, index - 1);
                if (index < dest.Length)
                {
                    suff = dest.GetSublist(index, -1);
                    return pref + src + suff;
                }
                return pref + src;
            }
            if (index < dest.Length)
            {
                suff = dest.GetSublist(index, -1);
                return src + suff;
            }
            return src;
        }

        /// <summary>
        /// Returns the index of the first occurrence of test
        /// in src.
        /// </summary>

        public LSL_Integer llListFindList(LSL_List src, LSL_List test)
        {

            int index = -1;
            int length = src.Length - test.Length + 1;

            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;


            // If either list is empty, do not match

            if (src.Length != 0 && test.Length != 0)
            {
                for (int i = 0; i < length; i++)
                {
                    if (src.Data[i].Equals(test.Data[0]))
                    {
                        int j;
                        for (j = 1; j < test.Length; j++)
                            if (!src.Data[i + j].Equals(test.Data[j]))
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            return m_host.Name ?? String.Empty;
        }

        public void llSetObjectName(string name)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.Name = name ?? String.Empty;
        }

        public LSL_String llGetDate()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            DateTime date = DateTime.Now.ToUniversalTime();
            string result = date.ToString("yyyy-MM-dd");
            return result;
        }

        public LSL_Integer llEdgeOfWorld(LSL_Vector pos, LSL_Vector dir)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;


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
                // Y is the only valid direction
                edge.y = dir.y / Math.Abs(dir.y);
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
                    mag = (pos.x / dir.x);
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
            IGridRegisterModule service = World.RequestModuleInterface<IGridRegisterModule>();
            List<GridRegion> neighbors = new List<GridRegion>();
            if (service != null)
                neighbors = service.GetNeighbors(World);

            int neighborX = World.RegionInfo.RegionLocX + (int)dir.x;
            int neighborY = World.RegionInfo.RegionLocY + (int)dir.y;
#if (!ISWIN)
            foreach (GridRegion neighbor in neighbors)
                if (neighbor.RegionLocX == neighborX && neighbor.RegionLocY == neighborY)
                    return LSL_Integer.TRUE;
#else
            if (neighbors.Any(neighbor => neighbor.RegionLocX == neighborX && neighbor.RegionLocY == neighborY))
            {
                return LSL_Integer.TRUE;
            }
#endif
            return LSL_Integer.FALSE;
        }

        /// <summary>
        /// Fully implemented
        /// </summary>
        public LSL_Integer llGetAgentInfo(string id)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;


            UUID key = new UUID();
            if (!UUID.TryParse(id, out key))
            {
                return 0;
            }

            int flags = 0;

            IScenePresence agent = World.GetScenePresence(key);
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
                ISceneEntity[] att = attachMod.GetAttachmentsForAvatar(agent.UUID);
                if (att.Length > 0)
                {
                    flags |= ScriptBaseClass.AGENT_ATTACHMENTS;
#if (!ISWIN)
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
#else
                    if (att.Where(gobj => gobj != null).Any(gobj => gobj.RootChild.Inventory.ContainsScripts()))
                    {
                        flags |= ScriptBaseClass.AGENT_SCRIPTED;
                    }
#endif
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

            if ((agent.State & (byte)AgentState.Typing) != 0)
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            IAgentConnector AgentFrontend = DataManager.DataManager.RequestPlugin<IAgentConnector>();
            if (AgentFrontend == null)
                return "en-us";
            IAgentInfo Agent = AgentFrontend.GetAgent(new UUID(id));
            if (Agent == null)
                return "en-us";
            if (Agent.LanguageIsPublic)
            {
                return Agent.Language;
            }
            return "en-us";
        }

        /// <summary>
        /// http://wiki.secondlife.com/wiki/LlGetAgentList
        /// The list of options is currently not used in SL
        /// scope is one of:-
        /// AGENT_LIST_REGION - all in the region
        /// AGENT_LIST_PARCEL - all in the same parcel as the scripted object
        /// AGENT_LIST_PARCEL_OWNER - all in any parcel owned by the owner of the
        /// current parcel.
        /// </summary>
        public LSL_List llGetAgentList(LSL_Integer scope, LSL_List options)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID))
                return new LSL_List();

            // the constants are 1, 2 and 4 so bits are being set, but you
            // get an error "INVALID_SCOPE" if it is anything but 1, 2 and 4
            bool regionWide = scope == ScriptBaseClass.AGENT_LIST_REGION;
            bool parcelOwned = scope == ScriptBaseClass.AGENT_LIST_PARCEL_OWNER;
            bool parcel = scope == ScriptBaseClass.AGENT_LIST_PARCEL;
            LSL_List result = new LSL_List();

            if (!regionWide && !parcelOwned && !parcel)
            {
                result.Add("INVALID_SCOPE");
                return result;
            }

            Vector3 pos;
            UUID id = UUID.Zero;

            if (parcel || parcelOwned)
            {
                pos = m_host.GetWorldPosition();
                IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
                ILandObject land = parcelManagement.GetLandObject(pos.X, pos.Y);
                if (land == null)
                {
                    id = UUID.Zero;
                }
                else
                {
                    if (parcelOwned)
                    {
                        id = land.LandData.OwnerID;
                    }
                    else
                    {
                        id = land.LandData.GlobalID;
                    }
                }

            }

            World.ForEachScenePresence(delegate(IScenePresence ssp)
            {
                // Gods are not listed in SL

                if (!ssp.IsDeleted && ssp.GodLevel == 0.0 && !ssp.IsChildAgent)
                {
                    if (!regionWide)
                    {
                        pos = ssp.AbsolutePosition;
                        IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
                        ILandObject land = parcelManagement.GetLandObject(pos.X, pos.Y);
                        if (land != null)
                        {
                            if (parcelOwned && land.LandData.OwnerID == id ||
                               parcel && land.LandData.GlobalID == id)
                            {
                                result.Add(ssp.UUID.ToString());
                            }
                        }

                    }
                    else
                    {
                        result.Add(ssp.UUID.ToString());
                    }
                }

                // Maximum of 100 results
                if (result.Length > 99)
                {
                    return;
                }
            });
            return result;
        }

        public DateTime llAdjustSoundVolume(double volume)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            m_host.AdjustSoundGain(volume);
            return PScriptSleep(100);
        }

        public void llSetSoundQueueing(int queue)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.SetSoundQueueing(queue);
        }

        public void llSetSoundRadius(double radius)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.SoundRadius = radius;
        }

        public LSL_String llGetDisplayName(string id)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            UUID key = new UUID();
            if (UUID.TryParse(id, out key))
            {
                IScenePresence presence = World.GetScenePresence(key);

                if (presence != null)
                {
                    IProfileConnector connector = DataManager.DataManager.RequestPlugin<IProfileConnector>();
                    if (connector != null)
                        return connector.GetUserProfile(presence.UUID).DisplayName;
                }
            }
            return String.Empty;
        }

        public LSL_String llGetUsername(string id)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            UUID key = new UUID();
            if (UUID.TryParse(id, out key))
            {
                IScenePresence presence = World.GetScenePresence(key);

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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


            SetTextureAnim(m_host, mode, face, sizex, sizey, start, length, rate);
        }

        public void llSetLinkTextureAnim(int linknumber, int mode, int face, int sizex, int sizey, double start, double length, double rate)
        {


            List<ISceneChildEntity> parts = GetLinkParts(linknumber);

            foreach (var part in parts)
            {
                SetTextureAnim(part, mode, face, sizex, sizey, start, length, rate);
            }
        }

        private void SetTextureAnim(ISceneChildEntity part, int mode, int face, int sizex, int sizey, double start, double length, double rate)
        {

            Primitive.TextureAnimation pTexAnim = new Primitive.TextureAnimation { Flags = (Primitive.TextureAnimMode)mode };

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
        }

        public void llTriggerSoundLimited(string sound, double volume, LSL_Vector top_north_east,
                                          LSL_Vector bottom_south_west)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            float radius1 = (float)llVecDist(llGetPos(), top_north_east);
            float radius2 = (float)llVecDist(llGetPos(), bottom_south_west);
            float radius = Math.Abs(radius1 - radius2);
            m_host.SendSound(KeyOrName(sound, true).ToString(), volume, true, 0, radius, false, false);
        }

        public DateTime llEjectFromLand(string pest)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            UUID agentId = new UUID();
            if (UUID.TryParse(pest, out agentId))
            {
                IScenePresence presence = World.GetScenePresence(agentId);
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
                            return PScriptSleep(5000);
                        }
                    }
                    IEntityTransferModule transferModule = World.RequestModuleInterface<IEntityTransferModule>();
                    if (transferModule != null)
                        transferModule.TeleportHome(agentId, presence.ControllingClient);
                    else
                        presence.ControllingClient.SendTeleportFailed("Unable to perform teleports on this simulator.");
                }
            }
            return PScriptSleep(5000);
        }

        public LSL_Integer llOverMyLand(string id)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;

            UUID key = new UUID();
            if (UUID.TryParse(id, out key))
            {
                IScenePresence presence = World.GetScenePresence(key);
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
                    ISceneChildEntity obj = World.GetSceneObjectPart(key);
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

            IScenePresence avatar = World.GetScenePresence((UUID)id);
            LSL_Vector agentSize;
            if (avatar == null || avatar.IsChildAgent) // Fail if not in the same region
            {
                agentSize = ScriptBaseClass.ZERO_VECTOR;
            }
            else
            {
                IAvatarAppearanceModule appearance = avatar.RequestModuleInterface<IAvatarAppearanceModule>();
                agentSize = appearance != null ? new LSL_Vector(0.45, 0.6, appearance.Appearance.AvatarHeight) : ScriptBaseClass.ZERO_VECTOR;
            }
            return agentSize;
        }

        public LSL_Integer llSameGroup(string agent)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Integer();

            UUID agentId = new UUID();
            if (!UUID.TryParse(agent, out agentId))
                return new LSL_Integer(0);
            IScenePresence presence = World.GetScenePresence(agentId);
            if (presence == null || presence.IsChildAgent) // Return flase for child agents
                return new LSL_Integer(0);
            IClientAPI client = presence.ControllingClient;
            if (m_host.GroupID == client.ActiveGroupId)
                return new LSL_Integer(1);
            return new LSL_Integer(0);
        }

        public void llUnSit(string id)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


            UUID key = new UUID();
            if (UUID.TryParse(id, out key))
            {
                IScenePresence av = World.GetScenePresence(key);

                if (av != null)
                {
                    if (m_host.ParentEntity.SitTargetAvatar.Contains(key))
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

            //Get the slope normal.  This gives us the equation of the plane tangent to the slope.
            LSL_Vector vsn = llGroundNormal(offset);

            //Plug the x,y coordinates of the slope normal into the equation of the plane to get
            //the height of that point on the plane.  The resulting vector gives the slope.
            Vector3 vsl = new Vector3
                              {
                                  X = (float)vsn.x,
                                  Y = (float)vsn.y,
                                  Z = (float)(((vsn.x * vsn.x) + (vsn.y * vsn.y)) / (-1 * vsn.z))
                              };
            vsl.Normalize();
            //Normalization might be overkill here

            return new LSL_Vector(vsl.X, vsl.Y, vsl.Z);
        }

        public LSL_Vector llGroundNormal(LSL_Vector offset)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

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
            Vector3 vsn = new Vector3
                              {
                                  X = (v0.Y * v1.Z) - (v0.Z * v1.Y),
                                  Y = (v0.Z * v1.X) - (v0.X * v1.Z),
                                  Z = (v0.X * v1.Y) - (v0.Y * v1.X)
                              };
            vsn.Normalize();
            //I believe the crossproduct of two normalized vectors is a normalized vector so
            //this normalization may be overkill
            // then don't normalize them just the result

            return new LSL_Vector(vsn.X, vsn.Y, vsn.Z);
        }

        public LSL_Vector llGroundContour(LSL_Vector offset)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

            LSL_Vector x = llGroundSlope(offset);
            return new LSL_Vector(-x.y, x.x, 0.0);
        }

        public LSL_Integer llGetAttached()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Integer();

            return (int)m_host.ParentEntity.RootChild.AttachmentPoint;
        }

        public LSL_Integer llGetFreeMemory()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Integer();

            // Make scripts designed for LSO happy
            return 16384;
        }

        public LSL_Integer llSetMemoryLimit(LSL_Integer limit)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID))
                return new LSL_Integer();

            // Make scripts designed for LSO happy
            return 16384;
        }

        public LSL_Integer llGetMemoryLimit()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID))
                return new LSL_Integer();

            // Make scripts designed for LSO happy
            return 16384;
        }

        public LSL_Integer llGetSPMaxMemory()
        {
            //TODO: Not implemented!
            return 0;
        }

        public LSL_Integer llGetUsedMemory()
        {
            //TODO: Not implemented!
            return 0;
        }

        public void llScriptProfiler(LSL_Integer profilerFlags)
        {
            //TODO: We don't support this, not implemented
        }

        public LSL_Integer llGetFreeURLs()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Integer();

            if (m_UrlModule != null)
                return new LSL_Integer(m_UrlModule.GetFreeUrls());
            return new LSL_Integer(0);
        }


        public LSL_String llGetRegionName()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_String();
            return World.RegionInfo.RegionName;
        }

        public LSL_Float llGetRegionTimeDilation()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            return World.TimeDilation;
        }

        /// <summary>
        /// Returns the value reported in the client Statistics window
        /// </summary>
        public LSL_Float llGetRegionFPS()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            ISimFrameMonitor reporter = (ISimFrameMonitor)World.RequestModuleInterface<IMonitorModule>().GetMonitor(World.RegionInfo.RegionID.ToString(), MonitorModuleHelper.SimFrameStats);
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

        public enum PrimitiveRule
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
            const Primitive.ParticleSystem.ParticleDataFlags returnval = Primitive.ParticleSystem.ParticleDataFlags.None;

            return returnval;
        }

        protected Primitive.ParticleSystem getNewParticleSystemWithSLDefaultValues()
        {
            Primitive.ParticleSystem ps = new Primitive.ParticleSystem
                                              {
                                                  PartStartColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f),
                                                  PartEndColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f),
                                                  PartStartScaleX = 1.0f,
                                                  PartStartScaleY = 1.0f,
                                                  PartEndScaleX = 1.0f,
                                                  PartEndScaleY = 1.0f,
                                                  BurstSpeedMin = 1.0f,
                                                  BurstSpeedMax = 1.0f,
                                                  BurstRate = 0.1f,
                                                  PartMaxAge = 10.0f,
                                                  BurstPartCount = 10
                                              };

            // TODO find out about the other defaults and add them here
            return ps;
        }

        public void llLinkParticleSystem(int linknumber, LSL_List rules)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


            List<ISceneChildEntity> parts = GetLinkParts(linknumber);

            foreach (var part in parts)
            {
                SetParticleSystem(part, rules);
            }
        }

        public void llParticleSystem(LSL_List rules)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            SetParticleSystem(m_host, rules);
        }

        private void SetParticleSystem(ISceneChildEntity part, LSL_List rules)
        {
            if (rules.Length == 0)
            {
                part.RemoveParticleSystem();
            }
            else
            {
                Primitive.ParticleSystem prules = getNewParticleSystemWithSLDefaultValues();
                LSL_Vector tempv = new LSL_Vector();

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

                    else
                    {
                        float tempf = 0;
                        if (rule == (int)ScriptBaseClass.PSYS_PART_START_ALPHA)
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
                            int tmpi = rules.GetLSLIntegerItem(i + 1);
                            prules.Pattern = (Primitive.ParticleSystem.SourcePattern)tmpi;
                        }

                            // PSYS_SRC_INNERANGLE and PSYS_SRC_ANGLE_BEGIN use the same variables. The
                        // PSYS_SRC_OUTERANGLE and PSYS_SRC_ANGLE_END also use the same variable. The
                        // client tells the difference between the two by looking at the 0x02 bit in
                        // the PartFlags variable.
                        else if (rule == (int)ScriptBaseClass.PSYS_SRC_INNERANGLE)
                        {
                            tempf = (float)rules.GetLSLFloatItem(i + 1);
                            prules.InnerAngle = tempf;
                            prules.PartFlags &= 0xFFFFFFFD; // Make sure new angle format is off.
                        }

                        else if (rule == (int)ScriptBaseClass.PSYS_SRC_OUTERANGLE)
                        {
                            tempf = (float)rules.GetLSLFloatItem(i + 1);
                            prules.OuterAngle = tempf;
                            prules.PartFlags &= 0xFFFFFFFD; // Make sure new angle format is off.
                        }

                        else if (rule == (int)ScriptBaseClass.PSYS_SRC_TEXTURE)
                        {
                            prules.Texture = KeyOrName(rules.GetLSLStringItem(i + 1), false);
                        }

                        else if (rule == (int)ScriptBaseClass.PSYS_SRC_BURST_RATE)
                        {
                            tempf = (float)rules.GetLSLFloatItem(i + 1);
                            prules.BurstRate = tempf;
                        }

                        else if (rule == (int)ScriptBaseClass.PSYS_SRC_BURST_PART_COUNT)
                        {
                            prules.BurstPartCount = (byte)(int)rules.GetLSLIntegerItem(i + 1);
                        }

                        else if (rule == (int)ScriptBaseClass.PSYS_SRC_BURST_RADIUS)
                        {
                            tempf = (float)rules.GetLSLFloatItem(i + 1);
                            prules.BurstRadius = tempf;
                        }

                        else if (rule == (int)ScriptBaseClass.PSYS_SRC_BURST_SPEED_MIN)
                        {
                            tempf = (float)rules.GetLSLFloatItem(i + 1);
                            prules.BurstSpeedMin = tempf;
                        }

                        else if (rule == (int)ScriptBaseClass.PSYS_SRC_BURST_SPEED_MAX)
                        {
                            tempf = (float)rules.GetLSLFloatItem(i + 1);
                            prules.BurstSpeedMax = tempf;
                        }

                        else if (rule == (int)ScriptBaseClass.PSYS_SRC_MAX_AGE)
                        {
                            tempf = (float)rules.GetLSLFloatItem(i + 1);
                            prules.MaxAge = tempf;
                        }

                        else if (rule == (int)ScriptBaseClass.PSYS_SRC_TARGET_KEY)
                        {
                            UUID key = UUID.Zero;
                            prules.Target = UUID.TryParse(rules.Data[i + 1].ToString(), out key) ? key : part.UUID;
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
                            prules.InnerAngle = tempf;
                            prules.PartFlags |= 0x02; // Set new angle format.
                        }

                        else if (rule == (int)ScriptBaseClass.PSYS_SRC_ANGLE_END)
                        {
                            tempf = (float)rules.GetLSLFloatItem(i + 1);
                            prules.OuterAngle = tempf;
                            prules.PartFlags |= 0x02; // Set new angle format.
                        }
                    }
                }
                prules.CRC = 1;

                part.AddNewParticleSystem(prules);
            }
            part.ScheduleUpdate(PrimUpdateFlags.Particles);
        }

        public void llGroundRepel(double height, int water, double tau)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


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
                    m_host.UUID, m_host.Name + ", an object owned by " +
                    resolveName(m_host.OwnerID) + ",", destID,
                    (byte)InstantMessageDialog.InventoryOffered,
                    false, category + "\n" + m_host.Name + " is located at " +
                    World.RegionInfo.RegionName + " " +
                    m_host.AbsolutePosition.ToString(),
                    folderID, true, m_host.AbsolutePosition,
                    bucket);

            if (m_TransferModule != null)
                m_TransferModule.SendInstantMessage(msg);
        }

        public void llSetVehicleType(int type)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;
            if (m_host.ParentEntity != null)
            {
                if (!m_host.ParentEntity.IsDeleted)
                {
                    m_host.ParentEntity.RootChild.SetVehicleType(type);
                }
            }
        }

        //CFK 9/28: Most, but not all of the underlying plumbing between here and the physics modules is in
        //CFK 9/28: so these are not complete yet.
        public void llSetVehicleFloatParam(int param, LSL_Float value)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


            if (m_host.ParentEntity != null)
            {
                if (!m_host.ParentEntity.IsDeleted)
                {
                    m_host.ParentEntity.RootChild.SetVehicleFloatParam(param, (float)value);
                }
            }
        }

        //CFK 9/28: Most, but not all of the underlying plumbing between here and the physics modules is in
        //CFK 9/28: so these are not complete yet.
        public void llSetVehicleVectorParam(int param, LSL_Vector vec)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            if (m_host.ParentEntity != null)
            {
                if (!m_host.ParentEntity.IsDeleted)
                {
                    m_host.ParentEntity.RootChild.SetVehicleVectorParam(param,
                        new Vector3((float)vec.x, (float)vec.y, (float)vec.z));
                }
            }
        }

        //CFK 9/28: Most, but not all of the underlying plumbing between here and the physics modules is in
        //CFK 9/28: so these are not complete yet.
        public void llSetVehicleRotationParam(int param, LSL_Rotation rot)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            if (m_host.ParentEntity != null)
            {
                if (!m_host.ParentEntity.IsDeleted)
                {
                    m_host.ParentEntity.RootChild.SetVehicleRotationParam(param,
                        Rot2Quaternion(rot));
                }
            }
        }

        public void llSetVehicleFlags(int flags)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            if (m_host.ParentEntity != null)
            {
                if (!m_host.ParentEntity.IsDeleted)
                {
                    m_host.ParentEntity.RootChild.SetVehicleFlags(flags, false);
                }
            }
        }

        public void llRemoveVehicleFlags(int flags)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            if (m_host.ParentEntity != null)
            {
                if (!m_host.ParentEntity.IsDeleted)
                {
                    m_host.ParentEntity.RootChild.SetVehicleFlags(flags, true);
                }
            }
        }

        public void llSitTarget(LSL_Vector offset, LSL_Rotation rot)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            // LSL quaternions can normalize to 0, normal Quaternions can't.
            if (rot.s == 0 && rot.x == 0 && rot.y == 0 && rot.z == 0)
                rot.z = 1; // ZERO_ROTATION = 0,0,0,1

            m_host.SitTargetPosition = new Vector3((float)offset.x, (float)offset.y, (float)offset.z);
            m_host.SitTargetOrientation = Rot2Quaternion(rot);
        }

        public void llLinkSitTarget(LSL_Integer link, LSL_Vector offset, LSL_Rotation rot)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            // LSL quaternions can normalize to 0, normal Quaternions can't.
            if (rot.s == 0 && rot.x == 0 && rot.y == 0 && rot.z == 0)
                rot.z = 1; // ZERO_ROTATION = 0,0,0,1

            List<ISceneChildEntity> entities = GetLinkParts(link);
            if (entities.Count == 0)
                return;

            entities[0].SitTargetPosition = new Vector3((float)offset.x, (float)offset.y, (float)offset.z);
            entities[0].SitTargetOrientation = Rot2Quaternion(rot);
        }

        public LSL_String llAvatarOnSitTarget()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            return m_host.SitTargetAvatar.Count != 0
                       ? new LSL_String(m_host.SitTargetAvatar[0].ToString())
                       : ScriptBaseClass.NULL_KEY;
        }

        public LSL_Key llAvatarOnLinkSitTarget()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Key();

            return m_host.SitTargetAvatar.Count != 0
                       ? new LSL_String(m_host.SitTargetAvatar[0].ToString())
                       : ScriptBaseClass.NULL_KEY;
        }

        public DateTime llAddToLandPassList(string avatar, double hours)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                LandData land = parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y).LandData;
                if (land.OwnerID == m_host.OwnerID)
                {
                    ParcelManager.ParcelAccessEntry entry = new ParcelManager.ParcelAccessEntry();
                    UUID key;
                    if (UUID.TryParse(avatar, out key))
                    {
                        entry.AgentID = key;
                        entry.Flags = AccessList.Access;
                        entry.Time = DateTime.Now.AddHours(hours);
                        land.ParcelAccessList.Add(entry);
                    }
                }
            }
            return PScriptSleep(100);
        }

        public void llSetTouchText(string text)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.TouchName = text;
        }

        public void llSetSitText(string text)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.SitName = text;
        }

        public void llSetLinkCamera(LSL_Integer link, LSL_Vector eye, LSL_Vector at)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            List<ISceneChildEntity> entities = GetLinkParts(link);
            if (entities.Count > 0)
            {
                entities[0].CameraEyeOffset = new Vector3((float)eye.x, (float)eye.y, (float)eye.z);
                entities[0].CameraAtOffset = new Vector3((float)at.x, (float)at.y, (float)at.z);
            }
        }

        public void llSetCameraEyeOffset(LSL_Vector offset)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.CameraEyeOffset = new Vector3((float)offset.x, (float)offset.y, (float)offset.z);
        }

        public void llSetCameraAtOffset(LSL_Vector offset)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.CameraAtOffset = new Vector3((float)offset.x, (float)offset.y, (float)offset.z);
        }

        public LSL_String llDumpList2String(LSL_List src, string seperator)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            if (src.Length == 0)
            {
                return String.Empty;
            }
#if (!ISWIN)
            string ret = "";
            foreach (object o in src.Data)
                ret += o.ToString() + seperator;
#else
            string ret = src.Data.Aggregate("", (current, o) => current + (o.ToString() + seperator));
#endif

            ret = ret.Substring(0, ret.Length - seperator.Length);
            return ret;
        }

        public LSL_Integer llScriptDanger(LSL_Vector pos)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;

            bool result = m_ScriptEngine.PipeEventsForScript(m_host, new Vector3((float)pos.x, (float)pos.y, (float)pos.z));
            if (result)
            {
                return 1;
            }
            return 0;
        }

        public DateTime llDialog(string avatar, string message, LSL_List buttons, int chat_channel)
        {
            IDialogModule dm = World.RequestModuleInterface<IDialogModule>();

            if (dm == null)
                return DateTime.Now;

            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            UUID av = new UUID();
            if (!UUID.TryParse(avatar, out av))
            {
                //Silently accepted in in SL NOTE: it does sleep though!
                //LSLError("First parameter to llDialog needs to be a key");
                return PScriptSleep(1000);
            }
            if (buttons.Length > 12)
            {
                LSLError("No more than 12 buttons can be shown");
                return DateTime.Now;
            }
            string[] buts = new string[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons.Data[i].ToString() == String.Empty)
                {
                    LSLError("button label cannot be blank");
                    return DateTime.Now;
                }
                if (buttons.Data[i].ToString().Length > 24)
                {
                    LSLError("button label cannot be longer than 24 characters");
                    return DateTime.Now;
                }
                buts[i] = buttons.Data[i].ToString();
            }
            if (buts.Length == 0)
                buts = new[] { "OK" };

            dm.SendDialogToUser(
                av, m_host.Name, m_host.UUID, m_host.OwnerID,
                message, new UUID("00000000-0000-2222-3333-100000001000"), chat_channel, buts);

            return PScriptSleep(1000);
        }

        public void llVolumeDetect(int detect)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            if (m_host.ParentEntity != null)
            {
                if (!m_host.ParentEntity.IsDeleted)
                {
                    m_host.ParentEntity.RootChild.ScriptSetVolumeDetect(detect != 0);
                }
            }
        }

        /// <summary>
        /// This is a depecated function so this just replicates the result of
        /// invoking it in SL
        /// </summary>

        public DateTime llRemoteLoadScript(string target, string name, int running, int start_param)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            // Report an error as it does in SL
            ShoutError("Deprecated. Please use llRemoteLoadScriptPin instead.");
            return PScriptSleep(3000);
        }

        public void llSetRemoteScriptAccessPin(int pin)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.ScriptAccessPin = pin;
        }

        public DateTime llRemoteLoadScriptPin(string target, string name, int pin, int running, int start_param)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            bool found = false;
            UUID destId = UUID.Zero;
            UUID srcId = UUID.Zero;

            if (!UUID.TryParse(target, out destId))
            {
                llSay(0, "Could not parse key " + target);
                return DateTime.Now;
            }

            // target must be a different prim than the one containing the script
            if (m_host.UUID == destId)
            {
                return DateTime.Now;
            }

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
                return DateTime.Now;
            }

            // the rest of the permission checks are done in RezScript, so check the pin there as well
            ILLClientInventory inventoryModule = World.RequestModuleInterface<ILLClientInventory>();
            if (inventoryModule != null)
                inventoryModule.RezScript(srcId, m_host, destId, pin, running, start_param);
            // this will cause the delay even if the script pin or permissions were wrong - seems ok
            return PScriptSleep(3000);
        }

        public DateTime llOpenRemoteDataChannel()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            IXMLRPC xmlrpcMod = World.RequestModuleInterface<IXMLRPC>();
            if (xmlrpcMod.IsEnabled())
            {
                UUID channelID = xmlrpcMod.OpenXMLRPCChannel(m_host.UUID, m_itemID, UUID.Zero);
                IXmlRpcRouter xmlRpcRouter = World.RequestModuleInterface<IXmlRpcRouter>();
                if (xmlRpcRouter != null)
                {
                    string ExternalHostName = MainServer.Instance.HostName;

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
                                                                         new DetectParams[0]), EventPriority.FirstStart);
            }
            return PScriptSleep(1000);
        }

        public LSL_Key llSendRemoteData(string channel, string dest, int idata, string sdata)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            IXMLRPC xmlrpcMod = World.RequestModuleInterface<IXMLRPC>();
            ScriptSleep(3000);
            return (xmlrpcMod.SendRemoteData(m_host.UUID, m_itemID, channel, dest, idata, sdata)).ToString();
        }

        public DateTime llRemoteDataReply(string channel, string message_id, string sdata, int idata)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            IXMLRPC xmlrpcMod = World.RequestModuleInterface<IXMLRPC>();
            xmlrpcMod.RemoteDataReply(channel, message_id, sdata, idata);
            return PScriptSleep(100);
        }

        public DateTime llCloseRemoteDataChannel(object _channel)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            IXMLRPC xmlrpcMod = World.RequestModuleInterface<IXMLRPC>();
            xmlrpcMod.CloseXMLRPCChannel(UUID.Parse(_channel.ToString()));
            return PScriptSleep(100);
        }

        public LSL_String llMD5String(string src, int nonce)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_String();

            return Util.Md5Hash(String.Format("{0}:{1}", src, nonce.ToString()));
        }

        public LSL_String llSHA1String(string src)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            return Util.SHA1Hash(src).ToLower();
        }

        protected ObjectShapePacket.ObjectDataBlock SetPrimitiveBlockShapeParams(ISceneChildEntity part, int holeshape, LSL_Vector cut, float hollow, LSL_Vector twist)
        {
            ObjectShapePacket.ObjectDataBlock shapeBlock = new ObjectShapePacket.ObjectDataBlock();

            if (holeshape != (int)ScriptBaseClass.PRIM_HOLE_DEFAULT &&
                holeshape != (int)ScriptBaseClass.PRIM_HOLE_CIRCLE &&
                holeshape != (int)ScriptBaseClass.PRIM_HOLE_SQUARE &&
                holeshape != (int)ScriptBaseClass.PRIM_HOLE_TRIANGLE)
            {
                holeshape = ScriptBaseClass.PRIM_HOLE_DEFAULT;
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
            // A fairly large precision error occurs for some calculations,
            // if a float or double is directly cast to a byte or sbyte
            // variable, in both .Net and Mono. In .Net, coding
            // "(sbyte)(float)(some expression)" corrects the precision
            // errors. But this does not work for Mono. This longer coding
            // form of creating a tempoary float variable from the
            // expression first, then casting that variable to a byte or
            // sbyte, works for both .Net and Mono. These types of
            // assignments occur in SetPrimtiveBlockShapeParams and
            // SetPrimitiveShapeParams in support of llSetPrimitiveParams.
            float tempFloat = (float)(100.0d * twist.x);
            shapeBlock.PathTwistBegin = (sbyte)tempFloat;
            tempFloat = (float)(100.0d * twist.y);
            shapeBlock.PathTwist = (sbyte)tempFloat;

            shapeBlock.ObjectLocalID = part.LocalId;

            // retain pathcurve
            shapeBlock.PathCurve = part.Shape.PathCurve;

            part.Shape.SculptEntry = false;
            return shapeBlock;
        }

        protected void SetPrimitiveShapeParams(ISceneChildEntity part, int holeshape, LSL_Vector cut, float hollow, LSL_Vector twist, LSL_Vector taper_b, LSL_Vector topshear, byte fudge)
        {
            ObjectShapePacket.ObjectDataBlock shapeBlock = SetPrimitiveBlockShapeParams(part, holeshape, cut, hollow, twist);

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
            float tempFloat = (float)(100.0d * (2.0d - taper_b.x));
            shapeBlock.PathScaleX = (byte)tempFloat;
            tempFloat = (float)(100.0d * (2.0d - taper_b.y));
            shapeBlock.PathScaleY = (byte)tempFloat;
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
            tempFloat = (float)(100.0d * topshear.x);
            shapeBlock.PathShearX = (byte)tempFloat;
            tempFloat = (float)(100.0d * topshear.y);
            shapeBlock.PathShearY = (byte)tempFloat;

            part.Shape.SculptEntry = false;
            part.UpdateShape(shapeBlock);
        }

        protected void SetPrimitiveShapeParams(ISceneChildEntity part, int holeshape, LSL_Vector cut, float hollow, LSL_Vector twist, LSL_Vector dimple, byte fudge)
        {
            ObjectShapePacket.ObjectDataBlock shapeBlock = SetPrimitiveBlockShapeParams(part, holeshape, cut, hollow, twist);

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
            shapeBlock.ProfileEnd = (ushort)(50000 * (1 - dimple.y));

            part.Shape.SculptEntry = false;
            part.UpdateShape(shapeBlock);
        }

        protected void SetPrimitiveShapeParams(ISceneChildEntity part, int holeshape, LSL_Vector cut, float hollow, LSL_Vector twist, LSL_Vector holesize, LSL_Vector topshear, LSL_Vector profilecut, LSL_Vector taper_a, float revolutions, float radiusoffset, float skew, byte fudge)
        {
            ObjectShapePacket.ObjectDataBlock shapeBlock = SetPrimitiveBlockShapeParams(part, holeshape, cut, hollow, twist);

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
            float tempFloat = (float)(100.0d * (2.0d - holesize.x));
            shapeBlock.PathScaleX = (byte)tempFloat;
            tempFloat = (float)(100.0d * (2.0d - holesize.y));
            shapeBlock.PathScaleY = (byte)tempFloat;
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
            tempFloat = (float)(100.0d * topshear.x);
            shapeBlock.PathShearX = (byte)tempFloat;
            tempFloat = (float)(100.0d * topshear.y);
            shapeBlock.PathShearY = (byte)tempFloat;
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
            tempFloat = (float)(100.0d * taper_a.x);
            shapeBlock.PathTaperX = (sbyte)tempFloat;
            tempFloat = (float)(100.0d * taper_a.y);
            shapeBlock.PathTaperY = (sbyte)tempFloat;
            if (revolutions < 1f)
            {
                revolutions = 1f;
            }
            if (revolutions > 4f)
            {
                revolutions = 4f;
            }
            tempFloat = 66.66667f * (revolutions - 1.0f);
            shapeBlock.PathRevolutions = (byte)tempFloat;
            // limits on radiusoffset depend on revolutions and hole size (how?) seems like the maximum range is 0 to 1
            if (radiusoffset < 0f)
            {
                radiusoffset = 0f;
            }
            if (radiusoffset > 1f)
            {
                radiusoffset = 1f;
            }
            tempFloat = 100.0f * radiusoffset;
            shapeBlock.PathRadiusOffset = (sbyte)tempFloat;
            if (skew < -0.95f)
            {
                skew = -0.95f;
            }
            if (skew > 0.95f)
            {
                skew = 0.95f;
            }
            tempFloat = 100.0f * skew;
            shapeBlock.PathSkew = (sbyte)tempFloat;

            part.Shape.SculptEntry = false;
            part.UpdateShape(shapeBlock);
        }

        protected void SetPrimitiveShapeParams(ISceneChildEntity part, string map, int type)
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

            int onlytype = (type & (ScriptBaseClass.PRIM_SCULPT_FLAG_INVERT | ScriptBaseClass.PRIM_SCULPT_FLAG_MIRROR));//Removes the sculpt flags according to libOMV
            if (onlytype != (int)ScriptBaseClass.PRIM_SCULPT_TYPE_CYLINDER &&
                onlytype != (int)ScriptBaseClass.PRIM_SCULPT_TYPE_PLANE &&
                onlytype != (int)ScriptBaseClass.PRIM_SCULPT_TYPE_SPHERE &&
                onlytype != (int)ScriptBaseClass.PRIM_SCULPT_TYPE_TORUS &&
                onlytype != (int)ScriptBaseClass.PRIM_SCULPT_TYPE_MESH)
            {
                // default
                type |= ScriptBaseClass.PRIM_SCULPT_TYPE_SPHERE;
            }

            // retain pathcurve
            shapeBlock.PathCurve = part.Shape.PathCurve;
            bool changedTextureID = part.Shape.SculptTexture != sculptId;
            part.Shape.SetSculptProperties((byte)type, sculptId);
            part.Shape.SculptEntry = true;
            part.UpdateShape(shapeBlock, changedTextureID);
        }

        public void llSetPrimitiveParams(LSL_List rules)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            SetPrimParams(m_host, rules);
        }

        public void llSetLinkPrimitiveParams(int linknumber, LSL_List rules)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


            List<IEntity> parts = GetLinkPartsAndEntities(linknumber);

            foreach (IEntity part in parts)
                SetPrimParams(part, rules);
        }

        public void llSetLinkPrimitiveParamsFast(int linknumber, LSL_List rules)
        {
            List<ISceneChildEntity> parts = GetLinkParts(linknumber);

            foreach (ISceneChildEntity part in parts)
                SetPrimParams(part, rules);
        }

        public LSL_Integer llGetLinkNumberOfSides(int LinkNum)
        {
            List<ISceneChildEntity> Parts = GetLinkParts(LinkNum);
#if (!ISWIN)
            int faces = 0;
            foreach (ISceneChildEntity part in Parts)
                faces += GetNumberOfSides(part);
#else
            int faces = Parts.Sum(part => GetNumberOfSides(part));
#endif
            return new LSL_Integer(faces);
        }

        protected void SetPrimParams(IEntity part, LSL_List rules)
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
                    if (part is ISceneChildEntity)
                        (part as ISceneChildEntity).Name = name;
                }

                else if (code == (int)ScriptBaseClass.PRIM_DESC)
                {
                    if (remain < 1)
                        return;

                    string desc = rules.Data[idx++].ToString();
                    if (part is ISceneChildEntity)
                        (part as ISceneChildEntity).Description = desc;
                }

                else if (code == (int)ScriptBaseClass.PRIM_ROT_LOCAL)
                {
                    if (remain < 1)
                        return;
                    LSL_Rotation lr = rules.GetQuaternionItem(idx++);
                    if (part is ISceneChildEntity)
                        SetRot((part as ISceneChildEntity), Rot2Quaternion(lr));
                }

                else if (code == (int)ScriptBaseClass.PRIM_POSITION)
                {
                    if (remain < 1)
                        return;

                    v = rules.GetVector3Item(idx++);
                    if (part is ISceneChildEntity)
                        SetPos(part as ISceneChildEntity, v, true);
                    else if (part is IScenePresence)
                    {
                        (part as IScenePresence).OffsetPosition = new Vector3((float)v.x, (float)v.y, (float)v.z);
                        (part as IScenePresence).SendTerseUpdateToAllClients();
                    }
                }
                else if (code == (int)ScriptBaseClass.PRIM_POS_LOCAL)
                {
                    if (remain < 1)
                        return;

                    v = rules.GetVector3Item(idx++);
                    if (part is ISceneChildEntity)
                    {
                        if (((ISceneChildEntity)part).ParentID != 0)
                            ((ISceneChildEntity)part).OffsetPosition = new Vector3((float)v.x, (float)v.y, (float)v.z);
                        else
                            part.AbsolutePosition = new Vector3((float)v.x, (float)v.y, (float)v.z);
                    }
                    else if (part is IScenePresence)
                    {
                        (part as IScenePresence).OffsetPosition = new Vector3((float)v.x, (float)v.y, (float)v.z);
                        (part as IScenePresence).SendTerseUpdateToAllClients();
                    }
                }
                else if (code == (int)ScriptBaseClass.PRIM_SIZE)
                {
                    if (remain < 1)
                        return;


                    v = rules.GetVector3Item(idx++);
                    if (part is ISceneChildEntity)
                        SetScale(part as ISceneChildEntity, v);

                }
                else if (code == (int)ScriptBaseClass.PRIM_ROTATION)
                {
                    if (remain < 1)
                        return;

                    LSL_Rotation q = rules.GetQuaternionItem(idx++);
                    if (part is ISceneChildEntity)
                    {
                        // try to let this work as in SL...
                        if ((part as ISceneChildEntity).ParentID == 0)
                        {
                            // special case: If we are root, rotate complete SOG to new rotation
                            SetRot(part as ISceneChildEntity, Rot2Quaternion(q));
                        }
                        else
                        {
                            // we are a child. The rotation values will be set to the one of root modified by rot, as in SL. Don't ask.
                            ISceneEntity group = (part as ISceneChildEntity).ParentEntity;
                            if (group != null) // a bit paranoid, maybe
                            {
                                ISceneChildEntity rootPart = group.RootChild;
                                if (rootPart != null) // again, better safe than sorry
                                {
                                    SetRot((part as ISceneChildEntity), rootPart.RotationOffset * Rot2Quaternion(q));
                                }
                            }
                        }
                    }
                    else if (part is IScenePresence)
                    {
                        IScenePresence sp = (IScenePresence)part;
                        ISceneChildEntity childObj = sp.Scene.GetSceneObjectPart(sp.SittingOnUUID);
                        if (childObj != null)
                        {
                            sp.Rotation = childObj.ParentEntity.GroupRotation * Rot2Quaternion(q);
                            sp.SendTerseUpdateToAllClients();
                        }
                    }
                }

                else if (code == (int)ScriptBaseClass.PRIM_TYPE)
                {
                    if (remain < 3)
                        return;

                    if (part is ISceneChildEntity) { }
                    else
                        return;

                    code = rules.GetLSLIntegerItem(idx++);

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

                        face = rules.GetLSLIntegerItem(idx++);
                        v = rules.GetVector3Item(idx++); // cut
                        hollow = (float)rules.GetLSLFloatItem(idx++);
                        twist = rules.GetVector3Item(idx++);
                        taper_b = rules.GetVector3Item(idx++);
                        topshear = rules.GetVector3Item(idx++);

                        (part as ISceneChildEntity).Shape.PathCurve = (byte)Extrusion.Straight;
                        SetPrimitiveShapeParams((part as ISceneChildEntity), face, v, hollow, twist, taper_b, topshear, 1);

                    }

                    else if (code == (int)ScriptBaseClass.PRIM_TYPE_CYLINDER)
                    {
                        if (remain < 6)
                            return;

                        face = rules.GetLSLIntegerItem(idx++); // holeshape
                        v = rules.GetVector3Item(idx++); // cut
                        hollow = (float)rules.GetLSLFloatItem(idx++);
                        twist = rules.GetVector3Item(idx++);
                        taper_b = rules.GetVector3Item(idx++);
                        topshear = rules.GetVector3Item(idx++);
                        (part as ISceneChildEntity).Shape.ProfileShape = ProfileShape.Circle;
                        (part as ISceneChildEntity).Shape.PathCurve = (byte)Extrusion.Straight;
                        SetPrimitiveShapeParams((part as ISceneChildEntity), face, v, hollow, twist, taper_b, topshear, 0);
                    }

                    else if (code == (int)ScriptBaseClass.PRIM_TYPE_PRISM)
                    {
                        if (remain < 6)
                            return;

                        face = rules.GetLSLIntegerItem(idx++); // holeshape
                        v = rules.GetVector3Item(idx++); //cut
                        hollow = (float)rules.GetLSLFloatItem(idx++);
                        twist = rules.GetVector3Item(idx++);
                        taper_b = rules.GetVector3Item(idx++);
                        topshear = rules.GetVector3Item(idx++);
                        (part as ISceneChildEntity).Shape.PathCurve = (byte)Extrusion.Straight;
                        SetPrimitiveShapeParams((part as ISceneChildEntity), face, v, hollow, twist, taper_b, topshear, 3);

                    }

                    else if (code == (int)ScriptBaseClass.PRIM_TYPE_SPHERE)
                    {
                        if (remain < 5)
                            return;

                        face = rules.GetLSLIntegerItem(idx++); // holeshape
                        v = rules.GetVector3Item(idx++); // cut
                        hollow = (float)rules.GetLSLFloatItem(idx++);
                        twist = rules.GetVector3Item(idx++);
                        taper_b = rules.GetVector3Item(idx++); // dimple
                        (part as ISceneChildEntity).Shape.PathCurve = (byte)Extrusion.Curve1;
                        SetPrimitiveShapeParams((part as ISceneChildEntity), face, v, hollow, twist, taper_b, 5);

                    }

                    else if (code == (int)ScriptBaseClass.PRIM_TYPE_TORUS)
                    {
                        if (remain < 11)
                            return;

                        face = rules.GetLSLIntegerItem(idx++); // holeshape
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
                        (part as ISceneChildEntity).Shape.PathCurve = (byte)Extrusion.Curve1;
                        SetPrimitiveShapeParams((part as ISceneChildEntity), face, v, hollow, twist, holesize, topshear, profilecut, taper_b,
                                                revolutions, radiusoffset, skew, 0);
                    }

                    else if (code == (int)ScriptBaseClass.PRIM_TYPE_TUBE)
                    {
                        if (remain < 11)
                            return;

                        face = rules.GetLSLIntegerItem(idx++); // holeshape
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
                        (part as ISceneChildEntity).Shape.PathCurve = (byte)Extrusion.Curve1;
                        SetPrimitiveShapeParams((part as ISceneChildEntity), face, v, hollow, twist, holesize, topshear, profilecut, taper_b,
                                                revolutions, radiusoffset, skew, 1);
                    }

                    else if (code == (int)ScriptBaseClass.PRIM_TYPE_RING)
                    {
                        if (remain < 11)
                            return;

                        face = rules.GetLSLIntegerItem(idx++); // holeshape
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
                        (part as ISceneChildEntity).Shape.PathCurve = (byte)Extrusion.Curve1;
                        SetPrimitiveShapeParams((part as ISceneChildEntity), face, v, hollow, twist, holesize, topshear, profilecut, taper_b,
                                                revolutions, radiusoffset, skew, 3);
                    }

                    else if (code == (int)ScriptBaseClass.PRIM_TYPE_SCULPT)
                    {
                        if (remain < 2)
                            return;

                        string map = rules.Data[idx++].ToString();
                        face = rules.GetLSLIntegerItem(idx++); // type
                        (part as ISceneChildEntity).Shape.PathCurve = (byte)Extrusion.Curve1;
                        SetPrimitiveShapeParams((part as ISceneChildEntity), map, face);
                    }
                }

                else if (code == (int)ScriptBaseClass.PRIM_TEXTURE)
                {
                    if (remain < 5)
                        return;
                    if (part is ISceneChildEntity) { }
                    else
                        return;
                    face = rules.GetLSLIntegerItem(idx++);
                    string tex = rules.Data[idx++].ToString();
                    LSL_Vector repeats = rules.GetVector3Item(idx++);
                    LSL_Vector offsets = rules.GetVector3Item(idx++);
                    double rotation = rules.GetLSLFloatItem(idx++);

                    SetTexture((part as ISceneChildEntity), tex, face);
                    ScaleTexture((part as ISceneChildEntity), repeats.x, repeats.y, face);
                    OffsetTexture((part as ISceneChildEntity), offsets.x, offsets.y, face);
                    RotateTexture((part as ISceneChildEntity), rotation, face);

                }

                else if (code == (int)ScriptBaseClass.PRIM_COLOR)
                {
                    if (remain < 3)
                        return;
                    if (part is ISceneChildEntity) { }
                    else
                        return;
                    face = rules.GetLSLIntegerItem(idx++);
                    LSL_Vector color = rules.GetVector3Item(idx++);
                    double alpha = rules.GetLSLFloatItem(idx++);

                    (part as ISceneChildEntity).SetFaceColor(new Vector3((float)color.x, (float)color.y, (float)color.z), face);
                    SetAlpha((part as ISceneChildEntity), alpha, face);

                }

                else if (code == (int)ScriptBaseClass.PRIM_FLEXIBLE)
                {
                    if (remain < 7)
                        return;
                    if (!(part is ISceneChildEntity))
                        return;
                    bool flexi = rules.GetLSLIntegerItem(idx++);
                    int softness = rules.GetLSLIntegerItem(idx++);
                    float gravity = (float)rules.GetLSLFloatItem(idx++);
                    float friction = (float)rules.GetLSLFloatItem(idx++);
                    float wind = (float)rules.GetLSLFloatItem(idx++);
                    float tension = (float)rules.GetLSLFloatItem(idx++);
                    LSL_Vector force = rules.GetVector3Item(idx++);

                    SetFlexi((part as ISceneChildEntity), flexi, softness, gravity, friction, wind, tension, force);
                }
                else if (code == (int)ScriptBaseClass.PRIM_POINT_LIGHT)
                {
                    if (remain < 5)
                        return;
                    if (!(part is ISceneChildEntity))
                        return;
                    bool light = rules.GetLSLIntegerItem(idx++);
                    LSL_Vector lightcolor = rules.GetVector3Item(idx++);
                    float intensity = (float)rules.GetLSLFloatItem(idx++);
                    float radius = (float)rules.GetLSLFloatItem(idx++);
                    float falloff = (float)rules.GetLSLFloatItem(idx++);

                    SetPointLight((part as ISceneChildEntity), light, lightcolor, intensity, radius, falloff);

                }

                else if (code == (int)ScriptBaseClass.PRIM_GLOW)
                {
                    if (remain < 2)
                        return;
                    if (!(part is ISceneChildEntity))
                        return;
                    face = rules.GetLSLIntegerItem(idx++);
                    float glow = (float)rules.GetLSLFloatItem(idx++);

                    SetGlow((part as ISceneChildEntity), face, glow);

                }
                else if (code == (int)ScriptBaseClass.PRIM_BUMP_SHINY)
                {
                    if (remain < 3)
                        return;
                    if (!(part is ISceneChildEntity))
                        return;
                    face = rules.GetLSLIntegerItem(idx++);
                    int shiny = rules.GetLSLIntegerItem(idx++);
                    Bumpiness bump = (Bumpiness)Convert.ToByte((int)rules.GetLSLIntegerItem(idx++));

                    SetShiny(part as ISceneChildEntity, face, shiny, bump);

                }
                else if (code == (int)ScriptBaseClass.PRIM_FULLBRIGHT)
                {
                    if (remain < 2)
                        return;
                    if (!(part is ISceneChildEntity))
                        return;
                    face = rules.GetLSLIntegerItem(idx++);
                    bool st = rules.GetLSLIntegerItem(idx++);
                    SetFullBright(part as ISceneChildEntity, face, st);
                }

                else if (code == (int)ScriptBaseClass.PRIM_MATERIAL)
                {
                    if (remain < 1)
                        return;
                    if (!(part is ISceneChildEntity))
                        return;
                    int mat = rules.GetLSLIntegerItem(idx++);
                    if (mat < 0 || mat > 7)
                        return;

                    (part as ISceneChildEntity).Material = Convert.ToByte(mat);
                }
                else if (code == (int)ScriptBaseClass.PRIM_PHANTOM)
                {
                    if (remain < 1)
                        return;
                    if (!(part is ISceneChildEntity))
                        return;
                    string ph = rules.Data[idx++].ToString();

                    bool phantom = ph.Equals("1");

                    (part as ISceneChildEntity).ScriptSetPhantomStatus(phantom);
                }
                else if (code == (int)ScriptBaseClass.PRIM_PHYSICS)
                {
                    if (remain < 1)
                        return;
                    if (!(part is ISceneChildEntity))
                        return;
                    string phy = rules.Data[idx++].ToString();

                    ((SceneObjectGroup)m_host.ParentEntity).ScriptSetPhysicsStatus(phy.Equals("1"));
                }
                else if (code == (int)ScriptBaseClass.PRIM_TEMP_ON_REZ)
                {
                    if (remain < 1)
                        return;
                    if (!(part is ISceneChildEntity))
                        return;
                    string temp = rules.Data[idx++].ToString();

                    bool tempOnRez = temp.Equals("1");

                    (part as ISceneChildEntity).ScriptSetTemporaryStatus(tempOnRez);
                }
                else if (code == (int)ScriptBaseClass.PRIM_TEXGEN)
                {
                    if (remain < 2)
                        return;
                    if (!(part is ISceneChildEntity))
                        return;
                    //face,type
                    face = rules.GetLSLIntegerItem(idx++);
                    int style = rules.GetLSLIntegerItem(idx++);
                    SetTexGen((part as ISceneChildEntity), face, style);
                }
                else if (code == (int)ScriptBaseClass.PRIM_TEXT)
                {
                    if (remain < 3)
                        return;
                    if (!(part is ISceneChildEntity))
                        return;
                    string primText = rules.GetLSLStringItem(idx++);
                    LSL_Vector primTextColor = rules.GetVector3Item(idx++);
                    LSL_Float primTextAlpha = rules.GetLSLFloatItem(idx++);
                    Vector3 av3 = new Vector3(Util.Clip((float)primTextColor.x, 0.0f, 1.0f),
                                  Util.Clip((float)primTextColor.y, 0.0f, 1.0f),
                                  Util.Clip((float)primTextColor.z, 0.0f, 1.0f));
                    (part as ISceneChildEntity).SetText(primText, av3, Util.Clip((float)primTextAlpha, 0.0f, 1.0f));

                }
                else if (code == (int)ScriptBaseClass.PRIM_OMEGA)
                {
                    if (remain < 3)
                        return;
                    LSL_Vector direction = rules.GetVector3Item(idx++);
                    LSL_Float spinrate = rules.GetLSLFloatItem(idx++);
                    LSL_Float gain = rules.GetLSLFloatItem(idx++);
                    if (part is ISceneChildEntity)
                        llTargetOmega(direction, spinrate, gain);
                }
                else if (code == (int)ScriptBaseClass.PRIM_PHYSICS_SHAPE_TYPE)
                {
                    bool UsePhysics = ((m_host.Flags & PrimFlags.Physics) != 0);
                    bool IsTemporary = ((m_host.Flags & PrimFlags.TemporaryOnRez) != 0);
                    bool IsPhantom = ((m_host.Flags & PrimFlags.Phantom) != 0);
                    bool IsVolumeDetect = m_host.VolumeDetectActive;
                    ObjectFlagUpdatePacket.ExtraPhysicsBlock[] blocks = new ObjectFlagUpdatePacket.ExtraPhysicsBlock[1];
                    blocks[0] = new ObjectFlagUpdatePacket.ExtraPhysicsBlock
                                    {
                                        Density = m_host.Density,
                                        Friction = m_host.Friction,
                                        GravityMultiplier = m_host.GravityMultiplier
                                    };
                    LSL_Integer shapeType = rules.GetLSLIntegerItem(idx++);
                    if (shapeType == ScriptBaseClass.PRIM_PHYSICS_SHAPE_PRIM)
                        blocks[0].PhysicsShapeType = (byte)shapeType.value;
                    else if (shapeType == ScriptBaseClass.PRIM_PHYSICS_SHAPE_NONE)
                        blocks[0].PhysicsShapeType = (byte)shapeType.value;
                    else //if(shapeType == ScriptBaseClass.PRIM_PHYSICS_SHAPE_CONVEX)
                        blocks[0].PhysicsShapeType = (byte)shapeType.value;
                    blocks[0].Restitution = m_host.Restitution;
                    if (part is ISceneChildEntity)
                        if ((part as ISceneChildEntity).UpdatePrimFlags(UsePhysics,
                            IsTemporary, IsPhantom, IsVolumeDetect, blocks))
                            (part as ISceneChildEntity).ParentEntity.RebuildPhysicalRepresentation(true);
                }
                else if (code == (int)ScriptBaseClass.PRIM_LINK_TARGET)
                {
                    if (remain < 1)
                        return;
                    LSL_Integer nextLink = rules.GetLSLIntegerItem(idx++);
                    List<IEntity> entities = GetLinkPartsAndEntities(nextLink);
                    if (entities.Count > 0)
                        part = entities[0];
                }
            }
        }

        public LSL_String llStringToBase64(string str)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            try
            {
                byte[] encData_byte = new byte[str.Length];
                encData_byte = Util.UTF8.GetBytes(str);
                string encodedData = Convert.ToBase64String(encData_byte);
                return encodedData;
            }
            catch (Exception e)
            {
                throw new Exception("Error in base64Encode" + e);
            }
        }

        public LSL_String llBase64ToString(string str)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            try
            {
                return Util.Base64ToString(str);
            }
            catch (Exception e)
            {
                throw new Exception("Error in base64Decode" + e);
            }
        }

        public LSL_String llXorBase64Strings(string str1, string str2)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            Deprecated("llXorBase64Strings");
            ScriptSleep(300);
            return String.Empty;
        }

        public void llRemoteDataSetRegion()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            Deprecated("llRemoteDataSetRegion");
        }

        public LSL_Float llLog10(double val)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            return Math.Log10(val);
        }

        public LSL_Float llLog(double val)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            return Math.Log(val);
        }

        public LSL_List llGetAnimationList(string id)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_List();


            LSL_List l = new LSL_List();
            IScenePresence av = World.GetScenePresence((UUID)id);
            if (av == null || av.IsChildAgent) // only if in the region
                return l;
            UUID[] anims = av.Animator.GetAnimationArray();
            foreach (UUID foo in anims)
                l.Add(new LSL_Key(foo.ToString()));
            return l;
        }

        public DateTime llSetParcelMusicURL(string url)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                ILandObject land = parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y);

                if (land == null)
                    return DateTime.Now;

                if (!World.Permissions.CanEditParcel(m_host.OwnerID, land))
                    return DateTime.Now;

                land.SetMusicUrl(url);
            }

            return PScriptSleep(2000);
        }

        public LSL_Vector llGetRootPosition()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

            return new LSL_Vector(m_host.ParentEntity.AbsolutePosition.X, m_host.ParentEntity.AbsolutePosition.Y,
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Rotation();

            Quaternion q;
            if (m_host.ParentEntity.RootChild.AttachmentPoint != 0)
            {
                IScenePresence avatar = World.GetScenePresence(m_host.AttachedAvatar);
                if (avatar != null)
                    q = (avatar.AgentControlFlags & (uint)AgentManager.ControlFlags.AGENT_CONTROL_MOUSELOOK) != 0
                            ? avatar.CameraRotation
                            : avatar.Rotation;
                else
                    q = m_host.ParentEntity.GroupRotation; // Likely never get here but just in case
            }
            else
                q = m_host.ParentEntity.GroupRotation; // just the group rotation
            return new LSL_Rotation(q.X, q.Y, q.Z, q.W);
        }

        public LSL_String llGetObjectDesc()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            return m_host.Description ?? String.Empty;
        }

        public void llSetObjectDesc(string desc)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            m_host.Description = desc ?? String.Empty;
        }

        public LSL_String llGetCreator()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            return m_host.CreatorID.ToString();
        }

        public LSL_String llGetTimestamp()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            return DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
        }

        public LSL_Integer llGetNumberOfPrims()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;

            int avatarCount = m_host.ParentEntity.SitTargetAvatar.Count;

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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_List();

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
                        IAvatarAppearanceModule appearance = presence.RequestModuleInterface<IAvatarAppearanceModule>();
                        if (appearance != null)
                        {
                            float height = appearance.Appearance.AvatarHeight / 2.66666667f;
                            lower = new LSL_Vector(-0.3375f, -0.45f, height * -1.0f);
                            upper = new LSL_Vector(0.3375f, 0.45f, 0.0f);
                        }
                    }
                    else
                    {
                        // This is for standing/flying avatars
                        IAvatarAppearanceModule appearance = presence.RequestModuleInterface<IAvatarAppearanceModule>();
                        if (appearance != null)
                        {
                            float height = appearance.Appearance.AvatarHeight / 2.0f;
                            lower = new LSL_Vector(-0.225f, -0.3f, height * -1.0f);
                            upper = new LSL_Vector(0.225f, 0.3f, height + 0.05f);
                        }
                    }
                    result.Add(lower);
                    result.Add(upper);
                    return result;
                }
                // sitting on an object so we need the bounding box of that
                // which should include the avatar so set the UUID to the
                // UUID of the object the avatar is sat on and allow it to fall through
                // to processing an object
                ISceneChildEntity p = World.GetSceneObjectPart(presence.ParentID);
                objID = p.UUID;
            }
            ISceneChildEntity part = World.GetSceneObjectPart(objID);
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

            Vector3 MinPos = new Vector3(100000, 100000, 100000);
            Vector3 MaxPos = new Vector3(-100000, -100000, -100000);
            foreach (ISceneChildEntity child in m_host.ParentEntity.ChildrenEntities())
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_List();

            return GetLinkPrimitiveParams(m_host, rules);
        }

        public LSL_List llGetLinkPrimitiveParams(int linknumber, LSL_List rules)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_List();


            List<ISceneChildEntity> parts = GetLinkParts(linknumber);

            LSL_List res = new LSL_List();

#if (!ISWIN)
            LSL_List result = res;
            foreach (ISceneChildEntity part in parts)
            {
                LSL_List list = GetLinkPrimitiveParams(part, rules);
                result = result + list;
            }
            return result;
#else
            return parts.Select(part => GetLinkPrimitiveParams(part, rules)).Aggregate(res, (current, partRes) => current + partRes);
#endif
        }

        public LSL_List GetLinkPrimitiveParams(ISceneChildEntity part, LSL_List rules)
        {
            LSL_List res = new LSL_List();
            int idx = 0;
            while (idx < rules.Length)
            {
                int code = rules.GetLSLIntegerItem(idx++);
                int remain = rules.Length - idx;
                Primitive.TextureEntry tex = part.Shape.Textures;
                int face = 0;

                if (code == (int)ScriptBaseClass.PRIM_NAME)
                {
                    res.Add(new LSL_String(part.Name));
                }

                else if (code == (int)ScriptBaseClass.PRIM_DESC)
                {
                    res.Add(new LSL_String(part.Description));
                }

                else if (code == (int)ScriptBaseClass.PRIM_MATERIAL)
                {
                    res.Add(new LSL_Integer(part.Material));
                }

                else if (code == (int)ScriptBaseClass.PRIM_PHYSICS)
                {
                    res.Add((part.GetEffectiveObjectFlags() & (uint)PrimFlags.Physics) != 0
                                ? new LSL_Integer(1)
                                : new LSL_Integer(0));
                }

                else if (code == (int)ScriptBaseClass.PRIM_TEMP_ON_REZ)
                {
                    res.Add((part.GetEffectiveObjectFlags() & (uint)PrimFlags.TemporaryOnRez) != 0
                                ? new LSL_Integer(1)
                                : new LSL_Integer(0));
                }

                else if (code == (int)ScriptBaseClass.PRIM_PHANTOM)
                {
                    res.Add((part.GetEffectiveObjectFlags() & (uint)PrimFlags.Phantom) != 0
                                ? new LSL_Integer(1)
                                : new LSL_Integer(0));
                }

                else if (code == (int)ScriptBaseClass.PRIM_POSITION)
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
                else if (code == (int)ScriptBaseClass.PRIM_POS_LOCAL)
                {
                    res.Add(GetLocalPos(part));
                }
                else if (code == (int)ScriptBaseClass.PRIM_SIZE)
                {
                    Vector3 tmp = part.Scale;
                    res.Add(new LSL_Vector(tmp.X,
                                                  tmp.Y,
                                                  tmp.Z));
                }

                else if (code == (int)ScriptBaseClass.PRIM_ROTATION)
                {
                    res.Add(GetPartRot(part));
                }

                else if (code == (int)ScriptBaseClass.PRIM_TYPE)
                {
                    // implementing box
                    PrimitiveBaseShape Shape = part.Shape;
                    int primType = (int)part.GetPrimType();
                    res.Add(new LSL_Integer(primType));
                    double topshearx = (sbyte)Shape.PathShearX / 100.0; // Fix negative values for PathShearX
                    double topsheary = (sbyte)Shape.PathShearY / 100.0; // and PathShearY.
                    if (primType == ScriptBaseClass.PRIM_TYPE_BOX ||
                       primType == ScriptBaseClass.PRIM_TYPE_CYLINDER ||
                       primType == ScriptBaseClass.PRIM_TYPE_PRISM)
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
                       primType == ScriptBaseClass.PRIM_TYPE_TUBE ||
                       primType == ScriptBaseClass.PRIM_TYPE_TORUS)
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
                        res.Add(new LSL_Float(Math.Round(Shape.PathRevolutions * 0.015d, 2, MidpointRounding.AwayFromZero)) + 1.0d);
                        // Slightly inaccurate, because an unsigned byte is being used to represent
                        // the entire range of floating-point values from 1.0 through 4.0 (which is how
                        // SL does it).
                        //
                        // Using these formulas to store and retrieve PathRevolutions, it is not
                        // possible to use all values between 1.00 and 4.00. For instance, you can't
                        // represent 1.10. You can represent 1.09 and 1.11, but not 1.10. So, if you
                        // use llSetPrimitiveParams to set revolutions to 1.10 and then retreive them
                        // with llGetPrimitiveParams, you'll retrieve 1.09. You can also see a similar
                        // behavior in the viewer as you cannot set 1.10. The viewer jumps to 1.11.
                        // In SL, llSetPrimitveParams and llGetPrimitiveParams can set and get a value
                        // such as 1.10. So, SL must store and retreive the actual user input rather
                        // than only storing the encoded value.

                        // float radiusoffset
                        res.Add(new LSL_Float(Shape.PathRadiusOffset / 100.0));

                        // float skew
                        res.Add(new LSL_Float(Shape.PathSkew / 100.0));
                    }
                }

                else if (code == (int)ScriptBaseClass.PRIM_TEXTURE)
                {
                    if (remain < 1)
                        return res;
                    face = rules.GetLSLIntegerItem(idx++);
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

                else if (code == (int)ScriptBaseClass.PRIM_COLOR)
                {
                    if (remain < 1)
                        return res;
                    face = rules.GetLSLIntegerItem(idx++);
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

                else if (code == (int)ScriptBaseClass.PRIM_BUMP_SHINY)
                {
                    if (remain < 1)
                        return res;

                    face = rules.GetLSLIntegerItem(idx++);

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

                else if (code == (int)ScriptBaseClass.PRIM_FULLBRIGHT)
                {
                    if (remain < 1)
                        return res;

                    face = rules.GetLSLIntegerItem(idx++);
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

                else if (code == (int)ScriptBaseClass.PRIM_FLEXIBLE)
                {
                    PrimitiveBaseShape shape = part.Shape;

                    res.Add(shape.FlexiEntry ? new LSL_Integer(1) : new LSL_Integer(0));
                    res.Add(new LSL_Integer(shape.FlexiSoftness));// softness
                    res.Add(new LSL_Float(shape.FlexiGravity));   // gravity
                    res.Add(new LSL_Float(shape.FlexiDrag));      // friction
                    res.Add(new LSL_Float(shape.FlexiWind));      // wind
                    res.Add(new LSL_Float(shape.FlexiTension));   // tension
                    res.Add(new LSL_Vector(shape.FlexiForceX,       // force
                                           shape.FlexiForceY,
                                           shape.FlexiForceZ));
                }

                else if (code == (int)ScriptBaseClass.PRIM_TEXGEN)
                {
                    if (remain < 1)
                        return res;

                    face = rules.GetLSLIntegerItem(idx++);
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

                else if (code == (int)ScriptBaseClass.PRIM_POINT_LIGHT)
                {
                    PrimitiveBaseShape shape = part.Shape;

                    res.Add(shape.LightEntry ? new LSL_Integer(1) : new LSL_Integer(0));
                    res.Add(new LSL_Vector(shape.LightColorR,       // color
                                           shape.LightColorG,
                                           shape.LightColorB));
                    res.Add(new LSL_Float(shape.LightIntensity)); // intensity
                    res.Add(new LSL_Float(shape.LightRadius));    // radius
                    res.Add(new LSL_Float(shape.LightFalloff));   // falloff
                }

                else if (code == (int)ScriptBaseClass.PRIM_GLOW)
                {
                    if (remain < 1)
                        return res;

                    face = rules.GetLSLIntegerItem(idx++);
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

                else if (code == (int)ScriptBaseClass.PRIM_TEXT)
                {
                    Color4 textColor = part.GetTextColor();
                    res.Add(new LSL_String(part.Text));
                    res.Add(new LSL_Vector(textColor.R,
                                           textColor.G,
                                           textColor.B));
                    res.Add(new LSL_Float(textColor.A));
                }
                else if (code == (int)ScriptBaseClass.PRIM_ROT_LOCAL)
                {
                    Quaternion rtmp = part.RotationOffset;
                    res.Add(new LSL_Rotation(rtmp.X, rtmp.Y, rtmp.Z, rtmp.W));
                }
                else if (code == (int)ScriptBaseClass.PRIM_OMEGA)
                {
                    Vector3 axis = part.OmegaAxis;
                    LSL_Float spinRate = part.OmegaSpinRate;
                    LSL_Float gain = part.OmegaGain;
                    res.Add(axis);
                    res.Add(spinRate);
                    res.Add(gain);
                }
                else if (code == (int)ScriptBaseClass.PRIM_PHYSICS_SHAPE_TYPE)
                {
                    res.Add(new LSL_Integer(part.PhysicsType));
                }
                else if (code == (int)ScriptBaseClass.PRIM_LINK_TARGET)
                {
                    if (remain < 1)
                        continue;
                    LSL_Integer nextLink = rules.GetLSLIntegerItem(idx++);
                    List<ISceneChildEntity> entities = GetLinkParts(nextLink);
                    if (entities.Count > 0)
                        part = entities[0];
                }
            }
            return res;
        }

        public LSL_List llGetPhysicsMaterial()
        {
            return new LSL_List(m_host.GravityMultiplier, m_host.Restitution, m_host.Friction, m_host.Density);
        }

        public void llSetPhysicsMaterial(LSL_Integer bits, LSL_Float density, LSL_Float friction, LSL_Float restitution,
            LSL_Float gravityMultiplier)
        {
            ObjectFlagUpdatePacket.ExtraPhysicsBlock[] blocks = new ObjectFlagUpdatePacket.ExtraPhysicsBlock[1];
            blocks[0] = new ObjectFlagUpdatePacket.ExtraPhysicsBlock();
            if ((bits & ScriptBaseClass.DENSITY) == ScriptBaseClass.DENSITY)
                m_host.Density = (float)density;
            else
                blocks[0].Density = m_host.Density;

            if ((bits & ScriptBaseClass.FRICTION) == ScriptBaseClass.FRICTION)
                m_host.Friction = (float)friction;
            else
                blocks[0].Friction = m_host.Friction;

            if ((bits & ScriptBaseClass.RESTITUTION) == ScriptBaseClass.RESTITUTION)
                m_host.Restitution = (float)restitution;
            else
                blocks[0].Restitution = m_host.Restitution;

            if ((bits & ScriptBaseClass.GRAVITY_MULTIPLIER) == ScriptBaseClass.GRAVITY_MULTIPLIER)
                m_host.GravityMultiplier = (float)gravityMultiplier;
            else
                blocks[0].GravityMultiplier = m_host.GravityMultiplier;

            bool UsePhysics = ((m_host.Flags & PrimFlags.Physics) != 0);
            bool IsTemporary = ((m_host.Flags & PrimFlags.TemporaryOnRez) != 0);
            bool IsPhantom = ((m_host.Flags & PrimFlags.Phantom) != 0);
            bool IsVolumeDetect = m_host.VolumeDetectActive;
            blocks[0].PhysicsShapeType = m_host.PhysicsType;
            if (m_host.UpdatePrimFlags(UsePhysics, IsTemporary, IsPhantom, IsVolumeDetect, blocks))
                m_host.ParentEntity.RebuildPhysicalRepresentation(true);
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

            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";


            // Manually unroll the loop

            imdt[7] = '=';
            imdt[6] = '=';
            imdt[5] = i2ctable[number << 4 & 0x3F];
            imdt[4] = i2ctable[number >> 2 & 0x3F];
            imdt[3] = i2ctable[number >> 8 & 0x3F];
            imdt[2] = i2ctable[number >> 14 & 0x3F];
            imdt[1] = i2ctable[number >> 20 & 0x3F];
            imdt[0] = i2ctable[number >> 26 & 0x3F];

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

            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;


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
                return digit < 0 ? 0 : number;
            }
            number += --digit << 26;

            if ((digit = c2itable[str[1]]) <= 0)
            {
                return digit < 0 ? 0 : number;
            }
            number += --digit << 20;

            if ((digit = c2itable[str[2]]) <= 0)
            {
                return digit < 0 ? 0 : number;
            }
            number += --digit << 14;

            if ((digit = c2itable[str[3]]) <= 0)
            {
                return digit < 0 ? 0 : number;
            }
            number += --digit << 8;

            if ((digit = c2itable[str[4]]) <= 0)
            {
                return digit < 0 ? 0 : number;
            }
            number += --digit << 2;

            if ((digit = c2itable[str[5]]) <= 0)
            {
                return digit < 0 ? 0 : number;
            }
            number += --digit >> 4;

            // ignore trailing padding

            return number;
        }

        public LSL_Float llGetGMTclock()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            return DateTime.UtcNow.TimeOfDay.TotalSeconds;
        }

        public LSL_String llGetHTTPHeader(LSL_Key request_id, string header)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";


            if (m_UrlModule != null)
                return m_UrlModule.GetHttpHeader(request_id, header);
            return String.Empty;
        }


        public LSL_String llGetSimulatorHostname()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            IUrlModule UrlModule = World.RequestModuleInterface<IUrlModule>();
            return UrlModule.ExternalHostNameForLSL;
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

            //    Initial capacity reduces resize cost

            LSL_List tokens = new LSL_List();

            //    All entries are initially valid

            for (int i = 0; i < mlen; i++)
                active[i] = true;

            offset[mlen] = srclen;

            while (beginning < srclen)
            {

                int best = mlen;

                //    Scan for separators

                int j;
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
                    if ((keepNulls) || ((srclen - beginning) > 0))
                        tokens.Add(new LSL_String(src.Substring(beginning, srclen - beginning)));
                    break;
                }

                //    Otherwise we just add the newly delimited token
                //    and recalculate where the search should continue.
                if ((keepNulls) || ((offset[best] - beginning) > 0))
                    tokens.Add(new LSL_String(src.Substring(beginning, offset[best] - beginning)));

                if (best < seplen)
                {
                    beginning = offset[best] + (separray[best].ToString()).Length;
                }
                else
                {
                    beginning = offset[best] + (spcarray[best - seplen].ToString()).Length;
                    string str = spcarray[best - seplen].ToString();
                    if ((keepNulls) || ((str.Length > 0)))
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "llParseString2List", m_host, "LSL", m_itemID)) return new LSL_List();
            return ParseString(src, separators, spacers, false);
        }

        public LSL_List llParseStringKeepNulls(string src, LSL_List separators, LSL_List spacers)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "llParseStringKeepNulls", m_host, "LSL", m_itemID)) return new LSL_List();
            return ParseString(src, separators, spacers, true);
        }

        public LSL_Integer llGetObjectPermMask(int mask)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Integer();


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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;


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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";


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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            IChatModule chatModule = World.RequestModuleInterface<IChatModule>();
            if (chatModule != null)
                chatModule.SimChatBroadcast(msg, ChatTypeEnum.Owner, 0,
                                   m_host.AbsolutePosition, m_host.Name, m_host.UUID, false, UUID.Zero, World);
        }

        public LSL_String llRequestSecureURL()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            if (m_UrlModule != null)
                return m_UrlModule.RequestSecureURL(m_ScriptEngine.ScriptModule, m_host, m_itemID).ToString();
            return UUID.Zero.ToString();
        }

        public LSL_String llGetEnv(LSL_String name)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            if (name == "sim_channel")
                return "Aurora-Sim Server";
            if (name == "sim_version")
                return World.RequestModuleInterface<ISimulationBase>().Version;
            return "";
        }

        public void llTeleportAgent(LSL_Key avatar, LSL_String landmark, LSL_Vector position, LSL_Vector look_at)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            UUID invItemID = InventorySelf();

            if (invItemID == UUID.Zero)
                return;

            lock (m_host.TaskInventory)
            {
                if (m_host.TaskInventory[invItemID].PermsGranter == UUID.Zero)
                {
                    ShoutError("No permissions to teleport the agent");
                    return;
                }

                if ((m_host.TaskInventory[invItemID].PermsMask & ScriptBaseClass.PERMISSION_TELEPORT) == 0)
                {
                    ShoutError("No permissions to teleport the agent");
                    return;
                }
            }

            TaskInventoryItem item = null;
            lock (m_host.TaskInventory)
            {
                foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory)
                {
                    if (inv.Value.Name == landmark)
                        item = inv.Value;
                }
            }
            if (item == null && landmark != "")
                return;

            IScenePresence presence = World.GetScenePresence(m_host.OwnerID);
            if (presence != null)
            {
                IEntityTransferModule module = World.RequestModuleInterface<IEntityTransferModule>();
                if (module != null)
                {
                    if (landmark == "")
                        module.Teleport(presence, World.RegionInfo.RegionHandle,
                            position.ToVector3(), look_at.ToVector3(), (uint)TeleportFlags.ViaLocation);
                    else
                    {
                        AssetLandmark lm = new AssetLandmark(
                            World.AssetService.Get(item.AssetID.ToString()));
                        module.Teleport(presence, lm.RegionHandle, lm.Position,
                            look_at.ToVector3(), (uint)TeleportFlags.ViaLocation);
                    }
                }
            }
        }

        public void llTeleportAgentGlobalCoords(LSL_Key agent, LSL_Vector global_coordinates,
            LSL_Vector region_coordinates, LSL_Vector look_at)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            UUID invItemID = InventorySelf();

            if (invItemID == UUID.Zero)
                return;

            lock (m_host.TaskInventory)
            {
                if (m_host.TaskInventory[invItemID].PermsGranter == UUID.Zero)
                {
                    ShoutError("No permissions to teleport the agent");
                    return;
                }

                if ((m_host.TaskInventory[invItemID].PermsMask & ScriptBaseClass.PERMISSION_TELEPORT) == 0)
                {
                    ShoutError("No permissions to teleport the agent");
                    return;
                }
            }

            IScenePresence presence = World.GetScenePresence(m_host.OwnerID);
            if (presence != null)
            {
                IEntityTransferModule module = World.RequestModuleInterface<IEntityTransferModule>();
                if (module != null)
                {
                    module.Teleport(presence, Utils.UIntsToLong((uint)global_coordinates.x, (uint)global_coordinates.y),
                        region_coordinates.ToVector3(), look_at.ToVector3(), (uint)TeleportFlags.ViaLocation);
                }
            }
        }

        public LSL_Key llRequestSimulatorData(string simulator, int data)
        {
            UUID tid = UUID.Zero;

            try
            {
                if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

                string reply = String.Empty;

                GridRegion info = World.RegionInfo.RegionName == simulator
                                      ? new GridRegion(World.RegionInfo)
                                      : World.GridService.GetRegionByName(World.RegionInfo.AllScopeIDs, simulator);


                switch (data)
                {
                    case 5: // DATA_SIM_POS
                        if (info == null)
                            break;

                        reply = new LSL_Vector(
                            info.RegionLocX,
                            info.RegionLocY,
                            0).ToString();
                        break;
                    case 6: // DATA_SIM_STATUS
                        if (info != null)
                        {
                            reply = (info.Flags & (int)Framework.RegionFlags.RegionOnline) != 0 ? "up" : "down";
                        }
                        //if() starting
                        //if() stopping
                        //if() crashed
                        else
                            reply = "unknown";
                        break;
                    case 7: // DATA_SIM_RATING
                        if (info == null)
                            break;

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
                        try
                        {
                            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "llRequestSimulatorData", m_host, "LSL", m_itemID)) return "";
                            reply = "Aurora";
                        }
                        catch
                        {
                            reply = "";
                        }
                        break;
                }
                if (reply != "")
                {
                    UUID rq = UUID.Random();

                    DataserverPlugin dataserverPlugin = (DataserverPlugin)m_ScriptEngine.GetScriptPlugin("Dataserver");

                    tid = dataserverPlugin.RegisterRequest(m_host.UUID, m_itemID, rq.ToString());

                    dataserverPlugin.AddReply(rq.ToString(), reply, 1000);
                }
            }
            catch
            {
            }

            ScriptSleep(1000);
            return (LSL_Key)tid.ToString();
        }

        public LSL_String llRequestURL()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";


            if (m_UrlModule != null)
                return m_UrlModule.RequestURL(m_ScriptEngine.ScriptModule, m_host, m_itemID).ToString();
            return UUID.Zero.ToString();
        }

        public void llForceMouselook(int mouselook)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_List();


            // Note that although we have normalized, both
            // indices could still be negative.
            if (start < 0)
            {
                start = start + dest.Length;
            }

            if (end < 0)
            {
                end = end + dest.Length;
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
                    LSL_List pref = dest.GetSublist(0, start - 1);
                    // Only add a suffix if there is something
                    // beyond the end index (it's inclusive too).
                    if (end + 1 < dest.Length)
                    {
                        return pref + src + dest.GetSublist(end + 1, -1);
                    }
                    return pref + src;
                }
                // If start is less than or equal to zero, then
                // the new list is simply a prefix. We still need to
                // figure out any necessary surgery to the destination
                // based upon end. Note that if end exceeds the upper
                // bound in this case, the entire destination list
                // is removed.
                if (end + 1 < dest.Length)
                {
                    return src + dest.GetSublist(end + 1, -1);
                }
                return src;
            }
            // Finally, if start > end, we strip away a prefix and
            // a suffix, to leave the list that sits <between> ens
            // and start, and then tag on the src list. AT least
            // that's my interpretation. We can get sublist to do
            // this for us. Note that one, or both of the indices
            // might have been negative.
            return dest.GetSublist(end + 1, start - 1) + src;
        }

        public DateTime llLoadURL(string avatar_id, string message, string url)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;


            IDialogModule dm = World.RequestModuleInterface<IDialogModule>();
            if (null != dm)
                dm.SendUrlToUser(
                    new UUID(avatar_id), m_host.Name, m_host.UUID, m_host.OwnerID, false, message, url);

            return PScriptSleep(10000);
        }

        public DateTime llParcelMediaCommandList(LSL_List commandList)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;


            // according to the docs, this command only works if script owner and land owner are the same
            // lets add estate owners and gods, too, and use the generic permission check.
            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                ILandObject landObject = parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y);
                if (landObject == null)
                    return DateTime.Now;
                if (!World.Permissions.CanEditParcel(m_host.OwnerID, landObject))
                    return DateTime.Now;

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
                    int tmp = ((LSL_Integer)commandList.Data[i]).value;
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
                            NotImplemented("llParcelMediaCommandList parameter not supported yet: " + Enum.Parse(typeof(ParcelMediaCommandEnum), commandList.Data[i].ToString()));
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
            return PScriptSleep(2000);
        }

        public LSL_List llParcelMediaQuery(LSL_List aList)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_List();

            LSL_List list = new LSL_List();
            foreach (object t in aList.Data)
            {
                if (t != null)
                {
                    IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
                    if (parcelManagement != null)
                    {
                        LSL_Integer tmp = (LSL_Integer)t;
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
                                const ParcelMediaCommandEnum mediaCommandEnum = ParcelMediaCommandEnum.Url;
                                NotImplemented("llParcelMediaQuery parameter do not supported yet: " + Enum.Parse(mediaCommandEnum.GetType(), t.ToString()));
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_List();
            ScriptSleep(1000);

            // LSL Spec http://wiki.secondlife.com/wiki/LlGetPrimMediaParams says to fail silently if face is invalid
            // TODO: Need to correctly handle case where a face has no media (which gives back an empty list).
            // Assuming silently fail means give back an empty list.  Ideally, need to check this.
            if (face < 0 || face > m_host.GetNumberOfSides() - 1)
                return new LSL_List();
            return GetPrimMediaParams(m_host, face, rules);
        }

        public LSL_List llGetLinkMedia(LSL_Integer link, LSL_Integer face, LSL_List rules)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_List();

            List<ISceneChildEntity> entities = GetLinkParts(link);
            if (entities.Count == 0 || face < 0 || face > entities[0].GetNumberOfSides() - 1)
                return new LSL_List();
            LSL_List res = new LSL_List();

#if (!ISWIN)
            LSL_List result = res;
            foreach (ISceneChildEntity part in entities)
            {
                LSL_List list = GetPrimMediaParams(part, face, rules);
                result = result + list;
            }
            return result;
#else
            return entities.Select(part => GetPrimMediaParams(part, face, rules)).Aggregate(res,
                                                                                            (current, partRes) =>
                                                                                            current + partRes);
#endif
        }

        private LSL_List GetPrimMediaParams(ISceneChildEntity obj, int face, LSL_List rules)
        {
            IMoapModule module = World.RequestModuleInterface<IMoapModule>();
            if (null == module)
                throw new Exception("Media on a prim functions not available");

            MediaEntry me = module.GetMediaEntry(obj, face);

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
                    res.Add(me.Controls == MediaControls.Standard
                                ? new LSL_Integer(ScriptBaseClass.PRIM_MEDIA_CONTROLS_STANDARD)
                                : new LSL_Integer(ScriptBaseClass.PRIM_MEDIA_CONTROLS_MINI));
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;
            ScriptSleep(1000);

            ClearPrimMedia(m_host, face);

            return ScriptBaseClass.LSL_STATUS_OK;
        }

        public LSL_Integer llClearLinkMedia(LSL_Integer link, LSL_Integer face)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;
            ScriptSleep(1000);

            List<ISceneChildEntity> entities = GetLinkParts(link);
            if (entities.Count == 0 || face < 0 || face > entities[0].GetNumberOfSides() - 1)
                return ScriptBaseClass.LSL_STATUS_OK;

            foreach (ISceneChildEntity child in entities)
                ClearPrimMedia(child, face);

            return ScriptBaseClass.LSL_STATUS_OK;
        }

        private void ClearPrimMedia(ISceneChildEntity entity, LSL_Integer face)
        {
            // LSL Spec http://wiki.secondlife.com/wiki/LlClearPrimMedia says to fail silently if face is invalid
            // Assuming silently fail means sending back LSL_STATUS_OK.  Ideally, need to check this.
            // FIXME: Don't perform the media check directly
            if (face < 0 || face > entity.GetNumberOfSides() - 1)
                return;

            IMoapModule module = World.RequestModuleInterface<IMoapModule>();
            if (null == module)
                throw new Exception("Media on a prim functions not available");

            module.ClearMediaEntry(entity, face);
        }

        public LSL_Integer llSetPrimMediaParams(LSL_Integer face, LSL_List rules)
        {
            ScriptSleep(1000);

            // LSL Spec http://wiki.secondlife.com/wiki/LlSetPrimMediaParams says to fail silently if face is invalid
            // Assuming silently fail means sending back LSL_STATUS_OK.  Ideally, need to check this.
            // Don't perform the media check directly
            if (face < 0 || face > m_host.GetNumberOfSides() - 1)
                return ScriptBaseClass.LSL_STATUS_OK;
            return SetPrimMediaParams(m_host, face, rules);
        }

        public LSL_Integer llSetLinkMedia(LSL_Integer link, LSL_Integer face, LSL_List rules)
        {
            ScriptSleep(1000);

            // LSL Spec http://wiki.secondlife.com/wiki/LlSetPrimMediaParams says to fail silently if face is invalid
            // Assuming silently fail means sending back LSL_STATUS_OK.  Ideally, need to check this.
            // Don't perform the media check directly
            List<ISceneChildEntity> entities = GetLinkParts(link);
            if (entities.Count == 0 || face < 0 || face > entities[0].GetNumberOfSides() - 1)
                return ScriptBaseClass.LSL_STATUS_OK;
            foreach (ISceneChildEntity child in entities)
                SetPrimMediaParams(child, face, rules);
            return ScriptBaseClass.LSL_STATUS_OK;
        }

        public LSL_Integer SetPrimMediaParams(ISceneChildEntity obj, int face, LSL_List rules)
        {
            IMoapModule module = World.RequestModuleInterface<IMoapModule>();
            if (null == module)
                throw new Exception("Media on a prim functions not available");

            MediaEntry me = module.GetMediaEntry(obj, face) ?? new MediaEntry();

            int i = 0;

            while (i < rules.Length - 1)
            {
                int code = rules.GetLSLIntegerItem(i++);

                if (code == ScriptBaseClass.PRIM_MEDIA_ALT_IMAGE_ENABLE)
                {
                    me.EnableAlterntiveImage = (rules.GetLSLIntegerItem(i++) != 0);
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_CONTROLS)
                {
                    int v = rules.GetLSLIntegerItem(i++);
                    me.Controls = ScriptBaseClass.PRIM_MEDIA_CONTROLS_STANDARD == v
                                      ? MediaControls.Standard
                                      : MediaControls.Mini;
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
                    me.AutoLoop = (ScriptBaseClass.TRUE == rules.GetLSLIntegerItem(i++));
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_AUTO_PLAY)
                {
                    me.AutoPlay = (ScriptBaseClass.TRUE == rules.GetLSLIntegerItem(i++));
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_AUTO_SCALE)
                {
                    me.AutoScale = (ScriptBaseClass.TRUE == rules.GetLSLIntegerItem(i++));
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_AUTO_ZOOM)
                {
                    me.AutoZoom = (ScriptBaseClass.TRUE == rules.GetLSLIntegerItem(i++));
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_FIRST_CLICK_INTERACT)
                {
                    me.InteractOnFirstClick = (ScriptBaseClass.TRUE == rules.GetLSLIntegerItem(i++));
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_WIDTH_PIXELS)
                {
                    me.Width = rules.GetLSLIntegerItem(i++);
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_HEIGHT_PIXELS)
                {
                    me.Height = rules.GetLSLIntegerItem(i++);
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_WHITELIST_ENABLE)
                {
                    me.EnableWhiteList = (ScriptBaseClass.TRUE == rules.GetLSLIntegerItem(i++));
                }
                else if (code == ScriptBaseClass.PRIM_MEDIA_WHITELIST)
                {
                    string[] rawWhiteListUrls = rules.GetLSLStringItem(i++).ToString().Split(new[] { ',' });
                    List<string> whiteListUrls = new List<string>();
#if (!ISWIN)
                    Array.ForEach(
                        rawWhiteListUrls, delegate(string rawUrl) { whiteListUrls.Add(rawUrl.Trim()); });
#else
                    Array.ForEach(
                        rawWhiteListUrls, rawUrl => whiteListUrls.Add(rawUrl.Trim()));
#endif
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

            module.SetMediaEntry(obj, face, me);

            return ScriptBaseClass.LSL_STATUS_OK;
        }

        public LSL_Integer llModPow(int a, int b, int c)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;

            Int64 tmp = 0;
            Math.DivRem(Convert.ToInt64(Math.Pow(a, b)), c, out tmp);
            ScriptSleep(100);
            return Convert.ToInt32(tmp);
        }

        public LSL_Integer llGetInventoryType(string name)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;


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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;
            m_host.ParentEntity.RootChild.PayPrice[0] = price;

            if (quick_pay_buttons.Data.Length > 0)
                m_host.ParentEntity.RootChild.PayPrice[1] = (LSL_Integer)quick_pay_buttons.Data[0];
            else
                m_host.ParentEntity.RootChild.PayPrice[1] = (LSL_Integer)(-2);
            if (quick_pay_buttons.Data.Length > 1)
                m_host.ParentEntity.RootChild.PayPrice[2] = (LSL_Integer)quick_pay_buttons.Data[1];
            else
                m_host.ParentEntity.RootChild.PayPrice[2] = (LSL_Integer)(-2);
            if (quick_pay_buttons.Data.Length > 2)
                m_host.ParentEntity.RootChild.PayPrice[3] = (LSL_Integer)quick_pay_buttons.Data[2];
            else
                m_host.ParentEntity.RootChild.PayPrice[3] = (LSL_Integer)(-2);
            if (quick_pay_buttons.Data.Length > 3)
                m_host.ParentEntity.RootChild.PayPrice[4] = (LSL_Integer)quick_pay_buttons.Data[3];
            else
                m_host.ParentEntity.RootChild.PayPrice[4] = (LSL_Integer)(-2);
        }

        public LSL_Vector llGetCameraPos()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Vector();

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

            IScenePresence presence = World.GetScenePresence(m_host.OwnerID);
            if (presence != null)
            {
                LSL_Vector pos = new LSL_Vector(presence.CameraPosition.X, presence.CameraPosition.Y, presence.CameraPosition.Z);
                return pos;
            }
            return new LSL_Vector();
        }

        public LSL_Rotation llGetCameraRot()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Rotation();

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

            IScenePresence presence = World.GetScenePresence(m_host.OwnerID);
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
        public DateTime llSetPrimURL(string url)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            return PScriptSleep(2000);
        }

        /// <summary>
        /// The SL implementation shouts an error, it is deprecated
        /// This duplicates SL
        /// </summary>
        public DateTime llRefreshPrimURL()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            Deprecated("llRefreshPrimURL");
            return PScriptSleep(20000);
        }

        public LSL_String llEscapeURL(string url)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_String();

            try
            {
                return Uri.EscapeDataString(url);
            }
            catch (Exception ex)
            {
                return "llEscapeURL: " + ex;
            }
        }

        public LSL_String llUnescapeURL(string url)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            try
            {
                return Uri.UnescapeDataString(url);
            }
            catch (Exception ex)
            {
                return "llUnescapeURL: " + ex;
            }
        }

        public DateTime llMapDestination(string simname, LSL_Vector pos, LSL_Vector lookAt)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            UUID avatarID = m_host.OwnerID;
            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_host.UUID, m_itemID, 0);
            // only works on the first detected avatar
            //This only works in touch events or if the item is attached to the avatar
            if (detectedParams == null && !m_host.IsAttachment) return DateTime.Now;

            if (detectedParams != null)
                avatarID = detectedParams.Key;

            IScenePresence avatar = World.GetScenePresence(avatarID);
            if (avatar != null)
            {
                IMuteListModule module = m_host.ParentEntity.Scene.RequestModuleInterface<IMuteListModule>();
                if (module != null)
                {
                    bool cached = false; //Unneeded
#if (!ISWIN)
                    foreach (MuteList mute in module.GetMutes(avatar.UUID, out cached))
                        if (mute.MuteID == m_host.OwnerID)
                            return DateTime.Now;//If the avatar is muted, they don't get any contact from the muted av
#else
                    if (module.GetMutes(avatar.UUID, out cached).Any(mute => mute.MuteID == m_host.OwnerID))
                    {
                        return DateTime.Now;//If the avatar is muted, they don't get any contact from the muted av
                    }
#endif
                }
                avatar.ControllingClient.SendScriptTeleportRequest(m_host.Name, simname,
                                                                   new Vector3((float)pos.x, (float)pos.y, (float)pos.z),
                                                                   new Vector3((float)lookAt.x, (float)lookAt.y, (float)lookAt.z));
            }
            return PScriptSleep(1000);
        }

        public DateTime llAddToLandBanList(string avatar, double hours)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                LandData land = parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y).LandData;
                if (land.OwnerID == m_host.OwnerID)
                {
                    ParcelManager.ParcelAccessEntry entry = new ParcelManager.ParcelAccessEntry();
                    UUID key;
                    if (UUID.TryParse(avatar, out key))
                    {
                        entry.AgentID = key;
                        entry.Flags = AccessList.Ban;
                        entry.Time = DateTime.Now.AddHours(hours);
                        land.ParcelAccessList.Add(entry);
                    }
                }
            }
            return PScriptSleep(100);
        }

        public DateTime llRemoveFromLandPassList(string avatar)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                LandData land = parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y).LandData;
                if (land.OwnerID == m_host.OwnerID)
                {
                    UUID key;
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
            return PScriptSleep(100);
        }

        public DateTime llRemoveFromLandBanList(string avatar)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                LandData land = parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y).LandData;
                if (land.OwnerID == m_host.OwnerID)
                {
                    UUID key;
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
            return PScriptSleep(100);
        }

        public void llSetCameraParams(LSL_List rules)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


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

            IScenePresence presence = World.GetScenePresence(agentID);

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
                        parameters.Add(type, ((LSL_Integer)data[i]).value);
                    else parameters.Add(type, Convert.ToSingle(data[i]));
                }
            }
            if (parameters.Count > 0) presence.ControllingClient.SendSetFollowCamProperties(objectID, parameters);
        }

        public void llClearCameraParams()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


            // our key in the object we are in
            UUID invItemID = InventorySelf();
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

            IScenePresence presence = World.GetScenePresence(agentID);

            // we are not interested in child-agents
            if (presence.IsChildAgent) return;

            presence.ControllingClient.SendClearFollowCamProperties(objectID);
        }

        public LSL_Float llListStatistics(int operation, LSL_List src)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_Float();

            LSL_List nums = LSL_List.ToDoubleList(src);
            if (operation == ScriptBaseClass.LIST_STAT_RANGE)
                return nums.Range();
            if (operation == ScriptBaseClass.LIST_STAT_MIN)
                return nums.Min();
            if (operation == ScriptBaseClass.LIST_STAT_MAX)
                return nums.Max();
            if (operation == ScriptBaseClass.LIST_STAT_MEAN)
                return nums.Mean();
            if (operation == ScriptBaseClass.LIST_STAT_MEDIAN)
                return nums.Median();
            if (operation == ScriptBaseClass.LIST_STAT_NUM_COUNT)
                return nums.NumericLength();
            if (operation == ScriptBaseClass.LIST_STAT_STD_DEV)
                return nums.StdDev();
            if (operation == ScriptBaseClass.LIST_STAT_SUM)
                return nums.Sum();
            if (operation == ScriptBaseClass.LIST_STAT_SUM_SQUARES)
                return nums.SumSqrs();
            if (operation == ScriptBaseClass.LIST_STAT_GEOMETRIC_MEAN)
                return nums.GeometricMean();
            if (operation == ScriptBaseClass.LIST_STAT_HARMONIC_MEAN)
                return nums.HarmonicMean();
            return 0.0;
        }

        public LSL_Integer llGetUnixTime()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;

            return Util.UnixTimeSinceEpoch();
        }

        public LSL_Integer llGetParcelFlags(LSL_Vector pos)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;
            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                return (int)parcelManagement.GetLandObject((float)pos.x, (float)pos.y).LandData.Flags;
            }
            return 0;
        }

        public LSL_Integer llGetRegionFlags()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;

            IEstateModule estate = World.RequestModuleInterface<IEstateModule>();
            if (estate == null)
                return 67108864;
            return (int)estate.GetRegionFlags();
        }

        public LSL_String llXorBase64StringsCorrect(string str1, string str2)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            string ret = String.Empty;
            string src1 = llBase64ToString(str1);
            string src2 = llBase64ToString(str2);
            int c = 0;
            foreach (char t in src1)
            {
                ret += (char)(t ^ src2[c]);

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

            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            IHttpRequestModule httpScriptMod =
                World.RequestModuleInterface<IHttpRequestModule>();
#if (!ISWIN)
            List<string> param = new List<string>();
            foreach (object o in parameters.Data)
                param.Add(o.ToString());
#else
            List<string> param = parameters.Data.Select(o => o.ToString()).ToList();
#endif

            Vector3 position = m_host.AbsolutePosition;
            Vector3 velocity = m_host.Velocity;
            Quaternion rotation = m_host.RotationOffset;
            string ownerName = String.Empty;
            IScenePresence scenePresence = World.GetScenePresence(m_host.OwnerID);
            ownerName = scenePresence == null ? resolveName(m_host.OwnerID) : scenePresence.Name;

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
                userAgent = config.Configs["LSLRemoting"].GetString("user_agent", null);

            if (userAgent != null)
                httpHeaders["User-Agent"] = userAgent;

            const string authregex = @"^(https?:\/\/)(\w+):(\w+)@(.*)$";
            Regex r = new Regex(authregex);
            Match m = r.Match(url);
            if (m.Success)
            {
                //for (int i = 1; i < gnums.Length; i++) {
                //    //System.Text.RegularExpressions.Group g = m.Groups[gnums[i]];
                //    //CaptureCollection cc = g.Captures;
                //}
                if (m.Groups.Count == 5)
                {
                    httpHeaders["Authorization"] = String.Format("Basic {0}", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(m.Groups[2].ToString() + ":" + m.Groups[3].ToString())));
                    url = m.Groups[1].ToString() + m.Groups[4];
                }
            }

            UUID reqID = httpScriptMod.
                StartHttpRequest(m_host.UUID, m_itemID, url, param, httpHeaders, body);

            if (reqID != UUID.Zero)
                return reqID.ToString();
            else
                return new LSL_String("");
        }

        public void llSetContentType(LSL_Key id, LSL_Integer type)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;

            string content_type = "text/plain";
            if (type == ScriptBaseClass.CONTENT_TYPE_TEXT)
                content_type = "text/plain";
            else if (type == ScriptBaseClass.CONTENT_TYPE_HTML)
                content_type = "text/html";

            if (m_UrlModule != null)
                m_UrlModule.SetContentType(id, content_type);
        }

        public void llHTTPResponse(LSL_Key id, int status, string body)
        {
            // Partial implementation: support for parameter flags needed
            //   see http://wiki.secondlife.com/wiki/llHTTPResponse

            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;


            if (m_UrlModule != null)
                m_UrlModule.HttpResponse(id, status, body);
        }

        public DateTime llResetLandBanList()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

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
            return PScriptSleep(100);
        }

        public DateTime llResetLandPassList()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return DateTime.Now;

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
            return PScriptSleep(100);
        }

        public LSL_Integer llGetParcelPrimCount(LSL_Vector pos, int category, int sim_wide)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;

            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                LandData land = parcelManagement.GetLandObject((float)pos.x, (float)pos.y).LandData;

                if (land == null)
                {
                    return 0;
                }
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
                        return 0;
                    }
                    switch (category)
                    {
                        case 0:
                            return primCounts.Total;//land.
                        case 1:
                            return primCounts.Owner;
                        case 2:
                            return primCounts.Group;
                        case 3:
                            return primCounts.Others;
                        case 4:
                            return primCounts.Selected;
                        case 5:
                            return primCounts.Temporary;//land.
                    }
                }
            }
            return 0;
        }

        public LSL_List llGetParcelPrimOwners(LSL_Vector pos)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_List();

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
                            ret.Add(new LSL_String(detectedParams.Key.ToString()));
                            ret.Add(new LSL_Integer(detectedParams.Value));
                        }
                    }
                }
            }
            ScriptSleep(2000);
            return ret;
        }

        public LSL_Integer llGetObjectPrimCount(string object_id)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;

            ISceneChildEntity part = World.GetSceneObjectPart(new UUID(object_id));
            if (part == null)
            {
                return 0;
            }
            return part.ParentEntity.PrimCount;
        }

        public LSL_Integer llGetParcelMaxPrims(LSL_Vector pos, int sim_wide)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return 0;

            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                IPrimCountModule primCount = World.RequestModuleInterface<IPrimCountModule>();
                ILandObject land = parcelManagement.GetLandObject((float)pos.x, (float)pos.y);
                return primCount.GetParcelMaxPrimCount(land);
            }
            return 0;
        }

        public LSL_List llGetParcelDetails(LSL_Vector pos, LSL_List param)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_List();

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
                    if ((LSL_Integer)o == ScriptBaseClass.PARCEL_DETAILS_NAME)
                        ret.Add(new LSL_String(land.Name));
                    else if ((LSL_Integer)o == ScriptBaseClass.PARCEL_DETAILS_DESC)
                        ret.Add(new LSL_String(land.Description));
                    else if ((LSL_Integer)o == ScriptBaseClass.PARCEL_DETAILS_OWNER)
                        ret.Add(new LSL_Key(land.OwnerID.ToString()));
                    else if ((LSL_Integer)o == ScriptBaseClass.PARCEL_DETAILS_GROUP)
                        ret.Add(new LSL_Key(land.GroupID.ToString()));
                    else if ((LSL_Integer)o == ScriptBaseClass.PARCEL_DETAILS_AREA)
                        ret.Add(new LSL_Integer(land.Area));
                    else if ((LSL_Integer)o == ScriptBaseClass.PARCEL_DETAILS_ID)
                        //Returning the InfoUUID so that we can use this for landmarks outside of this region
                        // http://wiki.secondlife.com/wiki/PARCEL_DETAILS_ID
                        ret.Add(new LSL_Key(land.InfoUUID.ToString()));
                    else if ((LSL_Integer)o == ScriptBaseClass.PARCEL_DETAILS_PRIVACY)
                        ret.Add(new LSL_Integer(land.Private ? 1 : 0));
                    else
                        ret.Add(new LSL_Integer(0));
                }
            }
            return ret;
        }

        public LSL_String llStringTrim(string src, int type)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";

            if (type == (int)ScriptBaseClass.STRING_TRIM_HEAD) { return src.TrimStart(); }
            if (type == (int)ScriptBaseClass.STRING_TRIM_TAIL) { return src.TrimEnd(); }
            if (type == (int)ScriptBaseClass.STRING_TRIM) { return src.Trim(); }
            return src;
        }

        public LSL_List llGetObjectDetails(string id, LSL_List args)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return new LSL_List();

            LSL_List ret = new LSL_List();
            UUID key = new UUID();
            if (UUID.TryParse(id, out key))
            {
                IScenePresence av = World.GetScenePresence(key);

                if (av != null)
                {
                    foreach (object o in args.Data)
                    {
                        if ((LSL_Integer)o == ScriptBaseClass.OBJECT_NAME)
                        {
                            ret.Add(new LSL_String(av.Name));
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_DESC)
                        {
                            ret.Add(new LSL_String(""));
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_POS)
                        {
                            Vector3 tmp = av.AbsolutePosition;
                            ret.Add(new LSL_Vector(tmp.X, tmp.Y, tmp.Z));
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_ROT)
                        {
                            Quaternion rtmp = av.Rotation;
                            ret.Add(new LSL_Rotation(rtmp.X, rtmp.Y, rtmp.Z, rtmp.W));
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
#if (!ISWIN)
                            int activeScripts = 0;
                            foreach (IScriptModule mod in modules)
                                activeScripts += mod.GetActiveScripts(av);
#else
                            int activeScripts = modules.Sum(mod => mod.GetActiveScripts(av));
#endif
                            ret.Add(activeScripts);
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_TOTAL_SCRIPT_COUNT)
                        {
                            IScriptModule[] modules = World.RequestModuleInterfaces<IScriptModule>();
#if (!ISWIN)
                            int totalScripts = 0;
                            foreach (IScriptModule mod in modules)
                                totalScripts += mod.GetTotalScripts(av);
#else
                            int totalScripts = modules.Sum(mod => mod.GetTotalScripts(av));
#endif
                            ret.Add(totalScripts);
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_SCRIPT_MEMORY)
                        {
                            ret.Add(0);
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_SCRIPT_TIME)
                        {
                            IScriptModule[] modules = World.RequestModuleInterfaces<IScriptModule>();
#if (!ISWIN)
                            int scriptTime = 0;
                            foreach (IScriptModule mod in modules)
                                scriptTime += mod.GetScriptTime(m_itemID);
#else
                            int scriptTime = modules.Sum(mod => mod.GetScriptTime(m_itemID));
#endif
                            ret.Add(scriptTime);
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_PRIM_EQUIVALENCE)
                        {
                            ret.Add(0);
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_SERVER_COST)
                        {
                            ret.Add(0);
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_STREAMING_COST)
                        {
                            ret.Add(0);
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_PHYSICS_COST)
                        {
                            ret.Add(0);
                        }
                        else
                        {
                            ret.Add(ScriptBaseClass.OBJECT_UNKNOWN_DETAIL);
                        }
                    }
                    return ret;
                }
                ISceneChildEntity obj = World.GetSceneObjectPart(key);
                if (obj != null)
                {
                    foreach (object o in args.Data)
                    {
                        if ((LSL_Integer)o == ScriptBaseClass.OBJECT_NAME)
                        {
                            ret.Add(new LSL_String(obj.Name));
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_DESC)
                        {
                            ret.Add(new LSL_String(obj.Description));
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
                            ret.Add(new LSL_Key(obj.OwnerID.ToString()));
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_GROUP)
                        {
                            ret.Add(new LSL_Key(obj.GroupID.ToString()));
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_CREATOR)
                        {
                            ret.Add(new LSL_Key(obj.CreatorID.ToString()));
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_RUNNING_SCRIPT_COUNT)
                        {
                            IScriptModule[] modules = World.RequestModuleInterfaces<IScriptModule>();
#if (!ISWIN)
                            int activeScripts = 0;
                            foreach (IScriptModule mod in modules)
                                activeScripts += mod.GetActiveScripts(obj);
#else
                            int activeScripts = modules.Sum(mod => mod.GetActiveScripts(obj));
#endif
                            ret.Add(new LSL_Integer(activeScripts));
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_TOTAL_SCRIPT_COUNT)
                        {
                            IScriptModule[] modules = World.RequestModuleInterfaces<IScriptModule>();
#if (!ISWIN)
                            int totalScripts = 0;
                            foreach (IScriptModule mod in modules)
                                totalScripts += mod.GetTotalScripts(obj);
#else
                            int totalScripts = modules.Sum(mod => mod.GetTotalScripts(obj));
#endif
                            ret.Add(new LSL_Integer(totalScripts));
                        }
                        else if ((LSL_Integer)o == ScriptBaseClass.OBJECT_SCRIPT_MEMORY)
                        {
                            ret.Add(new LSL_Integer(0));
                        }
                        else
                        {
                            ret.Add(ScriptBaseClass.OBJECT_UNKNOWN_DETAIL);
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
            ShoutError("Command deprecated: " + command);
            //throw new Exception("Command deprecated: " + command);
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
            Vector3 dir = new Vector3((float)(end - start).x, (float)(end - start).y, (float)(end - start).z);
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
            bool checkTerrain = !((rejectTypes & ScriptBaseClass.RC_REJECT_LAND) == ScriptBaseClass.RC_REJECT_LAND);
            bool checkAgents = !((rejectTypes & ScriptBaseClass.RC_REJECT_AGENTS) == ScriptBaseClass.RC_REJECT_AGENTS);
            bool checkNonPhysical = !((rejectTypes & ScriptBaseClass.RC_REJECT_NONPHYSICAL) == ScriptBaseClass.RC_REJECT_NONPHYSICAL);
            bool checkPhysical = !((rejectTypes & ScriptBaseClass.RC_REJECT_PHYSICAL) == ScriptBaseClass.RC_REJECT_PHYSICAL);
            for (float i = 0; i <= distance; i += 0.1f)
            {
                posToCheck = startvector + (dir * (i / (float)distance));
                float groundHeight = channel[(int)(posToCheck.X + startvector.X), (int)(posToCheck.Y + startvector.Y)];
                if (checkTerrain && groundHeight > posToCheck.Z)
                {
                    ContactResult result = new ContactResult { ConsumerID = 0, Depth = 0, Normal = Vector3.Zero, Pos = posToCheck };
                    results.Add(result);
                    checkTerrain = false;
                }
                if (checkAgents)
                {
                    for (int presenceCount = 0; presenceCount < presences.Count; presenceCount++)
                    {
                        IScenePresence sp = presences[presenceCount];
                        if (sp.AbsolutePosition.ApproxEquals(posToCheck, sp.PhysicsActor.Size.X))
                        {
                            ContactResult result = new ContactResult
                                                       {
                                                           ConsumerID = sp.LocalId,
                                                           Depth = 0,
                                                           Normal = Vector3.Zero,
                                                           Pos = posToCheck
                                                       };
                            results.Add(result);
                            presences.RemoveAt(presenceCount);
                            if (presenceCount > 0)
                                presenceCount--; //Reset its position since we removed this one
                        }
                    }
                }
            }
            int refcount = 0;
            List<ContactResult> newResults = new List<ContactResult>();
            foreach (ContactResult result in results)
            {
                foreach (ContactResult r in newResults)
                    if (r.ConsumerID == result.ConsumerID)
                        newResults.Add(result);
            }
            castRaySort(startvector, ref newResults);
            foreach (ContactResult result in newResults)
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
                    refcount++;
                    continue; //Can't find it, so add UUID.Zero
                }

                /*if (detectPhantom == 0 && intersection.obj is ISceneChildEntity &&
                    ((ISceneChildEntity)intersection.obj).PhysActor == null)
                    continue;*/
                //Can't do this ATM, physics engine knows only of non phantom objects

                if (entity is ISceneChildEntity && ((ISceneChildEntity)entity).PhysActor != null && ((ISceneChildEntity)entity).PhysActor.IsPhysical)
                {
                    if (!checkPhysical)
                        continue;
                }
                else if (entity is ISceneChildEntity)
                    if (!checkNonPhysical)
                        continue;

                refcount++;
                if ((dataFlags & ScriptBaseClass.RC_GET_ROOT_KEY) == ScriptBaseClass.RC_GET_ROOT_KEY && entity is ISceneChildEntity)
                    list.Add(((ISceneChildEntity)entity).ParentEntity.UUID);
                else
                    list.Add(entity.UUID);

                if ((dataFlags & ScriptBaseClass.RC_GET_LINK_NUM) == ScriptBaseClass.RC_GET_LINK_NUM)
                    if (entity is ISceneChildEntity)
                        list.Add(entity.LinkNum);
                    else
                        list.Add(0);

                list.Add(result.Pos);
                if ((dataFlags & ScriptBaseClass.RC_GET_NORMAL) == ScriptBaseClass.RC_GET_NORMAL)
                    list.Add(result.Normal);
            }

            list.Add(refcount); //The status code, either the # of contacts, RCERR_SIM_PERF_LOW, or RCERR_CAST_TIME_EXCEEDED

            return list;
        }

        private void castRaySort(Vector3 pos, ref List<ContactResult> list)
        {
#if (!ISWIN)
            list.Sort(delegate(ContactResult a, ContactResult b)
            {
                return Vector3.DistanceSquared(a.Pos, pos).CompareTo(Vector3.DistanceSquared(b.Pos, pos));
            });
#else
            list.Sort ((a, b) => Vector3.DistanceSquared(a.Pos, pos).CompareTo(Vector3.DistanceSquared(b.Pos, pos)));
#endif
        }

        public LSL_Key llGetNumberOfNotecardLines(string name)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";


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

                return UUID.Zero.ToString();
            }

            // was: UUID tid = tid = m_ScriptEngine.
            UUID rq = UUID.Random();
            DataserverPlugin dataserverPlugin = (DataserverPlugin)m_ScriptEngine.GetScriptPlugin("Dataserver");
            UUID tid = dataserverPlugin.RegisterRequest(m_host.UUID, m_itemID, rq.ToString());

            if (NotecardCache.IsCached(assetID))
            {
                dataserverPlugin.AddReply(rq.ToString(),
                    NotecardCache.GetLines(assetID).ToString(), 100);
                ScriptSleep(100);
                return tid.ToString();
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
                        UTF8Encoding enc =
                            new UTF8Encoding();
                        string data = enc.GetString(a.Data);
                        NotecardCache.Cache(id, data);
                        dataserverPlugin.AddReply(rq.ToString(),
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

                return UUID.Zero.ToString();
            }

            DataserverPlugin dataserverPlugin = (DataserverPlugin)m_ScriptEngine.GetScriptPlugin("Dataserver");
            UUID tid = dataserverPlugin.RegisterRequest(m_host.UUID, m_itemID, uuid.ToString());

            Util.FireAndForget(delegate
                                   {
                                       string name = "";
                                       UserAccount info = World.UserAccountService.GetUserAccount(World.RegionInfo.AllScopeIDs, userID);
                                       if (info != null)
                                           name = info.Name;
                                       dataserverPlugin.AddReply(uuid.ToString(),
                                           name, 100);
                                   });

            ScriptSleep(100);
            return tid.ToString();
        }

        public LSL_Key llRequestDisplayName(LSL_Key uuid)
        {
            UUID userID = UUID.Zero;

            if (!UUID.TryParse(uuid, out userID))
            {
                // => complain loudly, as specified by the LSL docs
                ShoutError("Failed to parse uuid for avatar.");

                return UUID.Zero.ToString();
            }

            DataserverPlugin dataserverPlugin = (DataserverPlugin)m_ScriptEngine.GetScriptPlugin("Dataserver");
            UUID tid = dataserverPlugin.RegisterRequest(m_host.UUID, m_itemID, uuid.ToString());

            Util.FireAndForget(delegate
                                   {
                                       string name = "";
                                       IProfileConnector connector = DataManager.DataManager.RequestPlugin<IProfileConnector>();
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
            return tid.ToString();
        }

        public LSL_Key llGetNotecardLine(string name, int line)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return "";


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

                return UUID.Zero.ToString();
            }

            // was: UUID tid = tid = m_ScriptEngine.
            UUID rq = UUID.Random();
            DataserverPlugin dataserverPlugin = (DataserverPlugin)m_ScriptEngine.GetScriptPlugin("Dataserver");
            UUID tid = dataserverPlugin.RegisterRequest(m_host.UUID, m_itemID, rq.ToString());

            if (NotecardCache.IsCached(assetID))
            {
                dataserverPlugin.AddReply(rq.ToString(),
                                                               NotecardCache.GetLine(assetID, line, m_notecardLineReadCharsMax), 100);
                ScriptSleep(100);
                return tid.ToString();
            }

            WithNotecard(assetID, delegate(UUID id, AssetBase a)
                 {
                     if (a == null || a.Type != 7)
                     {
                         ShoutError("Notecard '" + name + "' could not be found.");
                     }
                     else
                     {
                         UTF8Encoding enc =
                             new UTF8Encoding();
                         string data = enc.GetString(a.Data);
                         NotecardCache.Cache(id, data);
                         dataserverPlugin.AddReply(rq.ToString(),
                            NotecardCache.GetLine(id, line, m_notecardLineReadCharsMax), 100);
                     }
                 });

            ScriptSleep(100);
            return tid.ToString();
        }

        public void SetPrimitiveParamsEx(LSL_Key prim, LSL_List rules)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osSetPrimitiveParams", m_host, "OSSL", m_itemID)) return;
            ISceneChildEntity obj = World.GetSceneObjectPart(prim);
            if (obj == null)
                return;

            if (obj.OwnerID != m_host.OwnerID)
                return;

            SetPrimParams(obj, rules);
        }

        public LSL_List GetLinkPrimitiveParamsEx(LSL_Key prim, LSL_List rules)
        {
            ISceneChildEntity obj = World.GetSceneObjectPart(prim);
            if (obj == null)
                return new LSL_List();

            if (obj.OwnerID != m_host.OwnerID)
                return new LSL_List();

            return GetLinkPrimitiveParams(obj, rules);
        }

        public void print(string str)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Severe, "print", m_host, "LSL", m_itemID)) return;

            if (m_ScriptEngine.Config.GetBoolean("AllowosConsoleCommand", false))
            {
                if (World.Permissions.CanRunConsoleCommand(m_host.OwnerID))
                {
                    // yes, this is a real LSL function. See: http://wiki.secondlife.com/wiki/Print
                    MainConsole.Instance.Output("LSL print():" + str);
                }
            }
        }

        public LSL_Integer llManageEstateAccess(LSL_Integer action, LSL_String avatar)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return LSL_Integer.FALSE;
            if (World.Permissions.IsAdministrator(m_host.OwnerID))
            {
                if (action == ScriptBaseClass.ESTATE_ACCESS_ALLOWED_AGENT_ADD)
                    World.RegionInfo.EstateSettings.AddEstateUser(UUID.Parse(avatar));
                else if (action == ScriptBaseClass.ESTATE_ACCESS_ALLOWED_AGENT_REMOVE)
                    World.RegionInfo.EstateSettings.RemoveEstateUser(UUID.Parse(avatar));
                else if (action == ScriptBaseClass.ESTATE_ACCESS_ALLOWED_GROUP_ADD)
                    World.RegionInfo.EstateSettings.AddEstateGroup(UUID.Parse(avatar));
                else if (action == ScriptBaseClass.ESTATE_ACCESS_ALLOWED_GROUP_REMOVE)
                    World.RegionInfo.EstateSettings.RemoveEstateGroup(UUID.Parse(avatar));
                else if (action == ScriptBaseClass.ESTATE_ACCESS_BANNED_AGENT_ADD)
                    World.RegionInfo.EstateSettings.AddBan(new EstateBan
                                                               {
                                                                   EstateID = World.RegionInfo.EstateSettings.EstateID,
                                                                   BannedUserID = UUID.Parse(avatar)
                                                               });
                else if (action == ScriptBaseClass.ESTATE_ACCESS_BANNED_AGENT_REMOVE)
                    World.RegionInfo.EstateSettings.RemoveBan(UUID.Parse(avatar));
                return LSL_Integer.TRUE;
            }
            return LSL_Integer.FALSE;
        }

        public void llSetKeyframedMotion(LSL_List keyframes, LSL_List options)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "LSL", m_host, "LSL", m_itemID)) return;
            if (!m_host.IsRoot)
            {
                ShoutError("Must be used in the root object!");
                return;
            }
            KeyframeAnimation.Data dataType = KeyframeAnimation.Data.Both;
            KeyframeAnimation.Modes currentMode = KeyframeAnimation.Modes.Forward;
            for (int i = 0; i < options.Length; i += 2)
            {
                LSL_Integer option = options.GetLSLIntegerItem(i);
                LSL_Integer value = options.GetLSLIntegerItem(i + 1);
                if (option == ScriptBaseClass.KFM_COMMAND)
                {
                    m_host.ParentEntity.AddKeyframedMotion(null, (KeyframeAnimation.Commands)value.value);
                    break;//Its supposed to be the only option in the list
                }
                if (option == ScriptBaseClass.KFM_MODE)
                {
                    currentMode = (KeyframeAnimation.Modes)value.value;
                }
                else if (option == ScriptBaseClass.KFM_DATA)
                {
                    dataType = (KeyframeAnimation.Data)value.value;
                }
            }
            List<Vector3> positions = new List<Vector3>();
            List<Quaternion> rotations = new List<Quaternion>();
            List<int> times = new List<int>();
            for (int i = 0; i < keyframes.Length; i += (dataType == KeyframeAnimation.Data.Both ? 3 : 2))
            {
                if (dataType == KeyframeAnimation.Data.Both ||
                    dataType == KeyframeAnimation.Data.Translation)
                {
                    LSL_Vector pos = keyframes.GetVector3Item(i);
                    positions.Add(pos.ToVector3());
                }
                if (dataType == KeyframeAnimation.Data.Both ||
                    dataType == KeyframeAnimation.Data.Rotation)
                {
                    LSL_Rotation rot = keyframes.GetQuaternionItem(i + (dataType == KeyframeAnimation.Data.Both ? 1 : 0));
                    Quaternion quat = rot.ToQuaternion();
                    quat.Normalize();
                    rotations.Add(quat);
                }
                int time = keyframes.GetLSLIntegerItem(i + (dataType == KeyframeAnimation.Data.Both ? 2 : 1));
                times.Add(time);
            }
            KeyframeAnimation animation = new KeyframeAnimation
                                              {
                                                  CurrentMode = currentMode,
                                                  PositionList = positions.ToArray(),
                                                  RotationList = rotations.ToArray(),
                                                  TimeList = times.ToArray(),
                                                  CurrentAnimationPosition = 0,
                                                  InitialPosition = m_host.AbsolutePosition,
                                                  InitialRotation = m_host.RotationOffset
                                              };
            m_host.ParentEntity.AddKeyframedMotion(animation, KeyframeAnimation.Commands.Play);
        }

        public LSL_String llGetParcelMusicURL()
        {
            ILandObject parcel = m_host.ParentEntity.Scene.RequestModuleInterface<IParcelManagementModule>().GetLandObject(m_host.ParentEntity.LastParcelUUID);
            return new LSL_String(parcel.LandData.MusicURL);
        }

        public LSL_String llTransferLindenDollars(LSL_String destination, LSL_Integer amt)
        {
            LSL_String transferID = UUID.Random().ToString();
            IMoneyModule moneyMod = World.RequestModuleInterface<IMoneyModule>();
            LSL_String data = "";
            LSL_Integer success = LSL_Integer.FALSE;
            TaskInventoryItem item = m_host.TaskInventory[m_itemID];
            UUID destID;
            if (item.PermsGranter == UUID.Zero || (item.PermsMask & ScriptBaseClass.PERMISSION_DEBIT) == 0)
                data = llList2CSV(new LSL_Types.list("MISSING_PERMISSION_DEBIT"));
            else if (!UUID.TryParse(destination, out destID))
                data = llList2CSV(new LSL_Types.list("INVALID_AGENT"));
            else if (amt <= 0)
                data = llList2CSV(new LSL_Types.list("INVALID_AMOUNT"));
            else if (World.UserAccountService.GetUserAccount(World.RegionInfo.AllScopeIDs, destID) == null)
                data = llList2CSV(new LSL_Types.list("LINDENDOLLAR_ENTITYDOESNOTEXIST"));
            else if (m_host.ParentEntity.OwnerID == m_host.ParentEntity.GroupID)
                data = llList2CSV(new LSL_Types.list("GROUP_OWNED"));
            else if (moneyMod != null)
            {
                success = moneyMod.Transfer(UUID.Parse(destination), m_host.OwnerID, amt, "");
                data = llList2CSV(success ? new LSL_List(destination, amt) : new LSL_Types.list("LINDENDOLLAR_INSUFFICIENTFUNDS"));
            }
            else
                data = llList2CSV(new LSL_Types.list("SERVICE_ERROR"));

            m_ScriptEngine.PostScriptEvent(m_itemID, m_host.UUID, new EventParams(
                    "transaction_result", new Object[] {
                    transferID, success, data},
                    new DetectParams[0]), EventPriority.FirstStart);

            return transferID;
        }


        public void llCreateCharacter(LSL_List options)
        {
            IBotManager botManager = World.RequestModuleInterface<IBotManager>();
            if (botManager != null)
            {
                botManager.CreateCharacter(m_host.ParentEntity.UUID, World);
                llUpdateCharacter(options);
            }
        }

        public void llUpdateCharacter(LSL_List options)
        {
            IBotManager botManager = World.RequestModuleInterface<IBotManager>();
            if (botManager != null)
            {
                IBotController controller = botManager.GetCharacterManager(m_host.ParentEntity.UUID);
                for (int i = 0; i < options.Length; i += 2)
                {
                    LSL_Types.LSLInteger opt = options.GetLSLIntegerItem(i);
                    LSL_Types.LSLFloat value = options.GetLSLFloatItem(i + 1);
                    if (opt == ScriptBaseClass.CHARACTER_DESIRED_SPEED)
                        controller.SetSpeedModifier((float)value.value);
                    else if (opt == ScriptBaseClass.CHARACTER_RADIUS)
                    {
                    }
                    else if (opt == ScriptBaseClass.CHARACTER_LENGTH)
                    {
                    }
                    else if (opt == ScriptBaseClass.CHARACTER_ORIENTATION)
                    {
                    }
                    else if (opt == ScriptBaseClass.TRAVERSAL_TYPE)
                    {
                    }
                    else if (opt == ScriptBaseClass.CHARACTER_TYPE)
                    {
                    }
                    else if (opt == ScriptBaseClass.CHARACTER_AVOIDANCE_MODE)
                    {
                    }
                    else if (opt == ScriptBaseClass.CHARACTER_MAX_ACCEL)
                    {
                    }
                    else if (opt == ScriptBaseClass.CHARACTER_MAX_DECEL)
                    {
                    }
                    else if (opt == ScriptBaseClass.CHARACTER_MAX_ANGULAR_SPEED)
                    {
                    }
                    else if (opt == ScriptBaseClass.CHARACTER_MAX_ANGULAR_ACCEL)
                    {
                    }
                    else if (opt == ScriptBaseClass.CHARACTER_TURN_SPEED_MULTIPLIER)
                    {
                    }
                }
            }
        }

        public void llDeleteCharacter()
        {
            IBotManager botManager = World.RequestModuleInterface<IBotManager>();
            if (botManager != null)
                botManager.RemoveCharacter(m_host.ParentEntity.UUID);
        }

        public void llPursue(LSL_String target, LSL_List options)
        {
            IBotManager botManager = World.RequestModuleInterface<IBotManager>();
            if (botManager != null)
            {
                float fuzz = 2;
                Vector3 offset = Vector3.Zero;
                bool requireLOS = false;
                bool intercept = false;//Not implemented
                for (int i = 0; i < options.Length; i += 2)
                {
                    LSL_Types.LSLInteger opt = options.GetLSLIntegerItem(i);
                    if (opt == ScriptBaseClass.PURSUIT_FUZZ_FACTOR)
                        fuzz = (float)options.GetLSLFloatItem(i + 1).value;
                    if (opt == ScriptBaseClass.PURSUIT_OFFSET)
                        offset = options.GetVector3Item(i + 1).ToVector3();
                    if (opt == ScriptBaseClass.REQUIRE_LINE_OF_SIGHT)
                        requireLOS = options.GetLSLIntegerItem(i + 1) == 1;
                    if (opt == ScriptBaseClass.PURSUIT_INTERCEPT)
                        intercept = options.GetLSLIntegerItem(i + 1) == 1;
                }
                botManager.FollowAvatar(m_host.ParentEntity.UUID, target.m_string, fuzz, fuzz, requireLOS, offset, m_host.ParentEntity.OwnerID);
            }
        }

        public void llEvade(LSL_String target, LSL_List options)
        {
            NotImplemented("llEvade");
        }

        public void llFleeFrom(LSL_Vector source, LSL_Float distance, LSL_List options)
        {
            NotImplemented("llFleeFrom");
        }

        public void llPatrolPoints(LSL_List patrolPoints, LSL_List options)
        {
            List<Vector3> positions = new List<Vector3>();
            List<TravelMode> travelMode = new List<TravelMode>();
            foreach (object pos in patrolPoints.Data)
            {
                if (!(pos is LSL_Vector))
                    continue;
                LSL_Vector p = (LSL_Vector)pos;
                positions.Add(p.ToVector3());
                travelMode.Add(TravelMode.Walk);
            }
            IBotManager botManager = World.RequestModuleInterface<IBotManager>();
            if (botManager != null)
                botManager.SetBotMap(m_host.ParentEntity.UUID, positions, travelMode, 1, m_host.ParentEntity.OwnerID);
        }

        public void llNavigateTo(LSL_Vector point, LSL_List options)
        {
            List<Vector3> positions = new List<Vector3>() { point.ToVector3() };
            List<TravelMode> travelMode = new List<TravelMode>() { TravelMode.Walk };
            IBotManager botManager = World.RequestModuleInterface<IBotManager>();
            int flags = 0;
            if (options.Length > 0)
                flags |= options.GetLSLIntegerItem(0);
            if (botManager != null)
                botManager.SetBotMap(m_host.ParentEntity.UUID, positions, travelMode, flags, m_host.ParentEntity.OwnerID);
        }

        public void llWanderWithin(LSL_Vector origin, LSL_Float distance, LSL_List options)
        {
            NotImplemented("llWanderWithin");
        }

        public LSL_List llGetClosestNavPoint(LSL_Vector point, LSL_List options)
        {
            Vector3 diff = new Vector3(0, 0, 0.1f) * (Vector3.RotationBetween(m_host.ParentEntity.AbsolutePosition, point.ToVector3()));
            return new LSL_List(new LSL_Vector((m_host.ParentEntity.AbsolutePosition + diff)));
        }

        public void llExecCharacterCmd(LSL_Integer command, LSL_List options)
        {
            IBotManager botManager = World.RequestModuleInterface<IBotManager>();
            if (botManager != null)
            {
                IBotController controller = botManager.GetCharacterManager(m_host.ParentEntity.UUID);
                if (command == ScriptBaseClass.CHARACTER_CMD_JUMP)
                    controller.Jump();
                if (command == ScriptBaseClass.CHARACTER_CMD_STOP)
                    controller.StopMoving(false, true);
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

                Notecard nc = new Notecard { lastRef = DateTime.Now, text = SLUtil.ParseNotecardToList(text).ToArray() };
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

            if (!IsCached(assetID))
                return "";

            lock (m_Notecards)
            {
                m_Notecards[assetID].lastRef = DateTime.Now;

                if (line >= m_Notecards[assetID].text.Length)
                    return "\n\n\n";

                string data = m_Notecards[assetID].text[line];
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
