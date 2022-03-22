using System;
using System.Collections.Generic;
using System.Text;
using libsmp;

namespace libconnection.Decoders
{
    public class SMPDecoder : DataStream
    {
        private readonly SMP smp = new ManagedSMP();

        public SMPDecoder()
        {
            
        }

        public Type SMPLibrayType => smp.GetType();

        public uint ReceiveErrors { get => smp.ReceiveErrors; }
        public uint ReceivedMessages { get => smp.ReceivedMessages; }

        public override bool SupportsDownstream => true;

        public override bool SupportsUpstream => true;

        public override void PublishUpstreamData(Message data)
        {
            //Try decode
            smp.ProcessBytes(data.Data);
            while(smp.StoredMessages > 0)
            {
                base.PublishUpstreamData(new Message(smp.GetMessage()));
            }
        }

        public override void PublishDownstreamData(Message data)
        {
            base.PublishDownstreamData(new Message(smp.GenerateMessage(data.Data)));
        }

        public override void Dispose()
        {
            GC.SuppressFinalize(this);
            smp.Dispose();
            base.Dispose();
        }
    }
}
