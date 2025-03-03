namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// A group of jobs
/// </summary>
public interface IJobGroup
{
    /// <summary>
    /// Cancellation token.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <inheritdoc cref="CancellationTokenSource.Cancel()"/>
    void Cancel();

    /// <inheritdoc cref="CancellationTokenSource.CancelAsync()"/>
    Task CancelAsync();
}
