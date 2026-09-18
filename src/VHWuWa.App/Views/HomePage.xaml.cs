using System.Windows.Controls;
using VHWuWa.App.ViewModels;

namespace VHWuWa.App.Views;

public partial class HomePage : UserControl, IPageView
{
    private readonly HomeViewModel _vm;
    private readonly InstallViewModel _installVm;

    public HomePage(HomeViewModel vm, InstallViewModel installVm)
    {
        _vm = vm;
        _installVm = installVm;
        InitializeComponent();
        DataContext = vm;
        InstallPanel.DataContext = installVm;
        _installVm.InstallationStateChanged += (_, _) => _vm.OnActivated();
    }

    public void OnNavigated()
    {
        _vm.OnActivated();
        _installVm.OnActivated();
    }
}
