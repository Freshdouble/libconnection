using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace libconnection.Decoders
{

    /********************************************************
     * Dataformat
     *  Header Byte
     *      2MSBs TransmitterState
     *          0 -> First subpacket of message
     *          1 -> Consecutive subpacket of message
     *          2 -> Last subpacket of message. If the message consists of a single subpacket this is the used state and the packet counter is 0
     *      1Bit Control Messagebit
     *          0 -> Packet is a payload packet
     *          1 -> Packet is a stream control packet eg ACK or NACK
     *      Last Bits Packet counter gets increment with each new message so other end can decide if it already received that message
     *      
     *  Payload
     *  
     *  Checksum at the end of last packet
     * ******************************************************/
    public class SecTransmit : DataStream, IStartable
    {
        private ConcurrentQueue<Message> queue = new ConcurrentQueue<Message>();
        private CancellationTokenSource cts = new CancellationTokenSource();
        private Task workingTask;
        private bool shouldRun = true;
        private volatile TaskCompletionSource<bool> tcs = null;
        private int bytesperPacket = 256;
        public override bool SupportsDownstream => true;

        public override bool SupportsUpstream => true;

        private int receivepacketcounter = -1;
        private List<byte> receiveBuffer = new List<byte>();

        private SemaphoreSlim messageSemaphore = new SemaphoreSlim(0);

        public int MaxBytesPerPacket
        {
            get
            {
                return bytesperPacket + 2;
            }
            set
            {
                bytesperPacket = value - 2;
            }
        }

        public int RetryCounter { get; set; } = 10;

        public SecTransmit()
        {
        }

        private Message PrepareMessage(Message msg)
        {
            //byte checksum = CalcChecksum(msg);
            //msg.PushEnd(checksum);
            return msg;
        }

        private async Task<bool> SendMessage(Message msg)
        {
            Task<bool> ackTask = WaitForACKOrNACK();
            base.PublishDownstreamData(msg);
            return await ackTask;
        }

        public int ACKTimeout { get; set; } = 100;

        private async Task<bool> WaitForACKOrNACK()
        {
            tcs = new TaskCompletionSource<bool>();
            var task = tcs.Task;
            bool ret = false;
            if (await Task.WhenAny(task, Task.Delay(ACKTimeout)) == task)
            {
                ret = task.Result;
            }
            tcs = null;
            return ret;
        }

        public override void PublishDownstreamData(Message data)
        {
            queue.Enqueue(data);
            messageSemaphore.Release();
        }

        public override void PublishUpstreamData(Message data)
        {
            SecTransmitHeader header = new SecTransmitHeader(data.PopFirst());
            if (header.IsControlMessage) //Check if this is a controlmessage
            {
                ControlMessageType ctlmsg = (ControlMessageType)data.PopFirst();
                switch (ctlmsg)
                {
                    case ControlMessageType.ACK:
                        tcs?.SetResult(true);
                        break;
                    case ControlMessageType.NACK:
                        tcs?.SetResult(false);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                bool sendAck = true;
                if (header.PacketCount != receivepacketcounter)
                {
                    if (header.State == PacketState.FirstTransmission)
                    {
                        receiveBuffer.Clear();
                    }
                    receivepacketcounter = header.PacketCount; //Update local packet counter to not receive the packet twice
                    if(header.State == PacketState.LastPacket)
                    {
                        byte checksum = data.PopLast();
                        receiveBuffer.AddRange(data);
                        if (checksum == CalcChecksum(receiveBuffer))
                        {
                            base.PublishUpstreamData(new Message(receiveBuffer));
                        }
                        else
                        {
                            if(receivepacketcounter == 0)
                            {
                                //This was the only packet of the message so we can send a NACK
                                sendAck = false;
                            }
                        }
                        receivepacketcounter = -1;
                        receiveBuffer.Clear();
                    }
                    else
                    {
                        receiveBuffer.AddRange(data);
                    }
                }
                if (sendAck)
                {
                    SecTransmitHeader transmitHeader = new SecTransmitHeader()
                    {
                        IsControlMessage = true
                    };
                    byte[] msg = new byte[] { transmitHeader, (byte)ControlMessageType.ACK };
                    Message ACKMessage = new Message(msg);
                    base.PublishDownstreamData(ACKMessage);
                }
            }
        }

        private async Task SendAndRetry(Message msg)
        {
            bool ack;
            int retry = RetryCounter + 1;
            do
            {
                if (retry <= 0)
                {
                    throw new Exception("Couldn't send message");
                }
                retry--;
                ack = await SendMessage(msg.Copy()).ConfigureAwait(false);
            } while (!ack);
        }

        public override void StartService()
        {
            CancellationToken token = cts.Token;
            workingTask = Task.Factory.StartNew(async () =>
            {
                SecTransmitHeader header = new SecTransmitHeader()
                {
                    State = PacketState.FirstTransmission,
                    IsControlMessage = false,
                    PacketCount = 0
                };
                Random rand = new Random();
                while (!token.IsCancellationRequested && shouldRun)
                {
                    Message msg;
                    await messageSemaphore.WaitAsync();
                    if (queue.TryDequeue(out msg))
                    {
                        try
                        {
                            msg.PushEnd(CalcChecksum(msg));
                            header.State = PacketState.FirstTransmission;
                            header.PacketCount = rand.Next(0, 31);
                            if (msg.Length > bytesperPacket)
                            {
                                do
                                {
                                    Message newMsg = new Message(msg.PopAndRemoveFirstMultiple(bytesperPacket))
                                    {
                                        Port = msg.Port
                                    };
                                    if (msg.Length == 0)
                                    {
                                        header.State = PacketState.LastPacket;
                                    }
                                    newMsg.PushFront(header);
                                    await SendAndRetry(newMsg).ConfigureAwait(false);
                                    if (header.State == PacketState.FirstTransmission)
                                    {
                                        header.State = PacketState.ConsecutiveTransmission;
                                    }
                                    header.PacketCount++;
                                } while (msg.Length > 0);
                            }
                            else
                            {
                                header.State = PacketState.LastPacket;
                                msg.PushFront(header);
                                PrepareMessage(msg);
                                await SendAndRetry(msg).ConfigureAwait(false);
                            }
                        }
                        catch (AggregateException ex)
                        {
                            foreach (var e in ex.InnerExceptions)
                            {
                                if (e is OperationCanceledException)
                                {
                                    throw e;
                                }
                            }
                            //We have some other exceptions that we might want to handle upstream
                            PublishException(ex);
                        }
                        catch (Exception ex)
                        {
                            PublishException(ex);
                        }
                    }
                }
            }, TaskCreationOptions.LongRunning);
            base.StartService();
        }

        public override void Dispose()
        {
            cts.Cancel();
            base.Dispose();
            try
            {
                workingTask?.Wait(1000);
            }
            catch (Exception)
            {

            }
            cts.Dispose();
        }

        public static byte CalcChecksum(IEnumerable<byte> data)
        {
            byte sum = 0;
            foreach (byte b in data)
            {
                sum += b;
            }
            return (byte)(sum ^ 0x55);
        }
    }
}
