using libconnection.Decoders;
using libconnection.Interfaces;
using libconnection.Interfaces.UDP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libconnection.Interfaces.Mqtt;

namespace libconnection
{
    public class NameResolver
    {
        public static DataStream GetStreamByName(string name, IEnumerable<string> initialization)
        {
            switch(name.ToLower())
            {
                case "udpserver":
                    return UDPServer.GenerateClassFromString(initialization.ToArray());
                case "udpreceiver":
                    return UdpReceiver.GenerateClassFromString(initialization.ToArray());
                case "serial":
                case "serialport":
                    return SerialPortConnection.GenerateClassFromString(initialization.ToArray());
                case "smp":
                    return new SMPDecoder();
            }
            return null;
        }
    }
}
