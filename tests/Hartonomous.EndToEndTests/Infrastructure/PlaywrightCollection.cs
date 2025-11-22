using Xunit;

namespace Hartonomous.EndToEndTests.Infrastructure;

/// <summary>
/// Collection fixture for Playwright to share browser instance across tests safely.
/// </summary>
[CollectionDefinition("Playwright")]
public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture>
{
}
