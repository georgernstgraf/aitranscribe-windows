using AITranscribe.Core.Data;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace AITranscribe.Console.Tui;

public class HistoryManager
{
    private readonly IPromptManager _promptManager;
    private readonly AITranscribeTui _tui;
    private List<StoredPrompt> _prompts = [];

    public HistoryManager(IPromptManager promptManager, AITranscribeTui tui)
    {
        _promptManager = promptManager;
        _tui = tui;
    }

    public IReadOnlyList<StoredPrompt> Prompts => _prompts;
    public long? SelectedHistoryId { get; private set; }
    public string SelectedHistoryText { get; private set; } = "";
    public string SelectedHistoryFilename { get; private set; } = "";
    public bool IsAppendMode { get; set; }

    public async Task RefreshHistoryAsync(CancellationToken ct = default)
    {
        var prompts = await _promptManager.GetRecentAsync(null, ct);
        _prompts = prompts.ToList();

        var frameWidth = _tui.HistoryList.Frame.Width;
        if (frameWidth < 20)
            frameWidth = 80;
        var items = new System.Collections.ObjectModel.ObservableCollection<string>();
        foreach (var prompt in _prompts)
        {
            var previewSource = !string.IsNullOrEmpty(prompt.Summary)
                ? prompt.Summary.Replace("\n", " ")
                : prompt.Prompt.Replace("\n", " ");
            var prefix = $"#{prompt.Id}: ";
            var textWidth = Math.Max(8, frameWidth - prefix.Length - 2);
            var shortened = Shorten(previewSource, textWidth);
            items.Add($"{prefix}{shortened}");
        }

        _tui.HistoryList.SetSource(items);

        if (_prompts.Count > 0 && SelectedHistoryId.HasValue)
        {
            var selectedIndex = _prompts.FindIndex(p => p.Id == SelectedHistoryId.Value);
            if (selectedIndex >= 0)
            {
                _tui.HistoryList.SelectedItem = selectedIndex;
            }
        }
    }

    private static string Shorten(string text, int width)
    {
        var collapsed = string.Join(" ", text.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries));
        if (collapsed.Length <= width)
            return collapsed;

        if (width <= 8)
            return collapsed[..width];

        var slice = collapsed[..(width - 4)];
        var lastSpace = slice.LastIndexOf(' ');
        if (lastSpace > width / 2)
            slice = collapsed[..lastSpace];

        return slice + " ...";
    }

    public async Task<long?> SaveTranscriptAsync(string text, string filename, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (SelectedHistoryId.HasValue)
        {
            var updated = await _promptManager.UpdateAsync(SelectedHistoryId.Value, text, ct);
            if (updated)
            {
                SelectedHistoryText = text;
                await RefreshHistoryAsync(ct);
                return SelectedHistoryId.Value;
            }
            return null;
        }

        var promptId = await _promptManager.AddAsync(text, filename, null, ct);
        if (promptId > 0)
        {
            SelectedHistoryId = promptId;
            SelectedHistoryText = text;
            SelectedHistoryFilename = filename;
            await RefreshHistoryAsync(ct);
        }
        return promptId > 0 ? promptId : null;
    }

    public async Task<bool> DeleteSelectedAsync(CancellationToken ct = default)
    {
        if (!SelectedHistoryId.HasValue)
            return false;

        var removed = await _promptManager.RemoveByIdAsync(SelectedHistoryId.Value, ct);
        if (removed)
        {
            SelectedHistoryId = null;
            SelectedHistoryText = "";
            SelectedHistoryFilename = "";
            _tui.TranscriptView.Text = "No transcript yet.";
            await RefreshHistoryAsync(ct);
        }
        return removed;
    }

    public void SelectHistoryItem(int index)
    {
        if (index < 0 || index >= _prompts.Count)
            return;

        var prompt = _prompts[index];
        SelectedHistoryId = prompt.Id;
        SelectedHistoryText = prompt.Prompt;
        SelectedHistoryFilename = prompt.Filename;
        _tui.TranscriptView.Text = prompt.Prompt;
    }
}
