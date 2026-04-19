namespace AITranscribe.Core.Data;

public interface IPromptManager
{
    Task InitializeAsync(CancellationToken ct = default);
    Task<long> AddAsync(string prompt, string filename, string? summary, CancellationToken ct = default);
    Task<StoredPrompt?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<StoredPrompt>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StoredPrompt>> GetRecentAsync(int? limit, CancellationToken ct = default);
    Task<bool> UpdateAsync(long id, string prompt, CancellationToken ct = default);
    Task<bool> UpdateSummaryAsync(long id, string summary, CancellationToken ct = default);
    Task<bool> RemoveByIdAsync(long id, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}
