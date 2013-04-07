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

namespace Aurora.Framework.ClientInterfaces
{
    public enum ParcelMediaCommandEnum
    {
        Stop = 0,
        Pause = 1,
        Play = 2,
        Loop = 3,
        Texture = 4,
        Url = 5,
        Time = 6,
        Agent = 7,
        Unload = 8,
        AutoAlign = 9,
        Type = 10,
        Size = 11,
        Desc = 12,
        LoopSet = 13
    }

    public enum PrimMediaCommandEnum
    {
        AltImageEnable = 0,
        Controls = 1,
        CurrentURL = 2,
        HomeURL = 3,
        AutoLoop = 4,
        AutoPlay = 5,
        AutoScale = 6,
        AutoZoom = 7,
        FirstClickInteract = 8,
        WidthPixels = 9,
        HeightPixels = 10,
        WhitelistEnable = 11,
        Whitelist = 12,
        PermsInteract = 13,
        PermsControl = 14
    }

    public enum PrimMediaUpdate
    {
        OK = 0,
        MALFORMED_PARAMS = 1000,
        TYPE_MISMATCH = 1001,
        BOUNDS_ERROR = 1002,
        NOT_FOUND = 1003,
        NOT_SUPPORTED = 1004,
        INTERNAL_ERROR = 1999,
        WHITELIST_FAILED = 2001,
    }
}