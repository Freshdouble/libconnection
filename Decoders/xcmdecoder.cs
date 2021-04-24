using libxcm;
using libxcmparse.DataObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace libconnection.Decoders
{
    public class Xcmdecoder : DataStream
    {
        public override bool SupportsDownstream => true;

        public override bool SupportsUpstream => false;

        private Func<IEnumerable<byte>, bool>[] matchfunctions;
        private DataMessage[] messages;

        public Xcmdecoder(XCMTokenizer tokenizer)
        {
            IEnumerable<DataMessage> messageList = tokenizer.GetObjects<DataMessage>();
            matchfunctions = new Func<IEnumerable<byte>, bool>[messageList.Count()];
            messages = new DataMessage[matchfunctions.Length];
            for (int i = 0; i < matchfunctions.Length; i++)
            {
                messages[i] = messageList.ElementAt(i);
                matchfunctions[i] = messages[i].GetMatchFunction();
            }
        }

        public override void PublishUpstreamData(Message data)
        {
            for(int i = 0; i < matchfunctions.Length; i++)
            {
                if(matchfunctions[i].Invoke(data))
                {
                    DataMessage msg = messages[i];
                    msg.ParseMessage(data);
                    break;
                }
            }
            base.PublishUpstreamData(data);
        }

        public void PublishDownstreamData(DataCommand message)
        {
            PublishDownstreamData(new Message(message.GetData()));
        }
    }
}
