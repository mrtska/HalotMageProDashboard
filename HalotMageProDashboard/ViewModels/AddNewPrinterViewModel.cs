using HalotMageProDashboard.Models;
using Livet;
using Livet.Messaging.Windows;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;

namespace HalotMageProDashboard.ViewModels {
    /// <summary>
    /// プリンターを新規追加する画面のViewModel
    /// </summary>
    public class AddNewPrinterViewModel(PrinterManager PrinterManager) : ViewModel {

        private bool _IsActive;
        /// <summary>
        /// 検索中の場合
        /// </summary>
        public bool IsActive {
            get { return _IsActive; }
            set {
                if (_IsActive == value)
                    return;
                _IsActive = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<NetworkInterface> _DetectedNetworkInterfaces = [];
        /// <summary>
        /// 検知したNIC
        /// </summary>
        public ObservableCollection<NetworkInterface> DetectedNetworkInterfaces {
            get { return _DetectedNetworkInterfaces; }
            set {
                if (_DetectedNetworkInterfaces == value)
                    return;
                _DetectedNetworkInterfaces = value;
                RaisePropertyChanged();
            }
        }

        private NetworkInterface _SelectedNetworkInterface = default!;
        /// <summary>
        /// 選択されたNIC
        /// </summary>
        public NetworkInterface SelectedNetworkInterface {
            get { return _SelectedNetworkInterface; }
            set {
                if (_SelectedNetworkInterface == value || value == null)
                    return;
                _SelectedNetworkInterface = value;
                RaisePropertyChanged();
                Refresh();
            }
        }

        private ObservableCollection<DetectedPrinter> _DetectedPrinters = [];

        public ObservableCollection<DetectedPrinter> DetectedPrinters {
            get { return _DetectedPrinters; }
            set {
                if (_DetectedPrinters == value)
                    return;
                _DetectedPrinters = value;
                RaisePropertyChanged();
            }
        }

        private object? _SelectedPrinters;

        public object? SelectedPrinters {
            get { return _SelectedPrinters; }
            set {
                if (_SelectedPrinters == value)
                    return;
                _SelectedPrinters = value;
                RaisePropertyChanged();
            }
        }

        public void Initialized() {
            var networkInterfaces = PrinterManager.GetNetworkInterfaces();
            DetectedNetworkInterfaces = new ObservableCollection<NetworkInterface>(networkInterfaces);
        }

        private CancellationTokenSource? CancellationTokenSource;

        public async void Refresh() {
            if (CancellationTokenSource != null) {
                CancellationTokenSource.Cancel();
            }
            CancellationTokenSource = new CancellationTokenSource();

            IsActive = true;
            var detectedPrinters = PrinterManager.SearchPrinters(SelectedNetworkInterface, CancellationTokenSource.Token);
            if (detectedPrinters == null) {
                IsActive = false;
                return;
            }

            await foreach (var printer in detectedPrinters) {
                DetectedPrinters.Add(printer);
            }

            IsActive = false;
        }

        public void AddSelected() {
            CancellationTokenSource?.Cancel();
            CancellationTokenSource = null;

            Messenger.Raise(new WindowActionMessage(WindowAction.Close));

            if (SelectedPrinters is DetectedPrinter printer) {
                PrinterManager.Add(printer);
            } else if (SelectedPrinters is IEnumerable<DetectedPrinter> printers) {
                foreach (var p in printers) {
                    PrinterManager.Add(p);
                }
            }
        }
    }
}
