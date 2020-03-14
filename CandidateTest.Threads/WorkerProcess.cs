﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CandidateTest.Threads
{
    public class WorkerProcess
    {
        private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
        private static ConcurrentBag<KeyValuePair<string, string>> _data;

        private event Action<string> OnError = delegate { };

        private readonly CancellationToken _token;
        private int _cnt = 0;

        private static ConcurrentBag<KeyValuePair<string, string>> Data
        {
            get => _data ??= new ConcurrentBag<KeyValuePair<string, string>>();
            set => _data = value;
        }

        private string ProcessName { get; }

        private int TimeOut { get; }

        public WorkerProcess(string processName, int timeOut, CancellationToken token)
        {
            ProcessName = string.IsNullOrWhiteSpace(processName) ? "Noname Process" : processName;
            TimeOut = timeOut > 0 ? timeOut : 500;
            _token = token;
            _cnt = 0;
            OnError += WriteError;
        }

        public static void Release()
        {
            Semaphore?.Dispose();
        }

        public Task Start()
        {
            return Task.Run(async () =>
            {
                while (!_token.IsCancellationRequested)
                {
                    _cnt++;
                    try
                    {
                        Data.Add(new KeyValuePair<string, string>(
                            ProcessName,
                            DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                            + " \t TimeOut : "
                            + TimeOut.ToString()
                            + " \t\t "
                            + ProcessName + "(" + _cnt + ")"
                            + Environment.NewLine));
                        var lineForWrite = Data.FirstOrDefault(x => x.Key == ProcessName).Value;
                        if (!string.IsNullOrEmpty(lineForWrite))
                        {
                            try
                            {
                                var passedData = new UTF8Encoding(true).GetBytes(lineForWrite);
                                await Semaphore.WaitAsync().ConfigureAwait(false);
                                using var fs = File.Open("Output\\data.txt", FileMode.Append, FileAccess.Write);
                                await fs.WriteAsync(passedData, 0, passedData.Length).ConfigureAwait(false);
                                await SaveStatisticsAsync().ConfigureAwait(false);
                            }
                            finally
                            {
                                Semaphore.Release();
                            }
                        }
                        await Task.Delay(TimeOut).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        OnError(ex.Message);
                    }
                }
            }, _token);
        }

        private static async Task SaveStatisticsAsync()
        {
            var sb = new StringBuilder();
            using var fs = File.Open("Output\\Statistics.txt", FileMode.Open, FileAccess.Write);
            var stat = Data.GroupBy(x => x.Key).OrderBy(x => x.Key).ToList();
            sb.AppendFormat("{0,10} | {1,10}", "Process", "Count")
                .AppendLine()
                .AppendFormat(new string('-', 24))
                .AppendLine();
            foreach (var process in stat)
            {
                sb.AppendFormat("{0,10} | {1,10}", process.Key, process.Count())
                    .AppendLine();
            }
            var toSave = new UTF8Encoding(true).GetBytes(sb.ToString());
            await fs.WriteAsync(toSave, 0, toSave.Length).ConfigureAwait(false);
        }

        private void WriteError(string message)
        {
            Console.WriteLine(message);
            Data.Add(new KeyValuePair<string, string>("Error", message.Trim()));
        }
    }
}