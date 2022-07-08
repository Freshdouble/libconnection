using System.Net;
using System.Xml.Serialization;

namespace libconnection.Interfaces.UDP
{
    [XmlRoot("UdpReceiver")]
    public class UDPReceiverXml : IDataStreamFactory
    {
        public string LocalIpAddress { get; set; }
        public string RemoteIpAddress { get; set; } = "";
        public int LocalPort { get; set; } = 0;
        public int RemotePort { get; set; } = 0;
        public bool UseActiveServerConnection { get; set; } = false;

        public DataStream GetInstance()
        {
            IPEndPoint local = new IPEndPoint(IPAddress.Parse(LocalIpAddress), LocalPort);
            IPEndPoint remote = null;
            if (!string.IsNullOrEmpty(RemoteIpAddress) && RemotePort != 0)
            {
                remote = new IPEndPoint(IPAddress.Parse(RemoteIpAddress), RemotePort);
            }

            return new UdpReceiver(local, remote, UseActiveServerConnection);
        }
    }
}