using System;
using System.Linq;

namespace z3nCore.Utilities
{
    public class Debugger
    {
        public static string AssemblyVer(string dllName)
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == dllName);
            if (assembly != null)
            {
                return $"{dllName} {assembly.GetName().Version}, PublicKeyToken: {BitConverter.ToString(assembly.GetName().GetPublicKeyToken())}";
            }
            else
            {
                return $"{dllName} not loaded";
            }
        }
    }
}
