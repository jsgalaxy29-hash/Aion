using System;
using Aion.AI.Abstractions;
using Aion.AI.Models;
using Aion.AI.Orchestration;
using Aion.AI.Services;
using Aion.Domain.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Aion.AI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAionAi(this IServiceCollection services, Action<LanguageModelOptions>? configureOptions = null)
    {
        services.TryAddScoped<IAuditTrailService, NullAuditTrailService>();
        services.TryAddScoped<IAiModuleSpecGenerator, MockAiModuleSpecGenerator>();

        services.Configure(configureOptions ?? (_ => { }));

        services.AddHttpClient<OpenAIProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<LanguageModelOptions>>().Value.OpenAI;
            client.BaseAddress = new Uri(options.DefaultBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddHttpClient<MistralProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<LanguageModelOptions>>().Value.Mistral;
            client.BaseAddress = new Uri(options.DefaultBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddScoped<ILanguageModelClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<LanguageModelOptions>>().Value;
            var provider = options.Provider?.ToLowerInvariant() ?? "mock";

            return provider switch
            {
                "openai" => sp.GetRequiredService<OpenAIProvider>(),
                "mistral" => sp.GetRequiredService<MistralProvider>(),
                _ => sp.GetRequiredService<MockLanguageModelClient>()
            };
        });

        services.TryAddScoped<MockLanguageModelClient>();
        
        services.AddScoped<IAionAiOrchestrator, AionAiOrchestrator>();
        services.AddScoped<IIntentRecognizer, RuleBasedIntentRecognizer>();
        services.AddScoped<IPlanner, SimplePlanner>();
        services.AddScoped<IRoadmapPatcher, YamlRoadmapPatcher>();
        services.AddScoped<ISimulator, InMemorySimulator>();
        services.AddScoped<IArtifactGenerator, TemplateArtifactGenerator>();

        return services;
    }
}
