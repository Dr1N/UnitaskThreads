using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace CandidateTest.Threads
{
    internal static class Program
    {
        private const int WorkersCount = 100;
        private const double WorkTimeInMinutes = 0.4;
        private const int BytesInMb = 1024 * 1024;

        private static readonly CancellationTokenSource Cts = new CancellationTokenSource();
        private static DateTime _timeToBeCompleted;
        private static bool _completedByTime;
        private static System.Timers.Timer _aTimer;

        private static void Main()
        {
            // Prepare

            InitTimer();

            Console.WriteLine($"RAM used: {Process.GetCurrentProcess().WorkingSet64 / BytesInMb} MB");
            Console.WriteLine("Press any key to start workers...");
            Console.ReadKey(true);

            // Start

            _timeToBeCompleted = DateTime.Now.AddMinutes(WorkTimeInMinutes);
            var workers = new Task[WorkersCount];
            for (var i = 1; i <= WorkersCount; i++)
            {
                var timeOut = WorkersCount + (WorkersCount / i * 2);
                var worker = new WorkerProcess("P#" + i.ToString(), timeOut, Cts.Token);
                Debug.WriteLine("P#" + i.ToString() + " " + timeOut);
                workers[i - 1] = worker.Start();
            }

            Console.WriteLine($"All workers started [{WorkersCount}]\nShould be automatically stopped at {_timeToBeCompleted.ToLongTimeString()}");

            // Wait

            while (!_completedByTime)
            {
                Thread.Sleep(100);
            }

            // Stop

            Cts.Cancel();
            Task.WaitAll(workers);

            Console.WriteLine($"\r\nRAM used: {Process.GetCurrentProcess().WorkingSet64 / BytesInMb} MB");
            Console.WriteLine("Press any key to close window");
            Console.ReadKey(true);

            Release();
        }

        private static void InitTimer()
        {
            _aTimer = new System.Timers.Timer()
            {
                Interval = 100,
                Enabled = true,
            };
            _aTimer.Elapsed += OnTimedEvent;
        }

        private static void Release()
        {
            Cts?.Dispose();
            _aTimer?.Dispose();
            WorkerProcess.Release();
        }
        
        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (_timeToBeCompleted > DateTime.Now) return;
            _completedByTime = true;
            _aTimer.Enabled = false;
            Console.WriteLine("----------------------Terminating Process by time...");
            Console.WriteLine($"\r\nRAM used: {Process.GetCurrentProcess().WorkingSet64 / BytesInMb} MB");
        }
    }
}