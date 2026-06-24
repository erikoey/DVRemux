using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            // Get the exact local path to ffprobe.exe
            string ffprobePath = Path.Combine(AppContext.BaseDirectory, "ffmpeg", "ffprobe.exe");

            // Pass the arguments directly to the executable instead of using cmd.exe
            var processStartInfo = new ProcessStartInfo(ffprobePath, $"-v quiet -print_format json -show_streams \"{filePath}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };
            
            using var process = Process.Start(processStartInfo);
            var outputTask = process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            string jsonOutput = await outputTask;

            try
            {
                if (!string.IsNullOrEmpty(jsonOutput))
                {
                    using var doc = JsonDocument.Parse(jsonOutput);
                    var streams = doc.RootElement.GetProperty("streams").EnumerateArray();
                    string[] bitmapCodecs = { "hdmv_pgs_subtitle", "dvd_subtitle" };
                    string[] supportedAudioCodecs = { "aac", "ac3", "eac3", "mp3", "mp2" };

                    // Variables to track ALL audio streams in the file
                    List<string> foundAudioCodecs = new List<string>();
                    int maxAudioChannels = 0;
                    bool requiresAudioConversion = false;

                    // Counter for subtitle streams
                    int subtitleStreamIndex = 0;

                    foreach (var stream in streams)
                    {
                        if (stream.TryGetProperty("codec_type", out var codecType) && codecType.GetString() == "video")
                        {
                            // Capture the video codec (e.g., "hevc" or "h264")
                            if (string.IsNullOrEmpty(mediaFile.VideoCodec))
                            {
                                mediaFile.VideoCodec = stream.TryGetProperty("codec_name", out var cn) ? cn.GetString() : "unknown";
                            }

                            if (stream.TryGetProperty("side_data_list", out var sideDataList))
                            {
                                foreach (var sideData in sideDataList.EnumerateArray())
                                {
                                    if (sideData.TryGetProperty("side_data_type", out var dataType) && dataType.GetString() == "DOVI configuration record")
                                    {
                                        var profile = sideData.TryGetProperty("dv_profile", out var dvProfile) ? dvProfile.GetInt32() : -1;
                                        var compatId = sideData.TryGetProperty("dv_bl_signal_compatibility_id", out var dvCompatId) ? dvCompatId.GetInt32() : -1;
                                        mediaFile.DolbyVisionProfile = (profile == 8 && compatId == 1) ? "8.1" : profile.ToString();
                                    }
                                }
                            }
                        }
                        else if (stream.TryGetProperty("codec_type", out codecType) && codecType.GetString() == "subtitle")
                        {
                            string codecName = stream.TryGetProperty("codec_name", out var cn) ? cn.GetString() : "unknown";

                            // NEW: Actively detect if the stream is empty
                            bool isEmpty = false;

                            // 1. Check if the codec is completely unknown/missing
                            if (codecName == "unknown")
                            {
                                isEmpty = true;
                            }

                            // 2. Check MKV tags for zero frames or zero bytes
                            if (stream.TryGetProperty("tags", out var tags))
                            {
                                // check the number frames, to detect forced frames and mark them as empty
                                tags.TryGetProperty("NUMBER_OF_FRAMES", out var framesElement);

                                int.TryParse(framesElement.GetString(), out int frames);

                                if (frames < 50)
                                    isEmpty = true;
                                else
                                {
                                    // check the byte size, streams smaller than 1kb are considerd garbage

                                    tags.TryGetProperty("NUMBER_OF_BYTES", out var bytesElement);

                                    // parse to long
                                    long.TryParse(bytesElement.GetString(), out long byteSize);

                                    if (byteSize < 1024)
                                        isEmpty = true;
                                }


                            }

                            // Sort the stream based on our findings
                            if (Array.Exists(bitmapCodecs, c => c == codecName))
                            {
                                mediaFile.HasBitmapSubs = true;
                            }
                            else if (!isEmpty) // ONLY add the index if it actually contains data
                            {
                                mediaFile.HasTextSubs = true;
                                mediaFile.ValidSubtitleIndices.Add(subtitleStreamIndex);
                            }

                            // Always increment the index so it matches FFmpeg's stream order perfectly
                            subtitleStreamIndex++;
                        }
                        // NEW: Evaluate ALL audio streams
                        else if (stream.TryGetProperty("codec_type", out codecType) && codecType.GetString() == "audio")
                        {
                            string codecName = stream.TryGetProperty("codec_name", out var cn) ? cn.GetString() : "unknown";

                            // Add the codec to our list if we haven't seen it yet
                            if (!foundAudioCodecs.Contains(codecName, StringComparer.OrdinalIgnoreCase))
                            {
                                foundAudioCodecs.Add(codecName);
                            }

                            // Track the maximum number of channels found
                            int channels = stream.TryGetProperty("channels", out var ch) ? ch.GetInt32() : 2;
                            if (channels > maxAudioChannels)
                            {
                                maxAudioChannels = channels;
                            }

                            // If any track is unsupported, flag the file for conversion
                            if (!Array.Exists(supportedAudioCodecs, c => string.Equals(c, codecName, StringComparison.OrdinalIgnoreCase)))
                            {
                                requiresAudioConversion = true;
                            }
                        }
                    }

                    // After scanning all streams, apply the findings to the MediaFile
                    if (foundAudioCodecs.Count > 0)
                    {
                        // Joins them cleanly, e.g. "ac3, dts"
                        mediaFile.OriginalAudioCodec = string.Join(", ", foundAudioCodecs).ToUpper();
                        mediaFile.AudioChannels = maxAudioChannels;
                        mediaFile.RequiresAudioConversion = requiresAudioConversion;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error analyzing {filePath}: {ex.Message}");
                mediaFile.DolbyVisionProfile = "Error";
            }

            if (string.IsNullOrEmpty(mediaFile.DolbyVisionProfile)) mediaFile.DolbyVisionProfile = "No";
            return mediaFile;
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

            // (Keep your existing smart audio bitrate logic here)
            string audioBitrate = "224k";
            if (File.AudioChannels >= 6)
            {
                audioBitrate = "640k";
            }

            string audioFlag = File.RequiresAudioConversion ? $"ac3 -b:a {audioBitrate}" : "copy";

            // Conditionally apply the hvc1 tag if the video is HEVC
            string videoTag = (File.VideoCodec != null && File.VideoCodec.Equals("hevc", StringComparison.OrdinalIgnoreCase)) ? "-tag:v hvc1" : "";

            /*
            string commandFlags = File.HasBitmapSubs
                ? $"-map 0:v? -map 0:a? -c:v {videoTag} copy -c:a {audioFlag}"
                : $"-map 0:v? -map 0:a? -map 0:s? -c:v copy {videoTag} -c:a {audioFlag} -c:s mov_text";

            string arguments = $"-y -fflags +genpts -i \"{File.FilePath}\" -hide_banner -loglevel warning -strict experimental {commandFlags} -map_metadata -1 -dn -map_chapters -1 -movflags +faststart -use_editlist 0 -video_track_timescale 90000 -avoid_negative_ts make_zero -strict -2 \"{outputFile}\"";
            */


            // NEW: Build the internal subtitle mapping dynamically
            string subMapArgs = "";
            string subCodecArgs = "";

            // Only map text subtitles if there are no incompatible bitmap subtitles blocking the process
            if (File.HasTextSubs && !File.HasBitmapSubs)
            {
                // Add a -map flag for every VALID subtitle index, ignoring empty ones
                foreach (int subIndex in File.ValidSubtitleIndices)
                {
                    subMapArgs += $" -map 0:s:{subIndex}";
                }
                // Tell FFmpeg to convert these explicitly mapped streams to MP4 text
                subCodecArgs = "-c:s mov_text";
            }

            // COMBINED ARGUMENTS:
            // We use -map 0:v:0 to grab ONLY the primary video track, stripping out embedded cover art thumbnails.
            // We inject {subMapArgs} to embed only the valid text tracks.
            string arguments = $"-y -fflags +genpts -i \"{File.FilePath}\" -hide_banner -loglevel warning -strict experimental -map 0:v:0 -map 0:a?{subMapArgs} -c:v copy {videoTag} -c:a {audioFlag} {subCodecArgs} -map_metadata -1 -dn -map_chapters -1 -movflags +faststart -use_editlist 0 -video_track_timescale 90000 -avoid_negative_ts make_zero -strict -2 \"{outputFile}\"";

            return await ExecuteFfmpegWithPolling(arguments, outputFile, "Converting...");
        }

        private async Task<ConversionResult> ExecuteFfmpegWithPolling(string arguments, string outputFile, string initialStatusText)
        {
            string ffmpegPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg", "ffmpeg.exe");

            var processStartInfo = new ProcessStartInfo(ffmpegPath, arguments)
            {
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                _process = Process.Start(processStartInfo);
                var errorTask = _process.StandardError.ReadToEndAsync();

                while (!_process.HasExited)
                {
                    if (_cts.Token.IsCancellationRequested)
                    {
                        _process.Kill(true);
                        return new ConversionResult { Success = false, ErrorMessage = "Cancelled" };
                    }
                    try
                    {
                        var outputInfo = new FileInfo(outputFile);
                        if (outputInfo.Exists && File.FileSizeBytes > 0)
                        {
                            int percent = (int)((double)outputInfo.Length / File.FileSizeBytes * 100);
                            bool isFinalizing = percent >= 99;
                            ProgressUpdated?.Invoke(Math.Min(100, percent), isFinalizing, isFinalizing ? "Finalizing..." : $"{initialStatusText} {percent}%");
                        }
                    }
                    catch { /* Ignore */ }
                    await Task.Delay(1000);
                }

                string errorOutput = await errorTask;
                return new ConversionResult { Success = _process.ExitCode == 0, ErrorMessage = _process.ExitCode == 0 ? "" : errorOutput };
            }
            catch (Exception ex) { return new ConversionResult { Success = false, ErrorMessage = ex.Message }; }
        }
    }
}

