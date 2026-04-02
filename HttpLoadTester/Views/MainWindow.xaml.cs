using System.Windows;
using HttpLoadTester.ViewModels;

namespace HttpLoadTester.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
