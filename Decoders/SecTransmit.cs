using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace libconnection.Decoders
{
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

        public SecTransmit()
        {
        }

        private Message PrepareMessage(Message msg)
        {
            byte checksum = CalcChecksum(msg);
            msg.PushEnd(checksum);
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
            if(await Task.WhenAny(task, Task.Delay(ACKTimeout)) == task)
            {
                ret = task.Result;
            }
            tcs = null;
            return ret;
        }

        public override void PublishDownstreamData(Message data)
        {
            queue.Enqueue(data);
        }

        public override void PublishUpstreamData(Message data)
        {
            byte checksum = data.PopLast();
            bool correctChecksum = checksum == CalcChecksum(data);
            if (correctChecksum)
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
                    SecTransmitHeader transmitHeader = new SecTransmitHeader()
                    {
                        IsControlMessage = true
                    };
                    byte[] msg = new byte[] { transmitHeader, (byte)ControlMessageType.ACK };
                    Message ACKMessage = new Message(msg);
                    base.PublishDownstreamData(ACKMessage);
                    base.PublishUpstreamData(data);
                }
            }
            else
            {
                SecTransmitHeader header = new SecTransmitHeader()
                {
                    IsControlMessage = true
                };
                byte[] msg = new byte[] { header, (byte)ControlMessageType.NACK };
                Message NACKMessage = new Message(msg);
                base.PublishDownstreamData(NACKMessage);
            }
        }

        private async Task SendAndRetry(Message msg)
        {
            bool ack;
            do
            {
                ack = await SendMessage(new Message(msg)).ConfigureAwait(false);
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
                    IsControlMessage = false
                };
                while (!token.IsCancellationRequested && shouldRun)
                {
                    Message msg;
                    if (queue.TryDequeue(out msg))
                    {
                        try
                        {
                            header.State = PacketState.FirstTransmission;
                            header.PacketCount = 0;
                            if (msg.Length > bytesperPacket)
                            {
                                do
                                {
                                    Message newMsg = new Message(msg.PopAndRemoveFirstMultiple(bytesperPacket));
                                    if (msg.Length == 0 && header.State == PacketState.ConsecutiveTransmission)
                                    {
                                        header.State = PacketState.LastPacket;
                                    }
                                    newMsg.PushFront(header);
                                    PrepareMessage(newMsg); //Add Checksum to message
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
                    }
                    else
                    {
                        await Task.Yield();
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
            catch(Exception)
            {

            }
            cts.Dispose();
        }

        public static byte CalcChecksum(IEnumerable<byte> data)
        {
            byte sum = 0;
            foreach(byte b in data)
            {
                sum += b;
            }
            return (byte)(sum ^ 0x55);
        }
    }
}
