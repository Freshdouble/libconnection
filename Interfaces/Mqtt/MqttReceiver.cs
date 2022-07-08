using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;

namespace libconnection.Interfaces.Mqtt
{
    public class MqttReceiver : DataStream
    {
        public override bool SupportsDownstream => true;
        public override bool SupportsUpstream => false;

        private readonly MqttServerConnection connection;
        private readonly MqttClientSubscribeOptions options;

        public MqttReceiver(MqttServerConnection connection, MqttXML xml)
        {
            this.connection = connection;
            
            var mqttSubscribeOptions = connection.Factory.CreateSubscribeOptionsBuilder();
            foreach (var topic in xml.Topics)
            {
                mqttSubscribeOptions = mqttSubscribeOptions.WithTopicFilter(topic);
            }
            options = mqttSubscribeOptions.Build();
        }

        public override void StartService()
        {
            base.StartService();

            if (!connection.Client.IsConnected)
            {
                connection.Connect();
            }
            connection.Client.ApplicationMessageReceivedAsync += e =>
            {
                var msg = new MqttMessage(e.ApplicationMessage.Payload)
                {
                    Topic = e.ApplicationMessage.Topic
                };
                PublishUpstreamData(msg);
                return Task.CompletedTask;
            };
            connection.ClientDisconnected += async (sender, args) =>
            {
                if (args.ClientWasConnected)
                {
                    await connection.Client.SubscribeAsync(options);
                }
            };
            connection.Client.SubscribeAsync(options).Wait();
        }
    }
}