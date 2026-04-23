using AITranscribe.Console;
using AITranscribe.Console.Tui;
using AITranscribe.Core.Data;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AITranscribe.Integration.Tests;

public class TuiSmokeTests : IDisposable
{
    private readonly CompositionRootTestHelper _helper;

    public TuiSmokeTests()
    {
        _helper = new CompositionRootTestHelper();
    }

    public void Dispose()
    {
        _helper?.Dispose();
    }

    [Fact(Skip = LiveTestConfig.SkipReason)]
    public void AITranscribeTui_ConstructsInMemory_WithoutApplicationInit()
    {
        if (!LiveTestConfig.IsLiveTest) return;

        var tui = _helper.ServiceProvider.GetRequiredService<AITranscribeTui>();

        tui.Should().NotBeNull();
        tui.CurrentState.Should().Be(TuiState.Idle);
    }

    [Fact(Skip = LiveTestConfig.SkipReason)]
    public void RecordingController_WiresCallbacks_ToAITranscribeTui()
    {
        if (!LiveTestConfig.IsLiveTest) return;

        var controller = _helper.ServiceProvider.GetRequiredService<RecordingController>();
        var tui = _helper.ServiceProvider.GetRequiredService<AITranscribeTui>();

        controller.OnStateChanged = state => tui.SetState(state);
        controller.OnFeedback = (stepId, status) => tui.SetFeedbackStep(stepId, status);
        controller.OnTranscriptUpdate = text => tui.TranscriptView.Text = text;

        controller.OnStateChanged.Should().NotBeNull();
        controller.OnFeedback.Should().NotBeNull();
        controller.OnTranscriptUpdate.Should().NotBeNull();

        controller.OnStateChanged.Invoke(TuiState.Recording);
        tui.CurrentState.Should().Be(TuiState.Recording);

        controller.OnFeedback.Invoke("compress", "active");
        tui.FeedbackStepLabels[0].Text.Should().Contain("active");

        controller.OnTranscriptUpdate.Invoke("test transcript");
        tui.TranscriptView.Text.ToString().Should().Contain("test transcript");
    }

    [Fact(Skip = LiveTestConfig.SkipReason)]
    public void TuiState_Transitions_IdleToRecordingToProcessingToIdle()
    {
        if (!LiveTestConfig.IsLiveTest) return;

        var tui = _helper.ServiceProvider.GetRequiredService<AITranscribeTui>();

        tui.CurrentState.Should().Be(TuiState.Idle);

        tui.SetState(TuiState.Recording);
        tui.CurrentState.Should().Be(TuiState.Recording);

        tui.SetState(TuiState.Processing);
        tui.CurrentState.Should().Be(TuiState.Processing);

        tui.SetState(TuiState.Idle);
        tui.CurrentState.Should().Be(TuiState.Idle);
    }

    [Fact(Skip = LiveTestConfig.SkipReason)]
    public void SetFeedbackStep_UpdatesCorrectLabelText()
    {
        if (!LiveTestConfig.IsLiveTest) return;

        var tui = _helper.ServiceProvider.GetRequiredService<AITranscribeTui>();

        tui.SetFeedbackStep("compress", "done");
        tui.FeedbackStepLabels[0].Text.Should().Contain("Compressing Message");
        tui.FeedbackStepLabels[0].Text.Should().Contain("done");

        tui.SetFeedbackStep("transcribe", "active");
        tui.FeedbackStepLabels[1].Text.Should().Contain("Transcribing Raw Message");
        tui.FeedbackStepLabels[1].Text.Should().Contain("active");

        tui.SetFeedbackStep("post_process", "failed");
        tui.FeedbackStepLabels[2].Text.Should().Contain("Post-Processing Message");
        tui.FeedbackStepLabels[2].Text.Should().Contain("failed");

        tui.SetFeedbackStep("summary", "active");
        tui.FeedbackStepLabels[3].Text.Should().Contain("Creating Summary");
        tui.FeedbackStepLabels[3].Text.Should().Contain("active");
    }

    [Fact(Skip = LiveTestConfig.SkipReason)]
    public void ResetFeedbackSteps_ResetsAllLabelsToPending()
    {
        if (!LiveTestConfig.IsLiveTest) return;

        var tui = _helper.ServiceProvider.GetRequiredService<AITranscribeTui>();

        tui.SetFeedbackStep("compress", "done");
        tui.SetFeedbackStep("transcribe", "done");
        tui.SetFeedbackStep("post_process", "active");
        tui.SetFeedbackStep("summary", "failed");

        foreach (var label in tui.FeedbackStepLabels)
        {
            label.Text.Should().NotContain("pending");
        }

        tui.ResetFeedbackSteps();

        foreach (var label in tui.FeedbackStepLabels)
        {
            label.Text.Should().Contain("pending");
        }
    }

    [Fact(Skip = LiveTestConfig.SkipReason)]
    public async Task HistoryManager_CRUD_Works_WithRealSQLite()
    {
        if (!LiveTestConfig.IsLiveTest) return;

        var promptManager = _helper.ServiceProvider.GetRequiredService<IPromptManager>();
        await ((PromptManager)promptManager).InitializeAsync();

        var tui = _helper.ServiceProvider.GetRequiredService<AITranscribeTui>();
        var historyManager = new HistoryManager(promptManager, tui);

        await historyManager.RefreshHistoryAsync();
        historyManager.Prompts.Should().BeEmpty();

        var id = await historyManager.SaveTranscriptAsync("Test transcript one", "test1.wav");
        id.Should().BeGreaterThan(0);
        historyManager.SelectedHistoryId.Should().Be(id);

        id = await historyManager.SaveTranscriptAsync("Test transcript two", "test2.wav");
        id.Should().BeGreaterThan(0);

        await historyManager.RefreshHistoryAsync();
        historyManager.Prompts.Should().HaveCount(2);

        historyManager.SelectHistoryItem(0);
        historyManager.SelectedHistoryText.Should().NotBeEmpty();

        var deleted = await historyManager.DeleteSelectedAsync();
        deleted.Should().BeTrue();

        await historyManager.RefreshHistoryAsync();
        historyManager.Prompts.Should().HaveCount(1);
    }

    [Fact(Skip = LiveTestConfig.SkipReason)]
    public void ClipboardHelper_DoesNotCrash_WhenSetText()
    {
        if (!LiveTestConfig.IsLiveTest) return;

        var act = () => ClipboardHelper.SetText("test clipboard text");

        act.Should().NotThrow();
    }

    [Fact(Skip = LiveTestConfig.SkipReason)]
    public void RecordingController_InitialState_IsIdle()
    {
        if (!LiveTestConfig.IsLiveTest) return;

        var controller = _helper.ServiceProvider.GetRequiredService<RecordingController>();

        controller.State.Should().Be(TuiState.Idle);
        controller.IsProcessing.Should().BeFalse();
    }
}