using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace PiTouchDate.Controls;

public partial class Spinner : UserControl
{
    private DispatcherTimer? _timer;
    private RotateTransform? _transform;
    private Stopwatch? _stopwatch;

    public Spinner()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var ring = this.FindControl<Border>("PART_Ring");
        if (ring == null) return;

        _transform = new RotateTransform(0);
        ring.RenderTransformOrigin = RelativePoint.Center;
        ring.RenderTransform = _transform;

        _stopwatch = Stopwatch.StartNew();
        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        _timer?.Stop();
        _timer = null;
        _stopwatch = null;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_transform == null || _stopwatch == null) return;
        _transform.Angle = _stopwatch.Elapsed.TotalSeconds * 360.0 % 360.0;
    }
}
