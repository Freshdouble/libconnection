using System;
using System.Collections.Generic;
using System.Text;

namespace libconnection.Driver
{
    public enum SerialToCANMessageType
    {
        Filter,
        Data
    }

    /**************************************************
     * Message Format:
     *  1Byte SerialTOCANMessageType
     *  2Byte CAN Address LSB first
     *  Payload
     * ************************************************/
    public class SerialToCANDriver : DataStream
    {
        public override bool SupportsDownstream => true;

        public override bool SupportsUpstream => true;

        private bool filteractivated = false;

        public SerialToCANDriver()
        {

        }

        public int CANMaxMessageLength { get; set; } = 8;

        public CANAddress SendAddress { get; set; } = new CANAddress(0xFFFF);
        public CANAddress FilterAddress { get; set; } = new CANAddress(0xFFFF);

        public bool UseSoftwareFilter { get; set; } = false;

        public bool ActivateHardwareFilter
        {
            get
            {
                return filteractivated;
            }
            set
            {
                filteractivated = value;
                if(value)
                {
                    SendBridgeMessage(SerialToCANMessageType.Filter, FilterAddress.GetAddressBytes());
                }
                else
                {
                    SendBridgeMessage(SerialToCANMessageType.Filter, new byte[] { 0xFF, 0xFF });
                }
            }
        }

        private void SendBridgeMessage(SerialToCANMessageType type, IEnumerable<byte> msg)
        {
            SendBridgeMessage(type, new Message(msg));
        }

        private void SendBridgeMessage(SerialToCANMessageType type, Message msg )
        {
            switch (type)
            {
                case SerialToCANMessageType.Filter:
                    break;
                case SerialToCANMessageType.Data:
                    if(msg is AddressMessage addrmsg)
                    {
                        addrmsg.AddAddresstoFront();
                    }
                    else
                    {
                        SendAddress.AddToMessage(msg);
                    }
                    break;
                default:
                    break;
            }
            msg.PushFront((byte)type);
            base.PublishDownstreamData(msg);
        }
        public override void PublishDownstreamData(Message data)
        {
            if (data.Length > CANMaxMessageLength)
            {
                PublishException(new ArgumentException("The can device can not handle messages that are larger than " + CANMaxMessageLength.ToString() + " byte"));
            }
            else
            {
                SendBridgeMessage(SerialToCANMessageType.Data, data);
            }
        }

        public override void PublishUpstreamData(Message data)
        {
            SerialToCANMessageType messageType = (SerialToCANMessageType)data.PopFirst();
            switch (messageType)
            {
                case SerialToCANMessageType.Filter:
                    break;
                case SerialToCANMessageType.Data:
                    byte[] canaddress = new byte[] { data.PopFirst(), data.PopFirst() };
                    if(UseSoftwareFilter)
                    {
                        CANAddress addr = CANAddress.GetFromArray(canaddress);
                        if(addr != FilterAddress)
                        {
                            return;
                        }
                    }
                    AddressMessage msg = new AddressMessage(data);
                    msg.Addresses.Add(canaddress);
                    base.PublishUpstreamData(msg);
                    break;
                default:
                    break;
            }
        }

        public override void StartService()
        {
            base.StartService();
            SendBridgeMessage(SerialToCANMessageType.Filter, new byte[] { 0xFF, 0xFF });
        }
    }
}
