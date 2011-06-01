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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse.StructuredData;
using OpenMetaverse;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface IOpenRegionSettingsModule
    {
        float MaxDragDistance { get; set; }
        float DefaultDrawDistance { get; set; }
        float MaximumPrimScale { get; set; }
        float MinimumPrimScale { get; set; }
        float MaximumPhysPrimScale { get; set; }
        float MaximumHollowSize { get; set; }
        float MinimumHoleSize { get; set; }
        int MaximumLinkCount { get; set; }
        int MaximumLinkCountPhys { get; set; }
        OSDArray LSLCommands { get; set; }
        float WhisperDistance { get; set; }
        float SayDistance { get; set; }
        float ShoutDistance { get; set; }
        bool RenderWater { get; set; }
        int MaximumInventoryItemsTransfer { get; set; }
        bool DisplayMinimap { get; set; }
        bool AllowPhysicalPrims { get; set; }
        int OffsetOfUTC { get; set; }
        bool EnableTeenMode { get; set; }
        UUID DefaultUnderpants { get; set; }
        UUID DefaultUndershirt { get; set; }
        bool ClampPrimSizes { get; set; }
        bool ForceDrawDistance { get; set; }
        int ShowTags { get; set; }
        int MaxGroups { get; set; }
        bool AllowParcelWindLight { get; set; }
        void RegisterGenericValue(string key, string value);
    }
}
