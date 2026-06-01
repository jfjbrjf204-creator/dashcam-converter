using System.Reflection;
using FFmpeg.AutoGen;

namespace DashcamConverter;

public class FfmpegException : Exception
{
    public string Stderr { get; }

    public FfmpegException(string message, string stderr = "") : base(message)
    {
        Stderr = stderr;
    }
}

public class FfmpegNotFoundException : FfmpegException
{
    public FfmpegNotFoundException(string message = "FFmpeg не найден.") : base(message) { }
}

public static class Ffmpeg
{
    private static bool _initialized;
    private static string _dllDir = string.Empty;

    public static void Initialize()
    {
        if (_initialized)
            return;

        _dllDir = Path.Combine(Path.GetTempPath(), "DashcamConverter", "ffmpeg");
        Directory.CreateDirectory(_dllDir);

        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();

        foreach (var name in resourceNames)
        {
            if (!name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                continue;

            var parts = name.Split('.');
            if (parts.Length < 2)
                continue;

            var fileName = parts[^2] + "." + parts[^1];
            var destPath = Path.Combine(_dllDir, fileName);

            if (File.Exists(destPath))
                continue;

            try
            {
                using var stream = assembly.GetManifestResourceStream(name);
                if (stream == null)
                    continue;
                using var file = File.Create(destPath);
                stream.CopyTo(file);
            }
            catch
            {
            }
        }

        ffmpeg.RootPath = _dllDir;
        _initialized = true;
    }

    private static unsafe void FixCodecIds(AVFormatContext* ctx)
    {
        for (int i = 0; i < ctx->nb_streams; i++)
        {
            var codecpar = ctx->streams[i]->codecpar;
            if (codecpar->codec_id == AVCodecID.AV_CODEC_ID_NONE
                && codecpar->codec_tag == 0x35363278)
            {
                codecpar->codec_id = AVCodecID.AV_CODEC_ID_HEVC;
            }
        }
    }

    public static unsafe double? ProbeDuration(string inputPath)
    {
        Initialize();

        AVFormatContext* ctx = null;
        try
        {
            var ret = ffmpeg.avformat_open_input(&ctx, inputPath, null, null);
            if (ret < 0)
                return null;

            FixCodecIds(ctx);
            ffmpeg.avformat_find_stream_info(ctx, null);
            FixCodecIds(ctx);
            var duration = ctx->duration / (double)ffmpeg.AV_TIME_BASE;
            return duration > 0 ? duration : null;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (ctx != null)
                ffmpeg.avformat_close_input(&ctx);
        }
    }

    public static unsafe (int ExitCode, string Error) RemuxDirect(
        string inputPath,
        string outputPath,
        Action<double>? onProgress = null)
    {
        Initialize();

        AVFormatContext* inCtx = null;
        AVFormatContext* outCtx = null;
        AVPacket* pkt = null;

        try
        {
            var ret = ffmpeg.avformat_open_input(&inCtx, inputPath, null, null);
            if (ret < 0)
                return (ret, $"Не удалось открыть входной файл: {inputPath}");

            FixCodecIds(inCtx);
            ret = ffmpeg.avformat_find_stream_info(inCtx, null);
            FixCodecIds(inCtx);
            if (ret < 0)
                return (ret, "Не удалось получить информацию о потоках.");

            ret = ffmpeg.avformat_alloc_output_context2(&outCtx, null, null, outputPath);
            if (ret < 0)
                return (ret, $"Не удалось создать выходной файл: {outputPath}");

            for (int i = 0; i < inCtx->nb_streams; i++)
            {
                var inStream = inCtx->streams[i];
                var outStream = ffmpeg.avformat_new_stream(outCtx, null);
                if (outStream == null)
                    return (-1, "Не удалось создать выходной поток.");

                ffmpeg.avcodec_parameters_copy(outStream->codecpar, inStream->codecpar);
                outStream->codecpar->codec_tag = 0;
            }

            if ((outCtx->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
            {
                ret = ffmpeg.avio_open(&outCtx->pb, outputPath, ffmpeg.AVIO_FLAG_WRITE);
                if (ret < 0)
                    return (ret, $"Не удалось открыть выходной файл для записи: {outputPath}");
            }

            ret = ffmpeg.avformat_write_header(outCtx, null);
            if (ret < 0)
                return (ret, "Не удалось записать заголовок выходного файла.");

            pkt = ffmpeg.av_packet_alloc();
            var totalDuration = inCtx->duration / (double)ffmpeg.AV_TIME_BASE;
            var lastPercent = -1.0;

            while (ffmpeg.av_read_frame(inCtx, pkt) >= 0)
            {
                var inStream = inCtx->streams[pkt->stream_index];
                var outStream = outCtx->streams[pkt->stream_index];

                pkt->pts = ffmpeg.av_rescale_q_rnd(
                    pkt->pts, inStream->time_base, outStream->time_base,
                    AVRounding.AV_ROUND_NEAR_INF | AVRounding.AV_ROUND_PASS_MINMAX);
                pkt->dts = ffmpeg.av_rescale_q_rnd(
                    pkt->dts, inStream->time_base, outStream->time_base,
                    AVRounding.AV_ROUND_NEAR_INF | AVRounding.AV_ROUND_PASS_MINMAX);
                pkt->duration = ffmpeg.av_rescale_q(
                    pkt->duration, inStream->time_base, outStream->time_base);
                pkt->pos = -1;

                if (onProgress != null && totalDuration > 0)
                {
                    var pts = pkt->pts * ffmpeg.av_q2d(outStream->time_base);
                    var percent = Math.Min(pts / totalDuration * 100.0, 100.0);
                    if (Math.Abs(percent - lastPercent) > 0.01)
                    {
                        lastPercent = percent;
                        onProgress(percent);
                    }
                }

                ffmpeg.av_interleaved_write_frame(outCtx, pkt);
                ffmpeg.av_packet_unref(pkt);
            }

            if (onProgress != null && totalDuration > 0 && lastPercent < 99.9)
                onProgress(100.0);

            ffmpeg.av_write_trailer(outCtx);

            return (0, string.Empty);
        }
        catch (Exception ex)
        {
            return (-1, ex.Message);
        }
        finally
        {
            if (pkt != null)
                ffmpeg.av_packet_free(&pkt);

            if (inCtx != null)
                ffmpeg.avformat_close_input(&inCtx);

            if (outCtx != null)
            {
                if ((outCtx->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0 && outCtx->pb != null)
                    ffmpeg.avio_closep(&outCtx->pb);
                ffmpeg.avformat_free_context(outCtx);
            }
        }
    }
}
