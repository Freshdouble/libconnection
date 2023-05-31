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
        public event EventHandler<ExceptionEventArgs> ExceptionReceived;
        public event EventHandler<EventArgs> BrokenPipe;
        protected bool disposed = false;

        public DataStream()
        {
        }

        public abstract bool IsInterface { get; }

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
            if(IsInterface)
            {
                throw new InvalidOperationException("Cannot add a transmitter stage to a transmitter");
            }
            if (!overwrite && transmitter != null)
            {
                throw new ArgumentException("Stage already has a upperstage");
            }
            transmitter = stage;
        }

        public bool PipeIsBroken { get; protected set; } = false;
        public List<Exception> ThrownExceptions { get; } = new ();

        public virtual void TransmitMessage(Message message)
        {
            if(PipeIsBroken)
            {
                throw new InvalidOperationException("Cannot write to a broken pipe");
            }

            try
            {
                transmitter?.TransmitMessage(message);
            }
            catch(Exception ex)
            {
                ThrowCriticalException(ex);
            }
        }

        protected virtual void ReceiveMessage(Message message)
        {
            if (PipeIsBroken)
            {
                throw new InvalidOperationException("Cannot write to a broken pipe");
            }
            try
            {
                receiver?.ReceiveMessage(message);
                MessageReceived?.Invoke(this, new MessageEventArgs()
                {
                    Message = message
                });
            }
            catch (Exception ex)
            {
                ThrowCriticalException(ex);
            }
        }

        protected void ThrowUncriticalExcpeption(Exception ex)
        {
            ThrownExceptions.Add(ex);
            ExceptionReceived?.Invoke(this, new ExceptionEventArgs
            {
                Exception = new AggregateException(ThrownExceptions)
            });
        }

        protected void ThrowCriticalException(Exception ex)
        {
            PipeIsBroken = true;
            ThrowUncriticalExcpeption(ex);
            BrokenPipe?.Invoke(this, new EventArgs());
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
