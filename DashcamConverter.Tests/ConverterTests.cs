using System.Runtime.InteropServices;
using Xunit;
using DashcamConverter;

/// <summary>
/// Тесты модуля Converter — бизнес-логика конвертации.
/// </summary>
public class ConverterTests
{
    private static readonly string ProjectRoot = ResolveProjectRoot();
    private static readonly string TestFile = Path.Combine(ProjectRoot, "tests", "fixtures", "test.ts");
    private static readonly string OutputDir = Path.Combine(ProjectRoot, "tests", "fixtures");
    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    private static string ResolveProjectRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "DashcamConverter.sln")))
            dir = Path.GetDirectoryName(dir);
        return dir ?? AppContext.BaseDirectory;
    }

    [Fact]
    public void ValidateInput_ValidFile_ReturnsTrue()
    {
        if (!IsWindows)
            return;

        Assert.True(Converter.ValidateInput(TestFile));
    }

    [Fact]
    public void ValidateInput_Nonexistent_ReturnsFalse()
    {
        if (!IsWindows)
            return;

        Assert.False(Converter.ValidateInput(Path.Combine(ProjectRoot, "nonexistent.ts")));
    }

    [Fact]
    public void Remux_CreatesOutputFile()
    {
        if (!IsWindows)
            return;

        var output = Path.Combine(OutputDir, "test-output.mp4");
        try
        {
            var result = Converter.Remux(TestFile, output);
            Assert.True(File.Exists(result));
            Assert.EndsWith(".mp4", result);
        }
        finally
        {
            if (File.Exists(output))
                File.Delete(output);
        }
    }

    [Fact]
    public void Remux_ReturnsOutputPath()
    {
        if (!IsWindows)
            return;

        var output = Path.Combine(OutputDir, "test-result.mp4");
        try
        {
            var result = Converter.Remux(TestFile, output);
            Assert.Equal(output, result);
            Assert.True(File.Exists(result));
        }
        finally
        {
            if (File.Exists(output))
                File.Delete(output);
        }
    }

    [Fact]
    public void Remux_WithProgress_CallsCallback()
    {
        if (!IsWindows)
            return;

        var progressValues = new List<double>();
        var output = Path.Combine(OutputDir, "test-progress.mp4");

        try
        {
            var result = Converter.Remux(TestFile, output, p => progressValues.Add(p));
            Assert.NotEmpty(progressValues);
            Assert.True(File.Exists(result));
        }
        finally
        {
            if (File.Exists(output))
                File.Delete(output);
        }
    }

    [Fact]
    public void Remux_NonexistentFile_ThrowsFileNotFoundException()
    {
        if (!IsWindows)
            return;

        var nonexistent = Path.Combine(ProjectRoot, "tests", "fixtures", "nonexistent.ts");
        Assert.Throws<FileNotFoundException>(() =>
            Converter.Remux(nonexistent, Path.Combine(OutputDir, "bad.mp4"))
        );
    }
}
