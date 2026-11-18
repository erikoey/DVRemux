using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace MKV_Converter
{
    public class AudioTrack
    {
        // Index among the file's audio streams (FFmpeg's 0:a:N)
        public int Index { get; set; }
        public string Codec { get; set; }
        public int Channels { get; set; }
        public bool RequiresConversion { get; set; }
    }

    public class MediaFile : INotifyPropertyChanged
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string FileSize { get; set; }
        public long FileSizeBytes { get; set; }

        // Video        
        public string DolbyVisionProfile { get; set; }

        public string VideoCodec { get; set; }

        // Subtitles

        public bool HasBitmapSubs { get; set; }
        public bool HasTextSubs { get; set; }
        public List<int> ValidSubtitleIndices { get; set; } = new List<int>();

        // Audio Information
        public string OriginalAudioCodec { get; set; }

        public int AudioChannels { get; set; } = 2; // Default to stereo

        public string AudioDescription
        {
            get
            {
                if (string.IsNullOrEmpty(OriginalAudioCodec)) return "Unknown";

                string channels = AudioChannels switch
                {
                    1 => "1.0 Mono",
                    2 => "2.0 Stereo",
                    6 => "5.1 Surround",
                    8 => "7.1 Surround",
                    _ => $"{AudioChannels} ch"
                };

                // OriginalAudioCodec is already formatted from the AnalysisWorker
                return $"{OriginalAudioCodec} ({channels})";
            }
        }

        public List<AudioTrack> AudioTracks { get; } = new List<AudioTrack>();

        public bool RequiresAudioConversion => AudioTracks.Any(t => t.RequiresConversion);

        // Container duration from ffprobe, used for time-based progress (0 = unknown)
        public double DurationSeconds { get; set; }

        // Profile 7 (UHD Blu-ray dual layer) is not played as Dolby Vision from MP4 by LG TVs
        public bool HasUnsupportedDolbyVisionProfile => DolbyVisionProfile == "7";

        public string ErrorMessage { get; set; }

        public string WarningTooltipText
        {
            get
            {
                string warnings = "";
                if (HasUnsupportedDolbyVisionProfile) warnings += "• Dolby Vision Profile 7 is not supported by LG TVs in MP4. It will likely play as HDR10 without Dolby Vision, or not at all.\n";
                if (HasBitmapSubs) warnings += "• Contains image-based subtitles that will be stripped.\n";
                if (RequiresAudioConversion)
                {
                    string codecs = string.Join(", ", AudioTracks.Where(t => t.RequiresConversion).Select(t => t.Codec.ToUpper()).Distinct());
                    warnings += $"• Unsupported audio ({codecs}) will be converted to AC3. Other audio tracks are copied as-is.\n";
                }
                return warnings.TrimEnd('\n');
            }
        }

        public bool ShowWarningIcon => HasBitmapSubs || RequiresAudioConversion || HasUnsupportedDolbyVisionProfile;

        private string _progressText = "Pending";
        public string ProgressText { get => _progressText; set { _progressText = value; OnPropertyChanged(); } }

        private int _progress = 0;
        public int Progress { get => _progress; set { _progress = value; OnPropertyChanged(); } }

        private bool _isIndeterminate = false;
        public bool IsIndeterminate { get => _isIndeterminate; set { _isIndeterminate = value; OnPropertyChanged(); } }

        private SolidColorBrush _rowBrush = Brushes.Transparent;
        public SolidColorBrush RowBrush { get => _rowBrush; set { _rowBrush = value; OnPropertyChanged(); } }

        private string _finalStatus;
        public string FinalStatus { get => _finalStatus; set { _finalStatus = value; OnPropertyChanged(); } }

        private string _status = "Pending";
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case "Processing": RowBrush = new SolidColorBrush(Color.FromRgb(255, 255, 204)); FinalStatus = "In Progress..."; break;
                    case "Success": RowBrush = new SolidColorBrush(Color.FromRgb(153, 255, 153)); FinalStatus = "Success"; break;
                    case "Failed": RowBrush = new SolidColorBrush(Color.FromRgb(255, 153, 153)); FinalStatus = "Failed"; break;
                    case "Cancelled": RowBrush = new SolidColorBrush(Color.FromRgb(255, 204, 153)); FinalStatus = "Cancelled"; break;
                    default: RowBrush = Brushes.Transparent; FinalStatus = "Pending"; break;
                }
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

