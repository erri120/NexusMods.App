using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Tutorial;

[JsonName("TutorialPageContext")]
public record TutorialPageContext : IPageFactoryContext;

[UsedImplicitly]
public class TutorialPageFactory : APageFactory<ITutorialPageViewModel, TutorialPageContext>
{
    public TutorialPageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public static PageFactoryId StaticId => PageFactoryId.From(Guid.Parse("fa7514f7-760a-4b05-9d1a-d4932d53abfb"));
    public override PageFactoryId Id { get; } = StaticId;

    public override ITutorialPageViewModel CreateViewModel(TutorialPageContext context)
    {
        return ServiceProvider.GetRequiredService<ITutorialPageViewModel>();
    }

    public override IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails(IWorkspaceContext workspaceContext)
    {
        return new[]
        {
            new PageDiscoveryDetails
            {
                ItemName = "Tutorial",
                PageData = new PageData
                {
                    Context = new TutorialPageContext(),
                    FactoryId = StaticId
                },
                SectionName = "Utilities",
            },
        };
    }
}
