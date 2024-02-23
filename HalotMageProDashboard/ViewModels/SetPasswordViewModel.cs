using Livet;
using Livet.Messaging.Windows;

namespace HalotMageProDashboard.ViewModels {
    /// <summary>
    /// プリンターを新規追加する画面のViewModel
    /// </summary>
    public class SetPasswordViewModel(string _Password) : ViewModel {


        private string _Password = _Password;

        public string Password {
            get { return _Password; }
            set {
                if (_Password == value)
                    return;
                _Password = value;
                RaisePropertyChanged();
            }
        }

        public bool DoSave { get; private set; }

        public void Save() {
            DoSave = true;
            Messenger.Raise(new WindowActionMessage(WindowAction.Close));
        }
    }
}
