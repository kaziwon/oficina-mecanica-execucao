using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using OficinaMecanica.Execucao.Application.Exceptions;
using OficinaMecanica.Execucao.Application.Inputs.Execucoes;
using OficinaMecanica.Execucao.Application.Messaging;
using OficinaMecanica.Execucao.Application.UseCases.Execucoes;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Infrastructure.Persistence;
using OficinaMecanica.Execucao.Infrastructure.Sources;
using OficinaMecanica.Execucao.InterfaceAdapters.Controllers;

namespace OficinaMecanica.Execucao.IntegrationTests;

public class RecusaOrcamentoTests
{
    [MongoFact]
    public async Task RecusaConcorrente_CancelaUmaVezEPreservaDiagnostico()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient();
        var comando = Criar(factory);
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => Processar(factory, comando))));
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var execucao = db.Execucoes.Find(item => item.Id == comando.Payload.ExecucaoId).Single();
        Assert.Equal(StatusExecucao.Cancelada, execucao.Status); Assert.Equal("Diagnostico preservado", execucao.Diagnostico);
        Assert.Equal(4, execucao.Historico.Count); Assert.Equal(2, execucao.EventosDiagnostico.Count);
        var resposta = Assert.Single(execucao.RespostasRecusaOrcamento);
        Assert.Equal("ExecucaoCanceladaPorRecusaOrcamento", resposta.Tipo);
        var outbox = new MongoOutboxDataSource(db);
        Assert.Equal(comando.MessageId, outbox.ObterPendentes(10).Single(item => item.Type == resposta.Tipo).CausationId);
        var copia = new MongoExecucaoDataSource(db).ObterPorId(execucao.Id)!; var versao = copia.Versao;
        outbox.ConfirmarPublicacao(resposta.MessageId);
        copia.Versao++;
        Assert.False(new MongoExecucaoDataSource(db).Atualizar(copia, versao));
        Processar(factory, comando);
        Assert.DoesNotContain(outbox.ObterPendentes(10), item => item.MessageId == resposta.MessageId);
    }

    [MongoFact]
    public void ReparoJaIniciado_RecusaCancelamentoSemDesfazerTrabalho()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); var comando = Criar(factory);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        db.Execucoes.UpdateOne(item => item.Id == comando.Payload.ExecucaoId,
            Builders<ExecucaoServico>.Update.Set(item => item.Status, StatusExecucao.EmExecucao).Inc(item => item.Versao, 1));
        Processar(factory, comando); Processar(factory, comando);
        var execucao = db.Execucoes.Find(item => item.Id == comando.Payload.ExecucaoId).Single();
        Assert.Equal(StatusExecucao.EmExecucao, execucao.Status);
        Assert.Equal(3, execucao.Historico.Count);
        Assert.Equal("ExecucaoNaoCancelavel", Assert.Single(execucao.RespostasRecusaOrcamento).Codigo);
    }

    [MongoFact]
    public void MessageIdComOutroOrcamento_NaoEhAceito()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); var comando = Criar(factory);
        Processar(factory, comando); comando.Payload.OrcamentoId = Guid.NewGuid();
        Assert.Throws<ValidacaoException>(() => Processar(factory, comando));
    }

    [MongoFact]
    public void OutraSaga_NaoCancelaENaoGravaRecusaFalsa()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); var comando = Criar(factory);
        comando.SagaId = Guid.NewGuid();
        Assert.Throws<ValidacaoException>(() => Processar(factory, comando));
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var execucao = db.Execucoes.Find(item => item.Id == comando.Payload.ExecucaoId).Single();
        Assert.Equal(StatusExecucao.AguardandoLiberacao, execucao.Status); Assert.Empty(execucao.RespostasRecusaOrcamento);
    }

    [MongoFact]
    public void MessageIdParaOutraExecucao_IndiceImpedeSegundoEfeito()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); var primeiro = Criar(factory);
        Processar(factory, primeiro);
        var segundo = Criar(factory); segundo.MessageId = primeiro.MessageId;
        Assert.Throws<MongoWriteException>(() => Processar(factory, segundo));
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        Assert.Equal(StatusExecucao.AguardandoLiberacao, db.Execucoes.Find(item => item.Id == segundo.Payload.ExecucaoId).Single().Status);
    }

    private static MensagemIntegracao Criar(ExecucaoApiFactory factory)
    {
        using var scope = factory.Services.CreateScope(); var entrada = new MensagemIntegracao
        {
            MessageId = Guid.NewGuid(),
            Type = "RegistrarExecucao",
            SagaId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            CorrelationId = "teste-recusa",
            Payload = new DadosMensagem { OrdemServicoId = Guid.NewGuid(), DescricaoSolicitacao = "Revisao" }
        };
        scope.ServiceProvider.GetRequiredService<IntegracaoCleanController>().Processar(entrada);
        var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var execucao = db.Execucoes.Find(item => item.OrdemServicoId == entrada.Payload.OrdemServicoId).Single();
        scope.ServiceProvider.GetRequiredService<IniciarDiagnosticoUseCase>().Executar(execucao.Id);
        scope.ServiceProvider.GetRequiredService<ConcluirDiagnosticoUseCase>().Executar(execucao.Id,
            new ConcluirDiagnosticoInput { Diagnostico = "Diagnostico preservado" });
        return new MensagemIntegracao
        {
            MessageId = Guid.NewGuid(),
            Type = "CancelarExecucaoPorRecusaOrcamento",
            SagaId = entrada.SagaId,
            CausationId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            CorrelationId = entrada.CorrelationId,
            Payload = new DadosMensagem
            {
                OrdemServicoId = execucao.OrdemServicoId,
                ExecucaoId = execucao.Id,
                OrcamentoId = Guid.NewGuid(),
                Motivo = "Cliente recusou"
            }
        };
    }

    private static void Processar(ExecucaoApiFactory factory, MensagemIntegracao mensagem)
    {
        using var scope = factory.Services.CreateScope(); scope.ServiceProvider.GetRequiredService<IntegracaoCleanController>().Processar(mensagem);
    }
}
