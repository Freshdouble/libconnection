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
        private readonly FileStream file;
        public FileInputInterface(string filename) : this(File.Open(filename, FileMode.Open, FileAccess.Read))
        {

        }

        public FileInputInterface(FileStream fs)
        {
            file = fs;
        }

        public override async Task StartStream(CancellationToken t)
        {
            await Task.Delay(1000, t);
            while (!t.IsCancellationRequested)
            {
                byte[] buffer = new byte[1024];
                int bytesRead = await file.ReadAsync(buffer, t);
                if (bytesRead > 0)
                {
                    ReceiveMessage(new Message(buffer.Take(bytesRead)));
                }
            }
        }

        public override bool IsInterface => true;

        public override void Dispose()
        {
            GC.SuppressFinalize(this);
            if(!disposed)
            {
                base.Dispose();
                file?.Dispose();
            }
        }
    }
}
