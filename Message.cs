using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace libconnection
{
    public class Message : IEnumerable<byte>
    {
        public enum CompressionMethodType
        {
            Base64,
            CsvCompatibleOrdered
        }
        private static readonly DateTime UnixEpoch =
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        private LinkedList<byte> data;
        private DateTime creationTime;
        public Message()
        {
            data = new LinkedList<byte>();
            creationTime = DateTime.Now;
        }

        public Message(IEnumerable<byte> data)
        {
            this.data = new LinkedList<byte>(data);
            creationTime = DateTime.Now;
        }

        public bool UseCompressedPrint { get; set; } = true;
        public CompressionMethodType CompressionMethod { get; set; } = CompressionMethodType.CsvCompatibleOrdered;

        public Message Copy()
        {
            return new Message(this.Data)
            {
                Port = Port,
                creationTime = creationTime,
                UseCompressedPrint = UseCompressedPrint
            };
        }

        public DateTime CreationTime => creationTime;

        public int Length => data.Count;

        public int Port { get; set; } = 0;

        public object CustomObject { get; set; } = null;

        public byte[] Data
        {
            get
            {
                return data.ToArray();
            }
            set
            {
                data.Clear();
                foreach(byte b in value)
                {
                    data.AddLast(b);
                }
                creationTime = DateTime.Now;
            }
        }

        public void PushFront(byte data)
        {
            this.data.AddFirst(data);
        }

        public void PushEnd(byte data)
        {
            this.data.AddLast(data);
        }

        public byte PopFirst(bool remove = true)
        {
            byte ret = data.First();
            if(remove)
            {
                data.RemoveFirst();
            }
            return ret;
        }

        public byte[] PopAndRemoveFirstMultiple(int blocksize)
        {
            int toRemove = Math.Min(blocksize, Length);
            byte[] ret = new byte[toRemove];
            for(int i = 0; i < toRemove; i++)
            {
                ret[i] = PopFirst(true);
            }
            return ret;
        }

        public byte PopLast(bool remove = true)
        {
            byte ret = data.Last();
            if(remove)
            {
                data.RemoveLast();
            }
            return ret;
        }

        public IEnumerator<byte> GetEnumerator()
        {
            return data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return data.GetEnumerator();
        }

        public override string ToString()
        {
            if (UseCompressedPrint)
            {
                switch (CompressionMethod)
                {
                    case CompressionMethodType.Base64:
                        return string.Format("Time:{0:dd.MM.yyyy.HH-mm-ss}; Timestamp:{1}; Data:[{2}];", creationTime, GetCurrentUnixTimestampMillis(creationTime.ToUniversalTime()), Convert.ToBase64String(Data));
                    case CompressionMethodType.CsvCompatibleOrdered:
                        return string.Format("{0}; {1};", GetCurrentUnixTimestampMillis(creationTime.ToUniversalTime()), Convert.ToBase64String(Data));
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                return string.Format("Time:{0:dd.MM.yyyy.HH-mm-ss}; Timestamp:{1}; Data:[{2}];", creationTime, GetCurrentUnixTimestampMillis(creationTime.ToUniversalTime()), BitConverter.ToString(Data));
            }
        }
        
        
        public static long GetCurrentUnixTimestampMillis(DateTime time)
        {
            return (long) (time - UnixEpoch).TotalMilliseconds;
        }
    }
}