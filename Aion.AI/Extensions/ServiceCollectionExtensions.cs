using Aion.AI.Abstractions;
using Aion.AI.Orchestration;
using Aion.AI.Services;
using Aion.Domain.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aion.AI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAionAi(this IServiceCollection services)
    {
        services.TryAddScoped<IAuditTrailService, NullAuditTrailService>();
        services.TryAddScoped<ILanguageModelClient, MockLanguageModelClient>();
        services.TryAddScoped<IAiModuleSpecGenerator, MockAiModuleSpecGenerator>();

        services.AddScoped<IAionAiOrchestrator, AionAiOrchestrator>();
        services.AddScoped<IIntentRecognizer, RuleBasedIntentRecognizer>();
        services.AddScoped<IPlanner, SimplePlanner>();
        services.AddScoped<IRoadmapPatcher, YamlRoadmapPatcher>();
        services.AddScoped<ISimulator, InMemorySimulator>();
        services.AddScoped<IArtifactGenerator, TemplateArtifactGenerator>();

        return services;
    }
}
