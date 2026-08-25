using System.Windows.Controls;
using VHWuWa.App.ViewModels;

namespace VHWuWa.App.Views;

public partial class TermsEditorPage : UserControl, IPageView
{
    private readonly TermsEditorViewModel _vm;

    public TermsEditorPage(TermsEditorViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
    }

    public async void OnNavigated() => await _vm.LoadAsync();
}
