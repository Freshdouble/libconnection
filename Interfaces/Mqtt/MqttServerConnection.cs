using System;
using System.Collections.Generic;
using System.Threading;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;

namespace libconnection.Interfaces.Mqtt
{
    public class MqttServerConnection : IDisposable
    {
        private readonly MqttXML xml;
        private readonly MqttClient client;
        private readonly MqttClientOptions options;
        private readonly MqttFactory factory;
        private readonly CancellationTokenSource cts = new();

        public MqttServerConnection(MqttXML xml)
        {
            this.xml = xml;
            factory = new MqttFactory();
            client = factory.CreateMqttClient();
            options = new MqttClientOptionsBuilder()
                .WithClientId(xml.ClientID)
                .WithTcpServer(xml.ServerAddress, xml.Port)
                .WithCredentials(xml.Username, xml.Password)
                .WithProtocolVersion(MqttProtocolVersion.V500)
                .WithTls()
                .WithCleanSession()
                .Build();
            client.DisconnectedAsync += async(args) =>
            {
                try
                {
                    if (args.ClientWasConnected)
                    {
                        await client.ConnectAsync(options, cts.Token);
                    }

                    ClientDisconnected?.Invoke(this, args);
                }
                catch (OperationCanceledException)
                {
                }
            };
        }

        public event EventHandler<MqttClientDisconnectedEventArgs> ClientDisconnected; 

        public void Connect()
        {
            if (!client.IsConnected)
            {
                client.ConnectAsync(options, cts.Token).Wait();
            }
        }

        public bool UsesServerConnection(MqttXML xml)
        {
            return this.xml.UsesSameServer(xml);
        }
        
        public MqttClient Client => client;
        public MqttFactory Factory => factory;

        public void Dispose()
        {
            client?.Dispose();
            cts?.Cancel();
            cts?.Dispose();
        }
    }
}