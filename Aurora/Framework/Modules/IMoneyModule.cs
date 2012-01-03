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

using OpenMetaverse;

namespace Aurora.Framework
{
    //public delegate void ObjectPaid(UUID objectID, UUID agentID, int amount);
    // For legacy money module. Fumi.Iseki
    public delegate bool ObjectPaid(UUID objectID, UUID agentID, int amount);

    // For DTL money module.
    public delegate bool PostObjectPaid(uint localID, ulong regionHandle, UUID agentID, int amount);

    public enum TransactionType
    {
        SystemGenerated = 0,
        RegionMoneyRequest = 1,
        Gift = 2,
        Purchase = 3,
        Upload = 4,
        ObjectPay = 5008
    }

    public interface IMoneyModule
    {
        int UploadCharge { get; }
        int GroupCreationCharge { get; }

        bool ObjectGiveMoney(UUID objectID, UUID fromID, UUID toID,
                             int amount);

        int Balance(IClientAPI client);
        bool Charge(IClientAPI client, int amount);
        bool Charge(UUID agentID, int amount, string text);

        event ObjectPaid OnObjectPaid;
        event PostObjectPaid OnPostObjectPaid;

        bool Transfer(UUID toID, UUID fromID, int amount, string description);
        bool Transfer(UUID toID, UUID fromID, int amount, string description, TransactionType type);

        bool Transfer(UUID toID, UUID fromID, UUID toObjectID, UUID fromObjectID, int amount, string description,
                      TransactionType type);
    }

    public delegate void UserDidNotPay(UUID agentID, string paymentTextThatFailed);
    public delegate bool CheckWhetherUserShouldPay(UUID agentID, string paymentTextThatFailed);

    public interface IScheduledMoneyModule
    {
        event UserDidNotPay OnUserDidNotPay;
        event CheckWhetherUserShouldPay OnCheckWhetherUserShouldPay;
        bool Charge(UUID agentID, int amount, string text, int daysUntilNextCharge);
    }
}