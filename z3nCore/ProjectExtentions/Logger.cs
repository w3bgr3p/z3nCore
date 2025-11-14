using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using ZennoLab.InterfacesLibrary.Enums.Log;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System.Text;
using System.IO;
using z3nCore;

namespace z3nCore
{
    public class Logger
    {
        private readonly IZennoPosterProjectModel _project;
        private bool _logShow = false;
        private string _emoji = null;
        private readonly bool _persistent;
        private readonly long _t0;

        public Logger(IZennoPosterProjectModel project, bool log = false, string classEmoji = null, bool persistent = true)
        {
            _project = project;
            _logShow = log || _project.Var("debug") == "True";
            _emoji = classEmoji;
            _persistent = persistent;
            _t0 = Time.Elapsed(useMs:true);
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

        public void Send(object toLog,
            [CallerMemberName] string callerName = "",
            bool show = false, bool thrw = false, bool toZp = true,
            int cut = 0, bool wrap = true,
            LogType type = LogType.Info, LogColor color = LogColor.Default)
        {
            var (acc, port, time, memory, caller, wrapFlag, force) = GetConfigFlags();
            
            if (force)
            {
                show = true;
                toZp = true;
            }

            if (!show && !_logShow) return;
            
            string header = string.Empty;
            string body = toLog?.ToString() ?? "null";

            if (wrapFlag)
            {
                var stackFrame = new System.Diagnostics.StackFrame(1);
                header = LogHeader(stackFrame, callerName, acc, port, time, memory, caller);
                body = LogBody(body, cut);
            }
            
            string toSend = header + body;
            if (toSend.Contains("!W")) type = LogType.Warning;
            if (toSend.Contains("!E")) type = LogType.Error;
            Execute(toSend, type, color, toZp, thrw);
        }
        public void Warn(object toLog, [CallerMemberName] string callerName = "", bool show = false, bool thrw = false, bool toZp = true, int cut = 0, bool wrap = true, LogColor color = LogColor.Default)
        {
            Send (toLog, callerName, show, thrw, toZp, cut, wrap, type:LogType.Warning, color: color);
        }

        private string LogHeader(System.Diagnostics.StackFrame stackFrame, string callerName, bool acc, bool port, bool time, bool memory, bool caller)
        {
            var sb = new StringBuilder(256);
            
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
        
                    if (_persistent) 
                    {
                        long elapsedMs = Time.Elapsed(_t0,useMs:true); 
                        double inSeconds = elapsedMs / 1000.0; 
                        sb.Append($"[{inSeconds:G}s]");
                    }
                }
                catch { }
                
            if (memory)
                try
                {
                    sb.Append(GetMemoryInfo());
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
                    if (callingMethod == null || 
                        callingMethod.DeclaringType == null || 
                        callingMethod.DeclaringType.FullName.Contains("Zenno") || 
                        callingMethod.Name == "log" || 
                        System.Text.RegularExpressions.Regex.IsMatch(callingMethod.DeclaringType.Name, @"^M[a-f0-9]{32}$") ||
                        System.Text.RegularExpressions.Regex.IsMatch(callingMethod.Name, @"^M[a-f0-9]{32}$"))   
                        sb.Append($"  🔳 [{_project.Name.Replace(".zp", "")}]");
                    else
                        sb.Append($"  🔲 [{callingMethod.DeclaringType.Name}.{callerName}]");
                }
                catch { }

            return sb.Length > 0 ? sb.ToString() : string.Empty;
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
                        
                        //var sb = new StringBuilder(toLog.Length);
                        //for (int i = 0; i < toLog.Length; i++)
                        //{
                        //    char c = toLog[i];
                        //   if (c == '\r' || c == '\n')
                        //      sb.Append(' ');
                        //  else
                        //     sb.Append(c);
                        //}
                        //toLog = sb.ToString();
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

        private void Execute(string toSend, LogType type, LogColor color, bool toZp, bool thrw)
        {
            _project.SendToLog(toSend, type, toZp, color);
            if (thrw) throw new Exception($"{toSend}");
        }
        
    }
}


public static partial class ProjectExtensions
{
    public static void log(this IZennoPosterProjectModel project, object toLog, [CallerMemberName] string callerName = "", bool show = true, bool thrw = false, bool toZp = true)
    {
        if (System.Text.RegularExpressions.Regex.IsMatch(callerName, @"^M[a-f0-9]{32}$")) callerName = project.Name;
        new Logger(project,persistent:false).Send(toLog, callerName, show: show, thrw: thrw, toZp: toZp);
    }
    public static void warn(this IZennoPosterProjectModel project, string toLog, bool thrw = false, [CallerMemberName] string callerName = "", bool show = true,  bool toZp = true)
    {
        new Logger(project).Warn(toLog, callerName, show: show, thrw: thrw, toZp: toZp);
    }
    public static void warn(this IZennoPosterProjectModel project, Exception ex, bool thrw = false, [CallerMemberName] string callerName = "", bool show = true,  bool toZp = true)
    {
        new Logger(project).Warn(ex.Message, callerName, show: show, thrw: thrw, toZp: toZp);
    }

    
    internal static void ObsoleteCode(this IZennoPosterProjectModel project, string newName = "unknown")
    {
        try
        {
            if (project == null) return;

            var sb = new System.Text.StringBuilder();

            try
            {
                var trace = new System.Diagnostics.StackTrace(1, true);
                string oldName = "";
                string callerName = "";
                
                for (int i = 0; i < trace.FrameCount; i++)
                {
                    var frame = trace.GetFrame(i);
                    var method = frame?.GetMethod();
                    if (method == null || method.DeclaringType == null) continue;

                    var typeName = method.DeclaringType.FullName;
                    if (string.IsNullOrEmpty(typeName)) continue;

                    if (typeName.StartsWith("System.") || typeName.StartsWith("ZennoLab.")) continue;

                    var methodName = $"{typeName}.{method.Name}";
                    
                    if (i == 0) 
                    {
                        oldName = methodName;
                    }
                    else
                    {
                        callerName = methodName;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(callerName) || callerName == "z3nCore.Init.RunProject" ) 
                    callerName = Path.Combine(project.Path,project.Name);
                

                sb.Append($"![OBSOLETE CODE]. Obsolete method: [{oldName}] called from: [{callerName}]");
                if (string.IsNullOrEmpty(newName))  sb.Append($". Use: [{newName}] instead");
                
                project.SendWarningToLog(sb.ToString().Trim(), true);
            }
            catch (Exception ex)
            {
                try
                {
                    project.SendToLog($"!E WarnObsolete logging failed: {ex.Message}", LogType.Error, true, LogColor.Red);
                }
                catch { }
            }
        }
        catch { }
    }
}