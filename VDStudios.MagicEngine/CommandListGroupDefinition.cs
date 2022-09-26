namespace VDStudios.MagicEngine;

/// <summary>
/// Represents the data required to define a new CommandListGroup for this GraphicsManager
/// </summary>
public readonly struct CommandListGroupDefinition
{
    /// <summary>
    /// The degree of parallelism that this CommandListGroup will have
    /// </summary>
    public int Parallelism { get; }

    /// <summary>
    /// The amount of expected operations each CommandList will have in the group
    /// </summary>
    public int ExpectedOperations { get; }

    /// <summary>
    /// Creates a new instance of type <see cref="CommandListGroupDefinition"/>
    /// </summary>
    /// <param name="parallelism">The degree of parallelism that this CommandListGroup will have</param>
    /// <param name="expectedOperations">The amount of expected operations each CommandList will have in the group</param>
    public CommandListGroupDefinition(int parallelism, int expectedOperations = 100)
    {
        if (parallelism <= 0)
            throw new ArgumentOutOfRangeException(nameof(parallelism), parallelism, "The degree of parallelism must be larger than 0");
        if (expectedOperations <= 0)
            throw new ArgumentOutOfRangeException(nameof(expectedOperations), expectedOperations, "The amount of expected operations must be larger than 0");
        Parallelism = parallelism;
        ExpectedOperations = expectedOperations;
    }

    /// <summary>
    /// Deconstructs this <see cref="CommandListGroupDefinition"/> in the following order: parallelism, expectedOperations
    /// </summary>
    /// <param name="parallelism">The degree of parallelism that this CommandListGroup will have</param>
    /// <param name="expectedOperations">The amount of expected operations each CommandList will have in the group</param>
    public void Deconstruct(out int parallelism, out int expectedOperations)
    {
        parallelism = Parallelism;
        expectedOperations = ExpectedOperations;
    }
}
