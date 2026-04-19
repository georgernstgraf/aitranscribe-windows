using AITranscribe.Core.Models;
using FluentAssertions;

namespace AITranscribe.Core.Tests.Models;

public class PreProcessModeTests
{
    [Fact]
    public void Raw_HasValue_Zero()
    {
        ((int)PreProcessMode.Raw).Should().Be(0);
    }

    [Fact]
    public void Cleanup_HasValue_One()
    {
        ((int)PreProcessMode.Cleanup).Should().Be(1);
    }

    [Fact]
    public void English_HasValue_Two()
    {
        ((int)PreProcessMode.English).Should().Be(2);
    }

    [Fact]
    public void Enum_HasExactlyThreeMembers()
    {
        Enum.GetValues<PreProcessMode>().Should().HaveCount(3);
    }

    [Fact]
    public void CanParse_FromString()
    {
        Enum.Parse<PreProcessMode>("Raw").Should().Be(PreProcessMode.Raw);
        Enum.Parse<PreProcessMode>("Cleanup").Should().Be(PreProcessMode.Cleanup);
        Enum.Parse<PreProcessMode>("English").Should().Be(PreProcessMode.English);
    }
}
