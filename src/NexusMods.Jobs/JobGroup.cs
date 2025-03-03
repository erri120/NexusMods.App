using NexusMods.Abstractions.Jobs;

namespace NexusMods.Jobs;

public class JobGroup : IJobGroup
{
    private readonly CancellationTokenSource _cts = new();

    public CancellationToken CancellationToken => _cts.Token;

    public void Cancel() => _cts.Cancel();
    public Task CancelAsync() => _cts.CancelAsync();
}
