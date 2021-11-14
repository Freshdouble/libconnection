/**
* @file "UDPPackage.cs"
* @author Florian Guggi
* @date 2019-12-18
* @brief A class that handles the UDP packages for interprocess communication
* @see GroundstationÜberblick
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace libconnection.Interfaces.UDP
{
    /**
    * @brief This class is used to create and parse the UDP packages used for
    *        interprocess communication. It can and should be used
    *        throughout all the different layers.
    */
    public struct Package
    {
        /**
        * @brief Describes the package type.
        * @see GroundstationÜberblick for definitions
        */
        public enum Type : byte
        {
            RESERVED,
            DATAFRAME,
            HEARTBEAT
        }

        public Type type;
        public DateTime timestamp;
        public byte[] payload;

        public bool UseShortHeader { get; set; }
        

        /**
        * @brief Simple constructor for creating a package.
        */
        public Package(Type type, DateTime timestamp, byte[] payload)
        {
            this.type = type;
            this.timestamp = timestamp;
            this.payload = payload;
            UseShortHeader = false;
        }

        public static Package parse(IEnumerable<byte> data)
        {
            return parse(data as byte[] ?? data.ToArray());
        }

        /**
        * @brief Parses and returns the given package.
        * @param data A byte array containing the UDP package payload.
        * @returns The parsed package
        */
        public static Package parse(byte[] data)
        {
            if ((data[0] & 0x80) != 0)
            {
                //Long Header
                long time = BitConverter.ToInt64(data.Skip(1).Take(8).ToArray(), 0);
                return new Package((Type)(data[0] & 0x7F), DateTime.FromBinary(time), data.Skip(9).ToArray());
            }
            else
            {
                //Short header
                return new Package((Type)data[0], DateTime.Now, data.Skip(1).ToArray());
            }
        }

        /**
        * @brief Serializes the package into byte array
        * @returns A byte array with length 9 + payload.Length
        */
        public byte[] Serialize() 
        {
            if (UseShortHeader)
            {
                byte[] data = new byte[1 + payload.Length];
                data[0] = (byte)type;
                Array.Copy(payload, 0, data, 1, payload.Length);
                return data;
            }
            else
            {
                byte[] data = new byte[9 + payload.Length];
                data[0] = (byte)((int)type | (1 << 7)); //Set header with the long header bit
                Array.Copy(BitConverter.GetBytes(timestamp.ToBinary()), 0, data, 1, 8);
                Array.Copy(payload, 0, data, 9, payload.Length);
                return data;
            }
        }

        /**
        * @brief Shortcut for creating a Heartbeat package
        * @returns A heartbeat with the current time and no payload
        */
        public static Package CreateHeartbeat()
        {
            return new Package(Type.HEARTBEAT, DateTime.Now, new byte[0]);
        }

        public override string ToString()
        {
            return String.Format("{0}:{1}:[{2}]", this.type.ToString(), this.timestamp.ToString("HH.mm.ss"), BitConverter.ToString(this.payload).Replace("-", ""));
        }
    }
}
