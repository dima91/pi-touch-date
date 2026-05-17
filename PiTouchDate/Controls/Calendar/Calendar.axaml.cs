using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using ReactiveUI;

namespace PiTouchDate.Controls;

public partial class Calendar : UserControl
{
    public static readonly StyledProperty<DateTime> DisplayDateProperty =
        AvaloniaProperty.Register<Calendar, DateTime>(nameof(DisplayDate), DateTime.Today);

    public DateTime DisplayDate
    {
        get => GetValue(DisplayDateProperty);
        set => SetValue(DisplayDateProperty, value);
    }

    private string _headerText = "";
    public static readonly DirectProperty<Calendar, string> HeaderTextProperty =
        AvaloniaProperty.RegisterDirect<Calendar, string>(nameof(HeaderText), o => o._headerText);
    public string HeaderText => _headerText;

    public ObservableCollection<CalendarDay> Days { get; } = new();

    public ICommand PreviousMonthCommand { get; }
    public ICommand NextMonthCommand { get; }
    public ICommand GoToTodayCommand { get; }

    public Calendar()
    {
        PreviousMonthCommand = ReactiveCommand.Create(() => DisplayDate = DisplayDate.AddMonths(-1));
        NextMonthCommand = ReactiveCommand.Create(() => DisplayDate = DisplayDate.AddMonths(1));
        GoToTodayCommand = ReactiveCommand.Create(() => DisplayDate = DateTime.Today);

        InitializeComponent();
        _UpdateCalendar();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == DisplayDateProperty)
            _UpdateCalendar();
    }

    private void _UpdateCalendar()
    {
        var display = DisplayDate;

        var raw = display.ToString("MMMM yyyy", CultureInfo.CurrentCulture);
        var newHeader = char.ToUpperInvariant(raw[0]) + raw[1..];
        SetAndRaise(HeaderTextProperty, ref _headerText, newHeader);

        var firstDay = new DateTime(display.Year, display.Month, 1);
        var daysBack = ((int)firstDay.DayOfWeek + 6) % 7; // Monday = 0
        var startDay = firstDay.AddDays(-daysBack);

        Days.Clear();
        for (var i = 0; i < 42; i++)
        {
            var date = startDay.AddDays(i);
            Days.Add(new CalendarDay(
                date,
                date.Month == display.Month,
                date.Date == DateTime.Today
            ));
        }
    }
}
