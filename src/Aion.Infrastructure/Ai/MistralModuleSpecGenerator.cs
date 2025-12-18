using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.Contracts;
using Aion.Domain.ModuleBuilder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aion.Infrastructure.Ai;

/// <summary>
/// IAiModuleSpecGenerator implementation backed by the Mistral community chat completion API.
/// </summary>
public sealed class MistralModuleSpecGenerator : IAiModuleSpecGenerator
{
    private const string DefaultEndpoint = "/v1/chat/completions";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly HttpClient _httpClient;
    private readonly MistralOptions _options;
    private readonly ILogger<MistralModuleSpecGenerator> _logger;

    public MistralModuleSpecGenerator(HttpClient httpClient, IOptions<MistralOptions> options, ILogger<MistralModuleSpecGenerator> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AiModuleBlueprint> GenerateAsync(string naturalLanguagePrompt, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(naturalLanguagePrompt);

        if (!_options.IsConfigured(out var reason))
        {
            throw new InvalidOperationException(reason);
        }

        var request = new MistralChatRequest
        {
            Model = _options.Model,
            Temperature = 0.2,
            Messages =
            [
                new MistralChatMessage
                {
                    Role = "system",
                    Content = BuildSystemPrompt()
                },
                new MistralChatMessage
                {
                    Role = "user",
                    Content = naturalLanguagePrompt
                }
            ]
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildEndpointUri())
        {
            Content = new StringContent(JsonSerializer.Serialize(request, SerializerOptions), Encoding.UTF8, "application/json")
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Mistral API returned {Status}: {Body}", response.StatusCode, payload);
            throw new InvalidOperationException($"Mistral API returned {(int)response.StatusCode}: {response.ReasonPhrase}");
        }

        var chatResponse = JsonSerializer.Deserialize<MistralChatResponse>(payload, SerializerOptions)
            ?? throw new InvalidOperationException("Unable to deserialize Mistral response.");

        var completionJson = ExtractCompletionJson(chatResponse);
        var dto = JsonSerializer.Deserialize<AiModuleSpecDto>(completionJson, SerializerOptions)
            ?? throw new InvalidOperationException("Mistral returned an empty module specification.");

        return dto.ToBlueprint(naturalLanguagePrompt, completionJson);
    }

    private string BuildEndpointUri()
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        return string.Concat(baseUrl, DefaultEndpoint);
    }

    private static string BuildSystemPrompt()
    {
        var builder = new StringBuilder();
        builder.AppendLine("You are an assistant that designs ERP modules for a system called \"Aion\".");
        builder.AppendLine("Aion uses metadata-driven modules with tables and fields.");
        builder.AppendLine("Your task is to read a natural language description of a business module and respond ONLY with a JSON structure describing the module.");
        builder.AppendLine("The JSON MUST strictly follow this schema: { moduleName, description, tables: [ { technicalName, displayName, description, fields: [ { technicalName, displayName, dataType, isPrimaryKey, isRequired, isUnique, maxLength?, foreignKeyTargetTable?, foreignKeyTargetField? } ] } ] }.");
        builder.AppendLine("Do not add explanations, comments or extra text. Return ONLY valid JSON.");
        builder.AppendLine("\nTu es un assistant qui conçoit des modules ERP pour le système \"Aion\".");
        builder.AppendLine("Aion est piloté par métadonnées avec des tables et des champs.");
        builder.AppendLine("Lis la description fonctionnelle fournie et réponds UNIQUEMENT avec un JSON conforme au schéma décrit.");
        builder.AppendLine("Tu dois répondre uniquement en JSON, sans texte autour. Le JSON doit pouvoir être directement désérialisé par System.Text.Json en C#.");
        return builder.ToString();
    }

    private static string ExtractCompletionJson(MistralChatResponse response)
    {
        var text = response.Choices?
            .OrderBy(c => c.Index)
            .Select(c => c.Message?.GetTextContent())
            .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Mistral returned an empty completion body.");
        }

        var trimmed = text.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLine = trimmed.IndexOf('\n');
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewLine >= 0 && lastFence > firstNewLine)
            {
                trimmed = trimmed[(firstNewLine + 1)..lastFence];
            }
        }

        return trimmed.Trim();
    }
}
