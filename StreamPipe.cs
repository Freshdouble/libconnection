using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

namespace libconnection
{
    public class StreamPipe : IDisposable
    {
        public event EventHandler<MessageEventArgs> MessageReceived;

        private readonly Queue<Message> receivedMessage = new();
        private readonly SemaphoreSlim semaphore = new(0);
        public StreamPipe()
        {

        }

        private readonly List<DataStream> streams = new ();

        public DataStream TopElement { get => streams.Count > 0 ? streams[^1] : null; }

        public string Name { get; set; } = string.Empty;

        public List<object> AdditionalData { get; } = new List<object>();

        public int MaxQueueLength { get; set; } = 10;

        public void Add(DataStream stream)
        {
            if (TopElement == null)
            {
                streams.Add(stream);
            }
            else
            {
                TopElement.MessageReceived -= TopElement_MessageReceived;
                TopElement.AddReceiverStage(stream);
                if(!stream.IsInterface)
                {
                    stream.AddTransmitterStage(TopElement);
                }
                streams.Add(stream);
            }
            stream.MessageReceived += TopElement_MessageReceived;
        }

        public void TransmitMessage(Message msg)
        {
            TopElement.TransmitMessage(msg);
        }

        private void AddQueueMessage(Message msg)
        {
            lock(receivedMessage)
            {
                if (receivedMessage.Count > MaxQueueLength)
                {
                    throw new SystemException("Message buffer overflow. This should not happen");
                }
                else if (receivedMessage.Count == MaxQueueLength)
                {
                    receivedMessage.Dequeue();
                    receivedMessage.Enqueue(msg);
                }
                else
                {
                    receivedMessage.Enqueue(msg); //Add semaphore if we didnt deque a message so the semaphore count represents the message queue length
                    semaphore.Release();
                }
            }
        }

        public async Task<Message> ReceiveAsync()
        {
            await semaphore.WaitAsync();
            lock(receivedMessage)
            {
                return receivedMessage.Dequeue();
            }
        }

        private void TopElement_MessageReceived(object sender, MessageEventArgs e)
        {
            AddQueueMessage(e.Message);
            MessageReceived?.Invoke(sender, e);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            TopElement?.Dispose();
        }
        
        public bool IsConnected {get; private set;} = false;
    }
}
