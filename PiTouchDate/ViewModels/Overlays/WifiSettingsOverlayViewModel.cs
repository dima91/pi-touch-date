using System;
using System.Reactive.Linq;
using ReactiveUI;
using Avalonia.Interactivity;
using PiTouchDate.ViewModels;
using SharpWifiManager;
using Avalonia.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PiTouchDate.Overlays;

public class WifiSettingsViewModel : ViewModelBase
{
    private WifiManager _wifiManager;

    private bool _scanning = false;
    public bool Scanning {
        get => _scanning;
        set => this.RaiseAndSetIfChanged(ref _scanning, value);
    }

    public ObservableCollection<WifiNetwork> Networks { get; } = new();

    
    public WifiSettingsViewModel()
    {
        Console.WriteLine("Overlay constructor!");

        _wifiManager = new WifiManager();

        _ = StartScanningLoop();
    }


    private async Task StartScanningLoop()
    {
        while (true)
        {
            Scanning = true;
            try {
                await RefreshNetworks();
            }
            catch {}
            finally
            {
                Scanning = false;
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    private async Task RefreshNetworks ()
    {
        IReadOnlyList<WifiNetwork> currentNetworks = await _wifiManager.GetNetworksAsync();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Networks.Clear();
            foreach (var network in currentNetworks)
            {
                Networks.Add(network);
            }
        });
    }
}
