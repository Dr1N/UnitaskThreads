using System;
using System.Diagnostics;
using System.Threading;
using System.Timers;

namespace CandidateTest.Threads
{
    class Program
    {
        #region Constants

        private const int WorkersCount = 1;
        private const int WorkTimeInMinutes = 1;

        #endregion

        #region Fields

        static readonly CancellationTokenSource cts = new CancellationTokenSource();
        static DateTime timeToBeCompleted;
        static bool completedByTime;
        static System.Timers.Timer aTimer;

        #endregion

        static void Main(string[] args)
        {
            InitTimer();
            
            Console.WriteLine("Should be automatically stopped at " + timeToBeCompleted.ToShortTimeString());
            Process currentProcess = Process.GetCurrentProcess();

            Console.WriteLine("Press ESCAPE to stop the process.");
            Console.WriteLine(string.Format("RAM used: {0} MB", currentProcess.WorkingSet64 / 1024 / 1024));
            Console.ReadKey();
            Console.WriteLine("Start Workers...");

            for (int i = 1; i <= WorkersCount; i++)
            {
                WorkerProcess p = new WorkerProcess("P#" + i.ToString(), WorkersCount + (WorkersCount / i * 2), cts);
                p.Start();
            }

            Console.WriteLine("Workers started");

            while (Console.ReadKey(true).Key == ConsoleKey.Escape && !completedByTime)
            {
                Process currentProcessExit = Process.GetCurrentProcess();
                Console.WriteLine("----------------------Terminating Process ESCAPE...");
                Console.WriteLine(string.Format("\r\nRAM used: {0} MB", currentProcessExit.WorkingSet64 / 1024 / 1024));
                
                aTimer.Enabled = false;
                cts.Cancel();
            }
           
            Console.WriteLine("Press ANY KEY");
            Console.ReadKey();
        }

        private static void InitTimer()
        {
            aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = 100;
            aTimer.Enabled = true;

            timeToBeCompleted = DateTime.Now.AddMinutes(WorkTimeInMinutes);
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (timeToBeCompleted <= DateTime.Now)
            {
                Process currentProcessExit = Process.GetCurrentProcess();
                Console.WriteLine("----------------------Terminating Process by time...");
                Console.WriteLine(string.Format("\r\nRAM used: {0} MB", currentProcessExit.WorkingSet64 / 1024 / 1024));
                cts.Cancel();
                aTimer.Enabled = false;
                completedByTime = true;
            }
        }
    }
}