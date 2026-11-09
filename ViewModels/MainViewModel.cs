using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using InsertionSort.Algorithms;
using InsertionSort.Infrastructure;
using InsertionSort.Models;

namespace InsertionSort.ViewModels;

/// <summary>
/// Holds the main page state and coordinates the insertion sort step sequence.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly ObservableCollection<double> _values = new();
    private readonly InsertionSortAlgorithm _algorithm;
    private int _nextExpectedStepNumber;
    private SortStep? _currentStep;
    private bool _isSorting;
    private string _inputText = string.Empty;
    private string _inputError = string.Empty;
    private string _statusMessage = "Добавьте числа или создайте случайный массив.";

    public MainViewModel()
        : this(new InsertionSortAlgorithm())
    {
    }

    public MainViewModel(InsertionSortAlgorithm algorithm)
    {
        _algorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm));
        Values = new ReadOnlyObservableCollection<double>(_values);
        _values.CollectionChanged += OnValuesChanged;

        AddInputCommand = new RelayCommand(AddInput, () => CanAddInput);
        AddRandomValuesCommand = new RelayCommand(AddRandomValues, () => CanEditValues);
        RemoveLastValueCommand = new RelayCommand(RemoveLastValue, () => CanRemoveLastValue);
        ClearValuesCommand = new RelayCommand(ClearValues, () => CanClearValues);
        SortCommand = new RelayCommand(SortAll, () => CanStartSorting);
    }

    /// <summary>
    /// Read-only collection exposed to the view.
    /// All modifications must go through this view model.
    /// </summary>
    public ReadOnlyObservableCollection<double> Values { get; }

    public RelayCommand AddInputCommand { get; }

    public RelayCommand AddRandomValuesCommand { get; }

    public RelayCommand RemoveLastValueCommand { get; }

    public RelayCommand ClearValuesCommand { get; }

    public RelayCommand SortCommand { get; }

    public bool HasValues => _values.Count > 0;

    public bool IsEmpty => !HasValues;

    public int ValueCount => _values.Count;

    public bool CanEditValues => !IsSorting;

    public bool CanAddInput => CanEditValues && !string.IsNullOrWhiteSpace(InputText);

    public bool CanRemoveLastValue => CanEditValues && HasValues;

    public bool CanClearValues => CanEditValues && HasValues;

    public bool CanStartSorting => !IsSorting && _values.Count > 1;

    public SortStep? CurrentStep
    {
        get => _currentStep;
        private set => SetProperty(ref _currentStep, value);
    }

    public bool IsSorting
    {
        get => _isSorting;
        private set
        {
            if (SetProperty(ref _isSorting, value))
            {
                OnPropertyChanged(nameof(CanEditValues));
                OnPropertyChanged(nameof(CanAddInput));
                OnPropertyChanged(nameof(CanRemoveLastValue));
                OnPropertyChanged(nameof(CanClearValues));
                OnPropertyChanged(nameof(CanStartSorting));
                NotifyCommandStates();
            }
        }
    }

    public string InputText
    {
        get => _inputText;
        set
        {
            if (SetProperty(ref _inputText, value))
            {
                InputError = string.Empty;
                OnPropertyChanged(nameof(CanAddInput));
                AddInputCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string InputError
    {
        get => _inputError;
        private set
        {
            if (SetProperty(ref _inputError, value))
            {
                OnPropertyChanged(nameof(HasInputError));
            }
        }
    }

    public bool HasInputError => !string.IsNullOrWhiteSpace(InputError);

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// Adds a batch of finite numeric values to the collection.
    /// </summary>
    public void AddValues(IEnumerable<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        EnsureValuesCanBeEdited();

        var incomingValues = values.ToArray();

        if (incomingValues.Any(static value => !double.IsFinite(value)))
        {
            throw new ArgumentException(
                "Only finite numeric values can be added.",
                nameof(values));
        }

        if (incomingValues.Length == 0)
        {
            return;
        }

        CurrentStep = null;

        foreach (var value in incomingValues)
        {
            _values.Add(value);
        }

        StatusMessage = $"Добавлено чисел: {incomingValues.Length}.";
    }

    /// <summary>
    /// Removes the last value. Returns false when the collection is empty.
    /// </summary>
    public bool RemoveLastValue()
    {
        EnsureValuesCanBeEdited();

        if (_values.Count == 0)
        {
            return false;
        }

        _values.RemoveAt(_values.Count - 1);
        CurrentStep = null;
        StatusMessage = "Последнее число удалено.";
        return true;
    }

    /// <summary>
    /// Clears all values.
    /// </summary>
    public void ClearValues()
    {
        EnsureValuesCanBeEdited();

        _values.Clear();
        CurrentStep = null;
        InputError = string.Empty;
        StatusMessage = "Список очищен.";
    }

    /// <summary>
    /// Starts a sorting session and returns its step sequence.
    /// The algorithm captures a snapshot before the collection is locked.
    /// </summary>
    public IEnumerable<SortStep> BeginSorting()
    {
        if (!CanStartSorting)
        {
            throw new InvalidOperationException(
                "At least two values are required and no sorting session may already be active.");
        }

        var steps = _algorithm.GenerateSteps(_values);

        CurrentStep = null;
        _nextExpectedStepNumber = 1;
        IsSorting = true;
        StatusMessage = "Выполняется сортировка…";

        return steps;
    }

    /// <summary>
    /// Publishes the next step to the view model.
    /// When the completed step arrives, the final sorted values are committed.
    /// </summary>
    public void ApplyStep(SortStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        if (!IsSorting)
        {
            throw new InvalidOperationException("There is no active sorting session.");
        }

        if (step.Number != _nextExpectedStepNumber)
        {
            throw new InvalidOperationException(
                $"Expected sorting step {_nextExpectedStepNumber}, but received {step.Number}.");
        }

        if (step.Values.Count != _values.Count)
        {
            throw new ArgumentException(
                "The step does not belong to the current collection.",
                nameof(step));
        }

        double[]? completedValues = null;

        if (step.Kind == SortStepKind.Completed)
        {
            completedValues = new double[step.Values.Count];

            for (var index = 0; index < step.Values.Count; index++)
            {
                completedValues[index] = step.Values[index]
                    ?? throw new ArgumentException(
                        "A completed sorting step cannot contain an empty slot.",
                        nameof(step));
            }
        }

        CurrentStep = step;
        _nextExpectedStepNumber++;

        if (completedValues is null)
        {
            return;
        }

        _values.Clear();

        foreach (var value in completedValues)
        {
            _values.Add(value);
        }

        _nextExpectedStepNumber = 0;
        IsSorting = false;
        StatusMessage = "Сортировка завершена.";
    }

    /// <summary>
    /// Cancels the active session. The source collection remains unchanged.
    /// </summary>
    public void CancelSorting()
    {
        if (!IsSorting)
        {
            return;
        }

        _nextExpectedStepNumber = 0;
        CurrentStep = null;
        IsSorting = false;
        StatusMessage = "Сортировка отменена.";
    }

    private void AddInput()
    {
        var tokens = InputText.Split(
            new[] { ' ', '\t', '\r', '\n', ';' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var parsedValues = new List<double>();
        var invalidValues = new List<string>();

        foreach (var token in tokens)
        {
            if (TryParseFiniteNumber(token, out var value))
            {
                parsedValues.Add(value);
            }
            else
            {
                invalidValues.Add(token);
            }
        }

        if (parsedValues.Count > 0)
        {
            AddValues(parsedValues);
        }

        InputText = string.Empty;

        if (invalidValues.Count > 0)
        {
            InputError = $"Не удалось добавить значения: {string.Join(", ", invalidValues)}.";
        }
        else if (parsedValues.Count == 0)
        {
            InputError = "Введите хотя бы одно конечное число.";
        }
    }

    private void AddRandomValues()
    {
        const int count = 10;
        var randomValues = Enumerable
            .Range(0, count)
            .Select(_ => (double)Random.Shared.Next(-100, 101));

        AddValues(randomValues);
        InputError = string.Empty;
    }

    private void SortAll()
    {
        var steps = BeginSorting();

        try
        {
            foreach (var step in steps)
            {
                ApplyStep(step);
            }
        }
        catch
        {
            CancelSorting();
            throw;
        }
    }

    private static bool TryParseFiniteNumber(string text, out double value)
    {
        const NumberStyles styles = NumberStyles.Float;

        if (double.TryParse(text, styles, CultureInfo.CurrentCulture, out value)
            && double.IsFinite(value))
        {
            return true;
        }

        if (double.TryParse(text, styles, CultureInfo.InvariantCulture, out value)
            && double.IsFinite(value))
        {
            return true;
        }

        if (text.Contains(',') &&
            !text.Contains('.') &&
            double.TryParse(
                text.Replace(',', '.'),
                styles,
                CultureInfo.InvariantCulture,
                out value) &&
            double.IsFinite(value))
        {
            return true;
        }

        value = default;
        return false;
    }

    private void EnsureValuesCanBeEdited()
    {
        if (IsSorting)
        {
            throw new InvalidOperationException(
                "Values cannot be changed while sorting is in progress.");
        }
    }

    private void OnValuesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasValues));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(ValueCount));
        OnPropertyChanged(nameof(CanRemoveLastValue));
        OnPropertyChanged(nameof(CanClearValues));
        OnPropertyChanged(nameof(CanStartSorting));
        NotifyCommandStates();
    }

    private void NotifyCommandStates()
    {
        AddInputCommand?.NotifyCanExecuteChanged();
        AddRandomValuesCommand?.NotifyCanExecuteChanged();
        RemoveLastValueCommand?.NotifyCanExecuteChanged();
        ClearValuesCommand?.NotifyCanExecuteChanged();
        SortCommand?.NotifyCanExecuteChanged();
    }
}
