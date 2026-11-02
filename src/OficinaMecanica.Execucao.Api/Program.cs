using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using OficinaMecanica.Execucao;
using OficinaMecanica.Execucao.Infrastructure.Persistence;
using OficinaMecanica.Execucao.Middleware;
using OficinaMecanica.Execucao.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);
builder.Logging.Configure(options => options.ActivityTrackingOptions =
    ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId | ActivityTrackingOptions.ParentId);
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});
builder.Services.AddExecucao(builder.Configuration);
var app = builder.Build();

await app.Services.GetRequiredService<MongoDbContext>().CriarIndices();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "execucao" })).AllowAnonymous();
app.MapGet("/health/ready", async (MongoDbContext database, CancellationToken cancellationToken) =>
{
    var ready = await database.EstaDisponivel(cancellationToken);
    return Results.Json(new { status = ready ? "Healthy" : "Unhealthy" }, statusCode: ready ? 200 : 503);
}).AllowAnonymous();
app.MapControllers();
app.Run();

public partial class Program;
