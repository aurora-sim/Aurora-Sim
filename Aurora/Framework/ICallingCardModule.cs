using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenSim.Framework;

namespace Aurora.Framework
{
    public interface ICallingCardModule
    {
        /// <summary>
        /// Create a calling card for the given user
        /// </summary>
        /// <param name="client">User the card is being given to</param>
        /// <param name="creator">Creator (the person giving the calling card) of the card</param>
        /// <param name="folder">Folder to create the calling card in</param>
        /// <param name="name">Name of the calling card</param>
        void CreateCallingCard(IClientAPI client, UUID creator, UUID folder, string name);
    }
}
