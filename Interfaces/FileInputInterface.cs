using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace libconnection.Interfaces
{
    public class FileInputInterface : DataStream
    {
        private FileStream file;
        private CancellationTokenSource cts = new CancellationTokenSource();
        public FileInputInterface(string filename) : this(File.Open(filename, FileMode.Open, FileAccess.Read))
        {

        }

        public FileInputInterface(FileStream fs)
        {
            file = fs;
            Task.Run(async () =>
            {
                await Task.Delay(1000);
                var t = cts.Token;
                while(!t.IsCancellationRequested)
                {
                    try
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead = await fs.ReadAsync(buffer, t);
                        if (bytesRead > 0)
                        {
                            ReceiveMessage(new Message(buffer.Take(bytesRead)));
                        }
                    }
                    catch (Exception ex)
                    {
                        ThrowCriticalException(ex);
                    }
                }
            });
        }

        public override bool IsInterface => true;

        public override void Dispose()
        {
            if(!disposed)
            {
                base.Dispose();
                cts.Cancel();
                cts.Dispose();
                file?.Dispose();
            }
        }
    }
}
