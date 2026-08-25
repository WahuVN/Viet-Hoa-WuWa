using System.Windows.Controls;
using System.Windows;
using VHWuWa.App.ViewModels;

namespace VHWuWa.App.Views;

public partial class FontPage : UserControl, IPageView
{
    private readonly FontViewModel _vm;
    public FontPage(FontViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
    }
    public void OnNavigated() => _vm.OnActivated();

    private void OpenLargePreview_Click(object sender, RoutedEventArgs e)
    {
        var window = new FontPreviewWindow(_vm) { Owner = Window.GetWindow(this) };
        window.ShowDialog();
    }
}
