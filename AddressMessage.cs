using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace libconnection
{
    public class AddressMessage : Message
    {
        public AddressMessage()
        {
        }

        public AddressMessage(IEnumerable<byte> data) : base(data)
        {
        }

        public List<byte[]> Addresses { get; } = new List<byte[]>();

        public IEnumerable<byte> GetAdressArray()
        {
            return Addresses.SelectMany(x => x);
        }

        public void AddAddresstoFront()
        {
            IEnumerable<byte> address = GetAdressArray();
            foreach(var b in address)
            {
                PushFront(b);
            }
        }
    }
}
