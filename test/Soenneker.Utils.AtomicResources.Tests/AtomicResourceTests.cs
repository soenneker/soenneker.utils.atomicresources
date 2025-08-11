using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.Utils.AtomicResources.Tests;

[Collection("Collection")]
public sealed class AtomicResourceTests : FixturedUnitTest
{

    public AtomicResourceTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {

    }

    [Fact]
    public void Default()
    {

    }
}
