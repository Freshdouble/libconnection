using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace libconnection
{
    public class SynchronizedQueue<T>
    {
        private Mutex mut = new();
        private Queue<T> data = new();
        private SemaphoreSlim semaphore = new SemaphoreSlim(0);

        public int Count => semaphore.CurrentCount;

        public void Enqueue(T message)
        {
            mut.WaitOne();
            try
            {
                data.Enqueue(message);
                semaphore.Release();
            }
            finally
            {
                mut.ReleaseMutex();
            }
        }

        public Task<T> DequeueAsync()
        {
            return DequeueAsync(CancellationToken.None);
        }
        
        public async Task<T> DequeueAsync(CancellationToken token)
        {
            await semaphore.WaitAsync(token);
            mut.WaitOne();
            try
            {
                return data.Dequeue();
            }
            finally
            {
                mut.ReleaseMutex();
            }
        }
    }
}