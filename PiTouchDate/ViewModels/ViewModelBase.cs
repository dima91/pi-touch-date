using System;
using ReactiveUI;
using Splat;

namespace PiTouchDate.ViewModels;

public abstract class ViewModelBase : ReactiveObject
{
    protected static T GetService<T>() =>
        Locator.Current.GetService<T>() ?? throw new InvalidOperationException($"Service {typeof(T).Name} not registered");
}
