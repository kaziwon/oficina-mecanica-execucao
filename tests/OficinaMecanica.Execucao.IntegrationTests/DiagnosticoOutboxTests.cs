using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using OficinaMecanica.Execucao.Application.Inputs.Execucoes;
using OficinaMecanica.Execucao.Application.Messaging;
using OficinaMecanica.Execucao.Application.UseCases.Execucoes;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Infrastructure.Persistence;
using OficinaMecanica.Execucao.Infrastructure.Sources;
using OficinaMecanica.Execucao.InterfaceAdapters.Controllers;

namespace OficinaMecanica.Execucao.IntegrationTests;

public class DiagnosticoOutboxTests
{
    [MongoFact]
    public void Diagnostico_GravaMudancaEEventosNoMesmoDocumento()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient();
        var id = Registrar(factory);
        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IniciarDiagnosticoUseCase>().Executar(id);
        scope.ServiceProvider.GetRequiredService<ConcluirDiagnosticoUseCase>().Executar(id,
            new ConcluirDiagnosticoInput { Diagnostico = "Nenhum reparo necessario" });
        var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var atual = db.Execucoes.Find(item => item.Id == id).Single();
        Assert.Equal(StatusExecucao.AguardandoLiberacao, atual.Status);
        Assert.Equal(2, atual.EventosDiagnostico.Count);
        var outbox = new MongoOutboxDataSource(db);
        var mensagens = outbox.ObterPendentes(10);
        Assert.Equal(3, mensagens.Count);
        var concluida = mensagens.Single(item => item.Type == "DiagnosticoConcluido");
        Assert.Equal("teste-diagnostico", concluida.CorrelationId);
        Assert.Equal(atual.PedidoRegistroId, concluida.CausationId);
        Assert.Empty(concluida.Payload.Itens!);
        foreach (var mensagem in mensagens) outbox.ConfirmarPublicacao(mensagem.MessageId);
        Assert.Empty(outbox.ObterPendentes(10));
        Assert.Equal(2, db.Execucoes.Find(item => item.Id == id).Single().EventosDiagnostico.Count);
    }

    [MongoFact]
    public void PublicacaoDoEvento_NaoEhDesfeitaPorReplaceAntigo()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient();
        var id = Registrar(factory);
        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IniciarDiagnosticoUseCase>().Executar(id);
        var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var source = new MongoExecucaoDataSource(db);
        var copia = source.ObterPorId(id)!; var versao = copia.Versao;
        var eventoId = Assert.Single(copia.EventosDiagnostico).MessageId;
        new MongoOutboxDataSource(db).ConfirmarPublicacao(eventoId);
        copia.ConcluirDiagnostico("Conclusao concorrente", [], []);
        Assert.False(source.Atualizar(copia, versao));
        var atual = source.ObterPorId(id)!;
        Assert.NotNull(Assert.Single(atual.EventosDiagnostico).PublicadoEm);
        Assert.Equal(StatusExecucao.EmDiagnostico, atual.Status);
        Assert.DoesNotContain(new MongoOutboxDataSource(db).ObterPendentes(10), item => item.Type == "DiagnosticoConcluido");
    }

    private static Guid Registrar(ExecucaoApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var pedido = new MensagemIntegracao
        {
            MessageId = Guid.NewGuid(),
            Type = "RegistrarExecucao",
            SagaId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            CorrelationId = "teste-diagnostico",
            Payload = new DadosMensagem { OrdemServicoId = Guid.NewGuid(), DescricaoSolicitacao = "Revisao" }
        };
        scope.ServiceProvider.GetRequiredService<IntegracaoCleanController>().Processar(pedido);
        return scope.ServiceProvider.GetRequiredService<MongoDbContext>().Execucoes
            .Find(item => item.OrdemServicoId == pedido.Payload.OrdemServicoId).Single().Id;
    }
}
