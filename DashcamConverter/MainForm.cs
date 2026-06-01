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

    public MainForm()
    {
        Text = "Dashcam Converter v1.0";
        Size = new Size(600, 440);
        MinimumSize = new Size(500, 380);
        Font = new Font("Arial", 10);

        // --- Группа: исходные файлы ---
        var filesGroup = new GroupBox
        {
            Text = "Исходные файлы",
            Dock = DockStyle.Top,
            Height = 200,
            Padding = new Padding(10),
        };

        _fileListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
        };
        filesGroup.Controls.Add(_fileListBox);

        var filesBtnPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 35,
            Padding = new Padding(0, 5, 0, 0),
        };

        _addButton = new Button
        {
            Text = "Добавить файлы",
            Width = 130,
            Dock = DockStyle.Left,
        };
        _addButton.Click += OnAddFiles;
        filesBtnPanel.Controls.Add(_addButton);

        _clearButton = new Button
        {
            Text = "Очистить",
            Width = 100,
            Dock = DockStyle.Left,
            Margin = new Padding(5, 0, 0, 0),
        };
        _clearButton.Click += (_, _) => ClearFiles();
        filesBtnPanel.Controls.Add(_clearButton);

        filesGroup.Controls.Add(filesBtnPanel);
        Controls.Add(filesGroup);

        // --- Группа: папка сохранения ---
        var saveGroup = new GroupBox
        {
            Text = "Папка сохранения",
            Dock = DockStyle.Top,
            Height = 60,
            Padding = new Padding(10),
        };

        _outputDirTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
        };
        saveGroup.Controls.Add(_outputDirTextBox);

        _browseButton = new Button
        {
            Text = "Обзор",
            Width = 80,
            Dock = DockStyle.Right,
        };
        _browseButton.Click += OnBrowseOutput;
        saveGroup.Controls.Add(_browseButton);

        Controls.Add(saveGroup);

        // --- Группа: прогресс ---
        var progressGroup = new GroupBox
        {
            Text = "Прогресс",
            Dock = DockStyle.Top,
            Height = 70,
            Padding = new Padding(10),
        };

        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Top,
            Height = 25,
            Maximum = 100,
            Style = ProgressBarStyle.Continuous,
        };
        progressGroup.Controls.Add(_progressBar);

        _statusLabel = new Label
        {
            Text = "Готово",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        progressGroup.Controls.Add(_statusLabel);

        Controls.Add(progressGroup);

        // --- Панель: кнопка «Конвертировать» ---
        var bottomPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
        };

        _convertButton = new Button
        {
            Text = "Конвертировать",
            Height = 40,
            Width = 180,
            Anchor = AnchorStyles.None,
        };
        _convertButton.Click += OnConvert;
        bottomPanel.Controls.Add(_convertButton);
        // Центрирование кнопки
        bottomPanel.Resize += (_, _) =>
        {
            _convertButton.Left = (bottomPanel.Width - _convertButton.Width) / 2;
            _convertButton.Top = (bottomPanel.Height - _convertButton.Height) / 2;
        };

        Controls.Add(bottomPanel);

        Load += OnFormLoad;
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

        // Папка сохранения по умолчанию — из первого файла
        if (string.IsNullOrEmpty(_outputDirTextBox.Text) && dialog.FileNames.Length > 0)
            _outputDirTextBox.Text = Path.GetDirectoryName(dialog.FileNames[0]) ?? "";
    }

    private void ClearFiles()
    {
        _fileListBox.Items.Clear();
        _progressBar.Value = 0;
        _statusLabel.Text = "Готово";
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
            return;
        }

        var outputDir = _outputDirTextBox.Text;
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
            return;
        }

        _convertButton.Enabled = false;
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
            _convertButton.Enabled = true;
            _cts?.Dispose();
            _cts = null;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cts?.Cancel();
        base.OnFormClosing(e);
    }
}
