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

namespace libconnection.Interfaces
{
    public class UDPServer : DataStream
    {
        public override bool SupportsDownstream => true;

        public override bool SupportsUpstream => false;

        private Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private IPEndPoint endpoint;
        private Task receiveTask = null;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private Timer heartbeatTimer = new Timer(50);

        public UDPServer(IPEndPoint endpoint, bool sendheartbeat)
        {
            this.endpoint = endpoint;
            if(sendheartbeat)
            {
                heartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
                heartbeatTimer.AutoReset = true;
                heartbeatTimer.Enabled = true;
            }
        }

        public bool UseShortHeader { get; set; } = false;

        private void HeartbeatTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Package package = Package.CreateHeartbeat();
            package.UseShortHeader = UseShortHeader;
            socket.SendTo(package.Serialize(), endpoint);
        }

        public override void StartService()
        {
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            socket.Bind(endpoint);
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
                        var receiveTask = socket.ReceiveFromAsync(buffer, SocketFlags.None, endpoint);
                        if (await Task.WhenAny(receiveTask, tcs.Task) == receiveTask)
                        {
                            var receiveFromResult = receiveTask.Result;
                            if (receiveFromResult.ReceivedBytes > 0)
                            {
                                Package data = Package.parse(buffer.Take(receiveFromResult.ReceivedBytes));
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

        public override void PublishUpstreamData(Message data)
        {
            Package package = new Package(Package.Type.DATAFRAME, DateTime.Now, data.Data)
            {
                UseShortHeader = UseShortHeader
            };
            socket.SendTo(package.Serialize(), endpoint);
        }
    }
}
