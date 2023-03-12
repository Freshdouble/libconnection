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

        public override bool IsInterface => false;

        protected override void ReceiveMessage(Message message)
        {
            smp.ProcessBytes(message.Data);
            while(smp.StoredMessages > 0)
            {
                base.ReceiveMessage(new Message(smp.GetMessage()));
            }
        }

        public override void TransmitMessage(Message message)
        {
            base.TransmitMessage(new Message(smp.GenerateMessage(message.Data)));
        }

        public override void Dispose()
        {
            GC.SuppressFinalize(this);
            smp.Dispose();
            base.Dispose();
        }
    }
}
