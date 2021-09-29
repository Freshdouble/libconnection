using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace libconnection.Interfaces
{
    public class SerialPortConnection : DataStream, IDisposable
    {
        private CancellationTokenSource cts = new CancellationTokenSource();
        private SerialPort port = null;
        private Task workingTask;
        private bool disposed = false;
        public override bool SupportsDownstream => false;

        public override bool SupportsUpstream => true;

        public SerialPortConnection(string portname, int baudrate = 115200, Parity parity = Parity.None, int databits = 8, StopBits stopBits = StopBits.One) :
            this(new SerialPort(portname, baudrate, parity, databits, stopBits))
        {
        }

        public bool SynchronizeContext { get; set; } = true;

        public SerialPortConnection(SerialPort port)
        {
            CancellationToken token = cts.Token;
            if (!port.IsOpen)
            {
                port.Open();
            }
            ExecutionContext ec = ExecutionContext.Capture();
            SemaphoreSlim sslm = new SemaphoreSlim(1, 1);
            workingTask = Task.Factory.StartNew(async () =>
            {
                AwaitableTrigger trigger = new AwaitableTrigger(token);
                SerialDataReceivedEventHandler eventhandler = null;
                eventhandler = (sender, eventargs) =>
                {
                    trigger.Trigger();
                };
                port.DataReceived += eventhandler;
                bool shouldCancel = false;
                while (!token.IsCancellationRequested && !shouldCancel)
                {
                    try
                    {
                        await trigger.WaitAsync().ConfigureAwait(false);
                        token.ThrowIfCancellationRequested();
                        while (port.BytesToRead > 0)
                        {
                            byte[] data = new byte[port.BytesToRead];
                            port.Read(data, 0, data.Length);
                            Message msg = new Message(data);
                            if (SynchronizeContext)
                            {
                                await sslm.WaitAsync(token);
                                token.ThrowIfCancellationRequested();
                                ExecutionContext.Run(ec, (context) =>
                                {
                                    base.PublishUpstreamData(msg as Message);
                                    if (sslm.CurrentCount == 0)
                                    {
                                        sslm.Release();
                                    } 
                                }, null);
                            }
                            else
                            {
                                base.PublishUpstreamData(msg);
                            }
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        shouldCancel = true;
                    }
                }
                port.DataReceived -= eventhandler;
            }, TaskCreationOptions.LongRunning);
            this.port = port;
        }

        public override void PublishDownstreamData(Message data)
        {
            byte[] msg = data.Data;
            port.Write(msg, 0, msg.Length);
        }

        ~SerialPortConnection()
        {
            Dispose();
        }

        private void StopExecution()
        {
            cts.Cancel();
            try
            {
                workingTask.Wait(2000);
            }
            catch(Exception)
            {

            }
            cts.Dispose();
        }

        public void Close()
        {
            StopExecution();
            if (port.IsOpen)
            {
                port.Close();
            }
        }

        public override void Dispose()
        {
            if (!disposed)
            {
                Close();
                port.Dispose();
                disposed = true;
            }
        }
    }
}
