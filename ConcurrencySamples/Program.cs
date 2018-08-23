namespace ConcurrencySamples
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal class Program
    {
        private static int runningTasks;

        private static int totalExecutedTasks;

        private static volatile bool printInfoExitFlag;

        private static void Main(string[] args)
        {
            ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);
            ThreadPool.GetMinThreads(out int minWorkerThreads, out int minCompletionPortThreads);
            Console.WriteLine($"Worker Threads: {minWorkerThreads} - {maxWorkerThreads}");
            Console.WriteLine($"Completion Port Threads: {minCompletionPortThreads} - {maxCompletionPortThreads}");
            Console.WriteLine("Press any key to continue...");
            var key = Console.ReadKey();

            var printInfoThread = new Thread(PrintInformation);
            printInfoThread.Start();

            switch (key.Key)
            {
                case ConsoleKey.D1:
                    ThreadPoolExecuteTask(DoWorkWithBlock);
                    break;

                case ConsoleKey.D2:
                    ThreadPoolExecuteTask(DoWorkNoBlock);
                    break;

                case ConsoleKey.D3:
                    ThreadPoolExecuteTask(AsyncDoWorkInAPM);
                    break;

                case ConsoleKey.D4:
                    TaskRunExecuteTask();
                    break;
            }

            printInfoExitFlag = true;
            printInfoThread.Join();
        }

        /*
         * https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/ 
        */

        private static void ThreadPoolExecuteTask(WaitCallback doWorkCallback)
        {
            const int TotalTasks = 1000;
            for (int i = 0; i < TotalTasks; i++)
            {
                ThreadPool.QueueUserWorkItem(doWorkCallback, null);
            }

            while (totalExecutedTasks < TotalTasks)
            {
                Thread.Sleep(500);
            }
        }

        private static void TaskRunExecuteTask()
        {
            const int TotalTasks = 1000;
            Task[] tasks = new Task[TotalTasks];
            for (int i = 0; i < TotalTasks; i++)
            {
                tasks[i] = Task.Run(AsyncDoWorkInTAP);
            }

            Task.WaitAll(tasks);
        }

        private static void DoWorkNoBlock(object state)
        {
            Interlocked.Increment(ref runningTasks);
            BusyDoWork(3);
            Interlocked.Decrement(ref runningTasks);
            Interlocked.Increment(ref totalExecutedTasks);
        }

        private static void DoWorkWithBlock(object state)
        {
            Interlocked.Increment(ref runningTasks);
            Thread.Sleep(1000);
            BusyDoWork(2);
            Interlocked.Decrement(ref runningTasks);
            Interlocked.Increment(ref totalExecutedTasks);
        }

        private static void AsyncDoWorkInAPM(object state)
        {
            // APM: https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/asynchronous-programming-model-apm
            Interlocked.Increment(ref runningTasks);
            new Timer((_) =>
            {
                BusyDoWork(2);
                Interlocked.Decrement(ref runningTasks);
                Interlocked.Increment(ref totalExecutedTasks);
            }, null, 1000, -1);
        }

        private static async Task AsyncDoWorkInTAP()
        {
            // TAP: https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap
            Interlocked.Increment(ref runningTasks);
            await Task.Delay(1000);
            BusyDoWork(2);
            Interlocked.Decrement(ref runningTasks);
            Interlocked.Increment(ref totalExecutedTasks);
        }

        private static void BusyDoWork(double seconds)
        {
            long sum = 0, k = 0;
            DateTime runUntil = DateTime.UtcNow.AddSeconds(seconds);
            while (runUntil > DateTime.UtcNow)
            {
                k++;
                sum += k;
            }
        }

        private static void PrintInformation()
        {
            while (!printInfoExitFlag)
            {
                Thread.Sleep(1000);
                Console.WriteLine($"Running Tasks: {runningTasks}, Completed Tasks: {totalExecutedTasks}, Current Threads: {Process.GetCurrentProcess().Threads.Count}");
            }
        }
    }
}
