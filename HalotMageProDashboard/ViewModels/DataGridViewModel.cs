using HalotMageProDashboard.Models;
using Livet;
using Livet.Messaging;
using System.Collections.ObjectModel;
using System.Windows;

namespace HalotMageProDashboard.ViewModels {
    /// <summary>
    /// DataGrid部分を制御するViewModel
    /// </summary>
    public class DataGridViewModel : ViewModel {

        private ObservableCollection<Printer> _Printers = [];
        /// <summary>
        /// 制御対象のプリンター一覧
        /// </summary>
        public ObservableCollection<Printer> Printers {
            get { return _Printers; }
            set {
                if (_Printers == value)
                    return;
                _Printers = value;
                RaisePropertyChanged();
            }
        }

        private readonly PrinterManager PrinterManager;
        private readonly Timer PeriodicRefreshTimer;

        public DataGridViewModel(PrinterManager printerManager) {
            PrinterManager = printerManager;

            PeriodicRefreshTimer = new Timer(GetStatus, null, 0, 5000);
            CompositeDisposable.Add(PeriodicRefreshTimer);
        }

        private void GetStatus(object? _) {
            try {
                foreach (var printer in Printers) {
                    printer.Refresh();
                }
            } catch (Exception ex) {
                MessageBox.Show(ex.Message + "\n\n" + ex.StackTrace, "エラーが発生しました");
            }
        }

        public async void Refresh() {
            // リフレッシュする前に既に接続されているプリンターを切断する
            foreach (var printer in Printers) {
                printer.Dispose();
            }

            var printers = await PrinterManager.GetAvailablePrintersAsync();
            Printers = new ObservableCollection<Printer>(printers);

            foreach (var printer in Printers) {
                printer.Connect();
            }
        }

        public void SetPassword(Printer printer) {

            var vm = new SetPasswordViewModel(printer.GetPassword());
            Messenger.Raise(new TransitionMessage(typeof(Views.SetPasswordDialog), vm, TransitionMode.Modal));

            if (vm.DoSave) {
                printer.SetPassword(vm.Password);
            }
        }

        public void RemovePrinter(Printer printer) {
            PrinterManager.Remove(printer);
            Refresh();
        }
    }
}
