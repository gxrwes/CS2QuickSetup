using System.Windows;
using ConfigCreator.ViewModels;

namespace ConfigCreator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel(); // ✅ Ensure ViewModel is set
        }
    }
}
