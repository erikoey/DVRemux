using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MKV_Converter
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<MediaFile> QueuedFiles { get; set; }
        public ObservableCollection<MediaFile> HistoryFiles { get; set; }
        private string _outputFolder = string.Empty;
        private QueueManager _queueManager;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            QueuedFiles = new ObservableCollection<MediaFile>();
            HistoryFiles = new ObservableCollection<MediaFile>();
            QueueGrid.ItemsSource = QueuedFiles;
            HistoryGrid.ItemsSource = HistoryFiles;
        }

        private async void SelectFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new CommonOpenFileDialog { IsFolderPicker = false, Multiselect = true };
                dialog.Filters.Add(new CommonFileDialogFilter("MKV Files", "*.mkv"));
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    await AddFilesAndFolders(dialog.FileNames.ToArray());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred while selecting files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            var dataGrid = GetFocusedDataGrid();
            if (dataGrid == null) return;
            var itemsToRemove = dataGrid.SelectedItems.Cast<MediaFile>().ToList();
            var collection = dataGrid.ItemsSource as ObservableCollection<MediaFile>;

            if (collection != null)
            {
                foreach (var item in itemsToRemove)
                {
                    if (collection == QueuedFiles && item.Status != "Processing") { collection.Remove(item); }
                    else if (collection == HistoryFiles) { collection.Remove(item); }
                }
            }
        }

        private void SelectOutput_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog { IsFolderPicker = true };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                _outputFolder = dialog.FileName;
                OutputFolderLabel.Text = $"Output to: {_outputFolder}";
                OpenFolderButton.IsEnabled = false;
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_outputFolder) && Directory.Exists(_outputFolder))
            {
                Process.Start("explorer.exe", _outputFolder);
            }
            else
            {
                MessageBox.Show("The specified output folder does not exist.", "Folder Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ContextOpenSourceFolder_Click(object sender, RoutedEventArgs e)
        {
            var selectedFile = GetFileFromContextMenu(sender);
            if (selectedFile != null)
            {
                string directory = Path.GetDirectoryName(selectedFile.FilePath);
                if (Directory.Exists(directory)) { Process.Start("explorer.exe", directory); }
                else { MessageBox.Show($"The source folder could not be found:\n{directory}", "Folder Not Found", MessageBoxButton.OK, MessageBoxImage.Warning); }
            }
        }

        private void ContextOpenOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            var selectedFile = GetFileFromContextMenu(sender);
            if (selectedFile != null)
            {
                string outputDir = !string.IsNullOrEmpty(_outputFolder) ? _outputFolder : Path.GetDirectoryName(selectedFile.FilePath);
                if (Directory.Exists(outputDir)) { Process.Start("explorer.exe", outputDir); }
                else { MessageBox.Show($"The output folder could not be found:\n{outputDir}", "Folder Not Found", MessageBoxButton.OK, MessageBoxImage.Warning); }
            }
        }

        private void ContextRemoveItems_Click(object sender, RoutedEventArgs e)
        {
            var dataGrid = GetDataGridFromContextMenu(sender);
            if (dataGrid == null) return;
            var itemsToRemove = dataGrid.SelectedItems.Cast<MediaFile>().ToList();
            var collection = dataGrid.ItemsSource as ObservableCollection<MediaFile>;

            if (collection != null)
            {
                foreach (var item in itemsToRemove)
                {
                    if (collection == QueuedFiles && item.Status != "Processing") { collection.Remove(item); }
                    else if (collection == HistoryFiles) { collection.Remove(item); }
                }
            }
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            if (QueuedFiles.Any() || HistoryFiles.Any())
            {
                var result = MessageBox.Show("Are you sure you want to clear both the Queue and History lists?", "Confirm Clear", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    var pendingFiles = QueuedFiles.Where(f => f.Status != "Processing").ToList();
                    foreach (var file in pendingFiles) { QueuedFiles.Remove(file); }
                    HistoryFiles.Clear();
                    OpenFolderButton.IsEnabled = false;
                }
            }
        }

        private void StartConversion_Click(object sender, RoutedEventArgs e)
        {
            if (!QueuedFiles.Any(f => f.Status == "Pending"))
            {
                MessageBox.Show("There are no pending files in the queue to convert.", "No Files", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetUiState(true);

            _queueManager = new QueueManager(QueuedFiles, HistoryFiles, _outputFolder);
            _queueManager.FileProcessingStarted += OnFileProcessingStarted;
            _queueManager.FileFinished += OnFileFinished;
            _queueManager.BatchFinished += OnBatchFinished;
            _queueManager.Start();
        }

        private void OnFileProcessingStarted(MediaFile file)
        {
            Dispatcher.Invoke(() =>
            {
                QueuedFiles.Move(QueuedFiles.IndexOf(file), 0);
                file.Status = "Processing";
                StatusLabel.Text = $"Converting {file.FileName}...";
            });
        }

        private void OnFileFinished(MediaFile file, ConversionResult result)
        {
            Dispatcher.Invoke(() =>
            {
                if (result.ErrorMessage == "Cancelled")
                {
                    file.Status = "Cancelled";
                }
                else
                {
                    file.Status = result.Success ? "Success" : "Failed";
                    file.ErrorMessage = result.ErrorMessage;
                }
                QueuedFiles.Remove(file);
                HistoryFiles.Insert(0, file);
            });
        }

        private void OnBatchFinished(bool wasCancelled)
        {
            Dispatcher.Invoke(() =>
            {
                SetUiState(false);
                if (wasCancelled)
                {
                    MessageBox.Show("The conversion was cancelled.", "Cancelled", MessageBoxButton.OK, MessageBoxImage.Warning);
                    StatusLabel.Text = "Conversion cancelled.";
                    var processingFile = HistoryFiles.FirstOrDefault(f => f.Status == "In Progress...");
                    if (processingFile != null) { processingFile.Status = "Cancelled"; }
                }
                else
                {
                    MessageBox.Show("All files in the queue have been processed.", "Queue Finished", MessageBoxButton.OK, MessageBoxImage.Information);
                    StatusLabel.Text = "Queue processing complete.";
                }
            });
        }

        private void CancelConversion_Click(object sender, RoutedEventArgs e)
        {
            if (_queueManager != null)
            {
                StatusLabel.Text = "Cancellation requested...";
                CancelButton.IsEnabled = false;
                _queueManager.Cancel();
            }
        }

        private void Donate_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.buymeacoffee.com/nice_erik") { UseShellExecute = true });
        }

        private void FileGrid_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) { e.Effects = DragDropEffects.Copy; }
            else { e.Effects = DragDropEffects.None; }
            e.Handled = true;
        }

        private async void FileGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                try { await AddFilesAndFolders((string[])e.Data.GetData(DataFormats.FileDrop)); }
                catch (Exception ex) { MessageBox.Show($"An error occurred processing dropped files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private void FileGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;
            if (grid != null && grid.SelectedItem is MediaFile selectedFile && !string.IsNullOrEmpty(selectedFile.ErrorMessage))
            {
                MessageBox.Show(selectedFile.ErrorMessage, "FFmpeg Error Details", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FileGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void DataGridRow_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && !row.IsSelected)
            {
                row.IsSelected = true;
            }
        }

        private void FileGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                RemoveSelected_Click(sender, null);
                e.Handled = true;
            }
        }

        private async Task AddFilesAndFolders(string[] paths)
        {
            SetUiState(true, isAnalyzing: true);
            StatusLabel.Text = "Analyzing files...";
            try
            {
                foreach (var path in paths)
                {
                    if (Directory.Exists(path))
                    {
                        foreach (var file in Directory.EnumerateFiles(path, "*.mkv", SearchOption.AllDirectories))
                        {
                            if (!QueuedFiles.Any(f => f.FilePath == file) && !HistoryFiles.Any(f => f.FilePath == file))
                            {
                                var mediaFile = await AnalysisWorker.AnalyzeFileAsync(file);
                                QueuedFiles.Add(mediaFile);
                            }
                        }
                    }
                    else if (File.Exists(path) && path.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!QueuedFiles.Any(f => f.FilePath == path) && !HistoryFiles.Any(f => f.FilePath == path))
                        {
                            var mediaFile = await AnalysisWorker.AnalyzeFileAsync(path);
                            QueuedFiles.Add(mediaFile);
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"An error occurred during file analysis: {ex.Message}", "Analysis Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            finally
            {
                SetUiState(false);
                StatusLabel.Text = "Analysis complete.";
            }
        }

        private void SetUiState(bool isBusy, bool isAnalyzing = false)
        {
            bool isConverting = isBusy && !isAnalyzing;

            SelectFilesButton.IsEnabled = !isBusy;
            RemoveSelectedButton.IsEnabled = !isConverting;
            ClearListButton.IsEnabled = !isConverting;
            SelectOutputButton.IsEnabled = !isBusy;
            StartButton.IsEnabled = !isBusy;

            CancelButton.IsEnabled = isConverting;

            if (!isBusy && !string.IsNullOrEmpty(_outputFolder))
            {
                OpenFolderButton.IsEnabled = true;
            }
            else
            {
                OpenFolderButton.IsEnabled = false;
            }
        }

        private MediaFile GetFileFromContextMenu(object sender)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var dataGrid = contextMenu?.PlacementTarget as DataGrid;
            return dataGrid?.SelectedItem as MediaFile;
        }

        private DataGrid GetDataGridFromContextMenu(object sender)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            return contextMenu?.PlacementTarget as DataGrid;
        }

        private DataGrid GetFocusedDataGrid()
        {
            if (MainTabControl.SelectedItem == QueueTab) return QueueGrid;
            if (MainTabControl.SelectedItem == HistoryTab) return HistoryGrid;
            return QueueGrid; // Default
        }
    }
}

