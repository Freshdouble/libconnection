using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace libconnection.Interfaces.UDP
{
    public class UdpReceiver : UdpTransceiver
    {
        public UdpReceiver(IPEndPoint localEndpoint, IPEndPoint remoteEndpoint = null, bool registerAtServer = false) : base(localEndpoint, remoteEndpoint, registerAtServer)
        {
        }

        public override bool SupportsDownstream => false;

        public override bool SupportsUpstream => true;

        public static UdpReceiver GenerateClassFromString(string[] parameter)
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
                    return new UdpReceiver(localEndpoint, remoteEndpoint, parameter[4].ToLower() == "true");
                }
                else
                {
                    return new UdpReceiver(localEndpoint, remoteEndpoint);
                }
            }
            else
                return new UdpReceiver(localEndpoint);
        }
    }
}
