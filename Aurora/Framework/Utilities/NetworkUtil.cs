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
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Aurora.Framework.ConsoleFramework;

namespace Aurora.Framework.Utilities
{
    /// <summary>
    ///     Handles NAT translation in a 'manner of speaking'
    ///     Allows you to return multiple different external
    ///     hostnames depending on the requestors network
    ///     This enables standard port forwarding techniques
    ///     to work correctly with OpenSim.
    /// </summary>
    public static class NetworkUtil
    {
        private static bool m_disabled = true;

        // IPv4Address, Subnet
        private static readonly Dictionary<IPAddress, IPAddress> m_subnets = new Dictionary<IPAddress, IPAddress>();

        static NetworkUtil()
        {
            try
            {
                foreach (UnicastIPAddressInformation address in from ni in NetworkInterface.GetAllNetworkInterfaces()
                                                                from address in ni.GetIPProperties().UnicastAddresses
                                                                where
                                                                    address.Address.AddressFamily ==
                                                                    AddressFamily.InterNetwork
                                                                where address.IPv4Mask != null
                                                                select address)
                {
                    m_subnets.Add(address.Address, address.IPv4Mask);
                }
            }
            catch (NotImplementedException)
            {
                // Mono Sucks.
            }
        }

        public static bool Enabled
        {
            set { m_disabled = value; }
            get { return m_disabled; }
        }

        public static IPAddress GetIPFor(IPAddress user, IPAddress simulator)
        {
            if (m_disabled)
                return simulator;

            // Check if we're accessing localhost.
            foreach (
                IPAddress host in
                    Dns.GetHostAddresses(Dns.GetHostName())
                       .Where(host => host.Equals(user) && host.AddressFamily == AddressFamily.InterNetwork))
            {
                MainConsole.Instance.Info("[NetworkUtil] Localhost user detected, sending them '" + host +
                                          "' instead of '" +
                                          simulator + "'");
                return host;
            }

            // Check for same LAN segment
            foreach (KeyValuePair<IPAddress, IPAddress> subnet in m_subnets)
            {
                byte[] subnetBytes = subnet.Value.GetAddressBytes();
                byte[] localBytes = subnet.Key.GetAddressBytes();
                byte[] destBytes = user.GetAddressBytes();

                if (subnetBytes.Length != destBytes.Length || subnetBytes.Length != localBytes.Length)
                    return null;

                bool valid = !subnetBytes.Where((t, i) => (localBytes[i] & t) != (destBytes[i] & t)).Any();

                if (subnet.Key.AddressFamily != AddressFamily.InterNetwork)
                    valid = false;

                if (valid)
                {
                    MainConsole.Instance.Info("[NetworkUtil] Local LAN user detected, sending them '" + subnet.Key +
                                              "' instead of '" +
                                              simulator + "'");
                    return subnet.Key;
                }
            }

            // Otherwise, return outside address
            return simulator;
        }

        private static IPAddress GetExternalIPFor(IPAddress destination, string defaultHostname)
        {
            // Adds IPv6 Support (Not that any of the major protocols supports it...)
            if (destination.AddressFamily == AddressFamily.InterNetworkV6)
            {
                foreach (
                    IPAddress host in
                        Dns.GetHostAddresses(defaultHostname)
                           .Where(host => host.AddressFamily == AddressFamily.InterNetworkV6))
                {
                    MainConsole.Instance.Info("[NetworkUtil] Localhost user detected, sending them '" + host +
                                              "' instead of '" +
                                              defaultHostname + "'");
                    return host;
                }
            }

            if (destination.AddressFamily != AddressFamily.InterNetwork)
                return null;

            // Check if we're accessing localhost.
            foreach (
                IPAddress host in
                    m_subnets.Select(pair => pair.Value)
                             .Where(host => host.Equals(destination) && host.AddressFamily == AddressFamily.InterNetwork)
                )
            {
                MainConsole.Instance.Info("[NATROUTING] Localhost user detected, sending them '" + host +
                                          "' instead of '" +
                                          defaultHostname + "'");
                return destination;
            }

            // Check for same LAN segment
            foreach (KeyValuePair<IPAddress, IPAddress> subnet in m_subnets)
            {
                byte[] subnetBytes = subnet.Value.GetAddressBytes();
                byte[] localBytes = subnet.Key.GetAddressBytes();
                byte[] destBytes = destination.GetAddressBytes();

                if (subnetBytes.Length != destBytes.Length || subnetBytes.Length != localBytes.Length)
                    return null;

                bool valid = !subnetBytes.Where((t, i) => (localBytes[i] & t) != (destBytes[i] & t)).Any();

                if (subnet.Key.AddressFamily != AddressFamily.InterNetwork)
                    valid = false;

                if (valid)
                {
                    MainConsole.Instance.Info("[NetworkUtil] Local LAN user detected, sending them '" + subnet.Key +
                                              "' instead of '" +
                                              defaultHostname + "'");
                    return subnet.Key;
                }
            }

            // Check to see if we can find a IPv4 address.
            foreach (
                IPAddress host in
                    Dns.GetHostAddresses(defaultHostname)
                       .Where(host => host.AddressFamily == AddressFamily.InterNetwork))
            {
                return host;
            }

            // Unable to find anything.
            throw new ArgumentException(
                "[NetworkUtil] Unable to resolve defaultHostname to an IPv4 address for an IPv4 client");
        }

        public static IPAddress GetIPFor(IPEndPoint user, string defaultHostname)
        {
            if (!m_disabled)
            {
                // Try subnet matching
                IPAddress rtn = GetExternalIPFor(user.Address, defaultHostname);
                if (rtn != null)
                    return rtn;
            }

            // Otherwise use the old algorithm
            IPAddress ia;

            if (IPAddress.TryParse(defaultHostname, out ia))
                return ia;

            return
                Dns.GetHostAddresses(defaultHostname)
                   .FirstOrDefault(Adr => Adr.AddressFamily == AddressFamily.InterNetwork);
        }

        public static string GetHostFor(IPAddress user, string defaultHostname)
        {
            if (!m_disabled)
            {
                IPAddress rtn = GetExternalIPFor(user, defaultHostname);
                if (rtn != null)
                    return rtn.ToString();
            }
            return defaultHostname;
        }
    }
}