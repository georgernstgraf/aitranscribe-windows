using AITranscribe.Console.Commands;
using AITranscribe.Core.Configuration;
using AITranscribe.Core.Data;
using AITranscribe.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace AITranscribe.Integration.Tests;

internal sealed class FakeRemainingArguments : IRemainingArguments
{
    public ILookup<string, string?> Parsed => Array.Empty<KeyValuePair<string, string?>>()
        .ToLookup(k => k.Key, k => k.Value);
    public IReadOnlyList<string> Raw => [];
}

public class CliIntegrationTests : IDisposable
{
    private readonly CompositionRootTestHelper _helper;

    public CliIntegrationTests()
    {
        _helper = new CompositionRootTestHelper();
        TranscribeCommand.Services = _helper.ServiceProvider;
    }

    public void Dispose()
    {
        TranscribeCommand.Services = null;
        _helper?.Dispose();
    }

    [Fact(Skip = LiveTestConfig.SkipReason)]
    public async Task ExecuteFile_Returns0_AndStoresTranscription()
    {
        if (!LiveTestConfig.IsLiveTest) return;

        var config = LiveTestConfig.LoadConfig();
        LiveTestConfig.HasRequiredApiKeys(config).Should().BeTrue("Groq API key required");

        var pm = _helper.ServiceProvider.GetRequiredService<IPromptManager>();
        await ((PromptManager)pm).InitializeAsync();

        TranscribeCommand.Services = _helper.ServiceProvider;

        var cmd = new TranscribeCommand();
        var settings = new TranscribeSettings { File = TestFixturePaths.TrumpMp3 };

        var result = cmd.Execute(new CommandContext([], new FakeRemainingArguments(), "", null), settings);

        result.Should().Be(0);

        var all = await pm.GetAllAsync();
        all.Should().HaveCountGreaterThan(0);
        all[0].Prompt.Should().NotBeNullOrWhiteSpace();
    }

    [Fact(Skip = LiveTestConfig.SkipReason)]
    public async Task ExecuteList_Returns0_WithPopulatedDB()
    {
        if (!LiveTestConfig.IsLiveTest) return;

        var pm = _helper.ServiceProvider.GetRequiredService<IPromptManager>();
        await ((PromptManager)pm).InitializeAsync();
        await pm.AddAsync("Test prompt 1", "file1.wav", null);
        await pm.AddAsync("Test prompt 2", "file2.wav", "Summary 2");

        TranscribeCommand.Services = _helper.ServiceProvider;

        var cmd = new TranscribeCommand();
        var settings = new TranscribeSettings { ListPrompts = true };

        var result = cmd.Execute(new CommandContext([], new FakeRemainingArguments(), "", null), settings);

        result.Should().Be(0);
    }

    [Fact(Skip = LiveTestConfig.SkipReason)]
    public async Task ExecuteQuery_Returns0_AndReturnsOldestPrompt()
    {
        if (!LiveTestConfig.IsLiveTest) return;

        var pm = _helper.ServiceProvider.GetRequiredService<IPromptManager>();
        await ((PromptManager)pm).InitializeAsync();
        await pm.AddAsync("First prompt", "file1.wav", null);
        await pm.AddAsync("Second prompt", "file2.wav", null);

        TranscribeCommand.Services = _helper.ServiceProvider;

        var cmd = new TranscribeCommand();
        var settings = new TranscribeSettings { QueryPrompt = true };

        var result = cmd.Execute(new CommandContext([], new FakeRemainingArguments(), "", null), settings);

        result.Should().Be(0);

        var remaining = await pm.GetAllAsync();
        remaining.Should().HaveCount(1);
        remaining[0].Prompt.Should().Be("First prompt");
    }

    [Fact(Skip = LiveTestConfig.SkipReason)]
    public async Task ExecuteRemove_Returns0_AndRemovesPrompt()
    {
        if (!LiveTestConfig.IsLiveTest) return;

        var pm = _helper.ServiceProvider.GetRequiredService<IPromptManager>();
        await ((PromptManager)pm).InitializeAsync();
        await pm.AddAsync("Prompt to remove", "file1.wav", null);

        var all = await pm.GetAllAsync();
        all.Should().HaveCount(1);

        TranscribeCommand.Services = _helper.ServiceProvider;

        var cmd = new TranscribeCommand();
        var settings = new TranscribeSettings { RemovePrompt = 1 };

        var result = cmd.Execute(new CommandContext([], new FakeRemainingArguments(), "", null), settings);

        result.Should().Be(0);

        var remaining = await pm.GetAllAsync();
        remaining.Should().BeEmpty();
    }
}