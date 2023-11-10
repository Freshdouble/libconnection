using libconnection.Interfaces.UDP;
using SocketCANSharp;
using SocketCANSharp.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace libconnection.Interfaces.CAN
{
    public class CAN : DataStream
    {
        public override bool IsInterface => true;
        public new int MTU { get; } = 8;

        public uint ReceiverID { get; set; } = 0;
        public int TransmitterID { get; set; } = -1;

        public bool AppendID { get; set; } = false;
        public bool ExtendedCAN { get; set; } = false;

        private readonly RawCanSocket socket = new RawCanSocket();
        private readonly Task thread;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        public static CAN GenerateWithParameters(IDictionary<string, string> parameter)
        {
            return new CAN()
            {
            };
        }

        public CAN()
        {
            if(Environment.OSVersion.Platform != PlatformID.Unix)
            {
                throw new SystemException("The can driver can only be used on systems, that support socketcan");
            }

            var vcan0 = CanNetworkInterface.GetAllInterfaces(true).First(iface => iface.Name.Equals("vcan0"));
            socket.Bind(vcan0);

            thread = Task.Run(async () =>
            {
                var token = cts.Token;
                while (!token.IsCancellationRequested)
                {
                    var frame = await CanReceiveAsync(token);
                    if(frame.Length > 0)
                    {
                        List<byte> list = new();
                        if(AppendID)
                        {
                            var id = frame.CanId & 0xFFFFFFF;
                            if (ExtendedCAN)
                            {
                                list.Add((byte)((id >> 24) & 0xFF));
                                list.Add((byte)((id >> 16) & 0xFF));
                            }
                            list.Add((byte)((id >> 8) & 0xFF));
                            list.Add((byte)((id >> 0) & 0xFF));
                        }
                        list.AddRange(frame.Data.Take(frame.Length));
                        base.ReceiveMessage(new Message(list));
                    }
                }
            });
        }

        private async Task<CanFrame> CanReceiveAsync(CancellationToken token)
        {
            var task = Task.Run(() =>
            {
                CanFrame frame;
                while (socket.Read(out frame) <= 0)
                {
                    if (token.IsCancellationRequested)
                    {
                        frame = new CanFrame()
                        {
                            Length = 0
                        };
                        break;
                    }
                }
                return frame;
            });
            return await task;
        }

        public override void TransmitMessage(Message message)
        {
            int transmitterID = TransmitterID;
            switch(message.CustomObject)
            {
                case int transmitID:
                    transmitterID = transmitID;
                    break;
                case string transmitIDString:
                    if(!int.TryParse(transmitIDString,out transmitterID))
                    {
                        transmitterID = TransmitterID;
                    }
                    break;
            }
            if(transmitterID >= 0)
            {
                base.TransmitMessage(message);
                socket.Write(new CanFrame((uint)transmitterID, message.Data));
            }
        }

        public override void Dispose()
        {
            try
            {
                base.Dispose();
                cts.Cancel();
                try
                {
                    thread.Wait(1000);
                }
                catch (AggregateException)
                {

                }
                cts.Dispose();
                socket.Dispose();
            }
            catch(Exception)
            {

            }
        }
    }
}
