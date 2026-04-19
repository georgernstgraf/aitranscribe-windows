using AITranscribe.Core.Configuration;
using AITranscribe.Core.Models;
using FluentAssertions;

namespace AITranscribe.Core.Tests.Configuration;

public class ConfigManagerTests : IDisposable
{
    private readonly string _tempDir;

    public ConfigManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"AITranscribeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    private string ConfigFilePath => Path.Combine(_tempDir, "config.json");

    private string PythonConfigDir => Path.Combine(_tempDir, "python");

    [Fact]
    public void Load_WhenNoFile_CreatesDefaultConfig()
    {
        var manager = new ConfigManager(ConfigFilePath, pythonConfigDir: PythonConfigDir);
        var config = manager.Load();

        config.Should().NotBeNull();
        config.Groq.ApiKey.Should().BeEmpty();
        config.Groq.SttModel.Should().Be("whisper-large-v3-turbo");
        File.Exists(ConfigFilePath).Should().BeTrue();
    }

    [Fact]
    public void Save_CreatesJsonFile()
    {
        var manager = new ConfigManager(ConfigFilePath, pythonConfigDir: PythonConfigDir);
        var config = AppConfig.CreateDefault();

        manager.Save(config);

        File.Exists(ConfigFilePath).Should().BeTrue();
        var json = File.ReadAllText(ConfigFilePath);
        json.Should().Contain("Groq");
        json.Should().Contain("whisper-large-v3-turbo");
    }

    [Fact]
    public void Load_AfterSave_RoundtripsSuccessfully()
    {
        var manager = new ConfigManager(ConfigFilePath, pythonConfigDir: PythonConfigDir);
        var original = new AppConfig(
            new GroqConfig("sk-test-key", "whisper-large-v3"),
            new LlmConfig("cohere", "command-r"),
            PreProcessMode.Cleanup,
            "file",
            @"C:\audio\meeting.wav",
            true,
            new OpenRouterConfig("or-key", "anthropic/claude-3-haiku"),
            new CohereConfig("co-key", "command-r"),
            new ZAiConfig("zai-key", "glm-5")
        );

        manager.Save(original);

        var loaded = manager.Load();

        loaded.Should().Be(original);
        loaded.Groq.ApiKey.Should().Be("sk-test-key");
        loaded.Llm.Provider.Should().Be("cohere");
        loaded.PreProcessMode.Should().Be(PreProcessMode.Cleanup);
        loaded.InputSource.Should().Be("file");
        loaded.LastFilePath.Should().Be(@"C:\audio\meeting.wav");
        loaded.Verbose.Should().BeTrue();
    }

    [Fact]
    public void MigrateFromPythonDotenv_ImportsValues()
    {
        Directory.CreateDirectory(PythonConfigDir);
        var dotenvPath = Path.Combine(PythonConfigDir, "config");
        var dotenvContent = """
            GROQ_API_KEY="my-groq-key"
            GROQ_STT_MODEL="whisper-large-v3"
            LLM_PROVIDER="cohere"
            COHERE_API_KEY="my-cohere-key"
            COHERE_LLM_MODEL="command-r-plus"
            PRE_PROCESS_MODE="raw"
            TRANSCRIBE_SOURCE="file"
            LAST_FILE_PATH="/home/user/audio.wav"
            VERBOSE_ERRORS="true"
            """;
        File.WriteAllText(dotenvPath, dotenvContent);

        var manager = new ConfigManager(ConfigFilePath, pythonConfigDir: PythonConfigDir);
        var config = manager.Load();

        config.Groq.ApiKey.Should().Be("my-groq-key");
        config.Groq.SttModel.Should().Be("whisper-large-v3");
        config.Llm.Provider.Should().Be("cohere");
        config.Cohere.ApiKey.Should().Be("my-cohere-key");
        config.Cohere.Model.Should().Be("command-r-plus");
        config.PreProcessMode.Should().Be(PreProcessMode.Raw);
        config.InputSource.Should().Be("file");
        config.LastFilePath.Should().Be("/home/user/audio.wav");
        config.Verbose.Should().BeTrue();
    }

    [Fact]
    public void MigrateFromPythonDotenv_IgnoresCommentedLines()
    {
        Directory.CreateDirectory(PythonConfigDir);
        var dotenvPath = Path.Combine(PythonConfigDir, "config");
        var dotenvContent = """
            GROQ_API_KEY="active-key"
            # COHERE_API_KEY="commented-key"
            """;
        File.WriteAllText(dotenvPath, dotenvContent);

        var manager = new ConfigManager(ConfigFilePath, pythonConfigDir: PythonConfigDir);
        var config = manager.Load();

        config.Groq.ApiKey.Should().Be("active-key");
        config.Cohere.ApiKey.Should().BeEmpty();
    }

    [Fact]
    public void MigrateFromPythonDotenv_OpenRouterKeysImported()
    {
        Directory.CreateDirectory(PythonConfigDir);
        var dotenvPath = Path.Combine(PythonConfigDir, "config");
        var dotenvContent = """
            OPENROUTER_API_KEY="my-or-key"
            OPENROUTER_LLM_MODEL="meta/llama-3"
            """;
        File.WriteAllText(dotenvPath, dotenvContent);

        var manager = new ConfigManager(ConfigFilePath, pythonConfigDir: PythonConfigDir);
        var config = manager.Load();

        config.OpenRouter.ApiKey.Should().Be("my-or-key");
        config.OpenRouter.Model.Should().Be("meta/llama-3");
    }

    [Fact]
    public void MigrateFromPythonDotenv_ZAiKeysImported()
    {
        Directory.CreateDirectory(PythonConfigDir);
        var dotenvPath = Path.Combine(PythonConfigDir, "config");
        var dotenvContent = """
            ZAI_API_KEY="my-zai-key"
            ZAI_LLM_MODEL="glm-5-plus"
            """;
        File.WriteAllText(dotenvPath, dotenvContent);

        var manager = new ConfigManager(ConfigFilePath, pythonConfigDir: PythonConfigDir);
        var config = manager.Load();

        config.ZAi.ApiKey.Should().Be("my-zai-key");
        config.ZAi.Model.Should().Be("glm-5-plus");
    }

    [Fact]
    public void EnsureExists_CreatesDirectoryAndFile()
    {
        var deepPath = Path.Combine(_tempDir, "nested", "dir", "config.json");
        var manager = new ConfigManager(deepPath, pythonConfigDir: PythonConfigDir);

        manager.EnsureExists();

        File.Exists(deepPath).Should().BeTrue();
        Directory.Exists(Path.GetDirectoryName(deepPath)).Should().BeTrue();
    }

    [Fact]
    public void Load_WhenExistingJsonFile_DoesNotOverwrite()
    {
        var manager1 = new ConfigManager(ConfigFilePath, pythonConfigDir: PythonConfigDir);
        var custom = new AppConfig(
            new GroqConfig("saved-key", "whisper-large-v3-turbo"),
            new LlmConfig("openrouter", "anthropic/claude-3-haiku"),
            PreProcessMode.Raw,
            "microphone",
            "",
            false,
            new OpenRouterConfig("or-saved", "anthropic/claude-3-haiku"),
            new CohereConfig("", "command-r"),
            new ZAiConfig("", "glm-5")
        );
        manager1.Save(custom);

        var manager2 = new ConfigManager(ConfigFilePath, pythonConfigDir: PythonConfigDir);
        var loaded = manager2.Load();

        loaded.Groq.ApiKey.Should().Be("saved-key");
        loaded.PreProcessMode.Should().Be(PreProcessMode.Raw);
    }

    [Fact]
    public void Save_JsonIsHierarchical()
    {
        var manager = new ConfigManager(ConfigFilePath, pythonConfigDir: PythonConfigDir);
        manager.Save(AppConfig.CreateDefault());

        var json = File.ReadAllText(ConfigFilePath);
        json.Should().Contain("\"Groq\"");
        json.Should().Contain("\"Llm\"");
        json.Should().Contain("\"OpenRouter\"");
        json.Should().Contain("\"Cohere\"");
        json.Should().Contain("\"ZAi\"");
    }
}
