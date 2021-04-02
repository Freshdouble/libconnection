﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace libconnection
{
    public class Message : IEnumerable<byte>
    {
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

        public DateTime CreationTime => creationTime;

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
            return string.Format("{0}:[{1}]", creationTime.ToString("HH-mm-ss"), BitConverter.ToString(Data).Replace("-", ""));
        }
    }
}