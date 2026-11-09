using InsertionSort.Models;

namespace InsertionSort.Algorithms;

/// <summary>
/// Generates immutable snapshots of a stable insertion sort operation.
/// The input sequence is copied, so the caller's collection is never modified.
/// </summary>
public sealed class InsertionSortAlgorithm
{
    /// <summary>
    /// Creates a lazy sequence of sorting steps for the supplied values.
    /// The input is copied and validated before the sequence is returned.
    /// </summary>
    /// <param name="source">Values to sort.</param>
    /// <returns>Snapshots describing each operation, including the initial and final states.</returns>
    /// <exception cref="ArgumentNullException">The source sequence is null.</exception>
    /// <exception cref="ArgumentException">The sequence contains NaN or an infinite value.</exception>
    public IEnumerable<SortStep> GenerateSteps(IEnumerable<double> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var items = source.ToArray();

        if (items.Any(static value => !double.IsFinite(value)))
        {
            throw new ArgumentException(
                "Insertion sort accepts only finite numeric values.",
                nameof(source));
        }

        return GenerateStepsCore(items);
    }

    private static IEnumerable<SortStep> GenerateStepsCore(double[] items)
    {
        var stepNumber = 0;

        yield return CreateStep(
            ++stepNumber,
            SortStepKind.Started,
            items,
            sortedCount: Math.Min(1, items.Length));

        for (var index = 1; index < items.Length; index++)
        {
            var key = items[index];
            var holeIndex = index;

            yield return CreateStep(
                ++stepNumber,
                SortStepKind.KeySelected,
                items,
                sortedCount: index,
                emptyIndex: holeIndex,
                keyValue: key,
                keyIndex: holeIndex);

            while (holeIndex > 0)
            {
                var comparedIndex = holeIndex - 1;

                yield return CreateStep(
                    ++stepNumber,
                    SortStepKind.Compared,
                    items,
                    sortedCount: index,
                    emptyIndex: holeIndex,
                    keyValue: key,
                    keyIndex: holeIndex,
                    comparedIndex: comparedIndex);

                // Using a strict greater-than comparison preserves the order of equal values.
                if (items[comparedIndex] <= key)
                {
                    break;
                }

                var destinationIndex = holeIndex;
                items[destinationIndex] = items[comparedIndex];
                holeIndex = comparedIndex;

                yield return CreateStep(
                    ++stepNumber,
                    SortStepKind.Shifted,
                    items,
                    sortedCount: index,
                    emptyIndex: holeIndex,
                    keyValue: key,
                    keyIndex: holeIndex,
                    movedFromIndex: comparedIndex,
                    movedToIndex: destinationIndex);
            }

            items[holeIndex] = key;

            yield return CreateStep(
                ++stepNumber,
                SortStepKind.Inserted,
                items,
                sortedCount: index + 1,
                keyValue: key,
                keyIndex: holeIndex);
        }

        yield return CreateStep(
            ++stepNumber,
            SortStepKind.Completed,
            items,
            sortedCount: items.Length);
    }

    private static SortStep CreateStep(
        int number,
        SortStepKind kind,
        double[] items,
        int sortedCount,
        int? emptyIndex = null,
        double? keyValue = null,
        int? keyIndex = null,
        int? comparedIndex = null,
        int? movedFromIndex = null,
        int? movedToIndex = null)
    {
        var snapshot = new double?[items.Length];

        for (var index = 0; index < items.Length; index++)
        {
            if (index != emptyIndex)
            {
                snapshot[index] = items[index];
            }
        }

        return new SortStep(
            number,
            kind,
            Array.AsReadOnly(snapshot),
            sortedCount,
            keyValue,
            keyIndex,
            comparedIndex,
            movedFromIndex,
            movedToIndex);
    }
}
