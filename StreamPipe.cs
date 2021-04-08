using System;
using System.Collections.Generic;
using System.Text;

namespace libconnection
{
    public class StreamPipe : DataStreamTap
    {
        private List<DataStream> streams = new List<DataStream>();

        public DataStream TopElement { get => streams.Count > 0 ? streams[streams.Count - 1] : null; }

        public override bool SupportsUpstream => false;

        public void Add(DataStream stream)
        {
            TopElement?.LinkUpstream(stream);
            streams.Add(stream);
        }

        public override void Dispose()
        {
            TopElement?.Dispose();
        }

        public override void StartService()
        {
            TopElement?.StartService();
        }
    }
}
