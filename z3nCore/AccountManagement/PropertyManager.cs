using System;
using System.Collections.Generic;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore.Utilities
{
    public static class PropertyManager
    {
        #region get
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
                    project.SendWarningToLog($"Error on field '{column}': {ex.Message}");
                }
            }
    
            if (!string.IsNullOrEmpty(tableToUpd)) project.DicToDb(data, tableToUpd, log:true);
            return data;
        }
        #endregion
        
        #region set
        
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
                    project.SendWarningToLog($"Error setting field '{column}': {ex.Message}");
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