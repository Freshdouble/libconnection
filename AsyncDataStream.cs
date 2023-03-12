using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace libconnection
{
    public abstract class AsyncDataStream : DataStream
    {
        Mutex mutex = new Mutex();
    }
}
