using System;
using System.Reactive.Linq;
using ReactiveUI;
using Avalonia.Interactivity;
using PiTouchDate.ViewModels;
using SharpWifiManager;
using Avalonia.Threading;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Reactive;

namespace PiTouchDate.Overlays;

public class WifiSettingsViewModel : ViewModelBase, IDisposable
{
    private WifiManager _wifiManager;
    private Task<(bool Success, string Message)>? _connectionTask;
    private readonly CancellationTokenSource _cts = new();

    private readonly ObservableAsPropertyHelper<bool> _isConnecting;
    public bool IsConnecting => _isConnecting.Value;

    private bool _scanning = false;
    public bool Scanning {
        get => _scanning;
        set => this.RaiseAndSetIfChanged(ref _scanning, value);
    }

    private bool _isRefreshError = false;
    public bool IsRefreshError
    {
        get => _isRefreshError;
        set => this.RaiseAndSetIfChanged(ref _isRefreshError, value);
    }

    private WifiNetwork? _selectedNetwork = null;
    public WifiNetwork? SelectedNetwork
    {
        get => _selectedNetwork;
        set => this.RaiseAndSetIfChanged(ref _selectedNetwork, value);
    }

    private string _wifiPassword = "";
    public string WifiPassword
    {
        get => _wifiPassword;
        set => this.RaiseAndSetIfChanged(ref _wifiPassword, value);
    }

    public ObservableCollection<WifiNetwork> Networks { get; } = new();

    // ----------  Commands  ----------
    public ReactiveCommand<Unit, bool> ConnectCommand { get; }

    public ReactiveCommand<string, Unit> KeyPressCommand { get; }

    public ReactiveCommand<Unit, Unit> CancelConnectionCommand {get; }



    public WifiSettingsViewModel()
    {
        _wifiManager = new WifiManager();
        _connectionTask = null;
        KeyPressCommand = ReactiveCommand.Create<string>(OnKeyPress);
        ConnectCommand = ReactiveCommand.CreateFromTask(ConnectCommandHandler);
        CancelConnectionCommand = ReactiveCommand.Create(CancelConnectionCommandHandler);

        _isConnecting = ConnectCommand.IsExecuting
            .ToProperty(this, x => x.IsConnecting, scheduler: RxApp.MainThreadScheduler);

        _ = StartScanningLoop(_cts.Token);
    }


    private async Task StartScanningLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (SelectedNetwork == null)
            {
                Scanning = true;
                try {
                    await RefreshNetworks();
                    IsRefreshError = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Got exception: {ex}");
                    IsRefreshError = true;
                }
                finally
                {
                    Scanning = false;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
        }
    }


    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
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


    public void SelectNetwork(WifiNetwork network)
    {
        SelectedNetwork = network;
        WifiPassword = string.Empty;
    }

    // ----------  Commands handlers  ----------

    private async Task<bool> ConnectCommandHandler()
    {
        bool connectionResult = false;

        if (SelectedNetwork == null)
            return connectionResult;
        
        try
        {
            _connectionTask = _wifiManager.CreateAndActivateUsingNmcliAsync(
                SelectedNetwork.InterfaceName,
                SelectedNetwork.Ssid,
                WifiPassword);

            var (success, message) = await _connectionTask;

            Console.WriteLine($"Connection result: {success}, {message}");
            if (success) { SelectedNetwork = null; }
        }
        catch (Exception ex) {
            Console.WriteLine(ex.Message);
            connectionResult = false;
        }

        _connectionTask = null;

        return connectionResult;
    }


    private void CancelConnectionCommandHandler()
    {
        SelectedNetwork = null;
    }


    public void OnKeyPress(string key)
    {
        if (key == "Backspace")
        {
            if (!string.IsNullOrEmpty(WifiPassword))
                WifiPassword = WifiPassword.Substring(0, WifiPassword.Length - 1);
        }
        else
        {
            WifiPassword += key;
        }
    }
}
