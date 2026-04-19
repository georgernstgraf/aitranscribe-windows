using AITranscribe.Core.Data;
using FluentAssertions;
using Microsoft.Data.Sqlite;

namespace AITranscribe.Core.Tests.Data;

public class PromptManagerTests : IDisposable
{
    private readonly string _dbPath;

    public PromptManagerTests()
    {
        _dbPath = Path.GetTempFileName();
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    private PromptManager CreateManager() => new(_dbPath);

    private async Task<PromptManager> CreateAndInitManager()
    {
        var manager = CreateManager();
        await manager.InitializeAsync();
        return manager;
    }

    [Fact]
    public async Task InitializeDb_CreatesTableWithCorrectSchema()
    {
        var manager = CreateManager();
        await manager.InitializeAsync();

        using var conn = new SqliteConnection($"Data Source={_dbPath};Pooling=False");
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA table_info(prompts)";
        var columns = new List<string>();
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(1));
            }
        }
        conn.Close();

        columns.Should().BeEquivalentTo(["id", "prompt", "filename", "created_at", "summary"]);
    }

    [Fact]
    public async Task AddAsync_ReturnsId()
    {
        var manager = await CreateAndInitManager();

        var id = await manager.AddAsync("Hello world", "test.wav", null);

        id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectPrompt()
    {
        var manager = await CreateAndInitManager();
        var id = await manager.AddAsync("test prompt", "audio.wav", "a summary");

        var result = await manager.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Prompt.Should().Be("test prompt");
        result.Filename.Should().Be("audio.wav");
        result.Summary.Should().Be("a summary");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllPrompts()
    {
        var manager = await CreateAndInitManager();
        await manager.AddAsync("prompt 1", "f1.wav", null);
        await manager.AddAsync("prompt 2", "f2.wav", null);

        var all = await manager.GetAllAsync();

        all.Should().HaveCount(2);
        all[0].Prompt.Should().Be("prompt 1");
        all[1].Prompt.Should().Be("prompt 2");
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsOrderedByDate()
    {
        var manager = await CreateAndInitManager();
        await manager.AddAsync("first", "a.wav", null);
        await manager.AddAsync("second", "b.wav", null);
        await manager.AddAsync("third", "c.wav", null);

        var recent = await manager.GetRecentAsync(2);

        recent.Should().HaveCount(2);
        recent[0].Prompt.Should().Be("third");
        recent[1].Prompt.Should().Be("second");
    }

    [Fact]
    public async Task UpdateAsync_ModifiesText()
    {
        var manager = await CreateAndInitManager();
        var id = await manager.AddAsync("original", "file.wav", null);

        var updated = await manager.UpdateAsync(id, "modified");

        updated.Should().BeTrue();
        var prompt = await manager.GetByIdAsync(id);
        prompt!.Prompt.Should().Be("modified");
    }

    [Fact]
    public async Task UpdateSummaryAsync_SetsSummary()
    {
        var manager = await CreateAndInitManager();
        var id = await manager.AddAsync("prompt", "file.wav", null);

        var updated = await manager.UpdateSummaryAsync(id, "new summary");

        updated.Should().BeTrue();
        var prompt = await manager.GetByIdAsync(id);
        prompt!.Summary.Should().Be("new summary");
    }

    [Fact]
    public async Task RemoveByIdAsync_DeletesPrompt()
    {
        var manager = await CreateAndInitManager();
        var id = await manager.AddAsync("to delete", "file.wav", null);

        var removed = await manager.RemoveByIdAsync(id);

        removed.Should().BeTrue();
        var prompt = await manager.GetByIdAsync(id);
        prompt.Should().BeNull();
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        var manager = await CreateAndInitManager();
        await manager.AddAsync("p1", "f1.wav", null);
        await manager.AddAsync("p2", "f2.wav", null);
        await manager.AddAsync("p3", "f3.wav", null);

        var count = await manager.CountAsync();

        count.Should().Be(3);
    }
}
