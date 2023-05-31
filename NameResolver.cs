using libconnection.Decoders;
using libconnection.Interfaces;
using libconnection.Interfaces.UDP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libconnection
{
    public class NameResolver
    {
        public static DataStream GetStreamByName(string name, IDictionary<string, string> initialization)
        {
            switch(name.ToLower())
            {
                /*
                case "udpserver":
                    return UDPServer.GenerateWithParameters(initialization.ToArray());*/
                case "udp":
                case "udptransceiver":
                    return UdpTransceiver.GenerateWithParameters(initialization);
                case "serial":
                case "serialport":
                    return SerialPortConnection.GenerateWithParameters(initialization);
                case "smp":
                    return new SMPDecoder();
                case "console":
                    return new console();
                case "fileinput":
                    return new FileInputInterface(initialization["file"]);
                case "fileoutput":
                    return new FileOutputInterface(initialization["file"]);
                    
            }
            return null;
        }
    }
}
