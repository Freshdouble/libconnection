/**
* @file "Hearbeat.cs"
* @author Florian Guggi
* @date 2019-12-20
* @brief Management of Heartbeats from IPEndPoints
*/

using System;
using System.Net;
using System.Collections.Generic;
using System.Threading;

namespace Heartbeat
{
    /**
    * @brief A simple manager for Hearbeats from an IPEndPoint. It updates on each retrieve call.
    */
    public class HeartbeatManager
    {
        /// Timeout in milliseconds
        public int timeout = 500;

        private Dictionary<IPEndPoint, DateTime> epMap = new Dictionary<IPEndPoint, DateTime>();
        private List<IPEndPoint> staticEndpoints = new List<IPEndPoint>();

        /**
        * @brief Simple Constructor
        * @param timeout The desired timeout in milliseconds
        */
        public HeartbeatManager(int timeout)
        {
            this.timeout = timeout;
        }

        public bool Verbose { get; set; } = true;

        public void AddStaticEndpoint(IPEndPoint endpoint)
        {
            lock (staticEndpoints)
            {
                if (!staticEndpoints.Contains(endpoint))
                {
                    staticEndpoints.Add(endpoint);
                }
            }
        }

        /**
        * @brief Call this everytime a heartbeat is received. Inserts or updates the IPEndPoint
        * @param ep IPEndPoint to update
        * @param timestamp DateTime at which the beat was received
        */
        public void beat(IPEndPoint ep, DateTime timestamp)
        {
            lock(epMap)
            {
                if (epMap.ContainsKey(ep))
                {
                    epMap[ep] = timestamp;
                }
                else if(!staticEndpoints.Contains(ep))
                {
                    epMap.Add(ep, timestamp);
                    if (Verbose)
                    {
                        Console.WriteLine("[HeartbeatManager] Port {0} connected", ep.Port);
                    }
                }
            }
        }

        /**
        * @brief Updates and then returns a list with all active IPEndPoints
        */
        public List<IPEndPoint> retrieve(bool withStaticEndpoint = true)
        {
            List<IPEndPoint> returnList = new List<IPEndPoint>();
            List<IPEndPoint> removeList = new List<IPEndPoint>();
            lock(epMap)
            {

                foreach (KeyValuePair<IPEndPoint, DateTime> pair in epMap)
                {
                    double diff = (DateTime.UtcNow - pair.Value.ToUniversalTime()).TotalMilliseconds;
                    if (diff < this.timeout)
                    {
                        // Still valid
                        returnList.Add(pair.Key);
                    }
                    else
                    {
                        // Invalid
                        removeList.Add(pair.Key);
                        if (Verbose)
                        {
                            Console.WriteLine("[HeartbeatManager] Port {0} timed out!", pair.Key.Port);
                        }
                    }
                }
                foreach(var pair in removeList)
                {
                    epMap.Remove(pair);
                }
                if (withStaticEndpoint)
                {
                    lock (staticEndpoints)
                    {
                        returnList.AddRange(staticEndpoints);
                    }
                }
            }
            return returnList;
        }
    }
}
