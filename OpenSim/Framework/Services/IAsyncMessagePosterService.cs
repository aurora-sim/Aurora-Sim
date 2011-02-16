using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.Interfaces
{
    /// <summary>
    /// This Service deals with posting events to a (local or remote) host
    /// This is used for secure communications between regions and the grid service
    /// for things such as EventQueueMessages and posting of a grid wide notice
    /// </summary>
    public interface ISyncMessagePosterService
    {
        /// <summary>
        /// Post a request to all hosts that we have
        /// This is asyncronous.
        /// </summary>
        /// <param name="request"></param>
        void Post(OSDMap request);

        /// <summary>
        /// Post a request to all hosts that we have
        /// Returns an OSDMap of the response.
        /// This is syncronous
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        OSDMap Get(OSDMap request);
    }

    public delegate OSDMap MessageReceived(OSDMap message);
    /// <summary>
    /// This is used to deal with incoming requests from the ISyncMessagePosterService
    /// </summary>
    public interface IAsyncMessageRecievedService
    {
        /// <summary>
        /// This is fired when a message from the ISyncMessagePosterService
        ///   has been received either by the IAsyncMessagePosterService for Aurora
        ///   or the ISyncMessagePosterService for Aurora.Server
        ///   
        /// Notes on this event:
        ///   This is subscribed to by many events and many events will not be dealing with the request.
        ///   If you do not wish to send a response back to the poster, return null, otherwise, return a
        ///   valid OSDMap that will be added to the response.
        /// </summary>
        event MessageReceived OnMessageReceived;

        /// <summary>
        /// Fire the MessageReceived event
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        OSDMap FireMessageReceived(OSDMap message);
    }

    /// <summary>
    /// This interface is used mainly on Aurora.Server to asyncronously post events to
    ///   regions in the grid. This can be used to send grid wide notices or other events 
    ///   that regions need to know about
    /// </summary>
    public interface IAsyncMessagePosterService
    {
        /// <summary>
        /// Post a request to all hosts that we have
        /// This is asyncronous.
        /// </summary>
        /// <param name="request"></param>
        void Post(OSDMap request);
    }
}
