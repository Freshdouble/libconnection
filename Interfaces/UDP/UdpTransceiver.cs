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
    public class UdpTransceiver : DataStream
    {
        public IPEndPoint RemoteEndpoint { get; set; }

        public bool UseShortHeader { get; set; } = true;

        public override bool IsInterface => true;

        private Timer heartbeatTimer = new Timer(100);

        private Task<bool> receiverTask = null;
        private Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private CancellationTokenSource cts = new CancellationTokenSource();
        private bool useserverprotocoll;

        public static UdpTransceiver GenerateWithParameters(IDictionary<string, string> parameter)
        {
            bool sendheartbeat = false;

            IPEndPoint local = null;
            if(parameter.ContainsKey("localendpoint"))
            {
                if (!IPEndPoint.TryParse(parameter["localendpoint"], out local))
                {
                    throw new NotImplementedException("DNS resolve will be added later");
                }
            }

            IPEndPoint remote = null;
            if(parameter.ContainsKey("remoteendpoint"))
            {
                if (!IPEndPoint.TryParse(parameter["remoteendpoint"], out remote))
                {
                    throw new NotImplementedException("DNS resolve will be added later");
                }
            }
            else
            {
                throw new ArgumentException("The udptransceiver must have a remote endpoint");
            }

            bool sendHeartbeat = false;
            bool useserverprotocol = false;

            if(parameter.ContainsKey("serverregistration"))
            {
                sendHeartbeat = useserverprotocol = parameter["serverregistration"].ToLower() == "true";
            }

            return new UdpTransceiver(local, remote, sendheartbeat, useserverprotocol);
        }

        public UdpTransceiver(IPEndPoint localEndpoint, IPEndPoint remoteEndpoint = null, bool sendHeartbeat = false, bool useserverprotocoll = false)
        {
            this.useserverprotocoll = useserverprotocoll;
            RemoteEndpoint = remoteEndpoint;

            if (localEndpoint != null)
            {
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
                socket.Bind(localEndpoint);
                receiverTask = Task.Run(async () =>
                {
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
                                            base.ReceiveMessage(new Message(data.payload));
                                        }
                                    }
                                    else
                                    {
                                        base.ReceiveMessage(new Message(buffer.Take(receiveFromResult.ReceivedBytes)));
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

        public override void TransmitMessage(Message message)
        {
            base.TransmitMessage(message);
            if (cts == null)
            {
                throw new ObjectDisposedException("UDPTransceiver is disposed");
            }
            if (RemoteEndpoint != null)
            {
                if (useserverprotocoll)
                {
                    Package packeddata = new Package(Package.Type.DATAFRAME, DateTime.Now, message.Data)
                    {
                        UseShortHeader = UseShortHeader
                    };
                    socket.SendTo(packeddata.Serialize(), SocketFlags.None, RemoteEndpoint);
                }
                else
                {
                    socket.SendTo(message.Data, SocketFlags.None, RemoteEndpoint);
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
