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
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using ProtoBuf;

namespace Aurora.Framework.ClientInterfaces
{
    /// <summary>
    ///     Information about an Animation
    /// </summary>
    [Serializable, ProtoContract(UseProtoMembersOnly = true)]
    public class Animation
    {
        /// <summary>
        ///     ID of Animation
        /// </summary>
        [ProtoMember(1)]
        public UUID AnimID { get; set; }

        [ProtoMember(2)]
        public int SequenceNum { get; set; }

        /// <summary>
        ///     Unique ID of object that is being animated
        /// </summary>
        [ProtoMember(3)]
        public UUID ObjectID { get; set; }


        public Animation()
        {
        }

        /// <summary>
        ///     Creates an Animation based on the data
        /// </summary>
        /// <param name="animID">UUID ID of animation</param>
        /// <param name="sequenceNum"></param>
        /// <param name="objectID">ID of object to be animated</param>
        public Animation(UUID animID, int sequenceNum, UUID objectID)
        {
            this.AnimID = animID;
            this.SequenceNum = sequenceNum;
            this.ObjectID = objectID;
        }

        /// <summary>
        ///     Animation from OSDMap from LLSD XML or LLSD json
        /// </summary>
        /// <param name="args"></param>
        public Animation(OSDMap args)
        {
            FromOSD(args);
        }

        /// <summary>
        ///     Pack this object up as an OSDMap for transferring via LLSD XML or LLSD json
        /// </summary>
        /// <returns></returns>
        public OSDMap ToOSD()
        {
            OSDMap anim = new OSDMap();
            anim["animation"] = OSD.FromUUID(AnimID);
            anim["object_id"] = OSD.FromUUID(ObjectID);
            anim["seq_num"] = OSD.FromInteger(SequenceNum);
            return anim;
        }

        /// <summary>
        ///     Fill object with data from OSDMap
        /// </summary>
        /// <param name="args"></param>
        public void FromOSD(OSDMap args)
        {
            if (args["animation"] != null)
                AnimID = args["animation"].AsUUID();
            if (args["object_id"] != null)
                ObjectID = args["object_id"].AsUUID();
            if (args["seq_num"] != null)
                SequenceNum = args["seq_num"].AsInteger();
        }
    }
}