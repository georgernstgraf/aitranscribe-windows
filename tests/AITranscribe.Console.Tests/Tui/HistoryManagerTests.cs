using AITranscribe.Console.Tui;
using AITranscribe.Core.Data;
using FluentAssertions;
using Moq;
using Terminal.Gui;

namespace AITranscribe.Console.Tests.Tui;

public class HistoryManagerTests
{
    private readonly Mock<IPromptManager> _promptManagerMock;
    private readonly AITranscribeTui _tui;
    private readonly HistoryManager _manager;

    public HistoryManagerTests()
    {
        _promptManagerMock = new Mock<IPromptManager>();
        _tui = new AITranscribeTui();
        _manager = new HistoryManager(_promptManagerMock.Object, _tui);
    }

    [Fact]
    public async Task RefreshHistoryAsync_PopulatesList()
    {
        var prompts = new List<StoredPrompt>
        {
            new(1, "Hello world", "test.wav", DateTime.Now, "Greeting"),
            new(2, "Second prompt text here", "test2.wav", DateTime.Now, null),
        };

        _promptManagerMock.Setup(pm => pm.GetRecentAsync(null, default))
            .ReturnsAsync(prompts);

        await _manager.RefreshHistoryAsync();

        _manager.Prompts.Should().HaveCount(2);
        _manager.Prompts[0].Id.Should().Be(1);
        _manager.Prompts[1].Id.Should().Be(2);
        _promptManagerMock.Verify(pm => pm.GetRecentAsync(null, default), Times.Once);
    }

    [Fact]
    public async Task RefreshHistoryAsync_EmptyList_ClearsPrompts()
    {
        _promptManagerMock.Setup(pm => pm.GetRecentAsync(null, default))
            .ReturnsAsync(new List<StoredPrompt>());

        await _manager.RefreshHistoryAsync();

        _manager.Prompts.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveTranscriptAsync_CallsPromptManager_WhenNew()
    {
        _promptManagerMock.Setup(pm => pm.AddAsync("transcript text", "test.wav", null, default))
            .ReturnsAsync(42L);
        _promptManagerMock.Setup(pm => pm.GetRecentAsync(null, default))
            .ReturnsAsync(new List<StoredPrompt>());

        var result = await _manager.SaveTranscriptAsync("transcript text", "test.wav");

        result.Should().Be(42);
        _manager.SelectedHistoryId.Should().Be(42);
        _manager.SelectedHistoryText.Should().Be("transcript text");
        _promptManagerMock.Verify(pm => pm.AddAsync("transcript text", "test.wav", null, default), Times.Once);
    }

    [Fact]
    public async Task SaveTranscriptAsync_UpdatesExisting_WhenSelected()
    {
        _promptManagerMock.Setup(pm => pm.GetRecentAsync(null, default))
            .ReturnsAsync(new List<StoredPrompt>
            {
                new(5, "original", "file.wav", DateTime.Now, null),
            });

        await _manager.RefreshHistoryAsync();
        _manager.SelectHistoryItem(0);

        _promptManagerMock.Setup(pm => pm.UpdateAsync(5, "updated text", default))
            .ReturnsAsync(true);

        var result = await _manager.SaveTranscriptAsync("updated text", "file.wav");

        result.Should().Be(5);
        _manager.SelectedHistoryText.Should().Be("updated text");
        _promptManagerMock.Verify(pm => pm.UpdateAsync(5, "updated text", default), Times.Once);
    }

    [Fact]
    public async Task SaveTranscriptAsync_ReturnsNull_WhenEmptyText()
    {
        var result = await _manager.SaveTranscriptAsync("", "test.wav");

        result.Should().BeNull();
        _promptManagerMock.Verify(pm => pm.AddAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveTranscriptAsync_ReturnsNull_WhenWhitespaceOnly()
    {
        var result = await _manager.SaveTranscriptAsync("   ", "test.wav");

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSelectedAsync_RemovesFromPromptManager()
    {
        _promptManagerMock.Setup(pm => pm.GetRecentAsync(null, default))
            .ReturnsAsync(new List<StoredPrompt>
            {
                new(3, "to delete", "file.wav", DateTime.Now, null),
            });

        await _manager.RefreshHistoryAsync();
        _manager.SelectHistoryItem(0);

        _promptManagerMock.Setup(pm => pm.RemoveByIdAsync(3, default))
            .ReturnsAsync(true);

        var result = await _manager.DeleteSelectedAsync();

        result.Should().BeTrue();
        _manager.SelectedHistoryId.Should().BeNull();
        _manager.SelectedHistoryText.Should().BeEmpty();
        _promptManagerMock.Verify(pm => pm.RemoveByIdAsync(3, default), Times.Once);
    }

    [Fact]
    public async Task DeleteSelectedAsync_ReturnsFalse_WhenNothingSelected()
    {
        var result = await _manager.DeleteSelectedAsync();

        result.Should().BeFalse();
        _promptManagerMock.Verify(pm => pm.RemoveByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void SelectHistoryItem_UpdatesTranscriptView()
    {
        var prompts = new List<StoredPrompt>
        {
            new(1, "First prompt text", "a.wav", DateTime.Now, "Summary 1"),
            new(2, "Second prompt text", "b.wav", DateTime.Now, null),
        };

        typeof(HistoryManager).GetField("_prompts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(_manager, prompts);

        _manager.SelectHistoryItem(0);

        _manager.SelectedHistoryId.Should().Be(1);
        _manager.SelectedHistoryText.Should().Be("First prompt text");
        _manager.SelectedHistoryFilename.Should().Be("a.wav");
        _tui.TranscriptView.Text.ToString().Should().Contain("First prompt text");
    }

    [Fact]
    public void SelectHistoryItem_InvalidIndex_DoesNothing()
    {
        _manager.SelectHistoryItem(-1);
        _manager.SelectedHistoryId.Should().BeNull();

        _manager.SelectHistoryItem(99);
        _manager.SelectedHistoryId.Should().BeNull();
    }

    [Fact]
    public void IsAppendMode_DefaultFalse()
    {
        _manager.IsAppendMode.Should().BeFalse();
    }

    [Fact]
    public void IsAppendMode_CanBeSet()
    {
        _manager.IsAppendMode = true;
        _manager.IsAppendMode.Should().BeTrue();
    }
}
