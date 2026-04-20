using AITranscribe.Console.Commands;
using FluentAssertions;

namespace AITranscribe.Console.Tests.Commands;

public class TranscribeCommandTests
{
    [Fact]
    public void TranscribeSettings_HasAllOptions()
    {
        var settings = new TranscribeSettings();

        var fileProp = typeof(TranscribeSettings).GetProperty("File");
        var listProp = typeof(TranscribeSettings).GetProperty("ListPrompts");
        var queryProp = typeof(TranscribeSettings).GetProperty("QueryPrompt");
        var removeProp = typeof(TranscribeSettings).GetProperty("RemovePrompt");
        var englishProp = typeof(TranscribeSettings).GetProperty("English");
        var llmModelProp = typeof(TranscribeSettings).GetProperty("LlmModel");
        var postProcessProp = typeof(TranscribeSettings).GetProperty("PostProcess");
        var sttModelProp = typeof(TranscribeSettings).GetProperty("SttModel");
        var verboseProp = typeof(TranscribeSettings).GetProperty("Verbose");

        fileProp.Should().NotBeNull();
        listProp.Should().NotBeNull();
        queryProp.Should().NotBeNull();
        removeProp.Should().NotBeNull();
        englishProp.Should().NotBeNull();
        llmModelProp.Should().NotBeNull();
        postProcessProp.Should().NotBeNull();
        sttModelProp.Should().NotBeNull();
        verboseProp.Should().NotBeNull();
    }

    [Fact]
    public void TranscribeSettings_DefaultValues()
    {
        var settings = new TranscribeSettings();

        settings.File.Should().BeNull();
        settings.ListPrompts.Should().BeFalse();
        settings.QueryPrompt.Should().BeFalse();
        settings.RemovePrompt.Should().BeNull();
        settings.English.Should().BeFalse();
        settings.LlmModel.Should().Be("anthropic/claude-3-haiku");
        settings.PostProcess.Should().BeFalse();
        settings.SttModel.Should().Be("whisper-large-v3-turbo");
        settings.Verbose.Should().BeFalse();
    }

    [Fact]
    public void TranscribeSettings_EnglishAndPostProcess_AreMutuallyExclusive()
    {
        var settings = new TranscribeSettings { English = true, PostProcess = true };

        var result = settings.Validate();

        result.Successful.Should().BeFalse();
    }

    [Fact]
    public void TranscribeSettings_EnglishOnly_IsValid()
    {
        var settings = new TranscribeSettings { English = true };

        var result = settings.Validate();

        result.Successful.Should().BeTrue();
    }

    [Fact]
    public void TranscribeSettings_PostProcessOnly_IsValid()
    {
        var settings = new TranscribeSettings { PostProcess = true };

        var result = settings.Validate();

        result.Successful.Should().BeTrue();
    }

    [Fact]
    public void TranscribeSettings_NeitherEnglishNorPostProcess_IsValid()
    {
        var settings = new TranscribeSettings();

        var result = settings.Validate();

        result.Successful.Should().BeTrue();
    }

    [Fact]
    public void TranscribeSettings_IsLegacyMode_WhenAllDefaults()
    {
        var settings = new TranscribeSettings();

        settings.IsLegacyMode().Should().BeFalse();
    }

    [Fact]
    public void TranscribeSettings_IsLegacyMode_WhenFileSet()
    {
        var settings = new TranscribeSettings { File = "test.wav" };

        settings.IsLegacyMode().Should().BeTrue();
    }

    [Fact]
    public void TranscribeSettings_IsLegacyMode_WhenVerboseSet()
    {
        var settings = new TranscribeSettings { Verbose = true };

        settings.IsLegacyMode().Should().BeTrue();
    }

    [Fact]
    public void TranscribeSettings_IsLegacyMode_WhenEnglishSet()
    {
        var settings = new TranscribeSettings { English = true };

        settings.IsLegacyMode().Should().BeTrue();
    }

    [Fact]
    public void TranscribeSettings_IsLegacyMode_WhenListPromptsSet()
    {
        var settings = new TranscribeSettings { ListPrompts = true };

        settings.IsLegacyMode().Should().BeTrue();
    }

    [Fact]
    public void TranscribeSettings_IsLegacyMode_WhenQueryPromptSet()
    {
        var settings = new TranscribeSettings { QueryPrompt = true };

        settings.IsLegacyMode().Should().BeTrue();
    }

    [Fact]
    public void TranscribeSettings_IsLegacyMode_WhenRemovePromptSet()
    {
        var settings = new TranscribeSettings { RemovePrompt = 1 };

        settings.IsLegacyMode().Should().BeTrue();
    }

    [Fact]
    public void TranscribeSettings_IsLegacyMode_WhenPostProcessSet()
    {
        var settings = new TranscribeSettings { PostProcess = true };

        settings.IsLegacyMode().Should().BeTrue();
    }

    [Fact]
    public void TranscribeSettings_IsLegacyMode_WhenSttModelNonDefault()
    {
        var settings = new TranscribeSettings { SttModel = "other-model" };

        settings.IsLegacyMode().Should().BeTrue();
    }

    [Fact]
    public void TranscribeSettings_IsLegacyMode_WhenLlmModelNonDefault()
    {
        var settings = new TranscribeSettings { LlmModel = "other-model" };

        settings.IsLegacyMode().Should().BeTrue();
    }
}
