using System.Diagnostics;
using System.Windows;

namespace MKV_Converter
{
    public partial class FfmpegSetupDialog : Window
    {
        public FfmpegSetupDialog()
        {
            InitializeComponent();
        }

        public static bool CheckFfmpeg()
        {
            try
            {
                var processStartInfo = new ProcessStartInfo("ffmpeg", "-version")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(processStartInfo);
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            // Opens the direct download link for the essentials build
            Process.Start(new ProcessStartInfo("https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip") { UseShellExecute = true });
        }

        private void OpenEnvVarsButton_Click(object sender, RoutedEventArgs e)
        {
            // This command directly opens the Environment Variables editor window
            Process.Start("rundll32.exe", "sysdm.cpl,EditEnvironmentVariables");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

