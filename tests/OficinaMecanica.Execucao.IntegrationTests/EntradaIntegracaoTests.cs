using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using OficinaMecanica.Execucao.Application.Exceptions;
using OficinaMecanica.Execucao.Application.Messaging;
using OficinaMecanica.Execucao.Application.UseCases.Execucoes;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Infrastructure.Persistence;
using OficinaMecanica.Execucao.Infrastructure.Sources;
using OficinaMecanica.Execucao.InterfaceAdapters.Controllers;

namespace OficinaMecanica.Execucao.IntegrationTests;

public class EntradaIntegracaoTests
{
    [MongoFact]
    public async Task RegistroConcorrente_RepetidoCriaUmaEntradaEUmaResposta()
    {
        using var factory = new ExecucaoApiFactory();
        using var client = factory.CreateClient();
        var mensagem = Registrar();
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => Processar(factory, mensagem))));
        var execucao = Assert.Single(Colecao(factory).Find(FilterDefinition<ExecucaoServico>.Empty).ToList());
        Assert.Equal(mensagem.SagaId, execucao.SagaId);
        Assert.Equal(StatusExecucao.AguardandoDiagnostico, execucao.Status);
        Assert.Single(execucao.Historico);
        Assert.Equal("ExecucaoRegistrada", Assert.Single(execucao.RespostasEntrada).Tipo);
        using var scope = factory.Services.CreateScope();
        var outbox = new MongoOutboxDataSource(scope.ServiceProvider.GetRequiredService<MongoDbContext>());
        var resposta = Assert.Single(outbox.ObterPendentes(10));
        Assert.Equal(mensagem.MessageId, resposta.CausationId);
        Assert.Equal(execucao.Id, resposta.Payload.ExecucaoId);
        outbox.ConfirmarPublicacao(resposta.MessageId);
        Processar(factory, mensagem);
        Assert.Empty(outbox.ObterPendentes(10));
    }

    [MongoFact]
    public void CancelamentoAntesDoRegistro_ImpedeRessurreicaoPorMensagemAtrasada()
    {
        using var factory = new ExecucaoApiFactory();
        using var client = factory.CreateClient();
        var registrar = Registrar();
        var cancelar = Cancelar(registrar);
        Processar(factory, cancelar);
        Processar(factory, cancelar);
        Processar(factory, registrar);
        var execucao = Colecao(factory).Find(item => item.OrdemServicoId == registrar.Payload.OrdemServicoId).Single();
        Assert.False(execucao.EntradaRegistrada);
        Assert.Equal(StatusExecucao.Cancelada, execucao.Status);
        Assert.Single(execucao.Historico);
        Assert.Equal(2, execucao.RespostasEntrada.Count);
        Assert.Null(execucao.RespostasEntrada.Single(item => item.Tipo == "EntradaExecucaoCancelada").ExecucaoId);
        Assert.Equal("ProcessoCancelado", execucao.RespostasEntrada.Single(item => item.Tipo == "RegistroExecucaoRecusado").Codigo);
        using var scope = factory.Services.CreateScope();
        Assert.Empty(scope.ServiceProvider.GetRequiredService<ObterFilaExecucoesUseCase>().Executar(null));
    }

    [MongoFact]
    public void Compensacao_DesativaEntradaSemApagarHistorico()
    {
        using var factory = new ExecucaoApiFactory();
        using var client = factory.CreateClient();
        var registrar = Registrar();
        Processar(factory, registrar);
        var cancelar = Cancelar(registrar);
        Processar(factory, cancelar);
        Processar(factory, cancelar);
        var execucao = Colecao(factory).Find(item => item.OrdemServicoId == registrar.Payload.OrdemServicoId).Single();
        Assert.True(execucao.EntradaRegistrada);
        Assert.Equal(StatusExecucao.Cancelada, execucao.Status);
        Assert.Equal(2, execucao.Historico.Count);
        Assert.Equal(2, execucao.RespostasEntrada.Count);
        Assert.Equal(execucao.Id, execucao.RespostasEntrada.Single(item => item.Tipo == "EntradaExecucaoCancelada").ExecucaoId);
    }

    [MongoFact]
    public void CompensacaoDepoisDoDiagnostico_EhRecusadaSemReverterAndamento()
    {
        using var factory = new ExecucaoApiFactory();
        using var client = factory.CreateClient();
        var registrar = Registrar();
        Processar(factory, registrar);
        var execucao = Colecao(factory).Find(item => item.OrdemServicoId == registrar.Payload.OrdemServicoId).Single();
        using (var scope = factory.Services.CreateScope())
            scope.ServiceProvider.GetRequiredService<IniciarDiagnosticoUseCase>().Executar(execucao.Id);
        Processar(factory, Cancelar(registrar));
        var atual = Colecao(factory).Find(item => item.Id == execucao.Id).Single();
        Assert.Equal(StatusExecucao.EmDiagnostico, atual.Status);
        Assert.Equal("DiagnosticoJaIniciado", atual.RespostasEntrada.Single(item => item.Tipo == "CancelamentoEntradaExecucaoRecusado").Codigo);
    }

    [MongoFact]
    public void EntradaManual_SoPodeSerReaproveitadaComDadosCompativeis()
    {
        using var factory = new ExecucaoApiFactory();
        using var client = factory.CreateClient();
        var registrar = Registrar();
        using (var scope = factory.Services.CreateScope())
            scope.ServiceProvider.GetRequiredService<CriarExecucaoUseCase>().Executar(new(registrar.Payload.OrdemServicoId, "Outro pedido"));
        Processar(factory, registrar);
        var execucao = Colecao(factory).Find(item => item.OrdemServicoId == registrar.Payload.OrdemServicoId).Single();
        Assert.Null(execucao.SagaId);
        Assert.Equal("Outro pedido", execucao.DescricaoSolicitacao);
        Assert.Equal("RegistroExecucaoRecusado", Assert.Single(execucao.RespostasEntrada).Tipo);
        Assert.Single(execucao.Historico);
    }

    [MongoFact]
    public void MessageIdReutilizadoComOutrosDados_EhRejeitado()
    {
        using var factory = new ExecucaoApiFactory();
        using var client = factory.CreateClient();
        var registrar = Registrar();
        Processar(factory, registrar);
        registrar.Payload.DescricaoSolicitacao = "Dados alterados";
        Assert.Throws<ValidacaoException>(() => Processar(factory, registrar));
        var execucao = Colecao(factory).Find(item => item.OrdemServicoId == registrar.Payload.OrdemServicoId).Single();
        Assert.Single(execucao.RespostasEntrada);
        Assert.Equal("Revisao dos freios", execucao.DescricaoSolicitacao);
    }

    [MongoFact]
    public void PublicacaoDaResposta_NaoPodeSerDesfeitaPorAtualizacaoDesatualizada()
    {
        using var factory = new ExecucaoApiFactory();
        using var client = factory.CreateClient();
        var registrar = Registrar();
        Processar(factory, registrar);
        var execucao = Colecao(factory).Find(item => item.OrdemServicoId == registrar.Payload.OrdemServicoId).Single();
        var versao = execucao.Versao;
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var outbox = new MongoOutboxDataSource(database);
        outbox.ConfirmarPublicacao(execucao.RespostasEntrada.Single().MessageId);
        execucao.IniciarDiagnostico();
        Assert.False(new MongoEntradaExecucaoDataSource(database).TentarAtualizar(execucao, versao));
        Assert.Empty(outbox.ObterPendentes(10));
    }

    [MongoFact]
    public void MesmoMessageIdParaOutraOS_NaoCriaUmaSegundaEntrada()
    {
        using var factory = new ExecucaoApiFactory();
        using var client = factory.CreateClient();
        var registrar = Registrar();
        Processar(factory, registrar);
        registrar.Payload.OrdemServicoId = Guid.NewGuid();
        Assert.Throws<ConflitoException>(() => Processar(factory, registrar));
        Assert.Equal(1, Colecao(factory).CountDocuments(FilterDefinition<ExecucaoServico>.Empty));
    }

    private static MensagemIntegracao Registrar() => new()
    {
        MessageId = Guid.NewGuid(),
        Type = "RegistrarExecucao",
        SagaId = Guid.NewGuid(),
        OccurredAt = DateTime.UtcNow,
        CorrelationId = "teste-entrada",
        Payload = new DadosMensagem { OrdemServicoId = Guid.NewGuid(), DescricaoSolicitacao = "Revisao dos freios" }
    };

    private static MensagemIntegracao Cancelar(MensagemIntegracao registrar) => new()
    {
        MessageId = Guid.NewGuid(),
        Type = "CancelarEntradaExecucao",
        SagaId = registrar.SagaId,
        CausationId = registrar.MessageId,
        OccurredAt = DateTime.UtcNow,
        CorrelationId = registrar.CorrelationId,
        Payload = new DadosMensagem { OrdemServicoId = registrar.Payload.OrdemServicoId, Motivo = "Cliente desistiu" }
    };

    private static IMongoCollection<ExecucaoServico> Colecao(ExecucaoApiFactory factory)
        => factory.Mongo.GetDatabase(factory.DatabaseName).GetCollection<ExecucaoServico>("execucoes");

    private static void Processar(ExecucaoApiFactory factory, MensagemIntegracao mensagem)
    {
        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IntegracaoCleanController>().Processar(mensagem);
    }
}
