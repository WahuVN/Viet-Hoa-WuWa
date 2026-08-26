using System.Windows.Controls;
using VHWuWa.App.ViewModels;

namespace VHWuWa.App.Views;

public partial class UidEditorPage : UserControl, IPageView
{
    private readonly UidEditorViewModel _vm;

    public UidEditorPage(UidEditorViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
    }

    public async void OnNavigated() => await _vm.LoadAsync();
}
