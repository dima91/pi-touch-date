using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace PiTouchDate.Controls;

public partial class Overlay : UserControl
{
    // Optional width
    public static readonly StyledProperty<double> ContentWidthProperty =
        AvaloniaProperty.Register<Overlay, double>(nameof(ContentWidth), double.NaN);

    public double ContentWidth
    {
        get => GetValue(ContentWidthProperty);
        set => SetValue(ContentWidthProperty, value);
    }

    // Optional height
    public static readonly StyledProperty<double> ContentHeightProperty =
        AvaloniaProperty.Register<Overlay, double>(nameof(ContentHeight), double.NaN);

    public double ContentHeight
    {
        get => GetValue(ContentHeightProperty);
        set => SetValue(ContentHeightProperty, value);
    }

    // Close command
    public static readonly StyledProperty<ICommand?> CloseCommandProperty =
        AvaloniaProperty.Register<Overlay, ICommand?>(nameof(CloseCommand));

    public ICommand? CloseCommand
    {
        get => GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }


    // Overlay icon
    public static readonly StyledProperty<object?> IconProperty =
    AvaloniaProperty.Register<Overlay, object?>(nameof(Icon));

    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
}


    // Overlay title
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<Overlay, string>(nameof(Title), string.Empty);

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }


    public Overlay()
    {
        InitializeComponent();

        Background = new SolidColorBrush(Color.FromArgb(230, 20, 28, 72));
        BorderBrush = Brushes.Black;
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(12);
        Padding = new Thickness(10);
    }
}