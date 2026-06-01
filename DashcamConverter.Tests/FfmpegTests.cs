using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;
using DashcamConverter;

/// <summary>
/// Тесты модуля Ffmpeg — P/Invoke FFmpeg C API.
/// </summary>
public class FfmpegTests
{
    private static readonly string ProjectRoot = ResolveProjectRoot();
    private static readonly string TestFile = Path.Combine(ProjectRoot, "tests", "fixtures", "test.ts");
    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    private static string ResolveProjectRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "DashcamConverter.sln")))
            dir = Path.GetDirectoryName(dir);
        return dir ?? AppContext.BaseDirectory;
    }

    [Fact]
    public void ProbeDuration_ValidFile_ReturnsPositive()
    {
        if (!IsWindows)
            return;

        var duration = Ffmpeg.ProbeDuration(TestFile);
        Assert.NotNull(duration);
        Assert.True(duration > 0);
    }

    [Fact]
    public void ProbeDuration_NonexistentFile_ReturnsNull()
    {
        if (!IsWindows)
            return;

        var duration = Ffmpeg.ProbeDuration(Path.Combine(ProjectRoot, "nonexistent.ts"));
        Assert.Null(duration);
    }

    [Fact]
    public void RemuxDirect_CreatesOutputFile()
    {
        if (!IsWindows)
            return;

        var output = Path.Combine(ProjectRoot, "tests", "fixtures", "test-remux.mp4");
        try
        {
            var (exitCode, _) = Ffmpeg.RemuxDirect(TestFile, output);
            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(output));
        }
        finally
        {
            if (File.Exists(output))
                File.Delete(output);
        }
    }

    [Fact]
    public void RemuxDirect_WithProgress_CallsCallback()
    {
        if (!IsWindows)
            return;

        var progressValues = new List<double>();
        var output = Path.Combine(ProjectRoot, "tests", "fixtures", "test-progress.mp4");

        try
        {
            var (exitCode, _) = Ffmpeg.RemuxDirect(TestFile, output, p => progressValues.Add(p));
            Assert.Equal(0, exitCode);
            Assert.NotEmpty(progressValues);
        }
        finally
        {
            if (File.Exists(output))
                File.Delete(output);
        }
    }
}
