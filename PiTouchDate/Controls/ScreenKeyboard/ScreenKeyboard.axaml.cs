using System;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using ReactiveUI;

namespace PiTouchDate.Controls;

public class ScreenKeyPressEventArgs(RoutedEvent routedEvent, string key) : RoutedEventArgs(routedEvent)
{
    public string Key { get; } = key;
}

public partial class ScreenKeyboard : UserControl
{
    public static readonly StyledProperty<ReactiveCommand<string, Unit>?> KeyPressCommandProperty =
        AvaloniaProperty.Register<ScreenKeyboard, ReactiveCommand<string, Unit>?>(nameof(KeyPressCommand));

    public static readonly RoutedEvent<ScreenKeyPressEventArgs> KeyPressEvent =
        RoutedEvent.Register<ScreenKeyboard, ScreenKeyPressEventArgs>(nameof(KeyPress), RoutingStrategies.Bubble);

    public ReactiveCommand<string, Unit>? KeyPressCommand
    {
        get => GetValue(KeyPressCommandProperty);
        set => SetValue(KeyPressCommandProperty, value);
    }

    public event EventHandler<ScreenKeyPressEventArgs> KeyPress
    {
        add => AddHandler(KeyPressEvent, value);
        remove => RemoveHandler(KeyPressEvent, value);
    }

    private enum KeyboardMode { Lower, Upper, Symbols, Symbols2 }

    // label, action, column width multiplier
    private record struct KeyDef(string Label, string Action, double Width = 1.0);

    private static readonly KeyDef[][] LowerRows =
    [
        [K("q"), K("w"), K("e"), K("r"), K("t"), K("y"), K("u"), K("i"), K("o"), K("p")],
        [Gap(0.5), K("a"), K("s"), K("d"), K("f"), K("g"), K("h"), K("j"), K("k"), K("l"), Gap(0.5)],
        [Sp("⇧", "ModeUpper", 1.5), K("z"), K("x"), K("c"), K("v"), K("b"), K("n"), K("m"), Sp("⌫", "Backspace", 1.5)],
        [Sp("123", "ModeSymbols", 2.0), Sp("spazio", "Space", 7.0), Sp(".", ".", 1.0)],
    ];

    private static readonly KeyDef[][] UpperRows =
    [
        [K("Q"), K("W"), K("E"), K("R"), K("T"), K("Y"), K("U"), K("I"), K("O"), K("P")],
        [Gap(0.5), K("A"), K("S"), K("D"), K("F"), K("G"), K("H"), K("J"), K("K"), K("L"), Gap(0.5)],
        [Sp("⇩", "ModeLower", 1.5), K("Z"), K("X"), K("C"), K("V"), K("B"), K("N"), K("M"), Sp("⌫", "Backspace", 1.5)],
        [Sp("123", "ModeSymbols", 2.0), Sp("spazio", "Space", 7.0), Sp(".", ".", 1.0)],
    ];

    private static readonly KeyDef[][] SymbolRows =
    [
        [K("1"), K("2"), K("3"), K("4"), K("5"), K("6"), K("7"), K("8"), K("9"), K("0")],
        [K("!"), K("@"), K("#"), K("$"), K("%"), K("&"), K("*"), K("("), K(")"), K("-"), K("?")],
        [K("_"), K("="), K("+"), K("["), K("]"), K("{"), K("}"), K(";"), K(":"), K("'")],
        [Sp("abc", "ModeLower", 1.5), Sp("▶", "ModeSymbols2", 1.5), Sp("spazio", "Space", 5.0), Sp(".", ".", 0.75), Sp("⌫", "Backspace", 1.25)],
    ];

    private static readonly KeyDef[][] SymbolRows2 =
    [
        [K("^"), K("~"), K("`"), K("\""), K("<"), K(">"), K("/"), K("\\"), K("|"), K(",")],
        [K("."), K("€"), K("£"), K("¥"), K("°"), K("•"), K("–"), K("—"), K("×"), K("÷")],
        [K("±"), K("™"), K("©"), K("®"), K("←"), K("→"), K("↑"), K("↓"), K("§"), K("¶")],
        [Sp("abc", "ModeLower", 1.5), Sp("◀", "ModeSymbols", 1.5), Sp("spazio", "Space", 5.0), Sp("⌫", "Backspace", 2.0)],
    ];

    private static KeyDef K(string key) => new(key, key);
    private static KeyDef Sp(string label, string action, double width) => new(label, action, width);
    private static KeyDef Gap(double width) => new("", "Gap", width);

    private KeyboardMode _mode = KeyboardMode.Lower;
    private Grid? _container;

    public ScreenKeyboard()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _container = e.NameScope.Find<Grid>("PART_Container");
        BuildKeyboard();
    }

    private void BuildKeyboard()
    {
        if (_container is null) return;

        var rows = _mode switch
        {
            KeyboardMode.Upper => UpperRows,
            KeyboardMode.Symbols => SymbolRows,
            KeyboardMode.Symbols2 => SymbolRows2,
            _ => LowerRows,
        };

        _container.Children.Clear();
        _container.RowDefinitions.Clear();
        _container.RowSpacing = 4;

        for (int r = 0; r < rows.Length; r++)
        {
            _container.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
            var rowGrid = BuildRow(rows[r]);
            Grid.SetRow(rowGrid, r);
            _container.Children.Add(rowGrid);
        }
    }

    private Grid BuildRow(KeyDef[] keys)
    {
        var grid = new Grid { ColumnSpacing = 4 };

        int col = 0;
        foreach (var key in keys)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(key.Width, GridUnitType.Star));

            if (key.Action == "Gap")
            {
                col++;
                continue;
            }

            var button = CreateKeyButton(key);
            Grid.SetColumn(button, col);
            grid.Children.Add(button);
            col++;
        }

        return grid;
    }

    private Button CreateKeyButton(KeyDef key)
    {
        var button = new Button { Content = key.Label };
        button.Classes.Add("key");

        bool isSpecial = key.Action is "ModeUpper" or "ModeLower" or "ModeSymbols" or "ModeSymbols2"
                                    or "Backspace" or "Space" or "123" or "abc";

        if (isSpecial) button.Classes.Add("key-special");
        if (key.Action == "Space") button.Classes.Add("key-space");

        button.Click += (_, _) => HandleKeyPress(key.Action);
        return button;
    }

    private void HandleKeyPress(string action)
    {
        switch (action)
        {
            case "ModeUpper":    _mode = KeyboardMode.Upper;    BuildKeyboard(); return;
            case "ModeLower":    _mode = KeyboardMode.Lower;    BuildKeyboard(); return;
            case "ModeSymbols":  _mode = KeyboardMode.Symbols;  BuildKeyboard(); return;
            case "ModeSymbols2": _mode = KeyboardMode.Symbols2; BuildKeyboard(); return;
        }

        var key = action == "Space" ? " " : action;

        RaiseEvent(new ScreenKeyPressEventArgs(KeyPressEvent, key));
        KeyPressCommand?.Execute(key).Subscribe();
    }
}
