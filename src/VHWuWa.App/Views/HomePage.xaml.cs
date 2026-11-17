using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using VHWuWa.App.Services;
using VHWuWa.App.ViewModels;

namespace VHWuWa.App.Views;

/// <summary>
/// Presentation-only home screen. The original installer commands/view models are unchanged.
/// The supplied H.264 clip plays from Assets/bg-video.mp4 (a separate publish file).
/// </summary>
public partial class HomePage : UserControl, IPageView
{
    private readonly HomeViewModel _vm;
    private readonly InstallViewModel _installVm;
    private readonly LauncherGifPlayer _progressAnimation;
    private bool _videoSourceAssigned;
    private bool _firstNavigation = true;

    public HomePage(HomeViewModel vm, InstallViewModel installVm)
    {
        _vm = vm;
        _installVm = installVm;
        InitializeComponent();
        DataContext = vm;
        InstallPanel.DataContext = installVm;
        _progressAnimation = new LauncherGifPlayer(
            InstallProgressGif,
            new Uri("pack://application:,,,/Assets/launcher-progress.gif", UriKind.Absolute));

        // Do not create synthetic wings, particles or a pan/zoom animation.
        // The video is muted, local, passive and never blocks input.
        LauncherBackgroundVideo.MediaOpened += OnVideoOpened;
        LauncherBackgroundVideo.MediaEnded += OnVideoEnded;
        LauncherBackgroundVideo.MediaFailed += OnVideoFailed;
        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
        IsVisibleChanged += OnPageVisibleChanged;
        _installVm.InstallationStateChanged += (_, _) => _vm.OnActivated();
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e) => ResumeVideo();

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        // Pause, but keep the local decoder and frame ready for instant tab return.
        // Never run video in the background while another editor is visible.
        LauncherBackgroundVideo.Pause();
    }

    private void OnPageVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!IsLoaded)
            return;
        if (IsVisible)
            ResumeVideo();
        else
            LauncherBackgroundVideo.Pause();
    }

    private void ResumeVideo()
    {
        if (!IsLoaded || !IsVisible)
            return;

        if (!_videoSourceAssigned)
        {
            var videoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "bg-video.mp4");
            if (!File.Exists(videoPath))
            {
                Trace.TraceWarning($"WAHU UI: local video absent; retaining source-image fallback: {videoPath}");
                return;
            }

            LauncherBackgroundVideo.Source = new Uri(videoPath, UriKind.Absolute);
            _videoSourceAssigned = true;
        }

        LauncherBackgroundVideo.Play();
    }

    private void OnVideoOpened(object sender, RoutedEventArgs e)
    {
        LauncherBackgroundVideo.Opacity = 1;
    }

    private void OnVideoEnded(object sender, RoutedEventArgs e)
    {
        // Seamlessly loop the original supplied 12-second clip.
        LauncherBackgroundVideo.Position = TimeSpan.Zero;
        if (IsLoaded && IsVisible)
            LauncherBackgroundVideo.Play();
    }

    private void OnVideoFailed(object? sender, ExceptionRoutedEventArgs e)
    {
        LauncherBackgroundVideo.Opacity = 0;
        Trace.TraceWarning("WAHU UI: background video playback failed; original still fallback retained: " +
                           e.ErrorException?.Message);
    }

    private void InstallFromHome(object sender, RoutedEventArgs e)
    {
        // Keep 'Cài Việt hóa' visible at all times; never silently swap its label to a folder chooser.
        // If the client is missing, select the game first; installing requires a second explicit click.
        if (!_vm.PathOk)
        {
            ChooseGameFolderFromInstall(sender, e);
            return;
        }

        _installVm.OnActivated();
        if (_installVm.InstallCommand.CanExecute(null))
        {
            _installVm.InstallCommand.Execute(null);
        }
        else if (!_installVm.Busy)
        {
            MessageBox.Show(
                _installVm.Summary,
                "Cài Việt hóa",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void ChooseGameFolderFromInstall(object sender, RoutedEventArgs e)
    {
        if (_vm.ChooseFolderCommand.CanExecute(null))
            _vm.ChooseFolderCommand.Execute(null);
        // Refresh the installer controls after the real folder-picker command completes.
        _installVm.OnActivated();
    }

    public void OnNavigated()
    {
        // HomeViewModel's constructor already performs the initial discovery.
        // Only refresh its state on subsequent visits.
        if (_firstNavigation)
            _firstNavigation = false;
        else
            _vm.OnActivated();
        _installVm.OnActivated();
        ResumeVideo();
    }
}
