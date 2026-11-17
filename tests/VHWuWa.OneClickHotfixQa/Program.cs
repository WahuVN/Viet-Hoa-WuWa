using System.Reflection;
using System.Runtime.CompilerServices;
using VHWuWa.App.ViewModels;

// UI-only regression: no real game and no installer execution.
var vm = (InstallViewModel)RuntimeHelpers.GetUninitializedObject(typeof(InstallViewModel));
void Set(string name, object value)
{
    var field = typeof(InstallViewModel).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new Exception("Missing field " + name);
    field.SetValue(vm, value);
}
void Require(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
Set("_r29Mode", true);
Set("_r29InstallReady", true);
Set("_r29Installed", false);
vm.VariantEnglish = true;
vm.VariantHanViet = true;
vm.ApplyFont = true;
Require(vm.VariantHanViet && !vm.VariantEnglish, "Han Viet radio was not selected");
Require(vm.ApplyFont, "Font checkbox could not be checked");
Require(vm.CanInstall && vm.InstallCommand.CanExecute(null),
    "OneClick button incorrectly disabled for selected R29 Han Viet/font");
Require(vm.OptionHelpText.Contains("xung đột", StringComparison.OrdinalIgnoreCase)
        && vm.OptionHelpText.Contains("cài đè", StringComparison.OrdinalIgnoreCase),
    "Missing force-install cleanup message");
Console.WriteLine("PASS ONECLICK_R29_BOTH_OPTIONS_SELECTED_STILL_INSTALLABLE");
vm.ApplyFont = false;
Require(vm.CanInstall && vm.InstallCommand.CanExecute(null), "Unchecking font disabled OneClick");
vm.VariantEnglish = true;
Require(vm.CanInstall && vm.InstallCommand.CanExecute(null), "Returning to English disabled OneClick");
Console.WriteLine("PASS ONECLICK_R29_OPTION_SWITCHES_STAY_ENABLED");
Set("_r29InstallReady", false);
vm.ApplyFont = true;
Require(!vm.CanInstall && !vm.InstallCommand.CanExecute(null),
    "Invalid game path incorrectly allowed OneClick");
Console.WriteLine("PASS ONECLICK_INVALID_GAME_STILL_BLOCKED");
