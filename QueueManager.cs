using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MKV_Converter
{
    public class QueueManager
    {
        public event Action<MediaFile> FileProcessingStarted;
        public event Action<MediaFile, ConversionResult> FileFinished;
        public event Action<bool> BatchFinished;

        private readonly ObservableCollection<MediaFile> _queuedFiles;
        private readonly ObservableCollection<MediaFile> _historyFiles;
        private readonly string _outputFolder;

        private bool _isCancellationRequested = false;

        public QueueManager(ObservableCollection<MediaFile> queuedFiles, ObservableCollection<MediaFile> historyFiles, string outputFolder)
        {
            _queuedFiles = queuedFiles;
            _historyFiles = historyFiles;
            _outputFolder = outputFolder;
        }

        public void Start()
        {
            ConversionWorker.ResetCancellation();
            _isCancellationRequested = false;
            Task.Run(() => ProcessQueueAsync());
        }

        public void Cancel()
        {
            _isCancellationRequested = true;
            ConversionWorker.CancelAll();
        }

        private async Task ProcessQueueAsync()
        {
            while (!_isCancellationRequested)
            {
                MediaFile fileToProcess = _queuedFiles.FirstOrDefault(f => f.Status == "Pending");

                if (fileToProcess == null)
                {
                    break;
                }

                FileProcessingStarted?.Invoke(fileToProcess);

                var worker = new ConversionWorker(fileToProcess, _outputFolder);
                worker.ProgressUpdated += (progress, isIndeterminate, text) =>
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        fileToProcess.Progress = progress;
                        fileToProcess.IsIndeterminate = isIndeterminate;
                        fileToProcess.ProgressText = text;
                    });
                };

                var result = await worker.RunConversionAsync();
                FileFinished?.Invoke(fileToProcess, result);

                if (_isCancellationRequested)
                {
                    break;
                }
            }
            BatchFinished?.Invoke(_isCancellationRequested);
        }
    }
}

