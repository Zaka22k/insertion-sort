using InsertionSort.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace InsertionSort;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    public MainPage()
    {
        InitializeComponent();

        ViewModel = new MainViewModel();
        DataContext = ViewModel;
    }
}
