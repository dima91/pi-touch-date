using System;

namespace PiTouchDate.Controls;

public record CalendarDay(DateTime Date, bool IsCurrentMonth, bool IsToday)
{
    public int DayNumber => Date.Day;
    public bool IsOtherMonth => !IsCurrentMonth;
}
