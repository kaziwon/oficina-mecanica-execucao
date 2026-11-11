using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Application.UseCases.Auth;
using OficinaMecanica.Execucao.Application.UseCases.Execucoes;
using OficinaMecanica.Execucao.Application.UseCases.Servicos;
using OficinaMecanica.Execucao.Application.UseCases.PecasInsumos;
using OficinaMecanica.Execucao.Infrastructure.Persistence;
using OficinaMecanica.Execucao.Infrastructure.Settings;
using OficinaMecanica.Execucao.Infrastructure.Sources;
using OficinaMecanica.Execucao.InterfaceAdapters.Controllers;
using OficinaMecanica.Execucao.InterfaceAdapters.DataSources;
using OficinaMecanica.Execucao.InterfaceAdapters.Gateways;

namespace OficinaMecanica.Execucao;

public static class DependencyInjection
{
    public static IServiceCollection AddExecucao(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<Infrastructure.Settings.MessagingSettings>().Bind(configuration.GetSection("Messaging"));
        services.AddSingleton<Infrastructure.Messaging.SqsTransport>();
        services.AddHostedService<Messaging.IntegracaoWorker>();
        services.AddScoped<IOutboxDataSource, MongoOutboxDataSource>();
        services.AddScoped<IOutboxGateway, OutboxGateway>();
        services.AddScoped<IEntradaExecucaoDataSource, MongoEntradaExecucaoDataSource>();
        services.AddScoped<IEntradaExecucaoGateway, EntradaExecucaoGateway>();
        services.AddScoped<ProcessarComandoEntradaUseCase>();
        services.AddScoped<ProcessarRecusaOrcamentoUseCase>();
        services.AddScoped<ProcessarLiberacaoExecucaoUseCase>();
        services.AddScoped<IntegracaoCleanController>();
        services.AddOptions<MongoSettings>().Bind(configuration.GetSection("MongoSettings"))
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.ConnectionString)
                && !string.IsNullOrWhiteSpace(settings.DatabaseName), "Configure a conexao e o banco de Execucao.")
            .ValidateOnStart();
        services.AddSingleton<IMongoClient>(provider =>
        {
            var settings = MongoClientSettings.FromConnectionString(provider.GetRequiredService<IOptions<MongoSettings>>().Value.ConnectionString);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
            return new MongoClient(settings);
        });
        services.AddSingleton<MongoDbContext>();
        services.AddOptions<JwtSettings>().Bind(configuration.GetSection("JwtSettings"))
            .Validate(settings => Encoding.UTF8.GetByteCount(settings.SecretKey) >= 32,
                "Configure JwtSettings:SecretKey com pelo menos 32 bytes.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Issuer) && !string.IsNullOrWhiteSpace(settings.Audience),
                "Configure issuer e audience do JWT.")
            .ValidateOnStart();
        services.AddOptions<AdminUserSettings>().Bind(configuration.GetSection("AdminUserSettings"))
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Username) && !string.IsNullOrWhiteSpace(settings.Password),
                "Configure as credenciais administrativas.")
            .ValidateOnStart();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, configured) =>
            {
                var jwt = configured.Value;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.Zero
                };
            });
        services.AddAuthorization();
        services.AddScoped<IAdminUserSource, AdminUserSettingsSource>();
        services.AddScoped<ITokenSource, JwtTokenSource>();
        services.AddScoped<IAdminUserGateway, AdminUserGateway>();
        services.AddScoped<ITokenGateway, TokenGateway>();
        services.AddScoped<IServicoDataSource, MongoServicoDataSource>();
        services.AddScoped<IPecaInsumoDataSource, MongoPecaInsumoDataSource>();
        services.AddScoped<IExecucaoDataSource, MongoExecucaoDataSource>();
        services.AddScoped<IServicoGateway, ServicoGateway>();
        services.AddScoped<IPecaInsumoGateway, PecaInsumoGateway>();
        services.AddScoped<IExecucaoGateway, ExecucaoGateway>();
        services.AddScoped<IInicioReparoDataSource, MongoInicioReparoDataSource>();
        services.AddScoped<IInicioReparoGateway, InicioReparoGateway>();
        services.AddScoped<IniciarReparoUseCase>();
        services.AddScoped<LoginUseCase>();
        services.AddScoped<CriarServicoUseCase>();
        services.AddScoped<AtualizarServicoUseCase>();
        services.AddScoped<ObterTodosServicosUseCase>();
        services.AddScoped<ObterServicoPorIdUseCase>();
        services.AddScoped<RemoverServicoUseCase>();
        services.AddScoped<CriarPecaInsumoUseCase>();
        services.AddScoped<AtualizarPecaInsumoUseCase>();
        services.AddScoped<ObterTodasPecasInsumosUseCase>();
        services.AddScoped<ObterPecaInsumoPorIdUseCase>();
        services.AddScoped<RemoverPecaInsumoUseCase>();
        services.AddScoped<MovimentarEstoqueUseCase>();
        services.AddScoped<CriarExecucaoUseCase>();
        services.AddScoped<ObterExecucaoPorIdUseCase>();
        services.AddScoped<ObterFilaExecucoesUseCase>();
        services.AddScoped<IniciarDiagnosticoUseCase>();
        services.AddScoped<ConcluirDiagnosticoUseCase>();
        services.AddScoped<AuthCleanController>();
        services.AddScoped<ServicosCleanController>();
        services.AddScoped<PecasCleanController>();
        services.AddScoped<ExecucoesCleanController>();
        return services;
    }
}
