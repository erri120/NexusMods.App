using JetBrains.Annotations;
using R3;

namespace NexusMods.Sdk.RemoteServices.NexusMods;

/// <summary>
/// Handles authentication for the Nexus Mods APIs.
/// </summary>
[PublicAPI]
public interface ILoginManager
{
    /// <summary>
    /// Gets whether the user is authenticated.
    /// </summary>
    IReadOnlyBindableReactiveProperty<bool> IsAuthenticated { get; }

    /// <summary>
    /// Authenticates the user with the Nexus Mods API.
    /// </summary>
    ValueTask Authenticate(CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes the authentication.
    /// </summary>
    ValueTask RevokeAuthentication(CancellationToken cancellationToken = default);
}
