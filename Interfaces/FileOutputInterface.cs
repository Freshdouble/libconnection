using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libconnection.Interfaces
{
    public class FileOutputInterface : DataStream
    {
        private FileStream file;
        public FileOutputInterface(string filename) : this(File.Open(filename, FileMode.Create, FileAccess.Write))
        {
        }
        public FileOutputInterface(FileStream fs) 
        {
            file = fs;
        }

        public override bool IsInterface => true;

        public override void Dispose()
        {
            if(!disposed)
            {
                base.Dispose();
                file?.Dispose();
            }
        }

        public override void TransmitMessage(Message message)
        {
            file.Write(message.Data, 0, message.Length);
        }
    }
}
