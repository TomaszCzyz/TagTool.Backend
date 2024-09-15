using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TagTool.Backend.Services;

public sealed class UserConfiguration : INotifyPropertyChanged
{
    public ObservableCollection<string> WatchedLocations { get; init; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public UserConfiguration()
    {
        WatchedLocations.CollectionChanged += (_, _) => OnPropertyChanged(nameof(WatchedLocations));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
