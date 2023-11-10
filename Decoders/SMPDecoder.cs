using System;
using System.Collections.Generic;
using System.Linq;
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
            var transmitterMTU = GetTransmitterMTU();
            var encodedMessage = smp.GenerateMessage(message.Data);
            if (transmitterMTU > 0)
            {
                int currentIndex = 0;
                while(currentIndex < encodedMessage.Length)
                {
                    var chunk = encodedMessage.Skip(currentIndex).Take(transmitterMTU);
                    if (!chunk.Any())
                    {
                        break;
                    }
                    base.TransmitMessage(new Message(chunk));
                    currentIndex += transmitterMTU;
                }
            }
            else
            {
                base.TransmitMessage(new Message(encodedMessage));
            }
        }

        public override void Dispose()
        {
            if(!disposed)
            {
                GC.SuppressFinalize(this);
                smp.Dispose();
                base.Dispose();
            }
        }
    }
}
