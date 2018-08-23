namespace ConcurrencySamples
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal class SyncWriteFileSample
    {
        private static void ExecuteSync()
        {
            using (FileStream fs = new FileStream("somefile", FileMode.Create, FileAccess.ReadWrite))
            {
                byte[] buffer = new byte[256];
                Random r = new Random();
                r.NextBytes(buffer);
                fs.Write(buffer, 0, 256);
                byte[] hash = MD5.Create().ComputeHash(buffer);
                fs.Write(hash, 0, hash.Length);
            }
        }
    }

    internal class AsyncWriteFileSampleInAPM : IDisposable
    {
        private byte[] buffer;
        private FileStream fs;
        private volatile bool completed;

        public static void Execute()
        {
            using (var instance = new AsyncWriteFileSampleInAPM())
            {
                instance.BeginExecute();
                while (!instance.completed)
                {
                    Thread.Sleep(500);
                }
            }
        }

        private void BeginExecute()
        {
            this.fs = new FileStream("somefile", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, FileOptions.Asynchronous);
            this.buffer = new byte[256];
            Random r = new Random();
            r.NextBytes(this.buffer);
            this.fs.BeginWrite(this.buffer, 0, 256, Callback1, null);
        }

        private void Callback1(IAsyncResult result)
        {
            this.fs.EndWrite(result);
            byte[] hash = MD5.Create().ComputeHash(this.buffer);
            this.fs.BeginWrite(hash, 0, hash.Length, Callback2, null);
        }

        private void Callback2(IAsyncResult result)
        {
            this.fs.EndWrite(result);
            this.completed = true;
        }

        public void Dispose()
        {
            this.fs.Dispose();
        }
    }

    internal class AsyncWriteFileSampleInAPM2
    {
        public static void Execute()
        {
            var iterator = ExecuteIterator().GetEnumerator();
            Callback(iterator, false);
        }

        private static IEnumerable<IAsyncResult> ExecuteIterator()
        {
            using (FileStream fs = new FileStream("somefile", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, FileOptions.Asynchronous))
            {
                byte[] buffer = new byte[256];
                Random r = new Random();
                r.NextBytes(buffer);
                var asyncResult = fs.BeginWrite(buffer, 0, 256, null, null);
                yield return asyncResult;
                fs.EndWrite(asyncResult);
                byte[] hash = MD5.Create().ComputeHash(buffer);
                asyncResult = fs.BeginWrite(hash, 0, hash.Length, null, null);
                yield return asyncResult;
                fs.EndWrite(asyncResult);
            }
        }

        private static void Callback(object state, bool timedOut)
        {
            var iterator = (IEnumerator<IAsyncResult>)state;
            if (iterator.MoveNext())
            {
                ThreadPool.RegisterWaitForSingleObject(iterator.Current.AsyncWaitHandle, Callback, iterator, -1, true);
            }
        }
    }


    internal class AsyncWriteFileSampleInTAP
    {
        private static async Task ExecuteAsync()
        {
            using (FileStream fs = new FileStream("somefile", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, FileOptions.Asynchronous))
            {
                byte[] buffer = new byte[256];
                Random r = new Random();
                r.NextBytes(buffer);
                await fs.WriteAsync(buffer, 0, 256);
                byte[] hash = MD5.Create().ComputeHash(buffer);
                await fs.WriteAsync(hash, 0, hash.Length);
            }
        }
    }
}
