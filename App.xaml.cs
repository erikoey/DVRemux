using System.Windows;

namespace MKV_Converter
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Logger.Clear();
            base.OnStartup(e);

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}

