using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Aion.Domain.ModuleBuilder;
using Aion.Infrastructure.Ai;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aion.Infrastructure.Tests;

public class MistralModuleSpecGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_maps_response_to_blueprint()
    {
        var modulePayload = new
        {
            moduleName = "Gestion des contrats",
            description = "Module de gestion des contrats d'assurance santé.",
            tables = new[]
            {
                new
                {
                    technicalName = "F_CONTRAT",
                    displayName = "Contrats",
                    description = "Table des contrats",
                    fields = new[]
                    {
                        new
                        {
                            technicalName = "Id",
                            displayName = "Identifiant",
                            dataType = "int",
                            isPrimaryKey = true,
                            isRequired = true,
                            isUnique = true
                        },
                        new
                        {
                            technicalName = "NumeroContrat",
                            displayName = "Numéro de contrat",
                            dataType = "string",
                            maxLength = 50,
                            isRequired = true,
                            isUnique = true
                        }
                    }
                }
            }
        };

        var chatResponse = new
        {
            id = "chatcmpl-123",
            choices = new[]
            {
                new
                {
                    index = 0,
                    message = new
                    {
                        role = "assistant",
                        content = JsonSerializer.Serialize(modulePayload)
                    }
                }
            }
        };

        var handler = new StubMessageHandler(JsonSerializer.Serialize(chatResponse), HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new MistralOptions
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.mistral.ai",
            Model = "mistral-small-latest"
        });

        using var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug));
        var generator = new MistralModuleSpecGenerator(httpClient, options, loggerFactory.CreateLogger<MistralModuleSpecGenerator>());

        var blueprint = await generator.GenerateAsync("Créer un module de contrats");

        blueprint.Name.Should().Be("Gestion des contrats");
        blueprint.Description.Should().Contain("assurance");
        blueprint.Tables.Should().HaveCount(1);

        var table = blueprint.Tables[0];
        table.TechnicalName.Should().Be("F_CONTRAT");
        table.Fields.Should().HaveCount(2);

        var field = table.Fields[0];
        field.IsPrimaryKey.Should().BeTrue();
        field.IsRequired.Should().BeTrue();

        blueprint.NaturalLanguagePrompt.Should().Be("Créer un module de contrats");
        blueprint.ParsedSpecificationJson.Should().Contain("Gestion des contrats");
    }

    private sealed class StubMessageHandler : HttpMessageHandler
    {
        private readonly string _responseContent;
        private readonly HttpStatusCode _statusCode;

        public StubMessageHandler(string responseContent, HttpStatusCode statusCode)
        {
            _responseContent = responseContent;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }
}
