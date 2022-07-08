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

        public override bool SupportsUpstream => false;
        public string Name { get; set; } = string.Empty;

        public List<object> AdditionalData { get; } = new List<object>();

        public void Add(DataStream stream)
        {
            TopElement?.LinkUpstream(stream);
            streams.Add(stream);
        }

        public override void Dispose()
        {
            TopElement?.Dispose();
        }
        
        public bool IsConnected {get; private set;} = false;

        public override void StartService()
        {
            if (TopElement != null)
            {
                if (TopElement.SupportsUpstream)
                {
                    TopElement.LinkUpstream(this);
                    IsConnected = true;
                }
                TopElement.StartService();
            }
        }
    }
}
