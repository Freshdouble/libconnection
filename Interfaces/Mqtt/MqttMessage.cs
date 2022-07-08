using System.Collections.Generic;

namespace libconnection.Interfaces.Mqtt
{
    public class MqttMessage : Message
    {
        public MqttMessage(IEnumerable<byte> data) : base(data)
        {
        }

        public string Topic { get; set; } = "";
    }
}