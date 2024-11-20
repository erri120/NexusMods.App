using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NSubstitute;
using Xunit;

namespace NexusMods.Telemetry.Tests;

public class EventSenderTests
{
    [Fact]
    public async Task Test()
    {
        var loginManager = Substitute.For<ILoginManager>();
        loginManager.UserInfo.Returns(new UserInfo
        {
            UserId = UserId.From(1337),
            Name = "",
            AvatarUrl = null,
            IsPremium = false,
        });

        var sender = new EventSender(loginManager, new HttpClient());

        sender.AddEvent(definition: UIEvents.AddLoadout, metadata: new EventMetadata(name: "stardewvalley"));
        sender.AddEvent(definition: UIEvents.RemoveLoadout, metadata: new EventMetadata(name: "stardewvalley"));

        await sender.Run();
    }
}
