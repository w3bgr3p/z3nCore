using System;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Threading;
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
        public static string RndNickname(int min = 8, int max = 16)
        {
            string[] adjectives = {
                // Original
                "Sunny", "Mystic", "Wild", "Cosmic", "Shadow", "Lunar", "Blaze", "Dream", "Star", "Vivid",
                "Frost", "Neon", "Gloomy", "Swift", "Silent", "Fierce", "Radiant", "Dusk", "Nova", "Spark",
                "Crimson", "Azure", "Golden", "Midnight", "Velvet", "Stormy", "Echo", "Vortex", "Phantom", "Bright",
                "Chill", "Rogue", "Daring", "Lush", "Savage", "Twilight", "Crystal", "Zesty", "Bold", "Hazy",
                "Vibrant", "Gleam", "Frosty", "Wicked", "Serene", "Bliss", "Rusty", "Hollow", "Sleek", "Pale",
                
                // Web3/Crypto themed
                "Crypto", "Cyber", "Digital", "Virtual", "Meta", "Quantum", "Binary", "Pixel", "Holo", "Nano",
                "Defi", "Web3", "Chain", "Block", "Hash", "Node", "Mint", "Token", "Smart", "Decen",
                "Atomic", "Layer", "Zero", "Prime", "Alpha", "Beta", "Gamma", "Delta", "Sigma", "Omega",
                
                // Additional variety
                "Electric", "Magnetic", "Plasma", "Sonic", "Turbo", "Ultra", "Mega", "Hyper", "Super", "Epic",
                "Ancient", "Modern", "Future", "Retro", "Classic", "Neo", "Zen", "Chaos", "Order", "Pure",
                "Dark", "Light", "Gray", "Silver", "Bronze", "Platinum", "Diamond", "Ruby", "Emerald", "Sapphire",
                "Arctic", "Desert", "Ocean", "Mountain", "Forest", "Urban", "Space", "Astral", "Ethereal", "Mystic",
                "Royal", "Noble", "Elite", "Prime", "Grand", "Supreme", "Ultimate", "Infinite", "Eternal", "Divine",
                "Stealth", "Rapid", "Quick", "Agile", "Sharp", "Smooth", "Raw", "Fresh", "Cool", "Hot"
            };

            string[] nouns = {
                // Original
                "Wolf", "Viper", "Falcon", "Spark", "Catcher", "Rider", "Echo", "Flame", "Voyage", "Knight",
                "Raven", "Hawk", "Storm", "Tide", "Drift", "Shade", "Quest", "Blaze", "Wraith", "Comet",
                "Lion", "Phantom", "Star", "Cobra", "Dawn", "Arrow", "Ghost", "Sky", "Vortex", "Wave",
                "Tiger", "Ninja", "Dreamer", "Seeker", "Glider", "Rebel", "Spirit", "Hunter", "Flash", "Beacon",
                "Jaguar", "Drake", "Scout", "Path", "Glow", "Riser", "Shadow", "Bolt", "Zephyr", "Forge",
                
                // Web3/Crypto themed
                "Wallet", "Ledger", "Miner", "Trader", "Hodler", "Whale", "Bull", "Bear", "Ape", "Degen",
                "Punk", "Kitty", "Bayc", "Azuki", "Doodle", "Cool", "Clone", "Mutant", "Alien", "Robot",
                "Validator", "Staker", "Farmer", "Yield", "Pool", "Vault", "Bridge", "Oracle", "Contract", "Gas",
                "Token", "Coin", "NFT", "DAO", "DeFi", "GameFi", "Metaverse", "Avatar", "Pixel", "Voxel",
                
                // Gaming/Tech themed
                "Gamer", "Player", "Master", "Legend", "Hero", "Warrior", "Mage", "Ranger", "Rogue", "Paladin",
                "Dragon", "Phoenix", "Unicorn", "Kraken", "Leviathan", "Behemoth", "Titan", "Giant", "Demon", "Angel",
                "Hacker", "Coder", "Dev", "Builder", "Creator", "Artist", "Designer", "Architect", "Engineer", "Pilot",
                
                // Additional variety
                "Eagle", "Shark", "Panther", "Bear", "Fox", "Owl", "Crow", "Bat", "Spider", "Scorpion",
                "Samurai", "Viking", "Spartan", "Gladiator", "Assassin", "Sniper", "Tank", "Support", "Carry", "Jungler",
                "Storm", "Thunder", "Lightning", "Tornado", "Hurricane", "Blizzard", "Avalanche", "Earthquake", "Tsunami", "Volcano",
                "Sword", "Shield", "Spear", "Axe", "Bow", "Staff", "Wand", "Dagger", "Hammer", "Scythe",
                "King", "Queen", "Prince", "Duke", "Baron", "Lord", "Lady", "Champion", "Guardian", "Sentinel"
            };

            string[] suffixes = { 
                // Original
                "", "", "", "", "", "X", "Z", "Vibe", "Glow", "Rush", "Peak", "Core", "Wave", "Zap",
                
                // Web3/Crypto themed
                "DAO", "NFT", "ETH", "BTC", "SOL", "AVAX", "MATIC", "DOT", "LINK", "UNI",
                "420", "69", "88", "777", "666", "999", "000", "001", "007", "2024",
                "GM", "GN", "WAGMI", "NGMI", "LFG", "DYOR", "HODL", "FOMO", "FUD", "REKT",
                
                // Gaming themed
                "GG", "EZ", "Pro", "Op", "Ace", "MVP", "FTW", "PWN", "LOL", "XD",
                "TV", "YT", "TTV", "Live", "Stream", "Play", "Game", "Win", "Top", "Best",
                
                // Additional variety
                "Max", "Min", "Plus", "Ultra", "Mega", "Giga", "Tera", "Nano", "Micro", "Macro",
                "Alpha", "Beta", "Gamma", "Delta", "Omega", "Prime", "Elite", "Pro", "VIP", "Gold",
                "2K", "3K", "4K", "8K", "HD", "UHD", "4D", "5D", "VR", "AR",
                "Jr", "Sr", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X"
            };

            // Additional number patterns for Web3 style
            string[] numberPatterns = {
                "", "", "", "", "", "", "", "", "", "", // 50% chance of no numbers
                "0x", "1337", "2077", "3000", "9000", "404", "808", "911", "247", "365",
                "11", "22", "33", "44", "55", "66", "77", "88", "99", "00",
                "123", "234", "345", "456", "567", "678", "789", "890", "321", "987"
            };

            Random random = new Random();
            const int maxAttempts = 100;
            
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Randomly choose pattern
                int pattern = random.Next(0, 5);
                string nickname = "";
                
                switch (pattern)
                {
                    case 0: // Adjective + Noun + Suffix
                        nickname = adjectives[random.Next(adjectives.Length)] +
                                  nouns[random.Next(nouns.Length)] +
                                  suffixes[random.Next(suffixes.Length)];
                        break;
                        
                    case 1: // Noun + Number
                        nickname = nouns[random.Next(nouns.Length)] +
                                  numberPatterns[random.Next(numberPatterns.Length)];
                        break;
                        
                    case 2: // Adjective + Number
                        nickname = adjectives[random.Next(adjectives.Length)] +
                                  numberPatterns[random.Next(numberPatterns.Length)];
                        break;
                        
                    case 3: // Single word + Suffix
                        nickname = (random.Next(2) == 0 ? 
                                  adjectives[random.Next(adjectives.Length)] : 
                                  nouns[random.Next(nouns.Length)]) +
                                  suffixes[random.Next(suffixes.Length)];
                        break;
                        
                    case 4: // Web3 style with underscore or dot
                        string separator = random.Next(3) == 0 ? "_" : (random.Next(2) == 0 ? "." : "");
                        nickname = adjectives[random.Next(adjectives.Length)] +
                                  separator +
                                  nouns[random.Next(nouns.Length)];
                        break;
                }
                
                // Sometimes add random capitalization for variety (Web3 style)
                if (random.Next(10) == 0 && nickname.Length > 3)
                {
                    nickname = nickname.ToLower();
                }
                else if (random.Next(10) == 0 && nickname.Length > 3)
                {
                    nickname = nickname.ToUpper();
                }
                
                if (nickname.Length >= min && nickname.Length <= max)
                {
                    return nickname;
                }
            }

            // Fallback: force a valid length nickname
            string fallback = adjectives[random.Next(adjectives.Length)].Substring(0, Math.Min(adjectives[random.Next(adjectives.Length)].Length, max));
            while (fallback.Length < min)
            {
                fallback += random.Next(10).ToString();
            }
            return fallback.Substring(0, Math.Min(fallback.Length, max));
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
        public static string RndFile(string directoryPath, string extension = null)
        {
            readrandom:
            try
            {
                string searchPattern = extension != null && !string.IsNullOrEmpty(extension) 
                    ? "*." + extension.TrimStart('.') 
                    : "*";
                var files = Directory.GetFiles(directoryPath, searchPattern, SearchOption.AllDirectories);
                if (files.Length == 0) return null;
                var random = new Random();
                return files[random.Next(files.Length)];
            }
            catch 
            {
                goto readrandom;
            }
        }
    }
}
