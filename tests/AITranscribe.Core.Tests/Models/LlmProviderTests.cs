using AITranscribe.Core.Models;
using FluentAssertions;

namespace AITranscribe.Core.Tests.Models;

public class LlmProviderTests
{
    [Fact]
    public void Construct_WithAllFields_SetsValues()
    {
        var provider = new LlmProvider(
            "openrouter",
            "https://openrouter.ai/api/v1",
            "OPENROUTER_API_KEY",
            "anthropic/claude-3-haiku",
            "OPENROUTER_API_KEY",
            "OPENROUTER_LLM_MODEL"
        );

        provider.Name.Should().Be("openrouter");
        provider.BaseUrl.Should().Be("https://openrouter.ai/api/v1");
        provider.ApiKey.Should().Be("OPENROUTER_API_KEY");
        provider.DefaultModel.Should().Be("anthropic/claude-3-haiku");
        provider.EnvKey.Should().Be("OPENROUTER_API_KEY");
        provider.EnvModel.Should().Be("OPENROUTER_LLM_MODEL");
    }

    [Fact]
    public void IsRecord_Equality()
    {
        var p1 = new LlmProvider("a", "b", "c", "d", "e", "f");
        var p2 = new LlmProvider("a", "b", "c", "d", "e", "f");

        p1.Should().Be(p2);
    }

    [Fact]
    public void Has_AllSixFields()
    {
        var properties = typeof(LlmProvider).GetProperties();
        properties.Should().HaveCount(6);
        properties.Select(p => p.Name).Should().BeEquivalentTo(
            new[] { "Name", "BaseUrl", "ApiKey", "DefaultModel", "EnvKey", "EnvModel" }
        );
    }
}
