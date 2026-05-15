using System;
using System.Reactive.Linq;
using ReactiveUI;
using Avalonia.Interactivity;
using PiTouchDate.ViewModels;

namespace PiTouchDate.Overlays;

public class WifiSettingsViewModel : ViewModelBase
{
    public WifiSettingsViewModel()
    {
        Console.WriteLine("Overlay constructor!");
    }
}
