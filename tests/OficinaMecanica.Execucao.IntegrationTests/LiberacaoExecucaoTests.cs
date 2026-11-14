using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using OficinaMecanica.Execucao.Application.Exceptions;
using OficinaMecanica.Execucao.Application.Inputs.Execucoes;
using OficinaMecanica.Execucao.Application.Inputs.PecasInsumos;
using OficinaMecanica.Execucao.Application.Inputs.Servicos;
using OficinaMecanica.Execucao.Application.Messaging;
using OficinaMecanica.Execucao.Application.UseCases.Execucoes;
using OficinaMecanica.Execucao.Application.UseCases.PecasInsumos;
using OficinaMecanica.Execucao.Application.UseCases.Servicos;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Infrastructure.Persistence;
using OficinaMecanica.Execucao.Infrastructure.Sources;
using OficinaMecanica.Execucao.InterfaceAdapters.Controllers;
using OficinaMecanica.Execucao.InterfaceAdapters.DTOs;

namespace OficinaMecanica.Execucao.IntegrationTests;

public class LiberacaoExecucaoTests
{
    [MongoFact]
    public async Task ComandoConcorrente_LiberaUmaVezSemConsumirEstoque()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); var comando = Criar(factory);
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => Processar(factory, comando))));
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var execucao = db.Execucoes.Find(item => item.Id == comando.Payload.ExecucaoId).Single();
        Assert.Equal(StatusExecucao.AguardandoExecucao, execucao.Status); Assert.Equal(4, execucao.Historico.Count);
        Assert.Equal(comando.MessageId, execucao.Autorizacao!.MensagemLiberacaoId);
        Assert.Equal(comando.CausationId, execucao.Autorizacao.ConfirmacaoPagamentoId);
        Assert.Equal(comando.Payload.PagamentoId, execucao.Autorizacao.PagamentoId);
        Assert.Equal(10, db.Pecas.Find(FilterDefinition<PecaInsumo>.Empty).Single().QuantidadeEstoque);
        Assert.Equal(200m, execucao.Autorizacao.ValorPago); Assert.Single(execucao.RespostasLiberacao);
        var outbox = new MongoOutboxDataSource(db);
        var resposta = outbox.ObterPendentes(10).Single(item => item.Type == "ExecucaoLiberada");
        Assert.Equal(comando.MessageId, resposta.CausationId);
        outbox.ConfirmarPublicacao(resposta.MessageId); Processar(factory, comando);
        Assert.DoesNotContain(outbox.ObterPendentes(10), item => item.MessageId == resposta.MessageId);
        await ApiTestHelper.Autenticar(client);
        var dto = await client.GetFromJsonAsync<ExecucaoResponseDto>($"/api/execucoes/{execucao.Id}", ApiTestHelper.Json);
        Assert.Equal(comando.Payload.OrcamentoId, dto!.OrcamentoId); Assert.Equal(comando.Payload.PagamentoId, dto.PagamentoId); Assert.NotNull(dto.LiberadaEm);
    }

    [MongoFact]
    public void PrecosAtuaisDoCatalogo_NaoAlteramValorDoDiagnosticoAutorizado()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); var comando = Criar(factory);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        db.Servicos.UpdateMany(FilterDefinition<Servico>.Empty, Builders<Servico>.Update.Set(item => item.Preco, 999m));
        Processar(factory, comando);
        Assert.Equal(StatusExecucao.AguardandoExecucao, db.Execucoes.Find(item => item.Id == comando.Payload.ExecucaoId).Single().Status);
    }

    [MongoTheory]
    [InlineData("valor")]
    [InlineData("cancelada")]
    [InlineData("emexecucao")]
    public void RegraIncompativel_GravaRecusaSemAutorizar(string erro)
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); var comando = Criar(factory);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        if (erro == "valor") comando.Payload.ValorTotal = 199m;
        else db.Execucoes.UpdateOne(item => item.Id == comando.Payload.ExecucaoId,
            Builders<ExecucaoServico>.Update.Set(item => item.Status, erro == "cancelada" ? StatusExecucao.Cancelada : StatusExecucao.EmExecucao).Inc(item => item.Versao, 1));
        Processar(factory, comando); Processar(factory, comando);
        var execucao = db.Execucoes.Find(item => item.Id == comando.Payload.ExecucaoId).Single();
        Assert.Null(execucao.Autorizacao); Assert.Equal(3, execucao.Historico.Count);
        Assert.Equal("LiberacaoExecucaoRecusada", Assert.Single(execucao.RespostasLiberacao).Tipo);
        Assert.Equal("ExecucaoNaoLiberavel", execucao.RespostasLiberacao[0].Codigo);
    }

    [MongoFact]
    public void SagaIncorretaOuDadosAusentes_NaoGravamRespostaFalsa()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); var comando = Criar(factory);
        var saga = comando.SagaId; comando.SagaId = Guid.NewGuid();
        Assert.Throws<ValidacaoException>(() => Processar(factory, comando));
        comando.SagaId = saga; comando.Payload.PagamentoId = null;
        Assert.Throws<ValidacaoException>(() => Processar(factory, comando));
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        Assert.Empty(db.Execucoes.Find(item => item.Id == comando.Payload.ExecucaoId).Single().RespostasLiberacao);
    }

    [MongoFact]
    public void MessageIdReutilizado_EhRejeitadoSemTrocarAutorizacao()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); var comando = Criar(factory);
        Processar(factory, comando); var pagamento = comando.Payload.PagamentoId; comando.Payload.PagamentoId = Guid.NewGuid();
        Assert.Throws<ValidacaoException>(() => Processar(factory, comando));
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        Assert.Equal(pagamento, db.Execucoes.Find(item => item.Id == comando.Payload.ExecucaoId).Single().Autorizacao!.PagamentoId);
    }

    [MongoFact]
    public void PublicacaoDaResposta_NaoEhDesfeitaPorReplaceAntigo()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); var comando = Criar(factory); Processar(factory, comando);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var source = new MongoExecucaoDataSource(db); var copia = source.ObterPorId(comando.Payload.ExecucaoId!.Value)!; var versao = copia.Versao;
        var resposta = Assert.Single(copia.RespostasLiberacao);
        new MongoOutboxDataSource(db).ConfirmarPublicacao(resposta.MessageId); copia.Versao++;
        Assert.False(source.Atualizar(copia, versao));
        Assert.NotNull(Assert.Single(source.ObterPorId(copia.Id)!.RespostasLiberacao).PublicadaEm);
        // Uma repeticao atrasada tambem nao faz uma execucao futura em andamento voltar para a fila.
        db.Execucoes.UpdateOne(item => item.Id == copia.Id, Builders<ExecucaoServico>.Update.Set(item => item.Status, StatusExecucao.EmExecucao).Inc(item => item.Versao, 1));
        Processar(factory, comando);
        Assert.Equal(StatusExecucao.EmExecucao, source.ObterPorId(copia.Id)!.Status);
    }

    [MongoFact]
    public void MesmoMessageIdEmOutraOS_IndiceImpedeSegundoEfeito()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); var primeiro = Criar(factory); Processar(factory, primeiro);
        var segundo = Criar(factory); segundo.MessageId = primeiro.MessageId;
        Assert.Throws<MongoWriteException>(() => Processar(factory, segundo));
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        Assert.Equal(StatusExecucao.AguardandoLiberacao, db.Execucoes.Find(item => item.Id == segundo.Payload.ExecucaoId).Single().Status);
    }

    internal static MensagemIntegracao Criar(ExecucaoApiFactory factory)
    {
        using var scope = factory.Services.CreateScope(); var entrada = new MensagemIntegracao
        {
            MessageId = Guid.NewGuid(),
            Type = "RegistrarExecucao",
            SagaId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            CorrelationId = "liberacao-teste",
            Payload = new DadosMensagem { OrdemServicoId = Guid.NewGuid(), DescricaoSolicitacao = "Revisao" }
        };
        scope.ServiceProvider.GetRequiredService<IntegracaoCleanController>().Processar(entrada);
        var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>(); var execucao = db.Execucoes.Find(item => item.OrdemServicoId == entrada.Payload.OrdemServicoId).Single();
        var servico = scope.ServiceProvider.GetRequiredService<CriarServicoUseCase>().Executar(new ServicoInput("Revisao " + Guid.NewGuid(), "", 100m));
        var peca = scope.ServiceProvider.GetRequiredService<CriarPecaInsumoUseCase>().Executar(new PecaInsumoInput("Filtro " + Guid.NewGuid(), "", 50m, 10));
        scope.ServiceProvider.GetRequiredService<IniciarDiagnosticoUseCase>().Executar(execucao.Id);
        scope.ServiceProvider.GetRequiredService<ConcluirDiagnosticoUseCase>().Executar(execucao.Id,
            new ConcluirDiagnosticoInput
            {
                Diagnostico = "Revisao completa",
                ServicoIds = [servico.Id],
                PecasInsumos = [new ItemPecaDiagnosticoInput { PecaInsumoId = peca.Id, Quantidade = 2 }]
            });
        return new MensagemIntegracao
        {
            MessageId = Guid.NewGuid(),
            Type = "LiberarExecucao",
            SagaId = entrada.SagaId,
            CausationId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            CorrelationId = entrada.CorrelationId,
            Payload = new DadosMensagem
            {
                OrdemServicoId = execucao.OrdemServicoId,
                ExecucaoId = execucao.Id,
                OrcamentoId = Guid.NewGuid(),
                PagamentoId = Guid.NewGuid(),
                ProvedorId = "mock-id",
                ValorTotal = 200m,
                Moeda = "BRL"
            }
        };
    }

    private static void Processar(ExecucaoApiFactory factory, MensagemIntegracao mensagem)
    { using var scope = factory.Services.CreateScope(); scope.ServiceProvider.GetRequiredService<IntegracaoCleanController>().Processar(mensagem); }
}
