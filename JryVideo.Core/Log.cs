﻿using System;
using System.IO;
using System.Threading.Tasks;

namespace JryVideo.Core
{
    public sealed class Log
    {
        private const string LogFileName = "history.log";
        private static readonly object rootSync = new object();

        public static void BeginWrite(string log) => Task.Run(() => Write(log));

        public static async Task WriteAsync(string log) => await Task.Run(() => Write(log));

        public static void Write(string log)
        {
            lock (rootSync)
            {
                try
                {
                    using (var stream = File.Open(LogFileName, FileMode.Append))
                    {
                        using (var writer = new StreamWriter(stream))
                        {
                            var time = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
                            writer.WriteLine($"[{time}] {log ?? string.Empty}");
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}