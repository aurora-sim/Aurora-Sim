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

using System.Collections.Generic;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{
    public class Email : IDataTransferable
    {
        public string message;
        public int numLeft;
        public string sender;
        public string subject;
        public string time;
        public UUID toPrimID;

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["message"] = message;
            map["numLeft"] = numLeft;
            map["sender"] = sender;
            map["subject"] = subject;
            map["time"] = time;
            map["toPrimID"] = toPrimID;
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            message = map["message"];
            numLeft = map["numLeft"];
            sender = map["sender"];
            subject = map["subject"];
            time = map["time"];
            toPrimID = map["toPrimID"];
        }
    }

    public interface IEmailConnector : IAuroraDataPlugin
    {
        /// <summary>
        ///   Adds an email to the database for the prim to get later
        /// </summary>
        /// <param name = "email"></param>
        void InsertEmail(Email email);

        /// <summary>
        ///   Finds previously saved AA data.
        /// </summary>
        /// <param name = "primID"></param>
        /// <returns></returns>
        List<Email> GetEmails(UUID primID);
    }
}