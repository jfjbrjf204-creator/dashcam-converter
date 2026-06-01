namespace DashcamConverter;

public class MainForm : Form
{
    private ListBox _fileListBox = null!;
    private TextBox _outputDirTextBox = null!;
    private ProgressBar _progressBar = null!;
    private Label _progressPercentLabel = null!;
    private Label _statusLabel = null!;
    private Button _convertButton = null!;
    private Button _addButton = null!;
    private Button _clearButton = null!;
    private Button _browseButton = null!;

    private CancellationTokenSource? _cts;
    private bool _isConverting;

    public MainForm()
    {
        Text = "Dashcam Converter";
        Size = new Size(680, 560);
        MinimumSize = new Size(600, 500);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(14, 10, 14, 8),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        Controls.Add(root);

        BuildHeader(root);
        BuildFileSection(root);
        BuildOutputSection(root);
        BuildProgressSection(root);
        BuildConvertSection(root);
        BuildFooter(root);

        Load += OnFormLoad;
        UpdateActionState();
    }

    // ================================================================
    // HEADER — app title + version
    // ================================================================

    private void BuildHeader(TableLayoutPanel root)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 4),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        root.Controls.Add(panel, 0, 0);

        var title = new Label
        {
            Text = "Dashcam Converter",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(33, 33, 33),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        panel.Controls.Add(title, 0, 0);

        var hash = GetCommitHash();
        var versionText = hash == "unknown"
            ? $"v{Program.Version}"
            : $"v{Program.Version}+{hash}";
        var versionLabel = new Label
        {
            Text = versionText,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9),
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 2, 0, 0),
        };
        panel.Controls.Add(versionLabel, 1, 0);
    }

    // ================================================================
    // SECTION 1 — file selection
    // ================================================================

    private void BuildFileSection(TableLayoutPanel root)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.Controls.Add(panel, 0, 1);

        var header = new Label
        {
            Text = "1. Исходные видеофайлы",
            Dock = DockStyle.Fill,
            Font = new Font(Font, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        panel.Controls.Add(header, 0, 0);

        _fileListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            Margin = new Padding(0, 4, 0, 0),
        };
        panel.Controls.Add(_fileListBox, 0, 1);

        var btnRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 6, 0, 0),
        };
        btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.Controls.Add(btnRow, 0, 2);

        _addButton = new Button
        {
            Text = "+ Добавить файлы",
            Width = 148,
            Height = 30,
        };
        _addButton.Click += OnAddFiles;
        btnRow.Controls.Add(_addButton, 0, 0);

        _clearButton = new Button
        {
            Text = "Очистить список",
            Width = 130,
            Height = 30,
            Margin = new Padding(6, 0, 0, 0),
        };
        _clearButton.Click += (_, _) => ClearFiles();
        btnRow.Controls.Add(_clearButton, 1, 0);
    }

    // ================================================================
    // SECTION 2 — output folder
    // ================================================================

    private void BuildOutputSection(TableLayoutPanel root)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 8, 0, 0),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(panel, 0, 2);

        var header = new Label
        {
            Text = "2. Папка для результатов",
            Dock = DockStyle.Fill,
            Font = new Font(Font, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        panel.Controls.Add(header, 0, 0);

        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 4, 0, 0),
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        panel.Controls.Add(row, 0, 1);

        _outputDirTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };
        _outputDirTextBox.TextChanged += (_, _) => UpdateActionState();
        row.Controls.Add(_outputDirTextBox, 0, 0);

        _browseButton = new Button
        {
            Text = "Обзор…",
            Dock = DockStyle.Fill,
            Margin = new Padding(6, 0, 0, 0),
        };
        _browseButton.Click += OnBrowseOutput;
        row.Controls.Add(_browseButton, 1, 0);
    }

    // ================================================================
    // SECTION 3 — progress bar + status
    // ================================================================

    private void BuildProgressSection(TableLayoutPanel root)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0, 8, 0, 0),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        root.Controls.Add(panel, 0, 3);

        var header = new Label
        {
            Text = "3. Ход конвертации",
            Dock = DockStyle.Fill,
            Font = new Font(Font, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        panel.Controls.Add(header, 0, 0);

        var progressRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 2, 0, 0),
        };
        progressRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        progressRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
        panel.Controls.Add(progressRow, 0, 1);

        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Fill,
            Maximum = 100,
            Style = ProgressBarStyle.Continuous,
        };
        progressRow.Controls.Add(_progressBar, 0, 0);

        _progressPercentLabel = new Label
        {
            Text = "0%",
            TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 10, FontStyle.Bold),
            ForeColor = Color.DimGray,
        };
        progressRow.Controls.Add(_progressPercentLabel, 1, 0);

        _statusLabel = new Label
        {
            Text = "Добавьте файлы для конвертации.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.Gray,
            Margin = new Padding(0, 2, 0, 0),
        };
        panel.Controls.Add(_statusLabel, 0, 2);
    }

    // ================================================================
    // CONVERT CTA — large prominent button
    // ================================================================

    private void BuildConvertSection(TableLayoutPanel root)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 8, 0, 0),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.Controls.Add(panel, 0, 4);

        _convertButton = new Button
        {
            Text = "▶ НАЧАТЬ КОНВЕРТАЦИЮ",
            Width = 320,
            Height = 40,
            Enabled = false,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            BackColor = SystemColors.Control,
            ForeColor = SystemColors.GrayText,
            Cursor = Cursors.Hand,
        };
        _convertButton.FlatAppearance.BorderSize = 0;
        _convertButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(24, 144, 236);
        _convertButton.Click += OnConvert;
        _convertButton.EnabledChanged += (_, _) =>
        {
            if (_convertButton.Enabled)
            {
                _convertButton.BackColor = Color.FromArgb(0, 120, 212);
                _convertButton.ForeColor = Color.White;
            }
            else
            {
                _convertButton.BackColor = SystemColors.Control;
                _convertButton.ForeColor = SystemColors.GrayText;
            }
        };
        panel.Controls.Add(_convertButton, 1, 0);
    }

    // ================================================================
    // FOOTER — contact links
    // ================================================================

    private void BuildFooter(TableLayoutPanel root)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 4, 0, 0),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        root.Controls.Add(panel, 0, 5);

        var contactLabel = new Label
        {
            Text = "Связь:",
            AutoSize = true,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 9),
            Margin = new Padding(0, 0, 6, 0),
        };
        panel.Controls.Add(contactLabel, 0, 0);

        var tgLabel = new LinkLabel
        {
            Text = "Telegram: @HUU4AB0",
            AutoSize = true,
            LinkColor = Color.DodgerBlue,
            ActiveLinkColor = Color.RoyalBlue,
            Font = new Font("Segoe UI", 9),
            Margin = new Padding(0, 0, 16, 0),
        };
        tgLabel.Links.Add(0, tgLabel.Text.Length, "https://t.me/HUU4AB0");
        tgLabel.LinkClicked += (_, e) =>
        {
            if (e.Link?.LinkData is string url)
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
                catch { }
            }
        };
        panel.Controls.Add(tgLabel, 1, 0);

        var ghLabel = new LinkLabel
        {
            Text = "GitHub",
            AutoSize = true,
            LinkColor = Color.DodgerBlue,
            ActiveLinkColor = Color.RoyalBlue,
            Font = new Font("Segoe UI", 9),
        };
        ghLabel.Links.Add(0, ghLabel.Text.Length, "https://github.com/jfjbrjf204-creator/dashcam-converter");
        ghLabel.LinkClicked += (_, e) =>
        {
            if (e.Link?.LinkData is string url)
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
                catch { }
            }
        };
        panel.Controls.Add(ghLabel, 2, 0);
    }

    // ================================================================
    // VERSION helpers
    // ================================================================

    private static string GetCommitHash()
    {
        try
        {
            foreach (System.Reflection.AssemblyMetadataAttribute attr in
                typeof(MainForm).Assembly.GetCustomAttributes(
                    typeof(System.Reflection.AssemblyMetadataAttribute), false))
            {
                if (attr.Key == "CommitHash")
                    return attr.Value ?? "unknown";
            }
        }
        catch { }
        return "unknown";
    }

    // ================================================================
    // EXISTING BEHAVIOR — preserved
    // ================================================================

    private void OnFormLoad(object? sender, EventArgs e)
    {
        try
        {
            Ffmpeg.Initialize();

            if (DebugLog.Enabled)
            {
                var logPath = Path.Combine(AppContext.BaseDirectory, $"dashcam_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                DebugLog.LogFilePath = logPath;
                DebugLog.Write("INIT", $"GUI started, log file: {logPath}");
                _statusLabel.Text = $"Режим отладки: {logPath}";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"FFmpeg не найден.\n\n{ex.Message}\n\n" +
                "Убедитесь, что FFmpeg DLL встроены в приложение.",
                "FFmpeg не найден",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            Close();
        }
    }

    private void OnAddFiles(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Выберите видеофайлы",
            Filter = "Видеофайлы|*.ts;*.mov;*.avi;*.mp4;*.mkv;*.wmv;*.flv;*.webm;*.mts;*.m2ts;*.vob;*.3gp|Все файлы|*.*",
            Multiselect = true,
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        foreach (var path in dialog.FileNames)
        {
            if (!_fileListBox.Items.Contains(path))
                _fileListBox.Items.Add(path);
        }

        if (string.IsNullOrWhiteSpace(_outputDirTextBox.Text) && dialog.FileNames.Length > 0)
            _outputDirTextBox.Text = Path.GetDirectoryName(dialog.FileNames[0]) ?? "";

        _statusLabel.Text = $"Готово к запуску. Файлов: {_fileListBox.Items.Count}.";
        UpdateActionState();
    }

    private void ClearFiles()
    {
        _fileListBox.Items.Clear();
        _progressBar.Value = 0;
        _progressPercentLabel.Text = "0%";
        _statusLabel.Text = "Добавьте файлы для конвертации.";
        UpdateActionState();
    }

    private void OnBrowseOutput(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Выберите папку сохранения",
            UseDescriptionForTitle = true,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            _outputDirTextBox.Text = dialog.SelectedPath;
    }

    private async void OnConvert(object? sender, EventArgs e)
    {
        var files = _fileListBox.Items.Cast<string>().ToArray();
        if (files.Length == 0)
        {
            MessageBox.Show(
                "Добавьте файлы для конвертации.",
                "Нет файлов",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            UpdateActionState();
            return;
        }

        var outputDir = _outputDirTextBox.Text.Trim();
        if (string.IsNullOrEmpty(outputDir) && files.Length > 0)
        {
            outputDir = Path.GetDirectoryName(files[0]) ?? "";
            _outputDirTextBox.Text = outputDir;
        }

        if (!Directory.Exists(outputDir))
        {
            MessageBox.Show(
                $"Папка сохранения не существует:\n{outputDir}",
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            UpdateActionState();
            return;
        }

        SetBusy(true);
        _progressBar.Value = 0;
        _progressPercentLabel.Text = "0%";
        _statusLabel.Text = "Конвертация...";

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            int processed = 0;

            await Task.Run(() =>
            {
                for (var index = 0; index < files.Length; index++)
                {
                    if (token.IsCancellationRequested)
                        break;

                    var file = files[index];
                    var fileName = Path.GetFileName(file);
                    BeginInvoke(() => _statusLabel.Text = $"Файл {index + 1} из {files.Length} — {fileName}");

                    var outputPath = Path.Combine(outputDir,
                        Path.ChangeExtension(fileName, ".mp4"));

                    try
                    {
                        Converter.Remux(file, outputPath,
                            onProgress: p =>
                            {
                                if (token.IsCancellationRequested)
                                    return;
                                BeginInvoke(() =>
                                {
                                    if (!token.IsCancellationRequested)
                                    {
                                        var pct = double.IsFinite(p)
                                            ? Math.Clamp((int)Math.Round(p), _progressBar.Minimum, _progressBar.Maximum)
                                            : _progressBar.Minimum;
                                        _progressBar.Value = pct;
                                        _progressPercentLabel.Text = $"{pct}%";
                                    }
                                });
                            });

                        processed++;
                        BeginInvoke(() => _statusLabel.Text = $"Готово: {fileName}");
                    }
                    catch (ConversionException ex)
                    {
                        BeginInvoke(() => MessageBox.Show(
                            ex.Message,
                            "Ошибка",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        ));
                    }
                    catch (FileNotFoundException ex)
                    {
                        BeginInvoke(() => MessageBox.Show(
                            ex.Message,
                            "Файл не найден",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        ));
                    }
                }
            }, token);

            if (!token.IsCancellationRequested)
            {
                _progressBar.Value = 100;
                _progressPercentLabel.Text = "100%";
                _statusLabel.Text = $"Готово. Обработано {processed} из {files.Length} файлов.";
            }
            else
            {
                _statusLabel.Text = "Отменено.";
            }
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = "Отменено.";
        }
        finally
        {
            SetBusy(false);
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void SetBusy(bool busy)
    {
        _isConverting = busy;
        _addButton.Enabled = !busy;
        _clearButton.Enabled = !busy;
        _browseButton.Enabled = !busy;
        _outputDirTextBox.Enabled = !busy;
        _fileListBox.Enabled = !busy;
        _convertButton.Text = busy ? "⏳ КОНВЕРТАЦИЯ…" : "▶ НАЧАТЬ КОНВЕРТАЦИЮ";
        UpdateActionState();
    }

    private void UpdateActionState()
    {
        if (_isConverting)
        {
            _convertButton.Enabled = false;
            return;
        }

        var hasFiles = _fileListBox.Items.Count > 0;
        var outputDir = _outputDirTextBox.Text.Trim();
        var hasOutputDir = outputDir.Length > 0 && Directory.Exists(outputDir);
        _convertButton.Enabled = hasFiles && hasOutputDir;

        if (!hasFiles)
        {
            _statusLabel.Text = "Добавьте файлы для конвертации.";
        }
        else if (!hasOutputDir)
        {
            _statusLabel.Text = "Выберите существующую папку сохранения.";
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cts?.Cancel();
        base.OnFormClosing(e);
    }
}
