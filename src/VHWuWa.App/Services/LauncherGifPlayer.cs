using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace VHWuWa.App.Services;

/// <summary>
/// Plays each actual GIF frame from a compiled local WPF resource.
/// Starts only while the Image is visible and loaded; navigation pauses the timer.
/// No game processes or files are touched.
/// </summary>
internal sealed class LauncherGifPlayer : IDisposable
{
    private readonly Image _target;
    private readonly DispatcherTimer _timer = new(DispatcherPriority.Background);
    private readonly IReadOnlyList<BitmapFrame> _frames;
    private readonly IReadOnlyList<TimeSpan> _durations;
    private int _frameIndex;
    private bool _disposed;

    internal LauncherGifPlayer(Image target, Uri resourceUri)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
        if (resourceUri is null) throw new ArgumentNullException(nameof(resourceUri));

        var resource = Application.GetResourceStream(resourceUri)
            ?? throw new InvalidOperationException($"GIF resource not found: {resourceUri}");

        using var stream = resource.Stream;
        var decoder = new GifBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frames = new List<BitmapFrame>();
        var durations = new List<TimeSpan>();
        foreach (var frame in decoder.Frames)
        {
            // GIF graphic-control delay is measured in hundredths of a second.
            int delayHundredths = 3;
            try
            {
                if (frame.Metadata is BitmapMetadata metadata && metadata.ContainsQuery("/grctlext/Delay"))
                    delayHundredths = Convert.ToInt32(metadata.GetQuery("/grctlext/Delay"));
            }
            catch (Exception)
            {
                delayHundredths = 3;
            }
            if (frame.CanFreeze && !frame.IsFrozen) frame.Freeze();
            frames.Add(frame);
            durations.Add(TimeSpan.FromMilliseconds(Math.Clamp(delayHundredths * 10, 20, 1000)));
        }
        if (frames.Count == 0) throw new InvalidOperationException("The GIF contains no frames.");

        _frames = frames;
        _durations = durations;
        _target.Source = frames[0];
        _timer.Interval = _durations[0];
        _timer.Tick += OnTick;
        _target.Loaded += OnLoaded;
        _target.Unloaded += OnUnloaded;
        _target.IsVisibleChanged += OnVisibilityChanged;
        UpdateRunningState();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => UpdateRunningState();
    private void OnUnloaded(object sender, RoutedEventArgs e) => UpdateRunningState();
    private void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e) => UpdateRunningState();

    private void UpdateRunningState()
    {
        if (_disposed) return;
        if (_target.IsLoaded && _target.IsVisible && _frames.Count > 1)
        {
            if (!_timer.IsEnabled) _timer.Start();
        }
        else
        {
            _timer.Stop();
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_disposed) return;
        _frameIndex = (_frameIndex + 1) % _frames.Count;
        _target.Source = _frames[_frameIndex];
        _timer.Interval = _durations[_frameIndex];
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTick;
        _target.Loaded -= OnLoaded;
        _target.Unloaded -= OnUnloaded;
        _target.IsVisibleChanged -= OnVisibilityChanged;
    }
}
