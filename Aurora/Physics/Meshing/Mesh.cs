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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Aurora.Framework.Physics;
using OpenMetaverse;
using OSDArray = OpenMetaverse.StructuredData.OSDArray;
using OSD = OpenMetaverse.StructuredData.OSD;
#if DEBUGING
using PrimMesher;
#else
using Aurora.Physics.PrimMesher;
#endif

namespace Aurora.Physics.Meshing
{
    public class Mesh : IMesh
    {
        private readonly ulong m_key;
        public bool WasCached { get; set; }

        private Vector3 _centroid;
        private int _centroidDiv;
        private IntPtr m_indicesPtr = IntPtr.Zero;
        private GCHandle m_pinnedIndex;
        private GCHandle m_pinnedVertexes;
        private int[] m_triangles;
        private float[] m_vertices;
        private int m_vertexCount;
        private int m_indexCount;
        private IntPtr m_verticesPtr = IntPtr.Zero;

        public Mesh(ulong key)
        {
            m_key = key;
            _centroid = Vector3.Zero;
            _centroidDiv = 0;
        }

        #region IMesh Members

        public ulong Key
        {
            get { return m_key; }
        }

        public Vector3 GetCentroid()
        {
            if (_centroidDiv > 0)
            {
                float tmp = 1.0f/_centroidDiv;
                return new Vector3(_centroid.X*tmp, _centroid.Y*tmp, _centroid.Z*tmp);
            }
            else
                return Vector3.Zero;
        }

        public void getVertexListAsPtrToFloatArray(out IntPtr vertices, out int vertexStride, out int vertexCount)
        {
            // A vertex is 3 floats
            vertexStride = 3*sizeof (float);

            // If there isn't an unmanaged array allocated yet, do it now
            if (m_verticesPtr == IntPtr.Zero)
            {
                // Each vertex is 3 elements (floats)
                int byteCount = m_vertexCount*vertexStride;
                m_verticesPtr = Marshal.AllocHGlobal(byteCount);
                Marshal.Copy(m_vertices, 0, m_verticesPtr, m_vertexCount*3);
                if (m_vertexCount > 0)
                    GC.AddMemoryPressure((long) m_vertexCount*3);
            }
            vertices = m_verticesPtr;
            vertexCount = m_vertexCount;
        }

        public void setIndexListAsInt(List<Face> faces)
        {
            m_triangles = new int[faces.Count * 3];
            for (int i = 0; i < faces.Count; i++)
            {
                //Face t = m_triangles[i];
                m_triangles[3 * i + 0] = faces[i].v1;
                m_triangles[3 * i + 1] = faces[i].v2;
                m_triangles[3 * i + 2] = faces[i].v3;
            }
            m_indexCount = m_triangles.Length;
        }

        private void setVertexListAsFloat(List<Coord> coords)
        {
            m_vertices = new float[coords.Count * 3];
            for (int i = 0; i < coords.Count; i++)
            {
                //Coord v = m_vertices[i];
                m_vertices[3 * i + 0] = coords[i].X;
                m_vertices[3 * i + 1] = coords[i].Y;
                m_vertices[3 * i + 2] = coords[i].Z;
            }
            m_vertexCount = m_vertices.Length / 3;
        }

        public void getIndexListAsPtrToIntArray(out IntPtr indices, out int triStride, out int indexCount)
        {
            // If there isn't an unmanaged array allocated yet, do it now
            if (m_indicesPtr == IntPtr.Zero)
            {
                int byteCount = m_indexCount*sizeof (int);
                m_indicesPtr = Marshal.AllocHGlobal(byteCount);
                Marshal.Copy(m_triangles, 0, m_indicesPtr, m_indexCount);
                if (byteCount > 0)
                    GC.AddMemoryPressure(byteCount);
            }
            // A triangle is 3 ints (indices)
            triStride = 3*sizeof (int);
            indices = m_indicesPtr;
            indexCount = m_indexCount;
        }

        public void releasePinned()
        {
            if (m_pinnedVertexes.IsAllocated)
                m_pinnedVertexes.Free();
            if (m_pinnedIndex.IsAllocated)
                m_pinnedIndex.Free();
            if (m_verticesPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(m_verticesPtr);
                m_verticesPtr = IntPtr.Zero;
            }
            if (m_indicesPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(m_indicesPtr);
                m_indicesPtr = IntPtr.Zero;
            }
        }

        /// <summary>
        ///     frees up the source mesh data to minimize memory - call this method after calling get*Locked() functions
        /// </summary>
        public void releaseSourceMeshData()
        {
            m_triangles = null;
            m_vertices = null;
            releasePinned();
        }

        #endregion

        public void Set(List<Coord> vertices, List<Face> faces)
        {
            if (m_pinnedIndex.IsAllocated || m_pinnedVertexes.IsAllocated || m_indicesPtr != IntPtr.Zero ||
                m_verticesPtr != IntPtr.Zero)
                throw new NotSupportedException("Attempt to Add to a pinned Mesh");

            _centroid = Vector3.Zero;
            _centroidDiv = 0;
            foreach (Coord vert in vertices)
            {
                _centroid.X += vert.X;
                _centroid.Y += vert.Y;
                _centroid.Z += vert.Z;
                _centroidDiv++;
            }

            setIndexListAsInt(faces);
            setVertexListAsFloat(vertices);
        }

        public OSD Serialize()
        {
            OSDArray array = new OSDArray();
            /*foreach (Face t in m_triangles)
            {
                OSDArray triArray = new OSDArray
                                        {
                                            new Vector3(t.v1.X, t.v1.Y, t.v1.Z),
                                            new Vector3(t.v2.X, t.v2.Y, t.v2.Z),
                                            new Vector3(t.v3.X, t.v3.Y, t.v3.Z)
                                        };
                array.Add(triArray);
            }*/
            return array;
        }

        public void Deserialize(OSD cachedMesh)
        {
            /*OSDArray array = (OSDArray) cachedMesh;
            foreach (OSD triangle in array)
            {
                OSDArray triangleArray = (OSDArray) triangle;
                Add(new Triangle(new Coord(triangleArray[0].AsVector3()),
                                 new Coord(triangleArray[1].AsVector3()),
                                 new Coord(triangleArray[2].AsVector3())));
            }*/
        }
    }
}