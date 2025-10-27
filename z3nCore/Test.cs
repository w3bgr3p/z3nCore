using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using System.Linq;
using System.Threading;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


using System.Xml;

using System.Net.Http;


using Formatting = Newtonsoft.Json.Formatting;


using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace z3nCore
{

    
    public static class Test
    {
        public static Dictionary<string, string> JsonToDic_(this string json)
        {
            var result = new Dictionary<string, string>();
            var jObject = JObject.Parse(json);
    
            FlattenJson(jObject, "", result);
    
            return result;
    
            void FlattenJson(JToken token, string prefix, Dictionary<string, string> dict)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        foreach (var property in token.Children<JProperty>())
                        {
                            var key = string.IsNullOrEmpty(prefix) 
                                ? property.Name 
                                : $"{prefix}_{property.Name}";
                            FlattenJson(property.Value, key, dict);
                        }
                        break;
                
                    case JTokenType.Array:
                        var index = 0;
                        foreach (var item in token.Children())
                        {
                            FlattenJson(item, $"{prefix}_{index}", dict);
                            index++;
                        }
                        break;
                
                    default:
                        dict[prefix] = token.ToString();
                        break;
                }
            }
        }

    }

    
    
  
  
}