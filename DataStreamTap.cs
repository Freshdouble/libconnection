using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace libconnection
{
    public class DataStreamTap : DataStream
    {
        private Queue<Message> messages = new Queue<Message>();
        public override bool SupportsDownstream => true;

        public override bool SupportsUpstream => true;

        private SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);

        public override void PublishUpstreamData(Message data)
        {
            lock(messages)
            {
                messages.Enqueue(data);
            }
            base.PublishUpstreamData(data);
            if(semaphore.CurrentCount == 0)
            {
                semaphore.Release();
            }
        }

        public override void PublishException(IEnumerable<Exception> list)
        {
            base.PublishException(list);
            if (semaphore.CurrentCount == 0)
            {
                semaphore.Release();
            }
        }

        public async Task<Message> ReadMessageAsync()
        {
            await semaphore.WaitAsync().ConfigureAwait(false);
            lock (messages)
            {
                ThrowIfException();
                return messages.Dequeue();
            }
        }

        public async Task<Message> ReadMessageAsync(int timeout)
        {
            if(await semaphore.WaitAsync(timeout))
            {
                lock (messages)
                {
                    ThrowIfException();
                    return messages.Dequeue();
                }
            }
            else
            {
                throw new TimeoutException();
            }
        }
    }
}
