namespace AITranscribe.Console.Tui;

public static class Prompts
{
    public const string SummaryPrompt =
        "Create a concise summary of the transcription in 70 to 80 characters. " +
        "Output only the summary text with no quotes, labels, or extra commentary.";

    public const string TranslateToGermanPrompt =
        "Translate the following text to German. " +
        "Output ONLY the translated text with no introductory remarks or explanations.";

    public const string TranslateToEnglishPrompt =
        "Translate the following text to English. " +
        "Output ONLY the translated text with no introductory remarks or explanations.";
}
