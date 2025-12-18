using System.Threading;
using System.Threading.Tasks;
using Aion.AI.Abstractions;

namespace Aion.AI.Services;

/// <summary>
/// Deterministic mock for integration tests.
/// </summary>
public sealed class MockLanguageModelClient : ILanguageModelClient
{
    public Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        return Task.FromResult($"MOCK_RESPONSE::{userPrompt}");
    }
}
