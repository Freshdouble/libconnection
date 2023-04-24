using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Intrinsics.Arm;
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
        private UdpClient udp;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private bool useserverprotocoll;
        private IPEndPoint localEndpoint;
        private UdpClient sendclient;

        public static UdpTransceiver GenerateWithParameters(IDictionary<string, string> parameter)
        {
            bool sendheartbeat = false;

            IPEndPoint local = null;
            if (parameter.ContainsKey("localendpoint"))
            {
                if (!IPEndPoint.TryParse(parameter["localendpoint"], out local))
                {
                    throw new NotImplementedException("DNS resolve will be added later");
                }
            }

            IPEndPoint remote = null;
            if (parameter.ContainsKey("remoteendpoint"))
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

            if (parameter.ContainsKey("serverregistration"))
            {
                sendHeartbeat = useserverprotocol = parameter["serverregistration"].ToLower() == "true";
            }

            return new UdpTransceiver(local, remote, sendheartbeat, useserverprotocol);
        }

        public UdpTransceiver(IPEndPoint localEndpoint, IPEndPoint remoteEndpoint = null, bool sendHeartbeat = false, bool useserverprotocoll = false)
        {
            this.useserverprotocoll = useserverprotocoll;
            RemoteEndpoint = remoteEndpoint;
            this.localEndpoint = localEndpoint;
            if(!useserverprotocoll)
            {
                sendclient = new UdpClient();
            }
            if (localEndpoint != null)
            {
                udp = new UdpClient(7000);
                receiverTask = Task.Run(async () =>
                {
                    var token = cts.Token;
                    try
                    {
                        while (!token.IsCancellationRequested)
                        {
                            try
                            {
                                var result = await udp.ReceiveAsync(token);
                                if (useserverprotocoll || sendHeartbeat)
                                {
                                    Package data = Package.parse(result.Buffer);
                                    if (data.type == Package.Type.DATAFRAME)
                                    {
                                        base.ReceiveMessage(new Message(data.payload));
                                    }
                                }
                                else
                                {
                                    base.ReceiveMessage(new Message(result.Buffer));
                                }
                            }
                            catch(SocketException)
                            {
                                udp = new UdpClient(localEndpoint);
                            }
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        return false;
                    }
                });
            }
            else
            {
                udp = new UdpClient();
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
                    try
                    {
                        udp.Send(packeddata.Serialize(), RemoteEndpoint);
                    }
                    catch (SocketException)
                    {
                        udp = new UdpClient(localEndpoint);
                    }
                }
                else
                {
                    try
                    {
                        sendclient?.Send(message.Data, RemoteEndpoint);
                    }
                    catch (SocketException)
                    {
                        sendclient = new UdpClient();
                    }
                }
            }
        }

        private void HeartbeatTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (RemoteEndpoint != null)
            {
                Package packeddata = Package.CreateHeartbeat();
                udp.Send(packeddata.Serialize(), RemoteEndpoint);
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
            udp?.Dispose();
            sendclient?.Dispose();
            base.Dispose();
        }
    }
}
