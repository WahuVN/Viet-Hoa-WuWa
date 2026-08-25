using System.Windows;
using VHWuWa.App.ViewModels;

namespace VHWuWa.App.Views;

public partial class FontPreviewWindow : Window
{
    public FontPreviewWindow(FontViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
