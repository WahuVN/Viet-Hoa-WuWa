using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using VHWuWa.App.ViewModels;
using VHWuWa.App.Views;
using Wpf.Ui.Controls;

namespace VHWuWa.App;

public partial class MainWindow : FluentWindow
{
    private readonly IServiceProvider _sp;

    public MainWindow(MainViewModel vm, IServiceProvider sp)
    {
        _sp = sp;
        InitializeComponent();
        DataContext = vm;
        Nav.SelectedIndex = 0; // mở Trang chủ
    }

    private void Nav_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Nav.SelectedItem is not ListBoxItem item || item.Tag is not string tag) return;
        object? page = tag switch
        {
            "home" => _sp.GetRequiredService<HomePage>(),
            "install" => _sp.GetRequiredService<InstallPage>(),
            "mod" => _sp.GetRequiredService<ModPage>(),
            "font" => _sp.GetRequiredService<FontPage>(),
            "graphics" => _sp.GetRequiredService<GraphicsPage>(),
            "guide" => _sp.GetRequiredService<GuidePage>(),
            "settings" => _sp.GetRequiredService<SettingsPage>(),
            _ => null
        };
        if (page is null) return;
        PageHost.Content = page;
        (page as IPageView)?.OnNavigated();
    }
}
