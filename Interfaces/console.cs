using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libconnection.Interfaces
{
    public class console : DataStream
    {
        private ConsoleLogger logger = ConsoleLogger.GetInstance();
        public override bool IsInterface => false;

        public override void TransmitMessage(Message msg)
        {
            base.TransmitMessage(msg);
            string message = Encoding.ASCII.GetString(msg.Data);
            logger.Write(message);
        }
    }
}
