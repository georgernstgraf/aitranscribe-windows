using AITranscribe.Core.Models;
using FluentAssertions;

namespace AITranscribe.Core.Tests.Models;

public class TranscriptionTests
{
    [Fact]
    public void Construct_WithAllProperties_SetsValues()
    {
        var now = DateTime.UtcNow;
        var t = new Transcription(1, "hello world", "audio.wav", now, "summary text");

        t.Id.Should().Be(1L);
        t.Text.Should().Be("hello world");
        t.Filename.Should().Be("audio.wav");
        t.CreatedAt.Should().Be(now);
        t.Summary.Should().Be("summary text");
    }

    [Fact]
    public void Default_Summary_IsNull()
    {
        var t = new Transcription(1, "text", "file.wav", DateTime.UtcNow, null);

        t.Summary.Should().BeNull();
    }

    [Fact]
    public void Id_IsLong()
    {
        var t = new Transcription(long.MaxValue, "text", "file.wav", DateTime.UtcNow, null);

        t.Id.Should().Be(long.MaxValue);
    }

    [Fact]
    public void IsRecord_Equality()
    {
        var now = DateTime.UtcNow;
        var t1 = new Transcription(1, "text", "f.wav", now, null);
        var t2 = new Transcription(1, "text", "f.wav", now, null);

        t1.Should().Be(t2);
    }
}
