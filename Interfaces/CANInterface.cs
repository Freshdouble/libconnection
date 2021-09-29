using libCAN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace libconnection.Interfaces
{
    public class CANInterface : DataStream
    {
        private CAN can;
        public override bool SupportsDownstream => false;

        public override bool SupportsUpstream => true;

        private Task receiveTask = null;
        private CancellationTokenSource cts = new CancellationTokenSource();

        public int OwnReceiveID { get; set; } = -1;

        public CANInterface(string caninterface)
        {
            can = new CAN(caninterface);
            var token = cts.Token;
            receiveTask = Task.Factory.StartNew(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        (int canID, byte[] data) = await can.ReceiveBytesAsync(token).ConfigureAwait(false);
                        if(OwnReceiveID >= 0 && canID != OwnReceiveID)
                        {
                            continue;
                        }
                        AddressMessage addrrmessage = new AddressMessage(data);
                        byte[] idBytes = new byte[] { (byte)(canID >> 8), (byte)(canID & 0xFF) };
                        addrrmessage.Addresses.Add(idBytes);
                        base.PublishUpstreamData(addrrmessage);
                    }
                    catch(Exception ex)
                    {
                        if (ex is TaskCanceledException)
                            throw;
                        else
                        {
                            base.PublishException(ex);
                        }
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        public int CANSendAddress { get; set; } = -1;

        public override void Dispose()
        {
            cts.Cancel();
            try
            {
                receiveTask?.Wait(1000);
            }
            catch(Exception)
            {

            }
            can.Dispose();
            cts.Dispose();
            base.Dispose();
        }

        public override void PublishDownstreamData(Message data)
        {
            int address = CANSendAddress;
            if (data is AddressMessage addrmessage)
            {
                byte[] addrbytes = addrmessage.Addresses[0];
                if(addrbytes.Length == 2)
                {
                    address = addrbytes[0] << 8 | addrbytes[1];
                }
                else
                {
                    address = addrbytes[0];
                }
                var leftAddresses = addrmessage.Addresses.Skip(1).ToList();
                addrmessage.Addresses.Clear();
                if(leftAddresses.Count > 0)
                {
                    addrmessage.Addresses.AddRange(leftAddresses);
                }
                addrmessage.AddAddresstoFront();
            }
            if (address < 0)
            {
                throw new ArgumentException("No CAN Address provided");
            }
            
            if(can.Send(address, data) != data.Length)
            {
                throw new ArgumentException("No data was sent");
            }
        }
    }
}
