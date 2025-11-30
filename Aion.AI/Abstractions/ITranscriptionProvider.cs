using System.Threading;
using System.Threading.Tasks;
using Aion.AI.Models;

namespace Aion.AI.Abstractions;

/// <summary>
/// Abstraction for transcribing audio into text.
/// </summary>
public interface ITranscriptionProvider
{
    Task<TranscriptionResponse> TranscribeAsync(TranscriptionRequest request, CancellationToken ct = default);
}
