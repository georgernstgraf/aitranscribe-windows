using AITranscribe.Core.Configuration;
using AITranscribe.Core.Data;
using AITranscribe.Core.Models;
using AITranscribe.Core.Services;
using AITranscribe.Console.Tui;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AITranscribe.Integration.Tests;

public class PipelineIntegrationTests
{
    [Fact(Skip = LiveTestConfig.SkipReason)]
    public async Task ProcessFileAsync_RawMode_StoresTranscription()
    {
        if (!LiveTestConfig.IsLiveTest) return;

        var config = LiveTestConfig.LoadConfig();
        LiveTestConfig.HasRequiredApiKeys(config).Should().BeTrue("Groq API key required");

        using var helper = new CompositionRootTestHelper();
        var svc = helper.ServiceProvider.GetRequiredService<TranscriptionService>();
        var pm = helper.ServiceProvider.GetRequiredService<IPromptManager>();
        await pm.InitializeAsync();

        var settings = new TranscriptionSettings(
            PreProcessMode.Raw, config.Groq.SttModel, "", "", "",
            false, "", null, false);

        var result = await svc.ProcessFileAsync(
            TestFixturePaths.TrumpMp3, settings, null, null);

        result.Text.Should().NotBeNullOrWhiteSpace();
        result.Id.Should().BeGreaterThan(0);

        var stored = await pm.GetByIdAsync(result.Id);
        stored.Should().NotBeNull();
        stored!.Prompt.Should().Be(result.Text);
    }

    [Fact(Skip = LiveTestConfig.SkipReason)]
    public async Task ProcessFileAsync_CleanupMode_StoresTranscription()
    {
        if (!LiveTestConfig.IsLiveTest) return;

        var config = LiveTestConfig.LoadConfig();
        LiveTestConfig.HasRequiredApiKeys(config).Should().BeTrue("Groq API key required");

        using var helper = new CompositionRootTestHelper();
        var svc = helper.ServiceProvider.GetRequiredService<TranscriptionService>();
        var pm = helper.ServiceProvider.GetRequiredService<IPromptManager>();
        await pm.InitializeAsync();

        var (model, baseUrl, apiKey) = TuiOrchestrator.ResolveLlmFromConfig(config);
        string.IsNullOrEmpty(apiKey).Should().BeFalse("LLM API key required for Cleanup mode");

        var settings = new TranscriptionSettings(
            PreProcessMode.Cleanup, config.Groq.SttModel, model, baseUrl, apiKey,
            false, "", null, false);

        var result = await svc.ProcessFileAsync(
            TestFixturePaths.TrumpMp3, settings, null, null);

        result.Text.Should().NotBeNullOrWhiteSpace();
        result.Id.Should().BeGreaterThan(0);

        var stored = await pm.GetByIdAsync(result.Id);
        stored.Should().NotBeNull();
        stored!.Prompt.Should().Be(result.Text);
    }

    [Fact(Skip = LiveTestConfig.SkipReason)]
    public async Task ProcessFileAsync_EnglishMode_StoresTranscription()
    {
        if (!LiveTestConfig.IsLiveTest) return;

        var config = LiveTestConfig.LoadConfig();
        LiveTestConfig.HasRequiredApiKeys(config).Should().BeTrue("Groq API key required");

        using var helper = new CompositionRootTestHelper();
        var svc = helper.ServiceProvider.GetRequiredService<TranscriptionService>();
        var pm = helper.ServiceProvider.GetRequiredService<IPromptManager>();
        await pm.InitializeAsync();

        var (model, baseUrl, apiKey) = TuiOrchestrator.ResolveLlmFromConfig(config);
        string.IsNullOrEmpty(apiKey).Should().BeFalse("LLM API key required for English mode");

        var settings = new TranscriptionSettings(
            PreProcessMode.English, config.Groq.SttModel, model, baseUrl, apiKey,
            false, "", null, false);

        var result = await svc.ProcessFileAsync(
            TestFixturePaths.TrumpMp3, settings, null, null);

        result.Text.Should().NotBeNullOrWhiteSpace();
        result.Id.Should().BeGreaterThan(0);

        var stored = await pm.GetByIdAsync(result.Id);
        stored.Should().NotBeNull();
        stored!.Prompt.Should().Be(result.Text);
    }

    [Fact(Skip = LiveTestConfig.SkipReason)]
    public async Task ProcessFileAsync_RawDiffersFromCleanup()
    {
        if (!LiveTestConfig.IsLiveTest) return;

        var config = LiveTestConfig.LoadConfig();
        LiveTestConfig.HasRequiredApiKeys(config).Should().BeTrue("Groq API key required");

        var (model, baseUrl, apiKey) = TuiOrchestrator.ResolveLlmFromConfig(config);
        if (string.IsNullOrEmpty(apiKey)) return;

        using var helper1 = new CompositionRootTestHelper();
        var svc1 = helper1.ServiceProvider.GetRequiredService<TranscriptionService>();
        var pm1 = helper1.ServiceProvider.GetRequiredService<IPromptManager>();
        await pm1.InitializeAsync();

        var rawSettings = new TranscriptionSettings(
            PreProcessMode.Raw, config.Groq.SttModel, "", "", "",
            false, "", null, false);

        var rawResult = await svc1.ProcessFileAsync(
            TestFixturePaths.TrumpMp3, rawSettings, null, null);

        using var helper2 = new CompositionRootTestHelper();
        var svc2 = helper2.ServiceProvider.GetRequiredService<TranscriptionService>();
        var pm2 = helper2.ServiceProvider.GetRequiredService<IPromptManager>();
        await pm2.InitializeAsync();

        var cleanupSettings = new TranscriptionSettings(
            PreProcessMode.Cleanup, config.Groq.SttModel, model, baseUrl, apiKey,
            false, "", null, false);

        var cleanupResult = await svc2.ProcessFileAsync(
            TestFixturePaths.TrumpMp3, cleanupSettings, null, null);

        rawResult.Text.Should().NotBeNullOrWhiteSpace();
        cleanupResult.Text.Should().NotBeNullOrWhiteSpace();
        rawResult.Text.Should().NotBe(cleanupResult.Text,
            "LLM cleanup post-processing should alter the raw STT output");
    }
}
