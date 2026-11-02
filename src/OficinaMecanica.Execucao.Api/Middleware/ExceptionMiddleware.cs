using MongoDB.Driver;
using OficinaMecanica.Execucao.Application.Exceptions;
using OficinaMecanica.Execucao.Domain.Exceptions;

namespace OficinaMecanica.Execucao.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger) { _next = next; _logger = logger; }
    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (ValidacaoException exception) { await Erro(context, 400, exception.Message); }
        catch (ConflitoException exception) { await Erro(context, 409, exception.Message); }
        catch (RegraNegocioException exception) { await Erro(context, 409, exception.Message); }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        { await Erro(context, 409, "Ja existe um cadastro com o mesmo nome ou ID da OS."); }
        catch (MongoCommandException exception) when (exception.Code == 11000)
        { await Erro(context, 409, "Ja existe um cadastro com o mesmo nome ou ID da OS."); }
        catch (Exception exception) when (exception is MongoException or TimeoutException)
        {
            _logger.LogError(exception, "Falha de acesso ao MongoDB");
            await Erro(context, 503, "O banco de Execucao esta indisponivel.");
        }
    }
    private static Task Erro(HttpContext context, int status, string mensagem)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new { message = mensagem, correlationId = context.TraceIdentifier });
    }
}
