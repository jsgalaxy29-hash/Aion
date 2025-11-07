using System.Threading;
using System.Threading.Tasks;
using Aion.AI.Models;

namespace Aion.AI.Abstractions;

/// <summary>
/// Classifies a natural language request into high level intents.
/// </summary>
public interface IIntentRecognizer
{
    Task<IntentRecognitionResult> RecognizeAsync(string requestText, CancellationToken ct = default);
}
