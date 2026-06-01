namespace DashcamConverter;

/// <summary>
/// Главное окно приложения Dashcam Converter.
/// </summary>
public class MainForm : Form
{
    private readonly ListBox _fileListBox;
    private readonly TextBox _outputDirTextBox;
    private readonly ProgressBar _progressBar;
    private readonly Label _statusLabel;
    private readonly Button _convertButton;
    private readonly Button _addButton;
    private readonly Button _clearButton;
    private readonly Button _browseButton;

    private CancellationTokenSource? _cts;
    private bool _isConverting;

    public MainForm()
    {
        Text = "Dashcam Converter v1.0";
        Size = new Size(680, 470);
        MinimumSize = new Size(620, 420);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        Controls.Add(root);

        // --- 1. Папка сохранения ---
        var saveGroup = new GroupBox
        {
            Text = "1. Папка сохранения",
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
        };
        root.Controls.Add(saveGroup, 0, 0);

        var saveLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 4, 0, 0),
        };
        saveLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        saveLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
        saveGroup.Controls.Add(saveLayout);

        _outputDirTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };
        _outputDirTextBox.TextChanged += (_, _) => UpdateActionState();
        saveLayout.Controls.Add(_outputDirTextBox, 0, 0);

        _browseButton = new Button
        {
            Text = "Обзор…",
            Dock = DockStyle.Fill,
        };
        _browseButton.Click += OnBrowseOutput;
        saveLayout.Controls.Add(_browseButton, 1, 0);

        // --- 2. Исходные видеофайлы ---
        var filesGroup = new GroupBox
        {
            Text = "2. Исходные видеофайлы",
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
        };
        root.Controls.Add(filesGroup, 0, 1);

        var filesLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
        };
        filesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        filesLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        filesGroup.Controls.Add(filesLayout);

        _fileListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
        };
        filesLayout.Controls.Add(_fileListBox, 0, 0);

        var filesButtonPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Padding = new Padding(0, 5, 0, 0),
        };
        filesButtonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filesButtonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filesButtonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        filesButtonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filesLayout.Controls.Add(filesButtonPanel, 0, 1);

        _addButton = new Button
        {
            Text = "+ Добавить файлы",
            Width = 150,
            Height = 30,
        };
        _addButton.Click += OnAddFiles;
        filesButtonPanel.Controls.Add(_addButton, 0, 0);

        _clearButton = new Button
        {
            Text = "Очистить",
            Width = 100,
            Height = 30,
        };
        _clearButton.Click += (_, _) => ClearFiles();
        filesButtonPanel.Controls.Add(_clearButton, 1, 0);

        // spacer column (2) is percent 100 — nothing to add

        _convertButton = new Button
        {
            Text = "▶ Старт конвертации",
            Height = 30,
            Width = 190,
            Enabled = false,
            Font = new Font(Font, FontStyle.Bold),
        };
        _convertButton.Click += OnConvert;
        filesButtonPanel.Controls.Add(_convertButton, 3, 0);

        // --- Прогресс ---
        var progressGroup = new GroupBox
        {
            Text = "Прогресс",
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
        };
        root.Controls.Add(progressGroup, 0, 2);

        var progressLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
        };
        progressLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        progressLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        progressGroup.Controls.Add(progressLayout);

        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Fill,
            Maximum = 100,
            Style = ProgressBarStyle.Continuous,
        };
        progressLayout.Controls.Add(_progressBar, 0, 0);

        _statusLabel = new Label
        {
            Text = "Добавьте файлы для конвертации.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        progressLayout.Controls.Add(_statusLabel, 0, 1);

        Load += OnFormLoad;
        UpdateActionState();
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

        // Папка сохранения по умолчанию — из первого файла, если пользователь её не выбрал.
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
