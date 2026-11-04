using System.Windows;
using AVACC.App.ViewModels;

namespace AVACC.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
