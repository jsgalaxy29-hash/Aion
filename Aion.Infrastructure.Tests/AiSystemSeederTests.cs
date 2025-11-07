using System;
using System.Threading.Tasks;
using Aion.DataEngine.Interfaces;
using Aion.Domain.AI;
using Aion.Infrastructure.Seeders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aion.Infrastructure.Tests;

public class AiSystemSeederTests
{
    [Fact]
    public async Task SeedAsync_EnsuresRequiredRecordsExist()
    {
        var options = new DbContextOptionsBuilder<AionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var userContext = new TestUserContext();
        await using var dbContext = new AionDbContext(options, userContext);

        await AiSystemSeeder.SeedAsync(dbContext);

        (await dbContext.SXAiConfigs.CountAsync()).Should().Be(1);
        (await dbContext.SXSynonyms.CountAsync()).Should().BeGreaterOrEqualTo(3);
        (await dbContext.SXTemplates.CountAsync()).Should().Be(2);
    }

    private sealed class TestUserContext : IUserContext
    {
        public int UserId => 1;
        public int TenantId => 1;
        public string? Username => "test";
    }
}
