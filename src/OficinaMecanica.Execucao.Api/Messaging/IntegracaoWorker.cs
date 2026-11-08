using Microsoft.Extensions.Options;
using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Infrastructure.Messaging;
using OficinaMecanica.Execucao.Infrastructure.Settings;
using OficinaMecanica.Execucao.InterfaceAdapters.Controllers;

namespace OficinaMecanica.Execucao.Messaging;

public class IntegracaoWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly SqsTransport _transport;
    private readonly MessagingSettings _settings;
    private readonly ILogger<IntegracaoWorker> _logger;

    public IntegracaoWorker(IServiceScopeFactory scopes, SqsTransport transport,
        IOptions<MessagingSettings> settings, ILogger<IntegracaoWorker> logger)
    {
        _scopes = scopes; _transport = transport; _settings = settings.Value; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _scopes.CreateScope())
                {
                    var outbox = scope.ServiceProvider.GetRequiredService<IOutboxGateway>();
                    foreach (var mensagem in outbox.ObterPendentes(10))
                    {
                        await _transport.Enviar(_settings.EventosQueue, MensagemSerializer.Serializar(mensagem), stoppingToken);
                        outbox.ConfirmarPublicacao(mensagem.MessageId);
                        _logger.LogInformation("Mensagem {MessageId} da Saga {SagaId} publicada. CorrelationId={CorrelationId}",
                            mensagem.MessageId, mensagem.SagaId, mensagem.CorrelationId);
                    }
                }
                foreach (var recebida in await _transport.Receber(_settings.ComandosQueue, stoppingToken))
                {
                    // A fila so e confirmada depois da transacao local. Falhas permitem reentrega/DLQ.
                    using var scope = _scopes.CreateScope();
                    var mensagem = MensagemSerializer.Desserializar(recebida.Body);
                    using var logScope = _logger.BeginScope(new Dictionary<string, object>
                    {
                        ["SagaId"] = mensagem.SagaId,
                        ["MessageId"] = mensagem.MessageId,
                        ["CorrelationId"] = mensagem.CorrelationId
                    });
                    scope.ServiceProvider.GetRequiredService<IntegracaoCleanController>().Processar(mensagem);
                    await _transport.Confirmar(_settings.ComandosQueue, recebida.ReceiptHandle, stoppingToken);
                    _logger.LogInformation("Mensagem {MessageId} processada", mensagem.MessageId);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Integracao interrompida; mensagem nao confirmada sera tentada novamente ou enviada a DLQ");
                try { await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            }
        }
    }
}
