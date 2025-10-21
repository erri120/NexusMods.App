using NexusMods.Backend.GitHub;
using NexusMods.Sdk;

namespace NexusMods.Backend.Tests.GitHub;

public class GitHubApiTests
{
    [Test]
    [Property("RequiresNetworking", "true")]
    public async Task Test_FetchReleases()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(ApplicationConstants.UserAgent);

        var api = new GitHubApi(TestHelpers.CreateLogger<GitHubApi>(), client);

        var releases = await api.FetchReleases("Nexus-Mods", "NexusMods.App");
        await Assert.That(releases).IsNotNull().And.IsNotEmpty();
    }
}
