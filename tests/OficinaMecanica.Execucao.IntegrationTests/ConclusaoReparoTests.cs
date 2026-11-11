using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Infrastructure.Persistence;
using OficinaMecanica.Execucao.Infrastructure.Sources;
using OficinaMecanica.Execucao.InterfaceAdapters.DTOs;
using static OficinaMecanica.Execucao.IntegrationTests.InicioReparoTests;

namespace OficinaMecanica.Execucao.IntegrationTests;

public class ConclusaoReparoTests
{
    [MongoFact]
    public async Task ConclusoesConcorrentes_ConsomemUmaVezComDataPersistidaEOutbox()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); await ApiTestHelper.Autenticar(client);
        var execucao = Criar(factory, 10); await Iniciar(client, execucao.Id);
        var respostas = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => client.PostAsync($"/api/execucoes/{execucao.Id}/concluir-reparo", null)));
        Assert.All(respostas, item => Assert.Equal(HttpStatusCode.OK, item.StatusCode));
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var salva = db.Execucoes.Find(item => item.Id == execucao.Id).Single();
        Assert.Equal(StatusExecucao.Finalizada, salva.Status); Assert.Equal(2, salva.Historico.Count);
        Assert.Equal(salva.ReparoConcluidoEm, Assert.Single(salva.ReservasPecas).ConsumidaEm);
        var peca = db.Pecas.Find(item => item.Id == execucao.PecasInsumos[0].PecaInsumoId).Single();
        Assert.Equal(8, peca.QuantidadeEstoque); Assert.Equal(0, peca.QuantidadeReservada); Assert.Equal(8, peca.QuantidadeDisponivel);
        foreach (var resposta in respostas)
        {
            var dto = await resposta.Content.ReadFromJsonAsync<ExecucaoResponseDto>(ApiTestHelper.Json);
            Assert.Equal(salva.ReparoConcluidoEm, dto!.ReparoConcluidoEm); Assert.NotNull(Assert.Single(dto.ReservasPecas).ConsumidaEm);
        }
        var outbox = new MongoOutboxDataSource(db); var evento = outbox.ObterPendentes(10).Single(item => item.Type == "ReparoConcluido");
        Assert.Equal(salva.Autorizacao!.MensagemLiberacaoId, evento.CausationId);
        Assert.Equal(salva.ReparoIniciadoEm, evento.Payload.ReparoIniciadoEm); Assert.Equal(salva.ReparoConcluidoEm, evento.OccurredAt);
        outbox.ConfirmarPublicacao(evento.MessageId);
        (await client.PostAsync($"/api/execucoes/{execucao.Id}/concluir-reparo", null)).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/execucoes/{execucao.Id}/iniciar-reparo", null)).EnsureSuccessStatusCode();
        Assert.DoesNotContain(outbox.ObterPendentes(10), item => item.Type == "ReparoConcluido");
        var fila = await client.GetFromJsonAsync<List<ExecucaoResponseDto>>("/api/execucoes", ApiTestHelper.Json);
        Assert.DoesNotContain(fila!, item => item.Id == execucao.Id);
        Assert.Equal(StatusExecucao.Finalizada, (await client.GetFromJsonAsync<ExecucaoResponseDto>($"/api/execucoes/{execucao.Id}", ApiTestHelper.Json))!.Status);
        // Publicacao nao pode ser desfeita por uma copia anterior da execucao.
        salva.Versao++;
        Assert.False(new MongoExecucaoDataSource(db).Atualizar(salva, salva.Versao - 1));
    }

    [MongoFact]
    public async Task InconsistenciaNaSegundaReserva_DesfazPrimeiroConsumoEConclusao()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); await ApiTestHelper.Autenticar(client);
        var execucao = Criar(factory, 10); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var segunda = new PecaInsumo { Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), Nome = "Segunda", QuantidadeEstoque = 5 };
        db.Pecas.InsertOne(segunda); execucao.PecasInsumos.Add(new ItemPecaDiagnostico { PecaInsumoId = segunda.Id, Quantidade = 1, PrecoUnitario = 50m });
        db.Execucoes.ReplaceOne(item => item.Id == execucao.Id, execucao); await Iniciar(client, execucao.Id);
        db.Pecas.UpdateOne(item => item.Id == segunda.Id, Builders<PecaInsumo>.Update.Set(item => item.QuantidadeReservada, 0));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/execucoes/{execucao.Id}/concluir-reparo", null)).StatusCode);
        var primeira = db.Pecas.Find(item => item.Id == execucao.PecasInsumos[0].PecaInsumoId).Single();
        Assert.Equal(10, primeira.QuantidadeEstoque); Assert.Equal(2, primeira.QuantidadeReservada);
        var salva = db.Execucoes.Find(item => item.Id == execucao.Id).Single();
        Assert.Equal(StatusExecucao.EmExecucao, salva.Status); Assert.Null(salva.ReparoConcluidoEm); Assert.Empty(salva.EventosConclusaoReparo);
        Assert.All(salva.ReservasPecas, item => Assert.Null(item.ConsumidaEm));
    }

    [MongoFact]
    public async Task DuasOSComReservasDaMesmaPeca_ConsomemSomenteSuasQuantidades()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); await ApiTestHelper.Autenticar(client);
        var primeira = Criar(factory, 6); var segunda = Criar(factory, 6, primeira.PecasInsumos[0].PecaInsumoId);
        await Iniciar(client, primeira.Id); await Iniciar(client, segunda.Id);
        var respostas = await Task.WhenAll(new[] { primeira, segunda }.Select(item => client.PostAsync($"/api/execucoes/{item.Id}/concluir-reparo", null)));
        Assert.All(respostas, item => item.EnsureSuccessStatusCode());
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var peca = db.Pecas.Find(item => item.Id == primeira.PecasInsumos[0].PecaInsumoId).Single();
        Assert.Equal(2, peca.QuantidadeEstoque); Assert.Equal(0, peca.QuantidadeReservada);
        Assert.Equal(2, db.Execucoes.Find(item => item.Status == StatusExecucao.Finalizada).ToList().Count);
    }

    [MongoTheory]
    [InlineData("ausente")]
    [InlineData("inativa")]
    [InlineData("fisico")]
    public async Task PecaInconsistente_NaoFinaliza(string erro)
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); await ApiTestHelper.Autenticar(client);
        var execucao = Criar(factory, 10); await Iniciar(client, execucao.Id);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        if (erro == "ausente") db.Pecas.DeleteMany(FilterDefinition<PecaInsumo>.Empty);
        else if (erro == "inativa") db.Pecas.UpdateMany(FilterDefinition<PecaInsumo>.Empty, Builders<PecaInsumo>.Update.Set(item => item.Ativo, false));
        else db.Pecas.UpdateMany(FilterDefinition<PecaInsumo>.Empty, Builders<PecaInsumo>.Update.Set(item => item.QuantidadeEstoque, 1));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/execucoes/{execucao.Id}/concluir-reparo", null)).StatusCode);
        var salva = db.Execucoes.Find(item => item.Id == execucao.Id).Single();
        Assert.Equal(StatusExecucao.EmExecucao, salva.Status); Assert.Empty(salva.EventosConclusaoReparo);
    }

    [MongoFact]
    public async Task SemLoginSemInicioOuIdInexistente_NaoConclui()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); var execucao = Criar(factory, 10);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync($"/api/execucoes/{execucao.Id}/concluir-reparo", null)).StatusCode);
        await ApiTestHelper.Autenticar(client);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/execucoes/{Guid.NewGuid()}/concluir-reparo", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/execucoes/{execucao.Id}/concluir-reparo", null)).StatusCode);
    }

    [MongoFact]
    public async Task SomenteServicos_FinalizaSemMovimentarEstoque()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); await ApiTestHelper.Autenticar(client);
        var execucao = Criar(factory, 10); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        execucao.PecasInsumos.Clear(); execucao.Servicos.Add(new ItemServicoDiagnostico { ServicoId = Guid.NewGuid(), Preco = 100m });
        db.Execucoes.ReplaceOne(item => item.Id == execucao.Id, execucao); await Iniciar(client, execucao.Id);
        (await client.PostAsync($"/api/execucoes/{execucao.Id}/concluir-reparo", null)).EnsureSuccessStatusCode();
        var salva = db.Execucoes.Find(item => item.Id == execucao.Id).Single();
        Assert.Equal(StatusExecucao.Finalizada, salva.Status); Assert.Empty(salva.ReservasPecas);
        var peca = db.Pecas.Find(FilterDefinition<PecaInsumo>.Empty).Single(); Assert.Equal(10, peca.QuantidadeEstoque); Assert.Equal(0, peca.QuantidadeReservada);
    }

    private static async Task Iniciar(HttpClient client, Guid id)
        => (await client.PostAsync($"/api/execucoes/{id}/iniciar-reparo", null)).EnsureSuccessStatusCode();
}
