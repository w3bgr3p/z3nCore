using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Core
{
    public enum LogMode
    {
        Console,
        File,
        UI
    }

    public class Logger
    {
        private readonly Variables _variables;
        private readonly LogMode _mode;
        private readonly string _filePath;
        private readonly Action<string> _uiAction;
        private bool _logShow = false;
        private string _emoji = null;
        private readonly bool _persistent;
        private readonly long _t0;
        private readonly string _projectName;

        // Конструктор для Console (по умолчанию)
        public Logger(Variables variables, bool log = false, string classEmoji = null, bool persistent = true, string projectName = null)
            : this(variables, LogMode.Console, null, null, log, classEmoji, persistent, projectName)
        {
        }

        // Конструктор для File
        public Logger(Variables variables, string filePath, bool log = false, string classEmoji = null, bool persistent = true, string projectName = null)
            : this(variables, LogMode.File, filePath, null, log, classEmoji, persistent, projectName)
        {
        }

        // Конструктор для UI
        public Logger(Variables variables, Action<string> uiAction, bool log = false, string classEmoji = null, bool persistent = true, string projectName = null)
            : this(variables, LogMode.UI, null, uiAction, log, classEmoji, persistent, projectName)
        {
        }

        // Общий приватный конструктор
        private Logger(Variables variables, LogMode mode, string filePath, Action<string> uiAction, bool log, string classEmoji, bool persistent, string projectName)
        {
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _mode = mode;
            _filePath = filePath;
            _uiAction = uiAction;
            _logShow = log || _variables.GetBool("debug");
            _emoji = classEmoji;
            _persistent = persistent;
            _projectName = projectName ?? _variables.Get("projectName");
            _t0 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private (bool acc, bool port, bool time, bool memory, bool caller, bool wrap, bool force) GetConfigFlags()
        {
            bool acc = false, port = false, time = false, memory = false, caller = false, wrap = false, force = false;

            string cfgLog = _variables.Get("cfgLog");
            if (string.IsNullOrEmpty(cfgLog))
                return (acc, port, time, memory, caller, wrap, force);

            var flags = cfgLog.Split(',').Select(x => x.Trim()).ToList();
            foreach (string flag in flags)
            {
                switch (flag)
                {
                    case "acc":
                        acc = true;
                        continue;
                    case "port":
                        port = true;
                        continue;
                    case "time":
                        time = true;
                        continue;
                    case "memory":
                        memory = true;
                        continue;
                    case "caller":
                        caller = true;
                        continue;
                    case "wrap":
                        wrap = true;
                        continue;
                    case "force":
                        force = true;
                        continue;
                    default:
                        continue;
                }
            }

            return (acc, port, time, memory, caller, wrap, force);
        }

        public void Send(object toLog,
            [CallerMemberName] string callerName = "",
            bool show = false, bool thrw = false,
            int cut = 0, bool wrap = true)
        {
            var (acc, port, time, memory, caller, wrapFlag, force) = GetConfigFlags();

            if (force)
            {
                show = true;
            }

            if (!show && !_logShow) return;

            string header = string.Empty;
            string body = toLog?.ToString() ?? "null";

            if (wrapFlag)
            {
                var stackFrame = new StackFrame(1);
                header = LogHeader(stackFrame, callerName, acc, port, time, memory, caller);
                body = LogBody(body, cut);
            }

            string toSend = header + body;
            Execute(toSend, thrw);
        }

        public void Warn(object toLog, [CallerMemberName] string callerName = "", bool show = false, bool thrw = false, int cut = 0, bool wrap = true)
        {
            var (acc, port, time, memory, caller, wrapFlag, force) = GetConfigFlags();

            if (force)
            {
                show = true;
            }

            if (!show && !_logShow) return;

            string header = string.Empty;
            string body = toLog?.ToString() ?? "null";

            if (wrapFlag)
            {
                var stackFrame = new StackFrame(1);
                header = LogHeader(stackFrame, callerName, acc, port, time, memory, caller);
                body = LogBody(body, cut);
            }

            string toSend = "[WARNING] " + header + body;
            Execute(toSend, thrw);
        }

        private string LogHeader(StackFrame stackFrame, string callerName, bool acc, bool port, bool time, bool memory, bool caller)
        {
            var sb = new StringBuilder(256);

            if (acc)
            {
                try
                {
                    string acc0 = _variables.Get("acc0");
                    if (!string.IsNullOrEmpty(acc0)) sb.Append($"  🤖 [{acc0}]");
                }
                catch { }
            }

            if (time)
            {
                try
                {
                    string totalAge = Age();
                    if (!string.IsNullOrEmpty(totalAge)) sb.Append($"  ⏱️ [{totalAge}]");

                    if (_persistent)
                    {
                        long elapsedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _t0;
                        double inSeconds = elapsedMs / 1000.0;
                        sb.Append($"[{inSeconds:G}s]");
                    }
                }
                catch { }
            }

            if (memory)
            {
                try
                {
                    sb.Append(GetMemoryInfo());
                }
                catch { }
            }

            if (port)
            {
                try
                {
                    string portValue = _variables.Get("instancePort");
                    if (!string.IsNullOrEmpty(portValue)) sb.Append($"  🔌 [{portValue}]");
                }
                catch { }
            }

            if (caller)
            {
                try
                {
                    var callingMethod = stackFrame.GetMethod();
                    if (callingMethod == null ||
                        callingMethod.DeclaringType == null ||
                        System.Text.RegularExpressions.Regex.IsMatch(callingMethod.DeclaringType.Name, @"^M[a-f0-9]{32}$") ||
                        System.Text.RegularExpressions.Regex.IsMatch(callingMethod.Name, @"^M[a-f0-9]{32}$"))
                        sb.Append($"  🔳 [{_projectName}]");
                    else
                        sb.Append($"  🔲 [{callingMethod.DeclaringType.Name}.{callerName}]");
                }
                catch { }
            }

            return sb.Length > 0 ? sb.ToString() : string.Empty;
        }

        private string Age()
        {
            try
            {
                string varSessionIdStr = _variables.Get("varSessionId");
                if (string.IsNullOrEmpty(varSessionIdStr))
                {
                    _variables.Set("varSessionId", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                    varSessionIdStr = _variables.Get("varSessionId");
                }

                long start = long.Parse(varSessionIdStr);
                long age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - start;
                return TimeSpan.FromSeconds(age).ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private string GetMemoryInfo()
        {
            try
            {
                using (var process = Process.GetCurrentProcess())
                {
                    process.Refresh();
                    long processMemory = process.WorkingSet64 / 1024 / 1024;
                    return $"  📊 [{processMemory} Mb]";
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private string LogBody(string toLog, int cut)
        {
            if (!string.IsNullOrEmpty(toLog))
            {
                if (cut != 0)
                {
                    int lineCount = 1;
                    for (int i = 0; i < toLog.Length; i++)
                    {
                        if (toLog[i] == '\n') lineCount++;
                    }

                    if (lineCount > cut)
                    {
                        toLog = toLog.Replace('\r', ' ').Replace('\n', ' ');
                    }
                }

                if (!string.IsNullOrEmpty(_emoji))
                {
                    toLog = $"[ {_emoji} ] {toLog}";
                }
                return $"\n          {toLog.Trim()}";
            }
            return string.Empty;
        }

        private void Execute(string toSend, bool thrw)
        {
            switch (_mode)
            {
                case LogMode.Console:
                    Console.WriteLine(toSend);
                    break;

                case LogMode.File:
                    try
                    {
                        if (!string.IsNullOrEmpty(_filePath))
                        {
                            File.AppendAllText(_filePath, toSend + Environment.NewLine);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to write to log file: {ex.Message}");
                    }
                    break;

                case LogMode.UI:
                    try
                    {
                        _uiAction?.Invoke(toSend + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to write to UI: {ex.Message}");
                    }
                    break;
            }

            if (thrw) throw new Exception($"{toSend}");
        }
    }
}
