using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Tedd.TUI.HumanTests.Infrastructure;

public class TestViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public string Text { get => field; set { if (field != value) { field = value; OnPropertyChanged(); } } } = "Bound Text";
    public bool IsChecked { get => field; set { if (field != value) { field = value; OnPropertyChanged(); } } } = false;
    public int Value { get => field; set { if (field != value) { field = value; OnPropertyChanged(); } } } = 0;
}
