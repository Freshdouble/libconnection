using libconnection.Interfaces.UDP;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Linq;
using Heartbeat;

namespace libconnection.Interfaces
{
    public class UDPServer : DataStream
    {
        public override bool SupportsDownstream => true;

        public override bool SupportsUpstream => false;

        private Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private EndPoint localEndpoint;
        private Task receiveTask = null;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private Timer heartbeatTimer = new Timer(50);

        private HeartbeatManager heartbeatmanager = new HeartbeatManager(1000);

        public UDPServer(IPEndPoint endpoint, bool sendheartbeat)
        {
            localEndpoint = endpoint;
            if (sendheartbeat)
            {
                heartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
                heartbeatTimer.AutoReset = true;
                heartbeatTimer.Enabled = true;
            }
        }

        public UDPServer(IPEndPoint endpoint, IEnumerable<IPEndPoint> staticEndpoints, bool sendheartbeat) : this(endpoint, sendheartbeat)
        {
            foreach(var staticendpoint in staticEndpoints)
            {
                AddStaticEndpoint(staticendpoint);
            }
        }

        public void AddStaticEndpoint(IPEndPoint endpoint)
        {
            heartbeatManager.AddStaticEndpoint(endpoint);
        }

        public bool UseShortHeader { get; set; } = false;

        public void AddStaticEndpoint(IPEndPoint endPoint)
        {
            heartbeatmanager.AddStaticEndpoint(endPoint);
        }

        private void HeartbeatTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Package package = Package.CreateHeartbeat();
            package.UseShortHeader = UseShortHeader;
            List<IPEndPoint> endpoints = heartbeatmanager.retrieve(false);
            foreach(var endpoint in endpoints)
            {
                socket.SendTo(package.Serialize(), endpoint);
            }
        }

        private void SendToAll(Package package)
        {
            SendToAll(package.Serialize());
        }

        private void SendToAll(byte[] data)
        {
            List<IPEndPoint> endpoints = heartbeatManager.retrieve();
            foreach(var endpoint in endpoints)
            {
                socket.SendTo(data, endpoint);
            }
        }

        public override void StartService()
        {
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            socket.Bind(localEndpoint);
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
                        var receiveTask = socket.ReceiveFromAsync(buffer, SocketFlags.None, localEndpoint);
                        if (await Task.WhenAny(receiveTask, tcs.Task) == receiveTask)
                        {
                            var receiveFromResult = receiveTask.Result;
                            if(receiveFromResult.RemoteEndPoint is IPEndPoint ipendpoint)
                            {
                                heartbeatManager.beat(ipendpoint, DateTime.Now);
                            }
                            if (receiveFromResult.ReceivedBytes > 0)
                            {
                                Package data = Package.parse(buffer.Take(receiveFromResult.ReceivedBytes));
                                if(receiveFromResult.RemoteEndPoint is IPEndPoint ipendpoint)
                                {
                                    heartbeatmanager.beat(ipendpoint, DateTime.Now);
                                }
                                if (data.type == Package.Type.DATAFRAME)
                                {
                                    PublishDownstreamData(new Message(data.payload));
                                }
                            }
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                }
            }, TaskCreationOptions.LongRunning);
            base.StartService();
        }

        private void SendToAll(Package package)
        {
            SendToAll(package.Serialize());
        }

        private void SendToAll(byte[] data)
        {
            List<IPEndPoint> endpoints = heartbeatmanager.retrieve(true);
            foreach(var endpoint in endpoints)
            {
                socket.SendTo(data, endpoint);
            }
        }

        public override void PublishUpstreamData(Message data)
        {
            Package package = new Package(Package.Type.DATAFRAME, DateTime.Now, data.Data)
            {
                UseShortHeader = UseShortHeader
            };
            SendToAll(package);
        }
    }
}
