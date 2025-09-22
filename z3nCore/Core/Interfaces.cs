// ============================================
// CORE INTERFACES
// ============================================


namespace z3nCore.Interfaces
{
    using System.Collections.Generic;
    public interface IProjectInitializer
    {
        void InitVariables(string author = "");
        void InitProject(string author = "w3bgr3p", string[] customQueries = null, bool log = false);
        void PrepareProject(bool log = false);
        void PrepareInstance();
    }

    public interface IBrowserManager
    {
        void LaunchBrowser(string cfgBrowser = null);
        void SetBrowser(bool strictProxy = true, string cookies = null, bool log = false);
        string LoadSocials(string requiredSocial);
        string LoadWallets(string walletsToUse);
    }

    public interface IAccountManager
    {
        string GetAccByMode();
        bool ChooseSingleAcc(bool oldest = false);
        void FilterAccList(HashSet<string> allAccounts);
        void MakeAccList(List<string> dbQueries);
    }

    public interface IProjectRunner
    {
        bool RunProject(List<string> additionalVars = null, bool add = true);
    }

    public interface ISessionManager
    {
        void FinishSession();
        void ClearAccountState();
    }

    public interface IReportManager
    {
        string ErrorReport(bool toTg = false, bool toDb = false, bool screenshot = false);
        string SuccessReport(bool log = false, bool toTg = false);
        void ToTelegram(string reportString);
    }
}