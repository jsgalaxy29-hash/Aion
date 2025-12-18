using System.Threading;
using System.Threading.Tasks;

namespace Aion.AI.Abstractions;

/// <summary>
/// Abstraction over an LLM provider.
/// </summary>
public interface ILanguageModelClient
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
}
