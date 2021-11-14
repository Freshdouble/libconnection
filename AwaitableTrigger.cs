using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace libconnection
{
    public sealed class AwaitableTrigger
    {
        private SemaphoreSlim sslm = new SemaphoreSlim(0, 1);
        private CancellationToken ct;
        public AwaitableTrigger(CancellationToken ct, bool isTriggered = false)
        {
            this.ct = ct;
            if(isTriggered)
            {
                sslm.Release();
            }
        }

        public bool WillWait => sslm.CurrentCount == 0;

        public void Trigger()
        {
            if (sslm.CurrentCount < 1)
            {
                sslm.Release();
            }
        }

        public Task WaitAsync()
        {
            return sslm.WaitAsync(ct);
        }
    }
}
