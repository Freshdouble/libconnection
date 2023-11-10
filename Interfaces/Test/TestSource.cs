using libconnection.Interfaces.UDP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace libconnection.Interfaces.Test
{
    public class TestSource : DataStream
    {
        public override bool IsInterface => true;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private Task workingTask;
        private Random rand = new Random();

        public static TestSource GenerateWithParameters(IDictionary<string, string> parameter)
        {
            int pause = 0;
            if (parameter.ContainsKey("pause"))
            {
                pause = int.Parse(parameter["pause"]);
            }
            return new TestSource(pause);
        }
        TestSource(int pause)
        {
            workingTask = Task.Run(async () =>
            {
                var token = cts.Token;
                while(!token.IsCancellationRequested)
                {
                    int msglength = rand.Next(1, 1000);
                    byte[] buf = new byte[msglength];
                    rand.NextBytes(buf);
                    base.ReceiveMessage(new Message(buf));
                    await Task.Delay(pause, token);
                }
            });
        }

        public override void Dispose()
        {
            if(cts != null)
            {
                cts.Cancel();
                workingTask.Wait();
                cts.Dispose();
                cts = null;
            }
        }
    }
}
