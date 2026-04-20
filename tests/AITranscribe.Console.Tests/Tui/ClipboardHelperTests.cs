using AITranscribe.Console.Tui;
using FluentAssertions;

namespace AITranscribe.Console.Tests.Tui;

public class ClipboardHelperTests
{
    [Fact]
    public void ClipboardHelper_InterfaceExists()
    {
        typeof(ClipboardHelper).Should().NotBeNull();
        var method = typeof(ClipboardHelper).GetMethod("SetText", new[] { typeof(string) });
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(bool));
    }
}
