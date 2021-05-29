using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace libCAN
{
    public class CAN : IDisposable
    {
        [DllImport("libcansock.so")]
        private static extern IntPtr GetInstance([MarshalAs(UnmanagedType.LPStr)] string caninterfacename);
        [DllImport("libcansock.so")]
        private static extern void DeleteInstance(IntPtr ptr);
        [DllImport("libcansock.so")]
        private static extern int SendBytes(IntPtr instance, int canID, [In] byte[] data, int length);
        [DllImport("libcansock.so")]
        private static extern int ReceiveBytes(IntPtr instance, [Out] byte[] data, int length, ref ushort canID);

        private IntPtr instance;
        public CAN(string interfacename)
        {
            instance = GetInstance(interfacename);
            if(instance.ToInt64() == 0)
                throw new Exception("Couldn't create instance of native library");
        }

        public void Dispose()
        {
            if(instance.ToInt64() != 0)
            {
                DeleteInstance(instance);
            }
        }

        public int Send(int canID, byte[] data)
        {
            if(instance.ToInt64() == 0)
                throw new Exception("Couldn't send data");
            return SendBytes(instance, canID, data, data.Length);
        }

        public int Send(int canID, IEnumerable<byte> data)
        {
            return Send(canID, data.ToArray());
        }

        public (int, byte[]) ReceiveBytes()
        {
            if(instance.ToInt64() == 0)
                throw new Exception("Couldn't send data");
            byte[] data = new byte[8];
            ushort canID = 0;
            int databytes = ReceiveBytes(instance, data, data.Length, ref canID);
            return ((int)canID, data.Take(databytes).ToArray());
        }

        public Task<(int, byte[])> ReceiveBytesAsync(CancellationToken ct)
        {
            return Task.Run(() => {
                return ReceiveBytes();
            }, ct);
        }

        public Task<(int, byte[])> ReceiveBytesAsync()
        {
            return Task.Run(() => {
                return ReceiveBytes();
            });
        }
    }
}
