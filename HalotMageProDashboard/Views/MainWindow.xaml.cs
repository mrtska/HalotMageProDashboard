using HalotMageProDashboard.ViewModels;
using MetroRadiance.UI.Controls;

namespace HalotMageProDashboard.Views {
    public partial class MainWindow : MetroWindow {
        public MainWindow(MainWindowViewModel vm) {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
