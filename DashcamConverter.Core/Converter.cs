namespace DashcamConverter;

public class ConversionException : Exception
{
    public string Stderr { get; }

    public ConversionException(string message, string stderr = "") : base(message)
    {
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
            throw new FileNotFoundException($"Файл не найден: {inputPath}");

        outputPath ??= Path.ChangeExtension(inputPath, ".mp4");

        var (exitCode, error) = Ffmpeg.RemuxDirect(inputPath, outputPath, onProgress);

        if (exitCode != 0)
            throw new ConversionException(
                $"Ошибка конвертации {Path.GetFileName(inputPath)}",
                error
            );

        return outputPath;
    }
}
