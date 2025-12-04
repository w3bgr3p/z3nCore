using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public static partial class Fallback
    {
        public static void ClFlv2(this Instance instance)
        {
            instance.CFSolve();
        }
        public static string ClFl(this Instance instance, int deadline = 60, bool strict = false)
        {
            return instance.CFToken();
        }
        public static void ReportDailyHtml(this IZennoPosterProjectModel project, bool call = false, bool withPid = false)
        {
            project.GenerateFullHtmlReport();
        }



        
    }
}