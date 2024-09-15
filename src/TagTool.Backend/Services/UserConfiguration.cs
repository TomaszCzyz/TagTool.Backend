using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TagTool.Backend.Services;

public sealed class UserConfiguration : INotifyPropertyChanged
{
    public ObservableCollection<string> ObservedLocations { get; init; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public UserConfiguration()
    {
        ObservedLocations.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ObservedLocations));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
