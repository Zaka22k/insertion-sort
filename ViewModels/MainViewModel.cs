using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using InsertionSort.Algorithms;
using InsertionSort.Infrastructure;
using InsertionSort.Models;

namespace InsertionSort.ViewModels;

/// <summary>
/// Holds the main page state and coordinates insertion sort playback.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly ObservableCollection<double> _values = new();
    private readonly ObservableCollection<SortCellViewModel> _cells = new();
    private readonly InsertionSortAlgorithm _algorithm;
    private int _nextExpectedStepNumber;
    private SortStep? _currentStep;
    private bool _isSorting;
    private bool _isPaused;
    private string _inputText = string.Empty;
    private string _inputError = string.Empty;
    private string _statusMessage = "Добавьте числа или создайте случайный массив.";
    private string _currentStepDescription = "Ожидание сортировки.";
    private string _currentKeyText = string.Empty;
    private double _stepDelayMilliseconds = 450;
    private CancellationTokenSource? _sortingCancellation;
    private TaskCompletionSource? _resumeSignal;

    public MainViewModel()
        : this(new InsertionSortAlgorithm())
    {
    }

    public MainViewModel(InsertionSortAlgorithm algorithm)
    {
        _algorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm));
        Values = new ReadOnlyObservableCollection<double>(_values);
        Cells = new ReadOnlyObservableCollection<SortCellViewModel>(_cells);
        _values.CollectionChanged += OnValuesChanged;

        AddInputCommand = new RelayCommand(AddInput, () => CanAddInput);
        AddRandomValuesCommand = new RelayCommand(AddRandomValues, () => CanEditValues);
        RemoveLastValueCommand = new RelayCommand(() => { RemoveLastValue(); }, () => CanRemoveLastValue);
        ClearValuesCommand = new RelayCommand(ClearValues, () => CanClearValues);
        SortCommand = new RelayCommand(() => _ = StartSortingAsync(), () => CanStartSorting);
        PauseCommand = new RelayCommand(PauseSorting, () => CanPauseSorting);
        ResumeCommand = new RelayCommand(ResumeSorting, () => CanResumeSorting);
        CancelSortCommand = new RelayCommand(CancelSorting, () => CanCancelSorting);
    }

    /// <summary>
    /// Read-only source collection exposed to the view.
    /// </summary>
    public ReadOnlyObservableCollection<double> Values { get; }

    /// <summary>
    /// Read-only presentation collection. Its cells are updated for each algorithm step.
    /// </summary>
    public ReadOnlyObservableCollection<SortCellViewModel> Cells { get; }

    public RelayCommand AddInputCommand { get; }

    public RelayCommand AddRandomValuesCommand { get; }

    public RelayCommand RemoveLastValueCommand { get; }

    public RelayCommand ClearValuesCommand { get; }

    public RelayCommand SortCommand { get; }

    public RelayCommand PauseCommand { get; }

    public RelayCommand ResumeCommand { get; }

    public RelayCommand CancelSortCommand { get; }

    public bool HasValues => _values.Count > 0;

    public bool IsEmpty => !HasValues;

    public int ValueCount => _values.Count;

    public bool CanEditValues => !IsSorting;

    public bool CanAddInput => CanEditValues && !string.IsNullOrWhiteSpace(InputText);

    public bool CanRemoveLastValue => CanEditValues && HasValues;

    public bool CanClearValues => CanEditValues && HasValues;

    public bool CanStartSorting => !IsSorting && _values.Count > 1;

    public bool CanPauseSorting => IsSorting && !IsPaused;

    public bool CanResumeSorting => IsSorting && IsPaused;

    public bool CanCancelSorting => IsSorting;

    public bool HasCurrentKey => CurrentStep?.KeyValue is not null;

    public string CurrentStepNumberText => CurrentStep is null
        ? "Шаг не начат"
        : $"Шаг {CurrentStep.Number}";

    public string CurrentKeyText => CurrentStep?.KeyValue?.ToString("G", CultureInfo.CurrentCulture) ?? string.Empty;

    public string SortedCountText
    {
        get
        {
            var sortedCount = CurrentStep?.SortedCount ?? 0;
            return $"Отсортировано: {sortedCount} из {ValueCount}";
        }
    }

    public string StepDelayDescription => $"Пауза между шагами: {StepDelayMilliseconds:0} мс";

    public SortStep? CurrentStep
    {
        get => _currentStep;
        private set
        {
            if (SetProperty(ref _currentStep, value))
            {
                OnPropertyChanged(nameof(HasCurrentKey));
                OnPropertyChanged(nameof(CurrentStepNumberText));
                OnPropertyChanged(nameof(CurrentKeyText));
                OnPropertyChanged(nameof(SortedCountText));
            }
        }
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
                OnPropertyChanged(nameof(CanPauseSorting));
                OnPropertyChanged(nameof(CanResumeSorting));
                OnPropertyChanged(nameof(CanCancelSorting));
                NotifyCommandStates();
            }
        }
    }

    public bool IsPaused
    {
        get => _isPaused;
        private set
        {
            if (SetProperty(ref _isPaused, value))
            {
                OnPropertyChanged(nameof(CanPauseSorting));
                OnPropertyChanged(nameof(CanResumeSorting));
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
    /// Delay between visualised operations, in milliseconds.
    /// </summary>
    public double StepDelayMilliseconds
    {
        get => _stepDelayMilliseconds;
        set
        {
            var boundedValue = Math.Clamp(value, 100, 2000);
            if (SetProperty(ref _stepDelayMilliseconds, boundedValue))
            {
                OnPropertyChanged(nameof(StepDelayDescription));
            }
        }
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

        RefreshCellsFromSource();
        CurrentStepDescription = "Новый массив готов к сортировке.";
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
        RefreshCellsFromSource();
        CurrentStepDescription = "Последнее число удалено.";
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
        RefreshCellsFromSource();
        InputError = string.Empty;
        CurrentStepDescription = "Ожидание сортировки.";
        StatusMessage = "Список очищен.";
    }

    /// <summary>
    /// Starts a sorting session and returns its step sequence.
    /// The algorithm captures a source snapshot before the collection is locked.
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
        IsPaused = false;
        IsSorting = true;
        CurrentStepDescription = "Подготовка последовательности.";
        StatusMessage = "Сортировка выполняется.";

        return steps;
    }

    /// <summary>
    /// Applies one validated algorithm step to the visualisation.
    /// The source collection is committed only after the Completed step.
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
        CurrentStepDescription = DescribeStep(step);
        _nextExpectedStepNumber++;
        UpdateCells(step);

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
        IsPaused = false;
        IsSorting = false;
        StatusMessage = "Сортировка завершена.";
    }

    /// <summary>
    /// Pauses playback after the currently displayed operation.
    /// </summary>
    public void PauseSorting()
    {
        if (!CanPauseSorting)
        {
            return;
        }

        _resumeSignal = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        IsPaused = true;
        StatusMessage = "Пауза. Нажмите «Продолжить», чтобы возобновить сортировку.";
    }

    /// <summary>
    /// Resumes playback from the next operation.
    /// </summary>
    public void ResumeSorting()
    {
        if (!CanResumeSorting)
        {
            return;
        }

        var resumeSignal = _resumeSignal;
        _resumeSignal = null;
        IsPaused = false;
        StatusMessage = "Сортировка продолжается.";
        resumeSignal?.TrySetResult();
    }

    /// <summary>
    /// Requests cancellation. The original source collection is restored by the playback loop.
    /// </summary>
    public void CancelSorting()
    {
        if (!CanCancelSorting)
        {
            return;
        }

        _sortingCancellation?.Cancel();
        _resumeSignal?.TrySetResult();
    }

    private async Task StartSortingAsync()
    {
        if (!CanStartSorting)
        {
            return;
        }

        using var cancellation = new CancellationTokenSource();
        _sortingCancellation = cancellation;

        try
        {
            var steps = BeginSorting();

            foreach (var step in steps)
            {
                await WaitWhilePausedAsync(cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();

                ApplyStep(step);

                if (step.Kind != SortStepKind.Completed)
                {
                    await Task.Delay(
                        TimeSpan.FromMilliseconds(StepDelayMilliseconds),
                        cancellation.Token);
                }
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            FinishCanceledSorting();
        }
        catch (Exception exception)
        {
            FinishCanceledSorting();
            StatusMessage = $"Не удалось выполнить сортировку: {exception.Message}";
            CurrentStepDescription = "Сортировка прервана из-за ошибки.";
        }
        finally
        {
            _resumeSignal?.TrySetResult();
            _resumeSignal = null;

            if (ReferenceEquals(_sortingCancellation, cancellation))
            {
                _sortingCancellation = null;
            }
        }
    }

    private async Task WaitWhilePausedAsync(CancellationToken cancellationToken)
    {
        while (IsPaused)
        {
            var resumeTask = _resumeSignal?.Task;
            if (resumeTask is null)
            {
                return;
            }

            await resumeTask.WaitAsync(cancellationToken);
        }
    }

    private void FinishCanceledSorting()
    {
        _nextExpectedStepNumber = 0;
        IsPaused = false;
        _resumeSignal = null;
        CurrentStep = null;
        RefreshCellsFromSource();
        IsSorting = false;
        CurrentStepDescription = "Исходный массив восстановлен.";
        StatusMessage = "Сортировка отменена. Исходный массив сохранён.";
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

    private void RefreshCellsFromSource()
    {
        var snapshot = _values.Select(static value => (double?)value).ToArray();
        UpdateCells(snapshot, sortedCount: 0);
    }

    private void UpdateCells(SortStep step)
    {
        for (var index = 0; index < step.Values.Count; index++)
        {
            var value = step.Values[index];
            var isGap = value is null && step.KeyIndex == index;
            var isCompared = step.ComparedIndex == index;
            var isShifted = step.Kind == SortStepKind.Shifted && step.MovedToIndex == index;
            var isInserted = step.Kind == SortStepKind.Inserted && step.KeyIndex == index;
            var isSorted = index < step.SortedCount &&
                !isGap &&
                !isCompared &&
                !isShifted &&
                !isInserted;

            UpdateCell(index, value, isGap, isCompared, isShifted, isInserted, isSorted);
        }

        TrimCells(step.Values.Count);
    }

    private void UpdateCells(IReadOnlyList<double?> values, int sortedCount)
    {
        for (var index = 0; index < values.Count; index++)
        {
            UpdateCell(
                index,
                values[index],
                isGap: false,
                isCompared: false,
                isShifted: false,
                isInserted: false,
                isSorted: index < sortedCount);
        }

        TrimCells(values.Count);
    }

    private void UpdateCell(
        int index,
        double? value,
        bool isGap,
        bool isCompared,
        bool isShifted,
        bool isInserted,
        bool isSorted)
    {
        while (_cells.Count <= index)
        {
            _cells.Add(new SortCellViewModel());
        }

        _cells[index].Update(
            index + 1,
            value,
            isGap,
            isCompared,
            isShifted,
            isInserted,
            isSorted);
    }

    private void TrimCells(int count)
    {
        while (_cells.Count > count)
        {
            _cells.RemoveAt(_cells.Count - 1);
        }
    }

    private static string DescribeStep(SortStep step)
    {
        static string Format(double? value) =>
            value?.ToString("G", CultureInfo.CurrentCulture) ?? "—";

        return step.Kind switch
        {
            SortStepKind.Started =>
                "Начальное состояние. Первый элемент считается отсортированным.",

            SortStepKind.KeySelected =>
                $"Выбран ключ {Format(step.KeyValue)}.",

            SortStepKind.Compared =>
                $"Сравнение {Format(step.Values[step.ComparedIndex!.Value])} с ключом {Format(step.KeyValue)}.",

            SortStepKind.Shifted =>
                $"Элемент {Format(step.Values[step.MovedToIndex!.Value])} сдвинут вправо.",

            SortStepKind.Inserted =>
                $"Ключ {Format(step.KeyValue)} вставлен на позицию {step.KeyIndex!.Value + 1}.",

            SortStepKind.Completed =>
                "Все элементы отсортированы.",

            _ => "Выполняется сортировка."
        };
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
        OnPropertyChanged(nameof(SortedCountText));
        NotifyCommandStates();
    }

    private void NotifyCommandStates()
    {
        AddInputCommand?.NotifyCanExecuteChanged();
        AddRandomValuesCommand?.NotifyCanExecuteChanged();
        RemoveLastValueCommand?.NotifyCanExecuteChanged();
        ClearValuesCommand?.NotifyCanExecuteChanged();
        SortCommand?.NotifyCanExecuteChanged();
        PauseCommand?.NotifyCanExecuteChanged();
        ResumeCommand?.NotifyCanExecuteChanged();
        CancelSortCommand?.NotifyCanExecuteChanged();
    }
}
