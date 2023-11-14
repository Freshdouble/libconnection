using libconnection.Interfaces.UDP;
using SocketCANSharp;
using SocketCANSharp.Network;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        public int TransmitterID { get; set; } = -1;
        public bool ExtendedCAN { get; set; } = false;

        public uint Mask { get; }
        public uint ID { get; }

        public uint AppendIDFilterMask { get; set; } = 0;

        private readonly RawCanSocket socket = new RawCanSocket();
        private readonly Task thread;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        public static CAN GenerateWithParameters(IDictionary<string, string> parameter)
        {
            uint mask = 0;
            uint id = 0;
            uint appendfilter = 0;
            int transmitid = 0;

            if (parameter.ContainsKey("mask"))
            {
                mask = uint.Parse(parameter["mask"], NumberStyles.HexNumber);
            }
            if (parameter.ContainsKey("id"))
            {
                id = uint.Parse(parameter["id"], NumberStyles.HexNumber);
            }
            if (parameter.ContainsKey("appendidfilter"))
            {
                appendfilter = uint.Parse(parameter["appendidfilter"], NumberStyles.HexNumber);
            }
            if (parameter.ContainsKey("transmitid"))
            {
                transmitid = int.Parse(parameter["transmitid"], NumberStyles.HexNumber);
            }

            return new CAN(mask, id)
            {
                AppendIDFilterMask = appendfilter,
                TransmitterID = transmitid
            };
        }

        public CAN() : this(0, 0)
        {

        }

        public CAN(uint mask, uint id)
        {
            Mask = mask;
            ID = id;
            if (Environment.OSVersion.Platform != PlatformID.Unix)
            {
                throw new SystemException("The can driver can only be used on systems, that support socketcan");
            }

            var canInterfaces = CanNetworkInterface.GetAllInterfaces(false);
            var vcan0 = canInterfaces.First();

            CanFilter filter = new CanFilter();
            filter.CanMask = Mask;
            filter.CanId = ID;
            socket.CanFilters = new CanFilter[] { filter };
            socket.Bind(vcan0);

            var token = cts.Token;

            thread = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    var frame = await CanReceiveAsync(token);
                    if (frame.Length > 0)
                    {
                        List<byte> list = new();
                        if (AppendIDFilterMask != 0)
                        {
                            var id = (frame.CanId & AppendIDFilterMask) & 0xFFFFFFF;
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
            }, token);
        }

        private async Task<CanFrame> CanReceiveAsync(CancellationToken token)
        {
            var task = Task.Run(() =>
            {
                CanFrame frame;
                while (socket.Read(out frame) <= 0)
                {
                    token.ThrowIfCancellationRequested();
                }
                return frame;
            }, token);
            return await task;
        }

        public override void TransmitMessage(Message message)
        {
            int transmitterID = TransmitterID;
            switch (message.CustomObject)
            {
                case int transmitID:
                    transmitterID = transmitID;
                    break;
                case string transmitIDString:
                    if (!int.TryParse(transmitIDString, out transmitterID))
                    {
                        transmitterID = TransmitterID;
                    }
                    break;
            }
            if (transmitterID >= 0)
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
            catch (Exception)
            {

            }
        }
    }
}
