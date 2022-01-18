using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace libconnection.Interfaces.UDP
{
    public abstract class UdpTransceiver : DataStream
    {
        public IPEndPoint RemoteEndpoint { get; set; }

        public bool UseShortHeader { get; set; } = true;

        private Timer heartbeatTimer = new Timer(100);

        private Task<bool> receiverTask = null;
        private Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private CancellationTokenSource cts = new CancellationTokenSource();
        private bool useserverprotocoll;

        public UdpTransceiver(IPEndPoint localEndpoint, IPEndPoint remoteEndpoint = null, bool sendHeartbeat = false, bool useserverprotocoll = false)
        {
            this.useserverprotocoll = useserverprotocoll;
            RemoteEndpoint = remoteEndpoint;

            if (localEndpoint != null)
            {
                receiverTask = Task.Run(async () =>
                {
                    socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
                    socket.Bind(localEndpoint);
                    IPEndPoint anyendpoint = new IPEndPoint(IPAddress.Any, 0);
                    var token = cts.Token;
                    TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
                    token.Register(() =>
                    {
                        tcs.SetResult(true);
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
                                if (receiveFromResult.ReceivedBytes > 0)
                                {
                                    if (useserverprotocoll || sendHeartbeat)
                                    {
                                        Package data = Package.parse(buffer.Take(receiveFromResult.ReceivedBytes));
                                        if (data.type == Package.Type.DATAFRAME)
                                        {
                                            if (SupportsUpstream)
                                            {
                                                base.PublishUpstreamData(new Message(data.payload));
                                            }
                                            else
                                            {
                                                base.PublishDownstreamData(new Message(data.payload));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (SupportsUpstream)
                                        {
                                            base.PublishUpstreamData(new Message(buffer.Take(receiveFromResult.ReceivedBytes)));
                                        }
                                        else
                                        {
                                            base.PublishDownstreamData(new Message(buffer.Take(receiveFromResult.ReceivedBytes)));
                                        }
                                    }
                                }
                            }
                        }
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                });
            }

            if (sendHeartbeat)
            {
                heartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
                heartbeatTimer.AutoReset = true;
                heartbeatTimer.Enabled = true;
            }
        }

        public override void PublishDownstreamData(Message data)
        {
            if (cts == null)
            {
                throw new ObjectDisposedException(nameof(UdpReceiver));
            }
            if (SupportsDownstream)
            {
                base.PublishDownstreamData(data);
            }
            else
            {
                if (RemoteEndpoint != null)
                {
                    if (useserverprotocoll)
                    {
                        Package packeddata = new Package(Package.Type.DATAFRAME, DateTime.Now, data.Data)
                        {
                            UseShortHeader = UseShortHeader
                        };
                        socket.SendTo(packeddata.Serialize(), SocketFlags.None, RemoteEndpoint);
                    }
                    else
                    {
                        socket.SendTo(data.Data, SocketFlags.None, RemoteEndpoint);
                    }
                }
            }
        }

        public override void PublishUpstreamData(Message data)
        {
            if (cts == null)
            {
                throw new ObjectDisposedException(nameof(UdpReceiver));
            }
            if (SupportsUpstream)
            {
                base.PublishUpstreamData(data);
            }
            else
            {
                if (RemoteEndpoint != null)
                {
                    if (useserverprotocoll)
                    {
                        Package packeddata = new Package(Package.Type.DATAFRAME, DateTime.Now, data.Data)
                        {
                            UseShortHeader = UseShortHeader
                        };
                        socket.SendTo(packeddata.Serialize(), SocketFlags.None, RemoteEndpoint);
                    }
                    else
                    {
                        socket.SendTo(data.Data, SocketFlags.None, RemoteEndpoint);
                    }
                }
            }
        }

        private void HeartbeatTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (RemoteEndpoint != null)
            {
                Package packeddata = Package.CreateHeartbeat();
                socket.SendTo(packeddata.Serialize(), SocketFlags.None, RemoteEndpoint);
            }
        }

        public override void Dispose()
        {
            heartbeatTimer?.Stop();
            heartbeatTimer?.Dispose();
            heartbeatTimer = null;
            cts?.Cancel();
            receiverTask?.Wait(1000);
            cts?.Dispose();
            receiverTask?.Dispose();
            cts = null;
            receiverTask = null;
            base.Dispose();
        }
    }
}
