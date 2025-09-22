using System;
using System.Management; // Для ManagementObjectSearcher и ManagementObject
using System.Linq; 


using System.Collections.Concurrent;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public static class FunctionStorage
    {
        public static ConcurrentDictionary<string, object> Functions = new ConcurrentDictionary<string, object>();
    }


    public interface ISAFU
    {
        string Encode(IZennoPosterProjectModel project, string toEncrypt, bool log);
        string EncodeV2(IZennoPosterProjectModel project, string toEncrypt, bool log);
        string Decode(IZennoPosterProjectModel project, string toDecrypt, bool log);
        string HWPass(IZennoPosterProjectModel project, bool v2);
    }

    internal class SimpleSAFU : ISAFU
    {
        public string Encode(IZennoPosterProjectModel project, string toEncrypt, bool log)
        {

            if (project.Variables["cfgPin"].Value == "") return toEncrypt;
            if (string.IsNullOrEmpty(toEncrypt)) return string.Empty;
            return AES.EncryptAES(toEncrypt, project.Variables["cfgPin"].Value, true);
        }
        public string EncodeV2(IZennoPosterProjectModel project, string toEncrypt, bool log)
        {

            if (project.Variables["cfgPin"].Value == "") return toEncrypt;
            if (string.IsNullOrEmpty(toEncrypt)) return string.Empty;
            return AES.EncryptAES(toEncrypt, project.Variables["cfgPin"].Value, true);
        }

        public string Decode(IZennoPosterProjectModel project, string toDecrypt, bool log)
        {

            if (project.Variables["cfgPin"].Value == "") return toDecrypt;
            if (string.IsNullOrEmpty(toDecrypt)) return string.Empty;
            try
            {
                return AES.DecryptAES(toDecrypt, project.Variables["cfgPin"].Value, true);
            }
            catch (Exception ex)
            {
                project.SendWarningToLog($"[SimpleSAFU.Decode] ERR: [{ex.Message}] key: ['{project.Variables["cfgPin"].Value}']");
                throw;
            }

        }

        public string HWPass(IZennoPosterProjectModel project, bool log)
        {
            string hwmb = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard").Get().Cast<ManagementObject>().First().GetPropertyValue("SerialNumber").ToString();
            string pass = hwmb + project.Var("acc0");
            return pass;
        }
    }
    
    public static class SAFU
    {
        private static readonly SimpleSAFU _defaultSAFU = new SimpleSAFU();

        public static void Initialize(IZennoPosterProjectModel project)
        {
            if (!FunctionStorage.Functions.ContainsKey("SAFU_Encode") ||
                !FunctionStorage.Functions.ContainsKey("SAFU_Decode") ||
                !FunctionStorage.Functions.ContainsKey("SAFU_HWPass"))
            {
                project.SendInfoToLog("using SAFU simple...");
                FunctionStorage.Functions.TryAdd("SAFU_Encode", (Func<IZennoPosterProjectModel, string, bool, string>)_defaultSAFU.Encode);
                FunctionStorage.Functions.TryAdd("SAFU_Decode", (Func<IZennoPosterProjectModel, string, bool, string>)_defaultSAFU.Decode);
                FunctionStorage.Functions.TryAdd("SAFU_HWPass", (Func<IZennoPosterProjectModel, bool, string>)_defaultSAFU.HWPass);
            }
        }

        public static string Encode(IZennoPosterProjectModel project, string toEncrypt, bool log = false)
        {
            if (string.IsNullOrEmpty(project.Variables["cfgPin"].Value)) return toEncrypt;
            var encodeFunc = (Func<IZennoPosterProjectModel, string, bool, string>)FunctionStorage.Functions["SAFU_Encode"];
            string result = encodeFunc(project, toEncrypt, log);
            return result;
        }
        public static string EncodeV2(IZennoPosterProjectModel project, string toEncrypt, bool log = false)
        {
            if (string.IsNullOrEmpty(project.Variables["cfgPin"].Value)) return toEncrypt;
            var encodeFunc = (Func<IZennoPosterProjectModel, string, bool, string>)FunctionStorage.Functions["SAFU_EncodeV2"];
            string result = encodeFunc(project, toEncrypt, log);
            return result;
        }
        
        public static string Decode(IZennoPosterProjectModel project, string toDecrypt, bool log = false)
        {
            if (string.IsNullOrEmpty(project.Variables["cfgPin"].Value)) return toDecrypt;
            var decodeFunc = (Func<IZennoPosterProjectModel, string, bool, string>)FunctionStorage.Functions["SAFU_Decode"];
            string result = decodeFunc(project, toDecrypt, log);
            return result;
        }

        public static string HWPass(IZennoPosterProjectModel project, bool v2 = false)
        {
            try {v2 = (project.GVar("_safu2")=="True")?true:false;}catch{}
            var hwPassFunc = (Func<IZennoPosterProjectModel, bool, string>)FunctionStorage.Functions["SAFU_HWPass"];
            string result = hwPassFunc(project, v2);
            return result;
        }
    }

}
