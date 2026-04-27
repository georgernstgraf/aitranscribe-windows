using AITranscribe.Console.Tui;
using FluentAssertions;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace AITranscribe.Console.Tests.Tui;

public class AITranscribeTuiTests
{
    private readonly AITranscribeTui _tui;

    public AITranscribeTuiTests()
    {
        _tui = new AITranscribeTui();
    }

    [Fact]
    public void AITranscribeTui_HasTranscriptView()
    {
        _tui.TranscriptView.Should().NotBeNull();
        _tui.TranscriptView.Should().BeOfType<TextView>();
    }

    [Fact]
    public void AITranscribeTui_HasHistoryList()
    {
        _tui.HistoryList.Should().NotBeNull();
        _tui.HistoryList.Should().BeOfType<ListView>();
    }

    [Fact]
    public void AITranscribeTui_HasStatusPanel()
    {
        _tui.StatusLabel.Should().NotBeNull();
        _tui.StatusLabel.Should().BeOfType<Label>();
        _tui.FlashLabel.Should().NotBeNull();
        _tui.FlashLabel.Should().BeOfType<Label>();
    }

    [Fact]
    public void AITranscribeTui_HasFeedbackSteps()
    {
        _tui.FeedbackStepLabels.Should().NotBeNull();
        _tui.FeedbackStepLabels.Should().HaveCount(4);
        _tui.FeedbackStepLabels.Should().AllBeAssignableTo<Label>();
    }

    [Fact]
    public void AITranscribeTui_InitialState_IsIdle()
    {
        _tui.CurrentState.Should().Be(TuiState.Idle);
    }

    [Fact]
    public void AITranscribeTui_HasConfigWidgets()
    {
        _tui.SourceRadioGroup.Should().NotBeNull();
        _tui.SourceRadioGroup.Should().BeOfType<OptionSelector>();
        _tui.PreprocessRadioGroup.Should().NotBeNull();
        _tui.PreprocessRadioGroup.Should().BeOfType<OptionSelector>();
        _tui.FilePathField.Should().NotBeNull();
        _tui.FilePathField.Should().BeOfType<TextField>();
        _tui.SttModelField.Should().NotBeNull();
        _tui.SttModelField.Should().BeOfType<TextField>();
        _tui.LlmModelField.Should().NotBeNull();
        _tui.LlmModelField.Should().BeOfType<TextField>();
    }

    [Fact]
    public void AITranscribeTui_SetState_UpdatesCurrentState()
    {
        _tui.SetState(TuiState.Recording);
        _tui.CurrentState.Should().Be(TuiState.Recording);

        _tui.SetState(TuiState.Processing);
        _tui.CurrentState.Should().Be(TuiState.Processing);

        _tui.SetState(TuiState.Idle);
        _tui.CurrentState.Should().Be(TuiState.Idle);
    }

    [Fact]
    public void AITranscribeTui_ToggleRecording_IdleToRecording()
    {
        _tui.OnToggleRecordingRequested = () =>
        {
            if (_tui.CurrentState == TuiState.Idle)
                _tui.SetState(TuiState.Recording);
            else if (_tui.CurrentState == TuiState.Recording)
                _tui.SetState(TuiState.Processing);
        };
        _tui.ToggleRecording();
        _tui.CurrentState.Should().Be(TuiState.Recording);
    }

    [Fact]
    public void AITranscribeTui_ToggleRecording_RecordingToProcessing()
    {
        _tui.OnToggleRecordingRequested = () =>
        {
            if (_tui.CurrentState == TuiState.Idle)
                _tui.SetState(TuiState.Recording);
            else if (_tui.CurrentState == TuiState.Recording)
                _tui.SetState(TuiState.Processing);
        };
        _tui.SetState(TuiState.Recording);
        _tui.ToggleRecording();
        _tui.CurrentState.Should().Be(TuiState.Processing);
    }

    [Fact]
    public void AITranscribeTui_SetFeedbackStep_UpdatesLabel()
    {
        _tui.SetFeedbackStep("compress", "done");
        _tui.FeedbackStepLabels[0].Text.Should().Contain("[x]");

        _tui.SetFeedbackStep("transcribe", "active");
        _tui.FeedbackStepLabels[1].Text.Should().Contain("[>]");

        _tui.SetFeedbackStep("post_process", "failed");
        _tui.FeedbackStepLabels[2].Text.Should().Contain("[!]");
    }

    [Fact]
    public void AITranscribeTui_ResetFeedbackSteps_SetsAllToPending()
    {
        _tui.SetFeedbackStep("compress", "done");
        _tui.SetFeedbackStep("transcribe", "done");

        _tui.ResetFeedbackSteps();

        foreach (var label in _tui.FeedbackStepLabels)
        {
            label.Text.Should().Contain("[ ]");
        }
    }

    [Fact]
    public void AITranscribeTui_StatusLabel_ContainsActivityInfo()
    {
        _tui.StatusLabel.Text.ToString().Should().Contain("Ready");

        _tui.SetState(TuiState.Recording);
        _tui.StatusLabel.Text.ToString().Should().Contain("Recording");
    }

    [Fact]
    public void AITranscribeTui_HasHelpBar()
    {
        _tui.HelpBar.Should().NotBeNull();
        _tui.HelpBar.Should().BeOfType<Label>();
    }

    [Fact]
    public void AITranscribeTui_HasHistorySubtitleLabel()
    {
        _tui.HistorySubtitleLabel.Should().NotBeNull();
        _tui.HistorySubtitleLabel.Should().BeOfType<Label>();
    }

    [Fact]
    public void AITranscribeTui_UpdateHistorySubtitle_SetsText()
    {
        _tui.UpdateHistorySubtitle(42);
        _tui.HistorySubtitleLabel.Text.Should().Contain("Stored: 42");
        _tui.HistorySubtitleLabel.Text.Should().Contain("Arrows to preview");
    }

    [Fact]
    public void AITranscribeTui_Translate_InvokesCallback()
    {
        var called = false;
        var language = "";
        _tui.OnTranslateRequested = lang => { called = true; language = lang; };

        _tui.Translate("german");

        called.Should().BeTrue();
        language.Should().Be("german");
    }

    [Fact]
    public void AITranscribeTui_WriteIssueFile_InvokesCallback()
    {
        var called = false;
        _tui.OnWriteIssueRequested = () => called = true;

        _tui.WriteIssueFile();

        called.Should().BeTrue();
    }

    [Fact]
    public void AITranscribeTui_DeleteSelected_InvokesCallback()
    {
        var called = false;
        _tui.OnDeleteRequested = () => called = true;

        _tui.DeleteSelected();

        called.Should().BeTrue();
    }
}
