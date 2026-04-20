using AITranscribe.Console.Tui;
using FluentAssertions;

namespace AITranscribe.Console.Tests.Tui;

public class TranslationTests
{
    [Fact]
    public void TranslateToGermanPrompt_MatchesPython()
    {
        Prompts.TranslateToGermanPrompt.Should().Be(
            "Translate the following text to German. " +
            "Output ONLY the translated text with no introductory remarks or explanations."
        );
    }

    [Fact]
    public void TranslateToEnglishPrompt_MatchesPython()
    {
        Prompts.TranslateToEnglishPrompt.Should().Be(
            "Translate the following text to English. " +
            "Output ONLY the translated text with no introductory remarks or explanations."
        );
    }
}
