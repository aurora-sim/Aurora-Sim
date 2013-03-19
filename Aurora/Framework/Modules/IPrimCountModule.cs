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

using System.Collections.Generic;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.SceneInfo.Entities;
using OpenMetaverse;

namespace Aurora.Framework
{
    public interface IPrimCountModule
    {
        void TaintPrimCount(ILandObject land);
        void TaintPrimCount(int x, int y);
        void TaintPrimCount();

        IPrimCounts GetPrimCounts(UUID parcelID);

        int GetParcelMaxPrimCount(ILandObject thisObject);
    }

    public interface IPrimCounts
    {
        int Owner { get; }
        int Group { get; }
        int Others { get; }
        int Simulator { get; }
        int Selected { get; }
        int Temporary { get; }
        int Total { get; }
        List<ISceneEntity> Objects { get; }
        IUserPrimCounts Users { get; }

        Dictionary<UUID, int> GetAllUserCounts();
    }

    public interface IUserPrimCounts
    {
        int this[UUID agentID] { get; }
    }

    public interface IEntityCountModule
    {
        int RootAgents { get; }
        int ChildAgents { get; }

        int Objects { get; }
        int ActiveObjects { get; }
    }
}