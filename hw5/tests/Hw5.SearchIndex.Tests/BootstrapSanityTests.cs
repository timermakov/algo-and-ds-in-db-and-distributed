using Hw5.SearchIndex;

namespace Hw5.SearchIndex.Tests;

public sealed class BootstrapSanityTests
{
    [Fact]
    public void BootstrapVersion_IsDefined()
    {
        Assert.Equal("phase1-bootstrap", SearchIndexBootstrap.Version);
    }
}
