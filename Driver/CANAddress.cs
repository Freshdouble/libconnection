using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace libconnection.Driver
{
    public struct CANAddress : IEquatable<CANAddress>
    {
        public CANAddress(int address)
        {
            Address = MaskCANAddress(address);
        }

        public static CANAddress GetFromArray(byte[] address)
        {
            int canaddr = 0;
            int bytesToRead = Math.Min(2, address.Length);
            for (int i = bytesToRead - 1; i >= 0; i--)
            {
                canaddr |= (address[i] << i);
            }
            return new CANAddress(canaddr);
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
            foreach(var b in GetAddressBytes().Reverse())
            {
                msg.PushFront(b);
            }
        }

        public bool Equals([AllowNull] CANAddress other)
        {
            return other.Address == Address;
        }

        public override bool Equals(object obj)
        {
            if(obj is CANAddress canaddr)
            {
                return Equals(canaddr);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Address.GetHashCode();
        }

        public static bool operator ==(CANAddress address1, CANAddress address2)
        {
            return address1.Equals(address2);
        }

        public static bool operator !=(CANAddress address1, CANAddress address2)
        {
            return !address1.Equals(address2);
        }
    }
}
