using System.Windows.Controls;
using VHWuWa.App.ViewModels;

namespace VHWuWa.App.Views;

public partial class CharacterNamesPage : UserControl, IPageView
{
    private readonly CharacterNamesViewModel _vm;

    public CharacterNamesPage(CharacterNamesViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
    }

    public async void OnNavigated() => await _vm.LoadAsync();
}
