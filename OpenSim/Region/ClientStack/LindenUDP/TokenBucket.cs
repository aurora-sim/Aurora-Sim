/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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
//#define Debug
using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using OpenSim.Framework;

namespace OpenSim.Region.ClientStack.LindenUDP
{
    /// <summary>
    /// A hierarchical token bucket for bandwidth throttling. See
    /// http://en.wikipedia.org/wiki/Token_bucket for more information
    /// </summary>
    public class TokenBucket
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static Int32 m_counter = 0;
        protected const float m_amountRequiredMultiplier = 1.5f;

        private Int32 m_identifier;

        /// <summary>
        /// Number of ticks (ms) per quantum, drip rate and max burst
        /// are defined over this interval.
        /// </summary>
        protected const Int32 m_ticksPerQuantum = 1000;

        /// <summary>
        /// This is the number of quantums worth of packets that can
        /// be accommodated during a burst
        /// </summary>
        protected const Double m_quantumsPerBurst = 1.5;

        /// <summary>
        /// </summary>
        protected const Int32 m_minimumDripRate = 1400;

        /// <summary>Time of the last drip, in system ticks</summary>
        protected Int32 m_lastDrip;

        /// <summary>
        /// Map of children buckets and their requested maximum burst rate
        /// </summary>
        protected Dictionary<TokenBucket, Int64> m_children = new Dictionary<TokenBucket, Int64>();

        #region Properties

        /// <summary>
        /// The number of bytes that can be sent at this moment. This is the
        /// current number of tokens in the bucket
        /// </summary>
        protected Int64 m_tokenCount;
        public Int64 TokenCount
        {
            get { return m_tokenCount; }
            set { m_tokenCount = value; }
        }

        /// <summary>
        /// The number of bytes that must be in this token bucket
        /// before the tokens are able to be taken from it by a parent
        /// </summary>
        protected Int64 m_minTokenAmount;
        public Int64 MinTokenAmount
        {
            get { return m_minTokenAmount; }
        }

        /// <summary>
        /// The parent bucket of this bucket, or null if this bucket has no
        /// parent. The parent bucket will limit the aggregate bandwidth of all
        /// of its children buckets
        /// </summary>
        protected TokenBucket m_parent;
        public TokenBucket Parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        /// <summary>
        /// Maximum burst rate in bytes per second. This is the maximum number
        /// of tokens that can accumulate in the bucket at any one time. This
        /// also sets the total request for leaf nodes
        /// </summary>
        protected Int64 m_burstRate;
        public Int64 RequestedBurstRate
        {
            get { return m_burstRate; }
            set { m_burstRate = (value < 0 ? 0 : value); }
        }

        public Int64 BurstRate
        {
            get
            {
                double rate = RequestedBurstRate * BurstRateModifier();
                if (rate < m_minimumDripRate * m_quantumsPerBurst)
                    rate = m_minimumDripRate * m_quantumsPerBurst;

                return (Int64)rate;
            }
        }

        /// <summary>
        /// The speed limit of this bucket in bytes per second. This is the
        /// number of tokens that are added to the bucket per quantum
        /// </summary>
        /// <remarks>Tokens are added to the bucket any time
        /// <seealso cref="RemoveTokens"/> is called, at the granularity of
        /// the system tick interval (typically around 15-22ms)</remarks>
        protected Int64 m_dripRate;
        public virtual Int64 RequestedDripRate
        {
            get { return (m_dripRate == 0 ? m_totalDripRequest : m_dripRate); }
            set
            {
                m_dripRate = (value < 0 ? 0 : value);
                m_burstRate = (Int64)((double)m_dripRate * m_quantumsPerBurst);
                m_totalDripRequest = m_dripRate;
                //m_log.Warn("New drip rate: " + m_totalDripRequest);
                if (m_parent != null)
                    m_parent.RegisterRequest(this, m_dripRate);
            }
        }

        public virtual Int64 DripRate
        {
            get
            {
                if (m_parent == null)
                    return Math.Min(RequestedDripRate, RequestedDripRate);

                double rate = (double)RequestedDripRate * m_parent.DripRateModifier();
                if (rate < m_minimumDripRate)
                    rate = m_minimumDripRate;

                return Math.Min((Int64)rate, RequestedDripRate);
            }
        }

        /// <summary>
        /// The current total of the requested maximum burst rates of
        /// this bucket's children buckets.
        /// </summary>
        protected Int64 m_totalDripRequest;
        public Int64 TotalDripRequest
        {
            get { return m_totalDripRequest; }
            set { m_totalDripRequest = value; }
        }

        #endregion Properties

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="parent">Parent bucket if this is a child bucket, or
        /// null if this is a root bucket</param>
        /// <param name="maxBurst">Maximum size of the bucket in bytes, or
        /// zero if this bucket has no maximum capacity</param>
        /// <param name="dripRate">Rate that the bucket fills, in bytes per
        /// second. If zero, the bucket always remains full</param>
        public TokenBucket(TokenBucket parent, Int64 dripRate, Int64 minTokenAmount)
        {
            m_identifier = m_counter++;

            Parent = parent;
            RequestedDripRate = dripRate;
            m_minTokenAmount = minTokenAmount;
            // TotalDripRequest = dripRate; // this will be overwritten when a child node registers
            // MaxBurst = (Int64)((double)dripRate * m_quantumsPerBurst);
            m_lastDrip = Util.EnvironmentTickCount();
        }

        /// <summary>
        /// Compute a modifier for the MaxBurst rate. This is 1.0, meaning
        /// no modification if the requested bandwidth is less than the
        /// max burst bandwidth all the way to the root of the throttle
        /// hierarchy. However, if any of the parents is over-booked, then
        /// the modifier will be less than 1.
        /// </summary>
        protected double DripRateModifier()
        {
            Int64 driprate = DripRate;
            return driprate >= TotalDripRequest ? 1.0 : (double)driprate / (double)TotalDripRequest;
        }

        /// <summary>
        /// </summary>
        protected double BurstRateModifier()
        {
            // for now... burst rate is always m_quantumsPerBurst (constant)
            // larger than drip rate so the ratio of burst requests is the
            // same as the drip ratio
            return DripRateModifier();
        }

        /// <summary>
        /// Register drip rate requested by a child of this throttle. Pass the
        /// changes up the hierarchy.
        /// </summary>
        public void RegisterRequest(TokenBucket child, Int64 request)
        {
            lock (m_children)
            {
                m_children[child] = request;
                // m_totalDripRequest = m_children.Values.Sum();

                m_totalDripRequest = 0;
                foreach (KeyValuePair<TokenBucket, Int64> cref in m_children)
                    m_totalDripRequest += cref.Value;
            }

            // Pass the new values up to the parent
            if (m_parent != null)
                m_parent.RegisterRequest(this, Math.Min(RequestedDripRate, RequestedDripRate));
        }

        /// <summary>
        /// Remove the rate requested by a child of this throttle. Pass the
        /// changes up the hierarchy.
        /// </summary>
        public void UnregisterRequest(TokenBucket child)
        {
            lock (m_children)
            {
                m_children.Remove(child);
                // m_totalDripRequest = m_children.Values.Sum();

                m_totalDripRequest = 0;
                foreach (KeyValuePair<TokenBucket, Int64> cref in m_children)
                    m_totalDripRequest += cref.Value;
            }

            // Pass the new values up to the parent
            if (m_parent != null)
                m_parent.RegisterRequest(this, Math.Min(RequestedDripRate, RequestedDripRate));
        }

        /// <summary>
        /// Remove a given number of tokens from the bucket
        /// </summary>
        /// <param name="amount">Number of tokens to remove from the bucket</param>
        /// <returns>True if the requested number of tokens were removed from
        /// the bucket, otherwise false</returns>
        public bool RemoveTokens(Int64 amount)
        {
            // Deposit tokens for this interval
            Deposit(Drip());

            // If we have enough tokens then remove them and return
            if (m_tokenCount - amount >= 0)
            {
                // we don't have to remove from the parent, the drip rate is already
                // reflective of the drip rate limits in the parent
                m_tokenCount -= amount;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Deposit tokens into the bucket from a child bucket that did
        /// not use all of its available tokens
        /// </summary>
        public void Deposit(Int64 count)
        {
            m_tokenCount += count;
#if Debug
            //m_log.Warn("depositing: " + count + ", total: " + m_tokenCount);
#endif

            // Deposit the overflow in the parent bucket, this is how we share
            // unused bandwidth
            Int64 burstrate = BurstRate;
            if (m_tokenCount > burstrate)
            {
                if(m_parent != null) //Give the parent the extra
                    m_parent.m_tokenCount += m_tokenCount - burstrate;
                m_tokenCount = burstrate;
            }
        }

        public Int64 DripUs()
        {
            Int64 tokensToAdd = 0;

            // This should never happen... means we are a leaf node and were created
            // with no drip rate...
            if (DripRate == 0)
            {
                //m_log.WarnFormat("[TOKENBUCKET] something odd is happening and drip rate is 0");
                return tokensToAdd;
            }

            // Determine the interval over which we are adding tokens, never add
            // more than a single quantum of tokens
            Int32 deltaMS = Math.Min(Util.EnvironmentTickCountSubtract(m_lastDrip), m_ticksPerQuantum);
            m_lastDrip = Util.EnvironmentTickCount();

            // This can be 0 in the very unusual case that the timer wrapped
            // It can be 0 if we try add tokens at a sub-tick rate
            if (deltaMS <= 0)
                return tokensToAdd;

            tokensToAdd += (deltaMS * DripRate / m_ticksPerQuantum);
            return tokensToAdd;
        }

        /// <summary>
        /// Add tokens to the bucket over time. The number of tokens added each
        /// call depends on the length of time that has passed since the last
        /// call to Drip
        /// </summary>
        /// <returns>The amount of tokens to add to the bucket</returns>
        public Int64 Drip()
        {
            Int64 tokensToAdd = DripUs();

            lock (m_children)
            {
                //We also can take from children if we need to, if they arn't using their drip
                foreach (TokenBucket child in m_children.Keys)
                {
                    Int32 deltaMS = Math.Min(Util.EnvironmentTickCountSubtract(child.m_lastDrip), m_ticksPerQuantum);
                    if (child.TokenCount > child.MinTokenAmount && (float)child.TokenCount / (float)m_tokenCount > m_amountRequiredMultiplier) //Make sure that we have less tokens and we havn't stolen from them in the last 5ms
                    {
                        Int64 oldTokens = tokensToAdd;
                        Int64 childDrip = child.Drip();
                        //See if we can steal actual tokens, not just the drip
                        childDrip += child.StealTokens(childDrip);
                        //Add in the extra tokens
                        tokensToAdd += childDrip;
#if Debug
                        m_log.WarnFormat("Taking {0} from children, {1}, ctokens: {2}, otokens: {3}", tokensToAdd - oldTokens, deltaMS, child.m_tokenCount, m_tokenCount);
#endif
                    }
                    else
                        child.Deposit(child.Drip()); //Drip for them then, since we might need it later
                }
            }
            if (m_parent != null && m_tokenCount < 1000)
                tokensToAdd += m_parent.AskParentForTokens(m_tokenCount);
            return tokensToAdd;
        }

        /// <summary>
        /// Take tokens from children and give them to the parent
        /// </summary>
        /// <param name="prevDrip"></param>
        /// <returns></returns>
        protected Int64 StealTokens(Int64 prevDrip)
        {
            Int64 diff = m_tokenCount / 3;
            //Make sure that the parent needs the tokens first, then make sure that we don't give away all of ours
            if (m_tokenCount > MinTokenAmount && (float)m_tokenCount / (float)m_parent.m_tokenCount > m_amountRequiredMultiplier)
            {
                //Steal actual tokens from the children
                m_tokenCount -= diff;
                return diff;
            }
            return 0;
        }

        protected virtual Int64 AskParentForTokens(Int64 tokenAmount)
        {
            Int32 deltaMS = Math.Min(Util.EnvironmentTickCountSubtract(m_lastDrip), m_ticksPerQuantum);
            //if (deltaMS > 100)
            //    Deposit(Drip()); //Drip ourselves if we havn't recently

            if (m_tokenCount > MinTokenAmount && (float)m_tokenCount / (float)tokenAmount > m_amountRequiredMultiplier) //Make sure that we have less tokens and we havn't stolen from them in the last 5ms
            {
                Int64 diff = Math.Min(MinTokenAmount, tokenAmount * 10);
#if Debug
                m_log.WarnFormat("Giving {0} from parent, {1}, ctokens: {2}, otokens: {3}", diff, deltaMS, tokenAmount, m_tokenCount);
#endif
                m_tokenCount -= diff;
                return diff;
            }
            return 0;
        }
    }

    public class AdaptiveTokenBucket : TokenBucket
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The minimum rate for flow control.
        /// </summary>
        protected const Int64 m_minimumFlow = m_minimumDripRate * 15;
        protected const Int64 m_resendPacketsDrip = 5000;
        protected const Int64 m_rateToAllowByteLoss = 500;
        protected const Int64 m_rateToAllowPacketLoss = 5;
        protected Int64 m_bytesResentTokenCount = 0;
        protected Int64 m_packetsResentTokenCount = 0;

        /// <summary>Time of the last resend drip, in system ticks</summary>
        protected Int32 m_lastBytesResentDrip;

        /// <summary>Time of the last resend drip, in system ticks</summary>
        protected Int32 m_lastPacketsResentDrip;

        protected Int64 BytesResentTokenCount
        {
            get { return m_bytesResentTokenCount; }
            set
            {
                m_bytesResentTokenCount += value;
                ResentBytesDrip();
            }
        }

        protected Int64 PacketsResentTokenCount
        {
            get { return m_packetsResentTokenCount; }
            set
            {
                m_packetsResentTokenCount += value;
                ResentPacketsDrip();
            }
        }

        protected Int64 ResendDripCount
        {
            get { return AdjustedDripRate / 100; }
        }

        private void ResentBytesDrip()
        {
            Int32 deltaMS = Util.EnvironmentTickCountSubtract(m_lastBytesResentDrip);
            m_lastBytesResentDrip = Util.EnvironmentTickCount();
            if (deltaMS <= 0)
                return;

            m_bytesResentTokenCount -= (deltaMS * ResendDripCount / m_ticksPerQuantum);
            if (m_bytesResentTokenCount < 0)
                m_bytesResentTokenCount = 0;
        }

        private void ResentPacketsDrip()
        {
            Int32 deltaMS = Util.EnvironmentTickCountSubtract(m_lastPacketsResentDrip);
            m_lastPacketsResentDrip = Util.EnvironmentTickCount();
            if (deltaMS <= 0)
                return;

            m_packetsResentTokenCount -= (deltaMS / m_resendPacketsDrip);
            if (m_packetsResentTokenCount < 0)
                m_packetsResentTokenCount = 0;
        }

        /// <summary>
        /// The maximum rate for flow control. Drip rate can never be
        /// greater than this.
        /// </summary>
        protected Int64 m_maxDripRate = 0;
        protected Int64 MaxDripRate
        {
            get { return (m_maxDripRate == 0 ? m_totalDripRequest : m_maxDripRate); }
            set { m_maxDripRate = (value == 0 ? 0 : Math.Max(value, m_minimumFlow)); }
        }

        // <summary>
        //
        // </summary>
        public virtual Int64 AdjustedDripRate
        {
            get { return m_dripRate; }
            set
            {
                m_dripRate = OpenSim.Framework.Util.Clamp<Int64>(value, m_minimumFlow, MaxDripRate);
                m_burstRate = (Int64)((double)m_dripRate * m_quantumsPerBurst);
                if (m_parent != null)
                    m_parent.RegisterRequest(this, m_dripRate);
            }
        }

        // <summary>
        //
        // </summary>
        public AdaptiveTokenBucket(TokenBucket parent, Int64 maxDripRate, Int64 minTokenAmount)
            : base(parent, m_minimumFlow, minTokenAmount)
        {
            MaxDripRate = maxDripRate;
        }

        /// <summary>
        /// Expires the given packets and reduces the amount of drip that will be used to send packets later
        /// </summary>
        /// <param name="count"># of packets that have expired</param>
        /// <param name="bytes"># of bytes in the packets that have expired</param>
        public void ExpirePackets(Int32 count, Int32 bytes)
        {
            BytesResentTokenCount += bytes;
            PacketsResentTokenCount += count;
            if (BytesResentTokenCount > m_rateToAllowByteLoss ||
                PacketsResentTokenCount > m_rateToAllowPacketLoss) //Check to see whether its greater than the min packet loss we allow for
            {
                Int64 old = AdjustedDripRate;
                AdjustedDripRate = (Int64)(AdjustedDripRate - (BytesResentTokenCount * 3));
                BytesResentTokenCount /= 3;
                //AdjustedDripRate = AdjustedDripRate - count;
                m_log.WarnFormat("[ADAPTIVEBUCKET] dropping {0} by {1} to {2} expired packets", old, count, AdjustedDripRate);
            }
        }

        /// <summary>
        /// Add successfully sent packets into the drip rate
        /// </summary>
        /// <param name="count"># of bytes in the successfully sent packet</param>
        public void AcknowledgePackets(Int32 count)
        {
            AdjustedDripRate = AdjustedDripRate + count;
        }

        protected override Int64 AskParentForTokens(Int64 tokenAmount)
        {
            Int32 deltaMS = Math.Min(Util.EnvironmentTickCountSubtract(m_lastDrip), m_ticksPerQuantum);
            //if (deltaMS > 100)
            //    Deposit(Drip()); //Drip ourselves if we havn't recently
            Int64 amt = 0;
            lock (m_children)
            {
                foreach (TokenBucket child in m_children.Keys)
                {
                    if (child.TokenCount == tokenAmount) //The requestor
                        continue;
                    child.Deposit(child.DripUs());
                    if (child.TokenCount > child.MinTokenAmount && (float)child.TokenCount / (float)tokenAmount > m_amountRequiredMultiplier) //Make sure that we have less tokens and we havn't stolen from them in the last 5ms
                    {
                        Int64 diff = Math.Min(child.MinTokenAmount / m_children.Count, tokenAmount * 10);
#if Debug
                        m_log.WarnFormat("Giving {0} from child, {1}, child tokens: {2}, giver tokens: {3}", diff, deltaMS, tokenAmount, child.TokenCount);
#endif
                        child.TokenCount -= diff;
                        amt += diff;
                    }
                }
            }
            return amt;
        }
    }
}