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
        private readonly SerialPort port = null;

        public static SerialPortConnection GenerateWithParameters(IDictionary<string, string> parameters)
        {
            string portname;
            int baudrate = 115200;
            Parity parity = Parity.None;
            int databits = 8;
            StopBits stopBits = StopBits.One;

            if(parameters.ContainsKey("port"))
            {
                portname = parameters["port"];
            }
            else
            {
                throw new ArgumentException("The serial port connection must have a port");
            }

            if (parameters.ContainsKey("baudrate"))
            {
                baudrate = int.Parse(parameters["baudrate"]);
            }

            if(parameters.ContainsKey("parity"))
            {
                parity = Enum.Parse<Parity>(parameters["parity"]);
            }

            if(parameters.ContainsKey("databits"))
            {
                databits = int.Parse(parameters["databits"]);
            }

            if(parameters.ContainsKey("stopbits"))
            {
                stopBits = Enum.Parse<StopBits>(parameters["stopbits"]);
            }

            return new SerialPortConnection(portname, baudrate, parity, databits, stopBits);
        }

        public SerialPortConnection(string portname, int baudrate = 115200, Parity parity = Parity.None, int databits = 8, StopBits stopBits = StopBits.One) :
            this(new SerialPort(portname, baudrate, parity, databits, stopBits))
        {
        }

        public bool SynchronizeContext { get; set; } = true;

        public override bool IsInterface => true;

        public SerialPortConnection(SerialPort port)
        {
            if (!port.IsOpen)
            {
                port.Open();
            }
            this.port = port;
        }

        public override async Task StartStream(CancellationToken token)
        {
            AwaitableTrigger trigger = new ();
            void eventhandler(object sender, SerialDataReceivedEventArgs eventargs)
            {
                trigger.Trigger();
            }

            port.DataReceived += eventhandler;
            while (!token.IsCancellationRequested)
            {
                    await trigger.WaitAsync(token);
                    while (port.BytesToRead > 0)
                    {
                        byte[] data = new byte[port.BytesToRead];
                        port.Read(data, 0, data.Length);
                        var msg = new Message(data);
                        base.ReceiveMessage(msg);
                    }
            }
            port.DataReceived -= eventhandler;
        }

        public override void TransmitMessage(Message data)
        {
            byte[] msg = data.Data;
            port.Write(msg, 0, msg.Length);
        }

        public void Close()
        {
            if (port.IsOpen)
            {
                port.Close();
            }
        }

        public override void Dispose()
        {
            GC.SuppressFinalize(this);
            if (!disposed)
            {
                Close();
                port.Dispose();
                base.Dispose();
            }
        }
    }
}
