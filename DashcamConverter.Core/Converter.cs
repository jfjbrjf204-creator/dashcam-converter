namespace DashcamConverter;

public class ConversionException : Exception
{
    public string ErrorCode { get; }
    public string Stderr { get; }

    public ConversionException(string errorCode, string message, string stderr = "")
        : base($"[{errorCode}] {message}")
    {
        ErrorCode = errorCode;
        Stderr = stderr;
    }
}

public static class Converter
{
    public static bool ValidateInput(string inputPath)
    {
        if (!File.Exists(inputPath))
            return false;
        return Ffmpeg.ProbeDuration(inputPath) is not null;
    }

    public static string Remux(
        string inputPath,
        string? outputPath = null,
        Action<double>? onProgress = null)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException($"[CORE-VAL-001] Файл не найден: {inputPath}");

        outputPath ??= Path.ChangeExtension(inputPath, ".mp4");

        var (exitCode, error) = Ffmpeg.RemuxDirect(inputPath, outputPath, onProgress);

        if (exitCode != 0)
            throw new ConversionException(
                "CORE-CONV-001",
                $"Ошибка конвертации {Path.GetFileName(inputPath)}: {error}",
                error
            );

        return outputPath;
    }
}
