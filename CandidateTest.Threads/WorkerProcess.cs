using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace CandidateTest.Threads
{
    public class WorkerProcess
    {
        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        private bool isCompleted;
        private int _cnt = 0;
        private static string _statistics;
        private CancellationTokenSource _cts;
        private delegate void RetryHandler(string message);
        private event RetryHandler OnRetry;
        private byte[] _passedData;
        private static List<KeyValuePair<string, string>> _data { get; set; }
        public static List<KeyValuePair<string, string>> Data
        {
            get
            {
                if (_data == null)
                {
                    _data = new List<KeyValuePair<string, string>>();
                }
                return _data;
            }
            set
            {
                _data = value;
            }
        }

        public string ProcessName { get; }
        public int TimeOut { get; }

        public WorkerProcess(string processName, int timeOut, CancellationTokenSource cts)
        {
            ProcessName = string.IsNullOrWhiteSpace(processName) ? "Noname Process" : processName;
            TimeOut = timeOut > 0 ? timeOut : 500;
            _cts = cts;
            _statistics = string.Empty;
            _cnt = 0;
        }

        public void Start()
        {
            var mainThread = new Thread(() =>
            {
                while (!isCompleted)
                {
                    _cnt++;
                    _cts.Token.Register(() =>
                    {
                        isCompleted = true;
                    });
                    try
                    {
                        _readWriteLock.EnterWriteLock();
                        var fs = File.Open("Output\\data.txt", FileMode.Append);
                        Data.Add(new KeyValuePair<string, string>(ProcessName, DateTime.UtcNow.ToString() + " \t TimeOut : " + TimeOut.ToString() + " \t\t " + ProcessName + "(" + _cnt + ")" + Environment.NewLine));
                        _passedData = new UTF8Encoding(true).GetBytes(Data.LastOrDefault(x => x.Key == ProcessName).Value);
                        byte[] bytes = _passedData;
                        fs.Write(bytes, 0, bytes.Length);
                    }
                    catch (Exception ex)
                    {
                        OnRetry += Retry;
                        OnRetry(ex.Message);
                    }
                    finally
                    {
                        _readWriteLock.ExitWriteLock();
                    }
                    Thread.Sleep(TimeOut);
                    var statisticsThread = new Thread(() =>
                    {
                        SaveStatistics();
                    });
                    statisticsThread.Start();
                }
            });

            mainThread.Start();
        }

        private static void Retry(string message)
        {
            Console.WriteLine(message);
            string error = message.Trim();
            Data.Add(new KeyValuePair<string, string>("Error", error));
        }

        public static void SaveStatistics()
        {
            try
            {
                var fs = File.Open("Output\\Statistics.txt", FileMode.Open);
                var stat = Data.GroupBy(x => x.Key).OrderBy(x => x.Key);
                _statistics = String.Format("{0,10} | {1,10}", "Process", "Count") + Environment.NewLine;
                _statistics += new String('-', 24) + Environment.NewLine;
                foreach (var process in stat)
                {
                    _statistics += String.Format("{0,10} | {1,10}", process.Key, process.Count()) + Environment.NewLine;
                }

                var toSave = new UTF8Encoding(true).GetBytes(_statistics);
                fs.Write(toSave, 0, toSave.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
