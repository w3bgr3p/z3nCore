using System;
using System.IO;
using System.Collections.Generic;
using ZennoLab.CommandCenter;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ZennoLab.InterfacesLibrary.ProjectModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace z3nCore
{
    public static class Test
    {

        public static string ZBDbGet(this IZennoPosterProjectModel project,string query, string tableName = "ProfileInfos", bool log = false)
        {
            var modeBkp = project.Var("DBmode");   
            var pathBkp = project.Var("DBsqltPath");   
            var acc0Bkp = project.Var("acc0");   
	
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ZennoLab", "ZP8", ".zp8", "ProfileManagement.db"
            );
    
            if (!File.Exists(dbPath))
            {
                throw new FileNotFoundException(
                    $"База данных не найдена по пути: {dbPath}"
                );
            }
            project.Var("DBmode", "SQLite");
            project.Var("acc0", 1);
            project.Var("DBsqltPath", dbPath);

            
            project.Var("acc0",project.Var("zb_id")); 
            string resp = project.DbGet(query,tableName, log:log );
            
            project.Var("DBmode",modeBkp);   
            project.Var("DBsqltPath",pathBkp);  
            project.Var("acc0",acc0Bkp); 
            return resp;
            
        }

        
        public static Dictionary<string,string> ZBIdDic(this IZennoPosterProjectModel project, string json, string folder = null)
        {
            var array = JArray.Parse(json);
    
            var filtered = string.IsNullOrEmpty(folder) 
                ? array 
                : array.Where(x => (string)x["FolderName"] == folder);
    
            var nameToId = filtered
                .GroupBy(x => (string)x["Name"])
                .ToDictionary(g => g.Key, g => (string)g.First()["Id"]);

            return nameToId;
        }
        
        public static List<string> ZBIdList(this IZennoPosterProjectModel project, string json, string folder = "Farm")
        {
            var dic = project.ZBIdDic(json, folder);
            var res = new List<string>();
            foreach(var p in dic){
                res.Add(p.Value);
            }

            return res;
        }

        
        public static string HWVideoVendor()
        {
            try
            {
                var searcher = new System.Management.ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
                var cards = searcher.Get().Cast<System.Management.ManagementObject>().ToList();
    
                if (cards.Count > 1)
                {
                    string name = (cards[1]["Name"]?.ToString() ?? "").ToLower();
        
                    if (name.Contains("nvidia")) return "NVIDIA";
                    if (name.Contains("amd")) return "AMD";
                    if (name.Contains("ati")) return "AMD";
                    if (name.Contains("intel")) return "Intel";
        
                    string originalName = cards[1]["Name"]?.ToString() ?? "";
                    return originalName.Split(' ').FirstOrDefault() ?? "";
                }
                else if (cards.Count > 0)
                {
                    string name = (cards[0]["Name"]?.ToString() ?? "").ToLower();
        
                    if (name.Contains("nvidia")) return "NVIDIA";
                    if (name.Contains("amd")) return "AMD";
                    if (name.Contains("ati")) return "AMD";
                    if (name.Contains("intel")) return "Intel";
        
                    string originalName = cards[0]["Name"]?.ToString() ?? "";
                    return originalName.Split(' ').FirstOrDefault() ?? "";
                }
    
                return "";
            }
            catch
            {
                return "";
            }
        }

        public static string VideoVendor(this Instance instance)
        {
            string webglData = instance.WebGLPreferences.Save();
            var jObject = JObject.Parse(webglData);


            var vendor = jObject["parameters"]["default"]["UNMASKED_VENDOR"].ToString().ToLower();
            if (vendor.Contains("nvidia")) return "NVIDIA";
            if (vendor.Contains("amd")) return "AMD";
            if (vendor.Contains("ati")) return "AMD";
            if (vendor.Contains("intel")) return "Intel";
            return "";
        }

        public static bool ValidateVideoVendor(this Instance instance, bool thrw = false)
        {
            try
            {
                var HW = HWVideoVendor();
                var ins = instance.VideoVendor();

                if (HW != ins) return false;
                return true;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        public static string SaveCookies(this Instance instance)
        {
            var tmp = Path.GetTempPath() + DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() + "temp.txt";
            instance.SaveCookie(tmp);
            var cookieContent = File.ReadAllText(tmp);
            File.Delete(tmp);
            return cookieContent;
        }
        
        public static void LaunchFromFolder(this Instance instance, string pathProfile,
            ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType browserType = ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.Chromium)
        {
            ZennoLab.CommandCenter.Classes.BuiltInBrowserLaunchSettings settings =
                (ZennoLab.CommandCenter.Classes.BuiltInBrowserLaunchSettings)ZennoLab.CommandCenter.Classes.BrowserLaunchSettingsFactory.Create(browserType);
            settings.CachePath = pathProfile; 
            settings.ConvertProfileFolder = true;
            settings.UseProfile = true;
            instance.Launch(settings);
        }
        public static void UpEmpty(this Instance instance)
        {
            instance.Launch(ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.Chromium, false);
        }
        
        public static void UpZpprofile(this Instance instance, string pathToZpProfile, IZennoPosterProjectModel project)
        {
            project.Profile.Load(pathToZpProfile, true);
            instance.Launch(ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.Chromium, true);
        }

        public static string DbToJson_(this IZennoPosterProjectModel project, string tableName = null, bool log = false, bool thrw = false)
{
    if (string.IsNullOrEmpty(tableName)) 
        tableName = project.ProjectTable();
    
    project.SendInfoToLog("=== DbToJson START ===", true);
    
    // Получаем список всех колонок
    var columns = project.TblColumns(tableName, log);
    
    project.SendInfoToLog($"Columns in table: {string.Join(", ", columns)}", true);
    
    if (!columns.Contains("_json_structure"))
    {
        project.SendErrorToLog("ОШИБКА: Колонка _json_structure отсутствует в таблице!", true);
        return "{}";
    }
    
    // Формируем строку колонок для SELECT
    var columnsString = string.Join(",", columns);
    
    project.SendInfoToLog($"Fetching columns: {columnsString}", true);
    
    // Теперь DbGetColumns сможет правильно распарсить результат
    var allColumns = project.DbGetColumns(columnsString, tableName, log, thrw);
    
    project.SendInfoToLog($"Fetched {allColumns.Count} columns", true);
    
    foreach (var col in allColumns)
    {
        var preview = col.Value.Length > 100 ? col.Value.Substring(0, 100) + "..." : col.Value;
        project.SendInfoToLog($"  [{col.Key}] = {preview}", true);
    }
    
    if (!allColumns.ContainsKey("_json_structure"))
    {
        project.SendErrorToLog("ОШИБКА: _json_structure отсутствует в результате!", true);
        return "{}";
    }
    
    var structureJson = allColumns["_json_structure"];
    project.SendInfoToLog($"Structure JSON length: {structureJson.Length}", true);
    
    Dictionary<string, string> structure;
    try
    {
        structure = JsonConvert.DeserializeObject<Dictionary<string, string>>(structureJson);
        project.SendInfoToLog($"Structure parsed, elements: {structure.Count}", true);
    }
    catch (Exception ex)
    {
        project.SendErrorToLog($"Ошибка парсинга structure: {ex.Message}", true);
        return "{}";
    }
    
    // Удаляем служебные поля
    allColumns.Remove("_json_structure");
    allColumns.Remove("id");
    allColumns.Remove("_id");
    
    project.SendInfoToLog($"Data fields for building: {allColumns.Count}", true);
    
    var result = BuildJson(project, allColumns, structure);
    
    project.SendInfoToLog($"=== Result ===\n{result}", true);
    
    return result;
}
        private static string BuildJson(IZennoPosterProjectModel project, Dictionary<string, string> data, Dictionary<string, string> structure)
        {
            project.SendInfoToLog("--- BuildJson START ---", true);
            
            var root = new JObject();
            var processed = new HashSet<string>();
            
            foreach (var kvp in data.OrderBy(x => x.Key))
            {
                if (processed.Contains(kvp.Key))
                {
                    project.SendInfoToLog($"  SKIP (уже обработан): {kvp.Key}", true);
                    continue;
                }
                
                project.SendInfoToLog($"  Обрабатываю: [{kvp.Key}] = {kvp.Value}", true);
                
                var path = kvp.Key.Split('_');
                project.SendInfoToLog($"    Path: {string.Join(" -> ", path)}", true);
                
                try
                {
                    BuildPath(project, root, path, 0, kvp.Key, data, structure, processed);
                }
                catch (Exception ex)
                {
                    project.SendErrorToLog($"    ОШИБКА при обработке {kvp.Key}: {ex.Message}\n{ex.StackTrace}", true);
                }
            }
            
            project.SendInfoToLog($"--- BuildJson END, обработано: {processed.Count} ---", true);
            
            return root.ToString(Formatting.Indented);
        }

        private static void BuildPath(IZennoPosterProjectModel project, JToken parent, string[] path, int index, string fullKey, 
            Dictionary<string, string> data, Dictionary<string, string> structure, HashSet<string> processed)
        {
            if (index >= path.Length)
            {
                project.SendInfoToLog($"      BuildPath: index >= path.Length, выход", true);
                return;
            }
            
            var segment = path[index];
            var isLast = index == path.Length - 1;
            
            project.SendInfoToLog($"      BuildPath: segment={segment}, index={index}/{path.Length}, isLast={isLast}, parent type={parent.Type}", true);
            
            if (isLast)
            {
                var value = data[fullKey];
                project.SendInfoToLog($"        LEAF: устанавливаю значение '{value}' для ключа '{fullKey}'", true);
                
                JToken tokenValue = CreateTypedToken(project, value, fullKey, structure);
                project.SendInfoToLog($"        Token создан: type={tokenValue.Type}, value={tokenValue}", true);
                
                if (parent is JObject jObj)
                {
                    project.SendInfoToLog($"        Добавляю в JObject[{segment}]", true);
                    jObj[segment] = tokenValue;
                }
                else if (parent is JArray jArr)
                {
                    project.SendInfoToLog($"        Добавляю в JArray (count={jArr.Count})", true);
                    jArr.Add(tokenValue);
                }
                else
                {
                    project.SendErrorToLog($"        ОШИБКА: неизвестный тип parent: {parent.GetType()}", true);
                }
                
                processed.Add(fullKey);
            }
            else
            {
                var currentPath = string.Join("_", path.Take(index + 1));
                bool isArray = structure.ContainsKey(currentPath) && structure[currentPath] == "array";
                
                project.SendInfoToLog($"        NODE: currentPath={currentPath}, isArray={isArray}", true);
                
                JToken child = null;
                
                if (parent is JObject jObj)
                {
                    child = jObj[segment];
                    project.SendInfoToLog($"        Parent=JObject, child exists={child != null}", true);
                    
                    if (child == null)
                    {
                        child = isArray ? (JToken)new JArray() : (JToken)new JObject();
                        project.SendInfoToLog($"        Создаю новый child: {child.Type}", true);
                        jObj[segment] = child;
                    }
                }
                else if (parent is JArray jArr)
                {
                    project.SendInfoToLog($"        Parent=JArray, count={jArr.Count}, segment={segment}", true);
                    
                    if (int.TryParse(segment, out int idx))
                    {
                        project.SendInfoToLog($"        Segment - число: {idx}", true);
                        if (idx < jArr.Count)
                        {
                            child = jArr[idx];
                            project.SendInfoToLog($"        Взял существующий элемент [{idx}]", true);
                        }
                        else
                        {
                            child = isArray ? (JToken)new JArray() : (JToken)new JObject();
                            project.SendInfoToLog($"        Создаю новый элемент: {child.Type}", true);
                            jArr.Add(child);
                        }
                    }
                    else
                    {
                        project.SendErrorToLog($"        ОШИБКА: segment '{segment}' не число для массива!", true);
                        child = isArray ? (JToken)new JArray() : (JToken)new JObject();
                        jArr.Add(child);
                    }
                }
                else
                {
                    project.SendErrorToLog($"        ОШИБКА: неизвестный тип parent: {parent.GetType()}", true);
                    return;
                }
                
                if (child != null)
                {
                    BuildPath(project, child, path, index + 1, fullKey, data, structure, processed);
                }
                else
                {
                    project.SendErrorToLog($"        ОШИБКА: child == null после обработки!", true);
                }
            }
        }

        private static JToken CreateTypedToken(IZennoPosterProjectModel project, string value, string fullKey, Dictionary<string, string> structure)
        {
            if (structure.ContainsKey(fullKey))
            {
                var type = structure[fullKey];
                project.SendInfoToLog($"          CreateToken: key={fullKey}, type={type}, value={value}", true);
                
                switch (type)
                {
                    case "integer":
                        if (int.TryParse(value, out int intVal))
                        {
                            project.SendInfoToLog($"          -> Integer: {intVal}", true);
                            return new JValue(intVal);
                        }
                        break;
                    case "float":
                        if (double.TryParse(value, out double dblVal))
                        {
                            project.SendInfoToLog($"          -> Float: {dblVal}", true);
                            return new JValue(dblVal);
                        }
                        break;
                    case "boolean":
                        if (bool.TryParse(value, out bool boolVal))
                        {
                            project.SendInfoToLog($"          -> Boolean: {boolVal}", true);
                            return new JValue(boolVal);
                        }
                        break;
                    case "null":
                        project.SendInfoToLog($"          -> Null", true);
                        return JValue.CreateNull();
                }
            }
            
            project.SendInfoToLog($"          CreateToken: key={fullKey} -> String: {value}", true);
            return new JValue(value);
        }
        
        public static List<string> GetTypeProperties(Type type, bool requireSetter = false)
        {
            var listColumnsToAdd = new List<string>();

            foreach (var prop in type.GetProperties())
            {
                bool hasGet = prop.CanRead && prop.GetMethod?.IsPublic == true;
                bool hasSet = prop.CanWrite && prop.SetMethod?.IsPublic == true;
        
                bool isAccessible = requireSetter ? (hasGet && hasSet) : hasGet;
                if (!isAccessible) continue;

                var propType = prop.PropertyType;
                bool isSimple = propType.IsPrimitive || 
                                propType == typeof(string) || 
                                propType == typeof(decimal) || 
                                propType == typeof(DateTime) ||
                                propType.IsEnum;

                if (isSimple) listColumnsToAdd.Add(prop.Name);
            }
            return listColumnsToAdd;
        }
        public static List<string> GetTypeProperties(object obj)
        {
            return GetTypeProperties(obj.GetType());
        }
        public static Dictionary<string, string> GetValuesByProperty(this IZennoPosterProjectModel project, object obj, List<string> propertyList = null, string tableToUpd = null)
        {
            var type = obj.GetType();
    
            if (propertyList == null || propertyList.Count == 0) 
                propertyList = GetTypeProperties(type);
            
            var data = new Dictionary<string, string>();
    
            foreach (var column in propertyList)
            {
                try
                {
                    var prop = type.GetProperty(column);
                    var value = prop.GetValue(obj, null); // берем из переданного объекта
                    string valueStr = value != null ? value.ToString().Replace("'", "''") : string.Empty;
                    data.Add(column, valueStr);
                }
                catch (Exception ex)
                {
                    project.warn($"Error on field '{column}': {ex.Message}");
                }
            }
    
            if (!string.IsNullOrEmpty(tableToUpd)) project.DicToDb(data, tableToUpd);
            return data;
        }


        private static string NetscapeToJson(string content, string domainFilter = null)
        {
            var cookies = new List<object>();
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split('\t');

                if (parts.Length < 7) continue;

                try
                {
                    var domain = parts[0];

                    if (!string.IsNullOrEmpty(domainFilter) && !domain.Contains(domainFilter))
                        continue;

                    var includeSubdomains = parts[1].Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                    var path = parts[2];
                    var secure = parts[3].Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                    var expiryStr = parts[4];
                    var name = parts[5];
                    var value = parts.Length > 6 ? parts[6] : "";

                    var httpOnly = false;
                    var sameSite = "Unspecified";

                    if (parts.Length > 7)
                        httpOnly = parts[7].Equals("TRUE", StringComparison.OrdinalIgnoreCase);

                    if (parts.Length > 9)
                        sameSite = parts[9];

                    double? expirationDate = null;
                    bool isSession = string.IsNullOrEmpty(expiryStr);

                    if (!isSession)
                    {
                        // Формат: MM/dd/yyyy HH:mm:ss
                        if (DateTime.TryParseExact(expiryStr, "MM/dd/yyyy HH:mm:ss",
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiry))
                        {
                            expirationDate = new DateTimeOffset(expiry).ToUnixTimeSeconds();
                        }
                    }

                    cookies.Add(new
                    {
                        domain = domain,
                        expirationDate = expirationDate,
                        hostOnly = !includeSubdomains,
                        httpOnly = httpOnly,
                        name = name,
                        path = path,
                        sameSite = sameSite,
                        secure = secure,
                        session = isSession,
                        storeId = (string)null,
                        value = value,
                        id = (domain + name + path).GetHashCode()
                    });
                }
                catch (Exception ex)
                {
                }
            }

            return Global.ZennoLab.Json.JsonConvert.SerializeObject(cookies, Global.ZennoLab.Json.Formatting.None);
        }

     
        #region restore

        
        public static void SetValuesFromDb(this IZennoPosterProjectModel project, object obj, string table = "profile", List<string> propertyList = null, string key = "id", object id = null, string where = "")
{
    var type = obj.GetType();

    if (propertyList == null)
        propertyList = GetTypeProperties(type);

    string columnsToGet = string.Join(", ", propertyList);
    var dbData = project.DbGetColumns(columnsToGet, table, key: key, id: id, where: where);

    foreach (var column in propertyList)
    {
        try
        {
            var prop = type.GetProperty(column);
            
            if (prop == null || !prop.CanWrite || prop.SetMethod?.IsPublic != true)
                continue;

            string valueStr = dbData.ContainsKey(column) ? dbData[column] : null;

            if (string.IsNullOrEmpty(valueStr))
                continue;

            object value = ConvertToPropertyType(valueStr, prop.PropertyType);

            if (value != null)
                prop.SetValue(obj, value);
        }
        catch (Exception ex)
        {
            project.warn($"Error setting field '{column}': {ex.Message}");
        }
    }
}
        private static object ConvertToPropertyType(string value, Type targetType)
{
    try
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType == typeof(string))
            return value;

        if (underlyingType == typeof(int))
            return int.Parse(value);

        if (underlyingType == typeof(long))
            return long.Parse(value);

        if (underlyingType == typeof(bool))
            return bool.Parse(value);

        if (underlyingType == typeof(decimal))
            return decimal.Parse(value);

        if (underlyingType == typeof(double))
            return double.Parse(value);

        if (underlyingType == typeof(DateTime))
            return DateTime.Parse(value);

        if (underlyingType.IsEnum)
            return Enum.Parse(underlyingType, value);

        return Convert.ChangeType(value, underlyingType);
    }
    catch
    {
        return null;
    }
}


        
        
        
        
        
        #endregion
        
    }
}