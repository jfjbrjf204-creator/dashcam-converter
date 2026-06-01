using System.Reflection;
using FFmpeg.AutoGen;

namespace DashcamConverter;

public class FfmpegException : Exception
{
    public string Stderr { get; }
    public string? Operation { get; }

    public FfmpegException(string message, string stderr = "", string? operation = null) : base(message)
    {
        Stderr = stderr;
        Operation = operation;
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

    private const int ErrorBufferSize = 64;

    private unsafe struct TranscodeState
    {
        public AVCodecContext* decCtx;
        public AVCodecContext* encCtx;
        public SwrContext* swrCtx;
        public AVFrame* decFrame;
        public AVFrame* encFrame;
        public AVPacket* encPkt;
        public int inStreamIdx;
        public int outStreamIdx;
        public long sampleCount;
    }

    /// <summary>
    /// Преобразует код ошибки FFmpeg (<c>AVERROR</c>) в читаемое описание через <c>av_strerror</c>.
    /// Возвращает строку вида <c>"-22: Invalid data found when processing input"</c>
    /// или <c>"код ошибки -22"</c> если расшифровка недоступна.
    /// </summary>
    private static unsafe string FfmpegErrorString(int errcode)
    {
        var buffer = stackalloc byte[ErrorBufferSize];
        var ret = ffmpeg.av_strerror(errcode, buffer, (ulong)ErrorBufferSize);
        if (ret == 0)
        {
            var len = 0;
            while (len < ErrorBufferSize && buffer[len] != 0)
                len++;
            return $"{errcode}: {System.Text.Encoding.UTF8.GetString(buffer, len)}";
        }
        return $"код ошибки {errcode}";
    }

    public static void Initialize()
    {
        if (_initialized)
            return;

        var envPath = Environment.GetEnvironmentVariable("DASHCAM_FFMPEG_PATH");
        if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
        {
            _dllDir = envPath;
            ffmpeg.RootPath = _dllDir;
            _initialized = true;
            return;
        }

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

    private static int NearestSupportedSampleRate(int rate)
    {
        int[] supported = { 8000, 11025, 12000, 16000, 22050, 24000, 32000, 44100, 48000 };
        int nearest = supported[0];
        int minDiff = Math.Abs(rate - nearest);
        for (int i = 1; i < supported.Length; i++)
        {
            int diff = Math.Abs(rate - supported[i]);
            if (diff < minDiff)
            {
                minDiff = diff;
                nearest = supported[i];
            }
        }
        return nearest;
    }

    public static double? ProbeDuration(string inputPath) => ProbeDuration(inputPath, out _);

    public static unsafe double? ProbeDuration(string inputPath, out string? error)
    {
        Initialize();
        error = null;

        AVFormatContext* ctx = null;
        try
        {
            var ret = ffmpeg.avformat_open_input(&ctx, inputPath, null, null);
            if (ret < 0)
            {
                error = $"[Ffmpeg.ProbeDuration] avformat_open_input \"{inputPath}\": {FfmpegErrorString(ret)}";
                return null;
            }

            FixCodecIds(ctx);
            ffmpeg.avformat_find_stream_info(ctx, null);
            FixCodecIds(ctx);

            var duration = ctx->duration / (double)ffmpeg.AV_TIME_BASE;
            if (duration <= 0)
            {
                error = $"[Ffmpeg.ProbeDuration] Не удалось определить длительность \"{inputPath}\": duration = {ctx->duration}";
                return null;
            }

            return duration;
        }
        catch (Exception ex)
        {
            error = $"[Ffmpeg.ProbeDuration] Исключение при анализе \"{inputPath}\": {ex.GetType().Name}: {ex.Message}";
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
        var transcodeStates = new List<TranscodeState>();

        try
        {
            var ret = ffmpeg.avformat_open_input(&inCtx, inputPath, null, null);
            if (ret < 0)
                return (ret, $"[FFMPEG-020] avformat_open_input \"{inputPath}\": {FfmpegErrorString(ret)}");

            FixCodecIds(inCtx);
            ret = ffmpeg.avformat_find_stream_info(inCtx, null);
            FixCodecIds(inCtx);
            if (ret < 0)
                return (ret, $"[FFMPEG-021] avformat_find_stream_info \"{inputPath}\": {FfmpegErrorString(ret)}");

            ret = ffmpeg.avformat_alloc_output_context2(&outCtx, null, null, outputPath);
            if (ret < 0)
                return (ret, $"[FFMPEG-022] avformat_alloc_output_context2 \"{outputPath}\": {FfmpegErrorString(ret)}");

            for (int i = 0; i < inCtx->nb_streams; i++)
            {
                var inStream = inCtx->streams[i];
                bool needsTranscode = false;

                if (inStream->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                {
                    var compatRet = ffmpeg.avformat_query_codec(outCtx->oformat, inStream->codecpar->codec_id, 0);
                    if (compatRet <= 0)
                        needsTranscode = true;
                }

                if (needsTranscode)
                {
                    var decoder = ffmpeg.avcodec_find_decoder(inStream->codecpar->codec_id);
                    if (decoder == null)
                    {
                        var codecName = inStream->codecpar->codec_id.ToString();
                        return (-1, $"[FFMPEG-030] Decoder not found for codec {codecName} (stream {i}).");
                    }

                    var decCtx = ffmpeg.avcodec_alloc_context3(decoder);
                    if (decCtx == null)
                        return (-1, $"[FFMPEG-031] Cannot alloc decoder context (stream {i}).");

                    ffmpeg.avcodec_parameters_to_context(decCtx, inStream->codecpar);
                    ret = ffmpeg.avcodec_open2(decCtx, decoder, null);
                    if (ret < 0)
                        return (ret, $"[FFMPEG-032] avcodec_open2 decoder (stream {i}): {FfmpegErrorString(ret)}");

                    var mp4FamilyOutput = outputPath.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
                                          || outputPath.EndsWith(".m4v", StringComparison.OrdinalIgnoreCase)
                                          || outputPath.EndsWith(".mov", StringComparison.OrdinalIgnoreCase);

                    AVCodec* encoder;
                    AVCodecID encoderCodecId;
                    if (mp4FamilyOutput)
                    {
                        encoderCodecId = AVCodecID.AV_CODEC_ID_AAC;
                        encoder = ffmpeg.avcodec_find_encoder(encoderCodecId);
                    }
                    else
                    {
                        encoderCodecId = AVCodecID.AV_CODEC_ID_MP3;
                        encoder = ffmpeg.avcodec_find_encoder_by_name("libmp3lame");
                        if (encoder == null)
                            encoder = ffmpeg.avcodec_find_encoder(encoderCodecId);
                        if (encoder == null)
                        {
                            encoderCodecId = AVCodecID.AV_CODEC_ID_MP2;
                            encoder = ffmpeg.avcodec_find_encoder(encoderCodecId)
                                      ?? ffmpeg.avcodec_find_encoder_by_name("mp2");
                        }
                    }

                    if (encoder == null)
                        return (-1, $"[FFMPEG-033] No compatible audio encoder found (stream {i}).");

                    var encCtx = ffmpeg.avcodec_alloc_context3(encoder);
                    if (encCtx == null)
                        return (-1, $"[FFMPEG-034] Cannot alloc encoder context (stream {i}).");

                    int sampleRate = decCtx->sample_rate;
                    if (sampleRate <= 0) sampleRate = 44100;
                    sampleRate = NearestSupportedSampleRate(sampleRate);

                    AVSampleFormat targetFmt = AVSampleFormat.AV_SAMPLE_FMT_FLTP;
#pragma warning disable CS0618 // AVCodec.sample_fmts deprecated in FFmpeg 8.1; TODO: migrate to avcodec_get_supported_config()
                    if (encoder->sample_fmts != null)
                    {
                        AVSampleFormat firstFmt = AVSampleFormat.AV_SAMPLE_FMT_NONE;
                        targetFmt = AVSampleFormat.AV_SAMPLE_FMT_NONE;
                        for (int j = 0; ; j++)
                        {
                            var fmt = encoder->sample_fmts[j];
#line hidden // avoid duplicate warning
#pragma warning restore CS0618
                            if (fmt == AVSampleFormat.AV_SAMPLE_FMT_NONE) break;
                            if (firstFmt == AVSampleFormat.AV_SAMPLE_FMT_NONE) firstFmt = fmt;
                            if (fmt == AVSampleFormat.AV_SAMPLE_FMT_FLTP || fmt == AVSampleFormat.AV_SAMPLE_FMT_S16P)
                            {
                                targetFmt = fmt;
                                break;
                            }
                            if (targetFmt == AVSampleFormat.AV_SAMPLE_FMT_NONE) targetFmt = fmt;
                        }
                        if (targetFmt == AVSampleFormat.AV_SAMPLE_FMT_NONE) targetFmt = firstFmt;
                        if (targetFmt == AVSampleFormat.AV_SAMPLE_FMT_NONE) targetFmt = AVSampleFormat.AV_SAMPLE_FMT_FLTP;
                    }

                    encCtx->sample_fmt = targetFmt;
                    var channelCount = decCtx->ch_layout.nb_channels > 0 ? decCtx->ch_layout.nb_channels : 1;
                    encCtx->bit_rate = encoderCodecId == AVCodecID.AV_CODEC_ID_AAC
                        ? Math.Min(128000, sampleRate * channelCount * 6)
                        : 128000;
                    encCtx->sample_rate = sampleRate;
                    ffmpeg.av_channel_layout_default(&encCtx->ch_layout, channelCount);
                    encCtx->time_base = new AVRational { num = 1, den = encCtx->sample_rate };
                    encCtx->codec_id = encoderCodecId;

                    ret = ffmpeg.avcodec_open2(encCtx, encoder, null);
                    if (ret < 0)
                        return (ret, $"[FFMPEG-035] avcodec_open2 encoder (stream {i}): {FfmpegErrorString(ret)}");

                    SwrContext* swr = null;
                    bool needSwr = decCtx->sample_fmt != encCtx->sample_fmt
                                   || decCtx->sample_rate != encCtx->sample_rate
                                   || decCtx->ch_layout.nb_channels != encCtx->ch_layout.nb_channels;
                    if (!needSwr && decCtx->ch_layout.nb_channels > 0)
                        needSwr = decCtx->ch_layout.order != encCtx->ch_layout.order;

                    if (needSwr)
                    {
                        ret = ffmpeg.swr_alloc_set_opts2(&swr,
                            &encCtx->ch_layout, encCtx->sample_fmt, encCtx->sample_rate,
                            &decCtx->ch_layout, decCtx->sample_fmt, decCtx->sample_rate,
                            0, null);
                        if (ret < 0 || swr == null)
                            return (ret < 0 ? ret : -1, $"[FFMPEG-036] swr_alloc_set_opts2 (stream {i}): {FfmpegErrorString(ret)}");

                        ret = ffmpeg.swr_init(swr);
                        if (ret < 0)
                            return (ret, $"[FFMPEG-037] swr_init (stream {i}): {FfmpegErrorString(ret)}");
                    }

                    var outStream = ffmpeg.avformat_new_stream(outCtx, null);
                    if (outStream == null)
                        return (-1, $"[FFMPEG-043] avformat_new_stream transcode (stream {i}): cannot create output stream.");

                    ret = ffmpeg.avcodec_parameters_from_context(outStream->codecpar, encCtx);
                    if (ret < 0)
                        return (ret, $"[FFMPEG-038] avcodec_parameters_from_context (stream {i}): {FfmpegErrorString(ret)}");

                    outStream->codecpar->codec_tag = 0;
                    outStream->time_base = encCtx->time_base;

                    var decFrame = ffmpeg.av_frame_alloc();
                    if (decFrame == null)
                        return (-1, $"[FFMPEG-039] av_frame_alloc decFrame (stream {i}): alloc failed.");

                    AVFrame* encFrame = null;
                    if (needSwr)
                    {
                        encFrame = ffmpeg.av_frame_alloc();
                        if (encFrame == null)
                            return (-1, $"[FFMPEG-040] av_frame_alloc encFrame (stream {i}): alloc failed.");

                        encFrame->format = (int)encCtx->sample_fmt;
                        encFrame->sample_rate = encCtx->sample_rate;
                        ffmpeg.av_channel_layout_copy(&encFrame->ch_layout, &encCtx->ch_layout);
                        encFrame->nb_samples = encCtx->frame_size > 0 ? encCtx->frame_size : 1024;
                        ret = ffmpeg.av_frame_get_buffer(encFrame, 0);
                        if (ret < 0)
                            return (ret, $"[FFMPEG-041] av_frame_get_buffer encFrame (stream {i}): {FfmpegErrorString(ret)}");
                    }

                    var encPkt = ffmpeg.av_packet_alloc();
                    if (encPkt == null)
                        return (-1, $"[FFMPEG-042] av_packet_alloc encPkt (stream {i}): alloc failed.");

                    transcodeStates.Add(new TranscodeState
                    {
                        decCtx = decCtx,
                        encCtx = encCtx,
                        swrCtx = swr,
                        decFrame = decFrame,
                        encFrame = encFrame,
                        encPkt = encPkt,
                        inStreamIdx = i,
                        outStreamIdx = i,
                        sampleCount = 0
                    });
                }
                else
                {
                    var outStream = ffmpeg.avformat_new_stream(outCtx, null);
                    if (outStream == null)
                        return (-1, $"[FFMPEG-023] avformat_new_stream (stream {i}, \"{inputPath}\"): cannot create output stream.");

                    ffmpeg.avcodec_parameters_copy(outStream->codecpar, inStream->codecpar);
                    outStream->codecpar->codec_tag = 0;
                }
            }


            if ((outCtx->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
            {
                ret = ffmpeg.avio_open(&outCtx->pb, outputPath, ffmpeg.AVIO_FLAG_WRITE);
                if (ret < 0)
                    return (ret, $"[FFMPEG-024] avio_open \"{outputPath}\": {FfmpegErrorString(ret)}");
            }

            ret = ffmpeg.avformat_write_header(outCtx, null);
            if (ret < 0)
                return (ret, $"[FFMPEG-025] avformat_write_header \"{outputPath}\": {FfmpegErrorString(ret)}");

            pkt = ffmpeg.av_packet_alloc();
            var totalDuration = inCtx->duration / (double)ffmpeg.AV_TIME_BASE;
            var lastPercent = -1.0;

            while (ffmpeg.av_read_frame(inCtx, pkt) >= 0)
            {
                int inIdx = pkt->stream_index;
                bool handled = false;

                for (int ti = 0; ti < transcodeStates.Count; ti++)
                {
                    var ts = transcodeStates[ti];
                    if (ts.inStreamIdx != inIdx)
                        continue;

                    ret = ffmpeg.avcodec_send_packet(ts.decCtx, pkt);
                    if (ret < 0 && ret != ffmpeg.AVERROR_EOF)
                    {
                        ffmpeg.av_packet_unref(pkt);
                        handled = true;
                        break;
                    }

                    while (ffmpeg.avcodec_receive_frame(ts.decCtx, ts.decFrame) >= 0)
                    {
                        var inStreamRef = inCtx->streams[inIdx];
                        if (ts.decFrame->pts == ffmpeg.AV_NOPTS_VALUE)
                            ts.decFrame->pts = ts.sampleCount;
                        ts.sampleCount += ts.decFrame->nb_samples;
                        ts.decFrame->pts = ffmpeg.av_rescale_q(
                            ts.decFrame->pts, inStreamRef->time_base, ts.encCtx->time_base);

                        AVFrame* frameToSend;
                        if (ts.swrCtx != null)
                        {
                            ret = ffmpeg.swr_convert_frame(ts.swrCtx, ts.encFrame, ts.decFrame);
                            if (ret < 0) break;
                            ts.encFrame->pts = ts.decFrame->pts;
                            frameToSend = ts.encFrame;
                        }
                        else
                        {
                            frameToSend = ts.decFrame;
                        }

                        ret = ffmpeg.avcodec_send_frame(ts.encCtx, frameToSend);
                        if (ret < 0) break;

                        while (ffmpeg.avcodec_receive_packet(ts.encCtx, ts.encPkt) >= 0)
                        {
                            ts.encPkt->stream_index = ts.outStreamIdx;
                            var outStreamRef = outCtx->streams[ts.outStreamIdx];
                            ts.encPkt->pts = ffmpeg.av_rescale_q_rnd(
                                ts.encPkt->pts, ts.encCtx->time_base, outStreamRef->time_base,
                                AVRounding.AV_ROUND_NEAR_INF | AVRounding.AV_ROUND_PASS_MINMAX);
                            ts.encPkt->dts = ffmpeg.av_rescale_q_rnd(
                                ts.encPkt->dts, ts.encCtx->time_base, outStreamRef->time_base,
                                AVRounding.AV_ROUND_NEAR_INF | AVRounding.AV_ROUND_PASS_MINMAX);
                            ts.encPkt->duration = ffmpeg.av_rescale_q(
                                ts.encPkt->duration, ts.encCtx->time_base, outStreamRef->time_base);
                            ts.encPkt->pos = -1;

                            ffmpeg.av_interleaved_write_frame(outCtx, ts.encPkt);
                            ffmpeg.av_packet_unref(ts.encPkt);
                        }

                        ffmpeg.av_frame_unref(ts.decFrame);
                        if (ts.swrCtx != null)
                            ffmpeg.av_frame_unref(ts.encFrame);
                    }

                    ffmpeg.av_packet_unref(pkt);
                    handled = true;

                    if (onProgress != null && totalDuration > 0)
                    {
                        var tsOutStream = outCtx->streams[ts.outStreamIdx];
                        var pts = ts.decFrame->pts * ffmpeg.av_q2d(tsOutStream->time_base);
                        var percent = Math.Min(pts / totalDuration * 100.0, 100.0);
                        if (Math.Abs(percent - lastPercent) > 0.01)
                        {
                            lastPercent = percent;
                            onProgress(percent);
                        }
                    }
                    break;
                }

                if (handled)
                    continue;

                var inStream = inCtx->streams[inIdx];
                var outStreamDc = outCtx->streams[inIdx];

                pkt->pts = ffmpeg.av_rescale_q_rnd(
                    pkt->pts, inStream->time_base, outStreamDc->time_base,
                    AVRounding.AV_ROUND_NEAR_INF | AVRounding.AV_ROUND_PASS_MINMAX);
                pkt->dts = ffmpeg.av_rescale_q_rnd(
                    pkt->dts, inStream->time_base, outStreamDc->time_base,
                    AVRounding.AV_ROUND_NEAR_INF | AVRounding.AV_ROUND_PASS_MINMAX);
                pkt->duration = ffmpeg.av_rescale_q(
                    pkt->duration, inStream->time_base, outStreamDc->time_base);
                pkt->pos = -1;

                if (onProgress != null && totalDuration > 0)
                {
                    var pts = pkt->pts * ffmpeg.av_q2d(outStreamDc->time_base);
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


            // Flush transcoded streams: decoder flush, then encoder flush
            for (int ti = 0; ti < transcodeStates.Count; ti++)
            {
                var ts = transcodeStates[ti];
                ffmpeg.avcodec_send_packet(ts.decCtx, null);
                while (ffmpeg.avcodec_receive_frame(ts.decCtx, ts.decFrame) >= 0)
                {
                    var inStreamRef = inCtx->streams[ts.inStreamIdx];
                    if (ts.decFrame->pts == ffmpeg.AV_NOPTS_VALUE)
                        ts.decFrame->pts = ts.sampleCount;
                    ts.sampleCount += ts.decFrame->nb_samples;
                    ts.decFrame->pts = ffmpeg.av_rescale_q(
                        ts.decFrame->pts, inStreamRef->time_base, ts.encCtx->time_base);

                    AVFrame* frameToSend;
                    if (ts.swrCtx != null)
                    {
                        ret = ffmpeg.swr_convert_frame(ts.swrCtx, ts.encFrame, ts.decFrame);
                        if (ret < 0) break;
                        ts.encFrame->pts = ts.decFrame->pts;
                        frameToSend = ts.encFrame;
                    }
                    else
                    {
                        frameToSend = ts.decFrame;
                    }

                    ffmpeg.avcodec_send_frame(ts.encCtx, frameToSend);
                    while (ffmpeg.avcodec_receive_packet(ts.encCtx, ts.encPkt) >= 0)
                    {
                        ts.encPkt->stream_index = ts.outStreamIdx;
                        var outStreamRef = outCtx->streams[ts.outStreamIdx];
                        ts.encPkt->pts = ffmpeg.av_rescale_q_rnd(
                            ts.encPkt->pts, ts.encCtx->time_base, outStreamRef->time_base,
                            AVRounding.AV_ROUND_NEAR_INF | AVRounding.AV_ROUND_PASS_MINMAX);
                        ts.encPkt->dts = ffmpeg.av_rescale_q_rnd(
                            ts.encPkt->dts, ts.encCtx->time_base, outStreamRef->time_base,
                            AVRounding.AV_ROUND_NEAR_INF | AVRounding.AV_ROUND_PASS_MINMAX);
                        ts.encPkt->duration = ffmpeg.av_rescale_q(
                            ts.encPkt->duration, ts.encCtx->time_base, outStreamRef->time_base);
                        ts.encPkt->pos = -1;

                        ffmpeg.av_interleaved_write_frame(outCtx, ts.encPkt);
                        ffmpeg.av_packet_unref(ts.encPkt);
                    }

                    ffmpeg.av_frame_unref(ts.decFrame);
                    if (ts.swrCtx != null)
                        ffmpeg.av_frame_unref(ts.encFrame);
                }

                // Flush encoder
                ffmpeg.avcodec_send_frame(ts.encCtx, null);
                while (ffmpeg.avcodec_receive_packet(ts.encCtx, ts.encPkt) >= 0)
                {
                    ts.encPkt->stream_index = ts.outStreamIdx;
                    var outStreamRef = outCtx->streams[ts.outStreamIdx];
                    ts.encPkt->pts = ffmpeg.av_rescale_q_rnd(
                        ts.encPkt->pts, ts.encCtx->time_base, outStreamRef->time_base,
                        AVRounding.AV_ROUND_NEAR_INF | AVRounding.AV_ROUND_PASS_MINMAX);
                    ts.encPkt->dts = ffmpeg.av_rescale_q_rnd(
                        ts.encPkt->dts, ts.encCtx->time_base, outStreamRef->time_base,
                        AVRounding.AV_ROUND_NEAR_INF | AVRounding.AV_ROUND_PASS_MINMAX);
                    ts.encPkt->duration = ffmpeg.av_rescale_q(
                        ts.encPkt->duration, ts.encCtx->time_base, outStreamRef->time_base);
                    ts.encPkt->pos = -1;

                    ffmpeg.av_interleaved_write_frame(outCtx, ts.encPkt);
                    ffmpeg.av_packet_unref(ts.encPkt);
                }
            }

            if (onProgress != null && totalDuration > 0 && lastPercent < 99.9)
                onProgress(100.0);

            ret = ffmpeg.av_write_trailer(outCtx);
            if (ret < 0)
                return (ret, $"[FFMPEG-026] av_write_trailer \"{outputPath}\": {FfmpegErrorString(ret)}");

            return (0, string.Empty);
        }
        catch (FfmpegException ex)
        {
            return (-1, $"[FFMPEG-027] FFmpeg error processing \"{inputPath}\": {ex.Message}");
        }
        catch (Exception ex)
        {
            return (-1, $"[FFMPEG-028] Exception ({ex.GetType().Name}) processing \"{inputPath}\": {ex.Message}");
        }
        finally
        {
            for (int ti = 0; ti < transcodeStates.Count; ti++)
            {
                var ts = transcodeStates[ti];
                if (ts.encPkt != null) ffmpeg.av_packet_free(&ts.encPkt);
                if (ts.decFrame != null) ffmpeg.av_frame_free(&ts.decFrame);
                if (ts.encFrame != null) ffmpeg.av_frame_free(&ts.encFrame);
                if (ts.swrCtx != null) ffmpeg.swr_free(&ts.swrCtx);
                if (ts.encCtx != null) ffmpeg.avcodec_free_context(&ts.encCtx);
                if (ts.decCtx != null) ffmpeg.avcodec_free_context(&ts.decCtx);
            }

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
