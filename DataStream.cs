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

    public abstract class DataStream : IDisposable, IStartable
    {
        private LinkedList<DataStream> uplink = new LinkedList<DataStream>();
        private LinkedList<DataStream> downlink = new LinkedList<DataStream>();
        private TaskCompletionSource<Message> tcs = null;
        public event EventHandler<MessageEventArgs> MessageReceived;
        public DataStream()
        {
        }

        public abstract bool SupportsDownstream { get; }
        public abstract bool SupportsUpstream { get; }

        public int FilterPort { get; set; } = -1;

        private IEnumerable<DataStream> GetUplinks()
        {
            lock (uplink)
            {
                foreach (var link in uplink)
                {
                    yield return link;
                }
            }
        }

        private IEnumerable<DataStream> GetDownlinks()
        {
            lock (downlink)
            {
                foreach (var link in downlink)
                {
                    yield return link;
                }
            }
        }

        public virtual void LinkUpstream(DataStream stream)
        {
            if (!SupportsUpstream)
            {
                throw new InvalidOperationException("Cannot link a upstream interface to " + GetType().Name);
            }
            lock (uplink)
            {
                uplink.AddLast(stream);
            }
            if (stream.SupportsDownstream)
            {
                stream.LinkDownstream(this);
            }
        }

        public void UnlinkUpstream(DataStream stream)
        {
            lock (uplink)
            {
                uplink.Remove(stream);
            }
            stream.UnlinkDownstream(this);
        }

        protected void LinkDownstream(DataStream stream)
        {
            if (!SupportsDownstream)
            {
                throw new InvalidOperationException("Cannot link a downstream interface to " + GetType().Name);
            }
            lock (uplink)
            {
                downlink.AddLast(stream);
            }
        }

        protected void UnlinkDownstream(DataStream stream)
        {
            if (SupportsDownstream)
            {
                lock (uplink)
                {
                    downlink.Remove(stream);
                }
            }
        }

        private void PushDataUpstream(Message data)
        {
            if (FilterPort >= 0)
            {
                if (FilterPort == data.Port)
                {
                    PublishUpstreamData(data);
                }
            }
            else
            {
                PublishUpstreamData(data);
            }
        }

        public virtual void PublishUpstreamData(Message data)
        {
            if (SupportsUpstream)
            {
                var upstreamlinks = GetUplinks();
                foreach (var stream in upstreamlinks)
                {
                    stream.PushDataUpstream(data);
                }
            }
            MessageReceived?.Invoke(this, new MessageEventArgs()
            {
                Message = data
            });
        }

        public virtual void PublishDownstreamData(Message data)
        {
            if (SupportsDownstream)
            {
                if (FilterPort >= 0)
                {
                    data.Port = FilterPort;
                }
                var downlinks = GetDownlinks();
                foreach (var stream in downlinks)
                {
                    stream.PublishDownstreamData(data);
                }
            }
        }

        public List<Exception> Exception { get; } = new List<Exception>();

        public void PublishException(Exception ex)
        {
            PublishException(new Exception[] { ex });
        }

        public virtual void PublishException(IEnumerable<Exception> list)
        {
            if (Exception != null)
            {
                Exception.AddRange(list);
            }
            if (SupportsUpstream)
            {
                var upstreamlinks = GetUplinks();
                foreach (var stream in upstreamlinks)
                {
                    stream.PublishException(Exception);
                }
            }
        }

        private void ClearExceptions()
        {
            Exception.Clear();
            if (SupportsDownstream)
            {
                var downlinks = GetDownlinks();
                foreach (var stream in downlinks)
                {
                    stream.ClearExceptions();
                }
            }
        }

        public void ThrowIfException()
        {
            if (Exception.Count > 0)
            {
                List<Exception> exceptions = Exception.Copy();
                ClearExceptions();
                throw new AggregateException(exceptions);
            }
        }

        public virtual void Dispose()
        {
            if (SupportsDownstream)
            {
                var downlinks = GetDownlinks();
                foreach (var stream in downlinks)
                {
                    stream.Dispose();
                }
            }
        }

        public virtual void StartService()
        {
            if (SupportsDownstream)
            {
                var downlinks = GetDownlinks();
                foreach (var stream in downlinks)
                {
                    stream.StartService();
                }
            }
        }
    }
}
