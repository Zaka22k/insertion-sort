using System.Globalization;
using InsertionSort.Infrastructure;

namespace InsertionSort.ViewModels;

/// <summary>
/// Presentation state for one position in the sequence visualisation.
/// </summary>
public sealed class SortCellViewModel : ObservableObject
{
    private int _position;
    private double? _value;
    private bool _isGap;
    private bool _isCompared;
    private bool _isShifted;
    private bool _isInserted;
    private bool _isSorted;

    public int Position
    {
        get => _position;
        private set => SetProperty(ref _position, value);
    }

    public double? Value
    {
        get => _value;
        private set
        {
            if (SetProperty(ref _value, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    public string DisplayText => Value?.ToString("G", CultureInfo.CurrentCulture) ?? string.Empty;

    public bool IsGap
    {
        get => _isGap;
        private set => SetProperty(ref _isGap, value);
    }

    public bool IsCompared
    {
        get => _isCompared;
        private set => SetProperty(ref _isCompared, value);
    }

    public bool IsShifted
    {
        get => _isShifted;
        private set => SetProperty(ref _isShifted, value);
    }

    public bool IsInserted
    {
        get => _isInserted;
        private set => SetProperty(ref _isInserted, value);
    }

    public bool IsSorted
    {
        get => _isSorted;
        private set => SetProperty(ref _isSorted, value);
    }

    public void Update(
        int position,
        double? value,
        bool isGap,
        bool isCompared,
        bool isShifted,
        bool isInserted,
        bool isSorted)
    {
        Position = position;
        Value = value;
        IsGap = isGap;
        IsCompared = isCompared;
        IsShifted = isShifted;
        IsInserted = isInserted;
        IsSorted = isSorted;
    }
}
