using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System;
using ZennoLab.InterfacesLibrary.Enums.Browser;

namespace z3nCore
{
    public class Main
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;

        public Main(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
        }
        public void FinishSession()
        {
            string acc0 = _project.Var("acc0");
            string accRnd = _project.Var("accRnd");
            
            try
            {
                if (!string.IsNullOrEmpty(acc0))
                {
                    new Reporter(_project, _instance).SuccessReport(true, true);
                }
            }
            catch (Exception ex)
            {
                _project.L0g(ex.Message);
            }
            if (ShouldSaveCookies(_instance, acc0, accRnd))
            {
                new Cookies(_project, _instance).Save("all", _project.Var("pathCookies"));
            }
            ClearAccountState(_project, acc0);
        }
        private static bool ShouldSaveCookies(Instance instance, string acc0, string accRnd)
        {
            try
            {
                return instance.BrowserType == BrowserType.Chromium && 
                       !string.IsNullOrEmpty(acc0) && 
                       string.IsNullOrEmpty(accRnd);
            }
            catch
            {
                return false;
            }
        }
        private static void ClearAccountState(IZennoPosterProjectModel project, string acc0)
        {
            if (!string.IsNullOrEmpty(acc0))
            {
                project.GVar($"acc{acc0}", "");
            }
            project.Var("acc0", "");
        }
    }
}