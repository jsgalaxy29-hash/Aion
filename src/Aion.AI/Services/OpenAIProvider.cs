using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aion.AI.Abstractions;
using Aion.AI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aion.AI.Services;

public sealed class OpenAIProvider : ILanguageModelClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAIProvider> _logger;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public OpenAIProvider(HttpClient httpClient, IOptions<LanguageModelOptions> options, ILogger<OpenAIProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value.OpenAI;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_options.DefaultBaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        if (!string.IsNullOrWhiteSpace(_options.Organization))
        {
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", _options.Organization);
        }
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API key is missing.");
        }

        var payload = new
        {
            model = _options.DefaultModel,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };
        var serializedPayload = JsonSerializer.Serialize(payload, SerializerOptions);

        for (var attempt = 0; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                using var content = new StringContent(serializedPayload, Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync("/v1/chat/completions", content, ct);
                var body = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    var message = $"OpenAI error {(int)response.StatusCode}: {body}";
                    if (attempt < _options.MaxRetries && IsTransient(response.StatusCode))
                    {
                        await DelayAsync(attempt, ct);
                        continue;
                    }

                    throw new HttpRequestException(message);
                }

                var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(body, SerializerOptions);
                var text = completion?.Choices?[0]?.Message?.Content;

                if (string.IsNullOrWhiteSpace(text))
                {
                    throw new InvalidOperationException("OpenAI returned an empty response.");
                }

                return text!;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested && attempt < _options.MaxRetries)
            {
                await DelayAsync(attempt, ct);
            }
            catch (HttpRequestException ex) when (attempt < _options.MaxRetries)
            {
                _logger.LogWarning(ex, "Retrying OpenAI request after failure on attempt {Attempt}", attempt + 1);
                await DelayAsync(attempt, ct);
            }
        }

        throw new InvalidOperationException("Unable to complete OpenAI request after retries.");
    }

    private static bool IsTransient(System.Net.HttpStatusCode statusCode)
    {
        return statusCode is System.Net.HttpStatusCode.RequestTimeout or >= System.Net.HttpStatusCode.InternalServerError;
    }

    private Task DelayAsync(int attempt, CancellationToken ct)
    {
        var delayMs = _options.RetryDelayMilliseconds * (int)Math.Pow(2, attempt);
        return Task.Delay(delayMs, ct);
    }

    private sealed class ChatCompletionResponse
    {
        public Choice[]? Choices { get; set; }
    }

    private sealed class Choice
    {
        public Message? Message { get; set; }
    }

    private sealed class Message
    {
        public string? Content { get; set; }
    }
}
