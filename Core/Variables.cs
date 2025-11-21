using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Core
{
    public class Variables
    {
        private readonly Dictionary<string, string> _storage = new Dictionary<string, string>();

        public Variables()
        {
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            // Вычисляем projectName и projectPath при инициализации
            try
            {
                string exePath = Assembly.GetEntryAssembly()?.Location ?? Assembly.GetExecutingAssembly().Location;
                string fileName = Path.GetFileName(exePath);
                string projectPath = Path.GetDirectoryName(exePath);

                Set("projectName", Path.GetFileNameWithoutExtension(fileName));
                Set("projectPath", projectPath);
                Set("projectTable", "__" + Get("projectName"));

                // Инициализируем varSessionId для Age()
                Set("varSessionId", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            }
            catch
            {
                // Если не удалось получить путь, устанавливаем пустые значения
                Set("projectName", "");
                Set("projectPath", "");
            }
        }

        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            return _storage.ContainsKey(key) ? _storage[key] : string.Empty;
        }

        public void Set(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                return;

            _storage[key] = value ?? string.Empty;
        }

        public int GetInt(string key)
        {
            string value = Get(key);
            if (int.TryParse(value, out int result))
                return result;
            return 0;
        }

        public decimal GetDecimal(string key)
        {
            string value = Get(key);
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                return result;
            return 0m;
        }

        public bool GetBool(string key)
        {
            string value = Get(key);
            return value == "True" || value == "true" || value == "1";
        }

        public string this[string key]
        {
            get => Get(key);
            set => Set(key, value);
        }

        // Методы из Constantes.cs

        public string ProjectName()
        {
            return Get("projectName");
        }

        public string ProjectTable()
        {
            return Get("projectTable");
        }

        public List<string> Range(string accRange = null)
        {
            if (string.IsNullOrEmpty(accRange))
                accRange = Get("cfgAccRange");

            if (string.IsNullOrEmpty(accRange))
                throw new Exception("range is not provided by input or setting [cfgAccRange]");

            int rangeS, rangeE;
            string range;

            if (accRange.Contains(","))
            {
                range = accRange;
                var rangeParts = accRange.Split(',').Select(int.Parse).ToArray();
                rangeS = rangeParts.Min();
                rangeE = rangeParts.Max();
            }
            else if (accRange.Contains("-"))
            {
                var rangeParts = accRange.Split('-').Select(int.Parse).ToArray();
                rangeS = rangeParts[0];
                rangeE = rangeParts[1];
                range = string.Join(",", Enumerable.Range(rangeS, rangeE - rangeS + 1));
            }
            else
            {
                rangeE = int.Parse(accRange);
                rangeS = int.Parse(accRange);
                range = accRange;
            }

            Set("rangeStart", $"{rangeS}");
            Set("rangeEnd", $"{rangeE}");
            Set("range", range);

            return range.Split(',').ToList();
        }

        // Математические операции над переменными
        public decimal VarsMath(string varA, string operation, string varB, string resultVar = null)
        {
            decimal a = GetDecimal(varA);
            decimal b = GetDecimal(varB);
            decimal result;

            switch (operation)
            {
                case "+":
                    result = a + b;
                    break;
                case "-":
                    result = a - b;
                    break;
                case "*":
                    result = a * b;
                    break;
                case "/":
                    result = a / b;
                    break;
                default:
                    throw new Exception($"unsupported operation {operation}");
            }

            if (!string.IsNullOrEmpty(resultVar))
                Set(resultVar, $"{result}");

            return result;
        }

        // Счетчик
        public int VarCounter(string varName, int input)
        {
            var counter = GetInt(varName) + input;
            Set(varName, counter.ToString());
            return counter;
        }

        // Заполнение из словаря
        public void FromDict(Dictionary<string, string> dict)
        {
            if (dict == null)
                return;

            foreach (var pair in dict)
            {
                Set(pair.Key, pair.Value);
            }
        }

        // Получить все переменные
        public Dictionary<string, string> GetAll()
        {
            return new Dictionary<string, string>(_storage);
        }
    }
}
