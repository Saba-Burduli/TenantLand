namespace PostyLand.IntegrationTests.Infrastructure;

[CollectionDefinition("PostyLandIntegration", DisableParallelization = true)]
public sealed class IntegrationCollection : ICollectionFixture<PostyLandIntegrationFixture>
{
    public const string Name = "PostyLandIntegration";
}
