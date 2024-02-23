using HalotMageProDashboard.Views;
using Livet;
using Livet.Messaging;

namespace HalotMageProDashboard.ViewModels {
    /// <summary>
    /// ルートViewModel
    /// </summary>
    public class MainWindowViewModel(IServiceProvider ServiceProvider) : ViewModel {

        public DataGridViewModel GridViewModel { get; } = ActivatorUtilities.GetServiceOrCreateInstance<DataGridViewModel>(ServiceProvider);

        public void Initialized() {
            GridViewModel.Messenger = Messenger;
            GridViewModel.Refresh();
        }

        /// <summary>
        /// プリンターを追加するダイアログを開く
        /// </summary>
        public void OpenAddNewPrinterDialog() {
            Messenger.Raise(new TransitionMessage(typeof(AddNewPrinterDialog),
                ActivatorUtilities.GetServiceOrCreateInstance<AddNewPrinterViewModel>(ServiceProvider), TransitionMode.Modal));

            GridViewModel.Refresh();
        }
    }
}
