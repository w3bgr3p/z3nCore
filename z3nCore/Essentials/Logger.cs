using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using ZennoLab.InterfacesLibrary.Enums.Log;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System.Text;
using System.IO;

namespace z3nCore
{
    public class Logger
    {
        protected readonly IZennoPosterProjectModel _project;
        protected bool _logShow = false;
        private string _emoji = null;
        private static readonly Process CurrentProcess = Process.GetCurrentProcess();

        private static readonly Dictionary<string, LogColor> ColorMap = new Dictionary<string, LogColor> { 
            { "`.", LogColor.Default },
            { "`w", LogColor.Gray },
            { "`y", LogColor.Yellow },
            { "`o", LogColor.Orange },
            { "`r", LogColor.Red },
            { "`p", LogColor.Pink },
            { "`v", LogColor.Violet },
            { "`b", LogColor.Blue },
            { "`lb", LogColor.LightBlue },
            { "`t", LogColor.Turquoise },
            { "`g", LogColor.Green },
            { "!W", LogColor.Orange },
            { "!E", LogColor.Orange },
            { "relax", LogColor.LightBlue },              
            { "Err", LogColor.Orange },
        };

        public Logger(IZennoPosterProjectModel project, bool log = false, string classEmoji = null)
        {
            _project = project;
            _logShow = log || _project.Var("debug") == "True";
            _emoji = classEmoji;
        }

        private (bool acc, bool port, bool time, bool memory, bool caller, bool wrap, bool force) GetConfigFlags()
        {
            bool acc = false, port = false, time = false, memory = false, caller = false, wrap = false, force = false;
            
            var flags = _project.Var("cfgLog").Split(',').Select(x => x.Trim()).ToList();
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

        public void Send(string toLog, [CallerMemberName] string callerName = "", bool show = false, bool thr0w = false, bool toZp = true, int cut = 0, bool wrap = true)
        {
            var (acc, port, time, memory, caller, wrapFlag, force) = GetConfigFlags();
            
            if (force)
            {
                show = true;
                toZp = true;
            }

            if (!show && !_logShow) return;
            
            string header = string.Empty;
            string body = toLog;

            if (wrapFlag)
            {
                var stackFrame = new System.Diagnostics.StackFrame(1);
                header = LogHeader(stackFrame, callerName, acc, port, time, memory, caller);
                body = LogBody(toLog, cut);
            }
            
            string toSend = header + body;
            (LogType type, LogColor color) = LogColour(header, toLog);
            Execute(toSend, type, color, toZp, thr0w);
        }

        private string LogHeader(System.Diagnostics.StackFrame stackFrame, string callerName, bool acc, bool port, bool time, bool memory, bool caller)
        {
            var sb = new StringBuilder(128);
            
            if (acc)
                try
                {
                    string acc0 = _project.Var("acc0");
                    if (!string.IsNullOrEmpty(acc0)) sb.Append($"  🤖 [{acc0}]");
                }
                catch { }
                
            if (time)
                try
                {
                    string totalAge = _project.Age<string>();
                    if (!string.IsNullOrEmpty(totalAge)) sb.Append($"  ⏱️ [{totalAge}]");
                }
                catch { }
                
            if (memory)
                try
                {
                    CurrentProcess.Refresh(); 
                    long processMemory = CurrentProcess.WorkingSet64 / 1024 / 1024;
                    sb.Append("  📊 [").Append(processMemory).Append(" Mb]");
                }
                catch { }
                
            if (port)
                try
                {
                    string portValue = _project.Var("instancePort");
                    if (!string.IsNullOrEmpty(portValue)) sb.Append($"  🔌 [{portValue}]");
                }
                catch { }
                
            if (caller)
                try
                {
                    var callingMethod = stackFrame.GetMethod();
                    if (callingMethod == null || callingMethod.DeclaringType == null || callingMethod.DeclaringType.FullName.Contains("Zenno") || callingMethod.Name == "L0g")   
                        sb.Append($"  🔳 [{_project.Name.Replace(".zp", "")}]");
                    else
                        sb.Append($"  🔲 [{callingMethod.DeclaringType.Name}.{callerName}]");
                }
                catch { }

            return sb.Length > 0 ? sb.ToString() : string.Empty;
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
                        var sb = new StringBuilder(toLog.Length);
                        for (int i = 0; i < toLog.Length; i++)
                        {
                            char c = toLog[i];
                            if (c == '\r' || c == '\n')
                                sb.Append(' ');
                            else
                                sb.Append(c);
                        }
                        toLog = sb.ToString();
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

        private (LogType, LogColor) LogColour(string header, string toLog)
        {
            LogType type = LogType.Info;
            LogColor color = LogColor.Default;

            string combined = (header ?? "") + (toLog ?? "");
            foreach (var pair in ColorMap)
            {
                if (combined.Contains(pair.Key))
                {
                    color = pair.Value;
                    break;
                }
            }

            if (combined.Contains("!W")) type = LogType.Warning;
            if (combined.Contains("!E")) type = LogType.Error;

            return (type, color);
        }

        private void Execute(string toSend, LogType type, LogColor color, bool toZp, bool thr0w)
        {
            _project.SendToLog(toSend, type, toZp, color);
            if (thr0w) throw new Exception($"{toSend}");
        }
        
        
    }
}