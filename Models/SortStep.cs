namespace InsertionSort.Models;

/// <summary>
/// Immutable snapshot of one operation performed by the insertion sort algorithm.
/// A null value in <see cref="Values"/> represents the temporary gap left while the key is held.
/// </summary>
/// <param name="Number">One-based position of this step in the generated sequence.</param>
/// <param name="Kind">Operation represented by this snapshot.</param>
/// <param name="Values">Read-only array state. A null entry represents the current gap.</param>
/// <param name="SortedCount">Number of values that form the sorted prefix after this operation.</param>
/// <param name="KeyValue">Value currently being inserted, when an insertion is in progress.</param>
/// <param name="KeyIndex">Index of the key or temporary gap.</param>
/// <param name="ComparedIndex">Index of the value currently compared with the key.</param>
/// <param name="MovedFromIndex">Source index of a shifted value.</param>
/// <param name="MovedToIndex">Destination index of a shifted value.</param>
public sealed record SortStep(
    int Number,
    SortStepKind Kind,
    IReadOnlyList<double?> Values,
    int SortedCount,
    double? KeyValue = null,
    int? KeyIndex = null,
    int? ComparedIndex = null,
    int? MovedFromIndex = null,
    int? MovedToIndex = null);
