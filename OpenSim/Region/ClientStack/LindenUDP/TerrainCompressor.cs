/*
 * Copyright (c) 2009, openmetaverse.org
 * All rights reserved.
 *
 * - Redistribution and use in source and binary forms, with or without
 *   modification, are permitted provided that the following conditions are met:
 *
 * - Redistributions of source code must retain the above copyright notice, this
 *   list of conditions and the following disclaimer.
 * - Neither the name of the openmetaverse.org nor the names
 *   of its contributors may be used to endorse or promote products derived from
 *   this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using OpenSim.Framework;
using OpenMetaverse;
using OpenMetaverse.Packets;

namespace OpenSim.Region.ClientStack.LindenUDP
{
    public static class AuroraTerrainCompressor
    {
        public const int END_OF_PATCHES = 97;

        private const float OO_SQRT2 = 0.7071067811865475244008443621049f;
        private const int STRIDE = 264;

        private const int ZERO_CODE = 0x0;
        private const int ZERO_EOB = 0x2;
        private const int POSITIVE_VALUE = 0x6;
        private const int NEGATIVE_VALUE = 0x7;

        private static readonly float[] DequantizeTable16 = new float[Constants.TerrainPatchSize * Constants.TerrainPatchSize];
        private static readonly float[] DequantizeTable32 = new float[Constants.TerrainPatchSize * Constants.TerrainPatchSize];
        private static readonly float[] CosineTable16 = new float[Constants.TerrainPatchSize * Constants.TerrainPatchSize];
        //private static readonly float[] CosineTable32 = new float[Constants.TerrainPatchSize * Constants.TerrainPatchSize];
        private static readonly int[] CopyMatrix16 = new int[Constants.TerrainPatchSize * Constants.TerrainPatchSize];
        private static readonly int[] CopyMatrix32 = new int[Constants.TerrainPatchSize * Constants.TerrainPatchSize];
        private static readonly float[] QuantizeTable16 = new float[Constants.TerrainPatchSize * Constants.TerrainPatchSize];

        static AuroraTerrainCompressor()
        {
            // Initialize the decompression tables
            BuildDequantizeTable16();
            SetupCosines16();
            BuildCopyMatrix16();
            BuildQuantizeTable16();
        }

        public static LayerDataPacket CreateLayerDataPacket(TerrainPatch[] patches, TerrainPatch.LayerType type, int RegionSizeX, int RegionSizeY)
        {
            LayerDataPacket layer = new LayerDataPacket();

            if (RegionSizeX > Constants.RegionSize || RegionSizeY > Constants.RegionSize)
                {
                // extended regions layers types
                
                if (type == TerrainPatch.LayerType.Land || type == TerrainPatch.LayerType.Water)
                    type++; // land becomes 77, Water 88
                else
                    type+=2; // wind becames 57, cloud 58
                }

            layer.LayerID.Type = (byte)type;

            TerrainPatch.GroupHeader header = new TerrainPatch.GroupHeader();
            header.Stride = STRIDE;
            header.PatchSize = Constants.TerrainPatchSize;
            header.Type = type;

            // Should be enough to fit even the most poorly packed data
            byte[] data = new byte[patches.Length * Constants.TerrainPatchSize * Constants.TerrainPatchSize * 2];
            BitPack bitpack = new BitPack(data, 0);
            bitpack.PackBits(header.Stride, 16);
            bitpack.PackBits(header.PatchSize, 8);
            bitpack.PackBits((int)header.Type, 8);

            for (int i = 0; i < patches.Length; i++)
                CreatePatch(bitpack, patches[i].Data, patches[i].X, patches[i].Y,RegionSizeX, RegionSizeY);

            bitpack.PackBits(END_OF_PATCHES, 8);

            layer.LayerData.Data = new byte[bitpack.BytePos + 1];
            Buffer.BlockCopy(bitpack.Data, 0, layer.LayerData.Data, 0, bitpack.BytePos + 1);

            return layer;
        }

        /// <summary>
        /// Creates a LayerData packet for compressed land data given a full
        /// simulator heightmap and an array of indices of patches to compress
        /// </summary>
        /// <param name="heightmap">A 256 * 256 array of floating point values
        /// specifying the height at each meter in the simulator</param>
        /// <param name="patches">Array of indexes in the 16x16 grid of patches
        /// for this simulator. For example if 1 and 17 are specified, patches
        /// x=1,y=0 and x=1,y=1 are sent</param>
        /// <returns></returns>
        public static LayerDataPacket CreateLandPacket(float[] heightmap, int[] x, int[] y, TerrainPatch.LayerType type, int RegionSizeX, int RegionSizeY)
        {
            LayerDataPacket layer = new LayerDataPacket();

            if (RegionSizeX > Constants.RegionSize || RegionSizeY > Constants.RegionSize)
                {
                // extended regions layers types

                if (type == TerrainPatch.LayerType.Land || type == TerrainPatch.LayerType.Water)
                    type++; // land becomes 77, Water 88
                else
                    type += 2; // wind becames 57, cloud 58
                }

            layer.LayerID.Type = (byte)type;

            TerrainPatch.GroupHeader header = new TerrainPatch.GroupHeader();
            header.Stride = STRIDE;
            header.PatchSize = Constants.TerrainPatchSize;
            header.Type = type;

            byte[] data = new byte[1536];
            BitPack bitpack = new BitPack(data, 0);
            bitpack.PackBits(header.Stride, 16);
            bitpack.PackBits(header.PatchSize, 8);
            bitpack.PackBits((int)header.Type, 8);

            for (int i = 0; i < x.Length; i++)
                CreatePatchFromHeightmap(bitpack, heightmap, x[i], y[i], RegionSizeX, RegionSizeY);

            bitpack.PackBits(END_OF_PATCHES, 8);

            layer.LayerData.Data = new byte[bitpack.BytePos + 1];
            Buffer.BlockCopy(bitpack.Data, 0, layer.LayerData.Data, 0, bitpack.BytePos + 1);

            return layer;
        }

        public static void CreatePatch(BitPack output, float[] patchData, int x, int y, int RegionSizeX, int RegionSizeY)
        {
            TerrainPatch.Header header = PrescanPatch(patchData);
            header.QuantWBits = 136;
            if (RegionSizeX > Constants.RegionSize || RegionSizeY > Constants.RegionSize)
                {
                header.PatchIDs = (y & 0xFFFF);
                header.PatchIDs += (x << 16);
                }
            else
                {
                header.PatchIDs = (y & 0x1F);
                header.PatchIDs += (x << 5);
                }


            // NOTE: No idea what prequant and postquant should be or what they do
            int[] patch = CompressPatch(patchData, header, 10);
            int wbits = EncodePatchHeader(output, header, patch, Constants.RegionSize, Constants.RegionSize);
            EncodePatch(output, patch, 0, wbits);
        }

        /// <summary>
        /// Add a patch of terrain to a BitPacker
        /// </summary>
        /// <param name="output">BitPacker to write the patch to</param>
        /// <param name="heightmap">Heightmap of the simulator, must be a 256 *
        /// 256 float array</param>
        /// <param name="x">X offset of the patch to create, valid values are
        /// from 0 to 15</param>
        /// <param name="y">Y offset of the patch to create, valid values are
        /// from 0 to 15</param>
        public static void CreatePatchFromHeightmap(BitPack output, float[] heightmap, int x, int y, int RegionSizeX, int RegionSizeY)
        {
            TerrainPatch.Header header = PrescanPatch(heightmap, x, y, RegionSizeX, RegionSizeY);
            header.QuantWBits = 136;
            if (RegionSizeX > Constants.RegionSize || RegionSizeY > Constants.RegionSize)
                {
                header.PatchIDs = (y & 0xFFFF);
                header.PatchIDs += (x << 16);
                }
            else
                {
                header.PatchIDs = (y & 0x1F);
                header.PatchIDs += (x << 5);
                }

            // NOTE: No idea what prequant and postquant should be or what they do
            int[] patch = CompressPatch(heightmap, x, y, header, 10, RegionSizeX, RegionSizeY);
            int wbits = EncodePatchHeader(output, header, patch, RegionSizeX, RegionSizeY);
            EncodePatch(output, patch, 0, wbits);
        }

        private static TerrainPatch.Header PrescanPatch(float[] patch)
        {
            TerrainPatch.Header header = new TerrainPatch.Header();
            float zmax = -99999999.0f;
            float zmin = 99999999.0f;

            for (int j = 0; j < Constants.TerrainPatchSize; j++)
            {
                for (int i = 0; i < Constants.TerrainPatchSize; i++)
                {
                    float val = patch[j * Constants.TerrainPatchSize + i];
                    if (val > zmax) zmax = val;
                    if (val < zmin) zmin = val;
                }
            }

            header.DCOffset = zmin;
            header.Range = (int)((zmax - zmin) + 1.0f);

            return header;
        }

        private static TerrainPatch.Header PrescanPatch(float[] heightmap, int patchX, int patchY, int RegionSizeX, int RegionSizeY)
        {
            TerrainPatch.Header header = new TerrainPatch.Header();
            float zmax = -99999999.0f;
            float zmin = 99999999.0f;

//            int sqrt = (int)Math.Sqrt(heightmap.Length);
            for (int j = patchY * Constants.TerrainPatchSize; j < ((patchY >= (RegionSizeY / Constants.TerrainPatchSize) ? (RegionSizeY - Constants.TerrainPatchSize) / Constants.TerrainPatchSize : patchY) + 1) * Constants.TerrainPatchSize; j++)
            {
                for (int i = patchX * Constants.TerrainPatchSize; i < ((patchX >= (RegionSizeX / Constants.TerrainPatchSize) ? (RegionSizeX - Constants.TerrainPatchSize) / Constants.TerrainPatchSize : patchX) + 1) * Constants.TerrainPatchSize; i++)
                {
                    float val = heightmap[j * RegionSizeX + i];
                    if (val > zmax) zmax = val;
                    if (val < zmin) zmin = val;
                }
            }

            header.DCOffset = zmin;
            header.Range = (int)((zmax - zmin) + 1.0f);

            return header;
        }

        public static TerrainPatch.Header DecodePatchHeader(BitPack bitpack)
        {
            TerrainPatch.Header header = new TerrainPatch.Header();

            // Quantized word bits
            header.QuantWBits = bitpack.UnpackBits(8);
            if (header.QuantWBits == END_OF_PATCHES)
                return header;

            // DC offset
            header.DCOffset = bitpack.UnpackFloat();

            // Range
            header.Range = bitpack.UnpackBits(16);

            // Patch IDs (10 bits)
            header.PatchIDs = bitpack.UnpackBits(10);

            // Word bits
            header.WordBits = (uint)((header.QuantWBits & 0x0f) + 2);

            return header;
        }

        private static int EncodePatchHeader(BitPack output, TerrainPatch.Header header, int[] patch, int RegionSizeX, int RegionSizeY)
        {
            int temp;
            int wbits = (header.QuantWBits & 0x0f) + 2;
            uint maxWbits = (uint)wbits + 5;
            uint minWbits = ((uint)wbits >> 1);

            wbits = (int)minWbits;

            for (int i = 0; i < patch.Length; i++)
            {
                temp = patch[i];

                if (temp != 0)
                {
                    // Get the absolute value
                    if (temp < 0) temp *= -1;

                    for (int j = (int)maxWbits; j > (int)minWbits; j--)
                    {
                        if ((temp & (1 << j)) != 0)
                        {
                            if (j > wbits) wbits = j;
                            break;
                        }
                    }
                }
            }

            wbits += 1;

            header.QuantWBits &= 0xf0;

            header.QuantWBits |= (wbits - 2);

            output.PackBits(header.QuantWBits, 8);
            output.PackFloat(header.DCOffset);
            output.PackBits(header.Range, 16);
//            if (RegionSizeX != Constants.RegionSize)
            if (RegionSizeX > Constants.RegionSize || RegionSizeY > Constants.RegionSize)
                output.PackBits(header.PatchIDs, 32);
            else
                output.PackBits(header.PatchIDs, 10);

            return wbits;
        }

        private static void IDCTColumn16(float[] linein, float[] lineout, int column)
        {
            float total;
            int usize;

            for (int n = 0; n < Constants.TerrainPatchSize; n++)
            {
                total = OO_SQRT2 * linein[column];

                for (int u = 1; u < Constants.TerrainPatchSize; u++)
                {
                    usize = u * Constants.TerrainPatchSize;
                    total += linein[usize + column] * CosineTable16[usize + n];
                }

                lineout[Constants.TerrainPatchSize * n + column] = total;
            }
        }

        private static void IDCTLine16(float[] linein, float[] lineout, int line)
        {
            const float oosob = 2.0f / Constants.TerrainPatchSize;
            int lineSize = line * Constants.TerrainPatchSize;
            float total;

            for (int n = 0; n < Constants.TerrainPatchSize; n++)
            {
                total = OO_SQRT2 * linein[lineSize];

                for (int u = 1; u < Constants.TerrainPatchSize; u++)
                {
                    total += linein[lineSize + u] * CosineTable16[u * Constants.TerrainPatchSize + n];
                }

                lineout[lineSize + n] = total * oosob;
            }
        }

        private static void DCTLine16(float[] linein, float[] lineout, int line)
        {
            float total = 0.0f;
            int lineSize = line * Constants.TerrainPatchSize;

            for (int n = 0; n < Constants.TerrainPatchSize; n++)
            {
                total += linein[lineSize + n];
            }

            lineout[lineSize] = OO_SQRT2 * total;

            for (int u = 1; u < Constants.TerrainPatchSize; u++)
            {
                total = 0.0f;

                for (int n = 0; n < Constants.TerrainPatchSize; n++)
                {
                    total += linein[lineSize + n] * CosineTable16[u * Constants.TerrainPatchSize + n];
                }

                lineout[lineSize + u] = total;
            }
        }

        private static void DCTColumn16(float[] linein, int[] lineout, int column)
        {
            float total = 0.0f;
            const float oosob = 2.0f / Constants.TerrainPatchSize;

            for (int n = 0; n < Constants.TerrainPatchSize; n++)
            {
                total += linein[Constants.TerrainPatchSize * n + column];
            }

            lineout[CopyMatrix16[column]] = (int)(OO_SQRT2 * total * oosob * QuantizeTable16[column]);

            for (int u = 1; u < Constants.TerrainPatchSize; u++)
            {
                total = 0.0f;

                for (int n = 0; n < Constants.TerrainPatchSize; n++)
                {
                    total += linein[Constants.TerrainPatchSize * n + column] * CosineTable16[u * Constants.TerrainPatchSize + n];
                }

                lineout[CopyMatrix16[Constants.TerrainPatchSize * u + column]] = (int)(total * oosob * QuantizeTable16[Constants.TerrainPatchSize * u + column]);
            }
        }

        public static void DecodePatch(int[] patches, BitPack bitpack, TerrainPatch.Header header, int size)
        {
            int temp;
            for (int n = 0; n < size * size; n++)
            {
                // ?
                temp = bitpack.UnpackBits(1);
                if (temp != 0)
                {
                    // Value or EOB
                    temp = bitpack.UnpackBits(1);
                    if (temp != 0)
                    {
                        // Value
                        temp = bitpack.UnpackBits(1);
                        if (temp != 0)
                        {
                            // Negative
                            temp = bitpack.UnpackBits((int)header.WordBits);
                            patches[n] = temp * -1;
                        }
                        else
                        {
                            // Positive
                            temp = bitpack.UnpackBits((int)header.WordBits);
                            patches[n] = temp;
                        }
                    }
                    else
                    {
                        // Set the rest to zero
                        // TODO: This might not be necessary
                        for (int o = n; o < size * size; o++)
                        {
                            patches[o] = 0;
                        }
                        break;
                    }
                }
                else
                {
                    patches[n] = 0;
                }
            }
        }

        private static void EncodePatch(BitPack output, int[] patch, int postquant, int wbits)
        {
            int temp;
            bool eob;

            if (postquant > Constants.TerrainPatchSize * Constants.TerrainPatchSize || postquant < 0)
            {
                Logger.Log("Postquant is outside the range of allowed values in EncodePatch()", Helpers.LogLevel.Error);
                return;
            }

            if (postquant != 0) patch[Constants.TerrainPatchSize * Constants.TerrainPatchSize - postquant] = 0;

            for (int i = 0; i < Constants.TerrainPatchSize * Constants.TerrainPatchSize; i++)
            {
                eob = false;
                temp = patch[i];

                if (temp == 0)
                {
                    eob = true;

                    for (int j = i; j < Constants.TerrainPatchSize * Constants.TerrainPatchSize - postquant; j++)
                    {
                        if (patch[j] != 0)
                        {
                            eob = false;
                            break;
                        }
                    }

                    if (eob)
                    {
                        output.PackBits(ZERO_EOB, 2);
                        return;
                    }
                    else
                    {
                        output.PackBits(ZERO_CODE, 1);
                    }
                }
                else
                {
                    if (temp < 0)
                    {
                        temp *= -1;

                        if (temp > (1 << wbits)) temp = (1 << wbits);

                        output.PackBits(NEGATIVE_VALUE, 3);
                        output.PackBits(temp, wbits);
                    }
                    else
                    {
                        if (temp > (1 << wbits)) temp = (1 << wbits);

                        output.PackBits(POSITIVE_VALUE, 3);
                        output.PackBits(temp, wbits);
                    }
                }
            }
        }

        public static float[] DecompressPatch(int[] patches, TerrainPatch.Header header, TerrainPatch.GroupHeader group)
        {
            float[] block = new float[group.PatchSize * group.PatchSize];
            float[] output = new float[group.PatchSize * group.PatchSize];
            int prequant = (header.QuantWBits >> 4) + 2;
            int quantize = 1 << prequant;
            float ooq = 1.0f / (float)quantize;
            float mult = ooq * (float)header.Range;
            float addval = mult * (float)(1 << (prequant - 1)) + header.DCOffset;

            if (group.PatchSize == Constants.TerrainPatchSize)
            {
                for (int n = 0; n < Constants.TerrainPatchSize * Constants.TerrainPatchSize; n++)
                {
                    block[n] = patches[CopyMatrix16[n]] * DequantizeTable16[n];
                }

                float[] ftemp = new float[Constants.TerrainPatchSize * Constants.TerrainPatchSize];

                for (int o = 0; o < Constants.TerrainPatchSize; o++)
                    IDCTColumn16(block, ftemp, o);
                for (int o = 0; o < Constants.TerrainPatchSize; o++)
                    IDCTLine16(ftemp, block, o);
            }
            else
            {
                for (int n = 0; n < Constants.TerrainPatchSize * 2 * Constants.TerrainPatchSize * 2; n++)
                {
                    block[n] = patches[CopyMatrix32[n]] * DequantizeTable32[n];
                }

                Logger.Log("Implement IDCTPatchLarge", Helpers.LogLevel.Error);
            }

            for (int j = 0; j < block.Length; j++)
            {
                output[j] = block[j] * mult + addval;
            }

            return output;
        }

        private static int[] CompressPatch(float[] patchData, TerrainPatch.Header header, int prequant)
        {
            float[] block = new float[Constants.TerrainPatchSize * Constants.TerrainPatchSize];
            int wordsize = prequant;
            float oozrange = 1.0f / (float)header.Range;
            float range = (float)(1 << prequant);
            float premult = oozrange * range;
            float sub = (float)(1 << (prequant - 1)) + header.DCOffset * premult;

            header.QuantWBits = wordsize - 2;
            header.QuantWBits |= (prequant - 2) << 4;

            int k = 0;
            for (int j = 0; j < Constants.TerrainPatchSize; j++)
            {
                for (int i = 0; i < Constants.TerrainPatchSize; i++)
                    block[k++] = patchData[j * Constants.TerrainPatchSize + i] * premult - sub;
            }

            float[] ftemp = new float[Constants.TerrainPatchSize * Constants.TerrainPatchSize];
            int[] itemp = new int[Constants.TerrainPatchSize * Constants.TerrainPatchSize];

            for (int o = 0; o < Constants.TerrainPatchSize; o++)
                DCTLine16(block, ftemp, o);
            for (int o = 0; o < Constants.TerrainPatchSize; o++)
                DCTColumn16(ftemp, itemp, o);

            return itemp;
        }

        private static int[] CompressPatch(float[,] patchData, TerrainPatch.Header header, int prequant)
        {
            float[] block = new float[Constants.TerrainPatchSize * Constants.TerrainPatchSize];
            int wordsize = prequant;
            float oozrange = 1.0f / (float)header.Range;
            float range = (float)(1 << prequant);
            float premult = oozrange * range;
            float sub = (float)(1 << (prequant - 1)) + header.DCOffset * premult;

            header.QuantWBits = wordsize - 2;
            header.QuantWBits |= (prequant - 2) << 4;

            int k = 0;
            for (int j = 0; j < Constants.TerrainPatchSize; j++)
            {
                for (int i = 0; i < Constants.TerrainPatchSize; i++)
                    block[k++] = patchData[j, i] * premult - sub;
            }

            float[] ftemp = new float[Constants.TerrainPatchSize * Constants.TerrainPatchSize];
            int[] itemp = new int[Constants.TerrainPatchSize * Constants.TerrainPatchSize];

            for (int o = 0; o < Constants.TerrainPatchSize; o++)
                DCTLine16(block, ftemp, o);
            for (int o = 0; o < Constants.TerrainPatchSize; o++)
                DCTColumn16(ftemp, itemp, o);

            return itemp;
        }

        private static int[] CompressPatch(float[] heightmap, int patchX, int patchY, TerrainPatch.Header header, int prequant, int RegionSizeX, int RegionSizeY)
        {
            float[] block = new float[Constants.TerrainPatchSize * Constants.TerrainPatchSize];
            int wordsize = prequant;
            float oozrange = 1.0f / (float)header.Range;
            float range = (float)(1 << prequant);
            float premult = oozrange * range;
            float sub = (float)(1 << (prequant - 1)) + header.DCOffset * premult;

            header.QuantWBits = wordsize - 2;
            header.QuantWBits |= (prequant - 2) << 4;

            int k = 0;
//            int sqrt = (int)Math.Sqrt(heightmap.Length);
            //OpenSim.Framework.Console.MainConsole.Instance.Output(sqrt + "," + patchX + "," + patchY + ","+
            //    patchX * Constants.TerrainPatchSize + "," + 
            //    ((patchX >= (RegionSizeX / Constants.TerrainPatchSize) ? (RegionSizeX - Constants.TerrainPatchSize) / Constants.TerrainPatchSize : patchX) + 1) * Constants.TerrainPatchSize + "," +
            //    patchY * Constants.TerrainPatchSize + "," +
            //    ((patchY >= (RegionSizeY / Constants.TerrainPatchSize) ? (RegionSizeY - Constants.TerrainPatchSize) / Constants.TerrainPatchSize : patchY) + 1) * Constants.TerrainPatchSize);

            for (int j = patchY * Constants.TerrainPatchSize; j < ((patchY >= (RegionSizeY / Constants.TerrainPatchSize) ? (RegionSizeY - Constants.TerrainPatchSize) / Constants.TerrainPatchSize : patchY) + 1) * Constants.TerrainPatchSize; j++)
            {
                for (int i = patchX * Constants.TerrainPatchSize; i < ((patchX >= (RegionSizeX / Constants.TerrainPatchSize) ? (RegionSizeX - Constants.TerrainPatchSize) / Constants.TerrainPatchSize : patchX) + 1) * Constants.TerrainPatchSize; i++)
//                    block[k++] = heightmap[j * sqrt + i] * premult - sub;
                    block[k++] = heightmap[j * RegionSizeX + i] * premult - sub;
            }

            float[] ftemp = new float[Constants.TerrainPatchSize * Constants.TerrainPatchSize];
            int[] itemp = new int[Constants.TerrainPatchSize * Constants.TerrainPatchSize];

            for (int o = 0; o < Constants.TerrainPatchSize; o++)
                DCTLine16(block, ftemp, o);
            for (int o = 0; o < Constants.TerrainPatchSize; o++)
                DCTColumn16(ftemp, itemp, o);

            return itemp;
        }

        #region Initialization

        private static void BuildDequantizeTable16()
        {
            for (int j = 0; j < Constants.TerrainPatchSize; j++)
            {
                for (int i = 0; i < Constants.TerrainPatchSize; i++)
                {
                    DequantizeTable16[j * Constants.TerrainPatchSize + i] = 1.0f + 2.0f * (float)(i + j);
                }
            }
        }

        private static void BuildQuantizeTable16()
        {
            for (int j = 0; j < Constants.TerrainPatchSize; j++)
            {
                for (int i = 0; i < Constants.TerrainPatchSize; i++)
                {
                    QuantizeTable16[j * Constants.TerrainPatchSize + i] = 1.0f / (1.0f + 2.0f * ((float)i + (float)j));
                }
            }
        }

        private static void SetupCosines16()
        {
            const float hposz = (float)Math.PI * 0.5f / Constants.TerrainPatchSize;

            for (int u = 0; u < Constants.TerrainPatchSize; u++)
            {
                for (int n = 0; n < Constants.TerrainPatchSize; n++)
                {
                    CosineTable16[u * Constants.TerrainPatchSize + n] = (float)Math.Cos((2.0f * (float)n + 1.0f) * (float)u * hposz);
                }
            }
        }

        private static void BuildCopyMatrix16()
        {
            bool diag = false;
            bool right = true;
            int i = 0;
            int j = 0;
            int count = 0;

            while (i < Constants.TerrainPatchSize && j < Constants.TerrainPatchSize)
            {
                CopyMatrix16[j * Constants.TerrainPatchSize + i] = count++;

                if (!diag)
                {
                    if (right)
                    {
                        if (i < Constants.TerrainPatchSize - 1) i++;
                        else j++;

                        right = false;
                        diag = true;
                    }
                    else
                    {
                        if (j < Constants.TerrainPatchSize - 1) j++;
                        else i++;

                        right = true;
                        diag = true;
                    }
                }
                else
                {
                    if (right)
                    {
                        i++;
                        j--;
                        if (i == Constants.TerrainPatchSize - 1 || j == 0) diag = false;
                    }
                    else
                    {
                        i--;
                        j++;
                        if (j == Constants.TerrainPatchSize - 1 || i == 0) diag = false;
                    }
                }
            }
        }

        #endregion Initialization
    }
}