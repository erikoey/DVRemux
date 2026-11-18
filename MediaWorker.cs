using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MKV_Converter
{
    public class ConversionResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public static class AnalysisWorker
    {
        // Subtitle streams below these thresholds are treated as empty/forced and skipped.
        private const int MinSubtitleFrames = 50;
        private const long MinSubtitleBytes = 1024;

        private static readonly string[] BitmapSubtitleCodecs = { "hdmv_pgs_subtitle", "dvd_subtitle" };
        private static readonly string[] SupportedAudioCodecs = { "aac", "ac3", "eac3", "mp3", "mp2" };

        public static async Task<MediaFile> AnalyzeFileAsync(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            var mediaFile = new MediaFile
            {
                FilePath = filePath,
                FileName = fileInfo.Name,
                FileSizeBytes = fileInfo.Length,
                FileSize = FormatFileSize(fileInfo.Length)
            };

            string ffprobePath = Path.Combine(AppContext.BaseDirectory, "ffmpeg", "ffprobe.exe");

            var processStartInfo = new ProcessStartInfo(ffprobePath, $"-v quiet -print_format json -show_format -show_streams \"{filePath}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using var process = Process.Start(processStartInfo);
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            string jsonOutput = await outputTask;
            await errorTask;

            try
            {
                if (!string.IsNullOrEmpty(jsonOutput))
                {
                    using var doc = JsonDocument.Parse(jsonOutput);

                    if (doc.RootElement.TryGetProperty("format", out var format) &&
                        format.TryGetProperty("duration", out var durationElement) &&
                        double.TryParse(durationElement.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double durationSeconds))
                    {
                        mediaFile.DurationSeconds = durationSeconds;
                    }

                    if (!doc.RootElement.TryGetProperty("streams", out var streamsElement))
                    {
                        streamsElement = default;
                    }

                    int subtitleStreamIndex = 0;
                    int audioStreamIndex = 0;

                    var streams = streamsElement.ValueKind == JsonValueKind.Array
                        ? streamsElement.EnumerateArray().ToList()
                        : new List<JsonElement>();

                    foreach (var stream in streams)
                    {
                        string codecType = GetString(stream, "codec_type");
                        string codecName = GetString(stream, "codec_name") ?? "unknown";

                        if (codecType == "video")
                        {
                            // Only the first video stream is used (cover art etc. is dropped)
                            if (!string.IsNullOrEmpty(mediaFile.VideoCodec)) continue;
                            mediaFile.VideoCodec = codecName;

                            if (stream.TryGetProperty("side_data_list", out var sideDataList) && sideDataList.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var sideData in sideDataList.EnumerateArray())
                                {
                                    if (GetString(sideData, "side_data_type") == "DOVI configuration record")
                                    {
                                        int profile = GetInt(sideData, "dv_profile") ?? -1;
                                        int compatId = GetInt(sideData, "dv_bl_signal_compatibility_id") ?? -1;
                                        mediaFile.DolbyVisionProfile = (profile == 8 && compatId == 1) ? "8.1" : profile.ToString();
                                    }
                                }
                            }
                        }
                        else if (codecType == "subtitle")
                        {
                            if (Array.Exists(BitmapSubtitleCodecs, c => c == codecName))
                            {
                                // Bitmap subs can't be converted to mov_text; they are simply not mapped.
                                mediaFile.HasBitmapSubs = true;
                            }
                            else if (!IsEmptySubtitleStream(stream, codecName))
                            {
                                mediaFile.HasTextSubs = true;
                                mediaFile.ValidSubtitleIndices.Add(subtitleStreamIndex);
                                mediaFile.SubtitleLanguages[subtitleStreamIndex] = GetLanguage(stream);
                            }

                            // Always increment so the index matches FFmpeg's 0:s:N numbering
                            subtitleStreamIndex++;
                        }
                        else if (codecType == "audio")
                        {
                            int channels = GetInt(stream, "channels") ?? 2;
                            bool needsConversion = !Array.Exists(SupportedAudioCodecs, c => string.Equals(c, codecName, StringComparison.OrdinalIgnoreCase));

                            mediaFile.AudioTracks.Add(new AudioTrack
                            {
                                Index = audioStreamIndex,
                                Codec = codecName,
                                Channels = channels,
                                RequiresConversion = needsConversion,
                                Language = GetLanguage(stream),
                                Role = GetAudioRole(stream)
                            });

                            audioStreamIndex++;
                        }
                    }

                    MarkRedundantAudioTracks(mediaFile.AudioTracks);

                    if (mediaFile.AudioTracks.Count > 0)
                    {
                        mediaFile.OriginalAudioCodec = string.Join(", ",
                            mediaFile.AudioTracks.Select(t => t.Codec).Distinct(StringComparer.OrdinalIgnoreCase)).ToUpper();
                        mediaFile.AudioChannels = mediaFile.AudioTracks.Max(t => t.Channels);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error analyzing {filePath}: {ex.Message}");
                mediaFile.DolbyVisionProfile = "Error";
            }

            if (string.IsNullOrEmpty(mediaFile.DolbyVisionProfile)) mediaFile.DolbyVisionProfile = "No";
            return mediaFile;
        }

        /// <summary>
        /// An unsupported track (e.g. DTS) is redundant when a supported track (e.g. AC3) with the same language
        /// and role already exists, and that track has at least as many channels as the AC3 conversion would produce
        /// (AC3 is capped at 5.1). This avoids e.g. dropping a 5.1 DTS track in favour of a stereo AC3 track.
        /// </summary>
        private static void MarkRedundantAudioTracks(List<AudioTrack> tracks)
        {
            foreach (var track in tracks.Where(t => t.RequiresConversion))
            {
                int convertedChannels = Math.Min(track.Channels, 6);
                track.IsRedundant = tracks.Any(s =>
                    !s.RequiresConversion &&
                    string.Equals(s.Language, track.Language, StringComparison.OrdinalIgnoreCase) &&
                    s.Role == track.Role &&
                    s.Channels >= convertedChannels);
            }
        }

        private static string GetLanguage(JsonElement stream)
        {
            if (stream.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Object)
            {
                string language = GetTag(tags, "language");
                if (!string.IsNullOrWhiteSpace(language)) return language.Trim().ToLowerInvariant();
            }
            return "und";
        }

        private static string GetAudioRole(JsonElement stream)
        {
            if (stream.TryGetProperty("disposition", out var disposition) && disposition.ValueKind == JsonValueKind.Object)
            {
                if (GetInt(disposition, "comment") == 1) return "commentary";
                if (GetInt(disposition, "visual_impaired") == 1) return "description";
            }
            if (stream.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Object)
            {
                string title = GetTag(tags, "title") ?? "";
                if (title.Contains("comment", StringComparison.OrdinalIgnoreCase)) return "commentary";
                if (title.Contains("descri", StringComparison.OrdinalIgnoreCase)) return "description";
            }
            return "main";
        }

        /// <summary>
        /// Detects empty or "forced" subtitle streams using the MKV statistics tags.
        /// A missing tag is treated as unknown (not empty), so streams without statistics are kept.
        /// </summary>
        private static bool IsEmptySubtitleStream(JsonElement stream, string codecName)
        {
            if (codecName == "unknown") return true;
            if (!stream.TryGetProperty("tags", out var tags) || tags.ValueKind != JsonValueKind.Object) return false;

            string framesText = GetTag(tags, "NUMBER_OF_FRAMES");
            if (framesText != null && long.TryParse(framesText, NumberStyles.Integer, CultureInfo.InvariantCulture, out long frames) && frames < MinSubtitleFrames)
                return true;

            string bytesText = GetTag(tags, "NUMBER_OF_BYTES");
            if (bytesText != null && long.TryParse(bytesText, NumberStyles.Integer, CultureInfo.InvariantCulture, out long bytes) && bytes < MinSubtitleBytes)
                return true;

            return false;
        }

        /// <summary>
        /// Finds an MKV statistics tag, which may be written as e.g. "NUMBER_OF_FRAMES" or "NUMBER_OF_FRAMES-eng".
        /// </summary>
        private static string GetTag(JsonElement tags, string name)
        {
            foreach (var property in tags.EnumerateObject())
            {
                if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    property.Name.StartsWith(name + "-", StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : property.Value.ToString();
                }
            }
            return null;
        }

        private static string GetString(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        }

        private static int? GetInt(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int result) ? result : null;
        }

        private static string FormatFileSize(long sizeInBytes)
        {
            if (sizeInBytes < 1024) return $"{sizeInBytes} B";
            double size = sizeInBytes;
            string[] units = { "KB", "MB", "GB", "TB" };
            int unitIndex = -1;
            do { size /= 1024.0; unitIndex++; } while (size >= 1024.0 && unitIndex < units.Length - 1);
            return $"{size:0.##} {units[unitIndex]}";
        }
    }

    public class ConversionWorker
    {
        public MediaFile File { get; }
        private readonly string _outputFolder;
        private Process _process;
        private static CancellationTokenSource _cts = new CancellationTokenSource();

        // Latest "out_time_us" reported by ffmpeg's -progress output, in microseconds
        private long _outTimeUs;

        public event Action<int, bool, string> ProgressUpdated;

        public ConversionWorker(MediaFile file, string outputFolder)
        {
            File = file;
            _outputFolder = outputFolder;
        }

        public static void CancelAll()
        {
            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }

        public static void ResetCancellation()
        {
            _cts = new CancellationTokenSource();
        }

        public async Task<ConversionResult> RunConversionAsync()
        {
            string outputFile = Path.Combine(string.IsNullOrEmpty(_outputFolder) ? Path.GetDirectoryName(File.FilePath) : _outputFolder, $"{Path.GetFileNameWithoutExtension(File.FilePath)}.mp4");
            string arguments = BuildArguments(File, outputFile);
            Logger.Log($"ffmpeg {arguments}");
            return await ExecuteFfmpegWithPolling(arguments, outputFile, "Converting...");
        }

        public static string BuildArguments(MediaFile file, string outputFile)
        {
            // Apply the hvc1 tag for HEVC so LG recognises HEVC / Dolby Vision in MP4
            string videoTag = (file.VideoCodec != null && file.VideoCodec.Equals("hevc", StringComparison.OrdinalIgnoreCase)) ? " -tag:v hvc1" : "";

            // Audio: map the kept tracks explicitly (redundant unsupported tracks are dropped), copy the supported
            // ones and convert only the unsupported ones to AC3.
            // -map uses the INPUT audio index; -c:a:N / -b:a:N / -ac:a:N use the OUTPUT audio index.
            var keptTracks = file.AudioTracks.Where(t => t.IsKept).ToList();
            var audioArgs = new StringBuilder();
            foreach (var track in keptTracks)
            {
                audioArgs.Append($" -map 0:a:{track.Index}");
            }
            for (int outIndex = 0; outIndex < keptTracks.Count; outIndex++)
            {
                var track = keptTracks[outIndex];
                if (!track.RequiresConversion)
                {
                    audioArgs.Append($" -c:a:{outIndex} copy");
                    continue;
                }

                string bitrate = track.Channels >= 6 ? "640k" : "224k";
                audioArgs.Append($" -c:a:{outIndex} ac3 -b:a:{outIndex} {bitrate}");
                if (track.Channels > 6)
                {
                    // AC3 supports at most 5.1, so downmix anything larger
                    audioArgs.Append($" -ac:a:{outIndex} 6");
                }
            }

            // Subtitles: map only valid text tracks. Bitmap tracks are left out, which doesn't affect the text tracks.
            var subArgs = new StringBuilder();
            foreach (int subIndex in file.ValidSubtitleIndices)
            {
                subArgs.Append($" -map 0:s:{subIndex}");
            }
            if (file.ValidSubtitleIndices.Count > 0)
            {
                subArgs.Append(" -c:s mov_text");
            }

            // -map_metadata -1 strips all metadata, including track languages, so write the languages back
            // explicitly (by OUTPUT index) so the TV can label audio and subtitle tracks.
            var languageArgs = new StringBuilder();
            for (int outIndex = 0; outIndex < keptTracks.Count; outIndex++)
            {
                AppendLanguage(languageArgs, "a", outIndex, keptTracks[outIndex].Language);
            }
            for (int outIndex = 0; outIndex < file.ValidSubtitleIndices.Count; outIndex++)
            {
                file.SubtitleLanguages.TryGetValue(file.ValidSubtitleIndices[outIndex], out string language);
                AppendLanguage(languageArgs, "s", outIndex, language);
            }

            // -map 0:v:0 grabs ONLY the primary video track, stripping out embedded cover art thumbnails.
            return $"-y -fflags +genpts -i \"{file.FilePath}\" -hide_banner -nostats -loglevel warning -progress pipe:1 " +
                   $"-map 0:v:0{audioArgs}{subArgs} -c:v copy{videoTag} " +
                   $"-map_metadata -1{languageArgs} -dn -map_chapters -1 -movflags +faststart -video_track_timescale 90000 -strict -2 \"{outputFile}\"";
        }

        private static void AppendLanguage(StringBuilder args, string streamType, int outIndex, string language)
        {
            // Only plain language codes (e.g. "eng", "ger"); anything else is left out of the command line
            if (string.IsNullOrEmpty(language) || language == "und" || !language.All(char.IsAsciiLetterLower)) return;
            args.Append($" -metadata:s:{streamType}:{outIndex} language={language}");
        }

        private async Task<ConversionResult> ExecuteFfmpegWithPolling(string arguments, string outputFile, string initialStatusText)
        {
            string ffmpegPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg", "ffmpeg.exe");

            var processStartInfo = new ProcessStartInfo(ffmpegPath, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                _process = Process.Start(processStartInfo);
                var errorTask = _process.StandardError.ReadToEndAsync();
                var progressTask = ReadProgressAsync(_process.StandardOutput);

                while (!_process.HasExited)
                {
                    if (_cts.Token.IsCancellationRequested)
                    {
                        _process.Kill(true);
                        _process.WaitForExit();
                        DeletePartialOutput(outputFile);
                        Logger.Log($"Cancelled: {File.FilePath}");
                        return new ConversionResult { Success = false, ErrorMessage = "Cancelled" };
                    }

                    int? percent = GetPercent(outputFile);
                    if (percent.HasValue)
                    {
                        bool isFinalizing = percent.Value >= 99;
                        ProgressUpdated?.Invoke(percent.Value, isFinalizing, isFinalizing ? "Finalizing..." : $"{initialStatusText} {percent.Value}%");
                    }

                    await Task.Delay(1000);
                }

                string errorOutput = await errorTask;
                await progressTask;

                bool success = _process.ExitCode == 0;
                if (success)
                {
                    ProgressUpdated?.Invoke(100, false, "Done");
                }
                else
                {
                    DeletePartialOutput(outputFile);
                }

                Logger.Log($"{(success ? "Success" : $"Failed (exit {_process.ExitCode})")}: {File.FilePath}{(string.IsNullOrWhiteSpace(errorOutput) ? "" : Environment.NewLine + errorOutput.Trim())}");
                return new ConversionResult { Success = success, ErrorMessage = success ? "" : errorOutput };
            }
            catch (Exception ex)
            {
                Logger.Log($"Error converting {File.FilePath}: {ex.Message}");
                return new ConversionResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        private async Task ReadProgressAsync(StreamReader reader)
        {
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.StartsWith("out_time_us=", StringComparison.Ordinal) &&
                    long.TryParse(line.AsSpan("out_time_us=".Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out long us))
                {
                    Interlocked.Exchange(ref _outTimeUs, us);
                }
            }
        }

        /// <summary>
        /// Progress based on output timestamp vs. duration. Falls back to output size vs. input size
        /// when the duration is unknown.
        /// </summary>
        private int? GetPercent(string outputFile)
        {
            if (File.DurationSeconds > 0)
            {
                long us = Interlocked.Read(ref _outTimeUs);
                if (us <= 0) return 0;
                return (int)Math.Clamp(us / (File.DurationSeconds * 1_000_000) * 100, 0, 100);
            }

            try
            {
                var outputInfo = new FileInfo(outputFile);
                if (outputInfo.Exists && File.FileSizeBytes > 0)
                {
                    return (int)Math.Min(100, (double)outputInfo.Length / File.FileSizeBytes * 100);
                }
            }
            catch { /* Ignore */ }
            return null;
        }

        private static void DeletePartialOutput(string outputFile)
        {
            try
            {
                if (System.IO.File.Exists(outputFile)) System.IO.File.Delete(outputFile);
            }
            catch (Exception ex)
            {
                Logger.Log($"Could not delete partial output {outputFile}: {ex.Message}");
            }
        }
    }
}
