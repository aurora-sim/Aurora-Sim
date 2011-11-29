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
using OpenMetaverse;

namespace Aurora.DataManager
{
    public static class DBGuid
    {
        /// <summary>
        ///   This function converts a value returned from the database in one of the
        ///   supported formats into a UUID.  This function is not actually DBMS-specific right
        ///   now
        /// </summary>
        /// <param name = "id"></param>
        /// <returns></returns>
        public static UUID FromDB(object id)
        {
            if ((id == null) || (id == DBNull.Value))
                return UUID.Zero;

            if (id.GetType() == typeof (Guid))
                return new UUID((Guid) id);

            if (id.GetType() == typeof (byte[]))
            {
                if (((byte[]) id).Length == 0)
                    return UUID.Zero;
                else if (((byte[]) id).Length == 16)
                    return new UUID((byte[]) id, 0);
                else
                {
                    string sid = Utils.BytesToString(((byte[]) id));
                    string[] split = sid.Split(';');
                    return new UUID(split[0]); //Old HyperGrid object
                }
            }
            else if (id.GetType() == typeof (string))
            {
                if (((string) id).Length == 0)
                    return UUID.Zero;
                else if (((string) id).Length == 36)
                    return new UUID((string) id);
                else
                {
                    string[] split = ((string) id).Split(';');
                    return new UUID(split[0]); //Old HyperGrid object
                }
            }

            throw new Exception("Failed to convert db value to UUID: " + id);
        }
    }
}