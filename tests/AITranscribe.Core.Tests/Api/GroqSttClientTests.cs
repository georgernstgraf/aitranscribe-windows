using AITranscribe.Core.Api;
using FluentAssertions;

namespace AITranscribe.Core.Tests.Api;

public class GroqSttClientTests
{
    [Fact]
    public void ISttClient_InterfaceExists()
    {
        ISttClient client = new GroqSttClient("test-key");
        client.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NullApiKey_ThrowsArgumentNullException()
    {
        Action act = () => new GroqSttClient(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var client = new GroqSttClient("test-key");
        client.ApiKey.Should().Be("test-key");
    }
}
