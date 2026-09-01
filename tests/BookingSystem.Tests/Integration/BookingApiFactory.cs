using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.MsSql;

namespace BookingSystem.Tests.Integration;

public class BookingApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestJwtKey = "integration-test-signing-key-at-least-32-bytes-long!!";

    private readonly MsSqlContainer msSqlContainer =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public Task InitializeAsync() => msSqlContainer.StartAsync();

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await msSqlContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = msSqlContainer.GetConnectionString(),
                ["Jwt:Key"] = TestJwtKey,
            });
        });

        // AddInfrastructure reads Jwt:Key eagerly (before this configuration override is merged in)
        // to build JwtBearerOptions, so token *validation* would otherwise use whatever key was
        // already configured on this machine while JwtTokenService (resolved lazily per-request via
        // IOptions<JwtSettings>) would sign with the overridden TestJwtKey above. Force the
        // validation key to match so issued tokens are actually accepted.
        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                options.TokenValidationParameters.IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey)));
        });
    }
}
