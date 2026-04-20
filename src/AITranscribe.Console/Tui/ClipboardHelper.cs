namespace AITranscribe.Console.Tui;

public static class ClipboardHelper
{
    public static bool SetText(string text)
    {
        try
        {
            System.Windows.Forms.Clipboard.SetText(text);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
