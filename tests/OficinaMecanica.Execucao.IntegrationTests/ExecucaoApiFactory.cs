using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using OficinaMecanica.Execucao.Infrastructure.Persistence;

namespace OficinaMecanica.Execucao.IntegrationTests;

public class ExecucaoApiFactory : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = "execucao_test_" + Guid.NewGuid().ToString("N");
    public MongoClient Mongo { get; }

    public ExecucaoApiFactory()
    {
        MongoSerialization.Configure();
        Mongo = new MongoClient(Environment.GetEnvironmentVariable("MONGODB_TEST_CONNECTION_STRING")
            ?? throw new InvalidOperationException("MONGODB_TEST_CONNECTION_STRING nao configurada."));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MongoSettings:ConnectionString"] = Environment.GetEnvironmentVariable("MONGODB_TEST_CONNECTION_STRING"),
            ["MongoSettings:DatabaseName"] = DatabaseName,
            ["JwtSettings:SecretKey"] = "chave-exclusiva-dos-testes-de-execucao-1234567890",
            ["AdminUserSettings:Username"] = "admin",
            ["AdminUserSettings:Password"] = "SenhaDosTestes@123"
            ,
            ["Messaging:Enabled"] = "false"
        }));
    }

    protected override void Dispose(bool disposing)
    {
        try
        {
            if (disposing) Mongo.DropDatabase(DatabaseName);
        }
        finally
        {
            base.Dispose(disposing);
            if (disposing) Mongo.Dispose();
        }
    }
}
