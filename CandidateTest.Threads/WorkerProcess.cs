using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace CandidateTest.Threads
{
    public class WorkerProcess
    {
        #region Static Fields

        private static readonly ReaderWriterLockSlim _dataWriteLock = new ReaderWriterLockSlim();
        private static readonly ReaderWriterLockSlim _statisticWriteLock = new ReaderWriterLockSlim();
        private static ConcurrentBag<KeyValuePair<string, string>> _data;
        private static string _statistics;

        #endregion

        #region Events

        private delegate void RetryHandler(string message);
        private event RetryHandler OnRetry;

        #endregion

        #region Fields

        private readonly CancellationTokenSource _cts;
        private bool isCompleted;
        private int _cnt = 0;
        private byte[] _passedData;

        #endregion

        #region Properties

        public static ConcurrentBag<KeyValuePair<string, string>> Data
        {
            get
            {
                if (_data == null)
                {
                    _data = new ConcurrentBag<KeyValuePair<string, string>>();
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

        #endregion

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
                        _dataWriteLock.EnterWriteLock();
                        using (var fs = File.Open("Output\\data.txt", FileMode.Append))
                        {
                            Data.Add(new KeyValuePair<string, string>(ProcessName, 
                                DateTime.UtcNow.ToString() 
                                + " \t TimeOut : " 
                                + TimeOut.ToString() 
                                + " \t\t " 
                                + ProcessName + "(" + _cnt + ")" 
                                + Environment.NewLine));
                            Console.WriteLine($"{ProcessName}: {_cnt}");
                            var last = Data.FirstOrDefault(x => x.Key == ProcessName).Value;
                            _passedData = new UTF8Encoding(true).GetBytes(last);
                            byte[] bytes = _passedData;
                            fs.Write(bytes, 0, bytes.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        OnRetry += Retry;
                        OnRetry(ex.Message);
                    }
                    finally
                    {
                        _dataWriteLock.ExitWriteLock();
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
                _statisticWriteLock.EnterWriteLock();
                using (var fs = File.Open("Output\\Statistics.txt", FileMode.Open))
                {
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
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                _statisticWriteLock.ExitWriteLock();
            }
        }
    }
}