namespace z3nCore
{
    using z3nCore.Orchestration;
    using ZennoLab.CommandCenter;
    using ZennoLab.InterfacesLibrary.ProjectModel;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Init class - maintains backward compatibility with existing code
    /// Now acts as a facade to the new orchestrated architecture
    /// </summary>
    public class Init
    {
        private readonly ProjectOrchestrator _orchestrator;
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly bool _showLog;

        public Init(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
            _showLog = log;
            _orchestrator = new ProjectOrchestrator(project, instance, log);
        }

        // Delegate all methods to orchestrator
        public void InitVariables(string author = "")
        {
            _orchestrator.InitVariables(author);
        }

        public void InitProject(string author = "w3bgr3p", string[] customQueries = null, bool log = false)
        {
            _orchestrator.InitProject(author, customQueries, log);
        }

        public void PrepareProject(bool log = false)
        {
            _orchestrator.PrepareProject(log);
        }

        public void PrepareInstance()
        {
            _orchestrator.PrepareInstance();
        }

        public bool RunProject(List<string> additionalVars = null, bool add = true)
        {
            return _orchestrator.RunProject(additionalVars, add);
        }

        public string LoadSocials(string requiredSocial)
        {
            _orchestrator.LoadSocials(requiredSocial);
            return requiredSocial;
        }

        public string LoadWallets(string walletsToUse)
        {
            _orchestrator.LoadWallets(walletsToUse);
            return walletsToUse;
        }
    }

    /// <summary>
    /// Main class - maintains backward compatibility
    /// </summary>
    public class Main
    {
        private readonly ProjectOrchestrator _orchestrator;
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;

        public Main(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
            _orchestrator = new ProjectOrchestrator(project, instance, log);
        }

        public void FinishSession()
        {
            _orchestrator.FinishSession();
        }
    }
}