using Microsoft.Data.Sqlite;

namespace AITranscribe.Core.Data;

public class PromptManager : IPromptManager
{
    private readonly string _dbPath;

    public PromptManager(string dbPath)
    {
        _dbPath = dbPath;
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    private string ConnectionString => $"Data Source={_dbPath};Pooling=False";

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=DELETE";
        await cmd.ExecuteNonQueryAsync(ct);

        cmd.CommandText = "PRAGMA table_info(prompts)";
        var columns = new List<(int cid, string name)>();
        using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                columns.Add((reader.GetInt32(0), reader.GetString(1)));
            }
        }

        if (columns.Count == 0)
        {
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS prompts (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    prompt TEXT NOT NULL,
                    filename TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    summary TEXT DEFAULT NULL
                )
                """;
            await cmd.ExecuteNonQueryAsync(ct);
        }
        else if (columns.Any(c => c.name == "played_count"))
        {
            cmd.CommandText = "ALTER TABLE prompts RENAME TO prompts_legacy";
            await cmd.ExecuteNonQueryAsync(ct);
            cmd.CommandText = """
                CREATE TABLE prompts (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    prompt TEXT NOT NULL,
                    filename TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    summary TEXT DEFAULT NULL
                )
                """;
            await cmd.ExecuteNonQueryAsync(ct);
            cmd.CommandText = """
                INSERT INTO prompts (id, prompt, filename, created_at, summary)
                SELECT id, prompt, filename, created_at, NULL
                FROM prompts_legacy
                ORDER BY id ASC
                """;
            await cmd.ExecuteNonQueryAsync(ct);
            cmd.CommandText = "DROP TABLE prompts_legacy";
            await cmd.ExecuteNonQueryAsync(ct);
        }
        else if (!columns.Any(c => c.name == "summary"))
        {
            cmd.CommandText = "ALTER TABLE prompts ADD COLUMN summary TEXT DEFAULT NULL";
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    public async Task<long> AddAsync(string prompt, string filename, string? summary, CancellationToken ct = default)
    {
        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO prompts (prompt, filename, created_at, summary)
            VALUES (@prompt, @filename, @createdAt, @summary);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("@prompt", prompt);
        cmd.Parameters.AddWithValue("@filename", filename);
        cmd.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("o"));
        cmd.Parameters.AddWithValue("@summary", (object?)summary ?? DBNull.Value);
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result);
    }

    public async Task<StoredPrompt?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, prompt, filename, created_at, summary FROM prompts WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return ReadPrompt(reader);
        }
        return null;
    }

    public async Task<IReadOnlyList<StoredPrompt>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, prompt, filename, created_at, summary FROM prompts ORDER BY created_at ASC, id ASC";
        var results = new List<StoredPrompt>();
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(ReadPrompt(reader));
        }
        return results;
    }

    public async Task<IReadOnlyList<StoredPrompt>> GetRecentAsync(int? limit, CancellationToken ct = default)
    {
        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        var sql = "SELECT id, prompt, filename, created_at, summary FROM prompts ORDER BY created_at DESC, id DESC";
        if (limit.HasValue)
        {
            sql += " LIMIT @limit";
            cmd.Parameters.AddWithValue("@limit", limit.Value);
        }
        cmd.CommandText = sql;
        var results = new List<StoredPrompt>();
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(ReadPrompt(reader));
        }
        return results;
    }

    public async Task<bool> UpdateAsync(long id, string prompt, CancellationToken ct = default)
    {
        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE prompts SET prompt = @prompt WHERE id = @id";
        cmd.Parameters.AddWithValue("@prompt", prompt);
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<bool> UpdateSummaryAsync(long id, string summary, CancellationToken ct = default)
    {
        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE prompts SET summary = @summary WHERE id = @id";
        cmd.Parameters.AddWithValue("@summary", summary);
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<bool> RemoveByIdAsync(long id, CancellationToken ct = default)
    {
        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM prompts WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM prompts";
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result);
    }

    private static StoredPrompt ReadPrompt(SqliteDataReader reader)
    {
        return new StoredPrompt(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.GetString(2),
            DateTime.Parse(reader.GetString(3)),
            reader.IsDBNull(4) ? null : reader.GetString(4)
        );
    }
}
