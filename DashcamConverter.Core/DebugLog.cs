namespace DashcamConverter;

/// <summary>
/// Пошаговый отладочный лог. Включается флагом /debug при запуске.
/// </summary>
public static class DebugLog
{
    public static bool Enabled { get; set; }
    public static string? LogFilePath { get; set; }

    private static readonly object _lock = new();

    /// <summary>
    /// Записать сообщение отладки с категорией.
    /// Категории: INIT, PROBE, CODEC, STREAM, PACKET, ENCODE, CLEANUP, ERROR
    /// </summary>
    public static void Write(string category, string message)
    {
        if (!Enabled) return;
        lock (_lock)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var line = $"[DBG {timestamp} {category}] {message}";
            Console.Error.WriteLine(line);
            if (LogFilePath != null)
                File.AppendAllText(LogFilePath, line + Environment.NewLine);
        }
    }

    /// <summary>
    /// Записать отладочное сообщение с форматированием.
    /// </summary>
    public static void WriteFmt(string category, string format, params object?[] args)
    {
        if (!Enabled) return;
        Write(category, string.Format(format, args));
    }

    /// <summary>
    /// Записать границу-разделитель в лог для наглядности.
    /// </summary>
    public static void Separator(string label)
    {
        if (!Enabled) return;
        Write("----", $"===== {label} =====");
    }

    /// <summary>
    /// Записать шестнадцатеричный дамп небольшого блока данных.
    /// </summary>
    public static void HexDump(string category, string label, byte[] data, int maxLen = 64)
    {
        if (!Enabled) return;
        var len = Math.Min(data.Length, maxLen);
        var hex = Convert.ToHexString(data, 0, len);
        Write(category, $"{label}: {len} bytes: {hex}");
    }
}
