using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public static class Rnd
    {
        private static Random random = new Random();
        
        public static string Seed()
        {
            return Blockchain.GenerateMnemonic("English", 12);
        }
        public static string RndHexString(int length)
        {
            const string chars = "0123456789abcdef";
            //var random = new Random();
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }
            return "0x" + new string(result);
        }
        public static string RndString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            //var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public static string RndNickname()
        {
            string[] adjectives = {
                "Sunny", "Mystic", "Wild", "Cosmic", "Shadow", "Lunar", "Blaze", "Dream", "Star", "Vivid",
                "Frost", "Neon", "Gloomy", "Swift", "Silent", "Fierce", "Radiant", "Dusk", "Nova", "Spark",
                "Crimson", "Azure", "Golden", "Midnight", "Velvet", "Stormy", "Echo", "Vortex", "Phantom", "Bright",
                "Chill", "Rogue", "Daring", "Lush", "Savage", "Twilight", "Crystal", "Zesty", "Bold", "Hazy",
                "Vibrant", "Gleam", "Frosty", "Wicked", "Serene", "Bliss", "Rusty", "Hollow", "Sleek", "Pale"
            };

            string[] nouns = {
                "Wolf", "Viper", "Falcon", "Spark", "Catcher", "Rider", "Echo", "Flame", "Voyage", "Knight",
                "Raven", "Hawk", "Storm", "Tide", "Drift", "Shade", "Quest", "Blaze", "Wraith", "Comet",
                "Lion", "Phantom", "Star", "Cobra", "Dawn", "Arrow", "Ghost", "Sky", "Vortex", "Wave",
                "Tiger", "Ninja", "Dreamer", "Seeker", "Glider", "Rebel", "Spirit", "Hunter", "Flash", "Beacon",
                "Jaguar", "Drake", "Scout", "Path", "Glow", "Riser", "Shadow", "Bolt", "Zephyr", "Forge"
            };

            string[] suffixes = { "", "", "", "", "", "X", "Z", "Vibe", "Glow", "Rush", "Peak", "Core", "Wave", "Zap" };

            Random random = new Random(Guid.NewGuid().GetHashCode());

            string adjective = adjectives[random.Next(adjectives.Length)];
            string noun = nouns[random.Next(nouns.Length)];
            string suffix = suffixes[random.Next(suffixes.Length)];

            string nickname = $"{adjective}{noun}{suffix}";

            if (nickname.Length > 15)
            {
                nickname = nickname.Substring(0, 15);
            }

            return nickname;
        }
        public static string RndInvite(this IZennoPosterProjectModel project,  object limit = null, bool log = false)
        {
            string refCode = project.Variables["cfgRefCode"].Value;

            if (string.IsNullOrEmpty(refCode))
            {
                string whereClause = "TRIM(refcode) != ''";
    
                if (limit != null)
                {
                    string parsedLimit = limit.ToString();
                    if (int.TryParse(parsedLimit, out int limitValue) && limitValue > 0)
                    {
                        whereClause += $" AND id <= {limitValue}";
                    }
                    else
                    {
                        project.SendErrorToLog($"Invalid limit. {limit}", log);
                        throw new Exception($"Invalid limit. {limit}");
                    }
                }
    
                whereClause += " ORDER BY RANDOM() LIMIT 1";
                refCode = project.SqlGet("refcode", log: log, where: whereClause);
                project.Variables["cfgRefCode"].Value = refCode;
            }

            
            return refCode;
        }
        public static double RndPercent(decimal input, double percent, double maxPercent)
        {
            if (percent < 0 || maxPercent < 0 || percent > 100 || maxPercent > 100)
                throw new ArgumentException("Percent and MaxPercent must be between 0 and 100");

            if (!double.TryParse(input.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double number))
                throw new ArgumentException("Input cannot be converted to double");

            double percentageValue = number * (percent / 100.0);

            //Random random = new Random();
            double randomReductionPercent = random.NextDouble() * maxPercent;
            double reduction = percentageValue * (randomReductionPercent / 100.0);

            double result = percentageValue - reduction;

            if (result <= 0)
            {
                result = Math.Max(percentageValue * 0.01, 0.0001);
            }

            return result;
        }
        public static decimal RndDecimal(this IZennoPosterProjectModel project, string Var)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            string value = string.Empty;
            try
            {
                value = project.Variables[Var].Value;
            }
            catch (Exception e)
            {
                project.SendInfoToLog(e.Message);
            }
            if (value == string.Empty) project.log($"no Value from [{Var}] `w");

            if (value.Contains("-"))
            {
                var min = decimal.Parse(value.Split('-')[0].Trim());
                var max = decimal.Parse(value.Split('-')[1].Trim());
                //Random rand = new Random();
                return min + (decimal)(random.NextDouble() * (double)(max - min));
            }
            return decimal.Parse(value.Trim());
        }
        public static int RndInt(this IZennoPosterProjectModel project, string Var)
        {
            string value = string.Empty;
            try
            {
                value = project.Variables[Var].Value;
            }
            catch (Exception e)
            {
                project.SendInfoToLog(e.Message);
            }
            if (value == string.Empty) project.log($"no Value from [{Var}] `w");

            if (value.Contains("-"))
            {
                var min = int.Parse(value.Split('-')[0].Trim());
                var max = int.Parse(value.Split('-')[1].Trim());
                random.Next(min, max);
            }
            return int.Parse(value.Trim());
        }
        public static bool RndBool(this int truePercent)
        {
            return random.NextDouble() * 100 < truePercent;
        }
    }
}
