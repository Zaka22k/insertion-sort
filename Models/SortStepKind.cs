namespace InsertionSort.Models;

/// <summary>
/// Describes the operation represented by a sorting step.
/// </summary>
public enum SortStepKind
{
    Started,
    KeySelected,
    Compared,
    Shifted,
    Inserted,
    Completed
}
