using System;
using System.Collections.Generic;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public class F0rms
    {
        private readonly IZennoPosterProjectModel _project;
        private static readonly System.Drawing.Font _font = new System.Drawing.Font("Cascadia Mono SemiBold", 10);

        public F0rms(IZennoPosterProjectModel project)
        {
            _project = project;
            
        }

        
        public static string InputBox(string message = "input data please", int width = 600, int height = 600)
        {
            //System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            using (System.Windows.Forms.Form form = new System.Windows.Forms.Form())
            using (System.Windows.Forms.TextBox smsBox = new System.Windows.Forms.TextBox())
            using (System.Windows.Forms.Button okButton = new System.Windows.Forms.Button())
            {
                form.Text = message;
                form.Width = width;
                form.Height = height;
                form.Font = _font;
                //System.Windows.Forms.TextBox smsBox = new System.Windows.Forms.TextBox();
                smsBox.Multiline = true;
                smsBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
                smsBox.Left = 5;
                smsBox.Top = 5;
                smsBox.Width = form.ClientSize.Width - 10;
                //System.Windows.Forms.Button okButton = new System.Windows.Forms.Button();
                okButton.Text = "OK";
                okButton.Width = form.ClientSize.Width - 10;
                okButton.Height = 25;
                okButton.Left = (form.ClientSize.Width - okButton.Width) / 2;
                okButton.Top = form.ClientSize.Height - okButton.Height - 5;
                okButton.Click += new System.EventHandler((sender, e) => { form.Close(); });
                smsBox.Height = okButton.Top - smsBox.Top - 5;
                form.Controls.Add(smsBox);
                form.Controls.Add(okButton);
                form.ShowDialog();
                return smsBox.Text;
            }
        }

        public Dictionary<string, string> GetLinesByKey(
            string keycolumn = "input Column Name",
            string title = "Input data line per line")
        {
            var result = new Dictionary<string, string>();
            // Создание формы
            System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            form.Text = title;
            form.Width = 420;
            form.Height = 700;
            form.TopMost = true;
            form.Location = new System.Drawing.Point(108, 108);

            System.Windows.Forms.Label columnLabel = new System.Windows.Forms.Label();
            columnLabel.Text = "input string for key here (it will be lowered)";
            columnLabel.AutoSize = true;
            columnLabel.Left = 10;
            columnLabel.Top = 10;
            form.Controls.Add(columnLabel);

            System.Windows.Forms.TextBox columnInput = new System.Windows.Forms.TextBox();
            columnInput.Left = 10;
            columnInput.Top = 30;
            columnInput.Width = 50;//form.ClientSize.Width - 20;
            columnInput.Text = keycolumn;//_project.Variables["addressType"].Value; // Предполагаем, что переменная существует
            form.Controls.Add(columnInput);

            System.Windows.Forms.Label addressLabel = new System.Windows.Forms.Label();
            addressLabel.Text = "Input strings (will be devided by \\n):";
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
                return null;
            }

            if (string.IsNullOrEmpty(columnInput.Text) || string.IsNullOrEmpty(addressInput.Text))
            {
                _project.SendWarningToLog("Column name or addresses cannot be empty");
                return null;
            }

            string columnName = columnInput.Text.ToLower().Trim();

            string[] lines = addressInput.Text.Trim().Split('\n');
            int lineCount = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    _project.SendWarningToLog($"Line {i} is empty");
                    continue;
                }

                try
                {

                    string key = (i + 1).ToString();
                    //string value = $"{columnName} = '{line}'";
                    string value = $"{columnName} = '{line.Replace("'", "''")}'";

                    _project.SendInfoToLog($"k [{i}], val = [{value}]", false);
                    result.Add(key, value);
                    lineCount++;
                }
                catch (Exception ex)
                {
                    _project.SendWarningToLog(ex.Message);
                }
            }

            return result;
        }

   
        public List<string> GetLines(
            string keycolumn = "input Column Name",
            string title = "Input data line per line")
        {
            var result = new List<string>();
            // Создание формы
            System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            form.Text = title;
            form.Width = 420;
            form.Height = 700;
            form.TopMost = true;
            form.Location = new System.Drawing.Point(108, 108);

            System.Windows.Forms.Label columnLabel = new System.Windows.Forms.Label();
            columnLabel.Text = "input string for key here (it will be lowered)";
            columnLabel.AutoSize = true;
            columnLabel.Left = 10;
            columnLabel.Top = 10;
            form.Controls.Add(columnLabel);

            System.Windows.Forms.TextBox columnInput = new System.Windows.Forms.TextBox();
            columnInput.Left = 10;
            columnInput.Top = 30;
            columnInput.Width = 50;//form.ClientSize.Width - 20;
            columnInput.Text = keycolumn;//_project.Variables["addressType"].Value; // Предполагаем, что переменная существует
            form.Controls.Add(columnInput);

            System.Windows.Forms.Label addressLabel = new System.Windows.Forms.Label();
            addressLabel.Text = "Input strings (will be devided by \\n):";
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
                return null;
            }

            if (string.IsNullOrEmpty(columnInput.Text) || string.IsNullOrEmpty(addressInput.Text))
            {
                _project.SendWarningToLog("Column name or addresses cannot be empty");
                return null;
            }

            string columnName = columnInput.Text.ToLower().Trim();

            string[] lines = addressInput.Text.Trim().Split('\n');
            int lineCount = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    _project.SendWarningToLog($"Line {i} is empty");
                    continue;
                }

                try
                {
                    string value = $"{columnName} = '{line.Replace("'", "''")}'";
                    result.Add(value);
                    lineCount++;
                }
                catch 
                {
                    
                }
            }
            return result;
        }
        
        
        public Dictionary<string, string> GetKeyValuePairs(
            int quantity,
            List<string> keyPlaceholders = null,
            List<string> valuePlaceholders = null,
            string title = "Input Key-Value Pairs",
            bool prepareUpd = true)
        {
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
                        result.Add(dictKey, dictValue);
                        _project.SendInfoToLog($"Added to dictionary: [{dictKey}] = [{dictValue}]", false);
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

        public Dictionary<string, bool> GetKeyBoolPairs(
            int quantity,
            List<string> keyPlaceholders = null,
            List<string> valuePlaceholders = null,
            string title = "Input Key-Bool Pairs",
            bool prepareUpd = true)
        {
            var result = new System.Collections.Generic.Dictionary<string, bool>();

            // Создание формы
            System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            form.Text = title;
            form.Width = 600;
            form.Height = 40 + quantity * 35; // Адаптивная высота в зависимости от количества полей
            form.TopMost = true;
            form.Location = new System.Drawing.Point(108, 108);

            // Список для хранения текстовых полей и чекбоксов
            var keyLabels = new System.Windows.Forms.Label[quantity];
            var keyTextBoxes = new System.Windows.Forms.TextBox[quantity];
            var valueCheckBoxes = new System.Windows.Forms.CheckBox[quantity];

            int currentTop = 5;
            int labelWidth = 40;
            int keyBoxWidth = 40;
            int checkBoxWidth = 370; // Ширина для чекбокса (для выравнивания)
            int spacing = 5;

            // Создаём поля для ключей и чекбоксов
            for (int i = 0; i < quantity; i++)
            {
                // Метка для ключа
                System.Windows.Forms.Label keyLabel = new System.Windows.Forms.Label();

                string keyDefault = keyPlaceholders != null && i < keyPlaceholders.Count && !string.IsNullOrEmpty(keyPlaceholders[i]) ? keyPlaceholders[i] : $"key{i + 1}";
                keyLabel.Text = keyDefault; //
                //keyTextBoxes[i] = keyDefault;
                //keyLabel.Text = $"Key:";
                keyLabel.AutoSize = true;
                keyLabel.Left = 5;
                keyLabel.Top = currentTop + 5; // Смещение для центрирования
                form.Controls.Add(keyLabel);
                keyLabels[i] = keyLabel;

                // Поле для ключа
                System.Windows.Forms.TextBox keyTextBox = new System.Windows.Forms.TextBox();
                keyTextBox.Left = keyLabel.Left + labelWidth + spacing;
                keyTextBox.Top = currentTop;
                keyTextBox.Width = keyBoxWidth;


                // Чекбокс для значения
                System.Windows.Forms.CheckBox valueCheckBox = new System.Windows.Forms.CheckBox();
                valueCheckBox.Left = keyTextBox.Left + keyBoxWidth + spacing + 10;
                valueCheckBox.Top = currentTop;
                valueCheckBox.Width = checkBoxWidth;
                string valueDefault = valuePlaceholders != null && i < valuePlaceholders.Count && !string.IsNullOrEmpty(valuePlaceholders[i]) ? valuePlaceholders[i] : $"Option{i + 1}";
                valueCheckBox.Text = valueDefault; // Текст чекбокса для удобства
                valueCheckBox.Checked = false; // По умолчанию выключен


                // Метка для чекбокса
                System.Windows.Forms.Label valueLabel = new System.Windows.Forms.Label();
                valueLabel.Text = $"";
                valueLabel.AutoSize = true;
                valueLabel.Left = valueLabel.Left + labelWidth + spacing;

                valueLabel.Top = currentTop + 5; // Смещение для центрирования
                form.Controls.Add(valueLabel);



                form.Controls.Add(valueCheckBox);
                valueCheckBoxes[i] = valueCheckBox;

                currentTop += valueCheckBox.Height + spacing;
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

            // Формируем словарь
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

        public string GetKeyValueString(
            int quantity,
            List<string> keyPlaceholders = null,
            List<string> valuePlaceholders = null,
            string title = "Input Key-Value Pairs")
        {
            // Создание формы
            System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            form.Text = title;
            form.Width = 600;
            form.Height = 40 + quantity * 35; // Адаптивная высота
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
                keyLabel.Top = currentTop + 5;
                form.Controls.Add(keyLabel);

                // Поле для ключа
                System.Windows.Forms.TextBox keyTextBox = new System.Windows.Forms.TextBox();
                keyTextBox.Left = keyLabel.Left + labelWidth + spacing;
                keyTextBox.Top = currentTop;
                keyTextBox.Width = keyBoxWidth;

                string keyDefault = keyPlaceholders != null && i < keyPlaceholders.Count && !string.IsNullOrEmpty(keyPlaceholders[i])
                    ? keyPlaceholders[i]
                    : $"key{i + 1}";
                keyTextBox.Text = keyDefault;
                form.Controls.Add(keyTextBox);
                keyTextBoxes[i] = keyTextBox;

                // Метка для значения
                System.Windows.Forms.Label valueLabel = new System.Windows.Forms.Label();
                valueLabel.Text = $"Value:";
                valueLabel.AutoSize = true;
                valueLabel.Left = keyTextBox.Left + keyBoxWidth + spacing + 10;
                valueLabel.Top = currentTop + 5;
                form.Controls.Add(valueLabel);

                // Поле для значения
                System.Windows.Forms.TextBox valueTextBox = new System.Windows.Forms.TextBox();
                valueTextBox.Left = valueLabel.Left + labelWidth + spacing;
                valueTextBox.Top = currentTop;
                valueTextBox.Width = valueBoxWidth;

                // Установка плейсхолдера
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

            // Формируем строку
            var pairs = new List<string>();
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
                    string pair = $"{key}='{value.Replace("'", "''")}'";
                    pairs.Add(pair);
                    _project.SendInfoToLog($"Added pair: {pair}", false);
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

            // Объединяем пары в строку
            return string.Join(", ", pairs);
        }
        
        public string GetSelectedItem(
        List<string> items,
        string title = "Select an Item",
        string labelText = "Select:")
        {
            // Проверка входных данных
            if (items == null || items.Count == 0)
            {
                _project.SendWarningToLog("No items provided for selection");
                return null;
            }

            // Создание формы
            System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            form.Text = title;
            form.Width = 600;
            form.Height = 120; // Фиксированная высота для одного выпадающего списка
            form.TopMost = true;
            form.Location = new System.Drawing.Point(108, 108);

            int currentTop = 5;
            int labelWidth = 40;
            int comboBoxWidth = 500;
            int spacing = 5;

            // Метка
            System.Windows.Forms.Label label = new System.Windows.Forms.Label();
            label.Text = labelText;
            label.AutoSize = true;
            label.Left = 5;
            label.Top = currentTop + 5; // Смещение для центрирования
            form.Controls.Add(label);

            // Выпадающий список
            System.Windows.Forms.ComboBox comboBox = new System.Windows.Forms.ComboBox();
            comboBox.Left = label.Left + labelWidth + spacing;
            comboBox.Top = currentTop;
            comboBox.Width = comboBoxWidth;
            comboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; // Только выбор из списка
            comboBox.Items.AddRange(items.ToArray());
            comboBox.SelectedIndex = 0; // Выбираем первый элемент по умолчанию
            form.Controls.Add(comboBox);

            currentTop += comboBox.Height + spacing;

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
                _project.SendInfoToLog("Selection cancelled by user", true);
                return null;
            }

            // Получаем выбранный элемент
            string selectedItem = comboBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedItem))
            {
                _project.SendWarningToLog("No item selected");
                return null;
            }

            _project.SendInfoToLog($"Selected item: {selectedItem}", false);
            return selectedItem;
        }
        

    }
    
}
