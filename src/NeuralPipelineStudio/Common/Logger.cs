using System;
using System.Collections.Concurrent;

namespace NeuralPipelineStudio.Common
{
    public static class Logger
    {
        public static event Action<string>? OnLogAdded;
        private static readonly ConcurrentQueue<string> _logs = new();

        public static void Log(string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _logs.Enqueue(line);
            OnLogAdded?.Invoke(line);
        }

        public static string[] GetAllLogs() => _logs.ToArray();
    }
}
