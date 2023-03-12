using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace libconnection
{
    public class DataStreamTap : DataStream
    {
        private Queue<Message> messages = new Queue<Message>();
        private SemaphoreSlim semaphore = new SemaphoreSlim(0);
        public uint MaxQueueLength { get; set; } = 0;

        public override bool IsInterface => false;

        protected override void ReceiveMessage(Message message)
        {
            base.ReceiveMessage(message);
            if(MaxQueueLength > 0)
            {
                lock (messages)
                {
                    while (messages.Count > MaxQueueLength)
                    {
                        messages.Dequeue();
                        if(semaphore.CurrentCount== 0)
                        {
                            throw new Exception("Semaphore count missmatch");
                        }
                        semaphore.Wait();
                    }
                    if(semaphore.CurrentCount != messages.Count)
                    {
                        throw new Exception("Semaphore count missmatch");
                    }
                    messages.Enqueue(message);
                    semaphore.Release();
                }
            }
        }

        
        public async Task<Message> ReadMessageAsync()
        {
            await semaphore.WaitAsync();
            lock(messages)
            {
                return messages.Dequeue();
            }
        }
    }
}
