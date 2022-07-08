using System.Collections.Generic;
using System.Xml.Serialization;

namespace libconnection.Interfaces.Mqtt
{
    [XmlRoot("MqttReceiver")]
    public class MqttXML
    {
        public string ClientID { get; set; }
        public string ServerAddress { get; set; }
        public int Port { get; set; }
        /*
         * This is actually dangerous because the password is saved as cleartext inside the memory.
         * Do not use this class or interface for security critical applications
         */
        public string Password { get; set; } 
        public string Username { get; set; }
        [XmlArray("Topics")]
        [XmlArrayItem("Topic")]
        public List<string> Topics { get; set; } = new ();

        public bool UsesSameServer(MqttXML settings)
        {
            return settings.ClientID == ClientID &&
                   settings.ServerAddress == ServerAddress &&
                   settings.Port == Port &&
                   settings.Username == Username;
        }
    }

    [XmlRoot("MqttPublisher")]
    public class MqttPublisherXML : MqttXML
    {

    }
}