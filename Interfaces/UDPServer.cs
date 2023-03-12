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

        private Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private EndPoint localEndpoint;
        private Task receiveTask = null;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private Timer heartbeatTimer = new Timer(50);

        private HeartbeatManager heartbeatmanager = new HeartbeatManager(1000);
        private bool sendheartbeat;

        public static UDPServer GenerateClassFromString(string[] parameter)
        {
            bool sendheartbeat = false;
            if(parameter.Length < 2)
            {
                throw new ArgumentException("A valid endpoint must have an ip address and port");
            }

            if(parameter.Length >= 3)
            {
                sendheartbeat = parameter[0].ToLower() == "true";
            }

            return new UDPServer(new IPEndPoint(IPAddress.Parse(parameter[0]), int.Parse(parameter[1])), sendheartbeat);
        }

        public UDPServer(IPEndPoint endpoint, bool sendheartbeat)
        {
            this.sendheartbeat = sendheartbeat;
            localEndpoint = endpoint;
            if (sendheartbeat)
            {
                heartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
                heartbeatTimer.AutoReset = true;
                heartbeatTimer.Enabled = true;
            }

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
                            if (receiveFromResult.ReceivedBytes > 0)
                            {
                                Package data = Package.parse(buffer.Take(receiveFromResult.ReceivedBytes));
                                heartbeatmanager.beat((IPEndPoint)receiveFromResult.RemoteEndPoint, DateTime.Now);
                                if (data.type == Package.Type.DATAFRAME)
                                {
                                    base.ReceiveMessage(new Message(data.payload));
                                }
                            }
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                }
            }, TaskCreationOptions.LongRunning);
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
            heartbeatmanager.AddStaticEndpoint(endpoint);
        }

        public bool UseShortHeader { get; set; } = false;

        public override bool IsInterface => true;

        private void SocketSend(ref Package package, IPEndPoint endPoint)
        {
            SocketSend(package.Serialize(), endPoint);
        }

        private void SocketSend(byte[] data, IPEndPoint endPoint)
        {
            try
            {
                socket.SendTo(data, endPoint);
            }
            catch (SocketException)
            {

            }
            catch (Exception)
            {
                throw;
            }
        }

        private void HeartbeatTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Package package = Package.CreateHeartbeat();
            package.UseShortHeader = UseShortHeader;
            List<IPEndPoint> endpoints = heartbeatmanager.retrieve(false);
            byte[] data = package.Serialize();
            foreach(var endpoint in endpoints)
            {
                SocketSend(data, endpoint);
            }
        }

        private void SendToAll(Package package)
        {
            SendToAll(package.Serialize());
        }

        private void SendToAll(byte[] data)
        {
            List<IPEndPoint> endpoints = heartbeatmanager.retrieve();
            foreach (var endpoint in endpoints)
            {
                SocketSend(data, endpoint);
            }
        }

        public override void TransmitMessage(Message message)
        {
            base.TransmitMessage(message);
            Package package = new Package(Package.Type.DATAFRAME, DateTime.Now, message.Data)
            {
                UseShortHeader = UseShortHeader
            };
            SendToAll(package);
        }
    }
}
