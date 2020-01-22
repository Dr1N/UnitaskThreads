using System;
using System.Diagnostics;
using System.Threading;
using System.Timers;

namespace CandidateTest.Threads
{
    class Program
    {
        #region Constants

        private const int WorkersCount = 200;
        private const int WorkTimeInMinutes = 3;
        private const int BytesInMb = 1024 * 1024;

        #endregion

        #region Fields

        static readonly CancellationTokenSource cts = new CancellationTokenSource();
        static DateTime timeToBeCompleted;
        static bool completedByTime;
        static System.Timers.Timer aTimer;

        #endregion

        static void Main(string[] args)
        {
            // Prepare

            InitTimer();
            
            Console.WriteLine("Should be automatically stopped at " + timeToBeCompleted.ToShortTimeString());
            Process currentProcess = Process.GetCurrentProcess();

            Console.WriteLine("Press ESCAPE to stop the process.");
            Console.WriteLine(string.Format("RAM used: {0} MB", currentProcess.WorkingSet64 / BytesInMb));
            Console.ReadKey();

            // Start

            for (int i = 1; i <= WorkersCount; i++)
            {
                WorkerProcess p = new WorkerProcess("P#" + i.ToString(), WorkersCount + (WorkersCount / i * 2), cts.Token);
                p.Start();
            }

            // Stop

            while (Console.ReadKey(true).Key == ConsoleKey.Escape || completedByTime)
            {
                Process currentProcessExit = Process.GetCurrentProcess();
                Console.WriteLine("----------------------Terminating Process ESCAPE...");
                Console.WriteLine(string.Format("\r\nRAM used: {0} MB", currentProcessExit.WorkingSet64 / BytesInMb));
                
                aTimer.Enabled = false;
                cts.Cancel();

                break;
            }

            Release();
            Console.WriteLine("Press ANY KEY");
            Console.ReadKey();
        }

        #region Private Methods

        private static void InitTimer()
        {
            aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = 100;
            aTimer.Enabled = true;

            timeToBeCompleted = DateTime.Now.AddMinutes(WorkTimeInMinutes);
        }

        private static void Release()
        {
            cts?.Dispose();
            aTimer?.Dispose();
            WorkerProcess.Release();
        }

        #endregion

        #region Callbacks

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (timeToBeCompleted <= DateTime.Now)
            {
                Process currentProcessExit = Process.GetCurrentProcess();
                Console.WriteLine("----------------------Terminating Process by time...");
                Console.WriteLine(string.Format("\r\nRAM used: {0} MB", currentProcessExit.WorkingSet64 / BytesInMb));
                aTimer.Enabled = false;
                completedByTime = true;
            }
        }

        #endregion
    }
}