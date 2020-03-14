using System;
using System.Diagnostics;
using System.Threading;
using System.Timers;

namespace CandidateTest.Threads
{
    internal static class Program
    {
        #region Constants

        private const int WorkersCount = 200;
        private const int WorkTimeInMinutes = 3;
        private const int BytesInMb = 1024 * 1024;

        #endregion

        #region Fields

        private static readonly CancellationTokenSource Cts = new CancellationTokenSource();
        private static DateTime _timeToBeCompleted;
        private static bool _completedByTime;
        private static System.Timers.Timer _aTimer;

        #endregion

        private static void Main(string[] args)
        {
            // Prepare

            InitTimer();
            
            Console.WriteLine("Should be automatically stopped at " + _timeToBeCompleted.ToShortTimeString());
            var currentProcess = Process.GetCurrentProcess();

            Console.WriteLine("Press ESCAPE to stop the process.");
            Console.WriteLine($"RAM used: {currentProcess.WorkingSet64 / BytesInMb} MB");
            Console.ReadKey();

            // Start

            for (var i = 1; i <= WorkersCount; i++)
            {
                var timeOut = WorkersCount + (WorkersCount / i * 2);
                var p = new WorkerProcess("P#" + i.ToString(), timeOut, Cts.Token);
                Debug.WriteLine("P#" + i.ToString() + " " + timeOut);
                p.Start();
            }

            Debug.WriteLine("All Started");

            // Stop

            while (Console.ReadKey(true).Key == ConsoleKey.Escape || _completedByTime)
            {
                var currentProcessExit = Process.GetCurrentProcess();
                Console.WriteLine("----------------------Terminating Process ESCAPE...");
                Console.WriteLine($"\r\nRAM used: {currentProcessExit.WorkingSet64 / BytesInMb} MB");
                
                _aTimer.Enabled = false;
                Cts.Cancel();

                break;
            }
            
            Console.WriteLine("Press ANY KEY");
            Console.ReadKey();

            Release();
        }

        #region Private Methods

        private static void InitTimer()
        {
            _aTimer = new System.Timers.Timer();
            _aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            _aTimer.Interval = 100;
            _aTimer.Enabled = true;

            _timeToBeCompleted = DateTime.Now.AddMinutes(WorkTimeInMinutes);
        }

        private static void Release()
        {
            Cts?.Dispose();
            _aTimer?.Dispose();
            WorkerProcess.Release();
        }

        #endregion

        #region Callbacks

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (_timeToBeCompleted > DateTime.Now) return;
            var currentProcessExit = Process.GetCurrentProcess();
            Console.WriteLine("----------------------Terminating Process by time...");
            Console.WriteLine($"\r\nRAM used: {currentProcessExit.WorkingSet64 / BytesInMb} MB");
            _aTimer.Enabled = false;
            _completedByTime = true;
        }

        #endregion
    }
}