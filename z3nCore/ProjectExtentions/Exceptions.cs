using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public class TracedException : Exception
    {
        public TracedException(Exception innerException, string caller = null)
            : base(BuildMessage(innerException, caller), innerException)
        {
        }

        private static string BuildMessage(Exception innerException, string caller)
        {
            string methodInfo = "Unknown";
            try
            {
                var trace = new StackTrace(innerException, true);
                var frame = trace.GetFrame(0);
                if (frame != null)
                {
                    var method = frame.GetMethod();
                    if (method != null)
                    {
                        var methodName = method.Name;
                        var className = method.DeclaringType?.Name ?? "Unknown";
                        methodInfo = $"{className}.{methodName}";
                    }
                }
            }
            catch
            {
            }

            string callerInfo = caller ?? "Unknown";
            try
            {
                var callerTrace = new StackTrace(true);
                for (int i = 0; i < callerTrace.FrameCount; i++)
                {
                    var frame = callerTrace.GetFrame(i);
                    var method = frame?.GetMethod();
                    if (method?.Name == caller && method.DeclaringType != null)
                    {
                        callerInfo = $"{method.DeclaringType.Name}.{caller}";
                        break;
                    }
                }
            }
            catch
            {
            }

            return $"{innerException.Message} (in {methodInfo}, called from {callerInfo})";
        }
    }

    public static class ExceptionExtensions
    {
        public static string Throw(this Exception ex, IZennoPosterProjectModel project,
            [CallerMemberName] string caller = null, bool throwEx = true)
        {
            var tracedEx = new TracedException(ex, caller);
            project.SendWarningToLog(tracedEx.Message);
            if (throwEx) 
                throw tracedEx;
            else 
                return tracedEx.Message;
        }

        public static Exception Throw(this Exception ex,
            [CallerMemberName] string caller = null)
        {
            var tracedEx = new TracedException(ex, caller);
            throw tracedEx;            
        }
        
        public static Exception Throw(this IZennoPosterProjectModel project, string exMessage = "",
            [CallerMemberName] string caller = null)
        {
            var previous = project.LastExecutedActionId;
            var ex = new Exception(exMessage);
            var tracedEx = new TracedException(ex, caller);
            project.SendWarningToLog($"Err: [{exMessage}] after [{previous}]", true);
            throw tracedEx;
        }
        public static Exception Throw(this IZennoPosterProjectModel project, Exception ex,
            [CallerMemberName] string caller = null)
        {
            var previous = project.LastExecutedActionId;
            
            var tracedEx = new TracedException(ex, caller);
            project.SendWarningToLog($"Err: [{ex.Message}] after [{previous}]", true);
            throw tracedEx;
        }

    }


}
