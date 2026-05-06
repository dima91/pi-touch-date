using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;

namespace PiTouchDate.Controls;

[PseudoClasses(":pressed")]
public partial class Card : UserControl
{
    public static readonly StyledProperty<BoxShadows> BoxShadowProperty =
        AvaloniaProperty.Register<Card, BoxShadows>(nameof(BoxShadow));

    public BoxShadows BoxShadow
    {
        get => GetValue(BoxShadowProperty);
        set => SetValue(BoxShadowProperty, value);
    }

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<Card, ICommand?>(nameof(Command));

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<Card, object?>(nameof(CommandParameter));

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<Card, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    private EventHandler<RoutedEventArgs>? _clickHandlers;

    public event EventHandler<RoutedEventArgs>? Click
    {
        add
        {
            AddHandler(ClickEvent, value);
            _clickHandlers += value;
        }
        remove
        {
            RemoveHandler(ClickEvent, value);
            _clickHandlers -= value;
        }
    }

    public Card()
    {
        InitializeComponent();

        this.Background = new SolidColorBrush(Color.FromArgb(153, 30, 41, 100));
        this.BorderBrush = Brushes.Black;
        this.Margin = new Thickness(4);
        this.Padding = new Thickness(10);
        this.BorderThickness = new Thickness(1);
        this.CornerRadius = new CornerRadius(12);
    }

    private bool HasHandlers => Command != null || _clickHandlers != null;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        
        if (!HasHandlers) return;

        PseudoClasses.Set(":pressed", true);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        
        if (!HasHandlers) return;

        PseudoClasses.Set(":pressed", false);

        // Check if the release event happened inside the control
        var point = e.GetCurrentPoint(this);
        if (new Rect(Bounds.Size).Contains(point.Position))
        {
            RaiseEvent(new RoutedEventArgs(ClickEvent));
            
            if (Command?.CanExecute(CommandParameter) == true)
            {
                Command.Execute(CommandParameter);
            }
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        PseudoClasses.Set(":pressed", false);
    }
}