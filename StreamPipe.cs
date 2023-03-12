using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace libconnection
{
    public class StreamPipe : DataStreamTap
    {

        public StreamPipe()
        {

        }
        public StreamPipe(string pipe) : this()
        {
            string[] pipeParts = pipe.Split('|');
            foreach(var part in pipeParts)
            {
                string[] pair = part.Split(':');
                Add(NameResolver.GetStreamByName(pair[0], pair.Skip(1)));
            }
        }

        private List<DataStream> streams = new List<DataStream>();

        public DataStream TopElement { get => streams.Count > 0 ? streams[streams.Count - 1] : null; }

        public string Name { get; set; } = string.Empty;

        public List<object> AdditionalData { get; } = new List<object>();

        public void Add(DataStream stream)
        {
            if(TopElement == null)
            {
                if(!stream.IsInterface)
                {
                    throw new InvalidOperationException("The first element in the pipe must be a interface");
                }
                streams.Add(stream);
            }
            else
            {
                TopElement.AddReceiverStage(stream);
                streams.Add(stream);
            }
        }

        public override void Dispose()
        {
            TopElement?.Dispose();
        }
        
        public bool IsConnected {get; private set;} = false;
    }
}
