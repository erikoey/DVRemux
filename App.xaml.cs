using System.Windows;

namespace MKV_Converter
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Logger.Clear();
            base.OnStartup(e);

            if (!FfmpegSetupDialog.CheckFfmpeg())
            {
                var setupDialog = new FfmpegSetupDialog();
                setupDialog.ShowDialog();

                if (!FfmpegSetupDialog.CheckFfmpeg())
                {
                    MessageBox.Show("FFmpeg is still not found. The application will now exit.", "FFmpeg Missing", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown();
                    return;
                }
            }

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}

