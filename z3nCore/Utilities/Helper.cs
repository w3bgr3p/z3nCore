using System.IO;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System;


namespace z3nCore.Utilities
{
    public static class Helper
    {
        private static readonly System.Drawing.Font defaultFont = new System.Drawing.Font("Cascadia Mono", 9F);
        private static string ShowInputDialog(string prompt = "Enter text:", string title = "Input", string defaultValue = "")
{
    string result = null;
    
    // Создаем форму
    var form = new System.Windows.Forms.Form();
    form.Text = title;
    form.Size = new System.Drawing.Size(350, 150);
    form.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
    form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
    form.MaximizeBox = false;
    form.MinimizeBox = false;
    form.TopMost = true;
    
    // Создаем элементы управления
    var label = new System.Windows.Forms.Label();
    label.Text = prompt;
    label.Font = defaultFont;
    label.Location = new System.Drawing.Point(12, 15);
    label.Size = new System.Drawing.Size(310, 20);
    
    var textBox = new System.Windows.Forms.TextBox();
    textBox.Text = defaultValue;
    textBox.Location = new System.Drawing.Point(12, 40);
    textBox.Size = new System.Drawing.Size(310, 23);
    textBox.Font = defaultFont;
    
    
    var buttonOK = new System.Windows.Forms.Button();
    buttonOK.Text = "OK";
    buttonOK.Location = new System.Drawing.Point(167, 75);
    buttonOK.Size = new System.Drawing.Size(75, 25);
    buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
    
    var buttonCancel = new System.Windows.Forms.Button();
    buttonCancel.Text = "Cancel";
    buttonCancel.Location = new System.Drawing.Point(247, 75);
    buttonCancel.Size = new System.Drawing.Size(75, 25);
    buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
    
    // Настраиваем кнопки формы
    form.AcceptButton = buttonOK;
    form.CancelButton = buttonCancel;
    
    // Обработчики событий
    buttonOK.Click += (s, e) => 
    {
        result = textBox.Text;
        form.DialogResult = System.Windows.Forms.DialogResult.OK;
        form.Close();
    };
    
    buttonCancel.Click += (s, e) => 
    {
        result = null;
        form.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        form.Close();
    };
    
    // Обработка Enter и Escape
    textBox.KeyDown += (s, e) =>
    {
        if (e.KeyCode == System.Windows.Forms.Keys.Enter)
        {
            result = textBox.Text;
            form.DialogResult = System.Windows.Forms.DialogResult.OK;
            form.Close();
        }
        else if (e.KeyCode == System.Windows.Forms.Keys.Escape)
        {
            result = null;
            form.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            form.Close();
        }
    };
    
    // Добавляем элементы на форму
    form.Controls.Add(label);
    form.Controls.Add(textBox);
    form.Controls.Add(buttonOK);
    form.Controls.Add(buttonCancel);
    
    // Устанавливаем фокус на текстовое поле и выделяем весь текст
    form.Shown += (s, e) => 
    {
        textBox.Focus();
        textBox.SelectAll();
    };
    
    // Показываем диалог
    var dialogResult = form.ShowDialog();
    
    // Возвращаем результат
    return dialogResult == System.Windows.Forms.DialogResult.OK ? result : null;
}
        private static void ShowForm(string textToShow)
        {
            // Создаем новую форму
            var form = new System.Windows.Forms.Form();
            form.TopMost = true;
            // Создаем TextBox для отображения текста (доступен для выделения и копирования)
            var textBox = new System.Windows.Forms.TextBox();
            
            // Настраиваем TextBox
            textBox.Text = textToShow;
            textBox.Multiline = true;
            textBox.ReadOnly = true;
            textBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            textBox.WordWrap = true;
            textBox.Font = defaultFont;//new System.Drawing.Font("Cascadia Mono", 9F);
            textBox.BackColor = System.Drawing.SystemColors.Window;
            //form.Font = new System.Drawing.Font("Cascadia Mono SemiBold", 10);
            // Вычисляем оптимальный размер на основе текста
            using (var g = form.CreateGraphics())
            {
                var textSize = g.MeasureString(textToShow, textBox.Font);
                
                // Определяем количество строк для более точного расчета высоты
                int lineCount = textToShow.Split('\n').Length;
                int lineHeight = (int)textBox.Font.GetHeight(g);
                
                // Рассчитываем размер с учетом отступов и скроллбаров
                int calculatedWidth = Math.Min((int)textSize.Width + 60, 800); // максимум 800px
                int calculatedHeight = Math.Min(lineCount * lineHeight + 80, 600); // максимум 600px
                
                // Устанавливаем минимальные размеры
                int formWidth = Math.Max(calculatedWidth, 300);
                int formHeight = Math.Max(calculatedHeight, 200);
                
                form.Size = new System.Drawing.Size(formWidth, formHeight);
            }
            
            // Настраиваем форму
            form.Text = "Text Viewer";
            form.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            form.MinimumSize = new System.Drawing.Size(300, 200);
            form.MaximizeBox = true;
            form.MinimizeBox = true;
            
            // Размещаем TextBox на всю область формы с отступами
            textBox.Dock = System.Windows.Forms.DockStyle.Fill;
            textBox.Margin = new System.Windows.Forms.Padding(10);
            
            // Создаем панель для отступов
            var panel = new System.Windows.Forms.Panel();
            panel.Dock = System.Windows.Forms.DockStyle.Fill;
            panel.Padding = new System.Windows.Forms.Padding(10);
            panel.Controls.Add(textBox);
            
            // Добавляем панель на форму
            form.Controls.Add(panel);
            
            // Добавляем контекстное меню для удобства копирования
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            var selectAllItem = new System.Windows.Forms.ToolStripMenuItem("Select All");
            var copyItem = new System.Windows.Forms.ToolStripMenuItem("Copy");
            
            selectAllItem.Click += (s, e) => textBox.SelectAll();
            copyItem.Click += (s, e) => 
            {
                if (textBox.SelectedText.Length > 0)
                    System.Windows.Forms.Clipboard.SetText(textBox.SelectedText);
                else
                    System.Windows.Forms.Clipboard.SetText(textBox.Text);
            };
            
            contextMenu.Items.Add(selectAllItem);
            contextMenu.Items.Add(copyItem);
            textBox.ContextMenuStrip = contextMenu;
            
            // Добавляем горячие клавиши
            form.KeyPreview = true;
            form.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == System.Windows.Forms.Keys.A)
                {
                    textBox.SelectAll();
                    e.Handled = true;
                }
                else if (e.Control && e.KeyCode == System.Windows.Forms.Keys.C)
                {
                    if (textBox.SelectedText.Length > 0)
                        System.Windows.Forms.Clipboard.SetText(textBox.SelectedText);
                    else
                        System.Windows.Forms.Clipboard.SetText(textBox.Text);
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.Escape)
                {
                    form.Close();
                }
            };
            
            // Показываем форму
            form.ShowDialog();
        }
        public static void Help(this IZennoPosterProjectModel project, string toSearch = null)
        {

            if (string.IsNullOrEmpty(toSearch))
            {
                toSearch = ShowInputDialog("methodToFind:");
            }

            if (string.IsNullOrEmpty(toSearch)) throw new ArgumentException("input can't be empty");

            string filesToCheck = "ZennoLab.CommandCenter.xml, ZennoLab.InterfacesLibrary.xml, ZennoLab.Macros.xml, ZennoLab.Emulation.xml";
            var currentProcessPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            var xmlPath = Path.GetDirectoryName(currentProcessPath);
            
            string result = "";

            foreach (string file in filesToCheck.Split(','))
            {
                string Filename = file.Trim();
                string path = Path.Combine(xmlPath, Filename);

                var obj = new object();
                string doc = String.Empty;
                lock (obj)
                {
                    doc = File.ReadAllText(path);
                }

                //var doc = File.ReadAllText(path);
                project.Xml.FromString(doc);
                int index = 0;
                
                go:
                try
                {
                    var memberName = project.Xml.doc.members.member[index]["name"].ToString();
                    if (memberName.Contains(toSearch)) 
                    {
                        project.SendInfoToLog(memberName);
                        result += $"=== {memberName} ===\n";
                        
                        // Parse all child elements of the member
                        var memberNode = project.Xml.doc.members.member[index];
                        
                        // Get summary
                        try 
                        {
                            var summary = memberNode.summary.InnerText?.Trim();
                            if (!string.IsNullOrEmpty(summary))
                            {
                                result += $"Summary: {summary}\n";
                            }
                        }
                        catch { /* Summary not found */ }
                        
                        // Get parameters
                        try 
                        {
                            int paramIndex = 0;
                            paramLoop:
                            try 
                            {
                                var param = memberNode.param[paramIndex];
                                var paramName = param["name"]?.ToString() ?? "";
                                var paramDescription = param.InnerText?.Trim() ?? "";
                                result += $"Parameter [{paramName}]: {paramDescription}\n";
                                paramIndex++;
                                goto paramLoop;
                            }
                            catch { /* No more parameters */ }
                        }
                        catch { /* No parameters section */ }
                        
                        // Get returns
                        try 
                        {
                            var returns = memberNode.returns.InnerText?.Trim();
                            if (!string.IsNullOrEmpty(returns))
                            {
                                result += $"Returns: {returns}\n";
                            }
                        }
                        catch { /* Returns not found */ }
                        
                        // Get remarks
                        try 
                        {
                            var remarks = memberNode.remarks.InnerText?.Trim();
                            if (!string.IsNullOrEmpty(remarks))
                            {
                                result += $"Remarks: {remarks}\n";
                            }
                        }
                        catch { /* Remarks not found */ }
                        
                        // Get examples
                        try 
                        {
                            int exampleIndex = 0;
                            exampleLoop:
                            try 
                            {
                                var example = memberNode.example[exampleIndex];
                                var exampleText = example.InnerText?.Trim() ?? "";
                                if (!string.IsNullOrEmpty(exampleText))
                                {
                                    result += $"Example {exampleIndex + 1}: {exampleText}\n";
                                }
                                exampleIndex++;
                                goto exampleLoop;
                            }
                            catch { /* No more examples */ }
                        }
                        catch { /* No examples section */ }
                        
                        // Get requirements
                        try 
                        {
                            var requirements = memberNode.requirements.InnerText?.Trim();
                            if (!string.IsNullOrEmpty(requirements))
                            {
                                result += $"Requirements: {requirements}\n";
                            }
                        }
                        catch { /* Requirements not found */ }
                        
                        // Get seealso references
                        try 
                        {
                            int seeAlsoIndex = 0;
                            seeAlsoLoop:
                            try 
                            {
                                var seeAlso = memberNode.seealso[seeAlsoIndex];
                                var seeAlsoRef = seeAlso["cref"]?.ToString() ?? seeAlso.InnerText?.Trim() ?? "";
                                if (!string.IsNullOrEmpty(seeAlsoRef))
                                {
                                    result += $"See Also: {seeAlsoRef}\n";
                                }
                                seeAlsoIndex++;
                                goto seeAlsoLoop;
                            }
                            catch { /* No more see also references */ }
                        }
                        catch { /* No seealso section */ }
                        
                        // Get overloads
                        try 
                        {
                            var overloads = memberNode.overloads.InnerText?.Trim();
                            if (!string.IsNullOrEmpty(overloads))
                            {
                                result += $"Overloads: {overloads}\n";
                            }
                        }
                        catch { /* Overloads not found */ }
                        
                        // Get exceptions
                        try 
                        {
                            int exceptionIndex = 0;
                            exceptionLoop:
                            try 
                            {
                                var exception = memberNode.exception[exceptionIndex];
                                var exceptionType = exception["cref"]?.ToString() ?? "";
                                var exceptionDescription = exception.InnerText?.Trim() ?? "";
                                if (!string.IsNullOrEmpty(exceptionType) || !string.IsNullOrEmpty(exceptionDescription))
                                {
                                    result += $"Exception [{exceptionType}]: {exceptionDescription}\n";
                                }
                                exceptionIndex++;
                                goto exceptionLoop;
                            }
                            catch { /* No more exceptions */ }
                        }
                        catch { /* No exceptions section */ }
                        
                        result += "\n" + new string('-', 50) + "\n\n";
                    }
                    
                    index++;
                    goto go;
                }
                catch (Exception ex)
                {
                    project.SendInfoToLog($"Finished processing file {Filename}: {ex.Message}");
                }
            }

            if (string.IsNullOrEmpty(result)) result = $"nothing found by [{toSearch}]";
            ShowForm(result.Replace("\n","\r\n"));
        }

    }
}