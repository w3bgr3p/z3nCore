using System.Collections.Generic;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System.Linq;
using System;


namespace z3nCore
{
    public static class DBuilder
    {

        public static string[] Columns(string tableSchem)
        {

            switch (tableSchem)
            {
                
                case "_google":
                    return new string[] { "status", "last", "cookies", "login", "password", "otpsecret", "otpbackup", "recoveryemail", "recovery_phone" };
                case "_twitter":
                    return new string[] { "status", "last", "cookies", "token", "login", "password", "otpsecret", "otpbackup", "email", "emailpass" };
                case "_discord":
                    return new string[] { "status", "last", "token", "login", "password", "otpsecret", "otpbackup", "email", "emailpass", "recovery_phone" };
                case "_github":
                    return new string[] { "status", "last", "cookies", "token", "login", "password", "otpsecret", "otpbackup", "email", "emailpass" };
                
                
                case "_addresses":
                    return new string[] { "evm_pk", "sol_pk", "apt_pk", "evm_seed" };
                case "_wallets":
                    return new string[] { "secp256k1", "base58", "bip39" };

                
                case "_settings":
                    return new string[] { "value" };
                case "_api":
                    return new string[] { "apikey", "apisecret", "passphrase", "proxy", "extra" };
                case "_instance":
                    return new string[] { "proxy", "cookies", "webgl", "zb_id" };


                case "_profile":
                    return new string[] { "nickname", "bio", "brsr_score" };

                case "_rpc":
                    return new string[] { "rpc", "explorer", "explorer_api" };

                case "_mail":
                    return new string[] { "google", "icloud", "firstmail" };

                default:
                    return new string[] { };
            }

        }
        
        public static List<string> GetLines(this IZennoPosterProjectModel project, string title = "Input lines")
        {
            var result = new List<string>();
            // Создание формы
            System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            form.Text = "Input lines";
            form.Width = 800;
            form.Height = 700;
            form.TopMost = true;
            form.Location = new System.Drawing.Point(108, 108);

            System.Windows.Forms.Label addressLabel = new System.Windows.Forms.Label();
            addressLabel.Text = title;
            addressLabel.AutoSize = true;
            //addressLabel.Font = new System.Drawing.Font("Iosevka", 10, System.Drawing.FontStyle.Bold);
            addressLabel.Left = 10;
            addressLabel.Top = 10;
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
            okButton.Font = new System.Drawing.Font("Iosevka", 10, System.Drawing.FontStyle.Bold);

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
                //_project.SendInfoToLog("Import cancelled by user", true);
                return null;
            }
            
            var lines = addressInput.Text.Trim().Split('\n').ToList();
            return lines;
            
        }
        
        public static void CreateBasicTable(this IZennoPosterProjectModel project,string table, bool log = false)
        {
            var columns = DBuilder.Columns(table);
            
            string primary = "INTEGER PRIMARY KEY";
            string defaultType = "TEXT DEFAULT ''";
	
            if (table == "_settings" || table == "_api" || table == "_rpc" ) 
                primary = "TEXT PRIMARY KEY";
	
	
            var tableStructure = new Dictionary<string, string>
            {
                { "id", primary },
            };
			
            foreach(string column  in columns)
                tableStructure.Add(column,defaultType);	
		
		
            bool exist = project.TblExist (table, log);
            if (!exist) project.TblAdd(tableStructure,table, log);
            
            
        }
        public static Dictionary<string, bool> FormKeyBool(this IZennoPosterProjectModel project,int quantity, List<string> keyPlaceholders = null, List<string> valuePlaceholders = null, string title = "Input Key-Bool Pairs", bool prepareUpd = true)
        {
            var _project = project;
            var result = new System.Collections.Generic.Dictionary<string, bool>();
            
            System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            form.Text = title;
            form.Width = 600;
            form.Height = 40 + quantity * 35; 
            form.TopMost = true;
            form.Location = new System.Drawing.Point(108, 108);

            var keyLabels = new System.Windows.Forms.Label[quantity];
            var keyTextBoxes = new System.Windows.Forms.TextBox[quantity];
            var valueCheckBoxes = new System.Windows.Forms.CheckBox[quantity];

            int currentTop = 5;
            int labelWidth = 40;
            int keyBoxWidth = 40;
            int checkBoxWidth = 370; 
            int spacing = 5;

            for (int i = 0; i < quantity; i++)
            {
                System.Windows.Forms.Label keyLabel = new System.Windows.Forms.Label();

                string keyDefault = keyPlaceholders != null && i < keyPlaceholders.Count && !string.IsNullOrEmpty(keyPlaceholders[i]) ? keyPlaceholders[i] : $"key{i + 1}";
                keyLabel.Text = keyDefault; //
                //keyTextBoxes[i] = keyDefault;
                //keyLabel.Text = $"Key:";
                keyLabel.AutoSize = true;
                keyLabel.Left = 5;
                keyLabel.Top = currentTop + 5; 
                form.Controls.Add(keyLabel);
                keyLabels[i] = keyLabel;

                System.Windows.Forms.TextBox keyTextBox = new System.Windows.Forms.TextBox();
                keyTextBox.Left = keyLabel.Left + labelWidth + spacing;
                keyTextBox.Top = currentTop;
                keyTextBox.Width = keyBoxWidth;


                System.Windows.Forms.CheckBox valueCheckBox = new System.Windows.Forms.CheckBox();
                valueCheckBox.Left = keyTextBox.Left + keyBoxWidth + spacing + 10;
                valueCheckBox.Top = currentTop;
                valueCheckBox.Width = checkBoxWidth;
                string valueDefault = valuePlaceholders != null && i < valuePlaceholders.Count && !string.IsNullOrEmpty(valuePlaceholders[i]) ? valuePlaceholders[i] : $"Option{i + 1}";
                valueCheckBox.Text = valueDefault; 
                valueCheckBox.Checked = false; 



                System.Windows.Forms.Label valueLabel = new System.Windows.Forms.Label();
                valueLabel.Text = $"";
                valueLabel.AutoSize = true;
                valueLabel.Left = valueLabel.Left + labelWidth + spacing;

                valueLabel.Top = currentTop + 5; 
                form.Controls.Add(valueLabel);



                form.Controls.Add(valueCheckBox);
                valueCheckBoxes[i] = valueCheckBox;

                currentTop += valueCheckBox.Height + spacing;
            }

         
            System.Windows.Forms.Button okButton = new System.Windows.Forms.Button();
            okButton.Text = "OK";
            okButton.Width = form.ClientSize.Width - 20;
            okButton.Height = 25;
            okButton.Left = (form.ClientSize.Width - okButton.Width) / 2;
            okButton.Top = currentTop + 10;
            okButton.Click += (s, e) => { form.DialogResult = System.Windows.Forms.DialogResult.OK; form.Close(); };
            form.Controls.Add(okButton);

            int requiredHeight = okButton.Top + okButton.Height + 40;
            if (form.Height < requiredHeight)
            {
                form.Height = requiredHeight;
            }

            form.Load += (s, e) => { form.Location = new System.Drawing.Point(108, 108); };
            form.FormClosing += (s, e) => { if (form.DialogResult != System.Windows.Forms.DialogResult.OK) form.DialogResult = System.Windows.Forms.DialogResult.Cancel; };

            form.ShowDialog();

            if (form.DialogResult != System.Windows.Forms.DialogResult.OK)
            {
                _project.SendInfoToLog("Input cancelled by user", true);
                return null;
            }
//LOGIC
            int lineCount = 0;
            for (int i = 0; i < quantity; i++)
            {
                string key = keyLabels[i].Text.ToLower().Trim();
                bool value = valueCheckBoxes[i].Checked;

                if (string.IsNullOrEmpty(key))
                {
                    //_project.SendWarningToLog($"Pair {i + 1} skipped: empty key");
                    continue;
                }

                try
                {
                    string dictKey = prepareUpd ? (i + 1).ToString() : key;
                    result.Add(dictKey, value);
                    //_project.SendInfoToLog($"Added to dictionary: [{dictKey}] = [{value}]", false);
                    lineCount++;
                }
                catch (System.Exception ex)
                {
                    _project.SendWarningToLog($"Error adding pair {i + 1}: {ex.Message}");
                }
            }

            if (lineCount == 0)
            {
                _project.SendWarningToLog("No valid key-value pairs entered");
                return null;
            }

            return result;
        }
        public static Dictionary<string, string> FormKeyString(this IZennoPosterProjectModel project, int quantity, List<string> keyPlaceholders = null, List<string> valuePlaceholders = null, string title = "Input Key-Value Pairs", bool prepareUpd = true)
        {
            var _project = project;
            var result = new Dictionary<string, string>();

            // Создание формы
            System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            form.Text = title;
            form.Width = 600;
            form.Height = 40 + quantity * 35; // Адаптивная высота в зависимости от количества полей
            form.TopMost = true;
            form.Location = new System.Drawing.Point(108, 108);

            // Список для хранения текстовых полей
            var keyTextBoxes = new System.Windows.Forms.TextBox[quantity];
            var valueTextBoxes = new System.Windows.Forms.TextBox[quantity];

            int currentTop = 5;
            int labelWidth = 40;
            int keyBoxWidth = 100;
            int valueBoxWidth = 370;
            int spacing = 5;

            // Создаём поля для ключей и значений
            for (int i = 0; i < quantity; i++)
            {
                // Метка для ключа
                System.Windows.Forms.Label keyLabel = new System.Windows.Forms.Label();
                keyLabel.Text = $"Key:";
                keyLabel.AutoSize = true;
                keyLabel.Left = 5;
                keyLabel.Top = currentTop + 5; // Смещение для центрирования
                form.Controls.Add(keyLabel);

                // Поле для ключа
                System.Windows.Forms.TextBox keyTextBox = new System.Windows.Forms.TextBox();
                keyTextBox.Left = keyLabel.Left + labelWidth + spacing;
                keyTextBox.Top = currentTop;
                keyTextBox.Width = keyBoxWidth;


                string keyDefault = keyPlaceholders != null && i < keyPlaceholders.Count && !string.IsNullOrEmpty(keyPlaceholders[i]) ? keyPlaceholders[i] : $"key{i + 1}";
                keyTextBox.Text = keyDefault; //

                form.Controls.Add(keyTextBox);
                keyTextBoxes[i] = keyTextBox;

                // Метка для значения
                System.Windows.Forms.Label valueLabel = new System.Windows.Forms.Label();
                valueLabel.Text = $"Value:";
                valueLabel.AutoSize = true;
                valueLabel.Left = keyTextBox.Left + keyBoxWidth + spacing + 10;
                valueLabel.Top = currentTop + 5; // Смещение для центрирования
                form.Controls.Add(valueLabel);

                // Поле для значения
                System.Windows.Forms.TextBox valueTextBox = new System.Windows.Forms.TextBox();
                valueTextBox.Left = valueLabel.Left + labelWidth + spacing;
                valueTextBox.Top = currentTop;
                valueTextBox.Width = valueBoxWidth;

                // Установка плейсхолдера, если placeholders не null
                string placeholder = valuePlaceholders != null && i < valuePlaceholders.Count ? valuePlaceholders[i] : "";
                if (!string.IsNullOrEmpty(placeholder))
                {
                    valueTextBox.Text = placeholder;
                    valueTextBox.ForeColor = System.Drawing.Color.Gray;
                    valueTextBox.Enter += (s, e) => { if (valueTextBox.Text == placeholder) { valueTextBox.Text = ""; valueTextBox.ForeColor = System.Drawing.Color.Black; } };
                    valueTextBox.Leave += (s, e) => { if (string.IsNullOrEmpty(valueTextBox.Text)) { valueTextBox.Text = placeholder; valueTextBox.ForeColor = System.Drawing.Color.Gray; } };
                }

                form.Controls.Add(valueTextBox);
                valueTextBoxes[i] = valueTextBox;

                currentTop += valueTextBox.Height + spacing;
            }

            // Кнопка "OK"
            System.Windows.Forms.Button okButton = new System.Windows.Forms.Button();
            okButton.Text = "OK";
            okButton.Width = form.ClientSize.Width - 20;
            okButton.Height = 25;
            okButton.Left = (form.ClientSize.Width - okButton.Width) / 2;
            okButton.Top = currentTop + 10;
            okButton.Click += (s, e) => { form.DialogResult = System.Windows.Forms.DialogResult.OK; form.Close(); };
            form.Controls.Add(okButton);

            // Адаптируем высоту формы
            int requiredHeight = okButton.Top + okButton.Height + 40;
            if (form.Height < requiredHeight)
            {
                form.Height = requiredHeight;
            }

            form.Load += (s, e) => { form.Location = new System.Drawing.Point(108, 108); };
            form.FormClosing += (s, e) => { if (form.DialogResult != System.Windows.Forms.DialogResult.OK) form.DialogResult = System.Windows.Forms.DialogResult.Cancel; };

            // Показываем форму
            form.ShowDialog();

            if (form.DialogResult != System.Windows.Forms.DialogResult.OK)
            {
                _project.SendInfoToLog("Input cancelled by user", true);
                return null;
            }


            if (prepareUpd)
            {
                // Формируем словарь
                int lineCount = 0;
                for (int i = 0; i < quantity; i++)
                {
                    string key = keyTextBoxes[i].Text.ToLower().Trim();
                    string value = valueTextBoxes[i].Text.Trim();
                    string placeholder = valuePlaceholders != null && i < valuePlaceholders.Count ? valuePlaceholders[i] : "";

                    if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(value) || value == placeholder)
                    {
                        _project.SendWarningToLog($"Pair {i + 1} skipped: empty key or value");
                        continue;
                    }

                    try
                    {
                        string dictKey = (i + 1).ToString();
                        string dictValue = $"{key} = '{value.Replace("'", "''")}'";
                        //result.Add(dictKey, dictValue);
                        //_project.SendInfoToLog($"Added to dictionary: [{dictKey}] = [{dictValue}]", false);
                        result.Add(key, value);
                        _project.SendInfoToLog($"Added to dictionary: [{key}] : [{value}]", false);
                        lineCount++;
                    }
                    catch (System.Exception ex)
                    {
                        _project.SendWarningToLog($"Error adding pair {i + 1}: {ex.Message}");
                    }
                }

                if (lineCount == 0)
                {
                    _project.SendWarningToLog("No valid key-value pairs entered");
                    return null;
                }

            }
            else
            {
                int lineCount = 0;
                for (int i = 0; i < quantity; i++)
                {
                    string key = keyTextBoxes[i].Text.ToLower().Trim();
                    string value = valueTextBoxes[i].Text.Trim();
                    string placeholder = valuePlaceholders != null && i < valuePlaceholders.Count ? valuePlaceholders[i] : "";

                    if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(value) || value == placeholder)
                    {
                        _project.SendWarningToLog($"Pair {i + 1} skipped: empty key or value");
                        continue;
                    }

                    try
                    {
                        result.Add(key, value);
                        _project.SendInfoToLog($"Added to dictionary: [{key}] = [{value}]", false);
                        lineCount++;
                    }
                    catch (System.Exception ex)
                    {
                        _project.SendWarningToLog($"Error adding pair {i + 1}: {ex.Message}");
                    }
                }

                if (lineCount == 0)
                {
                    _project.SendWarningToLog("No valid key-value pairs entered");
                    return null;
                }

            }


            return result;
        }
        
        public static string FormSocial(this IZennoPosterProjectModel project, string tableName, string formTitle, string[] availableFields, Dictionary<string, string> columnMapping, string message = "Select format (one field per box):")
        {
            var _project = project;
            

            string table = tableName;
            int lineCount = 0;


            System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            form.Text = formTitle;
            form.Width = 800;
            form.Height = 700;
            form.TopMost = true; 
            form.Location = new System.Drawing.Point(108, 108);

            List<string> selectedFormat = new List<string>();
            System.Windows.Forms.TextBox formatDisplay = new System.Windows.Forms.TextBox();
            System.Windows.Forms.TextBox dataInput = new System.Windows.Forms.TextBox();


            System.Windows.Forms.Label formatLabel = new System.Windows.Forms.Label();
            formatLabel.Text = message;
            formatLabel.AutoSize = true;
            formatLabel.Left = 10;
            formatLabel.Top = 10;
            form.Controls.Add(formatLabel);


            System.Windows.Forms.ComboBox[] formatComboBoxes = new System.Windows.Forms.ComboBox[availableFields.Length - 1]; // -1 из-за пустой строки
            int spacing = 5;
            int totalSpacing = spacing * (formatComboBoxes.Length - 1);
            int comboWidth = (form.ClientSize.Width - 20 - totalSpacing) / formatComboBoxes.Length;
            for (int i = 0; i < formatComboBoxes.Length; i++)
            {
                formatComboBoxes[i] = new System.Windows.Forms.ComboBox();
                formatComboBoxes[i].DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
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
                    formatDisplay.Text = string.Join(":", selectedFormat);
                };
                form.Controls.Add(formatComboBoxes[i]);
            }


            formatDisplay.Left = 10;
            formatDisplay.Top = 60;
            formatDisplay.Width = form.ClientSize.Width - 20;
            formatDisplay.ReadOnly = true;
            form.Controls.Add(formatDisplay);


            System.Windows.Forms.Label dataLabel = new System.Windows.Forms.Label();
            dataLabel.Text = "Input data (one per line, matching format):";
            dataLabel.AutoSize = true;
            dataLabel.Left = 10;
            dataLabel.Top = 90;
            form.Controls.Add(dataLabel);

            dataInput.Left = 10;
            dataInput.Top = 110;
            dataInput.Width = form.ClientSize.Width - 20;
            dataInput.Multiline = true;
            dataInput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            form.Controls.Add(dataInput);

            // Кнопка "OK"
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
                _project.SendInfoToLog($"Import to {tableName} cancelled by user", true);
                return "0";
            }


            selectedFormat.Clear();
            foreach (var combo in formatComboBoxes)
            {
                if (!string.IsNullOrEmpty(combo.SelectedItem?.ToString()))
                    selectedFormat.Add(combo.SelectedItem.ToString());
            }


            if (string.IsNullOrEmpty(dataInput.Text) || selectedFormat.Count == 0)
            {
                _project.SendWarningToLog("Data or format cannot be empty");
                return "0";
            }

            string[] lines = dataInput.Text.Trim().Split('\n');
            _project.SendInfoToLog($"Parsing [{lines.Length}] {tableName} data lines", true);


            for (int acc0 = 1; acc0 <= lines.Length; acc0++)
            {
                string line = lines[acc0 - 1].Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    _project.SendWarningToLog($"Line {acc0} is empty", false);
                    continue;
                }
                else
                {
                    string[] data_parts = line.Split(':');
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
                        string dbQuery = $@"UPDATE {table} SET {string.Join(", ", queryParts)} WHERE id = {acc0};";
                        project.DbQ(dbQuery, true);
                        lineCount++;
                    }
                    catch (Exception ex)
                    {
                        _project.SendWarningToLog($"Error processing line {acc0}: {ex.Message}", false);
                    }
                }
            }

            _project.SendInfoToLog($"[{lineCount}] records added to [{table}]", true);
            return lineCount.ToString();
        }
        public static string FormSocial(this IZennoPosterProjectModel project, List<string> availableFields, string tableName, string formTitle, string message = "Select format (one field per box):")
        {
            var _project = project;
            
            string table = tableName;
            int lineCount = 0;

            System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            form.Text = formTitle;
            form.Width = 800;
            form.Height = 750;
            form.TopMost = true; 
            form.Location = new System.Drawing.Point(108, 108);
            form.Font = new System.Drawing.Font("Cascadia Mono SemiBold", 10);
            
            List<string> selectedFormat = new List<string>();
            System.Windows.Forms.TextBox formatDisplay = new System.Windows.Forms.TextBox();
            System.Windows.Forms.TextBox dataInput = new System.Windows.Forms.TextBox();

            System.Windows.Forms.Label formatLabel = new System.Windows.Forms.Label();
            formatLabel.Text = message;
            formatLabel.AutoSize = true;
            formatLabel.Left = 10;
            formatLabel.Top = 10;
            form.Controls.Add(formatLabel);
            
            // Поле для разделителя
            System.Windows.Forms.Label separatorLabel = new System.Windows.Forms.Label();
            separatorLabel.Text = "Separator:";
            separatorLabel.AutoSize = true;
            separatorLabel.Left = 10;
            separatorLabel.Top = 90;
            form.Controls.Add(separatorLabel);

            System.Windows.Forms.TextBox separatorInput = new System.Windows.Forms.TextBox();
            separatorInput.Left = 100;
            separatorInput.Top = 88;
            separatorInput.Width = 25;
            separatorInput.Text = ":";
            form.Controls.Add(separatorInput);

            System.Windows.Forms.ComboBox[] formatComboBoxes = new System.Windows.Forms.ComboBox[availableFields.Count - 1]; // -1 из-за пустой строки
            int spacing = 5;
            int totalSpacing = spacing * (formatComboBoxes.Length - 1);
            int comboWidth = (form.ClientSize.Width - 20 - totalSpacing) / formatComboBoxes.Length;
            for (int i = 0; i < formatComboBoxes.Length; i++)
            {
                formatComboBoxes[i] = new System.Windows.Forms.ComboBox();
                formatComboBoxes[i].DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
                formatComboBoxes[i].Items.AddRange(availableFields.ToArray()); // Преобразуем List в Array
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
                    formatDisplay.Text = string.Join(separatorInput.Text, selectedFormat);
                };
                form.Controls.Add(formatComboBoxes[i]);
            }

            formatDisplay.Left = 10;
            formatDisplay.Top = 60;
            formatDisplay.Width = form.ClientSize.Width - 20;
            formatDisplay.ReadOnly = true;
            form.Controls.Add(formatDisplay);



            System.Windows.Forms.Label dataLabel = new System.Windows.Forms.Label();
            dataLabel.Text = "Input data (one per line, matching format):";
            dataLabel.AutoSize = true;
            dataLabel.Left = 10;
            dataLabel.Top = 120;
            form.Controls.Add(dataLabel);

            dataInput.Left = 10;
            dataInput.Top = 140;
            dataInput.Width = form.ClientSize.Width - 20;
            dataInput.Multiline = true;
            dataInput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            form.Controls.Add(dataInput);

            // Кнопка "OK"
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
                _project.SendInfoToLog($"Import to {tableName} cancelled by user", true);
                return "0";
            }

            selectedFormat.Clear();
            foreach (var combo in formatComboBoxes)
            {
                if (!string.IsNullOrEmpty(combo.SelectedItem?.ToString()))
                    selectedFormat.Add(combo.SelectedItem.ToString());
            }

            string separator = separatorInput.Text;
            if (string.IsNullOrEmpty(separator))
            {
                _project.SendWarningToLog("Separator cannot be empty");
                return "0";
            }

            if (string.IsNullOrEmpty(dataInput.Text) || selectedFormat.Count == 0)
            {
                _project.SendWarningToLog("Data or format cannot be empty");
                return "0";
            }

            string[] lines = dataInput.Text.Trim().Split('\n');
            _project.SendInfoToLog($"Parsing [{lines.Length}] {tableName} data lines with separator '{separator}'", true);

            project.AddRange(tableName, lines.Length);
            
            for (int acc0 = 1; acc0 <= lines.Length; acc0++)
            {
                string line = lines[acc0 - 1].Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    _project.SendWarningToLog($"Line {acc0} is empty", false);
                    continue;
                }
                else
                {
                    string[] data_parts = line.Split(new string[] { separator }, StringSplitOptions.None);
                    Dictionary<string, string> parsed_data = new Dictionary<string, string>();

                    for (int i = 0; i < selectedFormat.Count && i < data_parts.Length; i++)
                    {
                        parsed_data[selectedFormat[i]] = data_parts[i].Trim();
                    }

                    var queryParts = new List<string>();

                    foreach (var field in availableFields)
                    {
                        if (string.IsNullOrEmpty(field)) continue; 
                        
                        string value = parsed_data.ContainsKey(field) ? parsed_data[field].Replace("'", "''") : "";
                        if (field == "CODE2FA" && value.Contains('/'))
                            value = value.Split('/').Last();
                        queryParts.Add($"{field} = '{value}'");
                    }
                    
                    try
                    {
                        string dbQuery = $@"UPDATE {table} SET {string.Join(", ", queryParts)} WHERE id = {acc0};";
                        project.DbQ(dbQuery, true);
                        lineCount++;
                    }
                    catch (Exception ex)
                    {
                        _project.SendWarningToLog($"Error processing line {acc0}: {ex.Message}", false);
                    }
                }
            }

            _project.SendInfoToLog($"[{lineCount}] records added to [{table}]", true);
            return lineCount.ToString();
        }
        public static string FormSocial(this IZennoPosterProjectModel project, string tableName, string formTitle, string message = "Enter mask format using {field} syntax:")
            {
                var _project = project;
                
                string table = tableName;
                int lineCount = 0;

                System.Windows.Forms.Form form = new System.Windows.Forms.Form();
                form.Text = formTitle;
                form.Width = 800;
                form.Height = 600;
                form.TopMost = true; 
                form.Location = new System.Drawing.Point(108, 108);

                // Поле для ввода маски
                System.Windows.Forms.Label maskLabel = new System.Windows.Forms.Label();
                maskLabel.Text = message;
                maskLabel.AutoSize = true;
                maskLabel.Left = 10;
                maskLabel.Top = 10;
                form.Controls.Add(maskLabel);

                System.Windows.Forms.TextBox maskInput = new System.Windows.Forms.TextBox();
                maskInput.Left = 10;
                maskInput.Top = 30;
                maskInput.Width = form.ClientSize.Width - 20;
                maskInput.Height = 20;
                maskInput.Text = "{login}:{password}:{email}:{emailpass}:{token}:{code2fa}:{recovery2fa}";
                form.Controls.Add(maskInput);

                // Поле для ввода данных
                System.Windows.Forms.Label dataLabel = new System.Windows.Forms.Label();
                dataLabel.Text = "Input data (one per line, matching mask format):";
                dataLabel.AutoSize = true;
                dataLabel.Left = 10;
                dataLabel.Top = 60;
                form.Controls.Add(dataLabel);

                System.Windows.Forms.TextBox dataInput = new System.Windows.Forms.TextBox();
                dataInput.Left = 10;
                dataInput.Top = 80;
                dataInput.Width = form.ClientSize.Width - 20;
                dataInput.Multiline = true;
                dataInput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
                form.Controls.Add(dataInput);

                // Кнопка "OK"
                System.Windows.Forms.Button okButton = new System.Windows.Forms.Button();
                okButton.Text = "OK";
                okButton.Width = form.ClientSize.Width - 20;
                okButton.Height = 25;
                okButton.Left = 10;
                okButton.Top = form.ClientSize.Height - okButton.Height - 10;
                okButton.Click += (s, e) => { form.DialogResult = System.Windows.Forms.DialogResult.OK; form.Close(); };
                form.Controls.Add(okButton);
                
                // Устанавливаем высоту поля данных
                dataInput.Height = okButton.Top - dataInput.Top - 10;

                form.Load += (s, e) => { form.Location = new System.Drawing.Point(108, 108); };
                form.FormClosing += (s, e) => { if (form.DialogResult != System.Windows.Forms.DialogResult.OK) form.DialogResult = System.Windows.Forms.DialogResult.Cancel; };

                form.ShowDialog();

                if (form.DialogResult != System.Windows.Forms.DialogResult.OK)
                {
                    _project.SendInfoToLog($"Import to {tableName} cancelled by user", true);
                    return "0";
                }

                string mask = maskInput.Text.Trim();
                if (string.IsNullOrEmpty(dataInput.Text) || string.IsNullOrEmpty(mask))
                {
                    _project.SendWarningToLog("Data or mask cannot be empty");
                    return "0";
                }

                string[] lines = dataInput.Text.Trim().Split('\n');
                _project.SendInfoToLog($"Parsing [{lines.Length}] {tableName} data lines with mask: {mask}", true);

                for (int acc0 = 1; acc0 <= lines.Length; acc0++)
                {
                    string line = lines[acc0 - 1].Trim();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        _project.SendWarningToLog($"Line {acc0} is empty", false);
                        continue;
                    }

                    try
                    {
                        // Парсим строку по маске
                        var parsed_data = line.ParseByMask(mask);
                        
                        if (parsed_data.Count == 0)
                        {
                            _project.SendWarningToLog($"Line {acc0} doesn't match mask format", false);
                            continue;
                        }

                        var queryParts = new List<string>();
                        
                        // Формируем части запроса для каждого поля из маски
                        foreach (var field in parsed_data.Keys)
                        {
                            string value = parsed_data[field].Replace("'", "''");
                            
                            // Специальная обработка для CODE2FA
                            if (field.ToUpper() == "CODE2FA" && value.Contains('/'))
                                value = value.Split('/').Last();
                            
                            queryParts.Add($"{field} = '{value}'");
                        }
                        
                        if (queryParts.Count > 0)
                        {
                            string dbQuery = $@"UPDATE {table} SET {string.Join(", ", queryParts)} WHERE id = {acc0};";
                            project.DbQ(dbQuery, true);
                            lineCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _project.SendWarningToLog($"Error processing line {acc0}: {ex.Message}", false);
                    }
                }

                _project.SendInfoToLog($"[{lineCount}] records added to [{table}]", true);
                return lineCount.ToString();
            }
        
    }

    
}