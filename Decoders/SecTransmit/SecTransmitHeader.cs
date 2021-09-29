using System;
using System.Collections.Generic;
using System.Text;

namespace libconnection.Decoders
{
    public enum PacketState
    {
        FirstTransmission,
        ConsecutiveTransmission,
        LastPacket
    }

    public enum ControlMessageType
    {
        ACK,
        NACK
    };

    public struct SecTransmitHeader
    {
        private int packetcounter;
        public bool IsControlMessage { get; set; }
        public PacketState State { get; set; }

        public SecTransmitHeader(byte b)
        {
            packetcounter = b & 0x0F;
            IsControlMessage = (b & (1 << 5)) != 0;
            State = (PacketState)((b >> 6) & 0x03);
        }

        public int PacketCount
        {
            get => packetcounter;
            set
            {
                packetcounter = value & 0x0F;
            }
        }

        public byte GetHeaderByte()
        {
            int controlMSGBit = IsControlMessage ? 1 : 0;
            int header = (int)State << 6 | controlMSGBit << 5 | packetcounter;
            return (byte)(header & 0xFF);
        }

        public static implicit operator byte(SecTransmitHeader d) => d.GetHeaderByte();
        public static explicit operator SecTransmitHeader(byte b) => new SecTransmitHeader(b);
    }
}
