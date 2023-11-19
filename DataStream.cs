using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace libconnection
{
    public class MessageEventArgs : EventArgs
    {
        public Message Message { get; set; }
    }

    public class ExceptionEventArgs : EventArgs
    {
        public AggregateException Exception { get; set; }
    }

    public abstract class DataStream : IDisposable
    {
        protected DataStream receiver;
        protected DataStream transmitter;
        public event EventHandler<MessageEventArgs> MessageReceived;
        protected bool disposed = false;

        public DataStream()
        {
        }

        public abstract bool IsInterface { get; }
        public virtual int MTU { get; } = 0;

        public async Task StartStream()
        {
            await StartStream(CancellationToken.None);
        }

        public virtual async Task StartStream(CancellationToken token)
        {
            await transmitter?.StartStream(token);
        }

        protected int GetTransmitterMTU()
        {
            if(transmitter != null)
            {
                return transmitter.MTU;
            }
            return 0;
        }

        public virtual void AddReceiverStage(DataStream stage, bool overwrite = false)
        {
            if(!overwrite && receiver != null)
            {
                throw new ArgumentException("Stage already has a upperstage");
            }
            receiver = stage;
        }

        public virtual void AddTransmitterStage(DataStream stage, bool overwrite = false)
        {
            if (IsInterface)
            {
                throw new InvalidOperationException("Cannot add a transmitter stage to a transmitter");
            }
            if (!overwrite && transmitter != null)
            {
                throw new ArgumentException("Stage already has a upperstage");
            }
            transmitter = stage;
        }

        public virtual void TransmitMessage(Message message)
        {
                if(transmitter!= null)
                {
                    if(transmitter.MTU > 0)
                    {
                        if(message.Length > transmitter.MTU)
                        {
                            Console.WriteLine($"WARNING: Message length exeeds MTU in {nameof(transmitter)}");
                        }
                        else
                        {
                            transmitter.TransmitMessage(message);
                        }
                    }
                    else
                    {
                        transmitter.TransmitMessage(message);
                    }
                }
        }

        protected virtual void ReceiveMessage(Message message)
        {
                receiver?.ReceiveMessage(message);
                MessageReceived?.Invoke(this, new MessageEventArgs()
                {
                    Message = message
                });
        }

        public virtual void Dispose()
        {
            if(!disposed && transmitter != null)
            {
                receiver?.Dispose();
                transmitter?.Dispose();
                disposed = true;
            }
        }
    }
}
