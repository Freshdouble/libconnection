using libconnection.Interfaces.UDP;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Timer = System.Timers.Timer;

namespace libconnection.Interfaces
{
    public class UDPClient : DataStream
    {
        public override bool SupportsDownstream { get; }

        public override bool SupportsUpstream { get; }
        private Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private IPEndPoint endpoint;
        private Task receiveTask = null;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private Heartbeat.HeartbeatManager manager = new Heartbeat.HeartbeatManager(1000);
        private Timer heartbeatTimer = new Timer(50);

        public UDPClient(IPEndPoint endpoint, bool registerAtServer, bool isDownstream)
        {
            this.endpoint = endpoint;
            if(registerAtServer)
            {
                heartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
                heartbeatTimer.AutoReset = true;
                heartbeatTimer.Enabled = true;
            }
            if(isDownstream)
            {
                SupportsUpstream = true;
                SupportsDownstream = false;
            }
            else
            {
                SupportsUpstream = false;
                SupportsDownstream = true;
            }
        }

        private void HeartbeatTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Package packeddata = Package.CreateHeartbeat();
            List<IPEndPoint> endpoints = manager.retrieve(false); // Only send heartbeat to the registered endpoints
            Task<int>[] sendTasks = new Task<int>[endpoints.Count];
            for (int i = 0; i < sendTasks.Length; i++)
            {
                sendTasks[i] = socket.SendToAsync(packeddata.Serialize(), SocketFlags.None, endpoints[i]);
            }
            Task.WaitAll(sendTasks, 1000, cts.Token);
        }

        public bool UseShortHeader { get; set; } = false;

        public override void StartService()
        {
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            socket.Bind(endpoint);
            IPEndPoint anyendpoint = new IPEndPoint(IPAddress.Any, 0);
            receiveTask = Task.Factory.StartNew(async () =>
            {
                CancellationToken token = cts.Token;
                TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
                token.Register(() =>
                {
                    tcs.SetException(new TaskCanceledException());
                });
                byte[] buffer = new byte[100];
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        var receiveTask = socket.ReceiveFromAsync(buffer, SocketFlags.None, anyendpoint);
                        if (await Task.WhenAny(receiveTask, tcs.Task) == receiveTask)
                        {
                            var receiveFromResult = receiveTask.Result;
                            if(receiveFromResult.ReceivedBytes > 0)
                            {
                                Package data = Package.parse(buffer.Take(receiveFromResult.ReceivedBytes));
                                manager.beat((IPEndPoint)receiveFromResult.RemoteEndPoint, DateTime.Now);
                                if(data.type == Package.Type.DATAFRAME)
                                {
                                    if(SupportsUpstream)
                                    {
                                        base.PublishUpstreamData(new Message(data.payload));
                                    }
                                    else
                                    {
                                        base.PublishDownstreamData(new Message(data.payload));
                                    }
                                }
                            }
                        }
                    }
                }
                catch(TaskCanceledException)
                {
                }
            }, TaskCreationOptions.LongRunning);
            base.StartService();
        }

        public override void PublishDownstreamData(Message data)
        {
            if(SupportsDownstream)
            {
                base.PublishDownstreamData(data);
            }
            else
            {
                Package packeddata = new Package(Package.Type.DATAFRAME, DateTime.Now, data.Data)
                {
                    UseShortHeader = UseShortHeader
                };
                List<IPEndPoint> endpoints = manager.retrieve();
                Task<int>[] sendTasks = new Task<int>[endpoints.Count];
                for (int i = 0; i < sendTasks.Length; i++)
                {
                    sendTasks[i] = socket.SendToAsync(packeddata.Serialize(), SocketFlags.None, endpoints[i]);
                }
                Task.WaitAll(sendTasks, 1000, cts.Token);
            }
        }

        public override void PublishUpstreamData(Message data)
        {
            if(SupportsUpstream)
            {
                base.PublishUpstreamData(data);
            }
            else
            {
                Package packeddata = new Package(Package.Type.DATAFRAME, DateTime.Now, data.Data)
                {
                    UseShortHeader = UseShortHeader
                };
                List<IPEndPoint> endpoints = manager.retrieve();
                Task<int>[] sendTasks = new Task<int>[endpoints.Count];
                for (int i = 0; i < sendTasks.Length; i++)
                {
                    sendTasks[i] = socket.SendToAsync(packeddata.Serialize(), SocketFlags.None, endpoints[i]);
                }
                Task.WaitAll(sendTasks, 1000, cts.Token);
            }
        }

        public override void Dispose()
        {
            cts.Cancel();
            heartbeatTimer.Stop();
            try
            {
                receiveTask?.Wait(2000);
            }
            catch(Exception)
            {
            }
            heartbeatTimer.Dispose();
            cts.Dispose();
            base.Dispose();
        }
    }
}
