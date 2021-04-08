using System;
using System.Collections.Generic;
using System.Text;

namespace libconnection.Driver
{
    public struct CANAddress
    {
        public CANAddress(int address)
        {
            Address = MaskCANAddress(address);
        }
        public static int MaskCANAddress(int address)
        {
            return address & 0x7FF;
        }
        public int Address { get; private set; }

        public int DID
        {
            get
            {
                return (Address >> 5) & 0x0F;
            }
            set
            {
                Address = MID | ((value & 0x0F) << 5);
            }
        }

        public int MID
        {
            get
            {
                return Address & 0x1F;
            }
            set
            {
                Address = DID | (value & 0x1F);
            }
        }

        public IEnumerable<byte> GetAddressBytes()
        {
            int offset = 0;
            for(int i = 0; i < 2; i++)
            {
                yield return (byte)((Address >> (8 * offset)) & 0xFF);
                offset++;
            }
        }

        public void AddToMessage(Message msg)
        {
            foreach(var b in GetAddressBytes())
            {
                msg.PushFront(b);
            }
        }
    }
}
