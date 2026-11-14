using System.Net;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using OficinaMecanica.Execucao.Application.Exceptions;
using OficinaMecanica.Execucao.Application.Messaging;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Infrastructure.Persistence;
using OficinaMecanica.Execucao.Infrastructure.Sources;
using OficinaMecanica.Execucao.InterfaceAdapters.Controllers;
namespace OficinaMecanica.Execucao.IntegrationTests;

public class CompensacaoPagaTests
{
    [MongoFact]
    public async Task CancelamentosConcorrentes_LiberamUmaVezComRespostaDuravel()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); await ApiTestHelper.Autenticar(client);
        var liberacao = LiberacaoExecucaoTests.Criar(factory); Processar(factory, liberacao);
        (await client.PostAsync($"/api/execucoes/{liberacao.Payload.ExecucaoId}/iniciar-reparo", null)).EnsureSuccessStatusCode();
        var comando = Cancelar(liberacao);
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => Processar(factory, comando))));
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var salva = db.Execucoes.Find(item => item.Id == comando.Payload.ExecucaoId).Single();
        Assert.Equal(StatusExecucao.Cancelada, salva.Status); Assert.True(Assert.Single(salva.RespostasCancelamentoPago).Aceito);
        Assert.NotNull(salva.ReservasPecas[0].LiberadaEm); Assert.Null(salva.ReservasPecas[0].ConsumidaEm);
        var peca = db.Pecas.Find(FilterDefinition<PecaInsumo>.Empty).Single(); Assert.Equal(10, peca.QuantidadeEstoque); Assert.Equal(0, peca.QuantidadeReservada);
        var outbox = new MongoOutboxDataSource(db); var resposta = outbox.ObterPendentes(20).Single(item => item.Type == "ReparoPagoCancelado");
        Assert.Equal(comando.MessageId, resposta.CausationId); outbox.ConfirmarPublicacao(resposta.MessageId);
        Processar(factory, comando); Assert.DoesNotContain(outbox.ObterPendentes(20), item => item.MessageId == resposta.MessageId);
        comando.Payload.Motivo = "Outro motivo"; Assert.Throws<ValidacaoException>(() => Processar(factory, comando));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/execucoes/{liberacao.Payload.ExecucaoId}/iniciar-reparo", null)).StatusCode);
    }
    [MongoTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancelamentoAntesDoInicio_InclusiveAntesDaLiberacao_NaoReservaPecas(bool liberar)
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient();
        var liberacao = LiberacaoExecucaoTests.Criar(factory); if (liberar) Processar(factory, liberacao);
        Processar(factory, Cancelar(liberacao)); if (!liberar) Processar(factory, liberacao);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var salva = db.Execucoes.Find(item => item.Id == liberacao.Payload.ExecucaoId).Single();
        Assert.Equal(StatusExecucao.Cancelada, salva.Status); Assert.Empty(salva.ReservasPecas);
        Assert.Equal(0, db.Pecas.Find(FilterDefinition<PecaInsumo>.Empty).Single().QuantidadeReservada);
    }
    [MongoFact]
    public async Task FalhaNaSegundaLiberacao_DesfazPrimeiraEGravaRecusaSemPararReparo()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); await ApiTestHelper.Autenticar(client);
        var liberacao = LiberacaoExecucaoTests.Criar(factory); using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>(); var execucao = db.Execucoes.Find(item => item.Id == liberacao.Payload.ExecucaoId).Single();
        var segunda = new PecaInsumo { Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), Nome = "Segunda", QuantidadeEstoque = 5 };
        db.Pecas.InsertOne(segunda); execucao.PecasInsumos.Add(new ItemPecaDiagnostico { PecaInsumoId = segunda.Id, Quantidade = 1, PrecoUnitario = 50m });
        db.Execucoes.ReplaceOne(item => item.Id == execucao.Id, execucao); liberacao.Payload.ValorTotal = 250m; Processar(factory, liberacao);
        (await client.PostAsync($"/api/execucoes/{execucao.Id}/iniciar-reparo", null)).EnsureSuccessStatusCode();
        db.Pecas.UpdateOne(item => item.Id == segunda.Id, Builders<PecaInsumo>.Update.Set(item => item.QuantidadeReservada, 0));
        Processar(factory, Cancelar(liberacao)); var salva = db.Execucoes.Find(item => item.Id == execucao.Id).Single();
        Assert.Equal(StatusExecucao.EmExecucao, salva.Status); Assert.False(Assert.Single(salva.RespostasCancelamentoPago).Aceito);
        Assert.All(salva.ReservasPecas, item => Assert.Null(item.LiberadaEm)); Assert.Null(salva.ReparoCanceladoEm);
        var primeira = db.Pecas.Find(item => item.Id != segunda.Id).Single(); Assert.Equal(10, primeira.QuantidadeEstoque); Assert.Equal(2, primeira.QuantidadeReservada);
    }
    [MongoFact]
    public async Task DisputaComConclusao_NuncaCancelaEConsomeAoMesmoTempo()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); await ApiTestHelper.Autenticar(client);
        var liberacao = LiberacaoExecucaoTests.Criar(factory); Processar(factory, liberacao);
        (await client.PostAsync($"/api/execucoes/{liberacao.Payload.ExecucaoId}/iniciar-reparo", null)).EnsureSuccessStatusCode();
        var cancelamento = Task.Run(() => Processar(factory, Cancelar(liberacao)));
        var resposta = await client.PostAsync($"/api/execucoes/{liberacao.Payload.ExecucaoId}/concluir-reparo", null); await cancelamento;
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var salva = db.Execucoes.Find(item => item.Id == liberacao.Payload.ExecucaoId).Single(); var peca = db.Pecas.Find(FilterDefinition<PecaInsumo>.Empty).Single();
        var aceito = Assert.Single(salva.RespostasCancelamentoPago).Aceito;
        Assert.Equal(aceito ? StatusExecucao.Cancelada : StatusExecucao.Finalizada, salva.Status);
        Assert.Equal(aceito ? 10 : 8, peca.QuantidadeEstoque); Assert.Equal(0, peca.QuantidadeReservada);
        Assert.Equal(aceito ? HttpStatusCode.Conflict : HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(aceito, salva.ReservasPecas[0].LiberadaEm.HasValue); Assert.Equal(!aceito, salva.ReservasPecas[0].ConsumidaEm.HasValue);
    }
    [MongoFact]
    public async Task ReparoJaConcluido_RespondeRecusaSemDevolverPecas()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); await ApiTestHelper.Autenticar(client);
        var liberacao = LiberacaoExecucaoTests.Criar(factory); Processar(factory, liberacao);
        (await client.PostAsync($"/api/execucoes/{liberacao.Payload.ExecucaoId}/iniciar-reparo", null)).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/execucoes/{liberacao.Payload.ExecucaoId}/concluir-reparo", null)).EnsureSuccessStatusCode(); Processar(factory, Cancelar(liberacao));
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var salva = db.Execucoes.Find(item => item.Id == liberacao.Payload.ExecucaoId).Single();
        Assert.Equal(StatusExecucao.Finalizada, salva.Status); Assert.False(Assert.Single(salva.RespostasCancelamentoPago).Aceito);
        Assert.Equal(8, db.Pecas.Find(FilterDefinition<PecaInsumo>.Empty).Single().QuantidadeEstoque);
    }
    internal static MensagemIntegracao Cancelar(MensagemIntegracao liberacao) => new()
    {
        MessageId = Guid.NewGuid(),
        Type = "CancelarReparoPago",
        SagaId = liberacao.SagaId,
        CausationId = liberacao.MessageId,
        CorrelationId = liberacao.CorrelationId,
        OccurredAt = DateTime.UtcNow,
        Payload = new DadosMensagem
        {
            OrdemServicoId = liberacao.Payload.OrdemServicoId,
            ExecucaoId = liberacao.Payload.ExecucaoId,
            OrcamentoId = liberacao.Payload.OrcamentoId,
            PagamentoId = liberacao.Payload.PagamentoId,
            ProvedorId = liberacao.Payload.ProvedorId,
            ValorTotal = liberacao.Payload.ValorTotal,
            Moeda = "BRL",
            Motivo = "Desistencia"
        }
    };
    private static void Processar(ExecucaoApiFactory factory, MensagemIntegracao mensagem)
    { using var scope = factory.Services.CreateScope(); scope.ServiceProvider.GetRequiredService<IntegracaoCleanController>().Processar(mensagem); }
}
