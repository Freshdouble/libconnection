using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using libconnection.Interfaces.UDP;
using MQTTnet;
using MQTTnet.Client;

namespace libconnection.Interfaces.MQTT
{
    class MqttTransmitter : DataStream
    {
        private readonly IMqttClient mqttClient;
        public override bool IsInterface => true;

        public string Topic { get; set; } = string.Empty;

        public string TopicPrefix { get; set; } = string.Empty;

        private CancellationToken token = CancellationToken.None;

        public static MqttTransmitter GenerateWithParameters(IDictionary<string, string> parameter)
        {
            string topic = string.Empty;
            string topicprefix = string.Empty;
            if (parameter.ContainsKey("topic"))
            {
                topic = parameter["topic"];
            }

            if(parameter.ContainsKey("prefix"))
            {
                topicprefix = parameter["prefix"];
            }

            return new MqttTransmitter(parameter["broker"], parameter["id"], topic)
            {
                TopicPrefix = topicprefix
            };
        }


        private readonly string broker;
        private readonly string id;
        MqttTransmitter(string broker, string id, string topic) : this()
        {
            Topic = topic;
            this.broker = broker;
            this.id = id;
        }

        MqttTransmitter()
        {
            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();
        }

        public override async Task StartStream(CancellationToken token)
        {
            this.token = token;
            mqttClient.DisconnectedAsync += async (args) =>
            {
                if (args.ClientWasConnected)
                {
                    await mqttClient.ReconnectAsync(token);
                }
            };
            await Task.Run(async () => {
                while(!mqttClient.IsConnected)
                {
                    await ConnectAsync(broker, id, token);
                    await Task.Delay(10);
                }
            });
        }

        public async Task ConnectAsync(string broker, string clientID, CancellationToken token)
        {
            var options = new MqttClientOptionsBuilder()
            .WithTcpServer(broker)
            .WithClientId(clientID)
            .Build();

            await mqttClient.ConnectAsync(options, token);
        }

        public async Task SendData(byte[] data, string topic, CancellationToken token)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(data)
                .Build();

            if (mqttClient.IsConnected)
            {
                await mqttClient.PublishAsync(message, token);
            }
        }

        public override void TransmitMessage(Message message)
        {
            base.TransmitMessage(message);
            if (message.CustomObject != null && message.CustomObject is string str)
            {
                string topic;
                string prefix = string.Empty;
                if (str.StartsWith("/"))
                {
                    topic = Topic + str;
                }
                else
                {
                    topic = Topic + "/" + str;
                }
                if(!Topic.StartsWith("/"))
                {
                    prefix += "/";
                }
                prefix += TopicPrefix;
                if(prefix.EndsWith("/"))
                {
                    prefix = prefix.Remove(prefix.Length - 1, 1);
                }
                topic += prefix;
                SendData(message.Data, topic, token).Wait();
            }
            else if (!string.IsNullOrWhiteSpace(Topic))
            {
                SendData(message.Data, Topic, token).Wait();
            }
        }
    }
}
