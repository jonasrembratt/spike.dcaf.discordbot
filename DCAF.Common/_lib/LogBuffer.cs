using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using TetraPak.XP.Logging;
using TetraPak.XP.Logging.Abstractions;

namespace DCAF._lib
{

    [DebuggerDisplay("{ToString()}")]
    public sealed class LogBuffer : LogBase
    {
        readonly int _maxLength;
        readonly List<LogEventArgs> _events = new();
        readonly string _name;

        public int Count => _events.Count;

        public override string ToString() => $"CrashLogBuilder '{_name}' ({Count})";

        public Task WriteToLogAsync(ILog log) => Task.Run(() => WriteToLog(log));

        public void WriteToLog(ILog log)
        {
            using var section = log.Section(_name);
            foreach (var e in _events)
            {
                section.Write(e);
            }
        }

        void onLogged(object? sender, LogEventArgs e)
        {
            if (_events.Count == _maxLength)
            {
                _events.RemoveAt(0);
            }
            _events.Add(e);
        }

        public LogBuffer(string name, int maxLength = 1000) : base(LogRank.Trace)
        {
            _name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentNullException(nameof(name)) : name;
            _maxLength = maxLength;
            Logged += onLogged;
        }
    }
}