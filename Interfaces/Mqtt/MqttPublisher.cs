using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;

namespace libconnection.Interfaces.Mqtt
{
    public class MqttPublisher : DataStream
    {
        public override bool SupportsDownstream => false;
        public override bool SupportsUpstream => true;

        private readonly SynchronizedQueue<Message> dataToTransmit = new();
        private Task workerTask = null;
        private readonly CancellationTokenSource cts = new();
        private readonly MqttServerConnection connection;
        private readonly MqttClientSubscribeOptions options;

        public MqttPublisher(MqttServerConnection connection, MqttXML xml)
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

            workerTask = Task.Run(async () =>
            {
                try
                {
                    var token = cts.Token;
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(Topic))
                            {
                                var msg = await dataToTransmit.DequeueAsync(cts.Token);
                                var applicationMessage = new MqttApplicationMessageBuilder()
                                    .WithTopic(Topic)
                                    .WithPayload(msg.Data)
                                    .Build();
                                await connection.Client.PublishAsync(applicationMessage, cts.Token);
                            }
                            else
                            {
                                await Task.Delay(100, cts.Token);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            
                        }
                    }
                }
                catch (Exception ex)
                {
                    PublishException(ex);
                }
            });
        }

        public string Topic { get; set; } = "";    
        
        public override void PublishDownstreamData(Message data)
        {
            dataToTransmit.Enqueue(data);
        }

        public override void Dispose()
        {
            base.Dispose();
            cts.Cancel();
            try
            {
                workerTask?.Wait(1000);
            }
            catch (Exception)
            {
            }
            cts.Dispose();
        }
    }
}