using System.Collections.Generic;
using System.Linq;

namespace libconnection.Interfaces.Mqtt
{
    public class MqttConnectionHandler
    {
        public MqttServerConnection CreateConnection(MqttXML xml)
        {
            var serverConnection = new MqttServerConnection(xml);
            return serverConnection;
        }
    }
}