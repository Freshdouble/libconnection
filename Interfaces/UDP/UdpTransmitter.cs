using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace libconnection.Interfaces.UDP
{
    public class UdpTransmitter : UdpTransceiver
    {
        public override bool SupportsDownstream => true;

        public override bool SupportsUpstream => false;
        public UdpTransmitter(IPEndPoint localEndpoint, IPEndPoint remoteEndpoint = null, bool sendheartbeat = false) : base(localEndpoint, remoteEndpoint, sendheartbeat)
        {
        }

        public static UdpTransmitter GenerateClassFromString(string[] parameter)
        {
            if (parameter.Length < 2)
            {
                throw new ArgumentException("localEndpoint needs at least two parameter");
            }
            IPEndPoint localEndpoint = new IPEndPoint(IPAddress.Parse(parameter[0]), int.Parse(parameter[1]));
            if (parameter.Length >= 3)
            {
                if (parameter.Length < 4)
                {
                    throw new ArgumentException("remoteEndpoint needs at least two parameter");
                }
                IPEndPoint remoteEndpoint = new IPEndPoint(IPAddress.Parse(parameter[2]), int.Parse(parameter[3]));
                if (parameter.Length >= 5)
                {
                    return new UdpTransmitter(localEndpoint, remoteEndpoint, parameter[4].ToLower() == "true");
                }
                else
                {
                    return new UdpTransmitter(localEndpoint, remoteEndpoint);
                }
            }
            else
                return new UdpTransmitter(localEndpoint);
        }
    }
}
