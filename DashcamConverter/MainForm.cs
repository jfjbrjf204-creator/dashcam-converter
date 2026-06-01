namespace DashcamConverter;

public class MainForm : Form
{
    private ListBox _fileListBox = null!;
    private TextBox _outputDirTextBox = null!;
    private ProgressBar _progressBar = null!;
    private Label _statusLabel = null!;
    private Button _convertButton = null!;
    private Button _addButton = null!;
    private Button _clearButton = null!;
    private Button _browseButton = null!;

    private CancellationTokenSource? _cts;
    private bool _isConverting;

    public MainForm()
    {
        Text = "Dashcam Converter v1.0";
        Size = new Size(660, 480);
        MinimumSize = new Size(560, 420);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(14, 14, 14, 10),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        Controls.Add(root);

        BuildFileSection(root);
        BuildOutputSection(root);
        BuildProgressSection(root);
        BuildContactSection(root);

        Load += OnFormLoad;
        UpdateActionState();
    }

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
        root.Controls.Add(panel, 0, 0);

        var header = new Label
        {
            Text = "Исходные видеофайлы",
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

        _convertButton = new Button
        {
            Text = "▶ Старт конвертации",
            Height = 30,
            Width = 194,
            Enabled = false,
            Font = new Font(Font, FontStyle.Bold),
        };
        _convertButton.Click += OnConvert;
        btnRow.Controls.Add(_convertButton, 3, 0);
    }

    private void BuildOutputSection(TableLayoutPanel root)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 10, 0, 0),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(panel, 0, 1);

        var header = new Label
        {
            Text = "Папка для результатов",
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

    private void BuildProgressSection(TableLayoutPanel root)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 10, 0, 0),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(panel, 0, 2);

        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Fill,
            Maximum = 100,
            Style = ProgressBarStyle.Continuous,
        };
        panel.Controls.Add(_progressBar, 0, 0);

        _statusLabel = new Label
        {
            Text = "Добавьте файлы для конвертации.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.Gray,
            Margin = new Padding(0, 2, 0, 0),
        };
        panel.Controls.Add(_statusLabel, 0, 1);
    }

    private void BuildContactSection(TableLayoutPanel root)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 8, 0, 0),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        root.Controls.Add(panel, 0, 3);

        var tgLabel = new LinkLabel
        {
            Text = "Telegram: @HUU4AB0",
            AutoSize = true,
            LinkColor = Color.DodgerBlue,
            ActiveLinkColor = Color.RoyalBlue,
            Margin = new Padding(0, 0, 16, 0),
        };
        tgLabel.Links.Add(0, tgLabel.Text.Length, "https://t.me/HUU4AB0");
        tgLabel.LinkClicked += (_, e) =>
        {
            var url = e.Link?.LinkData as string;
            if (url != null)
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
        };
        ghLabel.Links.Add(0, ghLabel.Text.Length, "https://github.com/jfjbrjf204-creator/dashcam-converter");
        ghLabel.LinkClicked += (_, e) =>
        {
            var url = e.Link?.LinkData as string;
            if (url != null)
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
                catch { }
            }
        };
        panel.Controls.Add(ghLabel, 2, 0);
    }

    private void OnFormLoad(object? sender, EventArgs e)
    {
        try
        {
            Ffmpeg.Initialize();
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

        _statusLabel.Text = $"Выбрано файлов: {_fileListBox.Items.Count}. Нажмите «Старт конвертации».";
        UpdateActionState();
    }

    private void ClearFiles()
    {
        _fileListBox.Items.Clear();
        _progressBar.Value = 0;
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
        _statusLabel.Text = "Конвертация...";

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            int processed = 0;

            await Task.Run(() =>
            {
                foreach (var file in files)
                {
                    if (token.IsCancellationRequested)
                        break;

                    var fileName = Path.GetFileName(file);
                    BeginInvoke(() => _statusLabel.Text = $"Конвертация: {fileName}...");

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
                                        _progressBar.Value = Math.Min((int)p, 100);
                                });
                            });

                        processed++;
                        BeginInvoke(() => _statusLabel.Text = $"Готово: {fileName}");
                    }
                    catch (ConversionException ex)
                    {
                        BeginInvoke(() => MessageBox.Show(
                            $"Ошибка конвертации {fileName}:\n{ex.Message}",
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
                _statusLabel.Text = $"Готово. Обработано {processed} файлов.";
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
        _convertButton.Text = busy ? "Идёт конвертация…" : "▶ Старт конвертации";
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
