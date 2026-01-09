using System;
using System.CodeDom;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    
    
    public class Time
    {
        public class Deadline
        {
            private long Init { get; set; }

            public Deadline()
            {
                Reset();
            }
            public double Check(double limitSec)
            {
                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                double differenceMs = (double)(currentTime - Init);
                double differenceSec = differenceMs / 1000.0;
        
                if (differenceSec > limitSec) 
                    throw new TimeoutException($"Deadline Exception: {limitSec}s");
        
                return differenceSec;
            }
            public void Reset()
            {
                Init = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            
        }


        private static readonly object LockObject = new object();
        public static string Now(string format = "unix") // unix|iso
        {
            lock (LockObject)
            {
                if (format == "unix") return ((long)((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds)).ToString();   //Unix Epoch
                else if (format == "iso") return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"); // ISO 8601 
                else if (format == "short") return DateTime.UtcNow.ToString("MM-ddTHH:mm");
                else if (format == "utcToId") return (DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString();
                throw new ArgumentException("Invalid format. Use: 'unix|iso|short|UtcNow'");
            }
        }
        public static string Cd(object input = null, string o = "iso")
        {
            DateTime t = DateTime.UtcNow;
            if (input == null)
            {
                t = t.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
            }
            else if (input is decimal || input is int)
            {
                decimal minutes = Convert.ToDecimal(input);
                if (minutes == 0m) minutes = 999999999m;
                long secondsToAdd = (long)Math.Round(minutes * 60);
                t = t.AddSeconds(secondsToAdd);
            }
            else if (input is string timeString)
            {
                TimeSpan parsedTime = TimeSpan.Parse(timeString);
                t = t.Add(parsedTime);
            }

            if (o == "unix") 
                return ((long)(t - new DateTime(1970, 1, 1)).TotalSeconds).ToString();
            else if (o == "iso") 
                return t.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"); // ISO 8601
            else
                throw new ArgumentException($"unexpected format {o}");
        }
        public static long Elapsed(long startTime = 0, bool useMs = false)
        {
            if (startTime != 0)
            {
                long currentTime = useMs 
                    ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return (currentTime - startTime);
            }
            else
            {
                return useMs 
                    ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
        }
        
    }

    public static partial class ProjectExtensions
    {
        public static int TimeElapsed(this IZennoPosterProjectModel project, string varName = "varSessionId")
        {
            var start = project.Variables[varName].Value;
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long startTime = long.Parse(start);
            int difference = (int)(currentTime - startTime);

            return difference;
        }
        public static T Age<T>(this IZennoPosterProjectModel project)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            long start;
            try
            {
                start = long.Parse(project.Variables["varSessionId"].Value);
            }
            catch
            {
                project.Variables["varSessionId"].Value = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                start = long.Parse(project.Variables["varSessionId"].Value);
            }

            long Age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - start;


            if (typeof(T) == typeof(string))
            {
                string result = TimeSpan.FromSeconds(Age).ToString();
                return (T)(object)result;
            }
            else if (typeof(T) == typeof(TimeSpan))
            {
                TimeSpan result = TimeSpan.FromSeconds(Age);
                return (T)(object)result;
            }
            else
            {
                return (T)Convert.ChangeType(Age, typeof(T));
            }

        }
        public static void TimeOut(this IZennoPosterProjectModel project, int min = 0)
        {
            if (min == 0) {
                try
                {
                    min = int.Parse(project.Var("timeOut"));
                }
                catch 
                { 
                    throw new ArgumentException("value not provided"); }
                }
            if (project.TimeElapsed() > 60 * min)
                throw new Exception($"GlobalTimeout {min}min, after {project.LastExecutedActionId}");
        }
        public static int Deadline(this IZennoPosterProjectModel project, int sec = 0, bool log = false)
        {
            if (sec != 0)
            {
                var start = project.Variables[$"t0"].Value;
                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long startTime = long.Parse(start);
                int difference = (int)(currentTime - startTime);
                if (difference > sec) throw new Exception($"Deadline Exception: {sec}s, after {project.LastExecutedActionId}");
                if (log) project.log($"{difference}s");
                return difference;
            }
            else
            {
                project.Variables["t0"].Value = (DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString();
                return 0;
            }
        }
        public static void Sleep(this IZennoPosterProjectModel project, int min = 0, int max = 0)
        {
            
            if (max == 0)
                Thread.Sleep(project.RndInt("cfgDelay") * 1000);
            else
                Thread.Sleep(new Random().Next(min, max) * 1000);
        }
        
        public static long Elapsed(this IZennoPosterProjectModel project, long startTime = 0L, bool log = true, bool useMs = false)
        {
            long result = Time.Elapsed(startTime, useMs:useMs);
    
            if (log && startTime != 0)
            {
                string unit = useMs ? "ms" : "s";
                project.log($"{result}{unit}");
            }
            return result;
        }

        public static long Elapsed(this IZennoPosterProjectModel project, string varName = "varSessionId", bool useMs = false)
        {
            var start = project.Variables[varName].Value;
            long startTime = long.Parse(start);
    
            long currentTime = useMs 
                ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    
            return currentTime - startTime;
        }

        public static void StartSession(this IZennoPosterProjectModel project) 
        {
            project.Var("varSessionId", (DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString());
        }
        
    }

}
