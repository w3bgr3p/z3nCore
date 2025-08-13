using NBitcoin;
using Nethereum.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.Enums.Browser;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public enum schema
    {
        private_blockchain,
        private_twitter,
        private_discord,
        private_google,
        private_github,
        private_api,
        private_settings,
        private_profile,

        public_blockchain,
        public_deposits,
        public_native,
        public_mail,
        public_twitter,
        public_profile,
        public_github,
        public_discord,
        public_google,
        public_browser,
        public_rpc,

        project
    }

    public class DBuilder : Sql
    {
        private readonly IZennoPosterProjectModel _project;
        protected readonly Instance _instance;
        private readonly F0rms _f0rm;
        private readonly int _range;

        public DBuilder(IZennoPosterProjectModel project, Instance instance, bool log = false)
            : base(project, log: log)
        {
            _instance = instance;
            _project = project;         
            _f0rm = new F0rms(_project);
            _range = _project.Range();
        }

        public DBuilder(IZennoPosterProjectModel project, bool log = false)
          : base(project, log: log)
        {
            _project = project;
            _f0rm = new F0rms(_project);
            _range = _project.Range();
        }


        public string[] DefaultColumns(schema tableSchem)
        {

            switch (tableSchem)
            {

                case schema.public_native:
                case schema.public_deposits:
                    return new string[] { };
                case schema.private_google:
                    return new string[] { "status", "last", "cookies", "login", "password", "otpsecret", "otpbackup", "recoveryemail", "recovery_phone" };
                case schema.private_twitter:
                    return new string[] { "status", "last", "cookies", "token", "login", "password", "otpsecret", "otpbackup", "email", "emailpass" };
                case schema.private_discord:
                    return new string[] { "status", "last", "token", "login", "password", "otpsecret", "otpbackup", "email", "emailpass", "recovery_phone" };
                case schema.private_github:
                    return new string[] { "status", "last", "cookies", "token", "login", "password", "otpsecret", "otpbackup", "email", "emailpass" };
                case schema.public_blockchain:
                    return new string[] { "evm_pk", "sol_pk", "apt_pk", "evm_seed" };
                case schema.private_blockchain:
                    return new string[] { "secp256k1", "base58", "bip39" };

                case schema.private_settings:
                    return new string[] { "value" };
                case schema.private_api:
                    return new string[] { "apikey", "apisecret", "passphrase", "proxy" };
                case schema.private_profile:
                    return new string[] { "proxy", "cookies", "webgl", "zb_id" };


                case schema.public_profile:
                    return new string[] { "nickname", "bio", "brsr_score" };

                case schema.public_rpc:
                    return new string[] { "rpc", "explorer", "explorer_api" };

                case schema.public_mail:
                    return new string[] { "google", "icloud", "firstmail" };
                case schema.public_google:
                    return new string[] { "status", "last" };
                case schema.public_twitter:
                    return new string[] { "status", "last", "id", "following", "followers", "creation", "givenname", "description", "lang", "birth", "country", "gender", "homelocation", };
                case schema.public_discord:
                    return new string[] { "status", "last", "username", "servers", "roles" };


                default:
                    throw new Exception("no schema");
            }

        }

        public Dictionary<string, string> LoadSchema(schema tableSchem)
        {
            var tableStructure = new Dictionary<string, string>();

            string primary = "acc0";
            string primaryType = "INTEGER PRIMARY KEY";
            var toFill = new List<string>();
            string defaultColumn = "TEXT DEFAULT ''";



            switch (tableSchem)
            {

                case schema.private_twitter:
                case schema.private_google:
                case schema.private_discord:
                case schema.private_github:

                case schema.public_google:
                case schema.public_twitter:
                case schema.public_discord:
                case schema.public_github:

                case schema.private_blockchain:
                case schema.public_blockchain:
                case schema.public_native:
                case schema.public_deposits:

                case schema.private_profile:
                case schema.public_profile:
                case schema.public_mail:
                    foreach (string column in DefaultColumns(tableSchem)) toFill.Add(column);
                    break;

                case schema.private_settings:
                case schema.private_api:
                case schema.public_rpc:
                    primary = "key";
                    foreach (string column in DefaultColumns(tableSchem)) toFill.Add(column);
                    break;

                default:
                    throw new Exception("no schema");

            }


            if (primary != "acc0") primaryType = "TEXT PRIMARY KEY";
            tableStructure.Add(primary, primaryType);

            foreach (string name in toFill)
            {
                if (!tableStructure.ContainsKey(name)) tableStructure.Add(name, defaultColumn);
            }
            return tableStructure;
        }

       
        private string ImportData(string tableName, string[] availableFields, Dictionary<string, string> columnMapping, string formTitle = "title", string message = "Select format (one field per box):", int startFrom = 1)
        {
            _tableName = tableName;
            int lineCount = 0;

            var formFont = new System.Drawing.Font("Iosevka", 10);
            System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            form.Text = formTitle;
            form.Width = 800;
            form.Height = 700;
            form.BackColor = System.Drawing.Color.DarkGray;
            form.TopMost = true;
            form.Location = new System.Drawing.Point(108, 108);

            List<string> selectedFormat = new List<string>();
            System.Windows.Forms.TextBox formatDisplay = new System.Windows.Forms.TextBox();
            System.Windows.Forms.TextBox dataInput = new System.Windows.Forms.TextBox();
            System.Windows.Forms.TextBox dividerInput = new System.Windows.Forms.TextBox();

            System.Windows.Forms.Label formatLabel = new System.Windows.Forms.Label();
            formatLabel.Font = new System.Drawing.Font("Iosevka", 10, System.Drawing.FontStyle.Bold);
            formatLabel.Text = message;
            formatLabel.AutoSize = true;
            formatLabel.Left = 10;
            formatLabel.Top = 10;

            form.Controls.Add(formatLabel);

            System.Windows.Forms.ComboBox[] formatComboBoxes = new System.Windows.Forms.ComboBox[availableFields.Length - 1];
            int spacing = 5;
            int totalSpacing = spacing * (formatComboBoxes.Length - 1);
            int comboWidth = (form.ClientSize.Width - 20 - totalSpacing) / formatComboBoxes.Length;
            for (int i = 0; i < formatComboBoxes.Length; i++)
            {
                formatComboBoxes[i] = new System.Windows.Forms.ComboBox();
                formatComboBoxes[i].DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
                formatComboBoxes[i].Font = formFont;
                formatComboBoxes[i].Items.AddRange(availableFields);
                formatComboBoxes[i].SelectedIndex = 0;
                formatComboBoxes[i].Left = 10 + i * (comboWidth + spacing);
                formatComboBoxes[i].Top = 30;
                formatComboBoxes[i].Width = comboWidth;
                formatComboBoxes[i].SelectedIndexChanged += (s, e) =>
                {
                    selectedFormat.Clear();
                    foreach (var combo in formatComboBoxes)
                    {
                        if (!string.IsNullOrEmpty(combo.SelectedItem?.ToString()))
                            selectedFormat.Add(combo.SelectedItem.ToString());
                    }
                    string divider = dividerInput.Text == "\\t" ? "\t" : dividerInput.Text;
                    formatDisplay.Text = string.Join(divider, selectedFormat);
                };
                form.Controls.Add(formatComboBoxes[i]);
            }

            System.Windows.Forms.Label dividerLabel = new System.Windows.Forms.Label();
            dividerLabel.Text = "Divider:";
            dividerLabel.Font = formFont;
            dividerLabel.AutoSize = true;
            dividerLabel.Left = 10;
            dividerLabel.Top = 60;
            form.Controls.Add(dividerLabel);

            dividerInput.Left = 80;
            dividerInput.Top = 60;
            dividerInput.Width = 50;
            dividerInput.Text = ":";
            dividerInput.Font = formFont;
            dividerInput.BackColor = System.Drawing.Color.Black;
            dividerInput.ForeColor = System.Drawing.Color.White;
            dividerInput.TextChanged += (s, e) =>
            {
                string divider1 = dividerInput.Text == "\\t" ? "\t" : dividerInput.Text;
                formatDisplay.Text = string.Join(divider1, selectedFormat);
            };
            form.Controls.Add(dividerInput);

            formatDisplay.Left = 10;
            formatDisplay.Top = 90;
            formatDisplay.Font = new System.Drawing.Font("Iosevka", 11, System.Drawing.FontStyle.Bold);
            formatDisplay.BackColor = System.Drawing.Color.Black;
            formatDisplay.ForeColor = System.Drawing.Color.White;
            formatDisplay.Width = form.ClientSize.Width - 20;
            formatDisplay.ReadOnly = true;

            form.Controls.Add(formatDisplay);

            System.Windows.Forms.Label dataLabel = new System.Windows.Forms.Label();
            dataLabel.Text = "Input data (one per line, matching format):";
            dataLabel.Font = formFont;
            dataLabel.AutoSize = true;
            dataLabel.Left = 10;
            dataLabel.Top = 120;
            form.Controls.Add(dataLabel);

            dataInput.Left = 10;
            dataInput.Top = 140;
            dataInput.Width = form.ClientSize.Width - 20;
            dataInput.Multiline = true;
            dataInput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            dataInput.BackColor = System.Drawing.Color.Black;
            dataInput.ForeColor = System.Drawing.Color.White;
            form.Controls.Add(dataInput);

            System.Windows.Forms.Button okButton = new System.Windows.Forms.Button();
            okButton.Text = "OK";
            okButton.Font = new System.Drawing.Font("Iosevka", 10, System.Drawing.FontStyle.Bold);
            okButton.BackColor = System.Drawing.Color.Black;
            okButton.ForeColor = System.Drawing.Color.LightGreen;
            okButton.Width = form.ClientSize.Width - 10;
            okButton.Height = 25;
            okButton.Left = (form.ClientSize.Width - okButton.Width) / 2;
            okButton.Top = form.ClientSize.Height - okButton.Height - 5;
            okButton.Click += (s, e) => { form.DialogResult = System.Windows.Forms.DialogResult.OK; form.Close(); };
            form.Controls.Add(okButton);
            dataInput.Height = okButton.Top - dataInput.Top - 5;

            form.Load += (s, e) => { form.Location = new System.Drawing.Point(108, 108); };
            form.FormClosing += (s, e) => { if (form.DialogResult != System.Windows.Forms.DialogResult.OK) form.DialogResult = System.Windows.Forms.DialogResult.Cancel; };

            form.ShowDialog();

            if (form.DialogResult != System.Windows.Forms.DialogResult.OK)
            {
                _project.SendInfoToLog($"Import to {tableName} cancelled by user", true);
                return "0";
            }

            selectedFormat.Clear();
            foreach (var combo in formatComboBoxes)
            {
                if (!string.IsNullOrEmpty(combo.SelectedItem?.ToString()))
                    selectedFormat.Add(combo.SelectedItem.ToString());
            }

            if (string.IsNullOrEmpty(dataInput.Text) || selectedFormat.Count == 0 || string.IsNullOrEmpty(dividerInput.Text))
            {
                _project.SendWarningToLog("Data, format, or divider cannot be empty");
                return "0";
            }

            string divider0 = dividerInput.Text == "\\t" ? "\t" : dividerInput.Text;

            string[] lines = dataInput.Text.Trim().Split('\n');
            _project.SendInfoToLog($"Parsing [{lines.Length}] {tableName} data lines", true);

            for (int acc0unt = 1; acc0unt <= lines.Length; acc0unt++)
            {
                string line = lines[acc0unt - 1].Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    _project.SendWarningToLog($"Line {acc0unt} is empty", false);
                    continue;
                }
                else
                {
                    string[] data_parts = line.Split(new[] { divider0 }, StringSplitOptions.None);
                    Dictionary<string, string> parsed_data = new Dictionary<string, string>();

                    for (int i = 0; i < selectedFormat.Count && i < data_parts.Length; i++)
                    {
                        parsed_data[selectedFormat[i]] = data_parts[i].Trim();
                    }

                    var queryParts = new List<string>();
                    foreach (var field in columnMapping.Keys)
                    {
                        string value = parsed_data.ContainsKey(field) ? parsed_data[field].Replace("'", "''") : "";
                        if (field == "CODE2FA" && value.Contains('/'))
                            value = value.Split('/').Last();
                        queryParts.Add($"{columnMapping[field]} = '{value}'");
                    }

                    try
                    {
                        Upd(string.Join(", ", queryParts), _tableName, last: false, acc: acc0unt);
                        lineCount++;
                    }
                    catch (Exception ex)
                    {
                        _project.SendWarningToLog($"Error processing line {acc0unt}: {ex.Message}", false);
                    }
                }
            }

            _project.SendInfoToLog($"[{lineCount}] records added to [{_tableName}]", true);
            return lineCount.ToString();
        }
        public string ImportKeys(string keyType, int startFrom = 1)
        {

            
            _tableName = $"_wallets";
            //int rangeEnd = _rangeEnd + 1;
            var acc0 = _project.Variables["acc0"];
            acc0.Value = startFrom.ToString();

            var blockchain = new Blockchain();

            var formFont = new System.Drawing.Font("Iosevka", 10); //System.Drawing.FontStyle.Bold

            // Создание формы
            System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            form.Text = $"Import {keyType} keys";
            form.Width = 420;
            form.Height = 700;
            form.TopMost = true; // Форма поверх всех окон
            form.Location = new System.Drawing.Point(108, 108);

            System.Windows.Forms.Label dataLabel = new System.Windows.Forms.Label();
            dataLabel.Text = $"Input {keyType} keys (one per line):";
            dataLabel.AutoSize = true;
            dataLabel.Left = 10;
            dataLabel.Top = 10;
            form.Controls.Add(dataLabel);

            System.Windows.Forms.TextBox dataInput = new System.Windows.Forms.TextBox();
            dataInput.Left = 10;
            dataInput.Top = 30;
            dataInput.Width = form.ClientSize.Width - 20;
            dataInput.Multiline = true;
            dataInput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            dataInput.MaxLength = 1000000;
            form.Controls.Add(dataInput);

            System.Windows.Forms.Button okButton = new System.Windows.Forms.Button();
            okButton.Text = "OK";
            okButton.Width = form.ClientSize.Width - 10;
            okButton.Height = 25;
            okButton.Left = (form.ClientSize.Width - okButton.Width) / 2;
            okButton.Top = form.ClientSize.Height - okButton.Height - 5;
            okButton.Click += (s, e) => { form.DialogResult = System.Windows.Forms.DialogResult.OK; form.Close(); };
            form.Controls.Add(okButton);
            dataInput.Height = okButton.Top - dataInput.Top - 5;

            form.Load += (s, e) => { form.Location = new System.Drawing.Point(108, 108); }; // Фиксируем позицию перед показом

            form.FormClosing += (s, e) => { if (form.DialogResult != System.Windows.Forms.DialogResult.OK) form.DialogResult = System.Windows.Forms.DialogResult.Cancel; };

            form.ShowDialog();

            if (form.DialogResult != System.Windows.Forms.DialogResult.OK)
            {
                _project.SendInfoToLog("Import cancelled by user", true);
                return "0";
            }

            if (string.IsNullOrEmpty(dataInput.Text))
            {
                _project.SendWarningToLog("Input could not be empty");
                return "0";
            }

            string[] lines = dataInput.Text.Trim().Split('\n');
            _project.SendInfoToLog($"Parsing [{lines.Length}] strings", false);

            for (int i = 0; i < lines.Length; i++)
            {
                string key = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(key)) continue;
                _project.acc0w(i + 1);
                try
                {
                    switch (keyType)
                    {
                        case "seed":
                            string encodedSeed = SAFU.Encode(_project, key);
                            Upd($"bip39 = '{encodedSeed}'", _tableName);
                            break;

                        case "evm":
                            string privateKey;
                            string address;

                            if (key.Split(' ').Length > 1)
                            {
                                var mnemonicObj = new Mnemonic(key);
                                var hdRoot = mnemonicObj.DeriveExtKey();
                                var derivationPath = new NBitcoin.KeyPath("m/44'/60'/0'/0/0");
                                privateKey = hdRoot.Derive(derivationPath).PrivateKey.ToHex();
                            }
                            else
                            {
                                privateKey = key;
                            }

                            string encodedEvmKey = SAFU.Encode(_project, privateKey);
                            address = blockchain.GetAddressFromPrivateKey(privateKey);
                            DbQ($@"UPDATE {_tableName} SET secp256k1 = '{encodedEvmKey}' WHERE acc0 = {acc0.Value};", true);
                            break;

                        case "sol":
                            string encodedSolKey = SAFU.Encode(_project, key);
                            DbQ($@"UPDATE {_tableName} SET base58 = '{encodedSolKey}' WHERE acc0 = {acc0.Value};", true);
                            break;

                        default:
                            _project.SendWarningToLog($"Unknown key type: {keyType}");
                            return lines.Length.ToString();
                    }

                    acc0.Value = (int.Parse(acc0.Value) + 1).ToString();
                }
                catch (Exception ex)
                {
                    _project.SendWarningToLog($"Error processing record {acc0.Value}: {ex.Message}", false);
                    acc0.Value = (int.Parse(acc0.Value) + 1).ToString();
                }
            }

            return lines.Length.ToString();
        }
        public string ImportAddresses(int startFrom = 1)
        {
            var tablename = "_addresses";
            //int rangeEnd = _rangeEnd;
            var acc0 = _project.Variables["acc0"];
            acc0.Value = startFrom.ToString();


            acc0.Value = "1";

            // Создание формы
            System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            form.Text = "Import Addresses";
            form.Width = 420;
            form.Height = 700;
            form.TopMost = true;
            form.Location = new System.Drawing.Point(108, 108);

            // Поле для ввода имени столбца
            System.Windows.Forms.Label columnLabel = new System.Windows.Forms.Label();
            columnLabel.Text = "Column name (e.g., evm, sol):";
            columnLabel.AutoSize = true;
            columnLabel.Left = 10;
            columnLabel.Top = 10;
            form.Controls.Add(columnLabel);

            System.Windows.Forms.TextBox columnInput = new System.Windows.Forms.TextBox();
            columnInput.Left = 10;
            columnInput.Top = 30;
            columnInput.Width = form.ClientSize.Width - 20;
            columnInput.Text = "input address label here ex: evm | apt |sol ";//_project.Variables["addressType"].Value; // Предполагаем, что переменная существует
            form.Controls.Add(columnInput);

            // Поле для ввода адресов
            System.Windows.Forms.Label addressLabel = new System.Windows.Forms.Label();
            addressLabel.Text = "Addresses (one per line):";
            addressLabel.AutoSize = true;
            addressLabel.Left = 10;
            addressLabel.Top = 60;
            form.Controls.Add(addressLabel);

            System.Windows.Forms.TextBox addressInput = new System.Windows.Forms.TextBox();
            addressInput.Left = 10;
            addressInput.Top = 80;
            addressInput.Width = form.ClientSize.Width - 20;
            addressInput.Multiline = true;
            addressInput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            addressInput.MaxLength = 1000000;
            form.Controls.Add(addressInput);

            // Кнопка "OK"
            System.Windows.Forms.Button okButton = new System.Windows.Forms.Button();
            okButton.Text = "OK";
            okButton.Width = form.ClientSize.Width - 20;
            okButton.Height = 25;
            okButton.Left = (form.ClientSize.Width - okButton.Width) / 2;
            okButton.Top = form.ClientSize.Height - okButton.Height - 5;
            okButton.Click += (s, e) => { form.DialogResult = System.Windows.Forms.DialogResult.OK; form.Close(); };
            form.Controls.Add(okButton);
            addressInput.Height = okButton.Top - addressInput.Top - 5;

            form.Load += (s, e) => { form.Location = new System.Drawing.Point(108, 108); };

            form.FormClosing += (s, e) => { if (form.DialogResult != System.Windows.Forms.DialogResult.OK) form.DialogResult = System.Windows.Forms.DialogResult.Cancel; };

            form.ShowDialog();

            if (form.DialogResult != System.Windows.Forms.DialogResult.OK)
            {
                _project.SendInfoToLog("Import cancelled by user", true);
                return "0";
            }

            if (string.IsNullOrEmpty(columnInput.Text) || string.IsNullOrEmpty(addressInput.Text))
            {
                _project.SendWarningToLog("Column name or addresses cannot be empty");
                return "0";
            }

            string columnName = columnInput.Text.ToLower();

            var tableStructure = new Dictionary<string, string>
            {
                {"acc0", "INTEGER PRIMARY KEY"},
                {columnName, "TEXT DEFAULT ''"}
            };
            TblAdd(tablename, tableStructure);

            string[] lines = addressInput.Text.Trim().Split('\n');
            int lineCount = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string address = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(address))
                {
                    _project.SendWarningToLog($"Line {acc0.Value} is empty");
                    acc0.Value = (int.Parse(acc0.Value) + 1).ToString();
                    continue;
                }

                try
                {
                    _project.SendInfoToLog($"Processing acc0 = {acc0.Value}, address = '{address}'", false);
                    Upd($"{columnName} = '{address}'", _tableName, last: false);
                    acc0.Value = (int.Parse(acc0.Value) + 1).ToString();
                    lineCount++;
                }
                catch (Exception ex)
                {
                    _project.SendWarningToLog($"Error processing record {acc0.Value} for {columnName}: {ex.Message}", false);
                    acc0.Value = (int.Parse(acc0.Value) + 1).ToString();
                }
            }

            _project.SendInfoToLog($"[{lineCount}] strings added to [{_tableName}]", true);
            return lineCount.ToString();
        }
        public string ImportDepositAddresses(int startFrom = 1)
        {
            _tableName = $"_deposits";

            _project.L0g($"{_tableName} `b");
            //int rangeEnd = _rangeEnd;
            var acc0 = _project.Variables["acc0"];
            acc0.Value = startFrom.ToString();


            System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            form.Text = "Import Deposit Addresses";
            form.Width = 420;
            form.Height = 700;
            form.TopMost = true; // Форма поверх всех окон
            //form.Location = new System.Drawing.Point(108, 108);

            // Поле для ввода CHAIN
            System.Windows.Forms.Label chainLabel = new System.Windows.Forms.Label();
            chainLabel.Text = "Chain (e.g., ETH, BSC):";
            chainLabel.AutoSize = true;
            chainLabel.Left = 10;
            chainLabel.Top = 10;
            form.Controls.Add(chainLabel);

            System.Windows.Forms.TextBox chainInput = new System.Windows.Forms.TextBox();
            chainInput.Left = 10;
            chainInput.Top = 30;
            chainInput.Width = form.ClientSize.Width - 20;
            chainInput.Text = "ETH";//_project.Variables["depositChain"].Value; // Текущее значение из переменной
            form.Controls.Add(chainInput);

            // Поле для ввода CEX
            System.Windows.Forms.Label cexLabel = new System.Windows.Forms.Label();
            cexLabel.Text = "CEX (e.g., binance, kucoin):";
            cexLabel.AutoSize = true;
            cexLabel.Left = 10;
            cexLabel.Top = 60;
            form.Controls.Add(cexLabel);

            System.Windows.Forms.TextBox cexInput = new System.Windows.Forms.TextBox();
            cexInput.Left = 10;
            cexInput.Top = 80;
            cexInput.Width = form.ClientSize.Width - 20;
            cexInput.Text = "OKX";//_project.Variables["depositCEX"].Value; // Текущее значение из переменной
            form.Controls.Add(cexInput);

            // Поле для ввода адресов
            System.Windows.Forms.Label addressLabel = new System.Windows.Forms.Label();
            addressLabel.Text = "Deposit addresses (one per line):";
            addressLabel.AutoSize = true;
            addressLabel.Left = 10;
            addressLabel.Top = 110;
            form.Controls.Add(addressLabel);

            System.Windows.Forms.TextBox addressInput = new System.Windows.Forms.TextBox();
            addressInput.Left = 10;
            addressInput.Top = 130;
            addressInput.Width = form.ClientSize.Width - 20;
            addressInput.Multiline = true;
            addressInput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            form.Controls.Add(addressInput);

            // Кнопка "OK"
            System.Windows.Forms.Button okButton = new System.Windows.Forms.Button();
            okButton.Text = "OK";
            okButton.Width = form.ClientSize.Width - 10;
            okButton.Height = 25;
            okButton.Left = (form.ClientSize.Width - okButton.Width) / 2;
            okButton.Top = form.ClientSize.Height - okButton.Height - 5;
            okButton.Click += (s, e) => { form.DialogResult = System.Windows.Forms.DialogResult.OK; form.Close(); };
            form.Controls.Add(okButton);
            addressInput.Height = okButton.Top - addressInput.Top - 5;
            form.Load += (s, e) => { form.Location = new System.Drawing.Point(108, 108); }; // Фиксируем позицию перед показом

            form.FormClosing += (s, e) => { if (form.DialogResult != System.Windows.Forms.DialogResult.OK) form.DialogResult = System.Windows.Forms.DialogResult.Cancel; };

            form.ShowDialog();

            if (form.DialogResult != System.Windows.Forms.DialogResult.OK)
            {
                _project.SendInfoToLog("Import cancelled by user", true);
                return "0";
            }

            if (string.IsNullOrEmpty(chainInput.Text) || string.IsNullOrEmpty(cexInput.Text) || string.IsNullOrEmpty(addressInput.Text))
            {
                _project.SendWarningToLog("Chain, CEX, or addresses cannot be empty");
                return "0";
            }

            string CHAIN = chainInput.Text.ToLower();
            string CEX = cexInput.Text.ToLower();
            string columnName = $"{CEX}_{CHAIN}";

            var tableStructure = new Dictionary<string, string>
            {
                {"acc0", "INTEGER PRIMARY KEY"},
                {columnName, "TEXT DEFAULT ''"}
            };
            ClmnAdd(_tableName, tableStructure);


            string[] lines = addressInput.Text.Trim().Split('\n');
            int lineCount = 0;

            AddRange(_tableName, lines.Length);

            for (int acc0index = startFrom; acc0index <= lines.Length; acc0index++)
            {
                string line = lines[acc0index - 1].Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    _project.SendWarningToLog($"Line {acc0index} is empty");
                    continue;
                }

                try
                {
                    acc0.Value = acc0index.ToString();
                    Upd($"{columnName} = '{line}'", _tableName, last: false);

                    lineCount++;
                }
                catch (Exception ex)
                {
                    _project.SendWarningToLog($"Error processing line {acc0}: {ex.Message}");
                    continue;
                }
            }

            _project.SendInfoToLog($"[{lineCount}] strings added to [{_tableName}]", true);
            return lineCount.ToString();
        }
        public void MapAndImport(schema tableSchem, int startFrom = 1)
        {
            _project.Variables["acc0"].Value = startFrom.ToString();

            var mapping = new Dictionary<string, string>();

            Log($"mapping {tableSchem}");
            switch (tableSchem)
            {

                case schema.private_twitter:
                    string[] twitterFields = new string[] { "", "LOGIN", "PASSWORD", "EMAIL", "EMAIL_PASSWORD", "TOKEN", "2FA_SECRET", "2FA_BACKUP" };
                    var twitterMapping = new Dictionary<string, string>
                    {
                        { "LOGIN", "login" },
                        { "PASSWORD", "password" },
                        { "EMAIL", "email" },
                        { "EMAIL_PASSWORD", "emailpass" },
                        { "TOKEN", "token" },
                        { "2FA_SECRET", "otpsecret" },
                        { "2FA_BACKUP", "otpbackup" }
                    };
                    ImportData(tableSchem.ToString(), twitterFields, twitterMapping, "Import Twitter Data");
                    return;


                case schema.private_discord:
                    string[] discordFields = new string[] { "", "LOGIN", "PASSWORD", "TOKEN", "2FA_SECRET" };
                    var discordMapping = new Dictionary<string, string>
                    {
                        { "LOGIN", "login" },
                        { "PASSWORD", "password" },
                        { "TOKEN", "token" },
                        { "2FA_SECRET", "otpsecret" }
                    };
                    ImportData(tableSchem.ToString(), discordFields, discordMapping, "Import Discord Data");
                    return;

                case schema.private_google:
                    string[] googleFields = new string[] { "", "LOGIN", "PASSWORD", "RECOVERY_EMAIL", "2FA_SECRET", "2FA_BACKUP" };
                    var googleMapping = new Dictionary<string, string>
                    {
                        { "LOGIN", "login" },
                        { "PASSWORD", "password" },
                        { "RECOVERY_EMAIL", "recoveryemail" },
                        { "2FA_SECRET", "otpsecret" },
                        { "2FA_BACKUP", "otpbackup" }
                    };
                    ImportData(tableSchem.ToString(), googleFields, googleMapping, "Import Google Data");
                    return;

                case schema.private_github:
                    string[] githubFields = new string[] { "", "LOGIN", "PASSWORD", "TOKEN", "EMAIL", "EMAIL_PASSWORD", "2FA_SECRET", "2FA_BACKUP" };
                    var githubMapping = new Dictionary<string, string>
                    {
                        { "LOGIN", "login" },
                        { "PASSWORD", "password" },

                        { "EMAIL", "email" },
                        { "EMAIL_PASSWORD", "emailpass" },

                        { "TOKEN", "token" },
                        { "2FA_SECRET", "otpsecret" },
                        { "2FA_BACKUP", "otpbackup" }
                    };
                    ImportData(tableSchem.ToString(), githubFields, githubMapping, "Import GitHub Data");
                    return;


                case schema.public_mail:
                    var icloud = _f0rm.GetLinesByKey("icloud", "input data, don't change key!");
                    Upd(icloud, tableSchem.ToString(), last: false);
                    return;

                case schema.public_profile:
                    var nicknames = _f0rm.GetLinesByKey("nickname", "input data, don't change key!");
                    Upd(nicknames, tableSchem.ToString());
                    var bio = _f0rm.GetLinesByKey("bio", "input data, don't change key!");
                    Upd(bio, tableSchem.ToString(), last: false);
                    return;

                case schema.private_profile:
                    var proxy = _f0rm.GetLinesByKey("proxy", "input proxy, don't change key!");
                    if (proxy.Count != 0)
                    {
                        Upd(proxy, tableSchem.ToString(), last: false);
                    }
                    else Log("!W empty input");

                    var vendors = new List<string> {
                     "NVIDIA",
                     "AMD",
                     "Intel",
                     };

                    var vendor = _f0rm.GetSelectedItem(vendors);

                    var versions = new List<string> {
                     "my zennoposter version >= 7.8.0",
                     "my zennoposter version <= 7.7.21",
                     };

                    string version = _f0rm.GetSelectedItem(versions);

                    bool before78 = (version.Contains("7.7.21"));

                    if (!string.IsNullOrEmpty(vendor))
                    {
                        var webGl = ParseWebGl(vendor, _range, before78);//_instance.ParseWebGl(vendor, _range, _project);
                        Upd(webGl, "webgl", tableSchem.ToString(), last: false);
                    }
                    else Log("!W empty input");

                    return;

                case schema.public_blockchain:

                    var acc0 = 1;
                    _project.acc0w(acc0);
                    while (true)
                    {
                        _project.acc0w(acc0);
                        var pk = Key("evm");
                        if (string.IsNullOrEmpty(pk)) break;
                        var addrs = pk.ToPubEvm();
                        Upd($"evm_pk = '{addrs}'", "public_blockchain");
                        acc0++;
                    }

                    acc0 = 1;
                    while (true)
                    {
                        _project.acc0w(acc0);
                        var pk = Key("seed");
                        if (string.IsNullOrEmpty(pk)) break;
                        var addrs = pk.ToPubEvm();
                        Upd($"evm_seed = '{addrs}'", "public_blockchain");
                        acc0++;
                    }

                    return;

                case schema.private_blockchain:
                    ImportKeys("seed");
                    ImportKeys("evm");
                    ImportKeys("sol");
                    return;

                case schema.public_deposits:
                    ImportDepositAddresses();
                    return;

                case schema.private_settings:
                    var phK = new List<string> {
                        "discord_invite",
                        "discord_hub",
                        "tg_logger_token",
                        "tg_logger_group",
                        "tg_logger_topic",
                        "profiles_folder"
                        };


                    var phV = new List<string> {
                        "Инвайт на свой сервер",
                        "ссылка на канал с инвайтами на вашем сервере",
                        "Токен Telegram логгера",
                        "Id группы для логов в Telegram. Формат {-1002000000009}",
                        "Id топика в группе для логов. 0 - если нет топиков",
                        "Путь к папке с профилями и причастным данным. Формат: {F:\\farm\\}"
                        };


                    var settings = _f0rm.GetKeyValuePairs(phK.Count(), phK, phV, "input settings", prepareUpd: false);
                    Write(settings, tableSchem.ToString());
                    return;

                case schema.private_api:
                    phK = new List<string> { "apikey", "apisecret", "proxy", };
                    var toWrite = _f0rm.GetKeyValueString(phK.Count(), phK, null, "input binance api");
                    UpdTxt(toWrite, tableSchem.ToString(), "binance");

                    phK = new List<string> { "apikey", "apisecret", "passphrase", };
                    toWrite = _f0rm.GetKeyValueString(phK.Count(), phK, null, "input okx api");
                    UpdTxt(toWrite, tableSchem.ToString(), "okx");

                    phK = new List<string> { "apikey", "apisecret", "passphrase", };
                    toWrite = _f0rm.GetKeyValueString(phK.Count(), phK, null, "input firstmail login as apisecret)");
                    UpdTxt(toWrite, tableSchem.ToString(), "firstmail");

                    phK = new List<string> { "apikey", };
                    toWrite = _f0rm.GetKeyValueString(phK.Count(), phK, null, "input perplexity api");
                    UpdTxt(toWrite, tableSchem.ToString(), "perplexity");

                    return;

                case schema.public_rpc:
                case schema.public_native:
                case schema.public_google:
                case schema.public_twitter:
                case schema.public_discord:
                    Log($"{tableSchem.ToString()} is for manual fill or not compoulsary");

                    return;


                default:
                    throw new Exception($"no schema [{tableSchem}]");

            }

        }
       
        public void ImportDB(schema? schemaValue = null)
        {
            if (_project.Var("cfgBuildDB") != "True") return;
            
            try
            {
               // new Sql(_project).DbQ("CREATE SCHEMA IF NOT EXISTS private;");
                new Sql(_project).DbQ("CREATE SCHEMA IF NOT EXISTS public;");
               // new Sql(_project).DbQ("CREATE SCHEMA IF NOT EXISTS projects;");
            }
            catch (Exception ex) { }

            var phK = new List<string> {
                "_settings",
                "_instance",
                "_wallets",
                "_api",

                "_addresses",
                "_native",
                "_deposits",


                "_google",
                "_twitter",
                "_discord",
                "_github",
             


                "_profile",
                "_rpc",
                "_mail",
                
            };

            var phV = new List<string> {
                "Folders, loggers & etc. general settings",
                "Proxy, cookies, webgl",
                "Blockchain keys, seeds, etc. ",
                "Api keys ",

                "Blockchain addresses",
                "Balancees",
                "CEX deposit addresses",


                "Google Credentials",
                "Twitter Credentials",
                "Discord Credentials",
                "GitHub Credentials",

                "Username, bio, pfp, etc.",
                "Custom RPC & Explorers list",
                "All emails",

            };

            if (schemaValue.HasValue)
            {
                string tablename = schemaValue.ToString();
                if (!Enum.IsDefined(typeof(schema), schemaValue.Value))
                    throw new Exception($"Error: '{tablename}' is not a valid schema value. Valid values: {string.Join(", ", Enum.GetNames(typeof(schema)))}");

                var dic = LoadSchema(schemaValue.Value);
                TblAdd(tablename, dic);
                try
                {
                    AddRange(tablename);
                }
                catch { }
                MapAndImport(schemaValue.Value);
            }
            else
            {
                var import = _f0rm.GetKeyBoolPairs(phK.Count(), phK, phV, "check boxes to import", prepareUpd: false);

                foreach (KeyValuePair<string, bool> pair in import)
                {
                    string tablename = pair.Key;
                    bool execute = pair.Value;

                    var table = Enum.TryParse<schema>(pair.Key, true, out var parsed) ? parsed : default;
                    if (!Enum.IsDefined(typeof(schema), table))
                        throw new Exception($"Error: '{tablename}' is not a valid schema value. Valid values: {string.Join(", ", Enum.GetNames(typeof(schema)))}");

                    if (execute)
                    {
                        var dic = LoadSchema(table);
                        TblAdd(tablename, dic);
                        try
                        {
                            AddRange(tablename);
                        }
                        catch { }
                        MapAndImport(table);
                    }
                }
            }
        }

        public List<string> ParseWebGl( string vendor, int qnt, bool before78 = false)
        {
            if (_instance is null) throw new Exception("instance required but not provided. Probably MethosOverload mistake");
            var result = new List<string>();
            _instance.CanvasRenderMode = CanvasMode.Emulate;
            var alterBrowser = BrowserType.Firefox45;
            if (before78) alterBrowser = BrowserType.WithoutBrowser;
            while (result.Count < qnt)
            {
                if (_instance.BrowserType == BrowserType.Chromium)
                    try { _instance.Launch(alterBrowser, false); }
                    catch (Exception ex) { _project.L0g($"Error launching: {ex.Message}"); }

                _instance.Launch(BrowserType.Chromium, false);

                string webglData = _instance.WebGLPreferences.Save();
                if (webglData.Contains(vendor))
                {
                    result.Add(webglData);
                    _project.L0g($"{result.Count}/{qnt} strings collected");
                }
                Thread.Sleep(1000);
            }
            _instance.CanvasRenderMode = CanvasMode.Block;
            return result;
        }

        public void Upd(Dictionary<string, string> toWrite, string tableName = null, bool log = false, bool throwOnEx = false, bool last = true, bool byKey = false)
        {

            int dicSize = toWrite.Count;
            AddRange(_tableName, dicSize);

            foreach (KeyValuePair<string, string> pair in toWrite)
            {
                string key = pair.Key;
                string value = pair.Value;
                new Sql(_project).Upd(value, tableName, last: last, acc: key);
            }
        }


    }
}
